using System;
using System.Collections;
using UnityEngine;

namespace Babylon
{
    /// <summary>
    /// The riddle penalty: a giant scorpion bursts from a floor crack in front of the player, scuttles in,
    /// arches its tail and stings, then burrows back into the stone.
    /// </summary>
    public class Scorpion : MonoBehaviour
    {
        public Transform body;
        public Transform[] legs;
        public Transform[] tailSegments;   // base -> tip
        public Transform clawL, clawR;
        public float stingDamage = 25f;

        Quaternion[] tailBase, legBase;
        float gait, tailStrike;

        void Awake()
        {
            tailBase = new Quaternion[tailSegments.Length];
            for (int i = 0; i < tailSegments.Length; i++) tailBase[i] = tailSegments[i].localRotation;
            legBase = new Quaternion[legs.Length];
            for (int i = 0; i < legs.Length; i++) legBase[i] = legs[i].localRotation;
        }

        /// <summary>Plays the whole sting sequence against the player, then destroys itself.</summary>
        public IEnumerator Strike(PlayerVitals player, Action onStung)
        {
            Vector3 pp = player.transform.position;
            Vector3 fwd = player.transform.forward; fwd.y = 0; fwd.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, fwd);
            // Bursts out of the floor to the side (clear of the lectern) and scuttles in front of the player.
            Vector3 start = pp + fwd * 1.6f + right * 4.2f;
            Vector3 end = pp + fwd * 1.0f + right * 1.25f;
            transform.position = start + Vector3.down * 1.2f;
            transform.rotation = Quaternion.LookRotation(Flat(pp - start));
            FX.Spawn("FX_Dust", start, 3f, 0.8f);
            AudioManager.I?.PlayAt(SoundId.DoorGrind, start, 0.5f, 1.8f);
            AudioManager.I?.PlayAt(SoundId.SpiderSkitter, start, 1f, 1.4f);

            // Emerge.
            for (float t = 0; t < 1; t += Time.deltaTime / 0.5f) { transform.position = start + Vector3.down * 1.2f * (1 - t); yield return null; }
            // Scuttle in.
            AudioManager.I?.PlayAt(SoundId.DartWhoosh, start, 0.6f, 0.6f);
            for (float t = 0; t < 1; t += Time.deltaTime / 0.6f)
            {
                transform.position = Vector3.Lerp(start, end, t);
                gait += Time.deltaTime * 30f;
                yield return null;
            }
            // Face the player, arch the tail and strike.
            Quaternion face = Quaternion.LookRotation(Flat(pp - end));
            for (float t = 0; t < 1; t += Time.deltaTime / 0.2f) { transform.rotation = Quaternion.Slerp(transform.rotation, face, t); yield return null; }
            AudioManager.I?.PlayAt(SoundId.SpiderHiss, end, 1f, 1.6f);
            for (float t = 0; t < 1; t += Time.deltaTime / 0.35f) { tailStrike = -t * 0.4f; yield return null; }
            for (float t = 0; t < 1; t += Time.deltaTime / 0.12f) { tailStrike = Mathf.Lerp(-0.4f, 1f, t); yield return null; }
            AudioManager.I?.Play2D(SoundId.ScorpionSting, 1f);
            player.Health.TakeDamage(new DamageInfo(stingDamage, DamageKind.Sting, pp + Vector3.up * 1.2f, fwd, gameObject, 2f));
            onStung?.Invoke();
            yield return new WaitForSeconds(0.5f);
            for (float t = 0; t < 1; t += Time.deltaTime / 0.3f) { tailStrike = 1 - t; yield return null; }
            // Burrow away.
            Vector3 back = end + right * 1.5f;
            for (float t = 0; t < 1; t += Time.deltaTime / 0.7f)
            {
                transform.position = Vector3.Lerp(end, back, t) + Vector3.down * 1.3f * t;
                gait += Time.deltaTime * 30f;
                yield return null;
            }
            Destroy(gameObject);
        }

        static Vector3 Flat(Vector3 v) { v.y = 0; return v.sqrMagnitude < 0.001f ? Vector3.forward : v.normalized; }

        void Update()
        {
            for (int i = 0; i < legs.Length; i++)
                legs[i].localRotation = legBase[i] * Quaternion.Euler(0, Mathf.Sin(gait + i * 1.3f) * 20f, Mathf.Max(0, Mathf.Cos(gait + i * 1.3f)) * 15f);
            for (int i = 0; i < tailSegments.Length; i++)
                tailSegments[i].localRotation = tailBase[i] * Quaternion.Euler(tailStrike * 28f + Mathf.Sin(Time.time * 3 + i) * 3f, 0, 0);
            float pinch = Mathf.Abs(Mathf.Sin(Time.time * 6f)) * 20f;
            if (clawL) clawL.localRotation = Quaternion.Euler(0, -pinch, 0);
            if (clawR) clawR.localRotation = Quaternion.Euler(0, pinch, 0);
        }
    }
}
