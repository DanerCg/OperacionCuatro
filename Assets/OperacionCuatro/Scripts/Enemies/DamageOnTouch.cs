using System.Collections.Generic;
using UnityEngine;

namespace OperacionCuatro
{
    /// <summary>Daño por contacto repetido a los jugadores que toquen el trigger.</summary>
    public class DamageOnTouch : MonoBehaviour
    {
        public int damage = 15;
        public float repeatDelay = 0.9f;
        public Faction faction = Faction.Enemy;
        public Vector2 knockback = new Vector2(6f, 6f);

        // Un temporizador POR víctima: con un único campo compartido, dos jugadores tocando el
        // mismo HitBox se turnaban el turno de golpe y uno de ellos quedaba sin daño.
        private readonly Dictionary<Health, float> nextHit = new Dictionary<Health, float>();

        private void OnTriggerStay2D(Collider2D other)
        {
            // Unity entrega los callbacks de física incluso a un componente desactivado: sin esta
            // guarda, un DamageOnTouch con .enabled = false seguía hiriendo (auditoría de Fase 3:
            // 2 impactos en 3 s con el componente desactivado).
            if (!isActiveAndEnabled) return;
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;

            var health = other.GetComponentInParent<Health>();
            if (health == null || health.faction == faction) return;
            if (!health.IsAlive) return;

            float readyAt;
            if (nextHit.TryGetValue(health, out readyAt) && Time.time < readyAt) return;
            nextHit[health] = Time.time + repeatDelay;

            Vector2 dir = (other.transform.position - transform.position);
            dir.y = 0f;
            dir = dir.sqrMagnitude > 0.001f ? dir.normalized : Vector2.right;

            health.TakeDamage(new DamageInfo
            {
                amount = damage,
                point = other.transform.position,
                direction = dir,
                source = gameObject,
                playerIndex = -1
            });
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var health = other.GetComponentInParent<Health>();
            if (health != null) nextHit.Remove(health);
        }
    }
}
