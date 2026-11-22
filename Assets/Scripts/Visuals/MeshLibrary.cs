using System.Collections.Generic;
using UnityEngine;

namespace BalloonToHeaven.Visuals
{
    /// <summary>
    /// Procedurally generated low-poly meshes shared by every model. Generated once and cached,
    /// so the game needs no imported model files and no physics colliders.
    /// </summary>
    public static class MeshLibrary
    {
        static Mesh sphere, lowSphere, cube, cylinder, cone, torus, thinTorus, octa, quad, disc, wing, teardrop, gem, hemisphere;

        public static Mesh Sphere => sphere ? sphere : (sphere = BuildSphere(20, 14, "Sphere"));
        public static Mesh LowSphere => lowSphere ? lowSphere : (lowSphere = BuildSphere(10, 7, "LowSphere"));
        public static Mesh Cube => cube ? cube : (cube = BuildCube());
        public static Mesh Cylinder => cylinder ? cylinder : (cylinder = BuildCylinder(14, 0.5f, 0.5f, "Cylinder"));
        public static Mesh Cone => cone ? cone : (cone = BuildCylinder(14, 0.5f, 0.0f, "Cone"));
        public static Mesh Torus => torus ? torus : (torus = BuildTorus(24, 8, 0.5f, 0.12f, "Torus"));
        public static Mesh ThinTorus => thinTorus ? thinTorus : (thinTorus = BuildTorus(28, 6, 0.5f, 0.04f, "ThinTorus"));
        public static Mesh Octahedron => octa ? octa : (octa = BuildOctahedron());
        public static Mesh Quad => quad ? quad : (quad = BuildQuad());
        public static Mesh Disc => disc ? disc : (disc = BuildDisc(24));
        public static Mesh Wing => wing ? wing : (wing = BuildWing());
        public static Mesh Teardrop => teardrop ? teardrop : (teardrop = BuildTeardrop(20, 16));
        public static Mesh Gem => gem ? gem : (gem = BuildGem());
        static Mesh arc;
        /// <summary>Half torus (rainbow arc) in the XY plane, radius 0.5.</summary>
        public static Mesh Arc => arc ? arc : (arc = BuildArc(28, 6, 0.5f, 0.05f));
        public static Mesh Hemisphere => hemisphere ? hemisphere : (hemisphere = BuildSphere(18, 7, "Hemisphere", 0.5f));

        static Mesh Finish(Mesh m, string name)
        {
            m.name = name;
            m.RecalculateBounds();
            return m;
        }

        /// <summary>UV sphere of radius 0.5. "fraction" lets us build a hemisphere (top part only).</summary>
        static Mesh BuildSphere(int segments, int rings, string name, float fraction = 1f)
        {
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            for (int r = 0; r <= rings; r++)
            {
                float v = r / (float)rings;
                float phi = Mathf.PI * (1f - v * fraction);
                for (int s = 0; s <= segments; s++)
                {
                    float u = s / (float)segments;
                    float theta = u * Mathf.PI * 2f;
                    var n = new Vector3(Mathf.Sin(phi) * Mathf.Cos(theta), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(theta));
                    verts.Add(n * 0.5f);
                    normals.Add(n);
                    uvs.Add(new Vector2(u, 1f - v * fraction));
                }
            }
            int row = segments + 1;
            for (int r = 0; r < rings; r++)
            {
                for (int s = 0; s < segments; s++)
                {
                    int a = r * row + s, b = a + row;
                    tris.Add(a); tris.Add(a + 1); tris.Add(b);
                    tris.Add(b); tris.Add(a + 1); tris.Add(b + 1);
                }
            }
            var m = new Mesh();
            m.SetVertices(verts); m.SetUVs(0, uvs); m.SetTriangles(tris, 0);
            FixWinding(m);
            m.SetNormals(normals); // keep the analytic normals (no seam)
            return Finish(m, name);
        }

