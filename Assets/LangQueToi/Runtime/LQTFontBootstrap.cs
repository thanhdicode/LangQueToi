using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace LangQueToi
{
    /// <summary>
    /// Runtime fallback: at game startup, load Nunito TTFs from Resources and
    /// register a TMP_FontAsset as global fallback so every existing pixel-font
    /// TMP_Text renders Vietnamese glyphs through Nunito.
    ///
    /// This bypasses Unity batchmode's inability to call TMP_FontAsset.CreateFontAsset
    /// without a graphics context — at runtime the font engine is always available.
    /// </summary>
    public static class LQTFontBootstrap
    {
        private const string RegularResource = "LQT/Nunito-Regular";
        private const string BoldResource    = "LQT/Nunito-Bold";

        private static bool _initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            Font regularTtf = Resources.Load<Font>(RegularResource);
            Font boldTtf    = Resources.Load<Font>(BoldResource);
            if (regularTtf == null || boldTtf == null)
            {
                Debug.LogWarning("[LQTFontBootstrap] Nunito TTF Resources missing; Vietnamese fallback disabled.");
                return;
            }

            TMP_FontAsset regular = BuildTMPFromFont(regularTtf, "LQT_Nunito_Regular_Runtime");
            TMP_FontAsset bold    = BuildTMPFromFont(boldTtf,    "LQT_Nunito_Bold_Runtime");
            if (regular == null || bold == null)
            {
                Debug.LogWarning("[LQTFontBootstrap] CreateFontAsset failed at runtime; Vietnamese fallback disabled.");
                return;
            }

            var fallbacks = TMP_Settings.fallbackFontAssets ?? new List<TMP_FontAsset>();
            if (!fallbacks.Contains(regular)) fallbacks.Insert(0, regular);
            if (!fallbacks.Contains(bold))    fallbacks.Insert(0, bold);

            Debug.Log($"[LQTFontBootstrap] Registered Nunito Regular + Bold as global TMP fallbacks.");
        }

        private static TMP_FontAsset BuildTMPFromFont(Font ttf, string name)
        {
            const int samplingPointSize = 90;
            const int atlasPadding = 9;
            const int atlas = 2048;
            TMP_FontAsset a = TMP_FontAsset.CreateFontAsset(
                ttf,
                samplingPointSize,
                atlasPadding,
                UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                atlas, atlas,
                AtlasPopulationMode.Dynamic,
                enableMultiAtlasSupport: true);
            if (a != null) a.name = name;
            return a;
        }
    }
}
