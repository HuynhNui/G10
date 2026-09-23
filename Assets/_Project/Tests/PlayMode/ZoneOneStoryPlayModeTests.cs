using System;
using System.Collections;
using System.IO;
using G10.Prototype.Computer;
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
    public sealed class ZoneOneStoryPlayModeTests
    {
        private string folder;
        private CabinStationView cabin;
        private ZoneOneStory story;
        private ExpeditionLoop loop;
        [UnitySetUp] public IEnumerator Setup()
        {
            folder = Path.Combine(Application.temporaryCachePath, "ZoneOneStory-" + Guid.NewGuid().ToString("N"));
            ExpeditionSaveStore.PathOverride = Path.Combine(folder, "timeline.json");
            PhotoCaptureService.ArchivePathOverride = Path.Combine(folder, "photos");
            yield return Load();
        }
        private IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("GameplayCore", LoadSceneMode.Single);
            yield return SceneManager.LoadSceneAsync("Zone01", LoadSceneMode.Additive);
            yield return null; yield return null;
            cabin = Object.FindAnyObjectByType<CabinStationView>(); story = cabin.GetComponent<ZoneOneStory>();
            loop = Object.FindAnyObjectByType<ExpeditionLoop>();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            ExpeditionSaveStore.PathOverride = null; PhotoCaptureService.ArchivePathOverride = null;
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
        private void Arrive()
        {
            var poi = story.ActivePoi;
            cabin.Navigation.RestoreVoyage(poi.mapPosition + Vector2.down * 8, 0, story.survey.targetDepth, 0);
            cabin.Navigation.Ship.Refill();
        }
        private IEnumerator Scan()
        {
            cabin.OpenRadar(); cabin.Radar.StartSweep(); yield return null;
            yield return new WaitForSecondsRealtime(.08f);
            Assert.That(cabin.Radar.VisibleContactCount, Is.EqualTo(1), "North contact must reveal immediately, not after a complete two-second sweep.");
        }
        private void Photograph()
        {
            cabin.OpenCamera();
            var record = cabin.GetComponent<PhotoCaptureService>().Capture();
            Assert.That(record, Is.Not.Null);
            Assert.That(record.Result == "GoodPhoto" || record.Result == "LifeDetected", Is.True, record.Result);
        }
        [UnityTest] public IEnumerator RealActionsUnlockOrderedLocationsPersistAndInstallHullOnce()
        {
            Assert.That(story, Is.Not.Null); Assert.That(story.ActiveLocation, Is.Zero);
            Assert.That(story.Research(), Is.False); Assert.That(story.InstallHull(), Is.False);
            Arrive(); Photograph(); Assert.That(story.Has(ZoneOneStory.Progress.PhotoOne), Is.False, "Radar is required first.");
            yield return Scan();
            yield return new WaitForSecondsRealtime(.65f); Photograph();
            Assert.That(story.Has(ZoneOneStory.Progress.PhotoOne), Is.True);
            cabin.OpenComputer();
            var screen = cabin.GetComponentInChildren<ComputerScreenController>(true);
            screen.OpenMissionLog();
            var mission = screen.GetComponentInChildren<MissionLogView>();
            screen.OpenApp(ComputerAppId.Research);
            Assert.That(story.Research(), Is.True); Assert.That(story.ActiveLocation, Is.EqualTo(1));
            Assert.That(mission.DisplayedText, Does.Contain("RÃNH SAN HÔ CỔ"), "An already-open mission window must follow research progress.");
            yield return CabinNavigationPlayModeTests.CaptureArt(cabin, "story-research-analysis.png");
            Assert.That(story.LastMessage, Does.Contain("phân tán áp lực"));
            Arrive(); var catcher = cabin.GetComponent<CreatureCatcher>();
            Assert.That(catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.PhotoRequired));
            yield return Scan();
            cabin.OpenCapture(); Assert.That(catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.Caught));
            Assert.That(story.inventory.Contains(story.emmaTube.id), Is.True);
            Assert.That(catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.Empty));
            cabin.OpenComputer(); Assert.That(story.Research(), Is.True); Assert.That(story.ActiveLocation, Is.EqualTo(2));
            Assert.That(loop.SaveCurrent(), Is.True);
            yield return Load();
            Assert.That(story.ActiveLocation, Is.EqualTo(2)); Assert.That(story.inventory.Contains(story.emmaTube.id), Is.True);
            Assert.That(story.InstallHull(), Is.False);
            Arrive(); Photograph();
            Assert.That(story.Has(ZoneOneStory.Progress.PhotoTwo), Is.True);
            cabin.OpenCapture(); catcher = cabin.GetComponent<CreatureCatcher>();
            Assert.That(catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.Caught));
            Assert.That(story.survey.creaturePresent, Is.True, "Only adhesive is collected; the mollusk stays alive.");
            float oldHull = cabin.Navigation.Ship.HullCapacity;
            cabin.OpenComputer(); Assert.That(story.InstallHull(), Is.True);
            Assert.That(cabin.Navigation.Ship.HullCapacity, Is.EqualTo(oldHull + story.hullBonus));
            Assert.That(cabin.Navigation.Ship.MaximumDepth, Is.GreaterThanOrEqualTo(story.upgradedMaximumDepth));
            Assert.That(story.inventory.Contains(story.adhesive.id), Is.False);
            Assert.That(story.InstallHull(), Is.False); Assert.That(loop.RequiredObjectivesComplete, Is.True);
            Assert.That(loop.SaveCurrent(), Is.True); yield return Load();
            Assert.That(story.Complete, Is.True); Assert.That(loop.RequiredObjectivesComplete, Is.True);
            Assert.That(loop.PrepareZone("Zone02"), Is.True);
        }
        [UnityTest] public IEnumerator InventoryAndPresentationHaveUsableGeometry()
        {
            var bag = cabin.GetComponentInChildren<CreatureInventoryView>(true);
            Assert.That(bag.icons.Length, Is.EqualTo(10));
            for (int i = 0; i < 10; i++) Assert.That(story.inventory.TryAdd("test" + i, "Mẫu thử " + i, story.creatureOne.Image), Is.True);
            Assert.That(story.inventory.TryAdd("overflow", "overflow", null), Is.False);
            cabin.OpenCargo(); yield return null;
            yield return CabinNavigationPlayModeTests.CaptureArt(cabin, "story-inventory.png");
            cabin.OpenNavigation(); yield return null;
            var energy = cabin.NavigationPanel.GetComponentInChildren<ShipEnergyBar>();
            Assert.That(((RectTransform)energy.transform).anchorMax.y, Is.EqualTo(1));
            yield return CabinNavigationPlayModeTests.CaptureArt(cabin, "story-helm.png");
            cabin.OpenComputer(); var screen = cabin.GetComponentInChildren<ComputerScreenController>(true);
            screen.OpenUpgrade(); yield return null;
            yield return CabinNavigationPlayModeTests.CaptureArt(cabin, "story-upgrade.png");
            foreach (var entry in screen.GetComponentsInChildren<UpgradeEntryConfig>())
            {
                var label = entry.transform.Find("NameText").GetComponent<TMPro.TMP_Text>(); label.ForceMeshUpdate();
                Assert.That(label.isTextOverflowing, Is.False, entry.DisplayName);
            }
            screen.OpenApp(ComputerAppId.Research); yield return null;
            yield return CabinNavigationPlayModeTests.CaptureArt(cabin, "story-research.png");
        }
        [Test] public void SweepUsesBearingNotDistance()
        {
            Assert.That(RadarDisplay.SweepHasPassed(Vector2.up * 80, .01f), Is.True);
            Assert.That(RadarDisplay.SweepHasPassed(Vector2.right, .49f), Is.False);
            Assert.That(RadarDisplay.SweepHasPassed(Vector2.right * 80, .51f), Is.True);
            Assert.That(RadarDisplay.SweepHasPassed(Vector2.down, .99f), Is.False);
            Assert.That(RadarDisplay.SweepHasPassed(Vector2.down, 1.01f), Is.True);
            Assert.That(RadarDisplay.SweepHasPassed(Vector2.left, 1.49f), Is.False);
            Assert.That(RadarDisplay.SweepHasPassed(Vector2.left, 1.51f), Is.True);
        }
        [UnityTest] public IEnumerator FullBagAndCheckpointCannotDuplicateRewards()
        {
            story.Restore((int)(ZoneOneStory.Progress.RadarOne | ZoneOneStory.Progress.PhotoOne | ZoneOneStory.Progress.Analysis));
            Assert.That(loop.Rest(), Is.True, "The new starting site must also be a rest checkpoint.");
            Arrive(); yield return Scan(); cabin.OpenCapture();
            for (int i = 0; i < 10; i++) story.inventory.TryAdd("temporary" + i, "Temporary", story.creatureOne.Image);
            int charges = cabin.Navigation.Ship.Captures;
            Assert.That(cabin.GetComponent<CreatureCatcher>().TryCapture(), Is.EqualTo(CreatureCatcher.Result.Full));
            Assert.That(cabin.Navigation.Ship.Captures, Is.EqualTo(charges));
            Assert.That(story.Has(ZoneOneStory.Progress.Tube), Is.False);
            // Remove test cargo before saving; production saves only catalogued stable IDs.
            for (int i = 0; i < 10; i++) story.inventory.Remove("temporary" + i);
            Assert.That(cabin.GetComponent<CreatureCatcher>().TryCapture(), Is.EqualTo(CreatureCatcher.Result.Caught));
            cabin.OpenComputer(); Assert.That(story.Research(), Is.True);
            Assert.That(loop.RestoreDay(1), Is.True);
            Assert.That(story.ActiveLocation, Is.EqualTo(1));
            Assert.That(story.Has(ZoneOneStory.Progress.Recipe), Is.False);
            Assert.That(story.inventory.Contains(story.emmaTube.id), Is.False);
        }
        [UnityTest] public IEnumerator DragTracksPointerWithoutResizingContent()
        {
            cabin.OpenComputer(); var screen = cabin.GetComponentInChildren<ComputerScreenController>(true);
            screen.OpenUpgrade(); yield return null;
            var window = screen.GetComponentInChildren<SubmarineUpgradeUIController>().GetComponent<ComputerWindow>();
            var title = window.transform.Find("DragTitleBar").GetComponent<ComputerWindowHandle>();
            var press = RectTransformUtility.WorldToScreenPoint(null, title.transform.position);
            var data = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
            { button = UnityEngine.EventSystems.PointerEventData.InputButton.Left, pressPosition = press, position = press };
            var size = window.Rect.sizeDelta; var scale = window.transform.Find("ContentRoot").localScale;
            var before = window.Rect.anchoredPosition;
            title.OnInitializePotentialDrag(data); Assert.That(data.useDragThreshold, Is.False);
            title.OnBeginDrag(data); data.position = press + new Vector2(-70, -25); title.OnDrag(data);
            Assert.That(window.Rect.anchoredPosition.x, Is.LessThan(before.x));
            Assert.That(window.Rect.sizeDelta, Is.EqualTo(size));
            Assert.That(window.transform.Find("ContentRoot").localScale, Is.EqualTo(scale));
            window.ToggleMaximize(); Assert.That(window.Maximized, Is.True);
            title.OnBeginDrag(data); Assert.That(window.Maximized, Is.False);
            Assert.That(window.Rect.anchoredPosition.x, Is.GreaterThanOrEqualTo(0));
        }
        [UnityTest] public IEnumerator MapShowsOnlyLocalObjectivesWhileHoveringEachSite()
        {
            var world = cabin.GetComponent<WorldMapController>();
            world.OpenWorld(); world.OpenZone(0); yield return null;
            var map = world.zone01Overlay;
            Assert.That(map.survey.Story, Is.SameAs(story), "The displayed map must use the live story owner.");
            yield return CabinNavigationPlayModeTests.CaptureArt(cabin, "map-hover-open.png");
            // CaptureArt temporarily uses a WorldSpace Canvas; allow the overlay scaler to restore screen coordinates.
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.That(map.taskReadout.transform.parent.gameObject.activeSelf, Is.False, "Opening Zone01 must not show tasks until a site is hovered.");
            var chart = map.GetComponentInParent<CabinPointerTarget>();
            var chartRect = (RectTransform)chart.transform;
            var pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
                { button = UnityEngine.EventSystems.PointerEventData.InputButton.Left };
            string[] titles = { "01 • RẠN TẢO ĐỎ", "02 • RÃNH SAN HÔ CỔ", "03 • THỀM BIỂN SÂU" };
            string[] objectives = {
                "[ ] Bật Radar\n[ ] Chụp Sinh vật 001\n",
                "[ ] Bật Radar\n[ ] Dùng nút THU THẬP để lấy vật phẩm\n",
                "[ ] Chụp Sinh vật 002\n"
            };
            for (int order = 0; order < 3; order++)
            {
                int index = Array.FindIndex(map.Locations, poi => poi.id == story.poiIds[order]);
                // Hit the visible marker's edge, not just its smaller gameplay arrival circle.
                Vector2 uv = ZoneNavigation.CoordinatesToUV(map.Locations[index].mapPosition + new Vector2(23, 23));
                Vector2 local = chartRect.rect.min + Vector2.Scale(uv, chartRect.rect.size);
                pointer.position = RectTransformUtility.WorldToScreenPoint(null, chartRect.TransformPoint(local));
                var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
                UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointer, hits);
                Assert.That(hits.Exists(hit => hit.gameObject == chart.gameObject), Is.True,
                    $"Visible site {order + 1} at {pointer.position} must hit {chart.name}. Hits: " +
                    string.Join(", ", hits.ConvertAll(hit => hit.gameObject.name)));
                chart.OnPointerMove(pointer);
                Assert.That(map.taskReadout.transform.parent.gameObject.activeInHierarchy, Is.True, "Hover alone must show the site's tasks, without clicking.");
                Assert.That(map.taskReadout.text, Is.EqualTo(titles[order] + "\n" + objectives[order]));
                chart.OnPointerClick(pointer);
                Assert.That(map.SelectedLocation, Is.EqualTo(index));
                Assert.That(map.taskReadout.text, Is.EqualTo(story.MapLocationText(order)));
                yield return CabinNavigationPlayModeTests.CaptureArt(cabin, $"map-hover-site-{order + 1}.png");
                Assert.That(map.taskReadout.preferredHeight, Is.LessThanOrEqualTo(map.taskReadout.rectTransform.rect.height + 1), "Mission text must fit its panel.");
                chart.OnPointerExit(pointer);
                Assert.That(map.taskReadout.transform.parent.gameObject.activeSelf, Is.False, "Clicking a site must not pin its tooltip after the pointer leaves.");
                map.SetPointer(new Vector2(.99f, .99f));
                Assert.That(map.taskReadout.transform.parent.gameObject.activeSelf, Is.False, "Hovering empty chart space must not show tasks.");
                // Close while hovering to verify that reopening also clears a previously visible tooltip.
                map.SetPointer(uv);
                world.OpenWorld(); world.ResumeZone(); yield return null;
                Assert.That(map.SelectedLocation, Is.EqualTo(index));
                Assert.That(map.taskReadout.transform.parent.gameObject.activeSelf, Is.False, "Reopening must not display remembered tasks without a new hover.");
            }
            var thirdSite = Array.Find(map.Locations, poi => poi.id == story.poiIds[2]);
            map.SetPointer(ZoneNavigation.CoordinatesToUV(thirdSite.mapPosition));
            story.Restore(511); yield return null;
            Assert.That(map.taskReadout.text, Is.EqualTo(story.MapLocationText(2)), "Progress must update without moving the pointer.");
            // A checkpoint can replace flags without changing the number of completed steps.
            story.Restore((int)ZoneOneStory.Progress.PhotoTwo); yield return null;
            Assert.That(map.taskReadout.text, Does.Contain("[x] Chụp Sinh vật 002"));
            story.Restore((int)ZoneOneStory.Progress.Adhesive); yield return null;
            Assert.That(map.taskReadout.text, Does.Not.Contain("dịch kết dính"));
            Assert.That(map.taskReadout.text, Does.Contain("[ ] Chụp Sinh vật 002"));
            Assert.That(story.LocationText(0), Does.Contain("Phân tích tại RESEARCH"));
            Assert.That(story.LocationText(1), Does.Contain("Nạp bản vẽ E.A"));
            Assert.That(story.LocationText(2), Does.Contain("[x] Thu mẫu dịch kết dính"));
            Assert.That(story.LocationText(2), Does.Contain("Chế tạo/lắp vỏ Tầng 1"));
        }
    }
}
