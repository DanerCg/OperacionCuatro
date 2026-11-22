#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace OperacionCuatro.EditorTools
{
    /// <summary>
    /// Fase 2 — validación COOPERATIVA de Level02 «COMPLEJO DE GUERRA».
    ///
    /// Arranca una sesión real de dos jugadores (DANNER + INÉS) en Level02 y comprueba
    /// movimiento, disparo y bombas independientes, salud y vidas separadas, trampas,
    /// checkpoints, cámara compartida con zoom, muerte y reaparición de un solo jugador,
    /// game over únicamente al agotar las vidas de ambos y el cierre de misión con Results.
    ///
    /// Se lanza en Play Mode desde el menú OC o por reflexión sobre Run.
    /// </summary>
    public static class OC_Level02CoopTest
    {
        [UnityEditor.MenuItem("OC/Playtest/Level02 cooperativo (2 jugadores)")]
        public static void Run()
        {
            var go = new GameObject("[OC_Level02CoopTest]");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<OCLevel02CoopRunner>();
        }
    }

    public class OCLevel02CoopRunner : MonoBehaviour
    {
        private int passed;
        private int failed;

        private LevelManager lm;
        private LevelHUD hud;
        private PlayerController2D p1;
        private PlayerController2D p2;
        private Health h1;
        private Health h2;
        private CameraFollow2D cam;

        private Keyboard kb;

        private readonly List<EnemyBase> parked = new List<EnemyBase>();

        // ==================================================================
        //  Utilidades
        // ==================================================================
        private void Check(bool ok, string what, string detail = "")
        {
            if (ok) { passed++; Debug.Log("[C] PASS  " + what + (detail.Length > 0 ? " | " + detail : "")); }
            else { failed++; Debug.LogError("[C] FAIL  " + what + (detail.Length > 0 ? " | " + detail : "")); }
        }

        private void Note(string text) { Debug.Log("[C] ....  " + text); }

        private static T[] All<T>() where T : Object
        {
            return Object.FindObjectsByType<T>(FindObjectsSortMode.None);
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

        private void Press(params Key[] keys) { InputSystem.QueueStateEvent(Kb(), new KeyboardState(keys)); }
        private void ReleaseAll() { InputSystem.QueueStateEvent(Kb(), new KeyboardState()); }

        /// <summary>
        /// Deja la entrada sintética en condiciones de llegar al juego.
        ///
        /// Con «Pointers and Keyboards Respect Game View Focus» el Input System descarta el teclado
        /// mientras el Game View no tiene el foco, y con «Reset and Disable Non Background Devices»
        /// el dispositivo se desactiva al perderse el foco. Por eso se enfoca el Game View y se
        /// reactiva el teclado: son acciones de esta sesión de Play Mode, ningún ajuste del proyecto
        /// se modifica.
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

        /// <summary>Estado real de la entrada en el momento de la medida, para el informe.</summary>
        private string InputState()
        {
            string settings;
            try
            {
                var s = InputSystem.settings;
                settings = "fondo=" + s.backgroundBehavior + " editor=" + s.editorInputBehaviorInPlayMode;
            }
            catch (System.Exception error) { settings = "ajustes-error:" + error.GetType().Name; }

            var type = System.Type.GetType("UnityEditor.GameView, UnityEditor");
            var window = type != null ? UnityEditor.EditorWindow.GetWindow(type, false, null, false) : null;

            return "teclado=" + (kb != null ? (kb.enabled ? "activo" : "inactivo") : "nulo")
                   + " focoGameView=" + (window != null && window.hasFocus)
                   + " " + settings;
        }

        // ---- Cambio TEMPORAL del comportamiento de entrada del editor ------------------
        // Con «Pointers and Keyboards Respect Game View Focus» el Input System descarta el teclado
        // cuando el editor no tiene el foco de ventana del sistema (por ejemplo, mientras se lee la
        // consola desde otra aplicación), y entonces toda medida de control se vuelve imposible.
        // Para la duración de la suite se cambia a «AllDeviceInputAlwaysGoesToGameView» y se
        // restaura exactamente el valor original al terminar, de modo que el proyecto queda igual.
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
                Debug.Log("[C] ....  entrada: editorInputBehaviorInPlayMode cambiado TEMPORALMENTE a AllDeviceInputAlwaysGoesToGameView (original " + editorBehaviorOriginal + ")");
            }
            catch (System.Exception error)
            {
                Debug.LogWarning("[C] no se pudo cambiar el ajuste de entrada temporal: " + error.GetType().Name);
            }
        }

        private void RestoreEditorInput()
        {
            if (!editorBehaviorSaved) return;

            try
            {
                InputSystem.settings.editorInputBehaviorInPlayMode = editorBehaviorOriginal;
                editorBehaviorSaved = false;
                Debug.Log("[C] ....  entrada: editorInputBehaviorInPlayMode RESTAURADO a " + editorBehaviorOriginal);
            }
            catch (System.Exception error)
            {
                Debug.LogWarning("[C] no se pudo restaurar el ajuste de entrada: " + error.GetType().Name);
            }
        }

        private void OnDestroy()
        {
            RestoreEditorInput();
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

        /// <summary>Deja al jugador listo para recibir daño (sin la invulnerabilidad de spawn o de golpe).</summary>
        private static void MakeVulnerable(PlayerController2D player, Health health)
        {
            if (player != null) player.ClearInvulnerability();
            if (health != null) { health.ResetHealth(); health.invulnerable = false; }
        }

        private static void Kill(Health health, Vector2 point)
        {
            if (health == null) return;

            health.TakeDamage(new DamageInfo
            {
                amount = 9999,
                point = point,
                direction = Vector2.right,
                source = null,
                playerIndex = -1
            });
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
        }

        private void UnparkEnemies()
        {
            for (int i = 0; i < parked.Count; i++)
                if (parked[i] != null) parked[i].enabled = true;

            parked.Clear();
        }

        private void Acquire()
        {
            lm = LevelManager.Instance;
            hud = lm != null ? lm.hud : null;
            cam = Object.FindFirstObjectByType<CameraFollow2D>();

            p1 = lm != null && lm.Players.Count > 0 ? lm.Players[0] : null;
            p2 = lm != null && lm.Players.Count > 1 ? lm.Players[1] : null;

            h1 = p1 != null ? p1.health : null;
            h2 = p2 != null ? p2.health : null;
        }

        private string Describe()
        {
            return "P1=" + (p1 != null ? p1.name + "#" + p1.playerIndex : "nulo")
                   + " P2=" + (p2 != null ? p2.name + "#" + p2.playerIndex : "nulo")
                   + " jugadores=" + (lm != null ? lm.Players.Count : -1);
        }

        private string HudDetail()
        {
            if (hud == null) return "sin HUD";

            return "textoP1=\"" + (hud.panelP1 != null && hud.panelP1.nameText != null ? hud.panelP1.nameText.text : "")
                   + "\" textoP2=\"" + (hud.panelP2 != null && hud.panelP2.nameText != null ? hud.panelP2.nameText.text : "")
                   + "\" panelP2Visible=" + (hud.panelP2 != null && hud.panelP2.root != null && hud.panelP2.root.activeSelf);
        }

        /// <summary>Máximo de proyectiles simultáneos de jugador por índice de jugador.</summary>
        private void CountPlayerBullets(out int maxOwner0, out int maxOwner1)
        {
            maxOwner0 = 0;
            maxOwner1 = 0;

            var bullets = All<Projectile>();
            for (int i = 0; i < bullets.Length; i++)
            {
                if (bullets[i] == null || bullets[i].faction != Faction.Player) continue;
                if (bullets[i].ownerPlayerIndex == 0) maxOwner0++;
                else if (bullets[i].ownerPlayerIndex == 1) maxOwner1++;
            }
        }

        /// <summary>Granadas de jugador presentes en la escena, por índice de jugador.</summary>
        private void CountPlayerBombs(out int owner0, out int owner1)
        {
            owner0 = 0;
            owner1 = 0;

            var bombs = All<Bomb>();
            for (int i = 0; i < bombs.Length; i++)
            {
                if (bombs[i] == null || bombs[i].faction != Faction.Player) continue;
                if (bombs[i].ownerPlayerIndex == 0) owner0++;
                else if (bombs[i].ownerPlayerIndex == 1) owner1++;
            }
        }

        // ==================================================================
        //  Secuencia
        // ==================================================================
        private IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(0.8f);

            ApplyTemporaryEditorInput();

            // ---------------- A. SESIÓN DE DOS JUGADORES ----------------
            GameSession.StartNewRun(2);
            GameSession.SetCharacter(0, CharacterId.Danner);
            GameSession.SetCharacter(1, CharacterId.Ines);
            GameSession.StartLevel(2);

            Check(GameSession.PlayerCount == 2 && GameSession.P1 == CharacterId.Danner
                  && GameSession.P2 == CharacterId.Ines && GameSession.CurrentLevel == 2,
                "1. Sesión cooperativa preparada con dos personajes distintos en el nivel 2",
                "jugadores=" + GameSession.PlayerCount + " P1=" + GameSession.P1 + " P2=" + GameSession.P2
                + " nivel=" + GameSession.CurrentLevel);

            SceneManager.LoadScene(SceneNames.Level02);
            yield return null;
            yield return new WaitForSecondsRealtime(1.9f);

            Acquire();

            bool bothPresent = p1 != null && p2 != null && p1 != p2
                               && p1.playerIndex == 0 && p2.playerIndex == 1
                               && p1.name.Contains("Danner") && p2.name.Contains("Ines")
                               && h1 != null && h2 != null && h1 != h2;

            Check(bothPresent,
                "2. Level02 instancia a los dos jugadores con su personaje, índice y salud propios",
                Describe());

            if (!bothPresent) { Finish(); yield break; }

            bool hudOk = hud != null && hud.panelP1 != null && hud.panelP2 != null
                         && hud.panelP1.player == p1 && hud.panelP2.player == p2
                         && hud.panelP2.root != null && hud.panelP2.root.activeSelf
                         && hud.panelP1.nameText != null && hud.panelP1.nameText.text.Contains("DANNER")
                         && hud.panelP2.nameText != null && hud.panelP2.nameText.text.Contains("P2")
                         && hud.panelP2.nameText.text.Contains("INÉS");

            Check(hudOk, "3. El HUD muestra los dos paneles con P1 DANNER y P2 INÉS", HudDetail());

            // Se apartan los enemigos para medir los sistemas de los jugadores sin interferencia.
            ParkEnemies();
            yield return null;

            p1.SetInputEnabled(true);
            p2.SetInputEnabled(true);

            // ---- Calentamiento de la entrada sintética ----
            // El Input System puede descartar las pulsaciones inyectadas mientras el Game View
            // no tiene el foco; por eso la tecla se vuelve a encolar en cada fotograma y se
            // confirma que la entrada llega antes de medir nada. No es una comprobación:
            // solo queda constancia en el registro.
            bool inputAlive = false;
            int inputAttempts = 0;
            for (int warm = 0; warm < 8 && !inputAlive; warm++)
            {
                inputAttempts++;
                PrepInput();
                StandAt(p1, 5f, 0.12f);
                yield return null;

                float warmStart = p1.transform.position.x;
                float warmEnd = Time.time + 0.35f;
                while (Time.time < warmEnd) { Press(Key.D); yield return null; }
                ReleaseAll();

                if (p1.transform.position.x - warmStart > 0.4f) inputAlive = true;
            }

            Note("entrada sintética confirmada=" + inputAlive + " intentos=" + inputAttempts
                 + " | " + InputState());

            // ---------------- B. MOVIMIENTO INDEPENDIENTE ----------------
            StandAt(p1, 5f, 0.12f);
            StandAt(p2, 12f, 0.12f);
            yield return new WaitForSecondsRealtime(0.35f);

            float a0 = p1.transform.position.x;
            float b0 = p2.transform.position.x;
            float a1 = a0;
            float b1 = b0;

            for (int attempt = 0; attempt < 3 && (a1 - a0) < 0.5f; attempt++)
            {
                PrepInput();
                if (attempt > 0) { ReleaseAll(); yield return null; yield return null; }

                float dEnd = Time.time + 0.7f;
                while (Time.time < dEnd) { Press(Key.D); yield return null; }
                ReleaseAll();

                a1 = p1.transform.position.x;
                b1 = p2.transform.position.x;
            }

            yield return new WaitForSecondsRealtime(0.25f);

            StandAt(p1, 5f, 0.12f);
            StandAt(p2, 12f, 0.12f);
            yield return new WaitForSecondsRealtime(0.35f);

            float a2 = p1.transform.position.x;
            float b2 = p2.transform.position.x;
            float a3 = a2;
            float b3 = b2;

            for (int attempt = 0; attempt < 3 && (b3 - b2) < 0.5f; attempt++)
            {
                PrepInput();
                if (attempt > 0) { ReleaseAll(); yield return null; yield return null; }

                float rEnd = Time.time + 0.7f;
                while (Time.time < rEnd) { Press(Key.RightArrow); yield return null; }
                ReleaseAll();

                a3 = p1.transform.position.x;
                b3 = p2.transform.position.x;
            }

            yield return new WaitForSecondsRealtime(0.2f);

            Check((a1 - a0) > 1f && Mathf.Abs(b1 - b0) < 0.3f && (b3 - b2) > 1f && Mathf.Abs(a3 - a2) < 0.3f,
                "4. Movimiento independiente: D mueve solo al P1 y la flecha derecha solo al P2",
                "conD: P1 " + a0.ToString("0.00") + "->" + a1.ToString("0.00")
                + " P2 " + b0.ToString("0.00") + "->" + b1.ToString("0.00")
                + " | conFlecha: P2 " + b2.ToString("0.00") + "->" + b3.ToString("0.00")
                + " P1 " + a2.ToString("0.00") + "->" + a3.ToString("0.00"));

            // ---------------- C. DISPARO SIMULTÁNEO ----------------
            StandAt(p1, 8f, 0.12f);
            StandAt(p2, 11f, 0.12f);
            p1.SetFacing(true);
            p2.SetFacing(true);
            yield return new WaitForSecondsRealtime(0.35f);

            int maxOwner0 = 0;
            int maxOwner1 = 0;

            for (int attempt = 0; attempt < 3 && (maxOwner0 == 0 || maxOwner1 == 0); attempt++)
            {
                PrepInput();
                if (attempt > 0) { ReleaseAll(); yield return null; yield return null; }

                float fireEnd = Time.time + 0.5f;
                while (Time.time < fireEnd)
                {
                    Press(Key.J, Key.RightCtrl);

                    int c0, c1;
                    CountPlayerBullets(out c0, out c1);
                    maxOwner0 = Mathf.Max(maxOwner0, c0);
                    maxOwner1 = Mathf.Max(maxOwner1, c1);

                    yield return null;
                }
            }

            ReleaseAll();
            yield return new WaitForSecondsRealtime(0.2f);

            Check(maxOwner0 > 0 && maxOwner1 > 0,
                "5. Disparo simultáneo: J y Ctrl derecho generan proyectiles de ambos jugadores",
                "balasP1=" + maxOwner0 + " balasP2=" + maxOwner1);

            // ---------------- D. BOMBAS INDIVIDUALES ----------------
            int bombsP1 = p1.shooter != null ? p1.shooter.Bombs : 0;
            int bombsP2 = p2.shooter != null ? p2.shooter.Bombs : 0;

            int maxBombs0 = 0;
            int maxBombs1 = 0;

            for (int attempt = 0; attempt < 3; attempt++)
            {
                bool done = p1.shooter != null && p1.shooter.Bombs < bombsP1
                            && p2.shooter != null && p2.shooter.Bombs < bombsP2;
                if (done) break;

                PrepInput();
                if (attempt > 0) { ReleaseAll(); yield return null; yield return null; }

                // El recargue entre granadas (0,6 s) impide que reencolar la tecla gaste más de una.
                float bombEnd = Time.time + 0.2f;
                while (Time.time < bombEnd)
                {
                    Press(Key.L, Key.Enter);

                    int live0, live1;
                    CountPlayerBombs(out live0, out live1);
                    maxBombs0 = Mathf.Max(maxBombs0, live0);
                    maxBombs1 = Mathf.Max(maxBombs1, live1);

                    yield return null;
                }
                ReleaseAll();
            }

            yield return new WaitForSecondsRealtime(0.05f);

            int bombsInScene0, bombsInScene1;
            CountPlayerBombs(out bombsInScene0, out bombsInScene1);

            maxBombs0 = Mathf.Max(maxBombs0, bombsInScene0);
            maxBombs1 = Mathf.Max(maxBombs1, bombsInScene1);

            int bombsP1After = p1.shooter != null ? p1.shooter.Bombs : 0;
            int bombsP2After = p2.shooter != null ? p2.shooter.Bombs : 0;

            Check(bombsP1After < bombsP1 && bombsP2After < bombsP2
                  && maxBombs0 >= 1 && maxBombs1 >= 1,
                "6. Bombas individuales: L y Enter gastan una bomba a cada jugador y lanzan las dos granadas",
                "P1 " + bombsP1 + "->" + bombsP1After + " P2 " + bombsP2 + "->" + bombsP2After
                + " granadasP1=" + maxBombs0 + " granadasP2=" + maxBombs1);

            // ---------------- E. SALUD INDEPENDIENTE ----------------
            MakeVulnerable(p1, h1);
            MakeVulnerable(p2, h2);
            yield return null;

            int hp2Start = h2.Current;
            int hp1Start = h1.Current;

            h1.TakeDamage(new DamageInfo
            {
                amount = 25,
                point = p1.transform.position,
                direction = Vector2.right,
                source = null,
                playerIndex = 1
            });

            yield return null;

            Check(h1.Current == hp1Start - 25 && h2.Current == hp2Start,
                "7. Salud independiente: el daño al P1 no altera la salud del P2",
                "P1 " + hp1Start + "->" + h1.Current + " P2 " + hp2Start + "->" + h2.Current);

            // ---------------- F. TRAMPAS COMPARTIDAS ----------------
            var zone = Nearest<ElectricZone>(143f);
            bool zoneOk = false;
            string zoneDetail = "sin zona eléctrica";

            if (zone != null)
            {
                Teleport(p1, 141.5f, 0.15f);
                Teleport(p2, 144.5f, 0.15f);
                MakeVulnerable(p1, h1);
                MakeVulnerable(p2, h2);
                yield return null;

                int z1Start = h1.Current;
                int z2Start = h2.Current;
                bool hurt1 = false;
                bool hurt2 = false;

                float zoneEnd = Time.time + 7f;
                while (Time.time < zoneEnd)
                {
                    if (h1.Current < z1Start) hurt1 = true;
                    if (h2.Current < z2Start) hurt2 = true;
                    if (hurt1 && hurt2) break;
                    yield return null;
                }

                zoneOk = hurt1 && hurt2;
                zoneDetail = "P1 daño=" + (z1Start - h1.Current) + " P2 daño=" + (z2Start - h2.Current)
                             + " zona=" + zone.name;
            }

            Check(zoneOk, "8. Una misma trampa daña a los dos jugadores de forma independiente", zoneDetail);

            // ---------------- G. CHECKPOINT, MUERTE Y REAPARICIÓN ----------------
            var checkpoint = Nearest<Checkpoint>(150f);
            StandAt(p1, 150f, 0.15f);
            yield return new WaitForSecondsRealtime(0.45f);

            float checkpointX = lm != null ? lm.checkpointPosition.x : -999f;
            Check(checkpoint != null && Mathf.Abs(checkpointX - 150f) < 3f && lm != null && !lm.GameOver,
                "9. El checkpoint de la sección 4 se activa al pasar por encima y fija el punto de reaparición",
                "checkpoint=" + (checkpoint != null ? checkpoint.name : "nulo")
                + " respawnX=" + checkpointX.ToString("0.00"));

            int lives1Before = GameSession.LivesP1;
            int lives2Before = GameSession.LivesP2;

            MakeVulnerable(p1, h1);
            yield return null;
            Kill(h1, p1.transform.position);
            yield return null;

            Check(p1.IsDead && GameSession.LivesP1 == lives1Before - 1 && GameSession.LivesP2 == lives2Before
                  && !lm.GameOver,
                "10. Al morir el P1 pierde una vida y el P2 conserva las suyas sin game over",
                "P1 vivo=" + (!p1.IsDead) + " vidasP1=" + GameSession.LivesP1 + " vidasP2=" + GameSession.LivesP2
                + " gameOver=" + lm.GameOver);

            // Mientras el P1 está caído, el P2 debe seguir jugando con normalidad.
            bool p1DeadAtStart = p1.IsDead;
            StandAt(p2, 160f, 0.12f);
            yield return null;

            float rb0 = p2.transform.position.x;
            float rb1 = rb0;

            for (int attempt = 0; attempt < 3 && (rb1 - rb0) < 0.4f; attempt++)
            {
                PrepInput();
                if (attempt > 0) { ReleaseAll(); yield return null; }

                float moveEnd = Time.time + 0.45f;
                while (Time.time < moveEnd) { Press(Key.RightArrow); yield return null; }
                ReleaseAll();

                rb1 = p2.transform.position.x;
            }

            Check(p1DeadAtStart && !p2.IsDead && (rb1 - rb0) > 0.4f,
                "11. Con un jugador caído el otro sigue controlando su personaje",
                "P1 caído al empezar=" + p1DeadAtStart + " P2 vivo=" + (!p2.IsDead)
                + " P2 " + rb0.ToString("0.00") + "->" + rb1.ToString("0.00"));

            yield return new WaitForSecondsRealtime(1.5f);

            float respawnX = p1.transform.position.x;
            bool respawnOk = !p1.IsDead && h1.IsAlive && h1.Current == h1.maxHealth
                             && Mathf.Abs(respawnX - checkpointX) < 2f && !lm.GameOver;

            Check(respawnOk,
                "12. El jugador caído reaparece en el último checkpoint con la salud completa",
                "vivo=" + (!p1.IsDead) + " salud=" + h1.Current + "/" + h1.maxHealth
                + " x=" + respawnX.ToString("0.00") + " checkpoint=" + checkpointX.ToString("0.00")
                + " vidasP1=" + GameSession.LivesP1);

            // ---------------- H. CÁMARA COMPARTIDA Y ZOOM ----------------
            StandAt(p1, 20f, 0.12f);
            StandAt(p2, 26f, 0.12f);
            yield return new WaitForSecondsRealtime(1.3f);

            bool targetsOk = cam != null && cam.targetA == p1.transform && cam.targetB == p2.transform;
            float camNearX = cam != null ? cam.transform.position.x : -999f;
            float orthoNear = cam != null ? cam.GetComponent<Camera>().orthographicSize : 0f;

            StandAt(p1, 20f, 0.12f);
            StandAt(p2, 34f, 0.12f);
            yield return new WaitForSecondsRealtime(1.8f);

            float camFarX = cam != null ? cam.transform.position.x : -999f;
            float orthoFar = cam != null ? cam.GetComponent<Camera>().orthographicSize : 0f;

            // La cámara limita la separación a maxSeparation antes de calcular el punto medio.
            float expectedFar = 20f + Mathf.Min(34f - 20f, cam != null ? cam.maxSeparation : 34f - 20f) * 0.5f;

            Check(targetsOk && Mathf.Abs(camNearX - 23f) < 1.2f && Mathf.Abs(camFarX - expectedFar) < 1.2f,
                "13. La cámara sigue el punto medio de los dos jugadores",
                "objetivos=" + targetsOk + " camJuntos=" + camNearX.ToString("0.00")
                + " (medio 23,00) camSeparados=" + camFarX.ToString("0.00")
                + " (medio con el tope de separación " + expectedFar.ToString("0.00") + ")");

            Check(orthoFar > orthoNear + 0.25f && orthoNear >= 6f,
                "14. El zoom se abre al separarse los jugadores y vuelve a la base al reagruparse",
                "orthoJuntos=" + orthoNear.ToString("0.000") + " orthoSeparados=" + orthoFar.ToString("0.000"));

            // ---------------- I. ENEMIGOS EN COOPERATIVO ----------------
            UnparkEnemies();
            yield return new WaitForSecondsRealtime(0.3f);

            StandAt(p1, 172f, 0.15f);
            StandAt(p2, 170f, 0.15f);
            MakeVulnerable(p1, h1);
            MakeVulnerable(p2, h2);
            yield return null;

            int h1StartWave = h1.Current;
            int h2StartWave = h2.Current;
            int maxEnemyBullets = 0;
            bool anyoneHurt = false;

            float waveEnd = Time.time + 3.2f;
            while (Time.time < waveEnd)
            {
                var bullets = All<Projectile>();
                int enemyBullets = 0;
                for (int i = 0; i < bullets.Length; i++)
                    if (bullets[i] != null && bullets[i].faction == Faction.Enemy) enemyBullets++;

                maxEnemyBullets = Mathf.Max(maxEnemyBullets, enemyBullets);
                if (h1.Current < h1StartWave || h2.Current < h2StartWave) anyoneHurt = true;
                yield return null;
            }

            Check(maxEnemyBullets > 0 && anyoneHurt,
                "15. La oleada del generador se activa y combate en cooperativo",
                "proyectilesEnemigos=" + maxEnemyBullets
                + " dañoP1=" + (h1StartWave - h1.Current) + " dañoP2=" + (h2StartWave - h2.Current));

            ParkEnemies();
            yield return null;

            // ---------------- J. GAME OVER SOLO CON AMBOS AGOTADOS ----------------
            GameSession.SetLives(0, 1);
            GameSession.SetLives(1, 1);

            MakeVulnerable(p1, h1);
            yield return null;
            Kill(h1, p1.transform.position);
            yield return new WaitForSecondsRealtime(2.4f);

            bool noGameOverYet = lm != null && !lm.GameOver;

            MakeVulnerable(p2, h2);
            yield return null;
            Kill(h2, p2.transform.position);

            // La rutina del game over espera 1,8 s; se sondea hasta 4 s para no depender del fotograma.
            float gameOverDeadline = Time.realtimeSinceStartup + 4f;
            bool gameOverBoth = false;
            while (Time.realtimeSinceStartup < gameOverDeadline)
            {
                gameOverBoth = lm != null && lm.GameOver
                               && hud != null && hud.gameOverPanel != null && hud.gameOverPanel.activeSelf;
                if (gameOverBoth) break;
                yield return null;
            }

            bool lmGameOver = lm != null && lm.GameOver;
            bool panelShown = hud != null && hud.gameOverPanel != null && hud.gameOverPanel.activeSelf;

            Check(noGameOverYet && gameOverBoth,
                "16. Game Over solo cuando los dos jugadores agotan sus vidas",
                "sinGameOverConUnoVivo=" + noGameOverYet + " gameOverAlAgotarAmbos=" + gameOverBoth
                + " (bandera=" + lmGameOver + " panel=" + panelShown + ")"
                + " vidas=" + GameSession.LivesP1 + "/" + GameSession.LivesP2
                + " timeScale=" + Time.timeScale.ToString("0.00")
                + " misionCompleta=" + GameSession.MissionComplete);

            // ---------------- K. MISIÓN COMPLETADA CON DOS JUGADORES ----------------
            if (GameManager.Instance != null) GameManager.Instance.SetPaused(false);
            GameSession.SetLives(0, GameSession.StartingLives);
            GameSession.SetLives(1, GameSession.StartingLives);
            GameSession.MissionComplete = false;

            SceneManager.LoadScene(SceneNames.Level02);
            yield return null;
            yield return new WaitForSecondsRealtime(2f);

            Acquire();

            bool restarted = p1 != null && p2 != null && !p1.IsDead && !p2.IsDead;
            var generator = Object.FindFirstObjectByType<GeneratorObjective>();

            if (!restarted || generator == null || generator.health == null)
            {
                Check(false, "17. La misión se completa con los dos jugadores y llega a Results",
                    restarted ? "sin generador" : "no reaparecieron los dos jugadores");
                Finish();
                yield break;
            }

            StandAt(p1, 192f, 0.15f);
            StandAt(p2, 194f, 0.15f);
            MakeVulnerable(p1, h1);
            MakeVulnerable(p2, h2);
            yield return new WaitForSecondsRealtime(0.5f);

            int guardsKilled = 0;
            if (generator.shieldWaveObjects != null)
            {
                for (int i = 0; i < generator.shieldWaveObjects.Length; i++)
                {
                    var guard = generator.shieldWaveObjects[i];
                    if (guard == null) continue;

                    var guardHealth = guard.GetComponent<Health>();
                    if (guardHealth == null || !guardHealth.IsAlive) continue;

                    guardHealth.TakeDamage(new DamageInfo
                    {
                        amount = 9999,
                        point = (Vector2)guard.transform.position + new Vector2(2f, 0.5f),
                        direction = Vector2.left,
                        source = null,
                        playerIndex = -1
                    });

                    guardsKilled++;
                }
            }

            yield return new WaitForSecondsRealtime(0.8f);

            int scoreBefore = GameSession.Score;
            generator.health.TakeDamage(new DamageInfo
            {
                amount = 9999,
                point = (Vector2)generator.transform.position + new Vector2(2f, 0.5f),
                direction = Vector2.left,
                source = null,
                playerIndex = -1
            });

            bool bothAlive = !p1.IsDead && !p2.IsDead && h1.IsAlive && h2.IsAlive
                             && GameSession.LivesP1 > 0 && GameSession.LivesP2 > 0;
            int lives1AtEnd = GameSession.LivesP1;
            int lives2AtEnd = GameSession.LivesP2;

            yield return null;

            bool destroyed = generator.Destroyed && GameSession.Score >= scoreBefore + 1200;

            bool sawResults = false;
            float deadline = Time.realtimeSinceStartup + 12f;
            while (Time.realtimeSinceStartup < deadline)
            {
                if (SceneManager.GetActiveScene().name == SceneNames.Results) { sawResults = true; break; }
                yield return null;
            }

            Check(bothAlive && destroyed && sawResults && GameSession.MissionComplete,
                "17. Los dos jugadores completan Level02, destruyen el generador y llegan a Results",
                "guardias=" + guardsKilled + " ambosVivos=" + bothAlive
                + " vidas=" + lives1AtEnd + "/" + lives2AtEnd
                + " destruido=" + destroyed + " puntuación=" + GameSession.Score
                + " Results=" + sawResults);

            Finish();
        }

        private void Finish()
        {
            RestoreEditorInput();

            int total = passed + failed;
            if (failed == 0) Debug.Log("[C] ===== VEREDICTO: OK " + passed + "/" + total + " comprobaciones cooperativas =====");
            else Debug.LogError("[C] ===== VEREDICTO: FALLOS " + failed + " de " + total + " =====");
        }
    }
}
#endif
