using UnityEngine;

namespace Babylon
{
    /// <summary>Procedural 8-leg gait (alternating tetrapod), body sway, rear-up and death curl for the spider visual.</summary>
    public class SpiderRig : MonoBehaviour
    {
        public Transform body;
        public Transform[] hips;
        public Transform[] knees;

        [HideInInspector] public float speed;     // current planar speed
        [HideInInspector] public float rear;      // 0..1 rear-up amount
        [HideInInspector] public bool dead;

        Quaternion[] hipBase, kneeBase;
        Vector3 bodyBase;
        float phase, deadT;

        void Awake()
        {
            hipBase = new Quaternion[hips.Length]; kneeBase = new Quaternion[knees.Length];
            for (int i = 0; i < hips.Length; i++) hipBase[i] = hips[i].localRotation;
            for (int i = 0; i < knees.Length; i++) kneeBase[i] = knees[i].localRotation;
            bodyBase = body.localPosition;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dead)
            {
                deadT = Mathf.MoveTowards(deadT, 1, dt * 3f);
                body.localRotation = Quaternion.Slerp(body.localRotation, Quaternion.Euler(0, 0, 180), deadT);
                body.localPosition = bodyBase + Vector3.up * 0.25f * deadT;
                for (int i = 0; i < hips.Length; i++) hips[i].localRotation = hipBase[i] * Quaternion.Euler(-30 * deadT, 0, 0);
                for (int i = 0; i < knees.Length; i++) knees[i].localRotation = kneeBase[i] * Quaternion.Euler(45 * deadT, 0, 0);
                return;
            }
            float amt = Mathf.Clamp01(speed / 3f);
            phase += dt * (3f + speed * 4.5f);
            for (int i = 0; i < hips.Length; i++)
            {
                bool groupA = (i % 2 == 0) ^ (i >= 4);
                float p = phase + (groupA ? 0 : Mathf.PI) + i * 0.15f;
                float swing = Mathf.Sin(p) * 22f * amt;
                float lift = Mathf.Max(0, Mathf.Cos(p)) * 18f * amt + Mathf.Sin(Time.time * 1.3f + i) * 1.5f;
                float frontRaise = i % 4 == 0 ? rear * 55f : 0f;  // front legs lift when rearing
                hips[i].localRotation = Quaternion.Euler(0, swing, 0) * hipBase[i] * Quaternion.Euler(-(lift + frontRaise), 0, 0);
            }
            float bob = Mathf.Abs(Mathf.Sin(phase * 2f)) * 0.04f * amt;
            body.localPosition = bodyBase + Vector3.up * (bob + rear * 0.35f);
            body.localRotation = Quaternion.Euler(-rear * 32f, 0, Mathf.Sin(phase) * 2f * amt);
        }
    }
}
