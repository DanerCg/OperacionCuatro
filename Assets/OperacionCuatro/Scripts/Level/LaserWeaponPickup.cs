using UnityEngine;

namespace OperacionCuatro
{
    /// <summary>
    /// Fase 3 — ARMA LÁSER como recogible.
    ///
    /// Entrega munición limitada a PlayerShooter.ApplyLaserWeapon() y desaparece. Al agotarse,
    /// PlayerShooter vuelve automáticamente al arma anterior: no reemplaza ni desactiva el
    /// sistema de armas existente.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class LaserWeaponPickup : MonoBehaviour
    {
        [Header("Munición")]
        public int ammo = 40;

        [Header("Presentación")]
        public GameObject iconRoot;
        public GameObject pickupEffectPrefab;
        public float bobAmplitude = 0.14f;
        public float bobSpeed = 2.6f;

        private bool taken;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;

            if (iconRoot == null && transform.childCount > 0) iconRoot = transform.GetChild(0).gameObject;
        }

        private void Update()
        {
            if (iconRoot == null) return;

            float y = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
            iconRoot.transform.localPosition = new Vector3(0f, y, 0f);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (taken) return;

            var player = other.GetComponentInParent<PlayerController2D>();
            if (player == null || player.IsDead) return;
            if (player.shooter == null) return;

            taken = true;

            player.shooter.ApplyLaserWeapon(player.shooter.LaserAmmo + Mathf.Max(1, ammo));

            if (LevelManager.Instance != null && LevelManager.Instance.hud != null)
                LevelManager.Instance.hud.ShowMessage("ARMA LÁSER");

            AudioManager.Sfx("sfx_pickup", 0.85f);

            if (pickupEffectPrefab != null)
            {
                var fx = Instantiate(pickupEffectPrefab, transform.position, Quaternion.identity);
                Destroy(fx, 1f);
            }

            Destroy(gameObject);
        }
    }
}
