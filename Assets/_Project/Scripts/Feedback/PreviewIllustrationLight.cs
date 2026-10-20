using G10.Prototype.Atmosphere;
using UnityEngine;
using UnityEngine.UI;

namespace G10.Prototype.Feedback
{
    public sealed class PreviewIllustrationLight : MonoBehaviour
    {
        public CabinAtmosphere atmosphere;
        public RawImage image;
        private Color baseline;
        private void OnEnable() { if (image != null) baseline = image.color; }
        private void LateUpdate()
        { if (image != null && atmosphere != null) image.color = new Color(baseline.r * atmosphere.Light, baseline.g * atmosphere.Light, baseline.b * atmosphere.Light, baseline.a); }
        private void OnDisable() { if (image != null) image.color = baseline; }
    }
}
