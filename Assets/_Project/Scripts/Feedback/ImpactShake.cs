using UnityEngine;

namespace G10.Prototype.Feedback
{
    /// <summary>Owns position only on the outer CabinFrame, separate from breathing/bob child transforms.</summary>
    [DisallowMultipleComponent]
    public sealed class ImpactShake : MonoBehaviour
    {
        public const string PreferenceKey = "G10.Accessibility.ScreenShake";
        public static bool ScreenShakeEnabled => PlayerPrefs.GetInt(PreferenceKey, 1) != 0;
        public static void SetScreenShakeEnabled(bool value)
        { PlayerPrefs.SetInt(PreferenceKey, value ? 1 : 0); PlayerPrefs.Save(); }
        private readonly System.Random random = new();
        private Vector3 origin;
        private float remaining, duration, amplitude;
        public bool IsShaking => remaining > 0;
        public Vector3 Offset => transform.localPosition - origin;
        private void OnEnable() { origin = transform.localPosition; remaining = 0; }
        public void Shake(float pixels, float seconds)
        {
            if (!ScreenShakeEnabled || !isActiveAndEnabled || Time.timeScale <= 0) return;
            duration = remaining = Mathf.Clamp(seconds, .01f, .28f);
            amplitude = Mathf.Clamp(pixels, 0, 8);
        }
        public void Clear() { remaining = 0; transform.localPosition = origin; }
        private void LateUpdate()
        {
            if (!ScreenShakeEnabled) { Clear(); return; }
            if (!IsShaking || Time.deltaTime <= 0) return;
            remaining = Mathf.Max(0, remaining - Time.deltaTime);
            float strength = amplitude * remaining / duration;
            transform.localPosition = origin + new Vector3((float)random.NextDouble() * 2 - 1, (float)random.NextDouble() * 2 - 1) * strength;
            if (!IsShaking) Clear();
        }
        private void OnDisable() => Clear();
    }
}
