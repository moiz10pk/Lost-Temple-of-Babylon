using System;
using UnityEngine;

namespace Babylon
{
    public enum DamageKind { Generic, Melee, Spin, Plunge, Sweep, Smash, Shockwave, Charge, Bite, Sting, Projectile, Core }

    public struct DamageInfo
    {
        public float amount;
        public Vector3 point;
        public Vector3 direction;
        public GameObject source;
        public DamageKind kind;
        public float knockback;

        public DamageInfo(float amount, DamageKind kind, Vector3 point, Vector3 direction, GameObject source = null, float knockback = 0f)
        {
            this.amount = amount; this.kind = kind; this.point = point;
            this.direction = direction; this.source = source; this.knockback = knockback;
        }
    }

    public interface IDamageable
    {
        void TakeDamage(DamageInfo info);
        bool IsDead { get; }
    }

    /// <summary>Shared health for the player, spiders and the Stone Master.</summary>
    public class Health : MonoBehaviour, IDamageable
    {
        public float maxHealth = 100f;
        [Tooltip("Seconds of invulnerability after taking damage.")]
        public float iFrameTime = 0f;

        /// <summary>Optional filter that can reduce or cancel damage (dodges, blocks, armour).</summary>
        public Func<DamageInfo, float> filter;

        public event Action<DamageInfo, float> Damaged;
        public event Action Died;
        public event Action<float> Changed;

        public float Current { get; private set; }
        public bool IsDead { get; private set; }
        public float Normalized => maxHealth <= 0 ? 0 : Current / maxHealth;

        float lastHitTime = -99f;

        void Awake() { Current = maxHealth; }

        public void TakeDamage(DamageInfo info)
        {
            if (IsDead || Time.time - lastHitTime < iFrameTime) return;
            float amount = filter != null ? filter(info) : info.amount;
            if (amount <= 0f) { Damaged?.Invoke(info, 0f); return; }

            lastHitTime = Time.time;
            Current = Mathf.Max(0f, Current - amount);
            Damaged?.Invoke(info, amount);
            Changed?.Invoke(Normalized);
            if (Current <= 0f) { IsDead = true; Died?.Invoke(); }
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            Current = Mathf.Min(maxHealth, Current + amount);
            Changed?.Invoke(Normalized);
        }

        public void ResetHealth()
        {
            IsDead = false; Current = maxHealth;
            Changed?.Invoke(Normalized);
        }
    }
}
