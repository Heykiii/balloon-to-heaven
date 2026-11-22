using BalloonToHeaven.Core;
using BalloonToHeaven.Economy;
using BalloonToHeaven.Saving;

namespace BalloonToHeaven.Upgrades
{
    public enum UpgradeResult { Success, MaxLevel, NotEnoughCurrency, AlreadyOwned, NotOwned, Unknown }

    /// <summary>Permanent workshop upgrades and gem cosmetics.</summary>
    public sealed class UpgradeManager
    {
        readonly SaveManager save;
        readonly EconomyManager economy;

        public WorkshopConfig Config { get; }

        public UpgradeManager(SaveManager save, EconomyManager economy, WorkshopConfig config)
        {
            this.save = save;
            this.economy = economy;
            Config = config;
        }

        public int GetLevel(WorkshopCategory c) => save.Data.workshopLevels[(int)c];
        public int GetNextCost(WorkshopCategory c) => Config.GetCost(c, GetLevel(c));

        public int TotalLevels
        {
            get
            {
                int t = 0;
                foreach (var l in save.Data.workshopLevels) t += l;
                return t;
            }
        }

        public UpgradeResult CanUpgrade(WorkshopCategory c)
        {
            int cost = GetNextCost(c);
            if (cost < 0) return UpgradeResult.MaxLevel;
            return economy.CanAffordCoins(cost) ? UpgradeResult.Success : UpgradeResult.NotEnoughCurrency;
        }

        public UpgradeResult TryUpgrade(WorkshopCategory c)
        {
            var check = CanUpgrade(c);
            if (check != UpgradeResult.Success) return check;
            if (!economy.TrySpendCoins(GetNextCost(c))) return UpgradeResult.NotEnoughCurrency;
            save.Data.workshopLevels[(int)c] = GetLevel(c) + 1;
            save.SaveNow();
            return UpgradeResult.Success;
        }

        // Cosmetics (Sky Gems)
        public bool OwnsTrail(string id) => save.Data.ownedTrails.Contains(id);
        public string EquippedTrail => save.Data.equippedTrail;

        public UpgradeResult TryBuyTrail(TrailCosmetic trail)
        {
            if (trail == null) return UpgradeResult.Unknown;
            if (OwnsTrail(trail.id)) return UpgradeResult.AlreadyOwned;
            if (!economy.TrySpendGems(trail.gemPrice)) return UpgradeResult.NotEnoughCurrency;
            save.Data.ownedTrails.Add(trail.id);
            save.Data.equippedTrail = trail.id;
            save.SaveNow();
            return UpgradeResult.Success;
        }

        public UpgradeResult TryEquipTrail(string id)
        {
            if (!string.IsNullOrEmpty(id) && !OwnsTrail(id)) return UpgradeResult.NotOwned;
            save.Data.equippedTrail = id ?? "";
            save.SaveNow();
            return UpgradeResult.Success;
        }
    }
}
