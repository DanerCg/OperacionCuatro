using UnityEngine;

namespace OperacionCuatro
{
    /// <summary>
    /// Preferencias del jugador persistidas con PlayerPrefs.
    /// </summary>
    public static class GameOptions
    {
        public const string VolumeKey = "OC_Volume";
        public const string BrightnessKey = "OC_Brightness";

        public const float DefaultVolume = 0.8f;
        public const float DefaultBrightness = 0.5f;

        private static bool loaded;

        /// <summary>Volumen global 0..1.</summary>
        public static float Volume = DefaultVolume;

        /// <summary>Brillo del contenido 0..1 (0.5 = neutro).</summary>
        public static float Brightness = DefaultBrightness;

        public static event System.Action Changed;

        public static void Load()
        {
            Volume = PlayerPrefs.GetFloat(VolumeKey, DefaultVolume);
            Brightness = PlayerPrefs.GetFloat(BrightnessKey, DefaultBrightness);
            Volume = Mathf.Clamp01(Volume);
            Brightness = Mathf.Clamp01(Brightness);
            loaded = true;
        }

        public static void Save()
        {
            PlayerPrefs.SetFloat(VolumeKey, Volume);
            PlayerPrefs.SetFloat(BrightnessKey, Brightness);
            PlayerPrefs.Save();
            Notify();
        }

        public static void ResetToDefaults()
        {
            Volume = DefaultVolume;
            Brightness = DefaultBrightness;
            Save();
        }

        public static void Notify()
        {
            if (Changed != null) Changed();
        }

        public static void EnsureLoaded()
        {
            if (!loaded) Load();
        }

        /// <summary>Exposición (en EV) derivada del brillo, aplicada al contenido del juego.</summary>
        public static float PostExposure
        {
            get
            {
                float b = Mathf.Clamp01(Brightness);
                return b >= 0.5f
                    ? Mathf.Lerp(0f, 1.1f, (b - 0.5f) * 2f)
                    : Mathf.Lerp(0f, -0.85f, (0.5f - b) * 2f);
            }
        }
    }
}
