using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace LangQueToi.EditorTools
{
    /// <summary>
    /// Combined gate: catalog coverage, formatter contract, mapping counts, static reports,
    /// forbidden English literals. Fails the process if any fatal check fails.
    /// </summary>
    public static class LQTLocalizationValidator
    {
        private const string ArtifactPath = "Artifacts/Localization/coverage.json";

        // Keys that are registered but only referenced by randomized selection at runtime.
        private static readonly HashSet<string> AllowedUnusedKeys = new(StringComparer.Ordinal)
        {
            "dialogue.shop.greeting.0",
            "dialogue.shop.greeting.1",
            "dialogue.shop.greeting.2",
            "dialogue.shop.greeting.3",
        };

        private static readonly string[] ForbiddenEnglishLiterals =
        {
            "Not time to sleep yet!", "Inventory Full", "Already fed today!",
            "Clove paid you", "Partial order:", "Order delivered!",
            "TOTAL:", "Price:", "Sell:", " Days", "Saving...", "Saved!",
        };

        [MenuItem("LangQueToi/Localization/Run Validator")]
        public static void RunMenu() => Run();

        public static void RunFromCommandLine()
        {
            Report r = Run();
            if (r.fatalCount > 0)
                throw new BuildFailedException($"[LQTLocalizationValidator] fatalCount={r.fatalCount}");
        }

        private static Report Run()
        {
            var r = new Report
            {
                timestampUtc = DateTime.UtcNow.ToString("o"),
            };

            string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
            string[] allCsFiles = Directory
                .GetFiles(scriptsRoot, "*.cs", SearchOption.AllDirectories)
                .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}Editor{Path.DirectorySeparatorChar}"))
                .ToArray();
            string fullSource = string.Join("\n", allCsFiles.Select(File.ReadAllText));

            // --- Catalog coverage: every Loc.Get / Loc.Format literal is registered ---
            var locCallRegex = new Regex(@"Loc\.(Get|Format)\(\s*""([^""]+)""", RegexOptions.Compiled);
            var referencedKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (Match m in locCallRegex.Matches(fullSource))
                referencedKeys.Add(m.Groups[2].Value);

            var registeredKeys = GetRegisteredKeys();
            r.checks.Add(new Check("registered_keys_count", registeredKeys.Count.ToString(), "info"));
            r.checks.Add(new Check("referenced_keys_count", referencedKeys.Count.ToString(), "info"));

            foreach (string key in referencedKeys)
                if (!registeredKeys.Contains(key))
                    Fatal(r, "missing_catalog_key", key, $"Loc call references unregistered key: {key}");

            foreach (string key in registeredKeys)
                if (!referencedKeys.Contains(key) && !AllowedUnusedKeys.Contains(key))
                    Warn(r, "unused_catalog_key", key, $"Registered key never referenced: {key}");

            // --- Forbidden English literals (warning only; false positives from Debug.Log/comments) ---
            foreach (string bad in ForbiddenEnglishLiterals)
                if (fullSource.Contains(bad))
                    Warn(r, "forbidden_english_literal", bad, $"English literal appears in source (may be a Debug.Log or comment): '{bad}'");

            // --- Formatter placeholder contract (must be contiguous {0}, {1}, ...) ---
            foreach (KeyValuePair<string, string> kv in EnumerateEntries())
            {
                if (!ValidatePlaceholders(kv.Value, out string reason))
                    Fatal(r, "invalid_placeholders", kv.Key, $"{kv.Key}: {reason}");
            }

            // --- Data mapping counts ---
            r.checks.Add(new Check("manifest_items", LQTLocalizationManifest.ItemNames.Count.ToString(),
                LQTLocalizationManifest.ItemNames.Count == LQTLocalizationManifest.ExpectedItemCount ? "ok" : "fatal"));
            if (LQTLocalizationManifest.ItemNames.Count != LQTLocalizationManifest.ExpectedItemCount)
                Fatal(r, "manifest_items_count", "items", $"expected {LQTLocalizationManifest.ExpectedItemCount}, got {LQTLocalizationManifest.ItemNames.Count}");

            int fishInManifest = LQTLocalizationManifest.ItemNames.Keys
                .Count(p => p.StartsWith(LQTLocalizationManifest.FishPathPrefix, StringComparison.Ordinal));
            if (fishInManifest != LQTLocalizationManifest.ExpectedFishCount)
                Fatal(r, "manifest_fish_count", "fish", $"expected {LQTLocalizationManifest.ExpectedFishCount}, got {fishInManifest}");

            if (LQTLocalizationManifest.SeedNames.Count != LQTLocalizationManifest.ExpectedSeedCount)
                Fatal(r, "manifest_seeds_count", "seeds", $"expected {LQTLocalizationManifest.ExpectedSeedCount}, got {LQTLocalizationManifest.SeedNames.Count}");
            if (LQTLocalizationManifest.CropNames.Count != LQTLocalizationManifest.ExpectedCropCount)
                Fatal(r, "manifest_crops_count", "crops", $"expected {LQTLocalizationManifest.ExpectedCropCount}, got {LQTLocalizationManifest.CropNames.Count}");
            if (LQTLocalizationManifest.ShopNames.Count != LQTLocalizationManifest.ExpectedShopCount)
                Fatal(r, "manifest_shops_count", "shops", $"expected {LQTLocalizationManifest.ExpectedShopCount}, got {LQTLocalizationManifest.ShopNames.Count}");

            // --- Verify serialized assets carry Vietnamese values (post-apply gate) ---
            foreach (KeyValuePair<string, string> kv in LQTLocalizationManifest.ItemNames)
                CheckStringField(r, kv.Key, "itemName", kv.Value);
            foreach (KeyValuePair<string, string> kv in LQTLocalizationManifest.SeedNames)
                CheckStringField(r, kv.Key, "seedName", kv.Value);
            foreach (KeyValuePair<string, string> kv in LQTLocalizationManifest.CropNames)
                CheckStringField(r, kv.Key, "cropName", kv.Value);
            foreach (KeyValuePair<string, string> kv in LQTLocalizationManifest.ShopNames)
                CheckStringField(r, kv.Key, "shopName", kv.Value);

            r.passed = r.fatalCount == 0;
            WriteReport(r);
            Debug.Log($"[LQTLocalizationValidator] passed={r.passed} fatal={r.fatalCount} warn={r.warningCount} artifact={ArtifactPath}");
            return r;
        }

        private static void CheckStringField(Report r, string assetPath, string propertyName, string expected)
        {
            UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (asset == null)
            {
                Fatal(r, "asset_missing", assetPath, $"{assetPath} not found for validation");
                return;
            }
            var so = new SerializedObject(asset);
            SerializedProperty p = so.FindProperty(propertyName);
            if (p == null || p.propertyType != SerializedPropertyType.String)
            {
                Fatal(r, "field_missing", $"{assetPath}::{propertyName}", $"Missing string property {propertyName} on {assetPath}");
                return;
            }
            if (p.stringValue.Contains("⟦"))
                Fatal(r, "loc_marker_serialized", assetPath, $"⟦ marker leaked into serialized asset: {assetPath}");
            if (p.stringValue != expected)
                Fatal(r, "value_not_localized", $"{assetPath}::{propertyName}",
                    $"{assetPath} {propertyName}: expected \"{expected}\", got \"{p.stringValue}\"");
        }

        private static bool ValidatePlaceholders(string template, out string reason)
        {
            reason = null;
            var matches = Regex.Matches(template, @"\{(\d+)(?::[^}]*)?\}");
            if (matches.Count == 0) return true;
            var indices = matches.Cast<Match>().Select(m => int.Parse(m.Groups[1].Value)).Distinct().OrderBy(x => x).ToArray();
            for (int i = 0; i < indices.Length; i++)
            {
                if (indices[i] != i)
                {
                    reason = $"non-contiguous placeholders {string.Join(",", indices)}";
                    return false;
                }
            }
            return true;
        }

        private static HashSet<string> GetRegisteredKeys()
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, string> kv in EnumerateEntries())
                result.Add(kv.Key);
            return result;
        }

        private static IEnumerable<KeyValuePair<string, string>> EnumerateEntries()
        {
            // Duplicated map (kept in sync with Loc.cs). Runtime dictionary is private; enumerating via
            // reflection would couple this validator to Loc internals. This mirror is intentional.
            string[][] pairs =
            {
                new[] { "save.saving", "Đang lưu…" },
                new[] { "save.saved", "Đã lưu" },
                new[] { "calendar.day", "Ngày {0}" },
                new[] { "bed.too_early", "Chưa tới giờ ngủ đâu!" },
                new[] { "inventory.full", "Túi đồ đã đầy." },
                new[] { "animal.already_fed", "Hôm nay đã cho ăn rồi!" },
                new[] { "animal.need_feed", "Cần {0}!" },
                new[] { "shop.sell.success", "Bà Năm trả cháu {0}." },
                new[] { "shop.buy.partial", "Đã chi {0}; {1} món chưa giao được." },
                new[] { "shop.buy.success", "Đơn hàng đã giao, tổng cộng {0}." },
                new[] { "shop.total", "TỔNG: {0}" },
                new[] { "shop.sell_unit", "Bán: {0}" },
                new[] { "shop.sell_stock", "Bán: {0} - x{1}" },
                new[] { "shop.buy_unit", "Giá: {0}" },
                new[] { "dialogue.shop.greeting.0", "Hôm nay trời đẹp quá ha, cháu! Hàng mới vừa lên kệ đó, coi thử có món nào ưng không nhen?" },
                new[] { "dialogue.shop.greeting.1", "Cháu ghé chơi đó hả? Bà mới sắp hàng xong, cứ coi thong thả nhen." },
                new[] { "dialogue.shop.greeting.2", "Vô coi hàng đi cháu, hôm nay có mấy món tươi ngon lắm đó." },
                new[] { "dialogue.shop.greeting.3", "Bà Năm chờ cháu nãy giờ. Cần mua bán gì thì nói bà nghe nhen." },
            };
            foreach (string[] p in pairs)
                yield return new KeyValuePair<string, string>(p[0], p[1]);
        }

        private static void Fatal(Report r, string category, string key, string detail)
        {
            r.findings.Add(new Finding { severity = "fatal", category = category, key = key, detail = detail });
            r.fatalCount++;
        }

        private static void Warn(Report r, string category, string key, string detail)
        {
            r.findings.Add(new Finding { severity = "warning", category = category, key = key, detail = detail });
            r.warningCount++;
        }

        private static void WriteReport(Report r)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ArtifactPath));
            File.WriteAllText(ArtifactPath, JsonUtility.ToJson(r, true), new System.Text.UTF8Encoding(false));
        }

        [Serializable]
        public class Check
        {
            public string name;
            public string value;
            public string status;

            public Check() { }
            public Check(string name, string value, string status) { this.name = name; this.value = value; this.status = status; }
        }

        [Serializable]
        public class Finding
        {
            public string severity;
            public string category;
            public string key;
            public string detail;
        }

        [Serializable]
        public class Report
        {
            public string timestampUtc;
            public bool passed;
            public int fatalCount;
            public int warningCount;
            public List<Check> checks = new List<Check>();
            public List<Finding> findings = new List<Finding>();
        }
    }
}
