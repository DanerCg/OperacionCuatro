using UnityEngine;

namespace OperacionCuatro
{
    /// <summary>
    /// Fase 3 — PUERTA DE SEGURIDAD.
    ///
    /// Bloquea el paso hasta que se cumpla la condición de su <see cref="channel"/>: los
    /// <see cref="DoorSwitch"/> fijados y, si se cuenta, las <see cref="PowerUnit"/> destruidas.
    /// El estado es siempre legible: barera roja con la puerta cerrada, verde y hoja retirada
    /// cuando se abre.
    /// </summary>
    public class SecurityDoor : MonoBehaviour
    {
        [Header("Condición")]
        public string channel = "A";
        public int requiredCount = 1;
        public bool countPowerUnits = true;

        [Header("Presentación")]
        public GameObject lockedVisual;
        public GameObject openVisual;
        public SpriteRenderer statusLight;
        public Color lockedColor = new Color(0.95f, 0.28f, 0.24f, 1f);
        public Color openColor = new Color(0.35f, 0.95f, 0.45f, 1f);

        [Header("Hoja")]
        public Transform doorLeaf;
        public Vector2 openOffset = new Vector2(0f, 3.2f);
        public float openSpeed = 3.5f;

        [Header("Bloqueo físico")]
        public Collider2D blocker;

        public bool IsOpen { get; private set; }

        /// <summary>Progreso de la condición (para pruebas y HUD).</summary>
        public int Progress
        {
            get
            {
                int done = DoorSwitch.ActiveInChannel(channel);
                if (countPowerUnits) done += PowerUnit.DestroyedInChannel(channel);
                return done;
            }
        }

        private Vector3 leafStart;

        private void Awake()
        {
            if (blocker == null) blocker = GetComponent<Collider2D>();
            if (doorLeaf != null) leafStart = doorLeaf.localPosition;

            Apply(false);
        }

        private void Update()
        {
            bool shouldOpen = Progress >= Mathf.Max(1, requiredCount);
            if (shouldOpen != IsOpen) Apply(shouldOpen);

            if (doorLeaf != null)
            {
                Vector3 goal = IsOpen ? leafStart + (Vector3)openOffset : leafStart;
                doorLeaf.localPosition = Vector3.MoveTowards(doorLeaf.localPosition, goal, openSpeed * Time.deltaTime);
            }
        }

        private void Apply(bool open)
        {
            IsOpen = open;

            if (blocker != null) blocker.enabled = !open;
            if (lockedVisual != null) lockedVisual.SetActive(!open);
            if (openVisual != null) openVisual.SetActive(open);
            if (statusLight != null) statusLight.color = open ? openColor : lockedColor;

            if (open) AudioManager.Sfx("sfx_door", 0.6f);
        }

        /// <summary>Abre o cierra la puerta a la fuerza (pruebas y secuencia de escape).</summary>
        public void ForceOpen()
        {
            Apply(true);
            if (doorLeaf != null) doorLeaf.localPosition = leafStart + (Vector3)openOffset;
        }
    }
}
