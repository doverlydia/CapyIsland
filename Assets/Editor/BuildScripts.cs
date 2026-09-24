using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CapyIsland.EditorTools
{
    /// <summary>
    /// One-click builds for the two targets: WebGL (into Builds/WebGL, playable from any static
    /// host) and an Android APK (Builds/Android/CapyIsland.apk). Both allow every orientation, so the
    /// game can be turned mid-play. Results are written to Builds/last-build.txt.
    /// </summary>
    public static class BuildScripts
    {
        static readonly string[] Scenes = { "Assets/Scenes/SampleScene.unity" };

        static void CommonSettings()
        {
            PlayerSettings.companyName = "CapyIsland";
            PlayerSettings.productName = "Capy Island";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = true;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.runInBackground = false;
        }

        [MenuItem("CapyIsland/Build/WebGL")]
        public static void BuildWebGL()
        {
            CommonSettings();
            // Gzip with a decompression fallback plays from any host, even one that does not send
            // Content-Encoding headers (itch.io, GitHub Pages, a plain file server).
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.template = "APPLICATION:Default";

            Run(BuildTarget.WebGL, "Builds/WebGL");
        }

        [MenuItem("CapyIsland/Build/Android APK")]
        public static void BuildAndroid()
        {
            CommonSettings();
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.capyisland.game");
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            EditorUserBuildSettings.buildAppBundle = false;

            Directory.CreateDirectory("Builds/Android");
            Run(BuildTarget.Android, "Builds/Android/CapyIsland.apk");
        }

        static void Run(BuildTarget target, string path)
        {
            // Settings changed in memory can be lost across the build's domain reload: write them now.
            AssetDatabase.SaveAssets();
            var options = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = path,
                target = target,
                targetGroup = BuildPipeline.GetBuildTargetGroup(target),
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            string line = $"{System.DateTime.Now:u} {target}: {summary.result}, {summary.totalSize / (1024 * 1024)} MB, " +
                          $"{summary.totalErrors} errors, {summary.totalTime.TotalSeconds:F0}s -> {path}";
            Directory.CreateDirectory("Builds");
            File.AppendAllText("Builds/last-build.txt", line + "\n");
            Debug.Log("[BuildScripts] " + line);
            if (summary.result != BuildResult.Succeeded)
                foreach (var step in report.steps)
                    foreach (var m in step.messages)
                        if (m.type == LogType.Error || m.type == LogType.Exception)
                            File.AppendAllText("Builds/last-build.txt", "  " + m.content + "\n");
        }
    }
}
