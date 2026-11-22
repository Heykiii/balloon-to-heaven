using System;
using UnityEngine;

namespace BalloonToHeaven.Core
{
    /// <summary>Configurable reward values. Every economy payout reads from here.</summary>
    [Serializable]
    public class RewardConfig
    {
        public int coinPickup = 1;
        public int specialCoinValue = 10;
        public int levelComplete = 100;
        public int noDamageBonus = 50;
        public int specialCoinsBonus = 100;
        public int threeStarBonus = 100;
        public int bossDefeat = 500;
        public int worldComplete = 1000;
        public int bossFirstDefeatGems = 5;
        public int worldCompleteGems = 10;
        public int firstThreeStarGems = 1;
    }

    /// <summary>
    /// Global gameplay tuning. Movement, camera, damage and spawning numbers live here so that
    /// they are never hard-coded across the code base.
    /// </summary>
    [CreateAssetMenu(menuName = "Balloon to Heaven/Game Config", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Camera & playfield")]
        public float cameraOrthoSize = 10f;
        [Tooltip("Gameplay half width is capped so wide screens do not get an easier/harder game.")]
        public float maxPlayfieldHalfWidth = 5.625f;
        public float screenEdgeMargin = 0.35f;
        public float balloonScreenY = -5.2f;

        [Header("Movement")]
        public float horizontalSpeed = 9f;
        public float horizontalAcceleration = 60f;
        [Tooltip("Seconds used to smooth the follow target (higher = softer).")]
        public float movementSmoothing = 0.06f;
        public float maxHorizontalVelocity = 13f;
        [Tooltip("Velocity retained per second when the finger is lifted (inertia).")]
        public float releaseDamping = 6f;
        public float dragSensitivity = 1f;
        public float balloonCollisionRadius = 0.7f;
        public float maxTiltDegrees = 14f;

        [Header("Damage")]
        public float postHitInvulnerability = 1.4f;
        public float blockedHitInvulnerability = 0.6f;
        public float armorDeflectChancePerPoint = 0.05f;
        public float maxArmorDeflectChance = 0.4f;

        [Header("Spawning")]
        public float spawnMargin = 2f;
        public float despawnMargin = 3f;
        public int laneCount = 7;
        public float minSafeReactionTime = 1.3f;
        public float minCrosserSpacingSeconds = 1.1f;

        [Header("Coins")]
        public float baseMagnetRadius = 1.1f;
        public float magnetPullSpeed = 14f;

        [Header("Rewards")]
        public RewardConfig rewards = new RewardConfig();
    }
}
