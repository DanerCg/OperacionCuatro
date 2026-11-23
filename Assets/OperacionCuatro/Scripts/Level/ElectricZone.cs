using System.Collections.Generic;
using UnityEngine;

namespace OperacionCuatro
{
    /// <summary>
    /// Fase 2 — zona eléctrica alterna: encendida daña, apagada es segura y el estado seguro
    /// se ve claramente distinto (color apagado, sin chispas ni parpadeo).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ElectricZone : MonoBehaviour
    {
        [Header("Ciclo")]
        public float onTime = 2.2f;
        public float offTime = 1.8f;
        public float startOffset;

        [Header("Daño")]
        public int damage = 16;
        public float damageInterval = 0.5f;

        [Header("Presentación")]
        public SpriteRenderer fieldVisual;
        public GameObject sparkVisual;
        public Color onColor = new Color(0.55f, 0.85f, 1f, 0.95f);
        public Color offColor = new Color(0.2f, 0.32f, 0.42f, 0.35f);

        private readonly Dictionary<Health, float> nextHit = new Dictionary<Health, float>();

        public bool IsCharged { get; private set; }

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
            if (fieldVisual == null) fieldVisual = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            float period = Mathf.Max(0.5f, onTime + offTime);
            float t = Mathf.Repeat(Time.time + startOffset, period);

            IsCharged = t < onTime;

            if (fieldVisual != null)
            {
                fieldVisual.color = IsCharged ? onColor : offColor;
                if (IsCharged)
                {
                    float flicker = 0.75f + Mathf.PingPong(Time.time * 9f, 0.25f);
                    var c = onColor;
                    c.a *= flicker;
                    fieldVisual.color = c;
                }
            }

            if (sparkVisual != null) sparkVisual.SetActive(IsCharged);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            // Misma guarda que SteamVent: un componente desactivado sigue recibiendo los callbacks
            // de física, así que la zona debe comprobar su propio estado antes de dañar.
            if (!isActiveAndEnabled || !IsCharged) return;

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

            AudioManager.Sfx("sfx_electric", 0.45f);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var health = other.GetComponentInParent<Health>();
            if (health != null) nextHit.Remove(health);
        }
    }
}
