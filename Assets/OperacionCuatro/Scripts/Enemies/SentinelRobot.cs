using System.Collections;
using UnityEngine;

namespace OperacionCuatro
{
    /// <summary>
    /// Fase 3 — ENEMIGO 1: ROBOT CENTINELA.
    ///
    /// Unidad terrestre blindada. Patrulla un tramo del corredor, detecta jugadores con línea de
    /// visión, se acerca hasta su distancia de parada y dispara ráfagas. Reutiliza EnemyBase
    /// (salud, puntuación, muerte) y el proyectil de la Fase 2: no duplica lógica de daño.
    /// </summary>
    public class SentinelRobot : EnemyBase
    {
        [Header("Patrulla")]
        public float patrolLeft;
        public float patrolRight;
        public bool hasPatrolBounds;
        public float patrolRange = 5f;
        public float moveSpeed = 1.35f;

        [Header("Combate")]
        public float detectionRange = 16f;
        public float stopRange = 7f;
        public float fireCooldown = 2f;
        public int burstSize = 3;
        public float burstInterval = 0.14f;
        public int bulletDamage = 13;
        public Projectile bulletPrefab;
        public Transform muzzle;
        public LayerMask groundMask;

        private float minX;
        private float maxX;
        private float direction = -1f;
        private float nextFireTime;
        private float flashUntil;
        private bool bursting;

        protected override void Awake()
        {
            base.Awake();

            maxHealth = 75;
            scoreValue = 350;
            contactDamage = 16;

            if (health != null) health.SetMaxHealth(maxHealth, true);

            // Evita que el cuerpo se duerma mientras está parado en stopRange disparando: dormido
            // descartaría la velocidad con la que debe reanudar el avance.
            if (body != null) body.sleepMode = RigidbodySleepMode2D.NeverSleep;

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
        }

        private void FixedUpdate()
        {
            if (dead || body == null) return;
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;

            Transform target = FindNearestPlayer();
            float vx = 0f;

            bool engaged = target != null &&
                           Mathf.Abs(target.position.x - transform.position.x) <= detectionRange &&
                           HasLineOfSight(target);

            if (engaged)
            {
                float dx = target.position.x - transform.position.x;
                FlipTowards(dx);

                if (Mathf.Abs(dx) > stopRange)
                {
                    vx = Mathf.Sign(dx) * moveSpeed;
                }
                else if (!bursting && Time.time >= nextFireTime)
                {
                    nextFireTime = Time.time + fireCooldown;
                    StartCoroutine(Burst());
                }
            }
            else
            {
                vx = direction * moveSpeed;

                if (transform.position.x <= minX) { direction = 1f; vx = moveSpeed; }
                else if (transform.position.x >= maxX) { direction = -1f; vx = -moveSpeed; }
                else if (PathBlocked(direction)) { direction = -direction; vx = direction * moveSpeed; }

                FlipTowards(vx);
            }

            body.linearVelocity = new Vector2(vx, body.linearVelocity.y);
            if (animator != null) animator.SetFloat("Speed", Mathf.Abs(vx));

            if (flashUntil > 0f && Time.time > flashUntil && spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
                flashUntil = 0f;
            }
        }

        /// <summary>
        /// True si hay terreno sólido justo delante en esa dirección. Evita que un obstáculo dentro
        /// del carril de patrulla deje al centinela empujando contra él indefinidamente: los topes
        /// solo invertían la marcha al llegar a minX/maxX, que así nunca se alcanzaban.
        /// </summary>
        private bool PathBlocked(float dir)
        {
            if (Mathf.Abs(dir) < 0.01f || groundMask == 0) return false;

            Vector2 origin = (Vector2)transform.position + Vector2.up * 0.75f;
            return Physics2D.Raycast(origin, Vector2.right * Mathf.Sign(dir), 0.75f, groundMask).collider != null;
        }

        private IEnumerator Burst()
        {
            bursting = true;

            for (int i = 0; i < burstSize; i++)
            {
                if (dead) break;
                Fire();
                yield return new WaitForSeconds(burstInterval);
            }

            bursting = false;
        }

        private void Fire()
        {
            if (bulletPrefab == null) return;

            Vector3 spawn = MuzzlePosition();
            var bullet = Instantiate(bulletPrefab, spawn, Quaternion.identity);
            bullet.hitMask = groundMask;
            bullet.Launch(new Vector2(facing, 0f), bulletDamage, Faction.Enemy, -1);

            if (animator != null) animator.SetTrigger("Shoot");
            AudioManager.Sfx("sfx_enemy_shoot", 0.4f);
        }

        /// <summary>El punto de salida sigue el lado al que mira el robot.</summary>
        private Vector3 MuzzlePosition()
        {
            Vector3 p = muzzle.position;
            float offset = p.x - transform.position.x;
            if (Mathf.Abs(offset) < 0.001f) return p;

            bool muzzleOnRight = offset > 0f;
            bool fireToRight = facing >= 0f;
            if (muzzleOnRight != fireToRight) p.x = transform.position.x - offset;
            return p;
        }

        private bool HasLineOfSight(Transform target)
        {
            if (groundMask == 0) return true;

            Vector2 from = transform.position + Vector3.up * 0.9f;
            Vector2 to = target.position + Vector3.up * 0.9f;
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
                spriteRenderer.color = new Color(1f, 0.62f, 0.5f, 1f);
                flashUntil = Time.time + 0.12f;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.5f);
            float l = hasPatrolBounds ? Mathf.Min(patrolLeft, patrolRight) : transform.position.x - patrolRange;
            float r = hasPatrolBounds ? Mathf.Max(patrolLeft, patrolRight) : transform.position.x + patrolRange;
            Gizmos.DrawLine(new Vector3(l, transform.position.y, 0f), new Vector3(r, transform.position.y, 0f));
        }
    }
}
