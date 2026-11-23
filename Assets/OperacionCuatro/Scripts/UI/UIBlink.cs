using UnityEngine;
using UnityEngine.UI;

namespace OperacionCuatro
{
    /// <summary>
    /// Parpadeo de texto (estilo "INSERT COIN").
    ///
    /// Vive en su propio archivo para que tenga MonoScript propio y la referencia
    /// serializada de la escena MainMenu se resuelva por GUID. Cuando la clase
    /// compartía archivo con UIBob/UIPulse/UIScroll, el componente de InsertCoin
    /// quedaba nulo al cargar la escena (referencia de script perdida).
    /// </summary>
    public class UIBlink : MonoBehaviour
    {
        public Graphic target;
        public float speed = 2.2f;
        public float minAlpha = 0.18f;

        private void Awake()
        {
            if (target == null) target = GetComponent<Graphic>();
        }

        private void Update()
        {
            if (target == null) return;
            var c = target.color;
            c.a = Mathf.Lerp(minAlpha, 1f, (Mathf.Sin(Time.unscaledTime * speed) + 1f) * 0.5f);
            target.color = c;
        }
    }
}
