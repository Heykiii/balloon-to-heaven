using System;
using System.Collections.Generic;
using BalloonToHeaven.Obstacles;
using UnityEngine;

namespace BalloonToHeaven.Bosses
{
    public enum BossVisual
    {
        ValleyGuardian, MountainKing, ButterflyQueen, ThunderDragon, IceGuardian,
        FirePhoenix, CosmicGuardian, GoldenTitan, CelestialGuardian, HeavensGuardian
    }

    public enum BossMovement { HorizontalSweep, FigureEight, TrackPlayer, Hover, Teleport }

    public enum BossAttackType
    {
        ProjectileFan,   // spread of projectiles with a guaranteed gap
        AimedVolley,     // short burst aimed at the balloon's current position
        ColumnStrike,    // telegraphed vertical columns leaving safe lanes
        RainBarrage,     // falling objects across the screen leaving a corridor
        Summon,          // spawns regular enemies
        WallWithGap,     // a horizontal wall with one gap descends
        SpiralOrbs       // rotating ring of orbs that expands downward
    }

    [Serializable]
    public class BossAttack
    {
        public BossAttackType type;
        public ObstacleData projectile;
        public int count = 5;
        public float cooldown = 3f;
        public float speed = 6f;
        public float telegraph = 0.9f;
    }

    [Serializable]
    public class BossPhase
    {
        public string name = "Phase";
        [Tooltip("Phase begins when boss health drops to this normalized value or below.")]
        [Range(0f, 1f)] public float healthThreshold = 1f;
        public BossMovement movement;
        public float moveSpeed = 2f;
        public float attackRateMultiplier = 1f;
        public List<BossAttack> attacks = new List<BossAttack>();
    }

    /// <summary>
    /// Boss encounter definition. Bosses are defeated by surviving: health drains over the
    /// encounter duration and every collected Sky Crystal deals extra damage.
    /// </summary>
    [CreateAssetMenu(menuName = "Balloon to Heaven/Boss", fileName = "Boss")]
    public class BossData : ScriptableObject
    {
        public string id;
        public string bossName;
        public string title;
        public BossVisual visual;
        public Color primaryColor = Color.white;
        public Color secondaryColor = Color.gray;
        public Color glowColor = Color.yellow;
        public float modelScale = 1f;

        [Header("Encounter")]
        public float encounterDuration = 50f;
        [Range(0f, 0.5f)] public float crystalDamage = 0.06f;
        public float crystalInterval = 5f;
        public List<BossPhase> phases = new List<BossPhase>();

        [Header("Rewards")]
        public int rewardCoins = 500;
        public int rewardGems = 5;
    }
}
