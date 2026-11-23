using UnityEngine;

namespace OperacionCuatro
{
    /// <summary>
    /// Fase 3 — ZONA SEGURA DE EVACUACIÓN.
    ///
    /// La salida del laboratorio. No completa la misión por sí misma: se lo pide a
    /// <see cref="EscapeSequence"/>, que exige que la alarma esté en marcha y es la única que
    /// puede cerrar la misión. Está siempre accesible, sin puertas ni enemigos que la bloqueen.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class SafeExitZone : MonoBehaviour
    {
        [Tooltip("Si se deja vacío se usa la instancia activa de EscapeSequence.")]
        public EscapeSequence escape;

        public GameObject lockedVisual;
        public GameObject openVisual;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void Update()
        {
            var esc = escape != null ? escape : EscapeSequence.Instance;
            bool ready = esc != null && esc.Begun && !esc.Finished;

            if (lockedVisual != null) lockedVisual.SetActive(!ready);
            if (openVisual != null) openVisual.SetActive(ready);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController2D>() == null) return;

            var esc = escape != null ? escape : EscapeSequence.Instance;
            if (esc == null) return;

            esc.ReachExit();
        }
    }
}
