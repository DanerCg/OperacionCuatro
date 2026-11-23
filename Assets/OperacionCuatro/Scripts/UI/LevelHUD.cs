using UnityEngine;
using UnityEngine.UI;

namespace OperacionCuatro
{
    /// <summary>HUD del nivel: paneles por jugador, puntuación y pantallas de estado.</summary>
    public class LevelHUD : MonoBehaviour
    {
        public PlayerHudPanel panelP1;
        public PlayerHudPanel panelP2;
        public Text scoreText;
        public Text messageText;
        public CanvasGroup messageGroup;
        public GameObject gameOverPanel;
        public GameObject missionCompletePanel;
        public Text gameOverScoreText;
        public Text missionScoreText;

        private float messageTimer;

        public void Bind(PlayerController2D p1, PlayerController2D p2)
        {
            if (panelP1 != null) panelP1.Bind(p1, 0);
            if (panelP2 != null) panelP2.Bind(p2, 1);
        }

        public void SetPlayerCount(int count)
        {
            if (panelP2 != null && panelP2.root != null) panelP2.root.SetActive(count > 1);
        }

        public void RefreshLives(int index, int lives)
        {
            var panel = index == 0 ? panelP1 : panelP2;
            if (panel != null) panel.Refresh();
        }

        private void Update()
        {
            if (scoreText != null)
                scoreText.text = "PUNTOS " + GameSession.Score.ToString("000000");

            if (messageText != null && messageGroup != null)
            {
                if (messageTimer > 0f)
                {
                    messageTimer -= Time.unscaledDeltaTime;
                    messageGroup.alpha = Mathf.Clamp01(messageTimer);
                }
                else
                {
                    messageGroup.alpha = 0f;
                }
            }
        }

        public void ShowMessage(string message)
        {
            if (messageText == null) return;
            messageText.text = message;
            messageTimer = 2.2f;
            if (messageGroup != null) messageGroup.alpha = 1f;
        }

        public void ShowGameOver()
        {
            if (gameOverPanel == null) return;
            gameOverPanel.SetActive(true);
            if (gameOverScoreText != null)
                gameOverScoreText.text = "PUNTUACIÓN FINAL " + GameSession.Score.ToString("000000");
        }

        public void ShowMissionComplete()
        {
            if (missionCompletePanel == null) return;
            missionCompletePanel.SetActive(true);
            if (missionScoreText != null)
                missionScoreText.text = "PUNTUACIÓN " + GameSession.Score.ToString("000000");
        }
    }
}
