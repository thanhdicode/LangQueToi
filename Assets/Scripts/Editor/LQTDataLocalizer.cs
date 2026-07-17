using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace LangQueToi.EditorTools
{
    /// <summary>
    /// Migrates display fields (itemName, seedName, cropName, shopName) on ScriptableObjects
    /// from English to Vietnamese values from LQTLocalizationManifest.
    ///
    /// Writes go through SerializedObject only. Never touches asset name, GUID or fileID.
    /// Dry-run reports intended changes without mutating assets; apply is idempotent.
    /// </summary>
    public static class LQTDataLocalizer
    {
        private const string ArtifactDirectory = "Artifacts/Localization";
        private const string DryRunArtifact = ArtifactDirectory + "/data-dry-run.json";
        private const string AppliedArtifact = ArtifactDirectory + "/data-applied.json";

        [MenuItem("LangQueToi/Localization/Data - Dry Run")]
        public static void DryRunMenu() => Run(apply: false, artifact: DryRunArtifact);

        [MenuItem("LangQueToi/Localization/Data - Apply")]
        public static void ApplyMenu() => Run(apply: true, artifact: AppliedArtifact);

        // Command-line entry points for Unity -executeMethod
        public static void DryRunFromCommandLine()
        {
            Report report = Run(apply: false, artifact: DryRunArtifact);
            if (report.errors.Count > 0)
                throw new BuildFailedException($"[LQTDataLocalizer] Dry-run reported {report.errors.Count} error(s).");
        }

        public static void ApplyFromCommandLine()
        {
            Report report = Run(apply: true, artifact: AppliedArtifact);
            if (report.errors.Count > 0)
                throw new BuildFailedException($"[LQTDataLocalizer] Apply reported {report.errors.Count} error(s).");
        }

        private static Report Run(bool apply, string artifact)
        {
            var report = new Report
            {
                mode = apply ? "apply" : "dry-run",
                timestampUtc = DateTime.UtcNow.ToString("o"),
            };

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (KeyValuePair<string, string> pair in LQTLocalizationManifest.ItemNames)
                    SetString(pair.Key, "itemName", pair.Value, apply, report);

                foreach (KeyValuePair<string, string> pair in LQTLocalizationManifest.SeedNames)
                    SetString(pair.Key, "seedName", pair.Value, apply, report);

                foreach (KeyValuePair<string, string> pair in LQTLocalizationManifest.CropNames)
                    SetString(pair.Key, "cropName", pair.Value, apply, report);

                foreach (KeyValuePair<string, string> pair in LQTLocalizationManifest.ShopNames)
                    SetString(pair.Key, "shopName", pair.Value, apply, report);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                if (apply) AssetDatabase.SaveAssets();
            }

            report.counts.items = CountByField(report, "itemName");
            report.counts.fish = CountByFieldAndPrefix(report, "itemName", LQTLocalizationManifest.FishPathPrefix);
            report.counts.seeds = CountByField(report, "seedName");
            report.counts.crops = CountByField(report, "cropName");
            report.counts.shops = CountByField(report, "shopName");
            report.counts.changed = CountChanged(report);
            report.counts.unmapped = 0;
            report.counts.errors = report.errors.Count;

            report.expected.items = LQTLocalizationManifest.ExpectedItemCount;
            report.expected.fish = LQTLocalizationManifest.ExpectedFishCount;
            report.expected.seeds = LQTLocalizationManifest.ExpectedSeedCount;
            report.expected.crops = LQTLocalizationManifest.ExpectedCropCount;
            report.expected.shops = LQTLocalizationManifest.ExpectedShopCount;

            WriteReport(artifact, report);
            Debug.Log(
                $"[LQTDataLocalizer] mode={report.mode} items={report.counts.items}/{report.expected.items} " +
                $"fish={report.counts.fish}/{report.expected.fish} seeds={report.counts.seeds}/{report.expected.seeds} " +
                $"crops={report.counts.crops}/{report.expected.crops} shops={report.counts.shops}/{report.expected.shops} " +
                $"changed={report.counts.changed} errors={report.counts.errors} artifact={artifact}");
            return report;
        }

        private static bool SetString(string assetPath, string propertyName, string value, bool apply, Report report)
        {
            UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (asset == null)
            {
                report.errors.Add($"Missing asset: {assetPath}");
                report.entries.Add(new Entry(assetPath, propertyName, string.Empty, value, false, "missing_asset"));
                return false;
            }

            string objectNameBefore = asset.name;
            string guidBefore = AssetDatabase.AssetPathToGUID(assetPath);
            var serialized = new SerializedObject(asset);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null || property.propertyType != SerializedPropertyType.String)
            {
                report.errors.Add($"Missing string property {propertyName}: {assetPath}");
                report.entries.Add(new Entry(assetPath, propertyName, string.Empty, value, false, "missing_property"));
                return false;
            }

            bool changed = property.stringValue != value;
            report.entries.Add(new Entry(assetPath, propertyName, property.stringValue, value, changed, "ok"));
            if (apply && changed)
            {
                property.stringValue = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssetIfDirty(asset);
            }

            if (asset.name != objectNameBefore || AssetDatabase.AssetPathToGUID(assetPath) != guidBefore)
                report.errors.Add($"Identity changed: {assetPath}");
            return true;
        }

        private static int CountByField(Report report, string field)
        {
            int n = 0;
            foreach (Entry e in report.entries)
                if (e.propertyName == field && e.status == "ok") n++;
            return n;
        }

        private static int CountByFieldAndPrefix(Report report, string field, string prefix)
        {
            int n = 0;
            foreach (Entry e in report.entries)
                if (e.propertyName == field && e.status == "ok" && e.assetPath.StartsWith(prefix, StringComparison.Ordinal)) n++;
            return n;
        }

        private static int CountChanged(Report report)
        {
            int n = 0;
            foreach (Entry e in report.entries)
                if (e.changed) n++;
            return n;
        }

        private static void WriteReport(string artifact, Report report)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(artifact));
            File.WriteAllText(artifact, JsonUtility.ToJson(report, true), new System.Text.UTF8Encoding(false));
        }

        [Serializable]
        public class Entry
        {
            public string assetPath;
            public string propertyName;
            public string before;
            public string after;
            public bool changed;
            public string status;

            public Entry() { }
            public Entry(string assetPath, string propertyName, string before, string after, bool changed, string status)
            {
                this.assetPath = assetPath;
                this.propertyName = propertyName;
                this.before = before;
                this.after = after;
                this.changed = changed;
                this.status = status;
            }
        }

        [Serializable]
        public class Counts
        {
            public int items;
            public int fish;
            public int seeds;
            public int crops;
            public int shops;
            public int changed;
            public int unmapped;
            public int errors;
        }

        [Serializable]
        public class Report
        {
            public string mode;
            public string timestampUtc;
            public Counts counts = new Counts();
            public Counts expected = new Counts();
            public List<Entry> entries = new List<Entry>();
            public List<string> errors = new List<string>();
        }
    }
}
