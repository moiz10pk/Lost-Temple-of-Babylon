using System;
using UnityEngine;

namespace Babylon
{
    /// <summary>Sound propagation for the blind spiders. Anything loud calls Emit; listeners decide if they heard it.</summary>
    public static class NoiseSystem
    {
        public static event Action<Vector3, float> OnNoise;

        /// <summary>Most recent loudness made by the player (0..1) for the HUD noise meter.</summary>
        public static float PlayerLoudness;

        public static void Emit(Vector3 position, float radius, bool fromPlayer = true)
        {
            if (fromPlayer) PlayerLoudness = Mathf.Max(PlayerLoudness, Mathf.Clamp01(radius / 14f));
            OnNoise?.Invoke(position, radius);
        }

        /// <summary>Called by the HUD every frame so the meter falls back to zero.</summary>
        public static void Decay(float dt) => PlayerLoudness = Mathf.MoveTowards(PlayerLoudness, 0f, dt * 0.9f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void DomainReset() { OnNoise = null; PlayerLoudness = 0; }
    }
}
