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

    /// <summary>Sacudida de cámara sencilla.</summary>
    public class CameraShake : MonoBehaviour
    {
        private float remaining;
        private float strength;
        private Vector3 basePos;
        private bool captured;

        public void Shake(float amount, float duration)
        {
            strength = Mathf.Max(strength, amount);
            remaining = Mathf.Max(remaining, duration);
        }

        private void LateUpdate()
        {
            if (remaining > 0f)
            {
                remaining -= Time.unscaledDeltaTime;
                if (remaining <= 0f)
                {
                    strength = 0f;
                    return;
                }

                transform.localPosition = Random.insideUnitCircle * strength;
            }
        }

        private void OnDestroy()
        {
            transform.localPosition = Vector3.zero;
        }
    }
}
