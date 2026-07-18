using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LangQueToi.EditorTools
{
    /// <summary>
    /// Creates a project-owned player spritesheet and clip overrides without modifying
    /// the imported Sprout Lands source assets or their animation frame layout.
    /// </summary>
    public static class LQTCharacterArtFactory
    {
        private const string SourceSheetPath =
            "Assets/_SproutLandsAssets/Sprout Lands - Sprites - premium pack/Characters/Premium Charakter Spritesheet.png";
        private const string SourceControllerPath = "Assets/Animation/Player/PlayerAnimator.controller";
        private const string PlayerPrefabPath = "Assets/Prefabs/PlayerSystem.prefab";
        private const string OutputDirectory = "Assets/LangQueToi/Art/Characters/Minh";
        private const string OutputSheetPath = OutputDirectory + "/minh-player-spritesheet.png";
        private const string OutputControllerPath = OutputDirectory + "/LQT_Minh_Player.overrideController";

        [MenuItem("LangQueToi/Visual/Build Minh Player Art")]
        public static void BuildMenu() => Build();

        public static void BuildFromCommandLine() => Build();

        private static void Build()
        {
            var sourceImporter = AssetImporter.GetAtPath(SourceSheetPath) as TextureImporter;
            var sourceController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(SourceControllerPath);
            if (sourceImporter == null || sourceController == null)
                throw new InvalidOperationException("[LQTCharacterArtFactory] Player art source or controller is missing.");

            Directory.CreateDirectory(OutputDirectory);
            WriteMinhPaletteSheet(SourceSheetPath, sourceImporter.spritesheet, OutputSheetPath);
            ConfigureSpriteImport(sourceImporter, OutputSheetPath);

            var sourceSprites = LoadSprites(SourceSheetPath);
            var minhSprites = LoadSprites(OutputSheetPath);
            var spriteMap = BuildSpriteMap(sourceSprites, minhSprites);
            if (sourceSprites.Count == 0 || sourceSprites.Count != minhSprites.Count || spriteMap.Count != sourceSprites.Count)
                throw new InvalidOperationException("[LQTCharacterArtFactory] Spritesheet slice count does not match the source.");

            var overrideController = BuildOverrideController(sourceController, spriteMap);
            AssignOverrideToPlayer(overrideController);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[LQTCharacterArtFactory] Built Minh player art: sprites={minhSprites.Count}, controller={OutputControllerPath}");
        }

        public static void RebindClipsFromCommandLine()
        {
            var sourceController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(SourceControllerPath);
            if (sourceController == null)
                throw new InvalidOperationException("[LQTCharacterArtFactory] Source controller is missing.");

            var spriteMap = BuildSpriteMap(LoadSprites(SourceSheetPath), LoadSprites(OutputSheetPath));
            foreach (var sourceClip in sourceController.animationClips)
            {
                var clonePath = OutputDirectory + "/" + SanitizeFileName(sourceClip.name) + ".anim";
                var clone = AssetDatabase.LoadAssetAtPath<AnimationClip>(clonePath);
                if (clone == null)
                    throw new InvalidOperationException($"[LQTCharacterArtFactory] Missing generated clip: {clonePath}");
                ReplaceSpriteFrames(clone, spriteMap);
                EditorUtility.SetDirty(clone);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[LQTCharacterArtFactory] Rebound {spriteMap.Count} sprites across player clips.");
        }

        public static void RepaintSheetFromCommandLine()
        {
            var sourceImporter = AssetImporter.GetAtPath(SourceSheetPath) as TextureImporter;
            if (sourceImporter == null)
                throw new InvalidOperationException("[LQTCharacterArtFactory] Player art source is missing.");
            WriteMinhPaletteSheet(SourceSheetPath, sourceImporter.spritesheet, OutputSheetPath);
            ConfigureSpriteImport(sourceImporter, OutputSheetPath);
            RebindClipsFromCommandLine();
        }

        private static void WriteMinhPaletteSheet(string sourcePath, SpriteMetaData[] spriteMetaData, string destinationPath)
        {
            var bytes = File.ReadAllBytes(sourcePath);
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(source, bytes, false))
                throw new InvalidOperationException("[LQTCharacterArtFactory] Unable to read source player PNG.");

            var pixels = source.GetPixels32();
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = ToMinhPalette(pixels[i]);
            AddConicalHats(pixels, source.width, source.height, spriteMetaData);

            var output = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            output.SetPixels32(pixels);
            output.Apply(false, false);
            File.WriteAllBytes(destinationPath, output.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(source);
            UnityEngine.Object.DestroyImmediate(output);
        }

        private static Color32 ToMinhPalette(Color32 color)
        {
            if (color.a == 0) return color;

            // Preserve outline contrast while translating the old outfit into a
            // Mekong palette: nón lá straw, áo bà ba teal, and worn brown footwear.
            if (color.r < 55 && color.g < 55 && color.b < 70)
                return new Color32(46, 38, 31, color.a);
            if (color.b > color.r + 18 && color.b > color.g + 8)
                return new Color32(31, 91, 88, color.a);
            if (color.r > 210 && color.g > 210 && color.b > 210)
                return new Color32(224, 192, 112, color.a);
            if (color.r > 155 && color.g > 112 && color.g < 205 && color.b < 165)
                return new Color32(203, 145, 93, color.a);
            if (color.r > 90 && color.g > 55 && color.b < 85)
                return new Color32(104, 67, 36, color.a);
            return color;
        }

        private static void AddConicalHats(Color32[] pixels, int width, int height, SpriteMetaData[] spriteMetaData)
        {
            foreach (var metadata in spriteMetaData)
            {
                var rect = metadata.rect;
                var centerX = Mathf.RoundToInt(rect.x + rect.width * 0.5f);
                var topOpaqueY = Mathf.FloorToInt(rect.y);
                for (var y = Mathf.FloorToInt(rect.y); y < Mathf.CeilToInt(rect.y + rect.height); y++)
                {
                    for (var x = Mathf.FloorToInt(rect.x); x < Mathf.CeilToInt(rect.x + rect.width); x++)
                    {
                        if (x >= 0 && x < width && y >= 0 && y < height && pixels[y * width + x].a > 0)
                            topOpaqueY = Mathf.Max(topOpaqueY, y);
                    }
                }
                var topY = topOpaqueY + 7;
                const int hatHalfWidth = 10;
                const int hatHeight = 8;

                for (var row = 0; row < hatHeight; row++)
                {
                    var y = topY - row;
                    var halfWidth = Mathf.Max(1, Mathf.RoundToInt((row + 1f) * hatHalfWidth / hatHeight));
                    for (var x = centerX - halfWidth; x <= centerX + halfWidth; x++)
                    {
                        if (x < 0 || x >= width || y < 0 || y >= height) continue;
                        var isEdge = x == centerX - halfWidth || x == centerX + halfWidth || row == hatHeight - 1;
                        pixels[y * width + x] = isEdge
                            ? new Color32(74, 52, 29, 255)
                            : new Color32(224, 192, 112, 255);
                    }
                }
            }
        }

        private static void ConfigureSpriteImport(TextureImporter sourceImporter, string destinationPath)
        {
            AssetDatabase.ImportAsset(destinationPath, ImportAssetOptions.ForceSynchronousImport);
            if (AssetImporter.GetAtPath(destinationPath) is not TextureImporter importer)
                throw new InvalidOperationException("[LQTCharacterArtFactory] Generated PNG importer is missing.");

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = sourceImporter.spritePixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.spritesheet = sourceImporter.spritesheet;
            importer.SaveAndReimport();
        }

        private static List<Sprite> LoadSprites(string path)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .ToList();
        }

        private static Dictionary<Sprite, Sprite> BuildSpriteMap(IReadOnlyList<Sprite> sourceSprites, IReadOnlyList<Sprite> minhSprites)
        {
            var map = new Dictionary<Sprite, Sprite>();
            foreach (var source in sourceSprites)
            {
                var replacement = minhSprites.FirstOrDefault(candidate => SameRect(source.rect, candidate.rect));
                if (replacement != null)
                    map[source] = replacement;
            }
            return map;
        }

        private static bool SameRect(Rect first, Rect second)
        {
            return Mathf.Approximately(first.x, second.x)
                   && Mathf.Approximately(first.y, second.y)
                   && Mathf.Approximately(first.width, second.width)
                   && Mathf.Approximately(first.height, second.height);
        }

        private static AnimatorOverrideController BuildOverrideController(
            RuntimeAnimatorController sourceController,
            IReadOnlyDictionary<Sprite, Sprite> spriteMap)
        {
            AssetDatabase.DeleteAsset(OutputControllerPath);
            var overrideController = new AnimatorOverrideController(sourceController) { name = "LQT_Minh_Player" };
            var outputClips = new Dictionary<AnimationClip, AnimationClip>();

            foreach (var sourceClip in sourceController.animationClips)
            {
                var clonePath = OutputDirectory + "/" + SanitizeFileName(sourceClip.name) + ".anim";
                AssetDatabase.DeleteAsset(clonePath);
                var clone = UnityEngine.Object.Instantiate(sourceClip);
                clone.name = sourceClip.name;
                ReplaceSpriteFrames(clone, spriteMap);
                AssetDatabase.CreateAsset(clone, clonePath);
                outputClips[sourceClip] = clone;
            }

            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            overrideController.GetOverrides(overrides);
            for (var i = 0; i < overrides.Count; i++)
            {
                if (outputClips.TryGetValue(overrides[i].Key, out var replacement))
                    overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, replacement);
            }

            overrideController.ApplyOverrides(overrides);
            AssetDatabase.CreateAsset(overrideController, OutputControllerPath);
            return overrideController;
        }

        private static void ReplaceSpriteFrames(
            AnimationClip clip,
            IReadOnlyDictionary<Sprite, Sprite> spriteMap)
        {
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            {
                if (binding.type != typeof(SpriteRenderer) || binding.propertyName != "m_Sprite")
                    continue;

                var frames = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                for (var i = 0; i < frames.Length; i++)
                {
                    if (frames[i].value is Sprite oldSprite && spriteMap.TryGetValue(oldSprite, out var newSprite))
                        frames[i].value = newSprite;
                }
                AnimationUtility.SetObjectReferenceCurve(clip, binding, frames);
            }
        }

        private static void AssignOverrideToPlayer(AnimatorOverrideController overrideController)
        {
            var root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                var animator = root.GetComponentInChildren<Animator>(true);
                if (animator == null)
                    throw new InvalidOperationException("[LQTCharacterArtFactory] Player Animator is missing.");
                animator.runtimeAnimatorController = overrideController;
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static string SanitizeFileName(string value)
        {
            foreach (var invalid in Path.GetInvalidFileNameChars())
                value = value.Replace(invalid, '_');
            return value;
        }
    }
}
