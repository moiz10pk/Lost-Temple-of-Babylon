using System.Collections.Generic;
using UnityEngine;

namespace Babylon
{
    /// <summary>
    /// Blind giant spider. It cannot see: it hears noise (footsteps, landings, sword clangs) and feels anything that
    /// touches it. Heard noise -> rear up and hiss -> hunt to the sound -> search -> back to patrol.
    /// Crouch/duck to move silently, or stand still and let it pass.
    /// </summary>
    [RequireComponent(typeof(CharacterController), typeof(Health))]
    public class SpiderAI : MonoBehaviour
    {
        public WalkArea area;
        public SpiderRig rig;
        public float patrolSpeed = 1.6f, huntSpeed = 4.4f, hearing = 1f;
        public float biteDamage = 15f, attackRange = 2.2f, touchRange = 1.6f, attackCooldown = 1.5f;

        enum State { Patrol, Alert, Hunt, Search, Attack, Recoil, Dead }
        State state = State.Patrol;

        CharacterController cc;
        Health health;
        LoopedSound skitter;
        List<Vector3> path = new List<Vector3>();
        Vector3 lastHeard, recoilDir;
        float timer, repathTimer, cooldownTimer, waitTimer, currentSpeed;
        bool lunged;

        public bool IsHunting => state == State.Hunt || state == State.Attack || state == State.Alert;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            health = GetComponent<Health>();
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        void OnEnable() => NoiseSystem.OnNoise += OnNoise;
        void OnDisable() => NoiseSystem.OnNoise -= OnNoise;

        void Start()
        {
            if (AudioManager.I) skitter = AudioManager.I.CreateLoop(SoundId.SpiderSkitter, transform, 0.55f, 16f);
            if (!area) area = FindFirstObjectByType<WalkArea>();
            waitTimer = Random.Range(0f, 2f);
        }

        void OnNoise(Vector3 pos, float radius)
        {
            if (state == State.Dead) return;
            Vector3 ear = transform.position + Vector3.up * 0.8f;
            float d = Vector3.Distance(ear, pos);
            float eff = radius * hearing;
            if (d > eff) return;
            if (Physics.Linecast(ear, pos + Vector3.up * 0.5f, Layers.World, QueryTriggerInteraction.Ignore)) eff *= 0.6f;
            if (d <= eff) Heard(pos);
        }

        void Heard(Vector3 pos)
        {
            lastHeard = pos;
            if (state == State.Patrol || state == State.Search)
            {
                state = State.Alert; timer = 0;
                AudioManager.I?.PlayAt(SoundId.SpiderHiss, transform.position, 0.9f, Random.Range(0.85f, 1.1f), 25f);
            }
            else if (state == State.Hunt && repathTimer <= 0) Repath(pos);
        }

        void Repath(Vector3 target)
        {
            repathTimer = 0.4f;
            path = area ? area.FindPath(transform.position, target) : new List<Vector3> { target };
        }

        void Update()
        {
            if (state == State.Dead) return;
            float dt = Time.deltaTime;
            timer += dt; repathTimer -= dt; cooldownTimer -= dt;
            var player = PlayerVitals.I;
            Vector3 pp = player ? player.transform.position : Vector3.one * 9999f;
            float pd = Vector3.Distance(Flat(transform.position), Flat(pp));
            bool playerAlive = player && !player.IsDead;

            // Touch: anything brushing against it is attacked.
            if (playerAlive && pd < touchRange && state != State.Attack && state != State.Recoil && cooldownTimer <= 0)
            { state = State.Attack; timer = 0; lunged = false; lastHeard = pp; }

            float speed = 0;
            switch (state)
            {
                case State.Patrol:
                    if (path.Count == 0)
                    {
                        waitTimer -= dt;
                        if (waitTimer <= 0 && area) { path = area.FindPath(transform.position, area.RandomPatrolPoint()); waitTimer = Random.Range(1f, 3f); }
                    }
                    speed = patrolSpeed;
                    break;
                case State.Alert:
                    if (rig) rig.rear = Mathf.Sin(Mathf.Clamp01(timer / 0.6f) * Mathf.PI) * 0.7f;
                    if (timer > 0.6f) { state = State.Hunt; Repath(lastHeard); }
                    break;
                case State.Hunt:
                    speed = huntSpeed;
                    if (path.Count == 0) { state = State.Search; timer = 0; }
                    break;
                case State.Search:
                    transform.Rotate(0, 70f * dt * Mathf.Sin(timer * 1.5f), 0);
                    if (timer > 3.5f) { state = State.Patrol; path.Clear(); }
                    break;
                case State.Attack:
                    UpdateAttack(dt, pp, pd, player);
                    break;
                case State.Recoil:
                    cc.Move((recoilDir * 6f * (1f - timer / 0.4f) + Vector3.down * 4f) * dt);
                    if (timer > 0.4f) { state = State.Hunt; Repath(lastHeard); }
                    break;
            }

            if (speed > 0) FollowPath(speed, dt); else currentSpeed = Mathf.MoveTowards(currentSpeed, 0, dt * 10f);
            if (state != State.Alert && state != State.Attack && rig) rig.rear = Mathf.MoveTowards(rig.rear, 0, dt * 3f);
            if (rig) rig.speed = currentSpeed;
            if (skitter) skitter.modulation = Mathf.Clamp01(currentSpeed / huntSpeed + 0.1f);
        }

