#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OperacionCuatro.EditorTools
{
    /// <summary>
    /// Fase 3 — comprobaciones de Level03 «LABORATORIO SUBTERRÁNEO».
    ///
    /// Arranca una sesión real de un jugador apuntando al nivel 3 y verifica progresión de
    /// campaña, estructura, los tres enemigos nuevos (centinela, dron láser, granadero con
    /// granada parabólica), puertas de seguridad con interruptores, unidades de energía,
    /// ascensores, trampas láser, arma láser de munición limitada, alarma, cuenta atrás visible
    /// y cierre de misión por la zona segura.
    ///
    /// Se lanza en Play Mode desde el menú OC o por reflexión sobre Run.
    /// </summary>
    public static class OC_Level03Test
    {
        [UnityEditor.MenuItem("OC/Playtest/Level03 (laboratorio subterráneo)")]
        public static void Run()
        {
            var go = new GameObject("[OC_Level03Test]");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<OCLevel03TestRunner>();
        }
    }

    public class OCLevel03TestRunner : MonoBehaviour
    {
        private int passed;
        private int failed;

        private LevelManager lm;
        private LevelHUD hud;
        private PlayerController2D player;
        private Health playerHealth;
        private PlayerShooter shooter;

        private readonly List<EnemyBase> parked = new List<EnemyBase>();

        // ==================================================================
        //  Utilidades
        // ==================================================================
        private void Check(bool ok, string what, string detail = "")
        {
            if (ok) { passed++; Debug.Log("[L3] PASS  " + what + (detail.Length > 0 ? " | " + detail : "")); }
            else { failed++; Debug.LogError("[L3] FAIL  " + what + (detail.Length > 0 ? " | " + detail : "")); }
        }

        private void Note(string text) { Debug.Log("[L3] ....  " + text); }

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

        /// <summary>
        /// Superficie de suelo MÁS BAJA de la columna. El raycast simple devuelve el primer impacto
        /// desde arriba, que puede ser una caja, una pasarela o una plataforma elevada; para comprobar
        /// que el suelo es continuo hay que quedarse con la cota inferior.
        /// </summary>
        private static bool LowestGroundAt(float x, out float surfaceY)
        {
            var hits = Physics2D.RaycastAll(new Vector2(x, 40f), Vector2.down, 60f, 1 << 8);
            surfaceY = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
                if (hits[i].collider != null) surfaceY = Mathf.Min(surfaceY, hits[i].point.y);

            bool found = surfaceY < float.MaxValue;
            if (!found) surfaceY = -999f;
            return found;
        }

        private static void StandAt(PlayerController2D target, float x, float lift)
        {
            float surfaceY;
            if (!GroundSurfaceAt(x, out surfaceY)) surfaceY = 0f;
            Teleport(target, x, surfaceY + lift);
        }

        private static void Hurt(Health health, int amount)
        {
            if (health == null) return;

            health.TakeDamage(new DamageInfo
            {
                amount = amount,
                point = health.transform.position,
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

        private static string WeaponLabel(LevelHUD levelHud)
        {
            if (levelHud == null || levelHud.panelP1 == null) return "";
            var text = levelHud.panelP1.weaponText;
            return text != null ? text.text : "";
        }

        private int CountPlayerBullets(int damage)
        {
            var bullets = All<Projectile>();
            int count = 0;
            for (int i = 0; i < bullets.Length; i++)
                if (bullets[i] != null && bullets[i].faction == Faction.Player && bullets[i].damage == damage) count++;

            return count;
        }

        private int CountEnemyBullets()
        {
            var bullets = All<Projectile>();
            int count = 0;
            for (int i = 0; i < bullets.Length; i++)
                if (bullets[i] != null && bullets[i].faction == Faction.Enemy) count++;

            return count;
        }

        // ==================================================================
        //  Secuencia
        // ==================================================================
        private IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(0.8f);

            // ---------------- A. PROGRESIÓN ----------------
            GameSession.StartNewRun(1);
            GameSession.StartLevel(3);
            GameSession.P1 = CharacterId.Danner;

            Check(GameSession.CurrentLevel == 3
                  && GameSession.GetLevelSceneName() == SceneNames.Level03
                  && GameSession.GetLevelDisplayName() == "LABORATORIO SUBTERRÁNEO",
                "1. El nivel 3 se resuelve como Level03 «LABORATORIO SUBTERRÁNEO»",
                "nivel=" + GameSession.CurrentLevel + " escena=" + GameSession.GetLevelSceneName()
                + " nombre=" + GameSession.GetLevelDisplayName());

            Check(GameSession.LastLevel == 3 && !GameSession.HasNextLevel,
                "2. No existe Level04: la última misión es la 3 y REINTENTAR recarga Level03",
                "último=" + GameSession.LastLevel + " siguiente=" + GameSession.HasNextLevel);

            SceneManager.LoadScene(SceneNames.Level03);
            yield return null;
            yield return new WaitForSecondsRealtime(1.9f);

            lm = LevelManager.Instance;
            hud = lm != null ? lm.hud : null;
            player = lm != null && lm.Players.Count > 0 ? lm.Players[0] : null;

            Check(SceneManager.GetActiveScene().name == "Level03" && player != null,
                "3. La escena activa es Level03 y el personaje elegido aparece en ella",
                SceneManager.GetActiveScene().name + " | " + (player != null ? player.name : "sin jugadores"));

            string music = GameManager.Instance != null && GameManager.Instance.Audio != null
                ? GameManager.Instance.Audio.CurrentMusic
                : "";

            Check(music == "mus_level03",
                "4. Level03 reproduce su propia pista musical (mus_level03)",
                "música actual=" + music);

            var scenes = UnityEditor.EditorBuildSettings.scenes;
            int index02 = -1;
            int index03 = -1;
            int indexResults = -1;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path.EndsWith("Level02.unity")) index02 = i;
                if (scenes[i].path.EndsWith("Level03.unity")) index03 = i;
                if (scenes[i].path.EndsWith("Results.unity")) indexResults = i;
            }

            Check(index03 > index02 && index02 >= 0 && indexResults > index03 && scenes[index03].enabled,
                "5. Level03 está registrada en Build Settings entre Level02 y Results",
                "Level02=" + index02 + " Level03=" + index03 + " Results=" + indexResults);

            if (player == null) { Finish(); yield break; }

            playerHealth = player.health != null ? player.health : player.GetComponent<Health>();
            shooter = player.shooter != null ? player.shooter : player.GetComponent<PlayerShooter>();
            player.SetInputEnabled(false);

            // ---------------- B. ESTRUCTURA ----------------
            float y20, y120, y240;
            bool g20 = LowestGroundAt(20f, out y20);
            bool g120 = LowestGroundAt(120f, out y120);
            bool g240 = LowestGroundAt(240f, out y240);

            Check(g20 && g120 && g240 && Mathf.Abs(y20) <= 0.35f && Mathf.Abs(y120) <= 0.35f
                  && Mathf.Abs(y240) <= 0.35f,
                "6. El suelo del laboratorio es continuo a lo largo de las cinco secciones",
                "y(20)=" + y20.ToString("0.00") + " y(120)=" + y120.ToString("0.00")
                + " y(240)=" + y240.ToString("0.00"));

            var sections = All<SectionTrigger>();
            string[] expectedSections = { "ACCESO SUBTERRÁNEO", "LABORATORIOS", "AUTOMATIZACIÓN", "ENERGÍA", "EVACUACIÓN" };
            int sectionsFound = 0;
            for (int i = 0; i < expectedSections.Length; i++)
            {
                for (int j = 0; j < sections.Length; j++)
                {
                    if (sections[j].sectionName.Contains(expectedSections[i])) { sectionsFound++; break; }
                }
            }

            Check(sectionsFound == 5 && sections.Length >= 5,
                "7. Las cinco secciones se anuncian por HUD al entrar",
                sectionsFound + "/5 secciones, " + sections.Length + " triggers");

            var checkpoints = All<Checkpoint>();
            int checkpointsOk = 0;
            for (int i = 0; i < checkpoints.Length; i++)
                if (checkpoints[i].respawnPoint != null) checkpointsOk++;

            Check(checkpoints.Length >= 5 && checkpointsOk == checkpoints.Length,
                "8. Cinco checkpoints con punto de reaparición asignado",
                checkpoints.Length + " checkpoints, " + checkpointsOk + " con respawn");

            var layers = All<ParallaxLayer>();
            int layersWithCamera = 0;
            float deepestFactor = 0f;
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].cameraTransform != null) layersWithCamera++;
                deepestFactor = Mathf.Max(deepestFactor, layers[i].parallaxFactor);
            }

            Check(layers.Length >= 4 && layersWithCamera == layers.Length && deepestFactor > 0.3f,
                "9. Parallax multicapa con todas las capas enlazadas a la cámara",
                layers.Length + " capas, factorMáx=" + deepestFactor.ToString("0.00"));

            var pauseUi = Object.FindObjectsByType<PauseMenuUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var escapeUi = Object.FindObjectsByType<EscapeCountdownUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var powerUi = Object.FindObjectsByType<PowerStatusUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            bool escapeUiWired = escapeUi.Length > 0 && escapeUi[0].root != null && escapeUi[0].label != null;
            bool powerUiWired = powerUi.Length > 0 && powerUi[0].label != null;

            Check(hud != null && hud.panelP1 != null && hud.panelP2 != null && hud.scoreText != null
                  && hud.gameOverPanel != null && hud.missionCompletePanel != null
                  && pauseUi.Length > 0 && escapeUiWired && powerUiWired,
                "10. HUD completo: paneles de jugador, puntos, pausa, game over, misión, energía y cuenta atrás",
                "hud=" + (hud != null) + " pausa=" + (pauseUi.Length > 0)
                + " cuentaAtrás=" + escapeUiWired + " energía=" + powerUiWired);

            // ---------------- C. ENEMIGOS NUEVOS ----------------
            int sentinelCount = All<SentinelRobot>().Length;
            int droneCount = All<LaserDrone>().Length;
            int grenadierCount = All<GrenadierEnemy>().Length;
            int legacyCount = All<ShooterEnemy>().Length + All<PatrolEnemy>().Length;

            Check(sentinelCount >= 5 && droneCount >= 5 && grenadierCount >= 5 && legacyCount >= 3,
                "11. Conviven los tres enemigos de la Fase 3 con los enemigos de las fases anteriores",
                "centinelas=" + sentinelCount + " drones=" + droneCount + " granaderos=" + grenadierCount
                + " fases previas=" + legacyCount);

            if (playerHealth != null) playerHealth.invulnerable = true;

            // ---- Centinela ----
            var sentinel = Nearest<SentinelRobot>(28f);
            bool sentinelStats = sentinel != null
                                 && sentinel.health != null && sentinel.health.maxHealth == 75
                                 && sentinel.scoreValue == 350 && sentinel.contactDamage == 16
                                 && sentinel.burstSize == 3 && sentinel.bulletPrefab != null;

            ParkEnemies();
            if (sentinel != null) sentinel.enabled = true;

            int sentinelBullets = 0;
            float sentinelRangeStart = -1f;
            float sentinelRangeEnd = -1f;

            if (sentinel != null)
            {
                // Fuera de stopRange (7): colocado a 6 unidades el centinela ya estaba parado y
                // disparando, así que nunca se veía avanzar. Desde 9,5 debe acercarse hasta 7.
                StandAt(player, sentinel.transform.position.x + 9.5f, 0.12f);
                sentinelRangeStart = Mathf.Abs(sentinel.transform.position.x - player.transform.position.x);
                yield return new WaitForSecondsRealtime(0.3f);

                float scanEnd = Time.time + 4.4f;
                while (Time.time < scanEnd)
                {
                    sentinelBullets = Mathf.Max(sentinelBullets, CountEnemyBullets());
                    yield return null;
                }

                sentinelRangeEnd = Mathf.Abs(sentinel.transform.position.x - player.transform.position.x);
            }

            Check(sentinelStats && sentinelBullets > 0 && sentinelRangeStart >= 0f && sentinelRangeEnd < sentinelRangeStart - 0.4f,
                "12. Robot centinela: 75 PV, 350 puntos, ráfaga de 3 y avance hacia el jugador mientras dispara",
                sentinel != null
                    ? "pv=" + sentinel.health.maxHealth + " puntos=" + sentinel.scoreValue
                      + " contacto=" + sentinel.contactDamage + " ráfaga=" + sentinel.burstSize
                      + " balas=" + sentinelBullets
                      + " distancia=" + sentinelRangeStart.ToString("0.00") + "->" + sentinelRangeEnd.ToString("0.00")
                    : "sin centinela");

            // ---- Dron láser ----
            var drone = Nearest<LaserDrone>(142f);
            bool droneOk = false;
            string droneDetail = "sin dron";

            ParkEnemies();
            if (drone != null) drone.enabled = true;

            if (drone != null)
            {
                var droneBody = drone.GetComponent<Rigidbody2D>();
                StandAt(player, drone.transform.position.x, 0.12f);
                yield return new WaitForSecondsRealtime(0.2f);

                bool sawCharge = false;
                bool chargeVisible = false;
                bool fired = false;
                int droneBullets = 0;

                float droneEnd = Time.time + 5.5f;
                while (Time.time < droneEnd)
                {
                    if (drone.IsCharging)
                    {
                        sawCharge = true;
                        if (drone.chargeVisual != null && drone.chargeVisual.activeSelf) chargeVisible = true;
                    }

                    droneBullets = Mathf.Max(droneBullets, CountEnemyBullets());
                    if (droneBullets > 0) fired = true;
                    if (sawCharge && chargeVisible && fired) break;

                    yield return null;
                }

                droneOk = drone.maxHealth == 45 && drone.scoreValue == 300 && drone.contactDamage == 12
                          && droneBody != null && droneBody.gravityScale == 0f
                          && sawCharge && chargeVisible && fired;

                droneDetail = "pv=" + drone.maxHealth + " gravedad=" + (droneBody != null ? droneBody.gravityScale : -1f)
                              + " aviso=" + sawCharge + " avisoVisible=" + chargeVisible
                              + " balas=" + droneBullets;
            }

            Check(droneOk,
                "13. Dron láser: 45 PV, vuela sin gravedad y avisa con una luz de carga antes de disparar",
                droneDetail);

            // ---- Granadero ----
            var grenadier = Nearest<GrenadierEnemy>(132f);
            bool grenadeOk = false;
            string grenadeDetail = "sin granadero";

            ParkEnemies();
            if (grenadier != null) grenadier.enabled = true;

            if (grenadier != null)
            {
                float grenadierX = grenadier.transform.position.x;
                StandAt(player, grenadierX + 8f, 0.12f);
                yield return new WaitForSecondsRealtime(0.3f);

                int thrownBefore = grenadier.ThrownCount;
                float startDistance = Mathf.Abs(grenadier.transform.position.x - player.transform.position.x);

                Bomb spawned = null;
                bool sawWarning = false;
                float minY = float.MaxValue;
                float maxY = float.MinValue;
                float distanceAfterThrow = startDistance;

                float throwEnd = Time.time + 3.2f;
                while (Time.time < throwEnd)
                {
                    var bombs = All<Bomb>();
                    for (int i = 0; i < bombs.Length; i++)
                    {
                        if (bombs[i] == null || bombs[i].faction != Faction.Enemy) continue;
                        if (spawned == null) spawned = bombs[i];
                        if (bombs[i] == spawned)
                        {
                            minY = Mathf.Min(minY, bombs[i].transform.position.y);
                            maxY = Mathf.Max(maxY, bombs[i].transform.position.y);

                            var warning = bombs[i].GetComponent<GrenadeWarning>();
                            if (warning != null && warning.body != null
                                && Vector4.Distance(warning.body.color, warning.warnColor) < 0.6f) sawWarning = true;
                        }
                    }

                    if (grenadier.ThrownCount > thrownBefore && spawned != null && sawWarning) break;
                    yield return null;
                }

                // El retroceso a cubierto dura coverTime (0,9 s): hay que dejar correr el movimiento
                // antes de medir, no muestrear en el fotograma del lanzamiento.
                yield return new WaitForSecondsRealtime(0.5f);
                distanceAfterThrow = Mathf.Abs(grenadier.transform.position.x - player.transform.position.x);

                float rise = spawned != null ? maxY - minY : 0f;

                grenadeOk = grenadier.maxHealth == 40 && grenadier.scoreValue == 250 && grenadier.contactDamage == 12
                           && grenadier.ThrownCount > thrownBefore
                           && spawned != null && spawned.faction == Faction.Enemy
                           && sawWarning && rise > 0.8f
                           && distanceAfterThrow > startDistance + 0.3f;

                grenadeDetail = "pv=" + grenadier.maxHealth + " lanzamientos=" + grenadier.ThrownCount
                                + " granada=" + (spawned != null) + " aviso=" + sawWarning
                                + " altura=" + rise.ToString("0.00")
                                + " distancia=" + startDistance.ToString("0.00") + "->" + distanceAfterThrow.ToString("0.00");
            }

            Check(grenadeOk,
                "14. Soldado explosivo: 40 PV, lanza una granada parabólica con aviso previo y retrocede a cubierto",
                grenadeDetail);

            ParkEnemies();

            // ---------------- D. PUERTAS E INTERRUPTORES ----------------
            var doorA = Nearest<SecurityDoor>(49f);
            bool doorAClosed = doorA != null && !doorA.IsOpen && doorA.blocker != null && doorA.blocker.enabled
                               && Physics2D.OverlapPoint((Vector2)doorA.transform.position + new Vector2(0f, 1.5f), ~0) != null;

            int scoreBeforeSwitches = GameSession.Score;
            int switchHits = 0;

            var switches = DoorSwitch.All;
            for (int i = 0; i < switches.Count; i++)
            {
                var sw = switches[i];
                if (sw == null || sw.channel != "A") continue;

                Hurt(sw.health, 10);
                yield return null;
                Hurt(sw.health, 10);
                yield return null;
                if (sw.IsActive) switchHits++;
            }

            yield return null;
            yield return null;

            bool doorAOpen = doorA != null && doorA.IsOpen && doorA.blocker != null && !doorA.blocker.enabled
                             && Physics2D.OverlapPoint((Vector2)doorA.transform.position + new Vector2(0f, 1.5f), ~0) == null
                             && doorA.openVisual != null && doorA.openVisual.activeSelf
                             && doorA.lockedVisual != null && !doorA.lockedVisual.activeSelf;

            int switchScore = GameSession.Score - scoreBeforeSwitches;

            Check(doorAClosed && switchHits == 2 && doorAOpen && switchScore >= 100,
                "15. Los dos interruptores del canal A se fijan disparándoles y abren la puerta de los laboratorios",
                "cerradaBloquea=" + doorAClosed + " interruptores=" + switchHits
                + " abierta=" + doorAOpen + " puntos=" + switchScore);

            // ---------------- E. UNIDADES DE ENERGÍA Y PUERTA BLINDADA ----------------
            var units = All<PowerUnit>();
            int unitsFull = 0;
            for (int i = 0; i < units.Length; i++)
                if (units[i].health != null && units[i].health.maxHealth == 120) unitsFull++;

            var doorP = Nearest<SecurityDoor>(204f);

            Check(units.Length == 3 && unitsFull == 3,
                "16. Tres unidades de alimentación de 120 PV controlan el canal de energía",
                units.Length + " unidades, " + unitsFull + " con 120 PV");

            bool doorPLocked = doorP != null && !doorP.IsOpen && doorP.Progress == 0;

            Hurt(units[0].health, 9999);
            Hurt(units[1].health, 9999);
            yield return null;
            yield return new WaitForSecondsRealtime(0.3f);

            bool twoDownVisual = units[0].IsDestroyed && units[1].IsDestroyed
                                 && units[0].offlineVisual != null && units[0].offlineVisual.activeSelf
                                 && units[0].onlineVisual != null && !units[0].onlineVisual.activeSelf;

            bool doorPStillLocked = doorP != null && !doorP.IsOpen && doorP.Progress == 2;
            bool escapeNotBegun = EscapeSequence.Instance != null && !EscapeSequence.Instance.Begun;

            Check(doorPLocked && twoDownVisual && doorPStillLocked && escapeNotBegun,
                "17. Con dos unidades destruidas la puerta blindada sigue cerrada y la alarma no ha empezado",
                "bloqueadaAlInicio=" + doorPLocked + " dosCaídas=" + twoDownVisual
                + " progreso=" + (doorP != null ? doorP.Progress : -1)
                + " alarma=" + !escapeNotBegun);

            // ---------------- F. ASCENSOR ----------------
            var lift = Nearest<ElevatorPlatform>(100f);
            bool liftOk = false;
            string liftDetail = "sin ascensor";

            if (lift != null)
            {
                lift.SnapTo(0f);
                yield return null;

                float liftStartY = lift.transform.position.y;
                Teleport(player, lift.transform.position.x, liftStartY + 0.8f);
                yield return new WaitForSecondsRealtime(0.6f);

                int riders = lift.RiderCount;
                float playerStartY = player.transform.position.y;
                float topReached = liftStartY;
                float playerTop = playerStartY;

                float liftEnd = Time.time + 6f;
                while (Time.time < liftEnd)
                {
                    topReached = Mathf.Max(topReached, lift.transform.position.y);
                    playerTop = Mathf.Max(playerTop, player.transform.position.y);
                    if (lift.Height01 > 0.98f && Time.time > liftEnd - 4f) break;
                    yield return null;
                }

                float liftRise = topReached - liftStartY;
                float playerRise = playerTop - playerStartY;

                liftOk = riders >= 1 && liftRise > 5f && playerRise > 4.5f
                         && Mathf.Abs(liftRise - playerRise) < 1.4f;

                liftDetail = "pasajeros=" + riders + " recorrido=" + liftRise.ToString("0.00")
                             + " jugador=" + playerRise.ToString("0.00")
                             + " altura=" + lift.Height01.ToString("0.00");

                Teleport(player, 130f, 1.2f);
            }

            Check(liftOk,
                "18. El ascensor sube al jugador desde el suelo hasta la pasarela del centro de automatización",
                liftDetail);

            // ---------------- G. TRAMPA LÁSER ----------------
            var trap = Nearest<LaserTrap>(158f);
            bool trapOk = false;
            string trapDetail = "sin trampa";

            if (trap != null)
            {
                StandAt(player, trap.transform.position.x, 0.12f);
                if (playerHealth != null) { playerHealth.ResetHealth(); playerHealth.invulnerable = false; }
                yield return null;

                bool sawOff = false;
                bool sawWarning = false;
                bool sawActive = false;
                bool hurtWhileActive = false;
                bool hurtWhileWarning = false;
                int hp = playerHealth != null ? playerHealth.Current : 0;

                float trapEnd = Time.time + 7f;
                while (Time.time < trapEnd)
                {
                    int now = playerHealth != null ? playerHealth.Current : hp;

                    if (trap.Current == LaserTrap.Phase.Off) sawOff = true;
                    if (trap.Current == LaserTrap.Phase.Warning) { sawWarning = true; if (now < hp) hurtWhileWarning = true; }
                    if (trap.Current == LaserTrap.Phase.Active) { sawActive = true; if (now < hp) hurtWhileActive = true; }

                    hp = now;
                    if (sawOff && sawWarning && sawActive && hurtWhileActive && hurtWhileWarning) break;
                    yield return null;
                }

                trapOk = sawOff && sawWarning && sawActive && hurtWhileActive && !hurtWhileWarning
                         && trap.damage == 18;

                trapDetail = "apagada=" + sawOff + " aviso=" + sawWarning + " activa=" + sawActive
                             + " dañoEnActiva=" + hurtWhileActive + " dañoEnAviso=" + hurtWhileWarning
                             + " daño=" + trap.damage;

                if (playerHealth != null) { playerHealth.invulnerable = true; }
            }

            Check(trapOk,
                "19. Trampa láser: ciclo apagada → aviso → activa, y solo el haz activo hace daño (18)",
                trapDetail);

            // ---------------- H. ARMA LÁSER ----------------
            var pickup = Nearest<LaserWeaponPickup>(96f);
            bool pickupOk = false;
            string pickupDetail = "sin recogible";

            if (pickup != null && shooter != null)
            {
                // El recogible SUMA munición a la que ya se lleve (un jugador puede haber cogido
                // otro antes, p. ej. al pasar por la trampa láser de la sección de energía).
                int ammoBefore = shooter.LaserAmmo;
                int givenAmmo = pickup.ammo;
                int expectedAmmo = ammoBefore + givenAmmo;

                Teleport(player, pickup.transform.position.x, pickup.transform.position.y + 0.1f);
                yield return new WaitForSecondsRealtime(0.45f);

                string weaponLabel = WeaponLabel(hud);
                pickupOk = shooter.LaserAmmo == expectedAmmo && pickup == null && weaponLabel.Contains("LÁSER");
                pickupDetail = "munición=" + ammoBefore + "+" + givenAmmo + "=" + shooter.LaserAmmo
                               + " esperada=" + expectedAmmo
                               + " recogible_eliminado=" + (pickup == null) + " hud=\"" + weaponLabel + "\"";
            }

            Check(pickupOk,
                "20. El recogible de arma láser entrega munición limitada y el HUD muestra LÁSER",
                pickupDetail);

            bool laserOk = false;
            string laserDetail = "sin arma láser";
            if (shooter != null)
            {
                shooter.ApplyLaserWeapon(2);
                yield return null;

                int ammoStart = shooter.LaserAmmo;
                bool fired = shooter.TryFire(Vector2.right);
                yield return null;
                yield return null;

                int bolts = CountPlayerBullets(shooter.laserDamage);
                int ammoAfter = shooter.LaserAmmo;

                laserOk = fired && bolts > 0 && ammoAfter == ammoStart - 1
                          && shooter.laserDamage == 26;
                laserDetail = "disparo=" + fired + " proyectilesLáser=" + bolts
                              + " munición=" + ammoStart + "->" + ammoAfter
                              + " daño=" + shooter.laserDamage;
            }

            Check(laserOk,
                "21. El arma láser dispara su propio proyectil de 26 de daño y consume munición",
                laserDetail);

            bool fallbackOk = false;
            string fallbackDetail = "sin arma láser";
            if (shooter != null)
            {
                shooter.ApplyLaserWeapon(1);
                // El disparo de la comprobación 21 deja el arma en recarga (laserFireRate 0,2 s): sin
                // esperar al cooldown, el último cartucho no se gastaba aquí y TryFire devolvía false,
                // así que la munición seguía en 1 y el arma básica era en realidad el láser.
                yield return new WaitForSecondsRealtime(0.32f);

                bool spentLast = shooter.TryFire(Vector2.right);
                yield return new WaitForSecondsRealtime(0.25f);

                bool depleted = shooter.LaserAmmo == 0 && !shooter.HasLaser;
                bool basicFires = shooter.TryFire(Vector2.right);
                yield return null;
                yield return null;

                string weaponLabel = WeaponLabel(hud);
                bool labelIsBasic = weaponLabel.Contains("SICA") || weaponLabel.Contains("BASICA");

                fallbackOk = spentLast && depleted && basicFires && labelIsBasic;
                fallbackDetail = "últimoCartucho=" + spentLast + " munición_final=" + shooter.LaserAmmo
                                 + " agotada=" + depleted
                                 + " arma_básica_dispara=" + basicFires + " etiquetaBásica=" + labelIsBasic
                                 + " hud=\"" + weaponLabel + "\"";
            }

            Check(fallbackOk,
                "22. Al agotarse la munición láser el jugador vuelve al arma básica sin perder el sistema de armas",
                fallbackDetail);

            // ---------------- I. SALIDA ACCESIBLE ----------------
            var exit = Object.FindFirstObjectByType<SafeExitZone>();
            float exitGround;
            bool exitGroundOk = GroundSurfaceAt(exit != null ? exit.transform.position.x : 248f, out exitGround);

            var doors = All<SecurityDoor>();
            float lastDoorX = -999f;
            for (int i = 0; i < doors.Length; i++)
                if (doors[i] != null) lastDoorX = Mathf.Max(lastDoorX, doors[i].transform.position.x);

            Check(exit != null && exitGroundOk && exitGround > 1f && lastDoorX < exit.transform.position.x - 20f,
                "23. La zona segura está al final de la evacuación, sobre suelo firme y sin puertas que la bloqueen",
                "salidaX=" + (exit != null ? exit.transform.position.x.ToString("0.00") : "nulo")
                + " suelo=" + exitGround.ToString("0.00")
                + " últimaPuertaX=" + lastDoorX.ToString("0.00"));

            Teleport(player, exit != null ? exit.transform.position.x : 248f,
                exit != null ? exit.transform.position.y : 2.5f);
            yield return new WaitForSecondsRealtime(0.45f);

            bool noEarlyComplete = !GameSession.MissionComplete && lm != null && !lm.MissionComplete
                                   && exit != null && exit.lockedVisual != null && exit.lockedVisual.activeSelf;

            Check(noEarlyComplete,
                "24. Antes de la alarma la zona segura está cerrada y no completa la misión",
                "misión=" + (lm != null && lm.MissionComplete)
                + " salidaCerrada=" + (exit != null && exit.lockedVisual != null && exit.lockedVisual.activeSelf));

            Teleport(player, 210f, 0.2f);
            yield return new WaitForSecondsRealtime(0.2f);

            // ---------------- J. ALARMA Y CUENTA ATRÁS ----------------
            Hurt(units[2].health, 9999);
            yield return new WaitForSecondsRealtime(0.6f);

            var escape = EscapeSequence.Instance;
            bool alarmStarted = escape != null && escape.Begun && escape.alarmVisual != null && escape.alarmVisual.activeSelf;
            bool doorPOpen = doorP != null && doorP.IsOpen;

            float remainingStart = escape != null ? escape.Remaining : -1f;
            yield return new WaitForSecondsRealtime(1.6f);
            float remainingNow = escape != null ? escape.Remaining : -1f;

            var escapeHud = Object.FindFirstObjectByType<EscapeCountdownUI>(FindObjectsInactive.Include);
            bool timerVisible = escapeHud != null && escapeHud.root != null && escapeHud.root.activeSelf
                                && escapeHud.label != null && escapeHud.label.text.Contains("EVACUACIÓN");
            string timerText = escapeHud != null && escapeHud.label != null ? escapeHud.label.text : "";

            Check(alarmStarted && doorPOpen && timerVisible && remainingNow < remainingStart - 1f,
                "25. Al caer la tercera unidad se abren las puertas, suena la alarma y la cuenta atrás avanza en pantalla",
                "alarma=" + alarmStarted + " puertaEnergía=" + doorPOpen
                + " temporizador=" + timerVisible + " texto=\"" + timerText + "\""
                + " restante=" + remainingStart.ToString("0.0") + "->" + remainingNow.ToString("0.0"));

            // ---------------- K. CIERRE DE MISIÓN ----------------
            if (playerHealth != null) playerHealth.invulnerable = true;

            Teleport(player, 246f, 2.4f);
            yield return null;
            Teleport(player, exit != null ? exit.transform.position.x : 248f,
                exit != null ? exit.transform.position.y : 2.5f);

            bool completed = false;
            bool sawResults = false;

            float deadline = Time.realtimeSinceStartup + 14f;
            while (Time.realtimeSinceStartup < deadline)
            {
                if (lm != null && lm.MissionComplete) completed = true;
                if (GameSession.MissionComplete) completed = true;

                if (SceneManager.GetActiveScene().name == SceneNames.Results) { sawResults = true; break; }
                yield return null;
            }

            Check(completed && sawResults,
                "26. Alcanzar la zona segura durante la evacuación completa la misión y abre Results",
                "misión=" + completed + " Results=" + sawResults
                + " puntuación=" + GameSession.Score
                + " escena=" + SceneManager.GetActiveScene().name);

            Finish();
        }

        private void Finish()
        {
            int total = passed + failed;
            if (failed == 0) Debug.Log("[L3] ===== VEREDICTO: OK " + passed + "/" + total + " comprobaciones de Level03 =====");
            else Debug.LogError("[L3] ===== VEREDICTO: FALLOS " + failed + " de " + total + " =====");
        }
    }
}
#endif
