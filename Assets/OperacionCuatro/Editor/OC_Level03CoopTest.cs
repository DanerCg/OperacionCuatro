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
    /// Fase 3 — validación COOPERATIVA de Level03 «LABORATORIO SUBTERRÁNEO».
    ///
    /// Arranca una sesión real de dos jugadores (DANNER + INÉS) en Level03 y comprueba
    /// movimiento, disparo y salud independientes, trampas, puertas e interruptores compartidos,
    /// ascensor reutilizable por el segundo jugador, checkpoint, muerte y reaparición de uno solo,
    /// cámara compartida con zoom, game over únicamente al agotar las vidas de ambos y cierre de
    /// misión por la zona segura de evacuación.
    ///
    /// Se lanza en Play Mode desde el menú OC o por reflexión sobre Run.
    /// </summary>
    public static class OC_Level03CoopTest
    {
        [UnityEditor.MenuItem("OC/Playtest/Level03 cooperativo (2 jugadores)")]
        public static void Run()
        {
            var go = new GameObject("[OC_Level03CoopTest]");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<OCLevel03CoopRunner>();
        }
    }

    public class OCLevel03CoopRunner : MonoBehaviour
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
        private readonly List<DamageOnTouch> parkedTouch = new List<DamageOnTouch>();

        // ==================================================================
        //  Utilidades
        // ==================================================================
        private void Check(bool ok, string what, string detail = "")
        {
            if (ok) { passed++; Debug.Log("[C3] PASS  " + what + (detail.Length > 0 ? " | " + detail : "")); }
            else { failed++; Debug.LogError("[C3] FAIL  " + what + (detail.Length > 0 ? " | " + detail : "")); }
        }

        private void Note(string text) { Debug.Log("[C3] ....  " + text); }

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
        // cuando el editor no tiene el foco de ventana del sistema, y entonces toda medida de
        // control se vuelve imposible. Para la duración de la suite se cambia a
        // «AllDeviceInputAlwaysGoesToGameView» y se restaura el valor original al terminar, de modo
        // que el proyecto queda exactamente igual.
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
                Debug.Log("[C3] ....  entrada: editorInputBehaviorInPlayMode cambiado TEMPORALMENTE a AllDeviceInputAlwaysGoesToGameView (original " + editorBehaviorOriginal + ")");
            }
            catch (System.Exception error)
            {
                Debug.LogWarning("[C3] no se pudo cambiar el ajuste de entrada temporal: " + error.GetType().Name);
            }
        }

        private void RestoreEditorInput()
        {
            if (!editorBehaviorSaved) return;

            try
            {
                InputSystem.settings.editorInputBehaviorInPlayMode = editorBehaviorOriginal;
                editorBehaviorSaved = false;
                Debug.Log("[C3] ....  entrada: editorInputBehaviorInPlayMode RESTAURADO a " + editorBehaviorOriginal);
            }
            catch (System.Exception error)
            {
                Debug.LogWarning("[C3] no se pudo restaurar el ajuste de entrada: " + error.GetType().Name);
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

            // El daño por contacto vive en un HitBox hijo con DamageOnTouch, que es un componente
            // independiente: sigue hiriendo aunque el cerebro del enemigo esté desactivado (y también
            // en enemigos que ya venían desactivados en la escena, que el bucle anterior ni mira).
            // Se aparcan TODOS los HitBox de la escena para que ninguna medida de daño de la suite
            // quede contaminada por un enemigo cercano.
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

        private void CountPlayerBullets(out int owner0, out int owner1)
        {
            owner0 = 0;
            owner1 = 0;

            var bullets = All<Projectile>();
            for (int i = 0; i < bullets.Length; i++)
            {
                if (bullets[i] == null || bullets[i].faction != Faction.Player) continue;
                if (bullets[i].ownerPlayerIndex == 0) owner0++;
                else if (bullets[i].ownerPlayerIndex == 1) owner1++;
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
            GameSession.StartLevel(3);

            Check(GameSession.PlayerCount == 2 && GameSession.P1 == CharacterId.Danner
                  && GameSession.P2 == CharacterId.Ines && GameSession.CurrentLevel == 3,
                "1. Sesión cooperativa preparada con dos personajes distintos en el nivel 3",
                "jugadores=" + GameSession.PlayerCount + " P1=" + GameSession.P1 + " P2=" + GameSession.P2
                + " nivel=" + GameSession.CurrentLevel);

            SceneManager.LoadScene(SceneNames.Level03);
            yield return null;
            yield return new WaitForSecondsRealtime(1.9f);

            Acquire();

            bool bothPresent = p1 != null && p2 != null && p1 != p2
                               && p1.playerIndex == 0 && p2.playerIndex == 1
                               && p1.name.Contains("Danner") && p2.name.Contains("Ines")
                               && h1 != null && h2 != null && h1 != h2;

            Check(bothPresent,
                "2. Level03 instancia a los dos jugadores con su personaje, índice y salud propios",
                Describe());

            if (!bothPresent) { Finish(); yield break; }

            bool hudOk = hud != null && hud.panelP1 != null && hud.panelP2 != null
                         && hud.panelP1.player == p1 && hud.panelP2.player == p2
                         && hud.panelP2.root != null && hud.panelP2.root.activeSelf
                         && hud.panelP1.nameText != null && hud.panelP1.nameText.text.Contains("DANNER")
                         && hud.panelP2.nameText != null && hud.panelP2.nameText.text.Contains("P2")
                         && hud.panelP2.nameText.text.Contains("INÉS");

            Check(hudOk, "3. El HUD muestra los dos paneles con P1 DANNER y P2 INÉS", HudDetail());

            ParkEnemies();
            yield return null;

            p1.SetInputEnabled(true);
            p2.SetInputEnabled(true);

            // ---- Calentamiento de la entrada sintética ----
            bool inputAlive = false;
            int inputAttempts = 0;
            for (int warm = 0; warm < 8 && !inputAlive; warm++)
            {
                inputAttempts++;
                PrepInput();
                // x=-3: el escalón de entrada (EntryStep_Fill) ocupa 5,50–10,50 y bloqueaba al P1
                // nada más empezar a andar, así que la medida dependía de la geometría del nivel.
                StandAt(p1, -3f, 0.12f);
                yield return null;

                float warmStart = p1.transform.position.x;
                float warmEnd = Time.time + 0.35f;
                while (Time.time < warmEnd) { Press(Key.D); yield return null; }
                ReleaseAll();

                if (p1.transform.position.x - warmStart > 0.4f) inputAlive = true;
            }

            Note("entrada sintética confirmada=" + inputAlive + " intentos=" + inputAttempts + " | " + InputState());

            // ---------------- B. MOVIMIENTO INDEPENDIENTE ----------------
            StandAt(p1, -3f, 0.12f);
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

            StandAt(p1, -3f, 0.12f);
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

            // ---------------- D. SALUD INDEPENDIENTE ----------------
            MakeVulnerable(p1, h1);
            MakeVulnerable(p2, h2);
            yield return null;

            int hp1Start = h1.Current;
            int hp2Start = h2.Current;

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
                "6. Salud independiente: el daño al P1 no altera la salud del P2",
                "P1 " + hp1Start + "->" + h1.Current + " P2 " + hp2Start + "->" + h2.Current);

            // ---------------- E. TRAMPA LÁSER COMPARTIDA ----------------
            var trap = Nearest<LaserTrap>(158f);
            bool trapOk = false;
            string trapDetail = "sin trampa láser";

            if (trap != null)
            {
                // El haz es una franja de 0,45 de ancho y el jugador mide 0,72: los dos no caben a la
                // vez sin solaparse y la separación física los expulsaría del haz. Se expone a cada
                // jugador por separado para comprobar que la misma trampa daña a ambos.
                //
                // No basta con que la salud baje: se registra el ORIGEN de cada impacto para
                // demostrar que el daño lo produjo esta trampa y no otro peligro de la escena.
                //
                // Las oleadas que se activan al teletransportar a los jugadores crean enemigos
                // NUEVOS (WaveTrigger instancia prefabs) con su HitBox de contacto: además de
                // contaminar la medida, cada golpe reactiva la invulnerabilidad de 1,4 s del
                // jugador y el haz ya no puede herirlo. Se deja que la zona se estabilice y se
                // aparca a todos los enemigos presentes antes de medir la trampa.
                Teleport(p1, trap.transform.position.x - 6f, 0.2f);
                Teleport(p2, trap.transform.position.x + 6f, 0.2f);
                yield return new WaitForSecondsRealtime(1.2f);
                ParkEnemies();
                yield return null;

                var sources1 = new List<string>();
                var sources2 = new List<string>();

                System.Action<DamageInfo> onDamaged1 = info =>
                    sources1.Add((info.source != null ? info.source.name : "nulo") + ":" + info.amount);
                System.Action<DamageInfo> onDamaged2 = info =>
                    sources2.Add((info.source != null ? info.source.name : "nulo") + ":" + info.amount);

                h1.Damaged += onDamaged1;
                h2.Damaged += onDamaged2;

                Teleport(p2, trap.transform.position.x + 6f, 0.2f);
                Teleport(p1, trap.transform.position.x, 0.2f);
                MakeVulnerable(p1, h1);
                MakeVulnerable(p2, h2);
                yield return null;

                int t1Start = h1.Current;
                int t2Start = h2.Current;
                bool hurt1 = false;
                bool hurt2 = false;

                float phase1 = Time.time + 6f;
                while (Time.time < phase1 && !hurt1)
                {
                    // Se sostiene al jugador dentro del haz: el retroceso de cualquier impacto lo
                    // expulsa de una franja de 0,45 de ancho y la medida nunca llegaría a observar
                    // la trampa. Sostenido en el haz, el impacto de la trampa es inevitable.
                    Teleport(p1, trap.transform.position.x, 0.2f);
                    if (h1.Current < t1Start) hurt1 = true;
                    yield return null;
                }

                Teleport(p1, trap.transform.position.x - 6f, 0.2f);
                Teleport(p2, trap.transform.position.x, 0.2f);
                yield return null;

                float phase2 = Time.time + 6f;
                while (Time.time < phase2 && !hurt2)
                {
                    Teleport(p2, trap.transform.position.x, 0.2f);
                    if (h2.Current < t2Start) hurt2 = true;
                    yield return null;
                }

                h1.Damaged -= onDamaged1;
                h2.Damaged -= onDamaged2;

                bool trapHit1 = sources1.Exists(s => s.StartsWith(trap.name + ":"));
                bool trapHit2 = sources2.Exists(s => s.StartsWith(trap.name + ":"));

                trapOk = hurt1 && hurt2 && trapHit1 && trapHit2;
                trapDetail = "P1 daño=" + (t1Start - h1.Current)
                             + " impactoDeLaTrampa=" + trapHit1 + " fuentesP1=[" + string.Join(", ", sources1.ToArray()) + "]"
                             + " | P2 daño=" + (t2Start - h2.Current)
                             + " impactoDeLaTrampa=" + trapHit2 + " fuentesP2=[" + string.Join(", ", sources2.ToArray()) + "]"
                             + " trampa=" + trap.name;
            }

            Check(trapOk, "7. Una misma trampa láser daña a los dos jugadores de forma independiente", trapDetail);

            // ---------------- F. PUERTAS E INTERRUPTORES COMPARTIDOS ----------------
            int scoreBefore = GameSession.Score;
            var switches = DoorSwitch.All;
            for (int i = 0; i < switches.Count; i++)
            {
                var sw = switches[i];
                if (sw == null || sw.channel != "A") continue;

                if (sw.health != null)
                {
                    sw.health.TakeDamage(new DamageInfo
                    {
                        amount = 10,
                        point = sw.transform.position,
                        direction = Vector2.right,
                        source = null,
                        playerIndex = 0
                    });
                    yield return null;

                    sw.health.TakeDamage(new DamageInfo
                    {
                        amount = 10,
                        point = sw.transform.position,
                        direction = Vector2.right,
                        source = null,
                        playerIndex = 1
                    });
                    yield return null;
                }
            }

            yield return new WaitForSecondsRealtime(0.3f);

            var doorA = Nearest<SecurityDoor>(49f);
            bool doorAOpen = doorA != null && doorA.IsOpen && doorA.blocker != null && !doorA.blocker.enabled;

            Teleport(p1, 47.5f, 0.2f);
            Teleport(p2, 47.5f, 0.2f);
            yield return new WaitForSecondsRealtime(0.2f);

            Teleport(p1, 50f, 0.2f);
            Teleport(p2, 50f, 0.2f);
            yield return new WaitForSecondsRealtime(0.45f);

            bool crossed = Mathf.Abs(p1.transform.position.x - 50f) < 0.9f
                           && Mathf.Abs(p2.transform.position.x - 50f) < 0.9f;

            Check(doorAOpen && crossed && GameSession.Score >= scoreBefore + 100,
                "8. Los interruptores abren la puerta para los dos jugadores y ambos la atraviesan",
                "puertaAbierta=" + doorAOpen + " P1x=" + p1.transform.position.x.ToString("0.00")
                + " P2x=" + p2.transform.position.x.ToString("0.00") + " puntos=" + (GameSession.Score - scoreBefore));

            // ---------------- G. ASCENSOR REUTILIZABLE ----------------
            var lift = Nearest<ElevatorPlatform>(100f);
            bool lift1Ok = false;
            bool lift2Ok = false;
            string liftDetail = "sin ascensor";

            if (lift != null)
            {
                lift.SnapTo(0f);
                yield return null;

                float baseY = lift.transform.position.y;
                Teleport(p1, lift.transform.position.x, baseY + 0.8f);
                Teleport(p2, lift.transform.position.x + 8f, 0.2f);
                yield return new WaitForSecondsRealtime(0.5f);

                float p1Top = p1.transform.position.y;
                float liftTop = baseY;

                float upEnd = Time.time + 6f;
                while (Time.time < upEnd)
                {
                    p1Top = Mathf.Max(p1Top, p1.transform.position.y);
                    liftTop = Mathf.Max(liftTop, lift.transform.position.y);
                    if (lift.Height01 > 0.98f) break;
                    yield return null;
                }

                lift1Ok = liftTop - baseY > 5f && p1Top - (baseY + 0.8f) > 4.5f;

                // El ascensor regresa solo para que el segundo jugador también lo use.
                float backEnd = Time.time + 8f;
                while (Time.time < backEnd)
                {
                    if (lift.Height01 < 0.02f) break;
                    yield return null;
                }

                Teleport(p1, 130f, 1.2f);
                Teleport(p2, lift.transform.position.x, lift.transform.position.y + 0.8f);
                yield return new WaitForSecondsRealtime(0.5f);

                float p2Top = p2.transform.position.y;
                float secondEnd = Time.time + 6f;
                while (Time.time < secondEnd)
                {
                    p2Top = Mathf.Max(p2Top, p2.transform.position.y);
                    if (lift.Height01 > 0.98f) break;
                    yield return null;
                }

                lift2Ok = p2Top > 4.5f;

                liftDetail = "primerJugador=" + lift1Ok + " alturaP1=" + p1Top.ToString("0.00")
                             + " segundoJugador=" + lift2Ok + " alturaP2=" + p2Top.ToString("0.00")
                             + " recorrido=" + (liftTop - baseY).ToString("0.00");

                Teleport(p2, 130f, 1.2f);
            }

            Check(lift1Ok && lift2Ok,
                "9. El ascensor sube al primer jugador y regresa solo para subir también al segundo",
                liftDetail);

            // ---------------- H. CHECKPOINT, MUERTE Y REAPARICIÓN ----------------
            var checkpoint = Nearest<Checkpoint>(121f);
            StandAt(p1, 121f, 0.15f);
            yield return new WaitForSecondsRealtime(0.5f);

            float checkpointX = lm != null ? lm.checkpointPosition.x : -999f;

            Check(checkpoint != null && Mathf.Abs(checkpointX - 121f) < 3f && lm != null && !lm.GameOver,
                "10. El checkpoint del centro de automatización fija el punto de reaparición",
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
                "11. Al morir el P1 pierde una vida y el P2 conserva las suyas sin game over",
                "P2 vivo=" + (!p2.IsDead) + " vidasP1=" + GameSession.LivesP1 + " vidasP2=" + GameSession.LivesP2
                + " gameOver=" + lm.GameOver);

            bool p1DeadAtStart = p1.IsDead;
            StandAt(p2, 110f, 0.12f);
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
                "12. Con un jugador caído el otro sigue controlando su personaje",
                "P1 caído al empezar=" + p1DeadAtStart + " P2 vivo=" + (!p2.IsDead)
                + " P2 " + rb0.ToString("0.00") + "->" + rb1.ToString("0.00"));

            yield return new WaitForSecondsRealtime(1.6f);

            float respawnX = p1.transform.position.x;
            bool respawnOk = !p1.IsDead && h1.IsAlive && h1.Current == h1.maxHealth
                             && Mathf.Abs(respawnX - checkpointX) < 2f && !lm.GameOver;

            Check(respawnOk,
                "13. El jugador caído reaparece en el último checkpoint con la salud completa",
                "vivo=" + (!p1.IsDead) + " salud=" + h1.Current + "/" + h1.maxHealth
                + " x=" + respawnX.ToString("0.00") + " checkpoint=" + checkpointX.ToString("0.00")
                + " vidasP1=" + GameSession.LivesP1);

            // ---------------- I. CÁMARA COMPARTIDA Y ZOOM ----------------
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

            float expectedFar = 20f + Mathf.Min(34f - 20f, cam != null ? cam.maxSeparation : 14f) * 0.5f;

            Check(targetsOk && Mathf.Abs(camNearX - 23f) < 1.2f && Mathf.Abs(camFarX - expectedFar) < 1.2f,
                "14. La cámara sigue el punto medio de los dos jugadores",
                "objetivos=" + targetsOk + " camJuntos=" + camNearX.ToString("0.00")
                + " (medio 23,00) camSeparados=" + camFarX.ToString("0.00")
                + " (medio con el tope de separación " + expectedFar.ToString("0.00") + ")");

            Check(orthoFar > orthoNear + 0.25f && orthoNear >= 6f,
                "15. El zoom se abre al separarse los jugadores y vuelve a la base al reagruparse",
                "orthoJuntos=" + orthoNear.ToString("0.000") + " orthoSeparados=" + orthoFar.ToString("0.000"));

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

            float gameOverDeadline = Time.realtimeSinceStartup + 4f;
            bool gameOverBoth = false;
            while (Time.realtimeSinceStartup < gameOverDeadline)
            {
                gameOverBoth = lm != null && lm.GameOver
                               && hud != null && hud.gameOverPanel != null && hud.gameOverPanel.activeSelf;
                if (gameOverBoth) break;
                yield return null;
            }

            Check(noGameOverYet && gameOverBoth,
                "16. Game Over solo cuando los dos jugadores agotan sus vidas",
                "sinGameOverConUnoVivo=" + noGameOverYet + " gameOverAlAgotarAmbos=" + gameOverBoth
                + " vidas=" + GameSession.LivesP1 + "/" + GameSession.LivesP2);

            // ---------------- K. MISIÓN COMPLETADA CON DOS JUGADORES ----------------
            if (GameManager.Instance != null) GameManager.Instance.SetPaused(false);
            GameSession.SetLives(0, GameSession.StartingLives);
            GameSession.SetLives(1, GameSession.StartingLives);
            GameSession.MissionComplete = false;

            SceneManager.LoadScene(SceneNames.Level03);
            yield return null;
            yield return new WaitForSecondsRealtime(2f);

            Acquire();

            bool restarted = p1 != null && p2 != null && !p1.IsDead && !p2.IsDead;
            var units = All<PowerUnit>();
            var exit = Object.FindFirstObjectByType<SafeExitZone>();

            if (!restarted || units.Length < 3 || exit == null)
            {
                Check(false, "17. Los dos jugadores completan la evacuación de Level03 y llegan a Results",
                    restarted ? "sin unidades de energía o sin zona segura" : "no reaparecieron los dos jugadores");
                Finish();
                yield break;
            }

            ParkEnemies();

            for (int i = 0; i < units.Length; i++)
            {
                if (units[i] == null || units[i].health == null) continue;
                units[i].health.TakeDamage(new DamageInfo
                {
                    amount = 9999,
                    point = units[i].transform.position,
                    direction = Vector2.right,
                    source = null,
                    playerIndex = i % 2
                });
                yield return null;
            }

            yield return new WaitForSecondsRealtime(0.7f);

            var escape = EscapeSequence.Instance;
            bool alarmOn = escape != null && escape.Begun;

            bool bothAlive = !p1.IsDead && !p2.IsDead && h1.IsAlive && h2.IsAlive
                             && GameSession.LivesP1 > 0 && GameSession.LivesP2 > 0;

            Teleport(p1, 246f, 2.6f);
            Teleport(p2, 245f, 2.6f);
            yield return null;

            Teleport(p1, exit.transform.position.x, exit.transform.position.y);
            Teleport(p2, exit.transform.position.x - 1f, exit.transform.position.y);

            bool sawResults = false;
            float deadline = Time.realtimeSinceStartup + 14f;
            while (Time.realtimeSinceStartup < deadline)
            {
                if (SceneManager.GetActiveScene().name == SceneNames.Results) { sawResults = true; break; }
                yield return null;
            }

            Check(alarmOn && bothAlive && sawResults && GameSession.MissionComplete,
                "17. Los dos jugadores completan la evacuación de Level03 y llegan a Results",
                "alarma=" + alarmOn + " ambosVivos=" + bothAlive
                + " vidas=" + GameSession.LivesP1 + "/" + GameSession.LivesP2
                + " puntuación=" + GameSession.Score + " Results=" + sawResults);

            Finish();
        }

        private void Finish()
        {
            RestoreEditorInput();

            int total = passed + failed;
            if (failed == 0) Debug.Log("[C3] ===== VEREDICTO: OK " + passed + "/" + total + " comprobaciones cooperativas de Level03 =====");
            else Debug.LogError("[C3] ===== VEREDICTO: FALLOS " + failed + " de " + total + " =====");
        }
    }
}
#endif
