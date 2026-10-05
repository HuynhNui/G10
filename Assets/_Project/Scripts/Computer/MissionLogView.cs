using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace G10.Prototype.Computer
{
    public sealed class MissionLogView : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour providerSource;
        [SerializeField] private Text body;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform viewport;
        private bool layoutDirty = true;
        public ScrollRect Scroll => scrollRect;
        public RectTransform Viewport => viewport;
        public Text Body => body;
        private IMissionProvider provider;
        private G10.Prototype.Missions.ZoneOneStory story;
        public string DisplayedText => body.text;
        public ExpeditionLoop Expedition { get; set; }
        private void Awake()
        {
            provider = providerSource as IMissionProvider;
            story = GetComponentInParent<G10.Prototype.UI.CabinStationView>(true)?.GetComponent<G10.Prototype.Missions.ZoneOneStory>();
        }
        private void OnEnable()
        {
            if (story != null) story.Changed += Refresh;
            layoutDirty = true;
            Refresh();
            if (scrollRect != null) { scrollRect.StopMovement(); scrollRect.verticalNormalizedPosition = 1; }
        }
        private void OnDisable() { if (story != null) story.Changed -= Refresh; }
        public void Bind(IMissionProvider source) { provider = source; Refresh(); }
        public void Refresh()
        {
            string text = BuildText();
            bool changed = body.text != text;
            if (!changed && !layoutDirty) return;
            float position = scrollRect != null ? scrollRect.verticalNormalizedPosition : 1;
            body.text = text;
            layoutDirty = true;
            if (!isActiveAndEnabled || scrollRect == null) return;
            var adapter = body.GetComponent<ComputerDesktopText>();
            adapter?.Synchronize();
            Canvas.ForceUpdateCanvases();
            float width = Mathf.Max(1, viewport.rect.width);
            // The legacy Text is data-only after skinning; measure the renderer actually in use.
            float height = adapter != null && adapter.RenderedText != null
                ? adapter.RenderedText.GetPreferredValues(text, width, Mathf.Infinity).y
                : body.preferredHeight;
            body.GetComponent<LayoutElement>().preferredHeight = Mathf.Max(viewport.rect.height, height + 8);
            LayoutRebuilder.ForceRebuildLayoutImmediate(body.rectTransform);
            Canvas.ForceUpdateCanvases();
            scrollRect.StopMovement();
            scrollRect.verticalNormalizedPosition = position;
            layoutDirty = false;
        }

        public void EnsureScrollLayout(Transform root = null)
        {
            if (root == null) root = transform.Find("ContentRoot") ?? transform;
            if (body == null) body = GetComponentInChildren<Text>(true);
            if (body == null) return;
            var scroll = root.Find("MissionScroll") as RectTransform;
            if (scroll == null) scroll = NewRect("MissionScroll", root);
            scrollRect = scroll.GetComponent<ScrollRect>() ?? scroll.gameObject.AddComponent<ScrollRect>();
            Place(scroll, 30, 15, 1180, 510);
            viewport = scroll.Find("Viewport") as RectTransform;
            if (viewport == null) viewport = NewRect("Viewport", scroll);
            viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
            viewport.offsetMin = viewport.offsetMax = Vector2.zero;
            if (viewport.GetComponent<RectMask2D>() == null) viewport.gameObject.AddComponent<RectMask2D>();
            var hit = viewport.GetComponent<Image>() ?? viewport.gameObject.AddComponent<Image>();
            hit.color = Color.clear; hit.raycastTarget = true;
            body.transform.SetParent(viewport, false);
            var rect = body.rectTransform;
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one; rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = Vector2.zero; rect.sizeDelta = new Vector2(0, 510);
            body.alignment = TextAnchor.UpperLeft; body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Overflow; body.resizeTextForBestFit = false;
            body.raycastTarget = false;
            var fitter = body.GetComponent<ContentSizeFitter>() ?? body.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var size = body.GetComponent<LayoutElement>() ?? body.gameObject.AddComponent<LayoutElement>();
            size.layoutPriority = 1;
            scrollRect.viewport = viewport; scrollRect.content = rect;
            scrollRect.horizontal = false; scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped; scrollRect.scrollSensitivity = 35;
            var route = root.Find("ExpeditionRoute") as RectTransform;
            if (route != null) Place(route, 30, 545, 740, 120);
            var next = root.Find("NextExpeditionZone") as RectTransform;
            if (next != null) Place(next, 800, 560, 400, 76);
            layoutDirty = true;
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false); return rect;
        }
        private static void Place(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h);
        }

        private string BuildText()
        {
            if (Expedition != null) return Expedition.MissionText();
            if (provider == null) return "MISSION DATA OFFLINE";
            MissionDefinition mission = provider.CurrentMission;
            if (mission == null) return provider.ZoneName + "\n\nNO MISSION ASSIGNED";
            var text = new StringBuilder(provider.ZoneName).AppendLine().AppendLine();
            text.Append('[').Append(mission.state.ToString().ToUpperInvariant()).Append("] ").AppendLine(mission.objective);
            if (!string.IsNullOrEmpty(mission.targetPoiId))
                text.AppendLine($"TARGET POI: {mission.targetPoiId}");
            text.AppendLine();
            foreach (MissionStep step in mission.steps)
            {
                if (step == null) continue;
                text.Append(step.state == MissionState.Completed ? "[x] " : "[ ] ");
                text.Append(step.description).Append("  [").Append(step.state.ToString().ToUpperInvariant()).AppendLine("]");
            }
            if (mission.isTemplate) text.AppendLine().AppendLine("PREVIEW MISSION — PROGRESS NOT CONNECTED");
            if (!string.IsNullOrEmpty(mission.integrationNote)) text.AppendLine(mission.integrationNote);
            return text.ToString();
        }
    }
}
