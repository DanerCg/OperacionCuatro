using UnityEngine;
using UnityEngine.UI;

namespace OperacionCuatro
{
    /// <summary>
    /// Barra inferior del generador: solo aparece cuando el jugador se acerca al objetivo
    /// o cuando el escudo ya cayó. Muestra integridad del núcleo y estado del escudo.
    /// </summary>
    public class GeneratorHudUI : MonoBehaviour
    {
        public GeneratorObjective generator;
        public GameObject root;
        public Text label;
        public Image fill;
        public float visibleRange = 30f;

        private void Update()
        {
            if (generator == null)
            {
                if (root != null && root.activeSelf) root.SetActive(false);
                return;
            }

            bool visible = !generator.Destroyed && ShouldShow();
            if (root != null && root.activeSelf != visible) root.SetActive(visible);
            if (!visible) return;

            if (label != null)
            {
                if (generator.ShieldActive)
                    label.text = "GENERADOR CENTRAL   ·   ESCUDO ACTIVO";
                else
                    label.text = "GENERADOR CENTRAL   " + generator.Current + " / " + generator.Max;
            }

            if (fill != null)
            {
                fill.fillAmount = Mathf.Lerp(fill.fillAmount, generator.Normalized, Time.unscaledDeltaTime * 8f);

                if (generator.ShieldActive) fill.color = new Color(0.35f, 0.7f, 1f);
                else if (fill.fillAmount > 0.5f) fill.color = new Color(0.98f, 0.78f, 0.2f);
                else fill.color = new Color(0.92f, 0.28f, 0.24f);
            }
        }

        private bool ShouldShow()
        {
            if (generator.Engaged) return true;
            if (LevelManager.Instance == null) return false;

            var players = LevelManager.Instance.Players;
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (p == null || p.IsDead) continue;

                float dx = Mathf.Abs(p.transform.position.x - generator.transform.position.x);
                if (dx <= visibleRange) return true;
            }

            return false;
        }
    }
}
