using System.Collections.Generic;
using BalloonToHeaven.Balloons;
using BalloonToHeaven.Bosses;
using BalloonToHeaven.Core;
using BalloonToHeaven.Levels;
using BalloonToHeaven.Obstacles;
using BalloonToHeaven.PowerUps;
using BalloonToHeaven.Upgrades;
using BalloonToHeaven.Worlds;
using UnityEngine;

namespace BalloonToHeaven.Content
{
    /// <summary>
    /// Builds the default campaign content: 20 balloons, 10 worlds, 60 levels, 10 bosses, obstacles,
    /// power-ups, cosmetics and achievements. The editor generator saves the result as
    /// ScriptableObject assets (which designers then tune in the Inspector); the runtime uses it
    /// as a fallback when those assets are missing.
    /// </summary>
    public static class ContentFactory
    {
        static T Create<T>(string name) where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            asset.name = name;
            return asset;
        }

        /// <summary>Parses "#RRGGBB" without relying on engine-native helpers.</summary>
        public static Color Hex(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return Color.magenta;
            if (hex[0] == '#') hex = hex.Substring(1);
            if (hex.Length < 6) return Color.magenta;
            int v = System.Convert.ToInt32(hex.Substring(0, 6), 16);
            return new Color(((v >> 16) & 255) / 255f, ((v >> 8) & 255) / 255f, (v & 255) / 255f, 1f);
        }

        static int WorldBits(params int[] worlds)
        {
            int mask = 0;
            foreach (var w in worlds) mask |= 1 << (w - 1);
            return mask;
        }

        public static GameDatabase CreateDatabase()
        {
            var db = Create<GameDatabase>("GameDatabase");
            db.config = Create<GameConfig>("GameConfig");
            db.workshop = Create<WorkshopConfig>("WorkshopConfig");
            db.balloons = CreateBalloons();
            db.powerUps = CreatePowerUps();
            var obstacles = CreateObstacles();
            db.obstacles = new List<ObstacleData>(obstacles.Values);
            db.bosses = CreateBosses(obstacles);
            db.worlds = CreateWorlds(db.bosses);
            db.levels = CreateLevels(db.worlds, obstacles);
            db.trails = CreateTrails();
            db.achievements = CreateAchievements();
            return db;
        }

        // ------------------------------------------------------------------ Balloons

        static BalloonData Balloon(int order, string id, string name, BalloonRarity rarity, int price, int unlockWorld,
            int hp, float speed, int armor, float magnet, BalloonAbilityType ability, string abilityName, string abilityText,
            EnvelopeShape shape, EnvelopePattern pattern, string c1, string c2, string c3, BalloonOrnament ornaments)
        {
            var b = Create<BalloonData>(order.ToString("00") + "_" + id);
            b.id = id;
            b.displayName = name;
            b.sortOrder = order;
            b.rarity = rarity;
            b.price = price;
            b.unlockWorld = unlockWorld;
            b.baseHealth = hp;
            b.speed = speed;
            b.armor = armor;
            b.magnetRadius = magnet;
            b.ability = ability;
            b.abilityName = abilityName;
            b.abilityDescription = abilityText;
            b.shape = shape;
            b.pattern = pattern;
            b.primaryColor = Hex(c1);
            b.secondaryColor = Hex(c2);
            b.accentColor = Hex(c3);
            b.glowColor = Color.Lerp(Hex(c1), Color.white, 0.45f);
            b.ornaments = ornaments;
            b.basketColor = rarity >= BalloonRarity.Legendary ? Hex("#8E6B3A") : Hex("#9C6B3C");
            b.description = name + " - " + rarity + " balloon.";

            float baseCost = Mathf.Max(price, 1200);
            float[] factors = { 0.25f, 0.55f, 1.0f, 1.6f };
            b.stageUpgradeCosts = new int[4];
            for (int i = 0; i < 4; i++) b.stageUpgradeCosts[i] = Mathf.RoundToInt(baseCost * factors[i] / 50f) * 50;
            b.stageBonuses = new[]
            {
                new BalloonStageBonus(),
                new BalloonStageBonus { speedBonus = 0.03f, magnetBonus = 0.15f },
                new BalloonStageBonus { speedBonus = 0.03f, magnetBonus = 0.15f, bonusHealth = 1 },
                new BalloonStageBonus { speedBonus = 0.04f, magnetBonus = 0.35f, bonusHealth = 1, bonusArmor = 1 },
                new BalloonStageBonus { speedBonus = 0.08f, magnetBonus = 0.5f, bonusHealth = 2, bonusArmor = 1 },
            };
            return b;
        }

