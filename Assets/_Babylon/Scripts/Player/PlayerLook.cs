using UnityEngine;
using UnityEngine.InputSystem;

namespace Babylon
{
    /// <summary>Mouse look, eye height that follows the stance, head bob, roll/slide camera moves, sprint FOV, cursor lock.</summary>
    public class PlayerLook : MonoBehaviour
    {
        public Transform cameraRig;   // pitch + eye height
        public Transform bobPivot;    // bob, tilt and roll animation
        public Camera cam;
        public float baseFov = 70f;

        PlayerMotor motor;
        PlayerVitals vitals;
        float pitch, bobPhase, landDip, deathTilt;

        void Awake()
        {
            motor = GetComponent<PlayerMotor>();
            vitals = GetComponent<PlayerVitals>();
            motor.Landed += v => landDip = Mathf.Min(0.35f, v * 0.025f);
        }

        void Update()
        {
            bool free = UIState.CursorFree;
            Cursor.lockState = free ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = free;

            if (!UIState.InputBlocked && !vitals.IsDead && Mouse.current != null)
            {
                Vector2 d = Mouse.current.delta.ReadValue() * Settings.MouseSensitivity;
                transform.Rotate(0, d.x, 0);
                pitch = Mathf.Clamp(pitch - d.y, -82f, 82f);
            }
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            float eye = motor.Height - 0.16f;
            if (vitals.IsDead) { deathTilt = Mathf.MoveTowards(deathTilt, 1f, dt * 1.5f); eye = Mathf.Lerp(eye, 0.3f, deathTilt); }
            landDip = Mathf.MoveTowards(landDip, 0, dt * 1.4f);
            cameraRig.localPosition = new Vector3(0, eye - landDip, 0);

            // Roll: dip forward and sweep the camera through the roll direction.
            float rollPitch = 0, rollZ = 0;
            if (motor.IsRolling)
            {
                float p = Mathf.Sin(motor.RollProgress * Mathf.PI);
                rollPitch = p * 40f * Mathf.Max(0.3f, motor.RollLocalDir.z);
                rollZ = -p * 25f * motor.RollLocalDir.x;
            }
            if (motor.IsSliding) rollZ += 6f;
            cameraRig.localRotation = Quaternion.Euler(pitch + rollPitch, 0, 0);

            // Head bob from planar speed.
            float speed = motor.IsGrounded ? motor.PlanarSpeed : 0f;
            bobPhase += dt * speed * 1.7f;
            float amp = Mathf.Clamp01(speed / 7f) * 0.05f;
            Vector3 bob = new Vector3(Mathf.Cos(bobPhase) * amp * 0.6f, Mathf.Abs(Mathf.Sin(bobPhase)) * amp, 0);
            bobPivot.localPosition = Vector3.Lerp(bobPivot.localPosition, bob, dt * 12f);
            float tilt = vitals.IsDead ? deathTilt * 70f : rollZ;
            bobPivot.localRotation = Quaternion.Slerp(bobPivot.localRotation, Quaternion.Euler(0, 0, tilt), dt * 10f);

            float fov = baseFov + (motor.IsSprinting ? 6f : 0f) + (motor.IsSliding ? 9f : 0f) + (motor.IsRolling ? 5f : 0f);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, fov, dt * 6f);
        }

        public void LookAt(Vector3 worldPoint)
        {
            Vector3 d = worldPoint - cameraRig.position;
            transform.rotation = Quaternion.Euler(0, Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, 0);
            pitch = -Mathf.Atan2(d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg;
        }
    }
}
