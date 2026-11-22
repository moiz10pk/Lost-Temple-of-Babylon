using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Babylon
{
    /// <summary>
    /// The Sword of Marduk (earned in Round 2). LMB slash, hold LMB for a charged spin slash, LMB in mid-air for a plunge,
    /// RMB to block. Weak-point colliders take priority over everything else in reach.
    /// </summary>
    public class SwordWeapon : MonoBehaviour
    {
        public Transform eye;
        public Transform swordPivot;
        public Renderer bladeRenderer;
        public float slashDamage = 25f, spinDamage = 35f, plungeDamage = 45f;
        public float cooldown = 0.45f, reach = 2.7f, chargeTime = 0.55f;

        PlayerMotor motor;
        PlayerVitals vitals;
        float cooldownTimer, holdTimer, swingT = -1f, spinT = -1f, hintTimer;
        bool charging, hitDone, plungePose;
        Quaternion restRot = Quaternion.Euler(8, -12, -4);
        Vector3 restPos = new Vector3(0.38f, -0.36f, 0.62f);
        MaterialPropertyBlock mpb;

        void Awake()
        {
            motor = GetComponent<PlayerMotor>();
            vitals = GetComponent<PlayerVitals>();
            motor.PlungeLanded += OnPlungeLanded;
            mpb = new MaterialPropertyBlock();
        }

        public void Equip()
        {
            GameManager.I.HasSword = true;
            AudioManager.I?.Play2D(SoundId.SwordShing, 1f);
            if (HUD.I) { HUD.I.ShowSword(true); HUD.I.ShowHint("LMB strike  |  hold LMB: spin slash  |  LMB in the air: plunge  |  RMB: block", 6f); }
        }

        void Update()
        {
            bool has = GameManager.I && GameManager.I.HasSword;
            if (swordPivot.gameObject.activeSelf != has) swordPivot.gameObject.SetActive(has);
            hintTimer -= Time.deltaTime;
            cooldownTimer -= Time.deltaTime;
            var mouse = Mouse.current;
            bool blocked = UIState.InputBlocked || vitals.IsDead || mouse == null;

            if (!has)
            {
                if (!blocked && mouse.leftButton.wasPressedThisFrame && hintTimer <= 0)
                {
                    hintTimer = 3f;
                    if (HUD.I) HUD.I.ShowHint("You carry no weapon. Stay silent and avoid the spiders.", 2.5f);
                }
                return;
            }

            vitals.Blocking = !blocked && mouse.rightButton.isPressed && swingT < 0 && spinT < 0;

            if (!blocked && !vitals.Blocking)
            {
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    if (motor.CanPlunge) { motor.StartPlunge(); plungePose = true; AudioManager.I?.PlayAt(SoundId.SwordWhoosh, transform.position, 1f, 0.7f); }
                    else { charging = true; holdTimer = 0; }
                }
                if (charging && mouse.leftButton.isPressed)
                {
                    holdTimer += Time.deltaTime;
                    if (holdTimer >= chargeTime && holdTimer - Time.deltaTime < chargeTime) AudioManager.I?.Play2D(SoundId.SwordShing, 0.35f, 1.4f);
                }
                if (charging && mouse.leftButton.wasReleasedThisFrame)
                {
                    charging = false;
                    if (holdTimer >= chargeTime && vitals.UseStamina(30f)) Spin();
                    else Swing();
                }
            }
            else charging = false;

            Animate();
        }

        void Swing()
        {
            if (cooldownTimer > 0) return;
            cooldownTimer = cooldown; swingT = 0; hitDone = false;
            AudioManager.I?.PlayAt(SoundId.SwordWhoosh, transform.position, 0.9f, Random.Range(0.9f, 1.15f));
            NoiseSystem.Emit(transform.position, 6f);
        }

        void Spin()
        {
            spinT = 0; cooldownTimer = cooldown * 1.5f;
            AudioManager.I?.PlayAt(SoundId.SwordWhoosh, transform.position, 1f, 0.75f);
            NoiseSystem.Emit(transform.position, 8f);
            AreaHit(transform.position + Vector3.up * 1.1f, 3.1f, spinDamage, DamageKind.Spin);
            if (HUD.I) HUD.I.ShowHint("Spin slash!", 0.8f);
        }

        void OnPlungeLanded()
        {
            plungePose = false;
            if (!GameManager.I || !GameManager.I.HasSword) return;
            FX.Spawn("FX_Dust", transform.position, 3f, 1.2f);
            AudioManager.I?.PlayAt(SoundId.GroundSmash, transform.position, 0.7f, 1.5f);
            CameraShake.Shake(0.5f, 0.4f);
            AreaHit(transform.position + Vector3.up * 1.4f, 2.9f, plungeDamage, DamageKind.Plunge);
            if (HUD.I) HUD.I.ShowHint("Plunge strike!", 0.8f);
        }

        void DoSlashHit()
        {
            var hits = Physics.SphereCastAll(eye.position, 0.45f, eye.forward, reach, Layers.WorldAndEnemies, QueryTriggerInteraction.Ignore);
            WeakPoint weak = null; IDamageable target = null; RaycastHit best = default, worldHit = default;
            float bestD = float.MaxValue, worldD = float.MaxValue;
            foreach (var h in hits)
            {
                if (h.collider.transform.IsChildOf(transform)) continue;
                var wp = h.collider.GetComponent<WeakPoint>();
                if (wp && h.collider.CompareTag(Layers.TagWeakPoint)) { weak = wp; best = h; break; }
                var dmg = h.collider.GetComponentInParent<IDamageable>();
                if (dmg != null && !dmg.IsDead && h.distance < bestD) { target = dmg; best = h; bestD = h.distance; }
                else if (dmg == null && h.distance < worldD) { worldHit = h; worldD = h.distance; }
            }
            Vector3 dir = eye.forward;
            if (weak) { weak.Hit(new DamageInfo(slashDamage, DamageKind.Melee, Point(best), dir, gameObject, 0)); return; }
            if (target != null)
            {
                target.TakeDamage(new DamageInfo(slashDamage, DamageKind.Melee, Point(best), dir, gameObject, 6f));
                AudioManager.I?.PlayAt(SoundId.HitThud, Point(best), 0.9f);
                return;
            }
            if (worldD < reach)
            {
                FX.Spawn("FX_Sparks", Point(worldHit), Quaternion.LookRotation(worldHit.normal.sqrMagnitude > 0 ? worldHit.normal : -dir), 1.5f);
                AudioManager.I?.PlayAt(SoundId.StoneClang, Point(worldHit), 0.6f, 1.2f);
                NoiseSystem.Emit(Point(worldHit), 10f);
            }
        }

        Vector3 Point(RaycastHit h) => h.point == Vector3.zero ? eye.position + eye.forward * 1.5f : h.point;

        void AreaHit(Vector3 center, float radius, float damage, DamageKind kind)
        {
            var done = new HashSet<object>();
            foreach (var c in Physics.OverlapSphere(center, radius, Layers.WorldAndEnemies, QueryTriggerInteraction.Ignore))
            {
                if (c.transform.IsChildOf(transform)) continue;
                var wp = c.GetComponent<WeakPoint>();
                if (wp && c.CompareTag(Layers.TagWeakPoint))
                {
                    if (done.Add(wp.owner)) wp.Hit(new DamageInfo(damage, kind, c.transform.position, c.transform.position - center, gameObject));
                    continue;
                }
            }
            foreach (var c in Physics.OverlapSphere(center, radius, Layers.WorldAndEnemies, QueryTriggerInteraction.Ignore))
            {
                if (c.transform.IsChildOf(transform)) continue;
                var dmg = c.GetComponentInParent<IDamageable>();
                var owner = dmg as Health;
                if (dmg == null || dmg.IsDead || done.Contains(owner) || !done.Add(dmg)) continue;
                dmg.TakeDamage(new DamageInfo(damage, kind, c.ClosestPoint(center), c.transform.position - center, gameObject, 9f));
            }
        }

        void Animate()
        {
            float dt = Time.deltaTime;
            Quaternion rot = restRot; Vector3 pos = restPos;
            if (swingT >= 0)
            {
                swingT += dt;
                float p = Mathf.Clamp01(swingT / 0.24f);
                float e = 1f - Mathf.Pow(1f - p, 3f);
                rot = Quaternion.Slerp(Quaternion.Euler(-55, 50, 35), Quaternion.Euler(70, -65, -45), e);
                pos = Vector3.Lerp(new Vector3(0.5f, -0.1f, 0.5f), new Vector3(-0.15f, -0.45f, 0.7f), e);
                if (!hitDone && swingT >= 0.09f) { hitDone = true; DoSlashHit(); }
                if (swingT > 0.42f) swingT = -1f;
                swordPivot.localRotation = rot; swordPivot.localPosition = pos;
            }
            else if (spinT >= 0)
            {
                spinT += dt;
                float p = Mathf.Clamp01(spinT / 0.45f);
                rot = Quaternion.Euler(80, 0, 0) * Quaternion.Euler(0, 0, -p * 360f) * Quaternion.Euler(0, -90, 0);
                pos = new Vector3(0.1f, -0.3f, 0.65f);
                if (spinT > 0.5f) spinT = -1f;
                swordPivot.localRotation = rot; swordPivot.localPosition = pos;
            }
            else
            {
                if (vitals.Blocking) { rot = Quaternion.Euler(0, -75, -82); pos = new Vector3(0.05f, -0.2f, 0.6f); }
                else if (plungePose || motor.IsPlunging) { rot = Quaternion.Euler(95, 0, 0); pos = new Vector3(0.1f, -0.25f, 0.5f); }
                else if (charging && holdTimer > 0.15f)
                {
                    rot = Quaternion.Euler(-30, 60, 20); pos = new Vector3(0.55f, -0.2f, 0.4f) + Random.insideUnitSphere * (holdTimer >= chargeTime ? 0.008f : 0f);
                }
                else
                {
                    float sway = Mathf.Sin(Time.time * 1.6f) * 0.006f;
                    pos += new Vector3(0, sway, 0);
                }
                swordPivot.localRotation = Quaternion.Slerp(swordPivot.localRotation, rot, dt * 14f);
                swordPivot.localPosition = Vector3.Lerp(swordPivot.localPosition, pos, dt * 14f);
            }

            if (bladeRenderer)
            {
                float glow = charging && holdTimer >= chargeTime ? 3f : 0.25f;
                bladeRenderer.GetPropertyBlock(mpb);
                mpb.SetColor("_EmissionColor", new Color(1f, 0.7f, 0.25f) * glow);
                bladeRenderer.SetPropertyBlock(mpb);
            }
        }
    }
}
