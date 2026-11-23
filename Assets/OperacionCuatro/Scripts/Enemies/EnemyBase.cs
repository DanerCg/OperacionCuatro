using UnityEngine;

namespace OperacionCuatro
{
    /// <summary>
    /// Base común de los enemigos: salud, puntuación y muerte.
    /// </summary>
    public class EnemyBase : MonoBehaviour
    {
        public int maxHealth = 30;
        public int scoreValue = 100;
        public int contactDamage = 15;

        public Health health;
        public Animator animator;
        public SpriteRenderer spriteRenderer;
        public GameObject deathEffectPrefab;

        protected Rigidbody2D body;
        protected float facing = -1f;
        protected bool dead;

        public bool IsDead { get { return dead; } }

        protected virtual void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            if (health == null) health = GetComponent<Health>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (health != null)
            {
                health.faction = Faction.Enemy;
                health.SetMaxHealth(maxHealth, true);
            }
        }

        protected virtual void OnEnable()
        {
            if (health == null) return;
            health.Died += HandleDeath;
            health.Damaged += HandleDamaged;
        }

        protected virtual void OnDisable()
        {
            if (health == null) return;
            health.Died -= HandleDeath;
            health.Damaged -= HandleDamaged;
        }

        protected virtual void HandleDamaged(DamageInfo info)
        {
            if (animator != null) animator.SetTrigger("Hurt");
            AudioManager.Sfx("sfx_enemy_hit", 0.5f);
        }

        protected virtual void HandleDeath(DamageInfo info)
        {
            if (dead) return;
            dead = true;

            if (info.playerIndex >= 0) GameSession.AddKill(info.playerIndex, scoreValue);
            else GameSession.Score += scoreValue;

            if (animator != null) animator.SetTrigger("Die");

            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.bodyType = RigidbodyType2D.Kinematic;
            }

            var colliders = GetComponentsInChildren<Collider2D>();
            for (int i = 0; i < colliders.Length; i++) colliders[i].enabled = false;

            if (deathEffectPrefab != null)
            {
                var fx = Instantiate(deathEffectPrefab, transform.position + Vector3.up * 0.7f, Quaternion.identity);
                Destroy(fx, 1f);
            }

            AudioManager.Sfx("sfx_enemy_death", 0.65f);

            Destroy(gameObject, 1.1f);
        }

        protected void FlipTowards(float directionX)
        {
            if (Mathf.Abs(directionX) < 0.01f) return;
            facing = Mathf.Sign(directionX);
            if (animator != null) animator.SetFloat("Facing", facing);
            if (spriteRenderer != null) spriteRenderer.flipX = facing < 0f;
        }
    }
}
