using System.Collections.Generic;
using UnityEngine;

namespace OperacionCuatro
{
    /// <summary>
    /// Fase 3 — UNIDAD DE ALIMENTACIÓN.
    ///
    /// Objetivo secundario de la CÁMARA DE ENERGÍA. Es destructible con balas y bombas a través
    /// del <see cref="Health"/> normal, nunca desaparece (queda como testigo apagado) y al caer
    /// desbloquea las puertas de su canal y puede disparar la secuencia de escape.
    /// </summary>
    public class PowerUnit : MonoBehaviour
    {
        public static readonly List<PowerUnit> All = new List<PowerUnit>();

        [Header("Identidad")]
        public string channel = "A";
        public int scoreValue = 400;

        [Header("Recursos")]
        public Health health;
        public GameObject explosionPrefab;

        [Header("Presentación")]
        public GameObject onlineVisual;
        public GameObject offlineVisual;
        public SpriteRenderer coreVisual;
        public Color onlineColor = new Color(0.35f, 0.92f, 1f, 1f);
        public Color offlineColor = new Color(0.28f, 0.3f, 0.34f, 1f);

        public bool IsDestroyed { get; private set; }

        private void Awake()
        {
            if (health == null) health = GetComponent<Health>();

            if (health != null)
            {
                health.faction = Faction.Enemy;
                health.Died += HandleDied;
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
            if (health != null) health.Died -= HandleDied;
        }

        private void HandleDied(DamageInfo info)
        {
            if (IsDestroyed) return;
            IsDestroyed = true;

            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            if (info.playerIndex >= 0) GameSession.AddKill(info.playerIndex, scoreValue);
            else GameSession.Score += scoreValue;

            RefreshVisuals();

            if (explosionPrefab != null)
            {
                var fx = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
                Destroy(fx, 1.2f);
            }

            AudioManager.Sfx("sfx_explosion", 0.8f);

            if (LevelManager.Instance != null && LevelManager.Instance.hud != null)
            {
                LevelManager.Instance.hud.ShowMessage(
                    "UNIDAD DE ALIMENTACIÓN DESTRUIDA  " + DestroyedTotal() + "/" + TotalUnits);
            }
        }

        private void RefreshVisuals()
        {
            if (onlineVisual != null) onlineVisual.SetActive(!IsDestroyed);
            if (offlineVisual != null) offlineVisual.SetActive(IsDestroyed);
            if (coreVisual != null) coreVisual.color = IsDestroyed ? offlineColor : onlineColor;
        }

        /// <summary>Unidades destruidas en un canal concreto.</summary>
        public static int DestroyedInChannel(string channel)
        {
            int count = 0;
            for (int i = 0; i < All.Count; i++)
            {
                var u = All[i];
                if (u != null && u.IsDestroyed && u.channel == channel) count++;
            }
            return count;
        }

        /// <summary>Unidades destruidas en total.</summary>
        public static int DestroyedTotal()
        {
            int count = 0;
            for (int i = 0; i < All.Count; i++)
            {
                if (All[i] != null && All[i].IsDestroyed) count++;
            }
            return count;
        }

        /// <summary>Unidades presentes en la escena.</summary>
        public static int TotalUnits
        {
            get
            {
                int count = 0;
                for (int i = 0; i < All.Count; i++) if (All[i] != null) count++;
                return count;
            }
        }
    }
}
