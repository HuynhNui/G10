using UnityEngine;
using UnityEngine.EventSystems;

namespace G10.Prototype.Dialogue
{
    public sealed class DialogueButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        private bool hovered;
        private bool selected;
        private void Rest() => transform.localScale = Vector3.one * (hovered || selected ? 1.03f : 1);
        public void OnPointerEnter(PointerEventData e) { hovered = true; Rest(); }
        public void OnPointerExit(PointerEventData e) { hovered = false; Rest(); }
        public void OnPointerDown(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) transform.localScale = Vector3.one * .96f; }
        public void OnPointerUp(PointerEventData e) => Rest();
        public void OnSelect(BaseEventData e) { selected = true; Rest(); }
        public void OnDeselect(BaseEventData e) { selected = false; Rest(); }
        private void OnDisable() { hovered = selected = false; transform.localScale = Vector3.one; }
    }
}
