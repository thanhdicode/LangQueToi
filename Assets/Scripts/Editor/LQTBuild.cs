using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LangQueToi.EditorTools
{
    /// <summary>
    /// Windows x86_64 standalone build for Làng Quê Tôi.
    /// Prefers IL2CPP + LZ4HC per spec Section 10; falls back to Mono if IL2CPP module missing.
    /// Development Build always OFF for release. Writes build report to Artifacts/Build/.
    /// </summary>
    public static class LQTBuild
    {
        private const string OutputDir = "Builds/LangQueToi";
        private const string ExeName = "LangQueToi.exe";
        private const string ReportPath = "Artifacts/Build/build-report.txt";
        private const string ProductName = "Làng Quê Tôi";
        private const string CompanyName = "Studio Việt";
        private const string BundleId = "com.studioviet.langquetoi";

        [MenuItem("LangQueToi/Build/Windows Standalone (x86_64)")]
        public static void BuildMenu() => Build();

        public static void BuildFromCommandLine()
        {
            if (!Build())
                throw new UnityEditor.Build.BuildFailedException("[LQTBuild] Build failed.");
        }

        private static bool Build()
        {
            // Product identity per spec
            PlayerSettings.productName = ProductName;
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, BundleId);

            // Prefer IL2CPP; fall back to Mono if IL2CPP module unavailable
            try
            {
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.IL2CPP);
                PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.Standalone, Il2CppCompilerConfiguration.Release);
                Debug.Log("[LQTBuild] Scripting backend: IL2CPP (Release).");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LQTBuild] IL2CPP unavailable ({ex.Message}); falling back to Mono2x.");
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            }

            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Standalone, ApiCompatibilityLevel.NET_Standard);

            Directory.CreateDirectory(OutputDir);
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));

            string[] scenes =
            {
                "Assets/Scenes/MenuScene.unity",
                "Assets/Scenes/MainScene.unity",
            };

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(OutputDir, ExeName),
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.CompressWithLz4HC,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary s = report.summary;
            string status = s.result.ToString();
            string errorSummary = string.Join("\n",
                report.steps.SelectMany(st => st.messages)
                    .Where(m => m.type == LogType.Error || m.type == LogType.Exception)
                    .Take(20)
                    .Select(m => $"[{m.type}] {m.content}"));

            string text =
                $"result: {status}\n" +
                $"output: {s.outputPath}\n" +
                $"totalSize: {s.totalSize}\n" +
                $"totalTime: {s.totalTime}\n" +
                $"totalErrors: {s.totalErrors}\n" +
                $"totalWarnings: {s.totalWarnings}\n" +
                $"scenes: {string.Join(", ", scenes)}\n" +
                $"scriptingBackend: {PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone)}\n" +
                $"errors:\n{errorSummary}\n";
            File.WriteAllText(ReportPath, text);
            Debug.Log($"[LQTBuild] {status} — size={s.totalSize} time={s.totalTime} out={s.outputPath}");
            return s.result == BuildResult.Succeeded;
        }
    }
}
