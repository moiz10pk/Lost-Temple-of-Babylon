using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Babylon.EditorTools
{
    /// <summary>Babylon/1. Setup Project: layers, tags, Forward+ renderers, player settings.</summary>
    public static class ProjectSetup
    {
        static readonly (string name, int slot)[] LayerSlots =
        {
            (Layers.PlayerName, 8), (Layers.EnemyName, 9), (Layers.InteractableName, 10), (Layers.MinimapName, 11), (Layers.CeilingName, 12)
        };

        public static void Run()
        {
            var tm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var tags = tm.FindProperty("tags");
            foreach (var tag in new[] { Layers.TagPillar, Layers.TagWeakPoint })
            {
                bool found = false;
                for (int i = 0; i < tags.arraySize; i++) if (tags.GetArrayElementAtIndex(i).stringValue == tag) found = true;
                if (!found) { tags.InsertArrayElementAtIndex(tags.arraySize); tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag; }
            }
            var layers = tm.FindProperty("layers");
            foreach (var (name, slot) in LayerSlots)
            {
                bool exists = false;
                for (int i = 0; i < layers.arraySize; i++) if (layers.GetArrayElementAtIndex(i).stringValue == name) exists = true;
                if (exists) continue;
                int target = slot;
                if (!string.IsNullOrEmpty(layers.GetArrayElementAtIndex(target).stringValue))
                    for (target = 8; target < 32 && !string.IsNullOrEmpty(layers.GetArrayElementAtIndex(target).stringValue); target++) { }
                if (target < 32) layers.GetArrayElementAtIndex(target).stringValue = name;
            }
            tm.ApplyModifiedPropertiesWithoutUndo();

            // Forward+ on every URP renderer (many torches).
            foreach (var guid in AssetDatabase.FindAssets("t:UniversalRendererData", new[] { "Assets" }))
            {
                var rd = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(AssetDatabase.GUIDToAssetPath(guid));
                if (rd && rd.renderingMode != RenderingMode.ForwardPlus) { rd.renderingMode = RenderingMode.ForwardPlus; EditorUtility.SetDirty(rd); }
            }

            PlayerSettings.companyName = "Babylon Studio";
            PlayerSettings.productName = "The Lost Temple of Babylon";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            AssetDatabase.SaveAssets();
            Debug.Log("[Babylon] Project setup complete: layers, tags, Forward+.");
        }
    }
}
