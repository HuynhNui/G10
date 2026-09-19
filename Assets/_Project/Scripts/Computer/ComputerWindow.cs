using UnityEngine;

namespace G10.Prototype.Computer
{
    /// <summary>Desktop-only geometry. Application components and their content stay on the existing panel.</summary>
    public sealed class ComputerWindow : MonoBehaviour
    {
        public ComputerScreenController screen;
        public ComputerAppId app;
        private RectTransform rect, content;
        private Vector2 restorePosition, restoreSize;
        public bool Maximized { get; private set; }
        public RectTransform Rect => rect;
        public Vector2 Workspace => ((RectTransform)rect.parent).rect.size;
        public static readonly Vector2 MinimumSize = new(920, 650);
        public void Initialize(ComputerScreenController owner, ComputerAppId id, RectTransform body)
        { screen = owner; app = id; rect = (RectTransform)transform; content = body; LayoutContent(); }
        public void Focus() { if (screen.CurrentApp != app) screen.OpenApp(app); }
        public void ToggleMaximize()
        {
            Focus();
            if (Maximized) { Maximized = false; SetBounds(restorePosition, restoreSize); }
            else
            {
                restorePosition = rect.anchoredPosition; restoreSize = rect.sizeDelta; Maximized = true;
                SetBounds(new Vector2(8, -8), Workspace - Vector2.one * 16);
            }
        }
        public void SetBounds(Vector2 position, Vector2 size)
        {
            var area = Workspace;
            size = new Vector2(Mathf.Clamp(size.x, MinimumSize.x, area.x), Mathf.Clamp(size.y, MinimumSize.y, area.y));
            position.x = Mathf.Clamp(position.x, 0, area.x - size.x);
            position.y = -Mathf.Clamp(-position.y, 0, area.y - size.y);
            rect.anchoredPosition = position; rect.sizeDelta = size; LayoutContent();
        }
        private void LayoutContent()
        {
            if (content == null) return;
            // Uniformly fit the authored content: photos and text never stretch when a window changes aspect.
            var available = rect.sizeDelta - new Vector2(100, 195);
            float scale = Mathf.Min(available.x / 1240, available.y / 680);
            content.localScale = new Vector3(scale, scale, 1);
            content.anchoredPosition = new Vector2((rect.sizeDelta.x - 1240 * scale) / 2, -133);
        }
    }
}
