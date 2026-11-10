#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using G10.Prototype.Computer;
using G10.Prototype.Core;
using G10.Prototype.Feedback;
using G10.Prototype.Missions;
using G10.Prototype.Navigation;
using G10.Prototype.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace G10.Prototype.Tests
{
    /// <summary>Real authored scenes/controls and transactions. Position/refill and minigame Win are controlled seams, not a full voyage.</summary>
    public sealed class LilyArtistIntegrationPlayModeTests
    {
        private string folder, oldSave, oldPhotos;
        private SceneFlowController flow;
        private EditorWindow gameView;
        private int oldSize;
        private readonly List<Mouse> nativeMice = new();
        private Mouse testMouse;
        private CabinStationView Cabin => Object.FindAnyObjectByType<CabinStationView>();
        private ExpeditionLoop Loop => Object.FindAnyObjectByType<ExpeditionLoop>();
        private ZoneOneStory Story => Cabin.GetComponent<ZoneOneStory>();
        private PhotoCaptureService Photos => Cabin.GetComponent<PhotoCaptureService>();
        private ComputerScreenController Computer => Cabin.GetComponentInChildren<ComputerScreenController>(true);
        private static readonly string Qa = Path.GetFullPath("Docs/QA/LilyArtistIntegration/screens");

        [UnitySetUp] public IEnumerator Setup()
        {
            Assert.That(SessionState.GetBool("G10.Zone02Experience.isolated", false), Is.True, "Outer save isolation is required.");
            // These tests dispatch pointer events explicitly. Keep physical Editor mouse events
            // from clicking a newly opened desktop during a screenshot yield.
            nativeMice.Clear(); nativeMice.AddRange(InputSystem.devices.OfType<Mouse>().Where(m=>m.enabled));
            foreach(var mouse in nativeMice) InputSystem.DisableDevice(mouse);
            testMouse = InputSystem.AddDevice<Mouse>();
            oldSave = ExpeditionSaveStore.PathOverride; oldPhotos = PhotoCaptureService.ArchivePathOverride;
            folder = Path.Combine(Application.temporaryCachePath, "LilyArtist-" + Guid.NewGuid().ToString("N"));
            ExpeditionSaveStore.SelectNormalTimeline(); ExpeditionSaveStore.PathOverride = Path.Combine(folder,"timeline.json");
            PhotoCaptureService.ArchivePathOverride = Path.Combine(folder,"photos"); TutorialTestSave.SeedReturningPlayer();
            if (SceneFlowController.Instance != null) { Object.Destroy(SceneFlowController.Instance.gameObject); yield return null; }
            yield return SceneManager.LoadSceneAsync("Bootstrap",LoadSceneMode.Single); yield return null;
            flow = SceneFlowController.Instance; yield return Transition(); yield return null; yield return null;
            gameView = EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"));
            oldSize = (int)gameView.GetType().GetProperty("selectedSizeIndex").GetValue(gameView);
            Directory.CreateDirectory(Qa); Resolution(1920,1080); yield return null; yield return null;
        }
        private IEnumerator StartZone(int zone)
        {
            var menu = Object.FindAnyObjectByType<MainMenuDebugStart>(); menu.Open(); menu.SelectZone(zone); menu.StartRun();
            yield return Transition(); yield return null; yield return null;
            Assert.That(Loop.Zone, Is.EqualTo("Zone0"+zone)); Assert.That(Loop.IsInitialized, Is.True,Loop.LastError);
        }
        [UnityTest] public IEnumerator NativeMenuAndLilyDossierFitBothResolutionsAndEscapeReturnsToMission()
        {
            foreach (int width in new[] {1920,1366})
            {
                Resolution(width,width==1920?1080:768); yield return null; yield return null;
                Assert.That(Object.FindAnyObjectByType<LilyMenuPresentation>(),Is.Not.Null);
                foreach (string name in new[]{"StartButton","ContinueButton","QuitButton"})
                {
                    var button=GameObject.Find(name).GetComponent<Button>(); AssertOnScreen((RectTransform)button.transform);
                    AssertRaycast(button); AssertText(button.transform);
                }
                yield return Shot(width+"-native-menu");
            }
            flow.ContinueGame(); yield return Transition(); yield return null; yield return null;
            yield return Until(()=>!Cabin.GetComponent<ZoneOneAudioController>().IsIntroPlaying && !Loop.ZoneOneIntroPending,40,"Wait for the real opening before document input.");
            Cabin.OpenComputer(); Computer.OpenMissionLog(); yield return null;
            var archive=Computer.GetComponentInChildren<MissionArchiveView>(true);
            string before=JsonUtility.ToJson(Story.ExportProgress());
            Click(archive.transform.GetComponentsInChildren<Button>(true).Single(b=>b.name=="OpenMissionArchive")); yield return null;
            Assert.That(archive.IsOpen,Is.True); Assert.That(archive.document.sprite,Is.SameAs(archive.lily));
            foreach(int width in new[]{1920,1366})
            {
                Resolution(width,width==1920?1080:768);yield return null;yield return null; AssertText(archive.reader.transform);
                yield return Shot(width+"-lily-dossier");
            }
            archive.Next(); Assert.That(archive.EmmaAvailable,Is.False); Assert.That(archive.document.enabled,Is.False);
            archive.Next();Assert.That(archive.CoordinatesAvailable,Is.False);Assert.That(archive.paperA.gameObject.activeSelf,Is.False);
            Assert.That(archive.transcription.text,Does.Not.Contain("1006.56"));
            yield return PhotoLabLayoutPlayModeTests.Escape();
            Assert.That(archive.IsOpen,Is.False);Assert.That(Computer.CurrentApp,Is.EqualTo(ComputerAppId.MissionLog));
            Assert.That(JsonUtility.ToJson(Story.ExportProgress()),Is.EqualTo(before));
        }
        [UnityTest] public IEnumerator ZoneTwoShutterSendCaptureCargoContinueAndHullUseBothNativeMaterials()
        {
            yield return StartZone(2);
            yield return PhotographAndSend("zone02-l1","Z2_Creature_01");yield return Capture("zone02-l1","Z2_Creature_01");
            yield return PhotographAndSend("zone02-l3","Z2_Creature_02");yield return Capture("zone02-l3","Z2_Creature_02");
            yield return Cargo("Z2_Creature_01");yield return Cargo("Z2_Creature_02");
            yield return ContinueDebug();yield return Cargo("Z2_Creature_01");
            yield return Upgrade("Zone02-materials");
            Assert.That(Story.CanApplyProgressionAction,Is.True,Story.ProgressionActionDescription);
            var upgrade=Computer.GetComponentInChildren<SubmarineUpgradeUIController>(true);
            Click(upgrade.GetComponentsInChildren<Button>(true).Single(b=>b.name=="ActionButton"));yield return null;
            Assert.That(Story.ZoneTwoHullInstalled,Is.True);Assert.That(Cabin.Navigation.Ship.MaximumDepth,Is.GreaterThanOrEqualTo(750));
            Assert.That(Story.inventory.GetCount("Z2_Creature_01"),Is.Zero);Assert.That(Story.inventory.GetCount("Z2_Creature_02"),Is.Zero);
            Assert.That(Story.ApplyProgressionAction(),Is.False,"Repeated click cannot spend twice.");
            yield return ContinueDebug();Assert.That(Story.ZoneTwoHullInstalled,Is.True);
        }
        [UnityTest] public IEnumerator ZoneThreeShutterSendCaptureCraftAndInstallKeepCostsAndArtworkSeparate()
        {
            yield return StartZone(3);
            yield return PhotographAndSend("zone03-l1","Z3_Creature_01");yield return Capture("zone03-l1","Z3_Creature_01");
            yield return PhotographAndSend("zone03-l2","Z3_Creature_02");
            yield return PhotographAndSend("zone03-l4","Z3_Creature_03");yield return Capture("zone03-l4","Z3_Creature_03");
            yield return Cargo("Z3_Creature_01");yield return Cargo("Z3_Creature_03");
            yield return Upgrade("Zone03-materials");
            Assert.That(Story.CanApplyProgressionAction,Is.True,Story.ProgressionActionDescription);
            var apply=Computer.GetComponentInChildren<SubmarineUpgradeUIController>(true).GetComponentsInChildren<Button>(true).Single(b=>b.name=="ActionButton");
            Click(apply);yield return null;
            Assert.That(Story.HasObjective("Z3_GATE_CRAFT_ROCK_BREAKER"),Is.True);
            Assert.That(Story.inventory.GetCount("Z3_Creature_01"),Is.Zero);Assert.That(Story.inventory.GetCount("Z3_Creature_03"),Is.Zero);
            Click(apply);yield return null;Assert.That(Story.RockBreakerInstalled,Is.True);
            Assert.That(Story.HasObjective("Z3_GATE_DESTROY_ROCK"),Is.False);
            yield return ContinueDebug();Assert.That(Story.RockBreakerInstalled,Is.True);Assert.That(Story.inventory.Items.Count,Is.Zero);
        }
        [UnityTest] public IEnumerator WreckCaptureUnlocksArtistEmmaAndNaturalPaperThenContinueDoesNotRevealHiddenSites()
        {
            yield return StartZone(4);
            Cabin.ClosePanel(); int checkpointDay = Loop.Day; Assert.That(Loop.Rest(),Is.True,Loop.LastError);
            Cabin.OpenComputer();Computer.OpenMissionLog();yield return null;
            var archive=Computer.GetComponentInChildren<MissionArchiveView>(true);archive.Show(1);Assert.That(archive.document.enabled,Is.False);archive.Close();
            yield return PhotographAndSend("zone04-l1","Z4_Creature_01");yield return Capture("zone04-l1","Z4_Creature_01");
            Cabin.OpenComputer();Computer.OpenMissionLog();yield return null;
            var presentation=Cabin.GetComponent<ZoneFourPresentationController>();
            yield return Until(()=>presentation.fragmentView.ReadableCompleted,7,"Natural recovered-fragment sequence did not render.");
            Assert.That(presentation.fragmentView.FirstCoordinateText,Does.Contain("1006.56").And.Contain("450.51"));
            Assert.That(presentation.fragmentView.SecondCoordinateText,Does.Contain("358.56").And.Contain("119.57"));
            yield return Shot("1920-native-coordinate-recovery");
            yield return Until(()=>Loop.ZoneFourPresentation.Has("fragment"),3,"Fragment must publish after visible exposure.");
            yield return Until(()=>!presentation.FragmentActive,7,"Fragment must finish.");
            string authority=JsonUtility.ToJson(Story.ExportProgress());
            foreach(int width in new[]{1920,1366})
            {
                Resolution(width,width==1920?1080:768);yield return null;yield return null;
                archive.Show(1);yield return null;Assert.That(archive.document.sprite,Is.SameAs(archive.emma));AssertText(archive.reader.transform);yield return Shot(width+"-emma-card");
                archive.Show(2);yield return null;Assert.That(archive.paperA.text,Does.Contain("1006.56"));AssertText(archive.reader.transform);yield return Shot(width+"-coordinate-paper");
            }
            Assert.That(Story.IsLocationRevealed("Z4_L3")||Story.IsLocationRevealed("Z4_L4"),Is.False);
            Assert.That(JsonUtility.ToJson(Story.ExportProgress()),Is.EqualTo(authority));
            archive.Close();yield return ContinueDebug();Cabin.OpenComputer();Computer.OpenMissionLog();yield return null;
            archive=Computer.GetComponentInChildren<MissionArchiveView>(true);archive.Show(2);Assert.That(archive.CoordinatesAvailable,Is.True);
            Assert.That(Cabin.GetComponent<ZoneFourPresentationController>().FragmentActive,Is.False);
            Assert.That(Story.IsLocationRevealed("Z4_L3")||Story.IsLocationRevealed("Z4_L4"),Is.False);
            yield return Shot("1366-continued-coordinate-paper");
            // Same-zone Journal restoration must revoke an already open document's future data.
            Assert.That(Loop.RestoreDay(checkpointDay),Is.True,Loop.LastError);yield return null;
            Assert.That(archive.IsOpen,Is.True);Assert.That(archive.CoordinatesAvailable||archive.EmmaAvailable,Is.False);
            Assert.That(archive.document.enabled||archive.paperA.gameObject.activeSelf||archive.paperB.gameObject.activeSelf,Is.False);
            Assert.That(archive.transcription.text,Does.Not.Contain("1006.56"));
            yield return Shot("1366-journal-rollback-locks-open-document");
        }
        [UnityTest] public IEnumerator FixtureEndingBrandingUsesNativeLogoWithoutChangingSavedEnding()
        {
            // Visual scene fixture only; natural ending eligibility/arrival remain covered by progression regressions.
            yield return SceneManager.LoadSceneAsync("Ending",LoadSceneMode.Single);yield return null;
            var presentation=Object.FindAnyObjectByType<EndingPresentation>();
            foreach(string id in new[]{"normal","hidden"})
            {
                presentation.Show(id);yield return new WaitForSecondsRealtime(2);
                foreach(int width in new[]{1920,1366})
                {
                    Resolution(width,width==1920?1080:768);yield return null;yield return null;
                    var logo=GameObject.Find("Lily25DaysLogo").GetComponent<Image>();Assert.That(logo.raycastTarget,Is.False);AssertOnScreen(logo.rectTransform);
                    yield return Shot(width+"-"+id+"-branding-fixture");
                }
            }
            Assert.That(ExpeditionSaveStore.TryReadNormal(out var save,out _),Is.True);
            Assert.That(save.current.endingReached,Is.Null.Or.Empty,"A presentation-only fixture cannot award an ending.");
        }
        private IEnumerator PhotographAndSend(string poiId,string id)
        {
            Cabin.ClosePanel();Cabin.Navigation.Ship.Refill();var poi=Photos.survey.FindPoi(poiId);bool found=false;
            foreach(float radius in new[]{20f,14f,26f})
            {
                for(int i=0;i<16;i++)
                {
                    float angle=i*Mathf.PI/8;var pos=Photos.survey.ContactPosition(poi)-new Vector2(Mathf.Sin(angle),Mathf.Cos(angle))*radius;
                    if(!Cabin.Navigation.CanOccupy(pos))continue;
                    Cabin.Navigation.RestoreVoyage(pos,i*22.5f,Photos.survey.DepthFor(poi),0);
                    if(Photos.survey.TryGetPhotoContact(Cabin.Navigation,Photos.profile.visibleDistance,Photos.profile.fieldOfView,out var contact,out _)&&contact==poi){found=true;break;}
                }
                if(found)break;
            }
            Assert.That(found,Is.True,poiId);int count=Photos.Photos.Count;yield return null;
            Click(Cabin.GetComponentsInChildren<Button>(true).Single(b=>b.name=="CameraHotspot"));
            yield return Until(()=>Photos.Photos.Count>count&&!Cabin.IsDirectInteractionActive,4,"Native camera did not finish.");
            var photo=Photos.Photos.Last();Assert.That(photo.Result,Is.EqualTo("GoodPhoto"));Assert.That(photo.MissionTargetId,Is.EqualTo(id));
            var definition=Story.FindContent(id);Assert.That(photo.IsFullPainting,Is.True);Assert.That(photo.Image.GetPixels32(),Is.EqualTo(definition.photoArtwork.GetPixels32()));
            Cabin.OpenComputer();Computer.OpenPhotoLab();yield return null;
            var lab=Computer.GetComponentInChildren<PhotoLabView>(true);lab.Refresh();Assert.That(lab.SelectedPhotoId,Is.EqualTo(photo.Id));
            yield return Shot(id+"-full-painting-before-send");Click(lab.sendButton);yield return null;Assert.That(photo.IsMissionPhoto,Is.True);
        }
        private IEnumerator Capture(string poiId,string id)
        {
            Cabin.ClosePanel();Cabin.Navigation.Ship.Refill();var poi=Photos.survey.FindPoi(poiId);
            Cabin.Navigation.RestoreVoyage(Photos.survey.ContactPosition(poi),0,Photos.survey.DepthFor(poi),0);yield return null;
            var catcher=Cabin.GetComponent<CreatureCatcher>();Click(Cabin.GetComponentsInChildren<Button>(true).Single(b=>b.name=="CaptureHotspot"));
            yield return Until(()=>catcher.minigame.IsActive,4,"Native capture device did not open.");
            Assert.That(catcher.minigame.EncounterArtwork,Is.SameAs(Story.FindContent(id).Image));
            yield return Shot(id+"-capture-creature");CaptureMinigamePlayModeTests.Win(catcher.minigame);yield return null;
            Assert.That(catcher.LastResult,Is.EqualTo(CreatureCatcher.Result.Caught));
            Assert.That(Story.inventory.Items.Single(x=>x.Id==id).Icon,Is.SameAs(Story.FindContent(id).InventoryImage));
        }
        private IEnumerator Cargo(string id)
        {
            Cabin.OpenCargo();yield return null;var cargo=Computer.GetComponentInChildren<CreatureInventoryView>(true);
            int index=Story.inventory.Items.ToList().FindIndex(x=>x.Id==id);Assert.That(index,Is.GreaterThanOrEqualTo(0));Click(cargo.slots[index]);yield return null;
            Assert.That(cargo.detailPreview.texture,Is.SameAs(Story.FindContent(id).InventoryImage));
            Assert.That(cargo.detailName.text,Is.EqualTo(Story.FindContent(id).InventoryName));AssertText(cargo.transform);
            yield return Shot(id+"-native-cargo");
        }
        private IEnumerator Upgrade(string name)
        {
            Cabin.OpenComputer();Computer.OpenUpgrade();yield return null;
            var view=Computer.GetComponentInChildren<SubmarineUpgradeUIController>(true);
            var entry=view.GetComponentsInChildren<UpgradeEntryConfig>(true).Single(e=>e.UpgradeId=="ExpeditionModule");view.Select(entry);yield return null;
            var rows=view.GetComponentsInChildren<UpgradeMaterialRequirementView>();Assert.That(rows.Length,Is.EqualTo(2));
            foreach(var row in rows)Assert.That(row.GetComponentInChildren<Image>(true).enabled,Is.True);
            AssertText(view.transform);yield return Shot(name);
        }
        private IEnumerator ContinueDebug()
        {Cabin.ClosePanel();Assert.That(Loop.SaveCurrent(),Is.True);flow.LoadMainMenu();yield return Transition();flow.ContinueDebugGame();yield return Transition();yield return null;}
        private static IEnumerator Transition(){yield return Until(()=>SceneFlowController.Instance!=null&&!SceneFlowController.Instance.IsTransitioning,12,"Scene transition timed out.");}
        private static IEnumerator Until(Func<bool> condition,float seconds,string message)
        {float end=Time.realtimeSinceStartup+seconds;while(!condition()&&Time.realtimeSinceStartup<end)yield return null;Assert.That(condition(),Is.True,message);}
        private static void AssertRaycast(Button button)
        {
            Canvas.ForceUpdateCanvases();var rect=(RectTransform)button.transform;var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center))};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);Assert.That(hits,Is.Not.Empty,button.name);
            Assert.That(hits[0].gameObject.GetComponentInParent<Button>(),Is.SameAs(button),button.name+" is covered by "+hits[0].gameObject.name);
        }
        private static void Click(Button button)
        {
            Assert.That(button.interactable,Is.True,button.name);AssertRaycast(button);
            var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
        }
        private static void AssertOnScreen(RectTransform rect)
        {var corners=new Vector3[4];rect.GetWorldCorners(corners);Assert.That(corners.All(v=>v.x>=-1&&v.y>=-1&&v.x<=Screen.width+1&&v.y<=Screen.height+1),Is.True,rect.name);}
        private static void AssertText(Transform root)
        {
            Canvas.ForceUpdateCanvases();foreach(var text in root.GetComponentsInChildren<TMP_Text>())
            {text.ForceMeshUpdate();Assert.That(text.isTextOverflowing,Is.False,text.name+": "+text.text);Assert.That(text.text.All(c=>char.IsControl(c)||text.font.HasCharacter(c,true,false)),Is.True,"Missing glyph: "+text.text);}
        }
        private static IEnumerator Shot(string name)
        {Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(Qa,name+".png"),t.EncodeToPNG());Object.Destroy(t);}
        private void Resolution(int width,int height)
        {
            var asm=typeof(UnityEditor.Editor).Assembly;var sizesType=asm.GetType("UnityEditor.GameViewSizes");
            var sizes=sizesType.GetProperty("instance",BindingFlags.Public|BindingFlags.Static|BindingFlags.FlattenHierarchy).GetValue(null);
            var group=sizesType.GetMethod("GetGroup").Invoke(sizes,new[]{Enum.Parse(asm.GetType("UnityEditor.GameViewSizeGroupType"),"Standalone")});
            int count=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null),selected=-1;
            for(int i=0;i<count;i++){var size=group.GetType().GetMethod("GetGameViewSize").Invoke(group,new object[]{i});if((int)size.GetType().GetProperty("width").GetValue(size)==width&&(int)size.GetType().GetProperty("height").GetValue(size)==height){selected=i;break;}}
            if(selected<0){var size=Activator.CreateInstance(asm.GetType("UnityEditor.GameViewSize"),new object[]{Enum.Parse(asm.GetType("UnityEditor.GameViewSizeType"),"FixedResolution"),width,height,"Lily Artist QA"});group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});selected=count;}
            gameView.GetType().GetProperty("selectedSizeIndex").SetValue(gameView,selected);gameView.Repaint();
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
            if(gameView!=null)gameView.GetType().GetProperty("selectedSizeIndex").SetValue(gameView,oldSize);
            yield return SceneManager.LoadSceneAsync("MainMenu",LoadSceneMode.Single);
            if(SceneFlowController.Instance!=null)Object.Destroy(SceneFlowController.Instance.gameObject);yield return null;
            if(testMouse!=null&&testMouse.added)InputSystem.RemoveDevice(testMouse);
            foreach(var mouse in nativeMice)if(mouse.added)InputSystem.EnableDevice(mouse);
            nativeMice.Clear();testMouse=null;
            ExpeditionSaveStore.SelectNormalTimeline();ExpeditionSaveStore.PathOverride=oldSave;PhotoCaptureService.ArchivePathOverride=oldPhotos;
        }
    }
}
#endif
