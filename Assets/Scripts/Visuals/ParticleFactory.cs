using BalloonToHeaven.Core;
using UnityEngine;

namespace BalloonToHeaven.Visuals
{
    /// <summary>Settings for a code-built particle system.</summary>
    public struct ParticleSettings
    {
        public Color colorA, colorB;
        public float sizeMin, sizeMax;
        public float lifeMin, lifeMax;
        public float speedMin, speedMax;
        public float rate;
        public int maxParticles;
        public ParticleSystemShapeType shape;
        public float radius;
        public Vector3 boxSize;
        public float coneAngle;
        public Vector3 velocity;
        public float gravity;
        public bool worldSpace;
        public bool additive;
        public bool loop;
        public bool fadeIn;
        public bool shrink;
        public Texture texture;
        public float rotationSpeed;

        public static ParticleSettings Default => new ParticleSettings
        {
            colorA = Color.white, colorB = Color.white, sizeMin = 0.1f, sizeMax = 0.2f, lifeMin = 1f, lifeMax = 1.5f,
            speedMin = 0.2f, speedMax = 0.6f, rate = 10f, maxParticles = 100, shape = ParticleSystemShapeType.Sphere,
            radius = 0.5f, boxSize = Vector3.one, coneAngle = 15f, worldSpace = true, additive = true, loop = true, fadeIn = true
        };
    }

    /// <summary>Builds particle systems from code and provides pooled one-shot bursts.</summary>
    public static class ParticleFactory
    {
        static ParticleSystem burstAdditive, burstAlpha;

        public static ParticleSystem Create(string name, Transform parent, Vector3 localPosition, ParticleSettings s)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            float density = QualityManager.EffectsDensity;
            var main = ps.main;
            main.duration = 4f;
            main.loop = s.loop;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(s.lifeMin, s.lifeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(s.speedMin, s.speedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(s.sizeMin, s.sizeMax);
            main.startColor = new ParticleSystem.MinMaxGradient(s.colorA, s.colorB);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.maxParticles = Mathf.Max(4, Mathf.RoundToInt(s.maxParticles * density));
            main.simulationSpace = s.worldSpace ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
            main.gravityModifier = s.gravity;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            var emission = ps.emission;
            emission.rateOverTime = s.rate * density;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = s.shape;
            shape.radius = Mathf.Max(0.01f, s.radius);
            shape.scale = s.shape == ParticleSystemShapeType.Box ? s.boxSize : Vector3.one;
            shape.angle = s.coneAngle;

            if (s.velocity != Vector3.zero)
            {
                var vel = ps.velocityOverLifetime;
                vel.enabled = true;
                vel.space = ParticleSystemSimulationSpace.World;
                vel.x = new ParticleSystem.MinMaxCurve(s.velocity.x);
                vel.y = new ParticleSystem.MinMaxCurve(s.velocity.y);
                vel.z = new ParticleSystem.MinMaxCurve(s.velocity.z);
            }

            if (s.rotationSpeed != 0f)
            {
                var rot = ps.rotationOverLifetime;
                rot.enabled = true;
                rot.z = new ParticleSystem.MinMaxCurve(-s.rotationSpeed, s.rotationSpeed);
            }

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(s.fadeIn ? 0f : 1f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0.8f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(gradient);

            if (s.shrink)
            {
                var size = ps.sizeOverLifetime;
                size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.1f));
            }

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = MaterialLibrary.Particle(s.additive, s.texture);
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            if (s.loop) ps.Play();
            return ps;
        }

        static ParticleSystem BurstSystem(bool additive)
        {
            var existing = additive ? burstAdditive : burstAlpha;
            if (existing != null) return existing;
            var s = ParticleSettings.Default;
            s.loop = true; // keeps simulating; emission rate is zero, particles only come from Emit()
            s.rate = 0f;
            s.maxParticles = 600;
            s.additive = additive;
            s.fadeIn = false;
            s.shrink = true;
            s.texture = additive ? ProceduralTextures.Sparkle : ProceduralTextures.SoftCircle;
            var ps = Create(additive ? "[Burst Additive]" : "[Burst Alpha]", null, Vector3.zero, s);
            Object.DontDestroyOnLoad(ps.gameObject);
            var main = ps.main;
            main.maxParticles = 600;
            ps.Play();
            if (additive) burstAdditive = ps; else burstAlpha = ps;
            return ps;
        }

        /// <summary>Emits a radial one-shot burst. Uses a shared pooled system; no allocations.</summary>
        public static void Burst(Vector3 position, Color color, int count, float speed = 3f, float size = 0.25f, float life = 0.6f, bool additive = true)
        {
            var ps = BurstSystem(additive);
            if (ps == null) return;
            count = Mathf.Max(1, Mathf.RoundToInt(count * QualityManager.EffectsDensity));
            var p = new ParticleSystem.EmitParams { applyShapeToPosition = false };
            for (int i = 0; i < count; i++)
            {
                float a = Random.value * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                p.position = position + dir * 0.1f;
                p.velocity = dir * speed * Random.Range(0.5f, 1.1f);
                p.startColor = color;
                p.startSize = size * Random.Range(0.6f, 1.3f);
                p.startLifetime = life * Random.Range(0.7f, 1.2f);
                ps.Emit(p, 1);
            }
        }

        /// <summary>Clears pooled bursts (e.g. when a scene ends).</summary>
        public static void ClearBursts()
        {
            if (burstAdditive != null) burstAdditive.Clear();
            if (burstAlpha != null) burstAlpha.Clear();
        }
    }
}
