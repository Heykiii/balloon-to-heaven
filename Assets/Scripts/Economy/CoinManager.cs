using System;
using System.Collections.Generic;
using BalloonToHeaven.Core;
using BalloonToHeaven.Levels;
using BalloonToHeaven.Visuals;
using UnityEngine;

namespace BalloonToHeaven.Economy
{
    /// <summary>Pooled coin / star coin / sky crystal pickup.</summary>
    public sealed class CoinPickup : MonoBehaviour
    {
        public enum Kind { Coin, Special, Crystal }
        public Kind kind;
        public Vector3 position;
        public bool magnetized;
        public float pullSpeed;
    }

    /// <summary>
    /// Spawns coin patterns along the safe corridor (rewarding good routes), star coins at fixed
    /// points of the level, and Sky Crystals during boss fights. Handles magnet pull and pickup.
    /// </summary>
    public sealed class CoinManager
    {
        readonly LevelContext ctx;
        readonly Transform root;
        readonly ComponentPool<CoinPickup> coinPool, specialPool, crystalPool;
        readonly List<CoinPickup> active = new List<CoinPickup>(128);
        readonly Queue<Vector2> pendingPattern = new Queue<Vector2>();
        float patternTimer = 1.2f;
        float pendingTimer;
        int specialSpawned;

        public event Action CrystalCollected;
        public bool PatternsEnabled { get; set; } = true;

        public CoinManager(LevelContext context, Transform parent)
        {
            ctx = context;
            root = new GameObject("Coins").transform;
            root.SetParent(parent, false);
            coinPool = new ComponentPool<CoinPickup>(() => Create(CoinPickup.Kind.Coin), root, 24);
            specialPool = new ComponentPool<CoinPickup>(() => Create(CoinPickup.Kind.Special), root, 2);
            crystalPool = new ComponentPool<CoinPickup>(() => Create(CoinPickup.Kind.Crystal), root, 0);
            ctx.SpecialCoinsTotal = ctx.Level.specialCoinCount;
        }

        static CoinPickup Create(CoinPickup.Kind kind)
        {
            GameObject go = kind == CoinPickup.Kind.Crystal ? PickupModels.SkyCrystal(null) : PickupModels.Coin(null, kind == CoinPickup.Kind.Special);
            var c = go.AddComponent<CoinPickup>();
            c.kind = kind;
            return c;
        }

        ComponentPool<CoinPickup> PoolFor(CoinPickup.Kind kind) => kind == CoinPickup.Kind.Coin ? coinPool : kind == CoinPickup.Kind.Special ? specialPool : crystalPool;

        CoinPickup Spawn(CoinPickup.Kind kind, Vector3 position)
        {
            var c = PoolFor(kind).Get();
            c.position = position;
            c.magnetized = false;
            c.pullSpeed = 0f;
            c.transform.position = position;
            active.Add(c);
            return c;
        }

        public void SpawnCrystal(float x) => Spawn(CoinPickup.Kind.Crystal, new Vector3(x, ctx.SpawnY, -0.2f));

        void QueuePattern()
        {
            var lanes = ctx.Lanes;
            int count = Mathf.Max(3, ctx.Level.coinsPerPattern);
            float baseX = lanes.CorridorCenter + ((float)ctx.Rng.NextDouble() - 0.5f) * lanes.LaneWidth * 0.8f;
            int type = ctx.Rng.Next(4);
            float spacing = 0.85f;
            for (int i = 0; i < count; i++)
            {
                float x = baseX;
                switch (type)
                {
                    case 1: x = baseX + Mathf.Sin(i * 0.9f) * 1.2f; break;                  // wave
                    case 2: x = baseX + (i - count * 0.5f) * 0.45f; break;                  // diagonal
                    case 3: x = baseX + ((i % 2 == 0) ? -0.5f : 0.5f); break;               // zigzag
                }
                x = ctx.Field.ClampX(x, 0.3f, 0.3f);
                pendingPattern.Enqueue(new Vector2(x, spacing));
            }
        }

