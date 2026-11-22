using UnityEngine;

namespace Babylon
{
    /// <summary>The chest weak point: stone plates slide apart to reveal a pulsing core that can be struck.</summary>
    public class StoneMasterCore : MonoBehaviour
    {
        public Transform plateL, plateR;
        public Renderer coreRenderer;
        public Light coreLight;
        public Collider coreCollider;
        public float openDistance = 0.45f;
        public Color coreColor = new Color(0.25f, 0.82f, 1f);

        public bool Exposed { get; private set; }
        Vector3 lClosed, rClosed;
        float open, fade = 1f;
        MaterialPropertyBlock mpb;

        void Awake()
        {
            lClosed = plateL.localPosition; rClosed = plateR.localPosition;
            mpb = new MaterialPropertyBlock();
            coreCollider.enabled = false;
        }

        public void SetExposed(bool v)
        {
            if (v == Exposed) return;
            Exposed = v;
            coreCollider.enabled = v;
            AudioManager.I?.PlayAt(SoundId.DoorGrind, transform.position, 0.35f, 2.2f);
        }

        /// <summary>Death: let the core burn out.</summary>
        public void FadeOut() { Exposed = false; coreCollider.enabled = false; fade = 0.999f; }

        void Update()
        {
            open = Mathf.MoveTowards(open, Exposed ? 1 : 0, Time.deltaTime * 4f);
            plateL.localPosition = lClosed + Vector3.left * openDistance * open;
            plateR.localPosition = rClosed + Vector3.right * openDistance * open;
            if (fade < 1f) fade = Mathf.MoveTowards(fade, 0f, Time.deltaTime * 0.5f);
            float pulse = (Exposed ? 4f + Mathf.Sin(Time.time * 10f) * 1.5f : 0.6f) * fade;
            coreRenderer.GetPropertyBlock(mpb);
            mpb.SetColor("_EmissionColor", coreColor * pulse);
            coreRenderer.SetPropertyBlock(mpb);
            if (coreLight) coreLight.intensity = Mathf.Lerp(coreLight.intensity, (Exposed ? 6f : 0.4f) * fade, Time.deltaTime * 6f);
        }
    }
}
