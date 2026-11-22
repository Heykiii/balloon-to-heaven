using System.Collections.Generic;
using BalloonToHeaven.Core;
using BalloonToHeaven.Worlds;
using UnityEngine;

namespace BalloonToHeaven.Visuals
{
    /// <summary>
    /// Scrolls parallax layers downward to create the feeling of ascent. Items that leave the
    /// bottom of the screen wrap back to the top, so the background is endless and allocation free.
    /// </summary>
    public sealed class EnvironmentController : MonoBehaviour
    {
        class Layer
        {
            public float factor;
            public float span;
            public readonly List<Transform> items = new List<Transform>();
            public readonly List<float> drift = new List<float>();
        }

        readonly List<Layer> layers = new List<Layer>();
        readonly List<Renderer> flashers = new List<Renderer>();
        float bottom, top;
        float flashTimer;
        public float AutoScrollSpeed { get; set; }

        public void Configure(float bottomY, float topY)
        {
            bottom = bottomY;
            top = topY;
        }

        public void AddLayer(float factor, List<Transform> items, List<float> drift)
        {
            var layer = new Layer { factor = factor, span = top - bottom };
            layer.items.AddRange(items);
            layer.drift.AddRange(drift);
            layers.Add(layer);
        }

        public void AddFlasher(Renderer r) { if (r != null) { flashers.Add(r); r.enabled = false; } }

        /// <summary>Moves all layers by the given world ascent distance.</summary>
        public void Scroll(float distance)
        {
            for (int l = 0; l < layers.Count; l++)
            {
                var layer = layers[l];
                float d = distance * layer.factor;
                for (int i = 0; i < layer.items.Count; i++)
                {
                    var t = layer.items[i];
                    if (t == null) continue;
                    var p = t.localPosition;
                    p.y -= d;
                    p.x += layer.drift[i] * Time.deltaTime;
                    if (p.y < bottom) p.y += layer.span;
                    if (p.x > 9f) p.x = -9f; else if (p.x < -9f) p.x = 9f;
                    t.localPosition = p;
                }
            }
        }

        void Update()
        {
            if (AutoScrollSpeed > 0f) Scroll(AutoScrollSpeed * Time.deltaTime);
            if (flashers.Count > 0)
            {
                flashTimer -= Time.deltaTime;
                if (flashTimer <= 0f)
                {
                    var r = flashers[Random.Range(0, flashers.Count)];
                    if (r != null) StartCoroutine(Flash(r));
                    flashTimer = Random.Range(1.5f, 4f);
                }
            }
        }

        System.Collections.IEnumerator Flash(Renderer r)
        {
            r.enabled = true;
            yield return new WaitForSeconds(0.08f);
            if (r != null) r.enabled = false;
            yield return new WaitForSeconds(0.07f);
            if (r != null) r.enabled = true;
            yield return new WaitForSeconds(0.1f);
            if (r != null) r.enabled = false;
        }
    }

