using System;
using System.IO;
using BalloonToHeaven.Content;
using BalloonToHeaven.Core;
using UnityEngine;

namespace BalloonToHeaven.Saving
{
    /// <summary>Abstracts where the save text lives (file on device, PlayerPrefs on WebGL, memory in tests).</summary>
    public interface ISaveStorage
    {
        string Read(string slot);
        void Write(string slot, string content);
        void Delete(string slot);
    }

    /// <summary>
    /// Writes to persistentDataPath using a temp file + swap so a crash during writing can never
    /// corrupt the previous save. The previous good save is kept as a backup slot.
    /// </summary>
    public sealed class FileSaveStorage : ISaveStorage
    {
        readonly string directory;

        public FileSaveStorage(string directory) { this.directory = directory; }

        string PathFor(string slot) => Path.Combine(directory, slot + ".json");

        public string Read(string slot)
        {
            try
            {
                string path = PathFor(slot);
                return File.Exists(path) ? File.ReadAllText(path) : null;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Save] Read failed: " + e.Message);
                return null;
            }
        }

        public void Write(string slot, string content)
        {
            try
            {
                Directory.CreateDirectory(directory);
                string path = PathFor(slot);
                string temp = path + ".tmp";
                File.WriteAllText(temp, content);
                if (File.Exists(path)) File.Delete(path);
                File.Move(temp, path);
            }
            catch (Exception e)
            {
                Debug.LogError("[Save] Write failed: " + e.Message);
            }
        }

