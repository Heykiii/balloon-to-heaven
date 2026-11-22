using System;

namespace BalloonToHeaven.Core
{
    /// <summary>
    /// Lightweight event hub used to decouple gameplay systems from UI and audio.
    /// Gameplay raises events, views subscribe in OnEnable and unsubscribe in OnDisable.
    /// All payloads are value types to avoid per-frame allocations.
    /// </summary>
    public static class GameEvents
    {
        // Meta-game
        public static event Action<int, int> CurrencyChanged;           // coins, gems
        public static event Action<string> Toast;                        // short notification text
        public static event Action<string> AchievementUnlocked;          // achievement title

        // Gameplay
        public static event Action<int, int> HealthChanged;              // current, max
        public static event Action<DamageInfo> BalloonDamaged;
        public static event Action HitBlocked;
        public static event Action<int> LevelCoinsChanged;               // coins collected this run
        public static event Action<float> LevelProgressChanged;          // 0..1
        public static event Action<PowerUpType, float, float> PowerUpUpdated; // type, remaining, duration (remaining <= 0 means expired)
        public static event Action<float> ShieldChanged;                 // remaining seconds (0 = none), negative = charge shield
        public static event Action<float> BossHealthChanged;             // 0..1, negative hides the bar
        public static event Action<string> BossPhaseStarted;
        public static event Action<float> AbilityCooldownChanged;        // 0..1 readiness

        public static void RaiseCurrencyChanged(int coins, int gems) => CurrencyChanged?.Invoke(coins, gems);
        public static void RaiseToast(string text) => Toast?.Invoke(text);
        public static void RaiseAchievementUnlocked(string title) => AchievementUnlocked?.Invoke(title);
        public static void RaiseHealthChanged(int current, int max) => HealthChanged?.Invoke(current, max);
        public static void RaiseBalloonDamaged(DamageInfo info) => BalloonDamaged?.Invoke(info);
        public static void RaiseHitBlocked() => HitBlocked?.Invoke();
        public static void RaiseLevelCoinsChanged(int coins) => LevelCoinsChanged?.Invoke(coins);
        public static void RaiseLevelProgressChanged(float progress) => LevelProgressChanged?.Invoke(progress);
        public static void RaisePowerUpUpdated(PowerUpType type, float remaining, float duration) => PowerUpUpdated?.Invoke(type, remaining, duration);
        public static void RaiseShieldChanged(float remaining) => ShieldChanged?.Invoke(remaining);
        public static void RaiseBossHealthChanged(float normalized) => BossHealthChanged?.Invoke(normalized);
        public static void RaiseBossPhaseStarted(string phaseName) => BossPhaseStarted?.Invoke(phaseName);
        public static void RaiseAbilityCooldownChanged(float readiness) => AbilityCooldownChanged?.Invoke(readiness);
    }
}
