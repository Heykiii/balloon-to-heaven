using System.Collections.Generic;
using BalloonToHeaven.Core;
using BalloonToHeaven.Levels;
using BalloonToHeaven.Obstacles;
using BalloonToHeaven.Visuals;
using UnityEngine;

namespace BalloonToHeaven.Bosses
{
    /// <summary>
    /// Runs one boss encounter: entrance, multi-phase movement and attack patterns, Sky Crystal
    /// objectives and the defeat sequence. The player wins by surviving; the boss's health drains
    /// over the encounter and every collected crystal deals extra damage. All attacks leave the
    /// lane planner's safe corridor open.
    /// </summary>
    public sealed class BossManager
    {
        enum State { Entering, Fighting, Defeated, Done }

        readonly LevelContext ctx;
        readonly BossData data;
        readonly BossView view;
        readonly float[] attackTimers = new float[8];
        readonly List<(float time, Vector3 target)> pendingShots = new List<(float, Vector3)>();
        State state;
        float health = 1f;
        int phaseIndex;
        float stateTimer;
        float moveTime;
        float crystalTimer;
        float teleportTimer;
        Vector3 position;
        float baseY;
        float sweepRange;
        ObstacleData pendingShotData;
        float pendingShotSpeed;

        public bool IsDone => state == State.Done;
        public bool IsDefeated => state == State.Defeated || state == State.Done;
        public float Health => health;
        public BossData Data => data;

        public BossManager(LevelContext context, BossData bossData, Transform parent)
        {
            ctx = context;
            data = bossData;
            view = BossModelBuilder.Build(data, parent);
            baseY = ctx.Field.Top - 3.4f;
            sweepRange = ctx.Field.HalfWidth - 2f;
            position = new Vector3(0f, ctx.Field.Top + 5f, 1f);
            view.transform.position = position;
            state = State.Entering;
            stateTimer = 0f;
            crystalTimer = data.crystalInterval * 0.6f;
            ctx.Coins.CrystalCollected += OnCrystalCollected;
            GameEvents.RaiseBossHealthChanged(1f);
            if (data.phases.Count > 0) GameEvents.RaiseBossPhaseStarted(data.phases[0].name);
            ctx.Audio?.PlaySfx(SfxId.BossAttack);
            ctx.Shake?.Shake(0.4f);
        }

        BossPhase Phase => data.phases.Count > 0 ? data.phases[Mathf.Clamp(phaseIndex, 0, data.phases.Count - 1)] : null;

        public void Tick(float dt)
        {
            if (dt <= 0f) return;
            stateTimer += dt;
            switch (state)
            {
                case State.Entering:
                    position.y = Mathf.Lerp(ctx.Field.Top + 5f, baseY, Ease.OutCubic(Mathf.Clamp01(stateTimer / 2.5f)));
                    view.transform.position = position;
                    if (stateTimer >= 2.5f) { state = State.Fighting; stateTimer = 0f; ResetAttackTimers(0.8f); }
                    break;
                case State.Fighting:
                    Fight(dt);
                    break;
                case State.Defeated:
                    position.y -= dt * 1.2f;
                    position.x += Mathf.Sin(stateTimer * 30f) * 0.04f;
                    view.transform.position = position;
                    view.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.2f, Mathf.Clamp01((stateTimer - 1.5f) / 1.2f));
                    if (Random.value < 0.3f) ParticleFactory.Burst(position + new Vector3(Random.Range(-1.5f, 1.5f), Random.Range(-1f, 1f), -2f), data.glowColor, 10, 4f, 0.4f);
                    if (stateTimer > 2.8f)
                    {
                        ParticleFactory.Burst(position + Vector3.back * 2f, Color.white, 60, 8f, 0.6f, 1f);
                        view.gameObject.SetActive(false);
                        state = State.Done;
                    }
                    break;
            }
        }

        void ResetAttackTimers(float scale)
        {
            var phase = Phase;
            if (phase == null) return;
            for (int i = 0; i < phase.attacks.Count && i < attackTimers.Length; i++)
                attackTimers[i] = phase.attacks[i].cooldown * scale * (0.6f + 0.25f * i);
        }

