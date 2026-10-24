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
        public EffectSequence radarDisturbance, scrapeDisturbance, reflection, following;
        public Material photoMotionTemplate;
        public AudioClip radarClip, shutterClip;
        private ZoneOneStory story;
        private ZoneNavigation navigation;
        private CreatureCatcher catcher;
        private CaptureMinigameController capture;
        private PhotoCaptureService photos;
        private ComputerScreenController computer;
        private CabinZoneSession session;
        private ExpeditionLoop loop;
        private EffectAudioExecutor audio;
        private PhotoLabView lab;
        private readonly ZoneTwoPhotoMotion photoMotion = new();
        private sealed class Delivery
        {
            public string Cue;
            public EffectHandle Handle;
            public bool Sequence, Committed, SawSignal, SawPeak;
            public float Threshold;
        }
        private Delivery delivery;
        private float propulsionAge, stoppedAge, motionAge;
        private bool wasPropelling, stoppedOpportunity;
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
        public int FollowingCount { get; private set; }
        public bool PhotoMotionVisible => photoMotion.Visible;
        public string PendingCue => delivery?.Cue;
        public string ExperienceStage => Has("scrape") ? "Aftermath" : Has("knocks") ? "Escalation" :
            Has("following") || Has("radar") ? "Followed" : Has("adhesive") ? "Suspicious" : "Normal";
        public bool InZone => loop != null && loop.IsInitialized && loop.Zone == "Zone02" && story != null && story.UsesZoneTwoHullStory;
        private bool Safe => InZone && !applicationPaused && Time.timeScale > 0 && !loop.Blocked &&
            PauseMenuController.Instance?.IsPaused != true && SceneFlowController.Instance?.IsTransitioning != true;
        private bool Unobstructed => Safe && cabin.Panels?.IsModalOpen != true && !cabin.IsDirectInteractionActive && capture?.IsActive != true;
        private bool Helm => cabin.Panels?.CurrentPanel == cabin.NavigationPanel;
        private bool Has(string cue) => loop?.ZoneTwoPresentation?.Has(cue) == true;
        private bool Busy => delivery != null || effects.ActiveSequenceCount > 0 || Playing("knocks_rear") ||
            Playing("hull_install") || Playing("capture_adhesive") || Playing("capture_armor");
        private void Awake()
        {
            cabin ??= GetComponent<CabinStationView>(); effects ??= GetComponent<EffectDirector>();
            story = GetComponent<ZoneOneStory>(); navigation = GetComponent<ZoneNavigation>();
            catcher = GetComponent<CreatureCatcher>(); capture = GetComponent<CaptureMinigameController>();
            photos = GetComponent<PhotoCaptureService>(); session = GetComponent<CabinZoneSession>();
            computer = GetComponentInChildren<ComputerScreenController>(true);
            audio = GetComponent<EffectAudioExecutor>();
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
            if (!Safe) { StopOwned(); quietTime = 0; return; }
            UpdateDelivery();
            if (installAge >= 0) installAge += dt;
            if (pendingInstall && !Has("install"))
                if (Once("install", "hull_install")) { pendingInstall = false; installAge = 0; }
            NormalLoops();
            motionAge += dt;
            if (computer != null && computer.gameObject.activeInHierarchy && computer.CurrentApp == ComputerAppId.PhotoLab)
                lab ??= computer.GetComponentInChildren<PhotoLabView>(true);
            photoMotion.Render(lab != null ? lab.preview : null, photoMotionTemplate,
                Unobstructed && computer != null && computer.gameObject.activeInHierarchy && computer.CurrentApp == ComputerAppId.PhotoLab,
                motionAge, Has("radar") || Has("following") ? .85f : .45f);
            Loop("z2.screen.cold", 1);
            var poi = story.survey.FindContactContaining(navigation.Position);
            bool nearby = poi != null && Mathf.Abs(navigation.Depth - story.survey.DepthFor(poi)) < 40;
            Loop("z2.audio.specimen_adhesive", nearby && poi.id == "zone02-l1" ? 1 : 0);
            Loop("z2.audio.specimen_armor", nearby && poi.id == "zone02-l3" ? 1 : 0);
            Loop("z1.audio.sfx_capture_mechanism", capture.IsActive ? .7f : 0);
            if (!Unobstructed) { CancelDelivery(); ClearStopOpportunity(); return; }
            ObservePropulsionStop(dt);
            // A quiet interval starts after the previous dramatic delivery has ceased.
            if (!Busy) quietTime += dt;
            if (!Busy && !nearby)
            {
                rootTimer -= dt;
                if (rootTimer <= 0)
                { Play(EnvironmentCount++ % 2 == 0 ? "root_flex" : "root_friction");
                    rootTimer = 25 + Mathf.Repeat(navigation.DistanceTravelled * .031f + EnvironmentCount * 11.37f, 29); }
            }
            // Resuming an old fieldwork checkpoint may not have an audio receipt yet.
            if (story.HasObjective("Z2_L1_CAPTURE") && !Has("adhesive")) Once("adhesive", "capture_adhesive");
            float travel = navigation.DistanceTravelled - (loop.ZoneTwoPresentation?.firstCaptureDistance ?? navigation.DistanceTravelled);
            if (Has("adhesive") && !Busy)
            {
                if (!Has("radar") && travel >= 150 && quietTime >= 8 && cabin.RadarPanel.activeInHierarchy && cabin.Radar?.IsScanning == true)
                { if (Once("radar", radarDisturbance)) RadarEventCount++; }
                else if ((Has("radar") || Has("following")) && !Has("knocks") && travel >= 300 && quietTime >= 10 && Helm && navigation.IsMoving)
                { if (Once("knocks", "knocks_rear")) KnockCount++; }
                else if (Has("knocks") && !Has("scrape") && travel >= 500 && quietTime >= 12 && computer.gameObject.activeInHierarchy)
                { if (Once("scrape", scrapeDisturbance)) ScrapeEventCount++; }
                else if (!Has("following") && stoppedOpportunity && stoppedAge >= 1.1f && travel >= 120 && quietTime >= 7 && !nearby)
                { if (Once("following", following)) { FollowingCount++; stoppedOpportunity = false; } }
            }
            var exit = session?.ActiveConfig?.exitArea;
            if (story.ZoneTwoHullInstalled && !Has("reflection") && !Busy && quietTime >= 8 && Helm && navigation.IsMoving &&
                exit != null && Vector2.Distance(navigation.Position, exit.mapPosition) <= 400)
                if (Once("reflection", reflection)) ReflectionCount++;
        }
        private void NormalLoops(float gain = 1)
        {
            float outside = delivery?.Cue == "following" ? .28f : Has("scrape") ? .7f : 1;
            Loop("z1.audio.ambient_submarine_engine", gain); Loop("z1.audio.ambient_cabin_ventilation", gain);
            Loop("z1.audio.ambient_water_current", gain * outside); Loop("z2.audio.ambient_roots", gain * outside);
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
        public void OnHullInstalled() { if (InZone) { CancelDelivery(); pendingInstall = true; } }
        public void BeginDeparture()
        { departureAge = 0; effects.SetSuspended(false); Play("far_root_groan"); }
        private bool Audience(string cue) => cue == "radar" ? cabin.RadarPanel.activeInHierarchy :
            cue == "reflection" ? Helm : cue == "scrape" ? computer.gameObject.activeInHierarchy : true;
        private bool Once(string cue, string sound) => cue == "knocks" ?
            BeginDelivery(cue, Play(sound), false, 1.1f) : Once(cue, () => Play(sound));
        private bool Once(string cue, EffectSequence sequence)
        {
            if (Has(cue) || delivery != null || sequence == null) return false;
            return BeginDelivery(cue, effects.PlaySequence(sequence, condition: () => Unobstructed && Audience(cue)), true,
                cue == "radar" ? .9f : cue == "scrape" ? 3.9f : cue == "reflection" ? 1.15f : .85f);
        }
        private bool BeginDelivery(string cue, EffectHandle handle, bool sequence, float threshold)
        {
            if (Has(cue) || delivery != null || !handle.IsValid) { if (handle.IsValid) effects.Stop(handle); return false; }
            delivery = new Delivery { Cue = cue, Handle = handle, Sequence = sequence, Threshold = threshold };
            handles["cue." + cue] = handle; quietTime = 0; return true;
        }
        private void UpdateDelivery()
        {
            if (delivery == null) return;
            if (!Unobstructed || !Audience(delivery.Cue)) { CancelDelivery(); return; }
            if (!effects.IsActive(delivery.Handle)) { delivery = null; quietTime = 0; return; }
            float age = 0;
            bool clock = delivery.Sequence ? effects.TryGetSequenceTime(delivery.Handle, out age) :
                audio != null && audio.TryPlaybackTime(delivery.Handle.Id, out age);
            if (!clock) return;
            if (delivery.Cue == "scrape")
            {
                delivery.SawSignal |= effects.Surface(EffectTarget.Computer)?.visual?.SignalVisible == true;
                var overlay = effects.Surface(EffectTarget.Global)?.overlay;
                delivery.SawPeak |= age >= 3.5f && overlay != null && overlay.tint != null && overlay.tint.color.a >= .30f;
            }
            if (delivery.Committed || age < delivery.Threshold) return;
            var surface = effects.Surface(delivery.Cue == "radar" ? EffectTarget.Radar : EffectTarget.Reflection);
            if (delivery.Cue == "radar" && surface?.visual?.SignalVisible != true ||
                delivery.Cue == "reflection" && surface?.visual?.AnomalyVisible != true) return;
            if (delivery.Cue == "scrape" && (!delivery.SawSignal || !delivery.SawPeak ||
                !effects.TryGetAudioTime("z2.audio.hull_scrape", out float scrapeTime) || scrapeTime < 3.2f)) return;
            if (delivery.Cue == "following" && (!effects.TryGetAudioTime("z2.audio.following_tail", out float tailTime) || tailTime < .55f)) return;
            if (!loop.CommitZoneTwoCue(delivery.Cue)) { CancelDelivery(); return; }
            delivery.Committed = true;
        }
        private void CancelDelivery()
        {
            if (delivery != null) effects?.Stop(delivery.Handle);
            delivery = null; quietTime = 0;
        }
        private void ObservePropulsionStop(float seconds)
        {
            bool propelling = Helm && Mathf.Abs(navigation.Speed) >= 2 && navigation.IsMoving;
            if (propelling) { propulsionAge += seconds; stoppedAge = 0; stoppedOpportunity = false; }
            else
            {
                if (wasPropelling && propulsionAge >= 4) stoppedOpportunity = true;
                stoppedAge += seconds; propulsionAge = 0;
            }
            wasPropelling = propelling;
        }
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
        private void ClearStopOpportunity()
        { propulsionAge = stoppedAge = 0; wasPropelling = stoppedOpportunity = false; }
        private void StopOwned() { foreach (var handle in handles.Values) effects?.Stop(handle); handles.Clear(); delivery = null; photoMotion.Restore(); ClearStopOpportunity(); }
        private void ResetPresentation() { StopOwned(); quietTime = 0; rootTimer = 24; installAge = departureAge = -1; pendingInstall = false;
            propulsionAge = stoppedAge = motionAge = 0; wasPropelling = stoppedOpportunity = false; lab = null; }
        private void ReleaseAudio() { if (ownsAudio) AudioManager.Instance?.ReleaseZoneAudio(GetInstanceID()); ownsAudio = false; }
        private void OnApplicationPause(bool value) { applicationPaused = value; if (value) StopOwned(); }
        private void OnDestroy() => photoMotion.Dispose();
    }
}