        static List<BalloonData> CreateBalloons()
        {
            const BalloonRarity C = BalloonRarity.Common, R = BalloonRarity.Rare, E = BalloonRarity.Epic, L = BalloonRarity.Legendary, M = BalloonRarity.Mythical;
            var list = new List<BalloonData>
            {
                Balloon(1, "classic", "Classic Balloon", C, 0, 1, 1, 1.00f, 0, 1.1f, BalloonAbilityType.None, "None", "A trusty basic balloon with no special ability.",
                    EnvelopeShape.Classic, EnvelopePattern.Stripes, "#E8413C", "#FFF4E0", "#FFC93C", BalloonOrnament.None),
                Balloon(2, "sunshine", "Sunshine Balloon", C, 500, 1, 1, 1.00f, 0, 1.1f, BalloonAbilityType.BonusCoins, "Sunny Fortune", "+25% coins from every pickup.",
                    EnvelopeShape.Round, EnvelopePattern.SunRays, "#FFC93C", "#FF8C42", "#FFFFFF", BalloonOrnament.SunRays),
                Balloon(3, "ocean", "Ocean Balloon", C, 1000, 1, 2, 1.00f, 0, 1.1f, BalloonAbilityType.ImprovedMagnet, "Tidal Pull", "Coin magnet radius +1.2.",
                    EnvelopeShape.Round, EnvelopePattern.Zigzag, "#2E86DE", "#48DBFB", "#FFFFFF", BalloonOrnament.None),
                Balloon(4, "forest", "Forest Balloon", C, 1500, 1, 2, 1.00f, 0, 1.1f, BalloonAbilityType.WindResistance, "Deep Roots", "Wind pushes you 60% less.",
                    EnvelopeShape.Classic, EnvelopePattern.Leaves, "#27AE60", "#A3E048", "#6D4C2F", BalloonOrnament.Leaves),
                Balloon(5, "magic", "Magic Balloon", R, 2500, 2, 2, 1.05f, 0, 1.1f, BalloonAbilityType.PeriodicShield, "Arcane Ward", "A 2.5s shield at the start and every 18 seconds.",
                    EnvelopeShape.Tall, EnvelopePattern.Stars, "#8E44AD", "#F368E0", "#FFD86B", BalloonOrnament.Stars),
                Balloon(6, "fireball", "Fireball Balloon", R, 4000, 2, 2, 1.05f, 0, 1.1f, BalloonAbilityType.FireResistance, "Flameproof", "Fire hazards cannot damage you.",
                    EnvelopeShape.Teardrop, EnvelopePattern.Flames, "#E74C3C", "#F39C12", "#FFE066", BalloonOrnament.Flames),
                Balloon(7, "ice_crystal", "Ice Crystal Balloon", R, 6000, 3, 3, 1.00f, 0, 1.1f, BalloonAbilityType.EnvironmentalResistance, "Frost Skin", "Wind, ice, gravity and darkness effects are halved.",
                    EnvelopeShape.Round, EnvelopePattern.Crystal, "#74B9FF", "#DFF9FB", "#FFFFFF", BalloonOrnament.Crystals),
                Balloon(8, "cloud_rider", "Cloud Rider Balloon", R, 8000, 3, 3, 1.15f, 0, 1.1f, BalloonAbilityType.Maneuverability, "Sky Glide", "+35% acceleration and +15% top speed.",
                    EnvelopeShape.Wide, EnvelopePattern.Swirl, "#F5F6FA", "#81ECEC", "#74B9FF", BalloonOrnament.None),
                Balloon(9, "iron_shield", "Iron Shield Balloon", E, 12000, 4, 3, 0.95f, 2, 1.1f, BalloonAbilityType.PhysicalResistance, "Iron Hull", "Every second physical hit is deflected.",
                    EnvelopeShape.Round, EnvelopePattern.Checker, "#636E72", "#B2BEC3", "#F1C40F", BalloonOrnament.None),
                Balloon(10, "golden_king", "Golden King Balloon", E, 16000, 4, 3, 1.00f, 0, 1.2f, BalloonAbilityType.CoinRewardBoost, "Royal Treasury", "Level completion coin rewards +50%.",
                    EnvelopeShape.Classic, EnvelopePattern.Bands, "#F1C40F", "#B7950B", "#C0392B", BalloonOrnament.Crown),
                Balloon(11, "rainbow", "Rainbow Balloon", E, 22000, 5, 3, 1.05f, 0, 1.2f, BalloonAbilityType.PowerUpDuration, "Prism Power", "Power-ups last 50% longer.",
                    EnvelopeShape.Round, EnvelopePattern.Rainbow, "#FF6B6B", "#48DBFB", "#FFFFFF", BalloonOrnament.None),
                Balloon(12, "shadow", "Shadow Balloon", E, 28000, 5, 4, 1.10f, 0, 1.2f, BalloonAbilityType.Invisibility, "Shadow Veil", "Tap the ability button to turn invisible for 3s (16s cooldown).",
                    EnvelopeShape.Tall, EnvelopePattern.Swirl, "#2D3436", "#6C5CE7", "#A29BFE", BalloonOrnament.Shadow),
                Balloon(13, "galaxy", "Galaxy Balloon", L, 35000, 6, 4, 1.05f, 1, 1.3f, BalloonAbilityType.SpaceResistance, "Stardust Hull", "Space hazards (asteroids, meteors, drones) cannot damage you.",
                    EnvelopeShape.Round, EnvelopePattern.Galaxy, "#1E1E5A", "#6C5CE7", "#FFFFFF", BalloonOrnament.Stars),
                Balloon(14, "saturn", "Saturn Balloon", L, 45000, 6, 4, 1.05f, 1, 1.3f, BalloonAbilityType.GravityResistance, "Orbital Balance", "Immune to gravity distortion and every second space hit is deflected.",
                    EnvelopeShape.Wide, EnvelopePattern.Bands, "#E0A96D", "#F5E6CA", "#C0392B", BalloonOrnament.Ring),
                Balloon(15, "phoenix", "Phoenix Balloon", L, 55000, 7, 4, 1.10f, 1, 1.3f, BalloonAbilityType.PhoenixRecovery, "Rebirth", "Once per level, survive a fatal hit and recover half your HP.",
                    EnvelopeShape.Teardrop, EnvelopePattern.Flames, "#FF6B35", "#FFD23F", "#C0392B", BalloonOrnament.Wings | BalloonOrnament.Flames),
                Balloon(16, "quantum", "Quantum Balloon", L, 70000, 7, 5, 1.10f, 1, 1.3f, BalloonAbilityType.Teleport, "Quantum Leap", "Tap the ability button to blink 3 units sideways (5s cooldown).",
                    EnvelopeShape.Round, EnvelopePattern.Crystal, "#00CEC9", "#0984E3", "#FFFFFF", BalloonOrnament.Orbit),
                Balloon(17, "angel", "Angel Balloon", M, 90000, 8, 5, 1.10f, 1, 1.4f, BalloonAbilityType.HeavenlyShield, "Heavenly Shield", "A shield blocks one hit and recharges every 14 seconds.",
                    EnvelopeShape.Round, EnvelopePattern.Solid, "#FFFFFF", "#FFEAA7", "#F1C40F", BalloonOrnament.Wings | BalloonOrnament.Halo),
                Balloon(18, "divine_sun", "Divine Sun Balloon", M, 120000, 8, 5, 1.10f, 1, 1.4f, BalloonAbilityType.StrongMagnet, "Solar Pull", "Huge coin magnet (+3 radius) and +25% coins from pickups.",
                    EnvelopeShape.Round, EnvelopePattern.SunRays, "#FDCB6E", "#E17055", "#FFFFFF", BalloonOrnament.SunRays | BalloonOrnament.Crown),
                Balloon(19, "eternal", "Eternal Balloon", M, 160000, 9, 6, 1.15f, 1, 1.4f, BalloonAbilityType.Invulnerability, "Eternity", "Tap the ability button for 4s of invulnerability (18s cooldown).",
                    EnvelopeShape.Tall, EnvelopePattern.Gradient, "#A29BFE", "#FD79A8", "#FFFFFF", BalloonOrnament.Stars | BalloonOrnament.Halo),
                Balloon(20, "heavens_king", "Heaven's King Balloon", M, 220000, 9, 6, 1.20f, 2, 1.5f, BalloonAbilityType.UltimateProtection, "Divine Aegis", "Recharging one-hit shield every 10s, one rebirth per level and +25% coins.",
                    EnvelopeShape.Classic, EnvelopePattern.Bands, "#F9E79F", "#FFFFFF", "#5DADE2", BalloonOrnament.Crown | BalloonOrnament.Wings | BalloonOrnament.Halo),
            };

            // Ability tuning values (kept here so they are easy to edit in the generated assets).
            foreach (var b in list)
            {
                switch (b.ability)
                {
                    case BalloonAbilityType.BonusCoins: b.abilityValue = 0.25f; break;
                    case BalloonAbilityType.ImprovedMagnet: b.abilityValue = 1.2f; break;
                    case BalloonAbilityType.WindResistance: b.abilityValue = 0.6f; break;
                    case BalloonAbilityType.PeriodicShield: b.abilityDuration = 2.5f; b.abilityCooldown = 18f; break;
                    case BalloonAbilityType.EnvironmentalResistance: b.abilityValue = 0.5f; break;
                    case BalloonAbilityType.Maneuverability: b.abilityValue = 0.35f; break;
                    case BalloonAbilityType.CoinRewardBoost: b.abilityValue = 0.5f; break;
                    case BalloonAbilityType.PowerUpDuration: b.abilityValue = 0.5f; break;
                    case BalloonAbilityType.Invisibility: b.abilityDuration = 3f; b.abilityCooldown = 16f; break;
                    case BalloonAbilityType.PhoenixRecovery: b.abilityValue = 0.5f; break;
                    case BalloonAbilityType.Teleport: b.abilityValue = 3f; b.abilityCooldown = 5f; break;
                    case BalloonAbilityType.HeavenlyShield: b.abilityCooldown = 14f; break;
                    case BalloonAbilityType.StrongMagnet: b.abilityValue = 3f; break;
                    case BalloonAbilityType.Invulnerability: b.abilityDuration = 4f; b.abilityCooldown = 18f; break;
                    case BalloonAbilityType.UltimateProtection: b.abilityCooldown = 10f; b.abilityValue = 0.25f; break;
                }
            }
            return list;
        }

        // ------------------------------------------------------------------ Power-ups

        static List<PowerUpData> CreatePowerUps()
        {
            PowerUpData P(PowerUpType t, string name, string desc, float dur, float mag, string color)
            {
                var p = Create<PowerUpData>("PowerUp_" + t);
                p.type = t; p.displayName = name; p.description = desc; p.duration = dur; p.magnitude = mag; p.color = Hex(color);
                return p;
            }
            return new List<PowerUpData>
            {
                P(PowerUpType.CoinMagnet, "Coin Magnet", "Attracts nearby coins for 10 seconds.", 10f, 4.5f, "#E84393"),
                P(PowerUpType.EnergyShield, "Energy Shield", "Blocks all collisions for 8 seconds.", 8f, 1f, "#00CEC9"),
                P(PowerUpType.SkyRocket, "Sky Rocket", "Boosts your ascent for 5 seconds.", 5f, 1.9f, "#FF7675"),
                P(PowerUpType.ExtraLife, "Extra Life", "Restores one durability point.", 0f, 1f, "#55EFC4"),
                P(PowerUpType.DoubleCoins, "Double Coins", "Doubles collected coins for 15 seconds.", 15f, 2f, "#FDCB6E"),
            };
        }

        // ------------------------------------------------------------------ Obstacles

        static ObstacleData Obstacle(Dictionary<string, ObstacleData> map, string id, string name, ObstacleVisual visual, DamageType type,
            MovementPattern movement, float speed, float radius, string c1, string c2, int worldMask)
        {
            var o = Create<ObstacleData>("Obstacle_" + id);
            o.id = id; o.displayName = name; o.visual = visual; o.damageType = type; o.movement = movement;
            o.speed = speed; o.radius = radius; o.primaryColor = Hex(c1); o.secondaryColor = Hex(c2); o.worldMask = worldMask;
            map[id] = o;
            return o;
        }

        static ObstacleData Box(ObstacleData o, float w, float h)
        {
            o.shape = ColliderShape.Box;
            o.boxSize = new Vector2(w, h);
            return o;
        }

