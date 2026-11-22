using System;
using System.Collections.Generic;
using BalloonToHeaven.Balloons;
using BalloonToHeaven.Bosses;
using BalloonToHeaven.Core;
using BalloonToHeaven.Levels;
using BalloonToHeaven.Obstacles;
using BalloonToHeaven.PowerUps;
using BalloonToHeaven.Upgrades;
using BalloonToHeaven.Worlds;
using UnityEngine;

namespace BalloonToHeaven.Content
{
    public enum AchievementStat
    {
        LevelsCompleted, TotalCoinsCollected, BalloonsOwned, BossesDefeated, NoDamageClears,
        TotalStars, MaxBalloonStage, WorkshopLevels, PowerUpsCollected, WorldsCompleted, CampaignCompleted
    }

    [Serializable]
    public class AchievementDefinition
    {
        public string id;
        public string title;
        public string description;
        public AchievementStat stat;
        public int target = 1;
        public int gemReward = 3;
    }

    /// <summary>
    /// Root of all game content. The editor generator saves one into Resources; at runtime the
    /// same content is rebuilt in memory if the asset is missing so the game always boots.
    /// </summary>
    [CreateAssetMenu(menuName = "Balloon to Heaven/Game Database", fileName = "GameDatabase")]
    public class GameDatabase : ScriptableObject
    {
        public const string ResourcePath = "GameDatabase";

        public GameConfig config;
        public WorkshopConfig workshop;
        public List<BalloonData> balloons = new List<BalloonData>();
        public List<WorldData> worlds = new List<WorldData>();
        public List<LevelData> levels = new List<LevelData>();
        public List<ObstacleData> obstacles = new List<ObstacleData>();
        public List<BossData> bosses = new List<BossData>();
        public List<PowerUpData> powerUps = new List<PowerUpData>();
        public List<TrailCosmetic> trails = new List<TrailCosmetic>();
        public List<AchievementDefinition> achievements = new List<AchievementDefinition>();

        Dictionary<string, BalloonData> balloonLookup;

        public GameConfig Config => config;
        public WorkshopConfig Workshop => workshop;

        public static GameDatabase LoadOrCreate()
        {
            var db = Resources.Load<GameDatabase>(ResourcePath);
            if (db != null && db.IsValid(out _)) return db;
            if (db != null) Debug.LogWarning("[GameDatabase] Asset is incomplete, rebuilding content in memory.");
            return ContentFactory.CreateDatabase();
        }

        /// <summary>Checks the structural requirements of the campaign.</summary>
        public bool IsValid(out string error)
        {
            error = null;
            if (config == null) error = "Missing GameConfig";
            else if (workshop == null) error = "Missing WorkshopConfig";
            else if (balloons.Count != GameConstants.BalloonCount) error = "Expected 20 balloons, found " + balloons.Count;
            else if (worlds.Count != GameConstants.WorldCount) error = "Expected 10 worlds, found " + worlds.Count;
            else if (levels.Count != GameConstants.TotalLevels) error = "Expected 60 levels, found " + levels.Count;
            else if (bosses.Count != GameConstants.WorldCount) error = "Expected 10 bosses, found " + bosses.Count;
            else if (powerUps.Count != 5) error = "Expected 5 power-ups, found " + powerUps.Count;
            else
            {
                for (int i = 0; i < levels.Count; i++)
                {
                    if (levels[i] == null || levels[i].levelNumber != i + 1) { error = "Level " + (i + 1) + " missing or out of order"; break; }
                    if (levels[i].isBossLevel && levels[i].boss == null) { error = "Boss level " + (i + 1) + " has no boss"; break; }
                }
                foreach (var b in balloons) if (b == null) { error = "Null balloon entry"; break; }
            }
            return error == null;
        }

        public BalloonData GetBalloon(string id)
        {
            if (balloonLookup == null || balloonLookup.Count != balloons.Count)
            {
                balloonLookup = new Dictionary<string, BalloonData>();
                foreach (var b in balloons) if (b != null && !string.IsNullOrEmpty(b.id)) balloonLookup[b.id] = b;
            }
            if (id != null && balloonLookup.TryGetValue(id, out var data)) return data;
            return null;
        }

        public BalloonData DefaultBalloon => GetBalloon(GameConstants.DefaultBalloonId) ?? (balloons.Count > 0 ? balloons[0] : null);

        public LevelData GetLevel(int levelNumber)
        {
            int index = levelNumber - 1;
            return index >= 0 && index < levels.Count ? levels[index] : null;
        }

        public WorldData GetWorld(int worldNumber)
        {
            int index = worldNumber - 1;
            return index >= 0 && index < worlds.Count ? worlds[index] : null;
        }

        public PowerUpData GetPowerUp(PowerUpType type)
        {
            foreach (var p in powerUps) if (p != null && p.type == type) return p;
            return null;
        }

        public TrailCosmetic GetTrail(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var t in trails) if (t.id == id) return t;
            return null;
        }

        public ObstacleData GetObstacle(string id)
        {
            foreach (var o in obstacles) if (o != null && o.id == id) return o;
            return null;
        }
    }
}
