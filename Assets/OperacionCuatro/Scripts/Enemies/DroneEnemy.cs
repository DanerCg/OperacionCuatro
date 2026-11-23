using UnityEngine;

namespace OperacionCuatro
{
    /// <summary>
    /// Fase 2 — ENEMIGO B: DRON MILITAR.
    ///
    /// Vuela, patrulla una zona delimitada por <see cref="patrolLeft"/>/<see cref="patrolRight"/>,
    /// detecta jugadores con línea de visión y dispara. No atraviesa paredes: el cuerpo es un
    /// Rigidbody2D dinámico sin gravedad y además comprueba la pared que tiene delante.
    /// </summary>
    public class DroneEnemy : EnemyBase
    {
        [Header("Vuelo")]
        public float hoverAmplitude = 0.45f;
        public float hoverSpeed = 1.7f;
        public float flySpeed = 2.4f;
        public float verticalGain = 4.5f;

        [Header("Patrulla")]
        public float patrolLeft = 0f;
        public float patrolRight = 0f;
        public bool hasPatrolBounds;
        public float patrolRange = 6f;

        [Header("Ataque")]
        public float detectionRange = 13f;
        public float fireCooldown = 1.9f;
        public Projectile bulletPrefab;
        public Transform muzzle;
        public LayerMask groundMask;

        [Header("Presentación")]
        public float bobVisualAmplitude = 0.06f;
        public float bobVisualSpeed = 6f;

        private float homeY;
        private float minX;
        private float maxX;
        private float direction = -1f;
        private float nextFireTime;
        private float flashUntil;
        private Transform visualTransform;

        protected override void Awake()
        {
            base.Awake();

            maxHealth = 35;
            scoreValue = 200;
            contactDamage = 10;

            if (health != null) health.SetMaxHealth(maxHealth, true);

            if (body != null)
            {
                body.gravityScale = 0f;
                body.freezeRotation = true;
            }

            homeY = transform.position.y;

            if (!hasPatrolBounds)
            {
                minX = transform.position.x - patrolRange;
                maxX = transform.position.x + patrolRange;
            }
            else
            {
                minX = Mathf.Min(patrolLeft, patrolRight);
                maxX = Mathf.Max(patrolLeft, patrolRight);
            }

            if (muzzle == null) muzzle = transform;
            if (spriteRenderer != null && spriteRenderer.transform != transform) visualTransform = spriteRenderer.transform;
        }

        private void FixedUpdate()
        {
            if (dead || body == null) return;
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;

            Transform target = FindNearestPlayer();

            // ---- Altura: flota alrededor de su altura de origen ----
            float targetY = homeY + Mathf.Sin(Time.time * hoverSpeed) * hoverAmplitude;
            float vy = Mathf.Clamp((targetY - transform.position.y) * verticalGain, -flySpeed, flySpeed);

            // ---- Horizontal: patrulla y respeta paredes ----
            float vx = direction * flySpeed;

            if (transform.position.x <= minX) { direction = 1f; vx = flySpeed; }
            else if (transform.position.x >= maxX) { direction = -1f; vx = -flySpeed; }

            if (WallAhead(direction))
            {
                direction = -direction;
                vx = 0f;
            }

            if (target != null)
            {
                float dx = target.position.x - transform.position.x;
                FlipTowards(dx);

                // Se detiene a disparar cuando tiene al jugador a la vista.
                if (Mathf.Abs(dx) <= detectionRange && HasLineOfSight(target))
                {
                    vx *= 0.15f;

                    if (Time.time >= nextFireTime)
                    {
                        nextFireTime = Time.time + fireCooldown;
                        FireAt(target);
                    }
                }
            }

            body.linearVelocity = new Vector2(vx, vy);

            if (animator != null) animator.SetFloat("Speed", Mathf.Abs(vx));

            if (flashUntil > 0f && Time.time > flashUntil && spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
                flashUntil = 0f;
            }
        }

        private void Update()
        {
            // Balanceo visual del sprite (no afecta a la física ni a la posición del enemigo).
            if (visualTransform == null) return;
            var local = visualTransform.localPosition;
            local.y = Mathf.Sin(Time.time * bobVisualSpeed) * bobVisualAmplitude;
            visualTransform.localPosition = local;
        }

        private bool WallAhead(float dir)
        {
            if (groundMask == 0) return false;

            Vector2 origin = transform.position;
            return Physics2D.Raycast(origin, Vector2.right * dir, 0.85f, groundMask).collider != null;
        }

        private bool HasLineOfSight(Transform target)
        {
            if (groundMask == 0) return true;

            Vector2 from = transform.position;
            Vector2 to = target.position;
            var hit = Physics2D.Raycast(from, (to - from).normalized, Vector2.Distance(from, to), groundMask);
            return hit.collider == null;
        }

        private void FireAt(Transform target)
        {
            if (bulletPrefab == null || target == null) return;

            Vector2 dir = ((Vector2)target.position - (Vector2)muzzle.position);
            if (dir.sqrMagnitude < 0.01f) dir = Vector2.right;

            var bullet = Instantiate(bulletPrefab, muzzle.position, Quaternion.identity);
            bullet.hitMask = groundMask;
            bullet.Launch(dir.normalized, 11, Faction.Enemy, -1);

            if (animator != null) animator.SetTrigger("Shoot");
            AudioManager.Sfx("sfx_drone", 0.5f);
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

        protected override void HandleDamaged(DamageInfo info)
        {
            base.HandleDamaged(info);

            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(1f, 0.55f, 0.45f, 1f);
                flashUntil = Time.time + 0.12f;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.4f, 0.9f, 1f, 0.5f);
            float l = hasPatrolBounds ? Mathf.Min(patrolLeft, patrolRight) : transform.position.x - patrolRange;
            float r = hasPatrolBounds ? Mathf.Max(patrolLeft, patrolRight) : transform.position.x + patrolRange;
            Gizmos.DrawLine(new Vector3(l, transform.position.y, 0f), new Vector3(r, transform.position.y, 0f));
        }
    }
}