    /// <summary>Builds a world's sky, multi-depth parallax scenery and weather.</summary>
    public static class EnvironmentBuilder
    {
        public static EnvironmentController Build(WorldData world, Camera camera, Transform parent, int seed = 1)
        {
            var root = new GameObject("Environment_" + (world != null ? world.worldName : "Default"));
            root.transform.SetParent(parent, false);
            var env = root.AddComponent<EnvironmentController>();
            float halfH = camera != null ? camera.orthographicSize : 10f;
            float halfW = halfH * (camera != null ? Mathf.Max(camera.aspect, 0.4f) : 0.5625f) + 1f;
            env.Configure(-halfH - 4f, halfH + 8f);
            var theme = world != null ? world.theme : WorldTheme.GreenValley;
            Color top = world != null ? world.skyTop : new Color(0.3f, 0.6f, 1f);
            Color bottom = world != null ? world.skyBottom : new Color(0.8f, 0.9f, 1f);
            Color cloud = world != null ? world.cloudColor : Color.white;
            Color ground = world != null ? world.groundColor : new Color(0.4f, 0.75f, 0.4f);
            Color accent = world != null ? world.accentColor : Color.yellow;

            // Sky backdrop (fixed)
            var glow = new Color(accent.r, accent.g, accent.b, theme == WorldTheme.HeavensGate || theme == WorldTheme.GoldenClouds ? 0.5f : 0.18f);
            var sky = ModelKit.Part("Sky", root.transform, MeshLibrary.Quad, MaterialLibrary.Sky(top, bottom, glow), new Vector3(0, 0, 80f), new Vector3(halfW * 2.4f + 4f, halfH * 2.4f, 1f));
            sky.sortingOrder = -100;

            var rng = new System.Random(seed + (int)theme * 131);
            float span = halfH * 2f + 12f;
            BuildLayer(env, root.transform, rng, theme, 0, 7, 0.08f, 55f, span, halfH, halfW, top, bottom, cloud, ground, accent);
            BuildLayer(env, root.transform, rng, theme, 1, 6, 0.25f, 35f, span, halfH, halfW, top, bottom, cloud, ground, accent);
            BuildLayer(env, root.transform, rng, theme, 2, 9, 0.5f, 18f, span, halfH, halfW, top, bottom, cloud, ground, accent);
            BuildLayer(env, root.transform, rng, theme, 3, 5, 1.35f, -6f, span, halfH, halfW, top, bottom, cloud, ground, accent);

            if (world != null) AddWeather(world.weather, camera != null ? camera.transform : root.transform, halfW, halfH);
            if (theme == WorldTheme.Thunderstorm)
            {
                var flash = ModelKit.Part("LightningFlash", root.transform, MeshLibrary.Quad, MaterialLibrary.Unlit(new Color(0.8f, 0.85f, 1f, 0.25f), null, true), new Vector3(0, 0, 50f), new Vector3(halfW * 3f, halfH * 3f, 1f));
                env.AddFlasher(flash);
            }
            return env;
        }

        static void BuildLayer(EnvironmentController env, Transform root, System.Random rng, WorldTheme theme, int layerIndex, int count, float factor, float z,
            float span, float halfH, float halfW, Color top, Color bottom, Color cloud, Color ground, Color accent)
        {
            var layerRoot = ModelKit.Node("Layer" + layerIndex, root, new Vector3(0, 0, z)).transform;
            var items = new List<Transform>();
            var drift = new List<float>();
            count = Mathf.Max(2, Mathf.RoundToInt(count * Mathf.Lerp(0.6f, 1f, QualityManager.EffectsDensity)));
            // Distance tint makes far layers fade into the sky for depth.
            float haze = layerIndex == 0 ? 0.55f : layerIndex == 1 ? 0.3f : 0f;
            Color skyMid = Color.Lerp(bottom, top, 0.4f);
            for (int i = 0; i < count; i++)
            {
                float y = -halfH - 4f + (i + (float)rng.NextDouble() * 0.6f) / count * span;
                float x = ((float)rng.NextDouble() * 2f - 1f) * halfW * (layerIndex == 3 ? 1.1f : 0.95f);
                Transform item;
                if (layerIndex == 3)
                {
                    // Foreground: fast, faint cloud wisps passing in front for depth.
                    item = CloudPuff(layerRoot, cloud, 0.28f, 4f + (float)rng.NextDouble() * 3f);
                }
                else if (layerIndex == 2 && i % 2 == 0)
                {
                    item = CloudCluster(layerRoot, Color.Lerp(cloud, skyMid, 0.1f), rng, 0.8f + (float)rng.NextDouble() * 0.7f);
                }
                else if (layerIndex == 1 && i % 3 == 0)
                {
                    item = CloudPuff(layerRoot, Color.Lerp(cloud, skyMid, haze), 0.75f, 5f + (float)rng.NextDouble() * 4f);
                }
                else
                {
                    item = Decor(theme, layerIndex, layerRoot, rng, Tint(ground, skyMid, haze), Tint(accent, skyMid, haze * 0.7f), Tint(cloud, skyMid, haze));
                }
                item.localPosition = new Vector3(x, y, (float)rng.NextDouble() * 2f);
                items.Add(item);
                drift.Add(layerIndex >= 2 ? ((float)rng.NextDouble() - 0.5f) * 0.3f : 0f);
            }
            env.AddLayer(factor, items, drift);
        }

        static Color Tint(Color c, Color sky, float haze) => Color.Lerp(c, sky, haze);