        void UpdateAttack(float dt, Vector3 pp, float pd, PlayerVitals player)
        {
            Face(pp, dt * 3f);
            if (rig) rig.rear = timer < 0.4f ? timer / 0.4f : Mathf.Max(0, 1f - (timer - 0.4f) / 0.2f);
            if (timer >= 0.4f && !lunged)
            {
                lunged = true;
                AudioManager.I?.PlayAt(SoundId.SpiderHiss, transform.position, 1f, 1.3f, 25f);
                if (player && !player.IsDead && pd < attackRange)
                {
                    Vector3 dir = (pp - transform.position).normalized;
                    player.Health.TakeDamage(new DamageInfo(biteDamage, DamageKind.Bite, pp + Vector3.up, dir, gameObject, 5f));
                }
            }
            if (timer < 0.55f && timer >= 0.4f) cc.Move(transform.forward * 7f * dt);
            if (timer > 0.75f) { cooldownTimer = attackCooldown; state = State.Hunt; lastHeard = pp; Repath(pp); }
        }

        void FollowPath(float speed, float dt)
        {
            if (path.Count == 0) { currentSpeed = Mathf.MoveTowards(currentSpeed, 0, dt * 10f); return; }
            Vector3 target = path[0];
            Vector3 to = Flat(target) - Flat(transform.position);
            if (to.magnitude < 0.45f) { path.RemoveAt(0); return; }
            currentSpeed = Mathf.MoveTowards(currentSpeed, speed, dt * 8f);
            Face(target, dt * 6f);
            Vector3 move = to.normalized * currentSpeed;
            Vector3 before = transform.position;
            cc.Move((move + Vector3.down * 4f) * dt);
            // If blocked (pillar, wall corner) give up on this waypoint.
            if ((transform.position - before).sqrMagnitude < 0.0001f * currentSpeed) { timer += dt; if (state == State.Patrol && timer > 2f) { path.Clear(); timer = 0; } }
        }

        void Face(Vector3 target, float t)
        {
            Vector3 d = Flat(target) - Flat(transform.position);
            if (d.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), Mathf.Clamp01(t));
        }

        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }

        void OnDamaged(DamageInfo d, float amount)
        {
            if (amount <= 0 || health.IsDead) return;
            recoilDir = Flat(transform.position - (d.source ? d.source.transform.position : d.point)).normalized;
            lastHeard = d.source ? d.source.transform.position : d.point;
            state = State.Recoil; timer = 0;
            FX.Spawn("FX_Blood", d.point, 2f);
            AudioManager.I?.PlayAt(SoundId.SpiderHiss, transform.position, 1f, 0.7f, 25f);
        }

        void OnDied()
        {
            state = State.Dead;
            if (rig) rig.dead = true;
            cc.enabled = false;
            if (skitter) Destroy(skitter.gameObject);
            AudioManager.I?.PlayAt(SoundId.SpiderHiss, transform.position, 1f, 0.5f, 25f);
            FX.Spawn("FX_Blood", transform.position + Vector3.up * 0.6f, 2f, 1.5f);
            if (HUD.I) HUD.I.ShowHint("The spider is slain.", 1.5f);
            Destroy(gameObject, 8f);
        }
    }
}
