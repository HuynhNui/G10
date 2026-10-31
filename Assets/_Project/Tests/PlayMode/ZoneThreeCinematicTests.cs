using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using G10.Prototype.Computer;
using G10.Prototype.Core;
using G10.Prototype.Feedback;
using G10.Prototype.Missions;
using G10.Prototype.Navigation;
using G10.Prototype.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace G10.Prototype.Tests
{
    /// <summary>Actual shared-cabin production demos. Evidence is written only when these tests run.</summary>
    public sealed class ZoneThreeCinematicTests
    {
        private string folder, oldSave, oldPhotos;
        private SceneFlowController flow;
        private CabinStationView cabin;
        private ZoneOneStory story;
        private ExpeditionLoop loop;
        private PhotoCaptureService photos;
        private ComputerScreenController computer;
        private ZoneThreePresentationController presentation;
        private EffectDirector effects;
        private RockBreakerSequence rock;
        private ZoneNavigation Nav => cabin.Navigation;
        private CreatureCatcher Catcher => cabin.GetComponent<CreatureCatcher>();
        private static string Qa => Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/QA/Zone03ExperienceLoop/native-validation"));
        [Serializable] private sealed class AuthorityState
        {
            public ShipState ship;
            public Vector2 position;
            public float heading, depth, distance;
            public string mission, receipts;
            public int photos, contacts;
        }
        [Serializable] private sealed class Evidence
        {
            public string demo, status = "passed automated checks; human audio/visual review remains required", utc;
            public int width, height;
            public float changedFraction, meanChannelDifference;
            public string[] playingClipBindings;
        }
        private sealed class Frame { public Color32[] pixels; public int width, height; }
        [UnitySetUp] public IEnumerator Setup()
        {
            oldSave = ExpeditionSaveStore.PathOverride; oldPhotos = PhotoCaptureService.ArchivePathOverride;
            folder = Path.Combine(Application.temporaryCachePath, "ZoneThreeCinematic-" + Guid.NewGuid().ToString("N"));
            ExpeditionSaveStore.SelectNormalTimeline(); ExpeditionSaveStore.PathOverride = Path.Combine(folder, "timeline.json");
            PhotoCaptureService.ArchivePathOverride = Path.Combine(folder, "photos"); TutorialTestSave.SeedReturningPlayer();
            if (SceneFlowController.Instance != null) { Object.Destroy(SceneFlowController.Instance.gameObject); yield return null; }
            yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single); yield return null;
            flow = SceneFlowController.Instance; yield return Wait();
            var menu = Object.FindAnyObjectByType<MainMenuDebugStart>(); menu.Open(); menu.SelectZone(3); menu.StartRun(); yield return Wait();
            cabin = Object.FindAnyObjectByType<CabinStationView>(); loop = Object.FindAnyObjectByType<ExpeditionLoop>();
            story = cabin.GetComponent<ZoneOneStory>(); photos = cabin.GetComponent<PhotoCaptureService>();
            computer = cabin.GetComponentInChildren<ComputerScreenController>(true);
            presentation = cabin.GetComponent<ZoneThreePresentationController>(); effects = cabin.GetComponent<EffectDirector>();
            rock = cabin.GetComponent<RockBreakerSequence>();
            Assert.That(loop.IsInitialized, Is.True, loop.LastError); Assert.That(loop.Zone, Is.EqualTo("Zone03"));
            Assert.That(presentation, Is.Not.Null, "Install the production Zone 3 controller and assets first."); presentation.Tick(0);
            foreach (var sequence in new[] { presentation.delayedEngine, presentation.falseRadar, presentation.reflection, presentation.assembly, presentation.demolition, presentation.specimen07 })
            {
                Assert.That(sequence, Is.Not.Null); Assert.That(sequence.cues, Is.Not.Empty);
                foreach (var cue in sequence.cues) Assert.That(cue.effect, Is.Not.Null);
            }
            Assert.That(rock.productionSequence, Is.SameAs(presentation.demolition));
            foreach (var audience in new[] { EffectTarget.Radar, EffectTarget.Reflection, EffectTarget.PhotoLab, EffectTarget.Global })
            { Assert.That(effects.Surface(audience)?.visual?.procedural?.compositionTemplate, Is.Not.Null, audience.ToString()); }
            cabin.ClosePanel(); Nav.Brake(); yield return null;
        }
        private IEnumerator Wait()
        {
            yield return null; float until = Time.realtimeSinceStartup + 25;
            while (flow.IsTransitioning && Time.realtimeSinceStartup < until) yield return null;
            Assert.That(flow.IsTransitioning, Is.False); Assert.That(flow.LastError, Is.Null.Or.Empty); yield return null;
        }
        private void Place(string poiId)
        {
            var poi = story.survey.FindPoi(poiId); Assert.That(poi, Is.Not.Null); Nav.Ship.Refill();
            Nav.RestoreVoyage(story.survey.ContactPosition(poi) + Vector2.down * 14, 0, story.survey.DepthFor(poi), Nav.DistanceTravelled);
        }
        private IEnumerator Survey(string poiId, bool capture)
        {
            yield return new WaitForSeconds(.65f); Place(poiId); cabin.ClosePanel(); int before = photos.Photos.Count; cabin.OpenCamera();
            float deadline = Time.realtimeSinceStartup + 4;
            while ((photos.Photos.Count == before || cabin.IsDirectInteractionActive) && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(photos.Photos.Count, Is.EqualTo(before + 1)); var photo = photos.Photos.Last();
            Assert.That(photo.MissionZoneId, Is.EqualTo("Zone03"), photo.Result); Assert.That(photo.MissionObjectiveId, Is.Not.Null.And.Not.Empty);
            cabin.OpenComputer(); computer.OpenPhotoLab(); yield return null; yield return null;
            var lab = computer.GetComponentInChildren<PhotoLabView>(true); Assert.That(lab.sendButton.interactable, Is.True);
            lab.sendButton.onClick.Invoke(); yield return null; Assert.That(story.HasObjective(photo.MissionObjectiveId), Is.True);
            cabin.ClosePanel(); Place(poiId);
            if (capture)
            {
                Assert.That(Catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.Started)); CaptureMinigamePlayModeTests.Win(Catcher.minigame);
                Assert.That(Catcher.LastResult, Is.EqualTo(CreatureCatcher.Result.Caught));
            }
            yield return null;
        }
        private string Authority() => JsonUtility.ToJson(new AuthorityState
        {
            ship = Nav.Ship.Export(), position = Nav.Position, heading = Nav.Heading, depth = Nav.Depth, distance = Nav.DistanceTravelled,
            mission = JsonUtility.ToJson(story.ExportProgress()), receipts = JsonUtility.ToJson(loop.ZoneThreePresentation),
            photos = photos.Photos.Count, contacts = cabin.Radar.VisibleContactCount
        });
        private AudioSource[] Voices() => cabin.GetComponent<EffectAudioExecutor>().GetComponentsInChildren<AudioSource>(true)
            .Where(source => source.isPlaying && source.clip != null).ToArray();
        private bool Playing(string clip) => Voices().Any(source => source.clip.name == clip);
        private static IEnumerator Shot(string name, Action<Frame> result = null)
        {
            Canvas.ForceUpdateCanvases(); yield return new WaitForEndOfFrame();
            var texture = ScreenCapture.CaptureScreenshotAsTexture(); Assert.That(texture, Is.Not.Null);
            try
            {
                Assert.That(texture.width, Is.GreaterThan(100)); Assert.That(texture.height, Is.GreaterThan(100));
                Directory.CreateDirectory(Qa); File.WriteAllBytes(Path.Combine(Qa, name + ".png"), texture.EncodeToPNG());
                result?.Invoke(new Frame { pixels = texture.GetPixels32(), width = texture.width, height = texture.height });
            }
            finally { Object.Destroy(texture); }
        }
        private static RectInt ScreenRegion(EffectSurface surface, Frame frame)
        {
            var rect = (surface.visibilityRoot != null ? surface.visibilityRoot.transform : surface.transform) as RectTransform;
            Assert.That(rect, Is.Not.Null); var canvas = rect.GetComponentInParent<Canvas>().rootCanvas;
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]), max = min;
            foreach (var corner in corners)
            { var point = RectTransformUtility.WorldToScreenPoint(camera, corner); min = Vector2.Min(min, point); max = Vector2.Max(max, point); }
            int x = Mathf.Clamp(Mathf.FloorToInt(min.x), 0, frame.width - 1), y = Mathf.Clamp(Mathf.FloorToInt(min.y), 0, frame.height - 1);
            return new RectInt(x, y, Mathf.Clamp(Mathf.CeilToInt(max.x) - x, 1, frame.width - x), Mathf.Clamp(Mathf.CeilToInt(max.y) - y, 1, frame.height - y));
        }
        private static Evidence Difference(string demo, Frame before, Frame after, RectInt region, IEnumerable<string> heard)
        {
            Assert.That(after.width, Is.EqualTo(before.width)); Assert.That(after.height, Is.EqualTo(before.height));
            int changed = 0, placeholder = 0; long difference = 0;
            for (int y = region.yMin; y < region.yMax; y++) for (int x = region.xMin; x < region.xMax; x++)
            {
                var a = before.pixels[y * before.width + x]; var b = after.pixels[y * after.width + x];
                int delta = Math.Abs(a.r - b.r) + Math.Abs(a.g - b.g) + Math.Abs(a.b - b.b); difference += delta;
                if (delta > 18) changed++;
                if (b.r > 190 && b.b > 190 && b.g < 60 || b.r < 40 && b.g > 220 && b.b > 220) placeholder++;
            }
            int count = region.width * region.height; Assert.That(count, Is.GreaterThan(100));
            Assert.That(placeholder / (float)count, Is.LessThan(.35f), "Reject a monitor filled by an uncompiled shader placeholder.");
            return new Evidence { demo = demo, utc = DateTime.UtcNow.ToString("o"), width = after.width, height = after.height,
                changedFraction = changed / (float)count, meanChannelDifference = difference / (count * 3f * 255), playingClipBindings = heard.Distinct().ToArray() };
        }
        private static void SaveEvidence(Evidence evidence)
        { Directory.CreateDirectory(Qa); File.WriteAllText(Path.Combine(Qa, evidence.demo + ".json"), JsonUtility.ToJson(evidence, true)); }
        private IEnumerator Until(Func<bool> predicate, float seconds, string reason)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (!predicate() && Time.realtimeSinceStartup < end) yield return null;
            Assert.That(predicate(), Is.True, reason);
        }
        private void OpenWater(float depth)
        {
            Nav.ConfigureMapCoordinates(new Vector2(2000, 1000), 50);
            var water = new byte[200 * 100]; Array.Fill(water, (byte)1); Nav.SetChart(water, 200, 100);
            Nav.RestoreVoyage(new Vector2(800, 500), 90, depth, Nav.DistanceTravelled);
        }
        private IEnumerator Delivered(string id, string clip, float voiceSeconds, float deadline,
            EffectTarget? audience = null, ScreenInterference preset = ScreenInterference.None)
        {
            // Only runtime yields advance delivery. Tick() is reserved for eligibility clocks.
            yield return Until(() => loop.ZoneThreePresentation.Has(id), deadline, id + " must reach its actual delivery checkpoint.");
            var voice = Voices().SingleOrDefault(source => source.clip.name == clip);
            Assert.That(voice, Is.Not.Null, "A published receipt requires its real voice to be playing at the checkpoint.");
            Assert.That(voice.time, Is.GreaterThanOrEqualTo(voiceSeconds), "Scheduling or a large observation delta cannot substitute for playback.");
            if (audience.HasValue)
                Assert.That(effects.Surface(audience.Value).visual.procedural.ActivePreset, Is.EqualTo(preset));
        }
        private IEnumerator ContinueDebug()
        {
            flow.LoadMainMenu(); yield return Wait(); flow.ContinueDebugGame(); yield return Wait();
            cabin = Object.FindAnyObjectByType<CabinStationView>(); loop = Object.FindAnyObjectByType<ExpeditionLoop>();
            story = cabin.GetComponent<ZoneOneStory>(); photos = cabin.GetComponent<PhotoCaptureService>();
            computer = cabin.GetComponentInChildren<ComputerScreenController>(true);
            presentation = cabin.GetComponent<ZoneThreePresentationController>(); effects = cabin.GetComponent<EffectDirector>();
            rock = cabin.GetComponent<RockBreakerSequence>(); presentation.Tick(0);
            Assert.That(loop.IsInitialized, Is.True, loop.LastError); Assert.That(loop.Zone, Is.EqualTo("Zone03"));
        }

        [UnityTest] public IEnumerator DemoARealCaptureAndPropulsionStopTriggerDistinctReflectedAudioOnce()
        {
            yield return Survey("zone03-l1", true); Assert.That(story.HasObjective("Z3_L1_CAPTURE"), Is.True);
            effects.ClearTransients(); cabin.OpenNavigation();
            var audioEvidence = ZoneThreeAudioEvidence.Begin(Object.FindAnyObjectByType<AudioListener>(), 15);
            // Navigation driver uses an open chart to isolate the audio stop edge from terrain collisions.
            OpenWater(640); cabin.Hold(1); Nav.Navigate(1, 0, 0, 2.2f);
            Assert.That(Nav.Speed, Is.GreaterThan(.1f)); presentation.Tick(4.2f);
            yield return Shot("A-01-real-engine-moving"); Assert.That(Nav.Speed, Is.GreaterThan(.1f));
            // Keep real held propulsion audible in the listener recording before the stop edge.
            yield return new WaitForSeconds(2.2f); Assert.That(Playing("ambient_propulsion_thrust"), Is.True);
            cabin.Release(1); cabin.Brake(); presentation.Tick(.01f);
            Assert.That(presentation.EngineEventCount, Is.Zero); Assert.That(loop.ZoneThreePresentation.Has("engine"), Is.False);
            Assert.That(Playing("engine_reflection"), Is.True, "Launch starts a real voice without consuming the cue.");
            Assert.That(Playing("ambient_propulsion_thrust"), Is.False, "True propulsion ends at the stop edge.");
            Assert.That(Playing("ambient_submarine_engine"), Is.True, "Normal cabin machinery stays present.");
            var echo = Voices().Single(source => source.clip.name == "engine_reflection");
            Assert.That(echo.clip, Is.Not.SameAs(effects.Definition("z1.audio.ambient_submarine_engine").clip));
            Assert.That(echo.clip.samples, Is.GreaterThan(1000));
            yield return Delivered("engine", "engine_reflection", .35f, presentation.delayedEngine.Duration + 3);
            Assert.That(presentation.EngineEventCount, Is.EqualTo(1)); string authority = Authority();
            yield return new WaitForSeconds(.35f); Assert.That(Playing("engine_reflection"), Is.True); Assert.That(Authority(), Is.EqualTo(authority));
            yield return Shot("A-02-stopped-engine-reflection");
            SaveEvidence(new Evidence { demo = "A-delayed-engine", utc = DateTime.UtcNow.ToString("o"), width = Screen.width, height = Screen.height,
                playingClipBindings = Voices().Select(source => source.clip.name).ToArray() });
            yield return Until(() => !Playing("engine_reflection"), presentation.delayedEngine.Duration + 1, "Echo should finish.");
            Nav.Navigate(1, 0, 0, .2f); presentation.Tick(4.2f); Nav.Brake(); presentation.Tick(.1f);
            Assert.That(presentation.EngineEventCount, Is.EqualTo(1)); Assert.That(loop.ZoneThreePresentation.Has("engine"), Is.True);
            var mix = audioEvidence.Finish(Path.Combine(Qa, "A-gameplay-listener.wav"));
            File.WriteAllText(Path.Combine(Qa, "A-gameplay-listener.json"), JsonUtility.ToJson(mix, true));
            Assert.That(mix.nonzero, Is.True, "Record the real listener mix, without substituting source WAVs.");
            Assert.That(mix.durationSeconds, Is.GreaterThan(1)); Assert.That(mix.clippedSamples, Is.Zero);
        }
        [UnityTest] public IEnumerator RealSurveyScanAndTravelCommitRadarAndReflectionOnceAcrossContinue()
        {
            yield return Survey("zone03-l2", false);
            Assert.That(story.HasObjective("Z3_L2_PHOTO"), Is.True);
            Assert.That(loop.ZoneThreePresentation.Has("radar"), Is.False);
            Assert.That(loop.ZoneThreePresentation.Has("reflection"), Is.False);
            cabin.OpenRadar(); presentation.Tick(4.1f);
            int radarCharges = Nav.Ship.Radar;
            cabin.Scan(); Assert.That(cabin.Radar.IsScanning, Is.True);
            Assert.That(Nav.Ship.Radar, Is.EqualTo(radarCharges - 1));
            presentation.Tick(0);
            Assert.That(presentation.RadarEventCount, Is.Zero);
            Assert.That(loop.ZoneThreePresentation.Has(ZoneThreePresentationState.RadarOpportunity), Is.True);
            Assert.That(loop.ZoneThreePresentation.Has("radar"), Is.False, "The durable scan opportunity is not delivery.");
            float scanDistance = loop.ZoneThreePresentation.radarDistance;
            Assert.That(scanDistance, Is.EqualTo(Nav.DistanceTravelled));
            cabin.Scan();
            Assert.That(Nav.Ship.Radar, Is.EqualTo(radarCharges - 1), "An already-running scan spends no second charge.");
            yield return Delivered("radar", "false_radar_echo", .25f, presentation.falseRadar.Duration + 7,
                EffectTarget.Radar, ScreenInterference.FalseRadarSweep);
            Assert.That(presentation.RadarEventCount, Is.EqualTo(1));
            Assert.That(Nav.Ship.Radar, Is.EqualTo(radarCharges - 1), "Delivery spends no additional scan charge.");
            yield return Until(() => effects.ActiveSequenceCount == 0, presentation.falseRadar.Duration + 1, "The false sweep must finish before the passage event.");

            cabin.OpenNavigation();
            // Use the actual navigation driver on open water; moving the saved position cannot earn passage distance.
            OpenWater(400);
            for (int step = 0; step < 80 && Nav.DistanceTravelled - scanDistance < 110; step++) Nav.Navigate(1, 0, 0, .5f);
            Nav.Brake();
            Assert.That(Nav.DistanceTravelled - scanDistance, Is.GreaterThanOrEqualTo(100));
            // Clear the post-sequence observation, then advance eligibility only.
            presentation.Tick(0); presentation.Tick(8.1f);
            Assert.That(effects.IsPlaying("z3.mix.void"), Is.False, "Passage at 400 m must not start the deep reflection.");
            Assert.That(presentation.ReflectionCount, Is.Zero); Assert.That(loop.ZoneThreePresentation.Has("reflection"), Is.False);
            Nav.RestoreVoyage(Nav.Position, Nav.Heading, 640, Nav.DistanceTravelled);
            Nav.Navigate(0, 1, 0, .1f); Assert.That(Nav.IsTurning, Is.True); presentation.Tick(0);
            Assert.That(effects.IsPlaying("z3.mix.void"), Is.False, "Active steering prevents the start.");
            Nav.Navigate(0, 0, 1, .1f); Assert.That(Nav.IsChangingDepth, Is.True); presentation.Tick(0);
            Assert.That(effects.IsPlaying("z3.mix.void"), Is.False, "Active depth adjustment prevents the start.");
            Nav.Brake(); presentation.Tick(0);
            Assert.That(effects.IsPlaying("z3.mix.void"), Is.True); Assert.That(presentation.ReflectionCount, Is.Zero);
            Assert.That(loop.ZoneThreePresentation.Has(ZoneThreePresentationState.ReflectionOpportunity), Is.True);
            cabin.OpenRadar(); yield return null; yield return null;
            Assert.That(effects.ActiveSequenceCount, Is.Zero); Assert.That(Playing("soft_movement"), Is.False);
            Assert.That(loop.ZoneThreePresentation.Has("reflection"), Is.False, "Leaving the Helm before its peak preserves the reflection.");
            Nav.Navigate(0, 0, -1, (Nav.Depth - 590) / Nav.Ship.AscentSpeed); Nav.Brake();
            Assert.That(Nav.Depth, Is.EqualTo(590).Within(.01f), "A real ascent after earning the deep opportunity must not consume it.");
            cabin.OpenNavigation(); presentation.Tick(0); presentation.Tick(8.1f);
            Assert.That(effects.IsPlaying("z3.mix.void"), Is.True);
            cabin.OpenPause(); yield return null; yield return null;
            Assert.That(effects.ActiveSequenceCount, Is.Zero); Assert.That(loop.ZoneThreePresentation.Has("reflection"), Is.False);
            PauseMenuController.Instance.Resume(); yield return null; yield return null;
            presentation.Tick(0); presentation.Tick(8.1f);
            Assert.That(effects.IsPlaying("z3.mix.void"), Is.True);
            Nav.Navigate(0, 1, 0, .1f); presentation.Tick(0);
            Assert.That(effects.IsPlaying("z3.mix.void"), Is.True, "Steering after the start must leave controls usable and keep the sequence.");
            yield return Delivered("reflection", "soft_movement", .25f, presentation.reflection.Duration + 11,
                EffectTarget.Reflection, ScreenInterference.ProceduralReflection);
            Assert.That(presentation.ReflectionCount, Is.EqualTo(1));
            yield return Until(() => effects.ActiveSequenceCount == 0, presentation.reflection.Duration + 1, "The reflection must recover before Continue.");
            Place("zone03-l2");
            string receipts = JsonUtility.ToJson(loop.ZoneThreePresentation);
            Assert.That(loop.SaveCurrent(), Is.True);
            yield return ContinueDebug();
            Assert.That(story.HasObjective("Z3_L2_PHOTO"), Is.True);
            Assert.That(JsonUtility.ToJson(loop.ZoneThreePresentation), Is.EqualTo(receipts));
            Assert.That(Nav.DistanceTravelled - loop.ZoneThreePresentation.radarDistance, Is.GreaterThanOrEqualTo(100));
            cabin.OpenRadar(); radarCharges = Nav.Ship.Radar; cabin.Scan();
            Assert.That(cabin.Radar.IsScanning, Is.True); presentation.Tick(4.1f);
            Assert.That(Nav.Ship.Radar, Is.EqualTo(radarCharges - 1));
            Assert.That(presentation.RadarEventCount, Is.Zero, "Continue retains the radar receipt even when another real scan qualifies.");
            Assert.That(Playing("false_radar_echo"), Is.False);
            cabin.OpenNavigation(); presentation.Tick(8.1f); yield return null;
            Assert.That(presentation.ReflectionCount, Is.Zero, "The earned passage remains eligible, but its saved reflection receipt prevents replay.");
            Assert.That(effects.ActiveSequenceCount, Is.Zero); Assert.That(Playing("soft_movement"), Is.False);
            Assert.That(JsonUtility.ToJson(loop.ZoneThreePresentation), Is.EqualTo(receipts));
        }
        [UnityTest] public IEnumerator InterruptedRadarOpportunitySurvivesContinueAndDeliversWithoutAnotherScanCharge()
        {
            yield return Survey("zone03-l2", false);
            Nav.RestoreVoyage(Nav.Position, Nav.Heading, Nav.Depth, 0);
            cabin.OpenRadar(); presentation.Tick(4.1f);
            int charges = Nav.Ship.Radar;
            cabin.Scan(); presentation.Tick(0);
            Assert.That(cabin.Radar.IsScanning, Is.True); Assert.That(Nav.Ship.Radar, Is.EqualTo(charges - 1));
            Assert.That(loop.ZoneThreePresentation.Has(ZoneThreePresentationState.RadarOpportunity), Is.True);
            Assert.That(loop.ZoneThreePresentation.radarDistance, Is.Zero);
            Assert.That(effects.ActiveSequenceCount, Is.EqualTo(1)); Assert.That(loop.ZoneThreePresentation.Has("radar"), Is.False);
            yield return Until(() => Playing("false_radar_echo") &&
                effects.Surface(EffectTarget.Radar).visual.procedural.ActivePreset == ScreenInterference.FalseRadarSweep,
                2, "Interrupt the first actual voice/preset exposure before its continuous checkpoint.");
            Assert.That(loop.ZoneThreePresentation.Has("radar"), Is.False);
            cabin.ClosePanel(); yield return null; yield return null;
            Assert.That(effects.ActiveSequenceCount, Is.Zero); Assert.That(Playing("false_radar_echo"), Is.False);
            Assert.That(presentation.RadarEventCount, Is.Zero); Assert.That(loop.ZoneThreePresentation.Has("radar"), Is.False);
            // Move through the real driver after the original scan, then save at a canonical POI
            // without refilling resources. The scan anchor must stay zero as distance advances.
            OpenWater(640); Nav.Navigate(1, 0, 0, 1); Nav.Brake(); Assert.That(Nav.DistanceTravelled, Is.GreaterThan(0));
            var poi = story.survey.FindPoi("zone03-l2");
            Nav.RestoreVoyage(story.survey.ContactPosition(poi) + Vector2.down * 14, 0, story.survey.DepthFor(poi), Nav.DistanceTravelled);
            Assert.That(loop.SaveCurrent(), Is.True);
            Assert.That(ExpeditionSaveStore.TryRead(out var saved, out var error), Is.True, error);
            var durable = saved.current.zones.Single(zone => zone.zone == "Zone03").zoneThreePresentation;
            Assert.That(durable.Has(ZoneThreePresentationState.RadarOpportunity), Is.True); Assert.That(durable.Has("radar"), Is.False);
            Assert.That(durable.radarDistance, Is.Zero);
            yield return ContinueDebug();
            Assert.That(loop.ZoneThreePresentation.Has(ZoneThreePresentationState.RadarOpportunity), Is.True);
            Assert.That(loop.ZoneThreePresentation.Has("radar"), Is.False); Assert.That(loop.ZoneThreePresentation.radarDistance, Is.Zero);
            Assert.That(Nav.Ship.Radar, Is.EqualTo(charges - 1));
            cabin.OpenRadar(); Assert.That(cabin.Radar.IsScanning, Is.False, "Continue must retry the earned cue without a new real scan.");
            yield return Delivered("radar", "false_radar_echo", .25f, presentation.falseRadar.Duration + 7,
                EffectTarget.Radar, ScreenInterference.FalseRadarSweep);
            Assert.That(presentation.RadarEventCount, Is.EqualTo(1)); Assert.That(Nav.Ship.Radar, Is.EqualTo(charges - 1));
            Assert.That(loop.ZoneThreePresentation.radarDistance, Is.Zero, "Retry delivery cannot replace the scan's zero anchor.");
            yield return Until(() => effects.ActiveSequenceCount == 0, presentation.falseRadar.Duration + 1, "The retry must finish.");
            cabin.ClosePanel(); cabin.OpenRadar(); presentation.Tick(0); presentation.Tick(4.1f); yield return null; yield return null;
            Assert.That(Playing("false_radar_echo"), Is.False); Assert.That(presentation.RadarEventCount, Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator RestAndJournalRestoreKeepUndeliveredRadarOpportunityAndOriginalAnchor()
        {
            // Functional save-lifecycle fixture: placement/open-water helpers isolate state.
            // These restored positions and direct navigation steps are not voyage evidence.
            yield return Survey("zone03-l2", false);
            Assert.That(loop.Day, Is.EqualTo(1));
            cabin.OpenRadar(); presentation.Tick(4.1f);
            int chargesBeforeScan = Nav.Ship.Radar;
            float scanDistance = Nav.DistanceTravelled;
            cabin.Scan(); presentation.Tick(0);
            Assert.That(Nav.Ship.Radar, Is.EqualTo(chargesBeforeScan - 1));
            Assert.That(loop.ZoneThreePresentation.Has(ZoneThreePresentationState.RadarOpportunity), Is.True);
            Assert.That(loop.ZoneThreePresentation.radarDistance, Is.EqualTo(scanDistance));
            yield return Until(() => Playing("false_radar_echo") &&
                effects.Surface(EffectTarget.Radar).visual.procedural.Visible &&
                effects.Surface(EffectTarget.Radar).visual.procedural.ActivePreset == ScreenInterference.FalseRadarSweep,
                2, "Observe the real first exposure, then interrupt before its delivery checkpoint.");
            Assert.That(loop.ZoneThreePresentation.Has("radar"), Is.False);
            cabin.ClosePanel(); yield return null; yield return null;
            Assert.That(effects.ActiveSequenceCount, Is.Zero); Assert.That(Playing("false_radar_echo"), Is.False);
            Assert.That(presentation.RadarEventCount, Is.Zero);

            Assert.That(loop.Rest(), Is.True, loop.LastError);
            Assert.That(loop.Day, Is.EqualTo(2));
            Assert.That(Nav.Ship.Radar, Is.EqualTo(Nav.Ship.RadarCapacity), "Rest refills charges; cue delivery must not spend them.");
            Assert.That(ExpeditionSaveStore.TryRead(out var afterRest, out var error), Is.True, error);
            Assert.That(afterRest.current.day, Is.EqualTo(2));
            Assert.That(afterRest.hasDayStart, Is.True); Assert.That(afterRest.dayStart.day, Is.EqualTo(2));
            var dayOne = afterRest.journal.Single(entry => entry.day == 1).checkpoint;
            Assert.That(dayOne.day, Is.EqualTo(1));
            Assert.That(dayOne.ship.radar, Is.EqualTo(chargesBeforeScan - 1), "The Journal keeps pre-refill day-one charges.");
            foreach (var snapshot in new[] { afterRest.current, afterRest.dayStart, dayOne })
            {
                var state = snapshot.zones.Single(zone => zone.zone == "Zone03").zoneThreePresentation;
                Assert.That(state.Has(ZoneThreePresentationState.RadarOpportunity), Is.True);
                Assert.That(state.Has("radar"), Is.False); Assert.That(state.radarDistance, Is.EqualTo(scanDistance));
            }
            string dayOnePresentation = JsonUtility.ToJson(dayOne.zones.Single(zone => zone.zone == "Zone03").zoneThreePresentation);
            int dayTwoCharges = Nav.Ship.Radar;
            cabin.OpenRadar(); Assert.That(cabin.Radar.IsScanning, Is.False);
            yield return Delivered("radar", "false_radar_echo", .25f, presentation.falseRadar.Duration + 7,
                EffectTarget.Radar, ScreenInterference.FalseRadarSweep);
            Assert.That(Nav.Ship.Radar, Is.EqualTo(dayTwoCharges));
            Assert.That(presentation.RadarEventCount, Is.EqualTo(1));
            Assert.That(loop.ZoneThreePresentation.radarDistance, Is.EqualTo(scanDistance));

            // Earn the deep-passage marker through the real navigation driver without
            // exposing its Helm audience. Journal rollback must remove future opportunity too.
            OpenWater(640);
            for (int step = 0; step < 80 && Nav.DistanceTravelled - scanDistance < 110; step++) Nav.Navigate(1, 0, 0, .5f);
            Nav.Brake(); Assert.That(Nav.DistanceTravelled - scanDistance, Is.GreaterThanOrEqualTo(100));
            presentation.Tick(0);
            Assert.That(loop.ZoneThreePresentation.Has(ZoneThreePresentationState.ReflectionOpportunity), Is.True);
            Assert.That(loop.ZoneThreePresentation.Has("reflection"), Is.False);
            Assert.That(effects.ActiveSequenceCount, Is.GreaterThan(0), "Restore while the delivered radar tail still owns presentation.");
            Assert.That(loop.RestoreDay(1), Is.True, loop.LastError);
            Assert.That(loop.Day, Is.EqualTo(1));
            Assert.That(JsonUtility.ToJson(loop.ZoneThreePresentation), Is.EqualTo(dayOnePresentation));
            Assert.That(loop.ZoneThreePresentation.Has("radar"), Is.False);
            Assert.That(loop.ZoneThreePresentation.Has(ZoneThreePresentationState.ReflectionOpportunity), Is.False);
            Assert.That(effects.ActiveSequenceCount, Is.Zero); Assert.That(Playing("false_radar_echo"), Is.False);
            Assert.That(effects.Surface(EffectTarget.Radar).visual.procedural.Visible, Is.False);
            Assert.That(effects.Surface(EffectTarget.Reflection).visual.procedural.Visible, Is.False);
            Assert.That(cabin.Radar.IsScanning, Is.False);
            Assert.That(Nav.Ship.Radar, Is.EqualTo(chargesBeforeScan - 1), "Journal restores day-one charges rather than the Rest refill.");
            Assert.That(ExpeditionSaveStore.TryRead(out var restored, out error), Is.True, error);
            Assert.That(restored.hasDayStart, Is.False, "Restoring day one drops the future day-two start snapshot.");
            Assert.That(restored.dayStart, Is.Null);
            Assert.That(JsonUtility.ToJson(restored.journal.Single(entry => entry.day == 1).checkpoint.zones
                .Single(zone => zone.zone == "Zone03").zoneThreePresentation), Is.EqualTo(dayOnePresentation));

            // The controller instance survives same-zone Journal restore. Its telemetry
            // counter is cumulative, but each restored timeline earns one delivery receipt.
            int commitsBeforeRestoredRetry = presentation.RadarEventCount;
            int restoredCharges = Nav.Ship.Radar;
            cabin.OpenRadar();
            yield return Delivered("radar", "false_radar_echo", .25f, presentation.falseRadar.Duration + 7,
                EffectTarget.Radar, ScreenInterference.FalseRadarSweep);
            Assert.That(presentation.RadarEventCount, Is.EqualTo(commitsBeforeRestoredRetry + 1));
            Assert.That(Nav.Ship.Radar, Is.EqualTo(restoredCharges));
            Assert.That(loop.ZoneThreePresentation.radarDistance, Is.EqualTo(scanDistance));
            Assert.That(loop.ZoneThreePresentation.played.Count(id => id == "radar"), Is.EqualTo(1));
            yield return Until(() => effects.ActiveSequenceCount == 0, presentation.falseRadar.Duration + 1, "The restored retry must recover.");
            Assert.That(loop.SaveCurrent(), Is.True, loop.LastError);
            string delivered = JsonUtility.ToJson(loop.ZoneThreePresentation);
            yield return ContinueDebug();
            Assert.That(loop.Day, Is.EqualTo(1));
            Assert.That(JsonUtility.ToJson(loop.ZoneThreePresentation), Is.EqualTo(delivered));
            Assert.That(Nav.Ship.Radar, Is.EqualTo(restoredCharges));
            Assert.That(JsonUtility.ToJson(loop.Journal.Single(entry => entry.day == 1).checkpoint.zones
                .Single(zone => zone.zone == "Zone03").zoneThreePresentation), Is.EqualTo(dayOnePresentation));
            cabin.OpenRadar(); Assert.That(cabin.Radar.IsScanning, Is.False);
            yield return new WaitForSeconds(presentation.falseRadar.Duration + 5);
            Assert.That(presentation.RadarEventCount, Is.Zero, "Continue honors the restored timeline's saved delivered receipt.");
            Assert.That(Playing("false_radar_echo"), Is.False); Assert.That(effects.ActiveSequenceCount, Is.Zero);
            Assert.That(Nav.Ship.Radar, Is.EqualTo(restoredCharges));
        }
        [UnityTest] public IEnumerator RadarPublicationFailuresRetryOpportunityAndDeliveredReceiptWithoutLiveReplay()
        {
            yield return Survey("zone03-l2", false); cabin.OpenRadar(); effects.ClearTransients();
            Assert.That(presentation.Preview(ZoneThreeDemo.DelayedEngine).IsValid, Is.True,
                "An authored preview keeps the sequence/quiet gate busy while the real scan opportunity is observed.");
            Assert.That(loop.SaveCurrent(), Is.True);
            string valid = ExpeditionSaveStore.PathOverride, publishedPath = ExpeditionSaveStore.SavePath;
            byte[] beforeOpportunity = File.ReadAllBytes(publishedPath);
            int charges = Nav.Ship.Radar; float scanDistance = Nav.DistanceTravelled;
            // Debug Start appends .debug. An existing file as parent blocks both selected paths.
            string blocked = Path.Combine(publishedPath, "blocked-radar.json");
            try
            {
                ExpeditionSaveStore.PathOverride = blocked;
                cabin.Scan(); presentation.Tick(0);
                Assert.That(cabin.Radar.IsScanning, Is.True); Assert.That(Nav.Ship.Radar, Is.EqualTo(charges - 1));
                Assert.That(loop.LastError, Is.Not.Null.And.Not.Empty);
                Assert.That(loop.ZoneThreePresentation.Has(ZoneThreePresentationState.RadarOpportunity), Is.False);
                Assert.That(loop.ZoneThreePresentation.Has("radar"), Is.False); Assert.That(presentation.RadarEventCount, Is.Zero);
                CollectionAssert.AreEqual(beforeOpportunity, File.ReadAllBytes(publishedPath));
                cabin.ClosePanel(); effects.ClearTransients(); yield return null; yield return null;
                ExpeditionSaveStore.PathOverride = valid;
                yield return Until(() => loop.ZoneThreePresentation.Has(ZoneThreePresentationState.RadarOpportunity), 3,
                    "Retry the observed scan's opportunity after write recovery, even though its real sweep has ended.");
                Assert.That(cabin.Radar.IsScanning, Is.False); Assert.That(loop.ZoneThreePresentation.Has("radar"), Is.False);
                Assert.That(loop.ZoneThreePresentation.radarDistance, Is.EqualTo(scanDistance));
                Assert.That(Nav.Ship.Radar, Is.EqualTo(charges - 1));
                byte[] opportunityPublished = File.ReadAllBytes(publishedPath);
                Assert.That(ExpeditionSaveStore.TryRead(out var saved, out var error), Is.True, error);
                var durable = saved.current.zones.Single(zone => zone.zone == "Zone03").zoneThreePresentation;
                Assert.That(durable.Has(ZoneThreePresentationState.RadarOpportunity), Is.True); Assert.That(durable.Has("radar"), Is.False);

                ExpeditionSaveStore.PathOverride = blocked;
                cabin.OpenRadar(); presentation.Tick(0); presentation.Tick(4.1f);
                Assert.That(presentation.RadarEventCount, Is.Zero); Assert.That(loop.ZoneThreePresentation.Has("radar"), Is.False);
                yield return Until(() => Voices().Any(source => source.clip.name == "false_radar_echo" && source.time >= .6f) &&
                    effects.Surface(EffectTarget.Radar).visual.procedural.ActivePreset == ScreenInterference.FalseRadarSweep,
                    presentation.falseRadar.Duration + 7, "The real voice and preset must pass the checkpoint while publication is blocked.");
                yield return null; yield return null;
                Assert.That(loop.LastError, Is.Not.Null.And.Not.Empty); Assert.That(presentation.RadarEventCount, Is.Zero);
                Assert.That(loop.ZoneThreePresentation.Has("radar"), Is.False);
                yield return Until(() => effects.ActiveSequenceCount == 0, presentation.falseRadar.Duration + 1, "The delivered sequence must finish while its receipt remains pending.");
                float until = Time.realtimeSinceStartup + 5.25f;
                while (Time.realtimeSinceStartup < until)
                {
                    Assert.That(Playing("false_radar_echo"), Is.False, "A delivered cue cannot replay while retrying its failed receipt.");
                    Assert.That(effects.Surface(EffectTarget.Radar).visual.procedural.ActivePreset, Is.Not.EqualTo(ScreenInterference.FalseRadarSweep));
                    Assert.That(loop.ZoneThreePresentation.Has("radar"), Is.False); Assert.That(presentation.RadarEventCount, Is.Zero);
                    yield return null;
                }
                CollectionAssert.AreEqual(opportunityPublished, File.ReadAllBytes(publishedPath), "Failed delivery publication preserves the durable opportunity-only save.");
                ExpeditionSaveStore.PathOverride = valid;
                yield return Until(() => loop.ZoneThreePresentation.Has("radar"), 3, "Save recovery must publish the delivered receipt without replaying its content.");
                Assert.That(loop.LastError, Is.Null.Or.Empty); Assert.That(presentation.RadarEventCount, Is.EqualTo(1));
                Assert.That(Playing("false_radar_echo"), Is.False); Assert.That(effects.ActiveSequenceCount, Is.Zero);
                Assert.That(Nav.Ship.Radar, Is.EqualTo(charges - 1)); Assert.That(loop.ZoneThreePresentation.radarDistance, Is.EqualTo(scanDistance));
                Assert.That(ExpeditionSaveStore.TryRead(out saved, out error), Is.True, error);
                durable = saved.current.zones.Single(zone => zone.zone == "Zone03").zoneThreePresentation;
                Assert.That(durable.Has(ZoneThreePresentationState.RadarOpportunity), Is.True); Assert.That(durable.Has("radar"), Is.True);
            }
            finally { ExpeditionSaveStore.PathOverride = valid; }
        }
        [UnityTest] public IEnumerator OpportunityZeroAnchorAndLegacyDeliveredReceiptsRoundTripWithoutReplay()
        {
            // Receipt seeding here tests save compatibility only; it is not evidence of delivery.
            Nav.RestoreVoyage(Nav.Position, Nav.Heading, Nav.Depth, 37);
            Assert.That(loop.CommitZoneThreeCue(ZoneThreePresentationState.RadarOpportunity, 0), Is.True);
            Assert.That(loop.ZoneThreePresentation.radarDistance, Is.Zero);
            Assert.That(ExpeditionSaveStore.TryRead(out var saved, out var error), Is.True, error);
            var durable = saved.current.zones.Single(zone => zone.zone == "Zone03").zoneThreePresentation;
            Assert.That(durable.Has(ZoneThreePresentationState.RadarOpportunity), Is.True); Assert.That(durable.Has("radar"), Is.False);
            Assert.That(durable.radarDistance, Is.Zero);
            Assert.That(loop.CommitZoneThreeCue("radar", Nav.DistanceTravelled), Is.True);
            Assert.That(loop.ZoneThreePresentation.radarDistance, Is.Zero, "Explicit delivery distance must not move an existing opportunity anchor.");
            // Model the old played-list shape: delivered receipts existed without opportunity markers.
            var state = loop.ZoneThreePresentation;
            state.played = new List<string> { "engine", "radar", "reflection", "radar", "" }; state.radarDistance = 17;
            state.Normalize(); Assert.That(loop.SaveCurrent(), Is.True);
            yield return ContinueDebug();
            state = loop.ZoneThreePresentation;
            CollectionAssert.AreEquivalent(new[] { "engine", "radar", "reflection" }, state.played);
            Assert.That(state.Has(ZoneThreePresentationState.RadarOpportunity), Is.False);
            Assert.That(state.Has(ZoneThreePresentationState.ReflectionOpportunity), Is.False); Assert.That(state.radarDistance, Is.EqualTo(17));
            cabin.OpenRadar(); int charges = Nav.Ship.Radar; cabin.Scan(); presentation.Tick(4.1f);
            Assert.That(Nav.Ship.Radar, Is.EqualTo(charges - 1));
            yield return new WaitForSeconds(RadarDisplay.SweepDuration + .1f);
            cabin.OpenNavigation(); Nav.RestoreVoyage(Nav.Position, Nav.Heading, 640, 200); presentation.Tick(8.1f);
            yield return null; yield return null;
            Assert.That(presentation.EngineEventCount + presentation.RadarEventCount + presentation.ReflectionCount, Is.Zero);
            Assert.That(Playing("engine_reflection"), Is.False); Assert.That(Playing("false_radar_echo"), Is.False); Assert.That(Playing("soft_movement"), Is.False);
            Assert.That(effects.ActiveSequenceCount, Is.Zero); Assert.That(loop.ZoneThreePresentation.radarDistance, Is.EqualTo(17));
            CollectionAssert.AreEquivalent(new[] { "engine", "radar", "reflection" }, loop.ZoneThreePresentation.played);
        }
        [UnityTest] public IEnumerator PanelBrakeCannotArmEngineAndFocusLossBeforeCheckpointKeepsEarnedRetry()
        {
            yield return Survey("zone03-l1", true); effects.ClearTransients(); cabin.OpenNavigation(); OpenWater(640);
            Nav.Navigate(1, 0, 0, 2.2f); presentation.Tick(4.2f);
            cabin.OpenRadar(); presentation.Tick(0); yield return null; yield return null;
            Assert.That(loop.ZoneThreePresentation.Has("engine"), Is.False); Assert.That(Playing("engine_reflection"), Is.False);
            cabin.OpenNavigation(); presentation.Tick(4.1f);
            Assert.That(Playing("engine_reflection"), Is.False, "Returning to the Helm cannot turn a panel-induced brake into an earned stop edge.");
            Nav.Navigate(1, 0, 0, 2.2f); presentation.Tick(4.2f); cabin.Brake(); presentation.Tick(0);
            Assert.That(Playing("engine_reflection"), Is.True); Assert.That(loop.ZoneThreePresentation.Has("engine"), Is.False);
            presentation.SendMessage("OnApplicationFocus", false); yield return null; yield return null;
            Assert.That(Playing("engine_reflection"), Is.False); Assert.That(effects.ActiveSequenceCount, Is.Zero);
            Assert.That(loop.ZoneThreePresentation.Has("engine"), Is.False); Assert.That(presentation.EngineEventCount, Is.Zero);
            presentation.SendMessage("OnApplicationFocus", true);
            yield return Delivered("engine", "engine_reflection", .35f, presentation.delayedEngine.Duration + 7);
            Assert.That(presentation.EngineEventCount, Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator DemoBProductionFalseSweepChangesRadarPixelsWithoutAnyGameplayAuthority()
        {
            cabin.OpenRadar(); cabin.Radar.ResetScanState(); yield return null;
            Frame before = null, after = null; yield return Shot("B-01-radar-before", frame => before = frame);
            string authority = Authority(); int count = presentation.RadarEventCount;
            Assert.That(presentation.Preview(ZoneThreeDemo.FalseRadar).IsValid, Is.True);
            var surface = effects.Surface(EffectTarget.Radar);
            yield return Until(() => surface.visual.procedural.ActivePreset == ScreenInterference.FalseRadarSweep, 2, "The production secondary sweep must be visible.");
            yield return new WaitForSeconds(1.2f); Assert.That(Playing("false_radar_echo"), Is.True);
            yield return Shot("B-02-false-secondary-sweep", frame => after = frame);
            var evidence = Difference("B-false-radar", before, after, ScreenRegion(surface, after), Voices().Select(source => source.clip.name));
            Assert.That(evidence.changedFraction, Is.GreaterThan(.003f), "A real radar display must visibly change.");
            Assert.That(cabin.Radar.IsScanning, Is.False); Assert.That(Authority(), Is.EqualTo(authority));
            Assert.That(presentation.RadarEventCount, Is.EqualTo(count), "Preview neither writes a story receipt nor charges a real scan.");
            yield return Until(() => !surface.visual.procedural.Visible, presentation.falseRadar.Duration + 1, "Sweep must recover.");
            yield return Shot("B-03-radar-recovered"); SaveEvidence(evidence);
        }
        [UnityTest] public IEnumerator DemoCRealMonitorReflectionHasMeasuredPixelsAndPauseCancelsEveryOwnedTransient()
        {
            cabin.OpenNavigation(); yield return null; Frame before = null, after = null;
            yield return Shot("C-01-monitor-before", frame => before = frame); string authority = Authority();
            Assert.That(presentation.Preview(ZoneThreeDemo.Reflection).IsValid, Is.True); var surface = effects.Surface(EffectTarget.Reflection);
            yield return Until(() => surface.visual.procedural.ActivePreset == ScreenInterference.ProceduralReflection, 5, "The actual monitor must show its procedural reflection.");
            yield return new WaitForSeconds(.3f); Assert.That(Playing("soft_movement"), Is.True);
            Assert.That(presentation.EnvironmentLevel, Is.LessThan(.4f));
            yield return Shot("C-02-procedural-glass-reflection", frame => after = frame);
            var evidence = Difference("C-reflection", before, after, ScreenRegion(surface, after), Voices().Select(source => source.clip.name));
            Assert.That(evidence.changedFraction, Is.GreaterThan(.08f)); Assert.That(Authority(), Is.EqualTo(authority));
            Assert.That(surface.visual.AnomalyVisible, Is.False, "The old crew sprite must not be substituted for the procedural composition.");
            cabin.OpenPause(); yield return null; yield return null;
            Assert.That(effects.ActiveSequenceCount, Is.Zero); Assert.That(surface.visual.procedural.Visible, Is.False);
            Assert.That(Playing("soft_movement"), Is.False); Assert.That(Playing("engine_reflection"), Is.False);
            PauseMenuController.Instance.Resume(); yield return null; yield return null;
            Assert.That(surface.visual.procedural.Visible, Is.False); Assert.That(Playing("ambient_submarine_engine"), Is.True);
            Assert.That(cabin.GetComponent<EffectAudioExecutor>().GetComponentsInChildren<AudioSource>(true).Length, Is.LessThanOrEqualTo(16));
            yield return Shot("C-03-monitor-recovered-after-pause"); SaveEvidence(evidence);
        }
        [UnityTest] public IEnumerator DemoDRealDemolitionLayersAudioResumesAtElapsedTimeAndRevealsTerrainAtOnePointFiveSeconds()
        {
            yield return Survey("zone03-l1", true); yield return Survey("zone03-l2", false); yield return Survey("zone03-l4", true);
            Assert.That(story.MainLocationsComplete, Is.True); Assert.That(story.ApplyProgressionAction(), Is.True);
            Assert.That(Playing("rock_breaker_assembly"), Is.True); Assert.That(story.ApplyProgressionAction(), Is.True);
            effects.ClearTransients(); cabin.ClosePanel(); var area = loop.ActiveMap.rockInteractionArea;
            Nav.RestoreVoyage(area.mapPosition, 90, story.survey.DepthFor(area), Nav.DistanceTravelled);
            Frame before = null, after = null; yield return Shot("D-01-cabin-before-break", frame => before = frame);
            // PNG encoding is synchronous; let that capture frame drain before starting
            // a .28-second real-time cue, so the QA capture cannot skip its whole attack.
            yield return null; yield return null;
            var audioEvidence = ZoneThreeAudioEvidence.Begin(Object.FindAnyObjectByType<AudioListener>(), 20);
            Assert.That(story.ApplyProgressionAction(), Is.True); Assert.That(rock.IsActive, Is.True); Assert.That(rock.Duration, Is.EqualTo(10));
            Assert.That(rock.DeferTerrainReveal, Is.True); Assert.That(Nav.CanOccupy(loop.ActiveMap.exitArea.mapPosition), Is.False);
            Assert.That(Playing("underwater_detonation"), Is.True); var heard = new HashSet<string> { "underwater_detonation" };
            // Capture the actual rendered pressure peak, after its first update, rather than
            // the scheduling frame at age zero before the overlay has been composited.
            yield return Until(() => rock.Elapsed >= .1f, 2, "The pressure pulse must reach its visible attack.");
            Assert.That(effects.Surface(EffectTarget.Global).overlay.tint.color.a, Is.GreaterThan(.05f),
                "Pressure elapsed=" + rock.Elapsed + ", frame=" + Time.unscaledDeltaTime + ", active=" +
                effects.IsPlaying("z3.visual.pressure") + ", suppressed=" + effects.Suppressed +
                ", audience=" + effects.Surface(EffectTarget.Global).Available);
            Frame pressureFrame = null;
            yield return Shot("D-02-pressure-impact", frame => pressureFrame = frame);
            var pressureEvidence = Difference("D-pressure", before, pressureFrame,
                new RectInt(0, pressureFrame.height / 2, pressureFrame.width, pressureFrame.height / 2), heard);
            Assert.That(pressureEvidence.changedFraction, Is.GreaterThan(.25f));
            Assert.That(pressureEvidence.meanChannelDifference, Is.GreaterThan(.03f), "A pressure still must show more than a changed status label or subpixel shake.");
            SaveEvidence(pressureEvidence);
            yield return Until(() => rock.Elapsed >= .25f, 3, "Initial impact should run.");
            foreach (var voice in Voices()) heard.Add(voice.clip.name);
            Time.timeScale = 0; float pausedAt = rock.Elapsed; Assert.That(pausedAt, Is.LessThan(1.5f)); yield return new WaitForSecondsRealtime(.25f);
            Assert.That(rock.Elapsed, Is.EqualTo(pausedAt)); Assert.That(Playing("underwater_detonation"), Is.False);
            Time.timeScale = 1; yield return null; yield return null;
            var resumed = Voices().SingleOrDefault(source => source.clip.name == "underwater_detonation");
            Assert.That(resumed, Is.Not.Null); Assert.That(resumed.time, Is.GreaterThanOrEqualTo(pausedAt - .12f), "Resume seeks the production cue instead of replaying the explosion.");
            Assert.That(rock.DeferTerrainReveal, Is.True);
            yield return Until(() => rock.Elapsed >= 1.55f, 3, "Authoritative reveal timing should advance.");
            Assert.That(rock.DeferTerrainReveal, Is.False); Assert.That(Nav.CanOccupy(loop.ActiveMap.exitArea.mapPosition), Is.True);
            yield return Shot("D-03-layered-collapse-debris", frame => after = frame);
            var global = effects.Surface(EffectTarget.Global);
            Assert.That(global.visual.procedural.ActivePreset, Is.EqualTo(ScreenInterference.RockDebris));
            var evidence = Difference("D-demolition", before, after, new RectInt(0, 0, after.width, after.height), heard);
            Assert.That(evidence.changedFraction, Is.GreaterThan(.003f));
            float end = Time.realtimeSinceStartup + 12;
            while (rock.IsActive && Time.realtimeSinceStartup < end)
            { foreach (var voice in Voices()) heard.Add(voice.clip.name); yield return null; }
            Assert.That(rock.IsActive, Is.False); Assert.That(rock.SequenceCount, Is.EqualTo(1));
            Assert.That(Nav.ExpeditionBlocked, Is.False, "The ten-second gameplay lock must end independently of the audio tail.");
            Assert.That(Playing("post_explosion_resonance"), Is.True, "The authored receding resonance remains after stabilization.");
            yield return Until(() => !Playing("post_explosion_resonance"), presentation.demolition.Duration + 1, "The bounded receding tail must finish.");
            foreach (string clip in new[] { "underwater_detonation", "rock_collapse_01", "rock_collapse_02", "rock_collapse_03", "post_explosion_resonance" })
            { Assert.That(heard, Does.Contain(clip)); Assert.That(Playing(clip), Is.False); }
            Assert.That(story.HasWorldFlag("ROCK_BARRIER_DESTROYED"), Is.True); Assert.That(loop.CurrentProgress.exitUnlocked, Is.True);
            Assert.That(story.ApplyProgressionAction(), Is.False); Assert.That(global.visual.procedural.Visible, Is.False);
            yield return Shot("D-04-cabin-stabilized"); evidence.playingClipBindings = heard.ToArray(); SaveEvidence(evidence);
            var mix = audioEvidence.Finish(Path.Combine(Qa, "D-gameplay-listener.wav"));
            File.WriteAllText(Path.Combine(Qa, "D-gameplay-listener.json"), JsonUtility.ToJson(mix, true));
            Assert.That(mix.nonzero, Is.True, "Record the actual layered demolition mix.");
            Assert.That(mix.durationSeconds, Is.GreaterThan(10)); Assert.That(mix.clippedSamples, Is.Zero);
        }
        [UnityTest] public IEnumerator ScopedPhotoEffectStaysBehindForegroundWindowsAndReflectionFollowsTiltedNativeGlass()
        {
            yield return Survey("zone03-l2", false); cabin.OpenComputer(); computer.OpenPhotoLab(); yield return null; yield return null;
            effects.ClearTransients(); var lab = computer.GetComponentInChildren<PhotoLabView>(true);
            var photoSurface = effects.Surface(EffectTarget.PhotoLab); Frame baseline = null, composed = null;
            yield return Shot("E-01-photo-before-scoped-effect", frame => baseline = frame);
            // Extend only the lifetime so window movement/captures can inspect the same production shader/strength.
            var handle = effects.Play("z3.visual.photo-reflection", new EffectContext { Duration = 20 }); Assert.That(handle.IsValid, Is.True);
            yield return new WaitForSeconds(.5f);
            var graphic = photoSurface.visual.procedural.Graphic;
            Assert.That(graphic, Is.Not.Null); Assert.That(graphic.raycastTarget, Is.False);
            Assert.That(graphic.transform.parent, Is.SameAs(lab.preview.transform));
            Assert.That(graphic.GetComponentInParent<ComputerWindow>(), Is.SameAs(lab.GetComponent<ComputerWindow>()));
            Assert.That(graphic.rectTransform.rect.size, Is.EqualTo(lab.preview.rectTransform.rect.size));
            yield return Shot("E-02-photo-with-scoped-effect", frame => composed = frame);
            var visible = Difference("E-scoped-photo", baseline, composed, ScreenRegion(photoSurface, composed), Voices().Select(source => source.clip.name));
            Assert.That(visible.changedFraction, Is.GreaterThan(.01f), "The production photo effect must render before testing occlusion.");
            computer.OpenMissionLog(); var front = computer.GetComponentInChildren<MissionLogView>().GetComponent<ComputerWindow>();
            Assert.That(front, Is.Not.Null); if (!front.Maximized) front.ToggleMaximize(); yield return null; yield return null;
            Assert.That(lab.gameObject.activeInHierarchy, Is.True, "A background desktop window stays open; this must be real sibling occlusion.");
            Assert.That(effects.IsActive(handle), Is.True); Assert.That(graphic.enabled, Is.True);
            Frame covered = null, uncoveredEffect = null;
            yield return Shot("E-03-foreground-window-over-effect", frame => covered = frame);
            effects.Stop(handle); yield return Shot("E-04-foreground-window-baseline", frame => uncoveredEffect = frame);
            var hidden = Difference("E-scoped-photo-occlusion", covered, uncoveredEffect, ScreenRegion(photoSurface, covered), Array.Empty<string>());
            Assert.That(hidden.changedFraction, Is.LessThan(.005f), "A background photo effect must not paint over the foreground mission window.");

            cabin.ClosePanel(); cabin.OpenNavigation(); yield return null;
            var miniMap = (RectTransform)cabin.NavigationPanel.transform.Find("MiniMap");
            Assert.That(Quaternion.Angle(miniMap.rotation, Quaternion.identity), Is.GreaterThan(.1f), "Check the authored tilted glass, not an axis-aligned test rectangle.");
            var reflection = effects.Play("z3.visual.reflection", new EffectContext { Duration = 5 }); Assert.That(reflection.IsValid, Is.True);
            // The authored .18 s attack begins at zero opacity; the lazy graphic is created
            // by a later production render rather than synchronously by Play().
            yield return Until(() => effects.Surface(EffectTarget.Reflection).visual.procedural.ActivePreset == ScreenInterference.ProceduralReflection,
                1, "Wait for the production reflection attack before inspecting its native glass geometry.");
            var reflectionGraphic = effects.Surface(EffectTarget.Reflection).visual.procedural.Graphic;
            Assert.That(reflectionGraphic, Is.Not.Null); Assert.That(reflectionGraphic.transform.parent, Is.SameAs(miniMap));
            Assert.That(Quaternion.Angle(reflectionGraphic.rectTransform.rotation, miniMap.rotation), Is.LessThan(.01f));
            var expected = new Vector3[4]; var actual = new Vector3[4]; miniMap.GetWorldCorners(expected); reflectionGraphic.rectTransform.GetWorldCorners(actual);
            for (int i = 0; i < 4; i++) Assert.That(Vector3.Distance(expected[i], actual[i]), Is.LessThan(.1f));
            yield return Shot("E-05-reflection-follows-tilted-glass"); effects.Stop(reflection);
            SaveEvidence(visible); SaveEvidence(hidden);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1; PauseMenuController.Instance?.Resume();
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            if (SceneFlowController.Instance != null) Object.Destroy(SceneFlowController.Instance.gameObject); yield return null;
            ExpeditionSaveStore.SelectNormalTimeline(); ExpeditionSaveStore.PathOverride = oldSave; PhotoCaptureService.ArchivePathOverride = oldPhotos;
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
    }
}
