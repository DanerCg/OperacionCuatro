using System.Collections.Generic;
using UnityEngine;

namespace OperacionCuatro
{
    /// <summary>
    /// Fase 3 — ASCENSOR.
    ///
    /// Plataforma vertical que solo arranca cuando hay un pasajero encima, sube, espera en la
    /// planta alta y regresa sola para que el segundo jugador también lo use. Arrastra a los
    /// pasajeros aplicándoles el desplazamiento real en LateUpdate (PlayerController2D reescribe
    /// linearVelocity.x cada FixedUpdate, así que el rozamiento no serviría) y no tiene techo
    /// encima: no puede atrapar a nadie.
    /// </summary>
    public class ElevatorPlatform : MonoBehaviour
    {
        [Header("Recorrido")]
        public float travel = 6f;
        public float speed = 1.6f;
        public float waitAtEnds = 0.8f;

        [Header("Presentación")]
        public GameObject chainVisual;
        public GameObject arrowUpVisual;
        public GameObject arrowDownVisual;

        private readonly List<Rigidbody2D> riders = new List<Rigidbody2D>();
        private Vector2 basePosition;
        private Vector2 previousPosition;
        private float reached;
        private int dir;
        private float waitUntil;

        public bool IsMoving { get { return dir != 0; } }
        public float Height01 { get { return travel <= 0.01f ? 0f : Mathf.Clamp01(reached / travel); } }
        public int RiderCount { get { return riders.Count; } }

        private void Awake()
        {
            basePosition = transform.position;
            previousPosition = basePosition;
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;

            if (arrowUpVisual != null) arrowUpVisual.SetActive(dir > 0);
            if (arrowDownVisual != null) arrowDownVisual.SetActive(dir < 0);
            if (chainVisual != null) chainVisual.SetActive(reached > 0.01f);

            if (Time.time < waitUntil) return;

            if (dir == 0)
            {
                if (reached <= 0.001f && HasRider()) dir = 1;
                else if (reached >= travel - 0.001f) dir = -1;
            }

            if (dir == 0) return;

            reached += dir * speed * Time.deltaTime;

            if (reached >= travel)
            {
                reached = travel;
                dir = 0;
                waitUntil = Time.time + waitAtEnds;
            }
            else if (reached <= 0f)
            {
                reached = 0f;
                dir = 0;
                waitUntil = Time.time + waitAtEnds;
            }

            transform.position = basePosition + Vector2.up * reached;
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

        private bool HasRider()
        {
            for (int i = 0; i < riders.Count; i++) if (riders[i] != null) return true;
            return false;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            var body = collision.rigidbody;
            if (body == null || body.bodyType != RigidbodyType2D.Dynamic) return;
            if (!riders.Contains(body)) riders.Add(body);
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            if (collision.rigidbody == null) return;
            riders.Remove(collision.rigidbody);
        }

        /// <summary>Coloca el ascensor en una altura concreta (pruebas y reinicio).</summary>
        public void SnapTo(float height01)
        {
            reached = Mathf.Clamp01(height01) * travel;
            dir = 0;
            waitUntil = 0f;
            transform.position = basePosition + Vector2.up * reached;
            previousPosition = transform.position;
        }
    }
}
