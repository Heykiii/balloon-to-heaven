using System.Collections.Generic;
using BalloonToHeaven.Balloons;
using BalloonToHeaven.Upgrades;
using UnityEngine;

namespace BalloonToHeaven.Visuals
{
    /// <summary>
    /// Builds a 3D cartoon hot air balloon from a BalloonData definition. Every upgrade stage adds
    /// visible parts: stage 2 ropes and basket, stage 3 reinforced bands and glossy fabric,
    /// stage 4 protective energy aura, stage 5 crown ornament, glow and a special trail.
    /// </summary>
    public static class BalloonModelBuilder
    {
        public static BalloonVisual Build(BalloonData data, int stage, Transform parent, TrailCosmetic trail = null)
        {
            var root = new GameObject("Balloon_" + data.id);
            root.transform.SetParent(parent, false);
            var visual = root.AddComponent<BalloonVisual>();

            var sway = ModelKit.Node("Sway", root.transform).transform;
            var envelopeRoot = ModelKit.Node("Envelope", sway, new Vector3(0, 0.35f, 0)).transform;

            // ---------------- Envelope
            Vector3 envScale;
            Mesh envMesh = MeshLibrary.Teardrop;
            switch (data.shape)
            {
                case EnvelopeShape.Round: envScale = new Vector3(1.7f, 1.75f, 1.7f); break;
                case EnvelopeShape.Tall: envScale = new Vector3(1.45f, 2.05f, 1.45f); break;
                case EnvelopeShape.Wide: envScale = new Vector3(1.95f, 1.6f, 1.95f); break;
                case EnvelopeShape.Teardrop: envScale = new Vector3(1.55f, 1.95f, 1.55f); break;
                default: envScale = new Vector3(1.65f, 1.85f, 1.65f); break;
            }
            float gloss = stage >= 3 ? 0.55f : 0.2f;
            float emission = stage >= 5 ? 0.18f : 0f;
            var envMat = MaterialLibrary.ToonInstance(Color.white, 0f, gloss, ProceduralTextures.BalloonPattern(data), stage >= 3 ? 0.8f : 0.6f);
            if (emission > 0f) envMat.SetColor(MaterialLibrary.EmissionId, data.glowColor * emission);
            var envelope = ModelKit.Part("Fabric", envelopeRoot, envMesh, envMat, Vector3.zero, envScale);
            visual.RegisterFlashMaterial(envMat);
            visual.envelope = envelope.transform;

            float envBottom = -0.62f * envScale.y;          // mouth position (local to envelopeRoot)
            float mouthRadius = 0.09f * envScale.x;
            var trimColor = stage >= 3 ? Color.Lerp(data.accentColor, new Color(1f, 0.84f, 0.3f), 0.6f) : data.accentColor;
            var trimMat = MaterialLibrary.Toon(trimColor, stage >= 4 ? 0.25f : 0f, 0.6f);
            ModelKit.Part("Skirt", envelopeRoot, MeshLibrary.Torus, trimMat, new Vector3(0, envBottom, 0), new Vector3(mouthRadius * 2.4f, mouthRadius * 3f, mouthRadius * 2.4f));

            // Stage 3+: reinforced fabric bands
            if (stage >= 3)
            {
                var bandMat = MaterialLibrary.Toon(stage >= 5 ? new Color(1f, 0.82f, 0.25f) : new Color(0.85f, 0.86f, 0.9f), stage >= 5 ? 0.3f : 0f, 0.8f);
                ModelKit.Part("Band_Equator", envelopeRoot, MeshLibrary.ThinTorus, bandMat, new Vector3(0, 0.02f * envScale.y, 0), new Vector3(envScale.x * 1.01f, envScale.y * 0.9f, envScale.z * 1.01f));
                ModelKit.Part("Band_Upper", envelopeRoot, MeshLibrary.ThinTorus, bandMat, new Vector3(0, 0.3f * envScale.y, 0), new Vector3(envScale.x * 0.8f, envScale.y * 0.7f, envScale.z * 0.8f));
                ModelKit.Part("Band_Lower", envelopeRoot, MeshLibrary.ThinTorus, bandMat, new Vector3(0, -0.28f * envScale.y, 0), new Vector3(envScale.x * 0.62f, envScale.y * 0.6f, envScale.z * 0.62f));
            }

            // ---------------- Basket
            var basketPivot = ModelKit.Node("BasketPivot", sway, new Vector3(0, 0.35f + envBottom, 0)).transform;
            visual.basket = basketPivot;
            float basketDrop = 0.75f;
            var basketColor = data.basketColor;
            var basketMat = MaterialLibrary.Toon(basketColor, 0f, 0.1f);
            var basketSize = stage >= 2 ? new Vector3(0.62f, 0.42f, 0.62f) : new Vector3(0.52f, 0.38f, 0.52f);
            var basketPos = new Vector3(0, -basketDrop, 0);
            ModelKit.Part("Basket", basketPivot, MeshLibrary.Cube, basketMat, basketPos, basketSize);
            var rimMat = MaterialLibrary.Toon(Color.Lerp(basketColor, Color.black, 0.25f), 0f, 0.2f);
            ModelKit.Part("Rim", basketPivot, MeshLibrary.Cube, rimMat, basketPos + new Vector3(0, basketSize.y * 0.5f, 0), new Vector3(basketSize.x * 1.08f, 0.07f, basketSize.z * 1.08f));
            if (stage >= 2)
            {
                // Reinforced corners and a woven band
                var metal = MaterialLibrary.Toon(stage >= 4 ? new Color(1f, 0.82f, 0.3f) : new Color(0.7f, 0.72f, 0.78f), 0f, 0.7f);
                for (int i = 0; i < 4; i++)
                {
                    float sx = (i % 2 == 0 ? -1 : 1) * basketSize.x * 0.5f, sz = (i < 2 ? -1 : 1) * basketSize.z * 0.5f;
                    ModelKit.Part("Corner" + i, basketPivot, MeshLibrary.Cube, metal, basketPos + new Vector3(sx, 0, sz), new Vector3(0.06f, basketSize.y * 1.05f, 0.06f));
                }
                ModelKit.Part("Band", basketPivot, MeshLibrary.Cube, metal, basketPos + new Vector3(0, -basketSize.y * 0.15f, 0), new Vector3(basketSize.x * 1.04f, 0.05f, basketSize.z * 1.04f));
            }
            // Burner and flame
            var burnerMat = MaterialLibrary.Toon(new Color(0.3f, 0.3f, 0.35f), 0f, 0.6f);
            ModelKit.Part("Burner", basketPivot, MeshLibrary.Cylinder, burnerMat, new Vector3(0, -basketDrop + 0.38f, 0), new Vector3(0.16f, 0.12f, 0.16f));
            visual.flame = ModelKit.Glow("Flame", basketPivot, new Vector3(0, -basketDrop + 0.52f, -0.3f), 0.42f, new Color(1f, 0.65f, 0.2f, 0.9f)).transform;

            // ---------------- Ropes (4 at stage 1, 8 from stage 2)
            int ropeCount = stage >= 2 ? 8 : 4;
            var ropeMat = MaterialLibrary.Toon(stage >= 4 ? new Color(1f, 0.85f, 0.4f) : new Color(0.35f, 0.25f, 0.18f), stage >= 4 ? 0.2f : 0f, 0.1f);
            for (int i = 0; i < ropeCount; i++)
            {
                float a = (i / (float)ropeCount) * Mathf.PI * 2f + Mathf.PI / 4f;
                var top = new Vector3(Mathf.Cos(a) * mouthRadius * 1.6f, 0f, Mathf.Sin(a) * mouthRadius * 1.6f);
                var bottom = new Vector3(Mathf.Cos(a) * basketSize.x * 0.5f, -basketDrop + basketSize.y * 0.5f, Mathf.Sin(a) * basketSize.z * 0.5f);
                ModelKit.Rope("Rope" + i, basketPivot, top, bottom, 0.025f, ropeMat);
            }

            AddOrnaments(data, stage, envelopeRoot, sway, envScale, visual);

            // ---------------- Stage 4: protective energy aura
            if (stage >= 4)
            {
                var aura = ModelKit.Part("Aura", envelopeRoot, MeshLibrary.Sphere, MaterialLibrary.Shield(Color.Lerp(data.glowColor, Color.cyan, 0.3f), 0.35f, 3f), new Vector3(0, -0.1f, 0), envScale * 1.18f);
                visual.aura = aura.transform;
                var s = ParticleSettings.Default;
                s.colorA = data.glowColor; s.colorB = Color.white; s.rate = 6f; s.radius = envScale.x * 0.7f;
                s.sizeMin = 0.06f; s.sizeMax = 0.14f; s.worldSpace = false; s.texture = ProceduralTextures.Sparkle; s.lifeMin = 0.8f; s.lifeMax = 1.4f;
                ParticleFactory.Create("AuraSparkles", envelopeRoot, Vector3.zero, s);
            }

            // ---------------- Stage 5: crown ornament + signature trail
            if (stage >= 5)
            {
                var gold = MaterialLibrary.Toon(new Color(1f, 0.8f, 0.2f), 0.35f, 0.9f);
                float topY = envScale.y * 0.5f;
                ModelKit.Part("CrownGem", envelopeRoot, MeshLibrary.Gem, MaterialLibrary.Toon(data.glowColor, 0.5f, 0.9f), new Vector3(0, topY + 0.18f, 0), Vector3.one * 0.3f);
                ModelKit.Part("CrownRing", envelopeRoot, MeshLibrary.Torus, gold, new Vector3(0, topY - 0.02f, 0), new Vector3(0.55f, 0.4f, 0.55f));
                visual.SetSignatureTrail(CreateTrail(root.transform, data.glowColor, Color.white, true));
            }
            if (trail != null)
                visual.SetCosmeticTrail(CreateTrail(root.transform, trail.colorA, trail.colorB, trail.additive));

            // ---------------- Shield bubble and ghost (hidden until needed)
            var shield = ModelKit.Part("Shield", root.transform, MeshLibrary.Sphere, MaterialLibrary.Shield(new Color(0.3f, 0.95f, 1f), 0.9f, 2f), new Vector3(0, 0.05f, 0), new Vector3(2.5f, 2.9f, 2.5f));
            shield.gameObject.SetActive(false);
            visual.shield = shield.gameObject;
            var ghost = ModelKit.Part("Ghost", root.transform, envMesh, MaterialLibrary.Shield(new Color(0.65f, 0.55f, 1f), 1f, 1.3f), new Vector3(0, 0.35f, 0), envScale);
            ghost.gameObject.SetActive(false);
            visual.ghost = ghost.gameObject;
            visual.swayRoot = sway;
            visual.Initialize(data);
            return visual;
        }

