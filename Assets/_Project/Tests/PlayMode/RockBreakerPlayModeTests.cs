using System;
using System.Collections;
using System.IO;
using G10.Prototype.Computer;
using G10.Prototype.Core;
using G10.Prototype.Feedback;
using G10.Prototype.Missions;
using G10.Prototype.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace G10.Prototype.Tests
{
    public sealed class RockBreakerPlayModeTests
    {
        private string folder, oldSave, oldPhotos;
        private bool oldShake;
        private SceneFlowController flow;
        private CabinStationView Cabin => Object.FindAnyObjectByType<CabinStationView>();
        private ExpeditionLoop Loop => Object.FindAnyObjectByType<ExpeditionLoop>();
        private ZoneOneStory Story => Cabin.GetComponent<ZoneOneStory>();
        private RockBreakerSequence Effect => Cabin.GetComponent<RockBreakerSequence>();
        private ImpactShake ProductionShake => Cabin.GetComponent<EffectDirector>().Surface(EffectTarget.Global).shake.shake;
        private PhotoSurveyMap Map => Cabin.GetComponent<WorldMapController>().zoneMaps[2].GetComponent<ZoneMapPresentation>().overlay;
        [UnitySetUp] public IEnumerator Setup()
        {
            folder = Path.Combine(Application.temporaryCachePath, "RockRound3-" + Guid.NewGuid().ToString("N"));
            oldSave = ExpeditionSaveStore.PathOverride; oldPhotos = PhotoCaptureService.ArchivePathOverride;
            oldShake = ImpactShake.ScreenShakeEnabled;
            ExpeditionSaveStore.SelectNormalTimeline(); ExpeditionSaveStore.PathOverride = Path.Combine(folder, "timeline.json");
            PhotoCaptureService.ArchivePathOverride = Path.Combine(folder, "photos"); TutorialTestSave.SeedReturningPlayer();
            if (SceneFlowController.Instance != null) { Object.Destroy(SceneFlowController.Instance.gameObject); yield return null; }
            yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single); yield return null;
            flow = SceneFlowController.Instance; yield return Wait();
            var menu = Object.FindAnyObjectByType<MainMenuDebugStart>(); menu.Open(); menu.SelectZone(3); menu.StartRun(); yield return Wait();
            Cabin.ClosePanel();
        }
        private void Ready()
        {
            foreach (var location in Story.config.locations)
                if (location.visibility == LocationVisibility.Visible)
                    foreach (var objective in location.objectives)
                        Assert.That(objective.type == MissionObjectiveType.Capture ? Story.ResolveCapture(location.poiId, objective) : Story.RecordObjective(location.poiId, objective.type, objective.targetId), Is.True);
            while (Story.PendingGateObjective.type != MissionObjectiveType.DestroyObstacle)
                Assert.That(Story.ApplyProgressionAction(), Is.True, Story.ProgressionActionDescription);
            var point = Loop.ActiveMap.rockInteractionArea;
            Cabin.Navigation.RestoreVoyage(point.mapPosition, 90, point.targetDepth, 0);
        }
        [UnityTest] public IEnumerator MarkerMatchesTriggerAndTenSecondEventCommitsOnceAndRestoresFrame()
        {
            ImpactShake.SetScreenShakeEnabled(true);
            Assert.That(Map.RockApproachVisible, Is.False);
            Ready(); Cabin.GetComponent<WorldMapController>().OpenZone(2); yield return null;
            Assert.That(Map.RockApproachVisible, Is.True);
            var point = Loop.ActiveMap.rockInteractionArea;
            Assert.That(point.mapPosition, Is.EqualTo(new Vector2(1480, 150))); Assert.That(point.arrivalRadius, Is.EqualTo(70));
            Assert.That(Map.RockApproachRadii, Is.EqualTo(Vector2.Scale(Map.rectTransform.rect.size,
                new Vector2(70 / Loop.ActiveMap.WorldSize.x, 70 / Loop.ActiveMap.WorldSize.y))));
            Cabin.Navigation.RestoreVoyage(point.mapPosition + Vector2.up * 71, 90, 590, 0);
            Assert.That(Map.RockPrompt, Does.Contain("APPROACH")); Assert.That(Story.ApplyProgressionAction(), Is.False);
            Cabin.Navigation.RestoreVoyage(point.mapPosition, 90, 579, 0);
            Assert.That(Map.RockPrompt, Does.Contain("590 M")); Assert.That(Story.ApplyProgressionAction(), Is.False);
            Cabin.Navigation.RestoreVoyage(point.mapPosition, 90, 590, 0);
            Assert.That(Map.RockPrompt, Is.EqualTo("EXPLOSIVES + DETONATOR READY"));
            Map.SetPointer(Loop.ActiveMap.CoordinatesToUV(point.mapPosition));
            Canvas.ForceUpdateCanvases();
            ScreenCapture.CaptureScreenshot(CabinNavigationPlayModeTests.CapturePath("round3-rock-marker.png"));
            yield return null;
            var motion = ProductionShake; Assert.That(motion, Is.Not.Null);
            var original = motion.transform.localPosition;
            string path = ExpeditionSaveStore.PathOverride; byte[] disk = File.ReadAllBytes(ExpeditionSaveStore.SavePath);
            ExpeditionSaveStore.PathOverride = Path.Combine(path, "blocked.json");
            try { Assert.That(Story.ApplyProgressionAction(), Is.False); }
            finally { ExpeditionSaveStore.PathOverride = path; }
            Assert.That(Effect.IsActive, Is.False); Assert.That(Map.RockCompleted, Is.False);
            CollectionAssert.AreEqual(disk, File.ReadAllBytes(ExpeditionSaveStore.SavePath));
            float began = Time.realtimeSinceStartup;
            Assert.That(Story.ApplyProgressionAction(), Is.True); Assert.That(Effect.IsActive, Is.True);
            Assert.That(Effect.SequenceCount, Is.EqualTo(1)); Assert.That(Story.ApplyProgressionAction(), Is.False);
            Assert.That(Loop.CanRest, Is.False); Assert.That(Loop.Rest(), Is.False); Assert.That(Loop.RestoreDay(1), Is.False);
            Assert.That(Loop.PrepareZone("Zone04"), Is.False); Assert.That(Loop.TryPurchaseUpgrade(G10.Prototype.Navigation.ShipUpgrade.Speed), Is.False);
            Assert.That(ExpeditionSaveStore.TryRead(out var saved, out var error), Is.True, error);
            Assert.That(saved.current.zones.Find(z => z.zone == "Zone03").missionProgress.worldFlags, Does.Contain("ROCK_BARRIER_DESTROYED"));
            Assert.That(Effect.DeferTerrainReveal, Is.True); Assert.That(Cabin.Navigation.CanOccupy(Loop.ActiveMap.exitArea.mapPosition), Is.False);
            var preview = Cabin.NavigationPanel.GetComponentInChildren<HelmMapPreview>(true);
            preview.RefreshMap();
            Assert.That(preview.MapImage.texture, Is.SameAs(Loop.ActiveMap.map), "Preview must not reveal cleared terrain before the event does.");
            var firstPulse = Array.Find(Effect.productionSequence.cues, cue => cue.effect != null && cue.effect.shakePixels > 0);
            var laterPulse = Array.Find(Effect.productionSequence.cues, cue => cue.effect != null && cue.effect.shakePixels > 0 && cue.offset > firstPulse.offset);
            Assert.That(firstPulse, Is.Not.Null); Assert.That(laterPulse, Is.Not.Null);
            bool firstMoved = false, laterMoved = false, restedBetween = false;
            while (Effect.Elapsed < 1.7f && Time.realtimeSinceStartup - began < 4)
            {
                yield return new WaitForEndOfFrame();
                float time = Effect.Elapsed; var offset = motion.Offset;
                Assert.That(Mathf.Abs(offset.x), Is.LessThanOrEqualTo(8.001f));
                Assert.That(Mathf.Abs(offset.y), Is.LessThanOrEqualTo(8.001f));
                if (time >= firstPulse.offset && time < firstPulse.offset + firstPulse.duration + .05f)
                    firstMoved |= offset.sqrMagnitude > .000001f;
                if (time >= laterPulse.offset && time < laterPulse.offset + laterPulse.duration + .05f)
                    laterMoved |= offset.sqrMagnitude > .000001f;
                if (time > firstPulse.offset + firstPulse.duration + .05f && time < laterPulse.offset - .05f)
                    restedBetween |= !motion.IsShaking && offset.sqrMagnitude < .000001f;
            }
            Assert.That(firstMoved, Is.True, "The actual director-owned frame must move during the authored pressure pulse.");
            Assert.That(laterMoved, Is.True, "The authored fracture pulse must move the same frame again.");
            Assert.That(restedBetween, Is.True, "Discrete pulses must return to rest between impacts, not leave continuous shaking.");
            Assert.That(Effect.DeferTerrainReveal, Is.False); Assert.That(Cabin.Navigation.CanOccupy(Loop.ActiveMap.exitArea.mapPosition), Is.True);
            preview.RefreshMap(); Assert.That(preview.MapImage.texture, Is.SameAs(Loop.ActiveMap.alternateMap));
            ScreenCapture.CaptureScreenshot(CabinNavigationPlayModeTests.CapturePath("round3-rock-destruction.png"));
            while (Effect.IsActive && Time.realtimeSinceStartup - began < 12) yield return null;
            Assert.That(Time.realtimeSinceStartup - began, Is.InRange(9.8f, 11.5f));
            Assert.That(Effect.IsActive, Is.False); Assert.That(motion.transform.localPosition, Is.EqualTo(original));
            Assert.That(motion.IsShaking, Is.False); Assert.That(motion.Offset, Is.EqualTo(Vector3.zero));
            Assert.That(Cabin.Navigation.ExpeditionBlocked, Is.False); Assert.That(Loop.CanRest, Is.True);
            Assert.That(Map.RockCompleted, Is.True); Assert.That(Map.RockPrompt, Does.Contain("DESTROYED"));
            Assert.That(Story.ExportProgress().grantedRewards.FindAll(id => id == "rock-destroyed").Count, Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator NoShakePauseAndInterruptedContinueKeepPersistentRockWithoutReplaying()
        {
            ImpactShake.SetScreenShakeEnabled(false); Ready();
            Vector3 original = ProductionShake.transform.localPosition;
            Assert.That(Story.ApplyProgressionAction(), Is.True);
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(ProductionShake.Offset, Is.EqualTo(Vector3.zero)); Assert.That(ProductionShake.IsShaking, Is.False);
            Time.timeScale = 0; float elapsed = Effect.Elapsed;
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(Effect.Elapsed, Is.EqualTo(elapsed)); Time.timeScale = 1;
            Assert.That(Loop.SaveCurrent(), Is.True); flow.LoadMainMenu(); yield return Wait();
            flow.ContinueDebugGame(); yield return Wait();
            Assert.That(Loop.Zone, Is.EqualTo("Zone03")); Assert.That(Loop.CurrentProgress.rockDestroyed, Is.True);
            Assert.That(Loop.ActiveMap.UsesAlternate(Loop.MissionRuntime), Is.True);
            Assert.That(Effect.IsActive, Is.False); Assert.That(Effect.SequenceCount, Is.Zero);
            Cabin.OpenNavigation();
            Assert.That(Cabin.NavigationPanel.GetComponentInChildren<HelmMapPreview>(true).MapImage.texture,
                Is.SameAs(Loop.ActiveMap.alternateMap), "Continue retains cleared zone artwork without replaying the event.");
            Assert.That(ProductionShake.transform.localPosition, Is.EqualTo(original));
            Assert.That(ProductionShake.Offset, Is.EqualTo(Vector3.zero)); Assert.That(ProductionShake.IsShaking, Is.False);
            Assert.That(Cabin.Navigation.ExpeditionBlocked, Is.False);
            Assert.That(Story.ApplyProgressionAction(), Is.False);
            Assert.That(RockBreakerSequence.AmplitudeAt(0), Is.EqualTo(8));
            Assert.That(RockBreakerSequence.AmplitudeAt(.15f), Is.EqualTo(7));
            Assert.That(RockBreakerSequence.AmplitudeAt(.4f), Is.EqualTo(5));
            Assert.That(RockBreakerSequence.AmplitudeAt(.7f), Is.EqualTo(1.5f));
            Assert.That(RockBreakerSequence.AmplitudeAt(1), Is.Zero);
        }
        private IEnumerator Wait()
        {
            yield return null; float deadline = Time.realtimeSinceStartup + 25;
            while (flow.IsTransitioning && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(flow.IsTransitioning, Is.False); Assert.That(flow.LastError, Is.Null.Or.Empty); yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1; ImpactShake.SetScreenShakeEnabled(oldShake);
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            if (SceneFlowController.Instance != null) Object.Destroy(SceneFlowController.Instance.gameObject); yield return null;
            ExpeditionSaveStore.SelectNormalTimeline(); ExpeditionSaveStore.PathOverride = oldSave; PhotoCaptureService.ArchivePathOverride = oldPhotos;
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
    }
}
