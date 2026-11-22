using BalloonToHeaven.Content;
using BalloonToHeaven.Core;
using BalloonToHeaven.Economy;
using BalloonToHeaven.Saving;
using BalloonToHeaven.Upgrades;
using UnityEngine;

namespace BalloonToHeaven.Balloons
{
    public enum ShopResult { Success, AlreadyOwned, NotOwned, NotEnoughCoins, LockedByProgress, MaxStage, UnknownBalloon }

    /// <summary>Computed statistics for a balloon at a given stage with the player's workshop upgrades.</summary>
    public struct BalloonStatBlock
    {
        public int maxHealth;
        public float speedMultiplier;
        public int armor;
        public float magnetRadius;
        public float shieldBonusSeconds;
    }

    public static class BalloonStatCalculator
    {
        public static BalloonStatBlock Compute(BalloonData data, int stage, int[] workshopLevels, WorkshopConfig workshop)
        {
            var block = new BalloonStatBlock
            {
                maxHealth = Mathf.Max(1, data.baseHealth),
                speedMultiplier = Mathf.Max(0.5f, data.speed),
                armor = Mathf.Max(0, data.armor),
                magnetRadius = Mathf.Max(0f, data.magnetRadius)
            };
            stage = Mathf.Clamp(stage, 1, GameConstants.MaxBalloonStage);
            if (data.stageBonuses != null)
            {
                for (int s = 0; s < stage && s < data.stageBonuses.Length; s++)
                {
                    var bonus = data.stageBonuses[s];
                    if (bonus == null) continue;
                    // Stage bonuses are cumulative definitions: use the highest reached stage only.
                    if (s == stage - 1)
                    {
                        block.maxHealth += bonus.bonusHealth;
                        block.speedMultiplier += bonus.speedBonus;
                        block.armor += bonus.bonusArmor;
                        block.magnetRadius += bonus.magnetBonus;
                    }
                }
            }
            if (workshop != null && workshopLevels != null && workshopLevels.Length >= GameConstants.WorkshopCategoryCount)
            {
                block.maxHealth += workshop.HealthBonus(workshopLevels[(int)WorkshopCategory.Durability]);
                block.armor += workshop.ArmorBonus(workshopLevels[(int)WorkshopCategory.Durability]);
                block.magnetRadius += workshop.MagnetBonus(workshopLevels[(int)WorkshopCategory.CoinMagnet]);
                block.speedMultiplier *= workshop.MovementMultiplier(workshopLevels[(int)WorkshopCategory.Movement]);
                block.shieldBonusSeconds = workshop.ShieldBonus(workshopLevels[(int)WorkshopCategory.ShieldDuration]);
            }
            if (data.ability == BalloonAbilityType.ImprovedMagnet || data.ability == BalloonAbilityType.StrongMagnet)
                block.magnetRadius += data.abilityValue;
            return block;
        }
    }

    /// <summary>Purchasing, equipping and stage upgrades of balloons with full validation.</summary>
    public sealed class BalloonShopManager
    {
        readonly SaveManager save;
        readonly EconomyManager economy;
        readonly GameDatabase db;

        public BalloonShopManager(SaveManager save, EconomyManager economy, GameDatabase db)
        {
            this.save = save;
            this.economy = economy;
            this.db = db;
        }

        public bool IsOwned(string id) => save.Data.ownedBalloons.Contains(id);
        public bool IsEquipped(string id) => save.Data.equippedBalloon == id;
        public int GetStage(string id) => IsOwned(id) ? save.Data.GetStage(id) : 1;
        public BalloonData Equipped => db.GetBalloon(save.Data.equippedBalloon) ?? db.DefaultBalloon;

        public bool IsUnlockedByProgress(BalloonData data)
        {
            if (data == null) return false;
            int highestWorld = GameConstants.WorldOfLevel(Mathf.Clamp(save.Data.highestUnlockedLevel, 1, GameConstants.TotalLevels));
            return data.unlockWorld <= highestWorld;
        }

        public BalloonStatBlock GetStats(BalloonData data, int stageOverride = -1)
        {
            int stage = stageOverride > 0 ? stageOverride : GetStage(data.id);
            return BalloonStatCalculator.Compute(data, stage, save.Data.workshopLevels, db.Workshop);
        }

        public ShopResult CanPurchase(string id)
        {
            var data = db.GetBalloon(id);
            if (data == null) return ShopResult.UnknownBalloon;
            if (IsOwned(id)) return ShopResult.AlreadyOwned;
            if (!IsUnlockedByProgress(data)) return ShopResult.LockedByProgress;
            if (!economy.CanAffordCoins(data.price)) return ShopResult.NotEnoughCoins;
            return ShopResult.Success;
        }

        public ShopResult TryPurchase(string id)
        {
            var check = CanPurchase(id);
            if (check != ShopResult.Success) return check;
            var data = db.GetBalloon(id);
            if (!economy.TrySpendCoins(data.price)) return ShopResult.NotEnoughCoins;
            save.Data.ownedBalloons.Add(id);
            save.Data.SetStage(id, 1);
            save.SaveNow();
            return ShopResult.Success;
        }

        public ShopResult TryEquip(string id)
        {
            if (db.GetBalloon(id) == null) return ShopResult.UnknownBalloon;
            if (!IsOwned(id)) return ShopResult.NotOwned;
            save.Data.equippedBalloon = id;
            save.SaveNow();
            return ShopResult.Success;
        }

        public int GetUpgradeCost(string id)
        {
            var data = db.GetBalloon(id);
            return data == null ? -1 : data.GetUpgradeCost(GetStage(id));
        }

        public ShopResult CanUpgrade(string id)
        {
            var data = db.GetBalloon(id);
            if (data == null) return ShopResult.UnknownBalloon;
            if (!IsOwned(id)) return ShopResult.NotOwned;
            int cost = data.GetUpgradeCost(GetStage(id));
            if (cost < 0) return ShopResult.MaxStage;
            if (!economy.CanAffordCoins(cost)) return ShopResult.NotEnoughCoins;
            return ShopResult.Success;
        }

        public ShopResult TryUpgrade(string id)
        {
            var check = CanUpgrade(id);
            if (check != ShopResult.Success) return check;
            int stage = GetStage(id);
            int cost = db.GetBalloon(id).GetUpgradeCost(stage);
            if (!economy.TrySpendCoins(cost)) return ShopResult.NotEnoughCoins;
            save.Data.SetStage(id, stage + 1);
            save.SaveNow();
            return ShopResult.Success;
        }

        public static string Describe(ShopResult r)
        {
            switch (r)
            {
                case ShopResult.Success: return "Done!";
                case ShopResult.AlreadyOwned: return "You already own this balloon.";
                case ShopResult.NotOwned: return "Buy this balloon first.";
                case ShopResult.NotEnoughCoins: return "Not enough coins.";
                case ShopResult.LockedByProgress: return "Reach a later world to unlock.";
                case ShopResult.MaxStage: return "Already at maximum stage.";
                default: return "Unknown balloon.";
            }
        }
    }
}