        /// <summary>Balloon envelope: round top that narrows to a mouth at the bottom.</summary>
        static Mesh BuildTeardrop(int segments, int rings)
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            for (int r = 0; r <= rings; r++)
            {
                float v = r / (float)rings;          // 0 bottom .. 1 top
                float phi = Mathf.PI * (1f - v);
                float radius = Mathf.Sin(phi) * 0.5f;
                float y = Mathf.Cos(phi) * 0.5f;
                // Pinch the lower half into a cone towards the mouth
                if (v < 0.5f)
                {
                    float k = v / 0.5f;
                    radius = Mathf.Lerp(0.09f, 0.5f, Mathf.Pow(k, 0.75f));
                    y = Mathf.Lerp(-0.62f, 0f, k);
                }
                for (int s = 0; s <= segments; s++)
                {
                    float u = s / (float)segments;
                    float theta = u * Mathf.PI * 2f;
                    verts.Add(new Vector3(Mathf.Cos(theta) * radius, y, Mathf.Sin(theta) * radius));
                    uvs.Add(new Vector2(u, v));
                }
            }
            int row = segments + 1;
            for (int r = 0; r < rings; r++)
            {
                for (int s = 0; s < segments; s++)
                {
                    int a = r * row + s, b = a + row;
                    tris.Add(a); tris.Add(a + 1); tris.Add(b);
                    tris.Add(b); tris.Add(a + 1); tris.Add(b + 1);
                }
            }
            var m = new Mesh();
            m.SetVertices(verts); m.SetUVs(0, uvs); m.SetTriangles(tris, 0);
            FixWinding(m);
            return Finish(m, "Teardrop");
        }

