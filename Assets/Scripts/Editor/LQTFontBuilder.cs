using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace LangQueToi.EditorTools
{
    /// <summary>
    /// Builds two TMP SDF font assets (Nunito Regular + Bold) with full Vietnamese
    /// glyph coverage and mutual fallback. Uses dynamic atlas population to load
    /// glyphs, then locks to static for release.
    ///
    /// Requires Nunito-Regular.ttf and Nunito-Bold.ttf to already exist at the
    /// paths listed in FontPaths.
    /// </summary>
    public static class LQTFontBuilder
    {
        public const string RegularTtfPath  = "Assets/LangQueToi/Fonts/Nunito-Regular.ttf";
        public const string BoldTtfPath     = "Assets/LangQueToi/Fonts/Nunito-Bold.ttf";
        public const string RegularSdfPath  = "Assets/LangQueToi/Fonts/LQT_Nunito_Regular_SDF.asset";
        public const string BoldSdfPath     = "Assets/LangQueToi/Fonts/LQT_Nunito_Bold_SDF.asset";

        /// <summary>Exact corpus every Vietnamese-facing glyph must cover.</summary>
        public const string GlyphCorpus =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789" +
            "ÀÁẢÃẠĂẰẮẲẴẶÂẦẤẨẪẬĐÈÉẺẼẸÊỀẾỂỄỆÌÍỈĨỊÒÓỎÕỌÔỒỐỔỖỘƠỜỚỞỠỢÙÚỦŨỤƯỪỨỬỮỰỲÝỶỸỴ" +
            "àáảãạăằắẳẵặâầấẩẫậđèéẻẽẹêềếểễệìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵ" +
            "₫…“”‘’.,:;!?()[]{}+-×/\\%&";

        [MenuItem("LangQueToi/Visual/Font - Build Nunito SDF")]
        public static void BuildMenu() => Build();

        public static void BuildFromCommandLine()
        {
            // Do NOT throw on batchmode failure — TMP CreateFontAsset requires a graphics
            // context that isn't available in headless batchmode on some Unity 6 builds.
            // The runtime LQTFontBootstrap covers Vietnamese fallback at game start.
            if (!Build())
                Debug.LogWarning("[LQTFontBuilder] Batchmode SDF build not available; runtime bootstrap will provide Vietnamese fallback instead.");
        }

        private static bool Build()
        {
            bool ok = true;
            ok &= BuildOne(RegularTtfPath, RegularSdfPath, "Regular");
            ok &= BuildOne(BoldTtfPath, BoldSdfPath, "Bold");

            if (ok)
            {
                var regular = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RegularSdfPath);
                var bold    = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldSdfPath);
                if (regular != null && bold != null)
                {
                    regular.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset> { bold };
                    bold.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset> { regular };
                    EditorUtility.SetDirty(regular);
                    EditorUtility.SetDirty(bold);
                    AssetDatabase.SaveAssets();
                }
            }
            return ok;
        }

        private static bool BuildOne(string ttfPath, string sdfPath, string label)
        {
            AssetDatabase.Refresh();
            EnsureFontImportSettings(ttfPath);
            // FreeType font engine is loaded lazily by TMP; force init before batch use
            // (in batchmode without prior text rendering it stays uninitialized and
            // CreateFontAsset silently returns null).
            try { UnityEngine.TextCore.LowLevel.FontEngine.InitializeFontEngine(); }
            catch (Exception initEx) { Debug.LogWarning($"[LQTFontBuilder] FontEngine init: {initEx.Message}"); }

            Font ttf = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            if (ttf == null)
            {
                Debug.LogError($"[LQTFontBuilder] Missing TTF: {ttfPath}. Download Nunito from https://fonts.google.com/specimen/Nunito and place under Assets/LangQueToi/Fonts/.");
                return false;
            }

            // Explicit atlas size so TryAddCharacters can allocate texture immediately.
            const int samplingPointSize = 90;
            const int atlasPadding = 9;
            const int atlas = 2048;
            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(
                ttf,
                samplingPointSize,
                atlasPadding,
                UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                atlas, atlas,
                AtlasPopulationMode.Dynamic,
                enableMultiAtlasSupport: true);

            if (asset == null)
            {
                Debug.LogError($"[LQTFontBuilder] CreateFontAsset returned null for {ttfPath}");
                return false;
            }

            // Save the asset FIRST so its texture is persisted, then add characters.
            Directory.CreateDirectory(Path.GetDirectoryName(sdfPath));
            AssetDatabase.DeleteAsset(sdfPath);
            AssetDatabase.CreateAsset(asset, sdfPath);
            // Also persist the atlas texture that Unity attached to the asset in memory.
            if (asset.atlasTexture != null && !AssetDatabase.Contains(asset.atlasTexture))
                AssetDatabase.AddObjectToAsset(asset.atlasTexture, asset);
            if (asset.material != null && !AssetDatabase.Contains(asset.material))
                AssetDatabase.AddObjectToAsset(asset.material, asset);
            AssetDatabase.SaveAssets();

            try
            {
                if (!asset.TryAddCharacters(GlyphCorpus, out string missing))
                {
                    Debug.LogWarning($"[LQTFontBuilder] {label}: some characters missing '{missing}' — keeping asset with partial coverage.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LQTFontBuilder] {label}: TryAddCharacters exception ({ex.Message}) — asset saved with dynamic population; runtime will lazy-load glyphs.");
            }

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Debug.Log($"[LQTFontBuilder] Built {label}: {sdfPath} ({GlyphCorpus.Length} char corpus)");
            return true;
        }

        private static void EnsureFontImportSettings(string ttfPath)
        {
            if (AssetImporter.GetAtPath(ttfPath) is not TrueTypeFontImporter importer)
                return;

            if (importer.includeFontData)
                return;

            importer.includeFontData = true;
            importer.SaveAndReimport();
            Debug.Log($"[LQTFontBuilder] Enabled Include Font Data for {ttfPath}");
        }
    }
}
