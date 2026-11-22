using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using static Babylon.EditorTools.Kit;

namespace Babylon.EditorTools
{
    /// <summary>Shared scene plumbing: new/save, player, UI, lighting, fog, post-processing, scene info.</summary>
    public static class SceneCommon
    {
        public const string SceneDir = "Assets/_Babylon/Scenes";

        public static Scene New(string name)
        {
            var s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            MeshStore.Begin(name + "_Meshes");
            return s;
        }

        public static void Save(Scene s, string name)
        {
            MeshStore.End();
            System.IO.Directory.CreateDirectory(SceneDir);
            EditorSceneManager.SaveScene(s, $"{SceneDir}/{name}.unity");
            Debug.Log($"[Babylon] Built scene {name}");
        }

        public static GameObject Player(Vector3 pos, float yaw, bool handTorch)
        {
            var p = Place("Player", null, pos, yaw);
            var torch = p.transform.Find("CameraRig/BobPivot/Main Camera/HandTorch");
            if (torch) torch.gameObject.SetActive(handTorch);
            return p;
        }

        public static HUD UI(bool riddle = false, bool victory = false)
        {
            var ui = Place("UI_Gameplay", null, Vector3.zero);
            if (riddle) Place("UI_Riddle", null, Vector3.zero);
            if (victory) Place("UI_Victory", null, Vector3.zero);
            EventSystem();
            return ui.GetComponentInChildren<HUD>(true);
        }

        public static void EventSystem()
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        public static SceneInfo Info(int index, string title, string caption, MusicId music, SoundId ambient, string objective, string story, bool grantSword = false, float ambientVol = 0.5f)
        {
            var go = new GameObject("SceneInfo");
            var i = go.AddComponent<SceneInfo>();
            i.sceneIndex = index; i.title = title; i.caption = caption; i.music = music; i.ambient = ambient; i.ambientVolume = ambientVol;
            i.objective = objective; i.openingStory = story; i.grantSword = grantSword;
            go.AddComponent<TorchLightCuller>();
            return i;
        }

        public static void Lighting(string skybox, Color sky, Color equator, Color ground, Color fog, float fogDensity, FogMode mode = FogMode.ExponentialSquared)
        {
            RenderSettings.skybox = skybox != null ? M(skybox) : null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = sky; RenderSettings.ambientEquatorColor = equator; RenderSettings.ambientGroundColor = ground;
            RenderSettings.fog = true; RenderSettings.fogMode = mode; RenderSettings.fogColor = fog; RenderSettings.fogDensity = fogDensity;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.reflectionIntensity = 0.6f;
            Lightmapping.lightingSettings = LightingAsset();
        }

        static LightingSettings LightingAsset()
        {
            const string path = "Assets/_Babylon/Generated/RealtimeLighting.lighting";
            var ls = AssetDatabase.LoadAssetAtPath<LightingSettings>(path);
            if (ls == null)
            {
                ls = new LightingSettings { name = "RealtimeLighting", bakedGI = false, realtimeGI = false };
                AssetDatabase.CreateAsset(ls, path);
            }
            return ls;
        }

        public static Light Sun(Vector3 euler, Color color, float intensity, bool shadows = true)
        {
            var go = new GameObject("Sun");
            go.transform.rotation = Quaternion.Euler(euler);
            var l = go.AddComponent<Light>();
            l.type = LightType.Directional; l.color = color; l.intensity = intensity;
            l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            l.shadowStrength = 0.85f;
            RenderSettings.sun = l;
            return l;
        }

        public static void Volume(string profile)
        {
            var go = new GameObject("PostProcessVolume");
            var v = go.AddComponent<Volume>();
            v.isGlobal = true; v.priority = 1;
            v.sharedProfile = VolumeGen.Load(profile);
        }

        public static void ReflectionProbe(Vector3 pos, Vector3 size)
        {
            var go = new GameObject("ReflectionProbe");
            go.transform.position = pos;
            var p = go.AddComponent<ReflectionProbe>();
            p.mode = ReflectionProbeMode.Realtime;
            p.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            p.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
            p.size = size; p.resolution = 128; p.intensity = 0.8f;
        }

        public static void SetMinimap(HUD hud, Texture tex)
        {
            hud.minimapTexture = tex;
            PrefabUtility.RecordPrefabInstancePropertyModifications(hud);
        }
    }
}
