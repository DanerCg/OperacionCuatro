#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

namespace OperacionCuatro.EditorTools
{
    /// <summary>
    /// Fase 2 — AUDITORÍA de cierre: balance de la oleada del generador, vapor industrial
    /// sobre la cinta, rendimiento por sección y análisis objetivo de la música.
    ///
    /// Todas las cifras del informe proceden de medidas hechas en Play Mode sobre la escena
    /// real: no hay valores estimados salvo los que se etiquetan expresamente como cálculo
    /// teórico (techo de daño/tiempo a partir de las estadísticas de los scripts).
    /// </summary>
    public static class OC_Phase2Audit
    {
        [UnityEditor.MenuItem("OC/Playtest/Fase 2 auditoría (balance, vapor, rendimiento, audio)")]
        public static void Run()
        {
            var go = new GameObject("[OC_Phase2Audit]");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<OCPhase2AuditRunner>();
        }
    }

    public class OCPhase2AuditRunner : MonoBehaviour
    {
        private const string Prefix = "[P2]";

        private int passed;
        private int failed;

        private LevelManager lm;
        private PlayerController2D p1;
        private PlayerController2D p2;
        private Health h1;
        private Health h2;

        private Keyboard kb;
        private readonly List<EnemyBase> parked = new List<EnemyBase>();
        private readonly List<DamageOnTouch> parkedTouch = new List<DamageOnTouch>();

        // ==================================================================
        //  Registro y utilidades
        // ==================================================================
        private void Check(bool ok, string what, string detail = "")
        {
            if (ok) { passed++; Debug.Log(Prefix + " PASS  " + what + (detail.Length > 0 ? " | " + detail : "")); }
            else { failed++; Debug.LogError(Prefix + " FAIL  " + what + (detail.Length > 0 ? " | " + detail : "")); }
        }

        private void Note(string text) { Debug.Log(Prefix + " ....  " + text); }

        private static T[] All<T>() where T : Object
        {
            return Object.FindObjectsByType<T>(FindObjectsSortMode.None);
        }

        private static T[] AllIncludingInactive<T>() where T : Object
        {
            return Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        private static T Nearest<T>(float x) where T : Component
        {
            var list = All<T>();
            T best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < list.Length; i++)
            {
                float d = Mathf.Abs(list[i].transform.position.x - x);
                if (d < bestDistance) { bestDistance = d; best = list[i]; }
            }

            return best;
        }

        private Keyboard Kb()
        {
            if (kb == null || !kb.added) kb = Keyboard.current;
            if (kb == null) kb = InputSystem.AddDevice<Keyboard>();
            return kb;
        }

        private void Hold(params Key[] keys) { InputSystem.QueueStateEvent(Kb(), new KeyboardState(keys)); }
        private void ReleaseAll() { InputSystem.QueueStateEvent(Kb(), new KeyboardState()); }

        /// <summary>
        /// El Input System descarta el teclado mientras el Game View no tiene el foco
        /// («Pointers and Keyboards Respect Game View Focus»). Se enfoca la ventana y se
        /// reactiva el dispositivo: acciones de esta sesión de Play Mode, ningún ajuste
        /// del proyecto se modifica.
        /// </summary>
        private bool PrepInput()
        {
            var k = Kb();
            if (k == null) return false;

            FocusGameView();
            if (!k.enabled) InputSystem.EnableDevice(k);

            return k.enabled;
        }

        private static bool FocusGameView()
        {
            var type = System.Type.GetType("UnityEditor.GameView, UnityEditor");
            if (type == null) return false;

            var window = UnityEditor.EditorWindow.GetWindow(type, false, null, false);
            if (window == null) return false;

            window.Focus();
            window.Repaint();
            return window.hasFocus;
        }

        private static void Teleport(PlayerController2D target, float x, float y)
        {
            if (target == null) return;

            var position = new Vector3(x, y, target.transform.position.z);
            var body = target.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.position = position;
            }

            target.transform.position = position;
            Physics2D.SyncTransforms();
        }

        private static bool GroundSurfaceAt(float x, out float surfaceY)
        {
            var hit = Physics2D.Raycast(new Vector2(x, 40f), Vector2.down, 60f, 1 << 8);
            surfaceY = hit.collider != null ? hit.point.y : -999f;
            return hit.collider != null;
        }

        private static void StandAt(PlayerController2D target, float x, float lift)
        {
            float surfaceY;
            if (!GroundSurfaceAt(x, out surfaceY)) surfaceY = 0f;
            Teleport(target, x, surfaceY + lift);
        }

        private static void MakeVulnerable(PlayerController2D player, Health health)
        {
            if (player != null) player.ClearInvulnerability();
            if (health != null) { health.ResetHealth(); health.invulnerable = false; }
        }

        private IEnumerator WaitUntil(System.Func<bool> condition, float timeout)
        {
            float end = Time.time + timeout;
            while (Time.time < end && !condition()) yield return null;
        }

        private void Acquire()
        {
            lm = LevelManager.Instance;
            p1 = lm != null && lm.Players.Count > 0 ? lm.Players[0] : null;
            p2 = lm != null && lm.Players.Count > 1 ? lm.Players[1] : null;
            h1 = p1 != null ? p1.health : null;
            h2 = p2 != null ? p2.health : null;
        }

