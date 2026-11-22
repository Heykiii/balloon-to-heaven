using System.Collections.Generic;

namespace BalloonToHeaven.Levels
{
    /// <summary>Raw outcome of one run, produced by the gameplay scene.</summary>
    public class LevelResult
    {
        public int levelNumber;
        public bool victory;
        public int coinsCollected;
        public int specialCoinsCollected;
        public int specialCoinsTotal;
        public bool tookDamage;
        public int powerUpsCollected;
        public float progress;
        public float timeSeconds;
        public bool bossDefeated;
        public string balloonId;
        public float rewardMultiplier = 1f;

        // Filled in by ProgressionManager
        public int stars;
        public bool[] starFlags = new bool[3];
        public RewardBreakdown rewards = new RewardBreakdown();
    }

    public class RewardLine
    {
        public string label;
        public int coins;
        public int gems;
        public RewardLine(string label, int coins, int gems = 0) { this.label = label; this.coins = coins; this.gems = gems; }
    }

    public class RewardBreakdown
    {
        public readonly List<RewardLine> lines = new List<RewardLine>();
        public readonly List<string> unlocks = new List<string>();
        public bool newBestStars;
        public bool campaignJustCompleted;

        public int TotalCoins
        {
            get { int t = 0; foreach (var l in lines) t += l.coins; return t; }
        }

        public int TotalGems
        {
            get { int t = 0; foreach (var l in lines) t += l.gems; return t; }
        }
    }

    /// <summary>Evaluates the three star conditions of a level.</summary>
    public static class StarCalculator
    {
        public static int Calculate(LevelData level, LevelResult result, bool[] flags)
        {
            if (flags == null || flags.Length < 3) flags = new bool[3];
            flags[0] = flags[1] = flags[2] = false;
            if (level == null || result == null || !result.victory) return 0;
            flags[0] = true;
            flags[1] = result.coinsCollected >= level.coinsForSecondStar;
            switch (level.challenge)
            {
                case StarChallengeType.NoDamage: flags[2] = !result.tookDamage; break;
                case StarChallengeType.AllSpecialCoins: flags[2] = result.specialCoinsTotal > 0 && result.specialCoinsCollected >= result.specialCoinsTotal; break;
                case StarChallengeType.CoinTarget: flags[2] = result.coinsCollected >= level.challengeValue; break;
                case StarChallengeType.CollectPowerUps: flags[2] = result.powerUpsCollected >= level.challengeValue; break;
            }
            int stars = 0;
            foreach (var f in flags) if (f) stars++;
            return stars;
        }
    }
}
