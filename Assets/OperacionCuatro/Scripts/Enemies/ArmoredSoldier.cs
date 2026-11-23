using UnityEngine;

namespace OperacionCuatro
{
    /// <summary>
    /// Fase 2 — ENEMIGO A: SOLDADO BLINDADO.
    ///
    /// Avanza despacio, dispara ráfagas cortas y lleva un escudo frontal: los disparos que
    /// llegan de frente hacen solo una fracción del daño. Es vulnerable por la espalda, con
    /// granadas y durante la ventana de recuperación posterior a cada ráfaga.
    ///
    /// La resistencia se implementa con <see cref="IDamageModifier"/>, que Health consulta
    /// antes de aplicar el daño; no cambia el comportamiento de ningún otro enemigo.
    /// </summary>
    public class ArmoredSoldier : EnemyBase, IDamageModifier
    {
        [Header("Movimiento")]
        public float walkSpeed = 1.2f;
        public float detectionRange = 15f;
        public float stopRange = 6.5f;
        public LayerMask groundMask;
        public float edgeCheckDistance = 0.9f;
        public float wallCheckDistance = 0.55f;

        [Header("Ataque")]
        public Projectile bulletPrefab;
        public Transform muzzle;
        public float fireCooldown = 2.1f;
        public int burstSize = 2;
        public float burstInterval = 0.16f;

        [Header("Blindaje frontal")]
        [Range(0f, 1f)] public float frontalResistance = 0.25f;
        [Tooltip("Tiempo vulnerable tras cada ráfaga, durante el cual el escudo no reduce el daño.")]
        public float vulnerableWindow = 1.3f;
        public SpriteRenderer shieldRenderer;

        [Header("Efectos")]
        public GameObject hitSparkPrefab;

        private float nextFireTime;
        private float nextBurstShotTime;
        private int burstRemaining;
        private float vulnerableUntil;
        private Transform target;
        private float stuckTimer;

        protected override void Awake()
        {
            base.Awake();

            maxHealth = 60;
            scoreValue = 250;
            contactDamage = 14;

            if (health != null)
            {
                health.SetMaxHealth(maxHealth, true);
                health.damageModifier = this;
            }

            if (muzzle == null) muzzle = transform;
        }

        private void FixedUpdate()
        {
            if (dead || body == null) return;
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;

            UpdateBurst();

            target = FindNearestPlayer();

            if (target == null)
            {
                Patrol();
                return;
            }

            float dx = target.position.x - transform.position.x;
            float dist = Mathf.Abs(dx);
            FlipTowards(dx);

            if (dist > stopRange)
            {
                body.linearVelocity = new Vector2(Mathf.Sign(dx) * walkSpeed, body.linearVelocity.y);
                if (animator != null) animator.SetFloat("Speed", walkSpeed);

                if (WallOrEdge()) body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
            }
            else
            {
                body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
                if (animator != null) animator.SetFloat("Speed", 0f);

                if (burstRemaining <= 0 && Time.time >= nextFireTime)
                {
                    burstRemaining = Mathf.Max(1, burstSize);
                    nextBurstShotTime = Time.time;
                }
            }
        }

        private void UpdateBurst()
        {
            if (burstRemaining <= 0) return;
            if (Time.time < nextBurstShotTime) return;

            FireTowardsTarget();
            burstRemaining--;

            if (burstRemaining <= 0)
            {
                nextFireTime = Time.time + fireCooldown;
                // Ventana de recuperación: durante este tiempo el escudo no protege.
                vulnerableUntil = Time.time + vulnerableWindow;
                if (animator != null) animator.SetFloat("Speed", 0f);
            }
            else
            {
                nextBurstShotTime = Time.time + burstInterval;
            }
        }

        private void FireTowardsTarget()
        {
            if (bulletPrefab == null || target == null) return;

            float dx = target.position.x - transform.position.x;
            Vector2 dir = new Vector2(Mathf.Sign(dx), 0.05f).normalized;

            var bullet = Instantiate(bulletPrefab, MuzzlePosition(dx), Quaternion.identity);
            bullet.hitMask = groundMask;
            bullet.Launch(dir, 13, Faction.Enemy, -1);

            if (animator != null) animator.SetTrigger("Shoot");
            AudioManager.Sfx("sfx_turret", 0.45f);
        }

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

        private void Patrol()
        {
            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer > 3f) { stuckTimer = 0f; FlipTowards(-facing); }

            if (WallOrEdge())
            {
                FlipTowards(-facing);
                body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
                if (animator != null) animator.SetFloat("Speed", 0f);
                return;
            }

            body.linearVelocity = new Vector2(facing * walkSpeed * 0.6f, body.linearVelocity.y);
            if (animator != null) animator.SetFloat("Speed", walkSpeed * 0.6f);
        }

        private bool WallOrEdge()
        {
            Vector2 origin = transform.position;

            bool wall = Physics2D.Raycast(origin + new Vector2(facing * 0.45f, 0.75f), Vector2.right * facing,
                wallCheckDistance, groundMask).collider != null;

            if (wall) return true;

            return Physics2D.Raycast(origin + new Vector2(facing * 0.55f, 0.15f), Vector2.down,
                edgeCheckDistance, groundMask).collider == null;
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

        // ------------------------------------------------------------------
        //  Blindaje
        // ------------------------------------------------------------------
        public int ModifyDamage(DamageInfo info, int amount)
        {
            if (dead || amount <= 0) return amount;

            // Las granadas ignoran el escudo.
            if (info.source != null && info.source.GetComponent<Bomb>() != null) return amount;

            // Ventana de recuperación tras la ráfaga.
            if (Time.time < vulnerableUntil) return amount;

            float relativeX = info.point.x - transform.position.x;
            if (Mathf.Abs(relativeX) < 0.02f) return amount;

            bool fromFront = Mathf.Sign(relativeX) == facing;
            if (!fromFront) return amount;

            int reduced = Mathf.Max(1, Mathf.RoundToInt(amount * Mathf.Clamp01(frontalResistance)));
            if (hitSparkPrefab != null)
            {
                var spark = Instantiate(hitSparkPrefab, info.point, Quaternion.identity);
                Destroy(spark, 0.4f);
            }

            return reduced;
        }

        protected override void HandleDamaged(DamageInfo info)
        {
            base.HandleDamaged(info);

            if (shieldRenderer != null) shieldRenderer.color = new Color(0.72f, 0.9f, 1f, 1f);
        }

        private void Update()
        {
            if (shieldRenderer != null && dead == false)
                shieldRenderer.color = Color.Lerp(shieldRenderer.color, Color.white, Time.deltaTime * 6f);
        }
    }
}
