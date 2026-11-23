using UnityEngine;

namespace OperacionCuatro
{
    /// <summary>
    /// Proyectil recto (balas de jugador y de enemigo).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Projectile : MonoBehaviour
    {
        public int damage = 10;
        public float speed = 20f;
        public float lifetime = 2.5f;
        public Faction faction = Faction.Player;
        public LayerMask hitMask;
        public GameObject impactPrefab;
        public int ownerPlayerIndex = -1;

        private Rigidbody2D body;
        private float dieAt;
        private bool launched;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        public void Launch(Vector2 direction, int dmg, Faction who, int playerIndex = -1)
        {
            damage = dmg;
            faction = who;
            ownerPlayerIndex = playerIndex;

            if (direction.sqrMagnitude < 0.0001f) direction = Vector2.right;
            direction.Normalize();

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            body.linearVelocity = direction * speed;
            dieAt = Time.time + lifetime;
            launched = true;
        }

        private void Update()
        {
            if (launched && Time.time >= dieAt) Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!launched) return;

            // Ignorar el propio emisor
            if (other.transform.IsChildOf(transform)) return;

            var health = other.GetComponentInParent<Health>();
            if (health != null)
            {
                if (health.faction == faction) return;

                health.TakeDamage(new DamageInfo
                {
                    amount = damage,
                    point = transform.position,
                    direction = body != null ? body.linearVelocity.normalized : Vector2.right,
                    source = gameObject,
                    playerIndex = ownerPlayerIndex
                });

                SpawnImpact(transform.position, -transform.right);
                Destroy(gameObject);
                return;
            }

            // Impacto contra el escenario
            if (!other.isTrigger)
            {
                SpawnImpact(transform.position, -transform.right);
                Destroy(gameObject);
            }
        }

        private void SpawnImpact(Vector3 position, Vector3 normal)
        {
            if (impactPrefab != null)
            {
                var go = Instantiate(impactPrefab, position, Quaternion.identity);
                Destroy(go, 0.4f);
            }
        }
    }
}
