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
        public void RenderProfile(long id, float pixels, float seconds, ShakeProfile profile, Vector2 direction, int seed)
        {
            if (profile == ShakeProfile.LegacyImpact) { Render(id, pixels, seconds); return; }
            if (!isActiveAndEnabled || shake == null || !ImpactShake.ScreenShakeEnabled || id == 0) { Restore(); return; }
            if (winner == id) return;
            shake.Clear(); winner = id; shake.PlayProfile(profile, pixels, seconds, direction, seed);
        }
        public virtual void Restore() { winner = 0; if (shake != null) shake.Clear(); }
        private void OnDisable() => Restore();
    }
}
