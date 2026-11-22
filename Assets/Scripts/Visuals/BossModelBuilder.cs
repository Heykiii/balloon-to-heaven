using System.Collections.Generic;
using BalloonToHeaven.Bosses;
using UnityEngine;

namespace BalloonToHeaven.Visuals
{
    /// <summary>Animated boss model: flapping wings, spinning parts, body chains and attack/phase reactions.</summary>
    public sealed class BossView : MonoBehaviour
    {
        public Transform body;
        readonly List<(Transform t, Vector3 axis, float amp, float speed, Quaternion baseRot)> flappers = new List<(Transform, Vector3, float, float, Quaternion)>();
        readonly List<(Transform t, Vector3 speed)> spinners = new List<(Transform, Vector3)>();
        readonly List<(Transform t, int index, Vector3 basePos)> chain = new List<(Transform, int, Vector3)>();
        readonly List<Material> flashMaterials = new List<Material>();
        float punch;
        float flash;
        float intensity = 1f;
        float baseScale = 1f;

        public void AddFlapper(Transform t, Vector3 axis, float amplitude, float speed) => flappers.Add((t, axis, amplitude, speed, t.localRotation));
        public void AddSpinner(Transform t, Vector3 speed) => spinners.Add((t, speed));
        public void AddChain(Transform t, int index) => chain.Add((t, index, t.localPosition));
        public void AddFlashMaterial(Material m) => flashMaterials.Add(m);
        public void SetBaseScale(float s) { baseScale = s; if (body != null) body.localScale = Vector3.one * s; }

        /// <summary>Visual reaction when the boss launches an attack.</summary>
        public void AttackPulse() => punch = 1f;
        public void HitFlash() => flash = 0.3f;
        public void SetIntensity(float value) => intensity = value;

        void Update()
        {
            float t = Time.time;
            float dt = Time.deltaTime;
            foreach (var f in flappers)
                if (f.t != null) f.t.localRotation = f.baseRot * Quaternion.AngleAxis(Mathf.Sin(t * f.speed * intensity) * f.amp, f.axis);
            foreach (var s in spinners)
                if (s.t != null) s.t.Rotate(s.speed * intensity * dt, Space.Self);
            foreach (var c in chain)
                if (c.t != null) c.t.localPosition = c.basePos + new Vector3(0, Mathf.Sin(t * 3f * intensity - c.index * 0.7f) * 0.25f, 0);
            punch = Mathf.Max(0f, punch - dt * 4f);
            if (body != null) body.localScale = Vector3.one * baseScale * (1f + Mathf.Sin(punch * Mathf.PI) * 0.08f);
            if (flash > 0f || flashMaterials.Count > 0)
            {
                flash = Mathf.Max(0f, flash - dt);
                float v = flash > 0 ? 0.7f * flash / 0.3f : 0f;
                foreach (var m in flashMaterials) if (m != null) m.SetFloat(MaterialLibrary.FlashId, v);
            }
        }

        void OnDestroy()
        {
            foreach (var m in flashMaterials) if (m != null) Destroy(m);
        }
    }

