using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LangQueToi.EditorTools
{
    /// <summary>
    /// Idempotent scene/prefab rebinder for Làng Quê Tôi visual identity:
    /// - Sprite bindings for menu backgrounds, logo, dialogue frame, coin, weather, shop header
    /// - Font swap to LQT Nunito Regular/Bold for every player-facing TMP_Text
    /// - Wires DialoguePanelUI portraitImage/speakerNameText/defaultPortrait via reflection
    /// - Wires WeatherIconUI icon entries in enum order Sunny/Cloudy/Rainy
    ///
    /// Tolerant of missing hierarchy paths: warns and continues, does not fatal
    /// (real scenes evolve; a hard-fail here would block release for a stray rename).
    /// </summary>
    public static class LQTVisualSceneSetup
    {
        private const string ArtifactPath = "Artifacts/Visual/scene-setup.json";

        // Sprite paths
        private const string MenuBackgroundPath = "Assets/LangQueToi/Art/Menu/menu-background.png";
        private const string TitleMarkPath      = "Assets/LangQueToi/Art/Menu/title-mark.png";
        private const string MenuCharacterPath  = "Assets/LangQueToi/Art/Menu/menu-character.png";
        private const string DialogueFramePath  = "Assets/LangQueToi/Art/Dialogue/dialogue-frame.png";
        private const string PortraitPath       = "Assets/LangQueToi/Art/Characters/ba-nam-portrait.png";
        private const string CoinPath           = "Assets/LangQueToi/Art/HUD/coin-dong.png";
        private const string WeatherSunnyPath   = "Assets/LangQueToi/Art/HUD/weather-sunny.png";
        private const string WeatherCloudyPath  = "Assets/LangQueToi/Art/HUD/weather-cloudy.png";
        private const string WeatherRainyPath   = "Assets/LangQueToi/Art/HUD/weather-rainy.png";
        private const string NotificationPath   = "Assets/LangQueToi/Art/HUD/notification-frame.png";
        private const string ShopHeaderPath     = "Assets/LangQueToi/Art/Shop/ba-nam-shop-header.png";

        private const string RegularFontPath = "Assets/LangQueToi/Fonts/LQT_Nunito_Regular_SDF.asset";
        private const string BoldFontPath    = "Assets/LangQueToi/Fonts/LQT_Nunito_Bold_SDF.asset";

        private const string MenuScene = "Assets/Scenes/MenuScene.unity";
        private const string MainScene = "Assets/Scenes/MainScene.unity";

        [MenuItem("LangQueToi/Visual/Scene - Auto-Rebind && Font Swap")]
        public static void RunMenu() => Run();

        public static void ApplyFromCommandLine()
        {
            Report r = Run();
            if (r.errors.Count > 0)
                throw new BuildFailedException($"[LQTVisualSceneSetup] {r.errors.Count} error(s).");
        }

        private static Report Run()
        {
            var r = new Report { timestampUtc = DateTime.UtcNow.ToString("o") };

            TMP_FontAsset regular = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RegularFontPath);
            TMP_FontAsset bold    = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldFontPath);
            if (regular == null || bold == null)
                r.warnings.Add($"Nunito TMP asset missing (run LQTFontBuilder first). Fonts will not be swapped.");

            // --- MenuScene ---
            // DO NOT rebind Background, LogoTitle, or CharacterPortrait sprites.
            // The original MenuScene has a complete tilemap-based visual design that
            // must not be overridden. Only font swap is safe here.
            Scene menu = EditorSceneManager.OpenScene(MenuScene, OpenSceneMode.Single);
            bool menuDirty = false;
            if (regular != null && bold != null)
                menuDirty |= SwapAllFonts(menu.GetRootGameObjects(), regular, bold, r);
            if (menuDirty)
                EditorSceneManager.SaveScene(menu);
            r.checks.Add(new Check("menu_scene_saved", menuDirty.ToString(), menuDirty ? "changed" : "clean"));

            // --- MainScene ---
            Scene main = EditorSceneManager.OpenScene(MainScene, OpenSceneMode.Single);
            bool mainDirty = false;
            mainDirty |= TrySetImageSprite(main, "Canvas/DialoguePanel/DialogueBox", DialogueFramePath, ImageType.Sliced, r);
            mainDirty |= TrySetImageSprite(main, "Canvas/ShopPanel/BookContainer/GoldContainer/GoldImage", CoinPath, ImageType.Simple, r);
            mainDirty |= TrySetImageSprite(main, "Canvas/NotificationUI/Background", NotificationPath, ImageType.Sliced, r);

            // Weather icons — set the array on the WeatherIconUI component
            mainDirty |= WireWeatherIcons(main, r);

            // Ensure dialogue portrait Image exists and wire DialoguePanelUI fields
            mainDirty |= EnsureDialoguePortrait(main, r);
            mainDirty |= WireDialoguePanelUI(main, r);
            mainDirty |= EnsureShopHeaderBackground(main, r);

            if (regular != null && bold != null)
                mainDirty |= SwapAllFonts(main.GetRootGameObjects(), regular, bold, r);

            if (mainDirty)
                EditorSceneManager.SaveScene(main);
            r.checks.Add(new Check("main_scene_saved", mainDirty.ToString(), mainDirty ? "changed" : "clean"));

            WriteReport(r);
            Debug.Log($"[LQTVisualSceneSetup] warnings={r.warnings.Count} errors={r.errors.Count} artifact={ArtifactPath}");
            return r;
        }

        private enum ImageType { Simple, Sliced }

        private static bool TrySetImageSprite(Scene scene, string path, string spritePath, ImageType type, Report r)
        {
            GameObject go = ResolveInScene(scene, path);
            if (go == null)
            {
                r.warnings.Add($"Skip sprite bind — path not found: {scene.name}/{path}");
                return false;
            }
            Image img = go.GetComponent<Image>();
            if (img == null)
            {
                r.warnings.Add($"Skip sprite bind — no Image component: {scene.name}/{path}");
                return false;
            }
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null)
            {
                r.errors.Add($"Missing sprite asset: {spritePath}");
                return false;
            }

            bool changed = false;
            if (img.sprite != sprite) { img.sprite = sprite; changed = true; }
            Image.Type wanted = type == ImageType.Sliced ? Image.Type.Sliced : Image.Type.Simple;
            if (img.type != wanted) { img.type = wanted; changed = true; }
            if (changed) EditorUtility.SetDirty(img);
            return changed;
        }

        private static bool WireWeatherIcons(Scene scene, Report r)
        {
            WeatherIconUI[] all = UnityEngine.Object.FindObjectsByType<WeatherIconUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (all.Length == 0) { r.warnings.Add("No WeatherIconUI in MainScene."); return false; }

            Sprite sunny  = AssetDatabase.LoadAssetAtPath<Sprite>(WeatherSunnyPath);
            Sprite cloudy = AssetDatabase.LoadAssetAtPath<Sprite>(WeatherCloudyPath);
            Sprite rainy  = AssetDatabase.LoadAssetAtPath<Sprite>(WeatherRainyPath);
            if (sunny == null || cloudy == null || rainy == null)
            {
                r.errors.Add("Missing one or more weather sprites.");
                return false;
            }

            bool changed = false;
            foreach (WeatherIconUI ui in all)
            {
                var so = new SerializedObject(ui);
                SerializedProperty arr = so.FindProperty("iconEntries");
                if (arr == null || !arr.isArray) { r.warnings.Add("WeatherIconUI has no iconEntries array."); continue; }
                arr.arraySize = 3;
                if (SetWeatherEntry(arr.GetArrayElementAtIndex(0), 0, sunny))  changed = true;
                if (SetWeatherEntry(arr.GetArrayElementAtIndex(1), 1, cloudy)) changed = true;
                if (SetWeatherEntry(arr.GetArrayElementAtIndex(2), 2, rainy))  changed = true;
                if (so.ApplyModifiedPropertiesWithoutUndo()) { EditorUtility.SetDirty(ui); changed = true; }
            }
            return changed;
        }

        private static bool SetWeatherEntry(SerializedProperty entry, int typeIndex, Sprite icon)
        {
            SerializedProperty weatherType = entry.FindPropertyRelative("weatherType");
            SerializedProperty iconProp    = entry.FindPropertyRelative("icon");
            bool changed = false;
            if (weatherType != null && weatherType.enumValueIndex != typeIndex) { weatherType.enumValueIndex = typeIndex; changed = true; }
            if (iconProp != null && iconProp.objectReferenceValue != icon) { iconProp.objectReferenceValue = icon; changed = true; }
            return changed;
        }

        private static bool EnsureDialoguePortrait(Scene scene, Report r)
        {
            GameObject box = ResolveInScene(scene, "Canvas/DialoguePanel/DialogueBox");
            if (box == null) { r.warnings.Add("DialogueBox not found; cannot ensure NPCPortrait."); return false; }
            Transform existing = box.transform.Find("NPCPortrait");
            if (existing != null) return false;

            var go = new GameObject("NPCPortrait", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(box.transform, worldPositionStays: false);
            go.transform.SetAsFirstSibling();
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot     = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(40f, 0f);
            rt.sizeDelta = new Vector2(192f, 192f);
            Image img = go.GetComponent<Image>();
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PortraitPath);
            img.preserveAspect = true;
            EditorUtility.SetDirty(go);
            return true;
        }

        private static bool EnsureShopHeaderBackground(Scene scene, Report r)
        {
            GameObject header = ResolveInScene(scene, "Canvas/ShopPanel/BookContainer/BuyPage/ShopHeader");
            if (header == null) { r.warnings.Add("ShopHeader not found; cannot ensure HeaderBg."); return false; }

            Sprite headerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ShopHeaderPath);
            if (headerSprite == null) { r.warnings.Add("Missing shop header sprite: " + ShopHeaderPath); return false; }

            // Parented as a SIBLING of ShopHeader (not a child) so TMP's dynamically
            // generated SubMeshUI children can never push it in front of the text.
            Transform parent = header.transform.parent;
            Transform existing = parent.Find("ShopHeaderBg");
            bool changed = false;

            Vector2 wantedSize = new Vector2(288f, 72f);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
                Image existingImg = go.GetComponent<Image>();
                if (existingImg != null && existingImg.sprite != headerSprite)
                {
                    existingImg.sprite = headerSprite;
                    existingImg.type = Image.Type.Sliced;
                    EditorUtility.SetDirty(existingImg);
                    changed = true;
                }
                var existingRt = (RectTransform)go.transform;
                if (existingRt.sizeDelta != wantedSize)
                {
                    existingRt.sizeDelta = wantedSize;
                    EditorUtility.SetDirty(existingRt);
                    changed = true;
                }
            }
            else
            {
                go = new GameObject("ShopHeaderBg", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(parent, worldPositionStays: false);
                var headerRt = header.GetComponent<RectTransform>();
                var rt = (RectTransform)go.transform;
                rt.anchorMin = headerRt.anchorMin;
                rt.anchorMax = headerRt.anchorMax;
                rt.pivot = headerRt.pivot;
                rt.anchoredPosition = headerRt.anchoredPosition;
                rt.sizeDelta = new Vector2(288f, 72f);
                Image img = go.GetComponent<Image>();
                img.sprite = headerSprite;
                img.type = Image.Type.Sliced;
                EditorUtility.SetDirty(go);
                changed = true;
            }

            // Always sit immediately before ShopHeader in sibling order so it
            // renders behind the text regardless of TMP's dynamic children.
            int wantedIndex = Mathf.Max(0, header.transform.GetSiblingIndex());
            if (go.transform.GetSiblingIndex() != wantedIndex)
            {
                go.transform.SetSiblingIndex(wantedIndex);
                changed = true;
            }

            return changed;
        }

        private static bool WireDialoguePanelUI(Scene scene, Report r)
        {
            var panel = UnityEngine.Object.FindFirstObjectByType<DialoguePanelUI>(FindObjectsInactive.Include);
            if (panel == null) { r.warnings.Add("DialoguePanelUI not in MainScene."); return false; }

            GameObject portraitGo = ResolveInScene(scene, "Canvas/DialoguePanel/DialogueBox/NPCPortrait");
            GameObject nameGo     = ResolveInScene(scene, "Canvas/DialoguePanel/DialogueBox/NPCNameText");
            Sprite portrait       = AssetDatabase.LoadAssetAtPath<Sprite>(PortraitPath);

            var so = new SerializedObject(panel);
            bool changed = false;

            if (portraitGo != null)
            {
                Image img = portraitGo.GetComponent<Image>();
                if (img != null && SetObjectRef(so, "portraitImage", img)) changed = true;
            }
            if (nameGo != null)
            {
                TMP_Text tmp = nameGo.GetComponent<TMP_Text>();
                if (tmp != null && SetObjectRef(so, "speakerNameText", tmp)) changed = true;
            }
            if (portrait != null && SetObjectRef(so, "defaultPortrait", portrait)) changed = true;

            if (so.ApplyModifiedPropertiesWithoutUndo()) { EditorUtility.SetDirty(panel); changed = true; }
            return changed;
        }

        private static bool SetObjectRef(SerializedObject so, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty p = so.FindProperty(propertyName);
            if (p == null) return false;
            if (p.objectReferenceValue == value) return false;
            p.objectReferenceValue = value;
            return true;
        }

        private static bool SwapAllFonts(GameObject[] roots, TMP_FontAsset regular, TMP_FontAsset bold, Report r)
        {
            bool changed = false;
            foreach (GameObject root in roots)
            {
                foreach (TMP_Text t in root.GetComponentsInChildren<TMP_Text>(includeInactive: true))
                {
                    bool isHeadingOrButton = t.fontStyle == FontStyles.Bold
                        || t.GetComponentInParent<Button>() != null
                        || t.name.EndsWith("Header", StringComparison.OrdinalIgnoreCase)
                        || t.name.Equals("NPCNameText", StringComparison.OrdinalIgnoreCase);
                    TMP_FontAsset wanted = isHeadingOrButton ? bold : regular;
                    if (t.font != wanted)
                    {
                        var so = new SerializedObject(t);
                        SerializedProperty fontProp = so.FindProperty("m_fontAsset");
                        if (fontProp != null)
                        {
                            fontProp.objectReferenceValue = wanted;
                            so.ApplyModifiedPropertiesWithoutUndo();
                            EditorUtility.SetDirty(t);
                            changed = true;
                        }
                    }
                }
            }
            return changed;
        }

        private static GameObject ResolveInScene(Scene scene, string path)
        {
            string[] segments = path.Split('/');
            foreach (GameObject root in scene.GetRootGameObjects())
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
            public Check(string n, string v, string s) { name = n; value = v; status = s; }
        }

        [Serializable]
        public class Report
        {
            public string timestampUtc;
            public List<Check> checks = new List<Check>();
            public List<string> warnings = new List<string>();
            public List<string> errors = new List<string>();
        }
    }
}
