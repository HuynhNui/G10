using System;
using System.Collections;
using System.IO;
using G10.Prototype.Computer;
using G10.Prototype.Core;
using G10.Prototype.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace G10.Prototype.Tests
{
    public sealed class MissionLogScrollPlayModeTests
    {
        private string folder, oldSave, oldPhotos;
        private MissionDefinition definition;
        [UnitySetUp] public IEnumerator Setup()
        {
            folder = Path.Combine(Application.temporaryCachePath, "MissionScroll-" + Guid.NewGuid().ToString("N"));
            oldSave = ExpeditionSaveStore.PathOverride; oldPhotos = PhotoCaptureService.ArchivePathOverride;
            ExpeditionSaveStore.PathOverride = Path.Combine(folder, "timeline.json");
            PhotoCaptureService.ArchivePathOverride = Path.Combine(folder, "photos");
            TutorialTestSave.SeedReturningPlayer();
            if (SceneFlowController.Instance != null) { Object.Destroy(SceneFlowController.Instance.gameObject); yield return null; }
            yield return SceneManager.LoadSceneAsync("GameplayCore", LoadSceneMode.Single);
            yield return SceneManager.LoadSceneAsync("Zone01", LoadSceneMode.Additive);
            yield return null; yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (definition != null) Object.Destroy(definition);
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            if (SceneFlowController.Instance != null) Object.Destroy(SceneFlowController.Instance.gameObject);
            yield return null;
            ExpeditionSaveStore.PathOverride = oldSave; PhotoCaptureService.ArchivePathOverride = oldPhotos;
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
        [UnityTest] public IEnumerator LongTextClipsScrollsPreservesRefreshAndResetsOnReopen()
        {
            var cabin = Object.FindAnyObjectByType<CabinStationView>();
            cabin.OpenComputer();
            var screen = cabin.GetComponentInChildren<ComputerScreenController>(true);
            screen.OpenMissionLog(); yield return null;
            var view = screen.GetComponentInChildren<MissionLogView>(true);
            view.Expedition = null;
            definition = ScriptableObject.CreateInstance<MissionDefinition>();
            definition.objective = "Long mission fixture";
            definition.steps = new MissionStep[60];
            for (int i = 0; i < definition.steps.Length; i++)
                definition.steps[i] = new MissionStep { description = "Location " + i + " — survey and collect samples at the authored target depth." };
            view.Bind(new Provider(definition)); yield return null;
            Assert.That(view.GetComponentsInChildren<ScrollRect>(true).Length, Is.EqualTo(1));
            Assert.That(view.Scroll.vertical, Is.True); Assert.That(view.Scroll.horizontal, Is.False);
            Assert.That(view.Scroll.movementType, Is.EqualTo(ScrollRect.MovementType.Clamped));
            Assert.That(view.Viewport.GetComponent<RectMask2D>(), Is.Not.Null);
            Assert.That(view.Body.transform.parent, Is.SameAs(view.Viewport));
            Assert.That(view.Body.GetComponent<ContentSizeFitter>().verticalFit, Is.EqualTo(ContentSizeFitter.FitMode.PreferredSize));
            Assert.That(view.Body.rectTransform.rect.height, Is.GreaterThan(view.Viewport.rect.height));
            Assert.That(view.Scroll.verticalNormalizedPosition, Is.EqualTo(1).Within(.01));
            var root = view.Scroll.transform.parent;
            var route = screen.GetComponent<ExpeditionComputerView>().RouteText;
            var next = root.Find("NextExpeditionZone");
            // Current production routes are XY-only exits. Check the optional legacy action upgrade too.
            if (next == null)
            {
                next = new GameObject("NextExpeditionZone", typeof(RectTransform), typeof(Image), typeof(Button)).transform;
                next.SetParent(root, false);
                view.EnsureScrollLayout(root); view.Refresh();
            }
            Assert.That(route.transform.parent, Is.SameAs(root));
            Assert.That(next, Is.Not.Null); Assert.That(next.IsChildOf(view.Scroll.content), Is.False);
            Assert.That(route.transform.IsChildOf(view.Scroll.content), Is.False);
            Vector3 routePosition = route.transform.localPosition;
            view.Scroll.verticalNormalizedPosition = .3f; yield return null;
            Assert.That(view.Scroll.verticalNormalizedPosition, Is.EqualTo(.3f).Within(.01));
            definition.steps[0].state = MissionState.Completed; view.Refresh(); yield return null;
            Assert.That(view.Scroll.verticalNormalizedPosition, Is.EqualTo(.3f).Within(.01));
            Assert.That(route.transform.localPosition, Is.EqualTo(routePosition));
            var window = view.GetComponent<ComputerWindow>();
            window.SetBounds(new Vector2(100, -70), ComputerWindow.MinimumSize); yield return null;
            window.ToggleMaximize(); yield return null; window.ToggleMaximize(); yield return null;
            Assert.That(view.Viewport.rect.height, Is.EqualTo(510).Within(.01));
            Assert.That(route.transform.localPosition, Is.EqualTo(routePosition));
            Assert.That(root.localScale.x, Is.EqualTo(root.localScale.y));
            Assert.That(view.Body.GetComponent<ComputerDesktopText>().RenderedText.maskable, Is.True);
            Assert.That(view.Body.GetComponent<ComputerDesktopText>().RenderedText.canvasRenderer.hasRectClipping, Is.True);
            screen.CloseApp(ComputerAppId.MissionLog); screen.OpenMissionLog(); yield return null;
            Assert.That(view.Scroll.verticalNormalizedPosition, Is.EqualTo(1).Within(.01));
            view.EnsureScrollLayout(root); view.EnsureScrollLayout(root); view.Refresh();
            Assert.That(view.GetComponentsInChildren<ScrollRect>(true).Length, Is.EqualTo(1));
            Assert.That(view.GetComponentsInChildren<RectMask2D>(true).Length, Is.EqualTo(1));
            Assert.That(view.GetComponentsInChildren<ContentSizeFitter>(true).Length, Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator RealZoneListsGrowAndStoryChangedPreservesScroll()
        {
            var cabin = Object.FindAnyObjectByType<CabinStationView>();
            var session = cabin.GetComponent<CabinZoneSession>();
            var story = cabin.GetComponent<G10.Prototype.Missions.ZoneOneStory>();
            Assert.That(session.Configure("Zone03"), Is.True);
            cabin.OpenComputer();
            var screen = cabin.GetComponentInChildren<ComputerScreenController>(true);
            screen.OpenMissionLog(); yield return null;
            var view = screen.GetComponentInChildren<MissionLogView>(true);
            view.Refresh(); yield return null;
            Assert.That(view.Body.rectTransform.rect.height, Is.GreaterThan(view.Viewport.rect.height));
            Assert.That(view.DisplayedText, Does.Contain("TARGET DEPTH: 640 M"));
            view.Scroll.verticalNormalizedPosition = .4f;
            var location = story.config.locations[0];
            var objective = location.objectives[0];
            Assert.That(story.RecordObjective(location.poiId, objective.type, objective.targetId), Is.True);
            yield return null;
            Assert.That(view.Scroll.verticalNormalizedPosition, Is.EqualTo(.4f).Within(.01));
            Assert.That(session.Configure("Zone04"), Is.True);
            story.HiddenRouteAvailable = true;
            foreach (var hidden in story.config.locations)
                if (hidden.visibility == G10.Prototype.Missions.LocationVisibility.HiddenRadar) story.RevealPoi(hidden.poiId);
            cabin.OpenComputer(); screen.OpenMissionLog();
            view.Refresh(); yield return null;
            Assert.That(view.Body.rectTransform.rect.height, Is.GreaterThan(view.Viewport.rect.height));
            Assert.That(view.DisplayedText, Does.Contain("TARGET DEPTH: 735 M"));
        }
        private sealed class Provider : IMissionProvider
        {
            public string ZoneName => "SCROLL FIXTURE";
            public MissionDefinition CurrentMission { get; }
            public Provider(MissionDefinition mission) => CurrentMission = mission;
        }
    }
}
