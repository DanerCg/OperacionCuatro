#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OperacionCuatro.EditorTools
{
    public static class OC_PlayTest
    {
        [UnityEditor.MenuItem("OC/Playtest/1 jugador")]
        public static void RunOne() { Launch(1); }

        [UnityEditor.MenuItem("OC/Playtest/2 jugadores")]
        public static void RunTwo() { Launch(2); }

        private static void Launch(int players)
        {
            var go = new GameObject("[OC_PlayTest]");
            Object.DontDestroyOnLoad(go);
            var runner = go.AddComponent<OCPlayTestRunner>();
            runner.players = players;
        }
    }

    public static class OC_SystemsTest
    {
        // Precondicion: sesion limpia de 1 jugador en Level01. GameSession es estatico y,
        // con la recarga de dominio desactivada, sobrevive entre sesiones de Play; si se
        // lanza tras la prueba de 2 jugadores el nivel arranca con dos jugadores y vidas
        // ya gastadas, y la prueba de Game Over falla por estado heredado, no por el juego.
        [UnityEditor.MenuItem("OC/Playtest/Sistemas (pausa + game over)")]
        public static void RunSystems()
        {
            var go = new GameObject("[OC_SystemsTest]");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<OCSystemsRunner>();
        }
    }

    public class OCSystemsRunner : MonoBehaviour
    {
        private int passed;
        private int failed;

        private void Check(bool ok, string what, string detail = "")
        {
            if (ok) { passed++; Debug.Log("[S] PASS  " + what + (detail.Length > 0 ? " | " + detail : "")); }
            else { failed++; Debug.LogError("[S] FAIL  " + what + (detail.Length > 0 ? " | " + detail : "")); }
        }

        private IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(0.8f);

            var lm = LevelManager.Instance;
            Check(lm != null, "LevelManager en Level01");
            if (lm == null) { Finish(); yield break; }

            var hud = Object.FindFirstObjectByType<LevelHUD>();
            var pause = Object.FindFirstObjectByType<PauseMenuUI>();
            Check(hud != null, "LevelHUD presente");
            Check(pause != null, "PauseMenuUI presente");

            // ---------- PAUSA ----------
            if (pause != null)
            {
                pause.Open();
                yield return null;
                Check(GameManager.Instance != null && GameManager.Instance.IsPaused, "Pausa activa el bloqueo");
                Check(Mathf.Approximately(Time.timeScale, 0f), "Pausa detiene el tiempo", "timeScale=" + Time.timeScale);
                Check(pause.panel != null && pause.panel.activeSelf, "Panel de pausa visible");
                Check(pause.IsOpen, "IsOpen = true");

                pause.Resume();
                yield return null;
                Check(GameManager.Instance != null && !GameManager.Instance.IsPaused, "Reanudar quita el bloqueo");
                Check(Mathf.Approximately(Time.timeScale, 1f), "Reanudar restaura el tiempo", "timeScale=" + Time.timeScale);
                Check(pause.panel != null && !pause.panel.activeSelf, "Panel de pausa oculto");
            }

            // ---------- VIDAS Y GAME OVER ----------
            var p1 = lm.Players.Count > 0 ? lm.Players[0] : null;
            Check(p1 != null, "Jugador disponible para la prueba de vidas");
            if (p1 == null) { Finish(); yield break; }

            var health = p1.GetComponent<Health>();
            Check(health != null, "Jugador tiene Health");
            Check(GameSession.GetLives(0) == GameSession.StartingLives, "Vidas iniciales = " + GameSession.StartingLives,
                GameSession.GetLives(0).ToString());

            for (int i = 0; i < 6 && !lm.GameOver; i++)
            {
                p1.ClearInvulnerability();
                health.TakeDamage(new DamageInfo
                {
                    amount = 999,
                    point = p1.transform.position,
                    direction = Vector2.right,
                    source = null,
                    playerIndex = -1
                });

                yield return new WaitForSecondsRealtime(2.0f);
                Debug.Log("[S] muerte " + (i + 1) + " -> vidas=" + GameSession.GetLives(0) + " gameOver=" + lm.GameOver);
            }

            Check(lm.GameOver, "Game Over al agotar las vidas", "gameOver=" + lm.GameOver);
            if (hud != null)
                Check(hud.gameOverPanel != null && hud.gameOverPanel.activeSelf, "Panel GAME OVER visible");

            // ---------- REINICIO ----------
            lm.RestartLevel();
            yield return new WaitForSecondsRealtime(2.8f);
            Check(SceneManager.GetActiveScene().name == "Level01", "Reiniciar recarga Level01",
                SceneManager.GetActiveScene().name);
            Check(GameSession.GetLives(0) == GameSession.StartingLives, "Reiniciar restaura las vidas",
                GameSession.GetLives(0).ToString());
            Check(Mathf.Approximately(Time.timeScale, 1f), "Reiniciar reanuda el tiempo", "timeScale=" + Time.timeScale);

            Finish();
        }

        private void Finish()
        {
            int total = passed + failed;
            if (failed == 0) Debug.Log("[S] ===== VEREDICTO SISTEMAS: OK " + passed + "/" + total + " =====");
            else Debug.LogError("[S] ===== VEREDICTO SISTEMAS: FALLOS " + failed + " de " + total + " =====");
        }
    }

    public class OCPlayTestRunner : MonoBehaviour
    {
        public int players = 1;

        private int passed;
        private int failed;
        private Keyboard kb;

        private Keyboard Kb()
        {
            if (kb == null || !kb.added) kb = Keyboard.current;
            if (kb == null) kb = InputSystem.AddDevice<Keyboard>();
            return kb;
        }

        private void Press(params Key[] keys) { InputSystem.QueueStateEvent(Kb(), new KeyboardState(keys)); }
        private void ReleaseAll() { InputSystem.QueueStateEvent(Kb(), new KeyboardState()); }

        private void Check(bool ok, string what, string detail = "")
        {
            if (ok) { passed++; Debug.Log("[T] PASS  " + what + (detail.Length > 0 ? " | " + detail : "")); }
            else { failed++; Debug.LogError("[T] FAIL  " + what + (detail.Length > 0 ? " | " + detail : "")); }
        }

        private static T Find<T>() where T : Object { return Object.FindFirstObjectByType<T>(); }

        // Localiza un punto del nivel con suelo firme, caja del personaje libre y
        // espacio aereo suficiente para medir movimiento y salto sin obstaculos.
        // Se valida con consultas de fisica 2D reales, no con coordenadas fijas.
        internal static bool FindClearStand(float fromX, float toX, out Vector3 stand)
        {
            stand = Vector3.zero;
            const int SolidMask = ~((1 << 9) | (1 << 11) | (1 << 12) | (1 << 13));

            for (float x = fromX; x <= toX; x += 0.25f)
            {
                var ground = Physics2D.Raycast(new Vector2(x, 40f), Vector2.down, 60f, 1 << 8);
                if (ground.collider == null) continue;

                // Debe ser el pavimento del patio (ancho), no la tapa de un contenedor
                // ni un barril: sobre un prop estrecho el personaje queda apoyado en
                // una superficie minima y las medidas de movimiento no son fiables.
                if (ground.collider.bounds.size.x < 10f) continue;

                float top = ground.point.y;

                // Caja del personaje (0.72 x 1.72) con margen, elevada 0.10 para no
                // tocar el propio suelo del punto: debe estar completamente libre.
                if (Physics2D.OverlapBox(new Vector2(x, top + 0.96f), new Vector2(0.92f, 1.72f), 0f, SolidMask) != null)
                    continue;

                // Espacio del salto (sube ~2.5 m) y margen lateral para caminar.
                if (Physics2D.OverlapBox(new Vector2(x, top + 3.10f), new Vector2(3.20f, 1.60f), 0f, SolidMask) != null)
                    continue;

                // Carril a la altura del canon (Muzzle y=+1.06 respecto a los pies):
                // sin esto el punto puede quedar pegado a una pared y el disparo nace
                // dentro del obstaculo, invalidando la prueba de disparo.
                if (Physics2D.OverlapBox(new Vector2(x, top + 1.06f), new Vector2(1.60f, 0.30f), 0f, SolidMask) != null)
                    continue;

                stand = new Vector3(x, top + 0.60f, 0f);
                return true;
            }

            return false;
        }

        private static void Click(Button b, string name)
        {
            if (b == null) { Debug.LogError("[T] Boton nulo: " + name); return; }
            b.onClick.Invoke();
        }

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(0.5f);

            // ---------- 1. MENU -> SELECCION ----------
            Check(SceneManager.GetActiveScene().name == "MainMenu", "Escena inicial es MainMenu",
                SceneManager.GetActiveScene().name);

            var menu = Find<MainMenuUI>();
            Check(menu != null, "MainMenuUI presente");

            if (menu != null)
                Click(players == 1 ? menu.onePlayerButton : menu.twoPlayersButton, players == 1 ? "1 JUGADOR" : "2 JUGADORES");

            yield return new WaitForSeconds(1.8f);

            Check(SceneManager.GetActiveScene().name == "CharacterSelection",
                "Menu -> CharacterSelection", SceneManager.GetActiveScene().name);
            Check(GameSession.PlayerCount == players, "GameSession.PlayerCount = " + players, GameSession.PlayerCount.ToString());

            // ---------- 2. SELECCION -> NIVEL ----------
            var sel = Find<CharacterSelectUI>();
            Check(sel != null, "CharacterSelectUI presente");

            if (sel != null && sel.characterButtons != null)
            {
                Click(sel.characterButtons[0], "personaje 0");
                yield return new WaitForSeconds(0.15f);
                if (players == 2) { Click(sel.characterButtons[2], "personaje 2"); yield return new WaitForSeconds(0.15f); }
            }

            yield return new WaitForSeconds(0.7f); // margen: aqui se rompia antes el fundido

            if (sel != null) Click(sel.startButton, "INICIAR MISION");

            yield return new WaitForSeconds(2.6f);

            Check(SceneManager.GetActiveScene().name == "Level01", "CharacterSelection -> Level01",
                SceneManager.GetActiveScene().name);

            var lm = LevelManager.Instance;
            Check(lm != null, "LevelManager presente");
            if (lm == null) { Finish(); yield break; }

            Check(lm.Players.Count == players, "Jugadores instanciados = " + players, lm.Players.Count.ToString());

            var p1 = lm.Players.Count > 0 ? lm.Players[0] : null;
            Check(p1 != null, "Jugador 1 existe");
            if (p1 == null) { Finish(); yield break; }

            Debug.Log("[T] P1 nombre=" + p1.name + " pos=" + p1.transform.position + " vida=" + p1.GetComponent<Health>().Current);

            yield return new WaitForSeconds(0.8f);

            // ---------- 3. SUELO / MOVIMIENTO ----------
            Check(p1.IsGrounded, "P1 en el suelo tras aparecer", "IsGrounded=" + p1.IsGrounded);

            // Corredor de salida: debe poder caminar a la derecha desde el punto de aparicion.
            // La primera tanda de pulsaciones sinteticas puede perderse mientras el Editor gana
            // el foco del Game View: si no se registro ninguna velocidad, se repite una vez.
            float sp0 = p1.transform.position.x;
            float maxVxCorredor = 0f;
            for (int intento = 0; intento < 2; intento++)
            {
                float tCorredor = Time.time + 1.2f;
                while (Time.time < tCorredor)
                {
                    Press(Key.D);
                    var rc = p1.GetComponent<Rigidbody2D>();
                    if (rc != null) maxVxCorredor = Mathf.Max(maxVxCorredor, Mathf.Abs(rc.linearVelocity.x));
                    yield return null;
                }
                ReleaseAll();
                yield return new WaitForSeconds(0.15f);

                if (p1.transform.position.x > sp0 + 1.5f || maxVxCorredor > 0.01f) break;
                Debug.Log("[T] corredor: primera tanda sin entrada (maxVx=0); se repite");
            }

            float sp1 = p1.transform.position.x;
            Check(sp1 > sp0 + 1.5f, "Corredor de salida despejado (D desde spawn)",
                "x " + sp0.ToString("0.00") + " -> " + sp1.ToString("0.00") + " maxVx=" + maxVxCorredor.ToString("0.00"));

            // Zona despejada de FinalYard para medir movimiento y salto sin obstaculos.
            // El punto se valida con consultas de fisica 2D (suelo firme, caja del
            // personaje libre y altura suficiente) antes de mover al jugador.
            Vector3 stand;
            if (FindClearStand(116f, 121f, out stand))
                Debug.Log("[T] punto de prueba despejado | " + stand.ToString("0.00"));
            else
            {
                stand = new Vector3(118.5f, 2.4f, 0f);
                Debug.LogError("[T] sin punto despejado en FinalYard; usando reserva " + stand.ToString("0.00"));
            }

            p1.transform.position = stand;
            var rb1 = p1.GetComponent<Rigidbody2D>();
            if (rb1 != null) rb1.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            yield return new WaitForSeconds(0.7f);
            Check(p1.IsGrounded, "P1 se asienta en zona despejada", "y=" + p1.transform.position.y.ToString("0.00"));

            float x0 = p1.transform.position.x;
            float maxVx = 0f;
            float tD = Time.time + 0.8f;
            while (Time.time < tD)
            {
                Press(Key.D);
                maxVx = Mathf.Max(maxVx, Mathf.Abs(rb1.linearVelocity.x));
                yield return null;
            }
            ReleaseAll();
            yield return new WaitForSeconds(0.1f);
            float x1 = p1.transform.position.x;
            Check(maxVx > 5.5f, "Velocidad de avance = moveSpeed", "max |vx| = " + maxVx.ToString("0.00"));
            Check(x1 > x0 + 0.3f, "P1 avanza a la derecha con D", "x " + x0.ToString("0.00") + " -> " + x1.ToString("0.00"));
            Check(p1.FacingRight, "P1 mira a la derecha", p1.FacingRight.ToString());

            float x2 = p1.transform.position.x;
            float maxVx2 = 0f;
            float tA = Time.time + 0.6f;
            while (Time.time < tA)
            {
                Press(Key.A);
                maxVx2 = Mathf.Max(maxVx2, Mathf.Abs(rb1.linearVelocity.x));
                yield return null;
            }
            ReleaseAll();
            yield return new WaitForSeconds(0.1f);
            Check(maxVx2 > 5.5f, "Velocidad de retroceso = moveSpeed", "max |vx| = " + maxVx2.ToString("0.00"));
            Check(p1.transform.position.x < x2 - 0.3f, "P1 retrocede con A",
                "x " + x2.ToString("0.00") + " -> " + p1.transform.position.x.ToString("0.00"));

            // ---------- 4. SALTO ----------
            float y0 = p1.transform.position.y;
            float peak = y0;
            float tJump = Time.time + 0.35f;
            while (Time.time < tJump)
            {
                Press(Key.K);
                if (p1.transform.position.y > peak) peak = p1.transform.position.y;
                yield return null;
            }
            ReleaseAll();
            for (int i = 0; i < 30; i++)
            {
                yield return new WaitForSeconds(0.03f);
                if (p1.transform.position.y > peak) peak = p1.transform.position.y;
            }
            Check(peak > y0 + 1.0f, "P1 salta con K", "y " + y0.ToString("0.00") + " -> pico " + peak.ToString("0.00"));

            for (int i = 0; i < 40 && !p1.IsGrounded; i++) yield return new WaitForSeconds(0.05f);
            Check(p1.IsGrounded, "P1 vuelve al suelo", "IsGrounded=" + p1.IsGrounded +
                " pos=" + p1.transform.position.ToString("0.00") + " vy=" + rb1.linearVelocity.y.ToString("0.00"));

            // ---------- 5. DISPARO ----------
            var shooter = p1.GetComponent<PlayerShooter>();
            Check(shooter != null, "PlayerShooter presente");

            // La bala se instancia al disparar y puede morir en el mismo paso de fisica
            // si el canon esta pegado a un obstaculo, asi que se mide el maximo por
            // fotograma durante la ventana de disparo y no solo el recuento final.
            int before = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length;
            int peakProjectiles = before;
            float tFire = Time.time + 0.45f;
            while (Time.time < tFire)
            {
                Press(Key.J);
                int now = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length;
                if (now > peakProjectiles) peakProjectiles = now;
                yield return null;
            }
            ReleaseAll();
            int after = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length;
            Check(peakProjectiles > before, "Disparo crea proyectiles",
                before + " -> pico " + peakProjectiles + " (resto actual " + after + ")");

            yield return new WaitForSeconds(0.6f);

            // ---------- 6. BOMBA ----------
            int bombsBefore = shooter != null ? shooter.Bombs : -1;
            if (shooter != null) shooter.TryBomb(Vector2.right);
            yield return new WaitForSeconds(0.2f);
            var bomb = Object.FindFirstObjectByType<Bomb>();
            Check(bomb != null, "Bomba instanciada");
            Check(shooter != null && shooter.Bombs == bombsBefore - 1, "Bomba consume municion",
                bombsBefore + " -> " + (shooter != null ? shooter.Bombs : -1));

            yield return new WaitForSeconds(1.6f);

            // ---------- 7. DAÑO A ENEMIGO ----------
            var enemies = Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
            Check(enemies.Length > 0, "Enemigos en el nivel", "n=" + enemies.Length);

            if (enemies.Length > 0)
            {
                var e = enemies[0];
                var eh = e.GetComponent<Health>();
                int hpBefore = eh.Current;
                int scoreBefore = GameSession.Score;
                eh.TakeDamage(new DamageInfo { amount = 12, point = e.transform.position, direction = Vector2.right, source = p1.gameObject, playerIndex = 0 });
                Check(eh.Current == hpBefore - 12, "Enemigo recibe daño", hpBefore + " -> " + eh.Current);

                eh.TakeDamage(new DamageInfo { amount = 999, point = e.transform.position, direction = Vector2.right, source = p1.gameObject, playerIndex = 0 });
                yield return new WaitForSeconds(0.2f);
                Check(GameSession.Score > scoreBefore, "Puntuacion sube al matar enemigo",
                    scoreBefore + " -> " + GameSession.Score);
            }

            // ---------- 8. CHECKPOINT ----------
            var checkpoint = Find<Checkpoint>();
            Check(checkpoint != null, "Checkpoint presente");
            Vector3 cpBefore = lm.checkpointPosition;
            if (checkpoint != null)
            {
                p1.transform.position = checkpoint.transform.position + new Vector3(0f, 0.2f, 0f);
                yield return new WaitForSeconds(0.5f);
                Check(lm.checkpointPosition != cpBefore, "Checkpoint actualiza el punto de reaparicion",
                    cpBefore + " -> " + lm.checkpointPosition);
            }

            // ---------- 9. OLEADA FINAL ----------
            var wave = Find<WaveTrigger>();
            Check(wave != null, "WaveTrigger presente");
            int activeBefore = 0;
            if (wave != null && wave.enemiesToActivate != null)
                for (int i = 0; i < wave.enemiesToActivate.Length; i++)
                    if (wave.enemiesToActivate[i] != null && wave.enemiesToActivate[i].activeSelf) activeBefore++;

            if (wave != null)
            {
                p1.transform.position = wave.transform.position + new Vector3(0f, 0.5f, 0f);
                yield return new WaitForSeconds(0.6f);
            }

            int activeAfter = 0;
            if (wave != null && wave.enemiesToActivate != null)
                for (int i = 0; i < wave.enemiesToActivate.Length; i++)
                    if (wave.enemiesToActivate[i] != null && wave.enemiesToActivate[i].activeSelf) activeAfter++;

            Check(activeAfter > activeBefore, "WaveTrigger activa enemigos", activeBefore + " -> " + activeAfter);

            // ---------- 10. ZONA DE META ----------
            var goal = Find<GoalZone>();
            Check(goal != null, "GoalZone presente (script restaurado)");

            // Matar la oleada para que la meta se abra
            if (wave != null && wave.enemiesToActivate != null)
            {
                for (int i = 0; i < wave.enemiesToActivate.Length; i++)
                {
                    var go = wave.enemiesToActivate[i];
                    if (go == null) continue;
                    var h = go.GetComponent<Health>();
                    if (h != null && h.IsAlive)
                        h.TakeDamage(new DamageInfo { amount = 9999, point = go.transform.position, direction = Vector2.right, source = p1.gameObject, playerIndex = 0 });
                }
            }

            yield return new WaitForSeconds(1.6f);

            if (goal != null)
            {
                p1.transform.position = goal.transform.position + new Vector3(-1.2f, -1.5f, 0f);
                yield return new WaitForSeconds(0.8f);
            }

            Check(lm.MissionComplete, "Mision completada al entrar en GoalZone", "MissionComplete=" + lm.MissionComplete);

            // ---------- 11. RESULTADOS ----------
            if (lm.MissionComplete)
            {
                yield return new WaitForSeconds(3.6f);
                Check(SceneManager.GetActiveScene().name == "Results", "Level01 -> Results",
                    SceneManager.GetActiveScene().name);
                Check(Find<ResultsUI>() != null, "ResultsUI presente");
            }

            Finish();
        }

        private void Finish()
        {
            int total = passed + failed;
            if (failed == 0) Debug.Log("[T] ===== VEREDICTO: OK " + passed + "/" + total + " comprobaciones =====");
            else Debug.LogError("[T] ===== VEREDICTO: FALLOS " + failed + " de " + total + " =====");
        }
    }

    // Prueba de regresion de la deteccion de suelo: los cuatro personajes deben
    // quedar apoyados en suelo llano, borde de plataforma, plataforma de otra
    // altura y props solidos; y NO deben detectar suelo en el aire ni junto a una pared.
    public static class OC_GroundTest
    {
        [UnityEditor.MenuItem("OC/Playtest/Deteccion de suelo (4 personajes)")]
        public static void Run()
        {
            var go = new GameObject("[OC_GroundTest]");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<OCGroundTestRunner>();
        }
    }

    public class OCGroundTestRunner : MonoBehaviour
    {
        private int passed;
        private int failed;
        private Keyboard kb;

        private class Spot
        {
            public string label;
            public Vector3 position;
            public bool expectGrounded;
            public float settle;
            public bool testJump;
        }

        private Keyboard Kb()
        {
            if (kb == null || !kb.added) kb = Keyboard.current;
            if (kb == null) kb = InputSystem.AddDevice<Keyboard>();
            return kb;
        }

        private void Press(params Key[] keys) { InputSystem.QueueStateEvent(Kb(), new KeyboardState(keys)); }
        private void ReleaseAll() { InputSystem.QueueStateEvent(Kb(), new KeyboardState()); }

        private void Check(bool ok, string what, string detail = "")
        {
            if (ok) { passed++; Debug.Log("[G] PASS  " + what + (detail.Length > 0 ? " | " + detail : "")); }
            else { failed++; Debug.LogError("[G] FAIL  " + what + (detail.Length > 0 ? " | " + detail : "")); }
        }

        private static Bounds SolidBounds(string path)
        {
            var go = GameObject.Find(path);
            if (go == null) return new Bounds(Vector3.zero, Vector3.zero);
            var c = go.GetComponent<Collider2D>();
            return c != null ? c.bounds : new Bounds(Vector3.zero, Vector3.zero);
        }

        private static List<Spot> BuildSpots()
        {
            var spots = new List<Spot>();

            var ray = Physics2D.Raycast(new Vector2(118.5f, 40f), Vector2.down, 60f, 1 << 8);
            float flatTop = ray.collider != null ? ray.point.y : 2f;

            // 1. Suelo llano
            spots.Add(new Spot
            {
                label = "suelo llano",
                position = new Vector3(118.5f, flatTop + 0.08f, 0f),
                expectGrounded = true,
                settle = 0.60f
            });

            // 2. Borde de plataforma: el personaje apoya solo una parte de su huella.
            //    Es el caso que dejaba IsGrounded=false estando fisicamente apoyado.
            var p7 = SolidBounds("World/Platforms/Platform_7");
            if (p7.size.x > 0f)
                spots.Add(new Spot
                {
                    label = "borde de plataforma",
                    position = new Vector3(p7.max.x + 0.15f, p7.max.y + 0.02f, 0f),
                    expectGrounded = true,
                    settle = 0.60f,
                    testJump = true
                });

            // 3. Plataforma de otra altura
            var p8 = SolidBounds("World/Platforms/Platform_8");
            if (p8.size.x > 0f)
                spots.Add(new Spot
                {
                    label = "plataforma alta",
                    position = new Vector3(p8.center.x, p8.max.y + 0.06f, 0f),
                    expectGrounded = true,
                    settle = 0.60f
                });

            // 4. Prop solido (barril) sobre el que tambien se puede estar de pie
            var barrel = SolidBounds("World/Props/Barrel_Final");
            if (barrel.size.x > 0f)
                spots.Add(new Spot
                {
                    label = "barril (prop solido)",
                    position = new Vector3(barrel.center.x, barrel.max.y + 0.06f, 0f),
                    expectGrounded = true,
                    settle = 0.60f
                });

            // 5. En el aire: no debe detectar suelo
            spots.Add(new Spot
            {
                label = "en el aire",
                position = new Vector3(118.5f, flatTop + 1.20f, 0f),
                expectGrounded = false,
                settle = 0.10f
            });

            // 6. Pegado a una pared alta: no debe detectar suelo
            var wall = SolidBounds("World/Props/ContainerBlue_2");
            if (wall.size.x > 0f)
                spots.Add(new Spot
                {
                    label = "junto a pared",
                    position = new Vector3(wall.min.x - 0.38f, flatTop + 0.60f, 0f),
                    expectGrounded = false,
                    settle = 0.10f
                });

            return spots;
        }

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(0.6f);

            if (SceneManager.GetActiveScene().name == "MainMenu")
            {
                var menu = Object.FindFirstObjectByType<MainMenuUI>();
                if (menu != null && menu.onePlayerButton != null) menu.onePlayerButton.onClick.Invoke();
                yield return new WaitForSeconds(1.8f);

                var sel = Object.FindFirstObjectByType<CharacterSelectUI>();
                if (sel != null && sel.characterButtons != null && sel.characterButtons.Length > 0)
                    sel.characterButtons[0].onClick.Invoke();
                yield return new WaitForSeconds(0.85f);
                if (sel != null && sel.startButton != null) sel.startButton.onClick.Invoke();
                yield return new WaitForSeconds(2.6f);
            }

            var lm = LevelManager.Instance;
            if (lm == null)
            {
                Debug.LogError("[G] sin LevelManager; escena=" + SceneManager.GetActiveScene().name);
                yield break;
            }

            InputActionAsset asset = lm.inputAsset;
            var spots = BuildSpots();
            string[] characters = { "Danner", "Cesar", "Sebastian", "Ines" };

            for (int c = 0; c < characters.Length; c++)
            {
                var prefab = Resources.Load<GameObject>("Characters/Player_" + characters[c]);
                Check(prefab != null, characters[c] + ": prefab disponible");
                if (prefab == null) continue;

                var go = Object.Instantiate(prefab, new Vector3(0f, -50f, 0f), Quaternion.identity);
                var pc = go.GetComponent<PlayerController2D>();
                var rb = go.GetComponent<Rigidbody2D>();
                var health = go.GetComponent<Health>();

                if (pc == null)
                {
                    Check(false, characters[c] + ": PlayerController2D presente");
                    Object.Destroy(go);
                    continue;
                }

                pc.Setup(0, asset != null ? asset : pc.inputAsset, true);
                if (health != null) health.invulnerable = true;

                for (int s = 0; s < spots.Count; s++)
                {
                    var sp = spots[s];
                    go.transform.position = sp.position;
                    if (rb != null) rb.linearVelocity = Vector2.zero;
                    Physics2D.SyncTransforms();
                    yield return new WaitForSeconds(sp.settle);

                    bool grounded = pc.IsGrounded;
                    Check(grounded == sp.expectGrounded,
                        characters[c] + ": " + sp.label + (sp.expectGrounded ? " -> apoyado" : " -> sin suelo"),
                        "IsGrounded=" + grounded + " pos=" + go.transform.position.ToString("0.00"));

                    if (sp.testJump)
                    {
                        float yBefore = go.transform.position.y;
                        float peak = yBefore;
                        float maxVy = 0f;

                        Press(Key.K);
                        float tEnd = Time.time + 0.35f;
                        while (Time.time < tEnd)
                        {
                            Press(Key.K);
                            maxVy = Mathf.Max(maxVy, rb.linearVelocity.y);
                            if (go.transform.position.y > peak) peak = go.transform.position.y;
                            yield return null;
                        }
                        ReleaseAll();

                        for (int i = 0; i < 15; i++)
                        {
                            yield return new WaitForSeconds(0.03f);
                            if (go.transform.position.y > peak) peak = go.transform.position.y;
                        }

                        Check(peak > yBefore + 0.8f, characters[c] + ": salta desde el borde",
                            "y " + yBefore.ToString("0.00") + " -> pico " + peak.ToString("0.00") +
                            " maxVy=" + maxVy.ToString("0.00"));

                        if (peak <= yBefore + 0.8f)
                            Debug.Log("[G] detalle salto " + characters[c] +
                                " | grounded=" + pc.IsGrounded + " crouch=" + pc.IsCrouching +
                                " muerto=" + pc.IsDead + " timeScale=" + Time.timeScale +
                                " activo=" + go.activeInHierarchy + " simulando=" + rb.simulated +
                                " pos=" + go.transform.position.ToString("0.000"));
                    }
                }

                Object.Destroy(go);
                yield return new WaitForSeconds(0.2f);
            }

            int total = passed + failed;
            if (failed == 0) Debug.Log("[G] ===== VEREDICTO SUELO: OK " + passed + "/" + total + " comprobaciones =====");
            else Debug.LogError("[G] ===== VEREDICTO SUELO: FALLOS " + failed + " de " + total + " =====");
        }
    }
}
#endif
