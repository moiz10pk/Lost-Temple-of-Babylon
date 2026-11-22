using System.Collections;
using UnityEngine;

namespace Babylon
{
    /// <summary>
    /// The Stone Master, Guardian of Babylon (300 HP). Sleeps as a kneeling idol until the player comes close,
    /// then its eyes open and it hunts the player — slower than you, but every blow is lethal.
    /// Only the chest core takes real damage, and it is exposed only after a Ground Smash or a failed Charge.
    /// </summary>
    [RequireComponent(typeof(CharacterController), typeof(Health))]
    public partial class StoneMasterAI : MonoBehaviour
    {
        [Header("Rig")]
        public StoneMasterRig rig;
        public StoneMasterCore core;
        public Renderer[] eyes;
        public Renderer[] runes;
        public Transform[] eyelids;
        public Light eyeLight;
        public Transform handR;
        public Transform visualRoot;

        [Header("Arena")]
        public Vector3 arenaCenter;
        public float arenaRadius = 14.5f;
        public float wakeRadius = 11f;
        public StoneDoor exitDoor;

        [Header("Tuning")]
        public float chaseSpeed = 2.4f, phase2ChaseSpeed = 3.1f, chargeSpeed = 7f, turnSpeed = 95f;
        public float chipDamage = 3f;

        enum Mode { Dormant, Waking, Fighting, Dead }
        Mode mode = Mode.Dormant;

        CharacterController cc;
        Health health;
        TelegraphDecal ring, arc;
        MaterialPropertyBlock mpb;
        bool phase2, pendingPhase2;
        float attackCooldown = 1.5f, chargeCooldown, eyeGlow, runeGlow, hintTimer;
        Vector3 vel;

        PlayerVitals Player => PlayerVitals.I;
        bool PlayerAlive => Player && !Player.IsDead;

        // Lets attacks check the player's stance (duck / crouch / slide / roll).
        PlayerMotor motor;
        PlayerMotor Motor
        {
            get
            {
                if (!motor && Player) motor = Player.GetComponent<PlayerMotor>();
                return motor;
            }
        }

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            health = GetComponent<Health>();
            health.filter = Filter;
            health.Damaged += OnDamaged;
            health.Died += () => { StopAllCoroutines(); StartCoroutine(Die()); };
            health.Changed += n => { if (HUD.I) HUD.I.SetBoss(n); };
            mpb = new MaterialPropertyBlock();
            rig.Footstep = () =>
            {
                AudioManager.I?.PlayAt(SoundId.BossStep, transform.position, 0.9f, Random.Range(0.9f, 1.1f), 45f);
                CameraShake.ShakeAt(transform.position, 0.25f, 18f, 0.3f);
            };
        }

        IEnumerator Start()
        {
            rig.Snap(BossPose.Idol);
            ring = SpawnDecal("Decal_Ring");
            arc = SpawnDecal("Decal_Arc");
            SetGlow(0, 0);
            while (mode == Mode.Dormant)
            {
                if (PlayerAlive && Flat(Player.transform.position - transform.position).magnitude < wakeRadius) break;
                yield return null;
            }
            yield return Wake();
            yield return Brain();
        }

        TelegraphDecal SpawnDecal(string name)
        {
            var lib = BabylonLibrary.I;
            var p = lib ? lib.Prefab(name) : null;
            return p ? Instantiate(p).GetComponent<TelegraphDecal>() : null;
        }

        float Filter(DamageInfo d)
        {
            if (mode == Mode.Dormant) { mode = Mode.Waking; return 0; }
            if (mode != Mode.Fighting) return 0;
            if (d.kind == DamageKind.Core) return d.amount;
            // Stone armour: sparks, clang, a token chip of damage.
            FX.Spawn("FX_Sparks", d.point, Quaternion.LookRotation(-d.direction), 1.5f);
            AudioManager.I?.PlayAt(SoundId.StoneClang, d.point, 1f, Random.Range(0.8f, 1f));
            NoiseSystem.Emit(d.point, 12f, false);
            if (hintTimer <= 0 && HUD.I) { HUD.I.ShowHint("Strike the glowing core when it is exposed!", 2.5f); hintTimer = 4f; }
            return chipDamage;
        }

        void OnDamaged(DamageInfo d, float amount)
        {
            if (amount <= 0) return;
            if (!phase2 && health.Normalized <= 0.5f) { phase2 = true; pendingPhase2 = true; }
        }

