using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OperacionCuatro
{
    /// <summary>
    /// Fundido a negro persistente para las transiciones entre escenas.
    /// </summary>
    public class SceneFader : MonoBehaviour
    {
        private CanvasGroup group;
        private Image overlay;
        private bool loading;
        private Coroutine fadeRoutine;

        public bool IsLoading => loading;

        private void Awake()
        {
            BuildOverlay();
        }

        private void BuildOverlay()
        {
            var canvasGo = new GameObject("[OC_FadeCanvas]");
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);

            group = canvasGo.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            var imgGo = new GameObject("Fade");
            imgGo.transform.SetParent(canvasGo.transform, false);

            overlay = imgGo.AddComponent<Image>();
            overlay.color = Color.black;
            overlay.raycastTarget = false;

            var rt = overlay.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            group.alpha = 1f;
        }

        public void FadeIn(float duration = 0.35f)
        {
            StartFade(1f, 0f, duration);
        }

        public void FadeOut(float duration = 0.35f)
        {
            StartFade(0f, 1f, duration);
        }

        /// <summary>
        /// Lanza o reemplaza SOLO el fundido. Nunca usa StopAllCoroutines porque
        /// cancelaria la carga de escena en curso y dejaria 'loading' bloqueado.
        /// </summary>
        private void StartFade(float from, float to, float duration)
        {
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeRoutine(from, to, duration));
        }

        private IEnumerator FadeRoutine(float from, float to, float duration)
        {
            group.alpha = from;
            if (duration <= 0f)
            {
                group.alpha = to;
                yield break;
            }

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }

            group.alpha = to;
        }

        public void LoadScene(string sceneName)
        {
            if (loading)
            {
                Debug.LogWarning("[OC] Carga ya en curso, se ignora la peticion de '" + sceneName + "'.");
                return;
            }

            StartCoroutine(LoadRoutine(sceneName));
        }

        private IEnumerator LoadRoutine(string sceneName)
        {
            loading = true;
            group.blocksRaycasts = true;
            group.alpha = Mathf.Max(group.alpha, 0f);

            Time.timeScale = 1f;

            StartFade(group.alpha, 1f, 0.3f);
            yield return new WaitForSecondsRealtime(0.3f);
            group.alpha = 1f;

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError("[OC] La escena '" + sceneName + "' no esta en Build Settings.");
                loading = false;
                group.blocksRaycasts = false;
                StartFade(1f, 0f, 0.2f);
                yield break;
            }

            var op = SceneManager.LoadSceneAsync(sceneName);
            if (op == null)
            {
                Debug.LogError("[OC] No se pudo cargar la escena '" + sceneName + "'. ¿Esta añadida a Build Settings?");
                loading = false;
                group.blocksRaycasts = false;
                StartFade(1f, 0f, 0.2f);
                yield break;
            }

            while (!op.isDone) yield return null;

            // La escena ya esta activa: liberamos el bloqueo antes del fundido de entrada
            // para que Start() de la nueva escena pueda pedir otra carga si hace falta.
            yield return null;
            loading = false;
            group.blocksRaycasts = false;

            StartFade(1f, 0f, 0.3f);
        }
    }
}
