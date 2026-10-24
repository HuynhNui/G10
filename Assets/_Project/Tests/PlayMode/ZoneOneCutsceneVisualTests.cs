using System;
using System.Collections;
using System.IO;
using G10.Prototype.Computer;
using G10.Prototype.Feedback;
using G10.Prototype.Missions;
using G10.Prototype.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace G10.Prototype.Tests
{
    public sealed class ZoneOneCutsceneVisualTests
    {
        private string folder, oldSave, oldPhotos;
        private int shakePreference;
        private CabinStationView cabin;
        private GameObject owner;
        [UnitySetUp] public IEnumerator Setup()
        {
            oldSave=ExpeditionSaveStore.PathOverride;oldPhotos=PhotoCaptureService.ArchivePathOverride;
            folder=Path.Combine(Application.temporaryCachePath,"ZoneOneVisual-"+Guid.NewGuid().ToString("N"));
            ExpeditionSaveStore.PathOverride=Path.Combine(folder,"timeline.json");PhotoCaptureService.ArchivePathOverride=Path.Combine(folder,"photos");
            TutorialTestSave.SeedReturningPlayer();
            shakePreference=PlayerPrefs.GetInt(ImpactShake.PreferenceKey,-1);ImpactShake.SetScreenShakeEnabled(true);
            yield return SceneManager.LoadSceneAsync("GameplayCore",LoadSceneMode.Single);
            yield return SceneManager.LoadSceneAsync("Zone01",LoadSceneMode.Additive);yield return null;yield return null;
            cabin=Object.FindAnyObjectByType<CabinStationView>();owner=new GameObject("Cutscene visual test owner");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(owner!=null)Object.Destroy(owner);
            yield return SceneManager.LoadSceneAsync("MainMenu",LoadSceneMode.Single);
            if(shakePreference<0)PlayerPrefs.DeleteKey(ImpactShake.PreferenceKey);else PlayerPrefs.SetInt(ImpactShake.PreferenceKey,shakePreference);
            PlayerPrefs.Save();ExpeditionSaveStore.PathOverride=oldSave;PhotoCaptureService.ArchivePathOverride=oldPhotos;
            if(Directory.Exists(folder))Directory.Delete(folder,true);
        }
        [UnityTest] public IEnumerator KeyframesShowRealScreenDistortionImpactBlackoutAndReboot()
        {
            var material=cabin.GetComponent<EffectDirector>().Surface(EffectTarget.Computer).visual.signalTemplate;
            var view=ZoneOneOpeningView.Create(owner.transform,material);
            view.Render(.15f);int partial=view.VisibleCharacters;view.Render(3.5f);
            Assert.That(view.VisibleCharacters,Is.GreaterThan(partial));Assert.That(view.BodyText,Does.Contain("REQUIRED SPECIMENS: 09"));
            yield return Capture("01-boot",view);
            view.Render(8);Assert.That(view.Phase,Is.EqualTo(ZoneOneOpeningPhase.WaterContact));yield return Capture("02-water-contact",view);
            view.Render(16);yield return new WaitForEndOfFrame();yield return null;
            Assert.That(view.DistortionVisible,Is.True,"The supplied display shader must actually render a captured terminal frame.");
            yield return Capture("03-instability",view);
            view.Render(18.03f);Assert.That(view.ImpactActive,Is.True);yield return Capture("04-impact",view);
            view.Render(20.5f);Assert.That(view.TerminalOpacity,Is.Zero);Assert.That(view.ScreenOpacity,Is.EqualTo(1));
            Assert.That(view.DistortionVisible,Is.False);yield return Capture("05-blackout",view,true);
            view.Render(24.8f);Assert.That(view.BodyText,Does.Contain("NAVIGATION: ONLINE"));yield return Capture("06-reboot",view);
            view.Render(26.8f);Assert.That(view.BodyText,Does.Contain("EXTERNAL LIGHT: OFFLINE").And.Contain("DEPTH SYSTEM: LIMITED"));
            yield return Capture("07-diagnostics",view);
            view.Render(29);Assert.That(view.ScreenOpacity,Is.InRange(.1f,.8f));Assert.That(view.TerminalOpacity,Is.Zero);yield return Capture("08-cabin-reveal",view);
            view.Render(35);Assert.That(view.ScreenOpacity,Is.Zero);
            ImpactShake.SetScreenShakeEnabled(false);view.Render(0);view.Render(18.03f);Assert.That(view.ImpactActive,Is.False,"Accessibility setting applies to the visible terminal too.");
        }
        [UnityTest] public IEnumerator BioLampReadoutUsesCommittedGameplayAndDoesNotTakeWindowControl()
        {
            var story=cabin.GetComponent<ZoneOneStory>();
            story.RecordObjective("zone01-left",MissionObjectiveType.Photograph,"Z1_Creature_01");
            story.ResolveCapture("zone01-left",story.FindObjective("zone01-left",MissionObjectiveType.Capture));
            story.RecordObjective("zone01-north",MissionObjectiveType.Photograph,"Z1_Creature_02");
            cabin.OpenComputer();var computer=cabin.GetComponentInChildren<ComputerScreenController>(true);computer.OpenUpgrade();
            var view=computer.GetComponentInChildren<ZoneOneBioLampView>(true);Assert.That(view,Is.Not.Null);
            Assert.That(story.InstallBioLamp(),Is.True);yield return new WaitForSeconds(.25f);
            Assert.That(view.IsVisible,Is.True);Assert.That(view.message.text,Does.Contain("LOCKING MODULE"));
            Assert.That(view.card.blocksRaycasts,Is.False);Assert.That(cabin.Panels.IsModalOpen,Is.False);
            computer.OpenPhotoLab();yield return null;Assert.That(view.IsVisible,Is.False);Assert.That(computer.CurrentApp,Is.EqualTo(ComputerAppId.PhotoLab));
            computer.OpenUpgrade();yield return new WaitForSeconds(4.1f);
            Assert.That(view.IsVisible,Is.True);Assert.That(view.message.text,Does.Contain("INSTALLED\nONLINE"));Assert.That(view.fill.fillAmount,Is.EqualTo(1));
            view.message.ForceMeshUpdate();Assert.That(view.message.isTextOverflowing,Is.False);
            yield return Capture("09-biolamp-online",null);
            yield return new WaitForSeconds(2);Assert.That(view.IsVisible,Is.False);
        }
        private static IEnumerator Capture(string name,ZoneOneOpeningView view,bool black=false)
        {
            Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();
            var frame=ScreenCapture.CaptureScreenshotAsTexture();Assert.That(frame,Is.Not.Null);
            try
            {
                if(black)
                    foreach(var uv in new[]{new Vector2(.05f,.05f),new Vector2(.5f,.5f),new Vector2(.95f,.95f)})
                    {var pixel=frame.GetPixelBilinear(uv.x,uv.y);Assert.That(Mathf.Max(pixel.r,Mathf.Max(pixel.g,pixel.b)),Is.LessThan(.015f));}
                if(view!=null&&view.TerminalOpacity>0&&!view.DistortionVisible)
                    foreach(var text in view.GetComponentsInChildren<TMP_Text>())
                    {text.ForceMeshUpdate();Assert.That(text.isTextOverflowing,Is.False,text.name);}
                string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../Docs/QA/Zone01Cutscene"));Directory.CreateDirectory(output);
                File.WriteAllBytes(Path.Combine(output,name+".png"),frame.EncodeToPNG());
            }
            finally{Object.Destroy(frame);}
        }
    }
}
