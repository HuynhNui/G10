using G10.Prototype.Atmosphere;
using UnityEngine;

namespace G10.Prototype.Feedback
{
    /// <summary>Coordinates resource baseline plus temporary modifiers through the existing mood owner.</summary>
    public class EffectLightingExecutor : MonoBehaviour
    {
        public CabinAtmosphere atmosphere;
        public virtual void Render(CabinMood? mood, float instability, float lightDip)
        { if (isActiveAndEnabled) atmosphere?.SetResourceFeedback(mood, instability, lightDip); }
        public virtual void Restore() => atmosphere?.SetResourceFeedback(null);
        private void OnDisable() => Restore();
    }
}
