using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GeoPuzzle.Editor
{
    public static class BuildMenu
    {
        const string OutputFolder = "Builds/Android";

        [MenuItem("GeoPuzzle/Build/Android APK (test phone)")]
        public static void BuildTestApk() => Build(appBundle: false, BuildOptions.Development);

        [MenuItem("GeoPuzzle/Build/Android App Bundle (Play)")]
        public static void BuildAppBundle() => Build(appBundle: true, BuildOptions.None);

        static void Build(bool appBundle, BuildOptions options)
        {
            var previous = EditorUserBuildSettings.buildAppBundle;
            EditorUserBuildSettings.buildAppBundle = appBundle;
            Directory.CreateDirectory(OutputFolder);

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = $"{OutputFolder}/GeoPuzzle.{(appBundle ? "aab" : "apk")}",
                target = BuildTarget.Android,
                options = options,
            });

            EditorUserBuildSettings.buildAppBundle = previous;
            if (report.summary.result == BuildResult.Succeeded)
                EditorUtility.RevealInFinder(report.summary.outputPath);
            else
                Debug.LogError($"Android build failed: {report.summary.totalErrors} errors");
        }
    }
}
