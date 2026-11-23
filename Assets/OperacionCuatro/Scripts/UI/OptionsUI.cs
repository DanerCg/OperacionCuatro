using UnityEngine;
using UnityEngine.UI;

namespace OperacionCuatro
{
    /// <summary>Menú de opciones: volumen, brillo, restaurar y volver.</summary>
    public class OptionsUI : MonoBehaviour
    {
        public Slider volumeSlider;
        public Slider brightnessSlider;
        public Text volumeLabel;
        public Text brightnessLabel;
        public Button resetButton;
        public Button backButton;
        public MainMenuUI mainMenu;

        private void Awake()
        {
            if (volumeSlider != null) volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            if (brightnessSlider != null) brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
            if (resetButton != null) resetButton.onClick.AddListener(OnReset);
            if (backButton != null) backButton.onClick.AddListener(OnBack);
        }

        private void OnEnable()
        {
            SyncFromOptions();
        }

        private void SyncFromOptions()
        {
            if (volumeSlider != null) volumeSlider.SetValueWithoutNotify(GameOptions.Volume);
            if (brightnessSlider != null) brightnessSlider.SetValueWithoutNotify(GameOptions.Brightness);
            UpdateLabels();
        }

        private void UpdateLabels()
        {
            if (volumeLabel != null)
                volumeLabel.text = "VOLUMEN " + Mathf.RoundToInt(GameOptions.Volume * 100f) + "%";

            if (brightnessLabel != null)
                brightnessLabel.text = "BRILLO " + Mathf.RoundToInt(GameOptions.Brightness * 100f) + "%";
        }

        public void OnVolumeChanged(float value)
        {
            GameOptions.Volume = Mathf.Clamp01(value);
            GameOptions.Save();
            UpdateLabels();
        }

        public void OnBrightnessChanged(float value)
        {
            GameOptions.Brightness = Mathf.Clamp01(value);
            GameOptions.Save();
            UpdateLabels();
        }

        public void OnReset()
        {
            GameOptions.ResetToDefaults();
            SyncFromOptions();
            AudioManager.Sfx("sfx_select", 0.6f);
        }

        public void OnBack()
        {
            AudioManager.Sfx("sfx_select", 0.6f);
            if (mainMenu != null) mainMenu.ShowMain();
            else gameObject.SetActive(false);
        }
    }
}
