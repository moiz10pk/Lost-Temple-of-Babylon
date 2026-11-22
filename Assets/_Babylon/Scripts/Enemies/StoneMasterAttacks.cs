using System.Collections;
using UnityEngine;

namespace Babylon
{
    /// <summary>The Stone Master's attacks, phase change and death. Each attack is telegraphed and has a counter.</summary>
    public partial class StoneMasterAI
    {
        // Ground Smash: raise the spear overhead (red ring grows) -> slam + shockwave (jump it) -> kneel with core exposed.
        IEnumerator Smash(bool allowDouble)
        {
            int count = allowDouble ? 2 : 1;
            for (int i = 0; i < count; i++)
            {
                float windup = i == 0 ? (phase2 ? 0.8f : 1.1f) : 0.6f;
                Vector3 impact = transform.position + transform.forward * 3.2f;
                rig.Set(BossPose.Raise, 7f);
                AudioManager.I?.PlayAt(SoundId.BossRoar, transform.position + Vector3.up * 4, 0.45f, 1.35f, 60f);
                if (ring) ring.Show(impact, Quaternion.identity, 4.5f, windup);
                for (float t = 0; t < windup; t += Time.deltaTime)
                {
                    if (PlayerAlive) Turn(Flat(Player.transform.position - transform.position), 40f * Time.deltaTime);
                    impact = transform.position + transform.forward * 3.2f;
                    if (ring) ring.transform.position = impact + Vector3.up * 0.04f;
                    yield return null;
                }
                rig.Set(BossPose.Slam, 24f);
                yield return new WaitForSeconds(0.14f);
                if (ring) ring.Hide();
                FX.Spawn("FX_Dust", impact, 4f, 2f);
                AudioManager.I?.PlayAt(SoundId.GroundSmash, impact, 1f, Random.Range(0.9f, 1.05f), 70f);
                CameraShake.ShakeAt(impact, 0.9f, 28f, 0.7f);
                NoiseSystem.Emit(impact, 25f, false);
                if (PlayerAlive && !(Motor && Motor.IsInvulnerable) && Flat(Player.transform.position - impact).magnitude < 4.5f)
                    Player.Health.TakeDamage(new DamageInfo(30f, DamageKind.Smash, Player.transform.position, Flat(Player.transform.position - impact).normalized, gameObject, 10f));
                var swPrefab = BabylonLibrary.I ? BabylonLibrary.I.Prefab("Shockwave") : null;
                if (swPrefab)
                {
                    var sw = Instantiate(swPrefab, impact + Vector3.up * 0.05f, Quaternion.identity).GetComponent<Shockwave>();
                    if (sw) sw.source = gameObject;
                }
                yield return new WaitForSeconds(0.35f);
            }
            yield return Exposed(phase2 ? 1.7f : 2.3f, "Its core is exposed - STRIKE!");
        }

        // Spear Sweep: wind up to the side (arc decal) -> chest-high sweep. Duck, crouch, slide or roll under it.
        IEnumerator Sweep()
        {
            float windup = phase2 ? 0.7f : 0.9f;
            rig.Set(BossPose.SweepWind, 6f);
            if (arc) arc.Show(transform.position, transform.rotation, 6f, windup);
            AudioManager.I?.PlayAt(SoundId.BossStep, transform.position, 0.6f, 1.4f);
            for (float t = 0; t < windup; t += Time.deltaTime)
            {
                if (arc) arc.transform.SetPositionAndRotation(transform.position + Vector3.up * 0.04f, transform.rotation);
                yield return null;
            }
            rig.Set(BossPose.SweepFollow, 20f);
            AudioManager.I?.PlayAt(SoundId.SwordWhoosh, transform.position + Vector3.up * 2.5f, 1f, 0.45f);
            yield return new WaitForSeconds(0.1f);
            if (PlayerAlive)
            {
                Vector3 to = Flat(Player.transform.position - transform.position);
                if (to.magnitude < 6f && Vector3.Angle(transform.forward, to) < 85f && !(Motor && Motor.IsLow))
                    Player.Health.TakeDamage(new DamageInfo(25f, DamageKind.Sweep, Player.transform.position + Vector3.up * 1.4f, Vector3.Cross(Vector3.up, transform.forward) * -1f + to.normalized, gameObject, 9f));
            }
            yield return new WaitForSeconds(0.4f);
            if (arc) arc.Hide();
            rig.Set(BossPose.Idle, 4f);
            yield return new WaitForSeconds(phase2 ? 0.6f : 0.9f);
        }

