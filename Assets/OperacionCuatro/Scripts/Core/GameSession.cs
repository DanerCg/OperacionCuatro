using UnityEngine;

namespace OperacionCuatro
{
    public enum CharacterId { Danner = 0, Cesar = 1, Sebastian = 2, Ines = 3 }

    public enum Faction { Player, Enemy }

    /// <summary>
    /// Datos de la partida que sobreviven a los cambios de escena.
    ///
    /// Semántica de los tres caminos de progresión (ver también ResultsUI):
    ///  - NUEVA PARTIDA  -> StartNewRun(playerCount): borra puntuación, bajas, vidas, misión
    ///                      y selección; deja CurrentLevel en 1.
    ///  - REINTENTAR     -> NO toca PlayerCount, personajes ni CurrentLevel; reinicia
    ///                      puntuación, bajas, vidas y MissionComplete, y recarga el nivel actual.
    ///  - SIGUIENTE MISIÓN -> conserva PlayerCount, personajes, puntuación, bajas y vidas;
    ///                      reinicia solo MissionComplete y avanza CurrentLevel una posición.
    /// </summary>
    public static class GameSession
    {
        public const int MaxPlayers = 2;
        public const int StartingLives = 3;

        /// <summary>Primer nivel jugable de la campaña.</summary>
        public const int FirstLevel = 1;

        /// <summary>Último nivel implementado hasta ahora (Fase 2: Level01 y Level02).</summary>
        public const int LastLevel = 2;

        /// <summary>Nivel en curso. Se conserva al REINTENTAR y avanza con SIGUIENTE MISIÓN.</summary>
        public static int CurrentLevel = FirstLevel;

        public static int PlayerCount = 1;
        public static CharacterId P1 = CharacterId.Danner;
        public static CharacterId P2 = CharacterId.Cesar;

        public static int Score;
        public static int KillsP1;
        public static int KillsP2;
        public static bool MissionComplete;

        public static int LivesP1 = StartingLives;
        public static int LivesP2 = StartingLives;

        /// <summary>
        /// Devuelve la sesión a los valores de arranque. Se usa al comenzar una partida nueva
        /// y al iniciar cada sesión de Play (ver ResetOnPlayStart), de modo que el estado no se
        /// herede cuando la recarga de dominio está desactivada.
        /// </summary>
        public static void ResetToDefaults()
        {
            CurrentLevel = FirstLevel;
            PlayerCount = 1;
            P1 = CharacterId.Danner;
            P2 = CharacterId.Cesar;
            Score = 0;
            KillsP1 = 0;
            KillsP2 = 0;
            MissionComplete = false;
            LivesP1 = StartingLives;
            LivesP2 = StartingLives;
        }

        /// <summary>
        /// Con la recarga de dominio desactivada los estáticos sobreviven entre sesiones de Play:
        /// este método se ejecuta en cada arranque de Play Mode y deja la sesión limpia.
        /// No se ejecuta al recargar escenas, así que REINTENTAR conserva lo que corresponda.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlayStart()
        {
            ResetToDefaults();
        }

        /// <summary>Partida nueva desde el menú: borra puntuación, bajas, vidas y selección.</summary>
        public static void StartNewRun(int playerCount)
        {
            ResetToDefaults();
            PlayerCount = Mathf.Clamp(playerCount, 1, MaxPlayers);
        }

        /// <summary>
        /// Fija el nivel que se va a jugar sin tocar jugadores, personajes, puntuación ni vidas.
        /// Se usa al salir de la selección de personajes (siempre empieza por Level01) y lo
        /// usa internamente SIGUIENTE MISIÓN a través de AdvanceLevel().
        /// </summary>
        public static void StartLevel(int level)
        {
            CurrentLevel = Mathf.Clamp(level, FirstLevel, LastLevel);
            MissionComplete = false;
        }

        /// <summary>
        /// SIGUIENTE MISIÓN: avanza un nivel conservando puntuación, bajas, vidas, número de
        /// jugadores y personajes. Devuelve false si ya no quedan niveles implementados.
        /// </summary>
        public static bool AdvanceLevel()
        {
            if (CurrentLevel >= LastLevel) return false;
            CurrentLevel++;
            MissionComplete = false;
            return true;
        }

        /// <summary>¿Queda un nivel posterior al actual?</summary>
        public static bool HasNextLevel { get { return CurrentLevel < LastLevel; } }

        /// <summary>Escena que corresponde al nivel en curso. Nunca devuelve un nivel incorrecto.</summary>
        public static string GetLevelSceneName()
        {
            return CurrentLevel == 2 ? SceneNames.Level02 : SceneNames.Level01;
        }

        public static string GetLevelDisplayName()
        {
            return CurrentLevel == 2 ? "COMPLEJO DE GUERRA" : "PUERTO EN RUINAS";
        }

        public static CharacterId GetCharacter(int playerIndex)
        {
            return playerIndex == 0 ? P1 : P2;
        }

        public static void SetCharacter(int playerIndex, CharacterId id)
        {
            if (playerIndex == 0) P1 = id;
            else P2 = id;
        }

        public static int GetLives(int playerIndex)
        {
            return playerIndex == 0 ? LivesP1 : LivesP2;
        }

        public static void SetLives(int playerIndex, int value)
        {
            value = Mathf.Max(0, value);
            if (playerIndex == 0) LivesP1 = value;
            else LivesP2 = value;
        }

        public static void AddKill(int playerIndex, int points)
        {
            Score += points;
            if (playerIndex == 0) KillsP1++;
            else if (playerIndex == 1) KillsP2++;
        }
    }

    /// <summary>
    /// Metadatos de los cuatro personajes jugables.
    /// </summary>
    public static class CharacterDb
    {
        public static readonly string[] Names = { "DANNER", "CÉSAR", "SEBASTIÁN", "INÉS" };

        public static readonly string[] Roles =
        {
            "ESPECIALISTA EN ASALTO",
            "DEMOLEDOR PESADO",
            "TIRADOR RÁPIDO",
            "SOPORTE TÁCTICO"
        };

        public static readonly Color[] Colors =
        {
            new Color(0.18f, 0.36f, 0.84f),
            new Color(0.18f, 0.66f, 0.31f),
            new Color(0.84f, 0.18f, 0.18f),
            new Color(0.55f, 0.25f, 0.84f)
        };

        public static string GetName(CharacterId id) => Names[Mathf.Clamp((int)id, 0, Names.Length - 1)];
        public static string GetRole(CharacterId id) => Roles[Mathf.Clamp((int)id, 0, Roles.Length - 1)];
        public static Color GetColor(CharacterId id) => Colors[Mathf.Clamp((int)id, 0, Colors.Length - 1)];

        /// <summary>Ruta base de los prefabs de personaje dentro de Resources.</summary>
        public static string GetPrefabPath(CharacterId id) => "Characters/Player_" + id;

        /// <summary>Ruta base de los retratos dentro de Resources.</summary>
        public static string GetPortraitPath(CharacterId id) => "Portraits/Portrait_" + id;
    }
}
