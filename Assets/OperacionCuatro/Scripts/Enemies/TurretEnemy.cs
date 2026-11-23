using UnityEngine;

namespace OperacionCuatro
{
    /// <summary>
    /// Fase 2 — ENEMIGO C: TORRETA AUTOMÁTICA.
    ///
    /// Permanece fija, vigila un cono de visión, apunta con el cañón y dispara ráfagas.
    /// La cadencia (intervalo entre ráfagas, tamaño y separación de la ráfaga) es configurable.
    /// Destruible con armas y granadas; muestra destello al recibir daño.
    /// </summary>
    public class TurretEnemy : EnemyBase
    {
        [Header("Detección")]
        public float detectionRange = 14f;
        [Tooltip("Apertura total del cono de visión, en grados, alrededor del eje de mira.")]
        public float fieldOfView = 110f;
        public float aimHeight = 0.8f;
        public LayerMask groundMask;

        [Header("Disparo")]
        public Projectile bulletPrefab;
        public Transform muzzle;
        public Transform barrel;
        public int burstSize = 3;
        public float burstInterval = 0.15f;
        public float fireCooldown = 2.3f;
        public int bulletDamage = 12;

        [Header("Presentación")]
        public GameObject muzzleFlashPrefab;
        public GameObject hitSparkPrefab;

        private float nextFireTime;
        private float nextBurstShotTime;
        private int burstRemaining;
        private float flashUntil;
        private Transform target;
        private float aimX = 1f;

        protected override void Awake()
        {
            base.Awake();

            maxHealth = 50;
            scoreValue = 300;
            contactDamage = 0;

            if (health != null) health.SetMaxHealth(maxHealth, true);
            if (muzzle == null) muzzle = transform;
        }

        private void Update()
        {
            if (dead) return;
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;

            if (flashUntil > 0f && Time.time > flashUntil && spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
                flashUntil = 0f;
            }

            target = FindTargetInView();

            if (target != null)
            {
                Vector2 dir = ((Vector2)target.position - (Vector2)transform.position).normalized;
                aimX = Mathf.Sign(dir.x == 0f ? 1f : dir.x);

                if (barrel != null)
                {
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    barrel.rotation = Quaternion.Euler(0f, 0f, angle);
                }

                if (burstRemaining <= 0 && Time.time >= nextFireTime)
                {
                    burstRemaining = Mathf.Max(1, burstSize);
                    nextBurstShotTime = Time.time;
                }
            }

            UpdateBurst();
        }

        private void UpdateBurst()
        {
            if (burstRemaining <= 0) return;
            if (Time.time < nextBurstShotTime) return;

            FireOnce();
            burstRemaining--;

            if (burstRemaining <= 0) nextFireTime = Time.time + fireCooldown;
            else nextBurstShotTime = Time.time + burstInterval;
        }

        private void FireOnce()
        {
            if (bulletPrefab == null) return;

            Vector2 dir = target != null
                ? ((Vector2)target.position - (Vector2)muzzle.position).normalized
                : new Vector2(facing, 0f);

            if (dir.sqrMagnitude < 0.01f) dir = new Vector2(facing, 0f);

            var bullet = Instantiate(bulletPrefab, muzzle.position, Quaternion.identity);
            bullet.hitMask = groundMask;
            bullet.Launch(dir, bulletDamage, Faction.Enemy, -1);

            if (muzzleFlashPrefab != null)
            {
                var fx = Instantiate(muzzleFlashPrefab, muzzle.position,
                    Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg));
                Destroy(fx, 0.09f);
            }

            AudioManager.Sfx("sfx_turret", 0.5f);
        }

        private Transform FindTargetInView()
        {
            if (LevelManager.Instance == null) return null;

            float halfAngle = Mathf.Clamp(fieldOfView, 10f, 360f) * 0.5f;
            Vector2 eye = (Vector2)transform.position + new Vector2(0f, aimHeight);

            Transform best = null;
            float bestDist = float.MaxValue;

            var players = LevelManager.Instance.Players;
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (p == null || p.IsDead) continue;

                Vector2 toPlayer = (Vector2)p.transform.position - eye;
                float dist = toPlayer.magnitude;
                if (dist > detectionRange || dist < 0.01f) continue;

                // Cono de visión alrededor del eje de mira actual de la torreta.
                Vector2 forward = new Vector2(aimX, 0f);
                float angle = Vector2.Angle(forward, toPlayer.normalized);
                if (angle > halfAngle) continue;

                if (!HasLineOfSight(eye, p.transform.position)) continue;

                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = p.transform;
                }
            }

            return best;
        }

        private bool HasLineOfSight(Vector2 from, Vector2 to)
        {
            if (groundMask == 0) return true;
            var hit = Physics2D.Raycast(from, (to - from).normalized, Vector2.Distance(from, to), groundMask);
            return hit.collider == null;
        }

        protected override void HandleDamaged(DamageInfo info)
        {
            base.HandleDamaged(info);

            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(1f, 0.5f, 0.4f, 1f);
                flashUntil = Time.time + 0.12f;
            }

            if (hitSparkPrefab != null)
            {
                var spark = Instantiate(hitSparkPrefab, info.point, Quaternion.identity);
                Destroy(spark, 0.4f);
            }
        }
    }
}
