using System.Collections.Generic;
using System.IO;
using BalloonToHeaven.Balloons;
using BalloonToHeaven.Bosses;
using BalloonToHeaven.Content;
using BalloonToHeaven.Levels;
using BalloonToHeaven.Obstacles;
using BalloonToHeaven.PowerUps;
using BalloonToHeaven.Worlds;
using UnityEditor;
using UnityEngine;

namespace BalloonToHeaven.EditorTools
{
    /// <summary>
    /// Writes the campaign defined in ContentFactory to editable ScriptableObject assets:
    /// Assets/ScriptableObjects/{Balloons,Worlds,Levels,Obstacles,Bosses,PowerUps,Config}
    /// and Assets/Resources/GameDatabase.asset (loaded at runtime).
    /// </summary>
    public static class ContentGenerator
    {
        public const string Root = "Assets/ScriptableObjects";
        public const string DatabasePath = "Assets/Resources/GameDatabase.asset";

        public static bool DatabaseExists => AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath) != null;

        public static void GenerateIfMissing()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            if (existing != null && existing.IsValid(out _)) return;
            Generate();
        }

        [MenuItem("Balloon to Heaven/Content/Regenerate Content Assets (overwrites)")]
        public static void GenerateMenu()
        {
            if (DatabaseExists && !Application.isBatchMode &&
                !EditorUtility.DisplayDialog("Regenerate content", "This overwrites all generated ScriptableObjects in " + Root + " with the defaults from ContentFactory. Continue?", "Regenerate", "Cancel"))
                return;
            Generate();
        }

        public static void Generate()
        {
            var db = ContentFactory.CreateDatabase();

            EnsureFolder("Assets/Resources");
            foreach (var sub in new[] { "Config", "Balloons", "Worlds", "Levels", "Obstacles", "Bosses", "PowerUps" })
                EnsureFolder(Root + "/" + sub);

            // Collect every ScriptableObject referenced anywhere so no reference is lost on save.
            var obstacles = new HashSet<ObstacleData>(db.obstacles);
            foreach (var boss in db.bosses)
                foreach (var phase in boss.phases)
                    foreach (var attack in phase.attacks)
                        if (attack.projectile != null) obstacles.Add(attack.projectile);
            foreach (var level in db.levels)
                foreach (var entry in level.obstacles)
                    if (entry.obstacle != null) obstacles.Add(entry.obstacle);
            db.obstacles = new List<ObstacleData>(obstacles);
            db.obstacles.Sort((a, b) => string.CompareOrdinal(a.id, b.id));

            AssetDatabase.StartAssetEditing();
            try
            {
                Save(db.config, Root + "/Config/GameConfig.asset");
                Save(db.workshop, Root + "/Config/WorkshopConfig.asset");
                foreach (var b in db.balloons) Save(b, Root + "/Balloons/" + Sanitize(b.sortOrder.ToString("00") + "_" + b.id) + ".asset");
                foreach (var p in db.powerUps) Save(p, Root + "/PowerUps/" + p.type + ".asset");
                foreach (var o in db.obstacles) Save(o, Root + "/Obstacles/" + Sanitize(o.id) + ".asset");
                foreach (var boss in db.bosses) Save(boss, Root + "/Bosses/" + Sanitize(boss.id) + ".asset");
                foreach (var w in db.worlds) Save(w, Root + "/Worlds/World" + w.worldNumber.ToString("00") + "_" + Sanitize(w.worldName) + ".asset");
                foreach (var l in db.levels) Save(l, Root + "/Levels/Level" + l.levelNumber.ToString("00") + ".asset");
                Save(db, DatabasePath);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            foreach (var o in new Object[] { db, db.config, db.workshop }) EditorUtility.SetDirty(o);
            foreach (var l in db.levels) EditorUtility.SetDirty(l);
            foreach (var w in db.worlds) EditorUtility.SetDirty(w);
            foreach (var b in db.bosses) EditorUtility.SetDirty(b);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var reloaded = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            string error = "not found";
            if (reloaded == null || !reloaded.IsValid(out error))
                Debug.LogError("[Balloon to Heaven] Generated database failed validation: " + error);
            else
                Debug.Log("[Balloon to Heaven] Content generated: " + db.balloons.Count + " balloons, " + db.worlds.Count + " worlds, " + db.levels.Count + " levels, " + db.bosses.Count + " bosses, " + db.obstacles.Count + " obstacles.");
        }

        static void Save(Object asset, string path)
        {
            if (asset == null) return;
            var existing = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (existing != null) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(asset, path);
        }

        static string Sanitize(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return name.Replace(' ', '_').Replace('\'', '_');
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
