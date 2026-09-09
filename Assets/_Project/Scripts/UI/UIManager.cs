using UnityEngine;
using UnityEngine.InputSystem;

namespace G10.Prototype.UI
{
    [DisallowMultipleComponent]
    public sealed class UIManager : MonoBehaviour
    {
        [SerializeField] private GameObject dimBackground;

        private GameObject currentPanel;

        public bool IsPanelOpen => currentPanel != null && currentPanel.activeSelf;
        public GameObject CurrentPanel => currentPanel;

        private void Update()
        {
            if (IsPanelOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseCurrentPanel();
            }
        }

        public void OpenPanel(GameObject panel)
        {
            if (panel == null)
            {
                Debug.LogWarning("Cannot open a null interaction panel.", this);
                return;
            }

            if (currentPanel != null && currentPanel != panel)
            {
                currentPanel.SetActive(false);
            }

            currentPanel = panel;
            currentPanel.SetActive(true);

            if (dimBackground != null)
            {
                dimBackground.SetActive(true);
            }
        }

        public void CloseCurrentPanel()
        {
            if (currentPanel != null)
            {
                currentPanel.SetActive(false);
                currentPanel = null;
            }

            if (dimBackground != null)
            {
                dimBackground.SetActive(false);
            }
        }
    }
}
