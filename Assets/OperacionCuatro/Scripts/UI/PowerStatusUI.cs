using UnityEngine;
using UnityEngine.UI;

namespace OperacionCuatro
{
    /// <summary>
    /// Fase 3 — INDICADOR DE ENERGÍA.
    ///
    /// Muestra cuántas unidades de alimentación siguen en pie y cambia a verde cuando la red
    /// cae. Se coloca bajo la puntuación, en el centro superior, sin tapar los paneles de jugador.
    /// </summary>
    public class PowerStatusUI : MonoBehaviour
    {
        public Text label;

        public Color pendingColor = new Color(1f, 0.62f, 0.28f, 1f);
        public Color doneColor = new Color(0.42f, 0.95f, 0.52f, 1f);

        private void Update()
        {
            if (label == null) return;

            int total = PowerUnit.TotalUnits;
            int down = PowerUnit.DestroyedTotal();

            if (total <= 0)
            {
                label.text = "ENERGÍA  —";
                label.color = pendingColor;
                return;
            }

            label.text = "ENERGÍA  " + down + "/" + total;
            label.color = down >= total ? doneColor : pendingColor;
        }
    }
}
