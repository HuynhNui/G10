using UnityEngine;
using UnityEngine.InputSystem;

namespace G10.Prototype.UI
{
    public interface IPanelBackHandler
    {
        bool TryHandleBack();
    }

    [DisallowMultipleComponent]
    public sealed class UIManager : MonoBehaviour
    {
        [SerializeField] private GameObject dimBackground;

        private GameObject currentPanel;
        private GameObject modalPanel;
        private GameObject returnPanel;

        public bool IsPanelOpen => currentPanel != null && currentPanel.activeSelf;
        public GameObject CurrentPanel => currentPanel;
        public bool IsModalOpen => modalPanel != null;

        public bool TryOpenModal(GameObject panel)
        {
            if (panel == null || IsModalOpen) return false;
            returnPanel = currentPanel;
            OpenPanel(panel);
            modalPanel = panel;
            return true;
        }

        public void EndModal(GameObject owner, bool restorePrevious = true)
        {
            if (modalPanel == null || modalPanel != owner) return;
            var previous = returnPanel;
            modalPanel = null;
            returnPanel = null;
            CloseCurrentPanel();
            if (restorePrevious && previous != null && (previous.transform.parent == null || previous.transform.parent.gameObject.activeInHierarchy))
                OpenPanel(previous);
        }

        private void Update()
        {
            if (IsPanelOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (currentPanel.GetComponent<IPanelBackHandler>()?.TryHandleBack() != true)
                    CloseCurrentPanel();
            }
        }

        public void OpenPanel(GameObject panel)
        {
            if (IsModalOpen) return;
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
            if (IsModalOpen) return;
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