        static Transform CloudPuff(Transform parent, Color color, float alpha, float size)
        {
            var r = ModelKit.Part("CloudPuff", parent, MeshLibrary.Quad, MaterialLibrary.Unlit(new Color(color.r, color.g, color.b, alpha), ProceduralTextures.CloudPuff), Vector3.zero, new Vector3(size * 1.6f, size, 1f));
            return r.transform;
        }

        static Transform CloudCluster(Transform parent, Color color, System.Random rng, float scale)
        {
            var node = ModelKit.Node("CloudCluster", parent).transform;
            var mat = MaterialLibrary.Toon(color, 0.08f, 0.05f, null, 0.9f);
            int puffs = 4 + rng.Next(3);
            for (int i = 0; i < puffs; i++)
            {
                float px = (i - puffs * 0.5f) * 0.65f;
                float py = Mathf.Sin(i * 1.7f) * 0.25f + (i % 2) * 0.3f;
                float s = 0.9f + (float)rng.NextDouble() * 0.8f;
                ModelKit.Part("Puff", node, MeshLibrary.LowSphere, mat, new Vector3(px, py, 0), new Vector3(s * 1.2f, s, s));
            }
            node.localScale = Vector3.one * scale;
            return node;
        }

        static Transform Island(Transform parent, Color top, Color earth, float size, System.Random rng, bool trees, Color treeColor)
        {
            var node = ModelKit.Node("Island", parent).transform;
            ModelKit.Part("Rock", node, MeshLibrary.Cone, MaterialLibrary.Toon(earth, 0f, 0.1f), new Vector3(0, -0.45f, 0), new Vector3(1.6f, 1.1f, 1.2f), new Vector3(180, 0, 0));
            ModelKit.Part("Top", node, MeshLibrary.Cylinder, MaterialLibrary.Toon(top, 0.02f, 0.15f), new Vector3(0, 0.1f, 0), new Vector3(1.7f, 0.22f, 1.3f));
            if (trees)
            {
                int n = 1 + rng.Next(3);
                for (int i = 0; i < n; i++)
                {
                    float x = -0.5f + i * 0.5f;
                    ModelKit.Part("Trunk", node, MeshLibrary.Cylinder, MaterialLibrary.Toon(new Color(0.45f, 0.3f, 0.2f)), new Vector3(x, 0.4f, -0.1f), new Vector3(0.1f, 0.5f, 0.1f));
                    ModelKit.Part("Leaves", node, MeshLibrary.LowSphere, MaterialLibrary.Toon(treeColor, 0.02f, 0.1f), new Vector3(x, 0.78f, -0.1f), Vector3.one * (0.45f + (float)rng.NextDouble() * 0.2f));
                }
            }
            node.localScale = Vector3.one * size;
            return node;
        }

        static Transform Decor(WorldTheme theme, int layer, Transform parent, System.Random rng, Color ground, Color accent, Color cloud)
        {
            float size = layer == 0 ? 3.5f + (float)rng.NextDouble() * 2.5f : 1.2f + (float)rng.NextDouble() * 1.0f;
            switch (theme)
            {
                case WorldTheme.GreenValley:
                    if (layer == 0) return Hill(parent, ground, size);
                    return Island(parent, ground, new Color(0.55f, 0.38f, 0.25f), size, rng, true, Color.Lerp(ground, new Color(0.2f, 0.5f, 0.2f), 0.5f));
                case WorldTheme.MountainKingdom:
                    if (layer == 0) return Peak(parent, ground, Color.white, size);
                    return Rocky(parent, ground, size * 0.8f);
                case WorldTheme.RainbowSkies:
                    if (layer == 0) return Rainbow(parent, size * 1.3f);
                    {
                        var island = Island(parent, ground, new Color(0.9f, 0.7f, 0.85f), size, rng, true, accent);
                        Waterfall(island);
                        return island;
                    }
                case WorldTheme.Thunderstorm:
                    return CloudCluster(parent, Color.Lerp(cloud, Color.black, layer == 0 ? 0.35f : 0.2f), rng, size * (layer == 0 ? 0.7f : 0.9f));
                case WorldTheme.FrozenHeaven:
                    if (layer == 0) return Peak(parent, new Color(0.75f, 0.85f, 1f), Color.white, size);
                    {
                        var island = Island(parent, Color.white, new Color(0.6f, 0.75f, 0.9f), size, rng, false, Color.white);
                        Crystals(island, new Color(0.7f, 0.9f, 1f), rng);
                        return island;
                    }
                case WorldTheme.FireKingdom:
                    if (layer == 0) return Volcano(parent, ground, accent, size);
                    return Rocky(parent, ground, size * 0.8f, accent);
                case WorldTheme.CosmicJourney:
                    if (layer == 0) return Planet(parent, rng, size * 0.6f);
                    return Rocky(parent, new Color(0.35f, 0.32f, 0.4f), size * 0.5f);
                case WorldTheme.GoldenClouds:
                    if (layer == 0) return CloudCluster(parent, Color.Lerp(cloud, accent, 0.25f), rng, size * 0.8f);
                    return Temple(parent, ground, accent, size * 0.8f);
                case WorldTheme.CelestialRealm:
                    if (layer == 0) return Spire(parent, accent, size);
                    {
                        var island = Island(parent, ground, new Color(0.5f, 0.45f, 0.8f), size, rng, false, Color.white);
                        Crystals(island, accent, rng);
                        return island;
                    }
                default: // Heaven's Gate
                    if (layer == 0) return Castle(parent, Color.white, accent, size * 0.8f);
                    return Gate(parent, accent, size * 0.9f);
            }
        }

