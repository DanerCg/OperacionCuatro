using System.Collections;
using UnityEngine;

namespace OperacionCuatro
{
    /// <summary>
    /// Fase 2 — OBJETIVO FINAL de Level02: el generador central.
    ///
    /// Es un objetivo de misión, no un jefe: mientras queden enemigos de la oleada que lo
    /// protege su núcleo es invulnerable (escudo visible). Al destruirlo arranca una secuencia
    /// de sobrecarga y explosiones que termina llamando a LevelManager.CompleteMission().
    ///
    /// Vive en la capa Enemy con Health.faction = Enemy para que tanto las balas (máscara
    /// Ground|Enemy) como las granadas (damageMask Enemy|Ground) le hagan daño.
    /// </summary>
    public class GeneratorObjective : MonoBehaviour
    {
        [Header("Salud")]
        public Health health;
        public int maxHealth = 280;

        [Header("Escudo")]
        [Tooltip("Enemigos que deben morir para desactivar el escudo del núcleo.")]
        public GameObject[] shieldWaveObjects;
        public GameObject shieldVisual;

        [Header("Presentación")]
        public SpriteRenderer coreVisual;
        public GameObject explosionPrefab;
        public float overloadDuration = 2.6f;
        public int overloadExplosions = 6;

        public bool ShieldActive { get; private set; }
        public bool Engaged { get; private set; }
        public bool Destroyed { get; private set; }

        public int Current { get { return health != null ? health.Current : 0; } }
        public int Max { get { return health != null ? health.maxHealth : maxHealth; } }
        public float Normalized { get { return health != null ? health.Normalized : 0f; } }

        private float flashUntil;

        private void Awake()
        {
            if (health == null) health = GetComponent<Health>();
            if (coreVisual == null) coreVisual = GetComponentInChildren<SpriteRenderer>();

            if (health != null)
            {
                health.faction = Faction.Enemy;
                health.maxHealth = Mathf.Max(1, maxHealth);
                health.SetMaxHealth(maxHealth, true);
            }

            ShieldActive = HasLivingGuards();
            Engaged = !ShieldActive;

            if (health != null) health.invulnerable = ShieldActive;
            if (shieldVisual != null) shieldVisual.SetActive(ShieldActive);
        }

        private void OnEnable()
        {
            if (health == null) return;
            health.Died += HandleDestroyed;
            health.Damaged += HandleDamaged;
        }

        private void OnDisable()
        {
            if (health == null) return;
            health.Died -= HandleDestroyed;
            health.Damaged -= HandleDamaged;
        }

        private void Update()
        {
            if (flashUntil > 0f && Time.time > flashUntil && coreVisual != null)
            {
                coreVisual.color = Color.white;
                flashUntil = 0f;
            }

            if (!ShieldActive || Destroyed) return;

            if (!HasLivingGuards())
            {
                ShieldActive = false;
                Engaged = true;
                if (health != null) health.invulnerable = false;
                if (shieldVisual != null) shieldVisual.SetActive(false);

                if (LevelManager.Instance != null && LevelManager.Instance.hud != null)
                    LevelManager.Instance.hud.ShowMessage("¡NÚCLEO EXPUESTO!");

                AudioManager.Sfx("sfx_alert", 0.85f);
            }
        }

        private bool HasLivingGuards()
        {
            if (shieldWaveObjects == null) return false;

            for (int i = 0; i < shieldWaveObjects.Length; i++)
            {
                var go = shieldWaveObjects[i];
                if (go == null) continue;
                if (!go.activeInHierarchy) continue;

                var enemy = go.GetComponent<EnemyBase>();
                if (enemy != null && enemy.IsDead) continue;

                return true;
            }

            return false;
        }

        private void HandleDamaged(DamageInfo info)
        {
            if (ShieldActive) return;

            if (coreVisual != null)
            {
                coreVisual.color = new Color(1f, 0.55f, 0.4f, 1f);
                flashUntil = Time.time + 0.1f;
            }
        }

        private void HandleDestroyed(DamageInfo info)
        {
            if (Destroyed) return;
            Destroyed = true;
            ShieldActive = false;

            if (info.playerIndex >= 0) GameSession.AddKill(info.playerIndex, 1200);
            else GameSession.Score += 1200;

            StartCoroutine(OverloadRoutine());
        }

        private IEnumerator OverloadRoutine()
        {
            if (LevelManager.Instance != null && LevelManager.Instance.hud != null)
                LevelManager.Instance.hud.ShowMessage("SOBRECARGA DEL GENERADOR");

            AudioManager.Sfx("sfx_generator", 0.9f);

            if (coreVisual != null) coreVisual.color = new Color(1f, 0.7f, 0.3f, 1f);

            int bursts = Mathf.Max(1, overloadExplosions);
            float step = overloadDuration / bursts;

            for (int i = 0; i < bursts; i++)
            {
                Vector3 offset = new Vector3(Random.Range(-1.4f, 1.4f), Random.Range(-0.6f, 1.6f), 0f);

                if (explosionPrefab != null)
                {
                    var fx = Instantiate(explosionPrefab, transform.position + offset, Quaternion.identity);
                    Destroy(fx, 1f);
                }

                var shake = FindFirstObjectByType<CameraShake>();
                if (shake != null) shake.Shake(0.35f, 0.22f);
                AudioManager.Sfx("sfx_explosion", 0.5f);

                yield return new WaitForSeconds(step);
            }

            if (coreVisual != null) coreVisual.enabled = false;

            var colliders = GetComponentsInChildren<Collider2D>();
            for (int i = 0; i < colliders.Length; i++) colliders[i].enabled = false;

            yield return new WaitForSeconds(0.3f);

            if (LevelManager.Instance != null) LevelManager.Instance.CompleteMission();
            else GameSession.MissionComplete = true;
        }
    }
}
