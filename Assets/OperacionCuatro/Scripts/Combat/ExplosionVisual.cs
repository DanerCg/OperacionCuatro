using UnityEngine;

namespace OperacionCuatro
{
    /// <summary>
    /// Efecto de explosión: anima sus frames y aplica daño en área una sola vez al aparecer.
    /// (El daño lo aplica <see cref="Bomb"/>; este prefab es puramente visual).
    /// </summary>
    public class ExplosionVisual : MonoBehaviour
    {
        public float lifetime = 0.6f;
        public float shakeStrength = 0.25f;

        private void Start()
        {
            var cam = FindFirstObjectByType<CameraShake>();
            if (cam != null) cam.Shake(shakeStrength, 0.25f);
        }
    }
}
