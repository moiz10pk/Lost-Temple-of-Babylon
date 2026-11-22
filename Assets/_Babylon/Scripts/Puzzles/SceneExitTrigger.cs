using UnityEngine;

namespace Babylon
{
    /// <summary>Loads the next scene when the player walks in.</summary>
    [RequireComponent(typeof(Collider))]
    public class SceneExitTrigger : MonoBehaviour
    {
        public string sceneName;
        bool fired;

        void OnTriggerEnter(Collider other)
        {
            if (fired || !other.CompareTag(Layers.TagPlayer)) return;
            fired = true;
            SceneLoader.I.Load(sceneName);
        }
    }
}
