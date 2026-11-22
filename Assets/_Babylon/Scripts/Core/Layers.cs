using UnityEngine;

namespace Babylon
{
    /// <summary>Layer and tag names created by Babylon/1. Setup Project.</summary>
    public static class Layers
    {
        public const string PlayerName = "Player", EnemyName = "Enemy", InteractableName = "Interactable",
            MinimapName = "MinimapIcon", CeilingName = "Ceiling";
        public const string TagPlayer = "Player", TagPillar = "Pillar", TagWeakPoint = "WeakPoint";

        static int Safe(string n) { int l = LayerMask.NameToLayer(n); return l < 0 ? 0 : l; }

        public static int Player => Safe(PlayerName);
        public static int Enemy => Safe(EnemyName);
        public static int Interactable => Safe(InteractableName);
        public static int Minimap => Safe(MinimapName);
        public static int Ceiling => Safe(CeilingName);

        /// <summary>Solid world geometry plus enemies (used by weapons and line checks).</summary>
        public static int WorldAndEnemies => LayerMask.GetMask("Default", EnemyName, CeilingName, InteractableName);
        public static int World => LayerMask.GetMask("Default", CeilingName);
        public static int EnemyMask => LayerMask.GetMask(EnemyName);

        public static void SetRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform t in go.transform) SetRecursive(t.gameObject, layer);
        }
    }
}
