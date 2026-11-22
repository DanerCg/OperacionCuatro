#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OperacionCuatro.EditorTools
{
    /// <summary>
    /// Fase 2 — 24 comprobaciones de Level02 «COMPLEJO DE GUERRA».
    ///
    /// Se lanza en Play Mode (menú OC o por reflexión sobre Run). La suite arranca su propia
    /// sesión de un jugador apuntando al nivel 2, y comprueba estructura, enemigos nuevos,
    /// peligros, arma pesada, generador y cierre de misión.
    /// </summary>
    public static class OC_Level02Test
    {
        [UnityEditor.MenuItem("OC/Playtest/Level02 (24 comprobaciones)")]
        public static void Run()
        {
            var go = new GameObject("[OC_Level02Test]");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<OCLevel02TestRunner>();
        }
    }

    public class OCLevel02TestRunner : MonoBehaviour
    {
        private int passed;
        private int failed;

        private LevelManager lm;
        private LevelHUD hud;
        private PlayerController2D player;
        private Health playerHealth;
        private PlayerShooter shooter;
        private GeneratorObjective generator;

        private readonly List<EnemyBase> parked = new List<EnemyBase>();

        // ==================================================================
        //  Utilidades
        // ==================================================================
        private void Check(bool ok, string what, string detail = "")
        {
            if (ok) { passed++; Debug.Log("[L] PASS  " + what + (detail.Length > 0 ? " | " + detail : "")); }
            else { failed++; Debug.LogError("[L] FAIL  " + what + (detail.Length > 0 ? " | " + detail : "")); }
        }

        private void Note(string text) { Debug.Log("[L] ....  " + text); }

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

        /// <summary>Deja al jugador sobre la superficie sólida que haya en esa x.</summary>
        private static void StandAt(PlayerController2D target, float x, float lift)
        {
            float surfaceY;
            if (!GroundSurfaceAt(x, out surfaceY)) surfaceY = 0f;
            Teleport(target, x, surfaceY + lift);
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

        // ==================================================================
        //  Secuencia
        // ==================================================================
        private IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(0.8f);

            // ---------------- A. PROGRESIÓN ----------------
            GameSession.StartNewRun(1);
            GameSession.StartLevel(2);
            GameSession.P1 = CharacterId.Danner;

            Check(GameSession.CurrentLevel == 2
                  && GameSession.GetLevelSceneName() == SceneNames.Level02
                  && GameSession.GetLevelDisplayName() == "COMPLEJO DE GUERRA",
                "1. El nivel 2 se resuelve como Level02 «COMPLEJO DE GUERRA»",
                "nivel=" + GameSession.CurrentLevel + " escena=" + GameSession.GetLevelSceneName());

            SceneManager.LoadScene(SceneNames.Level02);
            yield return null;
            yield return new WaitForSecondsRealtime(1.8f);

            lm = LevelManager.Instance;
            hud = lm != null ? lm.hud : null;
            player = lm != null && lm.Players.Count > 0 ? lm.Players[0] : null;

            Check(SceneManager.GetActiveScene().name == "Level02" && player != null,
                "2. La escena activa es Level02 y el jugador elegido aparece en ella",
                SceneManager.GetActiveScene().name + " | " + (player != null ? player.name : "sin jugadores"));

            var scenes = UnityEditor.EditorBuildSettings.scenes;
            int index01 = -1;
            int index02 = -1;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path.EndsWith("Level01.unity")) index01 = i;
                if (scenes[i].path.EndsWith("Level02.unity")) index02 = i;
            }

            Check(index01 >= 0 && index02 > index01 && scenes[index02].enabled,
                "3. Level02 está registrada en Build Settings después de Level01",
                "Level01=" + index01 + " Level02=" + index02);

            Check(GameSession.LastLevel == 2 && !GameSession.HasNextLevel
                  && GameSession.GetLevelSceneName() == "Level02",
                "4. No existe un nivel posterior y REINTENTAR recargaría Level02, no Level01",
                "último=" + GameSession.LastLevel + " reintentar=" + GameSession.GetLevelSceneName());

            if (player == null) { Finish(); yield break; }

            playerHealth = player.health != null ? player.health : player.GetComponent<Health>();
            shooter = player.shooter != null ? player.shooter : player.GetComponent<PlayerShooter>();
            player.SetInputEnabled(false);

            // ---------------- B. ESTRUCTURA ----------------
            float y24;
            float y68;
            bool ground24 = GroundSurfaceAt(24f, out y24);
            bool ground68 = GroundSurfaceAt(68f, out y68);
            Check(ground24 && ground68 && Mathf.Abs(y24) <= 0.35f && Mathf.Abs(y68) <= 0.35f,
                "5. El suelo del complejo es continuo (superficie en y≈0)",
                "y(24)=" + y24.ToString("0.00") + " y(68)=" + y68.ToString("0.00"));

            var sections = All<SectionTrigger>();
            string[] expectedSections = { "ENTRADA", "PRODUCCIÓN", "SISTEMA DE DEFENSA", "PASILLO DE SEGURIDAD", "GENERADOR CENTRAL" };
            int sectionsFound = 0;
            for (int i = 0; i < expectedSections.Length; i++)
            {
                for (int j = 0; j < sections.Length; j++)
                {
                    if (sections[j].sectionName.Contains(expectedSections[i])) { sectionsFound++; break; }
                }
            }

            Check(sectionsFound == 5 && sections.Length >= 5,
                "6. Las cinco secciones se anuncian por HUD al entrar",
                sectionsFound + "/5 secciones, " + sections.Length + " triggers");

            var checkpoints = All<Checkpoint>();
            int checkpointsOk = 0;
            for (int i = 0; i < checkpoints.Length; i++)
                if (checkpoints[i].respawnPoint != null) checkpointsOk++;

            Check(checkpoints.Length == 3 && checkpointsOk == 3,
                "7. Tres checkpoints con punto de reaparición asignado",
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
                "8. Parallax multicapa con todas las capas enlazadas a la cámara",
                layers.Length + " capas, factorMáx=" + deepestFactor.ToString("0.00"));

            var pauseUi = Object.FindObjectsByType<PauseMenuUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var generatorHud = Object.FindObjectsByType<GeneratorHudUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            // La barra del generador vive inactiva hasta que el jugador se acerca al objetivo,
            // así que se busca incluyendo objetos inactivos.
            bool generatorBarWired = generatorHud.Length > 0 && generatorHud[0].generator != null
                                     && generatorHud[0].label != null && generatorHud[0].fill != null;

            Check(hud != null && hud.panelP1 != null && hud.panelP2 != null && hud.scoreText != null
                  && hud.gameOverPanel != null && hud.missionCompletePanel != null
                  && pauseUi.Length > 0 && generatorBarWired,
                "9. HUD completo: paneles de jugador, puntos, pausa, game over, misión y barra del generador",
                "hud=" + (hud != null) + " pausa=" + (pauseUi.Length > 0) + " barraGenerador=" + generatorBarWired);

            // ---------------- C. ENEMIGOS NUEVOS ----------------
            int armoredCount = All<ArmoredSoldier>().Length;
            int droneCount = All<DroneEnemy>().Length;
            int turretCount = All<TurretEnemy>().Length;
            int legacyCount = All<ShooterEnemy>().Length + All<PatrolEnemy>().Length;

            Check(armoredCount >= 2 && droneCount >= 2 && turretCount >= 2 && legacyCount >= 4,
                "10. Conviven los tres enemigos nuevos con los enemigos de la Fase 1",
                "blindados=" + armoredCount + " drones=" + droneCount + " torretas=" + turretCount + " fase1=" + legacyCount);

            var armored = Nearest<ArmoredSoldier>(52f);
            Check(armored != null && armored.maxHealth == 60 && armored.scoreValue == 250
                  && armored.contactDamage == 14 && armored.health != null && armored.health.maxHealth == 60,
                "11. Soldado blindado configurado con 60 PV, 250 puntos y 14 de daño por contacto",
                armored != null ? "pv=" + armored.health.maxHealth + " puntos=" + armored.scoreValue : "sin blindado");

            int lossLeft = 0;
            int lossRight = 0;
            if (armored != null && armored.health != null)
            {
                armored.health.SetMaxHealth(60, true);
                Vector2 center = armored.transform.position;

                int before = armored.health.Current;
                armored.health.TakeDamage(new DamageInfo
                {
                    amount = 20,
                    point = new Vector2(center.x - 2f, center.y + 0.5f),
                    direction = Vector2.right,
                    source = null,
                    playerIndex = -1
                });
                lossLeft = before - armored.health.Current;

                before = armored.health.Current;
                armored.health.TakeDamage(new DamageInfo
                {
                    amount = 20,
                    point = new Vector2(center.x + 2f, center.y + 0.5f),
                    direction = Vector2.left,
                    source = null,
                    playerIndex = -1
                });
                lossRight = before - armored.health.Current;
            }

            int frontLoss = Mathf.Min(lossLeft, lossRight);
            int backLoss = Mathf.Max(lossLeft, lossRight);
            Check(frontLoss == 5 && backLoss == 20,
                "12. El escudo frontal reduce el daño al 25 % y por la espalda entra el 100 %",
                "escudo=" + frontLoss + " espalda=" + backLoss);

            int scoreBeforeKill = GameSession.Score;
            if (armored != null && armored.health != null)
            {
                armored.health.TakeDamage(new DamageInfo
                {
                    amount = 9999,
                    point = (Vector2)armored.transform.position + new Vector2(2f, 0.5f),
                    direction = Vector2.left,
                    source = null,
                    playerIndex = -1
                });
            }

            yield return null;

            Check(armored != null && armored.IsDead && GameSession.Score == scoreBeforeKill + 250,
                "13. El blindado muere al agotar su salud y suma 250 puntos",
                armored != null ? "muerto=" + armored.IsDead + " puntos=" + GameSession.Score : "sin blindado");

            var drone = Nearest<DroneEnemy>(106f);
            bool droneOk = false;
            string droneDetail = "sin dron";
            if (drone != null)
            {
                var droneBody = drone.GetComponent<Rigidbody2D>();
                float gravity = droneBody != null ? droneBody.gravityScale : -1f;

                float startY = drone.transform.position.y;
                float minY = startY;
                float maxY = startY;
                float minX = drone.transform.position.x;
                float maxX = minX;

                float sampleEnd = Time.time + 2.4f;
                while (Time.time < sampleEnd)
                {
                    Vector3 p = drone.transform.position;
                    minY = Mathf.Min(minY, p.y);
                    maxY = Mathf.Max(maxY, p.y);
                    minX = Mathf.Min(minX, p.x);
                    maxX = Mathf.Max(maxX, p.x);
                    yield return null;
                }

                bool collidesWithGround = !Physics2D.GetIgnoreLayerCollision(drone.gameObject.layer, 8);

                droneOk = gravity == 0f && drone.maxHealth == 35
                          && minY > 3.6f && (maxY - minY) <= 1.3f
                          && (maxX - minX) <= drone.patrolRange * 2f + 2.5f
                          && drone.groundMask != 0 && collidesWithGround;

                droneDetail = "pv=" + drone.maxHealth + " gravedad=" + gravity
                              + " y=" + minY.ToString("0.0") + ".." + maxY.ToString("0.0")
                              + " x=" + minX.ToString("0.0") + ".." + maxX.ToString("0.0")
                              + " máscaraSuelo=" + drone.groundMask.value + " colisionaTerreno=" + collidesWithGround;
            }

            Check(droneOk,
                "14. Dron militar: 35 PV, vuela sin gravedad, se mantiene en su zona y no atraviesa el terreno",
                droneDetail);

            // ---------------- Torreta ----------------
            var turret = Nearest<TurretEnemy>(100f);
            ParkEnemies();
            if (turret != null) turret.enabled = true;

            Teleport(player, 104f, 3.78f);
            if (playerHealth != null) playerHealth.ResetHealth();
            yield return new WaitForSecondsRealtime(1.4f);

            bool turretStats = turret != null && turret.maxHealth == 50 && turret.scoreValue == 300
                               && turret.GetComponent<Rigidbody2D>() == null
                               && turret.burstSize >= 2 && turret.fireCooldown > 0f && turret.barrel != null;

            float barrelAngle = turret != null && turret.barrel != null
                ? Mathf.Abs(Mathf.DeltaAngle(0f, turret.barrel.eulerAngles.z))
                : 0f;

            Check(turretStats && barrelAngle > 3f,
                "15. Torreta: 50 PV, fija (sin Rigidbody2D), cadencia configurable y cañón apuntando al jugador",
                turret != null
                    ? "pv=" + turret.maxHealth + " ráfaga=" + turret.burstSize + " recarga=" + turret.fireCooldown
                      + " cañón=" + barrelAngle.ToString("0.0") + "º"
                    : "sin torreta");

            int maxEnemyBullets = 0;
            float fireEnd = Time.time + 2.6f;
            while (Time.time < fireEnd)
            {
                var bullets = All<Projectile>();
                int enemyBullets = 0;
                for (int i = 0; i < bullets.Length; i++)
                    if (bullets[i].faction == Faction.Enemy) enemyBullets++;

                maxEnemyBullets = Mathf.Max(maxEnemyBullets, enemyBullets);
                yield return null;
            }

            Check(maxEnemyBullets > 0,
                "16. La torreta dispara al jugador cuando entra en su cono de visión",
                "proyectiles enemigos simultáneos=" + maxEnemyBullets);

            // ---------------- D. PELIGROS ----------------
            ParkEnemies();
            yield return null;

            // El vapor de la sección de producción (x=58) está sobre la cinta Conveyor_2, que
            // arrastra al jugador fuera de su trigger de 1,3 de ancho antes de que el chorro se
            // active: no sirve para medir el daño. Se prueba el vapor del pasillo elevado (x=104),
            // que se apoya sobre el catwalk fijo.
            var vent = Nearest<SteamVent>(104f);
            bool ventOk = false;
            string ventDetail = "sin vapor";
            if (vent != null)
            {
                var ventCollider = vent.GetComponent<Collider2D>();

                StandAt(player, vent.transform.position.x, 0.12f);
                if (playerHealth != null) playerHealth.ResetHealth();
                yield return null;

                bool insideTrigger = false;
                int hpStart = playerHealth != null ? playerHealth.Current : 0;
                bool sawWarning = false;
                bool sawActive = false;
                bool tookDamage = false;

                float ventEnd = Time.time + 7f;
                while (Time.time < ventEnd)
                {
                    if (ventCollider != null && ventCollider.bounds.Contains(player.transform.position))
                        insideTrigger = true;
                    if (vent.warningVisual != null && vent.warningVisual.activeSelf) sawWarning = true;
                    if (vent.IsActive) sawActive = true;
                    if (playerHealth != null && playerHealth.Current < hpStart) tookDamage = true;
                    if (insideTrigger && sawWarning && sawActive && tookDamage) break;
                    yield return null;
                }

                ventOk = sawWarning && sawActive && tookDamage && insideTrigger;

                var playerCollider = player.GetComponent<Collider2D>();
                ventDetail = "aviso=" + sawWarning + " chorro=" + sawActive
                             + " dentro=" + insideTrigger
                             + " daño=" + (hpStart - (playerHealth != null ? playerHealth.Current : hpStart))
                             + " vapor=" + vent.name
                             + " boundsJugador=" + (playerCollider != null ? playerCollider.bounds.ToString() : "sin collider")
                             + " boundsVapor=" + (ventCollider != null ? ventCollider.bounds.ToString() : "sin collider")
                             + " posJugador=" + player.transform.position.ToString("0.00");
            }

            Check(ventOk, "17. Vapor industrial: avisa antes del chorro y daña al jugador", ventDetail);

            var zone = Nearest<ElectricZone>(143f);
            bool zoneOk = false;
            string zoneDetail = "sin zona";
            if (zone != null)
            {
                Teleport(player, 143f, 0.15f);
                if (playerHealth != null) playerHealth.ResetHealth();
                yield return null;

                int hpStart = playerHealth != null ? playerHealth.Current : 0;
                bool sawCharged = false;
                bool sawSafe = false;
                bool safeWithoutDamage = false;
                bool tookDamage = false;
                int hpAtSafeStart = hpStart;
                Color chargedColor = zone.onColor;
                Color safeColor = zone.offColor;
                bool colorsDiffer = false;

                float zoneEnd = Time.time + 8f;
                while (Time.time < zoneEnd)
                {
                    if (zone.IsCharged)
                    {
                        sawCharged = true;
                        if (zone.fieldVisual != null) chargedColor = zone.fieldVisual.color;
                    }
                    else
                    {
                        if (!sawSafe) { sawSafe = true; hpAtSafeStart = playerHealth != null ? playerHealth.Current : 0; }
                        if (zone.fieldVisual != null) safeColor = zone.fieldVisual.color;
                        if (playerHealth != null && playerHealth.Current == hpAtSafeStart) safeWithoutDamage = true;
                    }

                    if (playerHealth != null && playerHealth.Current < hpStart) tookDamage = true;

                    if (sawCharged && sawSafe && tookDamage && safeWithoutDamage)
                    {
                        colorsDiffer = Vector4.Distance(chargedColor, safeColor) > 0.15f;
                        break;
                    }

                    yield return null;
                }

                if (!colorsDiffer) colorsDiffer = Vector4.Distance(chargedColor, safeColor) > 0.15f;

                zoneOk = sawCharged && sawSafe && tookDamage && safeWithoutDamage && colorsDiffer;
                zoneDetail = "cargada=" + sawCharged + " segura=" + sawSafe
                             + " daño=" + (hpStart - (playerHealth != null ? playerHealth.Current : hpStart))
                             + " seguraSinDaño=" + safeWithoutDamage + " estadoVisible=" + colorsDiffer;
            }

            Check(zoneOk,
                "18. Zona eléctrica: alterna, el estado seguro no daña y se distingue visualmente",
                zoneDetail);

            var movingPlatform = Object.FindFirstObjectByType<MovingPlatform>();
            bool platformOk = false;
            string platformDetail = "sin plataforma móvil";
            if (movingPlatform != null)
            {
                var platformCollider = movingPlatform.GetComponent<Collider2D>();
                float top = platformCollider != null
                    ? platformCollider.bounds.max.y
                    : movingPlatform.transform.position.y + 0.3f;

                Teleport(player, movingPlatform.transform.position.x, top + 0.2f);
                if (playerHealth != null) playerHealth.ResetHealth();
                yield return new WaitForSecondsRealtime(0.35f);

                float platformStart = movingPlatform.transform.position.x;
                float playerStart = player.transform.position.x;

                yield return new WaitForSecondsRealtime(2.2f);

                float platformDelta = movingPlatform.transform.position.x - platformStart;
                float playerDelta = player.transform.position.x - playerStart;

                platformOk = Mathf.Abs(platformDelta) >= 1f && Mathf.Abs(playerDelta) >= 0.5f
                             && Mathf.Sign(playerDelta) == Mathf.Sign(platformDelta)
                             && Mathf.Abs(player.transform.position.y - top) < 1.2f;

                platformDetail = "plataforma=" + platformDelta.ToString("0.00")
                                 + " jugador=" + playerDelta.ToString("0.00");
            }

            Check(platformOk, "19. Plataforma móvil: se desplaza y arrastra al jugador que lleva encima", platformDetail);

            var conveyors = All<ConveyorBelt>();
            // La cinta de x=50 queda bajo el escalón BeltRun; se prueba la de x=78, que está despejada.
            var belt = Nearest<ConveyorBelt>(78f);
            bool beltOk = false;
            string beltDetail = "sin cintas";
            if (belt != null)
            {
                var beltCollider = belt.GetComponent<Collider2D>();
                float top = beltCollider != null
                    ? beltCollider.bounds.max.y
                    : belt.transform.position.y + 0.2f;

                // Aislamiento de la medida: `Armored_70` patrulla a 8 unidades de esta cinta y
                // puede caminar hasta el jugador y empujarlo (golpe de contacto con retroceso),
                // lo que falseaba el arrastre. Se aparcan los enemigos cercanos sólo mientras
                // dura la medida y se restauran después.
                var parkedNearBelt = new List<MonoBehaviour>();
                var enemiesNearBelt = All<EnemyBase>();
                for (int i = 0; i < enemiesNearBelt.Length; i++)
                {
                    var enemy = enemiesNearBelt[i];
                    if (enemy == null) continue;
                    if (Mathf.Abs(enemy.transform.position.x - belt.transform.position.x) > 10f) continue;

                    enemy.enabled = false;
                    parkedNearBelt.Add(enemy);

                    var enemyTouches = enemy.GetComponentsInChildren<DamageOnTouch>(true);
                    for (int t = 0; t < enemyTouches.Length; t++)
                        if (enemyTouches[t] != null) { enemyTouches[t].enabled = false; parkedNearBelt.Add(enemyTouches[t]); }
                }

                Teleport(player, belt.transform.position.x, top + 0.2f);
                if (playerHealth != null) playerHealth.ResetHealth();

                // La plataforma móvil recorre x 74→80 y su cara inferior queda a la altura de la
                // cabeza del jugador que va de pie sobre esta cinta (cima 0,28 + altura 1,72 =
                // 2,00 frente a 1,90 de la plataforma), así que lo empuja lateralmente cuando
                // pasa por encima. Su fase decide el resultado de la medida, de ahí que la
                // comprobación alternara entre pasar y fallar. Se retira sólo mientras dura.
                var platformNearBelt = Nearest<MovingPlatform>(belt.transform.position.x);
                bool platformWasActive = platformNearBelt != null && platformNearBelt.gameObject.activeSelf;
                float platformX = platformNearBelt != null ? platformNearBelt.transform.position.x : 0f;
                if (platformNearBelt != null) platformNearBelt.gameObject.SetActive(false);

                yield return new WaitForSecondsRealtime(0.4f);

                float beltStart = player.transform.position.x;
                yield return new WaitForSecondsRealtime(1.2f);

                float beltDelta = player.transform.position.x - beltStart;

                for (int i = 0; i < parkedNearBelt.Count; i++)
                    if (parkedNearBelt[i] != null) parkedNearBelt[i].enabled = true;

                if (platformNearBelt != null) platformNearBelt.gameObject.SetActive(platformWasActive);

                beltOk = conveyors.Length >= 3 && Mathf.Abs(beltDelta) >= 0.5f
                         && Mathf.Sign(beltDelta) == Mathf.Sign(belt.beltSpeed)
                         && Mathf.Abs(player.transform.position.y - top) < 1.2f;

                beltDetail = "cintas=" + conveyors.Length + " desplazamiento=" + beltDelta.ToString("0.00")
                             + " velocidad=" + belt.beltSpeed
                             + " enemigosAparcados=" + (parkedNearBelt.Count > 0 ? parkedNearBelt.Count / 2 : 0)
                             + " plataformaEn=" + platformX.ToString("0.00") + " plataformaRetirada=" + (platformNearBelt != null)
                             + " alturaJugador=" + (player.transform.position.y - top).ToString("0.00");
            }

            Check(beltOk, "20. Las cintas transportadoras arrastran al jugador en su dirección", beltDetail);

            // ---------------- E. ARMA PESADA ----------------
            var pickup = Nearest<HeavyWeaponPickup>(96f);
            bool pickupOk = false;
            string pickupDetail = "sin recogible";
            if (pickup != null && shooter != null)
            {
                int expectedAmmo = pickup.ammo;
                Teleport(player, pickup.transform.position.x, pickup.transform.position.y + 0.1f);
                yield return new WaitForSecondsRealtime(0.4f);

                string weaponLabel = WeaponLabel(hud);
                pickupOk = shooter.HeavyAmmo == expectedAmmo && pickup == null && weaponLabel.Contains("AMETRALLADORA");
                pickupDetail = "munición=" + shooter.HeavyAmmo + " esperada=" + expectedAmmo
                               + " recogible_eliminado=" + (pickup == null) + " hud=\"" + weaponLabel + "\"";
            }

            Check(pickupOk, "21. Ametralladora pesada: el recogible entrega munición limitada y el HUD lo muestra",
                pickupDetail);

            bool ammoOk = false;
            string ammoDetail = "sin arma";
            if (shooter != null)
            {
                shooter.ApplyHeavyWeapon(3);
                yield return null;

                int guard = 0;
                while (shooter.HasHeavyWeapon && guard < 8)
                {
                    shooter.TryFire(Vector2.right);
                    guard++;
                    yield return new WaitForSecondsRealtime(0.12f);
                }

                bool depleted = shooter.HeavyAmmo == 0 && !shooter.HasHeavyWeapon;
                bool basicFires = shooter.TryFire(Vector2.right);
                yield return null;
                yield return null;

                string weaponLabel = WeaponLabel(hud);
                ammoOk = depleted && basicFires && weaponLabel.Contains("ARMA BÁSICA");
                ammoDetail = "munición_final=" + shooter.HeavyAmmo + " arma_básica_dispara=" + basicFires
                             + " hud=\"" + weaponLabel + "\"";
            }

            Check(ammoOk, "22. La munición pesada es limitada y al agotarse vuelve el arma básica", ammoDetail);

            // ---------------- F. GENERADOR Y CIERRE ----------------
            // El jugador se vuelve invulnerable para que la oleada no lo elimine mientras se
            // comprueba el generador: aquí se mide el escudo y el cierre de misión, no el combate.
            if (playerHealth != null) playerHealth.invulnerable = true;

            UnparkEnemies();
            yield return new WaitForSecondsRealtime(0.4f);

            generator = Object.FindFirstObjectByType<GeneratorObjective>();
            bool shieldUp = false;
            bool damageBlocked = false;
            string shieldDetail = "sin generador";
            if (generator != null && generator.health != null)
            {
                shieldUp = generator.ShieldActive && generator.health.invulnerable;

                int healthBefore = generator.Current;
                generator.health.TakeDamage(new DamageInfo
                {
                    amount = 50,
                    point = generator.transform.position,
                    direction = Vector2.right,
                    source = null,
                    playerIndex = -1
                });

                yield return null;
                damageBlocked = generator.Current == healthBefore;
                shieldDetail = "escudo=" + generator.ShieldActive + " invulnerable=" + generator.health.invulnerable
                               + " salud=" + healthBefore + "->" + generator.Current;
            }

            Check(shieldUp && damageBlocked,
                "23. El escudo del generador lo hace invulnerable mientras vive la oleada", shieldDetail);

            bool chainOk = false;
            string chainDetail = "sin generador";
            if (generator != null && generator.health != null)
            {
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

                yield return new WaitForSecondsRealtime(0.6f);

                bool shieldDown = !generator.ShieldActive && !generator.health.invulnerable;
                int scoreBefore = GameSession.Score;

                generator.health.TakeDamage(new DamageInfo
                {
                    amount = 9999,
                    point = (Vector2)generator.transform.position + new Vector2(2f, 0.5f),
                    direction = Vector2.left,
                    source = null,
                    playerIndex = -1
                });

                yield return null;

                bool destroyed = generator.Destroyed && GameSession.Score == scoreBefore + 1200;

                bool sawMission = false;
                bool sawResults = false;
                bool sawPause = false;
                float nextNote = Time.realtimeSinceStartup + 1.5f;

                float deadline = Time.realtimeSinceStartup + 9f;
                while (Time.realtimeSinceStartup < deadline)
                {
                    if (lm != null && lm.MissionComplete) sawMission = true;
                    if (GameSession.MissionComplete) sawMission = true;
                    if (GameManager.Instance != null && GameManager.Instance.IsPaused) sawPause = true;

                    if (Time.realtimeSinceStartup >= nextNote)
                    {
                        nextNote += 1.5f;
                        Note("cierre: escena=" + SceneManager.GetActiveScene().name
                             + " timeScale=" + Time.timeScale
                             + " misión=" + sawMission + " pausa=" + sawPause
                             + " faderCargando=" + (GameManager.Instance != null && GameManager.Instance.Fader != null
                                 ? GameManager.Instance.Fader.IsLoading : false));
                    }

                    if (SceneManager.GetActiveScene().name == "Results") { sawResults = true; break; }
                    yield return null;
                }

                chainOk = shieldDown && destroyed && sawMission && sawResults;
                chainDetail = "guardias=" + guardsKilled + " escudoCae=" + shieldDown
                              + " destruido=" + destroyed + " misión=" + sawMission
                              + " pausa=" + sawPause + " Results=" + sawResults;
            }

            Check(chainOk,
                "24. Al caer el escudo, el generador se destruye (+1200), completa la misión y abre Results",
                chainDetail);

            Finish();
        }

        private void Finish()
        {
            int total = passed + failed;
            if (failed == 0) Debug.Log("[L] ===== VEREDICTO: OK " + passed + "/" + total + " comprobaciones =====");
            else Debug.LogError("[L] ===== VEREDICTO: FALLOS " + failed + " de " + total + " =====");
        }
    }
}
#endif
