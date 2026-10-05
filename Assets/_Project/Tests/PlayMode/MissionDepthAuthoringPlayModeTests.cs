using System;
using System.Collections;
using System.IO;
using System.Reflection;
using G10.Prototype.Computer;
using G10.Prototype.Core;
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
    public sealed class MissionDepthAuthoringPlayModeTests
    {
        private string folder, oldSave, oldPhotos;
        private CabinStationView cabin;
        private static readonly float[][] Depths = {
            new[] {230f,330f,460f}, new[] {280f,390f,510f},
            new[] {360f,470f,560f,640f}, new[] {525f,590f,615f,680f,735f}
        };
        [UnitySetUp] public IEnumerator Setup()
        {
            folder = Path.Combine(Application.temporaryCachePath, "MissionDepth-" + Guid.NewGuid().ToString("N"));
            oldSave = ExpeditionSaveStore.PathOverride; oldPhotos = PhotoCaptureService.ArchivePathOverride;
            ExpeditionSaveStore.PathOverride = Path.Combine(folder, "timeline.json");
            PhotoCaptureService.ArchivePathOverride = Path.Combine(folder, "photos");
            TutorialTestSave.SeedReturningPlayer();
            if (SceneFlowController.Instance != null) { Object.Destroy(SceneFlowController.Instance.gameObject); yield return null; }
            yield return SceneManager.LoadSceneAsync("GameplayCore", LoadSceneMode.Single);
            yield return SceneManager.LoadSceneAsync("Zone01", LoadSceneMode.Additive);
            yield return null; yield return null;
            cabin = Object.FindAnyObjectByType<CabinStationView>();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            if (SceneFlowController.Instance != null) Object.Destroy(SceneFlowController.Instance.gameObject);
            yield return null;
            ExpeditionSaveStore.PathOverride = oldSave; PhotoCaptureService.ArchivePathOverride = oldPhotos;
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }
        [Test] public void ExactDesignerDepthsFloorsEntriesAndUngatedExits()
        {
            var maps = cabin.GetComponent<WorldMapController>().zoneMaps;
            for (int zone = 0; zone < 4; zone++)
            {
                var config = maps[zone].GetComponent<ZoneMapPresentation>().config;
                Assert.That(config.minimumDepth, Is.EqualTo(zone == 0 ? 0 : zone == 3 ? 500 : 230));
                Assert.That(config.entryDepth, Is.EqualTo(zone == 3 ? 500 : 230));
                Assert.That(config.entryHeading, Is.EqualTo(90));
                for (int i = 0; i < Depths[zone].Length; i++)
                {
                    string id = zone == 0 ? new[] {"zone01-left","zone01-east","zone01-north"}[i] : $"zone0{zone+1}-l{i+1}";
                    var poi = Array.Find(config.locations, p => p.id == id);
                    Assert.That(poi, Is.Not.Null, id);
                    Assert.That(poi.overrideDepth, Is.True, id);
                    Assert.That(poi.targetDepth, Is.EqualTo(Depths[zone][i]), id);
                }
                if (config.exitArea != null) Assert.That(config.exitArea.overrideDepth, Is.False);
                if (config.finalHiddenPoint != null) Assert.That(config.finalHiddenPoint.overrideDepth, Is.False);
                if (zone == 0) Assert.That(config.entryPosition, Is.EqualTo(new Vector2(100,400)));
                if (zone == 2)
                {
                    Assert.That(config.rockInteractionArea.overrideDepth, Is.True);
                    Assert.That(config.rockInteractionArea.targetDepth, Is.EqualTo(590));
                }
            }
        }
        [Test] public void VisibleMissionDepthsAndHiddenRevealUseResolverAndHoverDoesNotReveal()
        {
            var session = cabin.GetComponent<CabinZoneSession>();
            var story = cabin.GetComponent<ZoneOneStory>();
            var survey = cabin.GetComponent<PhotoSurveyZone>();
            var field = typeof(CabinStationView).GetField("mapReadout", BindingFlags.Instance|BindingFlags.NonPublic);
            var readout = (UnityEngine.UI.Text)field.GetValue(cabin);
            // Current watercolor scene leaves this legacy binding empty; exercise its resolver without migrating UI.
            if (readout == null)
            {
                var fixture = new GameObject("Hover readout fixture", typeof(RectTransform));
                fixture.transform.SetParent(cabin.transform, false);
                readout = fixture.AddComponent<UnityEngine.UI.Text>(); field.SetValue(cabin, readout);
            }
            foreach (string zone in new[] {"Zone01", "Zone04"})
            {
                Assert.That(session.Configure(zone), Is.True);
                foreach (var poi in survey.locations)
                {
                    var overlay = cabin.MapPanel.GetComponentInChildren<PhotoSurveyMap>(true);
                    var uv = new Vector2(poi.mapPosition.x / session.ActiveConfig.WorldSize.x, poi.mapPosition.y / session.ActiveConfig.WorldSize.y);
                    var location = Array.Find(story.config.locations, x => x.poiId == poi.id);
                    if (location.visibility == LocationVisibility.Visible)
                        Assert.That(story.MissionText(), Does.Contain($"TARGET DEPTH: {survey.DepthFor(poi):0} M"));
                    else
                    {
                        Assert.That(story.MissionText(), Does.Not.Contain($"TARGET DEPTH: {survey.DepthFor(poi):0} M"));
                        Assert.That(story.IsPoiVisible(poi.id), Is.False);
                    }
                    cabin.ShowChartCoordinate(new Vector2(poi.mapPosition.x / session.ActiveConfig.WorldSize.x, poi.mapPosition.y / session.ActiveConfig.WorldSize.y));
                    Assert.That(readout.text, Does.Contain($"SÂU {survey.DepthFor(poi):0} m"));
                    overlay.SetPointer(uv);
                    if (location.visibility == LocationVisibility.Visible)
                        Assert.That(overlay.coordinateReadout.text, Does.Contain($"Z {survey.DepthFor(poi):0.0} M"));
                    if (location.visibility == LocationVisibility.HiddenRadar)
                    {
                        Assert.That(overlay.coordinateReadout.text, Does.Not.Contain($"Z {survey.DepthFor(poi):0.0} M"), "Unrevealed hidden target depth is not a hover clue.");
                        Assert.That(story.IsPoiVisible(poi.id), Is.False, "Hover must not reveal hidden markers.");
                        story.HiddenRouteAvailable = true; Assert.That(story.RevealPoi(poi.id), Is.True);
                        Assert.That(story.MissionText(), Does.Contain($"TARGET DEPTH: {survey.DepthFor(poi):0} M"));
                        overlay.SetPointer(uv);
                        Assert.That(overlay.coordinateReadout.text, Does.Contain($"Z {survey.DepthFor(poi):0.0} M"));
                    }
                }
            }
        }
        [Test] public void AuthoredDepthsDriveRadarPhotoAndCaptureInEveryZone()
        {
            var maps = cabin.GetComponent<WorldMapController>().zoneMaps;
            var owner = new GameObject("Authored depth gameplay fixture");
            var mission = ScriptableObject.CreateInstance<MissionDefinition>();
            try
            {
                var nav = owner.AddComponent<ZoneNavigation>();
                nav.ConfigureMapCoordinates(new Vector2(1200,700), 50);
                var water = new byte[120*70]; Array.Fill(water, (byte)1); nav.SetChart(water,120,70);
                var ship = nav.Ship.Export(); ship.maximumDepth = 750; nav.Ship.Restore(ship);
                var survey = owner.AddComponent<PhotoSurveyZone>(); survey.mission = mission;
                var catcher = owner.AddComponent<CreatureCatcher>(); catcher.survey = survey; catcher.navigation = nav;
                catcher.inventory = owner.AddComponent<CreatureInventory>();
                for (int i = 0; i < CreatureInventory.Capacity; i++) catcher.inventory.TryAdd("item"+i,"item",null);
                survey.CompleteTask(PhotoSurveyZone.TaskKind.Photograph);
                foreach (var map in maps)
                {
                    var config = map.GetComponent<ZoneMapPresentation>().config;
                    var authored = config.locations[config.locations.Length-1];
                    var poi = new MapPoi {id=authored.id, mapPosition=new Vector2(600,120), arrivalRadius=20, overrideDepth=authored.overrideDepth, targetDepth=authored.targetDepth};
                    survey.locations = new[] {poi}; mission.targetPoiId = poi.id;
                    survey.ResetContacts(); survey.RestoreCreatureSpawn(1,poi.id,poi.mapPosition);
                    nav.RestoreVoyage(poi.mapPosition,0,config.entryDepth,0);
                    Assert.That(survey.TryGetRadarContact(nav,85,out _,out _), Is.False, poi.id);
                    Assert.That(survey.TryGetPhotoContact(nav,85,60,out _,out _), Is.False, poi.id);
                    Assert.That(catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.Empty), poi.id);
                    nav.RestoreVoyage(poi.mapPosition,0,survey.DepthFor(poi),0);
                    Assert.That(survey.TryGetRadarContact(nav,85,out _,out _), Is.True, poi.id);
                    Assert.That(survey.TryGetPhotoContact(nav,85,60,out _,out var position), Is.True, poi.id);
                    Assert.That(position.z, Is.EqualTo(-survey.DepthFor(poi)), poi.id);
                    Assert.That(catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.Full), "Full means depth validation passed: "+poi.id);
                }
            }
            finally { Object.DestroyImmediate(owner); Object.DestroyImmediate(mission); }
        }
    }
}
