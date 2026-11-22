using System.Collections.Generic;
using BalloonToHeaven.Obstacles;
using UnityEngine;

namespace BalloonToHeaven.Visuals
{
    public enum HazardVisualState { Idle, Warning, Active }

    /// <summary>Animates an obstacle model (wings, spin, pulse, telegraphs). Presentation only.</summary>
    public sealed class ObstacleView : MonoBehaviour
    {
        public Transform body;
        public GameObject warning;
        public GameObject active;
        public Transform sizedPart;
        public ParticleSystem trail;
        readonly List<(Transform t, int side, Quaternion baseRot, float speed)> wings = new List<(Transform, int, Quaternion, float)>();
        readonly List<(Transform t, Vector3 speed)> spinners = new List<(Transform, Vector3)>();
        Renderer warningRenderer;
        float phase;
        float facing = 1f;

        public void AddWing(Transform t, int side, float speed = 9f) => wings.Add((t, side, t.localRotation, speed));
        public void AddSpinner(Transform t, Vector3 speed) => spinners.Add((t, speed));

        public void Prepare()
        {
            phase = Random.value * 10f;
            if (warning != null) warningRenderer = warning.GetComponentInChildren<Renderer>();
        }

        public void SetFacing(float dir)
        {
            facing = dir >= 0 ? 1f : -1f;
            if (body != null)
            {
                var s = body.localScale;
                s.x = Mathf.Abs(s.x) * facing;
                body.localScale = s;
            }
        }

        public void SetSize(Vector2 size)
        {
            if (sizedPart != null) sizedPart.localScale = new Vector3(size.x, size.y, Mathf.Min(1.4f, size.y));
        }

        public void SetState(HazardVisualState state)
        {
            if (warning != null) warning.SetActive(state == HazardVisualState.Warning);
            if (active != null) active.SetActive(state == HazardVisualState.Active);
        }

        public void ResetTrail()
        {
            if (trail != null) trail.Clear();
        }

        void Update()
        {
            float t = Time.time + phase;
            for (int i = 0; i < wings.Count; i++)
            {
                var w = wings[i];
                if (w.t != null) w.t.localRotation = w.baseRot * Quaternion.Euler(Mathf.Sin(t * w.speed) * 35f * w.side, 0, 0);
            }
            float dt = Time.deltaTime;
            for (int i = 0; i < spinners.Count; i++)
            {
                var s = spinners[i];
                if (s.t != null) s.t.Rotate(s.speed * dt, Space.Self);
            }
            if (warningRenderer != null && warning.activeSelf)
            {
                // Telegraph flicker so the player clearly sees where the hazard will strike.
                warningRenderer.enabled = Mathf.Repeat(t * 7f, 1f) > 0.3f;
            }
        }

        public void SetWindDirection(float direction)
        {
            if (trail == null) return;
            var vel = trail.velocityOverLifetime;
            vel.x = new ParticleSystem.MinMaxCurve(4f * Mathf.Sign(direction));
        }
    }

