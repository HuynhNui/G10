using System;
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
    public enum ZoneThreeDemo { DelayedEngine, FalseRadar, Reflection, Demolition }

    /// <summary>Mission/device observations select authored EffectDirector sequences. No gameplay authority.</summary>
    [DisallowMultipleComponent]
    public sealed class ZoneThreePresentationController : MonoBehaviour
    {
        public CabinStationView cabin;
        public EffectDirector effects;
        public EffectSequence delayedEngine, falseRadar, reflection, assembly, demolition, specimen07;
        public AudioClip radarClip, shutterClip;
        private ZoneOneStory story;
        private ZoneNavigation navigation;
        private CreatureCatcher catcher;
        private CaptureMinigameController capture;
        private PhotoCaptureService photos;
        private PhotoLabView lab;
        private RockBreakerSequence rock;
        private ExpeditionLoop loop;
        private readonly Dictionary<string, EffectHandle> handles = new();
        private enum PrimaryCue { Engine, Radar, Reflection }
        private sealed class PrimaryDelivery
        {
            public PrimaryCue Cue;
            public EffectHandle Handle;
            public bool Delivered;
            public float Exposure, FirstAudio = -1, LastAudio = -1, FirstObservation, Retry;
            public int ObservationFrame = -1;
        }
        private PrimaryDelivery primary;
        private string boundZone, observedPhoto;
        private float quietTime, resonanceTimer = 7, movingSeconds, environment = 1, beyondAge;
        private float? radarObservationDistance;
        private float opportunityRetry, reflectionOpportunityRetry;
        private bool wasMoving, ownsAudio, applicationPaused, engineArmed, reflectionOpportunityObserved, hadSequence, applicationFocused = true;
        public bool InZone => loop != null && loop.IsInitialized && loop.Zone == "Zone03";
        public float EnvironmentLevel => environment;
        public int EngineEventCount { get; private set; }
        public int RadarEventCount { get; private set; }
        public int ReflectionCount { get; private set; }
        public int StoneCount { get; private set; }
        public string PendingCue => primary != null ? PrimaryId(primary.Cue) + (primary.Delivered ? ".delivered-awaiting-publication" : ".playing") :
            radarObservationDistance.HasValue ? "radar.opportunity-awaiting-publication" :
            reflectionOpportunityObserved ? "reflection.opportunity-awaiting-publication" :
            Has(ZoneThreePresentationState.RadarOpportunity) && !Has("radar") ? "radar.armed" :
            engineArmed && !Has("engine") ? "engine.armed" :
            Has(ZoneThreePresentationState.ReflectionOpportunity) && !Has("reflection") ? "reflection.armed" : "none";
        public string ExperienceStage
        {
            get
            {
                if (!InZone) return "outside-zone03";
                if (Broken) return rock?.IsActive == true ? "demolition" : "beyond";
                if (story.RockBreakerInstalled) return "rock-breaker-installed";
                if (Has("reflection")) return story.HasObjective("Z3_L3_CAPTURE") ? "assembly" : "specimen07";
                if (Has(ZoneThreePresentationState.ReflectionOpportunity)) return "deep-passage";
                if (Has("radar") && navigation.DistanceTravelled - loop.ZoneThreePresentation.radarDistance >= 100)
                    return navigation.Depth >= 600 ? "deep-passage" : "descending-passage";
                if (Has("radar")) return "radar-delivered";
                if (Has("engine")) return "engine-delivered";
                return story.HasObjective("Z3_L1_CAPTURE") ? "after-specimen05" : "mineral-ruins";
            }
        }
        private bool Safe => InZone && !applicationPaused && Time.timeScale > 0 && !loop.Failed && !loop.IsDeathInProgress && navigation.Ship.Hull > 0 && (!loop.Blocked || rock?.IsActive == true) &&
            PauseMenuController.Instance?.IsPaused != true && SceneFlowController.Instance?.IsTransitioning != true;
        private bool Unobstructed => Safe && cabin.Panels?.IsModalOpen != true && !cabin.IsDirectInteractionActive && capture?.IsActive != true && rock?.IsActive != true;
        private bool Helm => cabin.Panels?.CurrentPanel == cabin.NavigationPanel;
        private bool Has(string id) => loop?.ZoneThreePresentation?.Has(id) == true;
        private bool Broken => story?.HasWorldFlag("ROCK_BARRIER_DESTROYED") == true;
        private void Awake()
        {
            cabin ??= GetComponent<CabinStationView>(); effects ??= GetComponent<EffectDirector>();
            story = GetComponent<ZoneOneStory>(); navigation = GetComponent<ZoneNavigation>();
            catcher = GetComponent<CreatureCatcher>(); capture = GetComponent<CaptureMinigameController>();
            photos = GetComponent<PhotoCaptureService>(); lab = GetComponentInChildren<PhotoLabView>(true);
            rock = GetComponent<RockBreakerSequence>();
        }
        private void OnEnable()
        {
            catcher.CaptureResolved += Captured; photos.PhotoSubmitted += Submitted;
            story.ZoneThreeActionCommitted += ActionCommitted;
            SceneManager.sceneLoaded += SceneLoaded; BindLoop();
        }
        private void Start() => BindLoop();
        private void SceneLoaded(Scene _, LoadSceneMode __) => BindLoop();
        private void BindLoop()
        {
            var value = cabin?.Panels?.Expedition ?? FindAnyObjectByType<ExpeditionLoop>();
            if (value == loop) return;
            if (loop != null) loop.TimelineRestored -= ResetPresentation;
            loop = value; if (loop != null) loop.TimelineRestored += ResetPresentation;
        }
        private void OnDisable()
        {
            catcher.CaptureResolved -= Captured; photos.PhotoSubmitted -= Submitted;
            story.ZoneThreeActionCommitted -= ActionCommitted;
            SceneManager.sceneLoaded -= SceneLoaded;
            if (loop != null) loop.TimelineRestored -= ResetPresentation;
            ResetPresentation(); ReleaseAudio(); loop = null;
        }
        private void LateUpdate() => Tick(Time.deltaTime);
        public void Tick(float seconds)
        {
            float dt = Mathf.Max(0, seconds);
            if (boundZone != loop?.Zone) { ResetPresentation(); ReleaseAudio(); boundZone = loop?.Zone; }
            if (!InZone) return;
            if (!ownsAudio && AudioManager.Instance != null)
            { AudioManager.Instance.AcquireZoneAudio(GetInstanceID(), radarClip, shutterClip); effects.BindAudio(AudioManager.Instance); ownsAudio = true; }
            if (!Safe)
            {
                InterruptPrimary(); StopOwned(); quietTime = 0; wasMoving = false; movingSeconds = 0;
                if (loop.Failed || loop.IsDeathInProgress || navigation.Ship.Hull <= 0) DropPrimary();
                return;
            }
            if (Broken) DropPrimary();

            ObserveRadarOpportunity(dt);
            ObservePrimary(dt);

            bool passage = Has("radar") && navigation.DistanceTravelled - loop.ZoneThreePresentation.radarDistance >= 100;
            ObserveReflectionOpportunity(dt, passage);
            bool apparition = effects.IsPlaying("z3.mix.void");
            if (Broken && rock?.IsActive != true) beyondAge += dt;
            float desired = Broken ? Mathf.Lerp(.2f, 0, Mathf.Clamp01(beyondAge / 8)) : apparition ? .025f : passage ? .10f : 1;
            environment = Mathf.MoveTowards(environment, desired, dt / (desired < environment ? 3.5f : 2));
            NormalLoops();
            Loop("z3.audio.ambient_stone", .62f * environment, Mathf.Lerp(650, 11000, environment));
            Loop("z3.audio.sparse_water", Broken ? .2f : .30f);
            Loop("z3.visual.stone", Broken ? 0 : .62f * environment);

            var poi = story.survey.FindContactContaining(navigation.Position);
            bool nearby = poi != null && Mathf.Abs(navigation.Depth - story.survey.DepthFor(poi)) < 35;
            // Sparse local reactions are one-shots, not repetitive looping creatures.
            bool moving = Mathf.Abs(navigation.Speed) > .1f;
            if (moving) { movingSeconds += dt; engineArmed = false; }
            if (!Unobstructed || !applicationFocused)
            { quietTime = 0; wasMoving = false; movingSeconds = 0; return; }
            // Opening another station brakes the ship too; only a stop observed on the Helm
            // can earn this opportunity. Keep it through an interrupted, undelivered attempt.
            if (Helm && wasMoving && !moving && movingSeconds >= 2 && story.HasObjective("Z3_L1_CAPTURE") && !Has("engine"))
                engineArmed = true;
            InspectPhoto();
            bool busy = effects.ActiveSequenceCount > 0;
            // The first observation after a sequence ceases also starts at zero: its
            // previous frame delta can include the tail of the sequence or a hitch.
            quietTime = busy || primary != null || hadSequence ? 0 : quietTime + dt;
            hadSequence = busy;
            if (!Broken && !busy && !nearby)
            {
                resonanceTimer -= dt;
                if (resonanceTimer <= 0)
                {
                    if (environment > .15f) Play("stone_resonance_0" + (StoneCount++ % 3 + 1), environment);
                    resonanceTimer = 19 + StoneCount % 3 * 7;
                }
            }
            if (!Broken && !busy && primary == null)
            {
                if (engineArmed && !moving && !Has("engine") && quietTime >= 4 && PrimaryAudience(PrimaryCue.Engine))
                    BeginPrimary(PrimaryCue.Engine, delayedEngine);
                else if (Has(ZoneThreePresentationState.RadarOpportunity) && !Has("radar") && quietTime >= 4 && PrimaryAudience(PrimaryCue.Radar))
                    BeginPrimary(PrimaryCue.Radar, falseRadar);
                else if (Has(ZoneThreePresentationState.ReflectionOpportunity) && !Has("reflection") && quietTime >= 8 &&
                    !navigation.IsTurning && !navigation.IsChangingDepth && PrimaryAudience(PrimaryCue.Reflection))
                    BeginPrimary(PrimaryCue.Reflection, reflection);
            }
            if (wasMoving && !moving) movingSeconds = 0;
            wasMoving = moving;
        }
        private void NormalLoops()
        {
            Loop("z1.audio.ambient_submarine_engine", 1); Loop("z1.audio.ambient_cabin_ventilation", 1);
            Loop("z1.audio.ambient_propulsion_thrust", Mathf.Clamp01(Mathf.Abs(navigation.Speed) / Mathf.Max(1, navigation.Ship.Speed)));
            Loop("z1.audio.sfx_steering_servo", Unobstructed && Helm && navigation.IsTurning ? 1 : 0);
            Loop("z1.audio.sfx_depth_pressure_adjustment", Unobstructed && Helm && navigation.IsChangingDepth ? 1 : 0);
            Loop("z1.audio.sfx_capture_mechanism", capture.IsActive ? .7f : 0);
        }
        private void InspectPhoto()
        {
            if (lab == null || !lab.gameObject.activeInHierarchy || string.IsNullOrEmpty(lab.SelectedPhotoId)) return;
            if (observedPhoto == lab.SelectedPhotoId) return;
            observedPhoto = lab.SelectedPhotoId;
            foreach (var photo in photos.Photos)
                if (photo.Id == observedPhoto && photo.MissionZoneId == "Zone03")
                {
                    if (photo.MissionTargetId == "Z3_Creature_01") Play("creature05_pressure", .65f);
                    if (photo.MissionTargetId == "Z3_Creature_02")
                    { Play("creature06_crystal_motion", .6f); PlayId("z3.visual.photo-reflection"); }
                    if (photo.MissionTargetId == "Z3_Creature_03") Track("specimen07", effects.PlaySequence(specimen07));
                    break;
                }
        }
        private void Captured(CreatureCatcher.Result result)
        {
            if (!Safe || result != CreatureCatcher.Result.Caught) return;
            if (catcher.LastCapturedId == "Z3_Creature_01")
            {
                if (!Once("capture05", () => Play("creature05_pressure"))) Play("creature05_pressure");
            }
            else if (catcher.LastCapturedId == "Z3_Creature_03") Play("creature07_bioelectric");
        }
        private void Submitted(PhotoRecord photo)
        { if (Safe && photo.MissionZoneId == "Zone03") PlayId("z1.audio.sfx_data_transmit_success"); }
        private void ActionCommitted(MissionObjectiveType type)
        {
            if (!Safe) return;
            if (type == MissionObjectiveType.Craft || type == MissionObjectiveType.InstallUpgrade)
                Track("assembly", effects.PlaySequence(assembly));
            if (type == MissionObjectiveType.DestroyObstacle) { DropPrimary(); Stop("z3.visual.stone"); quietTime = 0; }
        }
        private bool PrimaryAudience(PrimaryCue cue)
        {
            if (!Unobstructed || !applicationFocused || effects == null || effects.Suppressed || Broken) return false;
            if (cue == PrimaryCue.Radar)
                return cabin.Panels?.CurrentPanel == cabin.RadarPanel && effects.Surface(EffectTarget.Radar)?.Available == true;
            return Helm && effects.Surface(cue == PrimaryCue.Reflection ? EffectTarget.Reflection : EffectTarget.Helm)?.Available == true &&
                (cue != PrimaryCue.Engine || Mathf.Abs(navigation.Speed) <= .1f);
        }
        private static string PrimaryId(PrimaryCue cue) => cue switch
        { PrimaryCue.Engine => "engine", PrimaryCue.Radar => "radar", _ => "reflection" };
        private void ObserveRadarOpportunity(float dt)
        {
            if (Broken || Has("radar") || Has(ZoneThreePresentationState.RadarOpportunity))
            { radarObservationDistance = null; opportunityRetry = 0; return; }
            // The real sweep lasts only two seconds. Remember it independently of the
            // presentation quiet gate, then reuse the earned opportunity without another Scan.
            if (!radarObservationDistance.HasValue && story.HasObjective("Z3_L2_PHOTO") &&
                PrimaryAudience(PrimaryCue.Radar) && cabin.Radar.IsScanning)
                radarObservationDistance = navigation.DistanceTravelled;
            if (!radarObservationDistance.HasValue) return;
            opportunityRetry = Mathf.Max(0, opportunityRetry - dt);
            if (opportunityRetry > 0) return;
            if (loop.CommitZoneThreeCue(ZoneThreePresentationState.RadarOpportunity, radarObservationDistance.Value))
                radarObservationDistance = null;
            else opportunityRetry = 1;
        }
        private void BeginPrimary(PrimaryCue cue, EffectSequence sequence)
        {
            if (primary != null || Has(PrimaryId(cue))) return;
            var handle = effects.PlaySequence(sequence, condition: () => PrimaryAudience(cue));
            if (!handle.IsValid) return;
            primary = new PrimaryDelivery { Cue = cue, Handle = handle };
            Track("cue." + PrimaryId(cue), handle); quietTime = 0; hadSequence = true;
        }
        private void ObserveReflectionOpportunity(float dt, bool passage)
        {
            if (Broken || Has("reflection") || Has(ZoneThreePresentationState.ReflectionOpportunity))
            { reflectionOpportunityObserved = false; reflectionOpportunityRetry = 0; return; }
            // Reaching the deep passage earns the opportunity even during depth adjustment.
            // An interrupted peak may retry on the later 590 m rock approach without a detour.
            if (passage && navigation.Depth >= 600) reflectionOpportunityObserved = true;
            if (!reflectionOpportunityObserved) return;
            reflectionOpportunityRetry = Mathf.Max(0, reflectionOpportunityRetry - dt);
            if (reflectionOpportunityRetry > 0) return;
            if (loop.CommitZoneThreeCue(ZoneThreePresentationState.ReflectionOpportunity)) reflectionOpportunityObserved = false;
            else reflectionOpportunityRetry = 1;
        }
        private void ObservePrimary(float dt)
        {
            if (primary == null) return;
            if (!PrimaryAudience(primary.Cue)) InterruptPrimary();
            if (primary == null) return;
            if (!primary.Delivered)
            {
                if (!effects.TryGetSequenceTime(primary.Handle, out _))
                { primary = null; quietTime = 0; return; } // Cancellation/completion alone never proves delivery.
                string audio = primary.Cue switch
                { PrimaryCue.Engine => "z3.audio.engine_reflection", PrimaryCue.Radar => "z3.audio.false_radar_echo", _ => "z3.audio.soft_movement" };
                var surface = effects.Surface(primary.Cue == PrimaryCue.Radar ? EffectTarget.Radar : EffectTarget.Reflection)?.visual?.procedural;
                bool visible = primary.Cue == PrimaryCue.Engine ||
                    surface != null && surface.Visible && surface.ActivePreset ==
                    (primary.Cue == PrimaryCue.Radar ? ScreenInterference.FalseRadarSweep : ScreenInterference.ProceduralReflection);
                if (!visible || !effects.TryGetAudioTime(audio, out float audioTime)) ResetExposure();
                else if (primary.ObservationFrame != Time.frameCount)
                {
                    float now = Time.unscaledTime;
                    if (primary.FirstAudio < 0 || audioTime < primary.LastAudio)
                    { primary.FirstAudio = audioTime; primary.FirstObservation = now; }
                    // AudioSource.time advances in DSP-sized steps. Compare total progress
                    // over the uninterrupted observation window, rather than losing time by
                    // clamping every buffered audio step to one render frame's delta.
                    primary.Exposure = Mathf.Min(audioTime - primary.FirstAudio, Mathf.Max(0, now - primary.FirstObservation));
                    primary.LastAudio = audioTime; primary.ObservationFrame = Time.frameCount;
                    primary.Delivered = primary.Exposure >= (primary.Cue == PrimaryCue.Engine ? .35f : .25f);
                }
            }
            if (primary?.Delivered != true) return;
            primary.Retry = Mathf.Max(0, primary.Retry - dt);
            if (primary.Retry > 0) return;
            if (loop.CommitZoneThreeCue(PrimaryId(primary.Cue)))
            {
                if (primary.Cue == PrimaryCue.Engine) { EngineEventCount++; engineArmed = false; }
                else if (primary.Cue == PrimaryCue.Radar) RadarEventCount++;
                else ReflectionCount++;
                primary = null; quietTime = 0;
            }
            else primary.Retry = 1; // The checkpoint already happened: retry publication, never content.
        }
        private void ResetExposure()
        {
            primary.Exposure = 0; primary.FirstAudio = primary.LastAudio = -1; primary.ObservationFrame = -1;
        }
        private void InterruptPrimary()
        {
            if (primary == null) return;
            if (primary.Handle.IsValid)
            {
                effects?.Stop(primary.Handle); handles.Remove("cue." + PrimaryId(primary.Cue)); primary.Handle = default;
            }
            quietTime = 0;
            if (!primary.Delivered) primary = null;
        }
        private void DropPrimary()
        {
            InterruptPrimary(); primary = null; engineArmed = false;
            radarObservationDistance = null; opportunityRetry = reflectionOpportunityRetry = 0; reflectionOpportunityObserved = false;
        }
        private bool Once(string id, Func<EffectHandle> play)
        {
            if (Has(id)) return false;
            var handle = play(); if (!handle.IsValid) return false;
            if (!loop.CommitZoneThreeCue(id)) { effects.Stop(handle); return false; }
            Track("cue." + id, handle); quietTime = 0; return true;
        }
        /// <summary>QA/editor preview uses the exact production definitions and audience, without writing receipts.</summary>
        public EffectHandle Preview(ZoneThreeDemo demo)
        {
            if (!Unobstructed) return default;
            return Track("preview", effects.PlaySequence(demo switch
            { ZoneThreeDemo.DelayedEngine => delayedEngine, ZoneThreeDemo.FalseRadar => falseRadar,
                ZoneThreeDemo.Reflection => reflection, _ => demolition }, condition: () => Safe));
        }
        private EffectHandle Play(string id, float gain = 1) => PlayId("z3.audio." + id, gain);
        private EffectHandle PlayId(string id, float gain = 1) => Track(id, effects.Play(id, new EffectContext { Intensity = gain }));
        private EffectHandle Track(string id, EffectHandle handle) { if (handle.IsValid) handles[id] = handle; return handle; }
        private void Loop(string id, float gain, float? cutoff = null)
        {
            if (gain <= .001f) { Stop(id); return; }
            Track(id, effects.SetPersistentState(id, true, new EffectContext { Intensity = gain, LowPassHz = cutoff }));
        }
        private void Stop(string id) { if (handles.TryGetValue(id, out var h)) effects.Stop(h); handles.Remove(id); }
        private void StopOwned() { foreach (var h in handles.Values) effects?.Stop(h); handles.Clear(); }
        private void ResetPresentation()
        { DropPrimary(); StopOwned(); quietTime = movingSeconds = beyondAge = 0; resonanceTimer = 7; environment = 1; wasMoving = hadSequence = false; observedPhoto = null; }
        private void ReleaseAudio() { if (ownsAudio) AudioManager.Instance?.ReleaseZoneAudio(GetInstanceID()); ownsAudio = false; }
        private void OnApplicationPause(bool value)
        { applicationPaused = value; if (value) { InterruptPrimary(); StopOwned(); quietTime = 0; wasMoving = false; movingSeconds = 0; } }
        private void OnApplicationFocus(bool value)
        { applicationFocused = value; if (!value) { InterruptPrimary(); quietTime = 0; wasMoving = false; movingSeconds = 0; } }
    }
}
