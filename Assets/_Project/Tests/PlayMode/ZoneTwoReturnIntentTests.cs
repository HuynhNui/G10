using System;
using System.Collections;
using System.IO;
using G10.Prototype.Computer;
using G10.Prototype.Core;
using G10.Prototype.Navigation;
using G10.Prototype.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace G10.Prototype.Tests
{
    /// <summary>Functional gate regression, not a measured experience playthrough.</summary>
    public sealed class ZoneTwoReturnIntentTests
    {
        private string folder, oldSave, oldPhotos;
        private bool oldDebug;
        private SceneFlowController flow;
        private ExpeditionLoop Loop => Object.FindAnyObjectByType<ExpeditionLoop>();
        private CabinStationView Cabin => Object.FindAnyObjectByType<CabinStationView>();
        private ExpeditionProgression Route => Cabin.GetComponent<ExpeditionProgression>();
        private ZoneNavigation Nav => Cabin.Navigation;
        private static readonly Vector2 Interior = new(100, 850);

        [UnitySetUp] public IEnumerator Setup()
        {
            folder = Path.Combine(Application.temporaryCachePath, "ZoneTwoReturn-" + Guid.NewGuid().ToString("N"));
            oldSave = ExpeditionSaveStore.PathOverride;
            oldPhotos = PhotoCaptureService.ArchivePathOverride;
            oldDebug = ExpeditionSaveStore.IsDebugTimeline;
            ExpeditionSaveStore.SelectNormalTimeline();
            ExpeditionSaveStore.PathOverride = Path.Combine(folder, "timeline.json");
            PhotoCaptureService.ArchivePathOverride = Path.Combine(folder, "photos");
            TutorialTestSave.SeedReturningPlayer();
            if (SceneFlowController.Instance != null) { Object.Destroy(SceneFlowController.Instance.gameObject); yield return null; }
            yield return SceneManager.LoadSceneAsync("Bootstrap", LoadSceneMode.Single);
            yield return null;
            flow = SceneFlowController.Instance;
            Assert.That(flow, Is.Not.Null);
            yield return Wait();
            var menu = Object.FindAnyObjectByType<MainMenuDebugStart>();
            menu.Open(); menu.SelectZone(2); menu.StartRun();
            yield return Wait();
            Assert.That(Loop.Zone, Is.EqualTo("Zone02"));
            Assert.That(Loop.HasVisitedZone("Zone01"), Is.True);
            Assert.That(Loop.ActiveMap.entryPosition, Is.EqualTo(new Vector2(100, 900)));
            Assert.That(Loop.ActiveMap.returnArea.mapPosition, Is.EqualTo(new Vector2(100, 1000)));
            Assert.That(Loop.ActiveMap.returnArea.arrivalRadius, Is.EqualTo(75));
            Cabin.OpenNavigation();
            yield return null;
            Route.Evaluate();
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale = 1;
            PauseMenuController.Instance?.Resume();
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            if (SceneFlowController.Instance != null) Object.Destroy(SceneFlowController.Instance.gameObject);
            yield return null;
            ExpeditionSaveStore.PathOverride = oldSave;
            PhotoCaptureService.ArchivePathOverride = oldPhotos;
            if (oldDebug) ExpeditionSaveStore.SelectDebugTimeline();
            else ExpeditionSaveStore.SelectNormalTimeline();
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }

        [UnityTest] public IEnumerator OutboundTravelStaysInZoneAndPhysicalBacktrackingUsesAuthoredGate()
        {
            Assert.That(Nav.Position, Is.EqualTo(Loop.ActiveMap.entryPosition));
            Assert.That(Route.ReturnArmed, Is.True, "The authored entry is outside the return gate.");
            SailToInterior();
            Assert.That(Route.ReturnArmed, Is.True);
            int day = Loop.Day;
            int completed = Loop.MissionRuntime.CompletedCount;
            string previousObjectives = JsonUtility.ToJson(Loop.ReadZoneState("Zone01").missionProgress);
            EnterReturnGate();
            yield return Wait();
            Assert.That(Loop.Zone, Is.EqualTo("Zone01"));
            Assert.That(Loop.Day, Is.EqualTo(day));
            Assert.That(Loop.ReadZoneState("Zone02").missionProgress.completedObjectives.Count, Is.EqualTo(completed));
            Assert.That(JsonUtility.ToJson(Loop.MissionRuntime.ExportProgress()), Is.EqualTo(previousObjectives));
            Assert.That(Nav.Position, Is.EqualTo(Loop.ActiveMap.backwardArrivalPosition));
            Route.Evaluate();
            yield return null;
            Assert.That(flow.IsTransitioning, Is.False, "One intentional return must not loop.");
        }

        [UnityTest] public IEnumerator RestoreAndJournalInsideGateDoNotTurnSavedDistanceOrNudgesIntoArrival()
        {
            // Deliberate setup injection for persistence coverage; never an experience-playtest driver.
            Nav.RestoreVoyage(Loop.ActiveMap.returnArea.mapPosition, 90, Loop.ActiveMap.entryDepth, 500);
            yield return Reload();
            Route.Evaluate();
            Assert.That(Loop.ActiveMap.returnArea.Contains(Nav.Position), Is.True);
            Assert.That(Route.ReturnArmed, Is.False);
            Nav.Navigate(1, 0, 0, .2f); Nav.Brake(); Route.Evaluate();
            Nav.Navigate(0, 1, 0, .1f); Nav.StepDepth(1, .1f); Route.Evaluate();
            Assert.That(flow.IsTransitioning, Is.False);
            Assert.That(Route.ReturnArmed, Is.False);
            Nav.Brake();
            int checkpointDay = Loop.Day;
            Assert.That(Loop.Rest(), Is.True, Loop.LastError);
            Assert.That(Loop.RestoreDay(checkpointDay), Is.True, Loop.LastError);
            Route.Evaluate();
            Assert.That(Route.ReturnArmed, Is.False, "Journal inside is not a new boundary crossing.");
            Nav.Navigate(1, 0, 0, .2f); Nav.Brake(); Route.Evaluate();
            Assert.That(flow.IsTransitioning, Is.False);
            Assert.That(Route.ReturnArmed, Is.False);
            SailToInterior();
            Assert.That(Route.ReturnArmed, Is.True, "Actual travel to the interior still permits return.");
            EnterReturnGate();
            yield return Wait();
            Assert.That(Loop.Zone, Is.EqualTo("Zone01"));
        }

        [UnityTest] public IEnumerator ContinueReobservesEntryAndInteriorWithoutInventingAnArrival()
        {
            yield return Reload();
            Route.Evaluate();
            Assert.That(Route.ReturnArmed, Is.True, "Continue outside may observe an approach without travelling.");
            Assert.That(flow.IsTransitioning, Is.False);
            SailToInterior();
            Assert.That(Route.ReturnArmed, Is.True);
            Vector2 savedPosition = Nav.Position;
            yield return Reload();
            Route.Evaluate();
            Assert.That(Nav.Position, Is.EqualTo(savedPosition));
            Assert.That(Route.ReturnArmed, Is.True, "An observed saved interior approach remains usable.");
            Assert.That(flow.IsTransitioning, Is.False);
            EnterReturnGate();
            yield return Wait();
            Assert.That(Loop.Zone, Is.EqualTo("Zone01"));
        }

        private void Aim(Vector2 destination)
        {
            Nav.Brake();
            Vector2 delta = destination - Nav.Position;
            float angle = Mathf.DeltaAngle(Nav.Heading, Mathf.Atan2(delta.x, delta.y) * Mathf.Rad2Deg);
            Nav.Navigate(0, Mathf.Sign(angle), 0, Mathf.Abs(angle) / 40f);
            Route.Evaluate();
        }

        private void SailToInterior()
        {
            float distance = Nav.DistanceTravelled;
            float hull = Nav.Ship.Hull;
            Aim(Interior);
            Assert.That(Nav.CanOccupy(Interior), Is.True);
            for (int step = 0; step < 250 && Vector2.Distance(Nav.Position, Interior) > 2; step++)
            {
                Nav.Navigate(1, 0, 0, .1f);
                Route.Evaluate();
                Assert.That(flow.IsTransitioning, Is.False, "Travelling away from the authored gate must remain in Zone02.");
                Assert.That(Nav.Obstructed, Is.False, "Use the authored collision map, not an open-water replacement.");
            }
            Nav.Brake();
            Assert.That(Vector2.Distance(Nav.Position, Interior), Is.LessThanOrEqualTo(2));
            Assert.That(Nav.DistanceTravelled, Is.GreaterThan(distance + 45), "Use actual navigation to reach the interior.");
            Assert.That(Nav.Ship.Hull, Is.EqualTo(hull), "The route must preserve the authored collision map.");
            Assert.That(Loop.ActiveMap.returnArea.Contains(Nav.Position), Is.False);
            Assert.That(Loop.Zone, Is.EqualTo("Zone02"));
        }

        private void EnterReturnGate()
        {
            Aim(Loop.ActiveMap.returnArea.mapPosition);
            for (int step = 0; step < 100 && !flow.IsTransitioning; step++)
            {
                Nav.Navigate(1, 0, 0, .1f);
                Route.Evaluate();
                Assert.That(Nav.Obstructed, Is.False);
            }
            Assert.That(Loop.ActiveMap.returnArea.Contains(Nav.Position), Is.True);
            Assert.That(flow.IsTransitioning, Is.True, "Physical inbound entry must activate the original gate.");
        }

        private IEnumerator Reload()
        {
            Nav.Brake();
            Assert.That(Loop.SaveCurrent(), Is.True, Loop.LastError);
            flow.LoadMainMenu(); yield return Wait();
            flow.ContinueDebugGame(); yield return Wait();
            Assert.That(Loop.Zone, Is.EqualTo("Zone02"));
            Cabin.OpenNavigation(); yield return null;
        }

        private IEnumerator Wait()
        {
            yield return null;
            float deadline = Time.realtimeSinceStartup + 25;
            while (flow.IsTransitioning && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(flow.IsTransitioning, Is.False);
            Assert.That(flow.LastError, Is.Null.Or.Empty);
            yield return null;
        }
    }
}
