using System.Collections.Generic;
using BalloonToHeaven.Core;
using BalloonToHeaven.Levels;
using BalloonToHeaven.Visuals;
using UnityEngine;

namespace BalloonToHeaven.PowerUps
{
    /// <summary>Tracks active timed power-ups and applies their effects through queries.</summary>
    public sealed class PowerUpManager
    {
        readonly LevelContext ctx;
        readonly float[] remaining = new float[5];
        readonly float[] durations = new float[5];
        readonly PowerUpData[] data = new PowerUpData[5];

        public PowerUpManager(LevelContext context)
        {
            ctx = context;
            foreach (var p in ctx.Database.powerUps) if (p != null) data[(int)p.type] = p;
        }

        public bool IsActive(PowerUpType type) => remaining[(int)type] > 0f;
        public float Remaining(PowerUpType type) => remaining[(int)type];
        public float MagnetRadius => IsActive(PowerUpType.CoinMagnet) ? Magnitude(PowerUpType.CoinMagnet, 4.5f) : 0f;
        public float ScrollMultiplier => IsActive(PowerUpType.SkyRocket) ? Magnitude(PowerUpType.SkyRocket, 1.8f) : 1f;
        public int CoinMultiplier => IsActive(PowerUpType.DoubleCoins) ? Mathf.Max(1, Mathf.RoundToInt(Magnitude(PowerUpType.DoubleCoins, 2f))) : 1;
        /// <summary>No new hazards spawn during the Sky Rocket boost (the scroll is faster).</summary>
        public bool SpawnPaused => IsActive(PowerUpType.SkyRocket);

        float Magnitude(PowerUpType type, float fallback) => data[(int)type] != null ? data[(int)type].magnitude : fallback;

        public void Activate(PowerUpType type)
        {
            var d = data[(int)type];
            float baseDuration = d != null ? d.duration : 0f;
            ctx.PowerUpsCollected++;
            ctx.Audio?.PlaySfx(SfxId.PowerUp);
            GameEvents.RaiseToast(d != null ? d.displayName + "!" : type.ToString());

            if (type == PowerUpType.ExtraLife)
            {
                ctx.Health.Heal(1);
                return;
            }

            float duration = ctx.Abilities != null ? ctx.Abilities.ModifyPowerUpDuration(type, baseDuration) : baseDuration;
            if (type == PowerUpType.EnergyShield && ctx.Abilities != null) duration += ctx.Abilities.Stats.shieldBonusSeconds;
            remaining[(int)type] = duration;
            durations[(int)type] = duration;
            if (type == PowerUpType.EnergyShield)
            {
                ctx.Health.AddShieldTime(duration);
                ctx.Audio?.PlaySfx(SfxId.Shield);
            }
            if (type == PowerUpType.SkyRocket)
            {
                ctx.Health.SetInvulnerable(duration + 0.5f);
                ctx.Shake?.Shake(0.25f);
            }
            GameEvents.RaisePowerUpUpdated(type, duration, duration);
        }

        public void Tick(float dt)
        {
            for (int i = 0; i < remaining.Length; i++)
            {
                if (remaining[i] <= 0f) continue;
                remaining[i] -= dt;
                if (remaining[i] <= 0f) remaining[i] = 0f;
                GameEvents.RaisePowerUpUpdated((PowerUpType)i, remaining[i], durations[i]);
            }
        }
    }

    public sealed class PowerUpPickup : MonoBehaviour
    {
        public PowerUpType type;
        public Vector3 position;
    }

    /// <summary>Spawns power-up pickups at configured intervals in free lanes.</summary>
    public sealed class PowerUpSpawner
    {
        readonly LevelContext ctx;
        readonly Transform root;
        readonly Dictionary<PowerUpType, ComponentPool<PowerUpPickup>> pools = new Dictionary<PowerUpType, ComponentPool<PowerUpPickup>>();
        readonly List<PowerUpPickup> active = new List<PowerUpPickup>();
        float timer;

        public bool Enabled { get; set; } = true;

        public PowerUpSpawner(LevelContext context, Transform parent)
        {
            ctx = context;
            root = new GameObject("PowerUps").transform;
            root.SetParent(parent, false);
            timer = ctx.Level.powerUpInterval * 0.6f;
        }

        ComponentPool<PowerUpPickup> Pool(PowerUpType type)
        {
            if (pools.TryGetValue(type, out var pool)) return pool;
            var d = ctx.Database.GetPowerUp(type);
            Color color = d != null ? d.color : Color.white;
            pool = new ComponentPool<PowerUpPickup>(() =>
            {
                var go = PickupModels.PowerUp(null, type, color);
                var p = go.AddComponent<PowerUpPickup>();
                p.type = type;
                return p;
            }, root);
            pools[type] = pool;
            return pool;
        }

        PowerUpType? Pick()
        {
            float total = 0f;
            foreach (var e in ctx.Level.powerUps) total += Mathf.Max(0f, e.weight);
            if (total <= 0f) return null;
            float roll = (float)ctx.Rng.NextDouble() * total;
            foreach (var e in ctx.Level.powerUps)
            {
                roll -= Mathf.Max(0f, e.weight);
                if (roll <= 0f)
                {
                    // Avoid wasting an Extra Life at full health
                    if (e.type == PowerUpType.ExtraLife && ctx.Health.Current >= ctx.Health.Max) return PowerUpType.CoinMagnet;
                    return e.type;
                }
            }
            return ctx.Level.powerUps[0].type;
        }

        public void SpawnAt(PowerUpType type, float x)
        {
            var p = Pool(type).Get();
            p.position = new Vector3(x, ctx.SpawnY, -0.3f);
            p.transform.position = p.position;
            active.Add(p);
        }

        public void Tick(float dt)
        {
            if (dt <= 0f) return;
            if (Enabled && ctx.Progress < 0.92f)
            {
                timer -= dt;
                if (timer <= 0f)
                {
                    timer = ctx.Level.powerUpInterval * Mathf.Lerp(0.85f, 1.2f, (float)ctx.Rng.NextDouble());
                    var type = Pick();
                    if (type.HasValue)
                    {
                        float x = ctx.Lanes.CorridorCenter + ((float)ctx.Rng.NextDouble() - 0.5f) * ctx.Lanes.LaneWidth;
                        SpawnAt(type.Value, ctx.Field.ClampX(x, 0.6f, 0.2f));
                    }
                }
            }

            var center = ctx.BalloonCenter;
            float r = ctx.Config.balloonCollisionRadius + 0.6f;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var p = active[i];
                p.position.y -= ctx.ScrollSpeed * dt;
                p.transform.position = p.position;
                float dx = center.x - p.position.x, dy = center.y - p.position.y;
                bool collected = !ctx.Health.IsDead && dx * dx + dy * dy <= r * r;
                if (collected)
                {
                    ParticleFactory.Burst(p.position + Vector3.back, ctx.Database.GetPowerUp(p.type)?.color ?? Color.white, 24, 4.5f, 0.35f);
                    ctx.PowerUps.Activate(p.type);
                }
                if (collected || p.position.y < ctx.DespawnY)
                {
                    active.RemoveAt(i);
                    Pool(p.type).Release(p);
                }
            }
        }
    }
}
