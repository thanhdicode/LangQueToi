using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace LangQueToi.EditorTools
{
    /// <summary>
    /// Deterministic sprite import settings for Làng Quê Tôi visual assets.
    /// Idempotent: re-running does not touch importers already in the target state.
    /// Also validates exact PNG dimensions and reports mismatches without mutating source.
    /// </summary>
    public static class LQTArtImporter
    {
        private const string ArtifactPath = "Artifacts/Visual/art-validation.json";

        // (path, width, height, sliced, maxSize)
        private static readonly ArtSpec[] Specs =
        {
            new("Assets/LangQueToi/Art/Menu/menu-background.png", 1920, 1080, false, 2048),
            new("Assets/LangQueToi/Art/Menu/title-mark.png",       500,  500, false,  512),
            new("Assets/LangQueToi/Art/Menu/menu-character.png",   400,  400, false,  512),
            new("Assets/LangQueToi/Art/Dialogue/dialogue-frame.png", 960, 256, true, 1024),
            new("Assets/LangQueToi/Art/Characters/ba-nam-portrait.png", 256, 256, false, 512),
            new("Assets/LangQueToi/Art/HUD/coin-dong.png",          64,  64, false,  512),
            new("Assets/LangQueToi/Art/HUD/weather-sunny.png",      90,  90, false,  512),
            new("Assets/LangQueToi/Art/HUD/weather-cloudy.png",     90,  90, false,  512),
            new("Assets/LangQueToi/Art/HUD/weather-rainy.png",      90,  90, false,  512),
            new("Assets/LangQueToi/Art/HUD/notification-frame.png", 640, 128, true, 1024),
            new("Assets/LangQueToi/Art/Shop/ba-nam-shop-header.png", 512, 128, true, 1024),
        };

        [MenuItem("LangQueToi/Visual/Art - Configure && Validate")]
        public static void RunMenu() => Run(apply: true);

        [MenuItem("LangQueToi/Visual/Art - Validate Only")]
        public static void ValidateMenu() => Run(apply: false);

        public static void ConfigureAndValidateFromCommandLine()
        {
            Report r = Run(apply: true);
            if (r.errors.Count > 0)
                throw new BuildFailedException($"[LQTArtImporter] {r.errors.Count} error(s).");
        }

        private static Report Run(bool apply)
        {
            var r = new Report { mode = apply ? "apply" : "validate", timestampUtc = DateTime.UtcNow.ToString("o") };

            foreach (ArtSpec spec in Specs)
            {
                var entry = new EntryReport { path = spec.path, expectedWidth = spec.width, expectedHeight = spec.height, sliced = spec.sliced };

                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(spec.path);
                if (tex == null)
                {
                    entry.status = "missing";
                    r.errors.Add($"Missing texture: {spec.path}");
                    r.entries.Add(entry);
                    continue;
                }

                entry.actualWidth = tex.width;
                entry.actualHeight = tex.height;

                if (tex.width != spec.width || tex.height != spec.height)
                {
                    entry.status = "dimension_mismatch";
                    r.errors.Add($"{spec.path} expected {spec.width}x{spec.height}, got {tex.width}x{tex.height}");
                    r.entries.Add(entry);
                    continue;
                }

                var importer = AssetImporter.GetAtPath(spec.path) as TextureImporter;
                if (importer == null)
                {
                    entry.status = "not_texture_importer";
                    r.errors.Add($"Not a TextureImporter: {spec.path}");
                    r.entries.Add(entry);
                    continue;
                }

                if (apply) Configure(importer, spec);
                entry.status = "ok";
                r.entries.Add(entry);
            }

            WriteReport(r);
            Debug.Log($"[LQTArtImporter] mode={r.mode} entries={r.entries.Count} errors={r.errors.Count} artifact={ArtifactPath}");
            return r;
        }

        private static void Configure(TextureImporter importer, ArtSpec spec)
        {
            bool dirty = false;

            if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; dirty = true; }
            if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; dirty = true; }
            if (!Mathf.Approximately(importer.spritePixelsPerUnit, 16f)) { importer.spritePixelsPerUnit = 16f; dirty = true; }
            if (importer.filterMode != FilterMode.Point) { importer.filterMode = FilterMode.Point; dirty = true; }
            if (importer.textureCompression != TextureImporterCompression.Uncompressed) { importer.textureCompression = TextureImporterCompression.Uncompressed; dirty = true; }
            if (importer.mipmapEnabled) { importer.mipmapEnabled = false; dirty = true; }
            if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; dirty = true; }
            if (importer.maxTextureSize != spec.maxSize) { importer.maxTextureSize = spec.maxSize; dirty = true; }
            if (importer.wrapMode != TextureWrapMode.Clamp) { importer.wrapMode = TextureWrapMode.Clamp; dirty = true; }

            Vector4 targetBorder = spec.sliced ? new Vector4(24f, 24f, 24f, 24f) : Vector4.zero;
            if (importer.spriteBorder != targetBorder) { importer.spriteBorder = targetBorder; dirty = true; }

            if (dirty) importer.SaveAndReimport();
        }

        private static void WriteReport(Report r)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ArtifactPath));
            File.WriteAllText(ArtifactPath, JsonUtility.ToJson(r, true), new System.Text.UTF8Encoding(false));
        }

        private readonly struct ArtSpec
        {
            public readonly string path;
            public readonly int width;
            public readonly int height;
            public readonly bool sliced;
            public readonly int maxSize;

            public ArtSpec(string path, int width, int height, bool sliced, int maxSize)
            {
                this.path = path; this.width = width; this.height = height; this.sliced = sliced; this.maxSize = maxSize;
            }
        }

        [Serializable]
        public class EntryReport
        {
            public string path;
            public int expectedWidth;
            public int expectedHeight;
            public int actualWidth;
            public int actualHeight;
            public bool sliced;
            public string status;
        }

        [Serializable]
        public class Report
        {
            public string mode;
            public string timestampUtc;
            public List<EntryReport> entries = new List<EntryReport>();
            public List<string> errors = new List<string>();
        }
    }
}
