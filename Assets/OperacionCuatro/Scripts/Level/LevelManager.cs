using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OperacionCuatro
{
    /// <summary>
    /// Orquestador del nivel: aparición de jugadores, vidas, reaparición,
    /// punto de control, fin de misión y game over.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        [Header("Configuración")]
        public InputActionAsset inputAsset;
        public CameraFollow2D cameraFollow;
        public LevelHUD hud;

        [Header("Aparición")]
        public Vector3[] spawnPositions = new Vector3[] { new Vector3(2f, 1f, 0f), new Vector3(1f, 1f, 0f) };
        public bool autoSpawnPlayers = true;

        [Header("Estado")]
        public Vector3 checkpointPosition = new Vector3(2f, 1f, 0f);
        public bool MissionComplete { get; private set; }
        public bool GameOver { get; private set; }

        public readonly List<PlayerController2D> Players = new List<PlayerController2D>();

        private readonly List<int> pendingRespawns = new List<int>();

        private void Awake()
        {
            Instance = this;
            if (hud == null) hud = FindFirstObjectByType<LevelHUD>();
        }

        private void Start()
        {
            if (autoSpawnPlayers) SpawnPlayers();

            if (GameManager.Instance != null && GameManager.Instance.Audio != null)
                GameManager.Instance.Audio.PlayMusic(MusicForCurrentLevel, 0.45f);
        }

        /// <summary>Música del nivel en curso (Level02 y Level03 tienen pista propia).</summary>
        private static string MusicForCurrentLevel
        {
            get
            {
                if (GameSession.CurrentLevel >= 3) return "mus_level03";
                return GameSession.CurrentLevel == 2 ? "mus_level02" : "mus_level";
            }
        }

        public void SpawnPlayers()
        {
            int count = Mathf.Clamp(GameSession.PlayerCount, 1, 2);

            for (int i = 0; i < count; i++)
            {
                var id = GameSession.GetCharacter(i);
                var prefab = Resources.Load<GameObject>("Characters/Player_" + id);
                if (prefab == null)
                {
                    Debug.LogError("[OC] No se encontró el prefab del personaje: Resources/Characters/Player_" + id);
                    continue;
                }

                Vector3 pos = spawnPositions.Length > i ? spawnPositions[i] : spawnPositions[0];
                var go = Instantiate(prefab, pos, Quaternion.identity);
                go.name = "Player" + (i + 1) + "_" + id;

                var controller = go.GetComponent<PlayerController2D>();
                if (controller == null)
                {
                    Debug.LogError("[OC] El prefab del personaje no tiene PlayerController2D.");
                    continue;
                }

                controller.Setup(i, inputAsset);
                controller.BindDamage();
                controller.ApplySpawnInvulnerability(1.5f);
                Players.Add(controller);
            }

            if (cameraFollow != null && Players.Count > 0)
            {
                cameraFollow.SetTargets(Players[0].transform, Players.Count > 1 ? Players[1].transform : null);
                cameraFollow.SnapToTarget();
            }

            if (hud != null)
            {
                hud.Bind(Players.Count > 0 ? Players[0] : null, Players.Count > 1 ? Players[1] : null);
                hud.SetPlayerCount(Players.Count);
            }

            if (Players.Count == 0)
                Debug.LogError("[OC] El nivel arrancó sin jugadores.");
        }

        public void OnPlayerKilled(PlayerController2D player)
        {
            if (player == null || MissionComplete) return;

            int index = player.playerIndex;
            int lives = GameSession.GetLives(index) - 1;
            GameSession.SetLives(index, lives);

            if (hud != null) hud.RefreshLives(index, lives);

            if (lives > 0 && !pendingRespawns.Contains(index))
            {
                StartCoroutine(RespawnRoutine(player, 1.4f));
            }
            else
            {
                StartCoroutine(CheckGameOverRoutine(1.8f));
            }
        }

        private IEnumerator RespawnRoutine(PlayerController2D player, float delay)
        {
            pendingRespawns.Add(player.playerIndex);
            yield return new WaitForSeconds(delay);

            if (MissionComplete || GameOver)
            {
                pendingRespawns.Remove(player.playerIndex);
                yield break;
            }

            if (player != null)
            {
                player.Revive(checkpointPosition);
                player.BindDamage();
                if (hud != null) hud.RefreshLives(player.playerIndex, GameSession.GetLives(player.playerIndex));
            }

            pendingRespawns.Remove(player.playerIndex);
        }

        private IEnumerator CheckGameOverRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (MissionComplete || GameOver) yield break;

            for (int i = 0; i < Players.Count; i++)
            {
                if (Players[i] != null && GameSession.GetLives(i) > 0) yield break;
            }

            GameOver = true;
            GameSession.MissionComplete = false;

            if (hud != null) hud.ShowGameOver();
            if (GameManager.Instance != null) GameManager.Instance.SetPaused(true);
        }

        /// <summary>
        /// Cierra la partida en Game Over sin pasar por la cuenta de vidas. Lo usa la secuencia
        /// de escape de Level03 cuando se agota el tiempo de evacuación.
        /// </summary>
        public void ForceGameOver()
        {
            if (MissionComplete || GameOver) return;

            GameOver = true;
            GameSession.MissionComplete = false;

            if (hud != null)
            {
                hud.ShowMessage("EVACUACIÓN FALLIDA");
                hud.ShowGameOver();
            }

            if (GameManager.Instance != null) GameManager.Instance.SetPaused(true);
        }

        public void SetCheckpoint(Vector3 position)
        {
            checkpointPosition = position;
            if (hud != null) hud.ShowMessage("PUNTO DE CONTROL");
        }

        public void CompleteMission()
        {
            if (MissionComplete || GameOver) return;
            MissionComplete = true;
            GameSession.MissionComplete = true;

            if (hud != null) hud.ShowMissionComplete();

            StartCoroutine(ResultsRoutine());
        }

        private IEnumerator ResultsRoutine()
        {
            yield return new WaitForSeconds(2.6f);
            if (GameManager.Instance != null) GameManager.Instance.SetPaused(false);
            if (GameManager.Instance != null) GameManager.Instance.LoadScene(SceneNames.Results);
            else UnityEngine.SceneManagement.SceneManager.LoadScene(SceneNames.Results);
        }

        public void RestartLevel()
        {
            if (GameManager.Instance != null)
            {
                GameSession.Score = 0;
                GameSession.KillsP1 = 0;
                GameSession.KillsP2 = 0;
                GameSession.LivesP1 = GameSession.StartingLives;
                GameSession.LivesP2 = GameSession.StartingLives;
                GameSession.MissionComplete = false;
                GameManager.Instance.SetPaused(false);
                // REINTENTAR reinicia el nivel en curso, no siempre Level01.
                GameManager.Instance.LoadScene(GameSession.GetLevelSceneName());
            }
        }

        public void BackToMainMenu()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetPaused(false);
                GameManager.Instance.LoadScene(SceneNames.MainMenu);
            }
        }

        public PlayerController2D GetPlayer(int index)
        {
            return index >= 0 && index < Players.Count ? Players[index] : null;
        }
    }
}
