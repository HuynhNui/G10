using System.Collections;
using System.IO;
using G10.Prototype.Computer;
using G10.Prototype.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace G10.Prototype.Tests
{
    public sealed class WorldMapPlayModeTests
    {
        private LegacyCabinTestSession session;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            session = new LegacyCabinTestSession(); yield return session.Begin();
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if(session!=null)yield return session.End();
        }

        [UnityTest]
        public IEnumerator WorldMapHoverNavigationAndSelectionAreRetained()
        {
            var world=Object.FindAnyObjectByType<WorldMapController>(); Assert.That(world,Is.Not.Null);
            var cabin=world.cabin; int scenes=SceneManager.sceneCount;
            // Session startup intentionally remembers the active zone. Select World explicitly.
            world.OpenWorld();cabin.ClosePanel();
            cabin.OpenMap(); yield return null;
            Assert.That(cabin.Panels.CurrentPanel,Is.EqualTo(world.worldPanel));
            Assert.That(cabin.MapPanel.activeSelf,Is.False);
            var spots=world.worldPanel.GetComponentsInChildren<WorldMapZoneHotspot>(); Assert.That(spots.Length,Is.EqualTo(4));
            var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
            var first=spots[0]; Vector2 center=Vector2.zero; foreach(var uv in first.polygon)center+=uv;center/=first.polygon.Length;
            Vector2 local=first.rectTransform.rect.min+Vector2.Scale(center,first.rectTransform.rect.size);
            pointer.position=RectTransformUtility.WorldToScreenPoint(null,first.rectTransform.TransformPoint(local));
            Canvas.ForceUpdateCanvases();
            var hits=new System.Collections.Generic.List<RaycastResult>(); EventSystem.current.RaycastAll(pointer,hits);
            Assert.That(hits.Exists(hit=>hit.gameObject==first.gameObject),Is.True,"Unhighlighted polygons must receive real UI raycasts.");
            Assert.That(first.Raycast(pointer.position,null),Is.True);
            Assert.That(first.Raycast(new Vector2(-100,-100),null),Is.False);
            first.OnPointerEnter(pointer); Assert.That(first.Highlighted,Is.True);
            yield return CabinNavigationPlayModeTests.CaptureArt(cabin,"world-map-hover.png");
            first.OnPointerExit(pointer);Assert.That(first.Highlighted,Is.False);
            first.OnPointerClick(pointer);Assert.That(cabin.Panels.CurrentPanel,Is.EqualTo(cabin.MapPanel));
            var overlay=world.zone01Overlay;
            overlay.SetPointer(overlay.mapConfig.CoordinatesToUV(overlay.Locations[1].mapPosition));
            overlay.SelectHoveredLocation(); Assert.That(overlay.SelectedLocation,Is.EqualTo(1));
            Assert.That(cabin.MapPanel.GetComponent<IPanelBackHandler>().TryHandleBack(),Is.True);
            Assert.That(cabin.Panels.IsPanelOpen,Is.False);
            Assert.That(world.worldPanel.activeSelf,Is.False,"Escape must not open the world map.");
            cabin.OpenMap();Assert.That(overlay.SelectedLocation,Is.EqualTo(1));
            Assert.That(cabin.Panels.CurrentPanel,Is.EqualTo(cabin.MapPanel));
            Assert.That(overlay.styledTaskReadout.transform.parent.gameObject.activeSelf,Is.False);
            overlay.SetPointer(overlay.mapConfig.CoordinatesToUV(overlay.Locations[1].mapPosition));
            Assert.That(overlay.styledTaskReadout.transform.parent.gameObject.activeSelf,Is.True);
            Assert.That(overlay.styledTaskReadout.text,Does.Contain("02 • RÃNH SAN HÔ CỔ")); // Stored index 1 is the rightmost site and story location 2.
            world.OpenWorld();spots[2].OnPointerClick(pointer);
            Assert.That(cabin.Panels.CurrentPanel,Is.EqualTo(world.zoneMaps[2]));
            world.zoneMaps[2].GetComponent<IPanelBackHandler>().TryHandleBack();
            world.ResumeZone();Assert.That(cabin.Panels.CurrentPanel,Is.EqualTo(world.zoneMaps[2]));
            cabin.OpenNavigation();cabin.OpenMap();
            Assert.That(cabin.Panels.CurrentPanel,Is.EqualTo(world.zoneMaps[2]),"Reopening Map must retain the selected zone during the current session.");
            Assert.That(cabin.NavigationPanel.activeSelf,Is.False);
            world.OpenWorld();world.CloseWorld();Assert.That(cabin.Panels.IsPanelOpen,Is.False);
            cabin.OpenMap();
            Assert.That(cabin.Panels.CurrentPanel,Is.EqualTo(world.worldPanel),"After explicitly returning to World Map, reopening Map must retain World Map.");
            cabin.OpenNavigation();cabin.OpenMap();
            Assert.That(cabin.Panels.CurrentPanel,Is.EqualTo(world.worldPanel),"The helm Map shortcut must use the same retained map state.");
            Assert.That(cabin.NavigationPanel.activeSelf,Is.False);
            world.CloseWorld();
            Assert.That(SceneManager.sceneCount,Is.EqualTo(scenes));
        }

        [UnityTest]
        public IEnumerator EscapeClosesEveryZoneWhileOnlyMapButtonReturnsToWorld()
        {
            var world = Object.FindAnyObjectByType<WorldMapController>();
            var cabin = world.cabin;
            int scenes = SceneManager.sceneCount;
            // Match the existing capture input test: the hidden batch Editor has no Game-view focus.
            var originalSettings = InputSystem.settings;
            var inputSettings = Object.Instantiate(originalSettings);
            inputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            inputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings = inputSettings;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                for (int index = 0; index < world.zoneMaps.Length; index++)
                {
                    world.OpenZone(index);
                    yield return null;
                    Assert.That(world.zoneMaps[index].transform.Find("WatercolorHUD/Cabin"), Is.Null, "Top HUD must not contain the removed cabin button.");
                    keyboard.MakeCurrent();
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
                    InputSystem.Update();
                    Assert.That(Keyboard.current, Is.SameAs(keyboard));
                    Assert.That(keyboard.escapeKey.wasPressedThisFrame, Is.True);
                    // Run the real keyboard handler before batchmode resets unfocused device state.
                    cabin.Panels.SendMessage("Update");
                    Assert.That(cabin.Panels.IsPanelOpen, Is.False, $"Escape must return from zone {index + 1} straight to cabin.");
                    Assert.That(world.worldPanel.activeSelf, Is.False);
                    Assert.That(PauseMenuController.Instance == null || !PauseMenuController.Instance.IsPaused, Is.True);
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                    InputSystem.Update();
                    yield return null;

                    cabin.OpenMap();
                    Assert.That(cabin.Panels.CurrentPanel, Is.SameAs(world.zoneMaps[index]), "Closing the map must retain the remembered zone.");
                    var worldButton = world.zoneMaps[index].transform.Find("WatercolorWorld").GetComponent<UnityEngine.UI.Button>();
                    Assert.That(worldButton.GetComponentInChildren<TMPro.TMP_Text>().text, Is.EqualTo("MAP TỔNG"));
                    worldButton.onClick.Invoke();
                    Assert.That(cabin.Panels.CurrentPanel, Is.SameAs(world.worldPanel), "Only the explicit MAP TỔNG action returns to the world map.");
                }

                foreach (var panel in new[] { cabin.NavigationPanel, cabin.RadarPanel, world.worldPanel })
                {
                    Assert.That(panel.transform.Find("WatercolorHUD/Cabin"), Is.Null);
                    cabin.Panels.OpenPanel(panel);
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
                    InputSystem.Update();
                    cabin.Panels.SendMessage("Update");
                    Assert.That(cabin.Panels.IsPanelOpen, Is.False, "Escape still returns to cabin from " + panel.name);
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                    InputSystem.Update();
                    yield return null;
                }
                Assert.That(SceneManager.sceneCount, Is.EqualTo(scenes), "Map navigation must not unload gameplay scenes.");
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                InputSystem.settings = originalSettings;
                Object.Destroy(inputSettings);
            }
        }

        [UnityTest]
        public IEnumerator CleanCreatureHasRealAlphaAndCapturesThroughExistingService()
        {
            var cabin=Object.FindAnyObjectByType<CabinStationView>(); var capture=cabin.GetComponent<PhotoCaptureService>();
            var art=capture.profile.creature;
            Assert.That(art.name,Is.EqualTo("Creature001_Clean"));Assert.That(art.isReadable,Is.True);
            int transparent=0,solid=0;foreach(var p in art.GetPixels32()){if(p.a==0)transparent++;if(p.a>200)solid++;}
            Assert.That(transparent,Is.GreaterThan(art.width*art.height/3));Assert.That(solid,Is.GreaterThan(art.width*art.height/20));
            Assert.That(art.GetPixel(0,0).a,Is.Zero);
            Assert.That(capture.profile.silhouette.name,Is.EqualTo("Creature001_Silhouette"));
            var nav=capture.navigation;nav.ResetVoyage();
            PhotoSurveyPlayModeTests.PlaceShip(nav, capture.survey.center + Vector2.right * 10);
            Vector2 delta=capture.survey.center-nav.Position;
            float turn=Mathf.DeltaAngle(nav.Heading,Mathf.Atan2(delta.x,delta.y)*Mathf.Rad2Deg);
            nav.Step(0,Mathf.Sign(turn),Mathf.Abs(turn)/40f);
            nav.StepDepth(Mathf.Sign(capture.survey.targetDepth-nav.Depth),Mathf.Abs(capture.survey.targetDepth-nav.Depth)/5f);
            var record=capture.Capture();Assert.That(record,Is.Not.Null);Assert.That(record.Result,Is.Not.EqualTo("NoSubject"));
            System.IO.File.WriteAllBytes(CabinNavigationPlayModeTests.CapturePath("creature-photo.png"),record.Image.EncodeToPNG());
            cabin.OpenCamera();yield return null;
            yield return CabinNavigationPlayModeTests.CaptureArt(cabin,"creature-camera.png");
        }
    }
}
