using System.Collections.Generic;
using BalloonToHeaven.Core;
using BalloonToHeaven.Levels;
using BalloonToHeaven.Visuals;
using UnityEngine;

namespace BalloonToHeaven.Obstacles
{
    /// <summary>
    /// Spawns, pools, ticks and collides all obstacles. Spawning is driven by the level's
    /// configuration and validated by the <see cref="LanePlanner"/> so that the player always has
    /// a reachable gap. Bosses use the same system to spawn projectiles and summons.
    /// </summary>
    public sealed class ObstacleSpawner
    {
        readonly LevelContext ctx;
        readonly Transform root;
        readonly Dictionary<ObstacleData, ComponentPool<ObstacleController>> pools = new Dictionary<ObstacleData, ComponentPool<ObstacleController>>();
        readonly List<ObstacleController> active = new List<ObstacleController>(64);
        float spawnTimer = 1.5f;
        float lastCrosserTime = -10f;
        float totalWeight;

        public int ActiveCount => active.Count;
        public IReadOnlyList<ObstacleController> Active => active;

        public ObstacleSpawner(LevelContext context, Transform parent)
        {
            ctx = context;
            root = new GameObject("Obstacles").transform;
            root.SetParent(parent, false);
            foreach (var entry in ctx.Level.obstacles)
            {
                if (entry?.obstacle == null) continue;
                totalWeight += Mathf.Max(0f, entry.weight);
                GetPool(entry.obstacle, 2);
            }
        }

        // ------------------------------------------------------------------ pooling

        ComponentPool<ObstacleController> GetPool(ObstacleData data, int prewarm = 0)
        {
            if (pools.TryGetValue(data, out var pool)) return pool;
            pool = new ComponentPool<ObstacleController>(() => CreateInstance(data), root, prewarm);
            pools[data] = pool;
            return pool;
        }

        static ObstacleController CreateInstance(ObstacleData data)
        {
            var view = ObstacleModelBuilder.Build(data, null);
            var go = view.gameObject;
            if (data.timed) go.AddComponent<TimedHazardBehavior>();
            if (data.harmless) go.AddComponent<WindZoneBehavior>();
            switch (data.movement)
            {
                case MovementPattern.CrossHorizontal: go.AddComponent<CrossHorizontalBehavior>(); break;
                case MovementPattern.Patrol: go.AddComponent<PatrolBehavior>(); break;
                case MovementPattern.Falling: go.AddComponent<FallingBehavior>(); break;
                case MovementPattern.Diagonal: go.AddComponent<DiagonalBehavior>(); break;
                case MovementPattern.Circular: go.AddComponent<CircularBehavior>(); break;
                case MovementPattern.Homing: go.AddComponent<HomingBehavior>(); break;
                case MovementPattern.Swarm: go.AddComponent<SwarmBehavior>(); break;
                case MovementPattern.VerticalBob: go.AddComponent<VerticalBobBehavior>(); break;
            }
            var controller = go.AddComponent<ObstacleController>();
            controller.Setup(data, view);
            return controller;
        }

        ObstacleController Take(ObstacleData data)
        {
            var o = GetPool(data).Get();
            o.ResetParameters();
            return o;
        }

        void Activate(ObstacleController o, Vector3 position)
        {
            o.Spawn(position, ctx);
            active.Add(o);
        }

        void Despawn(int index)
        {
            var o = active[index];
            active.RemoveAt(index);
            o.Alive = false;
            if (o.Data != null && pools.TryGetValue(o.Data, out var pool)) pool.Release(o);
            else Object.Destroy(o.gameObject);
        }

