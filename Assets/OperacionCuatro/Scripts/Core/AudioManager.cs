using UnityEngine;

namespace OperacionCuatro
{
    /// <summary>
    /// Reproductor de efectos de sonido con pool de AudioSources y volumen global.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private const int PoolSize = 10;
        private const float MusicDefaultVolume = 0.55f;

        private AudioSource[] pool;
        private AudioClip[] clips;
        private int cursor;

        private AudioSource musicSource;
        private string currentMusic;
        private float musicVolume = MusicDefaultVolume;

        private void Awake()
        {
            pool = new AudioSource[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 0f;
                pool[i] = src;
            }

            clips = Resources.LoadAll<AudioClip>("Audio");

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
            musicSource.volume = Mathf.Clamp01(GameOptions.Volume * musicVolume);

            GameOptions.Changed += OnOptionsChanged;
        }

        private void OnDestroy()
        {
            GameOptions.Changed -= OnOptionsChanged;
        }

        private void OnOptionsChanged()
        {
            if (musicSource != null)
                musicSource.volume = Mathf.Clamp01(GameOptions.Volume * musicVolume);
        }

        /// <summary>Reproduce una pista en bucle. La misma pista no se reinicia.</summary>
        public void PlayMusic(string clipName, float volume = MusicDefaultVolume)
        {
            if (musicSource == null) return;

            musicVolume = Mathf.Clamp01(volume);

            if (currentMusic == clipName && musicSource.isPlaying)
            {
                musicSource.volume = Mathf.Clamp01(GameOptions.Volume * musicVolume);
                return;
            }

            var clip = FindClip(clipName);
            if (clip == null)
            {
                Debug.LogWarning("[OC] Pista de música no encontrada en Resources/Audio: " + clipName);
                return;
            }

            currentMusic = clipName;
            musicSource.clip = clip;
            musicSource.loop = true;
            musicSource.volume = Mathf.Clamp01(GameOptions.Volume * musicVolume);
            musicSource.Play();
        }

        public void StopMusic()
        {
            currentMusic = null;
            if (musicSource == null) return;
            musicSource.Stop();
            musicSource.clip = null;
        }

        /// <summary>Pista actual (vacío si no hay música sonando).</summary>
        public string CurrentMusic { get { return musicSource != null && musicSource.isPlaying ? currentMusic : null; } }

        /// <summary>Número de clips cargados desde Resources/Audio (efectos + música).</summary>
        public int LoadedClipCount { get { return clips == null ? 0 : clips.Length; } }

        public void Play(string clipName, float volume = 1f, float pitchJitter = 0.05f)
        {
            var clip = FindClip(clipName);
            if (clip == null) return;

            var src = pool[cursor];
            cursor = (cursor + 1) % PoolSize;

            src.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            src.clip = clip;
            src.volume = Mathf.Clamp01(volume * GameOptions.Volume);
            src.Play();
        }

        public void PlayAt(string clipName, Vector3 position, float volume = 1f, float pitchJitter = 0.05f)
        {
            Play(clipName, volume, pitchJitter);
        }

        private AudioClip FindClip(string clipName)
        {
            if (clips == null) return null;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null && clips[i].name == clipName) return clips[i];
            }
            return null;
        }

        public static void Sfx(string clipName, float volume = 1f)
        {
            if (GameManager.Instance != null && GameManager.Instance.Audio != null)
                GameManager.Instance.Audio.Play(clipName, volume);
        }
    }
}
