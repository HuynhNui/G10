using System;
using TMPro;
using UnityEngine;

namespace G10.Prototype.UI
{
    /// <summary>Text-only presentation on the authored Ending canvas; navigation and artwork stay unchanged.</summary>
    [DisallowMultipleComponent]
    public sealed class EndingPresentation : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Text title;
        [SerializeField] private TMP_Text body;

        public string EndingId { get; private set; }

        public void Show(string endingId)
        {
            EndingId = string.Equals(endingId, "hidden", StringComparison.OrdinalIgnoreCase) ? "hidden" : "normal";
            if (title == null)
            {
                var canvas = GetComponentInParent<Canvas>();
                if (canvas == null) canvas = GetComponentInChildren<Canvas>(true);
                if (canvas != null)
                    foreach (var label in canvas.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                        if (label.name == "Title") { title = label; break; }
            }
            if (title == null)
            {
                Debug.LogError("EndingPresentation requires the existing Ending canvas Title.", this);
                return;
            }
            bool hidden = EndingId == "hidden";
            title.text = hidden ? "HIDDEN ENDING" : "EXPEDITION COMPLETE";
            if (body == null)
            {
                var content = new GameObject("EndingSummary", typeof(RectTransform), typeof(TextMeshProUGUI));
                content.transform.SetParent(title.transform.parent, false);
                body = content.GetComponent<TextMeshProUGUI>();
                body.font = TMP_Settings.defaultFontAsset;
                body.fontSize = 22;
                body.color = title.color;
                body.alignment = TextAlignmentOptions.Midline;
                body.raycastTarget = false;
                body.richText = false;
                var rect = body.rectTransform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new(.5f, .5f);
                rect.sizeDelta = new(1000, 44);
                rect.anchoredPosition = new(0, 20);
            }
            body.text = hidden
                ? "All hidden locations explored. Expedition complete."
                : "Normal ending — expedition complete.";
        }
    }
}
