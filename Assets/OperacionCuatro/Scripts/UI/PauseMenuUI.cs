using UnityEngine;
using UnityEngine.UI;

namespace OperacionCuatro
{
    /// <summary>Menú de pausa. El objeto que lo contiene debe estar siempre activo.</summary>
    public class PauseMenuUI : MonoBehaviour
    {
        public GameObject panel;
        public Button resumeButton;
        public Button restartButton;
        public Button menuButton;
        public Text titleText;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);

            if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
            if (restartButton != null) restartButton.onClick.AddListener(Restart);
            if (menuButton != null) menuButton.onClick.AddListener(GoToMenu);
        }

        private void Update()
        {
            if (LevelManager.Instance != null && LevelManager.Instance.GameOver && IsOpen) Close();
        }

        public void Toggle()
        {
            if (LevelManager.Instance != null && (LevelManager.Instance.GameOver || LevelManager.Instance.MissionComplete))
                return;

            if (IsOpen) Resume();
            else Open();
        }

        public void Open()
        {
            IsOpen = true;
            if (panel != null) panel.SetActive(true);
            if (GameManager.Instance != null) GameManager.Instance.SetPaused(true);
            AudioManager.Sfx("sfx_select", 0.5f);
        }

        public void Resume()
        {
            IsOpen = false;
            if (panel != null) panel.SetActive(false);
            if (GameManager.Instance != null) GameManager.Instance.SetPaused(false);
            AudioManager.Sfx("sfx_select", 0.5f);
        }

        private void Close()
        {
            IsOpen = false;
            if (panel != null) panel.SetActive(false);
        }

        public void Restart()
        {
            if (GameManager.Instance != null) GameManager.Instance.SetPaused(false);
            if (LevelManager.Instance != null) LevelManager.Instance.RestartLevel();
        }

        public void GoToMenu()
        {
            if (GameManager.Instance != null) GameManager.Instance.SetPaused(false);
            if (LevelManager.Instance != null) LevelManager.Instance.BackToMainMenu();
        }
    }
}
