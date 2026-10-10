using System;
using System.Collections;
using System.IO;
using System.Linq;
using G10.Prototype.Atmosphere;
using G10.Prototype.Audio;
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
    public sealed class CabinFeedbackPlayModeTests
    {
        private string folder, previousSave, previousPhotos;
        private int shakeSetting;
        private SceneFlowController flow;
        private ExpeditionLoop Loop => Object.FindAnyObjectByType<ExpeditionLoop>();
        private CabinStationView Cabin => Object.FindAnyObjectByType<CabinStationView>();
        private ZoneNavigation Nav => Cabin.Navigation;
        private CabinFeedbackController Feedback => Cabin.GetComponent<CabinFeedbackController>();
        private ImpactShake Shake => Cabin.GetComponentInChildren<ImpactShake>(true);
        private CreatureInventory Inventory => Cabin.GetComponent<CreatureInventory>();
        private CreatureCatcher Catcher => Cabin.GetComponent<CreatureCatcher>();
        [UnitySetUp] public IEnumerator Setup()
        {
            previousSave = ExpeditionSaveStore.PathOverride; previousPhotos = PhotoCaptureService.ArchivePathOverride;
            folder = Path.Combine(Application.temporaryCachePath, "FeedbackTests-" + Guid.NewGuid().ToString("N"));
            ExpeditionSaveStore.PathOverride = Path.Combine(folder, "timeline.json"); PhotoCaptureService.ArchivePathOverride = Path.Combine(folder, "photos");
            shakeSetting = PlayerPrefs.GetInt(ImpactShake.PreferenceKey, -1); ImpactShake.SetScreenShakeEnabled(true);
            TutorialTestSave.SeedReturningPlayer();
            if (SceneFlowController.Instance != null) { Object.Destroy(SceneFlowController.Instance.gameObject); yield return null; }
            yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single); yield return null;
            flow = SceneFlowController.Instance; yield return WaitFlow(); flow.StartNewGame(); yield return WaitFlow();
            Assert.That(Feedback, Is.Not.Null);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1;
            PauseMenuController.Instance?.Resume();
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            if (flow != null) Object.Destroy(flow.gameObject); yield return null;
            if (shakeSetting < 0) PlayerPrefs.DeleteKey(ImpactShake.PreferenceKey); else PlayerPrefs.SetInt(ImpactShake.PreferenceKey, shakeSetting);
            PlayerPrefs.Save();
            ExpeditionSaveStore.PathOverride = previousSave; PhotoCaptureService.ArchivePathOverride = previousPhotos;
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
        private IEnumerator WaitFlow()
        {
            yield return null; float timeout = Time.realtimeSinceStartup + 15;
            while (flow.IsTransitioning && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.That(flow.IsTransitioning, Is.False); Assert.That(flow.LastError, Is.Null.Or.Empty);
        }
        private void Resources(float hull, float energy)
        { var state = Nav.Ship.Export(); state.hull = hull; state.energy = energy; Nav.Ship.Restore(state); Feedback.Reevaluate(); }
        private void Water()
        { Nav.ConfigureMapCoordinates(new Vector2(1200, 700), 50); var cells = new byte[120 * 70]; Array.Fill(cells, (byte)1); Nav.SetChart(cells, 120, 70); }
        private IEnumerator ForceZone(string zone)
        {
            flow.LoadMainMenu(); yield return WaitFlow();
            Assert.That(ExpeditionSaveStore.TryRead(out var save, out _), Is.True);
            save.current.zone = zone; save.current.zones.Clear(); save.current.zones.Add(new ExpeditionZoneState { zone = zone, deadline = 15 });
            save.current.ship.maximumDepth = 750; // Represents the pressure hull required before visiting later zones.
            save.dayStart = null; save.hasDayStart = false; ExpeditionSaveStore.Write(save, true);
            flow.ContinueGame(); yield return WaitFlow();
        }
        private void Capture(string poiId)
        {
            var survey = Catcher.survey; var poi = survey.FindPoi(poiId);
            Nav.RestoreVoyage(survey.ContactPosition(poi), 0, survey.DepthFor(poi), 0);
            Assert.That(Catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.Started));
            CaptureMinigamePlayModeTests.Win(Catcher.minigame);
            Assert.That(Catcher.LastResult, Is.EqualTo(CreatureCatcher.Result.Caught));
        }
        [UnityTest] public IEnumerator RatiosMoodScanAndCriticalOverlaysSurvivePanelsWithoutChargeTriggers()
        {
            var state = Nav.Ship.Export(); state.radar = state.photos = state.captures = 0; Nav.Ship.Restore(state);
            yield return null;
            Assert.That(Nav.Ship.LowResources, Is.True); Assert.That(Feedback.EffectiveMood, Is.EqualTo(CabinMood.Normal));
            Assert.That(Feedback.HullTargetAlpha, Is.Zero); Assert.That(Feedback.EnergyTargetAlpha, Is.Zero);
            Nav.Ship.Refill(); Cabin.Scan(); yield return null;
            Assert.That(Feedback.EffectiveMood, Is.EqualTo(CabinMood.ScanActive));
            Resources(51, 21); yield return null; Assert.That(Feedback.HullTargetAlpha, Is.Zero); Assert.That(Feedback.EnergyTargetAlpha, Is.Zero);
            Resources(25, 20); yield return null; Assert.That(Feedback.EffectiveMood, Is.EqualTo(CabinMood.Danger));
            Assert.That(Cabin.GetComponentInChildren<CabinAtmosphere>().EffectiveMood, Is.EqualTo(CabinMood.Danger));
            Assert.That(Feedback.HullTargetAlpha, Is.EqualTo(.15f)); Assert.That(Feedback.EnergyTargetAlpha, Is.EqualTo(.15f));
            Resources(8, 9); Cabin.OpenComputer(); yield return new WaitForSeconds(.55f);
            Assert.That(Feedback.HullOverlayAlpha, Is.GreaterThan(.23f)); Assert.That(Feedback.EnergyOverlayAlpha, Is.GreaterThan(.20f));
            Assert.That(AudioManager.Instance.FeedbackLoopPlaying, Is.True); Assert.That(Shake.IsShaking, Is.False);
            Cabin.OpenMap(); yield return null; Assert.That(Feedback.HullOverlayAlpha, Is.GreaterThan(.23f));
            Resources(100, 9); yield return null; Assert.That(Feedback.EffectiveMood, Is.EqualTo(CabinMood.LowPower));
            var overlay = Cabin.transform.Find("FeedbackOverlayCanvas").GetComponent<Canvas>();
            Assert.That(overlay.sortingOrder, Is.LessThan(32000)); Assert.That(overlay.sortingOrder, Is.GreaterThan(100));
            Assert.That(overlay.GetComponentsInChildren<UnityEngine.UI.Graphic>().All(x => !x.raycastTarget), Is.True);
        }
        [UnityTest] public IEnumerator WallImpactIsLatchedAndShakeAccessibilityKeepsAudioFlashAndHitboxes()
        {
            Water(); var cells = new byte[120 * 70]; for (int y = 0; y < 70; y++) for (int x = 0; x < 65; x++) cells[y * 120 + x] = 1;
            Nav.SetChart(cells, 120, 70); Nav.RestoreVoyage(new Vector2(600, 100), 90, 230, 0); Resources(30, 100);
            int events = 0; float speed = 0, damage = 0;
            Nav.TerrainImpact += (s, d) => { events++; speed = s; damage = d; };
            Nav.Step(1, 0, 4); Assert.That(events, Is.EqualTo(1)); Assert.That(damage, Is.EqualTo(speed)); Assert.That(Nav.Ship.Hull, Is.EqualTo(12));
            Assert.That(Feedback.ImpactReactionCount, Is.EqualTo(1)); Assert.That(Shake.IsShaking, Is.True);
            Assert.That(Feedback.HullOverlayAlpha, Is.EqualTo(.30f).Within(.001f));
            for (int i = 0; i < 20; i++) Nav.Step(1, 0, .1f);
            Assert.That(events, Is.EqualTo(1)); Nav.Step(-1, 0, 3); Nav.Ship.Refill();
            ImpactShake.SetScreenShakeEnabled(false); yield return null; Assert.That(Shake.IsShaking, Is.False); Assert.That(Shake.Offset, Is.EqualTo(Vector3.zero));
            Nav.Step(1, 0, 4); Assert.That(events, Is.EqualTo(2)); Assert.That(Feedback.ImpactReactionCount, Is.EqualTo(2)); Assert.That(Shake.IsShaking, Is.False);
            Assert.That(Shake.GetComponent<CameraBreathing>(), Is.Null); Assert.That(Shake.GetComponent<CabinBob>(), Is.Null);
            yield return new WaitForSeconds(.1f); Assert.That(Feedback.HullOverlayAlpha, Is.GreaterThan(0));
            Assert.That(Shake.transform.Find("NavigationPanel"), Is.SameAs(Cabin.NavigationPanel.transform));
            Nav.Step(-1, 0, 3); Resources(8, 100); Nav.Step(1, 0, 4);
            Assert.That(events, Is.EqualTo(3)); Assert.That(Nav.Ship.Hull, Is.Zero);
            Assert.That(Feedback.ImpactReactionCount, Is.EqualTo(3));
            yield return null;
            float timeout = Time.realtimeSinceStartup + 10;
            while (Loop.IsDeathInProgress && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.That(Loop.IsDeathInProgress, Is.False); Assert.That(Feedback.HasPendingScrape, Is.False);
            Assert.That(Shake.IsShaking, Is.False); Assert.That(Feedback.EffectiveMood, Is.EqualTo(CabinMood.Normal));
        }
        [UnityTest] public IEnumerator DepthClampIsEdgeTriggeredAndNeverDamagesOrSpendsEnergyAtClamp()
        {
            Water(); Nav.RestoreVoyage(new Vector2(600, 100), 0, Nav.Ship.MaximumDepth, 0);
            int events = 0; Nav.DepthLimitReached += () => events++;
            float hull = Nav.Ship.Hull, energy = Nav.Ship.Energy;
            for (int i = 0; i < 30; i++) Nav.Navigate(0, 0, 1, .1f);
            Assert.That(events, Is.EqualTo(1)); Assert.That(Nav.Ship.Hull, Is.EqualTo(hull)); Assert.That(Nav.Ship.Energy, Is.EqualTo(energy));
            Assert.That(Feedback.NotificationText, Is.EqualTo("DEPTH LIMIT"));
            Nav.Navigate(0, 0, 0, .1f); Nav.Navigate(0, 0, 1, .1f); Assert.That(events, Is.EqualTo(2));
            yield return null;
        }
        [UnityTest] public IEnumerator TurningDivingAndReverseExhaustPowerWithOneReactionPerCrossing()
        {
            Water();
            int expected = 0;
            foreach (int axis in new[] { 0, 1, 2 })
            {
                Resources(100, .05f); yield return null;
                Nav.RestoreVoyage(new Vector2(600, 100), 90, 230, 0);
                Nav.Navigate(axis == 2 ? -1 : 0, axis == 0 ? 1 : 0, axis == 1 ? 1 : 0, 1);
                yield return null; expected++;
                Assert.That(Nav.Ship.Energy, Is.Zero); Assert.That(Feedback.PowerDownReactionCount, Is.EqualTo(expected));
                yield return null; yield return null; Assert.That(Feedback.PowerDownReactionCount, Is.EqualTo(expected));
                Assert.That(Feedback.EnergyTargetAlpha, Is.EqualTo(.28f));
            }
        }
        [UnityTest] public IEnumerator EmptyEnergyOverridesOldDepthNotificationAtEveryDepth()
        {
            Water(); Nav.RestoreVoyage(new Vector2(600, 100), 0, Nav.Ship.MaximumDepth, 0);
            Nav.StepDepth(1, .1f);
            Assert.That(Feedback.NotificationText, Is.EqualTo("DEPTH LIMIT"));
            int events = 0; Nav.DepthLimitReached += () => events++;
            Resources(100, 0);
            foreach (float depth in new[] { 0f, 230f, Nav.Ship.MaximumDepth })
            {
                Nav.RestoreVoyage(Nav.Position, 0, depth, 0);
                Nav.StepDepth(-1, .1f); Nav.Navigate(0, 0, 1, .1f);
                yield return null;
                Assert.That(Feedback.NotificationText, Is.EqualTo("NO ENERGY"));
                Assert.That(Nav.Depth, Is.EqualTo(depth)); Assert.That(events, Is.Zero);
            }
        }
        [UnityTest] public IEnumerator RealFarmCaptureEscalatesOnlyOnSuccessAndWorksInFullStackInventory()
        {
            yield return ForceZone("Zone02"); string material = RegularShipUpgradeRules.TierOneMaterial, poi = "zone02-l3";
            Loop.MissionRuntime.RecordObjective(poi, MissionObjectiveType.Photograph, material);
            int completed = Loop.MissionRuntime.CompletedCount;
            Capture(poi); Assert.That(Loop.CapturesToday, Is.EqualTo(1)); Assert.That(Feedback.CaptureEscalationTier, Is.Zero);
            Assert.That(Catcher.LastCaptureWasRepeat, Is.False); Assert.That(Catcher.LastSuccessText, Does.Contain("+1 ITEM").And.Contain("CARGO: 1"));
            var stacks = Inventory.Items.ToList();
            foreach (var c in Loop.contentCatalog.Where(c => c != null && c.id != material).Take(11)) stacks.Add(new CreatureInventory.Item(c.id, c.displayName, c.Image));
            Inventory.RestoreItems(stacks); Assert.That(Inventory.IsFull, Is.True); Assert.That(Inventory.CanAdd("unknown-new"), Is.False);
            for (int count = 2; count <= 4; count++)
            {
                Capture(poi); Assert.That(Loop.CapturesToday, Is.EqualTo(count)); Assert.That(Feedback.CaptureEscalationTier, Is.EqualTo(count - 1));
                Assert.That(Inventory.GetCount(material), Is.EqualTo(count)); Assert.That(Inventory.Items.Count, Is.EqualTo(12));
                Assert.That(Catcher.LastCaptureWasRepeat, Is.True); Assert.That(Catcher.LastSuccessText, Does.Contain("+1 MATERIAL").And.Contain("CARGO: " + count));
            }
            Assert.That(Loop.MissionRuntime.CompletedCount, Is.EqualTo(completed + 1)); Assert.That(Feedback.HasPendingScrape, Is.True);
            float timeout = Time.realtimeSinceStartup + 4.5f; while (Feedback.ScrapeReactionCount == 0 && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.That(Feedback.ScrapeReactionCount, Is.EqualTo(1));
            Assert.That(Feedback.IsFlickering, Is.True);
            Capture(poi); Assert.That(Feedback.HasPendingScrape, Is.True);
            Assert.That(Catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.Started)); Catcher.minigame.Cancel(); Assert.That(Loop.CapturesToday, Is.EqualTo(5));
            Assert.That(flow.PresentRest(Loop), Is.True); yield return null;
            Assert.That(Feedback.HasPendingScrape, Is.False); Assert.That(Feedback.IsFlickering, Is.False); Assert.That(Shake.IsShaking, Is.False);
            yield return WaitFlow(); Assert.That(Loop.CapturesToday, Is.Zero);
        }
        [UnityTest] public IEnumerator CaptureFailureDoesNotAdvanceDailyFeedbackOrGrantMaterial()
        {
            yield return ForceZone("Zone03"); string material = RegularShipUpgradeRules.TierTwoMaterial, poi = "zone03-l1";
            Loop.MissionRuntime.RecordObjective(poi, MissionObjectiveType.Photograph, material);
            var survey = Catcher.survey; var target = survey.FindPoi(poi); Nav.RestoreVoyage(survey.ContactPosition(target), 0, survey.DepthFor(target), 0);
            var original = Catcher.minigame.profile; var profile = Object.Instantiate(original); profile.attemptDuration = .01f;
            Catcher.minigame.profile = profile;
            try
            {
                int charges = Nav.Ship.Captures;
                Assert.That(Catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.Started)); Catcher.minigame.Tick(3, 0);
                Assert.That(Catcher.LastResult, Is.EqualTo(CreatureCatcher.Result.Failed)); Assert.That(Nav.Ship.Captures, Is.EqualTo(charges));
                Assert.That(Loop.CapturesToday, Is.Zero); Assert.That(Feedback.CaptureEscalationTier, Is.Zero);
                Assert.That(Feedback.HasPendingScrape, Is.False); Assert.That(Inventory.GetCount(material), Is.Zero);
            }
            finally { Catcher.minigame.profile = original; Object.Destroy(profile); }
            yield return null;
        }
        [UnityTest] public IEnumerator DamagedUpgradesRestAndPauseResetOnlyPresentation()
        {
            Resources(26, 9); Inventory.TryAdd(RegularShipUpgradeRules.TierOneMaterial, "Material", null); Inventory.TryAdd(RegularShipUpgradeRules.TierOneMaterial, "Material", null);
            Assert.That(Loop.TryPurchaseUpgrade(ShipUpgrade.Hull), Is.True);
            Assert.That(Nav.Ship.Hull, Is.EqualTo(26)); Assert.That(Nav.Ship.HullCapacity, Is.EqualTo(120));
            Assert.That(Feedback.EffectiveMood, Is.EqualTo(CabinMood.Danger));
            Inventory.TryAdd(RegularShipUpgradeRules.TierOneMaterial, "Material", null); Inventory.TryAdd(RegularShipUpgradeRules.TierOneMaterial, "Material", null);
            Assert.That(Loop.TryPurchaseUpgrade(ShipUpgrade.Energy), Is.True); Assert.That(Nav.Ship.Energy, Is.EqualTo(9));
            yield return null; Assert.That(Feedback.EnergyTargetAlpha, Is.EqualTo(.22f));
            Shake.Shake(5, .28f); PauseMenuController.Instance.OpenPause(); yield return null;
            Assert.That(Feedback.TransientsSuppressed, Is.True); Assert.That(Shake.IsShaking, Is.False); Assert.That(Feedback.HasPendingScrape, Is.False);
            int count = Feedback.ScrapeReactionCount, creaks = Feedback.CreakReactionCount, warnings = Feedback.WarningReactionCount;
            float timer = Feedback.WarningSecondsLeft;
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(Feedback.ScrapeReactionCount, Is.EqualTo(count)); Assert.That(Feedback.CreakReactionCount, Is.EqualTo(creaks));
            Assert.That(Feedback.WarningReactionCount, Is.EqualTo(warnings)); Assert.That(Feedback.WarningSecondsLeft, Is.EqualTo(timer));
            PauseMenuController.Instance.Resume(); yield return null;
            Assert.That(flow.PresentRest(Loop), Is.True); yield return WaitFlow();
            yield return new WaitForSeconds(.6f);
            Assert.That(Feedback.HullOverlayAlpha, Is.LessThan(.01f)); Assert.That(Feedback.EnergyOverlayAlpha, Is.LessThan(.01f));
            Assert.That(Nav.Ship.Hull, Is.EqualTo(120)); Assert.That(Loop.CapturesToday, Is.Zero); Assert.That(Feedback.HasPendingScrape, Is.False);
        }
        [UnityTest] public IEnumerator ContinueJournalZoneRebindAndDeathDeriveFeedbackFromSnapshots()
        {
            Resources(20, 12); Assert.That(Loop.SaveCurrent(), Is.True);
            string json = File.ReadAllText(ExpeditionSaveStore.SavePath);
            Assert.That(json, Does.Not.Contain("feedback").And.Not.Contain("overlayAlpha").And.Not.Contain("scrape"));
            Assert.That(ExpeditionSaveStore.CurrentVersion, Is.EqualTo(5));
            flow.LoadMainMenu(); yield return WaitFlow(); flow.ContinueGame(); yield return WaitFlow();
            Assert.That(Nav.Ship.Hull, Is.EqualTo(20)); Assert.That(Nav.Ship.Energy, Is.EqualTo(12)); Assert.That(Feedback.EffectiveMood, Is.EqualTo(CabinMood.Danger));
            Assert.That(Loop.Rest(), Is.True); Assert.That(Loop.RestoreDay(1), Is.True); yield return null;
            Assert.That(Feedback.EffectiveMood, Is.EqualTo(CabinMood.Danger)); Assert.That(Feedback.HasPendingScrape, Is.False);
            // Rebind the shared cabin to another zone using a saved, authoritative ship state.
            flow.LoadMainMenu(); yield return WaitFlow(); Assert.That(ExpeditionSaveStore.TryRead(out var save, out _), Is.True);
            save.current.zone = "Zone03"; save.current.zones.Add(new ExpeditionZoneState { zone = "Zone03", deadline = 15 });
            save.dayStart = null; save.hasDayStart = false; ExpeditionSaveStore.Write(save, true);
            flow.ContinueGame(); yield return WaitFlow();
            Assert.That(Nav.Ship.Hull, Is.EqualTo(20)); Assert.That(Nav.Ship.Energy, Is.EqualTo(12)); Assert.That(Feedback.EffectiveMood, Is.EqualTo(CabinMood.Danger));
            Assert.That(Loop.Rest(), Is.True); Nav.Ship.HitTerrain(10000); yield return null;
            float timeout = Time.realtimeSinceStartup + 10; while (Loop.IsDeathInProgress && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.That(Loop.IsDeathInProgress, Is.False); Assert.That(Feedback.EffectiveMood, Is.EqualTo(CabinMood.Normal)); Assert.That(Feedback.HasPendingScrape, Is.False);
        }
        [UnityTest] public IEnumerator FinalRestRevealsLockedFailureDirectlyAndEndingStopsFeedback()
        {
            Assert.That(Loop.TotalDays, Is.EqualTo(25));
            for (int day = 2; day <= Loop.TotalDays; day++) Assert.That(Loop.Rest(), Is.True);
            Assert.That(flow.PresentRest(Loop), Is.True); yield return WaitFlow();
            Assert.That(flow.DayLeftPresentationText, Is.EqualTo("DAY LEFT: 0")); Assert.That(Loop.Failed, Is.True);
            Assert.That(Cabin.Panels.LockedPanel, Is.Not.Null); Assert.That(Cabin.Panels.IsPanelOpen, Is.True); Assert.That(Nav.ExpeditionBlocked, Is.True);
            Assert.That(Feedback.TransientsSuppressed, Is.True); Assert.That(Feedback.HasPendingScrape, Is.False);
            yield return SceneManager.LoadSceneAsync("Ending", LoadSceneMode.Single); yield return null;
            Assert.That(Object.FindAnyObjectByType<CabinFeedbackController>(), Is.Null); Assert.That(AudioManager.Instance.FeedbackLoopPlaying, Is.False);
        }
        [UnityTest] public IEnumerator ShakeSettingIsAvailableInSystemTray()
        {
            Cabin.OpenComputer(); yield return null;
            var button = Cabin.GetComponentsInChildren<UnityEngine.UI.Button>(true).Single(b => b.name == "ScreenShakeToggle");
            button.onClick.Invoke(); Assert.That(ImpactShake.ScreenShakeEnabled, Is.False);
            Shake.Shake(6, .2f); Assert.That(Shake.IsShaking, Is.False);
            button.onClick.Invoke(); Assert.That(ImpactShake.ScreenShakeEnabled, Is.True);
        }
    }
}
