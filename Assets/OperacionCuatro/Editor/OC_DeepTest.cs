#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace OperacionCuatro.EditorTools
{
    /// <summary>
    /// Batería de pruebas profundas de la Fase 1: audio, enemigos, daño por contacto,
    /// reaparición, los cuatro personajes y cooperativo de dos jugadores.
    /// Se ejecuta en Play Mode sobre Level01 y registra PASS/FAIL en la consola.
    /// </summary>
    public static class OC_DeepTest
    {
        [UnityEditor.MenuItem("OC/Playtest/5. Profundo (audio, enemigos, vidas, personajes, 2P)")]
        public static void StartDeep() { Create(); }

        public static void Create()
        {
            var go = new GameObject("[OC_DeepTest]");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<OCDeepRunner>();
        }
    }

    public class OCDeepRunner : MonoBehaviour
    {
        private int passed;
        private int failed;
        private Keyboard kb;

        private static readonly string[] RequiredClips =
        {
            "sfx_shoot", "sfx_bomb_throw", "sfx_explosion", "sfx_enemy_shoot", "sfx_enemy_hit",
            "sfx_enemy_death", "sfx_checkpoint", "sfx_alert", "sfx_jump", "sfx_hit",
            "sfx_player_death", "sfx_select", "sfx_start", "mus_menu", "mus_level"
        };

        // ------------------------------------------------------------------
        //  Utilidades
        // ------------------------------------------------------------------
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
            if (ok) { passed++; Debug.Log("[D] PASS  " + what + (detail.Length > 0 ? " | " + detail : "")); }
            else { failed++; Debug.LogError("[D] FAIL  " + what + (detail.Length > 0 ? " | " + detail : "")); }
        }

        private static T Find<T>() where T : Object { return Object.FindFirstObjectByType<T>(); }

        private static void ClearProjectiles()
        {
            var all = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
                if (all[i] != null) Object.Destroy(all[i].gameObject);
        }

        private static int CountProjectiles(Faction faction, int owner = -1)
        {
            int n = 0;
            var all = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null) continue;
                if (all[i].faction != faction) continue;
                if (owner >= 0 && all[i].ownerPlayerIndex != owner) continue;
                n++;
            }
            return n;
        }

        private int sampleMax;
        private float sampleFirstX;
        private float samplePlayerX;
        private bool sampleCaptured;

        /// <summary>
        /// Cuenta proyectiles en CADA frame durante la ventana y devuelve el máximo. Una sola
        /// lectura al final no vale: las balas viajan a 22 u/s y mueren contra el escenario.
        /// </summary>
        private IEnumerator SampleMaxProjectiles(float duration, Faction faction, int owner = -1)
        {
            sampleMax = 0;
            float t = 0f;
            while (t < duration)
            {
                sampleMax = Mathf.Max(sampleMax, CountProjectiles(faction, owner));
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        /// <summary>Muestreo simultáneo del máximo de proyectiles de los dos jugadores.</summary>
        private int sampleMaxA;
        private int sampleMaxB;

        private IEnumerator SampleMaxTwoPlayers(float duration)
        {
            sampleMaxA = 0;
            sampleMaxB = 0;
            float t = 0f;
            while (t < duration)
            {
                sampleMaxA = Mathf.Max(sampleMaxA, CountProjectiles(Faction.Player, 0));
                sampleMaxB = Mathf.Max(sampleMaxB, CountProjectiles(Faction.Player, 1));
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        /// <summary>Muestreo que además guarda la posición del primer proyectil visto y la del jugador en ese frame.</summary>
        private IEnumerator SampleProjectilesSide(float duration, Faction faction, int owner, Transform watch)
        {
            sampleMax = 0;
            sampleCaptured = false;
            sampleFirstX = 0f;
            samplePlayerX = 0f;
            float t = 0f;
            while (t < duration)
            {
                var all = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None);
                int n = 0;
                for (int i = 0; i < all.Length; i++)
                {
                    var pr = all[i];
                    if (pr == null) continue;
                    if (pr.faction != faction) continue;
                    if (owner >= 0 && pr.ownerPlayerIndex != owner) continue;
                    n++;
                    if (!sampleCaptured)
                    {
                        sampleCaptured = true;
                        sampleFirstX = pr.transform.position.x;
                        samplePlayerX = watch != null ? watch.position.x : 0f;
                    }
                }
                sampleMax = Mathf.Max(sampleMax, n);
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        /// <summary>Busca un punto de suelo despejado (sin props encima) cerca del indicado.</summary>
        private static bool FindClearStand(Vector3 near, out Vector3 stand)
        {
            stand = near;
            for (float d = 0f; d <= 30f; d += 1f)
            {
                for (int side = 0; side < 2; side++)
                {
                    float x = near.x + (side == 0 ? d : -d);
                    if (x < 3f || x > 125f) continue;

                    var gh = Physics2D.Raycast(new Vector2(x, 40f), Vector2.down, 60f, 1 << 8);
                    if (gh.collider == null) continue;

                    Vector3 cand = new Vector3(x, gh.point.y + 0.05f, 0f);

                    bool blocked = false;
                    var bodyHits = Physics2D.OverlapBoxAll((Vector2)cand + new Vector2(0f, 0.86f), new Vector2(0.72f, 1.72f), 0f);
                    for (int i = 0; i < bodyHits.Length; i++)
                    {
                        var h = bodyHits[i];
                        if (h != null && !h.isTrigger && h.gameObject.layer == 8) { blocked = true; break; }
                    }
                    if (blocked) continue;

                    var air = cand + new Vector3(-1.4f, 0.9f, 0f);
                    var airHits = Physics2D.OverlapPointAll(air);
                    for (int i = 0; i < airHits.Length; i++)
                    {
                        var h = airHits[i];
                        if (h != null && !h.isTrigger) { blocked = true; break; }
                    }
                    if (blocked) continue;

                    stand = cand;
                    return true;
                }
            }
            return false;
        }

        private IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(1.2f);

            yield return PhaseAudio();
            yield return PhaseEnemies();
            yield return PhaseDamageOnTouch();
            yield return PhaseRespawn();
            yield return PhaseGameOver();
            yield return PhaseCharacters();
            yield return PhaseTwoPlayers();

            Finish();
        }

        private void Finish()
        {
            int total = passed + failed;
            string head = "[D] ===== VEREDICTO PROFUNDO: ";
            if (failed == 0) Debug.Log(head + "OK " + passed + "/" + total + " comprobaciones =====");
            else Debug.LogError(head + "FALLOS " + failed + " de " + total + " =====");
        }

        // ==================================================================
        //  TAREA 1 - AUDIO
        // ==================================================================
        private IEnumerator PhaseAudio()
        {
            var gm = GameManager.Instance;
            Check(gm != null && gm.Audio != null, "AudioManager presente en Play");
            if (gm == null || gm.Audio == null) yield break;

            var am = gm.Audio;
            Check(am.LoadedClipCount >= RequiredClips.Length,
                "Clips cargados desde Resources/Audio", "n=" + am.LoadedClipCount);

            var all = Resources.LoadAll<AudioClip>("Audio");
            var found = new HashSet<string>();
            for (int i = 0; i < all.Length; i++) if (all[i] != null) found.Add(all[i].name);

            int missing = 0;
            for (int i = 0; i < RequiredClips.Length; i++)
            {
                if (!found.Contains(RequiredClips[i])) { missing++; Debug.LogWarning("[D] Falta el clip: " + RequiredClips[i]); }
            }
            Check(missing == 0, "Los 15 clips requeridos existen", "faltan=" + missing + " de " + RequiredClips.Length);

            var sources = gm.GetComponents<AudioSource>();
            Check(sources.Length >= 11, "Pool de AudioSources creado", "n=" + sources.Length);

            // --- Reproducción real ---
            am.Play("sfx_shoot", 1f);
            yield return null;
            yield return null;
            int playing = 0;
            for (int i = 0; i < sources.Length; i++) if (sources[i].isPlaying) playing++;
            Check(playing > 0, "Un AudioSource suena en Play Mode", "playing=" + playing);

            // --- Volumen 0 % ---
            float previous = GameOptions.Volume;
            StopPool(sources);
            GameOptions.Volume = 0f;
            am.Play("sfx_select", 1f);
            yield return null;
            float volZero = PlayingVolume(sources, "sfx_select");
            Check(Mathf.Approximately(volZero, 0f), "Volumen 0% silencia los efectos", "vol=" + volZero.ToString("F2"));

            // --- Volumen 100 % ---
            StopPool(sources);
            GameOptions.Volume = 1f;
            am.Play("sfx_start", 1f);
            yield return null;
            float volFull = PlayingVolume(sources, "sfx_start");
            Check(volFull > 0.9f, "Volumen 100% reproduce al máximo", "vol=" + volFull.ToString("F2"));

            // --- Volumen intermedio ---
            StopPool(sources);
            GameOptions.Volume = 0.5f;
            am.Play("sfx_jump", 1f);
            yield return null;
            float volHalf = PlayingVolume(sources, "sfx_jump");
            Check(volHalf > 0.4f && volHalf < 0.6f, "Volumen 50% escala el efecto", "vol=" + volHalf.ToString("F2"));

            GameOptions.Volume = previous;
            GameOptions.Save();
            yield return null;

            // --- Música ---
            Check(am.CurrentMusic == "mus_level", "Música de nivel sonando en Level01", "pista=" + (am.CurrentMusic ?? "ninguna"));
            GameObject musicGo = null;
            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i].clip != null && sources[i].clip.name == "mus_level") { musicGo = sources[i].gameObject; break; }
            }
            Check(musicGo != null, "La pista mus_level está asignada a un AudioSource");

            // El volumen de la música responde al ajuste
            float beforeMusic = MusicVolume(sources);
            GameOptions.Volume = 0f;
            GameOptions.Notify();
            yield return null;
            float mutedMusic = MusicVolume(sources);
            GameOptions.Volume = previous;
            GameOptions.Notify();
            yield return null;
            float restoredMusic = MusicVolume(sources);
            Check(beforeMusic > 0f && mutedMusic == 0f && restoredMusic > 0f,
                "La música respeta el volumen de Opciones",
                "antes=" + beforeMusic.ToString("F2") + " mute=" + mutedMusic.ToString("F2") + " despues=" + restoredMusic.ToString("F2"));
        }

        /// <summary>Detiene el pool de efectos sin tocar la fuente de música.</summary>
        private static void StopPool(AudioSource[] sources)
        {
            for (int i = 0; i < sources.Length; i++)
            {
                var s = sources[i];
                if (s == null) continue;
                if (s.clip != null && s.clip.name.StartsWith("mus_")) continue;
                s.Stop();
            }
        }

        /// <summary>Volumen de la única fuente que está reproduciendo ese clip (tras vaciar el pool).</summary>
        private static float PlayingVolume(AudioSource[] sources, string clipName)
        {
            for (int i = 0; i < sources.Length; i++)
            {
                var s = sources[i];
                if (s != null && s.isPlaying && s.clip != null && s.clip.name == clipName) return s.volume;
            }
            return -1f;
        }

        private static float MusicVolume(AudioSource[] sources)
        {
            for (int i = 0; i < sources.Length; i++)
                if (sources[i].clip != null && sources[i].clip.name.StartsWith("mus_")) return sources[i].volume;
            return -1f;
        }

        // ==================================================================
        //  TAREA 2 - ENEMIGOS
        // ==================================================================
        private IEnumerator PhaseEnemies()
        {
            var lm = LevelManager.Instance;
            if (lm == null || lm.Players.Count == 0) { Check(false, "Fase enemigos: nivel con jugador"); yield break; }
            var p1 = lm.Players[0];

            var gunner = Find<ShooterEnemy>();
            Check(gunner != null, "Gunner (ShooterEnemy) presente en el nivel");
            if (gunner != null)
            {
                ClearProjectiles();
                p1.transform.position = new Vector3(gunner.transform.position.x + 2.2f, gunner.transform.position.y + 0.2f, 0f);
                var rb = p1.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = Vector2.zero;
                p1.ClearInvulnerability();

                yield return new WaitForSecondsRealtime(0.4f);

                yield return SampleMaxProjectiles(3.5f, Faction.Enemy);
                Check(sampleMax > 0, "ShooterEnemy detecta al jugador y dispara",
                    "proyectiles=" + sampleMax + " (máximo por frame en 3,50s)");

                // --- El proyectil enemigo produce daño ---
                yield return new WaitForSecondsRealtime(0.2f);
                p1.ClearInvulnerability();
                int hpBefore = p1.health.Current;
                float waitHit = 0f;
                while (waitHit < 2.5f && p1.health.Current >= hpBefore)
                {
                    waitHit += Time.unscaledDeltaTime;
                    yield return null;
                }
                int hpAfter = p1.health.Current;

                if (hpAfter >= hpBefore)
                {
                    // Refuerzo determinista: bala enemiga lanzada en suelo despejado contra el jugador
                    ClearProjectiles();
                    Vector3 stand;
                    bool found = FindClearStand(gunner.transform.position, out stand);
                    Check(found, "Hay un punto despejado para el impacto forzado",
                        stand.ToString("F1"));
                    if (found)
                    {
                        p1.transform.position = stand;
                        var rbClear = p1.GetComponent<Rigidbody2D>();
                        if (rbClear != null) rbClear.linearVelocity = Vector2.zero;
                        yield return new WaitForFixedUpdate();
                        p1.ClearInvulnerability();
                        hpBefore = p1.health.Current;

                        var shot = Object.Instantiate(gunner.bulletPrefab,
                            stand + new Vector3(-1.4f, 0.9f, 0f), Quaternion.identity);
                        shot.Launch(Vector2.right, 12, Faction.Enemy, -1);
                        yield return new WaitForSecondsRealtime(0.6f);
                        hpAfter = p1.health.Current;
                        Check(hpAfter < hpBefore, "Proyectil enemigo daña al jugador (impacto forzado)",
                            hpBefore + " -> " + hpAfter + " en " + stand.ToString("F1"));
                    }
                }
                else
                {
                    Check(true, "Proyectil enemigo daña al jugador (impacto natural)",
                        hpBefore + " -> " + hpAfter + " en " + waitHit.ToString("F2") + "s");
                }
                p1.ClearInvulnerability();

                // --- Disparo hacia la izquierda: el cañón debe salir por el lado del jugador ---
                ClearProjectiles();
                p1.transform.position = new Vector3(gunner.transform.position.x - 2.2f, gunner.transform.position.y + 0.2f, 0f);
                var rbLeft = p1.GetComponent<Rigidbody2D>();
                if (rbLeft != null) rbLeft.linearVelocity = Vector2.zero;
                p1.ClearInvulnerability();
                yield return new WaitForSecondsRealtime(0.4f);

                yield return SampleMaxProjectiles(3f, Faction.Enemy);
                Check(sampleMax > 0, "ShooterEnemy dispara también hacia la izquierda",
                    "proyectiles=" + sampleMax);
                ClearProjectiles();
            }

            // --- PatrolEnemy gira en el borde ---
            var patrol = Find<PatrolEnemy>();
            Check(patrol != null, "Raider (PatrolEnemy) presente en el nivel");
            if (patrol != null)
            {
                var ledge = FindLedge();
                if (ledge != null)
                {
                    float top = ledge.bounds.max.y;
                    float edgeX = ledge.bounds.max.x;
                    patrol.transform.position = new Vector3(edgeX - 1.0f, top + 0.7f, 0f);
                    var prb = patrol.GetComponent<Rigidbody2D>();
                    if (prb != null) prb.linearVelocity = Vector2.zero;

                    yield return new WaitForSecondsRealtime(0.8f);
                    bool flip0 = patrol.spriteRenderer != null && patrol.spriteRenderer.flipX;

                    float t = 0f;
                    bool flipped = false;
                    while (t < 2.5f)
                    {
                        bool f = patrol.spriteRenderer != null && patrol.spriteRenderer.flipX;
                        if (f != flip0) { flipped = true; break; }
                        t += Time.unscaledDeltaTime;
                        yield return null;
                    }

                    Check(flipped, "PatrolEnemy cambia de dirección en el límite de la plataforma",
                        "borde x=" + edgeX.ToString("F1") + " en " + t.ToString("F2") + "s");
                    Check(patrol != null && patrol.transform.position.y > top - 0.8f,
                        "PatrolEnemy no se cae del borde",
                        patrol != null ? "y=" + patrol.transform.position.y.ToString("F2") + " borde=" + top.ToString("F2") : "destruido");
                }
                else
                {
                    Check(false, "No se encontró plataforma elevada para probar el giro en el borde");
                }

                // --- Daño y muerte ---
                if (patrol != null)
                {
                    int scoreBefore = GameSession.Score;
                    int hp0 = patrol.health.Current;
                    patrol.health.TakeDamage(new DamageInfo
                    {
                        amount = 10,
                        point = patrol.transform.position,
                        direction = Vector2.right,
                        source = gameObject,
                        playerIndex = 0
                    });
                    Check(patrol.health.Current == hp0 - 10, "El enemigo recibe daño", hp0 + " -> " + patrol.health.Current);

                    patrol.health.TakeDamage(new DamageInfo
                    {
                        amount = 999,
                        point = patrol.transform.position,
                        direction = Vector2.right,
                        source = gameObject,
                        playerIndex = 0
                    });

                    Check(!patrol.health.IsAlive, "El enemigo muere al agotar su salud", "hp=" + patrol.health.Current);
                    Check(patrol.IsDead, "EnemyBase marca la muerte");
                    Check(GameSession.Score >= scoreBefore + 100, "La puntuación sube al morir el enemigo",
                        scoreBefore + " -> " + GameSession.Score);

                    yield return new WaitForSecondsRealtime(1.5f);
                    Check(patrol == null, "El enemigo se destruye tras la animación de muerte");
                }
            }
        }

        /// <summary>Plataforma elevada y ancha del nivel (borde derecho para la prueba de giro).</summary>
        private static BoxCollider2D FindLedge()
        {
            BoxCollider2D best = null;
            var all = Object.FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                var c = all[i];
                if (c == null || c.isTrigger) continue;
                if (c.gameObject.layer != 8) continue;                 // Ground
                if (c.bounds.max.y < 1.2f) continue;                   // elevada
                if (c.bounds.size.x < 4f) continue;                    // suficientemente larga
                if (best == null || c.bounds.size.x > best.bounds.size.x) best = c;
            }
            return best;
        }

        // ==================================================================
        //  TAREA 2b - DAÑO POR CONTACTO
        // ==================================================================
        private IEnumerator PhaseDamageOnTouch()
        {
            var lm = LevelManager.Instance;
            if (lm == null || lm.Players.Count == 0) { Check(false, "Fase contacto: jugador disponible"); yield break; }
            var p1 = lm.Players[0];
            if (p1.IsDead) { Check(false, "Fase contacto: el jugador está muerto"); yield break; }

            var raider = Find<PatrolEnemy>();
            Check(raider != null, "Raider disponible para la prueba de contacto");
            if (raider == null) yield break;

            var hit = raider.transform.Find("HitBox");
            Check(hit != null && hit.GetComponent<DamageOnTouch>() != null, "El HitBox tiene DamageOnTouch válido");

            p1.health.ResetHealth();
            p1.ClearInvulnerability();
            p1.transform.position = hit.position + new Vector3(0.62f, -0.74f, 0f);
            var rb = p1.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;

            int hp0 = p1.health.Current;
            float t = 0f;
            while (t < 1.6f && p1.health.Current >= hp0)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            Check(p1.health.Current < hp0, "DamageOnTouch daña por contacto",
                hp0 + " -> " + p1.health.Current + " en " + t.ToString("F2") + "s");
            p1.ClearInvulnerability();
        }

        // ==================================================================
        //  TAREA 3 - REAPARICIÓN
        // ==================================================================
        private IEnumerator PhaseRespawn()
        {
            var lm = LevelManager.Instance;
            if (lm == null || lm.Players.Count == 0) { Check(false, "Fase reaparición: nivel con jugador"); yield break; }
            var p1 = lm.Players[0];

            // Checkpoint real (por trigger)
            var checkpoint = Find<Checkpoint>();
            Check(checkpoint != null, "Checkpoint presente en el nivel");
            if (checkpoint != null)
            {
                p1.health.ResetHealth();
                p1.ClearInvulnerability();
                p1.transform.position = checkpoint.transform.position;
                var rb = p1.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = Vector2.zero;
                yield return new WaitForSecondsRealtime(0.4f);
                Check(Mathf.Abs(lm.checkpointPosition.x - checkpoint.transform.position.x) < 2.5f,
                    "El checkpoint actualiza el punto de reaparición",
                    "checkpoint x=" + checkpoint.transform.position.x.ToString("F1") +
                    " guardado x=" + lm.checkpointPosition.x.ToString("F1"));
            }

            // Salud
            p1.ClearInvulnerability();
            int hp0 = p1.health.Current;
            p1.health.TakeDamage(new DamageInfo
            {
                amount = 30, point = p1.transform.position, direction = Vector2.right,
                source = gameObject, playerIndex = -1
            });
            Check(p1.health.Current == hp0 - 30, "El jugador pierde salud al recibir daño", hp0 + " -> " + p1.health.Current);
            Check(p1.health.invulnerable, "El daño activa la invulnerabilidad temporal");

            // Muerte y vida
            int livesBefore = GameSession.GetLives(0);
            Vector3 cp = lm.checkpointPosition;
            p1.ClearInvulnerability();
            p1.health.TakeDamage(new DamageInfo
            {
                amount = 999, point = p1.transform.position, direction = Vector2.right,
                source = gameObject, playerIndex = -1
            });
            Check(p1.IsDead, "El jugador muere al agotar su salud");

            yield return new WaitForSecondsRealtime(0.3f);
            Check(GameSession.GetLives(0) == livesBefore - 1, "Al morir se pierde una vida",
                livesBefore + " -> " + GameSession.GetLives(0));

            yield return new WaitForSecondsRealtime(1.6f);
            Check(!p1.IsDead, "El jugador reaparece automáticamente");
            Check(Vector2.Distance(p1.transform.position, cp) < 1.5f, "Reaparece en el último checkpoint",
                p1.transform.position.ToString("F1") + " vs " + cp.ToString("F1"));
            Check(p1.health.Current == p1.health.maxHealth, "Reaparece con la salud completa",
                p1.health.Current + "/" + p1.health.maxHealth);
            Check(p1.health.invulnerable, "Reaparece con invulnerabilidad temporal");

            yield return new WaitForSecondsRealtime(2.8f);
            Check(!p1.health.invulnerable, "La invulnerabilidad de reaparición caduca");

            p1.ClearInvulnerability();
            int hpBefore = p1.health.Current;
            p1.health.TakeDamage(new DamageInfo
            {
                amount = 5, point = p1.transform.position, direction = Vector2.right,
                source = gameObject, playerIndex = -1
            });
            Check(p1.health.Current == hpBefore - 5, "Tras caducar la invulnerabilidad vuelve a recibir daño",
                hpBefore + " -> " + p1.health.Current);
        }

        // ==================================================================
        //  TAREA 3b - GAME OVER
        // ==================================================================
        private IEnumerator PhaseGameOver()
        {
            var lm = LevelManager.Instance;
            if (lm == null || lm.Players.Count == 0) { Check(false, "Fase game over: nivel con jugador"); yield break; }
            var p1 = lm.Players[0];

            GameSession.SetLives(0, 1);
            p1.ClearInvulnerability();
            if (!p1.IsDead)
            {
                p1.health.TakeDamage(new DamageInfo
                {
                    amount = 999, point = p1.transform.position, direction = Vector2.right,
                    source = gameObject, playerIndex = -1
                });
            }

            float t = 0f;
            while (t < 3f && !lm.GameOver)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            Check(lm.GameOver, "La partida termina al quedarse sin vidas", "en " + t.ToString("F2") + "s");
            Check(Mathf.Approximately(Time.timeScale, 0f), "El game over detiene el juego",
                "timeScale=" + Time.timeScale.ToString("F2"));
            Check(GameSession.GetLives(0) == 0, "Vidas a cero", "vidas=" + GameSession.GetLives(0));

            if (GameManager.Instance != null) GameManager.Instance.SetPaused(false);
            yield return new WaitForSecondsRealtime(0.2f);
        }

        // ==================================================================
        //  TAREA 4 - LOS CUATRO PERSONAJES
        // ==================================================================
        private IEnumerator PhaseCharacters()
        {
            var lm = LevelManager.Instance;
            if (lm == null) { Check(false, "Fase personajes: LevelManager disponible"); yield break; }

            var textures = new HashSet<int>();
            var ids = new CharacterId[] { CharacterId.Danner, CharacterId.Cesar, CharacterId.Sebastian, CharacterId.Ines };

            for (int i = 0; i < ids.Length; i++)
            {
                var id = ids[i];
                string label = CharacterDb.GetName(id);

                var prefab = Resources.Load<GameObject>("Characters/Player_" + id);
                if (prefab == null) { Check(false, label + ": prefab existe"); continue; }

                var go = Object.Instantiate(prefab, new Vector3(8f, 1.6f, 0f), Quaternion.identity);
                go.name = "Test_" + id;

                var ctrl = go.GetComponent<PlayerController2D>();
                var shooter = go.GetComponent<PlayerShooter>();
                var sprite = ctrl != null ? ctrl.spriteRenderer : null;
                var anim = ctrl != null ? ctrl.animator : null;

                Check(ctrl != null && shooter != null && sprite != null && anim != null,
                    label + ": prefab con controlador, arma, sprite y animador");

                // Sprites propios
                bool spriteOk = sprite != null && sprite.sprite != null;
                string spriteName = spriteOk ? sprite.sprite.name : "NULL";
                Check(spriteOk && spriteName.ToLower().Contains(id.ToString().ToLower()),
                    label + ": sprite propio del personaje", spriteName);
                if (spriteOk) textures.Add(sprite.sprite.texture.GetInstanceID());

                // Animator propio
                string ctrlName = anim != null && anim.runtimeAnimatorController != null
                    ? anim.runtimeAnimatorController.name : "NULL";
                Check(anim != null && anim.runtimeAnimatorController != null &&
                      ctrlName.ToLower().Contains(id.ToString().ToLower()),
                    label + ": AnimatorController propio", ctrlName);

                // Controles reales
                ctrl.Setup(0, lm.inputAsset);
                ctrl.ClearInvulnerability();

                yield return new WaitForSecondsRealtime(0.25f);

                ReleaseAll();
                Press(Key.D);
                float maxVx = 0f;
                float t = 0f;
                while (t < 0.7f)
                {
                    var rb = go.GetComponent<Rigidbody2D>();
                    if (rb != null) maxVx = Mathf.Max(maxVx, Mathf.Abs(rb.linearVelocity.x));
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
                ReleaseAll();
                Check(maxVx > 3f, label + ": se mueve con D", "max |vx| = " + maxVx.ToString("F2"));

                yield return new WaitForSecondsRealtime(0.25f);

                // Salto
                float y0 = go.transform.position.y;
                float peak = y0;
                Press(Key.K);
                t = 0f;
                while (t < 0.9f)
                {
                    peak = Mathf.Max(peak, go.transform.position.y);
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
                ReleaseAll();
                Check(peak > y0 + 0.6f, label + ": salta con K", "y " + y0.ToString("F2") + " -> " + peak.ToString("F2"));

                yield return new WaitForSecondsRealtime(0.4f);

                // Disparo
                ClearProjectiles();
                Press(Key.J);
                yield return SampleMaxProjectiles(0.45f, Faction.Player, 0);
                ReleaseAll();
                Check(sampleMax > 0, label + ": dispara con J", "proyectiles=" + sampleMax + " (máximo por frame)");

                // Disparo mirando a la izquierda: el proyectil debe nacer de ese lado
                yield return new WaitForSecondsRealtime(0.2f);
                Press(Key.A);
                yield return new WaitForSecondsRealtime(0.15f);
                ReleaseAll();
                yield return new WaitForSecondsRealtime(0.2f);

                ClearProjectiles();
                Press(Key.J);
                yield return SampleProjectilesSide(0.35f, Faction.Player, 0, go.transform);
                ReleaseAll();
                Check(sampleMax > 0 && sampleFirstX < samplePlayerX - 0.25f,
                    label + ": el disparo sale por el lado al que mira",
                    "proyectil.x=" + sampleFirstX.ToString("F2") + " jugador.x=" + samplePlayerX.ToString("F2"));

                // Bomba
                int bombs0 = shooter.Bombs;
                Press(Key.L);
                yield return new WaitForSecondsRealtime(0.2f);
                ReleaseAll();
                Check(shooter.Bombs == bombs0 - 1, label + ": lanza bomba con L",
                    bombs0 + " -> " + shooter.Bombs);

                ClearProjectiles();
                Object.Destroy(go);
                yield return new WaitForSecondsRealtime(0.25f);
            }

            Check(textures.Count == 4, "Los cuatro personajes usan sprites distintos", "texturas=" + textures.Count);
        }

        // ==================================================================
        //  TAREA 5 - DOS JUGADORES
        // ==================================================================
        private IEnumerator PhaseTwoPlayers()
        {
            var gm = GameManager.Instance;
            if (gm == null) { Check(false, "Fase 2P: GameManager disponible"); yield break; }
            if (gm.IsPaused) gm.SetPaused(false);

            GameSession.StartNewRun(2);
            GameSession.SetCharacter(0, CharacterId.Danner);
            GameSession.SetCharacter(1, CharacterId.Ines);

            gm.LoadScene(SceneNames.Level01);
            yield return new WaitForSecondsRealtime(2.6f);

            var lm = LevelManager.Instance;
            Check(lm != null && lm.Players.Count == 2, "Dos jugadores instanciados",
                lm != null ? "n=" + lm.Players.Count : "sin LevelManager");
            if (lm == null || lm.Players.Count < 2) yield break;

            var p1 = lm.Players[0];
            var p2 = lm.Players[1];
            Check(p1.name.Contains("Danner") && p2.name.Contains("Ines"),
                "Cada jugador usa su personaje elegido", p1.name + " / " + p2.name);

            p1.ClearInvulnerability();
            p2.ClearInvulnerability();
            yield return new WaitForSecondsRealtime(0.3f);

            // --- Controles independientes: P1 derecha, P2 izquierda ---
            float maxV1 = 0f, maxV2 = 0f;
            Press(Key.D, Key.LeftArrow);
            float t = 0f;
            while (t < 0.8f)
            {
                var r1 = p1.GetComponent<Rigidbody2D>();
                var r2 = p2.GetComponent<Rigidbody2D>();
                if (r1 != null) maxV1 = Mathf.Max(maxV1, r1.linearVelocity.x);
                if (r2 != null) maxV2 = Mathf.Min(maxV2, r2.linearVelocity.x);
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            ReleaseAll();
            Check(maxV1 > 3f, "J1 avanza con D", "vx=" + maxV1.ToString("F2"));
            Check(maxV2 < -3f, "J2 retrocede con flecha izquierda", "vx=" + maxV2.ToString("F2"));

            yield return new WaitForSecondsRealtime(0.3f);

            // --- Disparos simultáneos ---
            ClearProjectiles();
            Press(Key.J, Key.RightCtrl);
            yield return SampleMaxTwoPlayers(0.5f);
            ReleaseAll();
            Check(sampleMaxA > 0 && sampleMaxB > 0, "Ambos jugadores disparan a la vez",
                "J1=" + sampleMaxA + " J2=" + sampleMaxB + " (máximo por frame)");

            // --- Bombas individuales ---
            int bombs1 = p1.shooter.Bombs;
            int bombs2 = p2.shooter.Bombs;
            Press(Key.L);
            yield return new WaitForSecondsRealtime(0.2f);
            ReleaseAll();
            Check(p1.shooter.Bombs == bombs1 - 1 && p2.shooter.Bombs == bombs2,
                "La bomba de J1 no consume las de J2",
                "J1 " + bombs1 + "->" + p1.shooter.Bombs + " J2 " + bombs2 + "->" + p2.shooter.Bombs);

            Press(Key.Enter);
            yield return new WaitForSecondsRealtime(0.2f);
            ReleaseAll();
            Check(p2.shooter.Bombs == bombs2 - 1, "J2 lanza su propia bomba con Enter",
                bombs2 + " -> " + p2.shooter.Bombs);

            // --- Salud independiente ---
            int hp1 = p1.health.Current;
            int hp2 = p2.health.Current;
            p1.ClearInvulnerability();
            p1.health.TakeDamage(new DamageInfo
            {
                amount = 20, point = p1.transform.position, direction = Vector2.right,
                source = gameObject, playerIndex = 1
            });
            Check(p1.health.Current == hp1 - 20 && p2.health.Current == hp2,
                "La salud es independiente por jugador",
                "J1 " + hp1 + "->" + p1.health.Current + " J2 " + hp2 + "->" + p2.health.Current);

            // --- Cámara compartida, separación y zoom ---
            var cam = lm.cameraFollow;
            Check(cam != null && cam.targetA != null && cam.targetB != null,
                "La cámara sigue a los dos jugadores");
            if (cam != null)
            {
                p1.transform.position = new Vector3(40f, 1.6f, 0f);
                p2.transform.position = new Vector3(44f, 1.6f, 0f);
                yield return new WaitForSecondsRealtime(1.2f);
                float closeSize = cam.GetComponent<Camera>().orthographicSize;

                p2.transform.position = new Vector3(52f, 1.6f, 0f);
                yield return new WaitForSecondsRealtime(1.4f);
                float farSize = cam.GetComponent<Camera>().orthographicSize;

                Check(farSize > closeSize + 0.05f, "La cámara hace zoom al separarse los jugadores",
                    "juntos=" + closeSize.ToString("F2") + " separados=" + farSize.ToString("F2"));

                float camX = cam.transform.position.x;
                float mid = (p1.transform.position.x + p2.transform.position.x) * 0.5f;
                Check(Mathf.Abs(camX - mid) < 7f, "La cámara se mantiene entre los dos jugadores",
                    "camX=" + camX.ToString("F1") + " medio=" + mid.ToString("F1"));
            }

            // --- Muerte de uno de los dos ---
            int lives1 = GameSession.GetLives(0);
            int lives2 = GameSession.GetLives(1);
            p1.ClearInvulnerability();
            p1.health.TakeDamage(new DamageInfo
            {
                amount = 999, point = p1.transform.position, direction = Vector2.right,
                source = gameObject, playerIndex = 1
            });
            yield return new WaitForSecondsRealtime(2.2f);

            Check(GameSession.GetLives(0) == lives1 - 1 && GameSession.GetLives(1) == lives2,
                "Al morir J1 solo pierde vidas J1",
                "J1 " + lives1 + "->" + GameSession.GetLives(0) + " J2 " + lives2 + "->" + GameSession.GetLives(1));
            Check(!p1.IsDead, "J1 reaparece y la partida continúa");
            Check(!lm.GameOver, "La partida no termina si queda el otro jugador");

            // J2 sigue respondiendo
            ReleaseAll();
            float maxV2b = 0f;
            Press(Key.RightArrow);
            t = 0f;
            while (t < 0.7f)
            {
                var r2 = p2.GetComponent<Rigidbody2D>();
                if (r2 != null) maxV2b = Mathf.Max(maxV2b, r2.linearVelocity.x);
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            ReleaseAll();
            Check(maxV2b > 3f, "J2 sigue controlando tras la muerte de J1", "vx=" + maxV2b.ToString("F2"));
        }
    }
}
#endif