        static Dictionary<string, ObstacleData> CreateObstacles()
        {
            var m = new Dictionary<string, ObstacleData>();
            const DamageType P = DamageType.Physical, F = DamageType.Fire, I = DamageType.Ice, El = DamageType.Electric, S = DamageType.Space, Mg = DamageType.Magic;
            int all = ~0;

            Obstacle(m, "small_bird", "Small Bird", ObstacleVisual.SmallBird, P, MovementPattern.CrossHorizontal, 2.6f, 0.45f, "#8D6E63", "#FFF3E0", WorldBits(1, 2, 3));
            var eagle = Obstacle(m, "eagle", "Eagle", ObstacleVisual.Eagle, P, MovementPattern.Homing, 3.0f, 0.7f, "#5D4037", "#FFFFFF", WorldBits(1, 2));
            eagle.homingStrength = 1.4f; eagle.scale = 1.25f;
            var swarm = Obstacle(m, "butterfly_swarm", "Butterfly Swarm", ObstacleVisual.Butterfly, P, MovementPattern.Swarm, 1.4f, 0.33f, "#FD79A8", "#FDCB6E", WorldBits(3));
            swarm.groupMin = 4; swarm.groupMax = 6; swarm.amplitude = 1.1f;
            var rock = Obstacle(m, "falling_rock", "Falling Rock", ObstacleVisual.Rock, P, MovementPattern.Falling, 2.4f, 0.6f, "#7F8C8D", "#95A5A6", WorldBits(2));
            rock.spin = 90f; rock.destroyOnHit = true;
            var cliff = Box(Obstacle(m, "cliff_passage", "Narrow Passage", ObstacleVisual.CliffWall, P, MovementPattern.Static, 0f, 0.8f, "#8D8D8D", "#5E5E5E", WorldBits(2)), 4f, 1.6f);
            cliff.fullRowWithGap = true; cliff.gapWidth = 3.4f;
            var wind = Box(Obstacle(m, "wind_current", "Wind Current", ObstacleVisual.WindCurrent, P, MovementPattern.Static, 0f, 1f, "#DFF9FB", "#C7ECEE", WorldBits(1, 2, 3, 5, 10)), 3.2f, 3.6f);
            wind.harmless = true; wind.pushForce = 7f;
            var platform = Box(Obstacle(m, "moving_platform", "Moving Platform", ObstacleVisual.MovingPlatform, P, MovementPattern.Patrol, 1.6f, 0.5f, "#55EFC4", "#81ECEC", WorldBits(3, 8)), 2.4f, 0.6f);
            platform.amplitude = 2.0f;
            var dark = Obstacle(m, "dark_cloud", "Storm Cloud", ObstacleVisual.DarkCloud, El, MovementPattern.Patrol, 0.8f, 0.85f, "#2D3436", "#636E72", WorldBits(4));
            dark.amplitude = 1.0f;
            var lightning = Box(Obstacle(m, "lightning_cloud", "Lightning Cloud", ObstacleVisual.LightningCloud, El, MovementPattern.Static, 0f, 0.5f, "#FFF200", "#3D3D5C", WorldBits(4, 10)), 0.8f, 6.5f);
            lightning.timed = true; lightning.warningTime = 1.1f; lightning.activeTime = 0.6f; lightning.cooldownTime = 2.2f;
            Obstacle(m, "electric_bird", "Electric Bird", ObstacleVisual.ElectricBird, El, MovementPattern.CrossHorizontal, 3.3f, 0.5f, "#0984E3", "#FFF200", WorldBits(4));
            var frozenBird = Obstacle(m, "frozen_bird", "Frozen Bird", ObstacleVisual.FrozenBird, I, MovementPattern.Swarm, 1.8f, 0.42f, "#74B9FF", "#FFFFFF", WorldBits(5));
            frozenBird.groupMin = 3; frozenBird.groupMax = 5; frozenBird.amplitude = 1.0f;
            var snowball = Obstacle(m, "snowball", "Snowball", ObstacleVisual.Snowball, I, MovementPattern.Falling, 3.0f, 0.5f, "#FFFFFF", "#DFE6E9", WorldBits(5));
            snowball.spin = 120f; snowball.destroyOnHit = true;
            var iceCrystal = Obstacle(m, "ice_crystal", "Ice Crystal", ObstacleVisual.IceCrystal, I, MovementPattern.VerticalBob, 0.6f, 0.62f, "#A5F3FF", "#74B9FF", WorldBits(5));
            iceCrystal.spin = 45f; iceCrystal.amplitude = 0.5f;
            Obstacle(m, "burning_bird", "Burning Bird", ObstacleVisual.BurningBird, F, MovementPattern.CrossHorizontal, 3.6f, 0.5f, "#E17055", "#FDCB6E", WorldBits(6));
            var fireball = Obstacle(m, "fireball", "Fireball", ObstacleVisual.Fireball, F, MovementPattern.Diagonal, 3.8f, 0.48f, "#FF7675", "#FDCB6E", WorldBits(6));
            fireball.destroyOnHit = true;
            var lava = Box(Obstacle(m, "lava_column", "Lava Column", ObstacleVisual.LavaColumn, F, MovementPattern.Static, 0f, 0.5f, "#FF5E13", "#FFC048", WorldBits(6)), 1.0f, 5f);
            lava.timed = true; lava.warningTime = 1.2f; lava.activeTime = 1.2f; lava.cooldownTime = 1.9f;
            var asteroid = Obstacle(m, "asteroid", "Small Asteroid", ObstacleVisual.Asteroid, S, MovementPattern.Diagonal, 1.4f, 0.55f, "#6D6875", "#B5838D", WorldBits(7));
            asteroid.spin = 60f;
            var meteor = Obstacle(m, "meteorite", "Meteorite", ObstacleVisual.Meteorite, S, MovementPattern.Diagonal, 6.0f, 0.45f, "#E84118", "#FBC531", WorldBits(7, 10));
            meteor.destroyOnHit = true;
            var drone = Obstacle(m, "alien_drone", "Alien Drone", ObstacleVisual.AlienDrone, S, MovementPattern.Homing, 2.4f, 0.55f, "#A3CB38", "#C4E538", WorldBits(7));
            drone.homingStrength = 2.0f;
            var temple = Box(Obstacle(m, "temple_block", "Temple Block", ObstacleVisual.TempleBlock, P, MovementPattern.Patrol, 1.2f, 0.6f, "#F6E58D", "#BA8B02", WorldBits(8)), 1.8f, 1.2f);
            temple.amplitude = 1.8f;
            var barrier = Box(Obstacle(m, "magic_barrier", "Magical Barrier", ObstacleVisual.MagicBarrier, Mg, MovementPattern.Static, 0f, 0.3f, "#E056FD", "#F9CA24", WorldBits(8, 10)), 4.2f, 0.45f);
            barrier.timed = true; barrier.warningTime = 0.9f; barrier.activeTime = 1.6f; barrier.cooldownTime = 1.6f;
            var guardian = Obstacle(m, "guardian_orb", "Cloud Guardian", ObstacleVisual.GuardianOrb, Mg, MovementPattern.Homing, 1.8f, 0.6f, "#F9CA24", "#FFFFFF", WorldBits(8, 10));
            guardian.homingStrength = 1.1f;
            var debris = Obstacle(m, "temple_debris", "Temple Debris", ObstacleVisual.Rock, P, MovementPattern.Falling, 3.0f, 0.55f, "#E1B12C", "#FBC531", WorldBits(8));
            debris.spin = 80f; debris.destroyOnHit = true;
            Obstacle(m, "celestial_bird", "Celestial Bird", ObstacleVisual.CelestialBird, Mg, MovementPattern.CrossHorizontal, 3.7f, 0.5f, "#DFE6E9", "#81ECEC", WorldBits(9, 10));
            var orb = Obstacle(m, "energy_orb", "Energy Orb", ObstacleVisual.EnergyOrb, Mg, MovementPattern.Circular, 1.8f, 0.45f, "#A29BFE", "#FFFFFF", WorldBits(9, 10));
            orb.amplitude = 1.4f;
            var crystalBarrier = Box(Obstacle(m, "crystal_barrier", "Crystal Barrier", ObstacleVisual.CrystalBarrier, Mg, MovementPattern.Patrol, 1.2f, 0.4f, "#81ECEC", "#A29BFE", WorldBits(9)), 2.6f, 0.5f);
            crystalBarrier.amplitude = 1.5f;
            var shard = Obstacle(m, "crystal_shard", "Crystal Shard", ObstacleVisual.CrystalShard, Mg, MovementPattern.Falling, 3.6f, 0.4f, "#81ECEC", "#DFE6E9", WorldBits(9));
            shard.spin = 150f; shard.destroyOnHit = true;
            var gate = Box(Obstacle(m, "golden_gate", "Golden Gate", ObstacleVisual.GoldenGate, Mg, MovementPattern.Static, 0f, 0.6f, "#F9CA24", "#FFF7D6", WorldBits(10)), 4f, 1.2f);
            gate.fullRowWithGap = true; gate.gapWidth = 3.2f;

            // Boss projectiles and boss-only hazards
            ObstacleData Projectile(string id, string name, ObstacleVisual v, DamageType t, float r, string a, string b)
            {
                var p = Obstacle(m, id, name, v, t, MovementPattern.Falling, 5f, r, a, b, all);
                p.destroyOnHit = true; p.spin = 120f;
                return p;
            }
            Projectile("feather", "Guardian Feather", ObstacleVisual.Feather, P, 0.35f, "#F6E58D", "#6AB04C");
            Projectile("boss_rock", "Boulder", ObstacleVisual.Rock, P, 0.75f, "#636E72", "#B2BEC3");
            Projectile("pollen_orb", "Pollen Orb", ObstacleVisual.EnergyOrb, Mg, 0.38f, "#FD79A8", "#FFEAA7");
            Projectile("spark", "Storm Spark", ObstacleVisual.EnergyOrb, El, 0.38f, "#FFF200", "#74B9FF");
            Projectile("ice_shard", "Ice Shard", ObstacleVisual.CrystalShard, I, 0.38f, "#DFF9FB", "#74B9FF");
            Projectile("boss_fireball", "Phoenix Flame", ObstacleVisual.Fireball, F, 0.45f, "#FF5E13", "#FFC048");
            Projectile("boss_meteor", "Cosmic Meteor", ObstacleVisual.Meteorite, S, 0.5f, "#9C88FF", "#E84118");
            Projectile("cosmic_orb", "Void Orb", ObstacleVisual.EnergyOrb, S, 0.4f, "#8C7AE6", "#00A8FF");
            Projectile("golden_orb", "Golden Orb", ObstacleVisual.GuardianOrb, Mg, 0.42f, "#F9CA24", "#FFFFFF");
            Projectile("light_orb", "Light Orb", ObstacleVisual.EnergyOrb, Mg, 0.4f, "#FFFFFF", "#F9E79F");

            ObstacleData Column(string id, string name, ObstacleVisual v, DamageType t, string a, string b)
            {
                var c = Box(Obstacle(m, id, name, v, t, MovementPattern.Static, 0f, 0.5f, a, b, all), 1.1f, 30f);
                c.timed = true; c.warningTime = 1f; c.activeTime = 0.9f; c.cooldownTime = 999f;
                return c;
            }
            Column("storm_column", "Thunder Strike", ObstacleVisual.LightBeam, El, "#FFF200", "#74B9FF");
            Column("lava_burst", "Lava Burst", ObstacleVisual.LightBeam, F, "#FF5E13", "#FFC048");
            Column("light_column", "Pillar of Light", ObstacleVisual.LightBeam, Mg, "#FFFFFF", "#F9E79F");
            Column("rock_slide", "Rock Slide", ObstacleVisual.LightBeam, P, "#B2BEC3", "#636E72");

            var iceWall = Box(Obstacle(m, "ice_wall", "Ice Wall", ObstacleVisual.CliffWall, I, MovementPattern.Static, 0f, 0.8f, "#C7ECEE", "#74B9FF", all), 4f, 1.2f);
            iceWall.fullRowWithGap = true; iceWall.gapWidth = 3.3f;
            var flameWall = Box(Obstacle(m, "flame_wall", "Wall of Fire", ObstacleVisual.CliffWall, F, MovementPattern.Static, 0f, 0.8f, "#E17055", "#FDCB6E", all), 4f, 1.0f);
            flameWall.fullRowWithGap = true; flameWall.gapWidth = 3.3f;
            return m;
        }

