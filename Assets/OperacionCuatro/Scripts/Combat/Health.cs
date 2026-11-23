using System;
using UnityEngine;

namespace OperacionCuatro
{
    public struct DamageInfo
    {
        public int amount;
        public Vector2 point;
        public Vector2 direction;
        public GameObject source;
        public int playerIndex;
    }

    public interface IDamageable
    {
        bool IsAlive { get; }
        Faction Faction { get; }
        void TakeDamage(DamageInfo info);
    }

    /// <summary>
    /// Punto de extensión para resistencias direccionales (Fase 2). Sin modificador asignado
    /// el comportamiento de Health es exactamente el de la Fase 1.
    /// </summary>
    public interface IDamageModifier
    {
        /// <summary>Devuelve el daño real tras aplicar resistencias o ventanas de vulnerabilidad.</summary>
        int ModifyDamage(DamageInfo info, int amount);
    }

    /// <summary>
    /// Salud genérica para jugadores y enemigos.
    /// </summary>
    public class Health : MonoBehaviour, IDamageable
    {
        public int maxHealth = 100;
        public Faction faction = Faction.Enemy;

        public int Current { get; private set; }
        public bool IsAlive { get { return Current > 0; } }
        public Faction Faction { get { return faction; } }

        public bool invulnerable;

        /// <summary>Resistencia opcional (p. ej. escudo frontal del soldado blindado).</summary>
        public IDamageModifier damageModifier;

        public float Normalized { get { return maxHealth <= 0 ? 0f : Mathf.Clamp01((float)Current / maxHealth); } }

        public event Action<DamageInfo> Damaged;
        public event Action<int, int> HealthChanged; // current, max
        public event Action<DamageInfo> Died;

        private void Awake()
        {
            Current = maxHealth;
        }

        private void OnEnable()
        {
            if (Current <= 0) Current = maxHealth;
        }

        public void ResetHealth()
        {
            Current = maxHealth;
            invulnerable = false;
            if (HealthChanged != null) HealthChanged(Current, maxHealth);
        }

        public void SetMaxHealth(int value, bool refill)
        {
            maxHealth = Mathf.Max(1, value);
            if (refill) Current = maxHealth;
            if (HealthChanged != null) HealthChanged(Current, maxHealth);
        }

        public void TakeDamage(DamageInfo info)
        {
            if (!IsAlive || invulnerable) return;
            if (info.amount <= 0) return;

            int amount = info.amount;
            if (damageModifier != null) amount = damageModifier.ModifyDamage(info, amount);
            if (amount <= 0) return;

            Current = Mathf.Max(0, Current - amount);

            if (Damaged != null) Damaged(info);
            if (HealthChanged != null) HealthChanged(Current, maxHealth);

            if (Current <= 0 && Died != null) Died(info);
        }

        public void Heal(int amount)
        {
            if (!IsAlive) return;
            Current = Mathf.Min(maxHealth, Current + Mathf.Max(0, amount));
            if (HealthChanged != null) HealthChanged(Current, maxHealth);
        }
    }
}
