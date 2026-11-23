using System.Collections.Generic;
using UnityEngine;

namespace OperacionCuatro
{
    /// <summary>
    /// Fase 2 — plataforma móvil industrial de Level02.
    ///
    /// Se desplaza entre dos puntos locales (localA → localB) y arrastra a los pasajeros:
    /// como PlayerController2D reescribe linearVelocity.x en cada FixedUpdate, el arrastre por
    /// rozamiento no funcionaría, así que la plataforma aplica su propio desplazamiento al
    /// Rigidbody2D de los pasajeros en LateUpdate.
    /// </summary>
    public class MovingPlatform : MonoBehaviour
    {
        [Header("Recorrido (desplazamiento local desde la posición inicial)")]
        public Vector2 localA = new Vector2(0f, 0f);
        public Vector2 localB = new Vector2(6f, 0f);
        public float speed = 1.8f;
        public float waitTime = 0.5f;

        [Header("Presentación")]
        public GameObject chainVisual;
        public GameObject warningVisual;

        private readonly List<Rigidbody2D> riders = new List<Rigidbody2D>();
        private Vector2 startPosition;
        private Vector2 previousPosition;
        private float travel;
        private int direction = 1;
        private float waitUntil;

        public bool IsMoving { get { return Time.time >= waitUntil; } }

        private void Awake()
        {
            startPosition = transform.position;
            previousPosition = startPosition;
            travel = 0f;
            direction = 1;
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;

            float distance = Vector2.Distance(localA, localB);
            if (distance < 0.01f) return;

            if (warningVisual != null) warningVisual.SetActive(Time.time < waitUntil);
            if (chainVisual != null) chainVisual.SetActive(true);

            if (Time.time < waitUntil) return;

            travel += (speed / distance) * Time.deltaTime * direction;

            if (travel >= 1f)
            {
                travel = 1f;
                direction = -1;
                waitUntil = Time.time + waitTime;
            }
            else if (travel <= 0f)
            {
                travel = 0f;
                direction = 1;
                waitUntil = Time.time + waitTime;
            }

            Vector2 offset = Vector2.Lerp(localA, localB, travel);
            transform.position = new Vector3(startPosition.x + offset.x, startPosition.y + offset.y, transform.position.z);
        }

        private void LateUpdate()
        {
            Vector2 current = transform.position;
            Vector2 delta = current - previousPosition;
            previousPosition = current;

            if (delta.sqrMagnitude <= 0f || riders.Count == 0) return;

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
