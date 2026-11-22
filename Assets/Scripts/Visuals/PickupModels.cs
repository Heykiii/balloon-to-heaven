using BalloonToHeaven.Core;
using UnityEngine;

namespace BalloonToHeaven.Visuals
{
    /// <summary>Spins and bobs pickups.</summary>
    public sealed class PickupSpinner : MonoBehaviour
    {
        public float spinSpeed = 180f;
        public float bobAmount = 0.08f;
        public Transform target;
        float phase;

        void OnEnable() { phase = Random.value * 6f; }

        void Update()
        {
            if (target == null) return;
            target.Rotate(0, spinSpeed * Time.deltaTime, 0, Space.Self);
            var p = target.localPosition;
            p.y = Mathf.Sin(Time.time * 3f + phase) * bobAmount;
            target.localPosition = p;
        }
    }

    public static class PickupModels
    {
        public static readonly Color Gold = new Color(1f, 0.8f, 0.2f);

        public static GameObject Coin(Transform parent, bool special)
        {
            var root = new GameObject(special ? "StarCoin" : "Coin");
            root.transform.SetParent(parent, false);
            var spin = ModelKit.Node("Spin", root.transform).transform;
            float size = special ? 0.9f : 0.5f;
            var gold = MaterialLibrary.Toon(Gold, special ? 0.35f : 0.15f, 0.9f);
            ModelKit.Part("Disc", spin, MeshLibrary.Cylinder, gold, Vector3.zero, new Vector3(size, 0.08f, size), new Vector3(90, 0, 0));
            ModelKit.Part("Rim", spin, MeshLibrary.Torus, MaterialLibrary.Toon(new Color(1f, 0.65f, 0.1f), 0.1f, 0.9f), Vector3.zero, new Vector3(size * 1.0f, size * 0.6f, size * 1.0f), new Vector3(90, 0, 0));
            if (special)
            {
                ModelKit.Part("Star", spin, MeshLibrary.Gem, MaterialLibrary.Toon(Color.white, 0.6f, 0.9f), new Vector3(0, 0, -0.08f), new Vector3(0.45f, 0.45f, 0.2f), new Vector3(-90, 0, 0));
                ModelKit.Glow("Glow", root.transform, new Vector3(0, 0, 0.3f), 2f, new Color(1f, 0.9f, 0.4f, 0.55f));
            }
            var spinner = root.AddComponent<PickupSpinner>();
            spinner.target = spin;
            spinner.spinSpeed = special ? 120f : 220f;
            return root;
        }

        public static GameObject PowerUp(Transform parent, PowerUpType type, Color color)
        {
            var root = new GameObject("PowerUp_" + type);
            root.transform.SetParent(parent, false);
            var spin = ModelKit.Node("Spin", root.transform).transform;
            ModelKit.Part("Bubble", root.transform, MeshLibrary.Sphere, MaterialLibrary.Shield(color, 0.9f, 1.8f), Vector3.zero, Vector3.one * 1.25f);
            ModelKit.Glow("Glow", root.transform, new Vector3(0, 0, 0.4f), 2.2f, new Color(color.r, color.g, color.b, 0.45f));
            var mat = MaterialLibrary.Toon(color, 0.4f, 0.8f);
            var white = MaterialLibrary.Toon(Color.white, 0.3f, 0.8f);
            switch (type)
            {
                case PowerUpType.CoinMagnet:
                    ModelKit.Part("U", spin, MeshLibrary.Arc, mat, new Vector3(0, -0.05f, 0), new Vector3(0.6f, 0.6f, 2.5f), new Vector3(0, 0, 180));
                    ModelKit.Part("TipL", spin, MeshLibrary.Cube, white, new Vector3(-0.3f, 0.12f, 0), new Vector3(0.14f, 0.16f, 0.14f));
                    ModelKit.Part("TipR", spin, MeshLibrary.Cube, white, new Vector3(0.3f, 0.12f, 0), new Vector3(0.14f, 0.16f, 0.14f));
                    break;
                case PowerUpType.EnergyShield:
                    ModelKit.Part("Shield", spin, MeshLibrary.Hemisphere, mat, Vector3.zero, new Vector3(0.6f, 0.75f, 0.25f), new Vector3(-90, 0, 0));
                    ModelKit.Part("Boss", spin, MeshLibrary.LowSphere, white, new Vector3(0, 0, -0.12f), Vector3.one * 0.2f);
                    break;
                case PowerUpType.SkyRocket:
                    ModelKit.Part("Body", spin, MeshLibrary.Cylinder, white, Vector3.zero, new Vector3(0.22f, 0.5f, 0.22f));
                    ModelKit.Part("Nose", spin, MeshLibrary.Cone, mat, new Vector3(0, 0.36f, 0), new Vector3(0.22f, 0.22f, 0.22f));
                    ModelKit.Part("FinL", spin, MeshLibrary.Cube, mat, new Vector3(-0.15f, -0.22f, 0), new Vector3(0.12f, 0.18f, 0.04f));
                    ModelKit.Part("FinR", spin, MeshLibrary.Cube, mat, new Vector3(0.15f, -0.22f, 0), new Vector3(0.12f, 0.18f, 0.04f));
                    break;
                case PowerUpType.ExtraLife:
                    ModelKit.Part("HeartL", spin, MeshLibrary.LowSphere, mat, new Vector3(-0.13f, 0.08f, 0), Vector3.one * 0.32f);
                    ModelKit.Part("HeartR", spin, MeshLibrary.LowSphere, mat, new Vector3(0.13f, 0.08f, 0), Vector3.one * 0.32f);
                    ModelKit.Part("HeartTip", spin, MeshLibrary.Cone, mat, new Vector3(0, -0.15f, 0), new Vector3(0.42f, 0.35f, 0.25f), new Vector3(180, 0, 0));
                    break;
                case PowerUpType.DoubleCoins:
                    var gold = MaterialLibrary.Toon(Gold, 0.3f, 0.9f);
                    ModelKit.Part("CoinA", spin, MeshLibrary.Cylinder, gold, new Vector3(-0.12f, 0.06f, 0.05f), new Vector3(0.42f, 0.06f, 0.42f), new Vector3(90, 0, 0));
                    ModelKit.Part("CoinB", spin, MeshLibrary.Cylinder, gold, new Vector3(0.12f, -0.06f, -0.05f), new Vector3(0.42f, 0.06f, 0.42f), new Vector3(90, 0, 0));
                    break;
            }
            var spinner = root.AddComponent<PickupSpinner>();
            spinner.target = spin;
            spinner.spinSpeed = 90f;
            return root;
        }

