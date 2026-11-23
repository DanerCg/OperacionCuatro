using UnityEngine;

namespace OperacionCuatro
{
    /// <summary>
    /// Fase 2 — AMETRALLADORA PESADA como recogible.
    ///
    /// Entrega munición limitada a PlayerShooter.ApplyHeavyWeapon() y desaparece. No modifica
    /// el arma básica: al agotarse la munición el jugador vuelve a disparar como siempre.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class HeavyWeaponPickup : MonoBehaviour
    {
        [Header("Munición")]
        public int ammo = 150;

        [Header("Presentación")]
        public GameObject iconRoot;
        public GameObject pickupEffectPrefab;
        public float bobAmplitude = 0.14f;
        public float bobSpeed = 2.4f;

        private Vector3 baseLocalPosition;
        private bool taken;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;

            if (iconRoot == null && transform.childCount > 0) iconRoot = transform.GetChild(0).gameObject;
            baseLocalPosition = transform.localPosition;
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

            player.shooter.ApplyHeavyWeapon(player.shooter.HeavyAmmo + Mathf.Max(1, ammo));

            if (LevelManager.Instance != null && LevelManager.Instance.hud != null)
                LevelManager.Instance.hud.ShowMessage("AMETRALLADORA PESADA");

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
