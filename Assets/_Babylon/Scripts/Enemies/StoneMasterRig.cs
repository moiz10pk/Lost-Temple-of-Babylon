using UnityEngine;

namespace Babylon
{
    public enum BossPose { Idol, Idle, Walk, Raise, Slam, SweepWind, SweepFollow, ChargeWind, Charge, Kneel, ThrowWind, ThrowRelease, Roar }

    /// <summary>Procedural pose blending for the Stone Master's stone-block skeleton (no Animator needed).</summary>
    public class StoneMasterRig : MonoBehaviour
    {
        public Transform hips, spine, head, shoulderL, elbowL, shoulderR, elbowR, legL, kneeL, legR, kneeR;
        public float shinLength = 1.1f;

        [HideInInspector] public BossPose pose = BossPose.Idol;
        [HideInInspector] public float blend = 6f;
        [HideInInspector] public float moveSpeed;
        [HideInInspector] public float wobble;   // dizzy head wobble when stunned

        struct P
        {
            public Vector3 sp, hd, sL, eL, sR, eR, lL, kL, lR, kR; public float drop;
        }

        Vector3 hipsBase;
        float walkPhase;
        public System.Action Footstep;

        void Awake() => hipsBase = hips.localPosition;

        public void Set(BossPose p, float blendSpeed) { pose = p; blend = blendSpeed; }

