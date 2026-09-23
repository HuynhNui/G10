using UnityEngine;
using UnityEngine.EventSystems;

namespace G10.Prototype.Computer
{
    /// <summary>Pointer coordinates are converted through the Canvas, including scaled Game views.</summary>
    public sealed class ComputerWindowHandle : MonoBehaviour, IPointerDownHandler, IPointerClickHandler, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler
    {
        public ComputerWindow window;
        public bool move;
        public int edges; // Left=1, right=2, top=4, bottom=8. Zero without move is focus-only.
        private Vector2 startPointer, startPosition, startSize;
        public void OnInitializePotentialDrag(PointerEventData e) { if (move || edges != 0) e.useDragThreshold = false; }
        public void OnPointerDown(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) window.Focus(); }
        public void OnPointerClick(PointerEventData e)
        { if (move && e.button == PointerEventData.InputButton.Left && e.clickCount == 2) window.ToggleMaximize(); }
        public void OnBeginDrag(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left || !move && edges == 0) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)window.Rect.parent, e.pressPosition, e.pressEventCamera, out startPointer);
            if (move && window.Maximized)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(window.Rect, e.pressPosition, e.pressEventCamera, out var local);
                float fraction = Mathf.Clamp01((local.x - window.Rect.rect.xMin) / window.Rect.rect.width);
                float fromTop = Mathf.Clamp(window.Rect.rect.yMax - local.y, 0, 100);
                window.ToggleMaximize();
                var parentRect = ((RectTransform)window.Rect.parent).rect;
                window.SetBounds(new Vector2(startPointer.x - parentRect.xMin - fraction * window.Rect.sizeDelta.x,
                    startPointer.y - parentRect.yMax + fromTop), window.Rect.sizeDelta);
            }
            startPosition = window.Rect.anchoredPosition; startSize = window.Rect.sizeDelta;
        }
        public void OnDrag(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left || window.Maximized || !move && edges == 0) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)window.Rect.parent, e.position, e.pressEventCamera, out var point)) return;
            var delta = point - startPointer;
            if (move)
            {
                window.SetBounds(startPosition + delta, startSize);
                startPointer = point; startPosition = window.Rect.anchoredPosition;
                return;
            }
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
