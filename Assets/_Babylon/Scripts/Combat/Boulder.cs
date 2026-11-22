using UnityEngine;

namespace Babylon
{
    /// <summary>Ballistic rock thrown by the Stone Master in phase 2. Explodes on impact.</summary>
    public class Boulder : MonoBehaviour
    {
        public float damage = 20f, radius = 2.3f;
        public GameObject source;
        Vector3 velocity;
        bool flying;
        float life;
        static readonly Vector3 G = new Vector3(0, -18f, 0);

        public void Launch(Vector3 target, float flightTime)
        {
            transform.SetParent(null, true);
            Vector3 from = transform.position;
            velocity = (target - from) / flightTime - 0.5f * G * flightTime;
            flying = true;
            AudioManager.I?.PlayAt(SoundId.Throw, from, 1f, 0.8f);
        }

        void Update()
        {
            if (!flying) return;
            float dt = Time.deltaTime;
            life += dt;
            Vector3 step = velocity * dt;
            velocity += G * dt;
            transform.Rotate(new Vector3(300, 120, 0) * dt);
            if (life > 0.1f && Physics.SphereCast(transform.position, 0.4f, step.normalized, out var h, step.magnitude + 0.1f, Layers.World, QueryTriggerInteraction.Ignore))
            { transform.position = h.point; Explode(); return; }
            transform.position += step;
            var p = PlayerVitals.I;
            if (p && Vector3.Distance(p.transform.position + Vector3.up, transform.position) < 1.1f) { Explode(); return; }
            if (life > 5f) Destroy(gameObject);
        }

        void Explode()
        {
            flying = false;
            FX.Spawn("FX_Dust", transform.position, 3f, 1.3f);
            AudioManager.I?.PlayAt(SoundId.GroundSmash, transform.position, 0.8f, 1.3f);
            CameraShake.ShakeAt(transform.position, 0.6f, 15f);
            var p = PlayerVitals.I;
            if (p && !p.IsDead)
            {
                Vector3 d = p.transform.position - transform.position;
                if (d.magnitude < radius)
                    p.Health.TakeDamage(new DamageInfo(damage, DamageKind.Projectile, p.transform.position, d.normalized, source, 7f));
            }
            Destroy(gameObject);
        }
    }
}
