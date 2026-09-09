using G10.Prototype.Interaction;
using G10.Prototype.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace G10.Prototype.Core
{
    [DisallowMultipleComponent]
    public sealed class PointAndClickInputController : MonoBehaviour
    {
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private LayerMask interactableMask = ~0;

        private void Update()
        {
            if (uiManager != null && uiManager.IsPanelOpen)
            {
                return;
            }

            Pointer pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame)
            {
                return;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            Camera cameraToUse = gameplayCamera != null ? gameplayCamera : Camera.main;
            if (cameraToUse == null)
            {
                return;
            }

            Vector2 screenPosition = pointer.position.ReadValue();
            Vector3 worldPoint = cameraToUse.ScreenToWorldPoint(
                new Vector3(screenPosition.x, screenPosition.y, -cameraToUse.transform.position.z));

            Collider2D[] hits = Physics2D.OverlapPointAll(worldPoint, interactableMask);
            foreach (Collider2D hit in hits)
            {
                Interactable interactable = hit.GetComponentInParent<Interactable>();
                if (interactable != null && interactable.Interact(uiManager))
                {
                    return;
                }
            }
        }
    }
}
