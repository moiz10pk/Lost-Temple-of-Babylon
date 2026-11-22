using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Babylon
{
    /// <summary>Persistent music + SFX playback with volumes from Settings. Clips come from the library, else are synthesized.</summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager I { get; private set; }

        AudioSource musicA, musicB, ambient, ui;
        readonly List<AudioSource> pool = new List<AudioSource>();
        readonly Dictionary<SoundId, AudioClip> cache = new Dictionary<SoundId, AudioClip>();
        readonly Dictionary<MusicId, AudioClip> musicCache = new Dictionary<MusicId, AudioClip>();
        MusicId currentMusic = MusicId.None;
        float ambientBase = 0.5f;
        Coroutine musicFade;

        void Awake()
        {
            I = this;
            musicA = MakeSource("MusicA", true); musicB = MakeSource("MusicB", true);
            ambient = MakeSource("Ambient", true);
            ui = MakeSource("UI", false);
            musicA.ignoreListenerPause = musicB.ignoreListenerPause = ui.ignoreListenerPause = true;
            for (int i = 0; i < 20; i++)
            {
                var s = MakeSource("Sfx" + i, false);
                s.spatialBlend = 1f; s.rolloffMode = AudioRolloffMode.Linear; s.dopplerLevel = 0;
                pool.Add(s);
            }
            Settings.Changed += ApplyVolumes;
        }

        void OnDestroy() => Settings.Changed -= ApplyVolumes;

        AudioSource MakeSource(string n, bool loop)
        {
            var go = new GameObject(n);
            go.transform.SetParent(transform);
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false; s.loop = loop; s.spatialBlend = 0;
            return s;
        }

        void ApplyVolumes()
        {
            ambient.volume = ambientBase * Settings.SfxVolume;
        }

        public AudioClip Get(SoundId id)
        {
            if (id == SoundId.None) return null;
            if (cache.TryGetValue(id, out var c) && c) return c;
            var lib = BabylonLibrary.I;
            c = lib ? lib.Sound(id) : null;
            if (!c) c = ProceduralAudio.CreateClip(id);
            cache[id] = c;
            return c;
        }

        AudioClip GetMusic(MusicId id)
        {
            if (musicCache.TryGetValue(id, out var c) && c) return c;
            var lib = BabylonLibrary.I;
            c = lib ? lib.Music(id) : null;
            if (!c) c = ProceduralMusic.CreateClip(id);
            musicCache[id] = c;
            return c;
        }

        public void Play2D(SoundId id, float volume = 1f, float pitch = 1f)
        {
            var c = Get(id);
            if (!c) return;
            ui.pitch = pitch;
            ui.PlayOneShot(c, volume * Settings.SfxVolume);
        }

        public AudioSource PlayAt(SoundId id, Vector3 pos, float volume = 1f, float pitch = 1f, float maxDistance = 40f)
        {
            var c = Get(id);
            if (!c) return null;
            AudioSource s = null;
            foreach (var p in pool) if (!p.isPlaying) { s = p; break; }
            if (s == null) s = pool[Random.Range(0, pool.Count)];
            s.transform.position = pos;
            s.clip = c; s.pitch = pitch; s.volume = volume * Settings.SfxVolume;
            s.minDistance = 1.5f; s.maxDistance = maxDistance;
            s.Play();
            return s;
        }

        /// <summary>Creates a looping 3D source on a transform (torches, spider skitter). Volume follows the SFX setting.</summary>
        public LoopedSound CreateLoop(SoundId id, Transform parent, float volume, float maxDistance)
        {
            var go = new GameObject("Loop_" + id);
            go.transform.SetParent(parent, false);
            var s = go.AddComponent<AudioSource>();
            s.clip = Get(id); s.loop = true; s.spatialBlend = 1f; s.rolloffMode = AudioRolloffMode.Linear;
            s.minDistance = 1f; s.maxDistance = maxDistance; s.dopplerLevel = 0; s.playOnAwake = false;
            s.time = s.clip ? Random.Range(0f, s.clip.length * 0.9f) : 0f;
            var l = go.AddComponent<LoopedSound>();
            l.baseVolume = volume;
            s.Play();
            return l;
        }

        public void PlayAmbient(SoundId id, float volume)
        {
            ambientBase = volume;
            if (id == SoundId.None) { ambient.Stop(); return; }
            var c = Get(id);
            if (ambient.clip == c && ambient.isPlaying) { ApplyVolumes(); return; }
            ambient.clip = c; ApplyVolumes(); ambient.Play();
        }

        public void PlayMusic(MusicId id, float fade = 2.5f)
        {
            if (id == currentMusic && (musicA.isPlaying || musicB.isPlaying)) return;
            currentMusic = id;
            if (musicFade != null) StopCoroutine(musicFade);
            musicFade = StartCoroutine(Crossfade(id == MusicId.None ? null : GetMusic(id), fade));
        }

        IEnumerator Crossfade(AudioClip clip, float fade)
        {
            var from = musicA.isPlaying ? musicA : musicB;
            var to = from == musicA ? musicB : musicA;
            if (clip) { to.clip = clip; to.volume = 0; to.Play(); }
            float fromStart = from.volume;
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / fade)
            {
                from.volume = fromStart * (1 - t);
                if (clip) to.volume = t * Settings.MusicVolume * 0.6f;
                yield return null;
            }
            from.Stop();
            musicFade = null;
        }

        void Update()
        {
            // Keep music in sync with the slider even mid-song.
            if (musicFade == null)
            {
                var active = musicA.isPlaying ? musicA : musicB;
                active.volume = Settings.MusicVolume * 0.6f;
            }
        }
    }
}

