using G10.Prototype.Computer;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace G10.Prototype.Feedback
{
    /// <summary>Bounded non-interactive presentation inside the actual Mission Log viewport.</summary>
    public sealed class ZoneFourFragmentView : MonoBehaviour
    {
        [SerializeField] private MissionLogView log;
        private RectTransform card;
        private CanvasGroup group;
        private TMP_Text heading, first, second, status;
        private Image progress;
        private Graphic bodyGraphic;
        private bool bodyHidden, bodyWasEnabled;
        private string firstCoordinate, secondCoordinate;
        public bool Visible => card != null && card.gameObject.activeInHierarchy;
        public string FirstCoordinateText => first != null ? first.text : "";
        public string SecondCoordinateText => second != null ? second.text : "";
        public float Progress { get; private set; }
        public void Bind(MissionLogView value) => log = value;
        public void Coordinates(string a, string b) { firstCoordinate = a; secondCoordinate = b; }

        private void Build()
        {
            if (card != null || log == null || log.Viewport == null) return;
            card = NewRect("RecoveredCoordinateFragment", log.Viewport);
            card.anchorMin = new Vector2(0, 1); card.anchorMax = Vector2.one; card.pivot = new Vector2(.5f, 1);
            card.offsetMin = new Vector2(16, -250); card.offsetMax = new Vector2(-16, -14);
            var background = card.gameObject.AddComponent<Image>();
            background.color = new Color(.065f, .15f, .23f, 1); background.raycastTarget = false;
            var edge = card.gameObject.AddComponent<Outline>(); edge.effectColor = new Color(.3f, .55f, .63f, .8f);
            edge.effectDistance = new Vector2(1, -1);
            group = card.gameObject.AddComponent<CanvasGroup>(); group.blocksRaycasts = group.interactable = false;
            heading = Label("Heading", 20, 16, 34, 22, new Color(.72f, .87f, .88f));
            status = Label("Status", 20, 54, 30, 17, new Color(.48f, .68f, .72f));
            first = Label("CoordinateA", 20, 96, 32, 23, new Color(.78f, .91f, .87f));
            second = Label("CoordinateB", 20, 139, 32, 23, new Color(.78f, .91f, .87f));
            var track = NewRect("ProcessingTrack", card);
            track.anchorMin = new Vector2(0, 0); track.anchorMax = new Vector2(1, 0);
            track.offsetMin = new Vector2(20, 20); track.offsetMax = new Vector2(-20, 23);
            var trackImage = track.gameObject.AddComponent<Image>();
            trackImage.color = new Color(.11f, .2f, .25f); trackImage.raycastTarget = false;
            var fill = NewRect("Progress", track); fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one;
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            progress = fill.gameObject.AddComponent<Image>(); progress.color = new Color(.48f, .73f, .72f); progress.raycastTarget = false;
        }
        private static RectTransform NewRect(string name, Transform parent)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false); return rect;
        }
        private TMP_Text Label(string name, float x, float y, float height, float size, Color color)
        {
            var rect = NewRect(name, card); rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0, 1); rect.offsetMin = new Vector2(x, -y-height); rect.offsetMax = new Vector2(-20, -y);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            var adapter = log.Body != null ? log.Body.GetComponent<ComputerDesktopText>() : null;
            label.font = adapter != null && adapter.RenderedText != null ? adapter.RenderedText.font : TMP_Settings.defaultFontAsset;
            label.fontSize = size; label.color = color; label.richText = false; label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap; label.overflowMode = TextOverflowModes.Ellipsis;
            return label;
        }
        public void Render(float age, float duration)
        {
            Build(); if (card == null) return;
            if (!bodyHidden)
            {
                var adapter = log.Body != null ? log.Body.GetComponent<ComputerDesktopText>() : null;
                bodyGraphic = adapter != null && adapter.RenderedText != null ? adapter.RenderedText : log.Body;
                if (bodyGraphic != null) { bodyWasEnabled = bodyGraphic.enabled; bodyGraphic.enabled = false; }
                bodyHidden = true;
            }
            card.gameObject.SetActive(true);
            group.alpha = Mathf.Min(Mathf.Clamp01(age / .18f), Mathf.Clamp01((duration-age) / .5f));
            Progress = Mathf.Clamp01(age / 2.5f);
            heading.text = "DỮ LIỆU TỌA ĐỘ ĐÃ PHỤC HỒI";
            status.text = age < 2 ? "ĐANG ĐỌC MẢNH DỮ LIỆU" : "HAI TÍN HIỆU · XÁC MINH BẰNG RADAR";
            first.text = age < .85f ? "TÍN HIỆU A   ·   — — —" : firstCoordinate;
            second.text = age < 1.65f ? "TÍN HIỆU B   ·   — — —" : secondCoordinate;
            progress.rectTransform.anchorMax = new Vector2(Progress, 1);
        }
        public void Restore()
        {
            if (card != null) card.gameObject.SetActive(false);
            if (bodyHidden && bodyGraphic != null) bodyGraphic.enabled = bodyWasEnabled;
            bodyHidden = false;
            Progress = 0;
        }
        private void OnDisable() => Restore();
        private void OnDestroy()
        { if (card != null) { if (Application.isPlaying) Destroy(card.gameObject); else DestroyImmediate(card.gameObject); } }
    }
}
