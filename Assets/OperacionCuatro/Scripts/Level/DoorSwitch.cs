using System.Collections.Generic;
using UnityEngine;

namespace OperacionCuatro
{
    /// <summary>
    /// Fase 3 — INTERRUPTOR DE SEGURIDAD.
    ///
    /// Se activa disparándole: reutiliza el canal de daño existente (el proyectil busca un
    /// <see cref="Health"/> en el padre), así que no hace falta lógica nueva de impactos.
    /// Al recibir <see cref="hitsRequired"/> impactos queda fijado, se vuelve invulnerable y
    /// desbloquea las puertas de su mismo <see cref="channel"/>.
    /// </summary>
    public class DoorSwitch : MonoBehaviour
    {
        public static readonly List<DoorSwitch> All = new List<DoorSwitch>();

        [Header("Identidad")]
        public string channel = "A";

        [Header("Activación")]
        [Tooltip("Impactos necesarios para fijar el interruptor.")]
        public int hitsRequired = 2;
        public int scoreValue = 50;

        [Header("Presentación")]
        public Health health;
        public GameObject inactiveVisual;
        public GameObject activeVisual;
        public GameObject activationEffect;

        public bool IsActive { get; private set; }
        public int Hits { get; private set; }

        private void Awake()
        {
            if (health == null) health = GetComponentInChildren<Health>();

            if (health != null)
            {
                health.faction = Faction.Enemy;
                health.Damaged += HandleDamaged;
            }

            RefreshVisuals();
        }

        private void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
        }

        private void OnDisable()
        {
            All.Remove(this);
        }

        private void OnDestroy()
        {
            if (health != null) health.Damaged -= HandleDamaged;
        }

        private void HandleDamaged(DamageInfo info)
        {
            if (IsActive) return;

            Hits++;
            if (Hits >= Mathf.Max(1, hitsRequired)) Activate();
        }

        /// <summary>Fija el interruptor (también lo usan las pruebas).</summary>
        public void Activate()
        {
            if (IsActive) return;

            IsActive = true;
            if (health != null) health.invulnerable = true;

            RefreshVisuals();

            GameSession.Score += scoreValue;
            AudioManager.Sfx("sfx_switch", 0.6f);

            if (activationEffect != null)
            {
                var fx = Instantiate(activationEffect, transform.position, Quaternion.identity);
                Destroy(fx, 1f);
            }
        }

        public void ResetSwitch()
        {
            IsActive = false;
            Hits = 0;
            if (health != null) health.invulnerable = false;
            RefreshVisuals();
        }

        private void RefreshVisuals()
        {
            if (inactiveVisual != null) inactiveVisual.SetActive(!IsActive);
            if (activeVisual != null) activeVisual.SetActive(IsActive);
        }

        /// <summary>Interruptores fijados en un canal.</summary>
        public static int ActiveInChannel(string channel)
        {
            int count = 0;
            for (int i = 0; i < All.Count; i++)
            {
                var s = All[i];
                if (s != null && s.IsActive && s.channel == channel) count++;
            }
            return count;
        }

        public static int TotalInChannel(string channel)
        {
            int count = 0;
            for (int i = 0; i < All.Count; i++)
            {
                var s = All[i];
                if (s != null && s.channel == channel) count++;
            }
            return count;
        }
    }
}
