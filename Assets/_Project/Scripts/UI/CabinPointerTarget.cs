using G10.Prototype.Audio;
using UnityEngine;
using UnityEngine.EventSystems;

namespace G10.Prototype.UI
{
    public sealed class CabinPointerTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, IPointerMoveHandler, IPointerClickHandler
    {
        [SerializeField] private CabinStationView view;
        [SerializeField] private string hint;
        [SerializeField] private int heldCommand;
        [SerializeField] private bool chart;
        private PhotoSurveyMap chartOverlay;
        private void Awake() { if (chart) chartOverlay = GetComponentInChildren<PhotoSurveyMap>(true); }

        private void EnsureView()
        {
            if (view == null) view = GetComponentInParent<CabinStationView>() ?? FindAnyObjectByType<CabinStationView>();
        }

        public void Configure(CabinStationView owner, string tooltip, int command = 0, bool isChart = false)
        { view = owner; hint = tooltip; heldCommand = command; chart = isChart; }

        public void OnPointerEnter(PointerEventData eventData)
        {
            EnsureView();
            if (view != null)
            {
                if (!chart) view.SetHover(hint);
                else OnPointerMove(eventData);
            }
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            EnsureView();
            if (view != null)
            {
                view.SetHover("");
                if (heldCommand != 0) view.Release(heldCommand);
                if (chart) { view.ClearChartCoordinate(); chartOverlay?.SetPointer(null); }
            }
        }
        public void OnPointerDown(PointerEventData eventData)
        {
            EnsureView();
            if (view != null && heldCommand != 0 && eventData.button == PointerEventData.InputButton.Left)
                view.Hold(heldCommand);
        }
        public void OnPointerUp(PointerEventData eventData)
        {
            EnsureView();
            if (view != null && heldCommand != 0)
                view.Release(heldCommand);
        }
        public void OnPointerClick(PointerEventData eventData)
        {
            if (chart && eventData.button == PointerEventData.InputButton.Left)
            {
                OnPointerMove(eventData);
                chartOverlay?.SelectHoveredLocation();
                AudioManager.Instance?.PlayItemClick();
            }
        }
        public void OnPointerMove(PointerEventData eventData)
        {
            if (!chart) return;
            EnsureView();
            RectTransform rect = (RectTransform)transform;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.enterEventCamera, out Vector2 local))
            {
                Vector2 uv = new((local.x - rect.rect.xMin) / rect.rect.width, (local.y - rect.rect.yMin) / rect.rect.height);
                if (view != null) view.ShowChartCoordinate(uv);
                chartOverlay?.SetPointer(uv);
            }
        }
        private void OnDisable()
        { if (view != null && heldCommand != 0) view.Release(heldCommand); }
    }
}