        // ------------------------------------------------------------------ Bosses

        static BossAttack Attack(BossAttackType type, ObstacleData projectile, int count, float cooldown, float speed, float telegraph = 0.9f)
        {
            return new BossAttack { type = type, projectile = projectile, count = count, cooldown = cooldown, speed = speed, telegraph = telegraph };
        }

        static BossPhase Phase(string name, float threshold, BossMovement movement, float moveSpeed, params BossAttack[] attacks)
        {
            return new BossPhase { name = name, healthThreshold = threshold, movement = movement, moveSpeed = moveSpeed, attacks = new List<BossAttack>(attacks) };
        }

        static List<BossData> CreateBosses(Dictionary<string, ObstacleData> o)
        {
            BossData B(int world, string id, string name, string title, BossVisual visual, string c1, string c2, string glow, float duration, float scale)
            {
                var b = Create<BossData>("Boss_" + world.ToString("00") + "_" + id);
                b.id = id; b.bossName = name; b.title = title; b.visual = visual;
                b.primaryColor = Hex(c1); b.secondaryColor = Hex(c2); b.glowColor = Hex(glow);
                b.encounterDuration = duration; b.modelScale = scale;
                b.rewardCoins = 500; b.rewardGems = 5;
                b.crystalDamage = 0.06f; b.crystalInterval = 5.5f;
                return b;
            }
            var list = new List<BossData>();
            const BossAttackType Fan = BossAttackType.ProjectileFan, Aim = BossAttackType.AimedVolley, Col = BossAttackType.ColumnStrike,
                Rain = BossAttackType.RainBarrage, Sum = BossAttackType.Summon, Wall = BossAttackType.WallWithGap, Spiral = BossAttackType.SpiralOrbs;

            var b1 = B(1, "valley_guardian", "Valley Guardian", "Great Eagle of the Valley", BossVisual.ValleyGuardian, "#6AB04C", "#F6E58D", "#BADC58", 40f, 1f);
            b1.phases.Add(Phase("Gliding Watch", 1f, BossMovement.HorizontalSweep, 2.0f, Attack(Fan, o["feather"], 5, 3.4f, 4.5f)));
            b1.phases.Add(Phase("Call of the Flock", 0.66f, BossMovement.HorizontalSweep, 2.8f, Attack(Fan, o["feather"], 5, 3.2f, 5f), Attack(Sum, o["small_bird"], 2, 5f, 0f)));
            b1.phases.Add(Phase("Feather Storm", 0.33f, BossMovement.FigureEight, 2.4f, Attack(Rain, o["feather"], 6, 3.4f, 5f), Attack(Fan, o["feather"], 6, 4f, 5.5f)));
            list.Add(b1);

            var b2 = B(2, "mountain_king", "Mountain King", "Stone Crown of the Peaks", BossVisual.MountainKing, "#7F8C8D", "#F1C40F", "#E67E22", 44f, 1.1f);
            b2.phases.Add(Phase("Rumbling Throne", 1f, BossMovement.Hover, 1.2f, Attack(Rain, o["boss_rock"], 5, 3.2f, 4.5f)));
            b2.phases.Add(Phase("Landslide", 0.66f, BossMovement.TrackPlayer, 1.6f, Attack(Col, o["rock_slide"], 2, 3.6f, 0f, 1.1f), Attack(Rain, o["boss_rock"], 4, 4f, 5f)));
            b2.phases.Add(Phase("Mountain's Wrath", 0.33f, BossMovement.HorizontalSweep, 2.2f, Attack(Wall, o["cliff_passage"], 1, 4.2f, 0f), Attack(Fan, o["boss_rock"], 4, 3.6f, 5f)));
            list.Add(b2);

            var b3 = B(3, "butterfly_queen", "Sky Butterfly Queen", "Monarch of Rainbow Skies", BossVisual.ButterflyQueen, "#FD79A8", "#74B9FF", "#FFEAA7", 46f, 1f);
            b3.phases.Add(Phase("Royal Dance", 1f, BossMovement.FigureEight, 2.2f, Attack(Sum, o["butterfly_swarm"], 1, 4.5f, 0f), Attack(Fan, o["pollen_orb"], 5, 3.6f, 4.5f)));
            b3.phases.Add(Phase("Pollen Spiral", 0.66f, BossMovement.FigureEight, 2.6f, Attack(Spiral, o["pollen_orb"], 8, 4.2f, 3.2f)));
            b3.phases.Add(Phase("Rainbow Tempest", 0.33f, BossMovement.HorizontalSweep, 3f, Attack(Spiral, o["pollen_orb"], 9, 4.6f, 3.5f), Attack(Sum, o["butterfly_swarm"], 1, 5.5f, 0f), Attack(Rain, o["pollen_orb"], 5, 4f, 4.5f)));
            list.Add(b3);

            var b4 = B(4, "thunder_dragon", "Thunder Dragon", "Lord of the Storm", BossVisual.ThunderDragon, "#273C75", "#FBC531", "#00A8FF", 50f, 1.15f);
            b4.phases.Add(Phase("Storm Flight", 1f, BossMovement.HorizontalSweep, 2.4f, Attack(Col, o["storm_column"], 2, 3.2f, 0f, 1.0f)));
            b4.phases.Add(Phase("Electric Brood", 0.66f, BossMovement.HorizontalSweep, 3.4f, Attack(Col, o["storm_column"], 2, 3.4f, 0f, 0.95f), Attack(Sum, o["electric_bird"], 2, 4.5f, 0f)));
            b4.phases.Add(Phase("Great Tempest", 0.33f, BossMovement.FigureEight, 3f, Attack(Col, o["storm_column"], 3, 3.6f, 0f, 1.0f), Attack(Rain, o["spark"], 6, 3.4f, 5f)));
            list.Add(b4);

            var b5 = B(5, "ice_guardian", "Ice Guardian", "Warden of the Frozen Heaven", BossVisual.IceGuardian, "#74B9FF", "#DFF9FB", "#A5F3FF", 52f, 1.1f);
            b5.phases.Add(Phase("Frost Gaze", 1f, BossMovement.Hover, 1.4f, Attack(Fan, o["ice_shard"], 5, 3f, 5f)));
            b5.phases.Add(Phase("Blizzard", 0.66f, BossMovement.HorizontalSweep, 2f, Attack(Rain, o["snowball"], 6, 3.2f, 4f), Attack(Fan, o["ice_shard"], 5, 4f, 5.5f)));
            b5.phases.Add(Phase("Glacial Wall", 0.33f, BossMovement.TrackPlayer, 2f, Attack(Wall, o["ice_wall"], 1, 4f, 0f), Attack(Aim, o["ice_shard"], 3, 3.4f, 6f)));
            list.Add(b5);

            var b6 = B(6, "fire_phoenix", "Fire Phoenix", "Eternal Flame of the Volcano", BossVisual.FirePhoenix, "#E84118", "#FBC531", "#FF9F1A", 54f, 1.15f);
            b6.phases.Add(Phase("Blazing Wings", 1f, BossMovement.HorizontalSweep, 2.6f, Attack(Aim, o["boss_fireball"], 3, 2.8f, 6f)));
            b6.phases.Add(Phase("Eruption", 0.66f, BossMovement.FigureEight, 2.6f, Attack(Col, o["lava_burst"], 2, 3.4f, 0f, 1.05f), Attack(Aim, o["boss_fireball"], 3, 3.6f, 6f)));
            b6.phases.Add(Phase("Rebirth Inferno", 0.33f, BossMovement.HorizontalSweep, 3.4f, Attack(Rain, o["boss_fireball"], 7, 3.2f, 5f), Attack(Wall, o["flame_wall"], 1, 5f, 0f)));
            list.Add(b6);

            var b7 = B(7, "cosmic_guardian", "Cosmic Guardian", "Eye Between the Stars", BossVisual.CosmicGuardian, "#192A56", "#9C88FF", "#00D2D3", 56f, 1.1f);
            b7.phases.Add(Phase("Starfall", 1f, BossMovement.Teleport, 2f, Attack(Rain, o["boss_meteor"], 6, 3f, 5.5f)));
            b7.phases.Add(Phase("Drone Fleet", 0.66f, BossMovement.Teleport, 2.4f, Attack(Sum, o["alien_drone"], 2, 5f, 0f), Attack(Fan, o["cosmic_orb"], 6, 3.6f, 4.5f)));
            b7.phases.Add(Phase("Event Horizon", 0.33f, BossMovement.FigureEight, 2.8f, Attack(Spiral, o["cosmic_orb"], 10, 4.2f, 3.4f), Attack(Rain, o["boss_meteor"], 5, 3.8f, 6f)));
            list.Add(b7);

            var b8 = B(8, "golden_titan", "Golden Titan", "Colossus of the Golden Temples", BossVisual.GoldenTitan, "#F9CA24", "#B7950B", "#FFF7D6", 58f, 1.25f);
            b8.phases.Add(Phase("Temple Guard", 1f, BossMovement.Hover, 1.4f, Attack(Wall, o["golden_gate"], 1, 4f, 0f), Attack(Fan, o["golden_orb"], 5, 3.6f, 4.5f)));
            b8.phases.Add(Phase("Golden Barrage", 0.66f, BossMovement.TrackPlayer, 1.8f, Attack(Fan, o["golden_orb"], 7, 3f, 5f), Attack(Rain, o["temple_debris"], 5, 3.6f, 4.5f)));
            b8.phases.Add(Phase("Collapse", 0.33f, BossMovement.HorizontalSweep, 2.2f, Attack(Wall, o["golden_gate"], 1, 4.2f, 0f), Attack(Rain, o["temple_debris"], 6, 3.2f, 5f), Attack(Aim, o["golden_orb"], 3, 3.8f, 6f)));
            list.Add(b8);

            var b9 = B(9, "celestial_guardian", "Celestial Guardian", "Keeper of the Crystal Light", BossVisual.CelestialGuardian, "#A29BFE", "#81ECEC", "#FFFFFF", 60f, 1.15f);
            b9.phases.Add(Phase("Orbit of Light", 1f, BossMovement.FigureEight, 2f, Attack(Spiral, o["light_orb"], 8, 3.8f, 3.3f)));
            b9.phases.Add(Phase("Crystal Storm", 0.66f, BossMovement.HorizontalSweep, 2.6f, Attack(Rain, o["crystal_shard"], 7, 3f, 5f), Attack(Col, o["light_column"], 2, 4f, 0f, 1f)));
            b9.phases.Add(Phase("Light and Shadow", 0.33f, BossMovement.Teleport, 2.8f, Attack(Col, o["light_column"], 3, 3.6f, 0f, 1f), Attack(Spiral, o["light_orb"], 10, 4.4f, 3.6f)));
            list.Add(b9);

            var b10 = B(10, "heavens_guardian", "Heaven's Guardian", "Seraph of the Final Gate", BossVisual.HeavensGuardian, "#FFFFFF", "#F9CA24", "#FFF7D6", 70f, 1.3f);
            b10.phases.Add(Phase("Judgement", 1f, BossMovement.HorizontalSweep, 2.2f, Attack(Fan, o["light_orb"], 6, 3.2f, 5f), Attack(Col, o["light_column"], 2, 4.2f, 0f, 1f)));
            b10.phases.Add(Phase("Heavenly Host", 0.75f, BossMovement.FigureEight, 2.6f, Attack(Sum, o["celestial_bird"], 2, 4.5f, 0f), Attack(Spiral, o["light_orb"], 9, 4.2f, 3.4f)));
            b10.phases.Add(Phase("Golden Gates", 0.5f, BossMovement.TrackPlayer, 2.2f, Attack(Wall, o["golden_gate"], 1, 3.8f, 0f), Attack(Aim, o["light_orb"], 3, 3.4f, 6f)));
            b10.phases.Add(Phase("Final Ascent", 0.25f, BossMovement.HorizontalSweep, 3.2f, Attack(Col, o["light_column"], 3, 3.6f, 0f, 1f), Attack(Spiral, o["light_orb"], 10, 4.6f, 3.6f), Attack(Rain, o["boss_meteor"], 5, 4.2f, 5.5f)));
            b10.rewardCoins = 2000; b10.rewardGems = 25; b10.crystalInterval = 6f;
            list.Add(b10);
            return list;
        }

