using UnityEngine;

namespace Babylon
{
    /// <summary>The glowing cyan rune wheel on the riddle hall's back wall: one segment per solved riddle.</summary>
    public class RuneWheel : MonoBehaviour
    {
        public Renderer[] segments;
        public Material offMaterial, onMaterial;
        public Light wheelLight;
        float spin;

        public void SetLit(int count)
        {
            for (int i = 0; i < segments.Length; i++)
                segments[i].sharedMaterial = i < count ? onMaterial : offMaterial;
            if (wheelLight) wheelLight.intensity = 0.5f + count * 1.2f;
        }

        public void Spin() => spin = 360f;

        void Update()
        {
            if (spin <= 0) return;
            float s = Mathf.Min(spin, 180f * Time.deltaTime);
            transform.Rotate(0, s, 0, Space.Self);
            spin -= s;
        }
    }
}
