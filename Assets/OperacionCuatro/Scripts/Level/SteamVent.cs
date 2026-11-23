using System.Collections.Generic;
using UnityEngine;

namespace OperacionCuatro
{
    /// <summary>
    /// Fase 2 — vapor industrial: ciclo aviso → chorro activo → reposo.
    /// El aviso es visible antes del chorro para que el peligro sea anticipable.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class SteamVent : MonoBehaviour
    {
        [Header("Ciclo")]
        public float cycleTime = 4.6f;
        public float warningTime = 0.9f;
        public float activeTime = 1.5f;
        public float startOffset;

        [Header("Daño")]
        public int damage = 14;
        public float damageInterval = 0.45f;

        [Header("Presentación")]
        public GameObject warningVisual;
        public GameObject activeVisual;

        private readonly Dictionary<Health, float> nextHit = new Dictionary<Health, float>();

        public bool IsActive { get; private set; }

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void Update()
        {
            float t = Mathf.Repeat(Time.time + startOffset, Mathf.Max(0.5f, cycleTime));
            bool warning = t < warningTime;
            bool active = t >= warningTime && t < warningTime + activeTime;

            IsActive = active;

            if (warningVisual != null)
            {
                warningVisual.SetActive(warning);
                if (warning)
                {
                    float blink = Mathf.PingPong(Time.time * 6f, 1f);
                    var sr = warningVisual.GetComponentInChildren<SpriteRenderer>();
                    if (sr != null) sr.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.3f, 1f, blink));
                }
            }

            if (activeVisual != null) activeVisual.SetActive(active);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!IsActive) return;

            var health = other.GetComponentInParent<Health>();
            if (health == null || health.faction != Faction.Player || !health.IsAlive) return;

            float readyAt;
            if (nextHit.TryGetValue(health, out readyAt) && Time.time < readyAt) return;
            nextHit[health] = Time.time + damageInterval;

            health.TakeDamage(new DamageInfo
            {
                amount = damage,
                point = health.transform.position,
                direction = Vector2.up,
                source = gameObject,
                playerIndex = -1
            });

            AudioManager.Sfx("sfx_steam", 0.5f);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var health = other.GetComponentInParent<Health>();
            if (health != null) nextHit.Remove(health);
        }
    }
}
