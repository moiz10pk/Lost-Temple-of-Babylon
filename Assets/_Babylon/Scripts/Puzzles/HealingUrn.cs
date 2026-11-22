using UnityEngine;

namespace Babylon
{
    /// <summary>Glowing urn that restores 30 HP once.</summary>
    public class HealingUrn : Interactable
    {
        public float amount = 30f;
        public Light glowLight;
        public Renderer glowRenderer;

        void Reset() => verb = "drink from the healing urn";

        public override bool CanInteract => base.CanInteract && !used;

        public override void Interact(Interactor who)
        {
            var v = PlayerVitals.I;
            if (v && v.Health.Current >= v.Health.maxHealth) { if (HUD.I) HUD.I.ShowHint("You are already at full health.", 1.5f); return; }
            base.Interact(who);
            if (v) v.Health.Heal(amount);
            AudioManager.I?.Play2D(SoundId.HealShimmer, 1f);
            FX.Spawn("FX_Heal", transform.position + Vector3.up * 0.8f, 3f);
            if (HUD.I) { HUD.I.Flash(new Color(0.3f, 1f, 0.6f), 0.3f); HUD.I.ShowHint("+30 health", 1.5f); }
            if (glowLight) glowLight.enabled = false;
            if (glowRenderer) glowRenderer.enabled = false;
        }
    }
}
