using System.Collections;
using UnityEngine;

namespace Babylon
{
    /// <summary>The prize. "Press E to Collect": floats up, spins, light burst, fanfare, fade, victory screen.</summary>
    public class SumerianTablet : Interactable
    {
        public Light tabletLight;

        void Reset() => verb = "Collect";

        public override string Prompt => "Press E to Collect";

        public override void Interact(Interactor who)
        {
            if (used) return;
            base.Interact(who);
            StartCoroutine(Collect());
        }

        IEnumerator Collect()
        {
            UIState.PushLock();
            var motion = GetComponentInChildren<SimpleMotion>();
            if (motion) motion.enabled = false;
            AudioManager.I?.Play2D(SoundId.VictoryFanfare, 1f);
            if (HUD.I) HUD.I.ShowSubtitle("The Hidden Sumerian Tablet. The first words ever written, held in your hands.", 5f);
            Vector3 start = transform.position;
            for (float t = 0; t < 1f; t += Time.deltaTime / 3f)
            {
                transform.position = start + Vector3.up * Mathf.SmoothStep(0, 1.2f, t);
                transform.Rotate(0, 360f * Time.deltaTime * (0.3f + t * 1.5f), 0, Space.World);
                if (tabletLight) tabletLight.intensity = Mathf.Lerp(3f, 40f, t * t);
                yield return null;
            }
            FX.Spawn("FX_LightBurst", transform.position, 4f);
            AudioManager.I?.Play2D(SoundId.Chime, 1f);
            if (SceneLoader.I) yield return SceneLoader.I.FadeTo(1f, 1.5f);
            foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
            if (tabletLight) tabletLight.enabled = false;
            UIState.PopLock();
            if (VictoryScreen.I) VictoryScreen.I.Show();
            if (SceneLoader.I) yield return SceneLoader.I.FadeTo(0f, 1f);
        }
    }
}
