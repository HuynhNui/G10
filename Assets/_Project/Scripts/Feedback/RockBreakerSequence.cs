using G10.Prototype.Computer;
using G10.Prototype.Missions;
using G10.Prototype.UI;
using UnityEngine;

namespace G10.Prototype.Feedback
{
    /// <summary>Presentation only. The rock objective/flag is committed before this interruptible sequence.</summary>
    [DisallowMultipleComponent]
    public sealed class RockBreakerSequence : MonoBehaviour
    {
        public CabinStationView cabin;
        public ImpactShake shake;
        public AudioClip impactAudio;
        public EffectSequence productionSequence;
        [SerializeField] private EffectDirector effects;
        [SerializeField, Min(.1f)] private float duration = 10;
        [SerializeField, Min(0)] private float revealSeconds = 1.5f;
        public float Duration => Mathf.Max(.1f, duration);
        public bool IsActive { get; private set; }
        public bool DeferTerrainReveal => IsActive && Elapsed < Mathf.Min(revealSeconds, Duration);
        public float Elapsed { get; private set; }
        public int SequenceCount { get; private set; }
        private float nextPulse, nextNotice;
        private ExpeditionLoop loop;
        private CabinZoneSession session;
        private bool revealed;
        private bool applicationPaused;
        private EffectHandle pulseHandle, audioHandle, noticeHandle, productionHandle;
        private void Awake()
        { cabin ??= GetComponent<CabinStationView>(); session = GetComponent<CabinZoneSession>(); effects ??= GetComponent<EffectDirector>(); }
        public bool BeginCommitted(ZoneOneStory source)
        {
            if (IsActive || !isActiveAndEnabled || source == null || source.config?.zoneId != "Zone03" ||
                !source.HasWorldFlag("ROCK_BARRIER_DESTROYED")) return false;
            loop = cabin?.Panels?.Expedition; session ??= GetComponent<CabinZoneSession>();
            IsActive = true; Elapsed = nextPulse = nextNotice = 0; revealed = false; SequenceCount++;
            cabin?.Brake(); cabin?.CancelDirectInteraction(); cabin?.ClosePanel();
            loop?.EvaluateDeadline();
            if (productionSequence != null) productionHandle = effects != null ? effects.PlaySequence(productionSequence) : default;
            else audioHandle = effects != null ? effects.Play(CabinEffects.RockAudio) : default;
            session?.RefreshTerrain();
            Tick(0);
            return true;
        }
        private void Update()
        {
            if (!IsActive) return;
            if (applicationPaused || Time.timeScale <= 0 || PauseMenuController.Instance?.IsPaused == true)
            { effects?.Stop(pulseHandle); effects?.Stop(productionHandle); return; }
            if (productionSequence != null && effects != null && !effects.IsActive(productionHandle) && Elapsed < productionSequence.Duration)
                productionHandle = effects.PlaySequence(productionSequence, startAtSeconds: Elapsed);
            Tick(Time.unscaledDeltaTime);
        }
        public static float AmplitudeAt(float normalizedTime)
        {
            float t = Mathf.Clamp01(normalizedTime);
            if (t < .15f) return Mathf.Lerp(8, 7, t / .15f);
            if (t < .4f) return Mathf.Lerp(7, 5, (t - .15f) / .25f);
            if (t < .7f) return Mathf.Lerp(5, 1.5f, (t - .4f) / .3f);
            return Mathf.Lerp(1.5f, 0, (t - .7f) / .3f);
        }
        public void Tick(float seconds)
        {
            if (!IsActive) return;
            Elapsed = Mathf.Min(Duration, Elapsed + Mathf.Max(0, seconds));
            if (!revealed && !DeferTerrainReveal) { revealed = true; session?.RefreshTerrain(); }
            if (Elapsed >= Duration) { StopPresentation(false); return; }
            if (productionSequence == null && Elapsed >= nextPulse)
            {
                nextPulse = Elapsed + .12f;
                pulseHandle = effects != null ? effects.Play(CabinEffects.RockPulse,
                    new EffectContext { ShakePixels = AmplitudeAt(Elapsed / Duration) }) : default;
            }
            if (Elapsed >= nextNotice)
            {
                nextNotice = Elapsed + .75f;
                noticeHandle = effects != null ? effects.Play(CabinEffects.RockNotice, new EffectContext
                { Text = $"ROCK BARRIER DESTROYED · STABILIZING {Mathf.CeilToInt(Duration - Elapsed)}s" }) : default;
            }
        }
        public void Stop() => StopPresentation(true);
        private void StopPresentation(bool cancelPresentation)
        {
            bool active = IsActive;
            IsActive = false;
            effects?.Stop(pulseHandle); effects?.Stop(audioHandle); effects?.Stop(noticeHandle);
            // The ten-second gameplay lock ends independently of the bounded receding audio tail.
            if (cancelPresentation) effects?.Stop(productionHandle);
            if (active) { session?.RefreshTerrain(); loop?.EvaluateDeadline(); }
        }
        private void OnDisable() => Stop();
        private void OnDestroy() => Stop();
        private void OnApplicationPause(bool value) => applicationPaused = value;
    }
}