        // ------------------------------------------------------------------ Worlds

        static List<WorldData> CreateWorlds(List<BossData> bosses)
        {
            WorldData W(int n, string name, WorldTheme theme, string top, string bottom, string cloud, string ground, string accent, string ambient, string light,
                WeatherEffect weather, int tempo, int root, MusicScale scale, Waveform lead, float perc, float bright, string desc)
            {
                var w = Create<WorldData>("World_" + n.ToString("00") + "_" + theme);
                w.worldNumber = n; w.worldName = name; w.theme = theme; w.description = desc;
                w.skyTop = Hex(top); w.skyBottom = Hex(bottom); w.cloudColor = Hex(cloud); w.groundColor = Hex(ground);
                w.accentColor = Hex(accent); w.ambientColor = Hex(ambient); w.lightColor = Hex(light);
                w.rimColor = Color.Lerp(Hex(bottom), Color.white, 0.5f);
                w.weather = weather;
                w.music = new MusicStyle { tempo = tempo, rootNote = root, scale = scale, lead = lead, percussion = perc, brightness = bright, seed = n * 7919 };
                w.boss = bosses[n - 1];
                w.completionCoins = 1000; w.completionGems = 10 + (n == 10 ? 40 : 0);
                return w;
            }
            return new List<WorldData>
            {
                W(1, "Green Valley", WorldTheme.GreenValley, "#4FA3F7", "#CDEBFF", "#FFFFFF", "#6AB04C", "#F6E58D", "#8FA7BF", "#FFF6E0", WeatherEffect.Pollen, 96, 60, MusicScale.Major, Waveform.Triangle, 0.35f, 0.6f, "Peaceful green landscapes, floating grass islands and fluffy clouds."),
                W(2, "Mountain Kingdom", WorldTheme.MountainKingdom, "#5B7DB1", "#F8C9A9", "#FDF2F8", "#95A5A6", "#E67E22", "#8C8FA8", "#FFE8D0", WeatherEffect.None, 104, 57, MusicScale.Dorian, Waveform.SoftSquare, 0.45f, 0.5f, "Tall peaks, steep cliffs and drifting rocks."),
                W(3, "Rainbow Skies", WorldTheme.RainbowSkies, "#7ED6DF", "#FFD3E8", "#FFFFFF", "#7BED9F", "#FF6B81", "#A6A0C8", "#FFF7FB", WeatherEffect.Petals, 112, 62, MusicScale.Lydian, Waveform.Bell, 0.5f, 0.8f, "Colorful floating islands, rainbows and waterfalls."),
                W(4, "Thunderstorm", WorldTheme.Thunderstorm, "#1E2235", "#4B5574", "#5D6584", "#2F3640", "#FBC531", "#3E4560", "#C7D3FF", WeatherEffect.Rain, 124, 55, MusicScale.Minor, Waveform.SoftSquare, 0.75f, 0.35f, "Dark clouds, lightning, rain and powerful storms."),
                W(5, "Frozen Heaven", WorldTheme.FrozenHeaven, "#6AB7FF", "#EAF6FF", "#F5FBFF", "#DFF9FB", "#74B9FF", "#9DB8D6", "#F0F8FF", WeatherEffect.Snow, 92, 64, MusicScale.Pentatonic, Waveform.Bell, 0.3f, 0.7f, "Snow, frozen islands and shimmering ice crystals."),
                W(6, "Fire Kingdom", WorldTheme.FireKingdom, "#5A1A1A", "#FF8C42", "#FFB27A", "#3D1F1F", "#FF5E13", "#8A4A3A", "#FFD2A6", WeatherEffect.Embers, 132, 52, MusicScale.HarmonicMinor, Waveform.SoftSquare, 0.85f, 0.45f, "Volcanoes, lava rivers and burning orange clouds."),
                W(7, "Cosmic Journey", WorldTheme.CosmicJourney, "#05051A", "#2C1A5A", "#4B3B8C", "#1E1E3F", "#00D2D3", "#3A3A6A", "#D6E4FF", WeatherEffect.Stars, 100, 57, MusicScale.Dorian, Waveform.Sine, 0.4f, 0.55f, "Outer space, planets, galaxies and asteroid fields."),
                W(8, "Golden Clouds", WorldTheme.GoldenClouds, "#F5B041", "#FFF3D6", "#FFF8E7", "#F6E58D", "#B7950B", "#C7A97A", "#FFF1CC", WeatherEffect.GoldDust, 108, 65, MusicScale.Major, Waveform.Bell, 0.45f, 0.75f, "Golden floating temples and ancient structures."),
                W(9, "Celestial Realm", WorldTheme.CelestialRealm, "#3B2C85", "#C3B1E1", "#E9E1FF", "#A29BFE", "#81ECEC", "#8B80C8", "#F3EEFF", WeatherEffect.LightMotes, 98, 62, MusicScale.Lydian, Waveform.Sine, 0.35f, 0.7f, "Crystal islands, heavenly light and magical energy."),
                W(10, "Heaven's Gate", WorldTheme.HeavensGate, "#FFE9A8", "#FFFFFF", "#FFFFFF", "#FFF7D6", "#F9CA24", "#D9CBA8", "#FFFBEA", WeatherEffect.GoldDust, 88, 60, MusicScale.Major, Waveform.Bell, 0.3f, 0.9f, "Golden gates, magnificent castles and divine light."),
            };
        }

