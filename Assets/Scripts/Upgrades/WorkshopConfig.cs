using System;
using UnityEngine;

namespace BalloonToHeaven.Upgrades
{
    public enum WorkshopCategory { Durability = 0, CoinMagnet = 1, Movement = 2, ShieldDuration = 3 }

    /// <summary>Permanent upgrade tables. Index 0 = not upgraded, 1..5 = purchased levels.</summary>
    [CreateAssetMenu(menuName = "Balloon to Heaven/Workshop Config", fileName = "WorkshopConfig")]
    public class WorkshopConfig : ScriptableObject
    {
        [Header("Costs for levels 1..5 (per category)")]
        public int[] durabilityCosts = { 600, 1800, 4500, 10000, 20000 };
        public int[] magnetCosts = { 400, 1200, 3000, 7000, 14000 };
        public int[] movementCosts = { 500, 1500, 3800, 8500, 17000 };
        public int[] shieldCosts = { 450, 1300, 3400, 7800, 15500 };

        [Header("Effects by level 0..5")]
        public int[] durabilityHealth = { 0, 1, 1, 2, 2, 3 };
        public int[] durabilityArmor = { 0, 0, 1, 1, 2, 2 };
        public float[] magnetRadiusBonus = { 0f, 0.3f, 0.6f, 0.9f, 1.25f, 1.6f };
        public float[] movementMultiplier = { 1f, 1.05f, 1.1f, 1.15f, 1.2f, 1.26f };
        public float[] shieldSecondsBonus = { 0f, 1f, 2f, 3f, 4f, 5f };

        public int[] CostsFor(WorkshopCategory category)
        {
            switch (category)
            {
                case WorkshopCategory.Durability: return durabilityCosts;
                case WorkshopCategory.CoinMagnet: return magnetCosts;
                case WorkshopCategory.Movement: return movementCosts;
                default: return shieldCosts;
            }
        }

        /// <summary>Cost to buy the next level, or -1 if maxed.</summary>
        public int GetCost(WorkshopCategory category, int currentLevel)
        {
            var costs = CostsFor(category);
            if (costs == null || currentLevel < 0 || currentLevel >= costs.Length || currentLevel >= Core.GameConstants.MaxWorkshopLevel) return -1;
            return costs[currentLevel];
        }

        static T At<T>(T[] array, int level, T fallback)
        {
            if (array == null || array.Length == 0) return fallback;
            return array[Mathf.Clamp(level, 0, array.Length - 1)];
        }

        public int HealthBonus(int level) => At(durabilityHealth, level, 0);
        public int ArmorBonus(int level) => At(durabilityArmor, level, 0);
        public float MagnetBonus(int level) => At(magnetRadiusBonus, level, 0f);
        public float MovementMultiplier(int level) => At(movementMultiplier, level, 1f);
        public float ShieldBonus(int level) => At(shieldSecondsBonus, level, 0f);

        public static string CategoryName(WorkshopCategory c)
        {
            switch (c)
            {
                case WorkshopCategory.Durability: return "Durability";
                case WorkshopCategory.CoinMagnet: return "Coin Magnet";
                case WorkshopCategory.Movement: return "Movement";
                default: return "Shield Duration";
            }
        }

        public string DescribeLevel(WorkshopCategory c, int level)
        {
            switch (c)
            {
                case WorkshopCategory.Durability: return "+" + HealthBonus(level) + " HP, +" + ArmorBonus(level) + " armor";
                case WorkshopCategory.CoinMagnet: return "+" + MagnetBonus(level).ToString("0.0#") + " magnet radius";
                case WorkshopCategory.Movement: return "x" + MovementMultiplier(level).ToString("0.00") + " speed";
                default: return "+" + ShieldBonus(level).ToString("0") + "s shields";
            }
        }
    }

    /// <summary>Gem-priced cosmetic trail.</summary>
    [Serializable]
    public class TrailCosmetic
    {
        public string id;
        public string displayName;
        public int gemPrice;
        public Color colorA = Color.white;
        public Color colorB = Color.white;
        public bool additive = true;
    }
}
