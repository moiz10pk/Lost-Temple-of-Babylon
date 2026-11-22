using System.IO;
using UnityEditor;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEngine;

namespace Babylon.EditorTools
{
    /// <summary>Creates every material as a .mat asset (URP Lit / Unlit / Particles). Shader.Find is editor-only here.</summary>
    public static class MaterialGen
    {
        public const string Dir = "Assets/_Babylon/Generated/Materials";

        static Texture2D T(string n) => string.IsNullOrEmpty(n) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureGen.Dir}/{n}.png");
        static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }

        static Material Get(string name, string shader)
        {
            Directory.CreateDirectory(Dir);
            string path = $"{Dir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            var sh = Shader.Find(shader);
            if (m == null) { m = new Material(sh) { name = name }; AssetDatabase.CreateAsset(m, path); }
            else m.shader = sh;
            return m;
        }

        public static Material Lit(BabylonLibrary lib, string name, string tex, Color tint, float smooth, float metal = 0f,
            Color? emission = null, string emissionTex = null, bool forceEmissionKeyword = false, float normalScale = 1f, string normalTex = null)
        {
            var m = Get(name, "Universal Render Pipeline/Lit");
            var albedo = T(tex);
            m.SetTexture("_BaseMap", albedo);
            m.SetColor("_BaseColor", tint);
            var nrm = T(normalTex ?? (tex != null ? tex + "_N" : null));
            m.SetTexture("_BumpMap", nrm);
            m.SetFloat("_BumpScale", normalScale);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", metal);
            m.SetFloat("_Surface", 0); m.SetFloat("_AlphaClip", 0); m.SetFloat("_Cull", 2);
            Color e = emission ?? Color.black;
            m.SetColor("_EmissionColor", e);
            m.SetTexture("_EmissionMap", T(emissionTex));
            BaseShaderGUI.SetMaterialKeywords(m, LitGUI.SetMaterialKeywords);
            if (e.maxColorComponent > 0.001f || forceEmissionKeyword)
            {
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            EditorUtility.SetDirty(m);
            lib.SetMat(name, m);
            return m;
        }

        static Material Foliage(BabylonLibrary lib, string name, string tex)
        {
            var m = Get(name, "Universal Render Pipeline/Lit");
            m.SetTexture("_BaseMap", T(tex)); m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Smoothness", 0.2f); m.SetFloat("_Metallic", 0);
            m.SetFloat("_AlphaClip", 1); m.SetFloat("_Cutoff", 0.45f); m.SetFloat("_Cull", 0);
            BaseShaderGUI.SetMaterialKeywords(m, LitGUI.SetMaterialKeywords);
            EditorUtility.SetDirty(m); lib.SetMat(name, m);
            return m;
        }

        static Material Unlit(BabylonLibrary lib, string name, Color c, string tex = null, bool transparent = false)
        {
            var m = Get(name, "Universal Render Pipeline/Unlit");
            m.SetColor("_BaseColor", c); m.SetTexture("_BaseMap", T(tex));
            m.SetFloat("_Surface", transparent ? 1 : 0); m.SetFloat("_Blend", 0); m.SetFloat("_Cull", transparent ? 0 : 2);
            BaseShaderGUI.SetMaterialKeywords(m);
            EditorUtility.SetDirty(m); lib.SetMat(name, m);
            return m;
        }

        static Material Particle(BabylonLibrary lib, string name, Color c, bool additive)
        {
            var m = Get(name, "Universal Render Pipeline/Particles/Unlit");
            m.SetTexture("_BaseMap", T("Particle_Soft")); m.SetColor("_BaseColor", c);
            m.SetFloat("_Surface", 1); m.SetFloat("_Blend", additive ? 2 : 0); m.SetFloat("_Cull", 0);
            BaseShaderGUI.SetMaterialKeywords(m, null, ParticleGUI.SetMaterialKeywords);
            EditorUtility.SetDirty(m); lib.SetMat(name, m);
            return m;
        }

        static Material Sky(BabylonLibrary lib, string name, Color tint, Color ground, float atmo, float exposure, float sunSize)
        {
            var m = Get(name, "Skybox/Procedural");
            m.SetColor("_SkyTint", tint); m.SetColor("_GroundColor", ground);
            m.SetFloat("_AtmosphereThickness", atmo); m.SetFloat("_Exposure", exposure); m.SetFloat("_SunSize", sunSize);
            m.SetFloat("_SunSizeConvergence", 4f);
            EditorUtility.SetDirty(m); lib.SetMat(name, m);
            return m;
        }

        public static void GenerateAll(BabylonLibrary lib)
        {
            var white = Color.white;
            Lit(lib, "Sandstone", "Sandstone", white, 0.15f);
            Lit(lib, "SandstoneDark", "Sandstone", new Color(0.62f, 0.55f, 0.48f), 0.12f);
            Lit(lib, "MudBrick", "MudBrick", white, 0.1f, 0, null, null, false, 1.2f);
            Lit(lib, "CarvedWall", "CarvedWall", white, 0.15f, 0, null, null, false, 1.3f);
            Lit(lib, "LapisTile", "LapisTile", white, 0.78f, 0.05f);
            Lit(lib, "LapisStars", "LapisStars", white, 0.6f, 0.05f);
            Lit(lib, "CrackedFloor", "CrackedFloor", white, 0.28f);
            Lit(lib, "Sand", "Sand", white, 0.05f, 0, null, null, false, 0.7f);
            Lit(lib, "CaveRock", "CaveRock", white, 0.22f, 0, null, null, false, 1.4f);
            Lit(lib, "MossRock", "MossRock", white, 0.18f, 0, null, null, false, 1.3f);
            Lit(lib, "GolemStone", "GolemStone", white, 0.16f, 0, null, null, false, 1.5f);
            Lit(lib, "Wood", "Wood", white, 0.3f);
            Lit(lib, "PalmTrunk", "Wood", new Color(0.75f, 0.6f, 0.45f), 0.15f, 0, null, null, false, 2f);
            Lit(lib, "Clay", "Clay", white, 0.22f);
            Lit(lib, "ClayGlow", "Clay", white, 0.3f, 0, Hex("#F2C14E") * 2.5f, "CuneiformGlow");
            Lit(lib, "CuneiformFaint", "CarvedWall", white, 0.15f, 0, new Color(0.25f, 0.82f, 1f) * 0.8f, "CuneiformGlow");
            Lit(lib, "Bronze", "Bronze", white, 0.62f, 0.85f);
            Lit(lib, "BronzeBlade", "Bronze", new Color(1f, 0.85f, 0.7f), 0.8f, 0.95f, null, null, true);
            Lit(lib, "Gold", null, Hex("#D4A537"), 0.78f, 1f);
            Lit(lib, "GoldBright", null, Hex("#F2C14E"), 0.82f, 1f, Hex("#F2C14E") * 0.25f);
            Lit(lib, "Obsidian", null, new Color(0.05f, 0.05f, 0.06f), 0.92f, 0.3f);
            Lit(lib, "Linen", "Sand", Hex("#CDBB95"), 0.08f, 0, null, null, false, 0.5f);
            Lit(lib, "Leather", "Wood", Hex("#7A5236"), 0.3f);
            Lit(lib, "Skin", null, Hex("#A8714C"), 0.35f);
            Lit(lib, "LapisPlain", null, Hex("#234E9C"), 0.85f, 0.1f);
            Lit(lib, "SpiderBody", null, Hex("#1E1813"), 0.5f, 0.1f, null, null, false, 1.5f, "CaveRock_N");
            Lit(lib, "SpiderLeg", null, Hex("#2A2017"), 0.4f, 0f, null, null, false, 1f, "CaveRock_N");
            Lit(lib, "SpiderEye", null, new Color(0.15f, 0.02f, 0.02f), 0.95f, 0, new Color(1f, 0.08f, 0.04f) * 1.6f);
            Lit(lib, "ScorpionShell", null, Hex("#3A2414"), 0.65f, 0.1f, null, null, false, 1f, "CaveRock_N");
            Lit(lib, "Venom", null, Hex("#7CFF3A"), 0.9f, 0, Hex("#7CFF3A") * 2f);
            Lit(lib, "GolemEye", null, new Color(0.08f, 0.03f, 0.02f), 0.9f, 0, null, null, true);
            Lit(lib, "GolemRune", null, new Color(0.1f, 0.2f, 0.24f), 0.5f, 0, null, null, true);
            Lit(lib, "GolemCrystal", null, Hex("#8C6CFF"), 0.92f, 0.1f, Hex("#8C6CFF") * 1.4f);
            Lit(lib, "GolemCore", null, Hex("#3FD0FF"), 0.9f, 0, null, null, true);
            Lit(lib, "Flame", null, Hex("#FF8A2A"), 0f, 0, new Color(1f, 0.5f, 0.15f) * 5f);
            Lit(lib, "HealGlow", null, Hex("#2AFFC0"), 0.9f, 0, new Color(0.15f, 1f, 0.7f) * 2.5f);
            Lit(lib, "RuneOff", null, new Color(0.08f, 0.16f, 0.2f), 0.6f, 0, new Color(0.05f, 0.2f, 0.26f) * 0.6f);
            Lit(lib, "RuneOn", null, Hex("#3FD0FF"), 0.8f, 0, new Color(0.25f, 0.82f, 1f) * 6f);
            Lit(lib, "Iron", "CaveRock", new Color(0.35f, 0.33f, 0.32f), 0.5f, 0.8f);
            Foliage(lib, "PalmLeaf", "PalmLeaf");

            Unlit(lib, "MapWall", Hex("#C9A46A"));
            Unlit(lib, "MapFloor", Hex("#1A120B"));
            Unlit(lib, "MapPlayer", Hex("#F2C14E"));
            Unlit(lib, "MapKey", new Color(1f, 0.9f, 0.3f));
            Unlit(lib, "MapExit", new Color(0.3f, 1f, 0.4f));
            Unlit(lib, "MapSpider", new Color(1f, 0.15f, 0.12f));
            Unlit(lib, "DecalRed", new Color(1f, 0.1f, 0.05f, 0.7f), "Decal_Ring", true);
            Unlit(lib, "ShockRing", new Color(1f, 0.75f, 0.45f, 0.8f), "Decal_Ring", true);

            Particle(lib, "P_Fire", new Color(1f, 0.55f, 0.2f, 1f), true);
            Particle(lib, "P_Smoke", new Color(0.2f, 0.18f, 0.16f, 0.5f), false);
            Particle(lib, "P_Dust", new Color(0.72f, 0.6f, 0.43f, 0.55f), false);
            Particle(lib, "P_Spark", new Color(1f, 0.8f, 0.4f, 1f), true);
            Particle(lib, "P_Blood", new Color(0.12f, 0.2f, 0.05f, 0.9f), false);
            Particle(lib, "P_Heal", new Color(0.3f, 1f, 0.7f, 1f), true);
            Particle(lib, "P_Mote", new Color(1f, 0.82f, 0.45f, 0.8f), true);
            Particle(lib, "P_Core", new Color(0.3f, 0.85f, 1f, 1f), true);

            Sky(lib, "Sky_Sunset", new Color(0.95f, 0.55f, 0.38f), new Color(0.42f, 0.33f, 0.28f), 1.7f, 1.25f, 0.06f);
            Sky(lib, "Sky_Night", new Color(0.18f, 0.22f, 0.45f), new Color(0.05f, 0.05f, 0.07f), 0.5f, 0.18f, 0.02f);
        }
    }
}
