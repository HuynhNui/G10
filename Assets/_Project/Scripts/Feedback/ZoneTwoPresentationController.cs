using System.Collections.Generic;
using G10.Prototype.Audio;
using G10.Prototype.Computer;
using G10.Prototype.Core;
using G10.Prototype.Missions;
using G10.Prototype.Navigation;
using G10.Prototype.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace G10.Prototype.Feedback
{
    /// <summary>Zone 2 presentation adapter; expedition/save and mission owners retain all progression authority.</summary>
    [DisallowMultipleComponent]
    public sealed class ZoneTwoPresentationController : MonoBehaviour
    {
        public CabinStationView cabin;
        public EffectDirector effects;
        public EffectSequence radarDisturbance, scrapeDisturbance, reflection;
        public AudioClip radarClip, shutterClip;
        private ZoneOneStory story;
        private ZoneNavigation navigation;
        private CreatureCatcher catcher;
        private CaptureMinigameController capture;
        private PhotoCaptureService photos;
        private ComputerScreenController computer;
        private CabinZoneSession session;
        private ExpeditionLoop loop;
        private readonly Dictionary<string, EffectHandle> handles = new();
        private string boundZone;
        private float quietTime, rootTimer = 24, installAge = -1, departureAge = -1;
        private bool ownsAudio, applicationPaused, pendingInstall;
        public bool IsDeparting => departureAge >= 0;
        public float HullInstallElapsed => installAge;
        public int EnvironmentCount { get; private set; }
        public int RadarEventCount { get; private set; }
        public int KnockCount { get; private set; }
        public int ScrapeEventCount { get; private set; }
        public int ReflectionCount { get; private set; }
        public bool InZone => loop != null && loop.IsInitialized && loop.Zone == "Zone02" && story != null && story.UsesZoneTwoHullStory;
        private bool Safe => InZone && !applicationPaused && Time.timeScale > 0 && !loop.Blocked &&
            PauseMenuController.Instance?.IsPaused != true && SceneFlowController.Instance?.IsTransitioning != true;
        private bool Unobstructed => Safe && cabin.Panels?.IsModalOpen != true && !cabin.IsDirectInteractionActive && capture?.IsActive != true;
        private bool Helm => cabin.Panels?.CurrentPanel == cabin.NavigationPanel;
        private bool Has(string cue) => loop?.ZoneTwoPresentation?.Has(cue) == true;
        private bool Busy => effects.ActiveSequenceCount > 0 || Playing("knocks_rear");
        private void Awake()
        {
            cabin ??= GetComponent<CabinStationView>(); effects ??= GetComponent<EffectDirector>();
            story = GetComponent<ZoneOneStory>(); navigation = GetComponent<ZoneNavigation>();
            catcher = GetComponent<CreatureCatcher>(); capture = GetComponent<CaptureMinigameController>();
            photos = GetComponent<PhotoCaptureService>(); session = GetComponent<CabinZoneSession>();
            computer = GetComponentInChildren<ComputerScreenController>(true);
        }
        private void OnEnable()
        {
            catcher.CaptureResolved += OnCapture; photos.PhotoSubmitted += OnPhoto;
            SceneManager.sceneLoaded += SceneLoaded; BindLoop();
        }
        private void Start() => BindLoop();
        private void SceneLoaded(Scene _, LoadSceneMode __) => BindLoop();
        private void BindLoop()
        {
            var value = cabin?.Panels?.Expedition ?? FindAnyObjectByType<ExpeditionLoop>();
            if (loop == value) return;
            if (loop != null) loop.TimelineRestored -= ResetPresentation;
            loop = value; if (loop != null) loop.TimelineRestored += ResetPresentation;
        }
        private void OnDisable()
        {
            catcher.CaptureResolved -= OnCapture; photos.PhotoSubmitted -= OnPhoto;
            SceneManager.sceneLoaded -= SceneLoaded;
            if (loop != null) loop.TimelineRestored -= ResetPresentation;
            ResetPresentation(); ReleaseAudio(); loop = null;
        }
        private void LateUpdate() => Tick(Time.deltaTime);
        public void Tick(float seconds)
        {
            float dt = Mathf.Max(0, seconds);
            if (IsDeparting)
            {
                departureAge += dt; NormalLoops(1 - Mathf.Clamp01(departureAge / 2));
                if (departureAge >= 4.1f) { StopOwned(); departureAge = -1; }
                return;
            }
            if (boundZone != loop?.Zone) { ResetPresentation(); ReleaseAudio(); boundZone = loop?.Zone; }
            if (!InZone) return;
            // The expedition adds this shared session during its own initialization, after our Awake.
            session ??= GetComponent<CabinZoneSession>();
            if (!ownsAudio && AudioManager.Instance != null)
            { AudioManager.Instance.AcquireZoneAudio(GetInstanceID(), radarClip, shutterClip); effects.BindAudio(AudioManager.Instance); ownsAudio = true; }
            if (!Safe) { StopOwned(); return; }
            if (installAge >= 0) installAge += dt;
            if (pendingInstall && !Has("install"))
                if (Once("install", "hull_install")) { pendingInstall = false; installAge = 0; }
            NormalLoops();
            Loop("z2.screen.cold", 1);
            var poi = story.survey.FindContactContaining(navigation.Position);
            bool nearby = poi != null && Mathf.Abs(navigation.Depth - story.survey.DepthFor(poi)) < 40;
            Loop("z2.audio.specimen_adhesive", nearby && poi.id == "zone02-l1" ? 1 : 0);
            Loop("z2.audio.specimen_armor", nearby && poi.id == "zone02-l3" ? 1 : 0);
            Loop("z1.audio.sfx_capture_mechanism", capture.IsActive ? .7f : 0);
            if (!Unobstructed) return;
            quietTime += dt;
            if (!Busy && !nearby)
            {
                rootTimer -= dt;
                if (rootTimer <= 0)
                { Play(EnvironmentCount++ % 2 == 0 ? "root_flex" : "root_friction"); rootTimer = 28 + EnvironmentCount % 3 * 7; }
            }
            // Resuming an old fieldwork checkpoint may not have an audio receipt yet.
            if (!story.ZoneTwoHullInstalled && story.HasObjective("Z2_L1_CAPTURE") && !Has("adhesive")) Once("adhesive", "capture_adhesive");
            float travel = navigation.DistanceTravelled - (loop.ZoneTwoPresentation?.firstCaptureDistance ?? navigation.DistanceTravelled);
            if (!story.ZoneTwoHullInstalled && Has("adhesive") && !Busy)
            {
                if (!Has("radar") && travel >= 150 && quietTime >= 8 && cabin.RadarPanel.activeInHierarchy && cabin.Radar?.IsScanning == true)
                { if (Once("radar", radarDisturbance)) RadarEventCount++; }
                else if (Has("radar") && !Has("knocks") && travel >= 300 && quietTime >= 10 && Helm && navigation.IsMoving)
                { if (Once("knocks", "knocks_rear")) KnockCount++; }
                else if (Has("knocks") && !Has("scrape") && travel >= 500 && quietTime >= 12 && computer.gameObject.activeInHierarchy)
                { if (Once("scrape", scrapeDisturbance)) ScrapeEventCount++; }
            }
            var exit = session?.ActiveConfig?.exitArea;
            if (story.ZoneTwoHullInstalled && !Has("reflection") && !Busy && quietTime >= 8 && Helm && navigation.IsMoving &&
                exit != null && Vector2.Distance(navigation.Position, exit.mapPosition) <= 400)
                if (Once("reflection", reflection)) ReflectionCount++;
        }
        private void NormalLoops(float gain = 1)
        {
            Loop("z1.audio.ambient_submarine_engine", gain); Loop("z1.audio.ambient_cabin_ventilation", gain);
            Loop("z1.audio.ambient_water_current", gain); Loop("z2.audio.ambient_roots", gain);
            Loop("z1.audio.ambient_propulsion_thrust", gain * Mathf.Clamp01(Mathf.Abs(navigation.Speed) / Mathf.Max(1, navigation.Ship.Speed)));
            Loop("z1.audio.sfx_steering_servo", Unobstructed && Helm && navigation.IsTurning ? gain : 0);
            Loop("z1.audio.sfx_depth_pressure_adjustment", Unobstructed && Helm && navigation.IsChangingDepth ? gain : 0);
        }
        private void OnCapture(CreatureCatcher.Result result)
        {
            if (!Safe || result != CreatureCatcher.Result.Caught) return;
            bool adhesive = catcher.LastCapturedId == "Z2_Creature_01";
            if (!adhesive && catcher.LastCapturedId != "Z2_Creature_02") return;
            if (adhesive && !Has("adhesive")) Once("adhesive", "capture_adhesive");
            else Play(adhesive ? "capture_adhesive" : "capture_armor");
            GetComponent<CabinFeedbackController>()?.ShowRockEvent(adhesive ? "ADHESIVE MATERIAL ACQUIRED\nHARDENED MATERIAL STILL REQUIRED" :
                story.MainLocationsComplete ? "HARDENED MATERIAL ACQUIRED\nPRESSURE HULL MATERIALS READY" : "HARDENED MATERIAL ACQUIRED", 3);
        }
        private void OnPhoto(PhotoRecord _) { if (Safe) PlayId("z1.audio.sfx_data_transmit_success"); }
        public void OnHullInstalled() { if (InZone) pendingInstall = true; }
        public void BeginDeparture()
        { departureAge = 0; effects.SetSuspended(false); Play("far_root_groan"); }
        private bool Once(string cue, string sound) => Once(cue, () => Play(sound));
        private bool Once(string cue, EffectSequence sequence) => Once(cue, () => effects.PlaySequence(sequence, condition: () => Unobstructed));
        private bool Once(string cue, System.Func<EffectHandle> play)
        {
            if (Has(cue)) return false;
            var handle = play(); if (!handle.IsValid) return false;
            if (!loop.CommitZoneTwoCue(cue)) { effects.Stop(handle); return false; }
            handles["cue." + cue] = handle; quietTime = 0; return true;
        }
        private EffectHandle Play(string id) => PlayId("z2.audio." + id);
        private EffectHandle PlayId(string id)
        { var handle = effects.Play(id); if (handle.IsValid) handles[id] = handle; return handle; }
        private bool Playing(string id) => handles.TryGetValue("z2.audio." + id, out var handle) && effects.IsActive(handle);
        private void Loop(string id, float gain)
        {
            if (gain <= .001f) { if (handles.TryGetValue(id, out var h)) effects.Stop(h); handles.Remove(id); return; }
            var handle = effects.SetPersistentState(id, true, new EffectContext { Intensity = gain });
            if (handle.IsValid) handles[id] = handle;
        }
        private void StopOwned() { foreach (var handle in handles.Values) effects?.Stop(handle); handles.Clear(); }
        private void ResetPresentation() { StopOwned(); quietTime = 0; rootTimer = 24; installAge = departureAge = -1; pendingInstall = false; }
        private void ReleaseAudio() { if (ownsAudio) AudioManager.Instance?.ReleaseZoneAudio(GetInstanceID()); ownsAudio = false; }
        private void OnApplicationPause(bool value) { applicationPaused = value; if (value) StopOwned(); }
    }
}
