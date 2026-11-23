using UnityEngine;
using UnityEngine.UI;

namespace OperacionCuatro
{
    /// <summary>
    /// Pantalla de resultados / fin de partida.
    ///
    /// Semántica de los tres caminos (Fase 2):
    ///  - REINTENTAR: reinicia puntuación, bajas, vidas y MissionComplete, y recarga el
    ///    nivel actual (GameSession.CurrentLevel). Conserva modo y personajes.
    ///  - SIGUIENTE MISIÓN: conserva puntuación, bajas, vidas, modo y personajes; avanza
    ///    GameSession.CurrentLevel y carga ese nivel. Nunca llama a StartNewRun.
    ///  - MENÚ PRINCIPAL: vuelve al menú; la partida se descarta.
    /// </summary>
    public class ResultsUI : MonoBehaviour
    {
        public Text titleText;
        public Text scoreText;
        public Text detailText;
        public Button retryButton;
        public Button nextMissionButton;
        public Button menuButton;

        private void Awake()
        {
            if (retryButton != null) retryButton.onClick.AddListener(OnRetry);
            if (nextMissionButton != null) nextMissionButton.onClick.AddListener(OnNextMission);
            if (menuButton != null) menuButton.onClick.AddListener(OnMenu);
        }

        private void Start()
        {
            bool win = GameSession.MissionComplete;
            bool canAdvance = win && GameSession.HasNextLevel;

            if (titleText != null)
            {
                titleText.text = win ? "MISIÓN COMPLETADA" : "GAME OVER";
                titleText.color = win ? new Color(0.35f, 0.95f, 0.45f) : new Color(0.95f, 0.28f, 0.24f);
            }

            if (scoreText != null)
                scoreText.text = GameSession.Score.ToString("000000");

            if (detailText != null)
            {
                string kills = "MISIÓN " + GameSession.CurrentLevel + " · " + GameSession.GetLevelDisplayName() +
                               "   BAJAS J1 " + GameSession.KillsP1;
                if (GameSession.PlayerCount == 2) kills += "   BAJAS J2 " + GameSession.KillsP2;
                detailText.text = kills;
            }

            // El botón de avance solo aparece si se ganó y queda un nivel implementado.
            if (nextMissionButton != null) nextMissionButton.gameObject.SetActive(canAdvance);

            int best = PlayerPrefs.GetInt("OC_BestScore", 0);
            if (GameSession.Score > best)
            {
                PlayerPrefs.SetInt("OC_BestScore", GameSession.Score);
                PlayerPrefs.Save();
            }

            if (GameManager.Instance != null && GameManager.Instance.Fader != null)
                GameManager.Instance.Fader.FadeIn(0.4f);

            if (GameManager.Instance != null) GameManager.Instance.SetPaused(false);

            if (GameManager.Instance != null && GameManager.Instance.Audio != null)
                GameManager.Instance.Audio.PlayMusic("mus_menu", 0.5f);
        }

        public void OnRetry()
        {
            AudioManager.Sfx("sfx_select", 0.7f);
            GameSession.Score = 0;
            GameSession.KillsP1 = 0;
            GameSession.KillsP2 = 0;
            GameSession.LivesP1 = GameSession.StartingLives;
            GameSession.LivesP2 = GameSession.StartingLives;
            GameSession.MissionComplete = false;

            LoadLevel(GameSession.GetLevelSceneName());
        }

        /// <summary>SIGUIENTE MISIÓN: avanza de nivel conservando la partida en curso.</summary>
        public void OnNextMission()
        {
            if (!GameSession.MissionComplete) return;
            if (!GameSession.AdvanceLevel()) return;

            AudioManager.Sfx("sfx_start", 0.8f);
            LoadLevel(GameSession.GetLevelSceneName());
        }

        public void OnMenu()
        {
            AudioManager.Sfx("sfx_select", 0.7f);
            if (GameManager.Instance != null) GameManager.Instance.LoadScene(SceneNames.MainMenu);
            else UnityEngine.SceneManagement.SceneManager.LoadScene(SceneNames.MainMenu);
        }

        private static void LoadLevel(string sceneName)
        {
            if (GameManager.Instance != null) GameManager.Instance.LoadScene(sceneName);
            else UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }
    }
}
