using UnityEngine;
using UnityEngine.UI;

namespace OperacionCuatro
{
    /// <summary>Menú principal arcade.</summary>
    public class MainMenuUI : MonoBehaviour
    {
        public GameObject mainPanel;
        public GameObject optionsPanel;

        public Button onePlayerButton;
        public Button twoPlayersButton;
        public Button optionsButton;
        public Button quitButton;

        public Text bestScoreText;

        private void Awake()
        {
            if (onePlayerButton != null) onePlayerButton.onClick.AddListener(OnOnePlayer);
            if (twoPlayersButton != null) twoPlayersButton.onClick.AddListener(OnTwoPlayers);
            if (optionsButton != null) optionsButton.onClick.AddListener(OnOptions);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuit);
        }

        private void Start()
        {
            if (mainPanel != null) mainPanel.SetActive(true);
            if (optionsPanel != null) optionsPanel.SetActive(false);

            int best = PlayerPrefs.GetInt("OC_BestScore", 0);
            if (bestScoreText != null) bestScoreText.text = "RÉCORD " + best.ToString("000000");

            if (GameManager.Instance != null && GameManager.Instance.Fader != null)
                GameManager.Instance.Fader.FadeIn(0.4f);

            if (GameManager.Instance != null) GameManager.Instance.SetPaused(false);

            if (GameManager.Instance != null && GameManager.Instance.Audio != null)
                GameManager.Instance.Audio.PlayMusic("mus_menu", 0.5f);
        }

        private void OnOnePlayer()
        {
            AudioManager.Sfx("sfx_select", 0.7f);
            GameSession.StartNewRun(1);
            GoTo(SceneNames.CharacterSelection);
        }

        private void OnTwoPlayers()
        {
            AudioManager.Sfx("sfx_select", 0.7f);
            GameSession.StartNewRun(2);
            GoTo(SceneNames.CharacterSelection);
        }

        private void OnOptions()
        {
            AudioManager.Sfx("sfx_select", 0.6f);
            if (optionsPanel != null) optionsPanel.SetActive(true);
            if (mainPanel != null) mainPanel.SetActive(false);
        }

        private void OnQuit()
        {
            AudioManager.Sfx("sfx_select", 0.6f);
            if (GameManager.Instance != null) GameManager.Instance.QuitGame();
        }

        public void ShowMain()
        {
            if (optionsPanel != null) optionsPanel.SetActive(false);
            if (mainPanel != null) mainPanel.SetActive(true);
        }

        private void GoTo(string scene)
        {
            if (GameManager.Instance != null) GameManager.Instance.LoadScene(scene);
            else UnityEngine.SceneManagement.SceneManager.LoadScene(scene);
        }
    }
}
