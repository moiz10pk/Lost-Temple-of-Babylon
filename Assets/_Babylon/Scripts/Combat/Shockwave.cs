using UnityEngine;

namespace Babylon
{
    /// <summary>Expanding ground ring from the Ground Smash. Jump over it to avoid damage.</summary>
    public class Shockwave : MonoBehaviour
    {
        public float maxRadius = 13f, speed = 9f, damage = 15f, band = 0.8f;
        public Renderer ringRenderer;
        public GameObject source;
        float radius = 0.5f;
        bool hit;
        MaterialPropertyBlock mpb;

        void Awake() => mpb = new MaterialPropertyBlock();

        void Update()
        {
            radius += speed * Time.deltaTime;
            transform.localScale = new Vector3(radius, 1, radius);
            if (ringRenderer)
            {
                ringRenderer.GetPropertyBlock(mpb);
                mpb.SetColor("_BaseColor", new Color(1f, 0.75f, 0.45f, 0.8f * (1 - radius / maxRadius)));
                ringRenderer.SetPropertyBlock(mpb);
            }
            var p = PlayerVitals.I;
            if (!hit && p && !p.IsDead)
            {
                Vector3 d = p.transform.position - transform.position; d.y = 0;
                if (Mathf.Abs(d.magnitude - radius) < band)
                {
                    hit = true;
                    p.Health.TakeDamage(new DamageInfo(damage, DamageKind.Shockwave, p.transform.position, d.normalized, source, 6f));
                }
            }
            if (radius >= maxRadius) Destroy(gameObject);
        }
    }
}
