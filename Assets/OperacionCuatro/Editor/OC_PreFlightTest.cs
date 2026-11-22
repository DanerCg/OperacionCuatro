#if UNITY_EDITOR
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OperacionCuatro.EditorTools
{
    /// <summary>
    /// Comprobaciones previas a la Fase 2:
    ///  1) GameSession se reinicia al comenzar una partida nueva, también con la recarga de
    ///     dominio desactivada (los estáticos sucios antes de entrar en Play deben quedar limpios).
    ///  2) REINTENTAR conserva el modo y los personajes elegidos y solo reinicia lo que toca.
    ///  3) El HUD muestra "P1 nombre" / "P2 nombre" con el personaje seleccionado y los
    ///     retratos pixel art correspondientes (no el cuadrado de color provisional).
    ///
    /// Precondición: abrir MainMenu y pulsar Play; después ejecutar
    /// OC_PreFlightTest.Run() por reflexión (menú OC/Playtest/Previas Fase 2 (sesion + HUD)).
    /// </summary>
    public static class OC_PreFlightTest
    {
        [UnityEditor.MenuItem("OC/Playtest/Previas Fase 2 (sesion + HUD)")]
        public static void Run()
        {
            var go = new GameObject("[OC_PreFlight]");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<OCPreFlightRunner>();
        }
    }

    public class OCPreFlightRunner : MonoBehaviour
    {
        private int passed;
        private int failed;

        private void Check(bool ok, string label, string detail = "")
        {
            if (ok)
            {
                passed++;
                Debug.Log("[H] PASS  " + label + (string.IsNullOrEmpty(detail) ? "" : "  | " + detail));
            }
            else
            {
                failed++;
                Debug.LogError("[H] FAIL  " + label + (string.IsNullOrEmpty(detail) ? "" : "  | " + detail));
            }
        }

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(0.7f);

            // ---------------------------------------------------------------
            // 0. El reinicio de Play ya debe haber limpiado los estáticos
            //    (antes de entrar en Play se dejaron deliberadamente sucios).
            // ---------------------------------------------------------------
            Check(SceneManager.GetActiveScene().name == SceneNames.MainMenu,
                "Arranque en MainMenu", SceneManager.GetActiveScene().name);

            Check(GameSession.PlayerCount == 1, "Arranque de Play: PlayerCount a 1", GameSession.PlayerCount.ToString());
            Check(GameSession.P1 == CharacterId.Danner && GameSession.P2 == CharacterId.Cesar,
                "Arranque de Play: personajes por defecto", GameSession.P1 + " / " + GameSession.P2);
            Check(GameSession.Score == 0 && GameSession.KillsP1 == 0 && GameSession.KillsP2 == 0,
                "Arranque de Play: puntuación y bajas a cero",
                GameSession.Score + " | " + GameSession.KillsP1 + "/" + GameSession.KillsP2);
            Check(GameSession.LivesP1 == GameSession.StartingLives && GameSession.LivesP2 == GameSession.StartingLives,
                "Arranque de Play: vidas restablecidas", GameSession.LivesP1 + "/" + GameSession.LivesP2);
            Check(!GameSession.MissionComplete, "Arranque de Play: misión no completada");

            // ---------------------------------------------------------------
            // 1. Partida nueva desde el menú con datos sucios
            // ---------------------------------------------------------------
            GameSession.PlayerCount = 2;
            GameSession.SetCharacter(0, CharacterId.Ines);
            GameSession.SetCharacter(1, CharacterId.Sebastian);
            GameSession.Score = 999;
            GameSession.KillsP1 = 7;
            GameSession.KillsP2 = 6;
            GameSession.SetLives(0, 0);
            GameSession.SetLives(1, 1);
            GameSession.MissionComplete = true;

            var menu = Object.FindFirstObjectByType<MainMenuUI>();
            Check(menu != null, "MainMenuUI localizado");
            if (menu == null) { Finish(); yield break; }

            if (menu.twoPlayersButton != null) menu.twoPlayersButton.onClick.Invoke();
            yield return new WaitForSeconds(2.0f);

            Check(SceneManager.GetActiveScene().name == SceneNames.CharacterSelection,
                "Menú -> selección de personajes", SceneManager.GetActiveScene().name);
            Check(GameSession.PlayerCount == 2, "Partida nueva: se conserva el modo 2 jugadores", GameSession.PlayerCount.ToString());
            Check(GameSession.Score == 0, "Partida nueva: puntuación a cero", GameSession.Score.ToString());
            Check(GameSession.KillsP1 == 0 && GameSession.KillsP2 == 0,
                "Partida nueva: bajas a cero", GameSession.KillsP1 + "/" + GameSession.KillsP2);
            Check(GameSession.LivesP1 == GameSession.StartingLives && GameSession.LivesP2 == GameSession.StartingLives,
                "Partida nueva: vidas restablecidas", GameSession.LivesP1 + "/" + GameSession.LivesP2);
            Check(!GameSession.MissionComplete, "Partida nueva: misión no completada");
            Check(GameSession.P1 == CharacterId.Danner && GameSession.P2 == CharacterId.Cesar,
                "Partida nueva: selección restablecida", GameSession.P1 + " / " + GameSession.P2);

            // ---------------------------------------------------------------
            // 2. Elegir DANNER (J1) e INÉS (J2) y entrar al nivel
            // ---------------------------------------------------------------
            var sel = Object.FindFirstObjectByType<CharacterSelectUI>();
            Check(sel != null, "CharacterSelectUI localizado");

            if (sel != null && sel.characterButtons != null && sel.characterButtons.Length >= 4)
            {
                if (sel.characterButtons[0] != null) sel.characterButtons[0].onClick.Invoke();
                yield return new WaitForSeconds(0.25f);
                if (sel.characterButtons[3] != null) sel.characterButtons[3].onClick.Invoke();
                yield return new WaitForSeconds(0.25f);
            }

            Check(GameSession.P1 == CharacterId.Danner && GameSession.P2 == CharacterId.Ines,
                "Selección aplicada: DANNER / INÉS", GameSession.P1 + " / " + GameSession.P2);

            if (sel != null && sel.startButton != null) sel.startButton.onClick.Invoke();
            yield return new WaitForSeconds(2.8f);

            Check(SceneManager.GetActiveScene().name == SceneNames.Level01, "En Level01", SceneManager.GetActiveScene().name);

            var hud = Object.FindFirstObjectByType<LevelHUD>();
            Check(hud != null, "LevelHUD localizado");
            if (hud == null) { Finish(); yield break; }

            // ---------------------------------------------------------------
            // 3. HUD: etiquetas P1/P2 con nombre y retratos pixel art
            // ---------------------------------------------------------------
            string p1Label = hud.panelP1 != null && hud.panelP1.nameText != null ? hud.panelP1.nameText.text : "(sin texto)";
            string p2Label = hud.panelP2 != null && hud.panelP2.nameText != null ? hud.panelP2.nameText.text : "(sin texto)";

            Check(p1Label == "P1 DANNER", "HUD J1: 'P1 DANNER'", p1Label);
            Check(hud.panelP2 != null && hud.panelP2.root != null && hud.panelP2.root.activeSelf,
                "HUD J2: panel visible en cooperativo");
            Check(p2Label == "P2 INÉS", "HUD J2: 'P2 INÉS'", p2Label);

            var spriteP1 = hud.panelP1 != null && hud.panelP1.portrait != null ? hud.panelP1.portrait.sprite : null;
            var spriteP2 = hud.panelP2 != null && hud.panelP2.portrait != null ? hud.panelP2.portrait.sprite : null;

            Check(spriteP1 != null, "Retrato J1 con imagen", spriteP1 != null ? spriteP1.name : "sin sprite");
            Check(spriteP2 != null, "Retrato J2 con imagen", spriteP2 != null ? spriteP2.name : "sin sprite");
            Check(spriteP1 != null && spriteP1.name == "Portrait_Danner", "Retrato J1 corresponde a DANNER",
                spriteP1 != null ? spriteP1.name : "sin sprite");
            Check(spriteP2 != null && spriteP2.name == "Portrait_Ines", "Retrato J2 corresponde a INÉS",
                spriteP2 != null ? spriteP2.name : "(sin sprite)");
            Check(spriteP1 != null && spriteP2 != null && spriteP1 != spriteP2, "Retratos distintos entre jugadores");
            Check(spriteP1 != null && spriteP1.texture != null && spriteP1.texture.width == 84 && spriteP1.texture.height == 84,
                "Retrato J1 a 84x84 px", spriteP1 != null && spriteP1.texture != null ? spriteP1.texture.width + "x" + spriteP1.texture.height : "n/d");
            Check(hud.panelP1 != null && hud.panelP1.portrait != null && hud.panelP1.portrait.color == Color.white,
                "Retrato J1 sin tinte de color provisional");
            Check(hud.panelP1 != null && hud.panelP1.slotText != null && hud.panelP1.slotText.text == "P1"
                  && hud.panelP2 != null && hud.panelP2.slotText != null && hud.panelP2.slotText.text == "P2",
                "Marca de ranura P1/P2 en los paneles");

            // ---------------------------------------------------------------
            // 4. REINTENTAR: conserva modo y personajes, reinicia la partida
            // ---------------------------------------------------------------
            GameSession.Score = 1234;
            GameSession.KillsP1 = 5;
            GameSession.KillsP2 = 4;
            GameSession.SetLives(0, 0);
            GameSession.MissionComplete = true;

            if (GameManager.Instance != null) GameManager.Instance.LoadScene(SceneNames.Results);
            else SceneManager.LoadScene(SceneNames.Results);
            yield return new WaitForSeconds(2.6f);

            var results = Object.FindFirstObjectByType<ResultsUI>();
            Check(results != null, "ResultsUI localizado");
            if (results == null) { Finish(); yield break; }

            results.OnRetry();
            yield return new WaitForSecondsRealtime(3.0f);

            Check(SceneManager.GetActiveScene().name == SceneNames.Level01,
                "REINTENTAR recarga Level01", SceneManager.GetActiveScene().name);
            Check(GameSession.PlayerCount == 2, "REINTENTAR conserva 2 jugadores", GameSession.PlayerCount.ToString());
            Check(GameSession.P1 == CharacterId.Danner && GameSession.P2 == CharacterId.Ines,
                "REINTENTAR conserva los personajes elegidos", GameSession.P1 + " / " + GameSession.P2);
            Check(GameSession.Score == 0 && GameSession.KillsP1 == 0 && GameSession.KillsP2 == 0,
                "REINTENTAR reinicia puntuación y bajas", GameSession.Score + " | " + GameSession.KillsP1 + "/" + GameSession.KillsP2);
            Check(GameSession.LivesP1 == GameSession.StartingLives && GameSession.LivesP2 == GameSession.StartingLives,
                "REINTENTAR restaura las vidas", GameSession.LivesP1 + "/" + GameSession.LivesP2);
            Check(!GameSession.MissionComplete, "REINTENTAR limpia el estado de misión");

            var hud2 = Object.FindFirstObjectByType<LevelHUD>();
            string p2After = hud2 != null && hud2.panelP2 != null && hud2.panelP2.nameText != null ? hud2.panelP2.nameText.text : "(sin texto)";
            Check(p2After == "P2 INÉS", "HUD J2 correcto tras REINTENTAR", p2After);

            Finish();
        }

        private void Finish()
        {
            Debug.Log("[H] RESULTADO PREVIAS FASE 2: " + passed + " PASS / " + failed + " FAIL de " + (passed + failed));
            if (failed > 0) Debug.LogError("[H] FALLOS: " + failed);
        }
    }
}
#endif
