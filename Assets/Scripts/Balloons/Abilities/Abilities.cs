using System.Collections.Generic;
using BalloonToHeaven.Core;
using BalloonToHeaven.Player;
using BalloonToHeaven.Visuals;
using UnityEngine;

namespace BalloonToHeaven.Balloons
{
    public abstract class BalloonAbility : IBalloonAbility
    {
        protected AbilityContext Ctx;
        public virtual void Initialize(AbilityContext context) { Ctx = context; }
        public virtual void Tick(float deltaTime) { }
    }

    /// <summary>Adds a fractional bonus to every coin pickup (accumulated so it stays fair with integers).</summary>
    public sealed class BonusCoinsAbility : BalloonAbility, ICoinModifier
    {
        readonly float bonus;
        float accumulator;
        public BonusCoinsAbility(float bonus) { this.bonus = bonus; }

        public int ModifyCoinValue(int value)
        {
            accumulator += value * bonus;
            int extra = Mathf.FloorToInt(accumulator);
            accumulator -= extra;
            return value + extra;
        }
    }

    public sealed class WindResistanceAbility : BalloonAbility, IEnvironmentModifier
    {
        readonly float reduction;
        public WindResistanceAbility(float reduction) { this.reduction = Mathf.Clamp01(reduction); }
        public void ModifyEnvironment(ref EnvironmentModifiers m) { m.wind *= 1f - reduction; }
    }

    public sealed class EnvironmentalResistanceAbility : BalloonAbility, IEnvironmentModifier
    {
        readonly float factor;
        public EnvironmentalResistanceAbility(float factor) { this.factor = Mathf.Clamp01(factor); }

        public void ModifyEnvironment(ref EnvironmentModifiers m)
        {
            m.wind *= factor; m.slippery *= factor; m.gravity *= factor; m.darkness *= factor;
        }
    }

    public sealed class GravityImmunityAbility : BalloonAbility, IEnvironmentModifier
    {
        public void ModifyEnvironment(ref EnvironmentModifiers m) { m.gravity = 0f; }
    }

    /// <summary>Periodic timed shield (Magic balloon).</summary>
    public sealed class PeriodicShieldAbility : BalloonAbility
    {
        readonly float duration, cooldown;
        float timer;
        public PeriodicShieldAbility(float duration, float cooldown) { this.duration = duration; this.cooldown = Mathf.Max(1f, cooldown); }

        public override void Initialize(AbilityContext context)
        {
            base.Initialize(context);
            context.Health.AddShieldTime(duration + context.Stats.shieldBonusSeconds);
            timer = cooldown;
        }

        public override void Tick(float dt)
        {
            timer -= dt;
            if (timer > 0f) return;
            timer = cooldown;
            Ctx.Health.AddShieldTime(duration + Ctx.Stats.shieldBonusSeconds);
            Ctx.Level?.Audio?.PlaySfx(SfxId.Shield);
        }
    }

    /// <summary>A one-hit shield charge that recharges (Angel, Heaven's King).</summary>
    public sealed class RechargingShieldAbility : BalloonAbility
    {
        readonly float rechargeTime;
        float timer;
        public RechargingShieldAbility(float rechargeTime) { this.rechargeTime = Mathf.Max(1f, rechargeTime); }

        public override void Initialize(AbilityContext context)
        {
            base.Initialize(context);
            context.Health.AddShieldCharge(1);
        }

        public override void Tick(float dt)
        {
            if (Ctx.Health.HasShieldCharge) { timer = 0f; return; }
            timer += dt;
            float effective = Mathf.Max(1f, rechargeTime - Ctx.Stats.shieldBonusSeconds * 0.5f);
            if (timer >= effective)
            {
                timer = 0f;
                Ctx.Health.AddShieldCharge(1);
                Ctx.Level?.Audio?.PlaySfx(SfxId.Shield);
            }
        }
    }

    /// <summary>Completely ignores damage of one type (Fireball: fire, Galaxy: space).</summary>
    public sealed class DamageImmunityAbility : BalloonAbility, IDamageModifier
    {
        readonly DamageType type;
        public DamageImmunityAbility(DamageType type) { this.type = type; }
        public bool TryPreventDamage(ref DamageInfo info) => info.Type == type;
    }

