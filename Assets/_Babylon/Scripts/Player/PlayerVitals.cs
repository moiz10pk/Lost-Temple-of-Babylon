using UnityEngine;

namespace Babylon
{
    /// <summary>
    /// Player health (100) and stamina. Filters damage for heroic dodges: rolls are invulnerable, ducking avoids sweeps,
    /// jumping avoids shockwaves, blocking cuts frontal damage. Triggers the game-over screen on death.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class PlayerVitals : MonoBehaviour
    {
        public float maxStamina = 100f, staminaRegen = 28f, regenDelay = 0.6f;
        public static PlayerVitals I { get; private set; }

        public Health Health { get; private set; }
        public float Stamina { get; private set; }
        public bool IsDead => Health.IsDead;
        public bool Blocking { get; set; }
        public string LastDamageSource = "the temple";

        PlayerMotor motor;
        float regenTimer;

        void Awake()
        {
            I = this;
            Health = GetComponent<Health>();
            motor = GetComponent<PlayerMotor>();
            Stamina = maxStamina;
            Health.filter = Filter;
            Health.Damaged += OnDamaged;
            Health.Died += OnDied;
            Health.Changed += n => { if (HUD.I) HUD.I.SetHealth(n, Health.Current); };
        }

        void Start() { if (HUD.I) HUD.I.SetHealth(Health.Normalized, Health.Current); }

        float Filter(DamageInfo d)
        {
            string dodge = null;
            if (motor.IsInvulnerable) dodge = "Rolled clear!";
            else if (d.kind == DamageKind.Sweep && motor.IsLow) dodge = "Ducked under!";
            else if (d.kind == DamageKind.Shockwave && motor.IsAirborne) dodge = "Leapt the shockwave!";
            if (dodge != null) { if (HUD.I) HUD.I.ShowHint(dodge, 1.2f); return 0f; }

            float amount = d.amount;
            if (Blocking && d.direction.sqrMagnitude > 0.01f)
            {
                Vector3 toSource = -d.direction; toSource.y = 0;
                if (Vector3.Dot(transform.forward, toSource.normalized) > 0.3f && UseStamina(15f))
                {
                    amount *= 0.3f;
                    AudioManager.I?.PlayAt(SoundId.StoneClang, transform.position + transform.forward, 0.7f, 1.3f);
                    if (HUD.I) HUD.I.ShowHint("Blocked!", 0.8f);
                }
            }
            return amount;
        }

        void OnDamaged(DamageInfo d, float amount)
        {
            if (amount <= 0) return;
            LastDamageSource = d.kind switch
            {
                DamageKind.Bite => "the fangs of a blind spider",
                DamageKind.Sting => "the scorpion's venom",
                DamageKind.Smash or DamageKind.Shockwave or DamageKind.Sweep or DamageKind.Charge or DamageKind.Projectile => "the Stone Master",
                _ => "the temple"
            };
            if (HUD.I) HUD.I.Flash(d.kind == DamageKind.Sting ? new Color(0.3f, 0.9f, 0.1f) : Color.red, Mathf.Clamp(amount / 40f, 0.25f, 0.6f));
            CameraShake.Shake(Mathf.Clamp(amount / 40f, 0.25f, 0.7f), 0.5f);
            AudioManager.I?.Play2D(SoundId.PlayerHurt, 0.8f, Random.Range(0.9f, 1.1f));
            if (d.knockback > 0)
            {
                Vector3 k = d.direction; k.y = 0;
                motor.AddKnockback(k.normalized * d.knockback + Vector3.up * d.knockback * 0.25f);
            }
            regenTimer = regenDelay;
        }

        void OnDied()
        {
            if (GameOverScreen.I) GameOverScreen.I.Show($"Slain by {LastDamageSource}.");
        }

        public bool UseStamina(float amount)
        {
            if (Stamina < amount) { if (HUD.I) HUD.I.ShowHint("Too exhausted!", 0.8f); return false; }
            Stamina -= amount; regenTimer = regenDelay;
            return true;
        }

        public void DrainStamina(float amount) { Stamina = Mathf.Max(0, Stamina - amount); regenTimer = regenDelay; }

        void Update()
        {
            regenTimer -= Time.deltaTime;
            if (regenTimer <= 0 && !Blocking) Stamina = Mathf.Min(maxStamina, Stamina + staminaRegen * Time.deltaTime);
            if (HUD.I) HUD.I.SetStamina(Stamina / maxStamina);
        }
    }
}
