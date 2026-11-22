using UnityEngine;
using UnityEngine.SceneManagement;

namespace OperacionCuatro
{
    public static class SceneNames
    {
        public const string MainMenu = "MainMenu";
        public const string CharacterSelection = "CharacterSelection";
        public const string Level01 = "Level01";
        public const string Level02 = "Level02";
        public const string Results = "Results";
    }

    /// <summary>
    /// Nodo raíz persistente de sistemas (transición, audio, brillo).
    /// Se crea automáticamente al entrar en Play en cualquier escena.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public SceneFader Fader { get; private set; }
        public AudioManager Audio { get; private set; }
        public BrightnessController Brightness { get; private set; }

        public bool IsPaused { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("[OC_GameManager]");
            DontDestroyOnLoad(go);
            go.AddComponent<GameManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            GameOptions.EnsureLoaded();

            Fader = gameObject.AddComponent<SceneFader>();
            Audio = gameObject.AddComponent<AudioManager>();
            Brightness = gameObject.AddComponent<BrightnessController>();
        }

        private void Update()
        {
            if (InputPausePressed())
            {
                var pause = Object.FindFirstObjectByType<PauseMenuUI>();
                if (pause != null) pause.Toggle();
            }
        }

        private static bool InputPausePressed()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) return true;

            var pad = UnityEngine.InputSystem.Gamepad.current;
            if (pad != null && pad.startButton.wasPressedThisFrame) return true;

            return false;
        }

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
        }

        public void LoadScene(string sceneName)
        {
            SetPaused(false);
            if (Fader != null) Fader.LoadScene(sceneName);
            else SceneManager.LoadScene(sceneName);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
