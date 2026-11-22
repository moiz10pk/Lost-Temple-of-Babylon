using UnityEngine;

namespace Babylon
{
    /// <summary>The golden key of the maze gate.</summary>
    public class KeyPickup : Interactable
    {
        public static bool HasKey;
        public GameObject mapIcon;

        void Reset() => verb = "take the Golden Key";
        void Awake() => HasKey = false;

        public override void Interact(Interactor who)
        {
            base.Interact(who);
            HasKey = true;
            AudioManager.I?.Play2D(SoundId.KeyJingle, 1f);
            FX.Spawn("FX_Heal", transform.position, 2f);
            if (HUD.I)
            {
                HUD.I.ShowKey(true);
                HUD.I.SetObjective("Reach the exit gate (far corner of the map)");
                HUD.I.ShowSubtitle("The Golden Key of Ishtar. Somewhere, a gate awaits it.", 4f);
            }
            if (mapIcon) mapIcon.SetActive(false);
            gameObject.SetActive(false);
        }
    }
}
