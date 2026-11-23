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

        private float nextHitTime;

        private void OnTriggerStay2D(Collider2D other)
        {
            if (Time.time < nextHitTime) return;
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;

            var health = other.GetComponentInParent<Health>();
            if (health == null || health.faction == faction) return;
            if (!health.IsAlive) return;

            nextHitTime = Time.time + repeatDelay;

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
    }
}
