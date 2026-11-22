using System;
using System.Collections.Generic;
using UnityEngine;

namespace BalloonToHeaven.UI
{
    /// <summary>
    /// Generates crisp UI sprites (rounded panels, icons) from signed distance functions at runtime,
    /// so the interface needs no imported art.
    /// </summary>
    public static class SpriteFactory
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        static Sprite Make(string key, int size, Func<float, float, float> sdf, int border = 0, float softness = 1.2f)
        {
            if (cache.TryGetValue(key, out var s) && s != null) return s;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = key };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = sdf(x + 0.5f, y + 0.5f); // negative inside, in pixels
                    float a = Mathf.Clamp01(0.5f - d / softness);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            s.name = key;
            cache[key] = s;
            return s;
        }

        static float RoundedBox(float px, float py, float cx, float cy, float hw, float hh, float r)
        {
            float qx = Mathf.Abs(px - cx) - hw + r, qy = Mathf.Abs(py - cy) - hh + r;
            float outside = new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude;
            return outside + Mathf.Min(Mathf.Max(qx, qy), 0) - r;
        }

        static float Circle(float px, float py, float cx, float cy, float r) => new Vector2(px - cx, py - cy).magnitude - r;

        /// <summary>9-sliced rounded rectangle.</summary>
        public static Sprite RoundedRect => Make("rounded", 64, (x, y) => RoundedBox(x, y, 32, 32, 31, 31, 22), 26);
        public static Sprite RoundedOutline => Make("rounded_outline", 64, (x, y) => Mathf.Abs(RoundedBox(x, y, 32, 32, 29, 29, 20)) - 2.5f, 26);
        public static Sprite Pill => Make("pill", 64, (x, y) => RoundedBox(x, y, 32, 32, 31, 31, 31), 31);
        public static Sprite CircleSprite => Make("circle", 96, (x, y) => Circle(x, y, 48, 48, 46));
        public static Sprite Ring => Make("ring", 96, (x, y) => Mathf.Abs(Circle(x, y, 48, 48, 41)) - 5f);
        public static Sprite SoftGlow => Make("glow", 96, (x, y) => Circle(x, y, 48, 48, 4), 0, 40f);

        public static Sprite Star => Make("star", 96, (x, y) =>
        {
            float dx = x - 48, dy = y - 46;
            float a = Mathf.Atan2(dx, dy);
            float r = new Vector2(dx, dy).magnitude;
            float k = Mathf.Cos(Mathf.Repeat(a + Mathf.PI / 5f, 2f * Mathf.PI / 5f) - Mathf.PI / 5f);
            float radius = Mathf.Lerp(20f, 44f, Mathf.Pow(Mathf.Clamp01((k - 0.809f) / 0.191f), 1.4f));
            return r - radius;
        });

        public static Sprite Heart => Make("heart", 96, (x, y) =>
        {
            float px = (x - 48) / 40f, py = (y - 40) / 40f;
            float v = Mathf.Pow(px * px + py * py - 0.6f, 3) - px * px * py * py * py * 1.1f;
            return v * 60f;
        }, 0, 2f);

        public static Sprite Gem => Make("gem", 96, (x, y) =>
        {
            float dx = Mathf.Abs(x - 48), dy = y - 48;
            float top = dy - 26;                       // flat top
            float sides = dx * 0.9f + Mathf.Abs(dy - 14) * 0.25f - 40;
            float bottom = dx * 1.05f - dy - 30;       // pointed bottom
            return Mathf.Max(top, Mathf.Max(sides, bottom * 0.75f));
        });

        public static Sprite Coin => Make("coin", 96, (x, y) => Circle(x, y, 48, 48, 44));
        public static Sprite CoinInner => Make("coin_inner", 96, (x, y) => Mathf.Abs(Circle(x, y, 48, 48, 32)) - 4f);

        public static Sprite Lock => Make("lock", 96, (x, y) =>
        {
            float body = RoundedBox(x, y, 48, 36, 28, 22, 6);
            float shackle = Mathf.Abs(Circle(x, y, 48, 60, 18)) - 6f;
            if (y < 58) shackle = Mathf.Max(shackle, Mathf.Abs(x - 48) < 12 ? 10f : Mathf.Abs(Mathf.Abs(x - 48) - 18) - 6f);
            return Mathf.Min(body, shackle);
        });

        public static Sprite Pause => Make("pause", 96, (x, y) => Mathf.Min(RoundedBox(x, y, 34, 48, 9, 30, 4), RoundedBox(x, y, 62, 48, 9, 30, 4)));

        public static Sprite Play => Make("play", 96, (x, y) =>
        {
            float px = x - 40, py = y - 48;
            return Mathf.Max(Mathf.Max(-px - 18, px * 0.87f + Mathf.Abs(py) * 0.5f - 22f), -999f);
        });

        public static Sprite Shield => Make("shield", 96, (x, y) =>
        {
            float px = Mathf.Abs(x - 48), py = y - 48;
            float top = RoundedBox(x, y, 48, 60, 34, 22, 8);
            float bottom = px * 0.9f - (py + 40) * 0.7f;
            return py > 36 ? top : Mathf.Max(Mathf.Min(top, bottom), py - 80);
        });

        public static Sprite Gear => Make("gear", 96, (x, y) =>
        {
            float dx = x - 48, dy = y - 48;
            float r = new Vector2(dx, dy).magnitude;
            float a = Mathf.Atan2(dy, dx);
            float teeth = Mathf.Clamp(Mathf.Cos(a * 8f) * 6f, -2f, 4f);
            float outer = r - (34 + teeth);
            float hole = 13 - r;
            return Mathf.Max(outer, hole);
        });

        public static Sprite Trophy => Make("trophy", 96, (x, y) =>
        {
            float cup = RoundedBox(x, y, 48, 60, 24, 22, 14);
            float stem = RoundedBox(x, y, 48, 30, 6, 12, 2);
            float baseBox = RoundedBox(x, y, 48, 16, 20, 6, 3);
            float handles = Mathf.Abs(Circle(x, y, 22, 62, 12)) - 4f;
            float handles2 = Mathf.Abs(Circle(x, y, 74, 62, 12)) - 4f;
            return Mathf.Min(Mathf.Min(cup, stem), Mathf.Min(baseBox, Mathf.Min(handles, handles2)));
        });

        public static Sprite BalloonIcon => Make("balloon_icon", 96, (x, y) =>
        {
            float envelope = Circle(x, y, 48, 58, 30);
            float basket = RoundedBox(x, y, 48, 14, 10, 8, 2);
            float neck = Mathf.Max(Mathf.Abs(x - 48) - (y - 14) * 0.25f - 2f, Mathf.Max(y - 34, 20 - y));
            return Mathf.Min(envelope, Mathf.Min(basket, neck));
        });

        public static Sprite Wrench => Make("wrench", 96, (x, y) =>
        {
            Vector2 p = new Vector2(x - 48, y - 48);
            Vector2 d = new Vector2(0.7071f, 0.7071f);
            float along = Vector2.Dot(p, d);
            Vector2 perp = p - d * along;
            float handle = Mathf.Max(perp.magnitude - 7f, Mathf.Abs(along) - 30f);
            float head = Circle(x, y, 70, 70, 16);
            float notch = Circle(x, y, 78, 78, 9);
            return Mathf.Min(handle, Mathf.Max(head, -notch));
        });

        public static Sprite Arrow => Make("arrow_left", 96, (x, y) =>
        {
            float px = x - 48, py = y - 48;
            float head = Mathf.Max(-px - 26, Mathf.Abs(py) * 0.95f + px - 6);
            float shaft = RoundedBox(x, y, 58, 48, 22, 7, 3);
            return Mathf.Min(head, shaft);
        });

        public static Sprite Check => Make("check", 96, (x, y) =>
        {
            Vector2 p = new Vector2(x, y);
            float SegmentDist(Vector2 a, Vector2 b)
            {
                Vector2 pa = p - a, ba = b - a;
                float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
                return (pa - ba * h).magnitude;
            }
            return Mathf.Min(SegmentDist(new Vector2(22, 50), new Vector2(40, 30)), SegmentDist(new Vector2(40, 30), new Vector2(76, 70))) - 7f;
        });

        public static Sprite VerticalGradient(Color top, Color bottom)
        {
            string key = "grad_" + ColorKey(top) + ColorKey(bottom);
            if (cache.TryGetValue(key, out var s) && s != null) return s;
            var tex = new Texture2D(4, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color[4 * 64];
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 4; x++) px[y * 4 + x] = Color.Lerp(bottom, top, y / 63f);
            tex.SetPixels(px);
            tex.Apply(false, true);
            s = Sprite.Create(tex, new Rect(0, 0, 4, 64), new Vector2(0.5f, 0.5f), 100f);
            cache[key] = s;
            return s;
        }

        static string ColorKey(Color c) => ((int)(c.r * 255)).ToString("x2") + ((int)(c.g * 255)).ToString("x2") + ((int)(c.b * 255)).ToString("x2");

        /// <summary>Balloon pattern texture as a square sprite for shop cards.</summary>
        public static Sprite FromTexture(Texture2D texture, string key)
        {
            if (cache.TryGetValue(key, out var s) && s != null) return s;
            s = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            cache[key] = s;
            return s;
        }
    }
}