    /// <summary>Builds recognisable low-poly models for every obstacle visual type.</summary>
    public static class ObstacleModelBuilder
    {
        public static ObstacleView Build(ObstacleData data, Transform parent)
        {
            var root = new GameObject("Obstacle_" + data.id);
            root.transform.SetParent(parent, false);
            var view = root.AddComponent<ObstacleView>();
            var body = ModelKit.Node("Body", root.transform).transform;
            body.localScale = Vector3.one * data.scale;
            view.body = body;
            Color a = data.primaryColor, b = data.secondaryColor;

            switch (data.visual)
            {
                case ObstacleVisual.SmallBird: Bird(view, body, a, b, 0.9f, 0f); break;
                case ObstacleVisual.Eagle: Bird(view, body, a, b, 1.35f, 0f); break;
                case ObstacleVisual.ElectricBird: Bird(view, body, a, b, 1f, 0.35f); AddSparks(body, b, 0.5f); break;
                case ObstacleVisual.FrozenBird: Bird(view, body, a, b, 0.9f, 0.1f); break;
                case ObstacleVisual.BurningBird: Bird(view, body, a, b, 1f, 0.45f); AddFire(body, 0.4f, false); break;
                case ObstacleVisual.CelestialBird: Bird(view, body, a, b, 1f, 0.5f); ModelKit.Glow("Glow", body, new Vector3(0, 0, 0.3f), 1.6f, new Color(b.r, b.g, b.b, 0.5f)); break;
                case ObstacleVisual.Butterfly: Butterfly(view, body, a, b); break;
                case ObstacleVisual.Rock: Rock(view, body, a, b, data.radius * 2f); break;
                case ObstacleVisual.Asteroid: Rock(view, body, a, b, data.radius * 2f); break;
                case ObstacleVisual.Snowball:
                    ModelKit.Part("Snow", body, MeshLibrary.Sphere, MaterialLibrary.Toon(a, 0.05f, 0.3f), Vector3.zero, Vector3.one * data.radius * 2.1f);
                    view.AddSpinner(body, new Vector3(0, 0, data.spin));
                    break;
                case ObstacleVisual.IceCrystal:
                case ObstacleVisual.CrystalShard:
                {
                    var mat = MaterialLibrary.Toon(a, 0.25f, 0.9f, null, 1.2f);
                    ModelKit.Part("Crystal", body, MeshLibrary.Octahedron, mat, Vector3.zero, new Vector3(data.radius * 1.6f, data.radius * 2.6f, data.radius * 1.6f));
                    ModelKit.Part("CrystalSmall", body, MeshLibrary.Octahedron, MaterialLibrary.Toon(b, 0.2f, 0.9f), new Vector3(data.radius * 0.6f, -data.radius * 0.4f, 0), new Vector3(data.radius * 0.7f, data.radius * 1.2f, data.radius * 0.7f), new Vector3(0, 0, -30));
                    view.AddSpinner(body, new Vector3(0, data.spin, 0));
                    break;
                }
                case ObstacleVisual.Fireball:
                    ModelKit.Part("Core", body, MeshLibrary.Sphere, MaterialLibrary.Toon(b, 0.9f, 0.2f), Vector3.zero, Vector3.one * data.radius * 1.6f);
                    ModelKit.Glow("Glow", body, new Vector3(0, 0, -0.3f), data.radius * 4f, new Color(a.r, a.g, a.b, 0.8f));
                    view.trail = AddFire(root.transform, data.radius, true);
                    break;
                case ObstacleVisual.Meteorite:
                    Rock(view, body, a * 0.6f + Color.black * 0.4f, a, data.radius * 1.8f);
                    ModelKit.Glow("Glow", body, new Vector3(0, 0, -0.3f), data.radius * 3.4f, new Color(b.r, b.g, b.b, 0.6f));
                    view.trail = AddFire(root.transform, data.radius, true);
                    break;
                case ObstacleVisual.AlienDrone: Drone(view, body, a, b, data.radius); break;
                case ObstacleVisual.EnergyOrb:
                    ModelKit.Part("Orb", body, MeshLibrary.Sphere, MaterialLibrary.Toon(a, 0.7f, 0.6f), Vector3.zero, Vector3.one * data.radius * 1.7f);
                    ModelKit.Glow("Glow", body, new Vector3(0, 0, -0.4f), data.radius * 4.2f, new Color(a.r, a.g, a.b, 0.65f));
                    ModelKit.Glow("Core", body, new Vector3(0, 0, -0.45f), data.radius * 1.6f, new Color(b.r, b.g, b.b, 0.9f));
                    break;
                case ObstacleVisual.GuardianOrb:
                {
                    ModelKit.Part("Orb", body, MeshLibrary.Sphere, MaterialLibrary.Toon(a, 0.35f, 0.9f), Vector3.zero, Vector3.one * data.radius * 1.5f);
                    var ring1 = ModelKit.Part("Ring1", body, MeshLibrary.Torus, MaterialLibrary.Toon(b, 0.5f, 0.9f), Vector3.zero, Vector3.one * data.radius * 2.4f, new Vector3(70, 0, 0));
                    var ring2 = ModelKit.Part("Ring2", body, MeshLibrary.Torus, MaterialLibrary.Toon(a, 0.4f, 0.9f), Vector3.zero, Vector3.one * data.radius * 2.1f, new Vector3(0, 0, 70));
                    view.AddSpinner(ring1.transform, new Vector3(0, 0, 120));
                    view.AddSpinner(ring2.transform, new Vector3(120, 0, 0));
                    ModelKit.Part("Eye", body, MeshLibrary.LowSphere, MaterialLibrary.Toon(new Color(0.2f, 0.1f, 0.3f)), new Vector3(0, 0, -data.radius * 0.65f), Vector3.one * data.radius * 0.5f);
                    break;
                }
                case ObstacleVisual.Feather:
                    ModelKit.Part("Vane", body, MeshLibrary.LowSphere, MaterialLibrary.Toon(a, 0.1f, 0.2f), Vector3.zero, new Vector3(data.radius * 0.9f, data.radius * 2.6f, data.radius * 0.25f));
                    ModelKit.Part("Shaft", body, MeshLibrary.Cylinder, MaterialLibrary.Toon(b), Vector3.zero, new Vector3(0.05f, data.radius * 2.8f, 0.05f));
                    view.AddSpinner(body, new Vector3(0, 0, 200));
                    break;
                case ObstacleVisual.DarkCloud: Cloud(body, a, b, data.radius, true); AddSparks(body, new Color(1f, 0.95f, 0.3f), data.radius); break;
                case ObstacleVisual.LightningCloud: Lightning(view, root.transform, data); break;
                case ObstacleVisual.LavaColumn: Lava(view, root.transform, data); break;
                case ObstacleVisual.MagicBarrier: Barrier(view, root.transform, data); break;
                case ObstacleVisual.LightBeam: Beam(view, root.transform, data); break;
                case ObstacleVisual.WindCurrent: Wind(view, root.transform, data); break;
                case ObstacleVisual.CliffWall:
                case ObstacleVisual.GoldenGate:
                    Wall(view, root.transform, data);
                    break;
                case ObstacleVisual.MovingPlatform:
                {
                    var sized = ModelKit.Node("Sized", body).transform;
                    ModelKit.Part("Earth", sized, MeshLibrary.Cube, MaterialLibrary.Toon(new Color(0.55f, 0.4f, 0.3f)), new Vector3(0, -0.1f, 0), new Vector3(1f, 0.8f, 1f));
                    ModelKit.Part("Grass", sized, MeshLibrary.Cube, MaterialLibrary.Toon(a, 0.05f, 0.2f), new Vector3(0, 0.4f, 0), new Vector3(1.05f, 0.25f, 1.05f));
                    sized.localScale = new Vector3(data.boxSize.x, data.boxSize.y, 1f);
                    view.sizedPart = sized;
                    break;
                }
                case ObstacleVisual.TempleBlock:
                {
                    var sized = ModelKit.Node("Sized", body).transform;
                    var gold = MaterialLibrary.Toon(a, 0.1f, 0.7f);
                    ModelKit.Part("Block", sized, MeshLibrary.Cube, gold, Vector3.zero, Vector3.one);
                    var trim = MaterialLibrary.Toon(b, 0.15f, 0.8f);
                    ModelKit.Part("TopTrim", sized, MeshLibrary.Cube, trim, new Vector3(0, 0.5f, 0), new Vector3(1.1f, 0.12f, 1.1f));
                    ModelKit.Part("BottomTrim", sized, MeshLibrary.Cube, trim, new Vector3(0, -0.5f, 0), new Vector3(1.1f, 0.12f, 1.1f));
                    for (int i = -1; i <= 1; i += 2) ModelKit.Part("Pillar", sized, MeshLibrary.Cylinder, trim, new Vector3(i * 0.32f, 0, -0.5f), new Vector3(0.14f, 0.9f, 0.14f));
                    sized.localScale = new Vector3(data.boxSize.x, data.boxSize.y, 1f);
                    view.sizedPart = sized;
                    break;
                }
                case ObstacleVisual.CrystalBarrier:
                {
                    var sized = ModelKit.Node("Sized", body).transform;
                    var mat = MaterialLibrary.Toon(a, 0.35f, 0.9f, null, 1.2f);
                    for (int i = 0; i < 5; i++)
                        ModelKit.Part("Shard" + i, sized, MeshLibrary.Octahedron, mat, new Vector3(-0.4f + i * 0.2f, 0, 0), new Vector3(0.25f, 1.6f, 0.5f), new Vector3(0, 0, 90 + (i % 2 == 0 ? 10 : -10)));
                    sized.localScale = new Vector3(data.boxSize.x, data.boxSize.y, 1f);
                    view.sizedPart = sized;
                    break;
                }
                default:
                    ModelKit.Part("Blob", body, MeshLibrary.Sphere, MaterialLibrary.Toon(a), Vector3.zero, Vector3.one * data.radius * 2f);
                    break;
            }
            view.Prepare();
            return view;
        }

