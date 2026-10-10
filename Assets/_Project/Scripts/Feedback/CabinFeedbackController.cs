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
        [SerializeField] private CabinAtmosphere atmosphere;
        [SerializeField] private ImpactShake impactShake;
        [SerializeField] private UnityEngine.UI.RawImage lowHullOverlay, lowEnergyOverlay;
        [SerializeField] private TMP_Text notification;
        [SerializeField] private AudioClip hullImpact, hullCreak, criticalWarning, lowPowerHum, powerDown;
        [SerializeField] private AudioClip[] exteriorScrapes = Array.Empty<AudioClip>();
        [SerializeField, Range(.3f, .6f)] private float overlayFadeSeconds = .4f;
        private ExpeditionLoop loop;
        private ZoneNavigation navigation;
        private CreatureCatcher catcher;
        private readonly System.Random random = new();
        private float previousEnergy = -1, creakTimer, warningTimer, scrapeDelay = -1;
        private float flash, flickerTime, messageTime;
        private int pendingTier;
        private bool suspended, applicationPaused;
        public float HullRatio { get; private set; } = 1;
        public float EnergyRatio { get; private set; } = 1;
        public float HullTargetAlpha { get; private set; }
        public float EnergyTargetAlpha { get; private set; }
        public float HullOverlayAlpha => lowHullOverlay != null ? lowHullOverlay.color.a : 0;
        public float EnergyOverlayAlpha => lowEnergyOverlay != null ? lowEnergyOverlay.color.a : 0;
        public CabinMood EffectiveMood { get; private set; }
        public int CaptureEscalationTier => CabinFeedbackRules.CaptureTier(loop?.CapturesToday ?? 0);
        public bool HasPendingScrape => scrapeDelay >= 0;
        public bool IsFlickering => flickerTime > 0;
        public int ImpactReactionCount { get; private set; }
        public int PowerDownReactionCount { get; private set; }
        public int ScrapeReactionCount { get; private set; }
        public int CreakReactionCount { get; private set; }
        public int WarningReactionCount { get; private set; }
        public float WarningSecondsLeft => warningTimer;
        public string NotificationText => notification != null ? notification.text : null;
        public bool TransientsSuppressed => applicationPaused || Time.timeScale <= 0 || PauseMenuController.Instance?.IsPaused == true ||
            SceneFlowController.Instance?.IsTransitioning == true || loop != null &&
            (!loop.IsInitialized || loop.IsDeathInProgress || loop.Failed || !string.IsNullOrEmpty(loop.EndingReached));

        private void OnEnable()
        {
            cabin ??= GetComponent<CabinStationView>();
            navigation = cabin != null ? cabin.Navigation : null;
            catcher = GetComponent<CreatureCatcher>();
            if (navigation != null) { navigation.TerrainImpact += OnImpact; navigation.DepthLimitReached += OnDepthLimit; navigation.MovementRejected += OnMovementRejected; }
            if (catcher != null) catcher.CaptureResolved += OnCapture;
            SceneManager.sceneLoaded += SceneLoaded;
            BindLoop(); ResetPresentation();
        }
        private void Start() { BindLoop(); Reevaluate(); }
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
        private void OnApplicationPause(bool paused) => applicationPaused = paused;
        private void OnDisable()
        {
            if (navigation != null) { navigation.TerrainImpact -= OnImpact; navigation.DepthLimitReached -= OnDepthLimit; navigation.MovementRejected -= OnMovementRejected; }
            if (catcher != null) catcher.CaptureResolved -= OnCapture;
            if (loop != null) loop.Changed -= OnTimelineChanged;
            loop = null;
            SceneManager.sceneLoaded -= SceneLoaded;
            ResetPresentation();
            atmosphere?.SetResourceFeedback(null);
        }
        public void ResetPresentation()
        {
            scrapeDelay = -1; pendingTier = 0; flash = flickerTime = messageTime = 0;
            creakTimer = Range(18, 30); warningTimer = Range(6, 10); previousEnergy = navigation?.Ship.Energy ?? -1;
            impactShake?.Clear();
            if (notification != null) notification.text = "";
            AudioManager.Instance?.StopFeedback();
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
            CabinMood? resource = HullRatio <= .25f ? CabinMood.Danger : EnergyRatio <= .20f ? CabinMood.LowPower : null;
            float flicker = flickerTime > 0 ? (.5f + .5f * Mathf.Sin(flickerTime * 48)) * .35f : 0;
            atmosphere?.SetResourceFeedback(resource, EnergyRatio <= .10f ? 1.3f : 1, flicker);
        }
        private void Update()
        {
            Reevaluate();
            bool blocked = TransientsSuppressed;
            bool transition = SceneFlowController.Instance?.IsTransitioning == true || loop?.IsDeathInProgress == true || loop?.Failed == true || !string.IsNullOrEmpty(loop?.EndingReached);
            if (blocked)
            {
                if (!suspended) ResetPresentation();
                suspended = true;
                AudioManager.Instance?.SetFeedbackPaused(true);
                if (transition) { SetAlpha(lowHullOverlay, 0); SetAlpha(lowEnergyOverlay, 0); }
                return;
            }
            if (suspended) { ResetPresentation(); suspended = false; }
            AudioManager.Instance?.SetFeedbackPaused(false);
            var depthNotice = loop?.ConsumeTransitionDepthNotice();
            if (!string.IsNullOrEmpty(depthNotice)) ShowMessage(depthNotice, 4f);
            if (NotificationText == "DEPTH LIMIT" && !navigation.Ship.CanMove)
                ShowMessage(ResourceMovementWarning, 1.5f);
            var audio = AudioManager.Instance;
            if (EnergyRatio <= .20f && lowPowerHum != null) audio?.StartFeedbackLoop(lowPowerHum, .35f);
            else audio?.StopFeedbackLoop();
            if (previousEnergy > 0 && navigation.Ship.Energy <= 0)
            { PowerDownReactionCount++; audio?.PlayFeedbackOneShot(powerDown, .6f); }
            previousEnergy = navigation.Ship.Energy;
            float dt = Time.deltaTime;
            flash = Mathf.MoveTowards(flash, 0, dt * 1.3f);
            flickerTime = Mathf.Max(0, flickerTime - dt);
            Fade(lowHullOverlay, Mathf.Max(HullTargetAlpha, flash), dt);
            Fade(lowEnergyOverlay, Mathf.Min(.30f, EnergyTargetAlpha + (flickerTime > 0 ? Mathf.Abs(Mathf.Sin(flickerTime * 48)) * .04f : 0)), dt);
            messageTime = Mathf.Max(0, messageTime - dt);
            if (messageTime <= 0 && notification != null) notification.text = "";
            bool modal = cabin.Panels?.IsModalOpen == true;
            if (modal) { scrapeDelay = -1; return; }
            if (HullRatio <= .5f && HullRatio > 0)
            {
                creakTimer -= dt;
                if (creakTimer <= 0) { CreakReactionCount++; audio?.PlayFeedbackOneShot(hullCreak, .28f, Range(.97f, 1.03f)); creakTimer = HullRatio <= .25f ? Range(10, 18) : Range(18, 30); }
            }
            if (HullRatio <= .10f && HullRatio > 0)
            {
                warningTimer -= dt;
                if (warningTimer <= 0) { WarningReactionCount++; audio?.PlayFeedbackOneShot(criticalWarning, .38f); warningTimer = Range(6, 10); }
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
            AudioManager.Instance?.PlayFeedbackOneShot(hullImpact, Mathf.Lerp(.25f, .8f, severity), Mathf.Lerp(1.02f, .94f, severity));
            impactShake?.Shake(Mathf.Lerp(1, 7, severity), Mathf.Lerp(.12f, .28f, severity));
            flash = severity < .25f ? .05f : Mathf.Lerp(.12f, .30f, severity);
            // Impacts peak immediately, then fade back to the smoothly derived resource baseline.
            if (lowHullOverlay != null) SetAlpha(lowHullOverlay, Mathf.Max(lowHullOverlay.color.a, flash));
            Reevaluate();
        }
        private void OnDepthLimit()
        {
            if (!CanReact()) return;
            if (!navigation.Ship.CanMove) { ShowMessage(ResourceMovementWarning, 1.5f); return; }
            ShowMessage("DEPTH LIMIT", 1.5f);
            AudioManager.Instance?.PlayFeedbackOneShot(hullCreak, .10f);
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
            if (suspended) { ResetPresentation(); suspended = false; }
            AudioManager.Instance?.SetFeedbackPaused(false);
            return true;
        }
        private void PlayScrape()
        {
            if (TransientsSuppressed || cabin.Panels?.IsModalOpen == true || exteriorScrapes.Length == 0) return;
            var clip = exteriorScrapes[random.Next(exteriorScrapes.Length)];
            if (clip == null) return;
            ScrapeReactionCount++;
            AudioManager.Instance?.PlayFeedbackOneShot(clip, Range(.18f, pendingTier >= 3 ? .4f : .26f), Range(.97f, 1.03f));
            if (pendingTier >= 2) flickerTime = pendingTier >= 3 ? .5f : .22f;
            if (pendingTier >= 3) impactShake?.Shake(1.5f, .16f);
        }
        private void ShowMessage(string text, float seconds)
        { if (notification != null) notification.text = text; messageTime = seconds; }
        private float Range(float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());
        private void Fade(UnityEngine.UI.RawImage image, float target, float dt)
        { if (image != null) SetAlpha(image, Mathf.Lerp(image.color.a, target, 1 - Mathf.Exp(-3 * dt / Mathf.Max(.01f, overlayFadeSeconds)))); }
        private static void SetAlpha(UnityEngine.UI.RawImage image, float alpha)
        { if (image == null) return; Color color = image.color; color.a = alpha; image.color = color; }
    }
}
