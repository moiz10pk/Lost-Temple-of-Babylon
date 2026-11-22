using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Babylon.EditorTools
{
    /// <summary>Stores generated meshes as sub-assets of one container per scene / prefab set (so they persist).</summary>
    public static class MeshStore
    {
        public const string Dir = "Assets/_Babylon/Generated/Meshes";
        static Mesh container;
        static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();

        public static void Begin(string name)
        {
            System.IO.Directory.CreateDirectory(Dir);
            string path = $"{Dir}/{name}.asset";
            AssetDatabase.DeleteAsset(path);
            container = new Mesh { name = name };
            AssetDatabase.CreateAsset(container, path);
            cache.Clear();
        }

        public static Mesh Add(Mesh m)
        {
            if (container != null && !AssetDatabase.Contains(m)) AssetDatabase.AddObjectToAsset(m, container);
            return m;
        }

        public static Mesh Cached(string key, System.Func<Mesh> make)
        {
            if (cache.TryGetValue(key, out var m) && m) return m;
            m = Add(make()); m.name = key;
            cache[key] = m;
            return m;
        }

        public static void End() { AssetDatabase.SaveAssets(); container = null; cache.Clear(); }
    }

    /// <summary>Procedural meshes: world-scaled UV boxes, cylinders, rings, arcs, dunes, rocks, leaves.</summary>
    public static class MeshGen
    {
        static Mesh Finish(Mesh m, List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> t)
        {
            if (v.Count > 65000) m.indexFormat = IndexFormat.UInt32;
            m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0);
            if (n != null && n.Count == v.Count) m.SetNormals(n); else m.RecalculateNormals();
            m.RecalculateTangents(); m.RecalculateBounds();
            return m;
        }

        /// <summary>Box centred on the origin with UVs in world units / tile (so textures don't stretch).</summary>
        public static Mesh Box(Vector3 size, float tile = 2f)
        {
            return MeshStore.Cached($"Box_{size.x:0.##}x{size.y:0.##}x{size.z:0.##}_{tile:0.##}", () => BuildBox(size, tile));
        }

        static Mesh BuildBox(Vector3 s, float tile)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            Vector3 h = s * 0.5f;
            void Face(Vector3 normal, Vector3 up, float w, float hh)
            {
                Vector3 right = Vector3.Cross(normal, up);   // screen-right when viewed from outside (Unity is left-handed)
                Vector3 c = Vector3.Scale(normal, h);
                int i = v.Count;
                Vector3 r = right * (w * 0.5f), u = up * (hh * 0.5f);
                v.Add(c - r - u); v.Add(c + r - u); v.Add(c + r + u); v.Add(c - r + u);
                for (int k = 0; k < 4; k++) n.Add(normal);
                uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(w / tile, 0)); uv.Add(new Vector2(w / tile, hh / tile)); uv.Add(new Vector2(0, hh / tile));
                t.Add(i); t.Add(i + 2); t.Add(i + 1); t.Add(i); t.Add(i + 3); t.Add(i + 2);
            }
            Face(Vector3.forward, Vector3.up, s.x, s.y);
            Face(Vector3.back, Vector3.up, s.x, s.y);
            Face(Vector3.right, Vector3.up, s.z, s.y);
            Face(Vector3.left, Vector3.up, s.z, s.y);
            Face(Vector3.up, Vector3.forward, s.x, s.z);
            Face(Vector3.down, Vector3.back, s.x, s.z);
            return Finish(new Mesh(), v, n, uv, t);
        }

        public static Mesh Cylinder(float radius, float height, int seg = 20, float tile = 2f, float topRadius = -1)
        {
            if (topRadius < 0) topRadius = radius;
            return MeshStore.Cached($"Cyl_{radius:0.##}_{topRadius:0.##}_{height:0.##}_{seg}", () =>
            {
                var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
                float circ = 2 * Mathf.PI * radius;
                for (int i = 0; i <= seg; i++)
                {
                    float a = i / (float)seg * Mathf.PI * 2;
                    Vector3 d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                    v.Add(d * radius + Vector3.down * height * 0.5f); v.Add(d * topRadius + Vector3.up * height * 0.5f);
                    float u = i / (float)seg * circ / tile;
                    uv.Add(new Vector2(u, 0)); uv.Add(new Vector2(u, height / tile));
                }
                for (int i = 0; i < seg; i++) { int b = i * 2; t.AddRange(new[] { b, b + 1, b + 2, b + 2, b + 1, b + 3 }); }
                // caps
                foreach (int side in new[] { -1, 1 })
                {
                    int c = v.Count; float r = side > 0 ? topRadius : radius;
                    v.Add(Vector3.up * height * 0.5f * side); uv.Add(new Vector2(0.5f, 0.5f));
                    for (int i = 0; i <= seg; i++)
                    {
                        float a = i / (float)seg * Mathf.PI * 2;
                        v.Add(new Vector3(Mathf.Cos(a) * r, height * 0.5f * side, Mathf.Sin(a) * r));
                        uv.Add(new Vector2(Mathf.Cos(a) * r / tile + 0.5f, Mathf.Sin(a) * r / tile + 0.5f));
                    }
                    for (int i = 0; i < seg; i++)
                    {
                        if (side > 0) t.AddRange(new[] { c, c + i + 2, c + i + 1 });
                        else t.AddRange(new[] { c, c + i + 1, c + i + 2 });
                    }
                }
                var m = new Mesh();
                m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0);
                m.RecalculateNormals(); m.RecalculateTangents(); m.RecalculateBounds();
                return m;
            });
        }

        /// <summary>Flat ring in XZ (radius 1) used by the shockwave.</summary>
        public static Mesh Ring(float inner = 0.86f, int seg = 64, float startDeg = 0f, float endDeg = 360f)
        {
            return MeshStore.Cached($"Ring_{inner}_{startDeg}_{endDeg}", () =>
            {
                var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
                for (int i = 0; i <= seg; i++)
                {
                    float a = Mathf.Lerp(startDeg, endDeg, i / (float)seg) * Mathf.Deg2Rad;
                    Vector3 d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                    v.Add(d * inner); v.Add(d); uv.Add(new Vector2(i / (float)seg, 0)); uv.Add(new Vector2(i / (float)seg, 1));
                }
                for (int i = 0; i < seg; i++) { int b = i * 2; t.AddRange(new[] { b, b + 2, b + 1, b + 2, b + 3, b + 1 }); }
                return Finish(new Mesh(), v, null, uv, t);
            });
        }

        /// <summary>Flat disc or wedge (arc degrees) in XZ, radius 1, facing +Z. UVs map a square texture.</summary>
        public static Mesh Fan(float degrees, int seg = 48)
        {
            return MeshStore.Cached($"Fan_{degrees}", () =>
            {
                var v = new List<Vector3> { Vector3.zero }; var uv = new List<Vector2> { new Vector2(0.5f, 0.5f) }; var t = new List<int>();
                float half = degrees * 0.5f * Mathf.Deg2Rad;
                for (int i = 0; i <= seg; i++)
                {
                    float a = -half + i / (float)seg * half * 2 + Mathf.PI / 2;
                    Vector3 d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                    v.Add(d); uv.Add(new Vector2(d.x * 0.5f + 0.5f, d.z * 0.5f + 0.5f));
                }
                for (int i = 0; i < seg; i++) t.AddRange(new[] { 0, i + 2, i + 1 });
                return Finish(new Mesh(), v, null, uv, t);
            });
        }

        public static Mesh Quad(float w, float h, float tile = 1f)
        {
            return MeshStore.Cached($"Quad_{w}_{h}_{tile}", () =>
            {
                var v = new List<Vector3> { new Vector3(-w / 2, -h / 2, 0), new Vector3(w / 2, -h / 2, 0), new Vector3(w / 2, h / 2, 0), new Vector3(-w / 2, h / 2, 0) };
                var uv = new List<Vector2> { Vector2.zero, new Vector2(w / tile, 0), new Vector2(w / tile, h / tile), new Vector2(0, h / tile) };
                return Finish(new Mesh(), v, null, uv, new List<int> { 0, 2, 1, 0, 3, 2 });
            });
        }

        /// <summary>Minimap arrow pointing +Z, lying flat.</summary>
        public static Mesh Arrow() => MeshStore.Cached("Arrow", () =>
        {
            var v = new List<Vector3> { new Vector3(0, 0, 1.2f), new Vector3(0.8f, 0, -0.8f), new Vector3(0, 0, -0.3f), new Vector3(-0.8f, 0, -0.8f) };
            var uv = new List<Vector2> { Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero };
            return Finish(new Mesh(), v, null, uv, new List<int> { 0, 1, 2, 0, 2, 3 });
        });

        /// <summary>Desert dunes: Perlin heightfield, flattened inside 'flatRects' (x,z,halfW,halfD).</summary>
        public static Mesh Dunes(float size, int res, float height, Vector4[] flatRects, float tile = 6f)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            for (int z = 0; z <= res; z++)
                for (int x = 0; x <= res; x++)
                {
                    float px = (x / (float)res - 0.5f) * size, pz = (z / (float)res - 0.5f) * size;
                    float h = Mathf.PerlinNoise(px * 0.012f + 17, pz * 0.02f + 5) * height + Mathf.PerlinNoise(px * 0.05f, pz * 0.05f) * height * 0.15f;
                    float flat = 1f;
                    foreach (var r in flatRects)
                    {
                        float dx = Mathf.Max(0, Mathf.Abs(px - r.x) - r.z), dz = Mathf.Max(0, Mathf.Abs(pz - r.y) - r.w);
                        flat = Mathf.Min(flat, Mathf.Clamp01(Mathf.Sqrt(dx * dx + dz * dz) / 25f));
                    }
                    h = h * Mathf.SmoothStep(0, 1, flat) - 0.05f;
                    v.Add(new Vector3(px, h, pz)); uv.Add(new Vector2(px / tile, pz / tile));
                }
            for (int z = 0; z < res; z++)
                for (int x = 0; x < res; x++)
                {
                    int i = z * (res + 1) + x;
                    t.AddRange(new[] { i, i + res + 1, i + 1, i + 1, i + res + 1, i + res + 2 });
                }
            var m = new Mesh { name = "Dunes" };
            return MeshStore.Add(Finish(m, v, null, uv, t));
        }

        /// <summary>Lumpy rock: noise-displaced sphere.</summary>
        public static Mesh Rock(int seed) => MeshStore.Cached("Rock_" + seed, () =>
        {
            var tmp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var src = tmp.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(tmp);
            var v = new List<Vector3>(); src.GetVertices(v);
            var uv = new List<Vector2>(); src.GetUVs(0, uv);
            for (int i = 0; i < v.Count; i++)
            {
                Vector3 p = v[i];
                float n = Mathf.PerlinNoise(p.x * 2.1f + seed, p.z * 2.1f + p.y * 1.3f) * 0.45f + Mathf.PerlinNoise(p.y * 5 + seed, p.x * 5) * 0.12f;
                v[i] = p * (0.75f + n);
                v[i] = new Vector3(v[i].x, v[i].y * 0.7f, v[i].z);
            }
            var tris = new List<int>(src.triangles);
            return Finish(new Mesh(), v, null, uv, tris);
        });

        /// <summary>Drooping palm frond strip from origin along +Z, curving down.</summary>
        public static Mesh Leaf(float length = 3.2f, float width = 1.1f, float droop = 1.6f) => MeshStore.Cached($"Leaf_{length}_{droop}", () =>
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            int segs = 10;
            for (int i = 0; i <= segs; i++)
            {
                float f = i / (float)segs;
                float z = f * length, y = -f * f * droop + f * 0.4f;
                v.Add(new Vector3(-width * 0.5f, y, z)); v.Add(new Vector3(width * 0.5f, y, z));
                uv.Add(new Vector2(0, f)); uv.Add(new Vector2(1, f));
            }
            for (int i = 0; i < segs; i++) { int b = i * 2; t.AddRange(new[] { b, b + 2, b + 1, b + 1, b + 2, b + 3 }); }
            return Finish(new Mesh(), v, null, uv, t);
        });

        /// <summary>Merges child mesh renderers of 'root' by material into new objects; removes the originals' renderers.</summary>
        public static void CombineByMaterial(GameObject root, string name)
        {
            var groups = new Dictionary<Material, List<CombineInstance>>();
            var toKill = new List<GameObject>();
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>())
            {
                var mr = mf.GetComponent<MeshRenderer>();
                if (!mr || !mf.sharedMesh) continue;
                var mat = mr.sharedMaterial;
                if (!groups.TryGetValue(mat, out var list)) groups[mat] = list = new List<CombineInstance>();
                list.Add(new CombineInstance { mesh = mf.sharedMesh, transform = root.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix });
                Object.DestroyImmediate(mr); Object.DestroyImmediate(mf);
            }
            int k = 0;
            foreach (var g in groups)
            {
                var m = new Mesh { name = $"{name}_{k++}", indexFormat = IndexFormat.UInt32 };
                m.CombineMeshes(g.Value.ToArray(), true, true);
                m.RecalculateBounds();
                MeshStore.Add(m);
                var go = new GameObject($"{name}_{g.Key.name}");
                go.transform.SetParent(root.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = m;
                go.AddComponent<MeshRenderer>().sharedMaterial = g.Key;
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            }
        }
    }
}
