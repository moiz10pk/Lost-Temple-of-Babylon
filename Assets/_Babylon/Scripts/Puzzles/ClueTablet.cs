using UnityEngine;

namespace Babylon
{
    /// <summary>A clay tablet that shows a clue in the HUD message panel and optionally advances the objective.</summary>
    public class ClueTablet : Interactable
    {
        public string title = "A Clay Tablet";
        [TextArea] public string text = "\"Where the lion bows its head, the way below is opened.\"";
        public string objectiveAfter = "Find the lion that bows its head";

        void Reset() => verb = "Read the tablet";

        public override void Interact(Interactor who)
        {
            base.Interact(who);
            AudioManager.I?.PlayAt(SoundId.HitThud, transform.position, 0.3f, 1.6f);
            if (HUD.I)
            {
                HUD.I.ShowMessage(title, text);
                if (!string.IsNullOrEmpty(objectiveAfter)) HUD.I.SetObjective(objectiveAfter);
            }
        }
    }
}
