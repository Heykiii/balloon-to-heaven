using BalloonToHeaven.Levels;
using BalloonToHeaven.Visuals;
using UnityEngine;

namespace BalloonToHeaven.Obstacles
{
    /// <summary>Flies across the screen horizontally with a slight wave.</summary>
    public sealed class CrossHorizontalBehavior : MonoBehaviour, IObstacleBehavior
    {
        public void OnSpawn(ObstacleController o, LevelContext ctx) { }

        public void Tick(ObstacleController o, LevelContext ctx, float dt)
        {
            o.Position.x += o.Direction * o.Data.speed * o.SpeedMultiplier * dt;
            o.Position.y = o.Anchor.y + Mathf.Sin(o.Age * 2.5f + o.Phase) * 0.25f;
        }
    }

    /// <summary>Moves back and forth around its anchor (platforms, temple blocks).</summary>
    public sealed class PatrolBehavior : MonoBehaviour, IObstacleBehavior
    {
        public void OnSpawn(ObstacleController o, LevelContext ctx) { }

        public void Tick(ObstacleController o, LevelContext ctx, float dt)
        {
            float amplitude = Mathf.Max(0.1f, o.Data.amplitude);
            float w = o.Data.speed * o.SpeedMultiplier / amplitude;
            o.Position.x = o.Anchor.x + Mathf.Sin(o.Age * w + o.Phase) * amplitude;
        }
    }

    /// <summary>Falls faster than the world scrolls (rocks, snowballs, shards).</summary>
    public sealed class FallingBehavior : MonoBehaviour, IObstacleBehavior
    {
        public void OnSpawn(ObstacleController o, LevelContext ctx) { }

        public void Tick(ObstacleController o, LevelContext ctx, float dt)
        {
            o.Position.y -= o.Data.speed * o.SpeedMultiplier * dt;
        }
    }

    /// <summary>Falls diagonally across the screen (fireballs, meteorites, asteroids).</summary>
    public sealed class DiagonalBehavior : MonoBehaviour, IObstacleBehavior
    {
        public void OnSpawn(ObstacleController o, LevelContext ctx) { }

        public void Tick(ObstacleController o, LevelContext ctx, float dt)
        {
            float s = o.Data.speed * o.SpeedMultiplier;
            o.Position.x += o.Direction * s * 0.35f * dt;
            o.Position.y -= s * dt;
        }
    }

    /// <summary>Orbits its anchor (energy orbs).</summary>
    public sealed class CircularBehavior : MonoBehaviour, IObstacleBehavior
    {
        public void OnSpawn(ObstacleController o, LevelContext ctx) { }

        public void Tick(ObstacleController o, LevelContext ctx, float dt)
        {
            float r = Mathf.Max(0.2f, o.Data.amplitude);
            float a = o.Age * o.Data.speed * o.SpeedMultiplier * o.Direction + o.Phase;
            o.Position.x = o.Anchor.x + Mathf.Cos(a) * r;
            o.Position.y = o.Anchor.y + Mathf.Sin(a) * r;
        }
    }

    /// <summary>
    /// Steers towards the balloon only while far above it, then commits to its line so a late
    /// sidestep always dodges it. Ignores an invisible balloon.
    /// </summary>
    public sealed class HomingBehavior : MonoBehaviour, IObstacleBehavior
    {
        float vx;

        public void OnSpawn(ObstacleController o, LevelContext ctx) { vx = 0f; }

        public void Tick(ObstacleController o, LevelContext ctx, float dt)
        {
            float speed = o.Data.speed * o.SpeedMultiplier;
            bool canTrack = o.Position.y - ctx.BalloonY > 4f && (ctx.Abilities == null || !ctx.Abilities.IsInvisible);
            if (canTrack)
            {
                float dx = ctx.Balloon.X - o.Position.x;
                float desired = Mathf.Clamp(dx, -1f, 1f) * speed * 0.8f;
                vx = Mathf.MoveTowards(vx, desired, o.Data.homingStrength * dt * 2f);
            }
            o.Position.x += vx * dt;
            o.Position.y -= speed * 0.45f * dt;
            if (o.View != null && Mathf.Abs(vx) > 0.2f) o.View.SetFacing(vx);
        }
    }