        static Transform Hill(Transform parent, Color color, float size)
        {
            var r = ModelKit.Part("Hill", parent, MeshLibrary.Hemisphere, MaterialLibrary.Toon(color, 0f, 0.05f), Vector3.zero, new Vector3(size * 2f, size, size));
            return r.transform;
        }

        static Transform Peak(Transform parent, Color rock, Color snow, float size)
        {
            var node = ModelKit.Node("Peak", parent).transform;
            ModelKit.Part("Mountain", node, MeshLibrary.Cone, MaterialLibrary.Toon(rock, 0f, 0.1f), Vector3.zero, new Vector3(1.6f, 2f, 1f));
            ModelKit.Part("Snow", node, MeshLibrary.Cone, MaterialLibrary.Toon(snow, 0.05f, 0.2f), new Vector3(0, 0.65f, -0.05f), new Vector3(0.62f, 0.72f, 0.42f));
            node.localScale = Vector3.one * size;
            return node;
        }

        static Transform Rocky(Transform parent, Color color, float size, Color? glow = null)
        {
            var node = ModelKit.Node("Rock", parent).transform;
            var mat = MaterialLibrary.Toon(color, 0f, 0.1f);
            ModelKit.Part("A", node, MeshLibrary.Octahedron, mat, Vector3.zero, new Vector3(1.3f, 1f, 1.2f), new Vector3(10, 20, 30));
            ModelKit.Part("B", node, MeshLibrary.Octahedron, mat, new Vector3(0.6f, -0.2f, 0), new Vector3(0.7f, 0.6f, 0.7f), new Vector3(40, 0, 10));
            if (glow.HasValue)
            {
                ModelKit.Part("Lava", node, MeshLibrary.LowSphere, MaterialLibrary.Toon(glow.Value, 1f), new Vector3(0, -0.35f, -0.3f), new Vector3(0.5f, 0.2f, 0.3f));
                ModelKit.Glow("Glow", node, new Vector3(0, -0.4f, -0.5f), 1.4f, new Color(glow.Value.r, glow.Value.g, glow.Value.b, 0.5f));
            }
            node.localScale = Vector3.one * size;
            return node;
        }

        static Transform Rainbow(Transform parent, float size)
        {
            var node = ModelKit.Node("Rainbow", parent).transform;
            Color[] colors = { new Color(1f, 0.35f, 0.35f), new Color(1f, 0.65f, 0.25f), new Color(1f, 0.92f, 0.35f), new Color(0.4f, 0.85f, 0.45f), new Color(0.35f, 0.6f, 1f), new Color(0.65f, 0.45f, 1f) };
            for (int i = 0; i < colors.Length; i++)
            {
                float r = 1f - i * 0.09f;
                ModelKit.Part("Band" + i, node, MeshLibrary.Arc, MaterialLibrary.Unlit(new Color(colors[i].r, colors[i].g, colors[i].b, 0.55f)), Vector3.zero, new Vector3(r * 2f, r * 2f, 1f));
            }
            node.localScale = Vector3.one * size;
            return node;
        }

