using System;
using G10.Prototype.Atmosphere;
using G10.Prototype.Audio;
using G10.Prototype.Computer;
using G10.Prototype.Core;
using G10.Prototype.Navigation;
using G10.Prototype.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace G10.Prototype.Feedback
{
    public static class CabinFeedbackRules
    {
        public static float HullAlpha(float ratio) => ratio <= .10f ? .25f : ratio <= .25f ? .15f : 0;
        public static float EnergyAlpha(float ratio) => ratio <= 0 ? .28f : ratio <= .10f ? .22f : ratio <= .20f ? .15f : 0;
        public static CabinMood Mood(float hullRatio, float energyRatio, bool scanning)
            => hullRatio <= .25f ? CabinMood.Danger : energyRatio <= .20f ? CabinMood.LowPower : scanning ? CabinMood.ScanActive : CabinMood.Normal;
        public static int CaptureTier(int capturesToday) => Mathf.Clamp(capturesToday - 1, 0, 3);
    }

    /// <summary>Derived, unsaved presentation. Never mutates ship, inventory, mission or expedition authority.</summary>
    [DisallowMultipleComponent]
    public sealed class CabinFeedbackController : MonoBehaviour
    {
        [SerializeField] private CabinStationView cabin;
        [SerializeField] private EffectDirector effects;
        // Retained serialized authoring references for old scenes and the idempotent migration tool.
        // Runtime execution belongs exclusively to the director/executors and definition assets.
        [SerializeField, HideInInspector] private CabinAtmosphere atmosphere;
        [SerializeField, HideInInspector] private ImpactShake impactShake;
        [SerializeField, HideInInspector] private UnityEngine.UI.RawImage lowHullOverlay, lowEnergyOverlay;
        [SerializeField, HideInInspector] private TMP_Text notification;
        [SerializeField, HideInInspector] private AudioClip hullImpact, hullCreak, criticalWarning, lowPowerHum, powerDown;
        [SerializeField, HideInInspector] private AudioClip[] exteriorScrapes = Array.Empty<AudioClip>();
        [SerializeField, HideInInspector] private float overlayFadeSeconds = .4f;
        private ExpeditionLoop loop;
        private ZoneNavigation navigation;
        private CreatureCatcher catcher;
        private readonly System.Random random = new();
        private float previousEnergy = -1, creakTimer, warningTimer, scrapeDelay = -1;
        private int pendingTier;
        private bool suspended, applicationPaused;
        public float HullRatio { get; private set; } = 1;
        public float EnergyRatio { get; private set; } = 1;
        public float HullTargetAlpha { get; private set; }
        public float EnergyTargetAlpha { get; private set; }
        public EffectDirector Effects => effects;
        public float HullOverlayAlpha => effects?.Surface(EffectTarget.Global)?.overlay?.HullAlpha ?? 0;
        public float EnergyOverlayAlpha => effects?.Surface(EffectTarget.Global)?.overlay?.EnergyAlpha ?? 0;
        public CabinMood EffectiveMood { get; private set; }
        public int CaptureEscalationTier => CabinFeedbackRules.CaptureTier(loop?.CapturesToday ?? 0);
        public bool HasPendingScrape => scrapeDelay >= 0;
        public bool IsFlickering => effects != null && effects.IsPlaying(CabinEffects.Flicker);
        public int ImpactReactionCount { get; private set; }
        public int PowerDownReactionCount { get; private set; }
        public int ScrapeReactionCount { get; private set; }
        public int CreakReactionCount { get; private set; }
        public int WarningReactionCount { get; private set; }
        public float WarningSecondsLeft => warningTimer;
        public string NotificationText => effects?.Surface(EffectTarget.Global)?.overlay?.notification?.text;
        public void ShowRockEvent(string text, float seconds) => ShowMessage(text, seconds);
        public bool TransientsSuppressed => applicationPaused || Time.timeScale <= 0 || PauseMenuController.Instance?.IsPaused == true ||
            SceneFlowController.Instance?.IsTransitioning == true || loop != null &&
            (!loop.IsInitialized || loop.IsDeathInProgress || loop.Failed || !string.IsNullOrEmpty(loop.EndingReached));

        private void OnEnable()
        {
            cabin ??= GetComponent<CabinStationView>();
            effects ??= GetComponent<EffectDirector>();
            effects?.BindAudio(AudioManager.Instance);
            navigation = cabin != null ? cabin.Navigation : null;
            catcher = GetComponent<CreatureCatcher>();
            if (navigation != null) { navigation.TerrainImpact += OnImpact; navigation.DepthLimitReached += OnDepthLimit; navigation.MovementRejected += OnMovementRejected; }
            if (catcher != null) catcher.CaptureResolved += OnCapture;
            SceneManager.sceneLoaded += SceneLoaded;
            BindLoop(); ResetPresentation();
        }
        private void Start() { BindLoop(); effects?.BindAudio(AudioManager.Instance); Reevaluate(); }
        private void SceneLoaded(Scene scene, LoadSceneMode mode) => BindLoop();
        private void BindLoop()
        {
            var found = FindAnyObjectByType<ExpeditionLoop>();
            if (loop == found) return;
            if (loop != null) loop.Changed -= OnTimelineChanged;
            loop = found;
            if (loop != null) loop.Changed += OnTimelineChanged;
        }
        private void OnTimelineChanged() { ResetPresentation(); Reevaluate(); }
        private void OnApplicationPause(bool paused)
        { applicationPaused = paused; if (paused) ResetPresentation(); effects?.SetSuspended(TransientsSuppressed); }
        private void OnDisable()
        {
            if (navigation != null) { navigation.TerrainImpact -= OnImpact; navigation.DepthLimitReached -= OnDepthLimit; navigation.MovementRejected -= OnMovementRejected; }
            if (catcher != null) catcher.CaptureResolved -= OnCapture;
            if (loop != null) loop.Changed -= OnTimelineChanged;
            loop = null;
            SceneManager.sceneLoaded -= SceneLoaded;
            ResetPresentation();
            effects?.ClearAll();
        }
        public void ResetPresentation()
        {
            scrapeDelay = -1; pendingTier = 0;
            creakTimer = Range(18, 30); warningTimer = Range(6, 10); previousEnergy = navigation?.Ship.Energy ?? -1;
            effects?.ClearTransients();
        }
        public void Reevaluate()
        {
            if (navigation == null) return;
            float oldHullRatio = HullRatio;
            HullRatio = navigation.Ship.Hull / navigation.Ship.HullCapacity;
            EnergyRatio = navigation.Ship.Energy / navigation.Ship.EnergyCapacity;
            HullTargetAlpha = CabinFeedbackRules.HullAlpha(HullRatio);
            EnergyTargetAlpha = CabinFeedbackRules.EnergyAlpha(EnergyRatio);
            if (oldHullRatio > .25f && HullRatio <= .25f) creakTimer = Mathf.Min(creakTimer, Range(10, 18));
            EffectiveMood = CabinFeedbackRules.Mood(HullRatio, EnergyRatio, cabin.Radar?.IsScanning == true);
            effects?.SetPersistentState(CabinEffects.Hull, HullTargetAlpha > 0, new EffectContext { Alpha = HullTargetAlpha });
            effects?.SetPersistentState(CabinEffects.Energy, EnergyRatio <= .20f,
                new EffectContext { Alpha = EnergyTargetAlpha, Instability = EnergyRatio <= .10f ? 1.3f : 1 });
        }
        private void Update()
        {
            Reevaluate();
            bool blocked = TransientsSuppressed;
            bool transition = SceneFlowController.Instance?.IsTransitioning == true || loop?.IsDeathInProgress == true || loop?.Failed == true || !string.IsNullOrEmpty(loop?.EndingReached);
            effects?.SetSuspended(blocked, transition);
            if (blocked)
            {
                if (!suspended) ResetPresentation();
                suspended = true;
                return;
            }
            // Old requests were cleared on entry; never clear a newly delivered unlock-frame event.
            if (suspended) { previousEnergy = navigation?.Ship.Energy ?? -1; suspended = false; }
            var depthNotice = loop?.ConsumeTransitionDepthNotice();
            if (!string.IsNullOrEmpty(depthNotice)) ShowMessage(depthNotice, 4f);
            if (NotificationText == "DEPTH LIMIT" && !navigation.Ship.CanMove)
                ShowMessage(ResourceMovementWarning, 1.5f);
            if (previousEnergy > 0 && navigation.Ship.Energy <= 0)
            { PowerDownReactionCount++; effects?.Play(CabinEffects.PowerDown); }
            previousEnergy = navigation.Ship.Energy;
            float dt = Time.deltaTime;
            bool modal = cabin.Panels?.IsModalOpen == true;
            if (modal) { scrapeDelay = -1; return; }
            if (HullRatio <= .5f && HullRatio > 0)
            {
                creakTimer -= dt;
                if (creakTimer <= 0) { CreakReactionCount++; effects?.Play(CabinEffects.Creak, new EffectContext { Pitch = Range(.97f, 1.03f) }); creakTimer = HullRatio <= .25f ? Range(10, 18) : Range(18, 30); }
            }
            if (HullRatio <= .10f && HullRatio > 0)
            {
                warningTimer -= dt;
                if (warningTimer <= 0) { WarningReactionCount++; effects?.Play(CabinEffects.Warning); warningTimer = Range(6, 10); }
            }
            if (scrapeDelay >= 0)
            {
                scrapeDelay -= dt;
                if (scrapeDelay <= 0) { scrapeDelay = -1; PlayScrape(); }
            }
        }
        private void OnImpact(float speed, float damage)
        {
            if (damage <= 0 || !CanReact()) return;
            ImpactReactionCount++;
            float severity = Mathf.Clamp01(speed / Mathf.Max(1, navigation.Ship.Speed));
            float flash = severity < .25f ? .05f : Mathf.Lerp(.12f, .30f, severity);
            effects?.Play(CabinEffects.Impact, new EffectContext
            {
                Gain = Mathf.Lerp(.25f, .8f, severity), Pitch = Mathf.Lerp(1.02f, .94f, severity),
                ShakePixels = Mathf.Lerp(1, 7, severity), ShakeSeconds = Mathf.Lerp(.12f, .28f, severity),
                Alpha = flash, Duration = flash / 1.3f
            });
            Reevaluate();
        }
        private void OnDepthLimit()
        {
            if (!CanReact()) return;
            if (!navigation.Ship.CanMove) { ShowMessage(ResourceMovementWarning, 1.5f); return; }
            ShowMessage("DEPTH LIMIT", 1.5f);
            effects?.Play(CabinEffects.Creak, new EffectContext { Gain = .10f });
        }
        private string ResourceMovementWarning => navigation.Ship.Hull <= 0 ? "VESSEL DESTROYED" : "NO ENERGY";
        private void OnMovementRejected(ZoneNavigation.MovementRejection reason)
        { if (CanReact()) ShowMessage(ResourceMovementWarning, 1.5f); }
        private void OnCapture(CreatureCatcher.Result result)
        {
            if (!CanReact()) return;
            if (result != CreatureCatcher.Result.Caught)
            {
                if (result is not (CreatureCatcher.Result.Started or CreatureCatcher.Result.Busy))
                    ShowMessage(CreatureCatcher.FeedbackText(result), 2.5f);
                return;
            }
            ShowMessage(catcher.LastSuccessText, 2.5f);
            pendingTier = CaptureEscalationTier;
            if (pendingTier == 0 || pendingTier == 1 && random.NextDouble() > .35) return;
            scrapeDelay = Range(1, 4);
        }
        private bool CanReact()
        {
            if (TransientsSuppressed) return false;
            // A fresh event can arrive in the frame the fade unlocks, before our next Update.
            // Resume first so Update cannot discard this new reaction as stale transition state.
            if (suspended) { previousEnergy = navigation?.Ship.Energy ?? -1; suspended = false; }
            effects?.SetSuspended(false);
            return true;
        }
        private void PlayScrape()
        {
            if (TransientsSuppressed || cabin.Panels?.IsModalOpen == true || exteriorScrapes.Length == 0) return;
            var clip = exteriorScrapes[random.Next(exteriorScrapes.Length)];
            if (clip == null) return;
            ScrapeReactionCount++;
            effects?.Play(CabinEffects.Scrape, new EffectContext { Clip = clip, Gain = Range(.18f, pendingTier >= 3 ? .4f : .26f), Pitch = Range(.97f, 1.03f) });
            if (pendingTier >= 2) effects?.Play(CabinEffects.Flicker, new EffectContext { Duration = pendingTier >= 3 ? .5f : .22f });
            if (pendingTier >= 3) effects?.Play(CabinEffects.Jitter);
        }
        private void ShowMessage(string text, float seconds)
        { effects?.Play(CabinEffects.Notice, new EffectContext { Text = text, Duration = seconds }); }
        private float Range(float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());
    }
}
