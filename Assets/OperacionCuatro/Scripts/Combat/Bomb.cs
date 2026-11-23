using UnityEngine;

namespace OperacionCuatro
{
    /// <summary>
    /// Granada con trayectoria parabólica, mecha y explosión en área.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Bomb : MonoBehaviour
    {
        public float fuseTime = 1.6f;
        public int damage = 45;
        public float radius = 2.6f;
        public GameObject explosionPrefab;
        public LayerMask damageMask;
        public Faction faction = Faction.Player;
        public int ownerPlayerIndex = -1;

        private Rigidbody2D body;
        private float explodeAt;
        private bool exploded;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = false;
        }

        public void Throw(Vector2 velocity, Faction who, int playerIndex)
        {
            faction = who;
            ownerPlayerIndex = playerIndex;
            body.linearVelocity = velocity;
            body.angularVelocity = Random.Range(-220f, 220f);
            explodeAt = Time.time + fuseTime;
        }

        private void Update()
        {
            if (!exploded && Time.time >= explodeAt) Explode();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (exploded) return;

            var health = collision.collider.GetComponentInParent<Health>();
            if (health != null && health.faction != faction)
            {
                Explode();
            }
        }

        private void Explode()
        {
            if (exploded) return;
            exploded = true;

            if (explosionPrefab != null)
            {
                var fx = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
                Destroy(fx, 1.2f);
            }

            var hits = Physics2D.OverlapCircleAll(transform.position, radius, damageMask);
            for (int i = 0; i < hits.Length; i++)
            {
                var health = hits[i].GetComponentInParent<Health>();
                if (health == null || health.faction == faction) continue;

                health.TakeDamage(new DamageInfo
                {
                    amount = damage,
                    point = transform.position,
                    direction = ((Vector2)hits[i].transform.position - (Vector2)transform.position).normalized,
                    source = gameObject,
                    playerIndex = ownerPlayerIndex
                });
            }

            var ownHealth = GetComponent<Health>();
            if (ownHealth != null) ownHealth.TakeDamage(new DamageInfo { amount = 9999 });

            AudioManager.Sfx("sfx_explosion", 0.9f);
            Destroy(gameObject);
        }
    }
}