        // Charge: eyes flare + roar, locks direction, rams forward. Dodge sideways — lure it into a pillar to stun it.
        IEnumerator Charge()
        {
            chargeCooldown = phase2 ? 4f : 6f;
            rig.Set(BossPose.ChargeWind, 6f);
            AudioManager.I?.PlayAt(SoundId.BossRoar, transform.position + Vector3.up * 4, 1f, 1.1f, 80f);
            float baseEye = eyeGlow;
            for (float t = 0; t < 0.9f; t += Time.deltaTime)
            {
                eyeGlow = Mathf.Lerp(baseEye, 14f, t / 0.9f);
                if (PlayerAlive) Turn(Flat(Player.transform.position - transform.position), 200f * Time.deltaTime);
                CameraShake.Shake(0.2f, 0.2f);
                yield return null;
            }
            Vector3 dir = PlayerAlive ? Flat(Player.transform.position - transform.position).normalized : transform.forward;
            transform.rotation = Quaternion.LookRotation(dir);
            rig.Set(BossPose.Charge, 10f);
            bool hitPlayer = false;
            float travelled = 0, time = 0;
            while (travelled < 22f && time < 3.4f)
            {
                float dt = Time.deltaTime;
                time += dt;
                rig.moveSpeed = chargeSpeed;
                Vector3 before = transform.position;
                Move(dir * chargeSpeed);
                travelled += Flat(transform.position - before).magnitude;

                if (!hitPlayer && PlayerAlive && !(Motor && Motor.IsInvulnerable) && Flat(Player.transform.position - transform.position).magnitude < 2.1f)
                {
                    hitPlayer = true;
                    Vector3 side = Vector3.Cross(Vector3.up, dir) * (Vector3.Dot(Vector3.Cross(Vector3.up, dir), Player.transform.position - transform.position) > 0 ? 1 : -1);
                    Player.Health.TakeDamage(new DamageInfo(35f, DamageKind.Charge, Player.transform.position, (dir + side).normalized, gameObject, 13f));
                }
                if (Physics.SphereCast(transform.position + Vector3.up * 1.6f, 0.9f, dir, out var h, 1.4f, Layers.World, QueryTriggerInteraction.Ignore) && h.collider.CompareTag(Layers.TagPillar))
                { eyeGlow = baseEye; yield return Crash(h.point, 3.5f, "It slammed into the pillar! STRIKE THE CORE!"); yield break; }
                if (Flat(transform.position - arenaCenter).magnitude > arenaRadius - 1.6f)
                { eyeGlow = baseEye; yield return Crash(transform.position + dir * 1.5f + Vector3.up, 2.2f, "Dazed against the wall - strike now!"); yield break; }
                yield return null;
            }
            eyeGlow = baseEye;
            rig.Set(BossPose.Idle, 5f);
            yield return Exposed(1.1f, null);
        }

        IEnumerator Crash(Vector3 point, float stun, string hint)
        {
            FX.Spawn("FX_Dust", point, 4f, 1.8f);
            FX.Spawn("FX_Sparks", point, 2f, 2f);
            AudioManager.I?.PlayAt(SoundId.GroundSmash, point, 1f, 0.8f, 70f);
            AudioManager.I?.PlayAt(SoundId.Crumble, point, 0.6f, 1.2f, 50f);
            CameraShake.ShakeAt(point, 1f, 30f, 0.8f);
            yield return Exposed(stun, hint, true);
        }