        // ------------------------------------------------------------------ Levels

        /// <summary>Per-level design row. Obstacles are given as "id:weight" pairs.</summary>
        struct LevelSpec
        {
            public string Name;
            public string Hint;
            public string[] Obstacles;
            public WindMode Wind;
            public float WindStrength;
            public float Darkness;
            public float Slippery;
            public float Gravity;
            public bool LightShadow;
            public int ExtraCoins;
            public int ExtraSpecial;

            public LevelSpec(string name, string hint, params string[] obstacles)
            {
                Name = name; Hint = hint; Obstacles = obstacles;
                Wind = WindMode.None; WindStrength = 0; Darkness = 0; Slippery = 0; Gravity = 0; LightShadow = false; ExtraCoins = 0; ExtraSpecial = 0;
            }

            public LevelSpec With(WindMode wind = WindMode.None, float windStrength = 0f, float darkness = 0f, float slippery = 0f, float gravity = 0f, bool lightShadow = false, int extraCoins = 0, int extraSpecial = 0)
            {
                var s = this;
                s.Wind = wind; s.WindStrength = windStrength; s.Darkness = darkness; s.Slippery = slippery; s.Gravity = gravity;
                s.LightShadow = lightShadow; s.ExtraCoins = extraCoins; s.ExtraSpecial = extraSpecial;
                return s;
            }
        }

