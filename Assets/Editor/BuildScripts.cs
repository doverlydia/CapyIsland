using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CapyIsland.EditorTools
{
    /// <summary>
    /// One-click builds: WebGL (into Builds/WebGL, playable from any static host), an Android APK
    /// (Builds/Android/CapyIsland.apk) and an iOS Xcode project (Builds/iOS). All allow every
    /// orientation, so the game can be turned mid-play. Results are written to Builds/last-build.txt.
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

        /// <summary>
        /// A second player for testing friends and live visits: a Development build into
        /// Builds/WebGLTest (served on localhost), with DevCommands so it can be driven from the
        /// browser console. Never uploaded anywhere.
        /// </summary>
        [MenuItem("CapyIsland/Build/WebGL test player")]
        public static void BuildWebGLTest()
        {
            CommonSettings();
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = false;
            PlayerSettings.WebGL.template = "APPLICATION:Default";
            Run(BuildTarget.WebGL, "Builds/WebGLTest", BuildOptions.Development);
        }

        [MenuItem("CapyIsland/Build/Android APK")]
        public static void BuildAndroid()
        {
            CommonSettings();
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.capyisland.game");
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;   // the lowest Unity 6.3 accepts
            EditorUserBuildSettings.buildAppBundle = false;

            Directory.CreateDirectory("Builds/Android");
            Run(BuildTarget.Android, "Builds/Android/CapyIsland.apk");
        }

        /// <summary>
        /// Exports an Xcode project to Builds/iOS. Unity can do this on Windows, but compiling,
        /// signing and installing it on a phone has to happen in Xcode on a Mac (or a cloud Mac).
        /// </summary>
        [MenuItem("CapyIsland/Build/iOS Xcode Project")]
        public static void BuildIOS()
        {
            CommonSettings();
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS, "com.capyisland.game");
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
            // Signing is left to Xcode: tick "Automatically manage signing" and pick your team there.
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;

            // The GitHub workflow (.github/workflows/ios.yml) passes its own output folder.
            Run(BuildTarget.iOS, CommandLineArg("-customBuildPath") ?? "Builds/iOS");
        }

        static string CommandLineArg(string name)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }

        static void Run(BuildTarget target, string path, BuildOptions extra = BuildOptions.None)
        {
            // Settings changed in memory can be lost across the build's domain reload: write them now.
            AssetDatabase.SaveAssets();
            var options = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = path,
                target = target,
                targetGroup = BuildPipeline.GetBuildTargetGroup(target),
                options = extra,
            };
            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            string line = $"{System.DateTime.Now:u} {target}: {summary.result}, {summary.totalSize / (1024 * 1024)} MB, " +
                          $"{summary.totalErrors} errors, {summary.totalTime.TotalSeconds:F0}s -> {path}";
            Directory.CreateDirectory("Builds");
            File.AppendAllText("Builds/last-build.txt", line + "\n");
            Debug.Log("[BuildScripts] " + line);
            if (summary.result != BuildResult.Succeeded)
            {
                foreach (var step in report.steps)
                    foreach (var m in step.messages)
                        if (m.type == LogType.Error || m.type == LogType.Exception)
                            File.AppendAllText("Builds/last-build.txt", "  " + m.content + "\n");
                // Command-line builds (CI) otherwise exit 0 and the failure goes unnoticed.
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }
    }
}
