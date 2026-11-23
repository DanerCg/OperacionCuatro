#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OperacionCuatro.EditorTools
{
    /// <summary>
    /// Fase 3 — AUDITORÍA DEL DAÑO POR TRIGGERS.
    ///
    /// Comprueba SteamVent, ElectricZone y DamageOnTouch con un jugador INMÓVIL (incluido un
    /// Rigidbody2D forzado a dormir) y con dos jugadores a la vez. Mide:
    ///   · primer impacto y daño posterior sin mover al jugador (expiración de la invulnerabilidad),
    ///   · intervalos entre impactos y ausencia de daño duplicado,
    ///   · origen real de cada impacto (GameObject de la fuente),
    ///   · ausencia de daño cuando el peligro está inactivo.
    ///
    /// Los tres componentes se prueban en montajes de prueba (trigger grande en zona despejada)
    /// con la MISMA configuración que usan en Level02, para que el jugador no quede a merced de
    /// la cinta o de otro peligro del nivel y ninguna medida se contamine.
    /// </summary>
    public static class OC_HazardDamageTest
    {
        [UnityEditor.MenuItem("OC/Playtest/Auditoría de daño por triggers")]
        public static void Run()
        {
            var go = new GameObject("[OC_HazardDamageTest]");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<OCHazardDamageRunner>();
        }
    }

    public class OCHazardDamageRunner : MonoBehaviour
    {
        private int passed;
        private int failed;

        private PlayerController2D p1;
        private PlayerController2D p2;
        private Health h1;
        private Health h2;

        private readonly List<Recorder> recorders = new List<Recorder>();

        // ==================================================================
        //  Utilidades
        // ==================================================================
        private void Check(bool ok, string what, string detail = "")
        {
            if (ok) { passed++; Debug.Log("[HZ] PASS  " + what + (detail.Length > 0 ? " | " + detail : "")); }
            else { failed++; Debug.LogError("[HZ] FAIL  " + what + (detail.Length > 0 ? " | " + detail : "")); }
        }

        private void Note(string text) { Debug.Log("[HZ] ....  " + text); }

        private void Finish()
        {
            Debug.Log("[HZ] ===== VEREDICTO: " + (failed == 0
                ? "OK " + passed + "/" + (passed + failed) + " comprobaciones de daño por triggers ====="
                : "FALLOS " + failed + " de " + (passed + failed) + " ====="));

            Destroy(gameObject);
        }

        /// <summary>Registrador de impactos de un jugador concreto.</summary>
        private class Recorder
        {
            public string label;
            public GameObject expected;
            public System.Func<bool> activeQuery;

            public int hits;
            public int inactiveHits;
            public int foreignHits;
            public float minGap = 999f;
            private float last = -999f;

            public readonly List<string> log = new List<string>();

            public void Handle(DamageInfo info)
            {
                float now = Time.time;
                string source = info.source != null ? info.source.name : "nulo";
                bool fromExpected = expected != null && info.source == expected;
                bool active = activeQuery == null || activeQuery();

                if (!fromExpected) foreignHits++;
                else if (!active) inactiveHits++;

                if (last > -900f) minGap = Mathf.Min(minGap, now - last);
                last = now;
                hits++;

                if (log.Count < 14) log.Add(source + ":" + info.amount + (active ? "" : "[inactivo]"));
            }

            public void Reset()
            {
                hits = 0;
                inactiveHits = 0;
                foreignHits = 0;
                minGap = 999f;
                last = -999f;
                log.Clear();
            }

            public string Detail()
            {
                string gap = minGap > 900f ? "-" : minGap.ToString("0.00");
                return label + ": impactos=" + hits + " intervaloMin=" + gap
                       + " enInactivo=" + inactiveHits + " ajenos=" + foreignHits
                       + " [" + string.Join(", ", log.ToArray()) + "]";
            }
        }

        private Recorder MakeRecorder(string label, PlayerController2D player, System.Func<bool> activeQuery)
        {
            var recorder = new Recorder();
            recorder.label = label;
            recorder.activeQuery = activeQuery;
            player.health.Damaged += recorder.Handle;
            recorders.Add(recorder);
            return recorder;
        }

        private void DisposeRecorders()
        {
            for (int i = 0; i < recorders.Count; i++)
            {
                if (recorders[i] == null) continue;
                if (h1 != null) h1.Damaged -= recorders[i].Handle;
                if (h2 != null) h2.Damaged -= recorders[i].Handle;
            }

            recorders.Clear();
        }

        private static float GroundSurfaceAt(float x)
        {
            var hit = Physics2D.Raycast(new Vector2(x, 40f), Vector2.down, 60f, 1 << 8);
            return hit.collider != null ? hit.point.y : 0f;
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

        private static void MakeVulnerable(PlayerController2D player)
        {
            if (player == null) return;
            player.ClearInvulnerability();
            if (player.health != null) { player.health.ResetHealth(); player.health.invulnerable = false; }
        }

        private static GameObject MakeRig(string name, float x, float surfaceY, float width, float height)
        {
            var rig = new GameObject(name);
            rig.transform.position = new Vector3(x, surfaceY + height * 0.5f - 0.4f, 0f);

            var col = rig.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(width, height);

            return rig;
        }

        /// <summary>
        /// Coloca al jugador en el centro del montaje, sobre el suelo, y lo fuerza a dormir.
        /// Devuelve el Rigidbody2D para poder comprobar IsSleeping() durante el muestreo.
        /// </summary>
        private IEnumerator PlaceAndSleep(PlayerController2D player, float x, float surfaceY)
        {
            Teleport(player, x, surfaceY + 0.12f);
            yield return new WaitForSecondsRealtime(0.45f);

            var body = player.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.Sleep();
            }

            yield return new WaitForSecondsRealtime(0.1f);
        }

        private IEnumerator Awaken(PlayerController2D player)
        {
            var body = player.GetComponent<Rigidbody2D>();
            if (body != null) body.WakeUp();
            yield return null;
        }

        // ==================================================================
        //  Secuencia
        // ==================================================================
        private IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(0.8f);

            GameSession.StartNewRun(2);
            GameSession.SetCharacter(0, CharacterId.Danner);
            GameSession.SetCharacter(1, CharacterId.Ines);
            GameSession.StartLevel(2);

            SceneManager.LoadScene(SceneNames.Level02);
            yield return null;
            yield return new WaitForSecondsRealtime(1.9f);

            var lm = LevelManager.Instance;
            p1 = lm != null && lm.Players.Count > 0 ? lm.Players[0] : null;
            p2 = lm != null && lm.Players.Count > 1 ? lm.Players[1] : null;
            h1 = p1 != null ? p1.health : null;
            h2 = p2 != null ? p2.health : null;

            if (p1 == null || p2 == null || h1 == null || h2 == null)
            {
                Check(false, "0. Sesión de dos jugadores en Level02", "jugadores=" + (lm != null ? lm.Players.Count : -1));
                Finish();
                yield break;
            }

            p1.SetInputEnabled(false);
            p2.SetInputEnabled(false);

            // Fuera de la prueba: enemigos y HitBoxes no deben aportar daño.
            var enemies = Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
            for (int i = 0; i < enemies.Length; i++)
                if (enemies[i] != null) enemies[i].enabled = false;

            var touches = Object.FindObjectsByType<DamageOnTouch>(FindObjectsSortMode.None);
            for (int i = 0; i < touches.Length; i++)
                if (touches[i] != null) touches[i].enabled = false;

            yield return null;

            Note("jugadores=" + lm.Players.Count + " enemigos aparcados=" + enemies.Length + " hitboxes aparcados=" + touches.Length);

            float ventX = 8f;
            float zoneX = 20f;
            float touchX = 30f;

            float ventY = GroundSurfaceAt(ventX);
            float zoneY = GroundSurfaceAt(zoneX);
            float touchY = GroundSurfaceAt(touchX);

            // --- montajes de prueba con los componentes REALES y su configuración de Level02 ---
            var ventRig = MakeRig("Rig_SteamVent", ventX, ventY, 6f, 5f);
            var vent = ventRig.AddComponent<SteamVent>();
            vent.cycleTime = 4.6f;
            vent.warningTime = 0.9f;
            vent.activeTime = 1.5f;
            vent.damage = 14;
            vent.damageInterval = 0.45f;

            var zoneRig = MakeRig("Rig_ElectricZone", zoneX, zoneY, 6f, 5f);
            var zone = zoneRig.AddComponent<ElectricZone>();
            zone.onTime = 2.2f;
            zone.offTime = 1.8f;
            zone.damage = 16;
            zone.damageInterval = 0.5f;

            var touchRig = MakeRig("Rig_HitBox", touchX, touchY, 3f, 4f);
            var touch = touchRig.AddComponent<DamageOnTouch>();
            touch.damage = 15;
            touch.repeatDelay = 0.9f;
            touch.faction = Faction.Enemy;

            yield return null;

            var recVent = MakeRecorder("P1/vapor", p1, () => vent.IsActive);
            var recZone = MakeRecorder("P1/eléctrica", p1, () => zone.IsCharged);
            var recZone2 = MakeRecorder("P2/eléctrica", p2, () => zone.IsCharged);
            var recTouch = MakeRecorder("P1/contacto", p1, () => touch != null && touch.enabled);

            recVent.expected = ventRig;
            recZone.expected = zoneRig;
            recZone2.expected = zoneRig;
            recTouch.expected = touchRig;

            // ---------------- A0. PREMISA: ¿puede un jugador inmóvil entrar en reposo? ----------------
            Teleport(p2, zoneX + 14f, GroundSurfaceAt(zoneX + 14f) + 0.12f);
            MakeVulnerable(p1);
            MakeVulnerable(p2);
            recVent.Reset();

            // Con el componente desactivado: ni daño ni duda sobre el origen de un golpe.
            vent.enabled = false;
            yield return PlaceAndSleep(p1, ventX, ventY);

            var bodyIdle = p1.GetComponent<Rigidbody2D>();
            int idleSamples = 0;
            int idleSleeping = 0;
            float idleEnd = Time.time + 1.5f;

            while (Time.time < idleEnd)
            {
                idleSamples++;
                if (bodyIdle != null && bodyIdle.IsSleeping()) idleSleeping++;
                yield return null;
            }

            Check(recVent.hits == 0,
                "A0. Un SteamVent desactivado no hiere a un jugador inmóvil dentro de su trigger",
                "impactos con el componente desactivado=" + recVent.hits + " | " + recVent.Detail());

            Check(idleSleeping == 0,
                "A0b. Un jugador inmóvil nunca entra en reposo (por eso OnTriggerStay2D no se pierde)",
                "muestras=" + idleSamples + " dormidoEn=" + idleSleeping);

            vent.enabled = true;
            MakeVulnerable(p1);
            recVent.Reset();
            yield return PlaceAndSleep(p1, ventX, ventY);

            var bodyVent = p1.GetComponent<Rigidbody2D>();
            bool sleptBeforeHit = bodyVent != null && bodyVent.IsSleeping();

            bool ventHitWhileAsleep = false;
            int ventHitsAtSleep = recVent.hits;
            float ventEnd = Time.time + 10f;

            while (Time.time < ventEnd)
            {
                if (recVent.hits > ventHitsAtSleep && bodyVent != null && bodyVent.IsSleeping()) ventHitWhileAsleep = true;
                if (recVent.hits >= 2) break;
                yield return null;
            }

            Check(recVent.hits > 0 && bodyVent != null && !bodyVent.IsSleeping(),
                "A1. El chorro de vapor daña a un jugador inmóvil (cuerpo despierto, OnTriggerStay2D activo)",
                "dormidoAntes=" + sleptBeforeHit + " impactosEnReposo=" + ventHitWhileAsleep + " | " + recVent.Detail());

            Check(recVent.hits >= 2 && recVent.inactiveHits == 0,
                "A2. El vapor repite el daño sin mover al jugador y solo mientras el chorro está activo",
                recVent.Detail());

            Check(recVent.foreignHits == 0 && recVent.hits > 0,
                "A3. Todos los impactos del vapor se atribuyen al objeto del chorro",
                recVent.Detail());

            yield return Awaken(p1);

            // ---------------- B. ZONA ELÉCTRICA: DORMIDO + REPETICIÓN + INTERVALOS ----------------
            Teleport(p1, ventX - 10f, GroundSurfaceAt(ventX - 10f) + 0.12f);
            yield return null;

            MakeVulnerable(p1);
            MakeVulnerable(p2);
            recZone.Reset();
            recZone2.Reset();
            yield return PlaceAndSleep(p1, zoneX, zoneY);

            var bodyZone = p1.GetComponent<Rigidbody2D>();
            bool zoneSleptBeforeHit = bodyZone != null && bodyZone.IsSleeping();

            bool zoneHitWhileAsleep = false;
            int zoneHitsAtSleep = recZone.hits;
            float zoneEnd = Time.time + 10f;

            while (Time.time < zoneEnd)
            {
                if (recZone.hits > zoneHitsAtSleep && bodyZone != null && bodyZone.IsSleeping()) zoneHitWhileAsleep = true;
                if (recZone.hits >= 2) break;
                yield return null;
            }

            Check(recZone.hits > 0 && bodyZone != null && !bodyZone.IsSleeping(),
                "B1. La zona eléctrica daña a un jugador inmóvil (cuerpo despierto, OnTriggerStay2D activo)",
                "dormidoAntes=" + zoneSleptBeforeHit + " impactosEnReposo=" + zoneHitWhileAsleep + " | " + recZone.Detail());

            Check(recZone.hits >= 2,
                "B2. La zona eléctrica repite el daño sin mover al jugador (invulnerabilidad expirada)",
                recZone.Detail());

            Check(recZone.inactiveHits == 0,
                "B3. La zona eléctrica no daña mientras está apagada",
                recZone.Detail());

            Check(recZone.minGap >= 0.4f,
                "B4. Sin impactos duplicados: separación mínima entre golpes ≥ 0,40 s",
                recZone.Detail());

            Check(recZone.foreignHits == 0 && recZone.hits > 0,
                "B5. Todos los impactos eléctricos se atribuyen al objeto de la zona",
                recZone.Detail());

            yield return Awaken(p1);

            // ---------------- C. DOS JUGADORES EN LA MISMA ZONA ----------------
            MakeVulnerable(p1);
            MakeVulnerable(p2);
            recZone.Reset();
            recZone2.Reset();

            Teleport(p1, zoneX - 1f, GroundSurfaceAt(zoneX - 1f) + 0.12f);
            Teleport(p2, zoneX + 1f, GroundSurfaceAt(zoneX + 1f) + 0.12f);
            yield return new WaitForSecondsRealtime(0.35f);

            float bothEnd = Time.time + 10f;
            while (Time.time < bothEnd)
            {
                if (recZone.hits >= 2 && recZone2.hits >= 2) break;
                yield return null;
            }

            Check(recZone.hits > 0 && recZone2.hits > 0,
                "C1. Dos jugadores a la vez reciben daño de la misma zona eléctrica",
                recZone.Detail() + " || " + recZone2.Detail());

            Check(recZone.foreignHits == 0 && recZone2.foreignHits == 0
                  && recZone.inactiveHits == 0 && recZone2.inactiveHits == 0,
                "C2. Con dos jugadores no hay daño ajeno ni daño con la zona apagada",
                recZone.Detail() + " || " + recZone2.Detail());

            // ---------------- D. DAÑO POR CONTACTO (DamageOnTouch) ----------------
            Teleport(p2, touchX + 12f, GroundSurfaceAt(touchX + 12f) + 0.12f);
            yield return null;

            MakeVulnerable(p1);
            recTouch.Reset();
            yield return PlaceAndSleep(p1, touchX, touchY);

            var bodyTouch = p1.GetComponent<Rigidbody2D>();
            bool touchHitWhileAsleep = false;
            int touchHitsAtSleep = recTouch.hits;
            float touchEnd = Time.time + 8f;

            while (Time.time < touchEnd)
            {
                if (recTouch.hits > touchHitsAtSleep && bodyTouch != null && bodyTouch.IsSleeping()) touchHitWhileAsleep = true;
                if (recTouch.hits >= 2) break;
                yield return null;
            }

            Check(recTouch.hits > 0 && bodyTouch != null && !bodyTouch.IsSleeping(),
                "D1. El daño por contacto hiere a un jugador inmóvil (cuerpo despierto, OnTriggerStay2D activo)",
                "dormidoAlImpactar=" + touchHitWhileAsleep + " | " + recTouch.Detail());

            Check(recTouch.hits >= 2 && recTouch.foreignHits == 0,
                "D2. El contacto repite el daño con intervalos y sin fuente ajena",
                recTouch.Detail());

            // --- con el componente desactivado no debe haber daño ---
            yield return Awaken(p1);
            MakeVulnerable(p1);
            recTouch.Reset();
            int hitsBeforeDisable = recTouch.hits;
            touch.enabled = false;
            Teleport(p1, touchX, GroundSurfaceAt(touchX) + 0.12f);
            yield return new WaitForSecondsRealtime(3f);

            Check(recTouch.hits == hitsBeforeDisable,
                "D3. Con el daño por contacto desactivado el jugador no recibe golpes",
                recTouch.Detail() + " (hits durante la ventana=" + (recTouch.hits - hitsBeforeDisable) + ")");

            touch.enabled = true;

            // ---------------- E. ZONA ELÉCTRICA DESACTIVADA ----------------
            yield return Awaken(p1);
            Teleport(p2, touchX + 12f, GroundSurfaceAt(touchX + 12f) + 0.12f);
            yield return null;

            MakeVulnerable(p1);
            recZone.Reset();
            zone.enabled = false;
            Teleport(p1, zoneX, GroundSurfaceAt(zoneX) + 0.12f);
            yield return new WaitForSecondsRealtime(3f);

            Check(recZone.hits == 0,
                "E. La zona eléctrica desactivada no daña a un jugador dentro de su trigger",
                "impactos con el componente desactivado=" + recZone.hits + " | " + recZone.Detail());

            zone.enabled = true;

            // ---------------- F. DOS JUGADORES EN EL MISMO DAÑO POR CONTACTO ----------------
            MakeVulnerable(p1);
            MakeVulnerable(p2);
            recTouch.Reset();

            var recTouch2 = MakeRecorder("P2/contacto", p2, () => touch != null && touch.enabled);
            recTouch2.expected = touchRig;
            recTouch2.Reset();

            Teleport(p1, touchX - 0.6f, GroundSurfaceAt(touchX - 0.6f) + 0.12f);
            Teleport(p2, touchX + 0.6f, GroundSurfaceAt(touchX + 0.6f) + 0.12f);
            yield return new WaitForSecondsRealtime(0.3f);

            float pairEnd = Time.time + 4f;
            while (Time.time < pairEnd)
            {
                if (recTouch.hits >= 2 && recTouch2.hits >= 2) break;
                yield return null;
            }

            Check(recTouch.hits > 0 && recTouch2.hits > 0,
                "F1. Dos jugadores que tocan el mismo DamageOnTouch reciben ambos daño (temporizador por víctima)",
                recTouch.Detail() + " || " + recTouch2.Detail());

            Check(recTouch.foreignHits == 0 && recTouch2.foreignHits == 0,
                "F2. Ninguno de los dos recibe el golpe de una fuente ajena",
                recTouch.Detail() + " || " + recTouch2.Detail());

            // ==================================================================
            //  G. STEAMVENT — criterios restantes: dos jugadores, intervalos,
            //     fuera del volumen y facción
            // ==================================================================
            yield return Awaken(p1);
            yield return Awaken(p2);
            MakeVulnerable(p1);
            MakeVulnerable(p2);

            var recVent2 = MakeRecorder("P2/vapor", p2, () => vent.IsActive);
            recVent2.expected = ventRig;
            recVent.Reset();
            recVent2.Reset();

            Teleport(p1, ventX - 1f, GroundSurfaceAt(ventX - 1f) + 0.12f);
            Teleport(p2, ventX + 1f, GroundSurfaceAt(ventX + 1f) + 0.12f);
            yield return new WaitForSecondsRealtime(0.35f);

            float ventBothEnd = Time.time + 12f;
            while (Time.time < ventBothEnd)
            {
                if (recVent.hits >= 2 && recVent2.hits >= 2) break;
                yield return null;
            }

            Check(recVent.hits > 0 && recVent2.hits > 0,
                "G1. Dos jugadores dentro del chorro de vapor reciben daño independiente",
                recVent.Detail() + " || " + recVent2.Detail());

            Check(recVent.hits >= 2 && recVent2.hits >= 2
                  && recVent.minGap >= 0.4f && recVent2.minGap >= 0.4f
                  && recVent.foreignHits == 0 && recVent2.foreignHits == 0,
                "G2. Vapor: intervalos ≥ 0,40 s, sin impactos duplicados y con el chorro como única fuente",
                recVent.Detail() + " || " + recVent2.Detail());

            // --- fuera del volumen ---
            recVent.Reset();
            recVent2.Reset();
            Teleport(p1, ventX - 6f, GroundSurfaceAt(ventX - 6f) + 0.12f);
            Teleport(p2, ventX - 8f, GroundSurfaceAt(ventX - 8f) + 0.12f);
            yield return new WaitForSecondsRealtime(2.5f);

            Check(recVent.hits == 0 && recVent2.hits == 0,
                "G3. El vapor no daña a un jugador fuera de su volumen, aunque el chorro esté activo",
                "impactos fuera=" + recVent.hits + "/" + recVent2.hits + " | " + recVent.Detail());

            // --- facción: el P2 pasa a facción enemiga y deja de ser víctima válida ---
            MakeVulnerable(p1);
            MakeVulnerable(p2);
            recVent.Reset();
            recVent2.Reset();
            var factionP2 = h2.faction;
            h2.faction = Faction.Enemy;

            Teleport(p1, ventX - 1f, GroundSurfaceAt(ventX - 1f) + 0.12f);
            Teleport(p2, ventX + 1f, GroundSurfaceAt(ventX + 1f) + 0.12f);
            yield return new WaitForSecondsRealtime(3f);

            Check(recVent.hits > 0 && recVent2.hits == 0,
                "G4. El vapor respeta la facción: sólo hiere a salud de facción de jugador",
                "P1(facción jugador)=" + recVent.hits + " P2(facción enemiga)=" + recVent2.hits);

            h2.faction = factionP2;

            // ==================================================================
            //  H. ZONA ELÉCTRICA — fuera del volumen y facción
            // ==================================================================
            Teleport(p2, zoneX + 14f, GroundSurfaceAt(zoneX + 14f) + 0.12f);
            yield return null;

            MakeVulnerable(p1);
            recZone.Reset();
            Teleport(p1, zoneX - 6f, GroundSurfaceAt(zoneX - 6f) + 0.12f);
            yield return new WaitForSecondsRealtime(2.5f);

            Check(recZone.hits == 0,
                "H1. La zona eléctrica no daña a un jugador fuera de su volumen",
                "impactos fuera=" + recZone.hits + " | " + recZone.Detail());

            MakeVulnerable(p1);
            MakeVulnerable(p2);
            recZone.Reset();
            recZone2.Reset();
            var factionP2b = h2.faction;
            h2.faction = Faction.Enemy;

            Teleport(p1, zoneX - 1f, GroundSurfaceAt(zoneX - 1f) + 0.12f);
            Teleport(p2, zoneX + 1f, GroundSurfaceAt(zoneX + 1f) + 0.12f);
            yield return new WaitForSecondsRealtime(3f);

            Check(recZone.hits > 0 && recZone2.hits == 0,
                "H2. La zona eléctrica respeta la facción: sólo hiere a salud de facción de jugador",
                "P1(facción jugador)=" + recZone.hits + " P2(facción enemiga)=" + recZone2.hits);

            h2.faction = factionP2b;

            // ==================================================================
            //  I. DAMAGEONTOUCH — intervalos, fuera del volumen y facción
            // ==================================================================
            Teleport(p2, touchX + 12f, GroundSurfaceAt(touchX + 12f) + 0.12f);
            yield return null;

            MakeVulnerable(p1);
            recTouch.Reset();
            yield return PlaceAndSleep(p1, touchX, touchY);

            float touchIEnd = Time.time + 8f;
            while (Time.time < touchIEnd)
            {
                if (recTouch.hits >= 3) break;
                yield return null;
            }

            Check(recTouch.hits >= 3 && recTouch.minGap >= 0.85f && recTouch.foreignHits == 0,
                "I1. Contacto: repite el daño con el intervalo configurado (≥ 0,85 s) y sin duplicados",
                recTouch.Detail());

            // Fuera del volumen del contacto pero también lejos de los otros dos montajes
            // (vapor 5..11, eléctrica 17..23, contacto 28,5..31,5): a 25,5 ningún otro peligro
            // de la prueba puede aportar un impacto y la medida sigue siendo del contacto.
            recTouch.Reset();
            Teleport(p1, 25.5f, GroundSurfaceAt(25.5f) + 0.12f);
            yield return new WaitForSecondsRealtime(2.5f);

            Check(recTouch.hits == 0,
                "I2. El daño por contacto no alcanza a un jugador fuera de su volumen",
                "impactos fuera=" + recTouch.hits + " | " + recTouch.Detail());

            MakeVulnerable(p1);
            MakeVulnerable(p2);
            recTouch.Reset();
            var recTouch2f = MakeRecorder("P2/contacto/facción", p2, () => touch != null && touch.enabled);
            recTouch2f.expected = touchRig;
            recTouch2f.Reset();

            var factionP2c = h2.faction;
            h2.faction = Faction.Enemy;

            Teleport(p1, touchX - 1f, GroundSurfaceAt(touchX - 1f) + 0.12f);
            Teleport(p2, touchX + 1f, GroundSurfaceAt(touchX + 1f) + 0.12f);
            yield return new WaitForSecondsRealtime(3f);

            Check(recTouch.hits > 0 && recTouch2f.hits == 0,
                "I3. El contacto respeta la facción: sólo hiere a salud de facción de jugador",
                "P1(facción jugador)=" + recTouch.hits + " P2(facción enemiga)=" + recTouch2f.hits);

            h2.faction = factionP2c;

            // ==================================================================
            //  J. LASERTRAP — los diez criterios
            // ==================================================================
            float laserX = 40f;
            float laserY = GroundSurfaceAt(laserX);

            var laserRig = MakeRig("Rig_LaserTrap", laserX, laserY, 4f, 3f);
            var laser = laserRig.AddComponent<LaserTrap>();
            yield return null;

            Teleport(p2, laserX + 12f, GroundSurfaceAt(laserX + 12f) + 0.12f);
            MakeVulnerable(p1);
            MakeVulnerable(p2);

            var recLaser = MakeRecorder("P1/láser", p1, () => laser.Current == LaserTrap.Phase.Active);
            recLaser.expected = laserRig;
            recLaser.Reset();

            // J1: primer impacto con el cuerpo forzado a dormir.
            yield return PlaceAndSleep(p1, laserX, laserY);

            var bodyLaser = p1.GetComponent<Rigidbody2D>();
            bool laserSleptBeforeHit = bodyLaser != null && bodyLaser.IsSleeping();
            float laserEnd = Time.time + 12f;
            while (Time.time < laserEnd)
            {
                if (recLaser.hits >= 2) break;
                yield return null;
            }

            Check(recLaser.hits > 0,
                "J1. El láser daña a un jugador inmóvil desde el primer momento (cuerpo dormido al empezar="
                + laserSleptBeforeHit + ")",
                recLaser.Detail());

            Check(recLaser.hits >= 2 && recLaser.minGap >= 0.45f,
                "J2. El láser repite el daño sin mover al jugador, con intervalos ≥ 0,45 s y sin duplicados",
                recLaser.Detail());

            Check(recLaser.foreignHits == 0 && recLaser.inactiveHits == 0 && recLaser.hits > 0,
                "J3. Todos los impactos del láser se atribuyen al objeto del haz y ocurren con el haz activo",
                recLaser.Detail());

            yield return Awaken(p1);

            // J4: fuera del volumen.
            recLaser.Reset();
            Teleport(p1, laserX - 6f, GroundSurfaceAt(laserX - 6f) + 0.12f);
            yield return new WaitForSecondsRealtime(2.5f);

            Check(recLaser.hits == 0,
                "J4. El láser no daña a un jugador fuera de su volumen",
                "impactos fuera=" + recLaser.hits + " | " + recLaser.Detail());

            // J5: dos jugadores dentro del haz.
            MakeVulnerable(p1);
            MakeVulnerable(p2);
            recLaser.Reset();

            var recLaser2 = MakeRecorder("P2/láser", p2, () => laser.Current == LaserTrap.Phase.Active);
            recLaser2.expected = laserRig;
            recLaser2.Reset();

            Teleport(p1, laserX - 0.8f, GroundSurfaceAt(laserX - 0.8f) + 0.12f);
            Teleport(p2, laserX + 0.8f, GroundSurfaceAt(laserX + 0.8f) + 0.12f);
            yield return new WaitForSecondsRealtime(0.35f);

            float laserBothEnd = Time.time + 12f;
            while (Time.time < laserBothEnd)
            {
                if (recLaser.hits >= 1 && recLaser2.hits >= 1) break;
                yield return null;
            }

            Check(recLaser.hits > 0 && recLaser2.hits > 0,
                "J5. Dos jugadores dentro del haz reciben daño independiente",
                recLaser.Detail() + " || " + recLaser2.Detail());

            // J6: facción.
            Teleport(p2, laserX + 12f, GroundSurfaceAt(laserX + 12f) + 0.12f);
            yield return null;

            MakeVulnerable(p1);
            MakeVulnerable(p2);
            recLaser.Reset();
            recLaser2.Reset();

            var factionP2d = h2.faction;
            h2.faction = Faction.Enemy;

            Teleport(p1, laserX - 0.8f, GroundSurfaceAt(laserX - 0.8f) + 0.12f);
            Teleport(p2, laserX + 0.8f, GroundSurfaceAt(laserX + 0.8f) + 0.12f);
            yield return new WaitForSecondsRealtime(3f);

            Check(recLaser.hits > 0 && recLaser2.hits == 0,
                "J6. El láser respeta la facción: sólo hiere a salud de facción de jugador",
                "P1(facción jugador)=" + recLaser.hits + " P2(facción enemiga)=" + recLaser2.hits);

            h2.faction = factionP2d;

            // J7: peligro desactivado — se desactiva con el haz EN ACTIVO, el peor caso.
            Teleport(p2, laserX + 12f, GroundSurfaceAt(laserX + 12f) + 0.12f);
            yield return null;

            MakeVulnerable(p1);
            recLaser.Reset();
            Teleport(p1, laserX, GroundSurfaceAt(laserX) + 0.12f);

            float laserWaitActive = 0f;
            while (laser.Current != LaserTrap.Phase.Active && laserWaitActive < 6f)
            {
                laserWaitActive += Time.unscaledDeltaTime;
                yield return null;
            }

            bool laserWasActive = laser.Current == LaserTrap.Phase.Active;
            recLaser.Reset();
            laser.enabled = false;
            yield return new WaitForSecondsRealtime(3f);

            Check(laserWasActive && recLaser.hits == 0,
                "J7. Un láser desactivado (desactivado con el haz en activo) deja de dañar",
                "hazActivoAlDesactivar=" + laserWasActive + " impactos con el componente desactivado="
                + recLaser.hits + " | " + recLaser.Detail());

            laser.enabled = true;
            MakeVulnerable(p1);

            DisposeRecorders();
            Finish();
        }
    }
}
#endif
