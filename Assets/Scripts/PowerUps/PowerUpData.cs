using BalloonToHeaven.Core;
using UnityEngine;

namespace BalloonToHeaven.PowerUps
{
    /// <summary>Configurable power-up definition.</summary>
    [CreateAssetMenu(menuName = "Balloon to Heaven/Power-Up", fileName = "PowerUp")]
    public class PowerUpData : ScriptableObject
    {
        public PowerUpType type;
        public string displayName;
        [TextArea] public string description;
        [Tooltip("Seconds. Zero for instant power-ups such as Extra Life.")]
        public float duration = 10f;
        [Tooltip("Magnet radius, speed multiplier or coin multiplier depending on type.")]
        public float magnitude = 1f;
        public Color color = Color.white;
        public bool IsInstant => duration <= 0f;
    }
}
