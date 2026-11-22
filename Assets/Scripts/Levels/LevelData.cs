using System;
using System.Collections.Generic;
using BalloonToHeaven.Bosses;
using BalloonToHeaven.Core;
using BalloonToHeaven.Obstacles;
using UnityEngine;

namespace BalloonToHeaven.Levels
{
    public enum LevelRole { Introduction, NewObstacle, IncreasedDifficulty, AdvancedCombination, Survival, Boss }
    public enum StarChallengeType { NoDamage, AllSpecialCoins, CoinTarget, CollectPowerUps }
    public enum WindMode { None, Gusts, Unpredictable }

    [Serializable]
    public class ObstacleSpawnEntry
    {
        public ObstacleData obstacle;
        public float weight = 1f;
        [Range(0f, 1f)] public float startProgress;
    }

    [Serializable]
    public class PowerUpSpawnEntry
    {
        public PowerUpType type;
        public float weight = 1f;
    }

    /// <summary>One designed level. 60 instances drive the whole campaign through shared systems.</summary>
    [CreateAssetMenu(menuName = "Balloon to Heaven/Level", fileName = "Level")]
    public class LevelData : ScriptableObject
    {
        [Header("Identity")]
        public int levelNumber;
        public int worldNumber;
        public int indexInWorld;
        public string levelName;
        [TextArea] public string hint;
        public LevelRole role;
        [Range(0f, 1f)] public float difficulty;

        [Header("Ascent")]
        public float ascentSpeed = 3f;
        public float ascentDuration = 50f;

        [Header("Obstacles")]
        public List<ObstacleSpawnEntry> obstacles = new List<ObstacleSpawnEntry>();
        public float spawnIntervalStart = 2.2f;
        public float spawnIntervalEnd = 1.5f;
        public int maxActiveObstacles = 6;
        public float obstacleSpeedMultiplier = 1f;
        [Tooltip("Lanes (out of the configured lane count) that must stay free on every spawn row.")]
        public int minFreeLanes = 3;

        [Header("Coins")]
        public float coinPatternInterval = 2.4f;
        public int coinsPerPattern = 5;
        public int specialCoinCount = 3;

        [Header("Power-ups")]
        public float powerUpInterval = 14f;
        public List<PowerUpSpawnEntry> powerUps = new List<PowerUpSpawnEntry>();

        [Header("Environment")]
        public WindMode windMode;
        public float windStrength;
        [Range(0f, 1f)] public float darkness;
        [Range(0f, 1f)] public float slipperiness;
        [Range(0f, 1f)] public float gravityDistortion;
        public bool lightShadowZones;

        [Header("Stars")]
        public int coinsForSecondStar = 30;
        public StarChallengeType challenge;
        public int challengeValue;

        [Header("Finish")]
        public bool isBossLevel;
        public BossData boss;

        public float LevelLength => Mathf.Max(1f, ascentSpeed * ascentDuration);

        public string ChallengeDescription()
        {
            switch (challenge)
            {
                case StarChallengeType.NoDamage: return "Finish without taking damage";
                case StarChallengeType.AllSpecialCoins: return "Collect all " + specialCoinCount + " star coins";
                case StarChallengeType.CoinTarget: return "Collect " + challengeValue + " coins";
                case StarChallengeType.CollectPowerUps: return "Collect " + challengeValue + " power-ups";
            }
            return string.Empty;
        }
    }
}
