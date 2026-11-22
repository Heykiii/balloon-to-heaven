using BalloonToHeaven.Content;
using BalloonToHeaven.Core;
using BalloonToHeaven.Economy;
using BalloonToHeaven.Saving;
using UnityEngine;

namespace BalloonToHeaven.Progression
{
    /// <summary>Evaluates achievement conditions from saved statistics and pays gem rewards once.</summary>
    public sealed class AchievementManager
    {
        readonly SaveManager save;
        readonly EconomyManager economy;
        readonly GameDatabase db;

        public AchievementManager(SaveManager save, EconomyManager economy, GameDatabase db)
        {
            this.save = save;
            this.economy = economy;
            this.db = db;
        }

        public long GetValue(AchievementStat stat)
        {
            var d = save.Data;
            switch (stat)
            {
                case AchievementStat.LevelsCompleted:
                    int c = 0; foreach (var done in d.levelCompleted) if (done) c++; return c;
                case AchievementStat.TotalCoinsCollected: return d.stats.totalCoinsCollected;
                case AchievementStat.BalloonsOwned: return d.ownedBalloons.Count;
                case AchievementStat.BossesDefeated: return d.stats.bossesDefeated.Count;
                case AchievementStat.NoDamageClears: return d.stats.noDamageClears;
                case AchievementStat.TotalStars: return d.TotalStars();
                case AchievementStat.MaxBalloonStage:
                    int max = 1; foreach (var p in d.balloonProgress) max = Mathf.Max(max, p.stage); return max;
                case AchievementStat.WorkshopLevels:
                    int t = 0; foreach (var l in d.workshopLevels) t += l; return t;
                case AchievementStat.PowerUpsCollected: return d.stats.powerUpsCollected;
                case AchievementStat.WorldsCompleted:
                    int w = 0;
                    for (int i = 1; i <= GameConstants.WorldCount; i++) if (d.levelCompleted[i * GameConstants.LevelsPerWorld - 1]) w++;
                    return w;
                case AchievementStat.CampaignCompleted: return d.campaignCompleted ? 1 : 0;
            }
            return 0;
        }

        public bool IsUnlocked(string id)
        {
            foreach (var a in save.Data.achievements) if (a.id == id) return a.unlocked;
            return false;
        }

        public int UnlockedCount
        {
            get { int n = 0; foreach (var a in save.Data.achievements) if (a.unlocked) n++; return n; }
        }

        /// <summary>Checks every achievement; newly completed ones are unlocked and rewarded.</summary>
        public void Evaluate()
        {
            if (db == null || db.achievements == null) return;
            foreach (var def in db.achievements)
            {
                if (IsUnlocked(def.id)) continue;
                if (GetValue(def.stat) < def.target) continue;
                save.Data.achievements.Add(new AchievementProgress { id = def.id, unlocked = true });
                economy.TryGrantOnce("achievement_" + def.id, 0, def.gemReward);
                GameEvents.RaiseAchievementUnlocked(def.title);
            }
            save.SaveIfDirty();
        }
    }
}
