using System.Collections.Generic;
using BalloonToHeaven.Content;
using BalloonToHeaven.Core;
using BalloonToHeaven.Economy;
using BalloonToHeaven.Levels;
using BalloonToHeaven.Saving;
using UnityEngine;

namespace BalloonToHeaven.Progression
{
    /// <summary>Level and world unlocking, star records and level reward payouts.</summary>
    public sealed class ProgressionManager
    {
        readonly SaveManager save;
        readonly EconomyManager economy;
        readonly GameDatabase db;

        public ProgressionManager(SaveManager save, EconomyManager economy, GameDatabase db)
        {
            this.save = save;
            this.economy = economy;
            this.db = db;
        }

        SaveData Data => save.Data;

        public bool IsLevelUnlocked(int level) => level >= 1 && level <= Data.highestUnlockedLevel;
        public bool IsLevelCompleted(int level) => level >= 1 && level <= GameConstants.TotalLevels && Data.levelCompleted[level - 1];
        public int GetStars(int level) => level >= 1 && level <= GameConstants.TotalLevels ? Data.levelStars[level - 1] : 0;
        public bool IsWorldUnlocked(int world) => IsLevelUnlocked(GameConstants.FirstLevelOfWorld(world));
        public bool IsWorldCompleted(int world) => IsLevelCompleted(world * GameConstants.LevelsPerWorld);

        public int HighestUnlockedWorld => GameConstants.WorldOfLevel(Mathf.Clamp(Data.highestUnlockedLevel, 1, GameConstants.TotalLevels));

        public int WorldsCompleted
        {
            get
            {
                int n = 0;
                for (int w = 1; w <= GameConstants.WorldCount; w++) if (IsWorldCompleted(w)) n++;
                return n;
            }
        }

        public int WorldStars(int world)
        {
            int total = 0;
            int first = GameConstants.FirstLevelOfWorld(world);
            for (int i = 0; i < GameConstants.LevelsPerWorld; i++) total += GetStars(first + i);
            return total;
        }

        /// <summary>The next level to play: the highest unlocked level.</summary>
        public int CurrentLevel => Mathf.Clamp(Data.highestUnlockedLevel, 1, GameConstants.TotalLevels);

        /// <summary>
        /// Applies a finished run: computes stars, pays rewards (one-time rewards are protected
        /// against duplicates), updates records and unlocks. Saves immediately.
        /// </summary>
        public void RecordResult(LevelResult result)
        {
            var level = db.GetLevel(result.levelNumber);
            var rewards = result.rewards;
            var cfg = economy.Rewards;
            int index = result.levelNumber - 1;
            if (level == null || index < 0 || index >= GameConstants.TotalLevels) return;

            Data.stats.totalCoinsCollected += Mathf.Max(0, result.coinsCollected);
            int coinsToAdd = 0;

            if (result.coinsCollected > 0)
            {
                rewards.lines.Add(new RewardLine("Coins collected", result.coinsCollected));
                coinsToAdd += result.coinsCollected;
            }

            if (!result.victory)
            {
                Data.stats.levelsFailed++;
                result.stars = 0;
                economy.AddCoins(coinsToAdd, false);
                save.SaveNow();
                return;
            }

            result.stars = StarCalculator.Calculate(level, result, result.starFlags);
            float mult = Mathf.Max(1f, result.rewardMultiplier);
            int Scaled(int v) => Mathf.RoundToInt(v * mult);

            rewards.lines.Add(new RewardLine("Level complete", Scaled(cfg.levelComplete)));
            coinsToAdd += Scaled(cfg.levelComplete);
            if (!result.tookDamage)
            {
                rewards.lines.Add(new RewardLine("No damage bonus", Scaled(cfg.noDamageBonus)));
                coinsToAdd += Scaled(cfg.noDamageBonus);
            }
            if (result.specialCoinsTotal > 0 && result.specialCoinsCollected >= result.specialCoinsTotal)
            {
                rewards.lines.Add(new RewardLine("All star coins", Scaled(cfg.specialCoinsBonus)));
                coinsToAdd += Scaled(cfg.specialCoinsBonus);
            }
            if (level.isBossLevel && result.bossDefeated)
            {
                int bossCoins = level.boss != null ? Mathf.Max(cfg.bossDefeat, level.boss.rewardCoins) : cfg.bossDefeat;
                rewards.lines.Add(new RewardLine("Boss defeated", Scaled(bossCoins)));
                coinsToAdd += Scaled(bossCoins);
            }
            if (mult > 1f) rewards.lines.Add(new RewardLine("Balloon bonus x" + mult.ToString("0.##") + " applied", 0));

            economy.AddCoins(coinsToAdd, false);

            // One-time rewards
            if (result.stars == 3 && economy.TryGrantOnce("three_star_" + result.levelNumber, cfg.threeStarBonus, cfg.firstThreeStarGems))
                rewards.lines.Add(new RewardLine("First three-star bonus", cfg.threeStarBonus, cfg.firstThreeStarGems));

            if (level.isBossLevel && result.bossDefeated && level.boss != null)
            {
                int gems = Mathf.Max(cfg.bossFirstDefeatGems, level.boss.rewardGems);
                if (economy.TryGrantOnce("boss_" + level.boss.id, 0, gems))
                    rewards.lines.Add(new RewardLine("First victory over " + level.boss.bossName, 0, gems));
                if (!Data.stats.bossesDefeated.Contains(level.boss.id)) Data.stats.bossesDefeated.Add(level.boss.id);
            }

            // Records
            if (result.stars > Data.levelStars[index]) { Data.levelStars[index] = result.stars; rewards.newBestStars = true; }
            Data.bestCoins[index] = Mathf.Max(Data.bestCoins[index], result.coinsCollected);
            bool firstClear = !Data.levelCompleted[index];
            Data.levelCompleted[index] = true;
            Data.stats.levelsCompleted++;
            if (!result.tookDamage) Data.stats.noDamageClears++;

            // World completion
            if (level.isBossLevel)
            {
                var world = db.GetWorld(level.worldNumber);
                if (world != null && economy.TryGrantOnce("world_" + world.worldNumber, world.completionCoins, world.completionGems))
                    rewards.lines.Add(new RewardLine(world.worldName + " completed", world.completionCoins, world.completionGems));
            }

            // Unlocks
            int next = result.levelNumber + 1;
            if (next <= GameConstants.TotalLevels && Data.highestUnlockedLevel < next)
            {
                Data.highestUnlockedLevel = next;
                if (GameConstants.IndexInWorld(next) == 1)
                {
                    int newWorld = GameConstants.WorldOfLevel(next);
                    var w = db.GetWorld(newWorld);
                    rewards.unlocks.Add("World " + newWorld + ": " + (w != null ? w.worldName : "") + " unlocked!");
                    var names = new List<string>();
                    foreach (var b in db.balloons) if (b.unlockWorld == newWorld) names.Add(b.displayName);
                    if (names.Count > 0) rewards.unlocks.Add("New balloons in the shop: " + string.Join(", ", names));
                }
                else
                {
                    rewards.unlocks.Add("Level " + next + " unlocked");
                }
            }

            if (result.levelNumber == GameConstants.TotalLevels && firstClear && !Data.campaignCompleted)
            {
                Data.campaignCompleted = true;
                rewards.campaignJustCompleted = true;
                rewards.unlocks.Add("You reached Heaven's Gate!");
            }

            save.SaveNow();
        }

        public void RecordPowerUpCollected()
        {
            Data.stats.powerUpsCollected++;
            save.MarkDirty();
        }
    }
}
