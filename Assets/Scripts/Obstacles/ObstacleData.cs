using BalloonToHeaven.Core;
using UnityEngine;

namespace BalloonToHeaven.Obstacles
{
    public enum ObstacleVisual
    {
        SmallBird, Eagle, Butterfly, Rock, CliffWall, WindCurrent, MovingPlatform, DarkCloud,
        LightningCloud, ElectricBird, FrozenBird, Snowball, IceCrystal, BurningBird, Fireball,
        LavaColumn, Asteroid, Meteorite, AlienDrone, TempleBlock, MagicBarrier, GuardianOrb,
        CelestialBird, EnergyOrb, CrystalBarrier, CrystalShard, GoldenGate, Feather, LightBeam
    }

    public enum MovementPattern
    {
        Static,          // scrolls with the world only
        CrossHorizontal, // flies across the screen
        Patrol,          // moves back and forth around its spawn x
        Falling,         // falls faster than the world scroll
        Diagonal,        // falls with horizontal drift
        Circular,        // orbits around an anchor
        Homing,          // steers towards the balloon while still far above it
        Swarm,           // moves as part of a group
        VerticalBob      // bobs up and down
    }

    public enum ColliderShape { Circle, Box }

    /// <summary>
    /// Data definition of an obstacle type. Behaviour is assembled from separate components
    /// chosen by <see cref="movement"/> and the timed-hazard settings.
    /// </summary>
    [CreateAssetMenu(menuName = "Balloon to Heaven/Obstacle", fileName = "Obstacle")]
    public class ObstacleData : ScriptableObject
    {
        public string id;
        public string displayName;
        public ObstacleVisual visual;
        public DamageType damageType;

        [Header("Collision")]
        public ColliderShape shape = ColliderShape.Circle;
        public float radius = 0.5f;
        public Vector2 boxSize = Vector2.one;
        public int damage = 1;
        [Tooltip("Non-damaging zones (wind) push the balloon instead of hurting it.")]
        public bool harmless;
        public float pushForce;
        public bool destroyOnHit;

        [Header("Movement")]
        public MovementPattern movement;
        public float speed = 2f;
        public float amplitude = 1.5f;
        public float frequency = 1f;
        public float homingStrength = 2f;
        public int groupMin = 1;
        public int groupMax = 1;
        public float spin;

        [Header("Timed hazard")]
        public bool timed;
        public float warningTime = 1f;
        public float activeTime = 1f;
        public float cooldownTime = 1.5f;

        [Header("Spawning")]
        [Tooltip("Spans the whole row except for a guaranteed gap (cliffs, gates).")]
        public bool fullRowWithGap;
        public float gapWidth = 3.2f;
        [Tooltip("Bit mask of worlds (bit 0 = world 1) where the obstacle is designed to appear.")]
        public int worldMask = ~0;

        [Header("Presentation")]
        public Color primaryColor = Color.white;
        public Color secondaryColor = Color.gray;
        public SfxId hitSound = SfxId.Hit;
        public float scale = 1f;

        /// <summary>Approximate width used by the fair spawn planner.</summary>
        public float FootprintWidth
        {
            get
            {
                float w = shape == ColliderShape.Circle ? radius * 2f : boxSize.x;
                if (movement == MovementPattern.Patrol || movement == MovementPattern.Circular) w += amplitude * 2f;
                if (groupMax > 1) w += 1.2f;
                return w;
            }
        }

        public float FootprintHeight => shape == ColliderShape.Circle ? radius * 2f : boxSize.y;

        public bool IsCompatibleWith(int worldNumber) => (worldMask & (1 << (worldNumber - 1))) != 0;
    }
}