    /// <summary>Group member: follows a slowly drifting anchor with its own wobble (swarms).</summary>
    public sealed class SwarmBehavior : MonoBehaviour, IObstacleBehavior
    {
        public void OnSpawn(ObstacleController o, LevelContext ctx) { }

        public void Tick(ObstacleController o, LevelContext ctx, float dt)
        {
            o.Anchor.x += o.Direction * o.Data.speed * 0.35f * o.SpeedMultiplier * dt;
            float limit = ctx.Field.HalfWidth - 1f;
            if (o.Anchor.x > limit) o.Direction = -1f; else if (o.Anchor.x < -limit) o.Direction = 1f;
            float wobble = o.Data.amplitude * 0.35f;
            o.Position.x = o.Anchor.x + o.Offset.x + Mathf.Sin(o.Age * 3.1f + o.Phase) * wobble;
            o.Position.y = o.Anchor.y + o.Offset.y + Mathf.Cos(o.Age * 2.7f + o.Phase) * wobble;
            if (o.View != null) o.View.SetFacing(o.Direction);
        }
    }

    /// <summary>Bobs vertically around its anchor (ice crystals).</summary>
    public sealed class VerticalBobBehavior : MonoBehaviour, IObstacleBehavior
    {
        public void OnSpawn(ObstacleController o, LevelContext ctx) { }

        public void Tick(ObstacleController o, LevelContext ctx, float dt)
        {
            o.Position.y = o.Anchor.y + Mathf.Sin(o.Age * Mathf.Max(0.2f, o.Data.frequency) * 2f + o.Phase) * o.Data.amplitude;
        }
    }

    /// <summary>
    /// Timed appearance: idle -> warning (telegraph, harmless) -> active (damaging) -> cooldown.
    /// Only starts warning once fully on screen so the player always sees the telegraph.
    /// </summary>
    public sealed class TimedHazardBehavior : MonoBehaviour, IObstacleBehavior
    {
        enum State { Idle, Warning, Active, Cooldown }
        State state;
        float timer;

        public void OnSpawn(ObstacleController o, LevelContext ctx)
        {
            state = State.Idle;
            timer = o.WarningOverride >= 0f ? 0f : (float)ctx.Rng.NextDouble() * 0.6f;
            o.CollisionEnabled = false;
            o.View?.SetState(HazardVisualState.Idle);
        }

        public void Tick(ObstacleController o, LevelContext ctx, float dt)
        {
            timer -= dt;
            switch (state)
            {
                case State.Idle:
                case State.Cooldown:
                    bool onScreen = o.Top < ctx.Field.Top + 0.5f || o.ScrollFactor == 0f;
                    if (timer <= 0f && onScreen)
                    {
                        state = State.Warning;
                        timer = o.WarningOverride >= 0f ? o.WarningOverride : o.Data.warningTime;
                        o.View?.SetState(HazardVisualState.Warning);
                    }
                    break;
                case State.Warning:
                    if (timer <= 0f)
                    {
                        state = State.Active;
                        timer = o.Data.activeTime;
                        o.CollisionEnabled = true;
                        o.View?.SetState(HazardVisualState.Active);
                        ctx.Audio?.PlaySfx(o.Data.damageType == Core.DamageType.Electric ? Core.SfxId.Thunder : Core.SfxId.BossAttack, 0.6f);
                    }
                    break;
                case State.Active:
                    if (timer <= 0f)
                    {
                        o.CollisionEnabled = false;
                        o.View?.SetState(HazardVisualState.Idle);
                        if (o.DespawnAfterActive) { o.Alive = false; return; }
                        state = State.Cooldown;
                        timer = o.Data.cooldownTime;
                    }
                    break;
            }
        }
    }

    /// <summary>Non-damaging wind zone; its push is applied by the spawner while overlapping.</summary>
    public sealed class WindZoneBehavior : MonoBehaviour, IObstacleBehavior
    {
        public void OnSpawn(ObstacleController o, LevelContext ctx)
        {
            o.Direction = ctx.Rng.NextDouble() < 0.5 ? -1f : 1f;
            o.View?.SetWindDirection(o.Direction);
        }

        public void Tick(ObstacleController o, LevelContext ctx, float dt) { }
    }
}