        public void Tick(float dt)
        {
            if (dt <= 0f) return;
            float progress = ctx.Progress;

            if (PatternsEnabled && progress < 0.98f)
            {
                patternTimer -= dt;
                if (patternTimer <= 0f && pendingPattern.Count == 0)
                {
                    QueuePattern();
                    patternTimer = ctx.Level.coinPatternInterval;
                }
                // Special coins at evenly spaced points of the ascent
                if (specialSpawned < ctx.SpecialCoinsTotal && progress >= (specialSpawned + 0.5f) / ctx.SpecialCoinsTotal * 0.92f)
                {
                    float x = ctx.Lanes.CorridorCenter;
                    Spawn(CoinPickup.Kind.Special, new Vector3(x, ctx.SpawnY + 0.5f, -0.2f));
                    specialSpawned++;
                }
            }

            // Release queued pattern coins one row at a time (spacing in world units)
            if (pendingPattern.Count > 0)
            {
                pendingTimer -= dt;
                if (pendingTimer <= 0f)
                {
                    var next = pendingPattern.Dequeue();
                    Spawn(CoinPickup.Kind.Coin, new Vector3(next.x, ctx.SpawnY, -0.2f));
                    pendingTimer = next.y / Mathf.Max(0.5f, ctx.ScrollSpeed);
                }
            }

            float magnet = Mathf.Max(ctx.Abilities != null ? ctx.Abilities.Stats.magnetRadius : ctx.Config.baseMagnetRadius, ctx.PowerUps.MagnetRadius);
            var center = ctx.BalloonCenter;
            float collectRadius = ctx.Config.balloonCollisionRadius + 0.35f;
            float magnetSq = magnet * magnet;
            float collectSq = collectRadius * collectRadius;
            bool canCollect = ctx.Health != null && !ctx.Health.IsDead;

            for (int i = active.Count - 1; i >= 0; i--)
            {
                var c = active[i];
                var p = c.position;
                p.y -= ctx.ScrollSpeed * dt;
                float dx = center.x - p.x, dy = center.y - p.y;
                float distSq = dx * dx + dy * dy;
                if (canCollect && (c.magnetized || distSq < magnetSq))
                {
                    c.magnetized = true;
                    c.pullSpeed = Mathf.Min(ctx.Config.magnetPullSpeed * 1.6f, c.pullSpeed + ctx.Config.magnetPullSpeed * 3f * dt + 2f * dt);
                    float dist = Mathf.Sqrt(distSq);
                    if (dist > 0.001f)
                    {
                        float step = Mathf.Min(dist, c.pullSpeed * dt);
                        p.x += dx / dist * step;
                        p.y += dy / dist * step;
                    }
                    dx = center.x - p.x; dy = center.y - p.y; distSq = dx * dx + dy * dy;
                }
                c.position = p;
                c.transform.position = p;

                if (canCollect && distSq <= collectSq)
                {
                    Collect(c);
                    Release(i);
                    continue;
                }
                if (p.y < ctx.DespawnY) Release(i);
            }
        }

        void Release(int index)
        {
            var c = active[index];
            active.RemoveAt(index);
            PoolFor(c.kind).Release(c);
        }

        void Collect(CoinPickup c)
        {
            var rewards = ctx.Database.Config.rewards;
            switch (c.kind)
            {
                case CoinPickup.Kind.Crystal:
                    ctx.Audio?.PlaySfx(SfxId.Crystal);
                    ParticleFactory.Burst(c.position + Vector3.back, new Color(0.5f, 0.95f, 1f), 24, 5f, 0.35f);
                    CrystalCollected?.Invoke();
                    return;
                case CoinPickup.Kind.Special:
                    ctx.SpecialCoinsCollected++;
                    AddCoins(rewards.specialCoinValue);
                    ctx.Audio?.PlaySfx(SfxId.SpecialCoin);
                    ParticleFactory.Burst(c.position + Vector3.back, new Color(1f, 0.9f, 0.4f), 26, 5f, 0.4f);
                    GameEvents.RaiseToast("Star coin " + ctx.SpecialCoinsCollected + "/" + ctx.SpecialCoinsTotal);
                    return;
                default:
                    AddCoins(rewards.coinPickup);
                    ctx.Audio?.PlaySfx(SfxId.Coin, 0.55f);
                    ParticleFactory.Burst(c.position + Vector3.back, PickupModels.Gold, 6, 2.5f, 0.18f, 0.4f);
                    return;
            }
        }

        void AddCoins(int baseValue)
        {
            int value = ctx.Abilities != null ? ctx.Abilities.ModifyCoinValue(baseValue) : baseValue;
            value *= ctx.PowerUps.CoinMultiplier;
            ctx.CoinsCollected += value;
            GameEvents.RaiseLevelCoinsChanged(ctx.CoinsCollected);
        }

        public void ClearAll()
        {
            for (int i = active.Count - 1; i >= 0; i--) Release(i);
            pendingPattern.Clear();
        }
    }
}
