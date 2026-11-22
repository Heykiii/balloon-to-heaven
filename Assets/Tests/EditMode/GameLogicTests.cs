using System.Collections.Generic;
using BalloonToHeaven.Balloons;
using BalloonToHeaven.Content;
using BalloonToHeaven.Core;
using BalloonToHeaven.Economy;
using BalloonToHeaven.Levels;
using BalloonToHeaven.Obstacles;
using BalloonToHeaven.Progression;
using BalloonToHeaven.Saving;
using BalloonToHeaven.Upgrades;
using NUnit.Framework;
using UnityEngine;

namespace BalloonToHeaven.Tests
{
    /// <summary>Builds an isolated set of services on in-memory storage for each test.</summary>
    public sealed class TestWorld
    {
        public readonly GameDatabase Db;
        public readonly MemorySaveStorage Storage = new MemorySaveStorage();
        public readonly SaveManager Save;
        public readonly EconomyManager Economy;
        public readonly ProgressionManager Progression;
        public readonly BalloonShopManager Shop;
        public readonly UpgradeManager Upgrades;
        public readonly AchievementManager Achievements;

        static GameDatabase sharedDb;

        public TestWorld()
        {
            if (sharedDb == null) sharedDb = ContentFactory.CreateDatabase();
            Db = sharedDb;
            Save = new SaveManager(Storage, Db);
            Save.Load();
            Economy = new EconomyManager(Save, Db.Config.rewards);
            Progression = new ProgressionManager(Save, Economy, Db);
            Shop = new BalloonShopManager(Save, Economy, Db);
            Upgrades = new UpgradeManager(Save, Economy, Db.Workshop);
            Achievements = new AchievementManager(Save, Economy, Db);
        }

        public LevelResult Win(int level, int coins = 0, bool tookDamage = true, bool boss = false)
        {
            var data = Db.GetLevel(level);
            var r = new LevelResult
            {
                levelNumber = level,
                victory = true,
                coinsCollected = coins,
                tookDamage = tookDamage,
                bossDefeated = boss || data.isBossLevel,
                specialCoinsTotal = data.specialCoinCount,
                balloonId = GameConstants.DefaultBalloonId,
            };
            Progression.RecordResult(r);
            return r;
        }
    }

    public class ContentTests
    {
        [Test]
        public void Database_IsStructurallyValid()
        {
            var db = new TestWorld().Db;
            Assert.IsTrue(db.IsValid(out var error), error);
            Assert.AreEqual(20, db.balloons.Count);
            Assert.AreEqual(10, db.worlds.Count);
            Assert.AreEqual(60, db.levels.Count);
            Assert.AreEqual(10, db.bosses.Count);
            Assert.AreEqual(5, db.powerUps.Count);
        }

        [Test]
        public void Balloons_HaveUniqueIds_AndValidUpgradeCosts()
        {
            var db = new TestWorld().Db;
            var ids = new HashSet<string>();
            foreach (var b in db.balloons)
            {
                Assert.IsTrue(ids.Add(b.id), "Duplicate balloon id " + b.id);
                Assert.GreaterOrEqual(b.baseHealth, 1, b.id);
                Assert.GreaterOrEqual(b.price, 0, b.id);
                Assert.IsTrue(b.unlockWorld >= 1 && b.unlockWorld <= 10, b.id);
                Assert.AreEqual(4, b.stageUpgradeCosts.Length, b.id);
                for (int s = 1; s < 4; s++) Assert.Greater(b.stageUpgradeCosts[s], b.stageUpgradeCosts[s - 1], b.id + " stage costs must increase");
            }
            Assert.IsNotNull(db.GetBalloon(GameConstants.DefaultBalloonId));
            Assert.AreEqual(0, db.GetBalloon(GameConstants.DefaultBalloonId).price);
        }

        [Test]
        public void EveryWorld_HasSixLevels_EndingWithItsBoss()
        {
            var db = new TestWorld().Db;
            for (int w = 1; w <= 10; w++)
            {
                for (int i = 1; i <= 6; i++)
                {
                    var level = db.GetLevel((w - 1) * 6 + i);
                    Assert.IsNotNull(level);
                    Assert.AreEqual(w, level.worldNumber);
                    Assert.AreEqual(i, level.indexInWorld);
                    Assert.AreEqual(i == 6, level.isBossLevel, "Level " + level.levelNumber);
                    if (i == 6) Assert.IsNotNull(level.boss);
                    Assert.Greater(level.LevelLength, 10f);
                    foreach (var o in level.obstacles) Assert.IsNotNull(o.obstacle, "Null obstacle in level " + level.levelNumber);
                }
            }
        }

