using System.Collections;
using System;
using System.IO;
using G10.Prototype.Computer;
using G10.Prototype.Core;
using G10.Prototype.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace G10.Prototype.Tests
{
    public sealed class ProjectSceneFlowPlayModeTests
    {
        private string folder;
        [SetUp] public void IsolateSave()
        {
            folder = Path.Combine(Application.temporaryCachePath, "SceneFlow-" + Guid.NewGuid().ToString("N"));
            ExpeditionSaveStore.PathOverride = Path.Combine(folder, "timeline.json");
            PhotoCaptureService.ArchivePathOverride = Path.Combine(folder, "photos");
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            if (SceneFlowController.Instance != null)
            {
                Object.Destroy(SceneFlowController.Instance.gameObject);
                yield return null;
            }
            ExpeditionSaveStore.PathOverride = null;
            PhotoCaptureService.ArchivePathOverride = null;
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }

        [UnityTest]
        public IEnumerator BootstrapLoadsMenuThenStartsGameplayWithZone01()
        {
            yield return SceneManager.LoadSceneAsync(SceneFlowController.BootstrapScene, LoadSceneMode.Single);
            yield return WaitForScene(SceneFlowController.MainMenuScene);

            SceneFlowController sceneFlow = SceneFlowController.Instance;
            Assert.That(sceneFlow, Is.Not.Null);
            yield return WaitForTransition(sceneFlow);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(SceneFlowController.MainMenuScene));
            Assert.That(ActiveEventSystemCount(), Is.EqualTo(1), "MainMenu must own the only active EventSystem.");

            sceneFlow.StartNewGame();
            yield return WaitForTransition(sceneFlow);

            Assert.That(SceneManager.GetSceneByName(SceneFlowController.GameplayCoreScene).isLoaded, Is.True);
            Assert.That(SceneManager.GetSceneByName("Zone01").isLoaded, Is.True);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Zone01"));
            Assert.That(Object.FindAnyObjectByType<PointAndClickInputController>(), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<UIManager>(), Is.Not.Null);
            Assert.That(GameObject.Find("Player"), Is.Null);
            Assert.That(ActiveEventSystemCount(), Is.EqualTo(1), "GameplayCore must own the only active EventSystem.");
        }

        [UnityTest]
        public IEnumerator LockedZoneCannotBeReachedByCallingSceneNavigation()
        {
            yield return SceneManager.LoadSceneAsync(SceneFlowController.BootstrapScene, LoadSceneMode.Single);
            yield return WaitForScene(SceneFlowController.MainMenuScene);

            SceneFlowController sceneFlow = SceneFlowController.Instance;
            Assert.That(sceneFlow, Is.Not.Null);
            yield return WaitForTransition(sceneFlow);
            sceneFlow.StartNewGame();
            yield return WaitForTransition(sceneFlow);

            sceneFlow.LoadZone("Zone02");
            yield return WaitForTransition(sceneFlow);

            Assert.That(SceneManager.GetSceneByName(SceneFlowController.GameplayCoreScene).isLoaded, Is.True);
            Assert.That(SceneManager.GetSceneByName("Zone01").isLoaded, Is.True);
            Assert.That(SceneManager.GetSceneByName("Zone02").isLoaded, Is.False);
            Assert.That(sceneFlow.CurrentZoneScene, Is.EqualTo("Zone01"));
        }

        private static IEnumerator WaitForScene(string sceneName)
        {
            float deadline = Time.realtimeSinceStartup + 10f;
            while (Time.realtimeSinceStartup < deadline)
            {
                if (SceneManager.GetSceneByName(sceneName).isLoaded)
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail($"Timed out waiting for scene: {sceneName}");
        }

        private static IEnumerator WaitForTransition(SceneFlowController sceneFlow)
        {
            yield return null;
            float deadline = Time.realtimeSinceStartup + 10f;
            while (Time.realtimeSinceStartup < deadline)
            {
                if (!sceneFlow.IsTransitioning)
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail("Timed out waiting for scene transition.");
        }

        private static int ActiveEventSystemCount()
            => Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Exclude).Length;
    }
}