    /// <summary>Builds the ten unique boss models.</summary>
    public static class BossModelBuilder
    {
        public static BossView Build(BossData data, Transform parent)
        {
            var root = new GameObject("Boss_" + data.id);
            root.transform.SetParent(parent, false);
            var view = root.AddComponent<BossView>();
            var body = ModelKit.Node("Body", root.transform).transform;
            view.body = body;
            Color a = data.primaryColor, b = data.secondaryColor, g = data.glowColor;
            var main = MaterialLibrary.ToonInstance(a, 0.05f, 0.4f);
            var second = MaterialLibrary.Toon(b, 0.1f, 0.6f);
            var glow = MaterialLibrary.Toon(g, 0.9f, 0.8f);
            view.AddFlashMaterial(main);
            ModelKit.Glow("Aura", body, new Vector3(0, 0, 1.5f), 7f, new Color(g.r, g.g, g.b, 0.35f));

            switch (data.visual)
            {
                case BossVisual.ValleyGuardian:
                case BossVisual.FirePhoenix:
                {
                    bool phoenix = data.visual == BossVisual.FirePhoenix;
                    ModelKit.Part("Torso", body, MeshLibrary.Sphere, main, Vector3.zero, new Vector3(1.6f, 1.3f, 1.1f));
                    ModelKit.Part("Chest", body, MeshLibrary.Sphere, second, new Vector3(0, -0.25f, -0.35f), new Vector3(1.0f, 0.9f, 0.6f));
                    ModelKit.Part("Head", body, MeshLibrary.Sphere, main, new Vector3(0, 0.95f, -0.1f), Vector3.one * 0.85f);
                    ModelKit.Part("Beak", body, MeshLibrary.Cone, MaterialLibrary.Toon(new Color(1f, 0.75f, 0.2f), 0.1f), new Vector3(0, 0.8f, -0.6f), new Vector3(0.25f, 0.45f, 0.25f), new Vector3(-100, 0, 0));
                    Eyes(body, new Vector3(0, 1.05f, -0.42f), 0.2f, phoenix ? new Color(1f, 0.9f, 0.3f) : new Color(0.1f, 0.1f, 0.1f), phoenix ? 0.8f : 0f);
                    for (int i = -1; i <= 1; i++)
                        ModelKit.Part("Crest" + i, body, MeshLibrary.Cone, second, new Vector3(i * 0.18f, 1.45f, 0), new Vector3(0.14f, 0.5f, 0.14f), new Vector3(0, 0, -i * 20));
                    Wings(view, body, phoenix ? MaterialLibrary.Toon(b, 0.4f, 0.4f) : main, 2.4f, 0.4f, 4f, 30f);
                    for (int i = -2; i <= 2; i++)
                        ModelKit.Part("Tail" + i, body, MeshLibrary.LowSphere, second, new Vector3(i * 0.22f, -1.05f, 0.2f), new Vector3(0.22f, 0.8f, 0.1f), new Vector3(0, 0, i * 14));
                    if (phoenix) Fire(body, 1.4f, 50f);
                    break;
                }
                case BossVisual.MountainKing:
                {
                    ModelKit.Part("Torso", body, MeshLibrary.Octahedron, main, Vector3.zero, new Vector3(3f, 3f, 2.2f));
                    ModelKit.Part("Head", body, MeshLibrary.Cube, main, new Vector3(0, 1.45f, -0.1f), new Vector3(1.1f, 0.9f, 0.9f), new Vector3(0, 0, 5));
                    Eyes(body, new Vector3(0, 1.5f, -0.58f), 0.18f, g, 1f);
                    Crown(body, new Vector3(0, 2f, 0), 0.75f);
                    var armL = ModelKit.Node("ArmL", body, new Vector3(-1.2f, 0.4f, 0)).transform;
                    var armR = ModelKit.Node("ArmR", body, new Vector3(1.2f, 0.4f, 0)).transform;
                    ModelKit.Part("Fist", armL, MeshLibrary.Octahedron, second, new Vector3(-0.4f, -0.9f, 0), new Vector3(0.9f, 1.2f, 0.9f));
                    ModelKit.Part("Fist", armR, MeshLibrary.Octahedron, second, new Vector3(0.4f, -0.9f, 0), new Vector3(0.9f, 1.2f, 0.9f));
                    view.AddFlapper(armL, Vector3.forward, 18f, 2f);
                    view.AddFlapper(armR, Vector3.forward, -18f, 2f);
                    break;
                }
                case BossVisual.ButterflyQueen:
                {
                    ModelKit.Part("Thorax", body, MeshLibrary.Sphere, main, new Vector3(0, 0.2f, 0), new Vector3(0.6f, 1.2f, 0.6f));
                    ModelKit.Part("Abdomen", body, MeshLibrary.Sphere, second, new Vector3(0, -0.8f, 0), new Vector3(0.45f, 1.2f, 0.45f));
                    ModelKit.Part("Head", body, MeshLibrary.Sphere, main, new Vector3(0, 0.95f, 0), Vector3.one * 0.55f);
                    Eyes(body, new Vector3(0, 1.0f, -0.25f), 0.12f, g, 0.8f);
                    Crown(body, new Vector3(0, 1.3f, 0), 0.45f);
                    for (int side = -1; side <= 1; side += 2)
                    {
                        var pivot = ModelKit.Node("Wings", body).transform;
                        ModelKit.Part("Upper", pivot, MeshLibrary.Disc, MaterialLibrary.Toon(a, 0.3f, 0.4f), new Vector3(1.2f * side, 0.6f, 0.1f), new Vector3(2.2f, 2f, 1f));
                        ModelKit.Part("UpperInner", pivot, MeshLibrary.Disc, MaterialLibrary.Toon(g, 0.5f, 0.4f), new Vector3(1.2f * side, 0.6f, 0.05f), new Vector3(1.1f, 1f, 1f));
                        ModelKit.Part("Lower", pivot, MeshLibrary.Disc, MaterialLibrary.Toon(b, 0.3f, 0.4f), new Vector3(0.9f * side, -0.7f, 0.1f), new Vector3(1.5f, 1.4f, 1f));
                        view.AddFlapper(pivot, Vector3.up, 35f * side, 6f);
                    }
                    for (int side = -1; side <= 1; side += 2)
                        ModelKit.Rope("Antenna", body, new Vector3(0.1f * side, 1.15f, 0), new Vector3(0.45f * side, 1.8f, 0), 0.05f, second);
                    Sparkles(body, g, 2f);
                    break;
                }
                case BossVisual.ThunderDragon:
                {
                    ModelKit.Part("Head", body, MeshLibrary.Sphere, main, new Vector3(0, 0.4f, -0.2f), new Vector3(1.3f, 1f, 1.2f));
                    ModelKit.Part("Snout", body, MeshLibrary.Sphere, main, new Vector3(0, 0.15f, -0.85f), new Vector3(0.8f, 0.55f, 0.9f));
                    Eyes(body, new Vector3(0, 0.6f, -0.75f), 0.16f, g, 1f);
                    for (int side = -1; side <= 1; side += 2)
                        ModelKit.Part("Horn", body, MeshLibrary.Cone, second, new Vector3(0.45f * side, 1.0f, 0), new Vector3(0.2f, 0.8f, 0.2f), new Vector3(0, 0, -25 * side));
                    for (int i = 0; i < 7; i++)
                    {
                        float side = i % 2 == 0 ? -1 : 1;
                        var seg = ModelKit.Part("Segment" + i, body, MeshLibrary.Sphere, i % 2 == 0 ? main : second, new Vector3(side * (0.9f + i * 0.35f), -0.2f - i * 0.12f, 0.4f), Vector3.one * (0.9f - i * 0.08f));
                        view.AddChain(seg.transform, i);
                    }
                    Wings(view, body, MaterialLibrary.Toon(b * 0.8f, 0.2f, 0.4f), 2.2f, 0.6f, 3f, 25f);
                    Sparks(body, g, 1.6f);
                    break;
                }
                case BossVisual.IceGuardian:
                {
                    var ice = MaterialLibrary.Toon(a, 0.25f, 0.95f, null, 1.3f);
                    ModelKit.Part("Torso", body, MeshLibrary.Octahedron, ice, Vector3.zero, new Vector3(2.4f, 3.2f, 2f));
                    ModelKit.Part("Head", body, MeshLibrary.Gem, ice, new Vector3(0, 1.6f, 0), new Vector3(1f, 1.2f, 1f));
                    Eyes(body, new Vector3(0, 1.65f, -0.45f), 0.15f, g, 1f);
                    var orbit = ModelKit.Node("Shards", body).transform;
                    for (int i = 0; i < 6; i++)
                    {
                        float ang = i / 6f * Mathf.PI * 2f;
                        ModelKit.Part("Shard" + i, orbit, MeshLibrary.Octahedron, MaterialLibrary.Toon(b, 0.4f, 0.95f), new Vector3(Mathf.Cos(ang) * 2f, 0, Mathf.Sin(ang) * 2f), new Vector3(0.35f, 0.8f, 0.35f));
                    }
                    orbit.localEulerAngles = new Vector3(15, 0, 0);
                    view.AddSpinner(orbit, new Vector3(0, 60, 0));
                    Sparkles(body, Color.white, 2f);
                    break;
                }
                case BossVisual.CosmicGuardian:
                {
                    ModelKit.Part("Eyeball", body, MeshLibrary.Sphere, MaterialLibrary.Toon(new Color(0.95f, 0.95f, 1f), 0.1f, 0.8f), Vector3.zero, Vector3.one * 2.4f);
                    ModelKit.Part("Iris", body, MeshLibrary.Disc, MaterialLibrary.Unlit(b), new Vector3(0, 0, -1.21f), Vector3.one * 1.2f);
                    ModelKit.Part("Pupil", body, MeshLibrary.Disc, MaterialLibrary.Unlit(new Color(0.05f, 0.02f, 0.1f)), new Vector3(0, 0, -1.22f), Vector3.one * 0.55f);
                    ModelKit.Part("Lid", body, MeshLibrary.Hemisphere, main, new Vector3(0, 0.15f, 0), new Vector3(2.6f, 1.6f, 2.6f));
                    for (int i = 0; i < 3; i++)
                    {
                        var ring = ModelKit.Part("Ring" + i, body, MeshLibrary.Torus, MaterialLibrary.Toon(g, 0.6f, 0.9f), Vector3.zero, Vector3.one * (3.3f + i * 0.5f), new Vector3(60 + i * 30, i * 40, 0));
                        view.AddSpinner(ring.transform, new Vector3(0, 40 + i * 25, 0));
                    }
                    Sparkles(body, g, 3f);
                    break;
                }
                case BossVisual.GoldenTitan:
                {
                    ModelKit.Part("Torso", body, MeshLibrary.Cube, main, Vector3.zero, new Vector3(2.2f, 2.2f, 1.4f));
                    ModelKit.Part("Belt", body, MeshLibrary.Cube, second, new Vector3(0, -0.9f, 0), new Vector3(2.3f, 0.35f, 1.5f));
                    ModelKit.Part("Head", body, MeshLibrary.Cube, main, new Vector3(0, 1.6f, 0), new Vector3(0.9f, 1f, 0.9f));
                    ModelKit.Part("Helmet", body, MeshLibrary.Cone, second, new Vector3(0, 2.3f, 0), new Vector3(0.9f, 0.6f, 0.9f));
                    Eyes(body, new Vector3(0, 1.65f, -0.48f), 0.15f, new Color(0.4f, 0.9f, 1f), 1f);
                    ModelKit.Part("ChestGem", body, MeshLibrary.Gem, glow, new Vector3(0, 0.3f, -0.75f), new Vector3(0.6f, 0.6f, 0.6f), new Vector3(-90, 0, 0));
                    for (int side = -1; side <= 1; side += 2)
                    {
                        ModelKit.Part("Shoulder", body, MeshLibrary.Sphere, second, new Vector3(1.35f * side, 0.9f, 0), Vector3.one * 0.9f);
                        var arm = ModelKit.Node("Arm", body, new Vector3(1.4f * side, 0.6f, 0)).transform;
                        ModelKit.Part("Forearm", arm, MeshLibrary.Cube, main, new Vector3(0.2f * side, -1f, 0), new Vector3(0.6f, 1.6f, 0.6f));
                        view.AddFlapper(arm, Vector3.forward, 12f * side, 1.5f);
                    }
                    break;
                }
                case BossVisual.CelestialGuardian:
                case BossVisual.HeavensGuardian:
                {
                    bool final = data.visual == BossVisual.HeavensGuardian;
                    var crystal = MaterialLibrary.Toon(a, 0.3f, 0.95f, null, 1.2f);
                    ModelKit.Part("Body", body, MeshLibrary.Gem, crystal, Vector3.zero, new Vector3(1.6f, 2.8f, 1.6f), new Vector3(180, 0, 0));
                    ModelKit.Part("Head", body, MeshLibrary.Sphere, MaterialLibrary.Toon(final ? new Color(1f, 0.97f, 0.9f) : b, 0.3f, 0.8f), new Vector3(0, 1.55f, 0), Vector3.one * 0.85f);
                    Eyes(body, new Vector3(0, 1.6f, -0.38f), 0.12f, g, 1f);
                    var halo = ModelKit.Part("Halo", body, MeshLibrary.Torus, MaterialLibrary.Toon(new Color(1f, 0.88f, 0.4f), 0.9f, 0.9f), new Vector3(0, 2.25f, 0), new Vector3(1.3f, 0.8f, 1.3f), new Vector3(-15, 0, 0));
                    view.AddSpinner(halo.transform, new Vector3(0, 50, 0));
                    int pairs = final ? 3 : 2;
                    for (int p = 0; p < pairs; p++)
                    {
                        var wingMat = MaterialLibrary.Toon(p % 2 == 0 ? Color.white : b, 0.25f, 0.6f);
                        Wings(view, body, wingMat, 2.4f - p * 0.35f, 0.5f - p * 0.45f, 2.5f + p * 0.5f, 20f, -15f + p * 25f);
                    }
                    if (final)
                    {
                        var rays = ModelKit.Node("Rays", body, new Vector3(0, 0.6f, 1.2f)).transform;
                        var rayMat = MaterialLibrary.Unlit(new Color(1f, 0.92f, 0.6f, 0.5f), ProceduralTextures.Streak, true);
                        for (int i = 0; i < 12; i++)
                            ModelKit.Part("Ray" + i, rays, MeshLibrary.Quad, rayMat, Quaternion.Euler(0, 0, i * 30) * new Vector3(2.6f, 0, 0), new Vector3(5f, 0.6f, 1f), new Vector3(0, 0, i * 30));
                        view.AddSpinner(rays, new Vector3(0, 0, 15));
                    }
                    Sparkles(body, g, 2.5f);
                    break;
                }
            }
            view.SetBaseScale(data.modelScale);
            return view;
        }