        [Test]
        public void Levels_SecondStarCoinTarget_IsReachable()
        {
            var db = new TestWorld().Db;
            foreach (var level in db.levels)
            {
                float patterns = level.ascentDuration / Mathf.Max(0.5f, level.coinPatternInterval);
                float maxCoins = patterns * level.coinsPerPattern;
                Assert.LessOrEqual(level.coinsForSecondStar, maxCoins, "Level " + level.levelNumber + " coin target exceeds available coins");
            }
        }

        [Test]
        public void Bosses_HavePhasesWithAttacks()
        {
            var db = new TestWorld().Db;
            foreach (var boss in db.bosses)
            {
                Assert.GreaterOrEqual(boss.phases.Count, 3, boss.id);
                foreach (var phase in boss.phases) Assert.Greater(phase.attacks.Count, 0, boss.id + "/" + phase.name);
            }
        }
    }

    public class SaveTests
    {
        [Test]
        public void SaveData_RoundTrips()
        {
            var w = new TestWorld();
            w.Save.Data.coins = 1234;
            w.Save.Data.levelStars[3] = 2;
            w.Save.SaveNow();

            var other = new SaveManager(w.Storage, w.Db);
            other.Load();
            Assert.AreEqual(1234, other.Data.coins);
            Assert.AreEqual(2, other.Data.levelStars[3]);
            Assert.IsFalse(other.RecoveredFromCorruption);
        }

        [Test]
        public void CorruptedMainSave_FallsBackToBackup()
        {
            var w = new TestWorld();
            w.Save.Data.coins = 100;
            w.Save.SaveNow();
            w.Save.Data.coins = 200;
            w.Save.SaveNow(); // previous good save becomes the backup
            w.Storage.Write(SaveManager.MainSlot, w.Storage.Read(SaveManager.MainSlot).Replace("200", "999"));

            var other = new SaveManager(w.Storage, w.Db);
            other.Load();
            Assert.IsTrue(other.RecoveredFromCorruption);
            Assert.IsTrue(other.LoadedFromBackup);
            Assert.AreEqual(100, other.Data.coins);
        }

        [Test]
        public void GarbageSave_StartsFresh()
        {
            var storage = new MemorySaveStorage();
            storage.Write(SaveManager.MainSlot, "{not json");
            var save = new SaveManager(storage, new TestWorld().Db);
            save.Load();
            Assert.AreEqual(1, save.Data.highestUnlockedLevel);
            Assert.Contains(GameConstants.DefaultBalloonId, save.Data.ownedBalloons);
        }

        [Test]
        public void Validate_RepairsInvalidValues()
        {
            var db = new TestWorld().Db;
            var d = SaveData.CreateNew();
            d.coins = -50;
            d.gems = -1;
            d.levelStars = new int[3] { 9, -2, 1 };
            d.highestUnlockedLevel = 500;
            d.equippedBalloon = "does_not_exist";
            d.ownedBalloons.Clear();
            SaveManager.Validate(d, db);
            Assert.AreEqual(0, d.coins);
            Assert.AreEqual(0, d.gems);
            Assert.AreEqual(GameConstants.TotalLevels, d.levelStars.Length);
            foreach (var s in d.levelStars) Assert.IsTrue(s >= 0 && s <= 3);
            Assert.LessOrEqual(d.highestUnlockedLevel, GameConstants.TotalLevels);
            Assert.Contains(GameConstants.DefaultBalloonId, d.ownedBalloons);
            Assert.IsTrue(d.ownedBalloons.Contains(d.equippedBalloon));
        }
    }

    public class EconomyTests
    {
        [Test]
        public void Spending_NeverGoesNegative()
        {
            var w = new TestWorld();
            w.Save.Data.coins = 10;
            Assert.IsFalse(w.Economy.TrySpendCoins(11));
            Assert.AreEqual(10, w.Economy.Coins);
            Assert.IsFalse(w.Economy.TrySpendCoins(-5));
            Assert.IsTrue(w.Economy.TrySpendCoins(10));
            Assert.AreEqual(0, w.Economy.Coins);
            Assert.IsFalse(w.Economy.TrySpendGems(1));
        }

        [Test]
        public void OneTimeRewards_AreGrantedOnce()
        {
            var w = new TestWorld();
            int before = w.Economy.Gems;
            Assert.IsTrue(w.Economy.TryGrantOnce("test_reward", 0, 5));
            Assert.IsFalse(w.Economy.TryGrantOnce("test_reward", 0, 5));
            Assert.AreEqual(before + 5, w.Economy.Gems);
        }
    }

