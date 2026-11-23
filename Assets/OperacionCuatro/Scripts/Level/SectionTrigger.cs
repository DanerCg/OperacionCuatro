using UnityEngine;

namespace OperacionCuatro
{
    /// <summary>
    /// Fase 2 — zona de sección: anuncia por el HUD el nombre de la sección al entrar el primer
    /// jugador y, opcionalmente, fija un punto de control.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class SectionTrigger : MonoBehaviour
    {
        public string sectionName = "SECCIÓN";
        public bool showOnce = true;
        public bool setsCheckpoint;
        public Transform respawnPoint;

        private bool fired;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (fired && showOnce) return;

            var player = other.GetComponentInParent<PlayerController2D>();
            if (player == null || player.IsDead) return;

            fired = true;

            var manager = LevelManager.Instance;
            if (manager == null) return;

            if (manager.hud != null) manager.hud.ShowMessage(sectionName);

            if (setsCheckpoint)
            {
                Vector3 pos = respawnPoint != null ? respawnPoint.position : transform.position + Vector3.up;
                manager.SetCheckpoint(pos);
            }

            AudioManager.Sfx("sfx_section", 0.6f);
        }
    }
}