        void Fight(float dt)
        {
            health -= dt / Mathf.Max(5f, data.encounterDuration);
            CheckPhase();
            GameEvents.RaiseBossHealthChanged(Mathf.Max(0f, health));
            if (health <= 0f)
            {
                Defeat();
                return;
            }

            var phase = Phase;
            Move(phase, dt);

            if (phase != null)
            {
                float rate = Mathf.Max(0.2f, phase.attackRateMultiplier);
                for (int i = 0; i < phase.attacks.Count && i < attackTimers.Length; i++)
                {
                    attackTimers[i] -= dt * rate;
                    if (attackTimers[i] > 0f) continue;
                    attackTimers[i] = phase.attacks[i].cooldown;
                    Execute(phase.attacks[i]);
                }
            }

            for (int i = pendingShots.Count - 1; i >= 0; i--)
            {
                if (pendingShots[i].time > stateTimer) continue;
                var target = new Vector3(ctx.Balloon.X, ctx.BalloonY, 0f);
                var dir = (target - position).normalized;
                if (pendingShotData != null) ctx.Obstacles.SpawnProjectile(pendingShotData, position + Vector3.down * 1.2f, dir * pendingShotSpeed);
                pendingShots.RemoveAt(i);
            }

            crystalTimer -= dt;
            if (crystalTimer <= 0f)
            {
                crystalTimer = data.crystalInterval;
                ctx.Coins.SpawnCrystal(ctx.Lanes.CorridorCenter);
            }
        }

        void CheckPhase()
        {
            while (phaseIndex + 1 < data.phases.Count && health <= data.phases[phaseIndex + 1].healthThreshold)
            {
                phaseIndex++;
                GameEvents.RaiseBossPhaseStarted(Phase.name);
                view.SetIntensity(1f + phaseIndex * 0.25f);
                view.AttackPulse();
                ctx.Shake?.Shake(0.5f);
                ctx.Audio?.PlaySfx(SfxId.BossAttack);
                ResetAttackTimers(0.5f);
            }
        }

        void Move(BossPhase phase, float dt)
        {
            if (phase == null) return;
            moveTime += dt;
            float speed = phase.moveSpeed;
            switch (phase.movement)
            {
                case BossMovement.HorizontalSweep:
                    position.x = Mathf.Sin(moveTime * speed * 0.35f) * sweepRange;
                    position.y = baseY + Mathf.Sin(moveTime * 1.3f) * 0.3f;
                    break;
                case BossMovement.FigureEight:
                    position.x = Mathf.Sin(moveTime * speed * 0.3f) * sweepRange;
                    position.y = baseY + Mathf.Sin(moveTime * speed * 0.6f) * 0.8f;
                    break;
                case BossMovement.TrackPlayer:
                    position.x = Mathf.MoveTowards(position.x, Mathf.Clamp(ctx.Balloon.X, -sweepRange, sweepRange), speed * dt);
                    position.y = baseY + Mathf.Sin(moveTime * 1.5f) * 0.25f;
                    break;
                case BossMovement.Hover:
                    position.x = Mathf.Sin(moveTime * 0.5f) * 1.2f;
                    position.y = baseY + Mathf.Sin(moveTime * 1.1f) * 0.35f;
                    break;
                case BossMovement.Teleport:
                    teleportTimer -= dt;
                    float scale = 1f;
                    if (teleportTimer < 0.4f) scale = Mathf.Clamp01(teleportTimer / 0.4f);
                    if (teleportTimer <= 0f)
                    {
                        position.x = ((float)ctx.Rng.NextDouble() * 2f - 1f) * sweepRange;
                        teleportTimer = Mathf.Max(2f, 6f - speed);
                        ParticleFactory.Burst(position + Vector3.back * 2f, data.glowColor, 30, 5f, 0.4f);
                    }
                    view.transform.localScale = Vector3.one * Mathf.Max(0.05f, scale < 1f ? scale : Mathf.Clamp01((Mathf.Max(2f, 6f - speed) - teleportTimer) / 0.3f));
                    position.y = baseY + Mathf.Sin(moveTime * 1.2f) * 0.3f;
                    break;
            }
            view.transform.position = position;
        }

