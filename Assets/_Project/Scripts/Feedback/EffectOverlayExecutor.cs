using TMPro;
using UnityEngine;

namespace G10.Prototype.Feedback
{
    /// <summary>Only writer for bound feedback graphics; never writes ambient graphics.</summary>
    public class EffectOverlayExecutor : MonoBehaviour
    {
        public UnityEngine.UI.RawImage hull, energy, tint;
        public TMP_Text notification;
        [Range(.3f, .6f)] public float fadeSeconds = .4f;
        public float HullAlpha => hull != null ? hull.color.a : 0;
        public float EnergyAlpha => energy != null ? energy.color.a : 0;
        public void SetTintColor(Color value)
        {
            Capture();
            if (tint != null) { value.a = tint.color.a; tint.color = value; }
        }
        private Color hullOrigin, energyOrigin, tintOrigin;
        private bool captured;
        private void Capture()
        {
            if (captured) return;
            hullOrigin = hull != null ? hull.color : Color.white;
            energyOrigin = energy != null ? energy.color : Color.white;
            tintOrigin = tint != null ? tint.color : Color.black;
            captured = true;
        }
        public virtual void Render(float hullAlpha, float energyAlpha, float tintAlpha, string text, float dt, bool visible)
        {
            Capture();
            if (!visible) { Restore(); return; }
            Fade(hull, hullAlpha, dt); Fade(energy, energyAlpha, dt); Fade(tint, tintAlpha, dt);
            if (notification != null) { notification.text = text ?? ""; notification.raycastTarget = false; }
        }
        private void Fade(UnityEngine.UI.RawImage image, float target, float dt)
        {
            if (image == null) return;
            var color = image.color;
            // Flash peaks immediately. Downward changes ease to the independently derived baseline.
            color.a = dt <= 0 ? target : Mathf.Lerp(color.a, target, 1 - Mathf.Exp(-3 * dt / Mathf.Max(.01f, fadeSeconds)));
            image.color = color; image.raycastTarget = false;
        }
        public virtual void Restore()
        {
            Capture();
            if (hull != null) hull.color = hullOrigin;
            if (energy != null) energy.color = energyOrigin;
            if (tint != null) tint.color = tintOrigin;
            if (notification != null) notification.text = "";
        }
        private void OnDisable() => Restore();
    }
}
