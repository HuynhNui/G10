using UnityEngine;
using UnityEngine.EventSystems;

namespace G10.Prototype.Computer
{
    /// <summary>Pointer coordinates are converted through the Canvas, including scaled Game views.</summary>
    public sealed class ComputerWindowHandle : MonoBehaviour, IPointerDownHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler
    {
        public ComputerWindow window;
        public bool move;
        public int edges; // Left=1, right=2, top=4, bottom=8. Zero without move is focus-only.
        private Vector2 startPointer, startPosition, startSize;
        public void OnPointerDown(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) window.Focus(); }
        public void OnPointerClick(PointerEventData e)
        { if (move && e.button == PointerEventData.InputButton.Left && e.clickCount == 2) window.ToggleMaximize(); }
        public void OnBeginDrag(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left || !move && edges == 0) return;
            if (move && window.Maximized) window.ToggleMaximize();
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)window.Rect.parent, e.position, e.pressEventCamera, out startPointer);
            startPosition = window.Rect.anchoredPosition; startSize = window.Rect.sizeDelta;
        }
        public void OnDrag(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left || window.Maximized || !move && edges == 0) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)window.Rect.parent, e.position, e.pressEventCamera, out var point)) return;
            var delta = point - startPointer;
            if (move) { window.SetBounds(startPosition + delta, startSize); return; }
            float left = startPosition.x, top = -startPosition.y, right = left + startSize.x, bottom = top + startSize.y;
            var min = ComputerWindow.MinimumSize; var area = window.Workspace;
            if ((edges & 1) != 0) left = Mathf.Clamp(left + delta.x, 0, right - min.x);
            if ((edges & 2) != 0) right = Mathf.Clamp(right + delta.x, left + min.x, area.x);
            if ((edges & 4) != 0) top = Mathf.Clamp(top - delta.y, 0, bottom - min.y);
            if ((edges & 8) != 0) bottom = Mathf.Clamp(bottom - delta.y, top + min.y, area.y);
            window.SetBounds(new Vector2(left, -top), new Vector2(right - left, bottom - top));
        }
    }
}
