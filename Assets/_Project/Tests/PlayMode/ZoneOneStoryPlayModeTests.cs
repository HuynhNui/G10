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

        [UnitySetUp]
        public IEnumerator Setup()
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
            cabin = Object.FindAnyObjectByType<CabinStationView>();
            story = cabin.GetComponent<ZoneOneStory>();
            loop = Object.FindAnyObjectByType<ExpeditionLoop>();
            Assert.That(story, Is.Not.Null);
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            ExpeditionSaveStore.PathOverride = null;
            PhotoCaptureService.ArchivePathOverride = null;
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }

        [Test]
        public void AllThreeLocationsExistAndAreVisibleFromTheStart()
        {
            Assert.That(story.config.locations, Has.Length.EqualTo(3));
            foreach (var location in story.config.locations)
            {
                Assert.That(location.visibility, Is.EqualTo(LocationVisibility.Visible));
                Assert.That(story.IsLocationRevealed(location.id), Is.True);
                Assert.That(story.survey.FindPoi(location.poiId), Is.Not.Null, location.id);
            }
            Assert.That(story.survey.FindPoiContaining(story.survey.FindPoi("zone01-north").mapPosition).id, Is.EqualTo("zone01-north"));
        }

        [Test]
        public void ZoneOneObjectivesCanCompleteInAnyOrder()
        {
            Assert.That(story.RecordObjective("zone01-north", MissionObjectiveType.Photograph, "Z1_Creature_02"), Is.True);
            Assert.That(story.HasObjective(ZoneOneStory.PhotoTwoObjective), Is.True);
            Assert.That(story.HasRecipe(ZoneOneStory.PressureHullRecipe), Is.True);
            Assert.That(story.RecordObjective("zone01-left", MissionObjectiveType.Photograph, "Z1_Creature_01"), Is.True);
            Assert.That(story.HasResearch(ZoneOneStory.PressureData), Is.True);
            Assert.That(story.RecordObjective("zone01-east", MissionObjectiveType.Collect, ZoneOneStory.EmmaBlueprint), Is.True);
            Assert.That(story.HasItem(ZoneOneStory.EmmaBlueprint), Is.True);
            Assert.That(story.AreRequiredLocationsComplete(), Is.True);
        }

        [Test]
        public void LegacyBitmaskMigratesToStableIdsAndIgnoresRemovedAdhesiveBit()
        {
            story.Restore((int)(ZoneOneStory.Progress.PhotoOne | ZoneOneStory.Progress.Analysis |
                ZoneOneStory.Progress.Tube | ZoneOneStory.Progress.PhotoTwo) | 128);
            Assert.That(story.HasObjective(ZoneOneStory.PhotoOneObjective), Is.True);
            Assert.That(story.HasResearch(ZoneOneStory.PressureData), Is.True);
            Assert.That(story.HasObjective(ZoneOneStory.BlueprintObjective), Is.True);
            Assert.That(story.HasItem(ZoneOneStory.EmmaBlueprint), Is.True);
            Assert.That(story.HasObjective(ZoneOneStory.PhotoTwoObjective), Is.True);
            Assert.That(story.ProgressState.collectedItems, Does.Not.Contain("Adhesive02"));
            Assert.That(story.inventory.Contains("Adhesive02"), Is.False);
        }

        [UnityTest]
        public IEnumerator StableIdProgressPersistsWithoutLocationIndexState()
        {
            story.RecordObjective("zone01-north", MissionObjectiveType.Photograph, "Z1_Creature_02");
            story.RecordObjective("zone01-left", MissionObjectiveType.Photograph, "Z1_Creature_01");
            Assert.That(loop.SaveCurrent(), Is.True);
            Assert.That(ExpeditionSaveStore.TryRead(out var saved, out _), Is.True);
            var zone = saved.current.zones.Find(value => value.zone == "Zone01");
            Assert.That(zone.missionProgress.completedObjectives, Does.Contain(ZoneOneStory.PhotoTwoObjective));
            Assert.That(zone.missionProgress.completedObjectives, Does.Contain(ZoneOneStory.PhotoOneObjective));
            yield return Load();
            Assert.That(story.HasObjective(ZoneOneStory.PhotoTwoObjective), Is.True);
            Assert.That(story.HasObjective(ZoneOneStory.PhotoOneObjective), Is.True);
            Assert.That(story.HasObjective(ZoneOneStory.BlueprintObjective), Is.False);
        }

        [Test]
        public void PressureHullRequiresFieldworkButConsumesNoAdhesive()
        {
            story.RecordObjective("zone01-north", MissionObjectiveType.Photograph, "Z1_Creature_02");
            story.RecordObjective("zone01-left", MissionObjectiveType.Photograph, "Z1_Creature_01");
            story.RecordObjective("zone01-east", MissionObjectiveType.Collect, ZoneOneStory.EmmaBlueprint);
            float oldHull = cabin.Navigation.Ship.HullCapacity;
            Assert.That(story.CanInstall, Is.True);
            Assert.That(story.InstallHull(), Is.True);
            Assert.That(cabin.Navigation.Ship.HullCapacity, Is.EqualTo(oldHull + story.hullBonus));
            Assert.That(story.HasZone("Zone02"), Is.True);
            Assert.That(loop.PrepareZone("Zone02"), Is.True);
            Assert.That(story.inventory.Contains("Adhesive02"), Is.False);
            Assert.That(story.InstallHull(), Is.False);
        }

        [Test]
        public void HiddenLocationsExistBeforeRevealAndEndingsRemainIndependent()
        {
            var gameObject = new GameObject("Zone04MissionTest");
            var runtime = gameObject.AddComponent<ZoneMissionRuntime>();
            var config = ScriptableObject.CreateInstance<ZoneMissionConfig>();
            config.zoneId = "Zone04";
            config.locations = new[]
            {
                Location("Z4_L1", "p1", LocationVisibility.Visible, "Z4_L1_PHOTO", MissionObjectiveType.Photograph, "Z4_Creature_01"),
                Location("Z4_L2", "p2", LocationVisibility.Visible, "Z4_L2_PHOTO", MissionObjectiveType.Photograph, "Z4_Creature_02"),
                Location("Z4_L3", "p3", LocationVisibility.HiddenRadar, "Z4_L3_PHOTO", MissionObjectiveType.Photograph, "Z4_Creature_03"),
                Location("Z4_L4", "p4", LocationVisibility.HiddenRadar, "Z4_L4_PHOTO", MissionObjectiveType.Photograph, "Z4_Creature_04"),
                Location("Z4_L5", "p5", LocationVisibility.HiddenRadar, "Z4_L5_COLLECT", MissionObjectiveType.Collect, "Z4_ITEM_HIDDEN_01")
            };
            config.completions = new[]
            {
                Completion("normal", new[] { "Z4_L1_PHOTO", "Z4_L2_PHOTO" }, "NORMAL_ENDING"),
                Completion("hidden", new[] { "Z4_L3_PHOTO", "Z4_L4_PHOTO", "Z4_L5_COLLECT" }, "HIDDEN_ENDING")
            };
            runtime.config = config;
            runtime.RestoreProgress(new MissionProgressState());
            Assert.That(runtime.LocationForPoi("p3"), Is.Not.Null, "Hidden means undiscovered, not absent or locked.");
            Assert.That(runtime.IsPoiVisible("p3"), Is.False);
            Assert.That(runtime.RevealPoi("p3"), Is.True);
            Assert.That(runtime.IsPoiVisible("p3"), Is.True);
            runtime.RecordObjective("p1", MissionObjectiveType.Photograph, "Z4_Creature_01");
            runtime.RecordObjective("p2", MissionObjectiveType.Photograph, "Z4_Creature_02");
            Assert.That(runtime.HasEnding("NORMAL_ENDING"), Is.True);
            Assert.That(runtime.HasEnding("HIDDEN_ENDING"), Is.False);
            Object.DestroyImmediate(gameObject); Object.DestroyImmediate(config);
        }

        [Test]
        public void SweepUsesBearingNotDistance()
        {
            Assert.That(RadarDisplay.SweepHasPassed(Vector2.up * 80, .01f), Is.True);
            Assert.That(RadarDisplay.SweepHasPassed(Vector2.right, .49f), Is.False);
            Assert.That(RadarDisplay.SweepHasPassed(Vector2.right * 80, .51f), Is.True);
        }

        [Test]
        public void CaptureGuideRadiusRemainsVisibleOutsideEveryContact()
        {
            var catcher = cabin.GetComponent<CreatureCatcher>();
            PhotoSurveyPlayModeTests.PlaceShip(cabin.Navigation, Vector2.zero);
            Assert.That(story.survey.FindContactContaining(cabin.Navigation.Position), Is.Null);
            Assert.That(cabin.Radar.CaptureGuideRadius, Is.EqualTo(catcher.DefaultCaptureRadius));
            Assert.That(catcher.captureRadius, Is.GreaterThan(0f));
        }

        [UnityTest]
        public IEnumerator ValidL3PhotoOutsideCaptureRangeCompletesObjectiveAndRestoresMapCheck()
        {
            var poi = story.survey.FindPoi("zone01-north");
            var capture = cabin.GetComponent<PhotoCaptureService>();
            var catcher = cabin.GetComponent<CreatureCatcher>();
            Assert.That(poi, Is.Not.Null);
            Assert.That(story.survey.IsRadarContactPresent(poi), Is.True);
            Assert.That(PlaceForPhotoOutsideCaptureRange(poi, capture), Is.True,
                "Expected an unobstructed camera position outside the authored arrival radius.");
            Assert.That(story.survey.FindContactContaining(cabin.Navigation.Position), Is.Null);

            cabin.Navigation.Ship.Refill();
            var record = capture.Capture();
            Assert.That(record, Is.Not.Null);
            Assert.That(record.Result, Is.EqualTo(PhotoResultType.GoodPhoto.ToString())
                .Or.EqualTo(PhotoResultType.LifeDetected.ToString()));
            Assert.That(story.HasObjective(ZoneOneStory.PhotoTwoObjective), Is.True);
            Assert.That(story.HasObjective(ZoneOneStory.PhotoOneObjective), Is.False);
            Assert.That(story.HasObjective(ZoneOneStory.BlueprintObjective), Is.False);
            Assert.That(catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.Empty),
                "Photography range must not weaken grab/capture validation.");

            cabin.OpenMap();
            cabin.GetComponent<WorldMapController>().OpenZone(0);
            yield return null;
            yield return null;
            AssertOnlyL3CheckVisible();

            Assert.That(loop.SaveCurrent(), Is.True);
            yield return Load();
            cabin.OpenMap();
            cabin.GetComponent<WorldMapController>().OpenZone(0);
            yield return null;
            yield return null;
            Assert.That(story.HasObjective(ZoneOneStory.PhotoTwoObjective), Is.True);
            AssertOnlyL3CheckVisible();
        }

        private bool PlaceForPhotoOutsideCaptureRange(MapPoi poi, PhotoCaptureService capture)
        {
            Vector2 contact = story.survey.ContactPosition(poi);
            float maximum = Mathf.Max(poi.arrivalRadius + 1f, capture.profile.visibleDistance - 2f);
            for (float distance = poi.arrivalRadius + 4f; distance <= maximum; distance += 4f)
            for (int direction = 0; direction < 16; direction++)
            {
                float angle = direction * Mathf.PI * 2f / 16f;
                Vector2 offset = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * distance;
                Vector2 position = contact - offset;
                if (!cabin.Navigation.CanOccupy(position)) continue;
                float heading = Mathf.Atan2(offset.x, offset.y) * Mathf.Rad2Deg;
                cabin.Navigation.RestoreVoyage(position, heading, story.survey.targetDepth, cabin.Navigation.DistanceTravelled);
                if (story.survey.FindContactContaining(position) != null) continue;
                if (story.survey.TryGetPhotoContact(cabin.Navigation, capture.profile.visibleDistance, capture.profile.fieldOfView,
                    out var resolved, out _) && resolved == poi) return true;
            }
            return false;
        }

        private void AssertOnlyL3CheckVisible()
        {
            var overlay = cabin.MapPanel.transform.Find("SquareChartContent/PhotoSurveyOverlay").GetComponent<PhotoSurveyMap>();
            Assert.That(overlay.gameObject.activeInHierarchy, Is.True,
                $"Expected the authored Zone01 overlay to be active. MapPanel={cabin.MapPanel.name}, active={cabin.MapPanel.activeInHierarchy}.");
            Assert.That(overlay.completionIcons, Has.Length.EqualTo(3));
            int l3Index = System.Array.FindIndex(overlay.Locations, poi => poi != null && poi.id == "zone01-north");
            Assert.That(l3Index, Is.GreaterThanOrEqualTo(0));
            for (int i = 0; i < overlay.completionIcons.Length; i++)
                Assert.That(overlay.completionIcons[i].enabled, Is.EqualTo(i == l3Index),
                    $"Completion icon {i} should follow stable POI id, not display-number ordering.");
            Assert.That(overlay.completionIcons[l3Index].texture.name, Is.EqualTo("check"));
        }

        private static MissionLocationConfig Location(string id, string poi, LocationVisibility visibility, string objectiveId, MissionObjectiveType type, string target)
            => new() { id=id, displayName=id, poiId=poi, visibility=visibility,
                objectives=new[] { new MissionObjectiveConfig { id=objectiveId, type=type, targetId=target, required=true } } };
        private static MissionCompletionConfig Completion(string id, string[] requirements, string ending)
            => new() { id=id, requiredObjectiveIds=requirements,
                rewards=new[] { new MissionRewardConfig { type=MissionRewardType.Ending, targetId=ending } } };
    }
}
