using System.Collections.Generic;
using UnityEngine;

namespace OperacionCuatro
{
    /// <summary>
    /// Fase 2 — cinta transportadora de la sección de producción.
    /// Empuja horizontalmente a quien se apoya en ella; mismo criterio que MovingPlatform
    /// (desplazamiento directo del Rigidbody2D en LateUpdate para que el arrastre no se pierda).
    /// </summary>
    public class ConveyorBelt : MonoBehaviour
    {
        [Tooltip("Velocidad en unidades por segundo. Negativa mueve hacia la izquierda.")]
        public float beltSpeed = 2.2f;

        public GameObject arrowVisual;

        private readonly List<Rigidbody2D> riders = new List<Rigidbody2D>();

        private void LateUpdate()
        {
            if (riders.Count == 0) return;
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;

            Vector2 delta = new Vector2(beltSpeed * Time.deltaTime, 0f);

            for (int i = riders.Count - 1; i >= 0; i--)
            {
                var rider = riders[i];
                if (rider == null)
                {
                    riders.RemoveAt(i);
                    continue;
                }

                rider.position += delta;
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            var body = collision.rigidbody;
            if (body == null || body.bodyType != RigidbodyType2D.Dynamic) return;
            if (!riders.Contains(body)) riders.Add(body);
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            var body = collision.rigidbody;
            if (body == null) return;
            riders.Remove(body);
        }
    }
}
