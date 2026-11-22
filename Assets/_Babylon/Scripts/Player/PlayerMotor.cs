using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Babylon
{
    /// <summary>
    /// CharacterController movement: walk, sprint, jump (sprint-leap), crouch (C), duck (hold Ctrl), dodge-roll (Q),
    /// sprint-slide (crouch while sprinting) and the plunge used by the sword's air attack. Emits footstep noise.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour
    {
        public enum Stance { Stand, Crouch, Duck }

        [Header("Speeds")]
        public float walkSpeed = 4.5f, sprintSpeed = 7.5f, crouchSpeed = 2.2f, duckSpeed = 1.4f;
        public float jumpHeight = 1.2f, gravity = -20f;
        [Header("Heights")]
        public float standHeight = 1.8f, crouchHeight = 1.15f, duckHeight = 0.85f;
        [Header("Roll / Slide")]
        public float rollDistance = 6.5f, rollTime = 0.5f, rollCooldown = 0.8f, rollIFrames = 0.42f, rollStamina = 22f;
        public float slideTime = 0.75f, slideSpeed = 10.5f;

        public Stance stance { get; private set; } = Stance.Stand;
        public bool IsRolling { get; private set; }
        public bool IsSliding { get; private set; }
        public bool IsPlunging { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool IsGrounded => cc.isGrounded;
        public bool IsAirborne => airTime > 0.12f;
        public bool IsLow => stance != Stance.Stand || IsRolling || IsSliding || cc.height < standHeight - 0.3f;
        public bool IsInvulnerable => IsRolling && rollTimer < rollIFrames;
        public float Height => cc.height;
        public float PlanarSpeed => new Vector3(velocity.x, 0, velocity.z).magnitude;
        public float RollProgress => IsRolling ? rollTimer / rollTime : 0f;
        public Vector3 RollLocalDir { get; private set; }

        public event Action<float> Landed;
        public event Action PlungeLanded;

        CharacterController cc;
        PlayerVitals vitals;
        Vector3 velocity, external, rollDir;
        float vy, airTime, rollTimer, rollCooldownTimer, slideTimer, stepDistance;
        bool duckHeld, crouchToggled;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            vitals = GetComponent<PlayerVitals>();
        }

        public void AddKnockback(Vector3 v) { external += v; if (v.y > 0) vy = Mathf.Max(vy, v.y); }

        public bool CanPlunge => IsAirborne && !IsPlunging && !IsRolling;

        public void StartPlunge() { if (!CanPlunge) return; IsPlunging = true; vy = -26f; }

        /// <summary>Instantly moves the player (spawn points / respawns).</summary>
        public void Teleport(Vector3 pos, float yaw)
        {
            cc.enabled = false; transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0)); cc.enabled = true;
            velocity = external = Vector3.zero; vy = 0;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0) return;
            bool frozen = UIState.InputBlocked || (vitals && vitals.IsDead);
            var kb = Keyboard.current;

            Vector2 move = Vector2.zero;
            bool sprintHeld = false, jumpPressed = false, rollPressed = false, crouchPressed = false;
            if (!frozen && kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.y += 1;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.y -= 1;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1;
                sprintHeld = kb.leftShiftKey.isPressed;
                jumpPressed = kb.spaceKey.wasPressedThisFrame;
                rollPressed = kb.qKey.wasPressedThisFrame || kb.leftAltKey.wasPressedThisFrame;
                crouchPressed = kb.cKey.wasPressedThisFrame;
                duckHeld = kb.leftCtrlKey.isPressed;
            }
            else duckHeld = false;
            if (move.sqrMagnitude > 1) move.Normalize();

            bool grounded = cc.isGrounded;
            Vector3 wish = transform.right * move.x + transform.forward * move.y;
            IsSprinting = sprintHeld && move.y > 0.1f && grounded && stance == Stance.Stand && !IsRolling && vitals.Stamina > 1f;

            // Slide: crouch or duck while sprinting.
            if (!IsSliding && IsSprinting && (crouchPressed || (kb != null && kb.leftCtrlKey.wasPressedThisFrame)))
            {
                IsSliding = true; slideTimer = 0; crouchPressed = false;
                AudioManager.I?.PlayAt(SoundId.Roll, transform.position, 0.7f, 0.8f);
                NoiseSystem.Emit(transform.position, 6f);
            }

            if (crouchPressed) crouchToggled = !crouchToggled;
            stance = duckHeld ? Stance.Duck : (crouchToggled ? Stance.Crouch : Stance.Stand);

            // Dodge roll.
            rollCooldownTimer -= dt;
            if (rollPressed && !IsRolling && grounded && rollCooldownTimer <= 0 && vitals.UseStamina(rollStamina))
            {
                IsRolling = true; rollTimer = 0;
                rollDir = wish.sqrMagnitude > 0.01f ? wish.normalized : transform.forward;
                RollLocalDir = transform.InverseTransformDirection(rollDir);
                AudioManager.I?.PlayAt(SoundId.Roll, transform.position, 0.8f);
                NoiseSystem.Emit(transform.position, 5f);
            }

            Vector3 target;
            float accel = grounded ? 40f : 8f;
            if (IsRolling)
            {
                rollTimer += dt;
                float k = Mathf.Sin(Mathf.Clamp01(rollTimer / rollTime) * Mathf.PI) * 1.57f;
                target = rollDir * (rollDistance / rollTime) * k;
                accel = 200f;
                if (rollTimer >= rollTime) { IsRolling = false; rollCooldownTimer = rollCooldown; }
            }
            else if (IsSliding)
            {
                slideTimer += dt;
                target = transform.forward * Mathf.Lerp(slideSpeed, crouchSpeed, slideTimer / slideTime) + transform.right * move.x * 1.5f;
                accel = 60f;
                if (slideTimer >= slideTime || !grounded) IsSliding = false;
            }
            else
            {
                float speed = stance == Stance.Duck ? duckSpeed : stance == Stance.Crouch ? crouchSpeed : (IsSprinting ? sprintSpeed : walkSpeed);
                target = wish * speed;
            }
            velocity = Vector3.MoveTowards(velocity, target, accel * dt);
            if (IsSprinting && move.sqrMagnitude > 0.1f) vitals.DrainStamina(14f * dt);

            // Jump (stand up first). Sprint jumps get a heroic forward leap.
            if (jumpPressed && grounded && !IsRolling)
            {
                if (stance != Stance.Stand) { crouchToggled = false; }
                else if (vitals.UseStamina(8f))
                {
                    vy = Mathf.Sqrt(2f * jumpHeight * -gravity);
                    if (IsSprinting) external += transform.forward * 3f;
                    IsSliding = false;
                    NoiseSystem.Emit(transform.position, 4f);
                }
            }

            // Gravity.
            if (grounded && vy < 0 && !IsPlunging) vy = -2f;
            vy += gravity * dt * (vy < 0 ? 1.25f : 1f);
            external = Vector3.MoveTowards(external, Vector3.zero, (grounded ? 18f : 4f) * dt);

            float preVy = vy;
            cc.Move((velocity + external + Vector3.up * vy) * dt);

            // Landing.
            if (cc.isGrounded)
            {
                if (airTime > 0.25f || IsPlunging)
                {
                    float impact = -preVy;
                    Landed?.Invoke(impact);
                    AudioManager.I?.PlayAt(SoundId.Land, transform.position, Mathf.Clamp01(impact / 12f) + 0.3f);
                    NoiseSystem.Emit(transform.position, IsPlunging ? 14f : Mathf.Clamp(impact, 3f, 10f));
                    if (IsPlunging) { IsPlunging = false; PlungeLanded?.Invoke(); }
                }
                airTime = 0;
            }
            else airTime += dt;

            UpdateHeight(dt);
            Footsteps(dt);
        }

        void UpdateHeight(float dt)
        {
            float targetH = IsRolling || IsSliding ? duckHeight : stance == Stance.Duck ? duckHeight : stance == Stance.Crouch ? crouchHeight : standHeight;
            if (targetH > cc.height)
            {
                // Don't stand up into a ceiling.
                Vector3 bottom = transform.position + Vector3.up * cc.radius;
                if (Physics.SphereCast(bottom, cc.radius * 0.9f, Vector3.up, out _, targetH - cc.radius, Layers.World, QueryTriggerInteraction.Ignore))
                {
                    targetH = cc.height;
                    if (!duckHeld && !crouchToggled) stance = Stance.Crouch;
                }
            }
            float h = Mathf.MoveTowards(cc.height, targetH, dt * 6f);
            cc.height = h;
            cc.center = Vector3.up * (h * 0.5f);
        }

        void Footsteps(float dt)
        {
            if (!cc.isGrounded || IsRolling) return;
            float speed = PlanarSpeed;
            if (speed < 0.5f) return;
            stepDistance += speed * dt;
            float stride = IsSprinting ? 2.4f : stance == Stance.Stand ? 1.9f : 1.3f;
            if (stepDistance < stride) return;
            stepDistance = 0;
            float loud = IsSliding ? 6f : IsSprinting ? 13f : stance == Stance.Duck ? 1f : stance == Stance.Crouch ? 1.6f : 7f;
            float vol = IsSprinting ? 0.9f : stance == Stance.Stand ? 0.55f : 0.18f;

            // Crouched or ducked walking makes no audible sound (sliding still does).
            bool silent = !IsSliding && stance != Stance.Stand;
            if (!silent)
                AudioManager.I?.PlayAt(SoundId.Footstep, transform.position, vol, UnityEngine.Random.Range(0.85f, 1.15f), 20f);
            NoiseSystem.Emit(transform.position, loud);
        }
    }
}