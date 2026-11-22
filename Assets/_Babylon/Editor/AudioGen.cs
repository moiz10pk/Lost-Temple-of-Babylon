using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Babylon.EditorTools
{
    /// <summary>
    /// Renders every procedural sound and music loop to WAV under Generated/Audio. If a file with the same name exists in
    /// Audio/Override (wav/ogg/mp3), that clip is registered in the library instead.
    /// </summary>
    public static class AudioGen
    {
        public const string Dir = "Assets/_Babylon/Generated/Audio";
        public const string OverrideDir = "Assets/_Babylon/Audio/Override";

        public static void GenerateAll(BabylonLibrary lib)
        {
            Directory.CreateDirectory(Dir);
            Directory.CreateDirectory(OverrideDir);
            foreach (SoundId id in Enum.GetValues(typeof(SoundId)))
            {
                if (id == SoundId.None) continue;
                string name = id.ToString();
                var clip = Override(name) ?? Write(name, ProceduralAudio.Render(id), ProceduralAudio.Rate, false);
                lib.SetSound(id, clip);
            }
            foreach (MusicId id in Enum.GetValues(typeof(MusicId)))
            {
                if (id == MusicId.None) continue;
                string name = "Music_" + id;
                var clip = Override(name) ?? Write(name, ProceduralMusic.Render(id), ProceduralMusic.Rate, true);
                lib.SetMusic(id, clip);
            }
        }

        static AudioClip Override(string name)
        {
            foreach (var ext in new[] { ".wav", ".ogg", ".mp3" })
            {
                var c = AssetDatabase.LoadAssetAtPath<AudioClip>($"{OverrideDir}/{name}{ext}");
                if (c) return c;
            }
            return null;
        }

        static AudioClip Write(string name, float[] data, int rate, bool music)
        {
            string path = $"{Dir}/{name}.wav";
            using (var fs = new FileStream(path, FileMode.Create))
            using (var w = new BinaryWriter(fs))
            {
                int bytes = data.Length * 2;
                w.Write(new[] { 'R', 'I', 'F', 'F' }); w.Write(36 + bytes);
                w.Write(new[] { 'W', 'A', 'V', 'E' }); w.Write(new[] { 'f', 'm', 't', ' ' });
                w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(rate); w.Write(rate * 2); w.Write((short)2); w.Write((short)16);
                w.Write(new[] { 'd', 'a', 't', 'a' }); w.Write(bytes);
                foreach (var s in data) w.Write((short)(Mathf.Clamp(s, -1f, 1f) * 32767f));
            }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var imp = (AudioImporter)AssetImporter.GetAtPath(path);
            var st = imp.defaultSampleSettings;
            st.loadType = music ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            st.compressionFormat = AudioCompressionFormat.Vorbis;
            st.quality = music ? 0.6f : 0.8f;
            imp.defaultSampleSettings = st;
            imp.loadInBackground = music;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
    }
}