        static void Bird(ObstacleView view, Transform body, Color a, Color b, float size, float emission)
        {
            var bodyMat = MaterialLibrary.Toon(a, emission, 0.3f);
            var bellyMat = MaterialLibrary.Toon(b, emission * 0.5f, 0.2f);
            ModelKit.Part("Torso", body, MeshLibrary.LowSphere, bodyMat, Vector3.zero, new Vector3(0.9f, 0.5f, 0.5f) * size);
            ModelKit.Part("Belly", body, MeshLibrary.LowSphere, bellyMat, new Vector3(0.05f, -0.08f, -0.08f) * size, new Vector3(0.6f, 0.32f, 0.4f) * size);
            ModelKit.Part("Head", body, MeshLibrary.LowSphere, bellyMat, new Vector3(0.45f, 0.12f, 0) * size, Vector3.one * 0.38f * size);
            ModelKit.Part("Beak", body, MeshLibrary.Cone, MaterialLibrary.Toon(new Color(1f, 0.7f, 0.2f)), new Vector3(0.68f, 0.1f, 0) * size, new Vector3(0.12f, 0.22f, 0.12f) * size, new Vector3(0, 0, -90));
            ModelKit.Part("Eye", body, MeshLibrary.LowSphere, MaterialLibrary.Toon(new Color(0.08f, 0.08f, 0.1f)), new Vector3(0.55f, 0.18f, -0.15f) * size, Vector3.one * 0.08f * size);
            ModelKit.Part("Tail", body, MeshLibrary.Cone, bodyMat, new Vector3(-0.5f, 0.05f, 0) * size, new Vector3(0.25f, 0.35f, 0.1f) * size, new Vector3(0, 0, 90));
            var wingL = ModelKit.Node("WingPivotL", body, new Vector3(0, 0.12f, -0.1f) * size).transform;
            var wingR = ModelKit.Node("WingPivotR", body, new Vector3(0, 0.12f, 0.1f) * size).transform;
            ModelKit.Part("Wing", wingL, MeshLibrary.Wing, bodyMat, Vector3.zero, new Vector3(0.6f, 0.8f, 1f) * size, new Vector3(0, 90, 0));
            ModelKit.Part("Wing", wingR, MeshLibrary.Wing, bodyMat, Vector3.zero, new Vector3(0.6f, 0.8f, 1f) * size, new Vector3(0, -90, 0));
            view.AddWing(wingL, 1, 10f);
            view.AddWing(wingR, -1, 10f);
        }

