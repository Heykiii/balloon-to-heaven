using System.Collections.Generic;
using BalloonToHeaven.Core;
using BalloonToHeaven.Player;
using UnityEngine;

namespace BalloonToHeaven.Balloons
{
    /// <summary>
    /// Runtime holder of the equipped balloon's statistics and special abilities (the "BalloonStats"
    /// role). Other systems query it through the ability interfaces, so no system knows about
    /// specific balloons.
    /// </summary>
    public sealed class BalloonAbilities : MonoBehaviour
    {
        readonly List<IBalloonAbility> abilities = new List<IBalloonAbility>();
        readonly List<IDamageModifier> damage = new List<IDamageModifier>();
        readonly List<ILethalDamageHandler> lethal = new List<ILethalDamageHandler>();
        readonly List<ICoinModifier> coins = new List<ICoinModifier>();
        readonly List<IMovementModifier> movement = new List<IMovementModifier>();
        readonly List<IEnvironmentModifier> environment = new List<IEnvironmentModifier>();
        readonly List<IPowerUpModifier> powerUps = new List<IPowerUpModifier>();
        readonly List<IRewardModifier> rewards = new List<IRewardModifier>();
        readonly List<IVisibilityModifier> visibility = new List<IVisibilityModifier>();

        public BalloonData Data { get; private set; }
        public BalloonStatBlock Stats { get; private set; }
        public IActiveAbility Active { get; private set; }

        public void Initialize(BalloonData data, BalloonStatBlock stats, AbilityContext context)
        {
            Data = data;
            Stats = stats;
            context.Stats = stats;
            foreach (var a in AbilityFactory.Create(data))
            {
                abilities.Add(a);
                if (a is IDamageModifier d) damage.Add(d);
                if (a is ILethalDamageHandler l) lethal.Add(l);
                if (a is ICoinModifier c) coins.Add(c);
                if (a is IMovementModifier m) movement.Add(m);
                if (a is IEnvironmentModifier e) environment.Add(e);
                if (a is IPowerUpModifier p) powerUps.Add(p);
                if (a is IRewardModifier r) rewards.Add(r);
                if (a is IVisibilityModifier v) visibility.Add(v);
                if (a is IActiveAbility act && Active == null) Active = act;
            }
        }

        /// <summary>Called after Health exists so abilities can grant starting shields.</summary>
        public void Begin(AbilityContext context)
        {
            foreach (var a in abilities) a.Initialize(context);
        }

        public void Tick(float dt)
        {
            for (int i = 0; i < abilities.Count; i++) abilities[i].Tick(dt);
            if (Active != null) GameEvents.RaiseAbilityCooldownChanged(Active.Readiness);
        }

        public bool TryPreventDamage(ref DamageInfo info)
        {
            for (int i = 0; i < damage.Count; i++) if (damage[i].TryPreventDamage(ref info)) return true;
            return false;
        }

        public bool TryPreventDeath(BalloonHealth health)
        {
            for (int i = 0; i < lethal.Count; i++) if (lethal[i].TryPreventDeath(health)) return true;
            return false;
        }

        public int ModifyCoinValue(int value)
        {
            for (int i = 0; i < coins.Count; i++) value = coins[i].ModifyCoinValue(value);
            return value;
        }

        public void ModifyMovement(ref MovementModifiers m)
        {
            for (int i = 0; i < movement.Count; i++) movement[i].ModifyMovement(ref m);
        }

        public EnvironmentModifiers EnvironmentMultipliers()
        {
            var m = new EnvironmentModifiers { wind = 1f, slippery = 1f, gravity = 1f, darkness = 1f };
            for (int i = 0; i < environment.Count; i++) environment[i].ModifyEnvironment(ref m);
            return m;
        }

        public float ModifyPowerUpDuration(PowerUpType type, float duration)
        {
            for (int i = 0; i < powerUps.Count; i++) duration = powerUps[i].ModifyDuration(type, duration);
            return duration;
        }

        public float RewardMultiplier
        {
            get
            {
                float m = 1f;
                for (int i = 0; i < rewards.Count; i++) m *= rewards[i].RewardMultiplier;
                return m;
            }
        }

        public bool IsInvisible
        {
            get
            {
                for (int i = 0; i < visibility.Count; i++) if (visibility[i].IsInvisible) return true;
                return false;
            }
        }
    }
}
