using System.Collections.Generic;
using BalloonToHeaven.Core;
using BalloonToHeaven.Saving;
using BalloonToHeaven.Worlds;
using UnityEngine;

namespace BalloonToHeaven.Audio
{
    public enum MusicCue { Menu, World, Boss, Victory, Fail, Ending }

    /// <summary>
    /// Plays procedurally synthesized sound effects and music. Clips are rendered once into
    /// AudioClips (works on WebGL, unlike OnAudioFilterRead) and cached. Two music sources
    /// crossfade between cues; a small source pool plays overlapping effects.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        const int SfxVoices = 10;
        const float CrossfadeTime = 1.2f;

        readonly Dictionary<SfxId, AudioClip> sfx = new Dictionary<SfxId, AudioClip>();
        readonly Dictionary<string, AudioClip> music = new Dictionary<string, AudioClip>();
        readonly Dictionary<SfxId, float> lastPlayed = new Dictionary<SfxId, float>();
        AudioSource[] voices;
        int nextVoice;
        AudioSource musicA, musicB;
        bool aIsCurrent = true;
        float musicVolume = 0.7f, sfxVolume = 0.8f;
        float fade = 1f;
        bool ducked;
        string currentKey;

        public void Initialize(SettingsData settings)
        {
            if (settings != null)
            {
                musicVolume = settings.musicVolume;
                sfxVolume = settings.sfxVolume;
            }
            voices = new AudioSource[SfxVoices];
            for (int i = 0; i < SfxVoices; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
                voices[i].spatialBlend = 0f;
            }
            musicA = CreateMusicSource();
            musicB = CreateMusicSource();
        }

