using G10.Prototype.UI;
using UnityEngine;

namespace G10.Prototype.Interaction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class Interactable : MonoBehaviour
    {
        [SerializeField] private string displayName;
        [SerializeField] private GameObject targetPanel;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private bool interactionEnabled = true;

        public string DisplayName => displayName;
        public GameObject TargetPanel => targetPanel;
        public bool InteractionEnabled => interactionEnabled;

        public bool Interact()
        {
            return Interact(uiManager);
        }

        public bool Interact(UIManager fallbackUIManager)
        {
            UIManager manager = uiManager != null ? uiManager : fallbackUIManager;
            if (!interactionEnabled || targetPanel == null || manager == null)
            {
                return false;
            }

            manager.OpenPanel(targetPanel);
            return true;
        }
    }
}
