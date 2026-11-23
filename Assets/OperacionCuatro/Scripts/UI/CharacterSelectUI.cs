using UnityEngine;
using UnityEngine.UI;

namespace OperacionCuatro
{
    /// <summary>Pantalla de selección de personajes (1 y 2 jugadores).</summary>
    public class CharacterSelectUI : MonoBehaviour
    {
        public Button[] characterButtons = new Button[4];
        public Text[] characterNames = new Text[4];
        public Text[] characterRoles = new Text[4];
        public Image[] p1Marks = new Image[4];
        public Image[] p2Marks = new Image[4];

        public Text p1SelectionText;
        public Text p2SelectionText;
        public Text hintText;
        public Text modeText;

        public Button startButton;
        public Button backButton;

        private int activeSlot;

        private void Awake()
        {
            for (int i = 0; i < characterButtons.Length; i++)
            {
                int index = i;
                if (characterButtons[i] != null)
                    characterButtons[i].onClick.AddListener(delegate { OnCharacterClicked(index); });
            }

            if (startButton != null) startButton.onClick.AddListener(OnStart);
            if (backButton != null) backButton.onClick.AddListener(OnBack);
        }

        private void Start()
        {
            if (GameSession.PlayerCount < 1) GameSession.PlayerCount = 1;
            GameSession.SetCharacter(0, CharacterId.Danner);
            if (GameSession.PlayerCount == 2) GameSession.SetCharacter(1, CharacterId.Cesar);

            activeSlot = 0;

            for (int i = 0; i < characterNames.Length; i++)
            {
                if (characterNames[i] != null) characterNames[i].text = CharacterDb.Names[i];
                if (characterRoles[i] != null) characterRoles[i].text = CharacterDb.Roles[i];
            }

            if (hintText != null)
                hintText.text = GameSession.PlayerCount == 2
                    ? "JUGADOR 1 ELIGE PERSONAJE"
                    : "PULSA UNA TARJETA PARA ELEGIR";

            if (modeText != null)
                modeText.text = GameSession.PlayerCount == 2 ? "MODO 2 JUGADORES" : "MODO 1 JUGADOR";

            if (GameManager.Instance != null && GameManager.Instance.Fader != null)
                GameManager.Instance.Fader.FadeIn(0.4f);

            Refresh();
        }

        public void OnCharacterClicked(int index)
        {
            AudioManager.Sfx("sfx_select", 0.6f);

            if (GameSession.PlayerCount == 2)
            {
                if (activeSlot == 0)
                {
                    GameSession.SetCharacter(0, (CharacterId)index);
                    activeSlot = 1;
                    if (hintText != null) hintText.text = "JUGADOR 2 ELIGE PERSONAJE";
                }
                else
                {
                    if ((int)GameSession.P1 == index)
                    {
                        if (hintText != null) hintText.text = "PERSONAJE OCUPADO POR J1";
                        return;
                    }

                    GameSession.SetCharacter(1, (CharacterId)index);
                    activeSlot = 0;
                    if (hintText != null) hintText.text = "LISTO - PULSA INICIAR";
                }
            }
            else
            {
                GameSession.SetCharacter(0, (CharacterId)index);
                if (hintText != null) hintText.text = "LISTO - PULSA INICIAR";
            }

            Refresh();
        }

        private void Refresh()
        {
            for (int i = 0; i < 4; i++)
            {
                if (p1Marks[i] != null)
                {
                    bool on = (int)GameSession.P1 == i;
                    p1Marks[i].color = on ? CharacterDb.GetColor((CharacterId)i) : new Color(1f, 1f, 1f, 0.06f);
                }

                if (p2Marks[i] != null)
                {
                    bool visible = GameSession.PlayerCount == 2;
                    p2Marks[i].gameObject.SetActive(visible);
                    if (visible)
                    {
                        bool on = (int)GameSession.P2 == i;
                        p2Marks[i].color = on ? CharacterDb.GetColor((CharacterId)i) : new Color(1f, 1f, 1f, 0.06f);
                    }
                }
            }

            if (p1SelectionText != null)
                p1SelectionText.text = "P1: " + CharacterDb.GetName(GameSession.P1);

            if (p2SelectionText != null)
            {
                bool two = GameSession.PlayerCount == 2;
                p2SelectionText.gameObject.SetActive(two);
                p2SelectionText.text = two ? "P2: " + CharacterDb.GetName(GameSession.P2) : "";
            }

            if (startButton != null) startButton.interactable = true;
        }

        public void OnStart()
        {
            AudioManager.Sfx("sfx_start", 0.8f);

            // Una partida nueva empieza siempre por el primer nivel, conservando la
            // selección de personajes y el número de jugadores elegidos en este flujo.
            GameSession.StartLevel(GameSession.FirstLevel);

            string scene = GameSession.GetLevelSceneName();
            if (GameManager.Instance != null) GameManager.Instance.LoadScene(scene);
            else UnityEngine.SceneManagement.SceneManager.LoadScene(scene);
        }

        public void OnBack()
        {
            AudioManager.Sfx("sfx_select", 0.6f);
            if (GameManager.Instance != null) GameManager.Instance.LoadScene(SceneNames.MainMenu);
            else UnityEngine.SceneManagement.SceneManager.LoadScene(SceneNames.MainMenu);
        }
    }
}
