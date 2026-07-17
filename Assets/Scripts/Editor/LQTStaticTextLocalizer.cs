using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LangQueToi.EditorTools
{
    /// <summary>
    /// Migrates hardcoded English TMP_Text values on scenes and prefabs into Vietnamese,
    /// keyed by exact asset path + hierarchy path + expected-source guard.
    ///
    /// Never touches GameObject.name or GUIDs. Compares full text after \r\n → \n
    /// normalization only. Idempotent: if current text already equals target, skip.
    /// If current text is neither expected source nor target, fatal mismatch.
    /// </summary>
    public static class LQTStaticTextLocalizer
    {
        private const string ArtifactDirectory = "Artifacts/Localization";
        private const string DryRunArtifact = ArtifactDirectory + "/static-dry-run.json";
        private const string AppliedArtifact = ArtifactDirectory + "/static-applied.json";

        [MenuItem("LangQueToi/Localization/Static Text - Dry Run")]
        public static void DryRunMenu() => Run(apply: false, artifact: DryRunArtifact);

        [MenuItem("LangQueToi/Localization/Static Text - Apply")]
        public static void ApplyMenu() => Run(apply: true, artifact: AppliedArtifact);

        public static void DryRunFromCommandLine()
        {
            Report report = Run(apply: false, artifact: DryRunArtifact);
            if (report.errors.Count > 0)
                throw new BuildFailedException($"[LQTStaticTextLocalizer] Dry-run has {report.errors.Count} error(s).");
        }

        public static void ApplyFromCommandLine()
        {
            Report report = Run(apply: true, artifact: AppliedArtifact);
            if (report.errors.Count > 0)
                throw new BuildFailedException($"[LQTStaticTextLocalizer] Apply has {report.errors.Count} error(s).");
        }

        private static Report Run(bool apply, string artifact)
        {
            var report = new Report
            {
                mode = apply ? "apply" : "dry-run",
                timestampUtc = DateTime.UtcNow.ToString("o"),
            };

            // --- Scenes ---
            var byScene = new Dictionary<string, List<StaticEntry>>(StringComparer.Ordinal);
            foreach (StaticEntry e in StaticMappings.All)
            {
                if (!e.assetPath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase)) continue;
                if (!byScene.TryGetValue(e.assetPath, out var list))
                {
                    list = new List<StaticEntry>();
                    byScene[e.assetPath] = list;
                }
                list.Add(e);
            }
            foreach (KeyValuePair<string, List<StaticEntry>> kv in byScene)
                ProcessScene(kv.Key, kv.Value, apply, report);

            // --- Prefabs ---
            var byPrefab = new Dictionary<string, List<StaticEntry>>(StringComparer.Ordinal);
            foreach (StaticEntry e in StaticMappings.All)
            {
                if (!e.assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)) continue;
                if (!byPrefab.TryGetValue(e.assetPath, out var list))
                {
                    list = new List<StaticEntry>();
                    byPrefab[e.assetPath] = list;
                }
                list.Add(e);
            }
            foreach (KeyValuePair<string, List<StaticEntry>> kv in byPrefab)
                ProcessPrefab(kv.Key, kv.Value, apply, report);

            report.counts.total = StaticMappings.All.Length;
            report.counts.matched = CountByStatus(report, "matched");
            report.counts.applied = CountByStatus(report, "applied");
            report.counts.already_target = CountByStatus(report, "already_target");
            report.counts.mismatched = CountByStatus(report, "mismatch");
            report.counts.missing = CountByStatus(report, "missing_gameobject") + CountByStatus(report, "missing_tmp");
            report.counts.errors = report.errors.Count;

            WriteReport(artifact, report);
            Debug.Log(
                $"[LQTStaticTextLocalizer] mode={report.mode} total={report.counts.total} " +
                $"applied={report.counts.applied} already_target={report.counts.already_target} " +
                $"mismatch={report.counts.mismatched} missing={report.counts.missing} " +
                $"errors={report.counts.errors} artifact={artifact}");
            return report;
        }

        private static void ProcessScene(string scenePath, List<StaticEntry> entries, bool apply, Report report)
        {
            Scene scene;
            try
            {
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }
            catch (Exception ex)
            {
                report.errors.Add($"Failed to open scene {scenePath}: {ex.Message}");
                return;
            }

            var roots = new List<GameObject>();
            scene.GetRootGameObjects(roots);
            bool anyModified = false;
            foreach (StaticEntry entry in entries)
            {
                GameObject target = ResolveInRoots(roots, entry.hierarchyPath);
                if (!MigrateOne(target, entry, apply, report)) continue;
                anyModified = true;
            }

            if (apply && anyModified)
                EditorSceneManager.SaveScene(scene);
        }

        private static void ProcessPrefab(string prefabPath, List<StaticEntry> entries, bool apply, Report report)
        {
            GameObject prefabRoot;
            try
            {
                prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            }
            catch (Exception ex)
            {
                report.errors.Add($"Failed to load prefab {prefabPath}: {ex.Message}");
                return;
            }
            if (prefabRoot == null)
            {
                report.errors.Add($"Prefab load returned null: {prefabPath}");
                return;
            }

            bool anyModified = false;
            foreach (StaticEntry entry in entries)
            {
                GameObject target = ResolveInPrefab(prefabRoot, entry.hierarchyPath);
                if (!MigrateOne(target, entry, apply, report)) continue;
                anyModified = true;
            }

            if (apply && anyModified)
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        private static GameObject ResolveInRoots(List<GameObject> roots, string hierarchyPath)
        {
            string[] segments = hierarchyPath.Split('/');
            if (segments.Length == 0) return null;

            foreach (GameObject root in roots)
            {
                if (root.name != segments[0]) continue;
                if (segments.Length == 1) return root;
                Transform t = root.transform;
                for (int i = 1; i < segments.Length; i++)
                {
                    t = t.Find(segments[i]);
                    if (t == null) return null;
                }
                return t.gameObject;
            }
            return null;
        }

        private static GameObject ResolveInPrefab(GameObject prefabRoot, string hierarchyPath)
        {
            string[] segments = hierarchyPath.Split('/');
            if (segments.Length == 0 || prefabRoot.name != segments[0]) return null;
            if (segments.Length == 1) return prefabRoot;

            Transform t = prefabRoot.transform;
            for (int i = 1; i < segments.Length; i++)
            {
                t = t.Find(segments[i]);
                if (t == null) return null;
            }
            return t.gameObject;
        }

        private static bool MigrateOne(GameObject target, StaticEntry entry, bool apply, Report report)
        {
            var log = new EntryReport(entry.assetPath, entry.hierarchyPath, entry.expectedSource, entry.target);
            report.entries.Add(log);

            if (target == null)
            {
                log.status = "missing_gameobject";
                report.errors.Add($"Missing GameObject: {entry.assetPath} :: {entry.hierarchyPath}");
                return false;
            }

            TMP_Text tmp = target.GetComponent<TMP_Text>();
            if (tmp == null)
            {
                log.status = "missing_tmp";
                report.errors.Add($"Missing TMP_Text: {entry.assetPath} :: {entry.hierarchyPath}");
                return false;
            }

            string current = Normalize(tmp.text);
            string source = Normalize(entry.expectedSource);
            string target_ = Normalize(entry.target);

            log.currentNormalized = current;

            if (current == target_)
            {
                log.status = "already_target";
                return false;
            }

            if (current != source)
            {
                log.status = "mismatch";
                report.errors.Add(
                    $"Source mismatch (neither expected nor target): {entry.assetPath} :: {entry.hierarchyPath}");
                return false;
            }

            log.status = "matched";
            if (apply)
            {
                var serialized = new SerializedObject(tmp);
                SerializedProperty text = serialized.FindProperty("m_text");
                if (text == null)
                {
                    log.status = "missing_property";
                    report.errors.Add($"Missing m_text property: {entry.assetPath} :: {entry.hierarchyPath}");
                    return false;
                }
                text.stringValue = entry.target;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(tmp);
                log.status = "applied";
                return true;
            }
            return false;
        }

        private static string Normalize(string s) => s?.Replace("\r\n", "\n") ?? string.Empty;

        private static int CountByStatus(Report r, string status)
        {
            int n = 0;
            foreach (EntryReport e in r.entries)
                if (e.status == status) n++;
            return n;
        }

        private static void WriteReport(string artifact, Report r)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(artifact));
            File.WriteAllText(artifact, JsonUtility.ToJson(r, true), new System.Text.UTF8Encoding(false));
        }

        [Serializable]
        public class EntryReport
        {
            public string assetPath;
            public string hierarchyPath;
            public string expectedSource;
            public string target;
            public string currentNormalized;
            public string status;

            public EntryReport() { }
            public EntryReport(string a, string h, string es, string t)
            {
                assetPath = a; hierarchyPath = h; expectedSource = es; target = t;
            }
        }

        [Serializable]
        public class Counts
        {
            public int total;
            public int matched;
            public int applied;
            public int already_target;
            public int mismatched;
            public int missing;
            public int errors;
        }

        [Serializable]
        public class Report
        {
            public string mode;
            public string timestampUtc;
            public Counts counts = new Counts();
            public List<EntryReport> entries = new List<EntryReport>();
            public List<string> errors = new List<string>();
        }
    }

    /// <summary>
    /// Immutable mapping table. `expectedSource` is what the scene/prefab text must be
    /// today (English original); `target` is the Vietnamese replacement. Migration is
    /// fatal if current text is neither.
    /// </summary>
    internal readonly struct StaticEntry
    {
        public readonly string assetPath;
        public readonly string hierarchyPath;
        public readonly string expectedSource;
        public readonly string target;

        public StaticEntry(string assetPath, string hierarchyPath, string expectedSource, string target)
        {
            this.assetPath = assetPath;
            this.hierarchyPath = hierarchyPath;
            this.expectedSource = expectedSource;
            this.target = target;
        }
    }

    internal static class StaticMappings
    {
        private const string MenuScene = "Assets/Scenes/MenuScene.unity";
        private const string MainScene = "Assets/Scenes/MainScene.unity";

        private const string LongAboutSource =
"The Sprouty is a cozy 2D pixel art farming game where you tend your land, grow crops, harvest and explore the world at your own peaceful pace.\n" +
"\n" +
"─────────────────────────\n" +
"\n" +
"Game Development Course Project\n" +
"Dalat University (DLU)\n" +
"\n" +
"─────────────────────────\n" +
"\n" +
"  DEVELOPMENT TEAM\n" +
"\n" +
"Nguyen Dinh Thach\n" +
"  ID: 2314506\n" +
"\n" +
"Pham Nguyen Ngoc Phuoc\n" +
"  ID: 2312718\n" +
"\n" +
"Nguyen Van Quoc\n" +
"  ID: 2312729";

        private const string LongAboutTarget =
"Làng Quê Tôi là trò chơi nông trại pixel 2D ấm áp, nơi bạn chăm ruộng,\n" +
"gieo trồng, thu hoạch và khám phá miền quê theo nhịp sống yên bình.\n" +
"\n" +
"─────────────────────────\n" +
"\n" +
"Đồ án môn Phát triển Trò chơi\n" +
"Trường Đại học Đà Lạt (DLU)\n" +
"\n" +
"─────────────────────────\n" +
"\n" +
" NHÓM PHÁT TRIỂN\n" +
"\n" +
"Nguyen Dinh Thach\n" +
"  MSSV: 2314506\n" +
"\n" +
"Pham Nguyen Ngoc Phuoc\n" +
"  MSSV: 2312718\n" +
"\n" +
"Nguyen Van Quoc\n" +
"  MSSV: 2312729";

        public static readonly StaticEntry[] All =
        {
            // MenuScene
            new(MenuScene, "Canvas/AboutPanel/HeaderText", "ABOUT", "GIỚI THIỆU"),
            new(MenuScene, "Canvas/SettingsPanel/Rows/FullscreenRow/FullscreenText", "Fullscreen", "Toàn màn hình"),
            new(MenuScene, "Canvas/SettingsPanel/Rows/AmbienceRow/AmbienceText", "Ambience", "Âm thanh môi trường"),
            new(MenuScene, "Canvas/SettingsPanel/Rows/MusicRow/MusicText", "Music", "Nhạc"),
            new(MenuScene, "Canvas/CreditsPanel/HeaderText", "CREDITS", "GHI CÔNG"),
            new(MenuScene, "Canvas/ConfirmRemovePanel/ConfirmText", "DO YOU WANT TO REMOVE THIS SAVE?", "XÓA DỮ LIỆU LƯU NÀY?"),
            new(MenuScene, "Canvas/AboutPanel/Scroll View/Viewport/Content/AboutText", LongAboutSource, LongAboutTarget),
            new(MenuScene, "Canvas/SavePanel/HeaderText", "PICK A SAVE", "CHỌN Ô LƯU"),
            new(MenuScene, "Canvas/SettingsPanel/Rows/SFXRow/SFXText", "SFX", "Hiệu ứng"),
            new(MenuScene, "Canvas/SettingsPanel/Rows/TargetFPSRow/TargetFPSText", "Target FPS", "FPS mục tiêu"),
            new(MenuScene, "Canvas/ExitGamePanel/ConfirmText", "DO YOU WANT TO EXIT THE GAME?", "BẠN MUỐN THOÁT TRÒ CHƠI?"),

            // MainScene
            new(MainScene, "Canvas/ShopPanel/BookContainer/SellPage/NoteText", "* Your tems will be sold the next morning", "* Hàng sẽ được bán vào sáng hôm sau"),
            new(MainScene, "Canvas/PausePanel/ExitGameButton/ExitGameText", "EXIT GAME", "THOÁT TRÒ CHƠI"),
            new(MainScene, "Canvas/ItemActionPanel/SellButton/SelllText", "Sell", "Bán"),
            new(MainScene, "Canvas/PausePanel/OpenSettingsButton/OpenSettingsText", "SETTINGS", "CÀI ĐẶT"),
            new(MainScene, "Canvas/ItemActionPanel/SellAllButton/SellAllText", "Sell All", "Bán tất cả"),
            new(MainScene, "Canvas/ShopPanel/BookContainer/SellPage/MyItemsHeader", "MY ITEMS", "ĐỒ CỦA TÔI"),
            new(MainScene, "Canvas/ShopPanel/BookContainer/BuyPage/NoteText", "* Items will arrive the next morning", "* Hàng sẽ được giao vào sáng hôm sau"),
            new(MainScene, "Canvas/DialoguePanel/DialogueBox/ChoicesContainer/GoodbyeButton/GoodbyeText", "Take care!", "Bà giữ sức khỏe nhen!"),
            new(MainScene, "Canvas/SettingsPanel/Rows/FullscreenRow/FullscreenText", "Fullscreen", "Toàn màn hình"),
            new(MainScene, "Canvas/ShopPanel/BookContainer/SellPage/ToSellHeader", "TO SELL", "HÀNG SẼ BÁN"),
            new(MainScene, "Canvas/ShopPanel/BookContainer/TabsContainer/BuyTabButton/BuyTabText", "Buy", "Mua"),
            new(MainScene, "Canvas/PausePanel/BackToMenuButton/BackToMenuText", "BACK TO MENU", "VỀ MENU"),
            new(MainScene, "Canvas/DialoguePanel/DialogueBox/NPCNameText", "Clove", "Bà Năm"),
            new(MainScene, "Canvas/PausePanel/ResumeGameButton/ResumeGameButton", "RESUME", "TIẾP TỤC"),
            new(MainScene, "Canvas/SettingsPanel/Rows/TargetFPSRow/TargetFPSText", "Target FPS", "FPS mục tiêu"),
            new(MainScene, "Canvas/ShopPanel/BookContainer/TabsContainer/SellTabButton/SellTabText", "Sell", "Bán"),
            new(MainScene, "Canvas/DialoguePanel/DialogueBox/ChoicesContainer/ShopButton/ShopText", "Show what you've got!", "Cho cháu xem hàng với!"),
            new(MainScene, "Canvas/SettingsPanel/Rows/SFXRow/SFXText", "SFX", "Hiệu ứng"),
            new(MainScene, "Canvas/ShopPanel/BookContainer/BuyPage/ShopHeader", "SHOP", "TIỆM BÀ NĂM"),
            new(MainScene, "Canvas/ShopPanel/BookContainer/BuyPage/OrderHeader", "ORDER", "ĐƠN HÀNG"),
            new(MainScene, "Canvas/SettingsPanel/Rows/AmbienceRow/AmbienceText", "Ambience", "Âm thanh môi trường"),
            new(MainScene, "Canvas/ItemActionPanel/RemoveButton/RemoveText", "Remove", "Bỏ"),
            new(MainScene, "Canvas/TimePanel/ClockGroup/Text/LabelText", "DAY", "NGÀY"),
            new(MainScene, "Canvas/SettingsPanel/Rows/MusicRow/MusicText", "Music", "Nhạc"),
            new(MainScene, "Canvas/SavingIndicator/SavingLabel", "Saving...", "Đang lưu…"),
            new(MainScene, "Canvas/ExitGamePanel/ConfirmText", "DO YOU WANT TO EXIT THE GAME?", "BẠN MUỐN THOÁT TRÒ CHƠI?"),

            // Prefabs
            new("Assets/Prefabs/ExitGamePanel.prefab", "ExitGamePanel/ConfirmText", "DO YOU WANT TO EXIT THE GAME?", "BẠN MUỐN THOÁT TRÒ CHƠI?"),
            new("Assets/Prefabs/UI/EmptySaveSlot.prefab", "EmptySaveSlot/NewGameText", "START A NEW GAME", "BẮT ĐẦU TRÒ CHƠI MỚI"),
            new("Assets/Prefabs/UI/FilledSaveSlot.prefab", "FilledSaveSlot/SlotPanel/InfoGroup/IslandText", "THE SPROUTY ISLAND", "LÀNG QUÊ TÔI"),
            new("Assets/Prefabs/UI/FilledSaveSlot.prefab", "FilledSaveSlot/SlotPanel/InfoGroup/DayText", "x Days", "Ngày x"),
            new("Assets/Prefabs/UI/MyItemsRow.prefab", "MyItemsRow/NamePriceGroup/ItemNameText", "Carrot Seed", "Hạt giống cà rốt"),
            new("Assets/Prefabs/UI/MyItemsRow.prefab", "MyItemsRow/NamePriceGroup/PriceText", "Sell: 2 - x1", "Bán: 2 ₫ - x1"),
            new("Assets/Prefabs/UI/OrderItemRow.prefab", "OrderItemRow/NamePriceGroup/ItemNameText", "Carrot Seed", "Hạt giống cà rốt"),
            new("Assets/Prefabs/UI/OrderItemRow.prefab", "OrderItemRow/NamePriceGroup/PriceText", "Price: 2", "Giá: 2 ₫"),
            new("Assets/Prefabs/UI/ShopItemRow.prefab", "ShopItemRow/NamePriceGroup/ItemNameText", "Carrot Seed", "Hạt giống cà rốt"),
            new("Assets/Prefabs/UI/ShopItemRow.prefab", "ShopItemRow/NamePriceGroup/PriceText", "Price: 2", "Giá: 2 ₫"),
            new("Assets/Prefabs/UI/ToSellItemRow.prefab", "ToSellItemRow/NamePriceGroup/ItemNameText", "Carrot Seed", "Hạt giống cà rốt"),
            new("Assets/Prefabs/UI/ToSellItemRow.prefab", "ToSellItemRow/NamePriceGroup/PriceText", "Sell: 2", "Bán: 2 ₫"),
        };
    }
}
