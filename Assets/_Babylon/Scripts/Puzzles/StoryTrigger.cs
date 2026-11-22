using UnityEngine;

namespace Babylon
{
    /// <summary>One-shot trigger that shows a story subtitle and/or sets the objective.</summary>
    [RequireComponent(typeof(Collider))]
    public class StoryTrigger : MonoBehaviour
    {
        [TextArea] public string subtitle;
        public float seconds = 5f;
        public string objective;
        bool fired;

        void OnTriggerEnter(Collider other)
        {
            if (fired || !other.CompareTag(Layers.TagPlayer)) return;
            fired = true;
            if (!HUD.I) return;
            if (!string.IsNullOrEmpty(subtitle)) HUD.I.ShowSubtitle(subtitle, seconds);
            if (!string.IsNullOrEmpty(objective)) HUD.I.SetObjective(objective);
        }
    }
}
