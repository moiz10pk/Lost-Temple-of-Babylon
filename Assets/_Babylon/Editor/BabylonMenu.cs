using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Babylon.EditorTools
{
    /// <summary>Editor menu: everything is generated from here (idempotent).</summary>
    public static class BabylonMenu
    {
        [MenuItem("Babylon/1. Setup Project", priority = 1)]
        public static void Setup() => ProjectSetup.Run();

        [MenuItem("Babylon/2. Generate Assets", priority = 2)]
        public static void Generate()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            ProjectSetup.Run();
            AssetGenerator.GenerateAll();
        }

        [MenuItem("Babylon/3. Build Scene/00 Main Menu", priority = 10)] public static void B0() => One(MainMenuBuilder.Build);
        [MenuItem("Babylon/3. Build Scene/01 Temple", priority = 11)] public static void B1() => One(TempleBuilder.Build);
        [MenuItem("Babylon/3. Build Scene/02 Maze", priority = 12)] public static void B2() => One(MazeBuilder.Build);
        [MenuItem("Babylon/3. Build Scene/03 Riddles", priority = 13)] public static void B3() => One(RiddleBuilder.Build);
        [MenuItem("Babylon/3. Build Scene/04 Boss", priority = 14)] public static void B4() => One(BossBuilder.Build);
        [MenuItem("Babylon/3. Build Scene/05 Final Chamber", priority = 15)] public static void B5() => One(FinalBuilder.Build);

        static void One(System.Action build)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureAssets();
            build();
            UpdateBuildSettings();
        }

        [MenuItem("Babylon/4. Build ALL Scenes + Build Settings", priority = 30)]
        public static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildEverything(true);
            EditorSceneManager.OpenScene($"{SceneCommon.SceneDir}/00_MainMenu.unity");
        }

        [MenuItem("Babylon/5. Build Windows Player", priority = 40)]
        public static void BuildPlayer()
        {
            UpdateBuildSettings();
            var opts = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Select(s => s.path).ToArray(),
                locationPathName = "Builds/Windows/LostTempleOfBabylon.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log($"[Babylon] Windows build: {report.summary.result}, {report.summary.totalSize / (1024 * 1024)} MB, {report.summary.totalErrors} errors");
            if (report.summary.result == BuildResult.Succeeded && !Application.isBatchMode) EditorUtility.RevealInFinder(opts.locationPathName);
        }

        /// <summary>Batch entry point: Unity.exe -batchmode -executeMethod Babylon.EditorTools.BabylonMenu.BuildAllBatch -quit</summary>
        public static void BuildAllBatch()
        {
            BuildEverything(true);
            Debug.Log("[Babylon] BuildAllBatch finished.");
        }

        /// <summary>Batch entry point that also produces the Windows player.</summary>
        public static void BuildPlayerBatch()
        {
            BuildEverything(true);
            BuildPlayer();
        }

        static void BuildEverything(bool regenerateAssets)
        {
            ProjectSetup.Run();
            if (regenerateAssets) AssetGenerator.GenerateAll();
            MainMenuBuilder.Build();
            TempleBuilder.Build();
            MazeBuilder.Build();
            RiddleBuilder.Build();
            BossBuilder.Build();
            FinalBuilder.Build();
            UpdateBuildSettings();
            AssetDatabase.SaveAssets();
        }

        static void EnsureAssets()
        {
            if (Kit.Lib.Prefab("Player") == null || Kit.Lib.Mat("Sandstone") == null) { ProjectSetup.Run(); AssetGenerator.GenerateAll(); }
        }

        static void UpdateBuildSettings()
        {
            EditorBuildSettings.scenes = GameManager.SceneNames
                .Select(n => $"{SceneCommon.SceneDir}/{n}.unity")
                .Where(p => System.IO.File.Exists(p))
                .Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
        }
    }
}