        static LevelSpec[][] LevelTable()
        {
            LevelSpec L(string name, string hint, params string[] obstacles) => new LevelSpec(name, hint, obstacles);
            return new[]
            {
                new[] // World 1 - Green Valley
                {
                    L("First Flight", "Drag anywhere to steer your balloon. Collect the coins!").With(extraCoins: 1),
                    L("Bird Crossing", "Birds fly across the sky. Watch their path and slip past.", "small_bird:1"),
                    L("Coin Trail", "Follow the coin trails and grab every star coin.", "small_bird:1").With(extraCoins: 3, extraSpecial: 2),
                    L("Forest Wind", "Wind currents push you sideways. Steer against them!", "small_bird:1", "wind_current:0.8").With(WindMode.Gusts, 1.2f),
                    L("Eagle Attack", "Eagles dive towards you. Move late to dodge them.", "small_bird:1", "eagle:0.9@0.1"),
                    L("Valley Guardian", "Survive the Guardian's attacks. Collect Sky Crystals to weaken it!", "small_bird:1", "eagle:0.4"),
                },
                new[] // World 2 - Mountain Kingdom
                {
                    L("Mountain Entrance", "Welcome to the peaks. The air gets busier up here.", "small_bird:1", "eagle:0.5"),
                    L("Falling Rocks", "Rocks tumble down from the cliffs. Keep moving!", "falling_rock:1.2", "small_bird:0.5"),
                    L("Narrow Passage", "Fly through the gaps between the cliffs.", "cliff_passage:0.8", "falling_rock:0.6"),
                    L("Strong Winds", "Mountain gales are strong. Fight the current!", "wind_current:1", "falling_rock:0.8", "small_bird:0.5").With(WindMode.Gusts, 2.2f),
                    L("Eagle Territory", "This is eagle land. Stay alert.", "eagle:1.2", "falling_rock:0.7", "cliff_passage:0.4"),
                    L("Mountain King", "The Mountain King awakens!", "falling_rock:1", "eagle:0.5"),
                },
                new[] // World 3 - Rainbow Skies
                {
                    L("Rainbow Gate", "Colorful skies, colorful dangers.", "small_bird:1", "butterfly_swarm:0.4"),
                    L("Butterfly Swarm", "Butterflies travel in swarms. Find the gaps.", "butterfly_swarm:1.2", "small_bird:0.4"),
                    L("Moving Platforms", "Floating platforms slide back and forth.", "moving_platform:1.1", "butterfly_swarm:0.5"),
                    L("Wind Tunnels", "The wind changes direction without warning!", "wind_current:1", "moving_platform:0.8", "butterfly_swarm:0.5").With(WindMode.Unpredictable, 2.0f),
                    L("Rainbow Storm", "Survive the storm of colors.", "butterfly_swarm:1", "moving_platform:0.8", "small_bird:0.7", "wind_current:0.4").With(WindMode.Unpredictable, 1.6f),
                    L("Sky Butterfly Queen", "The Queen of the skies descends!", "butterfly_swarm:1", "moving_platform:0.4"),
                },
                new[] // World 4 - Thunderstorm
                {
                    L("Dark Clouds", "Storm clouds crackle with electricity.", "dark_cloud:1", "small_bird:0.4").With(darkness: 0.2f),
                    L("Lightning Strikes", "Lightning flashes before it strikes. Move away from the glow!", "lightning_cloud:1.1", "dark_cloud:0.5").With(darkness: 0.25f),
                    L("Electric Birds", "Charged birds zip across the storm.", "electric_bird:1.1", "lightning_cloud:0.6").With(darkness: 0.3f),
                    L("Storm Corridors", "Weave through corridors of thunderclouds.", "dark_cloud:1", "lightning_cloud:0.9", "electric_bird:0.5").With(WindMode.Gusts, 1.4f, darkness: 0.35f),
                    L("Thunder Survival", "Hold on through the heart of the storm!", "lightning_cloud:1", "electric_bird:1", "dark_cloud:0.7").With(WindMode.Unpredictable, 1.5f, darkness: 0.4f),
                    L("Thunder Dragon", "The Thunder Dragon rules this storm!", "electric_bird:0.8", "dark_cloud:0.6").With(darkness: 0.3f),
                },
                new[] // World 5 - Frozen Heaven
                {
                    L("Frozen Entrance", "Icy air makes your balloon slide a little.", "frozen_bird:0.6", "ice_crystal:0.8").With(slippery: 0.25f),
                    L("Snowball Rain", "Snowballs fall from the frozen clouds.", "snowball:1.2", "ice_crystal:0.5").With(slippery: 0.3f),
                    L("Ice Crystals", "Sharp crystals float in the air.", "ice_crystal:1.2", "snowball:0.6").With(slippery: 0.35f),
                    L("Freezing Winds", "Freezing winds and slippery air!", "wind_current:0.9", "snowball:1", "ice_crystal:0.5").With(WindMode.Gusts, 1.8f, slippery: 0.45f),
                    L("Frozen Bird Swarm", "Flocks of frozen birds fill the sky.", "frozen_bird:1.3", "snowball:0.8", "ice_crystal:0.5").With(slippery: 0.4f),
                    L("Ice Guardian", "The Ice Guardian blocks the way!", "snowball:0.8", "ice_crystal:0.5").With(slippery: 0.3f),
                },
                new[] // World 6 - Fire Kingdom
                {
                    L("Volcanic Entrance", "The air is burning hot. Watch for embers.", "burning_bird:0.7", "fireball:0.6"),
                    L("Fireballs", "Fireballs streak across the sky.", "fireball:1.3", "burning_bird:0.4"),
                    L("Lava Columns", "Lava erupts where the ground glows.", "lava_column:1.1", "fireball:0.6"),
                    L("Burning Birds", "Birds of flame dart through the smoke.", "burning_bird:1.2", "lava_column:0.6", "fireball:0.5"),
                    L("Volcanic Eruption", "The volcano erupts! Survive the inferno.", "fireball:1.3", "lava_column:0.9", "burning_bird:0.8").With(darkness: 0.15f),
                    L("Fire Phoenix", "The immortal Phoenix rises!", "fireball:0.8", "burning_bird:0.5"),
                },
                new[] // World 7 - Cosmic Journey
                {
                    L("Space Entrance", "You have left the sky behind. Welcome to space!", "asteroid:1"),
                    L("Small Asteroids", "Drifting asteroids everywhere.", "asteroid:1.4", "meteorite:0.3"),
                    L("Meteorite Shower", "Meteorites streak down at high speed!", "meteorite:1.3", "asteroid:0.6"),
                    L("Alien Drones", "Drones track your position. Dodge at the last moment.", "alien_drone:1.1", "asteroid:0.7"),
                    L("Gravity Distortion", "Gravity wells pull you sideways!", "asteroid:1", "meteorite:0.8", "alien_drone:0.6").With(gravity: 0.6f),
                    L("Cosmic Guardian", "The Cosmic Guardian watches from the void.", "asteroid:0.8", "meteorite:0.4").With(gravity: 0.3f),
                },
                new[] // World 8 - Golden Clouds
                {
                    L("Golden Entrance", "Ancient temples float among golden clouds.", "temple_block:1", "moving_platform:0.4"),
                    L("Floating Temple", "Temple structures slide through the sky.", "temple_block:1.3", "temple_debris:0.4"),
                    L("Magical Barriers", "Barriers flash before they activate.", "magic_barrier:1.1", "temple_block:0.6"),
                    L("Cloud Guardians", "Golden guardians pursue intruders.", "guardian_orb:1.1", "magic_barrier:0.6"),
                    L("Temple Collapse", "The temple is falling apart!", "temple_debris:1.3", "temple_block:0.8", "magic_barrier:0.6"),
                    L("Golden Titan", "The Golden Titan awakens!", "temple_debris:0.8", "guardian_orb:0.5"),
                },
                new[] // World 9 - Celestial Realm
                {
                    L("Celestial Entrance", "Heavenly light fills the crystal realm.", "celestial_bird:1", "energy_orb:0.4"),
                    L("Energy Orbs", "Orbs of energy circle their anchors.", "energy_orb:1.3", "celestial_bird:0.4"),
                    L("Crystal Storm", "Crystal shards rain down!", "crystal_shard:1.2", "crystal_barrier:0.8"),
                    L("Celestial Birds", "Swift celestial birds dart across the light.", "celestial_bird:1.3", "energy_orb:0.6", "crystal_barrier:0.4"),
                    L("Light and Shadow", "Shadows hide the danger. Trust the light!", "energy_orb:1", "crystal_shard:0.8", "celestial_bird:0.7").With(lightShadow: true),
                    L("Celestial Guardian", "The Celestial Guardian tests your soul.", "energy_orb:0.8", "crystal_shard:0.5"),
                },
                new[] // World 10 - Heaven's Gate
                {
                    L("Heavenly Entrance", "Heaven's Gate is near. Stay strong.", "celestial_bird:1", "energy_orb:0.6"),
                    L("Golden Gates", "Pass through the golden gates.", "golden_gate:0.9", "guardian_orb:0.6", "celestial_bird:0.5"),
                    L("Guardian Trials", "The guardians test every traveller.", "guardian_orb:1.1", "magic_barrier:0.8", "energy_orb:0.6"),
                    L("Endless Storm", "A final storm guards the heavens.", "lightning_cloud:1", "meteorite:0.8", "wind_current:0.5", "celestial_bird:0.6").With(WindMode.Unpredictable, 1.6f, darkness: 0.3f),
                    L("Final Ascent", "Everything you have learned leads to this.", "golden_gate:0.6", "guardian_orb:0.8", "magic_barrier:0.7", "energy_orb:0.8", "meteorite:0.6").With(WindMode.Gusts, 1.2f),
                    L("Heaven's Guardian", "Face Heaven's Guardian and reach the Gate!", "celestial_bird:0.8", "energy_orb:0.6"),
                },
            };
        }

        static readonly LevelRole[] Roles =
        {
            LevelRole.Introduction, LevelRole.NewObstacle, LevelRole.IncreasedDifficulty,
            LevelRole.AdvancedCombination, LevelRole.Survival, LevelRole.Boss
        };