        static void Butterfly(ObstacleView view, Transform body, Color a, Color b)
        {
            ModelKit.Part("Body", body, MeshLibrary.Cylinder, MaterialLibrary.Toon(new Color(0.2f, 0.15f, 0.2f)), Vector3.zero, new Vector3(0.08f, 0.45f, 0.08f));
            var wingMatA = MaterialLibrary.Toon(a, 0.25f, 0.4f);
            var wingMatB = MaterialLibrary.Toon(b, 0.25f, 0.4f);
            for (int side = -1; side <= 1; side += 2)
            {
                var pivot = ModelKit.Node("Wing", body).transform;
                ModelKit.Part("Upper", pivot, MeshLibrary.Disc, wingMatA, new Vector3(0.25f * side, 0.1f, 0), new Vector3(0.45f, 0.4f, 1f));
                ModelKit.Part("Lower", pivot, MeshLibrary.Disc, wingMatB, new Vector3(0.18f * side, -0.15f, 0), new Vector3(0.3f, 0.28f, 1f));
                view.AddWing(pivot, side, 14f);
            }
        }

        static void Rock(ObstacleView view, Transform body, Color a, Color b, float size)
        {
            var mat = MaterialLibrary.Toon(a, 0f, 0.15f);
            ModelKit.Part("Rock", body, MeshLibrary.Octahedron, mat, Vector3.zero, new Vector3(size * 1.35f, size * 1.1f, size * 1.3f), new Vector3(15, 30, 40));
            ModelKit.Part("Chunk", body, MeshLibrary.Octahedron, MaterialLibrary.Toon(b, 0f, 0.15f), new Vector3(size * 0.2f, size * 0.15f, -size * 0.2f), new Vector3(size * 0.8f, size * 0.7f, size * 0.8f), new Vector3(60, 10, 0));
            view.AddSpinner(body, new Vector3(20, 0, 70));
        }

