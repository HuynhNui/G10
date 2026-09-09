using UnityEngine;
using UnityEngine.EventSystems;

namespace G10.Prototype.UI
{
    public sealed class CabinPointerTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, IPointerMoveHandler
    {
        [SerializeField] private CabinStationView view;
        [SerializeField] private string hint;
        [SerializeField] private int heldCommand;
        [SerializeField] private bool chart;

        public void Configure(CabinStationView owner, string tooltip, int command = 0, bool isChart = false)
        { view = owner; hint = tooltip; heldCommand = command; chart = isChart; }

        public void OnPointerEnter(PointerEventData eventData)
        { if (!chart) view.SetHover(hint); else OnPointerMove(eventData); }
        public void OnPointerExit(PointerEventData eventData)
        {
            view.SetHover("");
            if (heldCommand != 0) view.Release(heldCommand);
            if (chart) view.ClearChartCoordinate();
        }
        public void OnPointerDown(PointerEventData eventData)
        { if (heldCommand != 0 && eventData.button == PointerEventData.InputButton.Left) view.Hold(heldCommand); }
        public void OnPointerUp(PointerEventData eventData)
        { if (heldCommand != 0) view.Release(heldCommand); }
        public void OnPointerMove(PointerEventData eventData)
        {
            if (!chart) return;
            RectTransform rect = (RectTransform)transform;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.enterEventCamera, out Vector2 local))
                view.ShowChartCoordinate(new Vector2((local.x - rect.rect.xMin) / rect.rect.width, (local.y - rect.rect.yMin) / rect.rect.height));
        }
        private void OnDisable()
        { if (view != null && heldCommand != 0) view.Release(heldCommand); }
    }
}
