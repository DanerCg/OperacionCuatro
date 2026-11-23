using UnityEngine;
using UnityEngine.UI;

namespace OperacionCuatro
{
    /// <summary>Panel de información de un jugador dentro del HUD.</summary>
    public class PlayerHudPanel : MonoBehaviour
    {
        public GameObject root;
        public Text nameText;
        public Text livesText;
        public Text bombsText;
        public Image healthFill;
        public Image portrait;
        public Text slotText;
        public Text weaponText;

        public PlayerController2D player;
        public int playerIndex;

        public void Bind(PlayerController2D controller, int index)
        {
            player = controller;
            playerIndex = index;

            if (root != null) root.SetActive(controller != null);
            if (nameText != null)
                nameText.text = "P" + (index + 1) + " " + CharacterDb.GetName(GameSession.GetCharacter(index));

            if (portrait != null)
            {
                // Retrato pixel art del personaje; si el asset no existe se mantiene el
                // bloque de color de siempre como respaldo.
                var id = GameSession.GetCharacter(index);
                var sprite = Resources.Load<Sprite>(CharacterDb.GetPortraitPath(id));

                if (sprite != null)
                {
                    portrait.sprite = sprite;
                    portrait.color = Color.white;
                    portrait.preserveAspect = true;
                }
                else
                {
                    portrait.sprite = null;
                    portrait.color = CharacterDb.GetColor(id);
                }
            }

            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (player == null) return;

            if (healthFill != null)
            {
                float value = player.health != null ? player.health.Normalized : 0f;
                healthFill.fillAmount = Mathf.Lerp(healthFill.fillAmount, value, Time.unscaledDeltaTime * 10f);

                if (healthFill.fillAmount > 0.55f) healthFill.color = new Color(0.29f, 0.9f, 0.36f);
                else if (healthFill.fillAmount > 0.28f) healthFill.color = new Color(0.98f, 0.78f, 0.2f);
                else healthFill.color = new Color(0.92f, 0.22f, 0.2f);
            }

            if (bombsText != null)
            {
                int bombs = player.shooter != null ? player.shooter.Bombs : 0;
                bombsText.text = "BOMBAS " + bombs;
            }

            if (livesText != null)
                livesText.text = "VIDAS " + GameSession.GetLives(playerIndex);

            // Estado del arma especial (solo los HUD que tengan el campo asignado).
            if (weaponText != null)
            {
                int laser = player.shooter != null ? player.shooter.LaserAmmo : 0;
                int heavy = player.shooter != null ? player.shooter.HeavyAmmo : 0;

                if (laser > 0)
                {
                    weaponText.text = "LÁSER " + laser;
                    weaponText.color = new Color(0.42f, 0.92f, 1f);
                }
                else if (heavy > 0)
                {
                    weaponText.text = "AMETRALLADORA " + heavy;
                    weaponText.color = new Color(1f, 0.79f, 0.25f);
                }
                else
                {
                    weaponText.text = "ARMA BÁSICA";
                    weaponText.color = new Color(1f, 1f, 1f, 0.55f);
                }
            }
        }
    }
}
