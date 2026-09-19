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
        public G10.Prototype.Computer.ExpeditionLoop Expedition { get; private set; }
        public GameObject LockedPanel { get; set; }
        private void Awake()
        {
            Expedition = GetComponent<G10.Prototype.Computer.ExpeditionLoop>();
            if (Expedition == null) Expedition = gameObject.AddComponent<G10.Prototype.Computer.ExpeditionLoop>();
        }

        public bool IsPanelOpen => currentPanel != null && currentPanel.activeSelf;
        public GameObject CurrentPanel => currentPanel;
        public bool IsModalOpen => modalPanel != null;

        public bool TryOpenModal(GameObject panel)
        {
            if (panel == null || IsModalOpen || LockedPanel != null) return false;
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
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (PauseMenuController.Instance != null && PauseMenuController.Instance.IsPaused)
                {
                    PauseMenuController.Instance.Resume();
                    return;
                }

                if (IsPanelOpen)
                {
                    if (currentPanel.GetComponent<IPanelBackHandler>()?.TryHandleBack() != true)
                        CloseCurrentPanel();
                    return;
                }

                if (PauseMenuController.Instance != null)
                {
                    PauseMenuController.Instance.OpenPause();
                }
            }
        }

        public void OpenPanel(GameObject panel)
        {
            if (LockedPanel != null && panel != LockedPanel) return;
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
            if (LockedPanel != null) return;
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