        static void Drone(ObstacleView view, Transform body, Color a, Color b, float r)
        {
            ModelKit.Part("Saucer", body, MeshLibrary.Sphere, MaterialLibrary.Toon(new Color(0.75f, 0.78f, 0.82f), 0f, 0.8f), Vector3.zero, new Vector3(r * 2.4f, r * 0.7f, r * 2.4f));
            ModelKit.Part("Dome", body, MeshLibrary.Hemisphere, MaterialLibrary.Toon(a, 0.4f, 0.9f), new Vector3(0, r * 0.15f, 0), new Vector3(r * 1.2f, r * 1.2f, r * 1.2f));
            var ring = ModelKit.Node("Lights", body).transform;
            for (int i = 0; i < 6; i++)
            {
                float ang = i / 6f * Mathf.PI * 2f;
                ModelKit.Part("Light", ring, MeshLibrary.LowSphere, MaterialLibrary.Toon(b, 0.9f), new Vector3(Mathf.Cos(ang) * r * 1.05f, -r * 0.05f, Mathf.Sin(ang) * r * 1.05f), Vector3.one * r * 0.22f);
            }
            view.AddSpinner(ring, new Vector3(0, 160, 0));
            ModelKit.Glow("Beam", body, new Vector3(0, -r * 0.8f, -0.2f), r * 2f, new Color(b.r, b.g, b.b, 0.35f));
        }

        static void Cloud(Transform body, Color a, Color b, float radius, bool dark)
        {
            var mat = MaterialLibrary.Toon(a, 0f, 0.05f, null, dark ? 0.3f : 0.8f);
            var mat2 = MaterialLibrary.Toon(b, 0f, 0.05f, null, 0.4f);
            ModelKit.Part("Puff0", body, MeshLibrary.LowSphere, mat, Vector3.zero, Vector3.one * radius * 1.7f);
            ModelKit.Part("Puff1", body, MeshLibrary.LowSphere, mat2, new Vector3(-radius * 0.8f, -radius * 0.2f, 0.1f), Vector3.one * radius * 1.25f);
            ModelKit.Part("Puff2", body, MeshLibrary.LowSphere, mat2, new Vector3(radius * 0.85f, -radius * 0.15f, 0.1f), Vector3.one * radius * 1.3f);
            ModelKit.Part("Puff3", body, MeshLibrary.LowSphere, mat, new Vector3(radius * 0.3f, radius * 0.45f, 0.15f), Vector3.one * radius * 1.1f);
        }

