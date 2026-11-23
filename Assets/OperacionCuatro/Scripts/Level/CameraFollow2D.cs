using UnityEngine;

namespace OperacionCuatro
{
    /// <summary>Seguimiento de cámara con uno o dos objetivos y límites de nivel.</summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow2D : MonoBehaviour
    {
        public Transform targetA;
        public Transform targetB;

        public float minX = 0f;
        public float maxX = 100f;
        public float minY = 3f;
        public float maxY = 8f;
        public float smoothTime = 0.18f;
        public float maxSeparation = 11f;
        public float baseOrthoSize = 6.2f;
        public float zoomOutPerUnit = 0.12f;
        public bool useBounds = true;

        private Vector3 velocity;
        private Camera cam;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = baseOrthoSize;
        }

        public void SetTargets(Transform a, Transform b)
        {
            targetA = a;
            targetB = b;
        }

        public void SnapToTarget()
        {
            Vector3 desired = ComputeTarget();
            transform.position = new Vector3(desired.x, desired.y, transform.position.z);
            velocity = Vector3.zero;
        }

        private Vector3 ComputeTarget()
        {
            if (targetA == null) return transform.position;

            Vector3 pos = targetA.position;

            if (targetB != null)
            {
                Vector3 bPos = targetB.position;

                // Evita separaciones excesivas: se limita la distancia entre ambos.
                float dx = bPos.x - pos.x;
                if (Mathf.Abs(dx) > maxSeparation)
                    dx = Mathf.Sign(dx) * maxSeparation;

                pos.x += dx * 0.5f;
                pos.y = (pos.y + bPos.y) * 0.5f;

                float separation = Mathf.Abs(targetB.position.x - targetA.position.x);
                float extraZoom = Mathf.Max(0f, separation - 8f) * zoomOutPerUnit;
                if (cam != null)
                    cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, baseOrthoSize + extraZoom, Time.deltaTime * 4f);
            }
            else if (cam != null)
            {
                cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, baseOrthoSize, Time.deltaTime * 4f);
            }

            if (useBounds)
            {
                pos.x = Mathf.Clamp(pos.x, minX, maxX);
                pos.y = Mathf.Clamp(pos.y, minY, maxY);
            }

            pos.z = transform.position.z;
            return pos;
        }

        private void LateUpdate()
        {
            if (targetA == null) return;

            Vector3 desired = ComputeTarget();
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
        }
    }
}
