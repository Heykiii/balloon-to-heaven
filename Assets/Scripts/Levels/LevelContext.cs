using BalloonToHeaven.Audio;
using BalloonToHeaven.Balloons;
using BalloonToHeaven.Content;
using BalloonToHeaven.Core;
using BalloonToHeaven.Economy;
using BalloonToHeaven.Obstacles;
using BalloonToHeaven.Player;
using BalloonToHeaven.PowerUps;
using BalloonToHeaven.Visuals;
using BalloonToHeaven.Worlds;
using UnityEngine;

namespace BalloonToHeaven.Levels
{
    /// <summary>
    /// Per-run state shared by the gameplay systems of one level. Systems receive this object
    /// instead of reaching for globals, which keeps them testable and independent.
    /// </summary>
    public sealed class LevelContext
    {
        public GameDatabase Database;
        public GameConfig Config;
        public LevelData Level;
        public WorldData World;
        public BalloonData BalloonData;
        public Playfield Field;
        public System.Random Rng;
        public Camera Camera;
        public CameraShake Shake;
        public Transform WorldRoot;
        public AudioManager Audio;

        public BalloonController Balloon;
        public BalloonHealth Health;
        public BalloonAbilities Abilities;
        public PowerUpManager PowerUps;
        public ObstacleSpawner Obstacles;
        public CoinManager Coins;
        public PowerUpSpawner PowerUpSpawner;
        public EnvironmentEffects Effects;
        public LanePlanner Lanes;

        public float ScrollSpeed;
        public float Distance;
        public float ElapsedTime;
        public bool NormalSpawning = true;
        public bool BossActive;

        // Run statistics
        public int CoinsCollected;
        public int SpecialCoinsCollected;
        public int SpecialCoinsTotal;
        public int PowerUpsCollected;

        public float BalloonY => Config.balloonScreenY;
        public float Progress => Mathf.Clamp01(Distance / Level.LevelLength);
        public Vector2 BalloonCenter => new Vector2(Balloon != null ? Balloon.X : 0f, BalloonY + 0.2f);
        public float SpawnY => Field.Top + Config.spawnMargin;
        public float DespawnY => Field.Bottom - Config.despawnMargin;
    }
}
