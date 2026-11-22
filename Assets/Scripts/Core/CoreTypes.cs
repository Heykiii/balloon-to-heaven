using System;
using UnityEngine;

namespace BalloonToHeaven.Core
{
    /// <summary>Category of damage. Balloon abilities can resist specific categories.</summary>
    public enum DamageType { Physical, Fire, Ice, Electric, Space, Magic }

    /// <summary>Describes one damaging contact between a hazard and the balloon.</summary>
    public struct DamageInfo
    {
        public int Amount;
        public DamageType Type;
        public Vector3 SourcePosition;

        public DamageInfo(int amount, DamageType type, Vector3 sourcePosition)
        {
            Amount = amount;
            Type = type;
            SourcePosition = sourcePosition;
        }
    }

    /// <summary>All scenes of the game. Names match the generated .unity files.</summary>
    public enum SceneId { Boot, MainMenu, WorldMap, Gameplay, BalloonShop, Workshop, Results }

    public enum PowerUpType { CoinMagnet, EnergyShield, SkyRocket, ExtraLife, DoubleCoins }

    public enum SfxId
    {
        Click, Coin, SpecialCoin, Hit, Shield, PowerUp, Upgrade, Purchase, BossAttack,
        LevelComplete, Fail, Whoosh, Thunder, Deny, Star, BossDefeat, Unlock, Crystal, Teleport
    }

    /// <summary>Global constants that are structural (not balancing values).</summary>
    public static class GameConstants
    {
        public const int WorldCount = 10;
        public const int LevelsPerWorld = 6;
        public const int TotalLevels = WorldCount * LevelsPerWorld;
        public const int BalloonCount = 20;
        public const int MaxBalloonStage = 5;
        public const int WorkshopCategoryCount = 4;
        public const int MaxWorkshopLevel = 5;
        public const string DefaultBalloonId = "classic";

        public static int WorldOfLevel(int levelNumber) => (levelNumber - 1) / LevelsPerWorld + 1;
        public static int IndexInWorld(int levelNumber) => (levelNumber - 1) % LevelsPerWorld + 1;
        public static int FirstLevelOfWorld(int world) => (world - 1) * LevelsPerWorld + 1;
    }
}
