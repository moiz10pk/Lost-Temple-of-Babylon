using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Babylon.EditorTools
{
    /// <summary>Babylon/2. Generate Assets: textures, materials, audio, volumes, render texture, prefabs, UI prefabs.</summary>
    public static class AssetGenerator
    {
        public const string RTPath = "Assets/_Babylon/Generated/RenderTextures/Minimap.renderTexture";

        public static void GenerateAll()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            foreach (var d in new[] { "Scripts", "Editor", "Generated/Textures", "Generated/Materials", "Generated/Prefabs", "Generated/RenderTextures",
                                      "Generated/Volumes", "Generated/Audio", "Generated/Meshes", "Audio/Override", "Scenes", "Resources" })
                System.IO.Directory.CreateDirectory($"{Kit.Root}/{d}");
            AssetDatabase.Refresh();

            var lib = Kit.Lib;
            Step("Textures", 0.05f); TextureGen.GenerateAll();
            lib.SetTex("Icon_Key", AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureGen.Dir}/Icon_Key.png"));
            lib.SetTex("Icon_Sword", AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureGen.Dir}/Icon_Sword.png"));
            Step("Materials", 0.3f); MaterialGen.GenerateAll(lib);
            Step("Audio", 0.4f); AudioGen.GenerateAll(lib);
            Step("Post-processing", 0.6f); VolumeGen.GenerateAll();
            Step("Render texture", 0.65f); MinimapTexture();
            EditorUtility.SetDirty(lib); AssetDatabase.SaveAssets();

            Step("Prefabs", 0.7f);
            MeshStore.Begin("PrefabMeshes");
            PropPrefabs.BuildAll();
            CharacterPrefabs.BuildAll();
            MeshStore.End();

            Step("UI prefabs", 0.9f); UIPrefabs();
            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar();
            Debug.Log("[Babylon] Assets generated: textures, materials, sounds, music, volumes, prefabs and UI.");
        }

        static void Step(string what, float p) => EditorUtility.DisplayProgressBar("Babylon: Generate Assets", what, p);

        public static RenderTexture MinimapTexture()
        {
            var rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(RTPath);
            if (rt == null)
            {
                rt = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32) { name = "Minimap", antiAliasing = 2 };
                AssetDatabase.CreateAsset(rt, RTPath);
            }
            return rt;
        }

        /// <summary>Saves the code-built UI so it can be inspected and reused as prefabs.</summary>
        static void UIPrefabs()
        {
            var root = new GameObject("UI_Gameplay");
            new GameObject("HUD").transform.SetParent(root.transform);
            root.transform.Find("HUD").gameObject.AddComponent<HUD>().Build();
            var pause = new GameObject("PauseMenu"); pause.transform.SetParent(root.transform); pause.AddComponent<PauseMenu>().Build();
            var over = new GameObject("GameOver"); over.transform.SetParent(root.transform); over.AddComponent<GameOverScreen>().Build();
            PropPrefabs.Save(root, "UI_Gameplay");

            var r = new GameObject("UI_Riddle"); r.AddComponent<RiddlePanel>().Build(); PropPrefabs.Save(r, "UI_Riddle");
            var v = new GameObject("UI_Victory"); v.AddComponent<VictoryScreen>().Build(); PropPrefabs.Save(v, "UI_Victory");
            var m = new GameObject("UI_MainMenu"); m.AddComponent<MainMenu>().Build(); PropPrefabs.Save(m, "UI_MainMenu");
        }
    }
}