        static void Waterfall(Transform island)
        {
            var s = ParticleSettings.Default;
            s.colorA = new Color(0.75f, 0.9f, 1f, 0.8f); s.colorB = Color.white; s.shape = ParticleSystemShapeType.Box; s.boxSize = new Vector3(0.3f, 0.05f, 0.05f);
            s.rate = 14f; s.velocity = new Vector3(0, -2.5f, 0); s.sizeMin = 0.12f; s.sizeMax = 0.25f; s.lifeMin = 0.6f; s.lifeMax = 0.9f; s.worldSpace = false; s.additive = false;
            ParticleFactory.Create("Waterfall", island, new Vector3(0.75f, 0.05f, -0.3f), s);
        }

        static void Crystals(Transform island, Color color, System.Random rng)
        {
            var mat = MaterialLibrary.Toon(color, 0.35f, 0.9f, null, 1.2f);
            for (int i = 0; i < 3; i++)
            {
                float x = -0.5f + i * 0.5f;
                float h = 0.5f + (float)rng.NextDouble() * 0.5f;
                ModelKit.Part("Crystal", island, MeshLibrary.Octahedron, mat, new Vector3(x, 0.2f + h * 0.4f, -0.1f), new Vector3(0.3f, h, 0.3f), new Vector3(0, 0, (i - 1) * 12));
            }
        }

        static Transform Volcano(Transform parent, Color rock, Color lava, float size)
        {
            var node = ModelKit.Node("Volcano", parent).transform;
            ModelKit.Part("Cone", node, MeshLibrary.Cone, MaterialLibrary.Toon(rock, 0f, 0.1f), Vector3.zero, new Vector3(1.8f, 1.6f, 1f));
            ModelKit.Part("Crater", node, MeshLibrary.LowSphere, MaterialLibrary.Toon(lava, 1.2f), new Vector3(0, 0.75f, -0.1f), new Vector3(0.35f, 0.15f, 0.2f));
            ModelKit.Glow("Glow", node, new Vector3(0, 0.95f, -0.3f), 1.6f, new Color(lava.r, lava.g, lava.b, 0.55f));
            var s = ParticleSettings.Default;
            s.colorA = new Color(0.25f, 0.2f, 0.2f, 0.5f); s.colorB = new Color(0.4f, 0.3f, 0.3f, 0.3f); s.additive = false;
            s.rate = 3f; s.velocity = new Vector3(0.2f, 0.8f, 0); s.sizeMin = 0.4f; s.sizeMax = 0.8f; s.lifeMin = 2f; s.lifeMax = 3f; s.radius = 0.1f; s.worldSpace = false;
            ParticleFactory.Create("Smoke", node, new Vector3(0, 0.85f, 0), s);
            node.localScale = Vector3.one * size;
            return node;
        }

        static Transform Planet(Transform parent, System.Random rng, float size)
        {
            var node = ModelKit.Node("Planet", parent).transform;
            Color c = Color.HSVToRGB((float)rng.NextDouble(), 0.45f, 0.8f);
            ModelKit.Part("Sphere", node, MeshLibrary.Sphere, MaterialLibrary.Toon(c, 0.05f, 0.3f), Vector3.zero, Vector3.one * 2f);
            if (rng.NextDouble() > 0.4)
                ModelKit.Part("Ring", node, MeshLibrary.Torus, MaterialLibrary.Toon(Color.Lerp(c, Color.white, 0.5f), 0.1f, 0.4f), Vector3.zero, new Vector3(3.2f, 0.4f, 3.2f), new Vector3(15, 0, -18));
            ModelKit.Glow("Nebula", node, new Vector3(0, 0, 1f), 6f, new Color(c.r, c.g, c.b, 0.18f));
            node.localScale = Vector3.one * size;
            return node;
        }

