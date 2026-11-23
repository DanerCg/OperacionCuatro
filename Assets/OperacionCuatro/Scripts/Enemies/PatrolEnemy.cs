using UnityEngine;

namespace OperacionCuatro
{
    /// <summary>Enemigo patrullero que recorre plataformas y daña al contacto.</summary>
    public class PatrolEnemy : EnemyBase
    {
        public float speed = 2.1f;
        public LayerMask groundMask;
        public float edgeCheckDistance = 0.9f;
        public float wallCheckDistance = 0.55f;

        private float stuckTimer;

        protected override void Awake()
        {
            base.Awake();
            maxHealth = 30;
            scoreValue = 100;
            contactDamage = 15;
            if (health != null) health.SetMaxHealth(maxHealth, true);
        }

        private void FixedUpdate()
        {
            if (dead || body == null) return;
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;

            Vector2 origin = transform.position;

            bool wall = Physics2D.Raycast(origin + new Vector2(facing * 0.45f, 0.75f), Vector2.right * facing, wallCheckDistance, groundMask).collider != null;
            bool noGround = Physics2D.Raycast(origin + new Vector2(facing * 0.55f, 0.15f), Vector2.down, edgeCheckDistance, groundMask).collider == null;

            if (wall || noGround) Turn();

            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer > 3.5f) { Turn(); stuckTimer = 0f; }

            body.linearVelocity = new Vector2(facing * speed, body.linearVelocity.y);

            if (animator != null) animator.SetFloat("Speed", Mathf.Abs(facing * speed));
        }

        private void Turn()
        {
            facing = -facing;
            stuckTimer = 0f;
            if (spriteRenderer != null) spriteRenderer.flipX = facing < 0f;
        }
    }
}
