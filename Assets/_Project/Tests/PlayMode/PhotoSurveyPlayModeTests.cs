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
            cabin.ShowChartCoordinate(ZoneNavigation.CoordinatesToUV(survey.center));
            Assert.That(cabin.MapPanel.transform.Find("ChartCoordinate").GetComponent<Text>().text, Does.Contain("P01"));
            Assert.That(ZoneNavigation.CellCenter(survey.center + new Vector2(10,10)), Is.EqualTo(survey.center));
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
        [UnityTearDown] public IEnumerator Cleanup()
        { if (SceneFlowController.Instance != null) { Object.Destroy(SceneFlowController.Instance.gameObject); yield return null; } }
    }
}
