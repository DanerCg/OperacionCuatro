using System.Collections;
using UnityEngine;

namespace OperacionCuatro
{
    /// <summary>
    /// Fase 3 — ENEMIGO 2: DRON LÁSER.
    ///
    /// Unidad aérea avanzada. Patrulla una franja, detecta jugadores con línea de visión y, antes
    /// de disparar, muestra un aviso luminoso (<see cref="chargeVisual"/>) durante
    /// <see cref="chargeTime"/> segundos: el haz nunca llega sin señal previa. No atraviesa
    /// paredes (raycast frontal + cuerpo cinemático) y explota al ser destruido.
    /// </summary>
    public class LaserDrone : EnemyBase
    {
        [Header("Vuelo")]
        public float hoverAmplitude = 0.5f;
        public float hoverSpeed = 1.6f;
        public float flySpeed = 2.2f;
        public float verticalGain = 4f;

        [Header("Patrulla")]
        public float patrolLeft;
        public float patrolRight;
        public bool hasPatrolBounds;
        public float patrolRange = 6f;

        [Header("Ataque")]
        public float detectionRange = 15f;
        public float fireCooldown = 2.6f;
        public float chargeTime = 0.65f;
        public int laserDamage = 14;
        public float laserSpeed = 28f;
        public Projectile laserPrefab;
        public Transform muzzle;
        public LayerMask groundMask;

        [Header("Presentación")]
        public GameObject chargeVisual;
        public GameObject deathBlastPrefab;
        public float bobVisualAmplitude = 0.05f;
        public float bobVisualSpeed = 6.5f;

        private float homeY;
        private float minX;
        private float maxX;
        private float direction = -1f;
        private float nextFireTime;
        private float flashUntil;
        private bool charging;
        private Transform visualTransform;

        /// <summary>True mientras el dron está cargando el disparo (lo usan las pruebas).</summary>
        public bool IsCharging { get { return charging; } }

        protected override void Awake()
        {
            base.Awake();

            maxHealth = 45;
            scoreValue = 300;
            contactDamage = 12;

            if (health != null) health.SetMaxHealth(maxHealth, true);

            if (body != null)
            {
                body.gravityScale = 0f;
                body.freezeRotation = true;
                // El dron se cierne en el sitio mientras carga: dormido descartaría la velocidad de
                // patrulla y se quedaría clavado en el aire.
                body.sleepMode = RigidbodySleepMode2D.NeverSleep;
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
            if (chargeVisual != null) chargeVisual.SetActive(false);
            if (spriteRenderer != null && spriteRenderer.transform != transform)
                visualTransform = spriteRenderer.transform;
        }

        private void FixedUpdate()
        {
            if (dead || body == null) return;
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;

            Transform target = FindNearestPlayer();

            float targetY = homeY + Mathf.Sin(Time.time * hoverSpeed) * hoverAmplitude;
            float vy = Mathf.Clamp((targetY - transform.position.y) * verticalGain, -flySpeed, flySpeed);

            float vx = direction * flySpeed;

            if (transform.position.x <= minX) { direction = 1f; vx = flySpeed; }
            else if (transform.position.x >= maxX) { direction = -1f; vx = -flySpeed; }

            if (WallAhead(direction)) { direction = -direction; vx = 0f; }

            if (target != null)
            {
                float dx = target.position.x - transform.position.x;
                FlipTowards(dx);

                bool inRange = Mathf.Abs(dx) <= detectionRange && HasLineOfSight(target);
                if (inRange)
                {
                    vx *= 0.18f;

                    if (!charging && Time.time >= nextFireTime)
                    {
                        nextFireTime = Time.time + fireCooldown;
                        StartCoroutine(ChargeAndFire(target));
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
            if (visualTransform == null) return;
            var local = visualTransform.localPosition;
            local.y = Mathf.Sin(Time.time * bobVisualSpeed) * bobVisualAmplitude;
            visualTransform.localPosition = local;
        }

        /// <summary>Aviso previo: el dron se ilumina y solo después dispara.</summary>
        private IEnumerator ChargeAndFire(Transform target)
        {
            charging = true;
            if (chargeVisual != null) chargeVisual.SetActive(true);
            AudioManager.Sfx("sfx_laser_charge", 0.35f);

            float until = Time.time + chargeTime;
            while (Time.time < until && !dead && target != null)
                yield return null;

            if (chargeVisual != null) chargeVisual.SetActive(false);
            charging = false;

            if (!dead && target != null) Fire(target);
        }

        private void Fire(Transform target)
        {
            if (laserPrefab == null || target == null) return;

            Vector2 origin = muzzle.position;
            Vector2 dir = (Vector2)target.position + Vector2.up * 0.6f - origin;
            if (dir.sqrMagnitude < 0.01f) dir = Vector2.right;

            var bolt = Instantiate(laserPrefab, origin, Quaternion.identity);
            bolt.hitMask = groundMask;
            bolt.Launch(dir.normalized, laserDamage, Faction.Enemy, -1);

            if (animator != null) animator.SetTrigger("Shoot");
            AudioManager.Sfx("sfx_laser", 0.45f);
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
                spriteRenderer.color = new Color(1f, 0.5f, 0.45f, 1f);
                flashUntil = Time.time + 0.12f;
            }
        }

        protected override void HandleDeath(DamageInfo info)
        {
            charging = false;
            if (chargeVisual != null) chargeVisual.SetActive(false);

            if (deathBlastPrefab != null)
            {
                var fx = Instantiate(deathBlastPrefab, transform.position, Quaternion.identity);
                Destroy(fx, 1.2f);
            }

            base.HandleDeath(info);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.5f, 0.9f, 1f, 0.5f);
            float l = hasPatrolBounds ? Mathf.Min(patrolLeft, patrolRight) : transform.position.x - patrolRange;
            float r = hasPatrolBounds ? Mathf.Max(patrolLeft, patrolRight) : transform.position.x + patrolRange;
            Gizmos.DrawLine(new Vector3(l, transform.position.y, 0f), new Vector3(r, transform.position.y, 0f));
        }
    }
}
