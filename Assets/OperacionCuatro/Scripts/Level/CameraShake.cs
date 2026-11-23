using UnityEngine;

namespace OperacionCuatro
{
    /// <summary>Sacudida de cámara sencilla.</summary>
    public class CameraShake : MonoBehaviour
    {
        private float remaining;
        private float strength;

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