        private void ParkEnemies()
        {
            var all = All<EnemyBase>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null || !all[i].enabled) continue;
                all[i].enabled = false;
                parked.Add(all[i]);
            }

            // El daño por contacto vive en un HitBox hijo con DamageOnTouch, que es un
            // MonoBehaviour aparte: apagar el EnemyBase NO lo detiene. Sin esto, un guardia
            // aparcado seguía golpeando al jugador que cruzaba el vapor (Armored_52, x≈61).
            var touches = All<DamageOnTouch>();
            for (int i = 0; i < touches.Length; i++)
            {
                if (touches[i] == null || !touches[i].enabled) continue;
                touches[i].enabled = false;
                parkedTouch.Add(touches[i]);
            }
        }

        private void UnparkEnemies()
        {
            for (int i = 0; i < parked.Count; i++)
                if (parked[i] != null) parked[i].enabled = true;

            for (int i = 0; i < parkedTouch.Count; i++)
                if (parkedTouch[i] != null) parkedTouch[i].enabled = true;

            parked.Clear();
            parkedTouch.Clear();
        }

        // ---- Aislamiento del daño por contacto durante las medidas del vapor -----------
        // DamageOnTouch es un MonoBehaviour aparte del EnemyBase: apagar la IA no lo detiene.
        // Se silencia de forma explícita (incluyendo GameObjects inactivos) y se restaura
        // después, para que B2/B3 midan el vapor y no el contacto de un guardia cercano.
        private readonly List<DamageOnTouch> silencedTouch = new List<DamageOnTouch>();

        private int SilenceContactDamage()
        {
            silencedTouch.Clear();
            var touches = AllIncludingInactive<DamageOnTouch>();

            for (int i = 0; i < touches.Length; i++)
            {
                if (touches[i] == null || !touches[i].enabled) continue;
                touches[i].enabled = false;
                silencedTouch.Add(touches[i]);
            }

            return silencedTouch.Count;
        }

        private void RestoreContactDamage()
        {
            for (int i = 0; i < silencedTouch.Count; i++)
                if (silencedTouch[i] != null) silencedTouch[i].enabled = true;

            silencedTouch.Clear();
        }

        private static int CountEnabledTouch()
        {
            var touches = AllIncludingInactive<DamageOnTouch>();
            int enabled = 0;
            for (int i = 0; i < touches.Length; i++)
                if (touches[i] != null && touches[i].enabled) enabled++;

            return enabled;
        }

        /// <summary>Vuelve a silenciar cualquier DamageOnTouch que se haya reactivado.</summary>
        private int SilenceAnyTouchAgain()
        {
            int count = 0;
            var touches = AllIncludingInactive<DamageOnTouch>();
            for (int i = 0; i < touches.Length; i++)
            {
                if (touches[i] == null || !touches[i].enabled) continue;
                touches[i].enabled = false;
                if (!silencedTouch.Contains(touches[i])) silencedTouch.Add(touches[i]);
                count++;
            }

            return count;
        }

        private static string PathOf(Transform t)
        {
            if (t == null) return "?";

            string path = t.name;
            var parent = t.parent;
            while (parent != null) { path = parent.name + "/" + path; parent = parent.parent; }

            return path;
        }

        /// <summary>Ruta, estado y componentes del emisor de un golpe, para no atribuirlo al azar.</summary>
        private static string DescribeDamageSource(GameObject source)
        {
            if (source == null) return "sin fuente";

            string comps = "";
            var behaviours = source.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] == null) { comps += "missing "; continue; }
                comps += behaviours[i].GetType().Name + "=" + behaviours[i].enabled + " ";
            }

            var colliders = source.GetComponents<Collider2D>();
            for (int i = 0; i < colliders.Length; i++)
                comps += colliders[i].GetType().Name + "=" + colliders[i].enabled + " ";

            return PathOf(source.transform) + " activo=" + source.activeInHierarchy
                   + " pos=" + source.transform.position.ToString("0.00")
                   + " comps=[" + comps.Trim() + "]";
        }

        /// <summary>Traza corta de la pila: identifica quién llamó a TakeDamage.</summary>
        private static string ShortTrace()
        {
            var trace = new System.Diagnostics.StackTrace(true);
            var sb = new System.Text.StringBuilder();

            int frames = Mathf.Min(8, trace.FrameCount);
            for (int i = 0; i < frames; i++)
            {
                var frame = trace.GetFrame(i);
                var method = frame != null ? frame.GetMethod() : null;
                if (method == null) continue;

                if (sb.Length > 0) sb.Append(" < ");
                sb.Append(method.DeclaringType != null ? method.DeclaringType.Name : "?")
                  .Append('.').Append(method.Name);
            }

            return sb.ToString();
        }

        private string Environment()
        {
            return "unity=" + Application.unityVersion
                   + " editor=" + Application.isEditor
                   + " plataforma=" + Application.platform
                   + " cpu=" + SystemInfo.processorType + " (" + SystemInfo.processorCount + " nucleos)"
                   + " gpu=" + SystemInfo.graphicsDeviceName
                   + " vSync=" + QualitySettings.vSyncCount
                   + " targetFPS=" + Application.targetFrameRate
                   + " resolucion=" + Screen.width + "x" + Screen.height
                   + " profilerHabilitado=" + Profiler.enabled;
        }

        private IEnumerator LoadLevel(int players, CharacterId a, CharacterId b)
        {
            GameSession.StartNewRun(players);
            GameSession.SetCharacter(0, a);
            if (players > 1) GameSession.SetCharacter(1, b);
            GameSession.StartLevel(2);

            SceneManager.LoadScene(SceneNames.Level02);
            yield return null;
            yield return new WaitForSecondsRealtime(2.2f);

            Acquire();
        }

        // ==================================================================
        //  FASE A — AUDIO: onda de mus_level02
        // ==================================================================
        private IEnumerator AudioTests()
        {
            var clip = Resources.Load<AudioClip>("Audio/mus_level02");

            if (clip == null)
            {
                Check(false, "A1. El clip mus_level02 se carga desde Resources/Audio", "clip no encontrado");
                yield break;
            }

            if (clip.loadState != AudioDataLoadState.Loaded)
            {
                clip.LoadAudioData();
                float loadEnd = Time.realtimeSinceStartup + 3f;
                while (clip.loadState != AudioDataLoadState.Loaded && Time.realtimeSinceStartup < loadEnd)
                    yield return null;
            }

            int channels = Mathf.Max(1, clip.channels);
            int totalSamples = clip.samples;
            float duration = clip.length;

            string musicNow = "sin AudioManager";
            var audio = Object.FindFirstObjectByType<AudioManager>();
            if (audio != null) musicNow = audio.CurrentMusic;

            // Onda completa (canal 0).
            float[] data = new float[totalSamples * channels];
            bool read = clip.GetData(data, 0);
            yield return null;

            if (!read)
            {
                Check(false, "A1. El clip mus_level02 se puede leer para analizar la onda",
                    "GetData falló (estado=" + clip.loadState + ")");
                yield break;
            }

            // ---- Silencios internos: RMS por ventanas de 50 ms ----
            int window = Mathf.Max(1, Mathf.RoundToInt(clip.frequency * 0.05f));
            int windows = totalSamples / window;
            var rms = new float[windows];
            double sum = 0.0;

            for (int w = 0; w < windows; w++)
            {
                double acc = 0.0;
                int baseIndex = w * window;
                for (int i = 0; i < window; i++)
                {
                    float s = data[(baseIndex + i) * channels];
                    acc += s * s;
                }
                rms[w] = Mathf.Sqrt((float)(acc / window));
                sum += rms[w];
            }

            float meanRms = windows > 0 ? (float)(sum / windows) : 0f;
            float silenceThreshold = meanRms * 0.05f;

            int firstLoud = -1;
            int lastLoud = -1;
            int silentWindows = 0;

            for (int w = 0; w < windows; w++)
            {
                if (rms[w] >= silenceThreshold)
                {
                    if (firstLoud < 0) firstLoud = w;
                    lastLoud = w;
                }
                else silentWindows++;
            }

            float leadingSilence = firstLoud > 0 ? firstLoud * 0.05f : 0f;
            float trailingSilence = firstLoud >= 0 && lastLoud < windows - 1 ? (windows - 1 - lastLoud) * 0.05f : 0f;

            // Tramo interior: el que rompería la música por el medio.
            int longestSilenceWindows = 0;
            int currentRun = 0;
            for (int w = firstLoud + 1; w < lastLoud; w++)
            {
                if (rms[w] < silenceThreshold)
                {
                    currentRun++;
                    if (currentRun > longestSilenceWindows) longestSilenceWindows = currentRun;
                }
                else currentRun = 0;
            }

            float longestSilence = longestSilenceWindows * 0.05f;

            // ---- Costura del bucle: salto entre el último y el primer sample ----
            float first = data[0];
            float last = data[(totalSamples - 1) * channels];

            double seamAcc = 0.0;
            int seamCount = Mathf.Min(441, totalSamples - 1);
            for (int i = 0; i < seamCount; i++)
            {
                float d = data[(i + 1) * channels] - data[i * channels];
                seamAcc += d * d;
            }
            float seamStep = Mathf.Sqrt((float)(seamAcc / Mathf.Max(1, seamCount)));
            float jump = Mathf.Abs(first - last);
            float jumpRatio = seamStep > 0f ? jump / seamStep : 0f;

            // ---- Estado del reproductor de música ----
            bool looping = audio != null && audio.CurrentMusic == "mus_level02";

            Note("audio: duración=" + duration.ToString("0.00") + "s muestras=" + totalSamples
                 + " canales=" + channels + " frecuencia=" + clip.frequency + "Hz"
                 + " loadState=" + clip.loadState + " musicaActual=" + musicNow);

            Check(duration > 30f && looping,
                "A1. mus_level02 cubre la sección completa y suena en Level02 en bucle",
                "duración=" + duration.ToString("0.00") + "s musicaActual=" + musicNow);

            Check(longestSilence < 0.35f,
                "A2. La pista no tiene silencios en el tramo interior (RMS por ventanas de 50 ms)",
                "ventanas=" + windows + " silencioInteriorMásLargo=" + longestSilence.ToString("0.00") + "s"
                + " ventanasBajoUmbral=" + silentWindows + "/" + windows
                + " RMSmedio=" + meanRms.ToString("0.0000"));

            Check(leadingSilence < 0.2f && trailingSilence < 0.2f,
                "A3. La pista empieza y termina con sonido, de modo que el bucle no deja un hueco audible",
                "silencioInicial=" + leadingSilence.ToString("0.00") + "s"
                + " silencioFinal=" + trailingSilence.ToString("0.00") + "s"
                + " primerTramoConSonido=" + (firstLoud >= 0 ? (firstLoud * 0.05f).ToString("0.00") : "n/d") + "s"
                + " umbralRMS=" + silenceThreshold.ToString("0.0000"));

            Note("audio: costura del bucle -> |primer-last|=" + jump.ToString("0.00000")
                 + " saltoMedioLocal=" + seamStep.ToString("0.00000")
                 + " ratio=" + jumpRatio.ToString("0.00") + "x"
                 + " (ratio < 3 se considera una costura sin clic audible)");
        }

        // ==================================================================
        //  FASE B — VAPOR INDUSTRIAL SOBRE LA CINTA
        // ==================================================================
        private static bool OverlapsVent(SteamVent vent, PlayerController2D player)
        {
            if (vent == null || player == null) return false;

            var ventCollider = vent.GetComponent<Collider2D>();
            var playerCollider = player.GetComponent<Collider2D>();
            if (ventCollider == null || playerCollider == null) return false;

            var a = ventCollider.bounds;
            var b = playerCollider.bounds;

            return a.min.x < b.max.x && a.max.x > b.min.x && a.min.y < b.max.y && a.max.y > b.min.y;
        }

        // Fase actual del ciclo del vapor, con la misma fórmula que SteamVent.Update.
        // 0 = acaba de empezar el aviso; warningTime..warningTime+activeTime = chorro.
        private static float VentPhase(SteamVent vent)
        {
            return Mathf.Repeat(Time.time + vent.startOffset, Mathf.Max(0.5f, vent.cycleTime));
        }

        private IEnumerator VentTests()
        {
            var vent = Nearest<SteamVent>(58f);

            if (vent == null)
            {
                Check(false, "B1. El vapor SteamVent_58 existe y permite medir su ciclo", "sin SteamVent cerca de x=58");
                yield break;
            }

            var ventCollider = vent.GetComponent<Collider2D>();
            var belt = Nearest<ConveyorBelt>(58f);

            int contactSilenced = SilenceContactDamage();
            Note("vapor: daño por contacto -> encontrados=" + AllIncludingInactive<DamageOnTouch>().Length
                 + " silenciados=" + contactSilenced + " siguenActivos=" + CountEnabledTouch());

            Note("vapor: " + vent.name + " pos=" + vent.transform.position.ToString("0.00")
                 + " trigger=" + (ventCollider != null ? ventCollider.bounds.size.ToString("0.00") : "n/d")
                 + " ciclo=" + vent.cycleTime + "s aviso=" + vent.warningTime + "s chorro=" + vent.activeTime
                 + "s daño=" + vent.damage + " cinta=" + (belt != null ? belt.name + " vel=" + belt.beltSpeed : "sin cinta"));

            // ---- Aviso previo: se mide el tiempo entre el aviso y el chorro ----
            yield return WaitUntil(delegate { return vent.IsActive; }, 8f);
            yield return WaitUntil(delegate { return !vent.IsActive; }, 8f);

            float warnAt = -1f;
            float jetAt = -1f;
            bool activeVisualDuringJet = false;
            bool activeVisualWhenIdle = false;
            float cycleEnd = Time.time + 12f;

            while (Time.time < cycleEnd)
            {
                bool warningVisible = vent.warningVisual != null && vent.warningVisual.activeSelf;
                bool activeVisible = vent.activeVisual != null && vent.activeVisual.activeSelf;

                if (warningVisible && warnAt < 0f) warnAt = Time.time;

                if (vent.IsActive)
                {
                    jetAt = Time.time;
                    activeVisualDuringJet = activeVisible;
                    break;
                }

                if (activeVisible) activeVisualWhenIdle = true;

                yield return null;
            }

            float lead = warnAt >= 0f && jetAt >= 0f ? jetAt - warnAt : -1f;

            Check(warnAt >= 0f && jetAt > warnAt && lead > 0.5f && lead < 1.4f && activeVisualDuringJet && !activeVisualWhenIdle,
                "B1. El vapor avisa con una señal visible antes de activarse y el chorro se ve al activarse",
                "avisoVisible=" + (warnAt >= 0f) + " anticipación=" + lead.ToString("0.00") + "s"
                + " (configurada " + vent.warningTime + "s) chorroVisible=" + activeVisualDuringJet
                + " visualActivoEnReposo=" + activeVisualWhenIdle);

            // ---- Daño real: el jugador queda dentro del chorro cuando se activa ----
            if (p2 != null) { StandAt(p2, 120f, 0.15f); p2.SetInputEnabled(false); }
            ReleaseAll();

            yield return WaitUntil(delegate { return vent.IsActive; }, 8f);

            float surfaceBelt;
            if (!GroundSurfaceAt(57.8f, out surfaceBelt)) surfaceBelt = 0.55f;
            Teleport(p1, 57.8f, surfaceBelt + 0.12f);
            MakeVulnerable(p1, h1);

            // La salud de referencia se lee ANTES de ceder un fotograma: si el chorro ya está
            // activo, el primer golpe entra en el mismo fotograma del teletransporte y antes
            // quedaba fuera de la medida (daño=0 con la salud ya rebajada).
            int hpStart = h1.Current;
            yield return null;
            float inside = 0f;
            float damageEnd = Time.time + 2.2f;
            float lastFrame = Time.time;
            float exitX = p1.transform.position.x;

            while (Time.time < damageEnd)
            {
                float now = Time.time;
                if (OverlapsVent(vent, p1)) inside += now - lastFrame;
                lastFrame = now;
                exitX = p1.transform.position.x;
                yield return null;
            }

            int damageTaken = hpStart - h1.Current;

            Check(damageTaken > 0,
                "B2. El jugador que queda dentro del vapor durante la activación sufre daño",
                "daño=" + damageTaken + " (por golpe " + vent.damage + ") permanenciaDentro=" + inside.ToString("0.00") + "s"
                + " salidaX=" + exitX.ToString("0.00") + " salud=" + h1.Current + "/" + h1.maxHealth
                + " cinta=" + (belt != null ? belt.beltSpeed.ToString("0.0") : "n/d"));

            // ---- Aviso aprovechable: cruzar durante el aviso no cuesta salud ----
            // La entrada sintética puede perderse si el Game View pierde el foco: antes de medir
            // se confirma que el personaje responde de verdad (velocidad horizontal real).
            PrepInput();
            bool inputAlive = false;
            float maxVx = 0f;
            var probeBody = p1.GetComponent<Rigidbody2D>();

            for (int attempt = 0; attempt < 3 && !inputAlive; attempt++)
            {
                StandAt(p1, 40f, 0.15f);
                yield return null;

                maxVx = 0f;
                float probeEnd = Time.time + 0.35f;
                while (Time.time < probeEnd)
                {
                    Hold(Key.D);
                    if (probeBody != null) maxVx = Mathf.Max(maxVx, Mathf.Abs(probeBody.linearVelocity.x));
                    yield return null;
                }
                ReleaseAll();
                inputAlive = maxVx > 4f;
            }

            Note("vapor: entrada sintética para la travesía -> confirmada=" + inputAlive
                 + " maxVx=" + maxVx.ToString("0.00"));

            // Se espera al INICIO de un aviso completo. Mirar sólo !IsActive no basta: durante el
            // aviso el chorro tampoco está activo, así que la travesía podía arrancar con el aviso
            // casi agotado y medir un daño que no corresponde a la señal de aviso.
            yield return WaitUntil(delegate { return VentPhase(vent) < 0.15f; }, 8f);
            yield return null;

            MakeVulnerable(p1, h1);
            StandAt(p1, 56.2f, 0.15f);
            yield return null;

            float warnRemaining = Mathf.Max(0f, vent.warningTime - VentPhase(vent));

            // Si la preparación se ha comido parte del aviso, se repite con el siguiente ciclo
            // en vez de medir una travesía que ya no puede ser justa.
            if (warnRemaining < 0.5f)
            {
                yield return WaitUntil(delegate { return VentPhase(vent) < 0.15f; }, 8f);
                yield return null;

                MakeVulnerable(p1, h1);
                StandAt(p1, 56.2f, 0.15f);
                yield return null;

                warnRemaining = Mathf.Max(0f, vent.warningTime - VentPhase(vent));
            }

            int hpCross = h1.Current;

            // El daño se atribuye por emisor: sólo cuenta contra el vapor el golpe cuyo
            // source es el propio SteamVent_58. Cualquier otro emisor se registra aparte con
            // su ruta y su pila de llamadas, para no confundir un guardia con el chorro.
            DamageInfo firstHit = default(DamageInfo);
            bool hasFirstHit = false;
            float firstHitAt = -1f;
            float firstHitX = 0f;
            bool firstHitCylinder = false;
            bool firstHitOverlap = false;
            string firstHitSource = "ninguno";
            int firstHitEnabledTouch = -1;
            string firstHitTrace = "";
            int ventDamage = 0;
            int foreignDamage = 0;
            int resilenced = 0;

            System.Action<DamageInfo> onDamaged = delegate (DamageInfo info)
            {
                if (info.source == vent.gameObject)
                {
                    ventDamage += info.amount;
                    return;
                }

                foreignDamage += info.amount;

                if (hasFirstHit) return;
                hasFirstHit = true;
                firstHit = info;
                firstHitAt = Time.time;
                firstHitX = p1.transform.position.x;
                firstHitCylinder = vent.IsActive;
                firstHitOverlap = OverlapsVent(vent, p1);
                firstHitSource = DescribeDamageSource(info.source);
                firstHitEnabledTouch = CountEnabledTouch();
                firstHitTrace = ShortTrace();
            };
            h1.Damaged += onDamaged;

            float crossStartX = p1.transform.position.x;
            float crossStart = Time.time;
            float crossEnd = Time.time + 1.6f;
            float exitAt = -1f;
            float maxCrossVx = 0f;

            // La ventana termina al dejar atrás el chorro (x=59,5; el trigger acaba en 58,65):
            // prolongarla metía al jugador en el cuerpo del guardia apostado al final de la
            // cinta y medía su contacto, no el vapor.
            while (Time.time < crossEnd && p1.transform.position.x < 59.5f)
            {
                Hold(Key.D);
                resilenced += SilenceAnyTouchAgain();
                if (probeBody != null) maxCrossVx = Mathf.Max(maxCrossVx, Mathf.Abs(probeBody.linearVelocity.x));
                if (exitAt < 0f && p1.transform.position.x > 59.1f) exitAt = Time.time;
                yield return null;
            }

            ReleaseAll();
            h1.Damaged -= onDamaged;

            int crossDamage = hpCross - h1.Current;
            float crossed = p1.transform.position.x - crossStartX;
            float crossTime = exitAt > 0f ? exitAt - crossStart : -1f;

            Check(inputAlive && ventDamage == 0 && crossed > 3f,
                "B3. Cruzar la cinta durante el aviso evita el chorro (el aviso concede tiempo suficiente)",
                "entradaActiva=" + inputAlive + " maxVx=" + maxCrossVx.ToString("0.00")
                + " daño=" + crossDamage + " dañoVapor=" + ventDamage + " dañoAjeno=" + foreignDamage
                + " resellados=" + resilenced
                + " avance=" + crossed.ToString("0.00")
                + " cruce=" + (crossTime > 0f ? crossTime.ToString("0.00") + "s" : "no salió del chorro")
                + " aviso=" + vent.warningTime + "s avisoRestante=" + warnRemaining.ToString("0.00") + "s"
                + " primerAjeno=" + (hasFirstHit
                    ? firstHitSource + "@" + (firstHitAt - crossStart).ToString("0.00") + "s x=" + firstHitX.ToString("0.00")
                      + " chorroActivo=" + firstHitCylinder + " solapaVapor=" + firstHitOverlap
                      + " cantidad=" + firstHit.amount + " touchesActivosEnElGolpe=" + firstHitEnabledTouch
                      + " traza=" + firstHitTrace
                    : "ninguno"));

            RestoreContactDamage();
        }

        // ==================================================================
        //  FASE B2 — geometría de la oleada final
        // ==================================================================
        private void GeometryTests()
        {
            var generator = Object.FindFirstObjectByType<GeneratorObjective>();
            if (generator == null || generator.shieldWaveObjects == null)
            {
                Check(false, "B4. La oleada del generador está definida para medir su geometría", "sin generador");
                return;
            }

            int embedded = 0;
            string detail = "";
            var groundMask = 1 << 8;

            for (int i = 0; i < generator.shieldWaveObjects.Length; i++)
            {
                var guard = generator.shieldWaveObjects[i];
                if (guard == null) continue;

                var collider = guard.GetComponent<Collider2D>();
                if (collider == null) continue;

                var bounds = collider.bounds;
                var size = new Vector2(bounds.size.x * 0.9f, bounds.size.y * 0.9f);
                var hits = Physics2D.OverlapBoxAll(bounds.center, size, 0f, groundMask);

                int solid = 0;
                for (int h = 0; h < hits.Length; h++)
                    if (hits[h] != null && !hits[h].isTrigger) solid++;

                if (solid > 0)
                {
                    embedded++;
                    detail += guard.name + "(" + solid + " solapes) ";
                }
            }

            Check(embedded == 0,
                "B4. Ningún guardia de la oleada final arranca incrustado en la geometría sólida",
                embedded == 0 ? "8 guardias comprobados, 0 incrustados" : detail.Trim());
        }

        // ==================================================================
        //  FASE C — RENDIMIENTO
        // ==================================================================
        private Unity.Profiling.ProfilerRecorder drawCalls;
        private Unity.Profiling.ProfilerRecorder batches;
        private bool renderCountersReady;

        // Coste real de CPU del fotograma. El reloj de pared del editor puede dispararse por
        // causas ajenas al juego (paneles del editor, ventanas en segundo plano), así que el
        // veredicto de rendimiento se apoya en el tiempo de CPU medido por el Profiler.
        private Unity.Profiling.ProfilerRecorder mainThread;
        private bool cpuCounterReady;

        private void PrepareCpuCounter()
        {
            if (cpuCounterReady) return;
            cpuCounterReady = true;

            try
            {
                mainThread = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Internal, "Main Thread", 1);
                if (!mainThread.Valid || mainThread.LastValue <= 0)
                {
                    mainThread.Dispose();
                    mainThread = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Internal, "PlayerLoop", 1);
                }

                if (!mainThread.Valid)
                    Note("rendimiento: el contador de CPU del hilo principal no está disponible en esta versión (recorder inválido)");
            }
            catch (System.Exception error)
            {
                Note("rendimiento: el contador de CPU del hilo principal no está disponible (" + error.GetType().Name + ")");
            }
        }

        private void PrepareRenderCounters()
        {
            if (renderCountersReady) return;
            renderCountersReady = true;

            try
            {
                drawCalls = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Render, "Draw Calls Count");
                batches = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Render, "Batches Count");

                if (!drawCalls.Valid && !batches.Valid)
                    Note("rendimiento: contadores de dibujado no disponibles en esta versión (recorders inválidos)");
            }
            catch (System.Exception error)
            {
                Note("rendimiento: los contadores de dibujado no están disponibles (" + error.GetType().Name + ")");
            }
        }

        private IEnumerator MeasureSection(string label, float x, int frames, List<string> lines)
        {
            StandAt(p1, x - 1.2f, 0.15f);
            if (p2 != null) StandAt(p2, x + 1.2f, 0.15f);
            yield return new WaitForSecondsRealtime(0.8f);

            PrepareRenderCounters();
            PrepareCpuCounter();

            long memStart = Profiler.GetTotalAllocatedMemoryLong();
            long monoStart = Profiler.GetMonoUsedSizeLong();
            int gcStart = System.GC.CollectionCount(0);

            var dts = new float[frames];
            long drawMax = 0;
            long batchMax = 0;
            double cpuSum = 0.0;
            int cpuCount = 0;
            float cpuWorst = 0f;

            for (int i = 0; i < frames; i++)
            {
                if (renderCountersReady)
                {
                    if (drawCalls.Valid) drawMax = System.Math.Max(drawMax, drawCalls.LastValue);
                    if (batches.Valid) batchMax = System.Math.Max(batchMax, batches.LastValue);
                }

                if (cpuCounterReady && mainThread.Valid)
                {
                    long ns = mainThread.LastValue;
                    if (ns > 0)
                    {
                        float cpuMs = ns / 1000000f;
                        cpuSum += cpuMs;
                        cpuCount++;
                        if (cpuMs > cpuWorst) cpuWorst = cpuMs;
                    }
                }

                dts[i] = Time.unscaledDeltaTime;
                yield return null;
            }

            long memEnd = Profiler.GetTotalAllocatedMemoryLong();
            long monoEnd = Profiler.GetMonoUsedSizeLong();
            int gcEnd = System.GC.CollectionCount(0);

            float sum = 0f;
            float worst = 0f;
            int over16 = 0;
            int over33 = 0;
            int over50 = 0;

            for (int i = 0; i < dts.Length; i++)
            {
                float ms = dts[i] * 1000f;
                sum += ms;
                if (ms > worst) worst = ms;
                if (ms > 16.7f) over16++;
                if (ms > 33.4f) over33++;
                if (ms > 50f) over50++;
            }

            float average = sum / dts.Length;
            float cpuAverage = cpuCount > 0 ? (float)(cpuSum / cpuCount) : -1f;
            var sorted = (float[])dts.Clone();
            System.Array.Sort(sorted);
            float p95 = sorted[Mathf.Clamp(Mathf.RoundToInt(sorted.Length * 0.95f) - 1, 0, sorted.Length - 1)] * 1000f;

            int enemiesActive = 0;
            var enemies = All<EnemyBase>();
            for (int i = 0; i < enemies.Length; i++)
                if (enemies[i] != null && enemies[i].enabled) enemiesActive++;

            var projectiles = All<Projectile>();
            int particles = All<ParticleSystem>().Length;
            int renderers = All<SpriteRenderer>().Length;

            int drawValue = renderCountersReady && drawCalls.Valid ? (int)drawMax : -1;
            int batchValue = renderCountersReady && batches.Valid ? (int)batchMax : -1;

            string line = label + ": fotogramas=" + dts.Length
                          + " media=" + average.ToString("0.00") + "ms (" + (average > 0f ? (1000f / average).ToString("0") : "0") + " FPS)"
                          + " p95=" + p95.ToString("0.00") + "ms peor=" + worst.ToString("0.00") + "ms"
                          + " >16,7ms=" + over16 + " >33,4ms=" + over33 + " >50ms=" + over50
                          + " cpuMedia=" + (cpuAverage >= 0f ? cpuAverage.ToString("0.00") + "ms" : "n/d")
                          + " cpuPeor=" + (cpuCount > 0 ? cpuWorst.ToString("0.00") + "ms" : "n/d")
                          + (average > 50f && cpuAverage >= 0f && cpuAverage < 25f ? " [reloj de pared contaminado por el editor]" : "")
                          + " | memoriaTotal=" + (memEnd / (1024 * 1024)) + "MB (Δ" + ((memEnd - memStart) / 1024) + "KB)"
                          + " mono=" + (monoEnd / (1024 * 1024)) + "MB (Δ" + ((monoEnd - monoStart) / 1024) + "KB)"
                          + " GC=" + (gcEnd - gcStart)
                          + " | enemigosActivos=" + enemiesActive + " proyectiles=" + projectiles.Length
                          + " renderers=" + renderers + " particleSystems=" + particles
                          + " drawCalls=" + (drawValue >= 0 ? drawValue.ToString() : "n/d")
                          + " batches=" + (batchValue >= 0 ? batchValue.ToString() : "n/d");

            lines.Add(line);
            Note("rendimiento " + line);
        }

        private IEnumerator PerfTests()
        {
            Note("rendimiento: condiciones -> " + Environment());
            Note("rendimiento: 2 jugadores, enemigos activos, mismas posiciones que el recorrido real; "
                 + "tiempos de fotograma medidos con Time.unscaledDeltaTime, memoria con Profiler.GetTotalAllocatedMemoryLong/GetMonoUsedSizeLong");

            var lines = new List<string>();

            string[] names = { "S1 entrada", "S2 producción", "S3 defensa", "S4 pasillo", "S5 generador" };
            float[] positions = { 10f, 55f, 105f, 145f, 180f };

            for (int i = 0; i < positions.Length; i++)
                yield return MeasureSection(names[i], positions[i], 120, lines);

            // Estrés: disparo simultáneo + granadas + torretas y drones de la sección 3.
            StandAt(p1, 104f, 0.15f);
            if (p2 != null) StandAt(p2, 107f, 0.15f);
            yield return new WaitForSecondsRealtime(0.6f);

            PrepareRenderCounters();
            PrepareCpuCounter();

            int frames = 180;
            var dts = new float[frames];
            var projectiles = All<Projectile>();
            int bulletsMax = 0;
            int drawMax = 0;
            int explosiveFrames = 0;
            double cpuSum = 0.0;
            int cpuCount = 0;
            float cpuWorst = 0f;
            long memStart = Profiler.GetTotalAllocatedMemoryLong();
            int gcStart = System.GC.CollectionCount(0);

            // La entrada se prepara una sola vez: Focus() por fotograma cuesta milisegundos y
            // además puede reiniciar el dispositivo, y aquí lo que se mide es el juego.
            PrepInput();

            for (int i = 0; i < frames; i++)
            {
                var keys = new List<Key>();
                keys.Add(Key.J);
                if (p2 != null && !p2.IsDead) keys.Add(Key.RightCtrl);
                if (i % 12 == 0) { keys.Add(Key.L); keys.Add(Key.Enter); }
                Hold(keys.ToArray());

                // El sondeo de objetos se hace cada 6 fotogramas para no medir el coste del arnés.
                if (i % 6 == 0)
                {
                    projectiles = All<Projectile>();
                    bulletsMax = Mathf.Max(bulletsMax, projectiles.Length);
                    if (All<Bomb>().Length > 0) explosiveFrames++;
                }

                if (renderCountersReady && drawCalls.Valid)
                    drawMax = Mathf.Max(drawMax, (int)drawCalls.LastValue);

                if (cpuCounterReady && mainThread.Valid)
                {
                    long ns = mainThread.LastValue;
                    if (ns > 0)
                    {
                        float cpuMs = ns / 1000000f;
                        cpuSum += cpuMs;
                        cpuCount++;
                        if (cpuMs > cpuWorst) cpuWorst = cpuMs;
                    }
                }

                dts[i] = Time.unscaledDeltaTime;
                yield return null;
            }

            ReleaseAll();

            long memEnd = Profiler.GetTotalAllocatedMemoryLong();
            int gcEnd = System.GC.CollectionCount(0);

            float sum = 0f;
            float worst = 0f;
            int over33 = 0;
            for (int i = 0; i < dts.Length; i++)
            {
                float ms = dts[i] * 1000f;
                sum += ms;
                if (ms > worst) worst = ms;
                if (ms > 33.4f) over33++;
            }

            float average = sum / dts.Length;
            float cpuAverage = cpuCount > 0 ? (float)(cpuSum / cpuCount) : -1f;

            Note("rendimiento estrés: fotogramas=" + frames
                 + " media=" + average.ToString("0.00") + "ms (" + (average > 0f ? (1000f / average).ToString("0") : "0") + " FPS)"
                 + " cpuMedia=" + (cpuAverage >= 0f ? cpuAverage.ToString("0.00") + "ms" : "n/d")
                 + " cpuPeor=" + (cpuCount > 0 ? cpuWorst.ToString("0.00") + "ms" : "n/d")
                 + " peor=" + worst.ToString("0.00") + "ms >33,4ms=" + over33
                 + " proyectilesMáximo=" + bulletsMax
                 + " fotogramasConGranada=" + explosiveFrames
                 + " memoriaΔ=" + ((memEnd - memStart) / 1024) + "KB GC=" + (gcEnd - gcStart)
                 + " drawCalls=" + (renderCountersReady && drawCalls.Valid ? drawMax.ToString() : "n/d"));

            // Si el contador de CPU está disponible, el veredicto se basa en el coste real del
            // fotograma; el reloj de pared sólo se usa como respaldo.
            bool perfOk = cpuAverage >= 0f
                ? (cpuAverage < 33.4f && cpuWorst < 120f)
                : (average < 33.4f && worst < 120f);

            Check(perfOk,
                "C1. El estrés de combate (disparo doble, granadas, torretas y drones) se mantiene estable",
                "cpuMedia=" + (cpuAverage >= 0f ? cpuAverage.ToString("0.00") + "ms" : "n/d")
                + " cpuPeor=" + (cpuCount > 0 ? cpuWorst.ToString("0.00") + "ms" : "n/d")
                + " relojMedia=" + average.ToString("0.00") + "ms relojPeor=" + worst.ToString("0.00") + "ms"
                + " fotogramas>33,4ms=" + over33 + " de " + frames
                + " proyectilesMáximo=" + bulletsMax);

            Check(lines.Count == 5,
                "C2. Las cinco secciones de Level02 se midieron con el mismo protocolo",
                "secciones=" + lines.Count);
        }

        // ==================================================================
        //  FASE D — BALANCE DE LA OLEADA DEL GENERADOR
        // ==================================================================
        private static bool GuardAlive(GameObject go)
        {
            if (go == null || !go.activeInHierarchy) return false;

            var enemy = go.GetComponent<EnemyBase>();
            return enemy != null && !enemy.IsDead;
        }

        private static GameObject NearestGuard(List<GameObject> guards, Vector3 from)
        {
            GameObject best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < guards.Count; i++)
            {
                if (!GuardAlive(guards[i])) continue;

                float d = (guards[i].transform.position - from).sqrMagnitude;
                if (d < bestDistance) { bestDistance = d; best = guards[i]; }
            }

            return best;
        }

        private void PlanAutopilot(PlayerController2D player, Key right, Key left, Key jump, Key fire, Key bomb,
                                   List<GameObject> guards, List<Key> keys)
        {
            var target = NearestGuard(guards, player.transform.position);
            if (target == null) return;

            float dx = target.transform.position.x - player.transform.position.x;
            float dy = target.transform.position.y - player.transform.position.y;

            player.SetFacing(dx >= 0f);
            keys.Add(fire);

            if (dy > 1.2f) keys.Add(jump);
            if (Mathf.Abs(dx) > 1.4f) keys.Add(dx > 0f ? right : left);
            if (player.shooter != null && player.shooter.Bombs > 0 && Mathf.Abs(dx) < 4.5f) keys.Add(bomb);
        }

        private IEnumerator WaveFight(string label, bool coop, float timeLimit)
        {
            var generator = Object.FindFirstObjectByType<GeneratorObjective>();
            var guards = new List<GameObject>();
            var aerial = new List<bool>();
            if (generator != null && generator.shieldWaveObjects != null)
                for (int i = 0; i < generator.shieldWaveObjects.Length; i++)
                {
                    var candidate = generator.shieldWaveObjects[i];
                    if (candidate == null) continue;
                    guards.Add(candidate);
                    aerial.Add(candidate.GetComponent<DroneEnemy>() != null);
                }

            int totalGuards = 0;
            int totalHealth = 0;
            int armoredHealth = 0;
            int groundTotal = 0;
            int airTotal = 0;

            for (int i = 0; i < guards.Count; i++)
            {
                if (!GuardAlive(guards[i])) continue;

                totalGuards++;
                if (aerial[i]) airTotal++; else groundTotal++;
                var health = guards[i].GetComponent<Health>();
                if (health != null) totalHealth += health.Current;
                if (guards[i].GetComponent<ArmoredSoldier>() != null && health != null) armoredHealth += health.Current;
            }

            if (totalGuards == 0)
            {
                Check(false, "D. La oleada del generador (" + label + ") está viva al empezar el combate", "guardias=0");
                yield break;
            }

            // Posiciones de partida: la entrada de la sección 5, como en una partida real.
            StandAt(p1, 173f, 0.15f);
            if (coop && p2 != null) StandAt(p2, 171f, 0.15f);
            MakeVulnerable(p1, h1);
            if (coop && p2 != null) MakeVulnerable(p2, h2);

            for (int i = 0; i < guards.Count; i++)
            {
                var guardEnemy = guards[i].GetComponent<EnemyBase>();
                if (guardEnemy != null) guardEnemy.enabled = true;
            }

            yield return new WaitForSecondsRealtime(0.5f);

            float startTime = Time.time;
            float realDeadline = Time.realtimeSinceStartup + timeLimit;

            int hp0 = h1.Current;
            int hp1 = coop && h2 != null ? h2.Current : 0;
            int prevHp0 = hp0;
            int prevHp1 = hp1;

            int damage0 = 0;
            int damage1 = 0;
            int deaths0 = 0;
            int deaths1 = 0;

            bool wasDead0 = p1.IsDead;
            bool wasDead1 = coop && p2 != null && p2.IsDead;

            int maxEnemyBullets = 0;
            bool cleared = false;
            bool groundCleared = false;
            float groundClearTime = -1f;
            int frame = 0;

            // La entrada se prepara una sola vez antes de medir: Focus() por fotograma falsearía
            // los tiempos y reiniciaría el dispositivo.
            PrepInput();

            while (Time.realtimeSinceStartup < realDeadline)
            {
                if (lm != null && lm.GameOver) break;

                frame++;

                var keys = new List<Key>();

                if (p1 != null && !p1.IsDead)
                    PlanAutopilot(p1, Key.D, Key.A, Key.K, Key.J, Key.L, guards, keys);

                if (coop && p2 != null && !p2.IsDead)
                    PlanAutopilot(p2, Key.RightArrow, Key.LeftArrow, Key.RightShift, Key.RightCtrl, Key.Enter, guards, keys);

                Hold(keys.ToArray());

                // Daño recibido acumulado (incluye el golpe final de cada muerte).
                if (h1 != null && p1 != null && h1.Current < prevHp0) damage0 += prevHp0 - h1.Current;
                if (h1 != null) prevHp0 = h1.Current;

                if (coop && h2 != null)
                {
                    if (h2.Current < prevHp1) damage1 += prevHp1 - h2.Current;
                    prevHp1 = h2.Current;
                }

                if (p1 != null && p1.IsDead && !wasDead0) { deaths0++; wasDead0 = true; }
                if (p1 != null && !p1.IsDead) wasDead0 = false;

                if (coop && p2 != null)
                {
                    if (p2.IsDead && !wasDead1) { deaths1++; wasDead1 = true; }
                    if (!p2.IsDead) wasDead1 = false;
                }

                if (frame % 6 == 0)
                {
                    var projectiles = All<Projectile>();
                    int enemyBullets = 0;
                    for (int i = 0; i < projectiles.Length; i++)
                        if (projectiles[i] != null && projectiles[i].faction == Faction.Enemy) enemyBullets++;

                    maxEnemyBullets = Mathf.Max(maxEnemyBullets, enemyBullets);
                }

                int groundAlive = 0;
                int airAlive = 0;
                for (int i = 0; i < guards.Count; i++)
                {
                    if (!GuardAlive(guards[i])) continue;
                    if (aerial[i]) airAlive++; else groundAlive++;
                }

                cleared = groundAlive == 0 && airAlive == 0;

                // El piloto de suelo no alcanza a los drones (vuelan a y≈5,0-6,0 y el disparo
                // desde el suelo llega a 3,62): la medición termina al caer el último enemigo
                // terrestre, que es lo que este piloto puede resolver.
                if (groundAlive == 0 && !groundCleared)
                {
                    groundCleared = true;
                    groundClearTime = Time.time - startTime;
                }

                if (groundCleared) break;

                // Si los dos jugadores se quedan sin vidas, el combate ha terminado.
                if (GameSession.LivesP1 <= 0 && (!coop || GameSession.LivesP2 <= 0)) break;

                yield return null;
            }

            ReleaseAll();

            float elapsed = groundClearTime > 0f ? groundClearTime : Time.time - startTime;

            int survivors = 0;
            int groundSurvivors = 0;
            int airSurvivors = 0;
            string survivorNames = "";
            for (int i = 0; i < guards.Count; i++)
            {
                if (!GuardAlive(guards[i])) continue;
                survivors++;
                if (aerial[i]) airSurvivors++; else groundSurvivors++;
                survivorNames += guards[i].name + " ";
            }

            float playerDps = p1 != null && p1.shooter != null ? p1.shooter.bulletDamage / Mathf.Max(0.01f, p1.shooter.fireRate) : 0f;
            int normalHealth = Mathf.Max(0, totalHealth - armoredHealth);
            float theoretical = playerDps > 0f
                ? (normalHealth / playerDps) + (armoredHealth / (playerDps * 0.25f))
                : -1f;

            Note("balance " + label + ": guardias=" + totalGuards
                 + " saludTotal=" + totalHealth + " (blindados " + armoredHealth + ")"
                 + " dpsJ1=" + playerDps.ToString("0.0")
                 + " tiempoTeóricoMínimo=" + (theoretical > 0f ? theoretical.ToString("0.0") + "s (si todos los impactos entran y los blindados se golpean de frente)" : "n/d")
                 + (coop ? " dpsConjunto=" + (playerDps * 2f).ToString("0.0") : ""));

            // La oleada mezcla enemigos terrestres y aéreos. El piloto de suelo debe poder
            // cerrar la parte terrestre (si no, el combate sería injugable) y los drones
            // quedan fuera de su alcance por diseño del nivel, que exige subir a las
            // plataformas del generador: E1/E2 lo demuestran con entrada real.
            Check(groundCleared && groundSurvivors == 0 && !(lm != null && lm.GameOver),
                "D1. El piloto automático de suelo limpia todos los enemigos terrestres de la oleada; los aéreos exigen plataforma (" + label + ")",
                "tierra=" + (groundTotal - groundSurvivors) + "/" + groundTotal + " eliminados"
                + " tiempoTierra=" + elapsed.ToString("0.00") + "s"
                + " aereosVivos=" + airSurvivors + "/" + airTotal
                + " oleadaCompleta=" + cleared
                + " alcanceDesdeSuelo=3,62 alturaDrones=5,0-6,0 (se abaten desde GeneratorStep/GeneratorBase, ver E1/E2)"
                + (survivors > 0 ? " supervivientes=" + survivorNames.Trim() : "")
                + " gameOver=" + (lm != null && lm.GameOver));

            Check(!(groundCleared && damage0 == 0 && (!coop || damage1 == 0) && elapsed < 5f),
                "D2. La oleada supone una amenaza real y no cae sin oposición (" + label + ")",
                "tiempo=" + elapsed.ToString("0.00") + "s dañoP1=" + damage0 + " dañoP2=" + damage1
                + " muertes=" + deaths0 + "/" + deaths1
                + " proyectilesEnemigosMáximo=" + maxEnemyBullets
                + " vidas=" + GameSession.LivesP1 + "/" + GameSession.LivesP2);

            Note("balance " + label + ": dañoP1=" + damage0 + " dañoP2=" + damage1
                 + " muertesP1=" + deaths0 + " muertesP2=" + deaths1
                 + " tiempo=" + elapsed.ToString("0.00") + "s"
                 + " proyectilesEnemigosMáximo=" + maxEnemyBullets
                 + " vidasFinales=" + GameSession.LivesP1 + "/" + GameSession.LivesP2
                 + " gameOver=" + (lm != null && lm.GameOver));
        }

        // ==================================================================
        //  E — Alcance de los dos drones de la oleada del generador
        // ==================================================================
        //  D1 demuestra que el piloto automático de SUELO no puede cerrar la oleada: los dos
        //  drones vuelan a y≈5, por encima del alcance del disparo desde el suelo (boca de fuego
        //  a 1,06 + salto 2,56 = 3,62). Aquí se comprueba con entrada real que un jugador SÍ los
        //  alcanza desde las plataformas del generador, que es la condición para que el escudo
        //  caiga y la misión se pueda completar.
        private IEnumerator DroneReachTests()
        {
            yield return LoadLevel(1, CharacterId.Danner, CharacterId.Ines);
            if (p1 == null)
            {
                Check(false, "E1. Los drones de la oleada se pueden alcanzar desde las plataformas del generador", "sin jugador");
                yield break;
            }

            PrepInput();
            ParkEnemies();

            var drones = All<DroneEnemy>();
            DroneEnemy low = null;
            DroneEnemy high = null;

            for (int i = 0; i < drones.Length; i++)
            {
                var d = drones[i];
                if (d == null) continue;
                if (d.name.IndexOf("Drone_176", System.StringComparison.Ordinal) == 0) low = d;
                else if (d.name.IndexOf("Drone_182", System.StringComparison.Ordinal) == 0) high = d;
            }

            if (low == null || high == null)
            {
                Check(false, "E1. Los drones de la oleada se pueden alcanzar desde las plataformas del generador",
                    "Drone_176h=" + (low != null) + " Drone_182h=" + (high != null) + " dronesEnEscena=" + drones.Length);
                yield break;
            }

            // Sólo estos dos vuelan durante la medida; el resto de enemigos sigue aparcado.
            low.enabled = true;
            high.enabled = true;
            yield return null;

            yield return ReachDrone("E1", low, 186.5f, "GeneratorStep");
            yield return ReachDrone("E2", high, 191.5f, "GeneratorBase");
        }

        private IEnumerator ReachDrone(string id, DroneEnemy drone, float standX, string platform)
        {
            var droneHealth = drone.GetComponent<Health>();
            var droneCollider = drone.GetComponent<Collider2D>();
            if (droneHealth == null)
            {
                Check(false, id + ". El dron " + drone.name + " se alcanza desde " + platform, "sin Health");
                yield break;
            }

            droneHealth.ResetHealth();
            p1.ApplySpawnInvulnerability(40f);   // el dron dispara: no debe matar al medidor
            p1.SetInputEnabled(true);
            StandAt(p1, standX, 0.15f);
            yield return null;

            // Orientación: se pulsa la dirección hacia el dron un instante y se suelta, porque
            // mantenerla alejaría al jugador de la plataforma.
            bool droneToRight = drone.transform.position.x > p1.transform.position.x;
            Key toward = droneToRight ? Key.D : Key.A;

            for (int i = 0; i < 3; i++) { Hold(toward); yield return null; }
            ReleaseAll();
            p1.SetFacing(droneToRight);
            yield return null;

            int startHealth = droneHealth.Current;
            float droneMinY = drone.transform.position.y;
            float droneMaxY = droneMinY;
            float muzzleMinY = 999f;
            float muzzleMaxY = -999f;
            int dealt = 0;

            for (int attempt = 0; attempt < 2 && dealt == 0; attempt++)
            {
                if (attempt > 0)
                {
                    // Segundo intento: por si la primera ventana no coincidió con el dron.
                    StandAt(p1, standX, 0.15f);
                    p1.SetFacing(droneToRight);
                    yield return null;
                }

                float end = Time.time + 12f;
                bool airborne = false;

                while (Time.time < end && droneHealth.IsAlive)
                {
                    // Salto sin recorte: K se mantiene pulsado toda la subida (soltarlo en el
                    // aire activa jumpCutMultiplier y recorta el ápice) y sólo se libera un
                    // fotograma al tocar el suelo para generar un pulso nuevo.
                    if (p1.IsGrounded)
                    {
                        if (airborne) { Hold(Key.J); airborne = false; }
                        else Hold(Key.J, Key.K);
                    }
                    else
                    {
                        Hold(Key.J, Key.K);
                        airborne = true;
                    }

                    var muzzle = p1.shooter != null ? p1.shooter.muzzle : null;
                    if (muzzle != null)
                    {
                        muzzleMinY = Mathf.Min(muzzleMinY, muzzle.position.y);
                        muzzleMaxY = Mathf.Max(muzzleMaxY, muzzle.position.y);
                    }

                    if (droneCollider != null)
                    {
                        droneMinY = Mathf.Min(droneMinY, droneCollider.bounds.center.y);
                        droneMaxY = Mathf.Max(droneMaxY, droneCollider.bounds.center.y);
                    }

                    yield return null;
                }

                ReleaseAll();
                dealt = startHealth - droneHealth.Current;
            }

            float apex = p1.jumpVelocity * p1.jumpVelocity / (2f * Mathf.Abs(Physics2D.gravity.y));

            Check(dealt > 0,
                id + ". El dron " + drone.name + " se alcanza y recibe daño desde " + platform,
                "plataforma=" + platform + " xJugador=" + standX.ToString("0.0")
                + " daño=" + dealt + "/" + startHealth
                + " alturaDron=" + droneMinY.ToString("0.00") + ".." + droneMaxY.ToString("0.00")
                + " bocaDeFuegoY=" + muzzleMinY.ToString("0.00") + ".." + muzzleMaxY.ToString("0.00")
                + " saltoApex=" + apex.ToString("0.00") + " bocaEnReposo=1.06");
        }

        // ==================================================================
        //  Secuencia
        // ==================================================================
        private IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(0.8f);

            ApplyTemporaryEditorInput();

            // ---- A. Audio (necesita una escena de nivel con AudioManager) ----
            yield return LoadLevel(1, CharacterId.Danner, CharacterId.Ines);
            if (p1 == null)
            {
                Check(false, "0. Level02 arranca para la auditoría", "sin jugador");
                Finish();
                yield break;
            }

            PrepInput();
            yield return AudioTests();

            // ---- B. Vapor industrial y geometría de la oleada ----
            ParkEnemies();
            yield return null;
            yield return VentTests();
            GeometryTests();
            UnparkEnemies();

            // ---- C. Rendimiento con dos jugadores y enemigos activos ----
            yield return LoadLevel(2, CharacterId.Danner, CharacterId.Ines);
            yield return PerfTests();

            // ---- D. Balance: un jugador ----
            yield return LoadLevel(1, CharacterId.Danner, CharacterId.Ines);
            yield return WaveFight("1 jugador", false, 35f);

            // ---- E. Balance: cooperativo ----
            yield return LoadLevel(2, CharacterId.Danner, CharacterId.Ines);
            yield return WaveFight("2 jugadores", true, 35f);

            // ---- F. Alcance de los dos drones de la oleada desde las plataformas ----
            yield return DroneReachTests();

            ReleaseAll();
            Finish();
        }

        // ---- Cambio TEMPORAL del comportamiento de entrada del editor ------------------
        // Con «Pointers and Keyboards Respect Game View Focus» el Input System descarta el teclado
        // cuando el editor no tiene el foco de ventana del sistema, y entonces el piloto automático
        // de la auditoría se queda sin control. Se cambia sólo durante la suite y se restaura el
        // valor original al terminar (el proyecto queda exactamente igual).
        private bool editorBehaviorSaved;
        private InputSettings.EditorInputBehaviorInPlayMode editorBehaviorOriginal;

        private void ApplyTemporaryEditorInput()
        {
            try
            {
                var settings = InputSystem.settings;
                editorBehaviorOriginal = settings.editorInputBehaviorInPlayMode;
                settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                editorBehaviorSaved = true;
                Note("entrada: editorInputBehaviorInPlayMode cambiado TEMPORALMENTE a AllDeviceInputAlwaysGoesToGameView (original " + editorBehaviorOriginal + ")");
            }
            catch (System.Exception error)
            {
                Note("entrada: no se pudo cambiar el ajuste temporal (" + error.GetType().Name + ")");
            }
        }

        private void RestoreEditorInput()
        {
            if (!editorBehaviorSaved) return;

            try
            {
                InputSystem.settings.editorInputBehaviorInPlayMode = editorBehaviorOriginal;
                editorBehaviorSaved = false;
                Note("entrada: editorInputBehaviorInPlayMode RESTAURADO a " + editorBehaviorOriginal);
            }
            catch (System.Exception error)
            {
                Note("entrada: no se pudo restaurar el ajuste (" + error.GetType().Name + ")");
            }
        }

        private void OnDestroy()
        {
            RestoreEditorInput();
        }

        private void Finish()
        {
            RestoreEditorInput();

            int total = passed + failed;
            if (failed == 0) Debug.Log(Prefix + " ===== VEREDICTO AUDITORÍA: OK " + passed + "/" + total + " =====");
            else Debug.LogError(Prefix + " ===== VEREDICTO AUDITORÍA: FALLOS " + failed + " de " + total + " =====");
        }
    }
}
#endif
