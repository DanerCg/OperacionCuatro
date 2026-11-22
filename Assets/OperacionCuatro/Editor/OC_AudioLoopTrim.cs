#if UNITY_EDITOR
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace OperacionCuatro.EditorTools
{
    /// <summary>
    /// Recorta el silencio digital que mus_level02 tiene al principio y al final para que el
    /// bucle de la música (AudioSource.loop = true) no deje un hueco audible de casi un segundo.
    ///
    /// - El MP3 original se conserva como master fuera de Resources (Assets/OperacionCuatro/Audio/Masters).
    /// - El clip de juego pasa a ser un WAV recortado con el MISMO nombre, de modo que
    ///   Resources.Load("Audio/mus_level02") y AudioManager.PlayMusic("mus_level02") siguen
    ///   funcionando sin tocar código.
    /// - El recorte usa las mismas ventanas de 50 ms y el mismo umbral relativo que la auditoría,
    ///   y añade rampas de 12 ms en los extremos para que la costura del bucle no chasquee.
    /// </summary>
    public static class OC_AudioLoopTrim
    {
        private const string SourcePath = "Assets/OperacionCuatro/Resources/Audio/mus_level02.mp3";
        private const string MasterFolder = "Assets/OperacionCuatro/Audio/Masters";
        private const string MasterPath = MasterFolder + "/mus_level02.mp3";
        private const string LoopPath = "Assets/OperacionCuatro/Resources/Audio/mus_level02.wav";

        private const float WindowSeconds = 0.05f;   // mismas ventanas que la auditoría
        private const float ThresholdFactor = 0.05f; // umbral = 5 % del RMS medio
        private const float FadeSeconds = 0.012f;    // rampa corta en los extremos

        [MenuItem("OC/Audio/Recortar silencio de bucle en mus_level02")]
        public static void Execute()
        {
            if (File.Exists(LoopPath))
            {
                UnityEngine.Debug.LogWarning("[AUDIO] " + LoopPath + " ya existe: no se vuelve a recortar. "
                    + "Borra el WAV y devuelve el MP3 a Resources/Audio si necesitas repetir el proceso.");
                return;
            }

            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(SourcePath);
            if (clip == null)
            {
                UnityEngine.Debug.LogError("[AUDIO] no se encontró " + SourcePath);
                return;
            }

            if (clip.loadState != AudioDataLoadState.Loaded)
            {
                clip.LoadAudioData();
                var wait = Stopwatch.StartNew();
                while (clip.loadState != AudioDataLoadState.Loaded && wait.ElapsedMilliseconds < 5000)
                    System.Threading.Thread.Sleep(20);
            }

            int channels = Mathf.Max(1, clip.channels);
            int frequency = Mathf.Max(1, clip.frequency);
            int total = clip.samples;

            if (total <= 0 || clip.loadState != AudioDataLoadState.Loaded)
            {
                UnityEngine.Debug.LogError("[AUDIO] no se pudo cargar la onda de " + SourcePath + " (estado=" + clip.loadState + ")");
                return;
            }

            var data = new float[total * channels];
            if (!clip.GetData(data, 0))
            {
                UnityEngine.Debug.LogError("[AUDIO] GetData falló para " + SourcePath + " (estado=" + clip.loadState + ")");
                return;
            }

            int window = Mathf.Max(1, Mathf.RoundToInt(frequency * WindowSeconds));
            int windows = total / window;
            var rms = new float[windows];
            double sum = 0.0;

            for (int w = 0; w < windows; w++)
            {
                double acc = 0.0;
                int baseIndex = w * window;
                for (int i = 0; i < window; i++)
                {
                    float sample = data[(baseIndex + i) * channels];
                    acc += sample * sample;
                }

                rms[w] = Mathf.Sqrt((float)(acc / window));
                sum += rms[w];
            }

            float meanRms = windows > 0 ? (float)(sum / windows) : 0f;
            float threshold = meanRms * ThresholdFactor;

            int firstLoud = -1;
            int lastLoud = -1;

            for (int w = 0; w < windows; w++)
            {
                if (rms[w] < threshold) continue;
                if (firstLoud < 0) firstLoud = w;
                lastLoud = w;
            }

            if (firstLoud < 0 || lastLoud <= firstLoud)
            {
                UnityEngine.Debug.LogError("[AUDIO] la pista parece silencio (RMS medio=" + meanRms + "): no se recorta nada");
                return;
            }

            int startSample = firstLoud * window;
            int endSample = Mathf.Min(total, (lastLoud + 1) * window);
            int trimmed = endSample - startSample;

            // Copia recortada con rampas en los extremos: el bucle empieza y acaba en cero,
            // de modo que la costura no produce ningún chasquido.
            var output = new float[trimmed * channels];
            int fade = Mathf.Clamp(Mathf.RoundToInt(frequency * FadeSeconds), 1, Mathf.Max(1, trimmed / 4));

            for (int i = 0; i < trimmed; i++)
            {
                float gain = 1f;
                if (i < fade) gain = i / (float)fade;
                else if (i >= trimmed - fade) gain = (trimmed - 1 - i) / (float)fade;

                for (int c = 0; c < channels; c++)
                    output[i * channels + c] = data[(startSample + i) * channels + c] * gain;
            }

            WriteWav(LoopPath, output, channels, frequency);

            if (!AssetDatabase.IsValidFolder("Assets/OperacionCuatro/Audio"))
                AssetDatabase.CreateFolder("Assets/OperacionCuatro", "Audio");
            if (!AssetDatabase.IsValidFolder(MasterFolder))
                AssetDatabase.CreateFolder("Assets/OperacionCuatro/Audio", "Masters");

            string moveError = AssetDatabase.MoveAsset(SourcePath, MasterPath);
            if (!string.IsNullOrEmpty(moveError))
                UnityEngine.Debug.LogWarning("[AUDIO] no se pudo mover el MP3 original fuera de Resources: " + moveError);

            AssetDatabase.ImportAsset(LoopPath, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(LoopPath) as AudioImporter;
            if (importer != null)
            {
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.PCM;
                settings.preloadAudioData = true;
                importer.defaultSampleSettings = settings;
                importer.forceToMono = false;
                importer.SaveAndReimport();
            }

            var loop = AssetDatabase.LoadAssetAtPath<AudioClip>(LoopPath);
            string result = loop != null
                ? "nuevoClip=" + LoopPath + " duración=" + loop.length.ToString("0.00") + "s muestras=" + loop.samples
                : "nuevo clip NO cargado";

            UnityEngine.Debug.Log("[AUDIO] recorte de bucle -> origen=" + SourcePath
                                  + " silencioInicialRecortado=" + (startSample / (float)frequency).ToString("0.000") + "s"
                                  + " silencioFinalRecortado=" + ((total - endSample) / (float)frequency).ToString("0.000") + "s"
                                  + " rampa=" + (fade / (float)frequency).ToString("0.000") + "s"
                                  + " master=" + MasterPath + " " + result);
        }

        private static void WriteWav(string assetPath, float[] interleaved, int channels, int frequency)
        {
            string relative = assetPath.Substring("Assets/".Length).Replace('/', Path.DirectorySeparatorChar);
            string absolute = Path.Combine(Application.dataPath, relative);

            string folder = Path.GetDirectoryName(absolute);
            if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder)) Directory.CreateDirectory(folder);

            int sampleCount = interleaved.Length;
            int dataBytes = sampleCount * 2;

            using (var stream = new FileStream(absolute, FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + dataBytes);
                writer.Write(Encoding.ASCII.GetBytes("WAVE"));
                writer.Write(Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)channels);
                writer.Write(frequency);
                writer.Write(frequency * channels * 2);
                writer.Write((short)(channels * 2));
                writer.Write((short)16);
                writer.Write(Encoding.ASCII.GetBytes("data"));
                writer.Write(dataBytes);

                for (int i = 0; i < sampleCount; i++)
                    writer.Write((short)Mathf.Clamp(Mathf.RoundToInt(interleaved[i] * 32767f), -32768, 32767));
            }
        }
    }
}
#endif
