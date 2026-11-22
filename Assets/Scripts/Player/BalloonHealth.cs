using System;
using BalloonToHeaven.Balloons;
using BalloonToHeaven.Core;
using BalloonToHeaven.Visuals;
using UnityEngine;

namespace BalloonToHeaven.Player
{
    /// <summary>
    /// Durability, shields and the damage pipeline:
    /// detect hit -> check invulnerability -> shields -> ability modifiers -> armor -> apply ->
    /// lethal handlers (Phoenix) -> feedback. Post-hit invulnerability prevents repeated damage
    /// from a single collision.
    /// </summary>
    public sealed class BalloonHealth : MonoBehaviour
    {
        GameConfig cfg;
        BalloonVisual visual;
        BalloonAbilities abilities;
        System.Random rng;
        int armor;

        float invulnerableTimer;      // post-hit blink
        float abilityInvulnerable;    // ability/rocket invulnerability (no blink)
        float shieldTimer;
        int shieldCharges;
        float lastShieldEvent = -1f;

        public int Current { get; private set; }
        public int Max { get; private set; }
        public bool IsDead { get; private set; }
        public bool TookDamage { get; private set; }

        public event Action Died;

        public bool HasShield => shieldTimer > 0f || shieldCharges > 0;
        public bool HasShieldCharge => shieldCharges > 0;
        public bool IsInvulnerable => invulnerableTimer > 0f || abilityInvulnerable > 0f;
        public float ShieldTime => shieldTimer;

        public void Initialize(int maxHealth, int armorValue, GameConfig config, BalloonVisual balloonVisual, BalloonAbilities balloonAbilities, System.Random random)
        {
            cfg = config;
            visual = balloonVisual;
            abilities = balloonAbilities;
            rng = random;
            armor = Mathf.Max(0, armorValue);
            Max = Mathf.Max(1, maxHealth);
            Current = Max;
            IsDead = false;
            TookDamage = false;
            GameEvents.RaiseHealthChanged(Current, Max);
        }

        public void AddShieldTime(float seconds)
        {
            if (seconds <= 0f) return;
            shieldTimer = Mathf.Max(shieldTimer, seconds);
            GameEvents.RaiseShieldChanged(shieldTimer);
        }

        public void AddShieldCharge(int charges = 1)
        {
            shieldCharges = Mathf.Clamp(shieldCharges + charges, 0, 1);
            GameEvents.RaiseShieldChanged(shieldTimer > 0 ? shieldTimer : -1f);
        }

        public void SetInvulnerable(float seconds) => abilityInvulnerable = Mathf.Max(abilityInvulnerable, seconds);

        public void Heal(int amount)
        {
            if (IsDead || amount <= 0) return;
            Current = Mathf.Min(Max, Current + amount);
            GameEvents.RaiseHealthChanged(Current, Max);
        }

        public void SetHealth(int value)
        {
            Current = Mathf.Clamp(value, 1, Max);
            IsDead = false;
            GameEvents.RaiseHealthChanged(Current, Max);
        }

        /// <summary>Processes a collision. Returns true if durability was lost.</summary>
        public bool TryHit(DamageInfo info)
        {
            if (IsDead || info.Amount <= 0) return false;
            if (IsInvulnerable) return false;

            if (shieldTimer > 0f || shieldCharges > 0)
            {
                if (shieldTimer <= 0f) shieldCharges--;
                Blocked();
                return false;
            }

            if (abilities != null && abilities.TryPreventDamage(ref info))
            {
                Blocked();
                return false;
            }

            float deflect = Mathf.Min(cfg.maxArmorDeflectChance, armor * cfg.armorDeflectChancePerPoint);
            if (deflect > 0f && rng.NextDouble() < deflect)
            {
                Blocked();
                return false;
            }

            Current -= info.Amount;
            TookDamage = true;
            invulnerableTimer = cfg.postHitInvulnerability;
            if (visual != null) visual.Flash();
            GameEvents.RaiseBalloonDamaged(info);

            if (Current <= 0)
            {
                if (abilities != null && abilities.TryPreventDeath(this))
                {
                    invulnerableTimer = Mathf.Max(invulnerableTimer, 2f);
                }
                else
                {
                    Current = 0;
                    IsDead = true;
                    GameEvents.RaiseHealthChanged(Current, Max);
                    Died?.Invoke();
                    return true;
                }
            }
            GameEvents.RaiseHealthChanged(Current, Max);
            return true;
        }

        void Blocked()
        {
            invulnerableTimer = Mathf.Max(invulnerableTimer, cfg.blockedHitInvulnerability);
            GameEvents.RaiseHitBlocked();
            GameEvents.RaiseShieldChanged(shieldTimer > 0 ? shieldTimer : (shieldCharges > 0 ? -1f : 0f));
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            if (invulnerableTimer > 0f) invulnerableTimer -= dt;
            if (abilityInvulnerable > 0f) abilityInvulnerable -= dt;
            if (shieldTimer > 0f)
            {
                shieldTimer -= dt;
                if (shieldTimer <= 0f || Time.time - lastShieldEvent > 0.1f)
                {
                    lastShieldEvent = Time.time;
                    GameEvents.RaiseShieldChanged(Mathf.Max(0f, shieldTimer) > 0f ? shieldTimer : (shieldCharges > 0 ? -1f : 0f));
                }
            }
            if (visual != null)
            {
                visual.SetShield(HasShield || abilityInvulnerable > 0f);
                visual.SetBlinking(invulnerableTimer > 0f && !IsDead);
            }
        }
    }
}