    /// <summary>Deflects every second hit of the given type (Iron Shield: physical, Saturn: space).</summary>
    public sealed class AlternatingResistanceAbility : BalloonAbility, IDamageModifier
    {
        readonly DamageType type;
        int count;
        public AlternatingResistanceAbility(DamageType type) { this.type = type; }

        public bool TryPreventDamage(ref DamageInfo info)
        {
            if (info.Type != type) return false;
            count++;
            return count % 2 == 0;
        }
    }

    public sealed class ManeuverabilityAbility : BalloonAbility, IMovementModifier
    {
        readonly float bonus;
        public ManeuverabilityAbility(float bonus) { this.bonus = bonus; }
        public void ModifyMovement(ref MovementModifiers m) { m.acceleration *= 1f + bonus; m.maxSpeed *= 1f + bonus * 0.43f; }
    }

    public sealed class RewardBoostAbility : BalloonAbility, IRewardModifier
    {
        public float RewardMultiplier { get; }
        public RewardBoostAbility(float bonus) { RewardMultiplier = 1f + bonus; }
    }

    public sealed class PowerUpDurationAbility : BalloonAbility, IPowerUpModifier
    {
        readonly float bonus;
        public PowerUpDurationAbility(float bonus) { this.bonus = bonus; }
        public float ModifyDuration(PowerUpType type, float duration) => duration * (1f + bonus);
    }

    /// <summary>Survive one fatal hit per level (Phoenix, Heaven's King).</summary>
    public sealed class RebirthAbility : BalloonAbility, ILethalDamageHandler
    {
        readonly float restoreFraction;
        bool used;
        public RebirthAbility(float restoreFraction) { this.restoreFraction = Mathf.Clamp01(restoreFraction); }

        public bool TryPreventDeath(BalloonHealth health)
        {
            if (used) return false;
            used = true;
            health.SetHealth(Mathf.Max(1, Mathf.CeilToInt(health.Max * restoreFraction)));
            health.SetInvulnerable(2f);
            var pos = Ctx.Controller != null ? Ctx.Controller.transform.position : Vector3.zero;
            ParticleFactory.Burst(pos + Vector3.back, new Color(1f, 0.55f, 0.15f), 40, 6f, 0.45f, 0.9f);
            GameEvents.RaiseToast("Rebirth!");
            Ctx.Level?.Audio?.PlaySfx(SfxId.PowerUp);
            return true;
        }
    }

    /// <summary>Base class for button-activated abilities with a cooldown.</summary>
    public abstract class CooldownAbility : BalloonAbility, IActiveAbility
    {
        protected readonly float Cooldown;
        protected float CooldownTimer;
        protected CooldownAbility(float cooldown) { Cooldown = Mathf.Max(0.5f, cooldown); }
        public abstract string ButtonLabel { get; }
        public float Readiness => 1f - Mathf.Clamp01(CooldownTimer / Cooldown);
        public virtual bool CanActivate => CooldownTimer <= 0f && Ctx != null && !Ctx.Health.IsDead;

        public void Activate()
        {
            if (!CanActivate) return;
            CooldownTimer = Cooldown;
            OnActivate();
        }

        protected abstract void OnActivate();

        public override void Tick(float dt)
        {
            if (CooldownTimer > 0f) CooldownTimer -= dt;
        }
    }

    public sealed class InvisibilityAbility : CooldownAbility, IVisibilityModifier
    {
        readonly float duration;
        float activeTimer;
        public InvisibilityAbility(float duration, float cooldown) : base(cooldown) { this.duration = duration; }
        public override string ButtonLabel => "VEIL";
        public bool IsInvisible => activeTimer > 0f;

