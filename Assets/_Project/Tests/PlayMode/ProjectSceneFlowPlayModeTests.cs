using System.Collections;
using G10.Prototype.Core;
using G10.Prototype.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace G10.Prototype.Tests
{
    public sealed class ProjectSceneFlowPlayModeTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (SceneFlowController.Instance != null)
            {
                Object.Destroy(SceneFlowController.Instance.gameObject);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator BootstrapLoadsMenuThenStartsGameplayWithZone01()
        {
            yield return SceneManager.LoadSceneAsync(SceneFlowController.BootstrapScene, LoadSceneMode.Single);
            yield return WaitForScene(SceneFlowController.MainMenuScene);

            SceneFlowController sceneFlow = SceneFlowController.Instance;
            Assert.That(sceneFlow, Is.Not.Null);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(SceneFlowController.MainMenuScene));

            sceneFlow.StartNewGame();
            yield return WaitForTransition(sceneFlow);

            Assert.That(SceneManager.GetSceneByName(SceneFlowController.GameplayCoreScene).isLoaded, Is.True);
            Assert.That(SceneManager.GetSceneByName("Zone01").isLoaded, Is.True);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Zone01"));
            Assert.That(Object.FindAnyObjectByType<PointAndClickInputController>(), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<UIManager>(), Is.Not.Null);
            Assert.That(GameObject.Find("Player"), Is.Null);
        }

        [UnityTest]
        public IEnumerator ZoneTransitionKeepsCoreAndUnloadsPreviousZone()
        {
            yield return SceneManager.LoadSceneAsync(SceneFlowController.BootstrapScene, LoadSceneMode.Single);
            yield return WaitForScene(SceneFlowController.MainMenuScene);

            SceneFlowController sceneFlow = SceneFlowController.Instance;
            Assert.That(sceneFlow, Is.Not.Null);
            sceneFlow.StartNewGame();
            yield return WaitForTransition(sceneFlow);

            sceneFlow.LoadZone("Zone02");
            yield return WaitForTransition(sceneFlow);

            Assert.That(SceneManager.GetSceneByName(SceneFlowController.GameplayCoreScene).isLoaded, Is.True);
            Assert.That(SceneManager.GetSceneByName("Zone01").isLoaded, Is.False);
            Assert.That(SceneManager.GetSceneByName("Zone02").isLoaded, Is.True);
            Assert.That(sceneFlow.CurrentZoneScene, Is.EqualTo("Zone02"));
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
    }
}