        P Get(BossPose p)
        {
            var k = new P();
            Vector3 legIdle = new Vector3(-8, 0, 0), kneeIdle = new Vector3(12, 0, 0);
            k.lL = legIdle; k.kL = kneeIdle; k.lR = legIdle; k.kR = kneeIdle; k.drop = 0.12f;
            switch (p)
            {
                case BossPose.Idol:
                case BossPose.Kneel:
                    k.drop = shinLength; k.lL = new Vector3(-90, 0, 0); k.kL = new Vector3(90, 0, 0); k.lR = Vector3.zero; k.kR = new Vector3(90, 0, 0);
                    if (p == BossPose.Idol) { k.sp = new Vector3(12, 0, 0); k.hd = new Vector3(22, 0, 0); k.sR = new Vector3(-35, 0, 8); k.eR = new Vector3(-55, 0, 0); k.sL = new Vector3(-30, 0, -10); k.eL = new Vector3(-80, 0, 0); }
                    else { k.sp = new Vector3(28, 0, 0); k.hd = new Vector3(30, 0, 0); k.sR = new Vector3(-15, 0, 25); k.eR = new Vector3(-20, 0, 0); k.sL = new Vector3(-15, 0, -25); k.eL = new Vector3(-20, 0, 0); }
                    break;
                case BossPose.Idle:
                case BossPose.Walk:
                    k.sp = new Vector3(8, 0, 0); k.sR = new Vector3(-25, 0, 10); k.eR = new Vector3(-50, 0, 0); k.sL = new Vector3(-10, 0, -12); k.eL = new Vector3(-30, 0, 0);
                    break;
                case BossPose.Raise:
                    k.sp = new Vector3(-12, 0, 0); k.hd = new Vector3(-10, 0, 0); k.sR = new Vector3(-165, 0, 8); k.eR = new Vector3(-25, 0, 0); k.sL = new Vector3(-165, 0, -8); k.eL = new Vector3(-25, 0, 0);
                    k.lL = new Vector3(-15, 0, -8); k.kL = new Vector3(25, 0, 0); k.lR = new Vector3(10, 0, 8); k.kR = new Vector3(20, 0, 0); k.drop = 0.25f;
                    break;
                case BossPose.Slam:
                    k.sp = new Vector3(40, 0, 0); k.hd = new Vector3(-10, 0, 0); k.sR = new Vector3(-75, 0, 5); k.eR = new Vector3(-5, 0, 0); k.sL = new Vector3(-75, 0, -5); k.eL = new Vector3(-5, 0, 0);
                    k.lL = new Vector3(-35, 0, -8); k.kL = new Vector3(55, 0, 0); k.lR = new Vector3(15, 0, 8); k.kR = new Vector3(40, 0, 0); k.drop = 0.6f;
                    break;
                case BossPose.SweepWind:
                    k.sp = new Vector3(5, 55, 0); k.sR = new Vector3(-80, 0, 75); k.eR = new Vector3(-15, 0, 0); k.sL = new Vector3(-40, 0, -20); k.eL = new Vector3(-60, 0, 0);
                    k.lL = new Vector3(-20, 0, -10); k.kL = new Vector3(30, 0, 0); k.lR = new Vector3(10, 0, 10); k.kR = new Vector3(25, 0, 0); k.drop = 0.3f;
                    break;
                case BossPose.SweepFollow:
                    k.sp = new Vector3(10, -65, 0); k.sR = new Vector3(-85, 0, -30); k.eR = new Vector3(-10, 0, 0); k.sL = new Vector3(-20, 0, -40); k.eL = new Vector3(-30, 0, 0);
                    k.lL = new Vector3(-20, 0, -10); k.kL = new Vector3(30, 0, 0); k.lR = new Vector3(10, 0, 10); k.kR = new Vector3(25, 0, 0); k.drop = 0.3f;
                    break;
                case BossPose.ChargeWind:
                case BossPose.Roar:
                    k.sp = new Vector3(-20, 0, 0); k.hd = new Vector3(-32, 0, 0); k.sR = new Vector3(-55, 0, 65); k.eR = new Vector3(-35, 0, 0); k.sL = new Vector3(-55, 0, -65); k.eL = new Vector3(-35, 0, 0);
                    k.drop = 0.1f;
                    break;
                case BossPose.Charge:
                    k.sp = new Vector3(35, 0, 0); k.hd = new Vector3(-25, 0, 0); k.sR = new Vector3(-100, 0, 10); k.eR = new Vector3(-10, 0, 0); k.sL = new Vector3(20, 0, -15); k.eL = new Vector3(-30, 0, 0);
                    k.drop = 0.3f;
                    break;
                case BossPose.ThrowWind:
                    k.sp = new Vector3(-15, 30, 0); k.sR = new Vector3(-170, 0, 30); k.eR = new Vector3(-70, 0, 0); k.sL = new Vector3(-60, 0, -30); k.eL = new Vector3(-20, 0, 0); k.drop = 0.2f;
                    break;
                case BossPose.ThrowRelease:
                    k.sp = new Vector3(30, -30, 0); k.sR = new Vector3(-60, 0, 10); k.eR = new Vector3(-5, 0, 0); k.sL = new Vector3(-10, 0, -20); k.eL = new Vector3(-30, 0, 0); k.drop = 0.3f;
                    break;
            }
            return k;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            var k = Get(pose);
            if (pose == BossPose.Walk || pose == BossPose.Charge)
            {
                float rate = pose == BossPose.Charge ? 1.6f : 1.15f;
                float prev = walkPhase;
                walkPhase += dt * moveSpeed * rate;
                float s = Mathf.Sin(walkPhase), amp = pose == BossPose.Charge ? 38f : 26f;
                k.lL += new Vector3(-s * amp, 0, 0); k.lR += new Vector3(s * amp, 0, 0);
                k.kL += new Vector3(Mathf.Max(0, Mathf.Cos(walkPhase)) * 35f, 0, 0);
                k.kR += new Vector3(Mathf.Max(0, -Mathf.Cos(walkPhase)) * 35f, 0, 0);
                k.sL += new Vector3(s * 14f, 0, 0);
                k.sp += new Vector3(0, s * 6f, Mathf.Cos(walkPhase) * 3f);
                k.drop += Mathf.Abs(Mathf.Cos(walkPhase)) * 0.12f;
                if (Mathf.FloorToInt(prev / Mathf.PI) != Mathf.FloorToInt(walkPhase / Mathf.PI)) Footstep?.Invoke();
            }
            k.hd += new Vector3(Mathf.Sin(Time.time * 7f) * wobble * 12f, Mathf.Sin(Time.time * 4.3f) * wobble * 18f, 0);
            // Idle breathing so it never looks frozen.
            k.sp += new Vector3(Mathf.Sin(Time.time * 1.1f) * 1.5f, 0, 0);

            float t = 1f - Mathf.Exp(-blend * dt);
            Blend(spine, k.sp, t); Blend(head, k.hd, t);
            Blend(shoulderL, k.sL, t); Blend(elbowL, k.eL, t); Blend(shoulderR, k.sR, t); Blend(elbowR, k.eR, t);
            Blend(legL, k.lL, t); Blend(kneeL, k.kL, t); Blend(legR, k.lR, t); Blend(kneeR, k.kR, t);
            hips.localPosition = Vector3.Lerp(hips.localPosition, hipsBase - Vector3.up * k.drop, t);
        }

        static void Blend(Transform j, Vector3 euler, float t)
        {
            if (j) j.localRotation = Quaternion.Slerp(j.localRotation, Quaternion.Euler(euler), t);
        }

        /// <summary>Snaps straight to a pose (used when the scene starts with the idol kneeling).</summary>
        public void Snap(BossPose p)
        {
            pose = p; var k = Get(p);
            spine.localRotation = Quaternion.Euler(k.sp); head.localRotation = Quaternion.Euler(k.hd);
            shoulderL.localRotation = Quaternion.Euler(k.sL); elbowL.localRotation = Quaternion.Euler(k.eL);
            shoulderR.localRotation = Quaternion.Euler(k.sR); elbowR.localRotation = Quaternion.Euler(k.eR);
            legL.localRotation = Quaternion.Euler(k.lL); kneeL.localRotation = Quaternion.Euler(k.kL);
            legR.localRotation = Quaternion.Euler(k.lR); kneeR.localRotation = Quaternion.Euler(k.kR);
            hips.localPosition = hipsBase - Vector3.up * k.drop;
        }
    }
}