        public static GameObject SkyCrystal(Transform parent)
        {
            var root = new GameObject("SkyCrystal");
            root.transform.SetParent(parent, false);
            var spin = ModelKit.Node("Spin", root.transform).transform;
            ModelKit.Part("Gem", spin, MeshLibrary.Gem, MaterialLibrary.Toon(new Color(0.55f, 0.95f, 1f), 0.6f, 0.95f, null, 1.3f), Vector3.zero, new Vector3(0.7f, 0.9f, 0.7f));
            ModelKit.Glow("Glow", root.transform, new Vector3(0, 0, 0.3f), 2.4f, new Color(0.5f, 0.95f, 1f, 0.6f));
            var spinner = root.AddComponent<PickupSpinner>();
            spinner.target = spin;
            spinner.spinSpeed = 140f;
            return root;
        }
    }

    /// <summary>Subtle camera shake. Never used for normal movement to keep gameplay comfortable.</summary>
    public sealed class CameraShake : MonoBehaviour
    {
        public static bool Enabled = true;
        Vector3 basePosition;
        float trauma;

        void Awake() { basePosition = transform.localPosition; }

        public void Shake(float amount)
        {
            if (!Enabled) return;
            trauma = Mathf.Clamp01(Mathf.Max(trauma, amount));
        }

        void LateUpdate()
        {
            if (trauma <= 0f) { transform.localPosition = basePosition; return; }
            trauma = Mathf.Max(0f, trauma - Time.unscaledDeltaTime * 2.2f);
            float s = trauma * trauma * 0.35f;
            float t = Time.unscaledTime * 30f;
            transform.localPosition = basePosition + new Vector3((Mathf.PerlinNoise(t, 0f) - 0.5f) * s, (Mathf.PerlinNoise(0f, t) - 0.5f) * s, 0f);
        }
    }

    public static class CameraRig
    {
        /// <summary>Creates the orthographic 2.5D camera used by every scene.</summary>
        public static Camera Create(float orthoSize, Color background, Transform parent = null)
        {
            var go = new GameObject("Main Camera");
            if (parent != null) go.transform.SetParent(parent, false);
            go.tag = "MainCamera";
            go.transform.position = new Vector3(0, 0, -20f);
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = orthoSize;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 200f;
            cam.allowHDR = false;
            cam.allowMSAA = QualityManager.CurrentLevel == 2;
            go.AddComponent<CameraShake>();
            go.AddComponent<AudioListener>();
            return cam;
        }

        /// <summary>
        /// Keeps the playfield fully visible on any portrait screen: for screens narrower than 9:16
        /// the orthographic size grows so the full gameplay width is always on screen.
        /// </summary>
        public static void FitPlayfield(Camera cam, float baseOrthoSize, float halfWidth)
        {
            float aspect = Mathf.Max(0.3f, cam.aspect);
            cam.orthographicSize = Mathf.Max(baseOrthoSize, halfWidth / aspect);
        }
    }
}
