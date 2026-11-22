using BalloonToHeaven.Core;
using BalloonToHeaven.Levels;
using BalloonToHeaven.Player;

namespace BalloonToHeaven.Balloons
{
    /// <summary>Everything an ability may interact with during a level.</summary>
    public sealed class AbilityContext
    {
        public BalloonData Data;
        public BalloonStatBlock Stats;
        public BalloonController Controller;
        public BalloonHealth Health;
        public LevelContext Level;
    }

    public struct MovementModifiers { public float acceleration; public float maxSpeed; }

    /// <summary>Multipliers for environmental hazards (1 = full effect).</summary>
    public struct EnvironmentModifiers { public float wind; public float slippery; public float gravity; public float darkness; }

    public interface IBalloonAbility
    {
        void Initialize(AbilityContext context);
        void Tick(float deltaTime);
    }

    /// <summary>Return true to fully prevent the damage.</summary>
    public interface IDamageModifier { bool TryPreventDamage(ref DamageInfo info); }
    public interface ILethalDamageHandler { bool TryPreventDeath(BalloonHealth health); }
    public interface ICoinModifier { int ModifyCoinValue(int value); }
    public interface IMovementModifier { void ModifyMovement(ref MovementModifiers modifiers); }
    public interface IEnvironmentModifier { void ModifyEnvironment(ref EnvironmentModifiers modifiers); }
    public interface IPowerUpModifier { float ModifyDuration(PowerUpType type, float duration); }
    public interface IRewardModifier { float RewardMultiplier { get; } }
    public interface IVisibilityModifier { bool IsInvisible { get; } }

    /// <summary>Abilities triggered by the HUD ability button.</summary>
    public interface IActiveAbility
    {
        string ButtonLabel { get; }
        float Readiness { get; }   // 0..1, 1 = ready
        bool CanActivate { get; }
        void Activate();
    }
}
