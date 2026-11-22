using UnityEngine;
using UnityEngine.Events;

namespace Babylon
{
    /// <summary>Base for anything the player can use with E. Lives on the Interactable layer with a collider.</summary>
    public class Interactable : MonoBehaviour
    {
        public string verb = "Interact";
        public bool singleUse;
        public UnityEvent onInteract = new UnityEvent();
        protected bool used;

        public virtual string Prompt => $"Press E to {verb}";
        public virtual bool CanInteract => isActiveAndEnabled && !(singleUse && used);

        public virtual void Interact(Interactor who)
        {
            used = true;
            onInteract?.Invoke();
        }
    }
}
