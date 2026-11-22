using UnityEditor;
using UnityEngine;

namespace BalloonToHeaven.EditorTools
{
    /// <summary>
    /// First-open convenience: when the project is opened in the editor and the generated
    /// content or scenes are missing, create them automatically. Skipped in batch mode
    /// (the build script does the same steps explicitly).
    /// </summary>
    [InitializeOnLoad]
    public static class AutoSetup
    {
        const string SessionKey = "BalloonToHeaven.AutoSetupDone";

        static AutoSetup()
        {
            if (Application.isBatchMode || SessionState.GetBool(SessionKey, false)) return;
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            SessionState.SetBool(SessionKey, true);
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += Run;
                SessionState.SetBool(SessionKey, false);
                return;
            }
            bool needsContent = !ContentGenerator.DatabaseExists;
            bool needsScenes = !SceneGenerator.ScenesValid();
            if (!needsContent && !needsScenes) return;

            Debug.Log("[Balloon to Heaven] First-time setup: generating " + (needsContent ? "content assets " : "") + (needsScenes ? "scenes" : ""));
            if (needsContent) ContentGenerator.Generate();
            ProjectConfigurator.Apply();
            if (needsScenes) SceneGenerator.Generate();
        }

        [MenuItem("Balloon to Heaven/Setup/Run Full Setup")]
        public static void RunAll()
        {
            ContentGenerator.GenerateIfMissing();
            ProjectConfigurator.Apply();
            SceneGenerator.Generate();
        }
    }
}
