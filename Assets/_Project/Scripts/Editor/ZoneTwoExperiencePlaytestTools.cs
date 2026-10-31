using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Reflection;
using System.Threading;
using G10.Prototype.Computer;
using G10.Prototype.Feedback;
using G10.Prototype.Navigation;
using G10.Prototype.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace G10.Prototype.Editor
{
    /// <summary>
    /// Opt-in editor evidence only. No production component calls this class. Input enters the
    /// existing keyboard/mouse devices and is interpreted by normal game Update/UIInputModule.
    /// It never advances the game clock, calls gameplay actions, or writes gameplay state.
    /// </summary>
    [InitializeOnLoad]
    public static class ZoneTwoExperiencePlaytestTools
    {
        const string Prefix = "G10.Zone02Experience.";
        static ZoneTwoExperienceRecorder recorder;
        static Key[] keys = Array.Empty<Key>();
        static double keyDeadline, mouseDeadline, trackingDeadline;
        static Vector2 mousePosition;
        static int mouseDownFrame = -1;
        static bool ownsKeyboard, ownsMouse, mouseHeld, pendingKeyDown;
        static string inputLabel = "none";
        static CaptureMinigameController trackedCapture;
        static Key lastTrackingKey = Key.None;

        static ZoneTwoExperiencePlaytestTools()
        {
            ApplyIsolation();
            InputSystem.onBeforeUpdate += BeforeInput;
            EditorApplication.update += EditorTick;
            EditorApplication.playModeStateChanged += PlayState;
            AssemblyReloadEvents.beforeAssemblyReload += BeforeReload;
        }

        /// <summary>EDIT MODE ONLY, before clicking the real Debug UI. Creates directories only.</summary>
        public static string SetupIsolation(string runFolder)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Set isolation in Edit Mode before Debug Start.");
            if (string.IsNullOrWhiteSpace(runFolder)) throw new ArgumentException(nameof(runFolder));
            if (SessionState.GetBool(Prefix + "isolated", false))
                throw new InvalidOperationException("A run is already isolated; use TeardownIsolation first.");
            string folder = Path.GetFullPath(runFolder);
            Directory.CreateDirectory(folder);
            Directory.CreateDirectory(Path.Combine(folder, "isolated-photos"));
            SessionState.SetString(Prefix + "oldSave", ExpeditionSaveStore.PathOverride ?? "");
            SessionState.SetString(Prefix + "oldPhotos", PhotoCaptureService.ArchivePathOverride ?? "");
            SessionState.SetString(Prefix + "folder", folder);
            SessionState.SetBool(Prefix + "isolated", true);
            ApplyIsolation();
            return folder;
        }

        static void ApplyIsolation()
        {
            if (!SessionState.GetBool(Prefix + "isolated", false)) return;
            string folder = SessionState.GetString(Prefix + "folder", "");
            ExpeditionSaveStore.PathOverride = Path.Combine(folder, "isolated-save", "timeline.json");
            PhotoCaptureService.ArchivePathOverride = Path.Combine(folder, "isolated-photos");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void ReapplyIsolationBeforeScenes() => ApplyIsolation();

        /// <summary>EDIT MODE ONLY. Stop defers restoration until Play exits to protect quit-save.</summary>
        public static void TeardownIsolation()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before restoring save paths (quit-save protection).");
            RestoreIsolation();
        }

        static void RestoreIsolation()
        {
            if (!SessionState.GetBool(Prefix + "isolated", false)) return;
            string oldSave = SessionState.GetString(Prefix + "oldSave", "");
            string oldPhotos = SessionState.GetString(Prefix + "oldPhotos", "");
            ExpeditionSaveStore.PathOverride = oldSave.Length == 0 ? null : oldSave;
            PhotoCaptureService.ArchivePathOverride = oldPhotos.Length == 0 ? null : oldPhotos;
            SessionState.SetBool(Prefix + "isolated", false);
        }

        /// <summary>PLAY MODE ONLY. Saves genuine Game View frames/audio; use an empty evidence folder.</summary>
        public static string StartRecording(string evidenceFolder, float maximumMinutes = 45)
        {
            RequirePlay();
            if (!SessionState.GetBool(Prefix + "isolated", false))
                throw new InvalidOperationException("SetupIsolation must precede the legitimate Debug UI start.");
            if (recorder != null) throw new InvalidOperationException("Recording already active.");
            var host = new GameObject("Zone02 Passive Evidence Recorder") { hideFlags = HideFlags.DontSave };
            Object.DontDestroyOnLoad(host);
            recorder = host.AddComponent<ZoneTwoExperienceRecorder>();
            try { recorder.Begin(Path.GetFullPath(evidenceFolder), Mathf.Clamp(maximumMinutes, 1, 45)); }
            catch { Object.Destroy(host); recorder = null; throw; }
            return Status();
        }

        /// <summary>PLAY MODE ONLY. Full key state, e.g. "W,D", for real wall time; never calls Update.</summary>
        public static string HoldKeys(string commaSeparatedKeys, double seconds, string label = "keyboard")
        {
            RequirePlay();
            if (Keyboard.current == null) throw new InvalidOperationException("No keyboard device available.");
            keys = (commaSeparatedKeys ?? "").Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(value => (Key)Enum.Parse(typeof(Key), value, true)).Where(value => value != Key.None).Distinct().ToArray();
            if (keys.Length > 12) throw new ArgumentException("At most 12 simultaneous keys.");
            trackingDeadline = 0; trackedCapture = null;
            keyDeadline = Now + BoundSeconds(seconds); ownsKeyboard = pendingKeyDown = true; inputLabel = label;
            Mark("input.request", label + ":keys=" + string.Join(",", keys) + ";seconds=" + seconds);
            return Status();
        }

        /// <summary>Screen coordinates, bottom-left origin. Down and up always occupy different frames.</summary>
        public static string Click(float x, float y, string label = "click") => MouseHold(x, y, .08, label);

        public static string MouseHold(float x, float y, double seconds, string label = "mouse-hold")
        {
            RequirePlay();
            if (Mouse.current == null) throw new InvalidOperationException("No mouse device available.");
            if (!float.IsFinite(x) || !float.IsFinite(y) || x < 0 || y < 0 || x >= Screen.width || y >= Screen.height)
                throw new ArgumentOutOfRangeException(nameof(x), "Use current Game View Screen coordinates.");
            if (mouseHeld) throw new InvalidOperationException("Release the active mouse gesture before a new one.");
            mousePosition = new Vector2(x, y); mouseDeadline = Now + BoundSeconds(seconds);
            mouseDownFrame = -1; mouseHeld = ownsMouse = true; inputLabel = label;
            Mark("input.request", $"{label}:pointer={x},{y};seconds={seconds}");
            return Status();
        }

        /// <summary>
        /// Optional disclosed input-driving agent. Tracks only the currently displayed hook/fish
        /// position and hook heading. No fish AI state, hitbox, future trajectory or gameplay calls.
        /// Human minigame difficulty cannot be inferred from this automated tracking.
        /// </summary>
        public static string CaptureInputAgent(double seconds = 55)
        {
            RequirePlay();
            trackedCapture = Object.FindAnyObjectByType<CaptureMinigameController>();
            if (trackedCapture == null || !trackedCapture.IsActive)
                throw new InvalidOperationException("The actual capture UI must already be active.");
            keys = Array.Empty<Key>(); keyDeadline = 0; ownsKeyboard = true;
            trackingDeadline = Now + BoundSeconds(seconds); lastTrackingKey = Key.None;
            inputLabel = "automated-visible-hook-tracking";
            Mark("input.agent.start", inputLabel + ";seconds=" + seconds);
            return Status();
        }

        public static void ReleaseAll()
        {
            trackingDeadline = keyDeadline = mouseDeadline = 0; trackedCapture = null;
            keys = Array.Empty<Key>(); mouseHeld = false; inputLabel = "none";
            // Queue release on existing devices; the next normal input update applies it.
            if (ownsKeyboard && Keyboard.current != null) InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
            if (ownsMouse && Mouse.current != null) InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = mousePosition });
            ownsKeyboard = ownsMouse = pendingKeyDown = false;
            Mark("input.release-all", "all injected controls released");
        }

        public static void Mark(string category, string detail)
        {
            if (recorder != null) recorder.Mark(category, detail);
        }

        public static string Status()
        {
            var status = recorder != null ? recorder.Sample("status", inputLabel) : new ZoneTwoEvidenceSample { kind = "status", utc = DateTime.UtcNow.ToString("O") };
            status.detail = $"isolation={SessionState.GetBool(Prefix + "isolated", false)};save={ExpeditionSaveStore.NormalSavePath};keys={string.Join(",", keys)};mouseHeld={mouseHeld};remainingKey={Math.Max(0, keyDeadline - Now):F3};remainingMouse={Math.Max(0, mouseDeadline - Now):F3}";
            return JsonUtility.ToJson(status);
        }

        public static string StopRecording()
        {
            ReleaseAll();
            string result = recorder != null ? recorder.Finish("requested-stop") : "No active recording.";
            if (recorder != null) Object.Destroy(recorder.gameObject);
            recorder = null;
            return result;
        }

        internal static string InputLabel => inputLabel;
        static double Now => Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
        static double BoundSeconds(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0 || value > 300)
                throw new ArgumentOutOfRangeException(nameof(value), "Use a duration in (0, 300] real seconds.");
            return value;
        }
        static void RequirePlay()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("This operation requires Play Mode.");
        }
        static void BeforeInput()
        {
            if (!Application.isPlaying || InputState.currentUpdateType == InputUpdateType.Editor) return;
            double now = Now;
            if (ownsKeyboard && Keyboard.current != null)
            {
                if (pendingKeyDown) { Mark("input.keys.down", string.Join(",", keys)); pendingKeyDown = false; }
                if (trackingDeadline > 0)
                {
                    if (now >= trackingDeadline || trackedCapture == null || !trackedCapture.IsActive)
                    { trackingDeadline = 0; trackedCapture = null; keys = Array.Empty<Key>(); Mark("input.agent.stop", "deadline or actual capture UI ended"); }
                    else
                    {
                        var hook = trackedCapture.HookPosition; var fish = trackedCapture.CreaturePosition;
                        float desired = Mathf.Clamp(Mathf.Atan2(fish.y - hook.y, Mathf.Max(90, fish.x - hook.x)) * Mathf.Rad2Deg, -55, 55);
                        float difference = desired - trackedCapture.HookHeading;
                        Key next = difference > 3 ? Key.W : difference < -3 ? Key.S : Key.None;
                        if (next != lastTrackingKey)
                        { Mark("input.agent.key", next + ";visibleHook=" + hook + ";visibleFish=" + fish); lastTrackingKey = next; }
                        keys = next == Key.None ? Array.Empty<Key>() : new[] { next };
                    }
                }
                else if (now >= keyDeadline) keys = Array.Empty<Key>();
                InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(keys));
                if (keys.Length == 0 && trackingDeadline <= 0) { ownsKeyboard = false; Mark("input.keys.up", "deadline release"); }
            }
            if (ownsMouse && Mouse.current != null)
            {
                if (mouseDownFrame < 0)
                { mouseDownFrame = Time.frameCount; Mark("input.pointer.down", mousePosition.ToString()); }
                else if (now >= mouseDeadline && Time.frameCount != mouseDownFrame) mouseHeld = false;
                var state = new MouseState { position = mousePosition };
                if (mouseHeld) state = state.WithButton(MouseButton.Left);
                InputSystem.QueueStateEvent(Mouse.current, state);
                if (!mouseHeld) { ownsMouse = false; Mark("input.pointer.up", mousePosition.ToString()); }
            }
        }
        static void EditorTick()
        {
            if (EditorApplication.isPlaying && EditorApplication.isPaused && (ownsKeyboard || ownsMouse)) ReleaseAll();
            if (recorder != null && recorder.Expired) StopRecording();
        }
        static void PlayState(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode) ApplyIsolation();
            if (state == PlayModeStateChange.ExitingPlayMode) StopRecording();
            if (state == PlayModeStateChange.EnteredEditMode) RestoreIsolation();
        }
        static void BeforeReload()
        {
            // Isolation survives the reload; release input and close evidence files safely.
            ReleaseAll();
            if (recorder != null) recorder.Finish("assembly-reload");
        }
    }

    [Serializable]
    internal sealed class ZoneTwoEvidenceSample
    {
        public string kind, utc, detail, scene, zone, panel, app, action, experienceStage, pendingCue, presentationZone;
        public string endingChoice, committedEndingReached, endingReceiptSource, endingPresentationId, endingTitle, endingSurvey, selectedPhotoPoi;
        public double realtime, unscaledTime, gameTime, elapsedWall, audioDspTime;
        public int frame, screenWidth, screenHeight, day, capturesToday, photosTaken, radarCharges, photoCharges, captureCharges;
        public float timeScale, x, y, depth, heading, speed, distance, energy, hull;
        public bool editorPaused, focused, blocked, moving, turning, changingDepth, captureActive, scanning, modal;
        public bool radarVisual, computerVisual, reflectionVisual, photoMotion, transitioning;
        public bool zoneThreeTelemetryObserved, stoneVisual, falseRadarVisual, proceduralReflectionVisual, bioelectricVisual, rockDebrisVisual;
        public bool rockActive, rockDestroyed;
        public bool zoneFourTelemetryObserved, presentationSequenceTelemetryObserved, fragmentSequenceActive, fragmentVisual;
        public bool missionLogAudience, photoLabAudience, helmAudience, hiddenAPhotoAudience, hiddenBPhotoAudience;
        public bool computerSurfaceAvailable, photoLabSurfaceAvailable, helmSurfaceAvailable, finalWaterRequested, finalWaterTintVisible;
        public bool mainObjectivesComplete, hiddenRouteUnlocked, hiddenRouteComplete, endingPresentationVisible, endingPresentationPlaying;
        public int captureHits, activeSequences, activeEffects, environmentCount, radarEvents, knocks, scrapes, reflections, followingEvents;
        public int engineEvents, stoneEvents;
        public int fragmentEvents, hiddenAEvents, hiddenBEvents, finalSignalEvents;
        public float captureRemaining, environmentLevel, rockElapsed;
        public float fragmentProgress, finalWaterTintAlpha, fragmentAudioTime, hiddenAAudioTime, hiddenBAudioTime, finalSignalAudioTime, endingPresentationElapsed;
        public Vector2 visibleHook, visibleFish;
        public string[] completedObjectives, cueReceipts, audioVoices, visibleProceduralPresets;
        public string[] activePresentationSequences, revealedLocationIds;
        public string image;
        public int skippedVideoSlots, audioChannels, audioSampleRate;
        public long audioSampleFrames, audioFramesWritten, droppedAudioSamples;
        public double firstAudioCallbackStopwatchSeconds;
    }

    /// <summary>Transient editor-created observer; never serialized into a game scene.</summary>
    internal sealed class ZoneTwoExperienceRecorder : MonoBehaviour
    {
        StreamWriter timeline, frames;
        FileStream wav;
        BinaryWriter pcm;
        ZoneTwoExperienceAudioTap tap;
        string folder;
        double startedWall, endWall, nextSample, nextCapture;
        int imageNumber, skippedSlots, channels;
        long framesWritten;
        bool finished, capturing;
        byte[] pcmBuffer = new byte[16384];
        float[] audioReadBuffer = new float[8192];
        float peak;
        long nonSilentSamples;
        double lastDiscovery;
        CabinStationView cabin;
        ExpeditionLoop loop;
        ZoneNavigation nav;
        CaptureMinigameController capture;
        PhotoCaptureService photos;
        ZoneTwoPresentationController horror;
        ZoneThreePresentationController zoneThreeHorror;
        ZoneFourPresentationController zoneFourHorror;
        ZoneFourFragmentView fragmentView;
        EndingPresentation ending;
        PhotoLabView lab;
        string lastCommittedEnding, endingReceiptSource;
        bool endingReceiptReadAttempted;
        // Read-only inspection of registered sequence clocks. No handles, conditions or gameplay
        // state are changed; absence of this private diagnostic shape is reported as unknown.
        static readonly FieldInfo SequenceList = typeof(EffectDirector).GetField("sequences", BindingFlags.Instance | BindingFlags.NonPublic);
        RockBreakerSequence rock;
        ZoneThreeVisualSurface[] proceduralSurfaces = Array.Empty<ZoneThreeVisualSurface>();
        EffectDirector effects;
        ComputerScreenController computer;
        AudioSource[] voices = Array.Empty<AudioSource>();
        internal bool Expired => !finished && Wall >= endWall;
        static double Wall => Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;

        internal void Begin(string path, float maximumMinutes)
        {
            folder = path; Directory.CreateDirectory(folder);
            if (File.Exists(Path.Combine(folder, "timeline.jsonl"))) throw new IOException("Evidence folder already has a timeline; choose a fresh folder.");
            Directory.CreateDirectory(Path.Combine(folder, "frames"));
            timeline = new StreamWriter(Path.Combine(folder, "timeline.jsonl"));
            frames = new StreamWriter(Path.Combine(folder, "frames.jsonl"));
            timeline.AutoFlush = frames.AutoFlush = true;
            startedWall = Wall; endWall = startedWall + maximumMinutes * 60;
            nextSample = nextCapture = Time.realtimeSinceStartupAsDouble;
            File.WriteAllText(Path.Combine(folder, "recording-notes.txt"),
                "Actual rendered Game View JPEGs, target 5 fps, variable real timestamps in frames.jsonl. Not an encoded constant-rate video.\n" +
                "Actual pass-through AudioListener PCM16 WAV. No silence is synthesized or inserted to cover missing callbacks.\n" +
                "Audio sample clock, first callback Stopwatch clock, DSP snapshots and video realtime are logged. Audio device latency, asynchronous DSP buffering, scheduling jitter and dropped buffers prevent sample-accurate A/V synchronization.\n" +
                "A cue receipt proves a gameplay receipt, not necessarily sensory exposure. isPlaying AudioSources do not prove audibility.\n" +
                "Editor pause prevents runtime Update and rendered frames/audio may stop: explicit editor-pause markers contain UTC and realtime; gaps remain gaps.\n" +
                "Telemetry is passive. Keyboard/mouse events enter existing InputSystem devices, with no InputSystem.Update or simulated clock advancement. Optional capture agent uses visible positions only; it cannot measure human minigame skill.\n" +
                "Run limits: 45 minutes, video 5 fps, telemetry 5 Hz, audio ring 4 Mi float samples (16 MiB), WAV PCM16. Disk capture overhead can affect real play and must be reported.\n");
            Discover();
            TryAttachAudio();
            Mark("recording.start", "passive evidence; normal runtime; targetVideoFps=5");
            EditorApplication.pauseStateChanged += PauseChanged;
            StartCoroutine(CaptureLoop());
        }

        void Discover()
        {
            cabin = Object.FindAnyObjectByType<CabinStationView>(); loop = Object.FindAnyObjectByType<ExpeditionLoop>();
            nav = cabin != null ? cabin.Navigation : Object.FindAnyObjectByType<ZoneNavigation>();
            capture = Object.FindAnyObjectByType<CaptureMinigameController>(); photos = Object.FindAnyObjectByType<PhotoCaptureService>();
            horror = Object.FindAnyObjectByType<ZoneTwoPresentationController>(); effects = Object.FindAnyObjectByType<EffectDirector>();
            zoneThreeHorror = Object.FindAnyObjectByType<ZoneThreePresentationController>();
            zoneFourHorror = Object.FindAnyObjectByType<ZoneFourPresentationController>();
            fragmentView = Object.FindAnyObjectByType<ZoneFourFragmentView>(FindObjectsInactive.Include);
            ending = Object.FindAnyObjectByType<EndingPresentation>();
            lab = Object.FindAnyObjectByType<PhotoLabView>(FindObjectsInactive.Include);
            rock = Object.FindAnyObjectByType<RockBreakerSequence>();
            proceduralSurfaces = Object.FindObjectsByType<ZoneThreeVisualSurface>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            computer = Object.FindAnyObjectByType<ComputerScreenController>(FindObjectsInactive.Include);
            voices = Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            lastDiscovery = Time.realtimeSinceStartupAsDouble;
        }
        void TryAttachAudio()
        {
            if (tap != null) return;
            var listeners = Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var listener = listeners.FirstOrDefault(value => value.enabled);
            if (listener == null) return;
            tap = listener.gameObject.AddComponent<ZoneTwoExperienceAudioTap>();
            tap.Begin(AudioSettings.outputSampleRate);
            Mark("audio.tap.attached", listener.name + ";sampleRate=" + tap.SampleRate + ";listeners=" + listeners.Count(value => value.enabled));
        }
        void Update()
        {
            if (finished) return;
            double now = Time.realtimeSinceStartupAsDouble;
            if (now - lastDiscovery >= 1 || cabin == null) { Discover(); TryAttachAudio(); }
            DrainAudio();
            if (now >= nextSample)
            {
                nextSample = now + .2;
                timeline.WriteLine(JsonUtility.ToJson(Sample("sample", ZoneTwoExperiencePlaytestTools.InputLabel)));
            }
        }
        IEnumerator CaptureLoop()
        {
            var end = new WaitForEndOfFrame();
            while (!finished)
            {
                yield return end;
                double now = Time.realtimeSinceStartupAsDouble;
                if (now < nextCapture || capturing) continue;
                int skipped = Math.Max(0, (int)((now - nextCapture) / .2));
                skippedSlots += skipped; nextCapture = now + .2;
                Texture2D texture = null; capturing = true;
                try
                {
                    var sample = Sample("video-frame", ZoneTwoExperiencePlaytestTools.InputLabel);
                    texture = ScreenCapture.CaptureScreenshotAsTexture();
                    sample.image = "frames/frame-" + (imageNumber++).ToString("D6") + ".jpg";
                    File.WriteAllBytes(Path.Combine(folder, sample.image), texture.EncodeToJPG(62));
                    sample.skippedVideoSlots = skipped;
                    frames.WriteLine(JsonUtility.ToJson(sample));
                }
                catch (Exception error) { Mark("video.capture.error", error.GetType().Name + ":" + error.Message); }
                finally { if (texture != null) Object.Destroy(texture); capturing = false; }
            }
        }

        internal ZoneTwoEvidenceSample Sample(string kind, string action)
        {
            var result = new ZoneTwoEvidenceSample
            {
                kind = kind, utc = DateTime.UtcNow.ToString("O"), realtime = Time.realtimeSinceStartupAsDouble,
                unscaledTime = Time.unscaledTimeAsDouble, gameTime = Time.timeAsDouble, elapsedWall = Wall - startedWall,
                audioDspTime = AudioSettings.dspTime, frame = Time.frameCount, timeScale = Time.timeScale,
                editorPaused = EditorApplication.isPaused, focused = Application.isFocused,
                screenWidth = Screen.width, screenHeight = Screen.height,
                scene = SceneManager.GetActiveScene().name, zone = loop != null ? loop.Zone : "none",
                day = loop != null ? loop.Day : 0, capturesToday = loop != null ? loop.CapturesToday : 0,
                blocked = loop != null && loop.IsInitialized && loop.Blocked, action = action,
                panel = cabin != null && cabin.Panels != null && cabin.Panels.CurrentPanel != null ? cabin.Panels.CurrentPanel.name : "none",
                modal = cabin != null && cabin.Panels != null && cabin.Panels.IsModalOpen,
                app = computer != null && computer.gameObject.activeInHierarchy ? computer.CurrentApp.ToString() : "none",
                completedObjectives = loop?.MissionRuntime?.ProgressState?.completedObjectives?.ToArray() ?? Array.Empty<string>(),
                cueReceipts = loop?.Zone == "Zone04" ? loop.ZoneFourPresentation?.played?.ToArray() ?? Array.Empty<string>() :
                    loop?.Zone == "Zone03" ? loop.ZoneThreePresentation?.played?.ToArray() ?? Array.Empty<string>() :
                    loop?.ZoneTwoPresentation?.played?.ToArray() ?? Array.Empty<string>(),
                photosTaken = photos != null ? photos.TotalPhotosTaken : 0,
                captureActive = capture != null && capture.IsActive,
                scanning = cabin != null && cabin.Radar != null && cabin.Radar.IsScanning,
                activeSequences = effects != null ? effects.ActiveSequenceCount : 0,
                activeEffects = effects != null ? effects.ActiveTransientCount : 0,
                environmentCount = horror != null ? horror.EnvironmentCount : 0,
                radarEvents = loop?.Zone == "Zone03" ? zoneThreeHorror != null ? zoneThreeHorror.RadarEventCount : 0 : horror != null ? horror.RadarEventCount : 0,
                knocks = horror != null ? horror.KnockCount : 0,
                scrapes = horror != null ? horror.ScrapeEventCount : 0,
                reflections = loop?.Zone == "Zone03" ? zoneThreeHorror != null ? zoneThreeHorror.ReflectionCount : 0 : horror != null ? horror.ReflectionCount : 0,
                followingEvents = horror != null ? horror.FollowingCount : 0,
                experienceStage = loop?.Zone == "Zone04" ? zoneFourHorror != null ? zoneFourHorror.ExperienceStage : "unobserved" : loop?.Zone == "Zone03" ? zoneThreeHorror != null ? zoneThreeHorror.ExperienceStage : "unobserved" : horror != null ? horror.ExperienceStage : "none",
                pendingCue = loop?.Zone == "Zone04" ? zoneFourHorror != null ? zoneFourHorror.PendingCue : "unobserved" : loop?.Zone == "Zone03" ? zoneThreeHorror != null ? zoneThreeHorror.PendingCue : "unobserved" : horror != null ? horror.PendingCue : null,
                presentationZone = loop != null ? loop.Zone : "none",
                zoneThreeTelemetryObserved = loop != null && loop.IsInitialized && loop.Zone == "Zone03" && zoneThreeHorror != null,
                engineEvents = zoneThreeHorror != null ? zoneThreeHorror.EngineEventCount : 0,
                stoneEvents = zoneThreeHorror != null ? zoneThreeHorror.StoneCount : 0,
                environmentLevel = loop?.Zone == "Zone04" ? zoneFourHorror != null ? zoneFourHorror.EnvironmentLevel : 0 : zoneThreeHorror != null ? zoneThreeHorror.EnvironmentLevel : 0,
                rockActive = rock != null && rock.IsActive, rockElapsed = rock != null ? rock.Elapsed : 0,
                rockDestroyed = loop?.MissionRuntime?.ProgressState?.worldFlags?.Contains("ROCK_BARRIER_DESTROYED") == true,
                radarVisual = effects != null && effects.Surface(EffectTarget.Radar)?.visual?.SignalVisible == true,
                computerVisual = effects != null && effects.Surface(EffectTarget.Computer)?.visual?.SignalVisible == true,
                reflectionVisual = effects != null && effects.Surface(EffectTarget.Reflection)?.visual?.AnomalyVisible == true,
                photoMotion = horror != null && horror.PhotoMotionVisible,
                transitioning = G10.Prototype.Core.SceneFlowController.Instance != null && G10.Prototype.Core.SceneFlowController.Instance.IsTransitioning,
                audioVoices = voices.Where(value => value != null && value.isPlaying).Select(value =>
                    value.name + ":" + (value.clip != null ? value.clip.name : "no-clip") + ":volume=" + value.volume.ToString("F3") + ":pan=" + value.panStereo.ToString("F2")).ToArray(),
                audioChannels = tap != null ? tap.Channels : 0, audioSampleRate = tap != null ? tap.SampleRate : 0,
                audioSampleFrames = tap != null ? tap.SampleFrames : 0, audioFramesWritten = framesWritten,
                droppedAudioSamples = tap != null ? tap.DroppedSamples : 0,
                firstAudioCallbackStopwatchSeconds = tap != null ? tap.FirstCallbackWall : 0
            };
            ObserveZoneFour(result);
            // Observe the actual scoped drawing modules, not requested effect IDs or receipts.
            // Legacy Zone02 flags above retain their original semantics.
            var visiblePresets = new List<string>();
            foreach (var surface in proceduralSurfaces)
            {
                if (surface == null || !surface.Visible) continue;
                var preset = surface.ActivePreset;
                var binding = surface.GetComponent<EffectSurface>();
                visiblePresets.Add((binding != null ? binding.target.ToString() : surface.name) + ":" + preset);
                switch (preset)
                {
                    case ScreenInterference.StoneDepth: result.stoneVisual = true; break;
                    case ScreenInterference.FalseRadarSweep: result.falseRadarVisual = true; break;
                    case ScreenInterference.ProceduralReflection: result.proceduralReflectionVisual = true; break;
                    case ScreenInterference.BioelectricPulse: result.bioelectricVisual = true; break;
                    case ScreenInterference.RockDebris: result.rockDebrisVisual = true; break;
                }
            }
            result.visibleProceduralPresets = visiblePresets.ToArray();
            if (nav != null)
            {
                result.x = nav.Position.x; result.y = nav.Position.y; result.depth = nav.Depth; result.heading = nav.Heading;
                result.speed = nav.Speed; result.distance = nav.DistanceTravelled;
                result.moving = nav.IsMoving; result.turning = nav.IsTurning; result.changingDepth = nav.IsChangingDepth;
                if (loop != null && loop.IsInitialized)
                {
                    result.energy = nav.Ship.Energy; result.hull = nav.Ship.Hull;
                    result.radarCharges = nav.Ship.Radar; result.photoCharges = nav.Ship.Photos; result.captureCharges = nav.Ship.Captures;
                }
            }
            if (result.captureActive)
            { result.captureHits = capture.CurrentHits; result.captureRemaining = capture.RemainingTime; result.visibleHook = capture.HookPosition; result.visibleFish = capture.CreaturePosition; }
            return result;
        }

        void ObserveZoneFour(ZoneTwoEvidenceSample result)
        {
            bool inZone = loop != null && loop.IsInitialized && loop.Zone == "Zone04";
            result.zoneFourTelemetryObserved = inZone && zoneFourHorror != null;
            result.fragmentEvents = zoneFourHorror != null ? zoneFourHorror.FragmentCount : 0;
            result.hiddenAEvents = zoneFourHorror != null ? zoneFourHorror.HiddenACount : 0;
            result.hiddenBEvents = zoneFourHorror != null ? zoneFourHorror.HiddenBCount : 0;
            result.finalSignalEvents = zoneFourHorror != null ? zoneFourHorror.FinalSignalCount : 0;
            result.fragmentSequenceActive = zoneFourHorror != null && zoneFourHorror.FragmentActive;
            result.fragmentVisual = fragmentView != null && fragmentView.Visible;
            result.fragmentProgress = fragmentView != null ? fragmentView.Progress : 0;
            result.helmAudience = cabin != null && cabin.NavigationPanel != null && cabin.NavigationPanel.activeInHierarchy;
            result.photoLabAudience = lab != null && lab.gameObject.activeInHierarchy && computer != null &&
                computer.gameObject.activeInHierarchy && computer.CurrentApp == ComputerAppId.PhotoLab;
            result.missionLogAudience = computer != null && computer.gameObject.activeInHierarchy && computer.CurrentApp == ComputerAppId.MissionLog;
            if (result.photoLabAudience && photos != null)
                foreach (var photo in photos.Photos)
                    if (photo.Id == lab.SelectedPhotoId)
                    { result.selectedPhotoPoi = photo.MissionPoiId; break; }
            result.hiddenAPhotoAudience = result.photoLabAudience && result.selectedPhotoPoi == "zone04-l3";
            result.hiddenBPhotoAudience = result.photoLabAudience && result.selectedPhotoPoi == "zone04-l5";
            result.computerSurfaceAvailable = effects != null && effects.Surface(EffectTarget.Computer)?.Available == true;
            result.photoLabSurfaceAvailable = effects != null && effects.Surface(EffectTarget.PhotoLab)?.Available == true;
            var helm = effects != null ? effects.Surface(EffectTarget.Helm) : null;
            result.helmSurfaceAvailable = helm != null && helm.Available;
            result.finalWaterRequested = effects != null && effects.IsPlaying("z4.visual.final-water");
            var tint = helm != null && helm.overlay != null ? helm.overlay.tint : null;
            result.finalWaterTintAlpha = tint != null ? tint.color.a : 0;
            result.finalWaterTintVisible = result.finalWaterRequested && result.helmSurfaceAvailable && tint != null &&
                tint.enabled && tint.gameObject.activeInHierarchy && tint.color.a > .001f;
            result.fragmentAudioTime = AudioTime("z4.audio.fragment_recovery");
            result.hiddenAAudioTime = AudioTime("z4.audio.hidden_signal_a");
            result.hiddenBAudioTime = AudioTime("z4.audio.hidden_signal_b");
            result.finalSignalAudioTime = AudioTime("z4.audio.final_signal");
            result.revealedLocationIds = inZone ? loop.MissionRuntime?.ProgressState?.revealedLocations?.ToArray() ?? Array.Empty<string>() : Array.Empty<string>();
            result.mainObjectivesComplete = inZone && loop.CurrentProgress?.mainObjectivesComplete == true;
            result.hiddenRouteUnlocked = inZone && loop.CurrentProgress?.hiddenRouteUnlocked == true;
            result.hiddenRouteComplete = inZone && loop.CurrentProgress?.hiddenRouteComplete == true;
            result.endingChoice = inZone ? loop.CurrentProgress?.endingChoice : null;
            if (loop != null && !string.IsNullOrEmpty(loop.EndingReached))
            { lastCommittedEnding = loop.EndingReached; endingReceiptSource = "live-expedition-committed-state"; }
            // Ending destroys the cabin. If that transition fell between 5-Hz samples, read the
            // selected isolated save once; TryRead only reads/normalizes a copy and never writes.
            if (ending != null && !endingReceiptReadAttempted && string.IsNullOrEmpty(lastCommittedEnding))
            {
                endingReceiptReadAttempted = true;
                if (ExpeditionSaveStore.TryRead(out var saved, out var error) && saved?.current != null &&
                    !string.IsNullOrEmpty(saved.current.endingReached))
                { lastCommittedEnding = saved.current.endingReached; endingReceiptSource = string.IsNullOrEmpty(error) ? "selected-isolated-save-primary-read" : "selected-isolated-save-backup-read"; }
            }
            result.committedEndingReached = lastCommittedEnding;
            result.endingReceiptSource = endingReceiptSource;
            result.endingPresentationId = ending != null ? ending.EndingId : null;
            result.endingPresentationVisible = ending != null && ending.isActiveAndEnabled && !string.IsNullOrEmpty(ending.EndingId);
            result.endingPresentationPlaying = ending != null && ending.IsPlaying;
            result.endingPresentationElapsed = ending != null ? ending.Elapsed : 0;
            result.endingTitle = ending != null ? ending.TitleText : null;
            result.endingSurvey = ending != null ? ending.SurveyText : null;
            var observed = new List<string>();
            result.presentationSequenceTelemetryObserved = effects != null && SequenceList != null;
            if (result.presentationSequenceTelemetryObserved && SequenceList.GetValue(effects) is IEnumerable sequences)
                foreach (var sequence in sequences)
                {
                    var type = sequence.GetType();
                    var definition = type.GetField("Definition")?.GetValue(sequence) as EffectSequence;
                    var age = type.GetField("Age")?.GetValue(sequence);
                    if (definition != null && age is float seconds)
                        observed.Add(definition.id + ":seconds=" + seconds.ToString("R", CultureInfo.InvariantCulture));
                    else result.presentationSequenceTelemetryObserved = false;
                }
            result.activePresentationSequences = observed.ToArray();
        }

        float AudioTime(string id) => effects != null && effects.TryGetAudioTime(id, out float seconds) ? seconds : -1;

        internal void Mark(string category, string detail)
        {
            if (finished || timeline == null) return;
            var sample = Sample(category, ZoneTwoExperiencePlaytestTools.InputLabel); sample.detail = detail;
            timeline.WriteLine(JsonUtility.ToJson(sample));
        }
        void PauseChanged(PauseState state) => Mark("editor.pause", state.ToString());
        void OnApplicationFocus(bool value) => Mark("application.focus", value.ToString());
        void OnApplicationPause(bool value) => Mark("application.pause", value.ToString());

        void DrainAudio()
        {
            if (tap == null || tap.Channels <= 0) return;
            if (pcm == null)
            {
                channels = tap.Channels;
                wav = new FileStream(Path.Combine(folder, "actual-game-audio.wav"), FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                pcm = new BinaryWriter(wav); WriteWavHeader(0);
            }
            int count;
            while ((count = tap.Read(audioReadBuffer)) > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    float value = Mathf.Clamp(audioReadBuffer[i], -1, 1); peak = Mathf.Max(peak, Mathf.Abs(value));
                    if (Mathf.Abs(value) > .00003f) nonSilentSamples++;
                    short sample = (short)Mathf.RoundToInt(value * 32767);
                    pcmBuffer[i * 2] = (byte)(sample & 255); pcmBuffer[i * 2 + 1] = (byte)((sample >> 8) & 255);
                }
                pcm.Write(pcmBuffer, 0, count * 2); framesWritten += count / channels;
            }
        }
        void WriteWavHeader(uint bytes)
        {
            pcm.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); pcm.Write(36u + bytes);
            pcm.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); pcm.Write(16u); pcm.Write((ushort)1);
            pcm.Write((ushort)channels); pcm.Write((uint)tap.SampleRate); pcm.Write((uint)(tap.SampleRate * channels * 2));
            pcm.Write((ushort)(channels * 2)); pcm.Write((ushort)16);
            pcm.Write(System.Text.Encoding.ASCII.GetBytes("data")); pcm.Write(bytes);
        }
        internal string Finish(string reason)
        {
            if (finished) return "Recording already closed.";
            Mark("recording.stop", reason); finished = true;
            EditorApplication.pauseStateChanged -= PauseChanged;
            if (tap != null) tap.StopTap();
            DrainAudio();
            long audioBytes = framesWritten * Math.Max(1, channels) * 2;
            if (pcm != null)
            { pcm.Flush(); wav.Position = 0; WriteWavHeader((uint)Math.Min(uint.MaxValue - 36, audioBytes)); pcm.Dispose(); pcm = null; wav = null; }
            string summary = $"reason={reason}\nwallSeconds={Wall - startedWall:F3}\nvideoFrames={imageNumber}\nskippedVideoSlots={skippedSlots}\naudioFramesWritten={framesWritten}\naudioSampleRate={(tap != null ? tap.SampleRate : 0)}\naudioChannels={channels}\npeak={peak:R}\nnonSilentSamples={nonSilentSamples}\ndroppedAudioSamples={(tap != null ? tap.DroppedSamples : 0)}\nformatChanges={(tap != null ? tap.FormatChanges : 0)}\naudioStatus={(tap == null ? "NO_LISTENER" : framesWritten == 0 ? "NO_AUDIO_CALLBACKS" : nonSilentSamples == 0 ? "ACTUAL_CAPTURE_IS_SILENT" : "ACTUAL_NON_SILENT_CAPTURE")}\n";
            File.WriteAllText(Path.Combine(folder, "summary.txt"), summary);
            timeline?.Dispose(); frames?.Dispose(); timeline = frames = null;
            if (tap != null) Object.Destroy(tap);
            return summary;
        }
        void OnDestroy() { if (!finished && timeline != null) Finish("recorder-destroyed"); }
    }

    /// <summary>
    /// Pass-through listener tap. Audio thread only copies into a bounded SPSC ring; no Unity
    /// APIs, locks, logging, file I/O, allocations or buffer modification in OnAudioFilterRead.
    /// </summary>
    internal sealed class ZoneTwoExperienceAudioTap : MonoBehaviour
    {
        const int Capacity = 1 << 22;
        readonly float[] ring = new float[Capacity];
        long written, read, sampleFrames, dropped, firstCallbackTicks;
        int channels, formatChanges, active;
        internal int SampleRate { get; private set; }
        internal int Channels => Volatile.Read(ref channels);
        internal long SampleFrames => Interlocked.Read(ref sampleFrames);
        internal long DroppedSamples => Interlocked.Read(ref dropped);
        internal int FormatChanges => Volatile.Read(ref formatChanges);
        internal double FirstCallbackWall => Interlocked.Read(ref firstCallbackTicks) / (double)Stopwatch.Frequency;
        internal void Begin(int sampleRate) { SampleRate = sampleRate; Volatile.Write(ref active, 1); }
        internal void StopTap() => Volatile.Write(ref active, 0);
        void OnAudioFilterRead(float[] data, int numberOfChannels)
        {
            if (Volatile.Read(ref active) == 0 || numberOfChannels <= 0) return;
            Interlocked.CompareExchange(ref firstCallbackTicks, Stopwatch.GetTimestamp(), 0);
            int current = Volatile.Read(ref channels);
            if (current == 0) Volatile.Write(ref channels, numberOfChannels);
            else if (current != numberOfChannels)
            { Interlocked.Increment(ref formatChanges); Interlocked.Add(ref dropped, data.Length); return; }
            Interlocked.Add(ref sampleFrames, data.Length / numberOfChannels);
            long writeAt = Volatile.Read(ref written), readAt = Volatile.Read(ref read);
            if (data.Length > Capacity - (writeAt - readAt))
            { Interlocked.Add(ref dropped, data.Length); return; }
            for (int i = 0; i < data.Length; i++) ring[(int)((writeAt + i) & (Capacity - 1))] = data[i];
            Volatile.Write(ref written, writeAt + data.Length);
        }
        internal int Read(float[] destination)
        {
            long readAt = Volatile.Read(ref read), writeAt = Volatile.Read(ref written);
            int available = (int)Math.Min(destination.Length, writeAt - readAt);
            int alignment = Math.Max(1, Channels); available -= available % alignment;
            for (int i = 0; i < available; i++) destination[i] = ring[(int)((readAt + i) & (Capacity - 1))];
            Volatile.Write(ref read, readAt + available); return available;
        }
    }
}