        public void ClearAll(bool withBursts)
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                if (withBursts && !active[i].Data.harmless) ParticleFactory.Burst(active[i].Position + Vector3.back, Color.white, 6, 2f, 0.2f);
                Despawn(i);
            }
        }

        // ------------------------------------------------------------------ regular spawning

        ObstacleData PickObstacle(float progress)
        {
            if (totalWeight <= 0f) return null;
            float available = 0f;
            foreach (var e in ctx.Level.obstacles) if (e.obstacle != null && progress >= e.startProgress) available += e.weight;
            if (available <= 0f) return null;
            float roll = (float)ctx.Rng.NextDouble() * available;
            foreach (var e in ctx.Level.obstacles)
            {
                if (e.obstacle == null || progress < e.startProgress) continue;
                roll -= e.weight;
                if (roll <= 0f) return e.obstacle;
            }
            return ctx.Level.obstacles[ctx.Level.obstacles.Count - 1].obstacle;
        }

        int CountHazards()
        {
            int n = 0;
            for (int i = 0; i < active.Count; i++) if (!active[i].Data.harmless) n++;
            return n;
        }

        public void Tick(float dt)
        {
            if (dt <= 0f) return;
            if (ctx.NormalSpawning && totalWeight > 0f && !ctx.PowerUps.SpawnPaused)
            {
                spawnTimer -= dt;
                if (spawnTimer <= 0f)
                {
                    float progress = ctx.Progress;
                    float interval = Mathf.Lerp(ctx.Level.spawnIntervalStart, ctx.Level.spawnIntervalEnd, progress);
                    // Never spawn in the final stretch so the finish is always reachable.
                    if (progress < 0.96f && CountHazards() < ctx.Level.maxActiveObstacles)
                    {
                        var data = PickObstacle(progress);
                        bool spawned = data != null && TrySpawn(data);
                        spawnTimer = spawned ? interval * Mathf.Lerp(0.85f, 1.15f, (float)ctx.Rng.NextDouble()) : 0.25f;
                    }
                    else spawnTimer = 0.3f;
                }
            }
            TickActive(dt);
        }

        float BlockDuration(float height)
        {
            float clearance = ctx.Config.balloonCollisionRadius * 2f + 0.9f;
            return (height + clearance) / Mathf.Max(0.5f, ctx.ScrollSpeed);
        }

        /// <summary>Attempts a fair spawn of the given obstacle type. Returns false if no fair placement exists right now.</summary>
        public bool TrySpawn(ObstacleData data)
        {
            var lanes = ctx.Lanes;
            float now = ctx.ElapsedTime;
            float y = ctx.SpawnY;
            float speedMul = ctx.Level.obstacleSpeedMultiplier;

            if (data.fullRowWithGap)
            {
                if (!lanes.TryReserveFullRow(now, now + BlockDuration(data.boxSize.y) + 0.4f)) return false;
                SpawnFullRow(data, y, lanes.CorridorCenter);
                lanes.AdvanceCorridor(now);
                return true;
            }

            if (data.harmless)
            {
                // Wind zones never block, but keep them away from the very edges.
                var wind = Take(data);
                float x = ((float)ctx.Rng.NextDouble() * 2f - 1f) * (ctx.Field.HalfWidth - data.boxSize.x * 0.5f);
                Activate(wind, new Vector3(x, y + data.boxSize.y * 0.5f, 0.5f));
                return true;
            }

            if (data.movement == MovementPattern.CrossHorizontal)
            {
                if (now - lastCrosserTime < ctx.Config.minCrosserSpacingSeconds || lanes.RowBlockedUntil > now) return false;
                lastCrosserTime = now;
                var bird = Take(data);
                bird.Direction = ctx.Rng.NextDouble() < 0.5 ? 1f : -1f;
                bird.SpeedMultiplier = speedMul;
                float startX = -bird.Direction * (ctx.Field.VisibleHalfWidth + 1f);
                float startY = Mathf.Lerp(1f, ctx.Field.Top - 1.5f, (float)ctx.Rng.NextDouble());
                Activate(bird, new Vector3(startX, startY, 0f));
                lanes.AdvanceCorridor(now);
                return true;
            }

            int count = lanes.LanesFor(data.FootprintWidth);
            int balloonLane = lanes.LaneOf(ctx.Balloon.X);
            bool fastFaller = data.movement == MovementPattern.Falling || data.movement == MovementPattern.Diagonal;
            float impactTime = (y - ctx.BalloonY) / Mathf.Max(0.5f, ctx.ScrollSpeed + (fastFaller ? data.speed * speedMul : 0f));
            int avoid = impactTime < ctx.Config.minSafeReactionTime + 0.4f ? balloonLane : -1; // never drop fast objects onto the balloon
            if (!lanes.TryFindPlacement(count, now, ctx.Level.minFreeLanes, out int start, avoid)) return false;

            float duration = BlockDuration(data.FootprintHeight + (data.groupMax > 1 ? 1.5f : 0f));
            if (data.movement == MovementPattern.Falling || data.movement == MovementPattern.Diagonal || data.movement == MovementPattern.Homing)
                duration *= 0.75f; // these leave the spawn row faster
            lanes.Block(start, count, now + duration);
            lanes.AdvanceCorridor(now);

            float centerX = (lanes.LaneCenter(start) + lanes.LaneCenter(start + count - 1)) * 0.5f;
            int group = data.groupMax > 1 ? ctx.Rng.Next(data.groupMin, data.groupMax + 1) : 1;
            float dir = centerX > 0f ? -1f : 1f;
            for (int i = 0; i < group; i++)
            {
                var o = Take(data);
                o.SpeedMultiplier = speedMul;
                o.Direction = data.movement == MovementPattern.Diagonal ? dir : (ctx.Rng.NextDouble() < 0.5 ? -1f : 1f);
                if (group > 1)
                {
                    o.Direction = dir;
                    float angle = i / (float)group * Mathf.PI * 2f;
                    o.Offset = new Vector3(Mathf.Cos(angle) * 0.75f, Mathf.Sin(angle) * 0.55f, 0f);
                }
                var pos = new Vector3(centerX, y + data.FootprintHeight * 0.5f, 0f);
                if (data.timed) pos.y = y + data.boxSize.y * 0.5f;
                Activate(o, pos);
            }
            return true;
        }

        void SpawnFullRow(ObstacleData data, float y, float gapCenter)
        {
            float gap = Mathf.Max(data.gapWidth, ctx.Config.balloonCollisionRadius * 2f + 1.8f);
            float left = -ctx.Field.VisibleHalfWidth - 0.5f;
            float right = ctx.Field.VisibleHalfWidth + 0.5f;
            float gapL = gapCenter - gap * 0.5f, gapR = gapCenter + gap * 0.5f;
            float h = data.boxSize.y;
            if (gapL - left > 0.2f)
            {
                var wall = Take(data);
                wall.BoxSize = new Vector2(gapL - left, h);
                Activate(wall, new Vector3((left + gapL) * 0.5f, y + h * 0.5f, 0f));
            }
            if (right - gapR > 0.2f)
            {
                var wall = Take(data);
                wall.BoxSize = new Vector2(right - gapR, h);
                Activate(wall, new Vector3((gapR + right) * 0.5f, y + h * 0.5f, 0f));
            }
        }

        // ------------------------------------------------------------------ boss helpers

        /// <summary>Spawns a projectile moving with a fixed screen-space velocity.</summary>
        public ObstacleController SpawnProjectile(ObstacleData data, Vector3 position, Vector3 velocity)
        {
            var o = Take(data);
            o.UseCustomVelocity = true;
            o.CustomVelocity = velocity;
            o.ScrollFactor = 0f;
            Activate(o, position);
            return o;
        }

        /// <summary>Spawns a screen-space telegraphed column that disappears after striking once.</summary>
        public ObstacleController SpawnColumn(ObstacleData data, float x, float telegraph)
        {
            var o = Take(data);
            o.ScrollFactor = 0f;
            o.DespawnAfterActive = true;
            o.WarningOverride = Mathf.Max(0.7f, telegraph);
            Activate(o, new Vector3(x, 0f, 0.3f));
            return o;
        }

        /// <summary>Spawns a wall with a gap aligned with the safe corridor (used by bosses).</summary>
        public bool TrySpawnWall(ObstacleData data)
        {
            var lanes = ctx.Lanes;
            float now = ctx.ElapsedTime;
            if (!lanes.TryReserveFullRow(now, now + BlockDuration(data.boxSize.y) + 0.4f)) return false;
            SpawnFullRow(data, ctx.SpawnY, lanes.CorridorCenter);
            lanes.AdvanceCorridor(now);
            return true;
        }

        // ------------------------------------------------------------------ simulation & collisions

        void TickActive(float dt)
        {
            float wind = 0f;
            var center = ctx.BalloonCenter;
            float radius = ctx.Config.balloonCollisionRadius;
            bool invisible = ctx.Abilities != null && ctx.Abilities.IsInvisible;
            float limitX = ctx.Field.VisibleHalfWidth + 4f;

            for (int i = active.Count - 1; i >= 0; i--)
            {
                var o = active[i];
                o.Tick(ctx, dt);

                if (!o.Alive || o.Top < ctx.DespawnY || Mathf.Abs(o.Position.x) > limitX || o.Age > 60f || o.Position.y > ctx.SpawnY + 12f)
                {
                    Despawn(i);
                    continue;
                }
                if (ctx.Health == null || ctx.Health.IsDead || !o.CollisionEnabled) continue;
                if (!o.Overlaps(center, radius)) continue;

                if (o.Data.harmless)
                {
                    wind += o.Direction * o.Data.pushForce;
                    continue;
                }
                if (invisible) continue;

                var info = new DamageInfo(o.Data.damage, o.Data.damageType, o.Position);
                bool damaged = ctx.Health.TryHit(info);
                if (damaged)
                {
                    ctx.Audio?.PlaySfx(o.Data.hitSound);
                    ctx.Shake?.Shake(0.45f);
                    ParticleFactory.Burst(new Vector3(center.x, center.y, -1f), new Color(1f, 0.4f, 0.35f), 16, 4f, 0.3f);
                }
                if (o.Data.destroyOnHit && (damaged || ctx.Health.HasShield || ctx.Health.IsInvulnerable))
                {
                    ParticleFactory.Burst(o.Position + Vector3.back, o.Data.primaryColor, 14, 3.5f, 0.3f);
                    Despawn(i);
                }
            }
            ctx.Effects?.SetZoneWind(wind);
        }
    }
}