        AudioSource CreateMusicSource()
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = true;
            s.spatialBlend = 0f;
            s.volume = 0f;
            s.ignoreListenerPause = true;
            return s;
        }

        public void SetMusicVolume(float v) { musicVolume = Mathf.Clamp01(v); }
        public void SetSfxVolume(float v) { sfxVolume = Mathf.Clamp01(v); }
        public void SetMusicDucked(bool value) { ducked = value; }

        public void PlaySfx(SfxId id, float volume = 1f)
        {
            if (voices == null || sfxVolume <= 0.001f) return;
            // Avoid stacking the same effect many times in one frame (e.g. a coin line).
            float now = Time.unscaledTime;
            if (lastPlayed.TryGetValue(id, out var t) && now - t < 0.035f) return;
            lastPlayed[id] = now;

            if (!sfx.TryGetValue(id, out var clip) || clip == null)
            {
                clip = SoundSynth.CreateSfx(id);
                sfx[id] = clip;
            }
            var src = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            src.pitch = id == SfxId.Coin ? Random.Range(0.96f, 1.08f) : Random.Range(0.98f, 1.02f);
            src.PlayOneShot(clip, Mathf.Clamp01(volume) * sfxVolume);
        }

        public void PlayMusic(MusicCue cue, MusicStyle style)
        {
            if (musicA == null) return;
            style = MusicComposer.StyleFor(cue, style);
            string key = cue + "_" + style.tempo + "_" + style.rootNote + "_" + style.scale + "_" + style.lead + "_" + style.seed;
            if (key == currentKey) return;
            currentKey = key;

            if (!music.TryGetValue(key, out var clip) || clip == null)
            {
                clip = MusicComposer.Compose(cue, style);
                music[key] = clip;
            }

            aIsCurrent = !aIsCurrent;
            var next = aIsCurrent ? musicA : musicB;
            next.clip = clip;
            next.loop = cue != MusicCue.Victory && cue != MusicCue.Fail;
            next.volume = 0f;
            next.Play();
            fade = 0f;
        }

        public void StopMusic()
        {
            currentKey = null;
            if (musicA != null) musicA.Stop();
            if (musicB != null) musicB.Stop();
        }

        void Update()
        {
            if (musicA == null) return;
            fade = Mathf.Min(1f, fade + Time.unscaledDeltaTime / CrossfadeTime);
            float target = musicVolume * (ducked ? 0.35f : 1f) * 0.6f;
            var current = aIsCurrent ? musicA : musicB;
            var previous = aIsCurrent ? musicB : musicA;
            current.volume = target * fade;
            previous.volume = target * (1f - fade);
            if (fade >= 1f && previous.isPlaying) previous.Stop();
        }
    }

    /// <summary>Small additive synthesizer used for all effects and music.</summary>
    public static class SoundSynth
    {
        public const int SampleRate = 22050;

        public static float Osc(Waveform w, float phase)
        {
            float p = phase - Mathf.Floor(phase);
            switch (w)
            {
                case Waveform.Sine: return Mathf.Sin(p * Mathf.PI * 2f);
                case Waveform.Triangle: return 1f - 4f * Mathf.Abs(p - 0.5f);
                case Waveform.SoftSquare: return Mathf.Clamp(Mathf.Sin(p * Mathf.PI * 2f) * 3f, -1f, 1f) * 0.7f;
                case Waveform.Bell:
                    return Mathf.Sin(p * Mathf.PI * 2f) * 0.7f + Mathf.Sin(p * Mathf.PI * 2f * 2.76f) * 0.2f + Mathf.Sin(p * Mathf.PI * 2f * 5.4f) * 0.1f;
            }
            return 0f;
        }

        public static float NoteToFreq(float midi) => 440f * Mathf.Pow(2f, (midi - 69f) / 12f);

        static uint noiseState = 22222;
        public static float Noise()
        {
            noiseState ^= noiseState << 13; noiseState ^= noiseState >> 17; noiseState ^= noiseState << 5;
            return (noiseState & 0xFFFF) / 32767.5f - 1f;
        }

        /// <summary>One synthesized "voice": frequency sweep + waveform + noise with an AR envelope.</summary>
        struct Tone
        {
            public float start, length, f0, f1, volume, attack, noise, vibrato;
            public Waveform wave;
        }

        static AudioClip Render(string name, float length, params Tone[] tones)
        {
            int n = Mathf.CeilToInt(length * SampleRate);
            var data = new float[n];
            foreach (var t in tones)
            {
                int s0 = Mathf.FloorToInt(t.start * SampleRate);
                int count = Mathf.CeilToInt(t.length * SampleRate);
                double phase = 0;
                for (int i = 0; i < count && s0 + i < n; i++)
                {
                    float u = i / (float)count;
                    float f = Mathf.Lerp(t.f0, t.f1, u) * (1f + Mathf.Sin(i / (float)SampleRate * 34f) * t.vibrato);
                    phase += f / SampleRate;
                    float env = Mathf.Min(1f, i / (Mathf.Max(0.001f, t.attack) * SampleRate)) * Mathf.Pow(1f - u, 2f);
                    float v = Osc(t.wave, (float)phase) * (1f - t.noise) + Noise() * t.noise;
                    data[s0 + i] += v * env * t.volume;
                }
            }
            SoftClip(data);
            var clip = AudioClip.Create(name, n, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static void SoftClip(float[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                float x = data[i];
                data[i] = x / (1f + Mathf.Abs(x));
            }
        }

        static Tone T(float start, float length, float f0, float f1, float volume, Waveform wave = Waveform.Sine, float noise = 0f, float attack = 0.005f, float vibrato = 0f)
            => new Tone { start = start, length = length, f0 = f0, f1 = f1, volume = volume, wave = wave, noise = noise, attack = attack, vibrato = vibrato };

        public static AudioClip CreateSfx(SfxId id)
        {
            string n = "sfx_" + id;
            switch (id)
            {
                case SfxId.Click: return Render(n, 0.08f, T(0, 0.07f, 900, 600, 0.6f, Waveform.Triangle));
                case SfxId.Coin: return Render(n, 0.22f, T(0, 0.08f, 1318, 1318, 0.5f, Waveform.Bell), T(0.06f, 0.15f, 1760, 1760, 0.55f, Waveform.Bell));
                case SfxId.SpecialCoin:
                    return Render(n, 0.5f, T(0, 0.15f, 1046, 1046, 0.5f, Waveform.Bell), T(0.1f, 0.15f, 1318, 1318, 0.5f, Waveform.Bell),
                        T(0.2f, 0.3f, 1568, 1568, 0.55f, Waveform.Bell), T(0.2f, 0.3f, 2093, 2093, 0.25f, Waveform.Sine));
                case SfxId.Hit: return Render(n, 0.35f, T(0, 0.3f, 220, 70, 0.9f, Waveform.SoftSquare), T(0, 0.15f, 0, 0, 0.6f, Waveform.Sine, 1f));
                case SfxId.Shield: return Render(n, 0.45f, T(0, 0.4f, 400, 900, 0.5f, Waveform.Sine, 0f, 0.02f, 0.02f), T(0.02f, 0.4f, 600, 1300, 0.3f, Waveform.Triangle));
                case SfxId.PowerUp:
                    return Render(n, 0.5f, T(0, 0.12f, 523, 523, 0.45f, Waveform.Triangle), T(0.1f, 0.12f, 659, 659, 0.45f, Waveform.Triangle),
                        T(0.2f, 0.12f, 784, 784, 0.45f, Waveform.Triangle), T(0.3f, 0.2f, 1046, 1046, 0.5f, Waveform.Triangle));
                case SfxId.Upgrade:
                    return Render(n, 0.7f, T(0, 0.6f, 300, 1200, 0.45f, Waveform.Triangle, 0f, 0.01f), T(0.35f, 0.35f, 1046, 1046, 0.4f, Waveform.Bell), T(0.45f, 0.25f, 1568, 1568, 0.3f, Waveform.Bell));
                case SfxId.Purchase:
                    return Render(n, 0.6f, T(0, 0.12f, 1568, 1568, 0.4f, Waveform.Bell), T(0.08f, 0.12f, 2093, 2093, 0.4f, Waveform.Bell), T(0.16f, 0.4f, 2637, 2637, 0.4f, Waveform.Bell), T(0, 0.08f, 0, 0, 0.25f, Waveform.Sine, 1f));
                case SfxId.BossAttack: return Render(n, 0.6f, T(0, 0.55f, 110, 55, 0.8f, Waveform.SoftSquare, 0.25f, 0.02f), T(0, 0.4f, 82, 41, 0.6f, Waveform.Sine));
                case SfxId.LevelComplete:
                    return Render(n, 1.2f, T(0, 0.2f, 523, 523, 0.45f, Waveform.Triangle), T(0.15f, 0.2f, 659, 659, 0.45f, Waveform.Triangle),
                        T(0.3f, 0.2f, 784, 784, 0.45f, Waveform.Triangle), T(0.45f, 0.7f, 1046, 1046, 0.5f, Waveform.Triangle, 0f, 0.01f, 0.01f), T(0.45f, 0.7f, 659, 659, 0.3f, Waveform.Sine));
                case SfxId.Fail:
                    return Render(n, 1.0f, T(0, 0.3f, 392, 370, 0.45f, Waveform.Triangle), T(0.28f, 0.3f, 330, 311, 0.45f, Waveform.Triangle), T(0.56f, 0.45f, 262, 220, 0.5f, Waveform.Triangle, 0f, 0.01f, 0.02f));
                case SfxId.Whoosh: return Render(n, 0.5f, T(0, 0.45f, 0, 0, 0.5f, Waveform.Sine, 1f, 0.2f));
                case SfxId.Thunder: return Render(n, 1.2f, T(0, 1.1f, 60, 30, 0.6f, Waveform.Sine, 0.85f, 0.01f), T(0, 0.2f, 0, 0, 0.6f, Waveform.Sine, 1f));
                case SfxId.Deny: return Render(n, 0.25f, T(0, 0.1f, 180, 170, 0.5f, Waveform.SoftSquare), T(0.12f, 0.12f, 150, 140, 0.5f, Waveform.SoftSquare));
                case SfxId.Star: return Render(n, 0.6f, T(0, 0.5f, 1568, 2093, 0.45f, Waveform.Bell, 0f, 0.005f, 0.01f), T(0.05f, 0.5f, 2637, 3136, 0.2f, Waveform.Sine));
                case SfxId.BossDefeat:
                    return Render(n, 2.0f, T(0, 1.4f, 120, 40, 0.7f, Waveform.Sine, 0.6f, 0.01f), T(0.6f, 0.3f, 523, 523, 0.4f, Waveform.Triangle),
                        T(0.85f, 0.3f, 659, 659, 0.4f, Waveform.Triangle), T(1.1f, 0.9f, 784, 784, 0.45f, Waveform.Triangle, 0f, 0.01f, 0.01f));
                case SfxId.Unlock:
                    return Render(n, 0.8f, T(0, 0.3f, 784, 784, 0.4f, Waveform.Bell), T(0.15f, 0.3f, 988, 988, 0.4f, Waveform.Bell), T(0.3f, 0.5f, 1175, 1175, 0.45f, Waveform.Bell));
                case SfxId.Crystal: return Render(n, 0.7f, T(0, 0.6f, 1760, 1760, 0.45f, Waveform.Bell, 0f, 0.005f, 0.015f), T(0.03f, 0.6f, 2637, 2637, 0.3f, Waveform.Bell), T(0.06f, 0.5f, 3520, 3520, 0.15f, Waveform.Sine));
                case SfxId.Teleport: return Render(n, 0.4f, T(0, 0.35f, 2000, 300, 0.4f, Waveform.Triangle), T(0, 0.35f, 300, 2000, 0.25f, Waveform.Sine));
            }
            return Render(n, 0.1f, T(0, 0.1f, 440, 440, 0.3f));
        }
    }

    /// <summary>
    /// Composes a short looping piece per world from its MusicStyle: chord progression, bass,
    /// pad, a seeded melody and light percussion. Victory/fail cues are short stingers.
    /// </summary>
    public static class MusicComposer
    {
        static readonly int[][] Scales =
        {
            new[] { 0, 2, 4, 5, 7, 9, 11 },  // Major
            new[] { 0, 2, 3, 5, 7, 8, 10 },  // Minor
            new[] { 0, 2, 4, 7, 9, 12, 14 }, // Pentatonic (padded to 7 degrees)
            new[] { 0, 2, 3, 5, 7, 9, 10 },  // Dorian
            new[] { 0, 2, 4, 6, 7, 9, 11 },  // Lydian
            new[] { 0, 2, 3, 5, 7, 8, 11 },  // Harmonic minor
        };

        static readonly int[][] Progressions =
        {
            new[] { 0, 4, 5, 3 }, // I V vi IV
            new[] { 0, 5, 3, 4 }, // I vi IV V
            new[] { 0, 3, 4, 3 }, // I IV V IV
            new[] { 5, 3, 0, 4 }, // vi IV I V
        };

        public static MusicStyle StyleFor(MusicCue cue, MusicStyle world)
        {
            var baseStyle = world ?? new MusicStyle { tempo = 96, rootNote = 60, scale = MusicScale.Major, lead = Waveform.Bell, percussion = 0.25f, brightness = 0.6f, seed = 7 };
            switch (cue)
            {
                case MusicCue.Menu:
                    return new MusicStyle { tempo = 88, rootNote = 62, scale = MusicScale.Lydian, lead = Waveform.Bell, percussion = 0.2f, brightness = 0.6f, seed = 3 };
                case MusicCue.Boss:
                    return new MusicStyle
                    {
                        tempo = Mathf.RoundToInt(baseStyle.tempo * 1.2f),
                        rootNote = baseStyle.rootNote - 2,
                        scale = baseStyle.scale == MusicScale.Major || baseStyle.scale == MusicScale.Lydian ? MusicScale.HarmonicMinor : baseStyle.scale,
                        lead = Waveform.SoftSquare,
                        percussion = Mathf.Max(0.7f, baseStyle.percussion),
                        brightness = 0.7f,
                        seed = baseStyle.seed + 100,
                    };
                case MusicCue.Ending:
                    return new MusicStyle { tempo = 76, rootNote = 60, scale = MusicScale.Lydian, lead = Waveform.Bell, percussion = 0.15f, brightness = 0.9f, seed = 777 };
                default:
                    return baseStyle;
            }
        }

        public static AudioClip Compose(MusicCue cue, MusicStyle style)
        {
            if (cue == MusicCue.Victory) return Stinger("music_victory", style, new[] { 0, 2, 4, 7, 9, 7, 11, 12 }, true);
            if (cue == MusicCue.Fail) return Stinger("music_fail", style, new[] { 7, 5, 3, 2, 0 }, false);

            int sr = SoundSynth.SampleRate;
            var rng = new System.Random(style.seed * 31 + (int)cue);
            int[] scale = Scales[Mathf.Clamp((int)style.scale, 0, Scales.Length - 1)];
            int[] prog = Progressions[rng.Next(Progressions.Length)];
            float beat = 60f / Mathf.Clamp(style.tempo, 50, 200);
            const int bars = 8;
            const int beatsPerBar = 4;
            int totalBeats = bars * beatsPerBar;
            int n = Mathf.CeilToInt(totalBeats * beat * sr);
            var data = new float[n];

            float Degree(int degree, int octave)
            {
                int len = scale.Length;
                int o = Mathf.FloorToInt(degree / (float)len);
                int d = ((degree % len) + len) % len;
                return style.rootNote + scale[d] + 12 * (o + octave);
            }

            // Pads + bass
            for (int bar = 0; bar < bars; bar++)
            {
                int chord = prog[bar % prog.Length];
                float start = bar * beatsPerBar * beat;
                float len = beatsPerBar * beat;
                for (int k = 0; k < 3; k++)
                    AddNote(data, start, len, SoundSynth.NoteToFreq(Degree(chord + k * 2, -1)), 0.07f, Waveform.Sine, 0.25f, 0.5f);
                for (int b = 0; b < beatsPerBar; b += 2)
                    AddNote(data, start + b * beat, beat * 1.8f, SoundSynth.NoteToFreq(Degree(chord, -2)), 0.22f, Waveform.Triangle, 0.01f, 0.2f);
            }

            // Melody: a motif of 2 bars, varied over the loop.
            var motif = new List<(float pos, float len, int degree)>();
            float p = 0f;
            int deg = 2 + rng.Next(3);
            while (p < 2 * beatsPerBar)
            {
                float len = rng.NextDouble() < 0.6 ? 0.5f : (rng.NextDouble() < 0.5 ? 1f : 1.5f);
                if (rng.NextDouble() < 0.18) { p += len; continue; } // rest
                deg += rng.Next(-2, 3);
                deg = Mathf.Clamp(deg, 0, 9);
                motif.Add((p, len, deg));
                p += len;
            }
            float leadVol = 0.11f + style.brightness * 0.05f;
            for (int rep = 0; rep < bars / 2; rep++)
            {
                int chordRoot = prog[(rep * 2) % prog.Length];
                bool answer = rep % 2 == 1;
                foreach (var m in motif)
                {
                    if (m.pos >= 2 * beatsPerBar) continue;
                    int d = m.degree + (answer ? (chordRoot % 3) - 1 : 0);
                    if (rep == bars / 2 - 1 && m.pos >= 6f) d = 7; // resolve to the octave at the loop end
                    float start = (rep * 2 * beatsPerBar + m.pos) * beat;
                    AddNote(data, start, m.len * beat * 0.95f, SoundSynth.NoteToFreq(Degree(d, 0)), leadVol, style.lead, 0.01f, 0.35f);
                }
            }

            // Percussion
            if (style.percussion > 0.05f)
            {
                for (int b = 0; b < totalBeats; b++)
                {
                    float t = b * beat;
                    if (b % 2 == 0) AddKick(data, t, 0.35f * style.percussion);
                    if (b % 4 == 2 && style.percussion > 0.45f) AddNoise(data, t, 0.12f, 0.14f * style.percussion);
                    AddNoise(data, t + beat * 0.5f, 0.03f, 0.05f * style.percussion);
                }
            }

            // Seamless loop: short fades at both ends.
            int fadeN = Mathf.Min(n / 4, sr / 50);
            for (int i = 0; i < fadeN; i++) { float g = i / (float)fadeN; data[i] *= g; data[n - 1 - i] *= g; }
            SoundSynth.SoftClip(data);
            var clip = AudioClip.Create("music_" + cue + "_" + style.seed, n, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip Stinger(string name, MusicStyle style, int[] semitones, bool major)
        {
            int sr = SoundSynth.SampleRate;
            float step = 0.13f;
            float total = semitones.Length * step + 1.4f;
            var data = new float[Mathf.CeilToInt(total * sr)];
            int root = style != null ? style.rootNote : 60;
            for (int i = 0; i < semitones.Length; i++)
            {
                bool last = i == semitones.Length - 1;
                AddNote(data, i * step, last ? 1.3f : step * 1.6f, SoundSynth.NoteToFreq(root + semitones[i]), 0.25f, major ? Waveform.Bell : Waveform.Triangle, 0.005f, 0.3f);
            }
            float end = semitones.Length * step;
            int[] chord = major ? new[] { 0, 4, 7 } : new[] { 0, 3, 7 };
            foreach (var c in chord) AddNote(data, end - step, 1.4f, SoundSynth.NoteToFreq(root - 12 + c), 0.12f, Waveform.Sine, 0.05f, 0.6f);
            SoundSynth.SoftClip(data);
            var clip = AudioClip.Create(name, data.Length, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }

        static void AddNote(float[] data, float start, float length, float freq, float volume, Waveform wave, float attack, float sustain)
        {
            int sr = SoundSynth.SampleRate;
            int s0 = Mathf.FloorToInt(start * sr);
            int count = Mathf.CeilToInt(length * sr);
            float inc = freq / sr;
            float phase = 0f;
            int attackN = Mathf.Max(1, Mathf.RoundToInt(attack * sr));
            for (int i = 0; i < count; i++)
            {
                int idx = s0 + i;
                if (idx >= data.Length) break;
                float u = i / (float)count;
                float env = Mathf.Min(1f, i / (float)attackN) * Mathf.Lerp(1f, sustain, u) * Mathf.Min(1f, (count - i) / (sr * 0.02f));
                data[idx] += SoundSynth.Osc(wave, phase) * env * volume;
                phase += inc;
                if (phase > 1f) phase -= 1f;
            }
        }

        static void AddKick(float[] data, float start, float volume)
        {
            int sr = SoundSynth.SampleRate;
            int s0 = Mathf.FloorToInt(start * sr);
            int count = sr / 6;
            float phase = 0f;
            for (int i = 0; i < count && s0 + i < data.Length; i++)
            {
                float u = i / (float)count;
                float f = Mathf.Lerp(120f, 45f, Mathf.Sqrt(u));
                phase += f / sr;
                data[s0 + i] += Mathf.Sin(phase * Mathf.PI * 2f) * (1f - u) * (1f - u) * volume;
            }
        }

        static void AddNoise(float[] data, float start, float length, float volume)
        {
            int sr = SoundSynth.SampleRate;
            int s0 = Mathf.FloorToInt(start * sr);
            int count = Mathf.CeilToInt(length * sr);
            float prev = 0f;
            for (int i = 0; i < count && s0 + i < data.Length; i++)
            {
                float u = i / (float)count;
                float nz = SoundSynth.Noise();
                float hp = nz - prev; // crude high-pass for a hat-like sound
                prev = nz;
                data[s0 + i] += hp * (1f - u) * (1f - u) * volume;
            }
        }
    }
}
