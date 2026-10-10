using System;
using System.Collections;
using System.IO;
using G10.Prototype.Computer;
using G10.Prototype.Core;
using G10.Prototype.Navigation;
using G10.Prototype.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace G10.Prototype.Tests
{
    public sealed class PlaytestFeedbackVisualPlayModeTests
    {
        private string folder, oldSave, oldPhotos;
        private SceneFlowController flow;
        private CabinStationView Cabin => Object.FindAnyObjectByType<CabinStationView>();
        [UnitySetUp] public IEnumerator Setup()
        {
            folder=Path.Combine(Application.temporaryCachePath,"FeedbackVisual-"+Guid.NewGuid().ToString("N"));
            oldSave=ExpeditionSaveStore.PathOverride;oldPhotos=PhotoCaptureService.ArchivePathOverride;
            ExpeditionSaveStore.PathOverride=Path.Combine(folder,"save.json");PhotoCaptureService.ArchivePathOverride=Path.Combine(folder,"photos");
            TutorialTestSave.SeedReturningPlayer();
            if(SceneFlowController.Instance!=null){Object.Destroy(SceneFlowController.Instance.gameObject);yield return null;}
            yield return SceneManager.LoadSceneAsync("Bootstrap");yield return null;flow=SceneFlowController.Instance;
            yield return Transition();flow.StartNewGame();yield return Transition();
        }
        private IEnumerator Transition()
        {yield return null;float end=Time.realtimeSinceStartup+25;while(flow.IsTransitioning&&Time.realtimeSinceStartup<end)yield return null;Assert.That(flow.LastError,Is.Null.Or.Empty);}
        [UnityTearDown] public IEnumerator Cleanup()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu");if(flow!=null)Object.Destroy(flow.gameObject);yield return null;
            ExpeditionSaveStore.PathOverride=oldSave;PhotoCaptureService.ArchivePathOverride=oldPhotos;
            if(Directory.Exists(folder))Directory.Delete(folder,true);
        }
        [UnityTest] public IEnumerator CaptureMapLightsBeforeAfterWithoutMutatingAuthoredArtwork()
        {
            var world=Cabin.GetComponent<WorldMapController>();
            for(int zone=1;zone<4;zone++)
            {
                world.OpenZone(zone);yield return null;
                var presentation=world.zoneMaps[zone].GetComponent<ZoneMapPresentation>();
                var authored=presentation.config;var preview=Object.Instantiate(authored);
                try
                {
                    preview.ConfigureLightOpacity(1);presentation.config=preview;presentation.RefreshMap();
                    yield return CabinNavigationPlayModeTests.CaptureArt(Cabin,$"zone{zone+1:00}-light-before.png");
                    presentation.config=authored;presentation.RefreshMap();
                    yield return CabinNavigationPlayModeTests.CaptureArt(Cabin,$"zone{zone+1:00}-light-after.png");
                    foreach(var light in presentation.lightImages)
                    {Assert.That(light.color.a,Is.EqualTo(authored.LightOpacity));Assert.That(light.texture,Is.Not.Null);}
                }
                finally {presentation.config=authored;Object.Destroy(preview);}
            }
        }
        [UnityTest] public IEnumerator PhotoLabButtonsAndConfirmationFitBothCaptureResolutions()
        {
            var photos=Cabin.GetComponent<PhotoCaptureService>();
            var poi=photos.survey.FindPoi("zone01-left");
            for(int i=0;i<16;i++)
            {
                float radians=i*Mathf.PI/8;
                var position=photos.survey.ContactPosition(poi)-new Vector2(Mathf.Sin(radians),Mathf.Cos(radians))*20;
                if(!Cabin.Navigation.CanOccupy(position))continue;
                Cabin.Navigation.RestoreVoyage(position,i*22.5f,photos.survey.DepthFor(poi),0);
                if(photos.survey.TryGetPhotoContact(Cabin.Navigation,photos.profile.visibleDistance,photos.profile.fieldOfView,out var found,out _)&&found==poi)break;
            }
            var captured=photos.Capture();Assert.That(captured,Is.Not.Null);Assert.That(captured.MissionObjectiveId,Is.Not.Null.And.Not.Empty);
            Cabin.OpenComputer();var computer=Cabin.GetComponentInChildren<ComputerScreenController>(true);computer.OpenPhotoLab();yield return null;
            var lab=computer.GetComponentInChildren<PhotoLabView>(true);
            Assert.That(lab.deleteButton,Is.Not.Null);
            foreach(var resolution in new[]{new Vector2Int(1920,1080),new Vector2Int(1366,768)})
            {
                lab.CancelDelete();yield return CabinNavigationPlayModeTests.CaptureArt(Cabin,$"photolab-delete-{resolution.x}.png",resolution.x,resolution.y);
                lab.RequestDelete();yield return null;Canvas.ForceUpdateCanvases();
                foreach(var label in lab.deleteConfirmation.GetComponentsInChildren<TMP_Text>())
                {label.ForceMeshUpdate();Assert.That(label.isTextOverflowing,Is.False,label.name);}
                yield return CabinNavigationPlayModeTests.CaptureArt(Cabin,$"photolab-confirm-{resolution.x}.png",resolution.x,resolution.y);
            }
        }
    }
}