        IEnumerator Exposed(float seconds, string hint, bool dizzy = false)
        {
            rig.Set(BossPose.Kneel, 5f);
            core.SetExposed(true);
            if (hint != null && HUD.I) HUD.I.ShowHint(hint, Mathf.Min(seconds, 2.5f));
            for (float t = 0; t < seconds && mode == Mode.Fighting; t += Time.deltaTime)
            {
                rig.wobble = dizzy ? 1f : 0f;
                yield return null;
            }
            rig.wobble = 0;
            core.SetExposed(false);
            rig.Set(BossPose.Idle, 3.5f);
            yield return new WaitForSeconds(0.5f);
        }

        // Phase 2 only: tears a rock from the floor and hurls it at where you are going.
        IEnumerator Throw()
        {
            rig.Set(BossPose.ThrowWind, 6f);
            var prefab = BabylonLibrary.I ? BabylonLibrary.I.Prefab("Boulder") : null;
            Boulder b = null;
            if (prefab) { b = Instantiate(prefab, handR.position, Quaternion.identity, handR).GetComponent<Boulder>(); b.source = gameObject; }
            AudioManager.I?.PlayAt(SoundId.Crumble, transform.position, 0.5f, 1.4f);
            for (float t = 0; t < 1f; t += Time.deltaTime)
            {
                if (PlayerAlive) Turn(Flat(Player.transform.position - transform.position), 120f * Time.deltaTime);
                yield return null;
            }
            rig.Set(BossPose.ThrowRelease, 16f);
            yield return new WaitForSeconds(0.12f);
            if (b && PlayerAlive)
            {
                var motorV = Player.GetComponent<CharacterController>().velocity;
                Vector3 target = Player.transform.position + Flat(motorV) * 0.9f;
                b.Launch(target, 1.1f);
            }
            else if (b) Destroy(b.gameObject);
            yield return new WaitForSeconds(0.8f);
            rig.Set(BossPose.Idle, 4f);
        }

        IEnumerator EnterPhase2()
        {
            if (HUD.I) HUD.I.ShowSubtitle("The Stone Master's runes blaze with fury!", 4.5f);
            runeGlow = 5f; eyeGlow = 8f;
            turnSpeed *= 1.2f;
            yield return RoarRoutine(1.6f);
        }

        IEnumerator Die()
        {
            mode = Mode.Dead;
            rig.wobble = 0;
            if (ring) ring.Hide();
            if (arc) arc.Hide();
            if (HUD.I) { HUD.I.ShowBoss("", false); HUD.I.ShowSubtitle("The Stone Master crumbles. The sealed door grinds open...", 5f); }
            AudioManager.I?.PlayAt(SoundId.BossRoar, transform.position + Vector3.up * 4, 1f, 0.6f, 80f);
            rig.Set(BossPose.Kneel, 3f);
            yield return new WaitForSeconds(1f);

            core.FadeOut();
            SetGlow(0, 0);
            cc.enabled = false;
            rig.enabled = false;
            AudioManager.I?.PlayAt(SoundId.Crumble, transform.position + Vector3.up * 2, 1f, 0.9f, 70f);
            FX.Spawn("FX_Dust", transform.position + Vector3.up * 2f, 5f, 3f);
            CameraShake.Shake(0.9f, 1.2f);
            Vector3 center = transform.position + Vector3.up * 2f;
            foreach (var mr in visualRoot.GetComponentsInChildren<MeshRenderer>())
            {
                var t = mr.transform;
                t.SetParent(null, true);
                var mf = t.GetComponent<MeshFilter>();
                var box = t.gameObject.AddComponent<BoxCollider>();
                if (mf && mf.sharedMesh) { box.center = mf.sharedMesh.bounds.center; box.size = mf.sharedMesh.bounds.size; }
                var rb = t.gameObject.AddComponent<Rigidbody>();
                rb.mass = 30f;
                rb.AddExplosionForce(6f, center, 6f, 0.4f, ForceMode.VelocityChange);
                Destroy(t.gameObject, 25f);
            }
            AudioManager.I?.PlayMusic(MusicId.Mysterious, 4f);
            yield return new WaitForSeconds(2.5f);
            if (exitDoor) exitDoor.Open();
            if (HUD.I) HUD.I.SetObjective("Pass through the open door to the final chamber");
        }
    }
}