using System;
using System.Collections.Generic;
using UnityEngine;

namespace Babylon
{
    /// <summary>
    /// Registry of generated assets (materials, prefabs, textures, sounds) so runtime code never needs Shader.Find.
    /// Lives at Assets/_Babylon/Resources/BabylonLibrary.asset and is filled by Babylon/2. Generate Assets.
    /// </summary>
    public class BabylonLibrary : ScriptableObject
    {
        [Serializable] public class MatEntry { public string name; public Material material; }
        [Serializable] public class PrefabEntry { public string name; public GameObject prefab; }
        [Serializable] public class TexEntry { public string name; public Texture2D texture; }
        [Serializable] public class SoundEntry { public SoundId id; public AudioClip clip; }
        [Serializable] public class MusicEntry { public MusicId id; public AudioClip clip; }

        public List<MatEntry> materials = new List<MatEntry>();
        public List<PrefabEntry> prefabs = new List<PrefabEntry>();
        public List<TexEntry> textures = new List<TexEntry>();
        public List<SoundEntry> sounds = new List<SoundEntry>();
        public List<MusicEntry> music = new List<MusicEntry>();

        static BabylonLibrary instance;
        public static BabylonLibrary I
        {
            get
            {
                if (instance == null) instance = Resources.Load<BabylonLibrary>("BabylonLibrary");
                return instance;
            }
        }

        Dictionary<string, Material> matMap;
        Dictionary<string, GameObject> prefabMap;
        Dictionary<string, Texture2D> texMap;

        void OnEnable() { matMap = null; prefabMap = null; texMap = null; }

        public Material Mat(string name)
        {
            if (matMap == null) { matMap = new Dictionary<string, Material>(); foreach (var e in materials) if (e.material) matMap[e.name] = e.material; }
            matMap.TryGetValue(name, out var m);
            return m;
        }

        public GameObject Prefab(string name)
        {
            if (prefabMap == null) { prefabMap = new Dictionary<string, GameObject>(); foreach (var e in prefabs) if (e.prefab) prefabMap[e.name] = e.prefab; }
            prefabMap.TryGetValue(name, out var p);
            return p;
        }

        public Texture2D Tex(string name)
        {
            if (texMap == null) { texMap = new Dictionary<string, Texture2D>(); foreach (var e in textures) if (e.texture) texMap[e.name] = e.texture; }
            texMap.TryGetValue(name, out var t);
            return t;
        }

        public AudioClip Sound(SoundId id)
        {
            foreach (var e in sounds) if (e.id == id) return e.clip;
            return null;
        }

        public AudioClip Music(MusicId id)
        {
            foreach (var e in music) if (e.id == id) return e.clip;
            return null;
        }

        // Editor helpers ---------------------------------------------------------------
        public void SetMat(string name, Material m) { materials.RemoveAll(e => e.name == name); materials.Add(new MatEntry { name = name, material = m }); matMap = null; }
        public void SetPrefab(string name, GameObject p) { prefabs.RemoveAll(e => e.name == name); prefabs.Add(new PrefabEntry { name = name, prefab = p }); prefabMap = null; }
        public void SetTex(string name, Texture2D t) { textures.RemoveAll(e => e.name == name); textures.Add(new TexEntry { name = name, texture = t }); texMap = null; }
        public void SetSound(SoundId id, AudioClip c) { sounds.RemoveAll(e => e.id == id); sounds.Add(new SoundEntry { id = id, clip = c }); }
        public void SetMusic(MusicId id, AudioClip c) { music.RemoveAll(e => e.id == id); music.Add(new MusicEntry { id = id, clip = c }); }
    }
}
