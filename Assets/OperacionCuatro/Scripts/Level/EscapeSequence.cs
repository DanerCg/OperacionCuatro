using UnityEngine;

namespace OperacionCuatro
{
    /// <summary>
    /// Fase 3 — SECUENCIA DE ESCAPE.
    ///
    /// Cuando caen todas las unidades de alimentación se dispara la alarma y arranca la cuenta
    /// regresiva. Si los jugadores alcanzan la zona segura antes de cero, la misión se completa
    /// por el cauce normal de <see cref="LevelManager.CompleteMission"/>; si el tiempo se agota,
    /// la evacuación falla y el nivel termina en Game Over. La salida nunca está detrás de una
    /// puerta cerrada, así que no puede bloquearse de forma permanente.
    /// </summary>
    public class EscapeSequence : MonoBehaviour
    {
        public static EscapeSequence Instance { get; private set; }

        [Header("Condición de inicio")]
        public PowerUnit[] powerUnits;
        public bool autoBeginWhenPowered = true;

        [Header("Cuenta regresiva")]
        public float countdownSeconds = 70f;

        [Header("Presentación")]
        public GameObject alarmVisual;
        public SpriteRenderer alarmLight;
        public Color alarmColor = new Color(1f, 0.24f, 0.2f, 1f);
        public float alarmBlinkSpeed = 3.5f;

        public bool Begun { get; private set; }
        public bool Finished { get; private set; }
        public float Remaining { get; private set; }
        public float Total { get { return countdownSeconds; } }

        /// <summary>Número de veces que un jugador alcanzó la zona segura.</summary>
        public int SafeZoneArrivals { get; private set; }

        private void Awake()
        {
            Instance = this;
            Remaining = countdownSeconds;
            if (alarmVisual != null) alarmVisual.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (!Begun)
            {
                if (autoBeginWhenPowered && AllUnitsDown()) Begin();
                return;
            }

            if (Finished) return;

            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;

            Remaining -= Time.deltaTime;

            if (alarmLight != null)
            {
                float pulse = Mathf.PingPong(Time.time * alarmBlinkSpeed, 1f);
                var c = alarmColor;
                c.a = Mathf.Lerp(0.25f, 1f, pulse);
                alarmLight.color = c;
            }

            if (Remaining <= 0f)
            {
                Remaining = 0f;
                Fail();
            }
        }

        /// <summary>Arranca la alarma y la cuenta regresiva.</summary>
        public void Begin()
        {
            if (Begun || Finished) return;

            Begun = true;
            Remaining = countdownSeconds;

            if (alarmVisual != null) alarmVisual.SetActive(true);
            AudioManager.Sfx("sfx_alarm", 0.8f);

            if (LevelManager.Instance != null && LevelManager.Instance.hud != null)
                LevelManager.Instance.hud.ShowMessage("ALARMA DE SEGURIDAD  ·  EVACUAD");
        }

        /// <summary>Un jugador entra en la zona segura.</summary>
        public void ReachExit()
        {
            if (Finished) return;

            SafeZoneArrivals++;

            if (!Begun)
            {
                if (LevelManager.Instance != null && LevelManager.Instance.hud != null)
                    LevelManager.Instance.hud.ShowMessage("DESTRUYE LAS UNIDADES DE ALIMENTACIÓN");

                return;
            }

            Finished = true;

            if (LevelManager.Instance != null && LevelManager.Instance.hud != null)
                LevelManager.Instance.hud.ShowMessage("EVACUACIÓN COMPLETADA");

            AudioManager.Sfx("sfx_start", 0.8f);

            if (LevelManager.Instance != null) LevelManager.Instance.CompleteMission();
        }

        /// <summary>Fuerza el fallo de la evacuación (tiempo agotado).</summary>
        public void Fail()
        {
            if (Finished) return;
            Finished = true;

            if (LevelManager.Instance != null) LevelManager.Instance.ForceGameOver();
        }

        /// <summary>¿Están todas las unidades destruidas?</summary>
        public bool AllUnitsDown()
        {
            if (powerUnits != null && powerUnits.Length > 0)
            {
                for (int i = 0; i < powerUnits.Length; i++)
                {
                    if (powerUnits[i] == null) continue;
                    if (!powerUnits[i].IsDestroyed) return false;
                }
                return true;
            }

            return PowerUnit.All.Count > 0 && PowerUnit.DestroyedTotal() >= PowerUnit.TotalUnits;
        }

        /// <summary>Segundos restantes en formato entero (lo usa el HUD).</summary>
        public int RemainingSeconds { get { return Mathf.CeilToInt(Mathf.Max(0f, Remaining)); } }
    }
}