        static ParticleSystem AddSparks(Transform parent, Color color, float radius)
        {
            var s = ParticleSettings.Default;
            s.colorA = color; s.colorB = Color.white; s.rate = 10f; s.radius = radius; s.sizeMin = 0.08f; s.sizeMax = 0.18f;
            s.lifeMin = 0.15f; s.lifeMax = 0.35f; s.texture = ProceduralTextures.Sparkle; s.worldSpace = false; s.speedMin = 0.5f; s.speedMax = 1.5f;
            return ParticleFactory.Create("Sparks", parent, Vector3.zero, s);
        }

        static ParticleSystem AddFire(Transform parent, float radius, bool trail)
        {
            var s = ParticleSettings.Default;
            s.colorA = new Color(1f, 0.45f, 0.1f); s.colorB = new Color(1f, 0.85f, 0.3f);
            s.rate = trail ? 35f : 15f; s.radius = radius * 0.6f; s.sizeMin = radius * 0.6f; s.sizeMax = radius * 1.2f;
            s.lifeMin = 0.25f; s.lifeMax = 0.5f; s.shrink = true; s.speedMin = 0.1f; s.speedMax = 0.4f;
            s.velocity = trail ? new Vector3(0, 2.5f, 0) : new Vector3(0, 0.8f, 0); s.worldSpace = trail; s.maxParticles = 60;
            return ParticleFactory.Create("Fire", parent, Vector3.zero, s);
        }

        static void Lightning(ObstacleView view, Transform root, ObstacleData data)
        {
            float h = data.boxSize.y;
            var cloud = ModelKit.Node("Cloud", root, new Vector3(0, h * 0.5f + 0.4f, 0.5f)).transform;
            Cloud(cloud, data.secondaryColor, data.secondaryColor * 0.8f + Color.black * 0.2f, 0.9f, true);
            var warn = ModelKit.Node("Warning", root).transform;
            ModelKit.Part("WarnBeam", warn, MeshLibrary.Quad, MaterialLibrary.Unlit(new Color(1f, 1f, 0.5f, 0.35f), ProceduralTextures.Streak, true), Vector3.zero, new Vector3(0.25f, h, 1f), new Vector3(0, 0, 90)).transform.localScale = new Vector3(h, 0.35f, 1f);
            ModelKit.Glow("WarnGlow", warn, new Vector3(0, h * 0.5f, 0), 2.2f, new Color(1f, 1f, 0.4f, 0.5f));
            var act = ModelKit.Node("Active", root).transform;
            var bolt = ModelKit.Part("Bolt", act, MeshLibrary.Quad, MaterialLibrary.Unlit(new Color(1f, 1f, 0.75f, 1f), ProceduralTextures.Streak, true), Vector3.zero, new Vector3(h, data.boxSize.x * 1.6f, 1f), new Vector3(0, 0, 90));
            ModelKit.Part("BoltCore", act, MeshLibrary.Quad, MaterialLibrary.Unlit(Color.white, ProceduralTextures.Streak, true), new Vector3(0, 0, -0.05f), new Vector3(h, data.boxSize.x * 0.6f, 1f), new Vector3(0, 0, 90));
            ModelKit.Glow("Flash", act, new Vector3(0, h * 0.5f, -0.1f), 3.5f, new Color(1f, 1f, 0.6f, 0.8f));
            view.warning = warn.gameObject;
            view.active = act.gameObject;
            view.SetState(HazardVisualState.Idle);
            _ = bolt;
        }

