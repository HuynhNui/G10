using System.Collections;
using G10.Prototype.Computer;
using G10.Prototype.Core;
using G10.Prototype.Navigation;
using G10.Prototype.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace G10.Prototype.Tests
{
    public sealed class CaptureMinigamePlayModeTests
    {
        private CabinStationView cabin;
        private CreatureCatcher catcher;
        private CaptureMinigameController game;
        private CaptureMinigameProfile testProfile;
        private Random.State randomState;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            randomState = Random.state; Random.InitState(1709);
            yield return SceneManager.LoadSceneAsync("GameplayCore", LoadSceneMode.Single);
            yield return SceneManager.LoadSceneAsync("Zone01", LoadSceneMode.Additive);
            yield return null;
            cabin = Object.FindAnyObjectByType<CabinStationView>();
            catcher = cabin.GetComponent<CreatureCatcher>(); game = catcher.minigame;
            Assert.That(game, Is.Not.Null, "Run the non-destructive capture minigame installer.");
            testProfile = Object.Instantiate(game.profile); game.profile = testProfile;
            cabin.OpenCapture();
        }

        private void Ready()
        {
            PhotoSurveyPlayModeTests.PlaceShip(cabin.Navigation, catcher.survey.center);
            catcher.survey.CompleteTask(PhotoSurveyZone.TaskKind.Photograph);
        }

        [UnityTest]
        public IEnumerator InvalidConditionsKeepExistingReasonsAndNeverOpenMinigame()
        {
            Assert.That(catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.Empty));
            Assert.That(cabin.Panels.IsModalOpen, Is.False);
            PhotoSurveyPlayModeTests.PlaceShip(cabin.Navigation, catcher.survey.center);
            Assert.That(catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.PhotoRequired));
            catcher.survey.CompleteTask(PhotoSurveyZone.TaskKind.Photograph);
            cabin.Navigation.StepDepth(1,10);
            Assert.That(catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.Empty));
            cabin.Navigation.StepDepth(-1,10);
            catcher.survey.creaturePresent = false;
            Assert.That(catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.Empty));
            catcher.survey.creaturePresent = true;
            cabin.Navigation.SetChart(new byte[4],2,2);
            Assert.That(catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.Empty));
            cabin.Navigation.SetChart(new byte[] {1,1,1,1},2,2);
            for (int i=0;i<CreatureInventory.Capacity;i++) catcher.inventory.TryAdd("test-"+i,"Test",catcher.itemIcon);
            Assert.That(catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.Full));
            Assert.That(game.IsActive, Is.False);
            Assert.That(catcher.survey.IsTaskComplete(PhotoSurveyZone.TaskKind.Capture), Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator KeyboardSteersOnlyVerticallyModalBlocksCabinAndEscapeRestoresCapturePanel()
        {
            Ready(); var previous = cabin.Panels.CurrentPanel;
            var ui = previous.GetComponent<CreatureCaptureView>(); ui.Catch();
            Assert.That(game.IsActive, Is.True); Assert.That(cabin.Panels.CurrentPanel, Is.EqualTo(game.view.gameObject));
            Assert.That(catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.Busy));
            Vector2 ship = cabin.Navigation.Position;
            Vector2 initial = game.HookPosition;
            Vector2 fish = game.CreaturePosition;
            // A hidden batch Editor has no Game-view focus. Isolate test input settings
            // so synthetic keys use the same player update path as a focused game.
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
                keyboard.MakeCurrent();
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W)); InputSystem.Update();
                // Exercise the real keyboard reader before batchmode's unfocused device reset.
                Assert.That(keyboard.wKey.isPressed, Is.True);
                game.SendMessage("Update");
                Assert.That(game.HookPosition.x, Is.GreaterThan(initial.x));
                Assert.That(game.HookPosition.y, Is.GreaterThan(initial.y));
                Assert.That(game.CreaturePosition, Is.Not.EqualTo(fish));
                float up = game.HookPosition.y;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.DownArrow)); InputSystem.Update();
                Assert.That(keyboard.downArrowKey.isPressed, Is.True);
                game.SendMessage("Update");
                Assert.That(game.HookPosition.y, Is.LessThan(up));
                InputSystem.QueueStateEvent(keyboard,new KeyboardState()); InputSystem.Update();
                cabin.OpenNavigation(); cabin.OpenMap(); cabin.OpenRadar(); cabin.OpenComputer(); cabin.OpenCamera(); cabin.OpenCargo();
                cabin.Panels.OpenPanel(previous); cabin.Panels.CloseCurrentPanel(); cabin.Scan(); cabin.Hold(1);
                Assert.That(cabin.Panels.CurrentPanel, Is.EqualTo(game.view.gameObject));
                Assert.That(cabin.Navigation.Position, Is.EqualTo(ship));
                Assert.That(cabin.Navigation.Speed, Is.Zero);
                yield return CabinNavigationPlayModeTests.CaptureArt(cabin,"capture-playing.png");
                keyboard.MakeCurrent();
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape)); InputSystem.Update();
                Assert.That(Keyboard.current, Is.SameAs(keyboard));
                Assert.That(keyboard.escapeKey.wasPressedThisFrame, Is.True);
                Assert.That(cabin.Panels.IsPanelOpen, Is.True);
                cabin.Panels.SendMessage("Update");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState()); InputSystem.Update();
                Assert.That(game.State, Is.EqualTo(CaptureMinigameState.Cancelled));
                Assert.That(cabin.Panels.IsModalOpen, Is.False);
                Assert.That(cabin.Panels.CurrentPanel, Is.EqualTo(previous));
                Assert.That(catcher.LastResult, Is.EqualTo(CreatureCatcher.Result.Cancelled));
                AssertUncaptured();
                cabin.OpenNavigation(); Assert.That(cabin.Panels.CurrentPanel, Is.EqualTo(cabin.NavigationPanel));
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                InputSystem.settings = originalSettings;
                Object.Destroy(inputSettings);
            }
        }

        [UnityTest]
        public IEnumerator PassesWrapTimerFailsAndRetryDoesNotMutateEncounter()
        {
            Ready(); testProfile.creatureMoveSpeed = 0; testProfile.horizontalSpeed = 600; testProfile.attemptDuration = 8;
            Assert.That(catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.Started));
            game.Tick(2.2f,0); float beforeWrap = game.HookPosition.x;
            game.Tick(.3f,0);
            Assert.That(game.HookPosition.x, Is.LessThan(beforeWrap));
            Assert.That(game.CurrentHits, Is.Zero); Assert.That(game.State, Is.EqualTo(CaptureMinigameState.Playing));
            game.Tick(5.6f,0);
            Assert.That(game.State, Is.EqualTo(CaptureMinigameState.Failure)); AssertUncaptured();
            yield return CabinNavigationPlayModeTests.CaptureArt(cabin,"capture-failure.png");
            game.Tick(1,0);
            Assert.That(catcher.LastResult, Is.EqualTo(CreatureCatcher.Result.Failed));
            Assert.That(cabin.Panels.IsModalOpen, Is.False);
            Assert.That(catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.Started));
            game.Cancel(); game.Cancel(); AssertUncaptured();
        }

        [UnityTest]
        public IEnumerator FiveSeparateContactsCommitExactlyOnceAndRemoveRadarAndPhotoSubject()
        {
            Ready(); Assert.That(testProfile.requiredHits, Is.EqualTo(5));
            Assert.That(catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.Started));
            DriveUntilHit(game,1);
            Assert.That(game.CurrentHits, Is.EqualTo(1));
            Assert.That(game.view.hitCounter.text, Is.EqualTo("1 / 5"));
            Assert.That(game.view.progressClip.rect.width, Is.EqualTo(game.view.progressWidth / 5).Within(.01f));
            Assert.That(game.view.spark.enabled, Is.True);
            Vector2 hook = game.HookPosition, target = game.CreaturePosition;
            game.Tick(.05f,1);
            Assert.That(game.HookPosition, Is.EqualTo(hook)); Assert.That(game.CurrentHits, Is.EqualTo(1));
            yield return CabinNavigationPlayModeTests.CaptureArt(cabin,"capture-hit.png");
            game.Tick(.2f,0);
            Assert.That(game.CreaturePosition, Is.Not.EqualTo(target));
            Assert.That(game.CreaturePosition.x, Is.GreaterThan(game.HookPosition.x+100));
            AssertUncaptured();
            DriveUntilHit(game,5);
            AssertUncaptured();
            game.Tick(.2f,0);
            Assert.That(game.State, Is.EqualTo(CaptureMinigameState.Success));
            Assert.That(game.view.success.enabled, Is.True);
            yield return CabinNavigationPlayModeTests.CaptureArt(cabin,"capture-success.png");
            game.Tick(1,0);
            Assert.That(catcher.LastResult, Is.EqualTo(CreatureCatcher.Result.Caught));
            Assert.That(catcher.inventory.Items.Count, Is.EqualTo(1));
            Assert.That(catcher.survey.IsTaskComplete(PhotoSurveyZone.TaskKind.Capture), Is.True);
            Assert.That(catcher.survey.creaturePresent, Is.False);
            game.Tick(10,1); game.Cancel();
            Assert.That(catcher.inventory.Items.Count, Is.EqualTo(1));
            Assert.That(catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.Empty));
            cabin.OpenRadar(); cabin.Scan(); yield return new WaitForSeconds(2.1f);
            Assert.That(cabin.Radar.VisibleContactCount, Is.Zero);
            var record = cabin.GetComponent<PhotoCaptureService>().Capture();
            Assert.That(record.Result, Is.EqualTo("NoSubject"));
        }

        [UnityTest]
        public IEnumerator SuccessRevalidatesCapacityAndDisablingModalCancelsCleanly()
        {
            Ready(); Assert.That(catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.Started));
            game.view.gameObject.SetActive(false);
            Assert.That(catcher.LastResult, Is.EqualTo(CreatureCatcher.Result.Cancelled));
            Assert.That(cabin.Panels.IsModalOpen, Is.False); AssertUncaptured();
            Assert.That(catcher.TryCapture(), Is.EqualTo(CreatureCatcher.Result.Started));
            for (int i=0;i<CreatureInventory.Capacity;i++) catcher.inventory.TryAdd("test-"+i,"Test",catcher.itemIcon);
            Win(game);
            Assert.That(catcher.LastResult, Is.EqualTo(CreatureCatcher.Result.Full));
            Assert.That(catcher.inventory.Items.Count, Is.EqualTo(CreatureInventory.Capacity));
            Assert.That(catcher.survey.creaturePresent, Is.True);
            Assert.That(catcher.survey.IsTaskComplete(PhotoSurveyZone.TaskKind.Capture), Is.False);
            yield return null;
        }

        private void AssertUncaptured()
        {
            Assert.That(catcher.inventory.Items.Count, Is.Zero);
            Assert.That(catcher.survey.creaturePresent, Is.True);
            Assert.That(catcher.survey.IsTaskComplete(PhotoSurveyZone.TaskKind.Capture), Is.False);
        }

        private static void DriveUntilHit(CaptureMinigameController controller, int count)
        {
            for (int i=0;i<10000 && controller.IsActive && controller.CurrentHits<count;i++)
            {
                float delta = controller.CreaturePosition.y-controller.HookPosition.y;
                controller.Tick(.01f, Mathf.Clamp(delta / (controller.profile.verticalSpeed*.01f),-1,1));
            }
            Assert.That(controller.CurrentHits, Is.EqualTo(count), "A steering player should be able to intercept the moving target before timeout.");
        }

        internal static void Win(CaptureMinigameController controller)
        {
            DriveUntilHit(controller,controller.profile.requiredHits);
            controller.Tick(controller.profile.hitPause+controller.profile.resultDuration+.1f,0);
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (game != null) game.Cancel();
            if (testProfile != null) Object.Destroy(testProfile);
            if (SceneFlowController.Instance != null) Object.Destroy(SceneFlowController.Instance.gameObject);
            Random.state = randomState;
            yield return null;
        }
    }
}
