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
        Quit
    }

    [DisallowMultipleComponent]
    public sealed class SceneNavigationButton : MonoBehaviour
    {
        [SerializeField] private SceneNavigationAction action;
        [SerializeField] private string zoneSceneName;

        public void Navigate()
        {
            SceneFlowController sceneFlow = SceneFlowController.Instance;
            if (sceneFlow == null)
            {
                Debug.LogError("Scene navigation requires the game to start from Bootstrap.", this);
                return;
            }

            switch (action)
            {
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
