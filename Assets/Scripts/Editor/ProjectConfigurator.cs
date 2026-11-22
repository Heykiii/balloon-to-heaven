using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace BalloonToHeaven.EditorTools
{
    /// <summary>
    /// Applies the player settings the game needs: portrait 9:16, identifiers, WebGL canvas and
    /// compression that works on GitHub Pages, Android IL2CPP/ARM64, legacy input, and shaders
    /// that are only found by name at runtime.
    /// </summary>
    public static class ProjectConfigurator
    {
        public const string CompanyName = "Heykiii";
        public const string ProductName = "Balloon to Heaven";
        public const string BundleId = "com.heykiii.balloontoheaven";
        public const string Version = "1.0.0";

        static readonly string[] RuntimeShaders =
        {
            "BalloonToHeaven/ToonLit", "BalloonToHeaven/Unlit", "BalloonToHeaven/Particle", "BalloonToHeaven/Sky", "BalloonToHeaven/Shield",
            "Sprites/Default", "Unlit/Color", "UI/Default",
        };

        [MenuItem("Balloon to Heaven/Setup/Configure Project Settings")]
        public static void Apply()
        {
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.bundleVersion = Version;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, BundleId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, BundleId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);

            // Portrait only
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            PlayerSettings.runInBackground = false;

            // Desktop/editor window in 9:16
            PlayerSettings.defaultScreenWidth = 540;
            PlayerSettings.defaultScreenHeight = 960;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;

            // WebGL: GitHub Pages does not send Content-Encoding headers, so keep the
            // decompression fallback on (the loader decompresses in JavaScript).
            PlayerSettings.defaultWebScreenWidth = 540;
            PlayerSettings.defaultWebScreenHeight = 960;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.nameFilesAsHashes = false;
            PlayerSettings.WebGL.template = "APPLICATION:Default";

            // Android: 64-bit IL2CPP (required by Google Play), API 24+
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)24;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.Android.startInFullscreen = true;
            PlayerSettings.Android.renderOutsideSafeArea = true;
            EditorUserBuildSettings.buildAppBundle = false;

            SetLegacyInput();
            AddAlwaysIncludedShaders();
            AssetDatabase.SaveAssets();
            Debug.Log("[Balloon to Heaven] Project settings applied.");
        }

        /// <summary>The game reads touches through the built-in Input Manager.</summary>
        static void SetLegacyInput()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets == null || assets.Length == 0) return;
            var so = new SerializedObject(assets[0]);
            var prop = so.FindProperty("activeInputHandler");
            if (prop != null && prop.intValue == 1) // 1 = new Input System only
            {
                prop.intValue = 2;                  // both: legacy Input keeps working
                so.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log("[Balloon to Heaven] Active input handling set to 'Both' so legacy Input works. Restart the editor if prompted.");
            }
        }

        static void AddAlwaysIncludedShaders()
        {
            var settings = AssetDatabase.LoadAssetAtPath<GraphicsSettings>("ProjectSettings/GraphicsSettings.asset");
            if (settings == null) return;
            var so = new SerializedObject(settings);
            var list = so.FindProperty("m_AlwaysIncludedShaders");
            if (list == null) return;
            foreach (var name in RuntimeShaders)
            {
                var shader = Shader.Find(name);
                if (shader == null) { Debug.LogWarning("[Balloon to Heaven] Shader not found: " + name); continue; }
                bool present = false;
                for (int i = 0; i < list.arraySize; i++)
                    if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader) { present = true; break; }
                if (present) continue;
                list.InsertArrayElementAtIndex(list.arraySize);
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
