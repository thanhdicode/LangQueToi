using System;
using System.Collections.Generic;
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
    /// One-shot visual rebind for Làng Quê Tôi Phase 2 assets:
    /// menu background, logo, character, dialogue frame/portrait, shop header,
    /// weather icons, coin icon, notification frame, and Vietnamese TMP fonts.
    /// </summary>
    public static class LQTVisualSetup
    {
        private const string ArtifactPath = "Artifacts/Visual/visual-setup.json";

        [MenuItem("LangQueToi/Visual/Setup Visual Bindings")]
        public static void Menu() => RunCommandLine();

        public static void RunCommandLine()
        {
            var report = new SetupReport { timestampUtc = DateTime.UtcNow.ToString("o") };

            var menuBg = LoadSprite("Assets/LangQueToi/Art/Menu/menu-background.png");
            var titleMark = LoadSprite("Assets/LangQueToi/Art/Menu/title-mark.png");
            var menuChar = LoadSprite("Assets/LangQueToi/Art/Menu/menu-character.png");
            var dialogueFrame = LoadSprite("Assets/LangQueToi/Art/Dialogue/dialogue-frame.png");
            var portrait = LoadSprite("Assets/LangQueToi/Art/Characters/ba-nam-portrait.png");
            var coin = LoadSprite("Assets/LangQueToi/Art/HUD/coin-dong.png");
            var notifFrame = LoadSprite("Assets/LangQueToi/Art/HUD/notification-frame.png");
            var shopHeader = LoadSprite("Assets/LangQueToi/Art/Shop/ba-nam-shop-header.png");
            var weatherSunny = LoadSprite("Assets/LangQueToi/Art/HUD/weather-sunny.png");
            var weatherCloudy = LoadSprite("Assets/LangQueToi/Art/HUD/weather-cloudy.png");
            var weatherRainy = LoadSprite("Assets/LangQueToi/Art/HUD/weather-rainy.png");

            var regularFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/LangQueToi/Fonts/LQT_Nunito_Regular_SDF.asset");
            var boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/LangQueToi/Fonts/LQT_Nunito_Bold_SDF.asset");

            // --- MenuScene ---
            var menuScene = EditorSceneManager.OpenScene("Assets/Scenes/MenuScene.unity", OpenSceneMode.Single);
            var menuRoots = new List<GameObject>();
            menuScene.GetRootGameObjects(menuRoots);

            BindImage(FindByPath(menuRoots, "Canvas/Background"), menuBg, report);
            BindImage(FindByPath(menuRoots, "Canvas/LogoTitle"), titleMark, report);
            BindImage(FindByPath(menuRoots, "Canvas/CharacterPortrait"), menuChar, report);

            foreach (var tmp in Resources.FindObjectsOfTypeAll<TMP_Text>())
            {
                if (tmp.gameObject.scene != menuScene) continue;
                SetFont(tmp, regularFont, boldFont, report);
            }

            EditorSceneManager.SaveScene(menuScene);

            // --- MainScene ---
            var mainScene = EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity", OpenSceneMode.Single);
            var mainRoots = new List<GameObject>();
            mainScene.GetRootGameObjects(mainRoots);

            foreach (var dialogue in Resources.FindObjectsOfTypeAll<DialoguePanelUI>())
            {
                if (dialogue.gameObject.scene != mainScene) continue;
                var so = new SerializedObject(dialogue);
                var speakerText = FindChildText(dialogue.transform, new[] { "NPCNameText", "SpeakerNameText", "NameText" });
                var portraitImage = FindChildImage(dialogue.transform, new[] { "NPCPortrait", "Portrait", "PortraitImage" });
                if (speakerText != null) so.FindProperty("speakerNameText").objectReferenceValue = speakerText;
                if (portraitImage != null) so.FindProperty("portraitImage").objectReferenceValue = portraitImage;
                so.FindProperty("defaultPortrait").objectReferenceValue = portrait;
                so.ApplyModifiedPropertiesWithoutUndo();
                report.bindings.Add($"DialoguePanelUI on {dialogue.name}: speaker={speakerText?.name}, portrait={portraitImage?.name}, default={portrait?.name}");
            }

            foreach (var shop in Resources.FindObjectsOfTypeAll<NPCShop>())
            {
                if (shop.gameObject.scene != mainScene) continue;
                var so = new SerializedObject(shop);
                so.FindProperty("portrait").objectReferenceValue = portrait;
                so.ApplyModifiedPropertiesWithoutUndo();
                report.bindings.Add($"NPCShop on {shop.name}: portrait={portrait?.name}");
            }

            foreach (var weather in Resources.FindObjectsOfTypeAll<WeatherIconUI>())
            {
                if (weather.gameObject.scene != mainScene) continue;
                var so = new SerializedObject(weather);
                var entriesProp = so.FindProperty("iconEntries");
                entriesProp.arraySize = 3;
                entriesProp.GetArrayElementAtIndex(0).FindPropertyRelative("weatherType").enumValueIndex = 0; // Sunny
                entriesProp.GetArrayElementAtIndex(0).FindPropertyRelative("icon").objectReferenceValue = weatherSunny;
                entriesProp.GetArrayElementAtIndex(1).FindPropertyRelative("weatherType").enumValueIndex = 1; // Cloudy
                entriesProp.GetArrayElementAtIndex(1).FindPropertyRelative("icon").objectReferenceValue = weatherCloudy;
                entriesProp.GetArrayElementAtIndex(2).FindPropertyRelative("weatherType").enumValueIndex = 2; // Rainy
                entriesProp.GetArrayElementAtIndex(2).FindPropertyRelative("icon").objectReferenceValue = weatherRainy;
                so.ApplyModifiedPropertiesWithoutUndo();
                report.bindings.Add($"WeatherIconUI on {weather.name}: Sunny, Cloudy, Rainy");
            }

            // Bind standalone UI images by name heuristic
            BindImageByName(mainRoots, "Coin", coin, report);
            BindImageByName(mainRoots, "Gold", coin, report);
            BindImageByName(mainRoots, "Currency", coin, report);
            BindImageByName(mainRoots, "Notification", notifFrame, report);
            BindImageByName(mainRoots, "DialogueBox", dialogueFrame, report);
            BindImageByName(mainRoots, "ShopHeader", shopHeader, report);
            BindImageByName(mainRoots, "Header", shopHeader, report);

            foreach (var tmp in Resources.FindObjectsOfTypeAll<TMP_Text>())
            {
                if (tmp.gameObject.scene != mainScene) continue;
                SetFont(tmp, regularFont, boldFont, report);
            }

            EditorSceneManager.SaveScene(mainScene);

            WriteReport(report);
            Debug.Log($"[LQTVisualSetup] done bindings={report.bindings.Count} errors={report.errors.Count} artifact={ArtifactPath}");
            if (report.errors.Count > 0)
                throw new BuildFailedException($"[LQTVisualSetup] {report.errors.Count} error(s).");
        }

        private static Sprite LoadSprite(string path)
        {
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp == null)
                sp = AssetDatabase.LoadAssetAtPath<Sprite>(path + "/" + System.IO.Path.GetFileNameWithoutExtension(path));
            return sp;
        }

        private static GameObject FindByPath(List<GameObject> roots, string path)
        {
            string[] parts = path.Split('/');
            foreach (var root in roots)
            {
                if (root.name != parts[0]) continue;
                Transform t = root.transform;
                for (int i = 1; i < parts.Length; i++)
                {
                    t = t.Find(parts[i]);
                    if (t == null) break;
                }
                if (t != null) return t.gameObject;
            }
            return null;
        }

        private static TMP_Text FindChildText(Transform root, string[] names)
        {
            foreach (var n in names)
            {
                var t = root.GetComponentsInChildren<TMP_Text>(true);
                foreach (var x in t) if (x.name == n) return x;
            }
            return null;
        }

        private static Image FindChildImage(Transform root, string[] names)
        {
            foreach (var n in names)
            {
                var imgs = root.GetComponentsInChildren<Image>(true);
                foreach (var img in imgs) if (img.name == n) return img;
            }
            return null;
        }

        private static void BindImage(GameObject go, Sprite sprite, SetupReport report)
        {
            if (go == null) { report.errors.Add($"Missing GameObject for binding"); return; }
            var img = go.GetComponent<Image>();
            if (img == null) { report.errors.Add($"No Image on {go.name}"); return; }
            if (sprite == null) { report.errors.Add($"Sprite null for {go.name}"); return; }
            Undo.RecordObject(img, "LQTVisualSetup");
            img.sprite = sprite;
            EditorUtility.SetDirty(img);
            report.bindings.Add($"Image {go.name} -> {sprite.name}");
        }

        private static void BindImageByName(List<GameObject> roots, string keyword, Sprite sprite, SetupReport report)
        {
            if (sprite == null) { report.errors.Add($"Sprite null for {keyword}"); return; }
            foreach (var root in roots)
            {
                var imgs = root.GetComponentsInChildren<Image>(true);
                foreach (var img in imgs)
                {
                    if (img.name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Undo.RecordObject(img, "LQTVisualSetup");
                        img.sprite = sprite;
                        EditorUtility.SetDirty(img);
                        report.bindings.Add($"Image {img.name} -> {sprite.name}");
                    }
                }
            }
        }

        private static void SetFont(TMP_Text text, TMP_FontAsset regular, TMP_FontAsset bold, SetupReport report)
        {
            if (regular == null) return;
            Undo.RecordObject(text, "LQTVisualSetup Font");
            // Keep bold if the object name or current style suggests it
            bool useBold = text.name.IndexOf("Header", StringComparison.OrdinalIgnoreCase) >= 0
                        || text.name.IndexOf("Title", StringComparison.OrdinalIgnoreCase) >= 0
                        || (text.fontStyle & FontStyles.Bold) == FontStyles.Bold;
            text.font = useBold && bold != null ? bold : regular;
            EditorUtility.SetDirty(text);
            report.fontsSet++;
        }

        private static void WriteReport(SetupReport r)
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ArtifactPath));
            System.IO.File.WriteAllText(ArtifactPath, JsonUtility.ToJson(r, true));
        }

        [Serializable]
        public class SetupReport
        {
            public string timestampUtc;
            public List<string> bindings = new List<string>();
            public List<string> errors = new List<string>();
            public int fontsSet;
        }
    }
}
