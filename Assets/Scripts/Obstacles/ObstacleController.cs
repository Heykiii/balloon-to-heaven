using BalloonToHeaven.Levels;
using BalloonToHeaven.Visuals;
using UnityEngine;

namespace BalloonToHeaven.Obstacles
{
    /// <summary>Movement or timing behaviour attached to an obstacle as a separate component.</summary>
    public interface IObstacleBehavior
    {
        void OnSpawn(ObstacleController obstacle, LevelContext context);
        void Tick(ObstacleController obstacle, LevelContext context, float deltaTime);
    }

    /// <summary>
    /// Runtime obstacle instance (pooled). Holds position, collision shape and state; delegates
    /// movement to its behaviour components. Ticked centrally by the ObstacleSpawner.
    /// </summary>
    public sealed class ObstacleController : MonoBehaviour
    {
        IObstacleBehavior[] behaviors;

        public ObstacleData Data { get; private set; }
        public ObstacleView View { get; private set; }

        public Vector3 Position;
        public Vector3 Anchor;
        public Vector3 Offset;
        public Vector2 BoxSize;
        public float Radius;
        public float Age;
        public float SpeedMultiplier = 1f;
        public float ScrollFactor = 1f;
        public float Direction = 1f;
        public float Phase;
        public Vector3 CustomVelocity;
        public bool UseCustomVelocity;
        public bool CollisionEnabled = true;
        public bool Alive;
        public bool DespawnAfterActive;
        public float WarningOverride = -1f;

        public void Setup(ObstacleData data, ObstacleView view)
        {
            Data = data;
            View = view;
            behaviors = GetComponents<IObstacleBehavior>();
        }

        /// <summary>Restores per-spawn parameters to defaults before a pooled instance is reused.</summary>
        public void ResetParameters()
        {
            BoxSize = Data.boxSize;
            Offset = Vector3.zero;
            SpeedMultiplier = 1f;
            ScrollFactor = 1f;
            Direction = 1f;
            CustomVelocity = Vector3.zero;
            UseCustomVelocity = false;
            DespawnAfterActive = false;
            WarningOverride = -1f;
        }

        public void Spawn(Vector3 position, LevelContext ctx)
        {
            Position = position;
            Anchor = position;
            Age = 0f;
            Alive = true;
            CollisionEnabled = !Data.timed;
            Radius = Data.radius;
            Phase = (float)ctx.Rng.NextDouble() * Mathf.PI * 2f;
            transform.position = position;
            if (View != null)
            {
                View.SetFacing(Direction);
                if (Data.shape == ColliderShape.Box) View.SetSize(BoxSize);
                View.ResetTrail();
            }
            for (int i = 0; i < behaviors.Length; i++) behaviors[i].OnSpawn(this, ctx);
        }

        public void Tick(LevelContext ctx, float dt)
        {
            Age += dt;
            float scroll = ctx.ScrollSpeed * ScrollFactor * dt;
            Position.y -= scroll;
            Anchor.y -= scroll;
            if (UseCustomVelocity)
            {
                Position += CustomVelocity * dt;
            }
            else
            {
                for (int i = 0; i < behaviors.Length; i++) behaviors[i].Tick(this, ctx, dt);
            }
            transform.position = Position;
        }

        /// <summary>Circle (balloon) versus this obstacle's circle or box.</summary>
        public bool Overlaps(Vector2 center, float radius)
        {
            if (Data.shape == ColliderShape.Circle)
            {
                float r = Radius + radius;
                float dx = center.x - Position.x, dy = center.y - Position.y;
                return dx * dx + dy * dy <= r * r;
            }
            float hx = BoxSize.x * 0.5f, hy = BoxSize.y * 0.5f;
            float cx = Mathf.Clamp(center.x, Position.x - hx, Position.x + hx);
            float cy = Mathf.Clamp(center.y, Position.y - hy, Position.y + hy);
            float ddx = center.x - cx, ddy = center.y - cy;
            return ddx * ddx + ddy * ddy <= radius * radius;
        }

        public float Top => Position.y + (Data.shape == ColliderShape.Circle ? Radius : BoxSize.y * 0.5f);
        public float Bottom => Position.y - (Data.shape == ColliderShape.Circle ? Radius : BoxSize.y * 0.5f);
    }
}
