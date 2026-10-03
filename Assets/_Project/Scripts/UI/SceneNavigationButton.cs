using G10.Prototype.Audio;
using G10.Prototype.Core;
using UnityEngine;

namespace G10.Prototype.UI
{
    public enum SceneNavigationAction
    {
        StartGame,
        MainMenu,
        Zone,
        Ending,
        Quit,
        ContinueGame
    }

    [DisallowMultipleComponent]
    public sealed class SceneNavigationButton : MonoBehaviour
    {
        [SerializeField] private SceneNavigationAction action;
        [SerializeField] private string zoneSceneName;

        private void Start()
        {
            if (action != SceneNavigationAction.StartGame || gameObject.scene.name != SceneFlowController.MainMenuScene) return;
            SetLabel(gameObject, "NEW GAME");
            if (transform.parent.Find("ContinueButton") != null) return;
            var copy = Instantiate(gameObject, transform.parent, false);
            copy.name = "ContinueButton";
            var navigation = copy.GetComponent<SceneNavigationButton>();
            navigation.action = SceneNavigationAction.ContinueGame;
            var rect = (RectTransform)copy.transform;
            rect.anchoredPosition = ((RectTransform)transform).anchoredPosition + Vector2.down * 90;
            SetLabel(copy, "CONTINUE");
            var button = copy.GetComponent<UnityEngine.UI.Button>();
            button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            button.onClick.AddListener(navigation.Navigate);
            button.interactable = G10.Prototype.Computer.ExpeditionSaveStore.TryRead(out var save, out _) && save != null;
            foreach (var other in transform.parent.GetComponentsInChildren<SceneNavigationButton>())
                if (other.action == SceneNavigationAction.Quit && other.transform is RectTransform quit)
                    quit.anchoredPosition = rect.anchoredPosition + Vector2.down * 90;
        }

        private static void SetLabel(GameObject target, string value)
        {
            foreach (var label in target.GetComponentsInChildren<UnityEngine.UI.Text>(true)) label.text = value;
            foreach (var label in target.GetComponentsInChildren<TMPro.TMP_Text>(true)) label.text = value;
        }

        public void Navigate()
        {
            AudioManager.Instance?.PlayButtonClick();
            SceneFlowController sceneFlow = SceneFlowController.Instance;
            if (sceneFlow == null)
            {
                Debug.LogError("Scene navigation requires the game to start from Bootstrap.", this);
                return;
            }

            switch (action)
            {
                case SceneNavigationAction.ContinueGame:
                    sceneFlow.ContinueGame();
                    break;
                case SceneNavigationAction.StartGame:
                    sceneFlow.StartNewGame();
                    break;
                case SceneNavigationAction.MainMenu:
                    sceneFlow.LoadMainMenu();
                    break;
                case SceneNavigationAction.Zone:
                    sceneFlow.LoadZone(zoneSceneName);
                    break;
                case SceneNavigationAction.Ending:
                    sceneFlow.LoadEnding();
                    break;
                case SceneNavigationAction.Quit:
                    sceneFlow.QuitGame();
                    break;
                default:
                    Debug.LogError($"Unsupported scene navigation action: {action}", this);
                    break;
            }
        }
    }
}
