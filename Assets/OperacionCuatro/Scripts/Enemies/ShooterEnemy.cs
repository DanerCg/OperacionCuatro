using UnityEngine;

namespace OperacionCuatro
{
    /// <summary>Enemigo a distancia: se detiene a disparar cuando detecta a un jugador.</summary>
    public class ShooterEnemy : EnemyBase
    {
        public float detectionRange = 16f;
        public float minRange = 3.5f;
        public float fireCooldown = 1.7f;
        public float patrolSpeed = 1.1f;
        public Projectile bulletPrefab;
        public Transform muzzle;
        public LayerMask groundMask;

        private float nextFireTime;
        private Transform currentTarget;

        protected override void Awake()
        {
            base.Awake();
            maxHealth = 40;
            scoreValue = 150;
            contactDamage = 12;
            if (health != null) health.SetMaxHealth(maxHealth, true);
            if (muzzle == null) muzzle = transform;
        }

        private void FixedUpdate()
        {
            if (dead || body == null) return;
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;

            currentTarget = FindNearestPlayer();

            if (currentTarget == null)
            {
                if (animator != null) animator.SetFloat("Speed", patrolSpeed);
                return;
            }

            float dx = currentTarget.position.x - transform.position.x;
            float dist = Mathf.Abs(dx);

            FlipTowards(dx);

            if (dist > minRange && dist < detectionRange)
            {
                // Se acerca lentamente para entrar en rango
                body.linearVelocity = new Vector2(Mathf.Sign(dx) * patrolSpeed, body.linearVelocity.y);
                if (animator != null) animator.SetFloat("Speed", patrolSpeed);
            }
            else
            {
                body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
                if (animator != null) animator.SetFloat("Speed", 0f);

                if (dist <= detectionRange && Time.time >= nextFireTime)
                {
                    nextFireTime = Time.time + fireCooldown;
                    Fire(dx);
                }
            }
        }

        private void Fire(float dx)
        {
            if (bulletPrefab == null) return;

            Vector2 dir = new Vector2(Mathf.Sign(dx), 0.06f).normalized;
            var bullet = Instantiate(bulletPrefab, MuzzlePosition(dx), Quaternion.identity);
            bullet.hitMask = groundMask;
            bullet.Launch(dir, 12, Faction.Enemy, -1);

            if (animator != null) animator.SetTrigger("Shoot");
            AudioManager.Sfx("sfx_enemy_shoot", 0.4f);
        }

        /// <summary>
        /// Punto del cañón en el lado hacia el que se dispara. Muzzle es hijo de la raíz y no se
        /// voltea con el sprite, así que sin esto la bala nacería detrás del enemigo al mirar a la izquierda.
        /// </summary>
        private Vector3 MuzzlePosition(float dx)
        {
            Vector3 p = muzzle.position;
            float offset = p.x - transform.position.x;
            if (Mathf.Abs(offset) < 0.001f) return p;

            bool muzzleOnRight = offset > 0f;
            bool fireToRight = dx >= 0f;
            if (muzzleOnRight != fireToRight) p.x = transform.position.x - offset;
            return p;
        }

        private Transform FindNearestPlayer()
        {
            if (LevelManager.Instance == null) return null;

            Transform best = null;
            float bestDist = float.MaxValue;
            var players = LevelManager.Instance.Players;

            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (p == null || p.IsDead) continue;

                float d = Vector2.Distance(p.transform.position, transform.position);
                if (d < bestDist && d <= detectionRange)
                {
                    bestDist = d;
                    best = p.transform;
                }
            }

            return best;
        }
    }
}
