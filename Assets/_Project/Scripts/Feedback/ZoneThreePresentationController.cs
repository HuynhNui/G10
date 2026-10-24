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
        private string boundZone, observedPhoto;
        private float quietTime, resonanceTimer = 7, movingSeconds, environment = 1, beyondAge;
        private bool wasMoving, ownsAudio, applicationPaused;
        public bool InZone => loop != null && loop.IsInitialized && loop.Zone == "Zone03";
        public float EnvironmentLevel => environment;
        public int EngineEventCount { get; private set; }
        public int RadarEventCount { get; private set; }
        public int ReflectionCount { get; private set; }
        public int StoneCount { get; private set; }
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
            if (!Safe) { StopOwned(); wasMoving = false; movingSeconds = 0; return; }

            bool passage = Has("radar") && navigation.DistanceTravelled - loop.ZoneThreePresentation.radarDistance >= 100;
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
            if (moving) movingSeconds += dt;
            if (!Unobstructed) { wasMoving = false; movingSeconds = 0; return; }
            quietTime += dt;
            InspectPhoto();
            bool busy = effects.ActiveSequenceCount > 0;
            if (!Broken && !busy && !nearby)
            {
                resonanceTimer -= dt;
                if (resonanceTimer <= 0)
                {
                    if (environment > .15f) Play("stone_resonance_0" + (StoneCount++ % 3 + 1), environment);
                    resonanceTimer = 19 + StoneCount % 3 * 7;
                }
            }
            if (!Broken && !busy)
            {
                if (wasMoving && !moving && movingSeconds >= 2 && quietTime >= 4 && story.HasObjective("Z3_L1_CAPTURE") && !Has("engine"))
                { if (Once("engine", delayedEngine)) EngineEventCount++; }
                else if (story.HasObjective("Z3_L2_PHOTO") && !Has("radar") && quietTime >= 4 && cabin.RadarPanel.activeInHierarchy && cabin.Radar.IsScanning)
                { if (Once("radar", falseRadar)) RadarEventCount++; }
                else if (passage && !Has("reflection") && quietTime >= 8 && Helm)
                { if (Once("reflection", reflection)) ReflectionCount++; }
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
            if (type == MissionObjectiveType.DestroyObstacle) { Stop("z3.visual.stone"); quietTime = 0; }
        }
        private bool Once(string id, EffectSequence sequence) => Once(id, () => effects.PlaySequence(sequence, condition: () => Unobstructed));
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
        { StopOwned(); quietTime = movingSeconds = beyondAge = 0; resonanceTimer = 7; environment = 1; wasMoving = false; observedPhoto = null; }
        private void ReleaseAudio() { if (ownsAudio) AudioManager.Instance?.ReleaseZoneAudio(GetInstanceID()); ownsAudio = false; }
        private void OnApplicationPause(bool value) { applicationPaused = value; if (value) { StopOwned(); wasMoving = false; movingSeconds = 0; } }
    }
}
