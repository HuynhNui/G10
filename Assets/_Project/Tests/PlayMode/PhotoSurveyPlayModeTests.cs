using System.Collections;
using G10.Prototype.Core;
using G10.Prototype.Navigation;
using G10.Prototype.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace G10.Prototype.Tests
{
    public sealed class PhotoSurveyPlayModeTests
    {
        [UnityTest]
        public IEnumerator DepthButtonsChartAndRealRadarContact()
        {
            yield return SceneManager.LoadSceneAsync("GameplayCore", LoadSceneMode.Single);
            yield return SceneManager.LoadSceneAsync("Zone01", LoadSceneMode.Additive);
            yield return null;
            var cabin = Object.FindAnyObjectByType<CabinStationView>(); var nav = cabin.Navigation;
            var survey = cabin.GetComponent<PhotoSurveyZone>();
            Assert.That(survey, Is.Not.Null); Assert.That(nav.CanOccupy(survey.center), Is.True);
            cabin.OpenNavigation(); float depth = nav.Depth; float heading = nav.Heading;
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            var up = cabin.NavigationPanel.transform.Find("AscendHotspot").gameObject;
            ExecuteEvents.Execute(up, pointer, ExecuteEvents.pointerDownHandler);
            yield return new WaitForSeconds(.25f);
            ExecuteEvents.Execute(up, pointer, ExecuteEvents.pointerUpHandler);
            Assert.That(nav.Depth, Is.LessThan(depth)); Assert.That(nav.Heading, Is.EqualTo(heading));
            float held = nav.Depth; yield return new WaitForSeconds(.15f); Assert.That(nav.Depth, Is.EqualTo(held));
            var down = cabin.NavigationPanel.transform.Find("DiveHotspot").gameObject;
            ExecuteEvents.Execute(down, pointer, ExecuteEvents.pointerDownHandler);
            yield return new WaitForSeconds(.25f); cabin.OpenMap();
            Assert.That(nav.Depth, Is.GreaterThan(held));
            held = nav.Depth; yield return new WaitForSeconds(.15f); Assert.That(nav.Depth, Is.EqualTo(held));
            cabin.GetComponent<WorldMapController>().OpenZone(0);
            cabin.ShowChartCoordinate(ZoneNavigation.CoordinatesToUV(survey.center));
            Assert.That(cabin.MapPanel.transform.Find("ChartCoordinate").GetComponent<Text>().text, Does.Contain("P01"));
            Assert.That(survey.TargetPoi.id, Is.EqualTo("zone01-left"));
            Assert.That(survey.center, Is.EqualTo(new Vector2(275,75)));
            Assert.That(survey.Detectable(nav, 85), Is.False, "The old near-spawn tile is no longer the mission.");
            PlaceShip(nav, survey.center + Vector2.right * 10);
            Assert.That(survey.Detectable(nav, 85), Is.True);
            cabin.OpenRadar(); cabin.Scan(); yield return new WaitForSeconds(2.1f);
            Assert.That(cabin.Radar.VisibleContactCount, Is.EqualTo(1));
            survey.creaturePresent = false; cabin.Scan(); yield return new WaitForSeconds(2.1f);
            Assert.That(cabin.Radar.VisibleContactCount, Is.Zero);
            survey.creaturePresent = true; nav.StepDepth(1,1000);
            Assert.That(nav.Depth, Is.EqualTo(500)); Assert.That(nav.WorldPosition.z, Is.EqualTo(-500));
            Assert.That(survey.Detectable(nav,85), Is.False);
            nav.StepDepth(-1,1000); Assert.That(nav.Depth, Is.Zero);
        }
        internal static void PlaceShip(ZoneNavigation nav, Vector2 position)
        {
            // Move the test fixture without changing the production movement API.
            typeof(ZoneNavigation).GetProperty("Position").SetValue(nav, position);
            nav.Brake();
        }

        [UnityTest]
        public IEnumerator MissionUsesPoiRadiusAndNeverDrawsDestinationTiles()
        {
            yield return SceneManager.LoadSceneAsync("GameplayCore", LoadSceneMode.Single);
            yield return SceneManager.LoadSceneAsync("Zone01", LoadSceneMode.Additive);
            yield return null;
            var cabin = Object.FindAnyObjectByType<CabinStationView>();
            var survey = cabin.GetComponent<PhotoSurveyZone>();
            var catcher = cabin.GetComponent<CreatureCatcher>();
            var poi = survey.TargetPoi;
            Assert.That(poi, Is.SameAs(survey.locations[2]));
            Assert.That(survey.Contains(new Vector2(575,125)), Is.False);
            Assert.That(survey.Contains(poi.mapPosition + Vector2.right * 20), Is.True);
            Assert.That(survey.Contains(poi.mapPosition + new Vector2(19,19)), Is.False, "A grid corner is outside the arrival circle.");
            PlaceShip(cabin.Navigation, new Vector2(575,125));
            survey.CompleteTask(PhotoSurveyZone.TaskKind.Photograph);
            Assert.That(catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.Empty));
            PlaceShip(cabin.Navigation, poi.mapPosition);
            cabin.OpenMap(); cabin.GetComponent<WorldMapController>().OpenZone(0);
            yield return null;
            var overlay = cabin.MapPanel.GetComponentInChildren<PhotoSurveyMap>();
            Assert.That(overlay.LocationAt(ZoneNavigation.CoordinatesToUV(poi.mapPosition)), Is.EqualTo(2));
            Assert.That(overlay.LocationAt(ZoneNavigation.CoordinatesToUV(new Vector2(575,125))), Is.EqualTo(-1));
            // With chart lines disabled, the overlay must contain only the two ship strokes.
            overlay.showGrid = false;
            survey.creaturePresent = true;
            foreach (var point in new[] { poi.mapPosition, new Vector2(575,125) })
            {
                overlay.SetPointer(ZoneNavigation.CoordinatesToUV(point));
                using (var vertices = new VertexHelper())
                {
                    typeof(PhotoSurveyMap).GetMethod("OnPopulateMesh", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null, new[] { typeof(VertexHelper) }, null)
                        .Invoke(overlay, new object[] { vertices });
                    Assert.That(vertices.currentVertCount, Is.EqualTo(8));
                    var vertex = new UIVertex();
                    for (int i = 0; i < vertices.currentVertCount; i++)
                    { vertices.PopulateUIVertex(ref vertex, i); Assert.That(vertex.color, Is.EqualTo((Color32)Color.white)); }
                }
            }
            Assert.That(catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.Started));
            CaptureMinigamePlayModeTests.Win(catcher.minigame);
            Assert.That(catcher.LastResult, Is.EqualTo(CreatureCatcher.Result.Caught));
            Assert.That(survey.IsComplete, Is.True);
            var fixture = Object.Instantiate(survey.mission);
            survey.mission = fixture;
            fixture.targetPoiId = "zone01-east";
            Assert.That(survey.TargetPoi, Is.SameAs(survey.locations[1]));
            Assert.That(survey.Contains(survey.locations[1].mapPosition), Is.True);
            Assert.That(survey.Contains(poi.mapPosition), Is.False);
            fixture.targetPoiId = "missing";
            Assert.That(survey.TargetPoi, Is.Null);
            Assert.That(survey.Contains(Vector2.zero), Is.False);
            Assert.That(catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.Unavailable));
            Object.Destroy(fixture);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        { if (SceneFlowController.Instance != null) { Object.Destroy(SceneFlowController.Instance.gameObject); yield return null; } }
    }
}
