using System.Collections;
using G10.Prototype.Computer;
using G10.Prototype.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace G10.Prototype.Tests
{
    public sealed class WorldMapPlayModeTests
    {
        [UnityTest]
        public IEnumerator WorldMapHoverNavigationAndSelectionAreRetained()
        {
            yield return SceneManager.LoadSceneAsync("GameplayCore",LoadSceneMode.Single);
            yield return SceneManager.LoadSceneAsync("Zone01",LoadSceneMode.Additive);
            yield return null;
            var world=Object.FindAnyObjectByType<WorldMapController>(); Assert.That(world,Is.Not.Null);
            var cabin=world.cabin; int scenes=SceneManager.sceneCount;
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
            overlay.SetPointer(G10.Prototype.Navigation.ZoneNavigation.CoordinatesToUV(overlay.locationCoordinates[1]));
            overlay.SelectHoveredLocation(); Assert.That(overlay.SelectedLocation,Is.EqualTo(1));
            Assert.That(cabin.MapPanel.GetComponent<IPanelBackHandler>().TryHandleBack(),Is.True);
            Assert.That(cabin.Panels.CurrentPanel,Is.EqualTo(world.worldPanel));
            world.ResumeZone();Assert.That(overlay.SelectedLocation,Is.EqualTo(1));
            Assert.That(overlay.taskReadout.transform.parent.gameObject.activeSelf,Is.True);
            Assert.That(overlay.taskReadout.text,Does.Contain("ĐỊA ĐIỂM 02"));
            world.OpenWorld();spots[2].OnPointerClick(pointer);
            Assert.That(cabin.Panels.CurrentPanel,Is.EqualTo(world.zoneMaps[2]));
            world.zoneMaps[2].GetComponent<IPanelBackHandler>().TryHandleBack();
            world.ResumeZone();Assert.That(cabin.Panels.CurrentPanel,Is.EqualTo(world.zoneMaps[2]));
            world.OpenWorld();world.CloseWorld();Assert.That(cabin.Panels.IsPanelOpen,Is.False);
            cabin.OpenMap();
            Assert.That(cabin.Panels.CurrentPanel,Is.EqualTo(world.worldPanel),"Reopening Map must start at World Map even after visiting another zone.");
            cabin.OpenNavigation();cabin.OpenMap();
            Assert.That(cabin.Panels.CurrentPanel,Is.EqualTo(world.worldPanel),"The helm Map shortcut must also open World Map first.");
            Assert.That(cabin.NavigationPanel.activeSelf,Is.False);
            world.CloseWorld();
            Assert.That(SceneManager.sceneCount,Is.EqualTo(scenes));
        }

        [UnityTest]
        public IEnumerator CleanCreatureHasRealAlphaAndCapturesThroughExistingService()
        {
            yield return SceneManager.LoadSceneAsync("GameplayCore",LoadSceneMode.Single);
            yield return SceneManager.LoadSceneAsync("Zone01",LoadSceneMode.Additive);
            yield return null;
            var cabin=Object.FindAnyObjectByType<CabinStationView>(); var capture=cabin.GetComponent<PhotoCaptureService>();
            var art=capture.profile.creature;
            Assert.That(art.name,Is.EqualTo("Creature001_Clean"));Assert.That(art.isReadable,Is.True);
            int transparent=0,solid=0;foreach(var p in art.GetPixels32()){if(p.a==0)transparent++;if(p.a>200)solid++;}
            Assert.That(transparent,Is.GreaterThan(art.width*art.height/3));Assert.That(solid,Is.GreaterThan(art.width*art.height/20));
            Assert.That(art.GetPixel(0,0).a,Is.Zero);
            Assert.That(capture.profile.silhouette.name,Is.EqualTo("Creature001_Silhouette"));
            var nav=capture.navigation;nav.ResetVoyage();
            Vector2 delta=capture.survey.center-nav.Position;
            float turn=Mathf.DeltaAngle(nav.Heading,Mathf.Atan2(delta.x,delta.y)*Mathf.Rad2Deg);
            nav.Step(0,Mathf.Sign(turn),Mathf.Abs(turn)/40f);
            nav.StepDepth(Mathf.Sign(capture.survey.targetDepth-nav.Depth),Mathf.Abs(capture.survey.targetDepth-nav.Depth)/5f);
            var record=capture.Capture();Assert.That(record,Is.Not.Null);Assert.That(record.Result,Is.Not.EqualTo("NoSubject"));
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath,"../creature-photo.png"),record.Image.EncodeToPNG());
            cabin.OpenCamera();yield return null;
            yield return CabinNavigationPlayModeTests.CaptureArt(cabin,"creature-camera.png");
        }
    }
}
