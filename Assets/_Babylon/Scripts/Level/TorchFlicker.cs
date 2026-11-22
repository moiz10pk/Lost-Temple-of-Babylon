using UnityEngine;

namespace Babylon
{
    /// <summary>Flickers a torch light and its emissive flame; registers with the light culler.</summary>
    public class TorchFlicker : MonoBehaviour
    {
        public Light torchLight;
        public Transform flame;
        public float baseIntensity = 2.2f;
        public float amount = 0.35f;
        public bool crackleSound = true;
        float seed;
        Vector3 flameScale;

        void OnEnable()
        {
            seed = Random.value * 50f;
            if (flame) flameScale = flame.localScale;
            if (torchLight) TorchLightCuller.Register(torchLight);
        }

        void Start()
        {
            if (crackleSound && AudioManager.I) AudioManager.I.CreateLoop(SoundId.TorchCrackle, transform, 0.25f, 9f);
        }

        void OnDisable() { if (torchLight) TorchLightCuller.Unregister(torchLight); }

        void Update()
        {
            float t = Time.time * 9f;
            float n = Mathf.PerlinNoise(seed, t) * 0.7f + Mathf.PerlinNoise(seed + 3, t * 2.3f) * 0.3f;
            if (torchLight) torchLight.intensity = baseIntensity * (1f - amount + amount * 2f * n);
            if (flame) flame.localScale = flameScale * (0.85f + 0.3f * n);
        }
    }
}
