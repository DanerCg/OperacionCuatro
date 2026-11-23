using UnityEngine;

namespace OperacionCuatro
{
    /// <summary>Capa de fondo con desplazamiento relativo a la cámara.</summary>
    public class ParallaxLayer : MonoBehaviour
    {
        public Transform cameraTransform;
        public float parallaxFactor = 0.4f;

        private Vector3 startPosition;
        private float startCameraX;

        private void Start()
        {
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            startPosition = transform.position;
            startCameraX = cameraTransform != null ? cameraTransform.position.x : 0f;
        }

        private void LateUpdate()
        {
            if (cameraTransform == null) return;
            float delta = cameraTransform.position.x - startCameraX;
            transform.position = new Vector3(startPosition.x + delta * parallaxFactor, startPosition.y, startPosition.z);
        }
    }
}