        static Transform Temple(Transform parent, Color stone, Color gold, float size)
        {
            var node = ModelKit.Node("Temple", parent).transform;
            var stoneMat = MaterialLibrary.Toon(Color.Lerp(stone, Color.white, 0.4f), 0.05f, 0.3f);
            var goldMat = MaterialLibrary.Toon(gold, 0.2f, 0.8f);
            ModelKit.Part("Base", node, MeshLibrary.Cube, stoneMat, new Vector3(0, -0.5f, 0), new Vector3(1.8f, 0.2f, 1f));
            for (int i = 0; i < 4; i++) ModelKit.Part("Column", node, MeshLibrary.Cylinder, stoneMat, new Vector3(-0.7f + i * 0.47f, 0, -0.2f), new Vector3(0.18f, 0.8f, 0.18f));
            ModelKit.Part("Roof", node, MeshLibrary.Cube, goldMat, new Vector3(0, 0.5f, 0), new Vector3(1.9f, 0.18f, 1f));
            ModelKit.Part("Pediment", node, MeshLibrary.Cone, goldMat, new Vector3(0, 0.75f, 0), new Vector3(2f, 0.35f, 0.9f));
            ModelKit.Part("Cloud", node, MeshLibrary.LowSphere, MaterialLibrary.Toon(Color.white, 0.1f, 0.05f), new Vector3(0, -0.7f, 0), new Vector3(2.4f, 0.5f, 1.2f));
            node.localScale = Vector3.one * size;
            return node;
        }

        static Transform Spire(Transform parent, Color color, float size)
        {
            var node = ModelKit.Node("Spire", parent).transform;
            var mat = MaterialLibrary.Toon(color, 0.3f, 0.9f, null, 1.2f);
            ModelKit.Part("Main", node, MeshLibrary.Octahedron, mat, Vector3.zero, new Vector3(0.8f, 3f, 0.8f));
            ModelKit.Part("Side", node, MeshLibrary.Octahedron, mat, new Vector3(0.5f, -0.6f, 0), new Vector3(0.45f, 1.6f, 0.45f), new Vector3(0, 0, -15));
            ModelKit.Glow("Glow", node, new Vector3(0, 0, 0.5f), 3f, new Color(color.r, color.g, color.b, 0.25f));
            node.localScale = Vector3.one * size * 0.6f;
            return node;
        }

        static Transform Castle(Transform parent, Color wall, Color gold, float size)
        {
            var node = ModelKit.Node("Castle", parent).transform;
            var wallMat = MaterialLibrary.Toon(wall, 0.15f, 0.4f);
            var goldMat = MaterialLibrary.Toon(gold, 0.35f, 0.9f);
            ModelKit.Part("Keep", node, MeshLibrary.Cube, wallMat, Vector3.zero, new Vector3(1.4f, 1.2f, 0.8f));
            for (int i = -1; i <= 1; i++)
            {
                float h = i == 0 ? 2.2f : 1.6f;
                ModelKit.Part("Tower", node, MeshLibrary.Cylinder, wallMat, new Vector3(i * 0.8f, h * 0.5f - 0.3f, -0.1f), new Vector3(0.45f, h, 0.45f));
                ModelKit.Part("Roof", node, MeshLibrary.Cone, goldMat, new Vector3(i * 0.8f, h - 0.1f, -0.1f), new Vector3(0.6f, 0.6f, 0.6f));
            }
            ModelKit.Part("Cloud", node, MeshLibrary.LowSphere, MaterialLibrary.Toon(Color.white, 0.2f, 0.05f), new Vector3(0, -0.7f, 0), new Vector3(3f, 0.6f, 1.2f));
            ModelKit.Glow("Glow", node, new Vector3(0, 0.6f, 0.6f), 4f, new Color(gold.r, gold.g, gold.b, 0.3f));
            node.localScale = Vector3.one * size;
            return node;
        }

        static Transform Gate(Transform parent, Color gold, float size)
        {
            var node = ModelKit.Node("Gate", parent).transform;
            var goldMat = MaterialLibrary.Toon(gold, 0.3f, 0.9f);
            ModelKit.Part("PillarL", node, MeshLibrary.Cylinder, goldMat, new Vector3(-0.7f, 0, 0), new Vector3(0.25f, 1.6f, 0.25f));
            ModelKit.Part("PillarR", node, MeshLibrary.Cylinder, goldMat, new Vector3(0.7f, 0, 0), new Vector3(0.25f, 1.6f, 0.25f));
            ModelKit.Part("Arch", node, MeshLibrary.Arc, MaterialLibrary.Unlit(gold), new Vector3(0, 0.8f, 0), new Vector3(1.4f, 1.1f, 1f));
            ModelKit.Part("Cloud", node, MeshLibrary.LowSphere, MaterialLibrary.Toon(Color.white, 0.2f, 0.05f), new Vector3(0, -0.85f, 0), new Vector3(2.4f, 0.5f, 1f));
            node.localScale = Vector3.one * size;
            return node;
        }