    public class ProgressionTests
    {
        [Test]
        public void Victory_UnlocksNextLevel_FailureDoesNot()
        {
            var w = new TestWorld();
            Assert.IsTrue(w.Progression.IsLevelUnlocked(1));
            Assert.IsFalse(w.Progression.IsLevelUnlocked(2));

            w.Progression.RecordResult(new LevelResult { levelNumber = 1, victory = false, coinsCollected = 5 });
            Assert.IsFalse(w.Progression.IsLevelUnlocked(2));

            w.Win(1, 10);
            Assert.IsTrue(w.Progression.IsLevelUnlocked(2));
            Assert.IsTrue(w.Progression.IsLevelCompleted(1));
        }

        [Test]
        public void Stars_FollowTheThreeObjectives()
        {
            var w = new TestWorld();
            var level = w.Db.GetLevel(1);
            var r = new LevelResult { levelNumber = 1, victory = true, coinsCollected = 0, tookDamage = true };
            Assert.AreEqual(1, StarCalculator.Calculate(level, r, r.starFlags));
            r.coinsCollected = level.coinsForSecondStar;
            Assert.GreaterOrEqual(StarCalculator.Calculate(level, r, r.starFlags), 2);
            r.victory = false;
            Assert.AreEqual(0, StarCalculator.Calculate(level, r, r.starFlags));
        }

        [Test]
        public void ReplayingLevel_DoesNotDuplicateOneTimeRewards()
        {
            var w = new TestWorld();
            for (int l = 1; l <= 6; l++) w.Win(l, 500, false);
            int gemsAfterFirst = w.Economy.Gems;
            w.Win(6, 500, false);
            Assert.AreEqual(gemsAfterFirst, w.Economy.Gems, "Boss/world/three-star gems must only be granted once");
        }

        [Test]
        public void BeatingBoss_UnlocksNextWorld()
        {
            var w = new TestWorld();
            for (int l = 1; l <= 6; l++) w.Win(l, 0);
            Assert.IsTrue(w.Progression.IsWorldCompleted(1));
            Assert.IsTrue(w.Progression.IsWorldUnlocked(2));
            Assert.IsFalse(w.Progression.IsWorldUnlocked(3));
        }

        [Test]
        public void FinishingLevel60_CompletesCampaign()
        {
            var w = new TestWorld();
            LevelResult last = null;
            for (int l = 1; l <= 60; l++) last = w.Win(l, 0);
            Assert.IsTrue(w.Save.Data.campaignCompleted);
            Assert.IsTrue(last.rewards.campaignJustCompleted);
            var again = w.Win(60, 0);
            Assert.IsFalse(again.rewards.campaignJustCompleted);
        }
    }

    public class ShopTests
    {
        [Test]
        public void Purchase_RequiresCoinsAndProgress()
        {
            var w = new TestWorld();
            var cheap = FirstPurchasable(w, 1);
            Assert.IsNotNull(cheap, "Expected a paid world-1 balloon");
            w.Save.Data.coins = cheap.price - 1;
            Assert.AreEqual(ShopResult.NotEnoughCoins, w.Shop.TryPurchase(cheap.id));
            Assert.IsFalse(w.Shop.IsOwned(cheap.id));

            w.Save.Data.coins = cheap.price;
            Assert.AreEqual(ShopResult.Success, w.Shop.TryPurchase(cheap.id));
            Assert.AreEqual(0, w.Economy.Coins);
            Assert.AreEqual(ShopResult.AlreadyOwned, w.Shop.TryPurchase(cheap.id));

            BalloonData late = null;
            foreach (var b in w.Db.balloons) if (b.unlockWorld >= 5) { late = b; break; }
            w.Save.Data.coins = 10000000;
            Assert.AreEqual(ShopResult.LockedByProgress, w.Shop.TryPurchase(late.id));
        }

        [Test]
        public void EquipAndUpgrade_AreValidated()
        {
            var w = new TestWorld();
            var cheap = FirstPurchasable(w, 1);
            Assert.AreEqual(ShopResult.NotOwned, w.Shop.TryEquip(cheap.id));
            Assert.AreEqual(ShopResult.NotOwned, w.Shop.TryUpgrade(cheap.id));

            string id = GameConstants.DefaultBalloonId;
            w.Save.Data.coins = 10000000;
            for (int s = 1; s < GameConstants.MaxBalloonStage; s++) Assert.AreEqual(ShopResult.Success, w.Shop.TryUpgrade(id));
            Assert.AreEqual(GameConstants.MaxBalloonStage, w.Shop.GetStage(id));
            Assert.AreEqual(ShopResult.MaxStage, w.Shop.TryUpgrade(id));
        }

