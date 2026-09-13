using UnityEngine;
using UnityEngine.UI;

namespace G10.Prototype.Atmosphere
{
    [RequireComponent(typeof(RawImage))]
    public sealed class CausticScroller : MonoBehaviour
    {
        [Tooltip("UV units per second; texture wrap must be Repeat.")]
        public Vector2 scrollSpeed = new(0.006f, 0.003f);
        [Range(0, 0.2f)] public float opacity = 0.045f;
        [Range(0, 0.5f)] public float pulseAmount = 0.12f;
        [Min(0)] public float pulseCyclesPerSecond = 0.035f;
        public CabinAtmosphere mood;
        private RawImage image;
        private Rect initialUV;
        private Color initialColor;
        private float elapsed;
        private void OnEnable()
        { image = GetComponent<RawImage>(); initialUV = image.uvRect; initialColor = image.color; image.raycastTarget = false; elapsed = 0; }
        private void Update()
        {
            elapsed += Time.deltaTime;
            Rect uv = initialUV; uv.position += new Vector2(Mathf.Repeat(elapsed * scrollSpeed.x, 1), Mathf.Repeat(elapsed * scrollSpeed.y, 1));
            image.uvRect = uv;
            Color tint = initialColor;
            tint.a = opacity * (1 + pulseAmount * Mathf.Sin(elapsed * pulseCyclesPerSecond * Mathf.PI * 2)) * (mood != null ? mood.Caustics : 1);
            image.color = tint;
        }
        private void OnDisable() { if (image != null) { image.uvRect = initialUV; image.color = initialColor; } }
    }
}
