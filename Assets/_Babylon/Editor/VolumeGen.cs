using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Babylon.EditorTools
{
    /// <summary>One post-processing profile per scene: bloom, ACES, colour grading, vignette 0.3, light grain.</summary>
    public static class VolumeGen
    {
        public const string Dir = "Assets/_Babylon/Generated/Volumes";

        public static VolumeProfile Create(string name, float bloom, float exposure, float saturation, Color filter, float contrast = 10f)
        {
            Directory.CreateDirectory(Dir);
            string path = $"{Dir}/{name}.asset";
            AssetDatabase.DeleteAsset(path);
            var p = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(p, path);

            var b = p.Add<Bloom>(true);
            b.intensity.Override(bloom); b.threshold.Override(0.9f); b.scatter.Override(0.7f);
            var t = p.Add<Tonemapping>(true);
            t.mode.Override(TonemappingMode.ACES);
            var c = p.Add<ColorAdjustments>(true);
            c.postExposure.Override(exposure); c.saturation.Override(saturation); c.contrast.Override(contrast); c.colorFilter.Override(filter);
            var v = p.Add<Vignette>(true);
            v.intensity.Override(0.3f); v.smoothness.Override(0.45f);
            var g = p.Add<FilmGrain>(true);
            g.type.Override(FilmGrainLookup.Thin1); g.intensity.Override(0.18f);
            var s = p.Add<ShadowsMidtonesHighlights>(true);
            s.shadows.Override(new Vector4(0.95f, 0.95f, 1.05f, 0f));

            foreach (var comp in p.components) { comp.name = comp.GetType().Name; AssetDatabase.AddObjectToAsset(comp, p); }
            EditorUtility.SetDirty(p);
            AssetDatabase.SaveAssets();
            return p;
        }

        public static void GenerateAll()
        {
            Create("Vol_MainMenu", 1.2f, 0.1f, -5f, new Color(0.9f, 0.95f, 1.1f));
            Create("Vol_Temple", 0.8f, 0.0f, 5f, new Color(1.05f, 0.97f, 0.88f));
            Create("Vol_Maze", 1.0f, 0.3f, -18f, new Color(1.08f, 0.95f, 0.82f), 14f);
            Create("Vol_Riddles", 1.3f, 0.2f, -5f, new Color(1.0f, 0.96f, 0.92f));
            Create("Vol_Boss", 2.2f, 0.25f, -10f, new Color(0.88f, 0.95f, 1.1f), 16f);
            Create("Vol_Final", 2.4f, 0.2f, 8f, new Color(1.08f, 0.98f, 0.85f));
        }

        public static VolumeProfile Load(string name) => AssetDatabase.LoadAssetAtPath<VolumeProfile>($"{Dir}/{name}.asset");
    }
}
