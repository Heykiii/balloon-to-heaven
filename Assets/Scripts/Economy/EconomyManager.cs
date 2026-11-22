using BalloonToHeaven.Core;
using BalloonToHeaven.Saving;
using UnityEngine;

namespace BalloonToHeaven.Economy
{
    /// <summary>
    /// The single authority over coins and gems. Validates every spend, never allows negative
    /// balances and records one-time rewards so they cannot be granted twice.
    /// </summary>
    public sealed class EconomyManager
    {
        const int MaxBalance = 999_999_999;
        readonly SaveManager save;

        public RewardConfig Rewards { get; }

        public EconomyManager(SaveManager save, RewardConfig rewards)
        {
            this.save = save;
            Rewards = rewards ?? new RewardConfig();
        }

        public int Coins => save.Data.coins;
        public int Gems => save.Data.gems;

        public bool CanAffordCoins(int amount) => amount >= 0 && save.Data.coins >= amount;
        public bool CanAffordGems(int amount) => amount >= 0 && save.Data.gems >= amount;

        public void AddCoins(int amount, bool persist = true)
        {
            if (amount <= 0) return;
            save.Data.coins = (int)Mathf.Min(MaxBalance, (long)save.Data.coins + amount);
            Changed(persist);
        }

        public void AddGems(int amount, bool persist = true)
        {
            if (amount <= 0) return;
            save.Data.gems = (int)Mathf.Min(MaxBalance, (long)save.Data.gems + amount);
            Changed(persist);
        }

        public bool TrySpendCoins(int amount)
        {
            if (amount < 0 || save.Data.coins < amount) return false;
            save.Data.coins -= amount;
            Changed(true);
            return true;
        }

        public bool TrySpendGems(int amount)
        {
            if (amount < 0 || save.Data.gems < amount) return false;
            save.Data.gems -= amount;
            Changed(true);
            return true;
        }

        public bool IsRewardClaimed(string rewardId) => save.Data.claimedRewards.Contains(rewardId);

        /// <summary>Grants a reward exactly once per save. Returns false if already claimed.</summary>
        public bool TryGrantOnce(string rewardId, int coins, int gems)
        {
            if (string.IsNullOrEmpty(rewardId) || IsRewardClaimed(rewardId)) return false;
            save.Data.claimedRewards.Add(rewardId);
            if (coins > 0) save.Data.coins = (int)Mathf.Min(MaxBalance, (long)save.Data.coins + coins);
            if (gems > 0) save.Data.gems = (int)Mathf.Min(MaxBalance, (long)save.Data.gems + gems);
            Changed(true);
            return true;
        }

        void Changed(bool persist)
        {
            if (persist) save.SaveNow(); else save.MarkDirty();
            GameEvents.RaiseCurrencyChanged(save.Data.coins, save.Data.gems);
        }
    }
}
