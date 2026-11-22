using UnityEditor;
using UnityEngine;

namespace Babylon.EditorTools
{
    /// <summary>Small construction helpers used by every prefab and scene builder.</summary>
    public static class Kit
    {
        public const string Root = "Assets/_Babylon";
        public const string LibPath = Root + "/Resources/BabylonLibrary.asset";

        static BabylonLibrary lib;
        public static BabylonLibrary Lib
        {
            get
            {
                if (lib == null) lib = AssetDatabase.LoadAssetAtPath<BabylonLibrary>(LibPath);
                if (lib == null)
                {
                    System.IO.Directory.CreateDirectory(Root + "/Resources");
                    lib = ScriptableObject.CreateInstance<BabylonLibrary>();
                    AssetDatabase.CreateAsset(lib, LibPath);
                }
                return lib;
            }
        }

        public static Material M(string name)
        {
            var m = Lib.Mat(name);
            if (m == null) Debug.LogWarning($"[Babylon] Missing material '{name}'. Run Babylon/2. Generate Assets.");
            return m;
        }

        public static GameObject Node(string name, Transform parent, Vector3 pos, Vector3? euler = null)
        {
            var go = new GameObject(name);
            if (parent) go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler ?? Vector3.zero);
            return go;
        }

        public static GameObject Mesh(Transform parent, string name, Mesh mesh, string mat, Vector3 pos, Vector3? euler = null, Vector3? scale = null)
        {
            var go = Node(name, parent, pos, euler);
            go.transform.localScale = scale ?? Vector3.one;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = M(mat);
            return go;
        }

        /// <summary>Box with world-scaled UVs; optional BoxCollider.</summary>
        public static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 size, string mat, bool collider = true, float tile = 2f, Vector3? euler = null)
        {
            var go = Mesh(parent, name, MeshGen.Box(size, tile), mat, pos, euler);
            if (collider) go.AddComponent<BoxCollider>().size = size;
            return go;
        }

        public static GameObject Cyl(Transform parent, string name, Vector3 pos, float radius, float height, string mat, bool collider = false, Vector3? euler = null, int seg = 20, float topRadius = -1, float tile = 2f)
        {
            var go = Mesh(parent, name, MeshGen.Cylinder(radius, height, seg, tile, topRadius), mat, pos, euler);
            if (collider) { var c = go.AddComponent<CapsuleCollider>(); c.radius = Mathf.Max(radius, topRadius); c.height = height + c.radius * 2; }
            return go;
        }

        /// <summary>Unity primitive with its collider removed (for character/prop visuals).</summary>
        public static GameObject Prim(PrimitiveType type, Transform parent, string name, Vector3 pos, Vector3 scale, string mat, Vector3? euler = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos; go.transform.localScale = scale;
            go.transform.localRotation = Quaternion.Euler(euler ?? Vector3.zero);
            go.GetComponent<MeshRenderer>().sharedMaterial = M(mat);
            return go;
        }

        public static Light PointLight(Transform parent, string name, Vector3 pos, Color color, float intensity, float range, bool shadows = false)
        {
            var go = Node(name, parent, pos);
            var l = go.AddComponent<Light>();
            l.type = LightType.Point; l.color = color; l.intensity = intensity; l.range = range;
            l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            return l;
        }

        public static GameObject Place(string prefab, Transform parent, Vector3 pos, float yaw = 0f, float scale = 1f)
        {
            var p = Lib.Prefab(prefab);
            if (p == null) { Debug.LogWarning($"[Babylon] Missing prefab '{prefab}'"); return Node(prefab + "_MISSING", parent, pos); }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(p, parent);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            go.transform.localScale = Vector3.one * scale;
            return go;
        }

        public static BoxCollider Trigger(Transform parent, string name, Vector3 pos, Vector3 size)
        {
            var go = Node(name, parent, pos);
            go.layer = 2; // Ignore Raycast
            var c = go.AddComponent<BoxCollider>();
            c.isTrigger = true; c.size = size;
            return c;
        }

        public static BoxCollider Invisible(Transform parent, string name, Vector3 pos, Vector3 size, Vector3? euler = null)
        {
            var go = Node(name, parent, pos, euler);
            var c = go.AddComponent<BoxCollider>(); c.size = size;
            return c;
        }

        /// <summary>
        /// Solid staircase rising along 'dir' from 'start' (ground level) by 'rise' over 'run', plus an invisible ramp
        /// collider so the CharacterController glides up instead of stepping.
        /// </summary>
        public static GameObject Stairs(Transform parent, string name, Vector3 start, Vector3 dir, float rise, float run, float width, string mat, float baseDepth = 0f)
        {
            var root = Node(name, parent, start, new Vector3(0, Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg, 0));
            int n = Mathf.Max(2, Mathf.CeilToInt(rise / 0.28f));
            float h = rise / n, d = run / n;
            for (int i = 0; i < n; i++)
            {
                float top = h * (i + 1);
                Box(root.transform, "Step" + i, new Vector3(0, (top - baseDepth) * 0.5f, d * (i + 0.5f)), new Vector3(width, top + baseDepth, d), mat, false, 1.5f);
            }
            // Ramp: line through every step nosing, from (-d, 0) to (run - d, rise).
            Vector3 a = new Vector3(0, 0, -d), b = new Vector3(0, rise, run - d);
            Vector3 mid = (a + b) * 0.5f; float len = (b - a).magnitude + 0.6f;
            float ang = Mathf.Atan2(rise, run) * Mathf.Rad2Deg;
            var ramp = Invisible(root.transform, "Ramp", mid, new Vector3(width, 0.3f, len), new Vector3(-ang, 0, 0));
            ramp.center = new Vector3(0, -0.15f, 0);
            // Flat landing collider on the top step.
            Invisible(root.transform, "TopStep", new Vector3(0, rise - 0.15f, run - d * 0.5f), new Vector3(width, 0.3f, d));
            MeshGen.CombineByMaterial(root, name);
            return root;
        }

        public static void Static(GameObject go)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>())
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
        }

        public static void SetLayer(GameObject go, int layer) => Layers.SetRecursive(go, layer);
    }
}
