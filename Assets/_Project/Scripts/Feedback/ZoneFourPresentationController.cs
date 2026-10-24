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
        private bool attemptedFragment, applicationPaused;
        private bool ownsAudio;
        private float environment = 1, structureIn = 18;
        private readonly Dictionary<string, EffectHandle> handles = new();
        private string boundZone;
        private string observedPhoto;
        public bool InZone => loop != null && loop.IsInitialized && loop.Zone == "Zone04";
        public int FragmentCount { get; private set; }
        public bool FragmentActive => effects != null && effects.IsActive(fragment);
        public float EnvironmentLevel => environment;
        public int HiddenACount { get; private set; }
        public int HiddenBCount { get; private set; }
        public int FinalSignalCount { get; private set; }
        public string LastSurveyMessage { get; private set; }
        private bool Safe => InZone && !applicationPaused && Time.timeScale > 0 && !loop.Blocked &&
            !loop.IsDeathInProgress && !loop.Failed && PauseMenuController.Instance?.IsPaused != true &&
            SceneFlowController.Instance?.IsTransitioning != true;
        private bool FragmentWindow => Safe && log != null && log.gameObject.activeInHierarchy &&
            computer.gameObject.activeInHierarchy && computer.CurrentApp == ComputerAppId.MissionLog;
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
        { catcher.CaptureResolved += Captured; photos.PhotoSubmitted += Submitted; SceneManager.sceneLoaded += SceneLoaded; BindLoop(); }
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
            if (!Safe) { StopOwned(); StopFragment(); return; }
            float dt = Mathf.Max(0, seconds);
            // Withdrawal affects the authored environmental bed only. Ship, warnings and UI keep their buses.
            float desired = DesiredEnvironment();
            environment = Mathf.MoveTowards(environment, desired, dt / 4);
            if (desired < .2f && handles.TryGetValue("structure", out var distant))
            { effects.Stop(distant); handles.Remove("structure"); }
            NormalLoops();
            Loop("z4.audio.abyss_sparse_water", .48f * environment, Mathf.Lerp(900, 7800, environment));
            InspectPhoto();
            if (loop.ActiveMap?.HiddenDestinationAvailable(story) == true && cabin.NavigationPanel.activeInHierarchy &&
                cabin.Panels?.IsModalOpen != true && !cabin.IsDirectInteractionActive && Mathf.Abs(navigation.Speed) > .1f &&
                Vector2.Distance(navigation.Position, loop.ActiveMap.finalHiddenPoint.mapPosition) <= 360)
                if (Once("final.signal", finalSignal, () => Safe && cabin.NavigationPanel.activeInHierarchy)) FinalSignalCount++;
            if (cabin.Panels?.IsModalOpen != true && !cabin.IsDirectInteractionActive && capture?.IsActive != true &&
                cabin.NavigationPanel.activeInHierarchy && effects.ActiveSequenceCount == 0)
            {
                structureIn -= dt;
                if (structureIn <= 0)
                {
                    if (environment > .2f) Track("structure", effects.Play("z4.audio.distant_structure", new EffectContext { Intensity = environment }));
                    structureIn = 47;
                }
            }
            if (!FragmentWindow)
            { attemptedFragment = false; StopFragment(); return; }
            if (!attemptedFragment && !loop.ZoneFourPresentation.Has("fragment") && story.HasObjective("Z4_L1_CAPTURE"))
            {
                attemptedFragment = true;
                var handle = effects.PlaySequence(fragmentSequence, condition: () => FragmentWindow);
                if (handle.IsValid)
                {
                    if (!loop.CommitZoneFourCue("fragment")) effects.Stop(handle);
                    else
                    {
                        fragment = handle; FragmentCount++;
                        var a = System.Array.Find(loop.ActiveMap.locations, p => p.id == "zone04-l3");
                        var b = System.Array.Find(loop.ActiveMap.locations, p => p.id == "zone04-l5");
                        fragmentView.Coordinates(Coordinate("A", a), Coordinate("B", b));
                    }
                }
            }
            if (effects.TryGetSequenceTime(fragment, out float age)) fragmentView.Render(age, fragmentSequence.Duration);
            else fragmentView.Restore();
        }
        private static string Coordinate(string name, MapPoi point)
            => point == null ? "" : "TÍN HIỆU " + name + "   ·   X " + point.mapPosition.x.ToString("0.00", CultureInfo.InvariantCulture) +
                "   /   Y " + point.mapPosition.y.ToString("0.00", CultureInfo.InvariantCulture);
        public EffectHandle PreviewFragment()
        {
            if (!FragmentWindow || !story.HasObjective("Z4_L1_CAPTURE")) return default;
            StopFragment(); attemptedFragment = true;
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
        private bool PhotoWindow(string id) => Safe && lab != null && lab.gameObject.activeInHierarchy &&
            computer.gameObject.activeInHierarchy && computer.CurrentApp == ComputerAppId.PhotoLab && lab.SelectedPhotoId == id;
        private void InspectPhoto()
        {
            if (lab == null || string.IsNullOrEmpty(lab.SelectedPhotoId) || !PhotoWindow(lab.SelectedPhotoId))
            { observedPhoto = null; return; }
            if (observedPhoto == lab.SelectedPhotoId) return;
            observedPhoto = lab.SelectedPhotoId;
            foreach (var photo in photos.Photos)
            {
                if (photo.Id != observedPhoto || photo.MissionZoneId != "Zone04") continue;
                string id = photo.MissionPoiId == "zone04-l3" ? "hidden.a" : photo.MissionPoiId == "zone04-l5" ? "hidden.b" : null;
                if (id == null || loop.ZoneFourPresentation.Has(id)) return;
                string selected = observedPhoto;
                if (Once(id, id == "hidden.a" ? hiddenA : hiddenB, () => PhotoWindow(selected)))
                { if (id == "hidden.a") HiddenACount++; else HiddenBCount++; }
                return;
            }
        }
        private bool Once(string id, EffectSequence sequence, System.Func<bool> condition)
        {
            if (loop.ZoneFourPresentation.Has(id)) return false;
            var handle = effects.PlaySequence(sequence, condition: condition);
            if (!handle.IsValid) return false;
            if (!loop.CommitZoneFourCue(id)) { effects.Stop(handle); return false; }
            Track("cue." + id, handle); return true;
        }
        /// <summary>Production surfaces and definitions, with no mission or first-display receipts.</summary>
        public EffectHandle Preview(ZoneFourDemo demo)
        {
            if (!Safe || cabin.Panels?.IsModalOpen == true || cabin.IsDirectInteractionActive) return default;
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
        private void StopOwned() { foreach (var h in handles.Values) effects?.Stop(h); handles.Clear(); }
        private void ReleaseAudio() { if (ownsAudio) AudioManager.Instance?.ReleaseZoneAudio(GetInstanceID()); ownsAudio = false; }
        private void ResetPresentation() { StopOwned(); StopFragment(); attemptedFragment = false; observedPhoto = null; LastSurveyMessage = null; environment = 1; structureIn = 18; }
        private void OnApplicationPause(bool value) { applicationPaused = value; if (value) ResetPresentation(); }
    }
}
