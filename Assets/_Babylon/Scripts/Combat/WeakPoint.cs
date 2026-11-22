using UnityEngine;

namespace Babylon
{
    /// <summary>A glowing core collider (tag WeakPoint). Only enabled while exposed; forwards hits to its owner as Core damage.</summary>
    public class WeakPoint : MonoBehaviour
    {
        public Health owner;
        public float multiplier = 1f;

        public void Hit(DamageInfo d)
        {
            if (!owner) return;
            d.kind = DamageKind.Core;
            d.amount *= multiplier;
            owner.TakeDamage(d);
            FX.Spawn("FX_CoreHit", transform.position, 2f);
            AudioManager.I?.PlayAt(SoundId.HitThud, transform.position, 1f, 0.7f);
            CameraShake.Shake(0.25f, 0.25f);
        }
    }
}
