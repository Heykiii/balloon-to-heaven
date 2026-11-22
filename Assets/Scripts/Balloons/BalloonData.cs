using System;
using UnityEngine;

namespace BalloonToHeaven.Balloons
{
    public enum BalloonRarity { Common, Rare, Epic, Legendary, Mythical }

    public enum BalloonAbilityType
    {
        None, BonusCoins, ImprovedMagnet, WindResistance, PeriodicShield, FireResistance,
        EnvironmentalResistance, Maneuverability, PhysicalResistance, CoinRewardBoost,
        PowerUpDuration, Invisibility, SpaceResistance, GravityResistance, PhoenixRecovery,
        Teleport, HeavenlyShield, StrongMagnet, Invulnerability, UltimateProtection
    }

    public enum EnvelopeShape { Classic, Round, Tall, Wide, Teardrop }

    public enum EnvelopePattern { Solid, Stripes, Dots, Zigzag, Stars, Checker, Gradient, Swirl, Bands, Flames, Crystal, Galaxy, Rainbow, SunRays, Leaves }

    [Flags]
    public enum BalloonOrnament
    {
        None = 0, Wings = 1, Ring = 2, Flames = 4, Crystals = 8, Crown = 16, Halo = 32,
        Leaves = 64, SunRays = 128, Stars = 256, Shadow = 512, Orbit = 1024
    }

    /// <summary>Stat bonus granted when a balloon reaches a given stage (index 0 = stage 1).</summary>
    [Serializable]
    public class BalloonStageBonus
    {
        public int bonusHealth;
        public float speedBonus;
        public int bonusArmor;
        public float magnetBonus;
    }

    /// <summary>Data definition of one balloon. All 20 balloons share the same runtime logic.</summary>
    [CreateAssetMenu(menuName = "Balloon to Heaven/Balloon", fileName = "Balloon")]
    public class BalloonData : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;
        [TextArea] public string description;
        public BalloonRarity rarity;
        public int sortOrder;
        public int price;
        [Tooltip("World the player must reach before this balloon can be bought.")]
        public int unlockWorld = 1;

        [Header("Base statistics")]
        public int baseHealth = 1;
        [Tooltip("Multiplier on horizontal speed and acceleration.")]
        public float speed = 1f;
        public int armor;
        public float magnetRadius = 1.1f;

        [Header("Special ability")]
        public BalloonAbilityType ability;
        public string abilityName;
        [TextArea] public string abilityDescription;
        public float abilityValue;
        public float abilityDuration;
        public float abilityCooldown;

        [Header("Visuals")]
        public EnvelopeShape shape;
        public EnvelopePattern pattern;
        public Color primaryColor = Color.red;
        public Color secondaryColor = Color.white;
        public Color accentColor = Color.yellow;
        public Color basketColor = new Color(0.55f, 0.35f, 0.18f);
        public Color glowColor = Color.white;
        public BalloonOrnament ornaments;

        [Header("Upgrade stages (2..5)")]
        public int[] stageUpgradeCosts = new int[4];
        public BalloonStageBonus[] stageBonuses = new BalloonStageBonus[5];

        /// <summary>Cost to go from currentStage to currentStage + 1, or -1 when maxed.</summary>
        public int GetUpgradeCost(int currentStage)
        {
            int index = currentStage - 1;
            if (currentStage >= Core.GameConstants.MaxBalloonStage || stageUpgradeCosts == null || index < 0 || index >= stageUpgradeCosts.Length)
                return -1;
            return Mathf.Max(0, stageUpgradeCosts[index]);
        }

        public static Color RarityColor(BalloonRarity rarity)
        {
            switch (rarity)
            {
                case BalloonRarity.Common: return new Color(0.62f, 0.78f, 0.86f);
                case BalloonRarity.Rare: return new Color(0.25f, 0.6f, 1f);
                case BalloonRarity.Epic: return new Color(0.7f, 0.35f, 1f);
                case BalloonRarity.Legendary: return new Color(1f, 0.62f, 0.12f);
                default: return new Color(1f, 0.3f, 0.55f);
            }
        }
    }
}
