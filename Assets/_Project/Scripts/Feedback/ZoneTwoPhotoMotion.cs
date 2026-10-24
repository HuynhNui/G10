using UnityEngine;
using UnityEngine.UI;

namespace G10.Prototype.Feedback
{
    /// <summary>Display-only motion at photograph edges. Never writes a photo texture or archive.</summary>
    internal sealed class ZoneTwoPhotoMotion
    {
        private RawImage audience;
        private Material original, instance;
        private static readonly int TimeId = Shader.PropertyToID("_EffectTime"), StrengthId = Shader.PropertyToID("_Strength");
        public bool Visible => audience != null && instance != null && audience.material == instance;

        public void Render(RawImage preview, Material template, bool visible, float age, float strength)
        {
            if (!visible || preview == null || !preview.isActiveAndEnabled || preview.texture == null || template == null)
            { Restore(); return; }
            if (audience != preview)
            { Restore(); audience = preview; original = preview.material; }
            if (instance == null) instance = new Material(template) { name = "Zone02 bounded photo-edge motion" };
            if (audience.material != instance) audience.material = instance;
            instance.SetFloat(TimeId, age);
            instance.SetFloat(StrengthId, Mathf.Clamp01(strength));
        }

        public void Restore()
        {
            if (audience != null && audience.material == instance) audience.material = original;
            audience = null; original = null;
        }

        public void Dispose()
        {
            Restore();
            if (instance != null)
            {
                if (Application.isPlaying) Object.Destroy(instance); else Object.DestroyImmediate(instance);
                instance = null;
            }
        }
    }
}
