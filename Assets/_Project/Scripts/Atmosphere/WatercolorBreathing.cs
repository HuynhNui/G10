using UnityEngine;
using UnityEngine.UI;

namespace G10.Prototype.Atmosphere
{
    /// <summary>Slow pigment/glow alpha modulation; no additive material or bloom.</summary>
    [RequireComponent(typeof(Graphic))]
    public sealed class WatercolorBreathing : MonoBehaviour
    {
        [Range(0, 0.25f)] public float opacity = 0.045f;
        [Range(0, 0.8f)] public float pulseAmount = 0.18f;
        [Min(0)] public float cyclesPerSecond = 0.04f;
        public Vector2 driftPixels = new(1.3f, 0.8f);
        public float phase;
        public bool screenGlow;
        public CabinAtmosphere mood;
        private Graphic graphic;
        private Color tint;
        private Vector3 origin;
        private float elapsed;
        private void OnEnable()
        { graphic = GetComponent<Graphic>(); tint = graphic.color; origin = transform.localPosition; graphic.raycastTarget = false; elapsed = 0; }
        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = elapsed * cyclesPerSecond * Mathf.PI * 2 + phase;
            float strength = mood == null ? 1 : screenGlow ? mood.Glow : mood.Light;
            Color color = tint; color.a = opacity * (1 + Mathf.Sin(t) * pulseAmount) * strength; graphic.color = color;
            transform.localPosition = origin + new Vector3(Mathf.Sin(t * 0.8f) * driftPixels.x, Mathf.Cos(t * 0.7f) * driftPixels.y);
        }
        private void OnDisable() { if (graphic != null) graphic.color = tint; transform.localPosition = origin; }
    }
}
