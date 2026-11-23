using UnityEngine;

namespace OperacionCuatro
{
    /// <summary>Activa el punto de control al pasar por encima.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint : MonoBehaviour
    {
        public Transform respawnPoint;
        public GameObject activeVisual;
        public GameObject inactiveVisual;

        private bool activated;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (activated) return;

            var player = other.GetComponentInParent<PlayerController2D>();
            if (player == null) return;

            activated = true;

            Vector3 pos = respawnPoint != null ? respawnPoint.position : transform.position + Vector3.up;
            if (LevelManager.Instance != null) LevelManager.Instance.SetCheckpoint(pos);

            if (activeVisual != null) activeVisual.SetActive(true);
            if (inactiveVisual != null) inactiveVisual.SetActive(false);

            AudioManager.Sfx("sfx_checkpoint", 0.7f);
        }
    }

    /// <summary>Zona que completa la misión.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class GoalZone : MonoBehaviour
    {
        public bool requiresWaveCleared = false;
        public GameObject[] waveObjects;

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerController2D>();
            if (player == null) return;

            if (requiresWaveCleared && waveObjects != null)
            {
                for (int i = 0; i < waveObjects.Length; i++)
                {
                    if (waveObjects[i] != null && waveObjects[i].activeInHierarchy) return;
                }
            }

            if (LevelManager.Instance != null) LevelManager.Instance.CompleteMission();
        }
    }

    /// <summary>Dispara una oleada de enemigos cuando el jugador entra en la zona.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class WaveTrigger : MonoBehaviour
    {
        public GameObject[] enemiesToActivate;
        public bool spawnAtPoints;
        public Transform[] spawnPoints;
        public GameObject[] enemyPrefabs;
        public float activateDelay = 0.4f;

        private bool fired;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (fired) return;
            if (other.GetComponentInParent<PlayerController2D>() == null) return;

            fired = true;

            if (enemiesToActivate != null)
            {
                for (int i = 0; i < enemiesToActivate.Length; i++)
                {
                    if (enemiesToActivate[i] != null) enemiesToActivate[i].SetActive(true);
                }
            }

            if (spawnAtPoints && enemyPrefabs != null && spawnPoints != null)
            {
                for (int i = 0; i < spawnPoints.Length && i < enemyPrefabs.Length; i++)
                {
                    if (enemyPrefabs[i] != null && spawnPoints[i] != null)
                        Instantiate(enemyPrefabs[i], spawnPoints[i].position, Quaternion.identity);
                }
            }

            AudioManager.Sfx("sfx_alert", 0.7f);
        }
    }
}
