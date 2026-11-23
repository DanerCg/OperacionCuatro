using System.Collections.Generic;
using UnityEngine;

namespace OperacionCuatro
{
    /// <summary>
    /// Fase 3 — TRAMPA LÁSER.
    ///
    /// Barrera con tres fases visibles: APAGADA (segura), AVISO (parpadeo rojo, todavía sin daño)
    /// y ACTIVA (haz sólido que daña). El daño pasa por <see cref="Health.TakeDamage"/>, así que
    /// respeta la invulnerabilidad temporal y las reglas de salud existentes.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class LaserTrap : MonoBehaviour
    {
        public enum Phase { Off, Warning, Active }

        [Header("Ciclo")]
        public float offTime = 1.6f;
        public float warningTime = 0.8f;
        public float activeTime = 1.4f;
        public float startOffset;

        [Header("Daño")]
        public int damage = 18;
        public float damageInterval = 0.5f;

        [Header("Presentación")]
        public SpriteRenderer beamVisual;
        public SpriteRenderer emitterVisual;
        public GameObject warningVisual;
        public Color offColor = new Color(0.3f, 0.42f, 0.5f, 0.18f);
        public Color warningColor = new Color(1f, 0.55f, 0.2f, 0.45f);
        public Color activeColor = new Color(1f, 0.24f, 0.2f, 0.95f);

        private readonly Dictionary<Health, float> nextHit = new Dictionary<Health, float>();

        private Collider2D beamCollider;
        private ContactFilter2D beamFilter;
        private readonly Collider2D[] beamHits = new Collider2D[16];

        public Phase Current { get; private set; }

        private void Awake()
        {
            beamCollider = GetComponent<Collider2D>();
            if (beamCollider != null) beamCollider.isTrigger = true;
            if (beamVisual == null) beamVisual = GetComponent<SpriteRenderer>();

            beamFilter = new ContactFilter2D();
            beamFilter.NoFilter();
            beamFilter.useTriggers = true;
        }

        /// <summary>
        /// Sondeo activo del haz mientras está ACTIVO.
        ///
        /// Un Rigidbody2D en reposo se DUERME, y un cuerpo dormido deja de recibir
        /// OnTriggerStay2D: un jugador que se quedaba quieto dentro del haz solo recibía el
        /// primer impacto y después era inmune. La consulta de solapamiento no depende del
        /// estado de reposo, así que el haz daña siempre que haya un jugador dentro.
        /// </summary>
        private void FixedUpdate()
        {
            if (Current != Phase.Active || beamCollider == null) return;

            int count = beamCollider.Overlap(beamFilter, beamHits);
            for (int i = 0; i < count; i++) TryDamage(beamHits[i]);
        }

        private void Update()
        {
            float period = Mathf.Max(0.2f, offTime + warningTime + activeTime);
            float t = Mathf.Repeat(Time.time + startOffset, period);

            if (t < offTime) Current = Phase.Off;
            else if (t < offTime + warningTime) Current = Phase.Warning;
            else Current = Phase.Active;

            float alpha;
            Color color;

            switch (Current)
            {
                case Phase.Warning:
                    // Parpadeo rápido: el aviso es imposible de confundir con el haz activo.
                    alpha = 0.55f + Mathf.PingPong(Time.time * 6f, 0.45f);
                    color = warningColor;
                    color.a = warningColor.a * alpha;
                    break;

                case Phase.Active:
                    color = activeColor;
                    break;

                default:
                    color = offColor;
                    break;
            }

            if (beamVisual != null) beamVisual.color = color;
            if (warningVisual != null) warningVisual.SetActive(Current == Phase.Warning);

            if (emitterVisual != null)
            {
                emitterVisual.color = Current == Phase.Active
                    ? new Color(1f, 0.45f, 0.4f, 1f)
                    : Current == Phase.Warning
                        ? new Color(1f, 0.7f, 0.35f, 1f)
                        : new Color(0.45f, 0.52f, 0.6f, 1f);
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryDamage(other);
        }

        private void TryDamage(Collider2D other)
        {
            // Misma guarda que SteamVent, ElectricZone y DamageOnTouch: Unity sigue entregando
            // OnTriggerStay2D a un componente desactivado, y el haz conserva su última fase.
            // Sin esta comprobación, desactivar la trampa con el haz en ACTIVO dejaba de actualizar
            // la fase pero seguía hiriendo (auditoría de Fase 3: 2 impactos de 18 en 3 s con el
            // componente desactivado).
            if (!isActiveAndEnabled || Current != Phase.Active || other == null) return;

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

            AudioManager.Sfx("sfx_laser_trap", 0.45f);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var health = other.GetComponentInParent<Health>();
            if (health != null) nextHit.Remove(health);
        }
    }
}