        [Test]
        public void HigherStages_DoNotReduceStats()
        {
            var w = new TestWorld();
            foreach (var b in w.Db.balloons)
            {
                var s1 = w.Shop.GetStats(b, 1);
                var s5 = w.Shop.GetStats(b, 5);
                Assert.GreaterOrEqual(s5.maxHealth, s1.maxHealth, b.id);
                Assert.GreaterOrEqual(s5.armor, s1.armor, b.id);
            }
        }

        static BalloonData FirstPurchasable(TestWorld w, int world)
        {
            foreach (var b in w.Db.balloons) if (b.price > 0 && b.unlockWorld <= world) return b;
            return null;
        }
    }

    public class WorkshopTests
    {
        [Test]
        public void Upgrades_CostCoins_AndStopAtMax()
        {
            var w = new TestWorld();
            var c = WorkshopCategory.Durability;
            w.Save.Data.coins = 0;
            Assert.AreEqual(UpgradeResult.NotEnoughCurrency, w.Upgrades.TryUpgrade(c));
            w.Save.Data.coins = 10000000;
            for (int i = 0; i < GameConstants.MaxWorkshopLevel; i++) Assert.AreEqual(UpgradeResult.Success, w.Upgrades.TryUpgrade(c));
            Assert.AreEqual(UpgradeResult.MaxLevel, w.Upgrades.TryUpgrade(c));
            Assert.AreEqual(GameConstants.MaxWorkshopLevel, w.Upgrades.GetLevel(c));
        }

        [Test]
        public void Trails_RequireGems()
        {
            var w = new TestWorld();
            var trail = w.Db.trails[0];
            w.Save.Data.gems = 0;
            Assert.AreEqual(UpgradeResult.NotEnoughCurrency, w.Upgrades.TryBuyTrail(trail));
            Assert.AreEqual(UpgradeResult.NotOwned, w.Upgrades.TryEquipTrail(trail.id));
            w.Save.Data.gems = trail.gemPrice;
            Assert.AreEqual(UpgradeResult.Success, w.Upgrades.TryBuyTrail(trail));
            Assert.AreEqual(0, w.Economy.Gems);
            Assert.AreEqual(trail.id, w.Upgrades.EquippedTrail);
        }
    }

    public class FairnessTests
    {
        /// <summary>
        /// Simulates thousands of random placements: the two-lane safe corridor must never be
        /// blocked and the minimum number of free lanes must always hold.
        /// </summary>
        [Test]
        public void LanePlanner_AlwaysLeavesSafeCorridor()
        {
            var planner = new LanePlanner(7, 5.4f, new System.Random(12345));
            var rng = new System.Random(99);
            const int minFree = 3;
            for (int step = 0; step < 5000; step++)
            {
                float time = step * 0.25f;
                if (step % 2 == 0) planner.AdvanceCorridor(time);
                Assert.IsTrue(planner.IsFree(planner.Corridor, time) && planner.IsFree(planner.Corridor + 1, time), "Corridor blocked at step " + step);
                int count = 1 + rng.Next(3);
                if (planner.TryFindPlacement(count, time, minFree, out int start))
                {
                    for (int i = start; i < start + count; i++) Assert.IsFalse(planner.IsCorridorLane(i), "Placement blocked the safe corridor");
                    planner.Block(start, count, time + 1f + (float)rng.NextDouble() * 2f);
                }
                Assert.GreaterOrEqual(planner.FreeCount(time), minFree, "Too few free lanes at step " + step);
                Assert.IsTrue(planner.Corridor >= 0 && planner.Corridor + 1 < planner.LaneCount);
            }
        }

        [Test]
        public void LanePlanner_FullRowAlwaysKeepsGap()
        {
            var planner = new LanePlanner(7, 5.4f, new System.Random(7));
            Assert.IsTrue(planner.TryReserveFullRow(0f, 2f));
            Assert.IsTrue(planner.IsFree(planner.Corridor, 1f));
            Assert.IsTrue(planner.IsFree(planner.Corridor + 1, 1f));
            Assert.IsFalse(planner.TryReserveFullRow(1f, 3f), "Rows must not overlap");
        }
    }
}