        static ParticleSystem CreateTrail(Transform parent, Color a, Color b, bool additive)
        {
            var s = ParticleSettings.Default;
            s.colorA = a; s.colorB = b; s.rate = 22f; s.radius = 0.15f; s.sizeMin = 0.12f; s.sizeMax = 0.26f;
            s.speedMin = 0f; s.speedMax = 0.2f; s.lifeMin = 0.8f; s.lifeMax = 1.2f; s.velocity = new Vector3(0, -3.5f, 0);
            s.additive = additive; s.texture = ProceduralTextures.Sparkle; s.shrink = true; s.maxParticles = 60;
            return ParticleFactory.Create("Trail", parent, new Vector3(0, -1.1f, 0.2f), s);
        }

        static void AddOrnaments(BalloonData data, int stage, Transform envelopeRoot, Transform sway, Vector3 envScale, BalloonVisual visual)
        {
            var o = data.ornaments;
            float half = envScale.x * 0.5f;
            if ((o & BalloonOrnament.Wings) != 0)
            {
                var wingMat = MaterialLibrary.Toon(Color.Lerp(Color.white, data.accentColor, 0.15f), 0.15f, 0.5f);
                var left = ModelKit.Part("WingL", envelopeRoot, MeshLibrary.Wing, wingMat, new Vector3(-half * 0.85f, 0.1f, 0.1f), new Vector3(1.1f, 1.3f, 1f), new Vector3(0, 180, 10));
                var right = ModelKit.Part("WingR", envelopeRoot, MeshLibrary.Wing, wingMat, new Vector3(half * 0.85f, 0.1f, 0.1f), new Vector3(1.1f, 1.3f, 1f), new Vector3(0, 0, 10));
                visual.AddFlapper(left.transform, 1);
                visual.AddFlapper(right.transform, -1);
            }
            if ((o & BalloonOrnament.Ring) != 0)
            {
                var ring = ModelKit.Part("PlanetRing", envelopeRoot, MeshLibrary.Torus, MaterialLibrary.Toon(data.accentColor, 0.1f, 0.6f), new Vector3(0, 0.05f, 0), new Vector3(envScale.x * 1.55f, 0.6f, envScale.x * 1.55f), new Vector3(12, 0, -14));
                visual.AddSpinner(ring.transform, new Vector3(0, 25f, 0));
            }
            if ((o & BalloonOrnament.Halo) != 0)
            {
                var halo = ModelKit.Part("Halo", envelopeRoot, MeshLibrary.Torus, MaterialLibrary.Toon(new Color(1f, 0.9f, 0.45f), 0.8f, 0.9f), new Vector3(0, envScale.y * 0.5f + 0.35f, 0), new Vector3(0.9f, 0.5f, 0.9f), new Vector3(-12, 0, 0));
                visual.AddBobber(halo.transform, 0.06f, 2f);
            }
            if ((o & BalloonOrnament.Crown) != 0 && stage < 5)
            {
                var gold = MaterialLibrary.Toon(new Color(1f, 0.8f, 0.2f), 0.25f, 0.9f);
                float topY = envScale.y * 0.5f - 0.02f;
                ModelKit.Part("CrownBase", envelopeRoot, MeshLibrary.Cylinder, gold, new Vector3(0, topY + 0.05f, 0), new Vector3(0.5f, 0.12f, 0.5f));
                for (int i = 0; i < 5; i++)
                {
                    float a = i / 5f * Mathf.PI * 2f;
                    ModelKit.Part("CrownSpike" + i, envelopeRoot, MeshLibrary.Cone, gold, new Vector3(Mathf.Cos(a) * 0.2f, topY + 0.2f, Mathf.Sin(a) * 0.2f), new Vector3(0.12f, 0.22f, 0.12f));
                }
            }
            if ((o & BalloonOrnament.Crystals) != 0)
            {
                var mat = MaterialLibrary.Toon(new Color(0.75f, 0.95f, 1f), 0.3f, 0.9f);
                for (int i = 0; i < 6; i++)
                {
                    float a = i / 6f * Mathf.PI * 2f;
                    ModelKit.Part("Crystal" + i, envelopeRoot, MeshLibrary.Octahedron, mat, new Vector3(Mathf.Cos(a) * half * 0.55f, envScale.y * 0.42f, Mathf.Sin(a) * half * 0.55f), new Vector3(0.22f, 0.4f, 0.22f), new Vector3(Mathf.Cos(a) * 25f, 0, Mathf.Sin(a) * 25f));
                }
            }
            if ((o & BalloonOrnament.Leaves) != 0)
            {
                var mat = MaterialLibrary.Toon(new Color(0.35f, 0.75f, 0.3f), 0f, 0.2f);
                for (int i = 0; i < 6; i++)
                {
                    float a = i / 6f * Mathf.PI * 2f;
                    ModelKit.Part("Leaf" + i, envelopeRoot, MeshLibrary.LowSphere, mat, new Vector3(Mathf.Cos(a) * half * 0.75f, -envScale.y * 0.32f, Mathf.Sin(a) * half * 0.75f), new Vector3(0.28f, 0.08f, 0.16f), new Vector3(0, -a * Mathf.Rad2Deg, 25));
                }
            }
            if ((o & BalloonOrnament.SunRays) != 0)
            {
                var mat = MaterialLibrary.Toon(new Color(1f, 0.85f, 0.3f), 0.45f, 0.5f);
                var rays = ModelKit.Node("SunRays", envelopeRoot, new Vector3(0, 0.05f, 0.4f)).transform;
                for (int i = 0; i < 10; i++)
                {
                    float a = i / 10f * 360f;
                    var ray = ModelKit.Part("Ray" + i, rays, MeshLibrary.Cone, mat, Quaternion.Euler(0, 0, a) * new Vector3(0, half * 1.18f, 0), new Vector3(0.18f, 0.32f, 0.06f), new Vector3(0, 0, a));
                    ray.transform.localPosition = Quaternion.Euler(0, 0, a) * new Vector3(0, half * 1.15f, 0);
                }
                visual.AddSpinner(rays, new Vector3(0, 0, 18f));
            }
            if ((o & BalloonOrnament.Flames) != 0)
            {
                var s = ParticleSettings.Default;
                s.colorA = new Color(1f, 0.5f, 0.1f); s.colorB = new Color(1f, 0.85f, 0.3f); s.rate = 18f; s.radius = half * 0.6f;
                s.velocity = new Vector3(0, 1.2f, 0); s.sizeMin = 0.15f; s.sizeMax = 0.3f; s.lifeMin = 0.4f; s.lifeMax = 0.7f; s.shrink = true; s.worldSpace = false;
                ParticleFactory.Create("Flames", envelopeRoot, new Vector3(0, envScale.y * 0.3f, 0), s);
            }
            if ((o & BalloonOrnament.Stars) != 0)
            {
                var s = ParticleSettings.Default;
                s.colorA = Color.white; s.colorB = data.accentColor; s.rate = 7f; s.radius = half * 1.1f; s.texture = ProceduralTextures.Sparkle;
                s.sizeMin = 0.1f; s.sizeMax = 0.22f; s.worldSpace = false; s.lifeMin = 0.6f; s.lifeMax = 1.2f;
                ParticleFactory.Create("Stars", envelopeRoot, Vector3.zero, s);
            }
            if ((o & BalloonOrnament.Shadow) != 0)
            {
                var s = ParticleSettings.Default;
                s.colorA = new Color(0.2f, 0.15f, 0.35f, 0.6f); s.colorB = new Color(0.45f, 0.35f, 0.8f, 0.5f); s.rate = 10f; s.radius = half;
                s.additive = false; s.sizeMin = 0.3f; s.sizeMax = 0.6f; s.velocity = new Vector3(0, -0.8f, 0); s.worldSpace = false;
                ParticleFactory.Create("ShadowMist", envelopeRoot, new Vector3(0, -0.3f, 0), s);
            }
            if ((o & BalloonOrnament.Orbit) != 0)
            {
                var orbit = ModelKit.Node("Orbit", envelopeRoot).transform;
                var mat = MaterialLibrary.Toon(new Color(0.5f, 1f, 1f), 0.6f, 0.8f);
                for (int i = 0; i < 3; i++)
                {
                    float a = i / 3f * Mathf.PI * 2f;
                    ModelKit.Part("Orb" + i, orbit, MeshLibrary.LowSphere, mat, new Vector3(Mathf.Cos(a) * half * 1.3f, 0, Mathf.Sin(a) * half * 1.3f), Vector3.one * 0.2f);
                }
                orbit.localEulerAngles = new Vector3(20, 0, 0);
                visual.AddSpinner(orbit, new Vector3(0, 90f, 0));
            }
        }
    }
}
