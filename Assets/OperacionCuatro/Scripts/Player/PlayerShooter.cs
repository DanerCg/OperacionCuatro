using System;
using UnityEngine;

namespace OperacionCuatro
{
    /// <summary>
    /// Armas del jugador: disparo automático y granadas de daño en área.
    /// </summary>
    public class PlayerShooter : MonoBehaviour
    {
        [Header("Prebalos")]
        public Projectile bulletPrefab;
        public Bomb bombPrefab;
        public GameObject muzzleFlashPrefab;

        [Header("Ajustes")]
        public float fireRate = 0.15f;
        public int bulletDamage = 12;
        public float bulletSpeed = 22f;
        public int maxBombs = 3;
        public float bombThrowCooldown = 0.6f;
        public float bombHorizontalSpeed = 7.5f;
        public float bombVerticalSpeed = 9f;
        public LayerMask hitMask;

        [Header("Arma pesada (Fase 2)")]
        [Tooltip("Proyectil de la ametralladora pesada; si falta se usa el básico.")]
        public Projectile heavyBulletPrefab;
        public float heavyFireRate = 0.07f;
        public int heavyDamage = 9;
        public float heavyBulletSpeed = 26f;

        [Header("Referencias")]
        public Transform muzzle;
        public Animator animator;

        public int Bombs { get; private set; }

        /// <summary>Municiones restantes del arma pesada (0 = arma básica).</summary>
        public int HeavyAmmo { get; private set; }

        public bool HasHeavyWeapon { get { return HeavyAmmo > 0; } }

        public event Action<int> BombsChanged;
        public event Action<int> HeavyAmmoChanged;

        /// <summary>Recoge la ametralladora pesada. Al agotar la munición vuelve el arma básica.</summary>
        public void ApplyHeavyWeapon(int ammo)
        {
            HeavyAmmo = Mathf.Max(0, ammo);
            if (HeavyAmmoChanged != null) HeavyAmmoChanged(HeavyAmmo);
        }

        private PlayerController2D owner;
        private float nextFireTime;
        private float nextBombTime;

        private static readonly int ParamShoot = Animator.StringToHash("Shoot");
        private static readonly int ParamThrow = Animator.StringToHash("Throw");

        public void Bind(PlayerController2D controller)
        {
            owner = controller;
            if (animator == null) animator = controller != null ? controller.animator : null;
        }

        private void Start()
        {
            if (owner == null) owner = GetComponent<PlayerController2D>();
            if (animator == null && owner != null) animator = owner.animator;
            if (Bombs <= 0) RefillBombs();
        }

        public void RefillBombs()
        {
            Bombs = maxBombs;
            if (BombsChanged != null) BombsChanged(Bombs);
        }

        public bool TryFire(Vector2 direction)
        {
            if (bulletPrefab == null || muzzle == null) return false;
            if (owner != null && owner.IsDead) return false;
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return false;
            if (Time.time < nextFireTime) return false;

            nextFireTime = Time.time + (HasHeavyWeapon ? heavyFireRate : fireRate);

            bool heavy = HasHeavyWeapon;
            var prefab = heavy && heavyBulletPrefab != null ? heavyBulletPrefab : bulletPrefab;
            int damage = heavy ? heavyDamage : bulletDamage;
            float speed = heavy ? heavyBulletSpeed : bulletSpeed;

            Vector3 spawnPos = MuzzlePosition(direction.x);
            var bullet = Instantiate(prefab, spawnPos, Quaternion.identity);
            bullet.hitMask = hitMask;
            bullet.speed = speed;
            bullet.Launch(direction, damage, Faction.Player, owner != null ? owner.playerIndex : 0);

            if (heavy)
            {
                HeavyAmmo--;
                if (HeavyAmmoChanged != null) HeavyAmmoChanged(HeavyAmmo);
            }

            if (muzzleFlashPrefab != null)
            {
                var fx = Instantiate(muzzleFlashPrefab, spawnPos, Quaternion.Euler(0f, 0f, direction.x >= 0f ? 0f : 180f));
                fx.transform.SetParent(muzzle, true);
                Destroy(fx, 0.09f);
            }

            if (animator != null) animator.SetTrigger(ParamShoot);
            AudioManager.Sfx("sfx_shoot", 0.45f);

            return true;
        }

        /// <summary>
        /// Punto del cañón en el lado hacia el que se dispara. El objeto Muzzle es hijo de la raíz
        /// (no de Visual), así que no se voltea con el personaje: sin esto la bala nacería detrás.
        /// </summary>
        private Vector3 MuzzlePosition(float dirX)
        {
            Vector3 p = muzzle.position;
            float offset = p.x - transform.position.x;
            if (Mathf.Abs(offset) < 0.001f) return p;

            bool muzzleOnRight = offset > 0f;
            bool fireToRight = dirX >= 0f;
            if (muzzleOnRight != fireToRight) p.x = transform.position.x - offset;
            return p;
        }

        public bool TryBomb(Vector2 direction)
        {
            if (bombPrefab == null || muzzle == null) return false;
            if (owner != null && owner.IsDead) return false;
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return false;
            if (Bombs <= 0 || Time.time < nextBombTime) return false;

            nextBombTime = Time.time + bombThrowCooldown;
            Bombs--;
            if (BombsChanged != null) BombsChanged(Bombs);

            Vector3 spawn = MuzzlePosition(direction.x) + new Vector3(direction.x * 0.25f, 0.1f, 0f);
            var bomb = Instantiate(bombPrefab, spawn, Quaternion.identity);
            bomb.faction = Faction.Player;
            bomb.Throw(new Vector2(direction.x * bombHorizontalSpeed, bombVerticalSpeed), Faction.Player, owner != null ? owner.playerIndex : 0);

            if (animator != null) animator.SetTrigger(ParamThrow);
            AudioManager.Sfx("sfx_bomb_throw", 0.6f);

            return true;
        }
    }
}
