using System.Collections;
using UnityEngine;

namespace Babylon
{
    /// <summary>Slides a stone slab / gate / door by an offset with a grinding sound and camera shake.</summary>
    public class StoneDoor : MonoBehaviour
    {
        public Vector3 openOffset = new Vector3(0, 4.5f, 0);
        public float duration = 2.5f;
        public bool shake = true;
        public GameObject enableOnOpen;
        public bool IsOpen { get; private set; }

        Vector3 closedPos;

        void Awake()
        {
            closedPos = transform.localPosition;
            if (enableOnOpen) enableOnOpen.SetActive(false);
        }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            StartCoroutine(OpenRoutine());
        }

        IEnumerator OpenRoutine()
        {
            AudioManager.I?.PlayAt(SoundId.DoorGrind, transform.position, 1f, 3f / Mathf.Max(1f, duration + 0.5f), 50f);
            NoiseSystem.Emit(transform.position, 18f, false);
            Vector3 target = closedPos + openOffset;
            for (float t = 0; t < 1f; t += Time.deltaTime / duration)
            {
                transform.localPosition = Vector3.Lerp(closedPos, target, Mathf.SmoothStep(0, 1, t));
                if (shake) CameraShake.ShakeAt(transform.position, 0.3f, 20f, 0.2f);
                yield return null;
            }
            transform.localPosition = target;
            if (enableOnOpen) enableOnOpen.SetActive(true);
        }
    }
}
