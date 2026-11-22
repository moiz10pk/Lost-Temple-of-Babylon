using UnityEngine;

namespace Babylon
{
    /// <summary>Red warning decal on the floor that grows during an attack wind-up.</summary>
    public class TelegraphDecal : MonoBehaviour
    {
        public Renderer rend;
        public Color color = new Color(1f, 0.12f, 0.05f, 0.75f);
        float target = 1f, duration = 1f, t;
        bool showing;
        MaterialPropertyBlock mpb;

        void Awake() { mpb = new MaterialPropertyBlock(); gameObject.SetActive(false); }

        public void Show(Vector3 pos, Quaternion rot, float radius, float growTime)
        {
            transform.SetPositionAndRotation(pos + Vector3.up * 0.04f, rot);
            target = radius; duration = Mathf.Max(0.05f, growTime); t = 0; showing = true;
            gameObject.SetActive(true);
        }

        public void Hide() { showing = false; gameObject.SetActive(false); }

        void Update()
        {
            if (!showing) return;
            t += Time.deltaTime / duration;
            float s = Mathf.Lerp(0.2f, target, Mathf.Clamp01(t));
            transform.localScale = new Vector3(s, 1, s);
            rend.GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColor", new Color(color.r, color.g, color.b, color.a * (0.6f + 0.4f * Mathf.Sin(Time.time * 18f))));
            rend.SetPropertyBlock(mpb);
        }
    }
}