        void Execute(BossAttack attack)
        {
            if (attack == null) return;
            view.AttackPulse();
            ctx.Audio?.PlaySfx(SfxId.BossAttack, 0.5f);
            var lanes = ctx.Lanes;
            var origin = position + Vector3.down * 1.2f;
            switch (attack.type)
            {
                case BossAttackType.ProjectileFan:
                {
                    if (attack.projectile == null) break;
                    int n = Mathf.Max(2, attack.count);
                    int gap = 1 + ctx.Rng.Next(Mathf.Max(1, n - 2));
                    for (int i = 0; i < n; i++)
                    {
                        if (i == gap) continue;
                        float angle = Mathf.Lerp(-55f, 55f, i / (float)(n - 1)) * Mathf.Deg2Rad;
                        var dir = new Vector3(Mathf.Sin(angle), -Mathf.Cos(angle), 0f);
                        ctx.Obstacles.SpawnProjectile(attack.projectile, origin, dir * attack.speed);
                    }
                    break;
                }
                case BossAttackType.AimedVolley:
                    pendingShotData = attack.projectile;
                    pendingShotSpeed = attack.speed;
                    for (int i = 0; i < Mathf.Max(1, attack.count); i++) pendingShots.Add((stateTimer + i * 0.35f, Vector3.zero));
                    break;
                case BossAttackType.ColumnStrike:
                {
                    if (attack.projectile == null) break;
                    var chosen = new List<int>();
                    int tries = 0;
                    while (chosen.Count < attack.count && tries++ < 30)
                    {
                        int lane = ctx.Rng.Next(lanes.LaneCount);
                        if (lanes.IsCorridorLane(lane) || chosen.Contains(lane) || chosen.Contains(lane - 1) || chosen.Contains(lane + 1)) continue;
                        chosen.Add(lane);
                    }
                    foreach (var lane in chosen) ctx.Obstacles.SpawnColumn(attack.projectile, lanes.LaneCenter(lane), attack.telegraph);
                    break;
                }
                case BossAttackType.RainBarrage:
                {
                    if (attack.projectile == null) break;
                    for (int i = 0; i < attack.count; i++)
                    {
                        int lane = ctx.Rng.Next(lanes.LaneCount);
                        if (lanes.IsCorridorLane(lane)) continue;
                        float x = lanes.LaneCenter(lane) + ((float)ctx.Rng.NextDouble() - 0.5f) * lanes.LaneWidth * 0.5f;
                        float y = ctx.SpawnY + (float)ctx.Rng.NextDouble() * 3f;
                        ctx.Obstacles.SpawnProjectile(attack.projectile, new Vector3(x, y, 0f), Vector3.down * attack.speed);
                        lanes.Block(lane, 1, ctx.ElapsedTime + 1.2f);
                    }
                    lanes.AdvanceCorridor(ctx.ElapsedTime);
                    break;
                }
                case BossAttackType.Summon:
                    if (attack.projectile == null) break;
                    for (int i = 0; i < Mathf.Max(1, attack.count); i++) ctx.Obstacles.TrySpawn(attack.projectile);
                    break;
                case BossAttackType.WallWithGap:
                    if (attack.projectile != null && !ctx.Obstacles.TrySpawnWall(attack.projectile)) attackTimers[0] = Mathf.Min(attackTimers[0], 0.5f);
                    break;
                case BossAttackType.SpiralOrbs:
                {
                    if (attack.projectile == null) break;
                    int n = Mathf.Max(4, attack.count);
                    float offset = (float)ctx.Rng.NextDouble() * Mathf.PI * 2f;
                    int gap = ctx.Rng.Next(n);
                    for (int i = 0; i < n; i++)
                    {
                        if (i == gap || i == (gap + 1) % n) continue;
                        float a = offset + i / (float)n * Mathf.PI * 2f;
                        var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                        ctx.Obstacles.SpawnProjectile(attack.projectile, position + dir * 1.5f, dir * attack.speed + Vector3.down * attack.speed * 0.6f);
                    }
                    break;
                }
            }
        }

        void OnCrystalCollected()
        {
            if (state != State.Fighting) return;
            health -= data.crystalDamage;
            view.HitFlash();
            ctx.Shake?.Shake(0.3f);
            GameEvents.RaiseToast("Crystal strike!");
            CheckPhase();
        }

        void Defeat()
        {
            state = State.Defeated;
            stateTimer = 0f;
            health = 0f;
            GameEvents.RaiseBossHealthChanged(0f);
            ctx.Obstacles.ClearAll(true);
            ctx.Shake?.Shake(0.8f);
            ctx.Audio?.PlaySfx(SfxId.BossDefeat);
            GameEvents.RaiseToast(data.bossName + " defeated!");
        }

        public void Dispose()
        {
            ctx.Coins.CrystalCollected -= OnCrystalCollected;
        }
    }
}
