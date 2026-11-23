using UnityEngine;
using UnityEngine.UI;

namespace OperacionCuatro
{
    /// <summary>Animación de flotación vertical para títulos y paneles.</summary>
    public class UIBob : MonoBehaviour
    {
        public float amplitude = 6f;
        public float speed = 1.6f;

        private RectTransform rect;
        private Vector2 start;

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            if (rect != null) start = rect.anchoredPosition;
        }

        private void Update()
        {
            if (rect == null) return;
            float y = Mathf.Sin(Time.unscaledTime * speed) * amplitude;
            rect.anchoredPosition = start + new Vector2(0f, y);
        }
    }

    // UIBlink se movió a Scripts/UI/UIBlink.cs para que tenga MonoScript propio:
    // el componente InsertCoin de MainMenu quedaba nulo al compartir archivo.

    /// <summary>Pulso de escala suave para botones destacados.</summary>
    public class UIPulse : MonoBehaviour
    {
        public float amplitude = 0.04f;
        public float speed = 3.2f;

        private Vector3 start;

        private void Awake()
        {
            start = transform.localScale;
        }

        private void Update()
        {
            float s = 1f + Mathf.Sin(Time.unscaledTime * speed) * amplitude;
            transform.localScale = start * s;
        }
    }

    /// <summary>Desplazamiento horizontal continuo para líneas de exploración.</summary>
    public class UIScroll : MonoBehaviour
    {
        public float speed = 40f;
        public float wrapWidth = 1600f;

        private RectTransform rect;
        private Vector2 start;

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            if (rect != null) start = rect.anchoredPosition;
        }

        private void Update()
        {
            if (rect == null) return;
            float x = Mathf.Repeat(Time.unscaledTime * speed, wrapWidth);
            rect.anchoredPosition = start + new Vector2(x, 0f);
        }
    }
}
