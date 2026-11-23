using UnityEngine;

namespace OperacionCuatro
{
    /// <summary>
    /// Fase 3 — AVISO DE GRANADA.
    ///
    /// Se coloca en el prefab de la granada enemiga: parpadea y late para que el jugador vea
    /// dónde va a explotar antes de que ocurra. No altera la mecha ni el daño de
    /// <see cref="Bomb"/>, solo su presentación.
    /// </summary>
    public class GrenadeWarning : MonoBehaviour
    {
        [Tooltip("Granada cuyo aviso se representa. Si se deja vacío se busca en el padre.")]
        public Bomb bomb;

        [Tooltip("Sprite que parpadea. Si se deja vacío se usa el SpriteRenderer del objeto.")]
        public SpriteRenderer body;

        public float blinkSpeed = 9f;
        public Color calmColor = new Color(1f, 1f, 1f, 1f);
        public Color warnColor = new Color(1f, 0.32f, 0.26f, 1f);
        public float minScale = 0.9f;
        public float maxScale = 1.25f;

        private void Awake()
        {
            if (bomb == null) bomb = GetComponentInParent<Bomb>();
            if (body == null) body = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (bomb == null || body == null) return;

            float pulse = Mathf.PingPong(Time.time * blinkSpeed, 1f);

            body.color = Color.Lerp(calmColor, warnColor, pulse);

            float s = Mathf.Lerp(minScale, maxScale, pulse);
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