        static void Eyes(Transform body, Vector3 center, float size, Color color, float emission)
        {
            var mat = MaterialLibrary.Toon(color, emission, 0.9f);
            ModelKit.Part("EyeL", body, MeshLibrary.LowSphere, mat, center + new Vector3(-size * 1.4f, 0, 0), Vector3.one * size);
            ModelKit.Part("EyeR", body, MeshLibrary.LowSphere, mat, center + new Vector3(size * 1.4f, 0, 0), Vector3.one * size);
        }

        static void Crown(Transform body, Vector3 pos, float size)
        {
            var gold = MaterialLibrary.Toon(new Color(1f, 0.8f, 0.2f), 0.35f, 0.9f);
            ModelKit.Part("CrownBase", body, MeshLibrary.Cylinder, gold, pos, new Vector3(size, size * 0.25f, size));
            for (int i = 0; i < 5; i++)
            {
                float a = i / 5f * Mathf.PI * 2f;
                ModelKit.Part("Spike" + i, body, MeshLibrary.Cone, gold, pos + new Vector3(Mathf.Cos(a) * size * 0.4f, size * 0.3f, Mathf.Sin(a) * size * 0.4f), new Vector3(size * 0.22f, size * 0.45f, size * 0.22f));
            }
        }

        static void Wings(BossView view, Transform body, Material mat, float span, float height, float speed, float amplitude, float tilt = 10f)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                var pivot = ModelKit.Node("WingPivot", body, new Vector3(0.5f * side, height, 0.2f)).transform;
                ModelKit.Part("Wing", pivot, MeshLibrary.Wing, mat, Vector3.zero, new Vector3(span, span * 1.2f, 1f), new Vector3(0, side > 0 ? 0 : 180, tilt));
                view.AddFlapper(pivot, Vector3.forward, amplitude * side, speed);
            }
        }

        static void Fire(Transform body, float radius, float rate)
        {
            var s = ParticleSettings.Default;
            s.colorA = new Color(1f, 0.4f, 0.1f); s.colorB = new Color(1f, 0.85f, 0.3f); s.rate = rate; s.radius = radius;
            s.sizeMin = 0.3f; s.sizeMax = 0.7f; s.velocity = new Vector3(0, 2f, 0); s.lifeMin = 0.4f; s.lifeMax = 0.8f; s.shrink = true; s.worldSpace = false;
            ParticleFactory.Create("Fire", body, Vector3.zero, s);
        }

        static void Sparkles(Transform body, Color color, float radius)
        {
            var s = ParticleSettings.Default;
            s.colorA = color; s.colorB = Color.white; s.rate = 12f; s.radius = radius; s.texture = ProceduralTextures.Sparkle;
            s.sizeMin = 0.15f; s.sizeMax = 0.35f; s.worldSpace = false;
            ParticleFactory.Create("Sparkles", body, Vector3.zero, s);
        }

        static void Sparks(Transform body, Color color, float radius)
        {
            var s = ParticleSettings.Default;
            s.colorA = color; s.colorB = Color.white; s.rate = 18f; s.radius = radius; s.texture = ProceduralTextures.Sparkle;
            s.sizeMin = 0.12f; s.sizeMax = 0.3f; s.lifeMin = 0.15f; s.lifeMax = 0.35f; s.speedMin = 1f; s.speedMax = 2.5f; s.worldSpace = false;
            ParticleFactory.Create("Sparks", body, Vector3.zero, s);
        }
    }
}
