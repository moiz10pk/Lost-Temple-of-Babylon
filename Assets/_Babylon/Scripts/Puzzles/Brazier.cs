using UnityEngine;

namespace Babylon
{
    /// <summary>A bronze brazier that can be lit (fire particles + light) when a riddle is answered.</summary>
    public class Brazier : MonoBehaviour
    {
        public GameObject fire;
        public Light fireLight;
        public bool litOnStart;

        void Start() => SetLit(litOnStart, false);

        public void SetLit(bool lit, bool sound = true)
        {
            if (fire) fire.SetActive(lit);
            if (fireLight) fireLight.enabled = lit;
            if (lit && sound) AudioManager.I?.PlayAt(SoundId.TorchCrackle, transform.position, 1f, 1.3f);
        }
    }
}
