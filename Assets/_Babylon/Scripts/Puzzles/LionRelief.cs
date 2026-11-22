using UnityEngine;

namespace Babylon
{
    /// <summary>One of four lion wall reliefs. The one with its head bowed (and faint glowing cuneiform) opens the way below.</summary>
    public class LionRelief : Interactable
    {
        public bool isCorrect;
        public StoneDoor slab;
        public string objectiveAfter = "Descend the hidden stairs";

        void Reset() => verb = "Press the lion relief";

        public override bool CanInteract => base.CanInteract && !(slab && slab.IsOpen);

        public override void Interact(Interactor who)
        {
            if (!isCorrect)
            {
                AudioManager.I?.PlayAt(SoundId.HitThud, transform.position, 0.5f, 0.8f);
                if (HUD.I) HUD.I.ShowHint("The lion stares ahead, proud and unmoved.", 2f);
                return;
            }
            base.Interact(who);
            CameraShake.Shake(0.6f, 2f);
            if (slab) slab.Open();
            if (HUD.I)
            {
                HUD.I.ShowSubtitle("Stone grinds against stone... a slab in the floor slides aside.", 4f);
                HUD.I.SetObjective(objectiveAfter);
            }
        }
    }
}
