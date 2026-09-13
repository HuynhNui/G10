using UnityEngine;
using UnityEngine.UI;

namespace G10.Prototype.Atmosphere
{
    /// <summary>Seeded, softly faded indicator pulses. Does not change Unity's gameplay random state.</summary>
    [RequireComponent(typeof(Graphic))]
    public sealed class RandomBlink : MonoBehaviour
    {
        public Vector2 intervalSeconds = new(3, 7);
        [Min(0.2f)] public float fadeSeconds = 0.8f;
        [Range(0, 0.5f)] public float minimumAlpha = 0.025f, maximumAlpha = 0.2f;
        public int seed = 71;
        public CabinAtmosphere mood;
        private Graphic graphic;
        private Color tint;
        private System.Random random;
        private float timer, target, alpha;
        private bool lit;
        private void OnEnable()
        { graphic = GetComponent<Graphic>(); tint = graphic.color; graphic.raycastTarget = false; random = new System.Random(seed); timer = 0; alpha = minimumAlpha; lit = false; }
        private void Update()
        {
            timer -= Time.deltaTime * (mood != null ? mood.BlinkRate : 1);
            if (timer <= 0)
            {
                lit = !lit;
                timer = Mathf.Max(0.5f, Mathf.Lerp(intervalSeconds.x, intervalSeconds.y, (float)random.NextDouble()));
                target = lit ? maximumAlpha : minimumAlpha;
            }
            alpha = Mathf.MoveTowards(alpha, target, Time.deltaTime * Mathf.Abs(maximumAlpha - minimumAlpha) / Mathf.Max(0.2f, fadeSeconds));
            Color color = tint; color.a = alpha * (mood != null ? mood.Glow : 1); graphic.color = color;
        }
        private void OnDisable() { if (graphic != null) graphic.color = tint; }
    }
}
