using UnityEngine;

namespace G10.Prototype.Feedback
{
    /// <summary>Arbitration adapter. ImpactShake alone owns the actual transform.</summary>
    public class EffectShakeExecutor : MonoBehaviour
    {
        public ImpactShake shake;
        private long winner;
        public long Winner => winner;
        public virtual void Render(long id, float pixels, float seconds)
        {
            if (!isActiveAndEnabled || shake == null || !ImpactShake.ScreenShakeEnabled || id == 0)
            { Restore(); return; }
            if (winner == id) return;
            shake.Clear(); winner = id;
            shake.Shake(pixels, seconds);
        }
        public virtual void Restore() { winner = 0; if (shake != null) shake.Clear(); }
        private void OnDisable() => Restore();
    }
}