        public void Delete(string slot)
        {
            try
            {
                string path = PathFor(slot);
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Save] Delete failed: " + e.Message);
            }
        }
    }

    /// <summary>WebGL storage: PlayerPrefs is backed by IndexedDB and flushed with Save().</summary>
    public sealed class PlayerPrefsSaveStorage : ISaveStorage
    {
        const string Prefix = "bth_save_";
        public string Read(string slot) => PlayerPrefs.HasKey(Prefix + slot) ? PlayerPrefs.GetString(Prefix + slot) : null;

        public void Write(string slot, string content)
        {
            PlayerPrefs.SetString(Prefix + slot, content);
            PlayerPrefs.Save();
        }

        public void Delete(string slot)
        {
            PlayerPrefs.DeleteKey(Prefix + slot);
            PlayerPrefs.Save();
        }
    }

    public sealed class MemorySaveStorage : ISaveStorage
    {
        readonly System.Collections.Generic.Dictionary<string, string> slots = new System.Collections.Generic.Dictionary<string, string>();
        public string Read(string slot) => slots.TryGetValue(slot, out var v) ? v : null;
        public void Write(string slot, string content) => slots[slot] = content;
        public void Delete(string slot) => slots.Remove(slot);
    }

    public static class SaveStorageFactory
    {
        public static ISaveStorage Create()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return new PlayerPrefsSaveStorage();
#else
            return new FileSaveStorage(Application.persistentDataPath);
#endif
        }
    }

    /// <summary>Envelope written to disk: payload plus a checksum used to detect corruption.</summary>
    [Serializable]
    class SaveEnvelope
    {
        public int version;
        public string checksum;
        public string payload;
    }

    /// <summary>Loads, validates and writes the player's progress.</summary>
    public sealed class SaveManager
    {
        public const string MainSlot = "save";
        public const string BackupSlot = "save_backup";

        readonly ISaveStorage storage;
        readonly GameDatabase database;
        bool dirty;

        public SaveData Data { get; private set; }
        public bool LoadedFromBackup { get; private set; }
        public bool RecoveredFromCorruption { get; private set; }

        public SaveManager(ISaveStorage storage, GameDatabase database)
        {
            this.storage = storage;
            this.database = database;
            Data = SaveData.CreateNew();
        }

        public void Load()
        {
            LoadedFromBackup = false;
            RecoveredFromCorruption = false;
            var data = TryParse(storage.Read(MainSlot));
            if (data == null)
            {
                string raw = storage.Read(MainSlot);
                if (!string.IsNullOrEmpty(raw)) RecoveredFromCorruption = true;
                data = TryParse(storage.Read(BackupSlot));
                if (data != null) LoadedFromBackup = true;
            }
            Data = data ?? SaveData.CreateNew();
            Validate(Data, database);
            if (RecoveredFromCorruption) Debug.LogWarning("[Save] Main save was corrupted. Restored " + (LoadedFromBackup ? "backup." : "a fresh save."));
        }

        static string Checksum(string text)
        {
            // FNV-1a 32 bit. Detects truncation and accidental corruption (not tampering).
            unchecked
            {
                uint hash = 2166136261;
                for (int i = 0; i < text.Length; i++)
                {
                    hash ^= text[i];
                    hash *= 16777619;
                }
                return hash.ToString("x8");
            }
        }

        public static string Serialize(SaveData data)
        {
            string payload = JsonUtility.ToJson(data);
            return JsonUtility.ToJson(new SaveEnvelope { version = data.version, checksum = Checksum(payload), payload = payload });
        }

        public static SaveData TryParse(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return null;
            try
            {
                var envelope = JsonUtility.FromJson<SaveEnvelope>(raw);
                if (envelope == null || string.IsNullOrEmpty(envelope.payload)) return null;
                if (Checksum(envelope.payload) != envelope.checksum) return null;
                var data = JsonUtility.FromJson<SaveData>(envelope.payload);
                if (data == null) return null;
                return Migrate(data, envelope.version);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Save] Parse failed: " + e.Message);
                return null;
            }
        }

        static SaveData Migrate(SaveData data, int fromVersion)
        {
            // Version 1 is the first released format. Future migrations go here, e.g.
            // if (fromVersion < 2) { ... }
            data.version = SaveData.CurrentVersion;
            return data;
        }

        /// <summary>Repairs any out-of-range or inconsistent values so the game can never load a broken state.</summary>
        public static void Validate(SaveData d, GameDatabase db)
        {
            int total = GameConstants.TotalLevels;
            d.coins = Mathf.Max(0, d.coins);
            d.gems = Mathf.Max(0, d.gems);
            d.levelStars = Resize(d.levelStars, total);
            d.bestCoins = Resize(d.bestCoins, total);
            d.levelCompleted = Resize(d.levelCompleted, total);
            d.workshopLevels = Resize(d.workshopLevels, GameConstants.WorkshopCategoryCount);
            for (int i = 0; i < total; i++)
            {
                d.levelStars[i] = Mathf.Clamp(d.levelStars[i], 0, 3);
                d.bestCoins[i] = Mathf.Max(0, d.bestCoins[i]);
                if (d.levelStars[i] > 0) d.levelCompleted[i] = true;
            }
            for (int i = 0; i < d.workshopLevels.Length; i++) d.workshopLevels[i] = Mathf.Clamp(d.workshopLevels[i], 0, GameConstants.MaxWorkshopLevel);

            // Unlock consistency: every completed level unlocks the next one.
            int highest = 1;
            for (int i = 0; i < total; i++) if (d.levelCompleted[i]) highest = Mathf.Max(highest, i + 2);
            d.highestUnlockedLevel = Mathf.Clamp(Mathf.Max(d.highestUnlockedLevel, highest), 1, total);

            if (d.ownedBalloons == null) d.ownedBalloons = new System.Collections.Generic.List<string>();
            if (d.balloonProgress == null) d.balloonProgress = new System.Collections.Generic.List<BalloonProgress>();
            if (d.claimedRewards == null) d.claimedRewards = new System.Collections.Generic.List<string>();
            if (d.achievements == null) d.achievements = new System.Collections.Generic.List<AchievementProgress>();
            if (d.ownedTrails == null) d.ownedTrails = new System.Collections.Generic.List<string>();
            if (d.stats == null) d.stats = new PlayerStats();
            if (d.stats.bossesDefeated == null) d.stats.bossesDefeated = new System.Collections.Generic.List<string>();
            if (d.settings == null) d.settings = new SettingsData();
            d.settings.musicVolume = Mathf.Clamp01(d.settings.musicVolume);
            d.settings.sfxVolume = Mathf.Clamp01(d.settings.sfxVolume);
            d.settings.quality = Mathf.Clamp(d.settings.quality, 0, 2);

            // Remove unknown or duplicate balloons, always own the default one.
            var seen = new System.Collections.Generic.HashSet<string>();
            for (int i = d.ownedBalloons.Count - 1; i >= 0; i--)
            {
                string id = d.ownedBalloons[i];
                bool known = db == null || db.GetBalloon(id) != null;
                if (string.IsNullOrEmpty(id) || !known || !seen.Add(id)) d.ownedBalloons.RemoveAt(i);
            }
            if (!d.ownedBalloons.Contains(GameConstants.DefaultBalloonId)) d.ownedBalloons.Insert(0, GameConstants.DefaultBalloonId);
            if (string.IsNullOrEmpty(d.equippedBalloon) || !d.ownedBalloons.Contains(d.equippedBalloon)) d.equippedBalloon = GameConstants.DefaultBalloonId;

            for (int i = d.balloonProgress.Count - 1; i >= 0; i--)
            {
                var p = d.balloonProgress[i];
                if (p == null || !d.ownedBalloons.Contains(p.id)) { d.balloonProgress.RemoveAt(i); continue; }
                p.stage = Mathf.Clamp(p.stage, 1, GameConstants.MaxBalloonStage);
            }
            foreach (var id in d.ownedBalloons)
            {
                bool has = false;
                foreach (var p in d.balloonProgress) if (p.id == id) { has = true; break; }
                if (!has) d.balloonProgress.Add(new BalloonProgress { id = id, stage = 1 });
            }

            if (db != null)
            {
                for (int i = d.ownedTrails.Count - 1; i >= 0; i--) if (db.GetTrail(d.ownedTrails[i]) == null) d.ownedTrails.RemoveAt(i);
            }
            if (!string.IsNullOrEmpty(d.equippedTrail) && !d.ownedTrails.Contains(d.equippedTrail)) d.equippedTrail = "";
            d.version = SaveData.CurrentVersion;
        }

        static T[] Resize<T>(T[] array, int length)
        {
            if (array != null && array.Length == length) return array;
            var result = new T[length];
            if (array != null) Array.Copy(array, result, Mathf.Min(length, array.Length));
            return result;
        }

        public void MarkDirty() => dirty = true;

        /// <summary>Writes immediately. Used after purchases and level results so progress is never lost.</summary>
        public void SaveNow()
        {
            if (Data == null) return;
            Data.lastSavedUtcTicks = DateTime.UtcNow.Ticks;
            string previous = storage.Read(MainSlot);
            if (TryParse(previous) != null) storage.Write(BackupSlot, previous);
            storage.Write(MainSlot, Serialize(Data));
            dirty = false;
        }

        public void SaveIfDirty()
        {
            if (dirty) SaveNow();
        }

        public void ResetProgress()
        {
            var settings = Data.settings;
            Data = SaveData.CreateNew();
            Data.settings = settings;
            Validate(Data, database);
            SaveNow();
            GameEvents.RaiseCurrencyChanged(Data.coins, Data.gems);
        }
    }
}
