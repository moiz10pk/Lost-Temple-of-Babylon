using UnityEngine;

namespace Babylon
{
    /// <summary>Trauma-based shake applied to the camera's local transform.</summary>
    public class CameraShake : MonoBehaviour
    {
        static CameraShake active;
        float trauma;
        float seed;

        void OnEnable() { active = this; seed = Random.value * 100f; }
        void OnDisable() { if (active == this) active = null; }

        public static void Shake(float amount, float duration = 0.5f)
        {
            if (active == null) return;
            active.trauma = Mathf.Clamp01(Mathf.Max(active.trauma, amount));
            active.decay = 1f / Mathf.Max(0.1f, duration);
        }

        /// <summary>Shake scaled by distance from the camera.</summary>
        public static void ShakeAt(Vector3 pos, float amount, float radius, float duration = 0.6f)
        {
            if (active == null) return;
            float d = Vector3.Distance(active.transform.position, pos);
            if (d < radius) Shake(amount * (1f - d / radius), duration);
        }

        float decay = 2f;

        void LateUpdate()
        {
            trauma = Mathf.MoveTowards(trauma, 0f, decay * Time.unscaledDeltaTime);
            float s = trauma * trauma;
            float t = Time.unscaledTime * 22f;
            Vector3 pos = new Vector3(Mathf.PerlinNoise(seed, t) - 0.5f, Mathf.PerlinNoise(seed + 1, t) - 0.5f, 0f) * 0.35f * s;
            Vector3 rot = new Vector3(Mathf.PerlinNoise(seed + 2, t) - 0.5f, Mathf.PerlinNoise(seed + 3, t) - 0.5f,
                                      Mathf.PerlinNoise(seed + 4, t) - 0.5f) * 9f * s;
            transform.localPosition = pos;
            transform.localRotation = Quaternion.Euler(rot);
        }
    }
}
