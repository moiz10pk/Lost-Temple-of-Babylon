using UnityEngine;

namespace Babylon
{
    /// <summary>Spawns one-shot particle prefabs from the library and cleans them up.</summary>
    public static class FX
    {
        public static GameObject Spawn(string prefabName, Vector3 pos, Quaternion rot, float life = 4f, float scale = 1f)
        {
            var lib = BabylonLibrary.I;
            var prefab = lib ? lib.Prefab(prefabName) : null;
            if (prefab == null) return null;
            var go = Object.Instantiate(prefab, pos, rot);
            if (!Mathf.Approximately(scale, 1f)) go.transform.localScale *= scale;
            if (life > 0) Object.Destroy(go, life);
            return go;
        }

        public static GameObject Spawn(string prefabName, Vector3 pos, float life = 4f, float scale = 1f)
            => Spawn(prefabName, pos, Quaternion.identity, life, scale);
    }
}
