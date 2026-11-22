using System;
using System.Collections.Generic;
using BalloonToHeaven.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BalloonToHeaven.EditorTools
{
    /// <summary>
    /// Creates one scene per SceneId (Boot first) containing only a SceneEntry, and registers
    /// them in the build settings. All screen content is created from code at runtime.
    /// </summary>
    public static class SceneGenerator
    {
        public const string Folder = "Assets/Scenes";

        public static string PathFor(SceneId id) => Folder + "/" + id + ".unity";

        public static bool ScenesValid()
        {
            var scenes = EditorBuildSettings.scenes;
            var ids = (SceneId[])Enum.GetValues(typeof(SceneId));
            if (scenes.Length < ids.Length) return false;
            if (scenes[0].path != PathFor(SceneId.Boot)) return false;
            foreach (var id in ids)
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(PathFor(id)) == null) return false;
            return true;
        }

        public static void GenerateIfMissing()
        {
            if (!ScenesValid()) Generate();
        }

        [MenuItem("Balloon to Heaven/Setup/Generate Scenes")]
        public static void Generate()
        {
            ContentGenerator.EnsureFolder(Folder);
            var ids = (SceneId[])Enum.GetValues(typeof(SceneId));
            var list = new List<EditorBuildSettingsScene>();
            string previous = SceneManager.GetActiveScene().path;

            foreach (var id in ids)
            {
                string path = PathFor(id);
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var go = new GameObject("SceneEntry");
                go.AddComponent<SceneEntry>().scene = id;
                EditorSceneManager.SaveScene(scene, path);
                list.Add(new EditorBuildSettingsScene(path, true));
            }

            EditorBuildSettings.scenes = list.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("[Balloon to Heaven] Generated " + list.Count + " scenes and updated build settings.");

            if (!Application.isBatchMode)
            {
                if (!string.IsNullOrEmpty(previous) && System.IO.File.Exists(previous)) EditorSceneManager.OpenScene(previous);
                else EditorSceneManager.OpenScene(PathFor(SceneId.Boot));
            }
        }

        [MenuItem("Balloon to Heaven/Open Boot Scene")]
        public static void OpenBoot()
        {
            GenerateIfMissing();
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(PathFor(SceneId.Boot));
        }
    }
}
