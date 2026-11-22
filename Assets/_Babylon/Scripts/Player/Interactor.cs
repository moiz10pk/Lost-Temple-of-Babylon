using UnityEngine;
using UnityEngine.InputSystem;

namespace Babylon
{
    /// <summary>Camera raycast for interactables within 3 m; shows the HUD prompt and fires on E.</summary>
    public class Interactor : MonoBehaviour
    {
        public Transform eye;
        public float range = 3f;
        Interactable current;
        /// <summary>Set when a panel closes on E so the same key press doesn't re-trigger an interaction.</summary>
        public static int SuppressUntilFrame;

        void Update()
        {
            current = null;
            if (!UIState.InputBlocked && Time.frameCount > SuppressUntilFrame)
            {
                int mask = LayerMask.GetMask("Default", Layers.InteractableName, Layers.CeilingName);
                if (Physics.SphereCast(eye.position, 0.12f, eye.forward, out var hit, range, mask, QueryTriggerInteraction.Ignore))
                {
                    var it = hit.collider.GetComponentInParent<Interactable>();
                    if (it && it.CanInteract) current = it;
                }
            }
            if (HUD.I) HUD.I.ShowPrompt(current ? current.Prompt : null);
            var kb = Keyboard.current;
            if (current && kb != null && kb.eKey.wasPressedThisFrame) current.Interact(this);
        }
    }
}
