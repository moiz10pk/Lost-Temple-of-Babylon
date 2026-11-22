namespace Babylon
{
    /// <summary>The stone lectern that poses the riddles.</summary>
    public class RiddleTablet : Interactable
    {
        public RiddleMaster master;

        void Reset() => verb = "read the riddle";

        public override string Prompt => "Press E to answer the riddles";
        public override bool CanInteract => base.CanInteract && master && master.CanAsk;
        public override void Interact(Interactor who) => master.Ask();
    }
}
