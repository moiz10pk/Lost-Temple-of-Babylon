using UnityEngine;

namespace Babylon
{
    /// <summary>Locked bronze gate at the maze exit. Needs the golden key; rises when unlocked.</summary>
    public class MazeGate : Interactable
    {
        public StoneDoor door;

        void Reset() => verb = "unlock the bronze gate";

        public override string Prompt => KeyPickup.HasKey ? "Press E to unlock the bronze gate" : "Locked - the gate needs the Golden Key";
        public override bool CanInteract => base.CanInteract && door && !door.IsOpen;

        public override void Interact(Interactor who)
        {
            if (!KeyPickup.HasKey)
            {
                AudioManager.I?.PlayAt(SoundId.StoneClang, transform.position, 0.5f, 1.5f);
                NoiseSystem.Emit(transform.position, 10f);
                if (HUD.I) HUD.I.ShowHint("The gate is locked. Find the Golden Key.", 2f);
                return;
            }
            base.Interact(who);
            AudioManager.I?.Play2D(SoundId.KeyJingle, 0.8f);
            door.Open();
            if (HUD.I) { HUD.I.ShowKey(false); HUD.I.SetObjective("Climb the stairs to the second level"); }
        }
    }
}
