using System.Collections.Generic;
using BalloonToHeaven.Balloons;
using UnityEngine;

namespace BalloonToHeaven.Visuals
{
    /// <summary>Small runtime-generated textures (patterns, soft particles, vignettes).</summary>
    public static class ProceduralTextures
    {
        static Texture2D softCircle, cloudPuff, vignette, streak, sparkle, ring;
        static readonly Dictionary<string, Texture2D> balloonPatterns = new Dictionary<string, Texture2D>();

        static Texture2D New(int size, bool clamp = true)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
            t.wrapMode = clamp ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            t.filterMode = FilterMode.Bilinear;
            return t;
        }

        public static Texture2D SoftCircle
        {
            get
            {
                if (softCircle != null) return softCircle;
                const int n = 64;
                softCircle = New(n);
                var px = new Color32[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.Clamp01(1f - d);
                        a = a * a * (3f - 2f * a);
                        px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
                    }
                softCircle.SetPixels32(px);
                softCircle.Apply(false, true);
                return softCircle;
            }
        }

        public static Texture2D Sparkle
        {
            get
            {
                if (sparkle != null) return sparkle;
                const int n = 64;
                sparkle = New(n);
                var px = new Color32[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float dx = Mathf.Abs((x + 0.5f) / n * 2f - 1f), dy = Mathf.Abs((y + 0.5f) / n * 2f - 1f);
                        float star = Mathf.Clamp01(1f - (dx * dy * 18f + Mathf.Sqrt(dx * dx + dy * dy) * 0.9f));
                        float core = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy) * 2.2f);
                        float a = Mathf.Clamp01(star + core);
                        px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
                    }
                sparkle.SetPixels32(px);
                sparkle.Apply(false, true);
                return sparkle;
            }
        }

        public static Texture2D Ring
        {
            get
            {
                if (ring != null) return ring;
                const int n = 64;
                ring = New(n);
                var px = new Color32[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.Clamp01(1f - Mathf.Abs(d - 0.8f) * 8f);
                        px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
                    }
                ring.SetPixels32(px);
                ring.Apply(false, true);
                return ring;
            }
        }

        /// <summary>Fluffy cloud blob made of overlapping soft circles; used for background clouds.</summary>
        public static Texture2D CloudPuff
        {
            get
            {
                if (cloudPuff != null) return cloudPuff;
                const int n = 128;
                cloudPuff = New(n);
                var alpha = new float[n * n];
                var rng = new System.Random(42);
                for (int i = 0; i < 9; i++)
                {
                    float cx = 0.25f + (float)rng.NextDouble() * 0.5f;
                    float cy = 0.35f + (float)rng.NextDouble() * 0.25f;
                    float r = 0.16f + (float)rng.NextDouble() * 0.14f;
                    for (int y = 0; y < n; y++)
                        for (int x = 0; x < n; x++)
                        {
                            float dx = (x + 0.5f) / n - cx, dy = (y + 0.5f) / n - cy;
                            float d = Mathf.Sqrt(dx * dx + dy * dy) / r;
                            float a = Mathf.Clamp01(1.2f - d);
                            alpha[y * n + x] = Mathf.Max(alpha[y * n + x], a);
                        }
                }
                var px = new Color32[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float a = Mathf.Clamp01(alpha[y * n + x] * 1.4f);
                        float shade = Mathf.Lerp(0.82f, 1f, y / (float)n); // darker underside = volume
                        byte s = (byte)(shade * 255);
                        px[y * n + x] = new Color32(s, s, s, (byte)(a * a * 255));
                    }
                cloudPuff.SetPixels32(px);
                cloudPuff.Apply(false, true);
                return cloudPuff;
            }
        }

        /// <summary>Transparent centre, dark edges. Used for darkness and damage overlays.</summary>
        public static Texture2D Vignette
        {
            get
            {
                if (vignette != null) return vignette;
                const int n = 128;
                vignette = New(n);
                var px = new Color32[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((d - 0.25f) / 0.75f));
                        px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
                    }
                vignette.SetPixels32(px);
                vignette.Apply(false, true);
                return vignette;
            }
        }

        public static Texture2D Streak
        {
            get
            {
                if (streak != null) return streak;
                const int n = 64;
                streak = New(n);
                var px = new Color32[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float u = (x + 0.5f) / n, v = (y + 0.5f) / n * 2f - 1f;
                        float a = Mathf.Clamp01(1f - Mathf.Abs(v) * 3f) * Mathf.Sin(u * Mathf.PI);
                        px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
                    }
                streak.SetPixels32(px);
                streak.Apply(false, true);
                return streak;
            }
        }

        /// <summary>Envelope texture for a balloon (u = around, v = bottom to top).</summary>
        public static Texture2D BalloonPattern(BalloonData data)
        {
            string key = data.id + "_" + data.pattern;
            if (balloonPatterns.TryGetValue(key, out var cached) && cached != null) return cached;
            const int n = 128;
            var tex = New(n, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            var px = new Color32[n * n];
            Color a = data.primaryColor, b = data.secondaryColor, c = data.accentColor;
            Color[] rainbow = { new Color(1f, 0.33f, 0.33f), new Color(1f, 0.6f, 0.2f), new Color(1f, 0.9f, 0.3f), new Color(0.35f, 0.85f, 0.4f), new Color(0.3f, 0.6f, 1f), new Color(0.6f, 0.4f, 1f) };
            var rng = new System.Random(data.id.GetHashCode());
            var stars = new List<Vector3>();
            for (int i = 0; i < 26; i++) stars.Add(new Vector3((float)rng.NextDouble(), 0.2f + (float)rng.NextDouble() * 0.75f, 0.02f + (float)rng.NextDouble() * 0.03f));

            for (int y = 0; y < n; y++)
            {
                float v = (y + 0.5f) / n;
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n;
                    Color col = a;
                    switch (data.pattern)
                    {
                        case EnvelopePattern.Solid:
                            col = Color.Lerp(b, a, Mathf.SmoothStep(0.05f, 0.4f, v));
                            if (Mathf.Abs(v - 0.55f) < 0.03f) col = c;
                            break;
                        case EnvelopePattern.Stripes:
                            col = (Mathf.FloorToInt(u * 12f) % 2 == 0) ? a : b;
                            break;
                        case EnvelopePattern.Dots:
                        {
                            float gu = u * 10f, gv = v * 8f;
                            float du = gu - Mathf.Floor(gu) - 0.5f, dv = gv - Mathf.Floor(gv) - 0.5f;
                            col = du * du + dv * dv < 0.07f ? b : a;
                            break;
                        }
                        case EnvelopePattern.Zigzag:
                        {
                            float tri = Mathf.Abs((u * 8f) % 1f - 0.5f) * 0.12f;
                            float band = (v + tri) * 6f;
                            col = Mathf.FloorToInt(band) % 2 == 0 ? a : b;
                            if (Mathf.Abs(band - Mathf.Round(band)) < 0.06f) col = c;
                            break;
                        }
                        case EnvelopePattern.Stars:
                            col = Color.Lerp(a, b, v * 0.6f);
                            break;
                        case EnvelopePattern.Checker:
                            col = ((Mathf.FloorToInt(u * 8f) + Mathf.FloorToInt(v * 6f)) % 2 == 0) ? a : b;
                            if (Mathf.Abs(v - 0.5f) < 0.025f) col = c;
                            break;
                        case EnvelopePattern.Gradient:
                            col = Color.Lerp(b, a, v);
                            col = Color.Lerp(col, c, Mathf.Pow(Mathf.Abs(Mathf.Sin((u * 6f + v * 2f) * Mathf.PI)), 18f) * 0.6f);
                            break;
                        case EnvelopePattern.Swirl:
                            col = Mathf.FloorToInt((u * 6f + v * 2.5f) * 2f) % 2 == 0 ? a : b;
                            break;
                        case EnvelopePattern.Bands:
                        {
                            float band = v * 7f;
                            col = Mathf.FloorToInt(band) % 2 == 0 ? a : b;
                            if (Mathf.Abs(band - Mathf.Round(band)) < 0.08f) col = c;
                            break;
                        }
                        case EnvelopePattern.Flames:
                        {
                            float flame = 0.42f + Mathf.Sin(u * Mathf.PI * 16f) * 0.08f + Mathf.Sin(u * Mathf.PI * 6f) * 0.06f;
                            col = v < flame ? Color.Lerp(c, b, v / Mathf.Max(0.01f, flame)) : a;
                            break;
                        }
                        case EnvelopePattern.Crystal:
                        {
                            float facet = Mathf.Abs(Mathf.Sin(u * Mathf.PI * 10f + v * 4f)) * Mathf.Abs(Mathf.Cos(v * Mathf.PI * 6f - u * 3f));
                            col = Color.Lerp(a, b, facet);
                            if (facet > 0.92f) col = c;
                            break;
                        }
                        case EnvelopePattern.Galaxy:
                        {
                            float swirl = Mathf.Sin((u * 4f + v * 3f) * Mathf.PI) * 0.5f + 0.5f;
                            col = Color.Lerp(a, b, swirl * swirl);
                            break;
                        }
                        case EnvelopePattern.Rainbow:
                            col = rainbow[Mathf.Clamp(Mathf.FloorToInt(v * rainbow.Length), 0, rainbow.Length - 1)];
                            break;
                        case EnvelopePattern.SunRays:
                        {
                            float ray = Mathf.Abs(Mathf.Sin(u * Mathf.PI * 9f));
                            col = Color.Lerp(a, b, Mathf.SmoothStep(0.3f, 1f, ray) * Mathf.Clamp01(1.2f - v));
                            if (v > 0.82f) col = Color.Lerp(col, c, 0.5f);
                            break;
                        }
                        case EnvelopePattern.Leaves:
                        {
                            float gu = u * 9f, gv = v * 7f + (Mathf.FloorToInt(u * 9f) % 2) * 0.5f;
                            float du = gu - Mathf.Floor(gu) - 0.5f, dv = gv - Mathf.Floor(gv) - 0.5f;
                            float leaf = du * du * 4f + dv * dv * 1.4f;
                            col = leaf < 0.18f ? b : a;
                            if (Mathf.Abs(du) < 0.03f && leaf < 0.18f) col = c;
                            break;
                        }
                    }
                    // Seam lines between gores give the fabric look.
                    float gore = Mathf.Abs((u * 12f) % 1f - 0.5f);
                    if (gore > 0.47f) col *= 0.88f;
                    px[y * n + x] = col;
                }
            }

            if (data.pattern == EnvelopePattern.Stars || data.pattern == EnvelopePattern.Galaxy)
            {
                foreach (var s in stars)
                {
                    int cx = Mathf.RoundToInt(s.x * n), cy = Mathf.RoundToInt(s.y * n), r = Mathf.Max(1, Mathf.RoundToInt(s.z * n));
                    for (int dy = -r; dy <= r; dy++)
                        for (int dx = -r; dx <= r; dx++)
                        {
                            if (Mathf.Abs(dx) + Mathf.Abs(dy) > r) continue;
                            int px2 = (cx + dx + n) % n, py2 = Mathf.Clamp(cy + dy, 0, n - 1);
                            px[py2 * n + px2] = data.pattern == EnvelopePattern.Galaxy ? Color.white : c;
                        }
                }
            }

            tex.SetPixels32(px);
            tex.Apply(true, true);
            balloonPatterns[key] = tex;
            return tex;
        }
    }
}
