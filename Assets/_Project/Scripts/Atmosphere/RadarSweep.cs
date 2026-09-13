using UnityEngine;
using UnityEngine.UI;

namespace G10.Prototype.Atmosphere
{
    /// <summary>Ambient display sweep; it never creates radar contacts or initiates a scan.</summary>
    public sealed class RadarSweep : MonoBehaviour
    {
        [Min(0)] public float degreesPerSecond = 14;
        [Range(0, 0.3f)] public float opacity = 0.065f;
        public Graphic sweepGraphic;
        public CabinAtmosphere mood;
        private Quaternion origin;
        private Color tint;
        private float angle;
        private void OnEnable() { origin = transform.localRotation; angle = 0; if (sweepGraphic != null) tint = sweepGraphic.color; }
        private void Update()
        {
            angle = Mathf.Repeat(angle + Time.deltaTime * degreesPerSecond, 360);
            transform.localRotation = origin * Quaternion.Euler(0, 0, -angle);
            if (sweepGraphic != null)
            { Color color = tint; color.a = opacity * (mood != null ? mood.Glow : 1); sweepGraphic.color = color; sweepGraphic.raycastTarget = false; }
        }
        private void OnDisable() { transform.localRotation = origin; if (sweepGraphic != null) sweepGraphic.color = tint; }
    }
}