        public static ParticleSystem AddWeather(WeatherEffect weather, Transform cameraTransform, float halfW, float halfH)
        {
            if (weather == WeatherEffect.None) return null;
            var s = ParticleSettings.Default;
            s.shape = ParticleSystemShapeType.Box;
            s.boxSize = new Vector3(halfW * 2.2f, 0.5f, 1f);
            s.worldSpace = true;
            s.speedMin = 0f; s.speedMax = 0.1f;
            Vector3 pos = new Vector3(0, halfH + 1f, 25f);
            switch (weather)
            {
                case WeatherEffect.Rain:
                    s.colorA = new Color(0.7f, 0.8f, 1f, 0.5f); s.colorB = new Color(0.8f, 0.9f, 1f, 0.3f); s.velocity = new Vector3(-1.5f, -22f, 0);
                    s.rate = 70f; s.sizeMin = 0.08f; s.sizeMax = 0.14f; s.lifeMin = 1.2f; s.lifeMax = 1.4f; s.texture = ProceduralTextures.Streak; s.additive = false; s.maxParticles = 200;
                    break;
                case WeatherEffect.Snow:
                    s.colorA = Color.white; s.colorB = new Color(0.85f, 0.95f, 1f); s.velocity = new Vector3(0.4f, -2.2f, 0);
                    s.rate = 25f; s.sizeMin = 0.1f; s.sizeMax = 0.25f; s.lifeMin = 9f; s.lifeMax = 11f; s.additive = false; s.maxParticles = 300;
                    break;
                case WeatherEffect.Embers:
                    s.colorA = new Color(1f, 0.5f, 0.1f); s.colorB = new Color(1f, 0.85f, 0.3f); s.velocity = new Vector3(0.3f, 1.8f, 0);
                    pos = new Vector3(0, -halfH - 1f, 25f); s.rate = 18f; s.sizeMin = 0.06f; s.sizeMax = 0.16f; s.lifeMin = 9f; s.lifeMax = 12f; s.maxParticles = 220;
                    break;
                case WeatherEffect.Stars:
                    s.shape = ParticleSystemShapeType.Box; s.boxSize = new Vector3(halfW * 2.2f, halfH * 2.2f, 1f); pos = new Vector3(0, 0, 70f);
                    s.colorA = Color.white; s.colorB = new Color(0.7f, 0.85f, 1f); s.rate = 12f; s.sizeMin = 0.05f; s.sizeMax = 0.18f; s.lifeMin = 3f; s.lifeMax = 6f;
                    s.texture = ProceduralTextures.Sparkle; s.maxParticles = 80; s.velocity = new Vector3(0, -0.4f, 0);
                    break;
                case WeatherEffect.GoldDust:
                case WeatherEffect.LightMotes:
                case WeatherEffect.Sparkles:
                    s.colorA = weather == WeatherEffect.GoldDust ? new Color(1f, 0.85f, 0.4f) : new Color(0.85f, 0.8f, 1f); s.colorB = Color.white;
                    s.velocity = new Vector3(0.1f, -1.2f, 0); s.rate = 10f; s.sizeMin = 0.06f; s.sizeMax = 0.16f; s.lifeMin = 12f; s.lifeMax = 16f; s.texture = ProceduralTextures.Sparkle; s.maxParticles = 160;
                    break;
                case WeatherEffect.Pollen:
                case WeatherEffect.Petals:
                    s.colorA = weather == WeatherEffect.Pollen ? new Color(1f, 1f, 0.8f, 0.8f) : new Color(1f, 0.7f, 0.85f, 0.9f); s.colorB = Color.white;
                    s.velocity = new Vector3(0.5f, -1.4f, 0); s.rate = 6f; s.sizeMin = 0.06f; s.sizeMax = 0.14f; s.lifeMin = 12f; s.lifeMax = 16f; s.additive = weather == WeatherEffect.Pollen; s.maxParticles = 120; s.rotationSpeed = 2f;
                    break;
            }
            var ps = ParticleFactory.Create("Weather_" + weather, cameraTransform, pos, s);
            // Prewarm so the screen is already filled when the level starts.
            ps.Clear();
            ps.Simulate(Mathf.Min(10f, s.lifeMax), true, true);
            ps.Play();
            return ps;
        }
    }
}
