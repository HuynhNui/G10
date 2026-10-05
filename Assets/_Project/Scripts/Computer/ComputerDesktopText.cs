using TMPro;
using UnityEngine;

namespace G10.Prototype.Computer
{
    /// <summary>Presentation adapter: existing views keep their text bindings; TMP renders the text.</summary>
    public sealed class ComputerDesktopText : MonoBehaviour
    {
        private UnityEngine.UI.Text source;
        private TextMeshProUGUI label;
        public TextMeshProUGUI RenderedText => label;
        public void Bind(UnityEngine.UI.Text existing, TMP_FontAsset font, Color color)
        {
            source = existing;
            source.enabled = false;
            var child = new GameObject("TMP", typeof(RectTransform));
            child.transform.SetParent(transform, false);
            var rect = (RectTransform)child.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            label = child.AddComponent<TextMeshProUGUI>();
            label.font = font; label.fontSize = existing.fontSize; label.color = color;
            label.raycastTarget = false; label.richText = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.alignment = existing.alignment switch
            {
                TextAnchor.MiddleCenter => TextAlignmentOptions.Center,
                TextAnchor.MiddleRight => TextAlignmentOptions.MidlineRight,
                TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
                TextAnchor.MiddleLeft => TextAlignmentOptions.MidlineLeft,
                _ => TextAlignmentOptions.TopLeft
            };
            label.text = source.text;
        }
        private void LateUpdate() => Synchronize();
        public void Synchronize()
        {
            if (source != null && label != null && label.text != source.text) label.text = source.text;
        }
    }
}
