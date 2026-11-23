using UnityEngine;

namespace OperacionCuatro
{
    /// <summary>
    /// Fase 3 — ENEMIGO 3: SOLDADO EXPLOSIVO.
    ///
    /// Mantiene una distancia de combate (ni cuerpo a cuerpo ni fuera de alcance), lanza granadas
    /// en trayectoria parabólica calculada con la gravedad real del proyecto y, tras cada lanzamiento,
    /// retrocede a cubierto durante <see cref="coverTime"/>. Reutiliza <see cref="Bomb"/> (mecha,
    /// explosión en área y facción) para no duplicar el sistema de explosivos.
    /// </summary>
    public class GrenadierEnemy : EnemyBase
    {
        [Header("Movimiento")]
        public float moveSpeed = 1.6f;
        public float detectionRange = 17f;
        public float minRange = 5.5f;
        public float maxRange = 12f;
        public float patrolRange = 4f;

        [Header("Granadas")]
        public Bomb grenadePrefab;
        public Transform throwPoint;
        public float throwCooldown = 3.2f;
        public float throwVertical = 11f;
        public float maxThrowSpeed = 13f;
        public float coverTime = 0.9f;

        [Header("Sensores")]
        public LayerMask groundMask;

        private float minX;
        private float maxX;
        private float direction = -1f;
        private float nextThrowTime;
        private float retreatUntil;
        private float flashUntil;

        /// <summary>Granadas lanzadas desde que existe (lo usan las pruebas).</summary>
        public int ThrownCount { get; private set; }

        protected override void Awake()
        {
            base.Awake();

            maxHealth = 40;
            scoreValue = 250;
            contactDamage = 12;

            if (health != null) health.SetMaxHealth(maxHealth, true);

            // El cuerpo entra en reposo cuando lleva un segundo sin moverse y, dormido, descarta las
            // velocidades que le asigna FixedUpdate: el granadero lanzaba pero nunca retrocedía y
            // tampoco reanudaba la patrulla. Nunca debe dormirse.
            if (body != null) body.sleepMode = RigidbodySleepMode2D.NeverSleep;

            minX = transform.position.x - patrolRange;
            maxX = transform.position.x + patrolRange;

            if (throwPoint == null) throwPoint = transform;
        }

        private void FixedUpdate()
        {
            if (dead || body == null) return;
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;

            Transform target = FindNearestPlayer();
            float vx;

            if (target == null)
            {
                vx = Patrol();
            }
            else
            {
                float dx = target.position.x - transform.position.x;
                float dist = Mathf.Abs(dx);
                int sign = dx >= 0f ? 1 : -1;

                FlipTowards(dx);

                if (Time.time < retreatUntil)
                {
                    // Retroceso a cubierto: se aleja del jugador tras lanzar. Si tiene un obstáculo
                    // detrás se queda cubierto donde está en lugar de empujar contra la pared.
                    vx = PathBlocked(-sign) ? 0f : -sign * moveSpeed;
                }
                else if (dist < minRange)
                {
                    vx = -sign * moveSpeed;
                }
                else if (dist > maxRange)
                {
                    vx = sign * moveSpeed;
                }
                else
                {
                    vx = 0f;

                    if (Time.time >= nextThrowTime)
                    {
                        nextThrowTime = Time.time + throwCooldown;
                        Throw(target);
                    }
                }
            }

            body.linearVelocity = new Vector2(vx, body.linearVelocity.y);
            if (animator != null) animator.SetFloat("Speed", Mathf.Abs(vx));

            if (flashUntil > 0f && Time.time > flashUntil && spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
                flashUntil = 0f;
            }
        }

        private float Patrol()
        {
            float vx = direction * moveSpeed * 0.6f;

            if (transform.position.x <= minX) { direction = 1f; vx = moveSpeed * 0.6f; }
            else if (transform.position.x >= maxX) { direction = -1f; vx = -moveSpeed * 0.6f; }
            else if (PathBlocked(direction)) { direction = -direction; vx = direction * moveSpeed * 0.6f; }

            FlipTowards(vx);
            return vx;
        }

        /// <summary>
        /// True si hay terreno sólido justo delante en esa dirección. Sin esta comprobación un
        /// obstáculo dentro del carril de patrulla detenía al granadero para siempre (los topes solo
        /// invertían la marcha al alcanzar minX/maxX, que nunca llegaba a tocar).
        /// </summary>
        private bool PathBlocked(float dir)
        {
            if (Mathf.Abs(dir) < 0.01f || groundMask == 0) return false;

            Vector2 origin = (Vector2)transform.position + Vector2.up * 0.55f;
            return Physics2D.Raycast(origin, Vector2.right * Mathf.Sign(dir), 0.6f, groundMask).collider != null;
        }

        /// <summary>
        /// Velocidad parabólica calculada con la gravedad real: tiempo de vuelo t = 2·v0/g y
        /// velocidad horizontal d/t. Así el lanzamiento es alcanzable y siempre visible.
        /// </summary>
        private void Throw(Transform target)
        {
            if (grenadePrefab == null || target == null) return;

            Vector3 origin = throwPoint.position;

            float g = Mathf.Abs(Physics2D.gravity.y);
            if (g < 0.01f) g = 30f;

            float v0 = Mathf.Clamp(throwVertical, 4f, 18f);
            float flight = 2f * v0 / g;
            float dx = target.position.x - origin.x;
            float vx = Mathf.Clamp(dx / Mathf.Max(0.15f, flight), -maxThrowSpeed, maxThrowSpeed);

            var grenade = Instantiate(grenadePrefab, origin, Quaternion.identity);
            grenade.faction = Faction.Enemy;
            grenade.Throw(new Vector2(vx, v0), Faction.Enemy, -1);

            ThrownCount++;
            retreatUntil = Time.time + coverTime;

            if (animator != null) animator.SetTrigger("Throw");
            AudioManager.Sfx("sfx_bomb_throw", 0.5f);
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
                spriteRenderer.color = new Color(1f, 0.58f, 0.42f, 1f);
                flashUntil = Time.time + 0.12f;
            }
        }
    }
}
