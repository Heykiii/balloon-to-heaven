using System;
using System.Collections.Generic;
using BalloonToHeaven.Core;

namespace BalloonToHeaven.Saving
{
    [Serializable]
    public class BalloonProgress
    {
        public string id;
        public int stage = 1;
    }

    [Serializable]
    public class AchievementProgress
    {
        public string id;
        public bool unlocked;
    }

    [Serializable]
    public class PlayerStats
    {
        public long totalCoinsCollected;
        public int levelsCompleted;
        public int noDamageClears;
        public int powerUpsCollected;
        public int levelsFailed;
        public List<string> bossesDefeated = new List<string>();
    }

    [Serializable]
    public class SettingsData
    {
        public float musicVolume = 0.7f;
        public float sfxVolume = 0.8f;
        public int quality = 2;
        public bool screenShake = true;
    }

    /// <summary>Everything persisted for the player. Versioned and validated on load.</summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public int coins;
        public int gems;
        public int highestUnlockedLevel = 1;
        public int[] levelStars = new int[GameConstants.TotalLevels];
        public int[] bestCoins = new int[GameConstants.TotalLevels];
        public bool[] levelCompleted = new bool[GameConstants.TotalLevels];
        public List<string> ownedBalloons = new List<string>();
        public string equippedBalloon = GameConstants.DefaultBalloonId;
        public List<BalloonProgress> balloonProgress = new List<BalloonProgress>();
        public int[] workshopLevels = new int[GameConstants.WorkshopCategoryCount];
        public List<string> claimedRewards = new List<string>();
        public List<AchievementProgress> achievements = new List<AchievementProgress>();
        public List<string> ownedTrails = new List<string>();
        public string equippedTrail = "";
        public PlayerStats stats = new PlayerStats();
        public SettingsData settings = new SettingsData();
        public bool campaignCompleted;
        public long lastSavedUtcTicks;

        public static SaveData CreateNew()
        {
            var data = new SaveData();
            data.ownedBalloons.Add(GameConstants.DefaultBalloonId);
            data.balloonProgress.Add(new BalloonProgress { id = GameConstants.DefaultBalloonId, stage = 1 });
            return data;
        }

        public int GetStage(string balloonId)
        {
            foreach (var p in balloonProgress) if (p.id == balloonId) return p.stage;
            return 1;
        }

        public void SetStage(string balloonId, int stage)
        {
            foreach (var p in balloonProgress)
            {
                if (p.id == balloonId) { p.stage = stage; return; }
            }
            balloonProgress.Add(new BalloonProgress { id = balloonId, stage = stage });
        }

        public int TotalStars()
        {
            int total = 0;
            foreach (var s in levelStars) total += s;
            return total;
        }
    }
}