        IEnumerator Wake()
        {
            mode = Mode.Waking;
            if (HUD.I) HUD.I.ShowSubtitle("The idol stirs... its stone eyes are opening.", 4f);
            var rumble = AudioManager.I ? AudioManager.I.PlayAt(SoundId.Rumble, transform.position, 1f, 1f, 60f) : null;
            FX.Spawn("FX_Dust", transform.position + Vector3.up * 3f, 4f, 2f);
            AudioManager.I?.PlayMusic(MusicId.Intense, 3f);
            for (float t = 0; t < 1f; t += Time.deltaTime / 2.2f)
            {
                CameraShake.Shake(0.25f, 0.3f);
                foreach (var lid in eyelids) if (lid) lid.localPosition = new Vector3(lid.localPosition.x, Mathf.Lerp(0f, 0.16f, t), lid.localPosition.z);
                SetGlow(t * 5f, t * 2f);
                yield return null;
            }
            if (rumble) rumble.Stop();
            rig.Set(BossPose.Idle, 1.6f);
            yield return new WaitForSeconds(1.6f);
            yield return RoarRoutine(1.6f);
            if (HUD.I) { HUD.I.ShowBoss("THE STONE MASTER  -  GUARDIAN OF BABYLON", true); HUD.I.SetBoss(1); HUD.I.SetObjective("Defeat the Stone Master: strike its glowing core"); }
            mode = Mode.Fighting;
        }

        IEnumerator RoarRoutine(float hold)
        {
            rig.Set(BossPose.Roar, 6f);
            AudioManager.I?.PlayAt(SoundId.BossRoar, transform.position + Vector3.up * 4f, 1f, 1f, 80f);
            NoiseSystem.Emit(transform.position, 30f, false);
            for (float t = 0; t < hold; t += Time.deltaTime) { CameraShake.Shake(0.55f, 0.4f); yield return null; }
            rig.Set(BossPose.Idle, 5f);
        }

        IEnumerator Brain()
        {
            while (mode == Mode.Fighting)
            {
                if (pendingPhase2) { pendingPhase2 = false; yield return EnterPhase2(); }
                if (!PlayerAlive) { rig.Set(BossPose.Idle, 3f); yield return null; continue; }

                attackCooldown -= Time.deltaTime; chargeCooldown -= Time.deltaTime;
                Vector3 toP = Flat(Player.transform.position - transform.position);
                float d = toP.magnitude;
                float facing = Vector3.Angle(transform.forward, toP);

                IEnumerator attack = null;
                if (attackCooldown <= 0 && facing < 40f)
                {
                    float r = Random.value;
                    if (d < 5f) attack = r < 0.45f ? Sweep() : Smash(phase2 && Random.value < 0.4f);
                    else if (d < 17f && phase2 && r < 0.3f) attack = Throw();
                    else if (d > 6.5f && chargeCooldown <= 0 && r < 0.6f) attack = Charge();
                    else if (d >= 17f && phase2) attack = Throw();
                }
                if (attack != null) { yield return attack; attackCooldown = phase2 ? 0.7f : 1.3f; continue; }

                ChaseStep(toP, d);
                yield return null;
            }
        }

        void ChaseStep(Vector3 toP, float d)
        {
            float dt = Time.deltaTime;
            Turn(toP, turnSpeed * (phase2 ? 1.3f : 1f) * dt);
            float speed = d > 3.5f ? (phase2 ? phase2ChaseSpeed : chaseSpeed) : 0f;
            Vector3 dir = transform.forward;
            // Steer around pillars.
            if (Physics.SphereCast(transform.position + Vector3.up * 1.5f, 1.0f, dir, out var hit, 3f, Layers.World, QueryTriggerInteraction.Ignore) && hit.collider.CompareTag(Layers.TagPillar))
            {
                Vector3 side = Vector3.Cross(Vector3.up, dir);
                if (Vector3.Dot(side, hit.point - transform.position) > 0) side = -side;
                dir = (dir + side * 1.2f).normalized;
            }
            vel = Vector3.MoveTowards(vel, dir * speed, dt * 6f);
            Move(vel);
            rig.moveSpeed = vel.magnitude;
            rig.Set(vel.magnitude > 0.3f ? BossPose.Walk : BossPose.Idle, 5f);
        }

        void Move(Vector3 v)
        {
            if (!cc.enabled) return;
            cc.Move((v + Vector3.down * 6f) * Time.deltaTime);
            // Keep inside the arena.
            Vector3 off = Flat(transform.position - arenaCenter);
            if (off.magnitude > arenaRadius - 1.4f)
            {
                cc.enabled = false;
                transform.position = arenaCenter + off.normalized * (arenaRadius - 1.4f) + Vector3.up * transform.position.y;
                cc.enabled = true;
            }
        }

        void Turn(Vector3 dir, float maxDeg)
        {
            if (dir.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), maxDeg);
        }

        void Update()
        {
            hintTimer -= Time.deltaTime;
            if (mode == Mode.Fighting || mode == Mode.Waking)
                SetGlow(eyeGlow, runeGlow);
        }

        void SetGlow(float eye, float rune)
        {
            eyeGlow = eye; runeGlow = rune;
            foreach (var e in eyes) Emit(e, new Color(1f, 0.32f, 0.08f) * eye);
            float pulse = 1f + Mathf.Sin(Time.time * 3f) * 0.2f;
            foreach (var r in runes) Emit(r, new Color(0.25f, 0.82f, 1f) * rune * pulse);
            if (eyeLight) eyeLight.intensity = eye * 0.6f;
        }

        void Emit(Renderer r, Color c)
        {
            if (!r) return;
            r.GetPropertyBlock(mpb); mpb.SetColor("_EmissionColor", c); r.SetPropertyBlock(mpb);
        }

        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }
    }
}