using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BalloonToHeaven.EditorTools
{
    /// <summary>
    /// Command line / CI build entry points (used by the GameCI workflow through -executeMethod):
    ///   BalloonToHeaven.EditorTools.BuildScript.BuildWebGL
    ///   BalloonToHeaven.EditorTools.BuildScript.BuildAndroid
    /// Optional argument: -customBuildPath &lt;path&gt;. Exits with code 1 when the build fails.
    /// </summary>
    public static class BuildScript
    {
        [MenuItem("Balloon to Heaven/Build/WebGL")]
        public static void BuildWebGL() => Build(BuildTarget.WebGL, "Builds/WebGL");

        [MenuItem("Balloon to Heaven/Build/Android APK")]
        public static void BuildAndroid() => Build(BuildTarget.Android, "Builds/Android/BalloonToHeaven.apk");

        /// <summary>Generates content, scenes and settings without building (useful in CI before tests).</summary>
        public static void PrepareProject()
        {
            ContentGenerator.GenerateIfMissing();
            SceneGenerator.GenerateIfMissing();
            ProjectConfigurator.Apply();
        }

        static void Build(BuildTarget target, string defaultPath)
        {
            try
            {
                PrepareProject();

                string path = GetArg("-customBuildPath") ?? defaultPath;
                if (target == BuildTarget.Android && !path.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
                    path = Path.Combine(path, "BalloonToHeaven.apk");
                string dir = target == BuildTarget.WebGL ? path : Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                if (target == BuildTarget.Android)
                {
                    EditorUserBuildSettings.buildAppBundle = false;
                    // Optional release signing through environment variables (debug key otherwise).
                    string keystore = Environment.GetEnvironmentVariable("ANDROID_KEYSTORE_PATH");
                    if (!string.IsNullOrEmpty(keystore) && File.Exists(keystore))
                    {
                        PlayerSettings.Android.useCustomKeystore = true;
                        PlayerSettings.Android.keystoreName = keystore;
                        PlayerSettings.Android.keystorePass = Environment.GetEnvironmentVariable("ANDROID_KEYSTORE_PASS");
                        PlayerSettings.Android.keyaliasName = Environment.GetEnvironmentVariable("ANDROID_KEYALIAS_NAME");
                        PlayerSettings.Android.keyaliasPass = Environment.GetEnvironmentVariable("ANDROID_KEYALIAS_PASS");
                    }
                    else PlayerSettings.Android.useCustomKeystore = false;

                    string versionCode = Environment.GetEnvironmentVariable("ANDROID_VERSION_CODE");
                    if (int.TryParse(versionCode, out int code) && code > 0) PlayerSettings.Android.bundleVersionCode = code;
                }

                var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
                var options = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = path,
                    target = target,
                    targetGroup = BuildPipeline.GetBuildTargetGroup(target),
                    options = BuildOptions.None,
                };

                Debug.Log("[Balloon to Heaven] Building " + target + " to " + path + " with " + scenes.Length + " scenes");
                var report = BuildPipeline.BuildPlayer(options);
                var summary = report.summary;
                Debug.Log("[Balloon to Heaven] Build result: " + summary.result + ", size " + (summary.totalSize / (1024f * 1024f)).ToString("0.0") + " MB, errors " + summary.totalErrors);
                if (summary.result != BuildResult.Succeeded)
                {
                    if (Application.isBatchMode) EditorApplication.Exit(1);
                    else Debug.LogError("[Balloon to Heaven] Build failed. See the console for details.");
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        static string GetArg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }
    }
}
