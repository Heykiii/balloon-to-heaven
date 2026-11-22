using System;
using System.Collections.Generic;
using BalloonToHeaven.Bosses;
using BalloonToHeaven.Levels;
using UnityEngine;

namespace BalloonToHeaven.Worlds
{
    public enum WorldTheme
    {
        GreenValley, MountainKingdom, RainbowSkies, Thunderstorm, FrozenHeaven,
        FireKingdom, CosmicJourney, GoldenClouds, CelestialRealm, HeavensGate
    }

    public enum WeatherEffect { None, Pollen, Petals, Sparkles, Rain, Snow, Embers, Stars, GoldDust, LightMotes }

    public enum MusicScale { Major, Minor, Pentatonic, Dorian, Lydian, HarmonicMinor }
    public enum Waveform { Sine, Triangle, SoftSquare, Bell }

    /// <summary>Parameters for the procedural music composer.</summary>
    [Serializable]
    public class MusicStyle
    {
        public int tempo = 100;
        public int rootNote = 60;
        public MusicScale scale = MusicScale.Major;
        public Waveform lead = Waveform.Triangle;
        [Range(0f, 1f)] public float percussion = 0.4f;
        [Range(0f, 1f)] public float brightness = 0.5f;
        public int seed = 1;
    }

    /// <summary>Visual, audio and progression definition of one world.</summary>
    [CreateAssetMenu(menuName = "Balloon to Heaven/World", fileName = "World")]
    public class WorldData : ScriptableObject
    {
        public int worldNumber;
        public string worldName;
        [TextArea] public string description;
        public WorldTheme theme;

        [Header("Palette")]
        public Color skyTop = new Color(0.35f, 0.62f, 1f);
        public Color skyBottom = new Color(0.75f, 0.9f, 1f);
        public Color cloudColor = Color.white;
        public Color groundColor = new Color(0.35f, 0.75f, 0.35f);
        public Color accentColor = Color.yellow;
        public Color ambientColor = new Color(0.55f, 0.6f, 0.7f);
        public Color lightColor = new Color(1f, 0.96f, 0.88f);
        public Color rimColor = new Color(1f, 1f, 1f, 1f);
        public Vector3 lightDirection = new Vector3(-0.4f, 0.7f, -0.6f);
        public WeatherEffect weather;

        [Header("Audio")]
        public MusicStyle music = new MusicStyle();

        [Header("Progression")]
        public List<LevelData> levels = new List<LevelData>();
        public BossData boss;
        public int completionCoins = 1000;
        public int completionGems = 10;
    }
}
