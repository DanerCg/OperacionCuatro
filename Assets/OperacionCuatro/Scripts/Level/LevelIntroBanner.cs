using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace OperacionCuatro
{
    /// <summary>
    /// Fase 2 — cartel de introducción del nivel: aparece al arrancar, muestra el nivel en curso
    /// (GameSession) y se desvanece. No pausa la partida.
    /// </summary>
    public class LevelIntroBanner : MonoBehaviour
    {
        public CanvasGroup group;
        public Text titleText;
        public Text subtitleText;
        public float fadeIn = 0.5f;
        public float holdTime = 2.2f;
        public float fadeOut = 0.8f;

        private IEnumerator Start()
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            if (group == null) yield break;

            if (titleText != null) titleText.text = "MISIÓN " + GameSession.CurrentLevel + "  ·  " + GameSession.GetLevelDisplayName();
            if (subtitleText != null) subtitleText.text = "LEVEL02 — COMPLEJO DE GUERRA";

            group.alpha = 0f;

            float t = 0f;
            while (t < fadeIn)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Clamp01(t / Mathf.Max(0.01f, fadeIn));
                yield return null;
            }

            group.alpha = 1f;
            yield return new WaitForSecondsRealtime(holdTime);

            t = 0f;
            while (t < fadeOut)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = 1f - Mathf.Clamp01(t / Mathf.Max(0.01f, fadeOut));
                yield return null;
            }

            group.alpha = 0f;
            gameObject.SetActive(false);
        }
    }
}