        static Mesh BuildCube()
        {
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            Vector3[] dirs = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
            foreach (var n in dirs)
            {
                Vector3 t1 = Vector3.Cross(n, Mathf.Abs(n.y) > 0.5f ? Vector3.forward : Vector3.up).normalized;
                Vector3 t2 = Vector3.Cross(n, t1);
                int start = verts.Count;
                verts.Add((n - t1 - t2) * 0.5f); verts.Add((n + t1 - t2) * 0.5f);
                verts.Add((n + t1 + t2) * 0.5f); verts.Add((n - t1 + t2) * 0.5f);
                for (int i = 0; i < 4; i++) normals.Add(n);
                uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(1, 0)); uvs.Add(new Vector2(1, 1)); uvs.Add(new Vector2(0, 1));
                tris.Add(start); tris.Add(start + 2); tris.Add(start + 1);
                tris.Add(start); tris.Add(start + 3); tris.Add(start + 2);
            }
            var m = new Mesh();
            m.SetVertices(verts); m.SetNormals(normals); m.SetUVs(0, uvs); m.SetTriangles(tris, 0);
            FixWinding(m);
            return Finish(m, "Cube");
        }

        /// <summary>Cylinder/cone of height 1 centred on the origin, with caps.</summary>
        static Mesh BuildCylinder(int segments, float bottomRadius, float topRadius, string name)
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            for (int s = 0; s <= segments; s++)
            {
                float u = s / (float)segments, a = u * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                verts.Add(dir * bottomRadius + Vector3.down * 0.5f); uvs.Add(new Vector2(u, 0));
                verts.Add(dir * topRadius + Vector3.up * 0.5f); uvs.Add(new Vector2(u, 1));
            }
            for (int s = 0; s < segments; s++)
            {
                int i = s * 2;
                tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
                tris.Add(i + 2); tris.Add(i + 1); tris.Add(i + 3);
            }
            AddCap(verts, uvs, tris, segments, bottomRadius, -0.5f, false);
            if (topRadius > 0.001f) AddCap(verts, uvs, tris, segments, topRadius, 0.5f, true);
            var m = new Mesh();
            m.SetVertices(verts); m.SetUVs(0, uvs); m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            FixWinding(m);
            return Finish(m, name);
        }

        static void AddCap(List<Vector3> verts, List<Vector2> uvs, List<int> tris, int segments, float radius, float y, bool up)
        {
            int center = verts.Count;
            verts.Add(new Vector3(0, y, 0)); uvs.Add(new Vector2(0.5f, 0.5f));
            for (int s = 0; s <= segments; s++)
            {
                float a = s / (float)segments * Mathf.PI * 2f;
                verts.Add(new Vector3(Mathf.Cos(a) * radius, y, Mathf.Sin(a) * radius));
                uvs.Add(new Vector2(Mathf.Cos(a) * 0.5f + 0.5f, Mathf.Sin(a) * 0.5f + 0.5f));
            }
            for (int s = 0; s < segments; s++)
            {
                if (up) { tris.Add(center); tris.Add(center + s + 2); tris.Add(center + s + 1); }
                else { tris.Add(center); tris.Add(center + s + 1); tris.Add(center + s + 2); }
            }
        }

        static Mesh BuildTorus(int segments, int sides, float radius, float tube, string name)
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            for (int s = 0; s <= segments; s++)
            {
                float a = s / (float)segments * Mathf.PI * 2f;
                var center = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * radius;
                for (int t = 0; t <= sides; t++)
                {
                    float b = t / (float)sides * Mathf.PI * 2f;
                    var normal = new Vector3(Mathf.Cos(a) * Mathf.Cos(b), Mathf.Sin(b), Mathf.Sin(a) * Mathf.Cos(b));
                    verts.Add(center + normal * tube);
                    uvs.Add(new Vector2(s / (float)segments, t / (float)sides));
                }
            }
            int row = sides + 1;
            for (int s = 0; s < segments; s++)
            {
                for (int t = 0; t < sides; t++)
                {
                    int a = s * row + t, b = a + row;
                    // Wound so that faces point away from the tube centre (not the torus centre).
                    tris.Add(a); tris.Add(a + 1); tris.Add(b);
                    tris.Add(a + 1); tris.Add(b + 1); tris.Add(b);
                }
            }
            var m = new Mesh();
            m.SetVertices(verts); m.SetUVs(0, uvs); m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            return Finish(m, name);
        }

        static Mesh BuildArc(int segments, int sides, float radius, float tube)
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            var uvs = new List<Vector2>();
            for (int s = 0; s <= segments; s++)
            {
                float a = s / (float)segments * Mathf.PI;
                var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                for (int t = 0; t <= sides; t++)
                {
                    float b = t / (float)sides * Mathf.PI * 2f;
                    var n = dir * Mathf.Cos(b) + Vector3.forward * Mathf.Sin(b);
                    verts.Add(dir * radius + n * tube);
                    uvs.Add(new Vector2(s / (float)segments, t / (float)sides));
                }
            }
            int row = sides + 1;
            for (int s = 0; s < segments; s++)
                for (int t = 0; t < sides; t++)
                {
                    int a = s * row + t, b = a + row;
                    tris.Add(a); tris.Add(b); tris.Add(a + 1);
                    tris.Add(a + 1); tris.Add(b); tris.Add(b + 1);
                }
            var m = new Mesh();
            m.SetVertices(verts); m.SetUVs(0, uvs); m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            return Finish(m, "Arc");
        }

        static Mesh BuildOctahedron()
        {
            Vector3[] p = { Vector3.up * 0.5f, Vector3.down * 0.5f, Vector3.left * 0.3f, Vector3.right * 0.3f, Vector3.forward * 0.3f, Vector3.back * 0.3f };
            int[,] faces = { { 0, 3, 4 }, { 0, 4, 2 }, { 0, 2, 5 }, { 0, 5, 3 }, { 1, 4, 3 }, { 1, 2, 4 }, { 1, 5, 2 }, { 1, 3, 5 } };
            return FlatMesh(p, faces, "Octahedron");
        }

        static Mesh BuildGem()
        {
            // Six sided gem: crown ring, girdle and point.
            var p = new List<Vector3> { new Vector3(0, 0.35f, 0), new Vector3(0, -0.5f, 0) };
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f;
                p.Add(new Vector3(Mathf.Cos(a) * 0.5f, 0.05f, Mathf.Sin(a) * 0.5f));
            }
            var faces = new int[12, 3];
            for (int i = 0; i < 6; i++)
            {
                int a = 2 + i, b = 2 + (i + 1) % 6;
                faces[i, 0] = 0; faces[i, 1] = b; faces[i, 2] = a;
                faces[i + 6, 0] = 1; faces[i + 6, 1] = a; faces[i + 6, 2] = b;
            }
            return FlatMesh(p.ToArray(), faces, "Gem");
        }

        static Mesh FlatMesh(Vector3[] points, int[,] faces, string name)
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            var uvs = new List<Vector2>();
            for (int f = 0; f < faces.GetLength(0); f++)
            {
                for (int k = 0; k < 3; k++)
                {
                    tris.Add(verts.Count);
                    verts.Add(points[faces[f, k]]);
                    uvs.Add(new Vector2(k * 0.5f, k == 1 ? 1 : 0));
                }
            }
            var m = new Mesh();
            m.SetVertices(verts); m.SetUVs(0, uvs); m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            FixWinding(m);
            return Finish(m, name);
        }

        static Mesh BuildQuad()
        {
            var m = new Mesh();
            m.vertices = new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(0.5f, 0.5f, 0), new Vector3(-0.5f, 0.5f, 0) };
            m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            m.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
            return Finish(m, "Quad");
        }

        static Mesh BuildDisc(int segments)
        {
            var verts = new List<Vector3> { Vector3.zero };
            var uvs = new List<Vector2> { new Vector2(0.5f, 0.5f) };
            var tris = new List<int>();
            for (int s = 0; s <= segments; s++)
            {
                float a = s / (float)segments * Mathf.PI * 2f;
                verts.Add(new Vector3(Mathf.Cos(a) * 0.5f, Mathf.Sin(a) * 0.5f, 0));
                uvs.Add(new Vector2(Mathf.Cos(a) * 0.5f + 0.5f, Mathf.Sin(a) * 0.5f + 0.5f));
            }
            for (int s = 0; s < segments; s++) { tris.Add(0); tris.Add(s + 2); tris.Add(s + 1); }
            var m = new Mesh();
            m.SetVertices(verts); m.SetUVs(0, uvs); m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            return Finish(m, "Disc");
        }

        /// <summary>A thin curved wing shape, extending along +X.</summary>
        static Mesh BuildWing()
        {
            Vector3[] p =
            {
                new Vector3(0, 0.12f, 0.06f), new Vector3(0, -0.12f, 0.06f), new Vector3(0, 0.12f, -0.06f), new Vector3(0, -0.12f, -0.06f),
                new Vector3(0.55f, 0.22f, 0.02f), new Vector3(0.5f, -0.05f, 0.02f), new Vector3(1f, 0.05f, 0f)
            };
            int[,] faces =
            {
                { 0, 4, 1 }, { 1, 4, 5 }, { 4, 6, 5 },
                { 2, 3, 4 }, { 3, 5, 4 }, { 4, 5, 6 },
                { 0, 2, 4 }, { 1, 5, 3 }
            };
            return FlatMesh(p, faces, "Wing");
        }

        /// <summary>
        /// Ensures every triangle faces away from the mesh centre (outward). Unity uses clockwise
        /// winding for front faces; for convex-ish shapes this keeps lighting and culling correct
        /// regardless of how the generator ordered vertices.
        /// </summary>
        static void FixWinding(Mesh m)
        {
            var verts = m.vertices;
            var tris = m.triangles;
            var center = m.bounds.center;
            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector3 a = verts[tris[i]], b = verts[tris[i + 1]], c = verts[tris[i + 2]];
                Vector3 normal = Vector3.Cross(b - a, c - a);
                Vector3 outward = (a + b + c) / 3f - center;
                if (Vector3.Dot(normal, outward) < 0f)
                {
                    int t = tris[i + 1]; tris[i + 1] = tris[i + 2]; tris[i + 2] = t;
                }
            }
            m.triangles = tris;
            m.RecalculateNormals();
        }
    }
}
