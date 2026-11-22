using UnityEngine;

namespace Babylon
{
    /// <summary>Keeps a looping 3D source's volume tied to the SFX setting; 'modulation' lets owners fade it.</summary>
    public class LoopedSound : MonoBehaviour
    {
        public float baseVolume = 0.5f;
        public float modulation = 1f;
        AudioSource src;
        void Awake() => src = GetComponent<AudioSource>();
        void Update() { if (src) src.volume = baseVolume * modulation * Settings.SfxVolume; }
    }
}
