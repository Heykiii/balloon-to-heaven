using System.Collections.Generic;
using BalloonToHeaven.Worlds;
using UnityEngine;
using UnityEngine.Rendering;

namespace BalloonToHeaven.Visuals
{
    /// <summary>Creates and caches materials for the custom shaders so identical looks share one material (batching).</summary>
    public static class MaterialLibrary
    {
        static readonly Dictionary<long, Material> cache = new Dictionary<long, Material>();
        static Shader toon, unlit, particle, sky, shield;

        public static readonly int ColorId = Shader.PropertyToID("_Color");
        public static readonly int EmissionId = Shader.PropertyToID("_Emission");
        public static readonly int FlashId = Shader.PropertyToID("_Flash");
        public static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        public static readonly int AlphaId = Shader.PropertyToID("_Alpha");

        static Shader Find(ref Shader slot, string name, string fallback)
        {
            if (slot != null) return slot;
            slot = Shader.Find(name);
            if (slot == null)
            {
                Debug.LogWarning("[Materials] Shader " + name + " not found, using " + fallback);
                slot = Shader.Find(fallback);
            }
            if (slot == null) slot = Shader.Find("Sprites/Default");
            return slot;
        }

        public static Shader ToonShader => Find(ref toon, "BalloonToHeaven/ToonLit", "Unlit/Color");
        public static Shader UnlitShader => Find(ref unlit, "BalloonToHeaven/Unlit", "Sprites/Default");
        public static Shader ParticleShader => Find(ref particle, "BalloonToHeaven/Particle", "Sprites/Default");
        public static Shader SkyShader => Find(ref sky, "BalloonToHeaven/Sky", "Unlit/Color");
        public static Shader ShieldShader => Find(ref shield, "BalloonToHeaven/Shield", "Sprites/Default");

        static long Key(int kind, Color c, float a = 0, float b = 0, int tex = 0)
        {
            unchecked
            {
                long k = kind;
                k = k * 397 + Mathf.RoundToInt(c.r * 255);
                k = k * 397 + Mathf.RoundToInt(c.g * 255);
                k = k * 397 + Mathf.RoundToInt(c.b * 255);
                k = k * 397 + Mathf.RoundToInt(c.a * 255);
                k = k * 397 + Mathf.RoundToInt(a * 100);
                k = k * 397 + Mathf.RoundToInt(b * 100);
                k = k * 397 + tex;
                return k;
            }
        }

        /// <summary>Stylised lit material. Shared and cached: do not modify the returned instance.</summary>
        public static Material Toon(Color color, float emission = 0f, float gloss = 0.25f, Texture texture = null, float rim = 0.6f)
        {
            long key = Key(1, color, emission + rim * 7f, gloss, texture != null ? texture.GetInstanceID() : 0);
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(ToonShader) { name = "Toon" };
            m.SetColor(ColorId, color);
            m.SetColor(EmissionId, color * emission);
            m.SetFloat("_Gloss", gloss);
            m.SetFloat("_Rim", rim);
            if (texture != null) m.SetTexture(MainTexId, texture);
            m.enableInstancing = true;
            cache[key] = m;
            return m;
        }

        /// <summary>Unique (uncached) lit material for objects that animate colour, e.g. the balloon flash.</summary>
        public static Material ToonInstance(Color color, float emission = 0f, float gloss = 0.25f, Texture texture = null, float rim = 0.6f)
        {
            var m = new Material(Toon(color, emission, gloss, texture, rim));
            return m;
        }

        public static Material Unlit(Color color, Texture texture = null, bool additive = false)
        {
            long key = Key(additive ? 3 : 2, color, 0, 0, texture != null ? texture.GetInstanceID() : 0);
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(UnlitShader) { name = additive ? "UnlitAdd" : "Unlit" };
            m.SetColor(ColorId, color);
            if (texture != null) m.SetTexture(MainTexId, texture);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            cache[key] = m;
            return m;
        }

        public static Material UnlitInstance(Color color, Texture texture = null, bool additive = false) => new Material(Unlit(color, texture, additive));

        public static Material Particle(bool additive, Texture texture = null)
        {
            long key = Key(additive ? 5 : 4, Color.white, 0, 0, texture != null ? texture.GetInstanceID() : 0);
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(ParticleShader) { name = additive ? "ParticleAdd" : "ParticleAlpha" };
            m.SetTexture(MainTexId, texture != null ? texture : ProceduralTextures.SoftCircle);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            cache[key] = m;
            return m;
        }

        public static Material Shield(Color color, float alpha = 0.8f, float power = 2.2f)
        {
            var m = new Material(ShieldShader) { name = "Shield" };
            m.SetColor(ColorId, color);
            m.SetFloat(AlphaId, alpha);
            m.SetFloat("_Power", power);
            return m;
        }

        public static Material Sky(Color top, Color bottom, Color glow)
        {
            var m = new Material(SkyShader) { name = "Sky" };
            m.SetColor("_Top", top);
            m.SetColor("_Bottom", bottom);
            m.SetColor("_Glow", glow);
            return m;
        }
    }

    /// <summary>Sets the global light values consumed by the ToonLit shader.</summary>
    public static class EnvironmentLighting
    {
        static readonly int LightDir = Shader.PropertyToID("_BTH_LightDir");
        static readonly int LightColor = Shader.PropertyToID("_BTH_LightColor");
        static readonly int Ambient = Shader.PropertyToID("_BTH_Ambient");
        static readonly int Rim = Shader.PropertyToID("_BTH_RimColor");

        public static void Apply(Vector3 direction, Color light, Color ambient, Color rim)
        {
            var d = direction.sqrMagnitude > 0.0001f ? direction.normalized : new Vector3(-0.4f, 0.7f, -0.6f).normalized;
            Shader.SetGlobalVector(LightDir, new Vector4(d.x, d.y, d.z, 0));
            Shader.SetGlobalColor(LightColor, light * 0.75f);
            Shader.SetGlobalColor(Ambient, ambient * 0.75f);
            Shader.SetGlobalColor(Rim, rim * 0.55f);
        }

        public static void Apply(WorldData world)
        {
            if (world == null) { ApplyDefault(); return; }
            Apply(world.lightDirection, world.lightColor, world.ambientColor, world.rimColor);
        }

        public static void ApplyDefault()
        {
            Apply(new Vector3(-0.4f, 0.7f, -0.6f), new Color(1f, 0.96f, 0.88f), new Color(0.55f, 0.6f, 0.72f), new Color(0.9f, 0.95f, 1f));
        }
    }
}