        static List<LevelData> CreateLevels(List<WorldData> worlds, Dictionary<string, ObstacleData> obstacles)
        {
            var table = LevelTable();
            var levels = new List<LevelData>(GameConstants.TotalLevels);
            for (int w = 0; w < GameConstants.WorldCount; w++)
            {
                var world = worlds[w];
                world.levels.Clear();
                for (int i = 0; i < GameConstants.LevelsPerWorld; i++)
                {
                    int number = w * GameConstants.LevelsPerWorld + i + 1;
                    var spec = table[w][i];
                    var role = Roles[i];
                    float d = (number - 1) / (float)(GameConstants.TotalLevels - 1);
                    var level = Create<LevelData>("Level_" + number.ToString("00") + "_" + spec.Name.Replace(" ", "").Replace("'", ""));
                    level.levelNumber = number;
                    level.worldNumber = w + 1;
                    level.indexInWorld = i + 1;
                    level.levelName = spec.Name;
                    level.hint = spec.Hint;
                    level.role = role;
                    level.difficulty = d;

                    // Ascent
                    level.ascentSpeed = Mathf.Lerp(3.0f, 5.2f, d);
                    float duration = Mathf.Lerp(42f, 66f, d);
                    if (role == LevelRole.Introduction) duration *= 0.9f;
                    if (role == LevelRole.Survival) duration *= 1.18f;
                    if (role == LevelRole.Boss) duration = 18f; // approach before the boss appears
                    level.ascentDuration = Mathf.Round(duration);

                    // Obstacles
                    float baseInterval = Mathf.Lerp(2.5f, 1.05f, d);
                    if (role == LevelRole.Introduction) baseInterval *= 1.25f;
                    if (role == LevelRole.Survival) baseInterval *= 0.82f;
                    if (role == LevelRole.Boss) baseInterval *= 1.3f;
                    level.spawnIntervalStart = baseInterval;
                    level.spawnIntervalEnd = baseInterval * (role == LevelRole.Survival ? 0.65f : 0.78f);
                    level.maxActiveObstacles = 3 + Mathf.RoundToInt(d * 7f) + (role == LevelRole.Survival ? 2 : 0);
                    level.obstacleSpeedMultiplier = 1f + d * 0.55f;
                    level.minFreeLanes = d < 0.35f ? 3 : 2;
                    foreach (var entry in spec.Obstacles)
                    {
                        // Format: id:weight or id:weight@startProgress
                        string id = entry, weightText = "1", startText = "0";
                        int colon = entry.IndexOf(':');
                        if (colon >= 0)
                        {
                            id = entry.Substring(0, colon);
                            weightText = entry.Substring(colon + 1);
                            int at = weightText.IndexOf('@');
                            if (at >= 0) { startText = weightText.Substring(at + 1); weightText = weightText.Substring(0, at); }
                        }
                        if (!obstacles.TryGetValue(id, out var data)) { Debug.LogWarning("Unknown obstacle " + id); continue; }
                        level.obstacles.Add(new ObstacleSpawnEntry
                        {
                            obstacle = data,
                            weight = float.Parse(weightText, System.Globalization.CultureInfo.InvariantCulture),
                            startProgress = float.Parse(startText, System.Globalization.CultureInfo.InvariantCulture)
                        });
                    }

                    // Coins
                    level.coinPatternInterval = Mathf.Lerp(2.2f, 2.9f, d);
                    level.coinsPerPattern = 5 + Mathf.RoundToInt(d * 2f) + spec.ExtraCoins;
                    level.specialCoinCount = 3 + spec.ExtraSpecial;

                    // Power-ups: unlocked gradually across world 1, all available afterwards.
                    level.powerUpInterval = Mathf.Lerp(11f, 15f, d);
                    level.powerUps.Add(new PowerUpSpawnEntry { type = PowerUpType.CoinMagnet, weight = 1f });
                    if (number >= 3) level.powerUps.Add(new PowerUpSpawnEntry { type = PowerUpType.DoubleCoins, weight = 0.9f });
                    if (number >= 4) level.powerUps.Add(new PowerUpSpawnEntry { type = PowerUpType.EnergyShield, weight = 1f });
                    if (number >= 5) level.powerUps.Add(new PowerUpSpawnEntry { type = PowerUpType.ExtraLife, weight = 0.6f });
                    if (number >= 7) level.powerUps.Add(new PowerUpSpawnEntry { type = PowerUpType.SkyRocket, weight = 0.7f });

                    // Environment
                    level.windMode = spec.Wind;
                    level.windStrength = spec.WindStrength;
                    level.darkness = spec.Darkness;
                    level.slipperiness = spec.Slippery;
                    level.gravityDistortion = spec.Gravity;
                    level.lightShadowZones = spec.LightShadow;

                    // Stars: estimate the coins available and derive targets.
                    int estimatedCoins = Mathf.RoundToInt(level.ascentDuration / level.coinPatternInterval * level.coinsPerPattern * 0.85f);
                    if (role == LevelRole.Boss) estimatedCoins += 30;
                    level.coinsForSecondStar = Mathf.Max(5, Mathf.RoundToInt(estimatedCoins * 0.5f));
                    switch (role)
                    {
                        case LevelRole.Introduction: level.challenge = StarChallengeType.CoinTarget; level.challengeValue = Mathf.RoundToInt(estimatedCoins * 0.75f); break;
                        case LevelRole.NewObstacle: level.challenge = StarChallengeType.NoDamage; break;
                        case LevelRole.IncreasedDifficulty: level.challenge = StarChallengeType.AllSpecialCoins; break;
                        case LevelRole.AdvancedCombination: level.challenge = StarChallengeType.CollectPowerUps; level.challengeValue = 2; break;
                        case LevelRole.Survival: level.challenge = StarChallengeType.NoDamage; break;
                        default: level.challenge = StarChallengeType.AllSpecialCoins; break;
                    }

                    if (role == LevelRole.Boss)
                    {
                        level.isBossLevel = true;
                        level.boss = world.boss;
                    }

                    world.levels.Add(level);
                    levels.Add(level);
                }
            }
            return levels;
        }

        // ------------------------------------------------------------------ Cosmetics & achievements

        static List<TrailCosmetic> CreateTrails()
        {
            return new List<TrailCosmetic>
            {
                new TrailCosmetic { id = "sparkle", displayName = "Sparkle Trail", gemPrice = 15, colorA = Hex("#FFFFFF"), colorB = Hex("#FFF3B0") },
                new TrailCosmetic { id = "ember", displayName = "Ember Trail", gemPrice = 25, colorA = Hex("#FF7F11"), colorB = Hex("#FF1E56") },
                new TrailCosmetic { id = "rainbow", displayName = "Rainbow Trail", gemPrice = 40, colorA = Hex("#FF6B6B"), colorB = Hex("#48DBFB") },
                new TrailCosmetic { id = "stardust", displayName = "Stardust Trail", gemPrice = 55, colorA = Hex("#A29BFE"), colorB = Hex("#00D2D3") },
                new TrailCosmetic { id = "halo", displayName = "Halo Trail", gemPrice = 80, colorA = Hex("#FFE9A8"), colorB = Hex("#FFFFFF") },
            };
        }

        static List<AchievementDefinition> CreateAchievements()
        {
            AchievementDefinition A(string id, string title, string desc, AchievementStat stat, int target, int gems)
                => new AchievementDefinition { id = id, title = title, description = desc, stat = stat, target = target, gemReward = gems };
            return new List<AchievementDefinition>
            {
                A("first_flight", "First Flight", "Complete your first level.", AchievementStat.LevelsCompleted, 1, 2),
                A("coin_collector", "Coin Collector", "Collect 1,000 coins in total.", AchievementStat.TotalCoinsCollected, 1000, 3),
                A("treasure_hunter", "Treasure Hunter", "Collect 10,000 coins in total.", AchievementStat.TotalCoinsCollected, 10000, 5),
                A("sky_tycoon", "Sky Tycoon", "Collect 100,000 coins in total.", AchievementStat.TotalCoinsCollected, 100000, 15),
                A("new_ride", "New Ride", "Own 2 balloons.", AchievementStat.BalloonsOwned, 2, 2),
                A("collector", "Balloon Collector", "Own 10 balloons.", AchievementStat.BalloonsOwned, 10, 10),
                A("full_fleet", "Full Fleet", "Own all 20 balloons.", AchievementStat.BalloonsOwned, 20, 40),
                A("boss_slayer", "Guardian Breaker", "Defeat your first boss.", AchievementStat.BossesDefeated, 1, 3),
                A("boss_master", "Guardian Master", "Defeat 5 different bosses.", AchievementStat.BossesDefeated, 5, 10),
                A("untouchable", "Untouchable", "Finish 10 levels without taking damage.", AchievementStat.NoDamageClears, 10, 5),
                A("star_hunter", "Star Hunter", "Earn 60 stars.", AchievementStat.TotalStars, 60, 8),
                A("star_legend", "Star Legend", "Earn all 180 stars.", AchievementStat.TotalStars, 180, 50),
                A("master_crafter", "Master Crafter", "Upgrade a balloon to stage 5.", AchievementStat.MaxBalloonStage, 5, 8),
                A("workshop_pro", "Workshop Pro", "Buy 10 workshop upgrades.", AchievementStat.WorkshopLevels, 10, 8),
                A("power_player", "Power Player", "Collect 50 power-ups.", AchievementStat.PowerUpsCollected, 50, 5),
                A("world_traveller", "World Traveller", "Complete 5 worlds.", AchievementStat.WorldsCompleted, 5, 10),
                A("heaven_reached", "Heaven Reached", "Complete the campaign and reach Heaven's Gate.", AchievementStat.CampaignCompleted, 1, 50),
            };
        }
    }
}