        protected override void OnActivate()
        {
            activeTimer = duration;
            Ctx.Controller.Visual?.SetInvisible(true);
            Ctx.Level?.Audio?.PlaySfx(SfxId.Teleport);
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);
            if (activeTimer <= 0f) return;
            activeTimer -= dt;
            if (activeTimer <= 0f) Ctx.Controller.Visual?.SetInvisible(false);
        }
    }

    public sealed class TeleportAbility : CooldownAbility
    {
        readonly float distance;
        public TeleportAbility(float distance, float cooldown) : base(cooldown) { this.distance = distance; }
        public override string ButtonLabel => "BLINK";

        protected override void OnActivate()
        {
            var c = Ctx.Controller;
            float dir = Mathf.Abs(c.Velocity) > 0.3f ? Mathf.Sign(c.Velocity) : (c.X > 0f ? -1f : 1f);
            float limit = Ctx.Level.Field.HalfWidth - 1.2f;
            if (Mathf.Abs(c.X + dir * distance) > limit) dir = -dir;
            c.Teleport(dir * distance);
            Ctx.Health.SetInvulnerable(0.45f);
            Ctx.Level?.Audio?.PlaySfx(SfxId.Teleport);
        }
    }

    public sealed class InvulnerabilityAbility : CooldownAbility
    {
        readonly float duration;
        public InvulnerabilityAbility(float duration, float cooldown) : base(cooldown) { this.duration = duration; }
        public override string ButtonLabel => "ETERNAL";

        protected override void OnActivate()
        {
            Ctx.Health.SetInvulnerable(duration + Ctx.Stats.shieldBonusSeconds * 0.5f);
            Ctx.Level?.Audio?.PlaySfx(SfxId.Shield);
            GameEvents.RaiseToast("Invulnerable!");
        }
    }

    /// <summary>Creates the runtime abilities for a balloon definition. One shared implementation per ability type.</summary>
    public static class AbilityFactory
    {
        public static List<IBalloonAbility> Create(BalloonData d)
        {
            var list = new List<IBalloonAbility>();
            switch (d.ability)
            {
                case BalloonAbilityType.BonusCoins: list.Add(new BonusCoinsAbility(d.abilityValue)); break;
                case BalloonAbilityType.ImprovedMagnet: break; // applied to the stat block
                case BalloonAbilityType.WindResistance: list.Add(new WindResistanceAbility(d.abilityValue)); break;
                case BalloonAbilityType.PeriodicShield: list.Add(new PeriodicShieldAbility(d.abilityDuration, d.abilityCooldown)); break;
                case BalloonAbilityType.FireResistance: list.Add(new DamageImmunityAbility(DamageType.Fire)); break;
                case BalloonAbilityType.EnvironmentalResistance:
                    list.Add(new EnvironmentalResistanceAbility(d.abilityValue));
                    list.Add(new AlternatingResistanceAbility(DamageType.Ice));
                    break;
                case BalloonAbilityType.Maneuverability: list.Add(new ManeuverabilityAbility(d.abilityValue)); break;
                case BalloonAbilityType.PhysicalResistance: list.Add(new AlternatingResistanceAbility(DamageType.Physical)); break;
                case BalloonAbilityType.CoinRewardBoost: list.Add(new RewardBoostAbility(d.abilityValue)); break;
                case BalloonAbilityType.PowerUpDuration: list.Add(new PowerUpDurationAbility(d.abilityValue)); break;
                case BalloonAbilityType.Invisibility: list.Add(new InvisibilityAbility(d.abilityDuration, d.abilityCooldown)); break;
                case BalloonAbilityType.SpaceResistance: list.Add(new DamageImmunityAbility(DamageType.Space)); break;
                case BalloonAbilityType.GravityResistance:
                    list.Add(new GravityImmunityAbility());
                    list.Add(new AlternatingResistanceAbility(DamageType.Space));
                    break;
                case BalloonAbilityType.PhoenixRecovery: list.Add(new RebirthAbility(d.abilityValue)); break;
                case BalloonAbilityType.Teleport: list.Add(new TeleportAbility(d.abilityValue, d.abilityCooldown)); break;
                case BalloonAbilityType.HeavenlyShield: list.Add(new RechargingShieldAbility(d.abilityCooldown)); break;
                case BalloonAbilityType.StrongMagnet: list.Add(new BonusCoinsAbility(0.25f)); break;
                case BalloonAbilityType.Invulnerability: list.Add(new InvulnerabilityAbility(d.abilityDuration, d.abilityCooldown)); break;
                case BalloonAbilityType.UltimateProtection:
                    list.Add(new RechargingShieldAbility(d.abilityCooldown));
                    list.Add(new RebirthAbility(0.5f));
                    list.Add(new BonusCoinsAbility(d.abilityValue));
                    break;
            }
            return list;
        }
    }
}
