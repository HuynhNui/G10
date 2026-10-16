using UnityEngine;

namespace G10.Prototype.Feedback
{
    /// <summary>Explicit presentation binding, not a gameplay screen owner.</summary>
    public sealed class EffectSurface : MonoBehaviour
    {
        public EffectTarget target;
        public GameObject visibilityRoot;
        public CanvasGroup visibilityGroup;
        public EffectOverlayExecutor overlay;
        public EffectLightingExecutor lighting;
        public EffectShakeExecutor shake;
        [SerializeField] private bool audienceRequired, groupRequired;
        private readonly Vector3[] corners = new Vector3[4];
        public void BindVisibility(GameObject root, CanvasGroup group = null)
        { visibilityRoot = root; visibilityGroup = group; audienceRequired = root != null; groupRequired = group != null; }
        public bool Available
        {
            get
            {
                // Unity can deserialize an optional None as a non-CLR-null wrapper. Remember
                // the binding contract, not ReferenceEquals, to distinguish None from destruction.
                audienceRequired |= visibilityRoot != null; groupRequired |= visibilityGroup != null;
                return isActiveAndEnabled && (!audienceRequired || visibilityRoot != null && visibilityRoot.activeInHierarchy) &&
                    (!groupRequired || visibilityGroup != null && visibilityGroup.alpha > 0);
            }
        }
        public void FitToAudience()
        {
            if (visibilityRoot == null || !(visibilityRoot.transform is RectTransform audience) ||
                !(transform is RectTransform rect) || !(transform.parent is RectTransform parent)) return;
            audience.GetWorldCorners(corners);
            Vector2 min = parent.InverseTransformPoint(corners[0]), max = min;
            for (int i = 1; i < corners.Length; i++)
            { Vector2 point = parent.InverseTransformPoint(corners[i]); min = Vector2.Min(min, point); max = Vector2.Max(max, point); }
            var center = new Vector2(.5f, .5f);
            if (rect.anchorMin != center) rect.anchorMin = center;
            if (rect.anchorMax != center) rect.anchorMax = center;
            if (rect.pivot != center) rect.pivot = center;
            Vector2 size = max - min, position = (min + max) * .5f - parent.rect.center;
            if (rect.sizeDelta != size) rect.sizeDelta = size;
            if (rect.anchoredPosition != position) rect.anchoredPosition = position;
        }
        private void OnEnable() { if (Application.isPlaying) Canvas.willRenderCanvases += BeforeRender; }
        private void BeforeRender()
        {
            // Follow final UI layout and outer-frame shake without writing the audience transform.
            if (!Available) Restore(); else FitToAudience();
        }
        public void Restore()
        { overlay?.Restore(); lighting?.Restore(); shake?.Restore(); }
        private void OnDisable() { Canvas.willRenderCanvases -= BeforeRender; Restore(); }
    }
}
