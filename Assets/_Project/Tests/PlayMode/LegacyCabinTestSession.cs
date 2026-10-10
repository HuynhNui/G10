using System;
using System.Collections;
using System.IO;
using G10.Prototype.Computer;
using G10.Prototype.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace G10.Prototype.Tests
{
    /// <summary>Isolation for legacy presentation/input fixtures, never the player's timeline.</summary>
    internal sealed class LegacyCabinTestSession
    {
        private string folder, oldSave, oldPhotos;
        public IEnumerator Begin()
        {
            folder=Path.Combine(Application.temporaryCachePath,"CabinFixture-"+Guid.NewGuid().ToString("N"));
            oldSave=ExpeditionSaveStore.PathOverride;oldPhotos=PhotoCaptureService.ArchivePathOverride;
            ExpeditionSaveStore.PathOverride=Path.Combine(folder,"save.json");PhotoCaptureService.ArchivePathOverride=Path.Combine(folder,"photos");
            TutorialTestSave.SeedReturningPlayer();
            if(SceneFlowController.Instance!=null){Object.Destroy(SceneFlowController.Instance.gameObject);yield return null;}
            yield return SceneManager.LoadSceneAsync("GameplayCore",LoadSceneMode.Single);
            yield return SceneManager.LoadSceneAsync("Zone01",LoadSceneMode.Additive);
            yield return null;
            float end=Time.realtimeSinceStartup+10;
            var loop=Object.FindAnyObjectByType<ExpeditionLoop>();
            while((loop==null||!loop.IsInitialized)&&Time.realtimeSinceStartup<end)
            {yield return null;loop=Object.FindAnyObjectByType<ExpeditionLoop>();}
            Assert.That(loop,Is.Not.Null);Assert.That(loop.IsInitialized,Is.True,loop.LastError);
        }
        public IEnumerator End()
        {
            if(folder==null)yield break;
            yield return SceneManager.LoadSceneAsync("MainMenu",LoadSceneMode.Single);
            if(SceneFlowController.Instance!=null)Object.Destroy(SceneFlowController.Instance.gameObject);
            yield return null;
            ExpeditionSaveStore.PathOverride=oldSave;PhotoCaptureService.ArchivePathOverride=oldPhotos;
            if(Directory.Exists(folder))Directory.Delete(folder,true);
        }
    }
}
