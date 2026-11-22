using UnityEngine;

namespace BalloonToHeaven.Visuals
{
    /// <summary>Helpers to assemble models from the procedural mesh library.</summary>
    public static class ModelKit
    {
        public static GameObject Node(string name, Transform parent, Vector3 localPosition = default)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            return go;
        }

        public static MeshRenderer Part(string name, Transform parent, Mesh mesh, Material material, Vector3 position, Vector3 scale, Vector3 euler = default)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.transform.localEulerAngles = euler;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            return r;
        }

        /// <summary>A thin cylinder spanning from a to b (local space of parent).</summary>
        public static MeshRenderer Rope(string name, Transform parent, Vector3 a, Vector3 b, float thickness, Material material)
        {
            var mid = (a + b) * 0.5f;
            var dir = b - a;
            var r = Part(name, parent, MeshLibrary.Cylinder, material, mid, new Vector3(thickness, dir.magnitude, thickness));
            r.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
            return r;
        }

        /// <summary>Camera-facing soft sprite quad (unlit), used for glows and cloud puffs.</summary>
        public static MeshRenderer Glow(string name, Transform parent, Vector3 position, float size, Color color, bool additive = true, Texture texture = null)
        {
            return Part(name, parent, MeshLibrary.Quad, MaterialLibrary.Unlit(color, texture != null ? texture : ProceduralTextures.SoftCircle, additive), position, Vector3.one * size);
        }

        public static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform) SetLayerRecursive(child.gameObject, layer);
        }
    }
}
