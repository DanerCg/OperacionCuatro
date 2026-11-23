using UnityEngine;
using UnityEngine.UI;

namespace OperacionCuatro
{
    /// <summary>
    /// Fase 3 — TEMPORIZADOR DE EVACUACIÓN.
    ///
    /// Muestra el tiempo que queda, en la franja superior central y sin solaparse con la
    /// puntuación ni con los paneles de jugador. Solo aparece con la alarma en marcha.
    /// </summary>
    public class EscapeCountdownUI : MonoBehaviour
    {
        public GameObject root;
        public Text label;

        public Color normalColor = new Color(1f, 0.86f, 0.35f, 1f);
        public Color criticalColor = new Color(1f, 0.3f, 0.26f, 1f);
        public int criticalSeconds = 10;

        private void Awake()
        {
            if (root == null) root = gameObject;
        }

        private void Update()
        {
            var esc = EscapeSequence.Instance;

            bool show = esc != null && esc.Begun && !esc.Finished;
            if (root != null && root.activeSelf != show) root.SetActive(show);
            if (!show || label == null) return;

            int seconds = esc.RemainingSeconds;
            label.text = "EVACUACIÓN  " + seconds.ToString("00") + "s";
            label.color = seconds <= criticalSeconds ? criticalColor : normalColor;
        }
    }
}
