#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BaseballTyping.Editor
{
    public static class BaseballBuildMenu
    {
        private static readonly string[] Scenes = { "Assets/Scenes/Main.unity" };

        [MenuItem("Baseball Typing/Build Windows")]
        public static void BuildWindows()
        {
            Directory.CreateDirectory("Builds/Windows");
            BuildReport report = BuildPipeline.BuildPlayer(
                Scenes,
                "Builds/Windows/BaseballTypingGame.exe",
                BuildTarget.StandaloneWindows64,
                BuildOptions.None);
            Report(report, "Windows");
        }

        [MenuItem("Baseball Typing/Build WebGL")]
        public static void BuildWebGl()
        {
            Directory.CreateDirectory("Builds/WebGL");
            BuildReport report = BuildPipeline.BuildPlayer(
                Scenes,
                "Builds/WebGL",
                BuildTarget.WebGL,
                BuildOptions.None);
            Report(report, "WebGL");
        }

        private static void Report(BuildReport report, string platform)
        {
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log(platform + " build succeeded: " + report.summary.totalSize + " bytes");
                return;
            }

            throw new BuildFailedException(platform + " build failed with " + report.summary.totalErrors + " errors.");
        }
    }
}
#endif
