using System.Collections;
using UnityEngine;

namespace Babylon
{
    /// <summary>Rises from the floor after the riddles; holds the Sword of Marduk.</summary>
    public class SwordPedestal : Interactable
    {
        public RiddleMaster master;
        public GameObject swordVisual;
        public Light glow;
        public float riseHeight = 1.6f;
        bool risen;

        void Reset() => verb = "take the Sword of Marduk";

        void Start()
        {
            transform.position -= Vector3.up * riseHeight;
            if (glow) glow.enabled = false;
        }

        public override bool CanInteract => base.CanInteract && risen && !used;

        public void Rise() => StartCoroutine(RiseRoutine());

        IEnumerator RiseRoutine()
        {
            AudioManager.I?.PlayAt(SoundId.DoorGrind, transform.position, 1f, 1.2f);
            if (glow) glow.enabled = true;
            Vector3 a = transform.position, b = a + Vector3.up * riseHeight;
            for (float t = 0; t < 1; t += Time.deltaTime / 2.5f)
            {
                transform.position = Vector3.Lerp(a, b, Mathf.SmoothStep(0, 1, t));
                CameraShake.Shake(0.15f, 0.2f);
                yield return null;
            }
            transform.position = b;
            risen = true;
        }

        public override void Interact(Interactor who)
        {
            base.Interact(who);
            if (swordVisual) swordVisual.SetActive(false);
            if (glow) glow.enabled = false;
            var sword = who.GetComponent<SwordWeapon>();
            if (sword) sword.Equip(); else GameManager.I.HasSword = true;
            if (master) master.OnSwordTaken();
        }
    }
}
