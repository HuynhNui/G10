using System;
using System.Collections.Generic;
using G10.Prototype.Audio;
using G10.Prototype.Computer;
using G10.Prototype.Core;
using G10.Prototype.Missions;
using G10.Prototype.Navigation;
using G10.Prototype.Tutorial;
using G10.Prototype.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace G10.Prototype.Feedback
{
    /// <summary>Zone 1 gameplay-to-presentation adapter. Mission and timeline authority stay with their existing owners.</summary>
    [DisallowMultipleComponent]
    public sealed class ZoneOneAudioController : MonoBehaviour
    {
        public CabinStationView cabin;
        public EffectDirector effects;
        public EffectSequence hullKnocks;
        public AudioClip radarClip, shutterClip;
        [Min(1)] public float algaeRadius = 240, algaeFullRadius = 85, algaeDepthBand = 120;
        [Min(0)] public float quietTravelSeconds = 8, knockTravelDistance = 330;
        public Vector2 knockReference = new(745, 295);
        [Min(1)] public float knockReferenceRadius = 110, deepApproachRadius = 150, departureRadius = 260;
        private ZoneOneStory story;
        private ZoneNavigation navigation;
        private CaptureMinigameController capture;
        private CreatureCatcher catcher;
        private PhotoCaptureService photos;
        private TutorialManager tutorial;
        private CabinZoneSession session;
        private ExpeditionLoop loop;
        private readonly Dictionary<string, EffectHandle> handles = new();
        private float quietTravel, creakIn = 24, lampAge = -1;
        private float departureAge = -1;
        private bool ownsAudio, pendingLamp, capturingAlgae, gapApplied, applicationPaused, handoffPlaying;
        private bool pressurePending;
        private float previousDepth, quietAfterKnock;
        private uint observedVoyageRevision;
        private string boundZone;
        private const string Prefix = "z1.audio.";
        private static readonly string[] Biology = { "ambient_singing_algae", "ambient_algae_harmonics", "ambient_algae_glass_tremors", "ambient_algae_response" };
        public bool IsIntroPlaying { get; private set; }
        public bool IsDeparting => departureAge >= 0;
        public bool IsTransitionPresentation => IsIntroPlaying || IsDeparting;
        public float AlgaeStrength { get; private set; }
        public float ExternalLightLevel { get; private set; }
        public float BioLampElapsed => lampAge;
        public float AlgaeObservation { get; private set; }
        public float RadarResponse { get; private set; }
        public int KnockCount { get; private set; }
        public int FinalSongCount { get; private set; }
        public ExpeditionLoop Expedition => loop;
        private bool InZone => loop != null && loop.IsInitialized && loop.Zone == "Zone01" && story != null && story.UsesBioLampStory;
        private bool Paused => applicationPaused || Time.timeScale <= 0 || PauseMenuController.Instance?.IsPaused == true;
        private bool Safe => InZone && !Paused && !loop.Blocked && !loop.ZoneOneIntroPending && SceneFlowController.Instance?.IsTransitioning != true;
        private bool Piloting => Safe && cabin.Panels != null && !cabin.Panels.IsModalOpen &&
            cabin.Panels.CurrentPanel == cabin.NavigationPanel && !cabin.IsDirectInteractionActive;
        private bool TutorialReady => tutorial == null || !tutorial.IsRunning || tutorial.HasLearned(TutorialStepId.Radar);
        private bool HasCue(string id) => loop?.ZoneOneAudio?.Has(id) == true;
        private MapPoi Site(int index) => story?.config?.locations?.Length > index ? story.survey.FindPoi(story.config.locations[index].poiId) : null;
        private MapPoi Algae => Site(0);
        private MapPoi DeepSite => Site(1);

        private void Awake()
        {
            cabin ??= GetComponent<CabinStationView>(); effects ??= GetComponent<EffectDirector>();
            story = GetComponent<ZoneOneStory>(); navigation = GetComponent<ZoneNavigation>();
            capture = GetComponent<CaptureMinigameController>(); catcher = GetComponent<CreatureCatcher>();
            photos = GetComponent<PhotoCaptureService>(); tutorial = GetComponent<TutorialManager>();
            session = GetComponent<CabinZoneSession>();
        }
        private void OnEnable()
        {
            if (catcher != null) catcher.CaptureResolved += OnCapture;
            if (photos != null) photos.PhotoSubmitted += OnPhotoSubmitted;
            SceneManager.sceneLoaded += SceneLoaded;
            BindLoop();
        }
        private void Start() => BindLoop();
        private void SceneLoaded(Scene scene, LoadSceneMode mode) => BindLoop();
        private void BindLoop()
        {
            var found = cabin?.Panels?.Expedition ?? FindAnyObjectByType<ExpeditionLoop>();
            if (loop == found) return;
            if (loop != null) loop.TimelineRestored -= ResetTimelinePresentation;
            loop = found;
            if (loop != null) loop.TimelineRestored += ResetTimelinePresentation;
        }
        private void OnDisable()
        {
            if (catcher != null) catcher.CaptureResolved -= OnCapture;
            if (photos != null) photos.PhotoSubmitted -= OnPhotoSubmitted;
            if (loop != null) loop.TimelineRestored -= ResetTimelinePresentation;
            SceneManager.sceneLoaded -= SceneLoaded;
            ResetTimelinePresentation(); ReleaseAudio(); IsIntroPlaying = false;
            loop = null;
        }
        private void AcquireAudio()
        {
            if (ownsAudio || AudioManager.Instance == null) return;
            AudioManager.Instance.AcquireZoneAudio(GetInstanceID(), radarClip, shutterClip);
            effects.BindAudio(AudioManager.Instance); ownsAudio = true;
        }
        private void ReleaseAudio()
        { if (ownsAudio) AudioManager.Instance?.ReleaseZoneAudio(GetInstanceID()); ownsAudio = false; }
        // Read navigation after CabinStationView has applied this frame's input or brake.
        private void LateUpdate() => Tick(Time.deltaTime);
        public void Tick(float seconds)
        {
            if (IsDeparting)
            {
                departureAge += Mathf.Max(0, seconds);
                NormalLoops(1 - Mathf.Clamp01(departureAge));
                if (departureAge >= 1) { StopOwned(); departureAge = -1; }
                return;
            }
            if (boundZone != loop?.Zone)
            {
                StopOwned(); ReleaseAudio(); boundZone = loop?.Zone;
                quietTravel = 0; lampAge = -1; pendingLamp = gapApplied = false;
            }
            if (InZone || IsIntroPlaying) AcquireAudio();
            if (InZone && session == null) session = GetComponent<CabinZoneSession>();
            if (IsIntroPlaying) return;
            if (handoffPlaying && !InZone) return;
            if (!Safe) { StopOwned(); AlgaeStrength = AlgaeObservation = RadarResponse = 0; return; }
            float dt = Mathf.Max(0, seconds);
            bool restored = observedVoyageRevision != navigation.VoyageRevision;
            bool descending = !restored && navigation.IsChangingDepth && navigation.Depth > previousDepth + .0001f;
            observedVoyageRevision = navigation.VoyageRevision; previousDepth = navigation.Depth;
            if (restored) pressurePending = false;
            quietAfterKnock = Playing("sfx_hull_knocks_three") ? 8 : Mathf.Max(0, quietAfterKnock - dt);
            if (pendingLamp && !HasCue("lamp"))
            {
                if (Once("lamp", "sfx_biolamp_startup")) { pendingLamp = false; lampAge = 0; gapApplied = false; }
            }
            if (lampAge >= 0) lampAge += dt;
            ExternalLightLevel = story.BioLampInstalled ? (lampAge < 0 ? 1 : Mathf.InverseLerp(1.5f, 2.2f, lampAge)) : 0;
            NormalLoops();
            var algae = Algae;
            AlgaeStrength = !TutorialReady || algae == null ? 0 :
                Mathf.InverseLerp(algaeRadius, algaeFullRadius, Vector2.Distance(navigation.Position, algae.mapPosition)) *
                (1 - Mathf.Clamp01(Mathf.Abs(navigation.Depth - story.survey.DepthFor(algae)) / algaeDepthBand));
            AlgaeObservation = Mathf.MoveTowards(AlgaeObservation, !navigation.IsMoving ? AlgaeStrength : 0, dt / 1.4f);
            RadarResponse = Mathf.MoveTowards(RadarResponse, cabin.Radar?.IsScanning == true && AlgaeStrength > 0 ? 1 : 0, dt * 1.5f);
            bool gap = lampAge >= 4.2f && lampAge < 4.65f;
            if (gap && !gapApplied)
            {
                // Only an audible colony is interrupted. Water and machinery keep running.
                gapApplied = true;
                foreach (var id in Biology) Stop(id);
            }
            if (!gap)
            {
                Loop(Biology[0], AlgaeStrength);
                Loop(Biology[1], AlgaeStrength * .8f);
                Loop(Biology[2], AlgaeStrength * .55f);
                Loop(Biology[3], cabin.Radar?.IsScanning == true ? AlgaeStrength * .25f : 0);
            }
            Loop("sfx_capture_mechanism", capture?.IsActive == true && capturingAlgae ? 1 : 0);
            if (!TutorialReady) return;
            bool moving = Piloting && navigation.IsMoving;
            if (AlgaeStrength <= .01f && moving) quietTravel += dt;
            else if (AlgaeStrength > .01f) quietTravel = 0;
            if (moving && story.HasObjective(ZoneOneStory.AlgaeCaptureObjective) && !story.BioLampInstalled && !HasCue("knocks") && !HasCue("final") &&
                AlgaeStrength <= .01f && quietTravel >= quietTravelSeconds && algae != null &&
                Vector2.Distance(navigation.Position, algae.mapPosition) > algaeRadius + 70 &&
                (Vector2.Distance(navigation.Position, knockReference) <= knockReferenceRadius ||
                    navigation.DistanceTravelled - (loop.ZoneOneAudio?.captureDistance ?? 0) >= knockTravelDistance))
            { if (Once("knocks", "sfx_hull_knocks_three")) { KnockCount++; creakIn = 28; quietAfterKnock = 8; } }
            var deep = DeepSite;
            bool nearDeep = moving && deep != null && Vector2.Distance(navigation.Position, deep.mapPosition) <= deepApproachRadius &&
                Mathf.Abs(navigation.Depth - story.survey.DepthFor(deep)) <= 70;
            if (nearDeep && !HasCue("deep") && !HasCue("final") && !Playing("sfx_hull_knocks_three")) Once("deep", "sfx_deep_specimen_low_tone");
            if (!moving || cabin.IsDirectInteractionActive) Stop("sfx_deep_specimen_low_tone");
            if (moving && navigation.Depth >= 380 && AlgaeStrength <= .01f && !HasCue("final"))
            {
                if (descending && !HasCue("pressure-descent")) pressurePending = true;
                creakIn -= dt;
                if (quietAfterKnock <= 0 && !Playing("sfx_hull_knocks_three") && !Playing("sfx_deep_specimen_low_tone"))
                {
                    if (pressurePending && Once("pressure-descent", "sfx_hull_pressure_creak"))
                    { pressurePending = false; creakIn = 28; }
                    else if (HasCue("pressure-descent") && creakIn <= 0)
                    { Play("sfx_hull_pressure_creak"); creakIn = 28; }
                }
            }
            var exit = session?.ActiveConfig?.exitArea;
            if (moving && story.BioLampInstalled && exit != null && Vector2.Distance(navigation.Position, exit.mapPosition) <= departureRadius && !HasCue("final") && !Playing("sfx_hull_knocks_three"))
                if (Once("final", "sfx_algae_final_echo")) { FinalSongCount++; Stop("sfx_hull_pressure_creak"); Stop("sfx_deep_specimen_low_tone"); }
        }
        private void NormalLoops(float gain = 1)
        {
            Loop("ambient_submarine_engine", gain); Loop("ambient_cabin_ventilation", gain);
            Loop("ambient_water_current", gain); Loop("ambient_deep_water", gain * Mathf.InverseLerp(250, 460, navigation.Depth));
            Loop("ambient_propulsion_thrust", gain * Mathf.Clamp01(Mathf.Abs(navigation.Speed) / Mathf.Max(1, navigation.Ship.Speed)));
            Loop("sfx_steering_servo", Piloting && navigation.IsTurning ? gain : 0);
            Loop("sfx_depth_pressure_adjustment", Piloting && navigation.IsChangingDepth ? gain : 0);
        }
        private void OnCapture(CreatureCatcher.Result result)
        {
            if (!InZone) return;
            if (result == CreatureCatcher.Result.Started)
            {
                var poi = story.survey.FindContactContaining(navigation.Position);
                capturingAlgae = poi != null && story.FindObjective(poi.id, MissionObjectiveType.Capture)?.id == ZoneOneStory.AlgaeCaptureObjective;
                return;
            }
            if (result == CreatureCatcher.Result.Busy) return;
            Stop("sfx_capture_mechanism");
            if (Safe && result == CreatureCatcher.Result.Caught && catcher.LastCapturedId == ZoneOneStory.LuminescentMaterial)
                Once("capture", "sfx_algae_capture_resonance");
            capturingAlgae = false;
        }
        private void OnPhotoSubmitted(PhotoRecord record)
        { if (Safe && record.MissionZoneId == "Zone01") Play("sfx_data_transmit_success"); }
        public void OnBioLampCommitted() { if (InZone) pendingLamp = true; }
        private bool Once(string cue, string sound)
        {
            if (HasCue(cue)) return false;
            var handle = Play(sound);
            if (!handle.IsValid) return false;
            if (loop.CommitZoneOneCue(cue)) return true;
            Stop(sound); return false;
        }
        public void BeginIntro()
        {
            BindLoop(); StopOwned(); boundZone = loop?.Zone;
            IsIntroPlaying = true; AcquireAudio(); effects.SetSuspended(false);
            Play("intro_descent");
        }
        public void IntroStage(int stage)
        {
            if (!IsIntroPlaying) return;
            if (stage == 12) Play("intro_descent_instability");
            if (stage == 17) Play("sfx_intro_warning");
            if (stage == 18) { Play("intro_landing_accident"); Play("landing_shake"); }
            if (stage == 20) { StopOwned(); Play("intro_system_reboot"); }
            if (stage == 27) { NormalLoops(); Play("reboot_flicker"); }
        }
        public void EndIntro() { IsIntroPlaying = false; Stop("intro_system_reboot"); }
        public void BeginDeparture() { departureAge = 0; effects.SetSuspended(false); }
        public void PlayZoneTwoHandoff()
        { StopOwned(); ReleaseAudio(); boundZone = loop?.Zone; handoffPlaying = true; effects.SetSuspended(false); Play("zone02_low_rumble"); }
        private EffectHandle Play(string id)
        {
            if (effects == null) return default;
            // Audio and the three screen impulses share one clock and one cancellable owner.
            var handle = id == "sfx_hull_knocks_three" && hullKnocks != null
                ? effects.PlaySequence(hullKnocks, condition: () => Safe && cabin.Panels?.IsModalOpen != true && !cabin.IsDirectInteractionActive)
                : effects.Play(Prefix + id);
            if (handle.IsValid) handles[id] = handle;
            return handle;
        }
        private bool Playing(string id) => handles.TryGetValue(id, out var handle) && effects.IsActive(handle);
        private void Loop(string id, float gain)
        {
            if (gain <= .001f)
            {
                if (handles.ContainsKey(id)) effects.SetPersistentState(Prefix + id, false);
                handles.Remove(id); return;
            }
            var handle = effects.SetPersistentState(Prefix + id, true, new EffectContext { Gain = gain });
            if (handle.IsValid) handles[id] = handle;
        }
        private void Stop(string id)
        {
            if (handles.TryGetValue(id, out var handle)) effects?.Stop(handle);
            handles.Remove(id);
        }
        private void StopOwned()
        {
            foreach (var handle in handles.Values) effects?.Stop(handle);
            handles.Clear(); handoffPlaying = false;
        }
        private void ResetTimelinePresentation()
        {
            StopOwned(); quietTravel = 0; creakIn = 24; lampAge = -1;
            pendingLamp = gapApplied = capturingAlgae = false; AlgaeStrength = 0;
            AlgaeObservation = RadarResponse = quietAfterKnock = 0; pressurePending = false;
            previousDepth = navigation != null ? navigation.Depth : 0;
            observedVoyageRevision = navigation != null ? navigation.VoyageRevision : 0;
            departureAge = -1;
        }
        private void OnApplicationPause(bool value) { applicationPaused = value; if (value) StopOwned(); }
    }
}