        static void Lava(ObstacleView view, Transform root, ObstacleData data)
        {
            float h = data.boxSize.y, w = data.boxSize.x;
            var rockMat = MaterialLibrary.Toon(new Color(0.25f, 0.15f, 0.12f));
            ModelKit.Part("Vent", root, MeshLibrary.Cone, rockMat, new Vector3(0, -h * 0.5f - 0.3f, 0.3f), new Vector3(w * 2.2f, 0.9f, w * 1.4f));
            var warn = ModelKit.Node("Warning", root).transform;
            ModelKit.Glow("WarnGlow", warn, new Vector3(0, -h * 0.5f + 0.2f, -0.2f), w * 2.4f, new Color(1f, 0.4f, 0.1f, 0.8f));
            ModelKit.Part("WarnColumn", warn, MeshLibrary.Quad, MaterialLibrary.Unlit(new Color(1f, 0.5f, 0.1f, 0.25f), ProceduralTextures.Streak, true), Vector3.zero, new Vector3(h, w * 0.7f, 1f), new Vector3(0, 0, 90));
            var act = ModelKit.Node("Active", root).transform;
            ModelKit.Part("LavaColumn", act, MeshLibrary.Cylinder, MaterialLibrary.Toon(data.primaryColor, 1.1f, 0.3f), Vector3.zero, new Vector3(w, h, w * 0.6f));
            ModelKit.Glow("LavaGlow", act, new Vector3(0, 0, -0.5f), w * 3f, new Color(1f, 0.6f, 0.2f, 0.6f)).transform.localScale = new Vector3(w * 2.6f, h * 1.1f, 1f);
            view.warning = warn.gameObject;
            view.active = act.gameObject;
            view.SetState(HazardVisualState.Idle);
        }

        static void Barrier(ObstacleView view, Transform root, ObstacleData data)
        {
            float w = data.boxSize.x;
            var pylonMat = MaterialLibrary.Toon(data.secondaryColor, 0.2f, 0.8f);
            ModelKit.Part("PylonL", root, MeshLibrary.Cylinder, pylonMat, new Vector3(-w * 0.5f - 0.15f, 0, 0), new Vector3(0.28f, 1f, 0.28f));
            ModelKit.Part("PylonR", root, MeshLibrary.Cylinder, pylonMat, new Vector3(w * 0.5f + 0.15f, 0, 0), new Vector3(0.28f, 1f, 0.28f));
            ModelKit.Part("GemL", root, MeshLibrary.Gem, MaterialLibrary.Toon(data.primaryColor, 0.7f, 0.9f), new Vector3(-w * 0.5f - 0.15f, 0.62f, 0), Vector3.one * 0.32f);
            ModelKit.Part("GemR", root, MeshLibrary.Gem, MaterialLibrary.Toon(data.primaryColor, 0.7f, 0.9f), new Vector3(w * 0.5f + 0.15f, 0.62f, 0), Vector3.one * 0.32f);
            var warn = ModelKit.Node("Warning", root).transform;
            ModelKit.Part("WarnBeam", warn, MeshLibrary.Quad, MaterialLibrary.Unlit(new Color(data.primaryColor.r, data.primaryColor.g, data.primaryColor.b, 0.3f), ProceduralTextures.Streak, true), Vector3.zero, new Vector3(w, 0.25f, 1f));
            var act = ModelKit.Node("Active", root).transform;
            ModelKit.Part("Beam", act, MeshLibrary.Quad, MaterialLibrary.Unlit(data.primaryColor, ProceduralTextures.Streak, true), Vector3.zero, new Vector3(w, data.boxSize.y * 2.2f, 1f));
            ModelKit.Part("BeamCore", act, MeshLibrary.Quad, MaterialLibrary.Unlit(Color.white, ProceduralTextures.Streak, true), new Vector3(0, 0, -0.05f), new Vector3(w, data.boxSize.y * 0.8f, 1f));
            view.warning = warn.gameObject;
            view.active = act.gameObject;
            view.SetState(HazardVisualState.Idle);
        }

