using UnityEngine;
using UnityEngine.Rendering.Universal;
using static Babylon.EditorTools.Kit;

namespace Babylon.EditorTools
{
    /// <summary>00_MainMenu: night ziggurat with blue tile bands, torch-lit stairs, lamassu silhouette, drifting camera.</summary>
    public static class MainMenuBuilder
    {
        public static void Build()
        {
            var s = SceneCommon.New("00_MainMenu");
            SceneCommon.Lighting("Sky_Night", new Color(0.12f, 0.14f, 0.24f), new Color(0.08f, 0.08f, 0.12f), new Color(0.03f, 0.03f, 0.04f),
                new Color(0.05f, 0.06f, 0.1f), 0.006f, FogMode.Exponential);
            SceneCommon.Sun(new Vector3(28, -35, 0), new Color(0.55f, 0.64f, 0.9f), 0.45f);
            SceneCommon.Volume("Vol_MainMenu");
            var info = SceneCommon.Info(0, "", "", MusicId.Calm, SoundId.DesertWind, "", "", false, 0.4f);
            info.updateEnvironmentLighting = true;

            var world = new GameObject("World").transform;
            var dunes = MeshGen.Dunes(400f, 120, 8f, new[] { new Vector4(0, 0, 32, 32) });
            Mesh(world, "Dunes", dunes, "Sand", Vector3.zero);
            TempleBuilder.BuildZiggurat(world, false);
            Box(world, "Shrine", new Vector3(0, TempleBuilder.Summit + 3.5f, 0), new Vector3(14, 7, 14), "MudBrick", false, 2f);
            Box(world, "ShrineDoor", new Vector3(0, TempleBuilder.Summit + 2.2f, -7.02f), new Vector3(3, 4.4f, 0.05f), "Obsidian", false, 1f);
            Box(world, "ShrineBand", new Vector3(0, TempleBuilder.Summit + 6.2f, -7.05f), new Vector3(14, 0.9f, 0.1f), "LapisTile", false, 1.2f);
            PointLight(world, "DoorGlow", new Vector3(0, TempleBuilder.Summit + 2, -8), new Color(1f, 0.55f, 0.25f), 3f, 8f);

            var lam = Place("Lamassu", world, new Vector3(-38f, 0, -52f), 200, 3.2f);
            for (int i = 0; i < 4; i++) Place("DatePalm", world, new Vector3(30 + i * 4, 0, -40 - i * 5), i * 70, 1.1f);
            for (int i = 0; i < 3; i++) Place("DatePalm", world, new Vector3(-26 - i * 5, 0, -20 + i * 6), i * 50, 1f);
            for (int i = 0; i < 8; i++) Place("Torch", world, new Vector3((i % 2 == 0 ? -1 : 1) * 4.6f, 0, -45f - (i / 2) * 7f));

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>(); cam.fieldOfView = 50f; cam.farClipPlane = 800f;
            cam.cullingMask = ~LayerMask.GetMask(Layers.MinimapName);
            camGo.AddComponent<AudioListener>();
            var d = camGo.AddComponent<UniversalAdditionalCameraData>(); d.renderPostProcessing = true; d.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            var drift = camGo.AddComponent<CameraDrift>(); drift.target = new Vector3(0, 13, 0); drift.radius = 78f; drift.height = 6f;
            camGo.transform.position = new Vector3(-30, 14, -70); camGo.transform.LookAt(new Vector3(0, 12, 0));

            Place("UI_MainMenu", null, Vector3.zero);
            SceneCommon.EventSystem();
            SceneCommon.Save(s, "00_MainMenu");
        }
    }
}
