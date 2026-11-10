using System.Globalization;
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
    public enum ZoneFourDemo { HiddenA, HiddenB, FinalSignal }
    /// <summary>Observes committed fieldwork; presentation never grants discoveries or mission progress.</summary>
    [DisallowMultipleComponent]
    public sealed class ZoneFourPresentationController : MonoBehaviour
    {
        public CabinStationView cabin;
        public EffectDirector effects;
        public EffectSequence fragmentSequence;
        public EffectSequence hiddenA, hiddenB, finalSignal;
        public ZoneFourFragmentView fragmentView;
        public AudioClip radarClip, shutterClip;
        private ExpeditionLoop loop;
        private ZoneOneStory story;
        private ZoneNavigation navigation;
        private CaptureMinigameController capture;
        private CreatureCatcher catcher;
        private ComputerScreenController computer;
        private MissionLogView log;
        private PhotoCaptureService photos;
        private PhotoLabView lab;
        private EffectHandle fragment;
        private sealed class CueDelivery
        {
            public string Id, PhotoId;
            public EffectHandle Handle;
            public bool Delivered, Published, FinalOnsetObserved;
            public float StartedAt, RetryAt, FirstObservation = -1, FirstAudio = -1, LastAudio = -1, LastAudioAdvance;
            public int ObservationFrame = -1;
            public AudioSource Voice;
        }
        private CueDelivery primary;
        private bool applicationPaused, applicationFocused = true;
        private bool ownsAudio;
        private float environment = 1, structureIn = 18;
        private float quietUntil, retryAt, opportunityRetryAt, listenerSearchAt;
        private bool finalOpportunityObserved;
        private int navigationFrame = -1;
        private Vector2 lastPosition;
        private float lastDistance;
        private AudioListener listener;
        private EffectAudioExecutor audioExecutor;
        private readonly List<AudioSource> voices = new();
        private readonly Dictionary<string, EffectHandle> handles = new();
        private string boundZone;
        public bool InZone => loop != null && loop.IsInitialized && loop.Zone == "Zone04";
        public int FragmentCount { get; private set; }
        public bool FragmentActive => effects != null && effects.IsActive(fragment);
        public float EnvironmentLevel => environment;
        public int HiddenACount { get; private set; }
        public int HiddenBCount { get; private set; }
        public int FinalSignalCount { get; private set; }
        public string LastSurveyMessage { get; private set; }
        public string PendingCue => !InZone ? "none" : primary != null && !primary.Published ? primary.Id +
            (primary.Delivered ? ".delivered-awaiting-publication" : ".playing") :
            finalOpportunityObserved ? "final.signal.opportunity-awaiting-publication" :
            Has(ZoneFourPresentationState.FinalSignalOpportunity) && !Has("final.signal") ? "final.signal.armed" :
            !Has("fragment") && story?.HasObjective("Z4_L1_CAPTURE") == true ? "fragment.armed" : "none";
        public string ExperienceStage => !InZone ? "outside-zone04" : Has("final.signal") ? "final-signal-delivered" :
            Has(ZoneFourPresentationState.FinalSignalOpportunity) ? "final-approach" :
            Has("hidden.b") ? "after-hidden-b" : Has("hidden.a") ? "after-hidden-a" :
            Has("fragment") ? "recovered-coordinates" : story?.HasObjective("Z4_L1_CAPTURE") == true ? "fragment-available" : "survey";
        private bool Has(string id) => loop?.ZoneFourPresentation?.Has(id) == true;
        private bool Safe => InZone && !applicationPaused && applicationFocused && Time.timeScale > 0 && !loop.Blocked &&
            !loop.IsDeathInProgress && !loop.Failed && PauseMenuController.Instance?.IsPaused != true &&
            SceneFlowController.Instance?.IsTransitioning != true;
        private bool Unobstructed => Safe && cabin.Panels?.IsModalOpen != true && !cabin.IsDirectInteractionActive && capture?.IsActive != true;
        private bool FragmentWindow => Unobstructed && log != null && log.gameObject.activeInHierarchy &&
            computer != null && computer.gameObject.activeInHierarchy && computer.CurrentApp == ComputerAppId.MissionLog &&
            log.GetComponent<MissionArchiveView>()?.IsOpen != true;
        private void Awake()
        {
            cabin ??= GetComponent<CabinStationView>(); effects ??= GetComponent<EffectDirector>();
            story = GetComponent<ZoneOneStory>(); catcher = GetComponent<CreatureCatcher>();
            navigation = GetComponent<ZoneNavigation>(); capture = GetComponent<CaptureMinigameController>();
            computer = GetComponentInChildren<ComputerScreenController>(true);
            log = GetComponentInChildren<MissionLogView>(true);
            photos = GetComponent<PhotoCaptureService>(); lab = GetComponentInChildren<PhotoLabView>(true);
        }
        private void OnEnable()
        {
            catcher.CaptureResolved += Captured; photos.PhotoSubmitted += Submitted;
            SceneManager.sceneLoaded += SceneLoaded; Canvas.willRenderCanvases += ObserveRenderedDelivery; BindLoop();
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
            catcher.CaptureResolved -= Captured; SceneManager.sceneLoaded -= SceneLoaded;
            photos.PhotoSubmitted -= Submitted;
            Canvas.willRenderCanvases -= ObserveRenderedDelivery;
            if (loop != null) loop.TimelineRestored -= ResetPresentation;
            ResetPresentation(); ReleaseAudio(); loop = null;
        }
        private void Captured(CreatureCatcher.Result result)
        {
            if (Safe && result == CreatureCatcher.Result.Caught && catcher.LastCapturedId == "Z4_Creature_01")
                effects.Play(CabinEffects.Notice, new EffectContext { Text = "DỮ LIỆU TỌA ĐỘ ĐÃ PHỤC HỒI · MISSION LOG", Duration = 2.5f });
        }
        private void LateUpdate() => Tick(Time.deltaTime);
        public void Tick(float seconds)
        {
            if (boundZone != loop?.Zone) { ResetPresentation(); ReleaseAudio(); boundZone = loop?.Zone; }
            if (!InZone) return;
            if (!ownsAudio && AudioManager.Instance != null)
            { AudioManager.Instance.AcquireZoneAudio(GetInstanceID(), radarClip, shutterClip); effects.BindAudio(AudioManager.Instance); ownsAudio = true; }
            if (!Safe) { InterruptPrimary(); StopOwned(); StopFragment(); navigationFrame = -1; return; }
            float dt = Mathf.Max(0, seconds);
            ObserveFinalOpportunity();
            UpdatePrimary();
            if (primary == null && Time.unscaledTime >= retryAt && effects.ActiveSequenceCount == 0)
            {
                if (FragmentWindow && !Has("fragment") && story.HasObjective("Z4_L1_CAPTURE"))
                    BeginPrimary("fragment", fragmentSequence);
                else if (!InspectPhoto() && Has(ZoneFourPresentationState.FinalSignalOpportunity) && !Has("final.signal") && HelmWindow)
                    BeginPrimary("final.signal", finalSignal);
            }
            // Withdrawal affects the authored environmental bed only. Ship, warnings and UI keep their buses.
            float desired = DesiredEnvironment();
            bool quiet = QuietPrimary || Time.unscaledTime < quietUntil;
            if (quiet) desired = Mathf.Min(desired, .12f);
            environment = Mathf.MoveTowards(environment, desired, dt / 4);
            if (quiet || desired < .2f) Stop("structure");
            NormalLoops();
            Loop("z4.audio.abyss_sparse_water", .48f * environment, Mathf.Lerp(900, 7800, environment));
            if (!quiet && Unobstructed && HelmWindow && effects.ActiveSequenceCount == 0)
            {
                structureIn -= dt;
                if (structureIn <= 0)
                {
                    if (environment > .2f) Track("structure", effects.Play("z4.audio.distant_structure", new EffectContext { Intensity = environment }));
                    structureIn = 47;
                }
            }
            if (effects.TryGetSequenceTime(fragment, out float age)) fragmentView.Render(age, fragmentSequence.Duration);
            else fragmentView.Restore();
        }
        private static string Coordinate(string name, MapPoi point)
            => point == null ? "" : "TÍN HIỆU " + name + "\nX " + point.mapPosition.x.ToString("0.00", CultureInfo.InvariantCulture) +
                "\nY " + point.mapPosition.y.ToString("0.00", CultureInfo.InvariantCulture);
        public EffectHandle PreviewFragment()
        {
            if (!FragmentWindow || !story.HasObjective("Z4_L1_CAPTURE")) return default;
            InterruptPrimary(); StopFragment();
            fragment = effects.PlaySequence(fragmentSequence, condition: () => FragmentWindow);
            if (fragment.IsValid)
                fragmentView.Coordinates(Coordinate("A", story.survey.FindPoi("zone04-l3")), Coordinate("B", story.survey.FindPoi("zone04-l5")));
            return fragment;
        }
        private void StopFragment() { effects?.Stop(fragment); fragment = default; fragmentView?.Restore(); }
        private void Submitted(PhotoRecord photo)
        {
            if (!Safe || photo.MissionZoneId != "Zone04") return;
            Track("transmit", effects.Play("z1.audio.sfx_data_transmit_success"));
            var counts = ExpeditionPhotoSurvey.Read(loop, GetComponent<CabinZoneSession>());
            if (photo.MissionPoiId is "zone04-l1" or "zone04-l2" && counts.MandatoryTotal == 9 && counts.AllMandatoryPhotosComplete)
            {
                LastSurveyMessage = "BIOLOGICAL SURVEY — 09 / 09";
                Track("survey", effects.Play(CabinEffects.Notice, new EffectContext { Text = LastSurveyMessage, Duration = 4.5f }));
            }
        }
        private bool PhotoWindow(string id) => Unobstructed && lab != null && lab.gameObject.activeInHierarchy &&
            !lab.IsDeleteConfirmationOpen && !lab.IsViewerOpen && lab.preview != null && lab.preview.isActiveAndEnabled && lab.preview.texture != null &&
            lab.preview.color.a > .001f && !lab.preview.canvasRenderer.cull && computer != null &&
            computer.gameObject.activeInHierarchy && computer.CurrentApp == ComputerAppId.PhotoLab && lab.SelectedPhotoId == id;
        private string PhotoCue(string photoId)
        {
            foreach (var photo in photos.Photos)
            {
                if (photo.Id == photoId && photo.MissionZoneId == "Zone04" && photo.Image != null && lab.preview.texture == photo.Image)
                    return photo.MissionPoiId == "zone04-l3" ? "hidden.a" : photo.MissionPoiId == "zone04-l5" ? "hidden.b" : null;
            }
            return null;
        }
        private bool InspectPhoto()
        {
            string selected = lab != null ? lab.SelectedPhotoId : null;
            if (string.IsNullOrEmpty(selected) || !PhotoWindow(selected)) return false;
            string id = PhotoCue(selected);
            if (id == null || Has(id)) return false;
            BeginPrimary(id, id == "hidden.a" ? hiddenA : hiddenB, selected); return true;
        }
        private bool HelmWindow => Unobstructed && cabin.Panels?.CurrentPanel == cabin.NavigationPanel &&
            cabin.NavigationPanel.activeInHierarchy && effects.Surface(EffectTarget.Helm)?.Available == true;
        private bool QuietPrimary => primary != null && primary.Id != "fragment" && effects.IsActive(primary.Handle);
        private static string AudioId(string id) => id == "hidden.a" ? "z4.audio.hidden_signal_a" :
            id == "hidden.b" ? "z4.audio.hidden_signal_b" : "z4.audio.final_signal";
        private bool Audience(CueDelivery cue)
        {
            if (!Unobstructed || effects == null || effects.Suppressed) return false;
            if (cue.Id == "fragment") return FragmentWindow;
            if (!OutputAllowed(AudioId(cue.Id))) return false;
            return cue.Id == "final.signal" ? HelmWindow : PhotoWindow(cue.PhotoId) && PhotoCue(cue.PhotoId) == cue.Id &&
                effects.Surface(EffectTarget.PhotoLab)?.Available == true;
        }
        private void BeginPrimary(string id, EffectSequence sequence, string photoId = null)
        {
            if (primary != null || Has(id)) return;
            var pending = new CueDelivery { Id = id, PhotoId = photoId, StartedAt = Time.unscaledTime };
            var handle = effects.PlaySequence(sequence, condition: () => Audience(pending));
            if (!handle.IsValid) { retryAt = Time.unscaledTime + .5f; return; }
            pending.Handle = handle; primary = pending; Track("cue." + id, handle);
            if (id != "fragment") Stop("structure");
            else
            {
                fragment = handle;
                fragmentView.Coordinates(Coordinate("A", story.survey.FindPoi("zone04-l3")), Coordinate("B", story.survey.FindPoi("zone04-l5")));
            }
        }
        private void ObserveFinalOpportunity()
        {
            bool newFrame = navigationFrame != Time.frameCount;
            bool moved = navigationFrame >= 0 && newFrame && navigation.DistanceTravelled > lastDistance + .001f &&
                (navigation.Position - lastPosition).sqrMagnitude > .000001f && Mathf.Abs(navigation.Speed) > .1f;
            if (newFrame)
            { navigationFrame = Time.frameCount; lastDistance = navigation.DistanceTravelled; lastPosition = navigation.Position; }
            if (Has("final.signal") || Has(ZoneFourPresentationState.FinalSignalOpportunity))
            { finalOpportunityObserved = false; return; }
            var map = loop.ActiveMap;
            if (moved && HelmWindow && map?.HiddenDestinationAvailable(story) == true && map.finalHiddenPoint != null &&
                Vector2.Distance(navigation.Position, map.finalHiddenPoint.mapPosition) <= 360) finalOpportunityObserved = true;
            if (!finalOpportunityObserved || Time.unscaledTime < opportunityRetryAt) return;
            if (loop.CommitZoneFourCue(ZoneFourPresentationState.FinalSignalOpportunity)) finalOpportunityObserved = false;
            else opportunityRetryAt = Time.unscaledTime + 1;
        }
        private void UpdatePrimary()
        {
            if (primary == null) return;
            if (!Audience(primary)) InterruptPrimary();
            if (primary == null) return;
            if (primary.Handle.IsValid && !effects.IsActive(primary.Handle))
            { StopPrimaryHandle(); if (!primary.Delivered) { primary = null; retryAt = Time.unscaledTime + .5f; return; } }
            if (primary.Published && !primary.Handle.IsValid) { primary = null; return; }
            if (!primary.Delivered || primary.Published || Time.unscaledTime < primary.RetryAt) return;
            if (Has(primary.Id) || loop.CommitZoneFourCue(primary.Id))
            {
                primary.Published = true;
                if (primary.Id == "fragment") FragmentCount++;
                else if (primary.Id == "hidden.a") HiddenACount++;
                else if (primary.Id == "hidden.b") HiddenBCount++;
                else FinalSignalCount++;
                if (!primary.Handle.IsValid) primary = null;
            }
            else primary.RetryAt = Time.unscaledTime + 1;
            if (primary?.Published == true && !primary.Handle.IsValid) primary = null;
        }
        // Canvas rendering, wall time and the executor voice clock are independent of fixture Tick(dt).
        // A duplicate callback in one frame, a skipped render, or a sequence seek cannot create exposure.
        private void ObserveRenderedDelivery()
        {
            var cue = primary;
            if (cue == null || cue.Delivered || cue.ObservationFrame == Time.frameCount) return;
            if (!Audience(cue) || !effects.TryGetSequenceTime(cue.Handle, out float age)) { ResetExposure(cue); return; }
            float now = Time.unscaledTime;
            if (age > now - cue.StartedAt + .12f) { ResetExposure(cue); return; }
            if (cue.Id == "fragment")
            {
                if (age < 2.5f || now - cue.StartedAt < 2.5f || fragmentView?.ReadableCompleted != true)
                { ResetExposure(cue); return; }
                ObserveExposure(cue, now, -1, null, .25f); return;
            }
            if (cue.Id == "final.signal")
            {
                var tint = effects.Surface(EffectTarget.Helm)?.overlay?.tint;
                if (age < 1.2f || now - cue.StartedAt < 1.2f || !effects.IsPlaying("z4.visual.final-water") ||
                    tint == null || !tint.isActiveAndEnabled || tint.canvasRenderer.cull || tint.color.a <= .001f)
                { ResetExposure(cue); return; }
            }
            if (!TryAudibleVoice(AudioId(cue.Id), out var voice, out float audioTime)) { ResetExposure(cue); return; }
            if (cue.Id == "final.signal" && !cue.FinalOnsetObserved)
            {
                // Observe the authored onset after its real wait; seeking into the signature is not its arrival.
                if (audioTime > .2f) { ResetExposure(cue); return; }
                cue.FinalOnsetObserved = true;
            }
            ObserveExposure(cue, now, audioTime, voice, .65f);
        }
        private static void ResetExposure(CueDelivery cue)
        { cue.FirstObservation = cue.FirstAudio = cue.LastAudio = -1; cue.ObservationFrame = -1; cue.Voice = null; }
        private static void ObserveExposure(CueDelivery cue, float now, float audioTime, AudioSource voice, float threshold)
        {
            // A missing rendered frame breaks the continuous observation window.
            if (cue.ObservationFrame != Time.frameCount - 1 || cue.Voice != voice || audioTime >= 0 && audioTime < cue.LastAudio)
                ResetExposure(cue);
            if (audioTime >= 0 && audioTime <= cue.LastAudio && now - cue.LastAudioAdvance > .2f) ResetExposure(cue);
            if (cue.FirstObservation < 0) { cue.FirstObservation = now; cue.FirstAudio = audioTime; cue.LastAudioAdvance = now; }
            if (audioTime > cue.LastAudio) cue.LastAudioAdvance = now;
            float exposure = now - cue.FirstObservation;
            if (audioTime >= 0) exposure = Mathf.Min(exposure, audioTime - cue.FirstAudio);
            cue.LastAudio = audioTime; cue.Voice = voice; cue.ObservationFrame = Time.frameCount;
            cue.Delivered = exposure >= threshold;
        }
        private bool OutputAllowed(string id)
        {
            var audio = AudioManager.Instance; var definition = effects.Definition(id);
            if (audio == null || definition == null || audio.OutputVolume <= .001f || audio.FeedbackSfxGain <= .001f ||
                audio.CategoryVolume(definition.audioCategory) <= .001f || AudioListener.pause) return false;
            if ((listener == null || !listener.isActiveAndEnabled) && Time.unscaledTime >= listenerSearchAt)
            { listener = FindAnyObjectByType<AudioListener>(); listenerSearchAt = Time.unscaledTime + 1; }
            return listener != null && listener.isActiveAndEnabled;
        }
        private bool TryAudibleVoice(string id, out AudioSource voice, out float audioTime)
        {
            voice = null; audioTime = 0;
            if (!OutputAllowed(id) || !effects.IsPlaying(id) || !effects.TryGetAudioTime(id, out audioTime)) return false;
            audioExecutor ??= effects.GetComponentInChildren<EffectAudioExecutor>(true);
            if (audioExecutor == null) return false;
            voices.Clear(); audioExecutor.GetComponentsInChildren(true, voices);
            var clip = effects.Definition(id).clip;
            foreach (var source in voices)
                if (source != null && source.clip == clip && source.isActiveAndEnabled && source.isPlaying && !source.mute &&
                    source.volume > .001f && Mathf.Abs(source.time - audioTime) < .12f)
                { voice = source; return true; }
            return false;
        }
        private void StopPrimaryHandle()
        {
            if (primary == null || !primary.Handle.IsValid) return;
            if (primary.Id != "fragment") quietUntil = Time.unscaledTime + 4;
            effects?.Stop(primary.Handle); handles.Remove("cue." + primary.Id); primary.Handle = default;
            if (primary.Id == "fragment") StopFragment();
        }
        private void InterruptPrimary()
        {
            if (primary == null) return;
            StopPrimaryHandle();
            if (!primary.Delivered || primary.Published) { primary = null; retryAt = Time.unscaledTime + .5f; }
        }
        /// <summary>Production surfaces and definitions, with no mission or first-display receipts.</summary>
        public EffectHandle Preview(ZoneFourDemo demo)
        {
            if (!Safe || cabin.Panels?.IsModalOpen == true || cabin.IsDirectInteractionActive) return default;
            InterruptPrimary();
            if (demo == ZoneFourDemo.FinalSignal)
                return Track("preview", effects.PlaySequence(finalSignal, condition: () => Safe && cabin.NavigationPanel.activeInHierarchy));
            string selected = lab != null ? lab.SelectedPhotoId : null;
            bool matches = false;
            foreach (var photo in photos.Photos)
                if (photo.Id == selected && photo.MissionZoneId == "Zone04" &&
                    photo.MissionPoiId == (demo == ZoneFourDemo.HiddenA ? "zone04-l3" : "zone04-l5")) { matches = true; break; }
            if (!matches) return default;
            return Track("preview", effects.PlaySequence(demo == ZoneFourDemo.HiddenA ? hiddenA : hiddenB,
                condition: () => PhotoWindow(selected)));
        }
        private float DesiredEnvironment()
        {
            var map = loop.ActiveMap;
            if (map?.HiddenDestinationAvailable(story) == true && map.finalHiddenPoint != null)
                return Mathf.Lerp(.015f, .35f, Mathf.InverseLerp(90, 600, Vector2.Distance(navigation.Position, map.finalHiddenPoint.mapPosition)));
            var b = story.survey.FindPoi("zone04-l5");
            return story.IsLocationRevealed("Z4_L4") && b != null &&
                Vector2.Distance(navigation.Position, story.survey.ContactPosition(b)) < 150 && Mathf.Abs(navigation.Depth - story.survey.DepthFor(b)) < 45 ? .025f : 1;
        }
        private void NormalLoops()
        {
            bool piloting = cabin.Panels?.IsModalOpen != true && !cabin.IsDirectInteractionActive && cabin.Panels?.CurrentPanel == cabin.NavigationPanel;
            Loop("z1.audio.ambient_submarine_engine", 1); Loop("z1.audio.ambient_cabin_ventilation", .78f);
            Loop("z1.audio.ambient_propulsion_thrust", Mathf.Clamp01(Mathf.Abs(navigation.Speed) / Mathf.Max(1, navigation.Ship.Speed)));
            Loop("z1.audio.sfx_steering_servo", piloting && navigation.IsTurning ? 1 : 0);
            Loop("z1.audio.sfx_depth_pressure_adjustment", piloting && navigation.IsChangingDepth ? 1 : 0);
            Loop("z1.audio.sfx_capture_mechanism", capture?.IsActive == true ? .7f : 0);
        }
        private EffectHandle Track(string id, EffectHandle handle)
        {
            if (!handle.IsValid) return handle;
            if (handles.TryGetValue(id, out var previous) && previous.Id != handle.Id) effects.Stop(previous);
            handles[id] = handle; return handle;
        }
        private void Loop(string id, float gain, float? cutoff = null)
        {
            if (gain <= .001f) { if (handles.TryGetValue(id, out var h)) effects.Stop(h); handles.Remove(id); return; }
            Track(id, effects.SetPersistentState(id, true, new EffectContext { Intensity = gain, LowPassHz = cutoff }));
        }
        private void Stop(string id) { if (handles.TryGetValue(id, out var handle)) effects?.Stop(handle); handles.Remove(id); }
        private void StopOwned() { foreach (var h in handles.Values) effects?.Stop(h); handles.Clear(); }
        private void ReleaseAudio() { if (ownsAudio) AudioManager.Instance?.ReleaseZoneAudio(GetInstanceID()); ownsAudio = false; }
        private void ResetPresentation()
        {
            StopPrimaryHandle(); primary = null; StopOwned(); StopFragment();
            finalOpportunityObserved = false; navigationFrame = -1;
            retryAt = opportunityRetryAt = quietUntil = 0; LastSurveyMessage = null; environment = 1; structureIn = 18;
        }
        private void OnApplicationPause(bool value)
        { applicationPaused = value; if (value) { InterruptPrimary(); StopOwned(); StopFragment(); navigationFrame = -1; } }
        private void OnApplicationFocus(bool value)
        { applicationFocused = value; if (!value) { InterruptPrimary(); StopOwned(); StopFragment(); navigationFrame = -1; } }
    }
}