        static void Beam(ObstacleView view, Transform root, ObstacleData data)
        {
            float h = data.boxSize.y, w = data.boxSize.x;
            var c = data.primaryColor;
            var warn = ModelKit.Node("Warning", root).transform;
            ModelKit.Part("WarnBeam", warn, MeshLibrary.Quad, MaterialLibrary.Unlit(new Color(c.r, c.g, c.b, 0.22f)), Vector3.zero, new Vector3(w, h, 1f));
            ModelKit.Part("WarnEdgeL", warn, MeshLibrary.Quad, MaterialLibrary.Unlit(new Color(c.r, c.g, c.b, 0.7f)), new Vector3(-w * 0.5f, 0, -0.01f), new Vector3(0.06f, h, 1f));
            ModelKit.Part("WarnEdgeR", warn, MeshLibrary.Quad, MaterialLibrary.Unlit(new Color(c.r, c.g, c.b, 0.7f)), new Vector3(w * 0.5f, 0, -0.01f), new Vector3(0.06f, h, 1f));
            var act = ModelKit.Node("Active", root).transform;
            ModelKit.Part("Beam", act, MeshLibrary.Quad, MaterialLibrary.Unlit(c, ProceduralTextures.Streak, true), Vector3.zero, new Vector3(h, w * 2.2f, 1f), new Vector3(0, 0, 90));
            ModelKit.Part("Core", act, MeshLibrary.Quad, MaterialLibrary.Unlit(Color.white, ProceduralTextures.Streak, true), new Vector3(0, 0, -0.05f), new Vector3(h, w * 0.9f, 1f), new Vector3(0, 0, 90));
            view.warning = warn.gameObject;
            view.active = act.gameObject;
            view.SetState(HazardVisualState.Idle);
        }

        static void Wind(ObstacleView view, Transform root, ObstacleData data)
        {
            var s = ParticleSettings.Default;
            s.colorA = new Color(1f, 1f, 1f, 0.7f); s.colorB = new Color(0.8f, 0.95f, 1f, 0.5f);
            s.shape = ParticleSystemShapeType.Box; s.boxSize = new Vector3(data.boxSize.x, data.boxSize.y, 0.1f);
            s.rate = 16f; s.sizeMin = 0.35f; s.sizeMax = 0.7f; s.lifeMin = 0.6f; s.lifeMax = 0.9f; s.speedMin = 0f; s.speedMax = 0.1f;
            s.texture = ProceduralTextures.Streak; s.worldSpace = false; s.additive = true; s.velocity = new Vector3(4f, 0, 0);
            var ps = ParticleFactory.Create("WindStreaks", root, Vector3.zero, s);
            ModelKit.Part("Zone", root, MeshLibrary.Quad, MaterialLibrary.Unlit(new Color(0.85f, 0.95f, 1f, 0.12f), ProceduralTextures.SoftCircle), new Vector3(0, 0, 0.5f), new Vector3(data.boxSize.x * 1.3f, data.boxSize.y * 1.2f, 1f));
            view.body = root;
            view.trail = ps;
        }

        static void Wall(ObstacleView view, Transform root, ObstacleData data)
        {
            var sized = ModelKit.Node("Sized", root).transform;
            bool gate = data.visual == ObstacleVisual.GoldenGate;
            var mat = MaterialLibrary.Toon(data.primaryColor, gate ? 0.15f : 0f, gate ? 0.8f : 0.15f);
            var mat2 = MaterialLibrary.Toon(data.secondaryColor, gate ? 0.3f : 0f, gate ? 0.9f : 0.15f);
            ModelKit.Part("Core", sized, MeshLibrary.Cube, mat, Vector3.zero, Vector3.one);
            ModelKit.Part("Cap", sized, MeshLibrary.Cube, mat2, new Vector3(0, 0.45f, -0.05f), new Vector3(1.02f, 0.2f, 1.1f));
            if (!gate)
            {
                ModelKit.Part("Rock1", sized, MeshLibrary.Octahedron, mat2, new Vector3(-0.3f, -0.3f, -0.4f), new Vector3(0.35f, 0.5f, 0.4f));
                ModelKit.Part("Rock2", sized, MeshLibrary.Octahedron, mat, new Vector3(0.25f, 0.1f, -0.45f), new Vector3(0.3f, 0.45f, 0.4f));
            }
            else
            {
                for (int i = 0; i < 4; i++) ModelKit.Part("Bar" + i, sized, MeshLibrary.Cylinder, mat2, new Vector3(-0.36f + i * 0.24f, 0, -0.5f), new Vector3(0.07f, 1f, 0.07f));
            }
            view.sizedPart = sized;
            sized.localScale = new Vector3(data.boxSize.x, data.boxSize.y, 1f);
        }
    }
}
