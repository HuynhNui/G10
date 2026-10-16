using UnityEngine;

namespace G10.Prototype.Tutorial
{
    /// <summary>A pointer-transparent dimmer with a real, unpainted opening around the current target.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TutorialSpotlight : UnityEngine.UI.MaskableGraphic
    {
        private const float ScreenPadding = 6f;
        private readonly Vector3[] corners = new Vector3[4];
        private RectTransform target;
        private Rect aperture;
        private Rect paintedBounds;
        private bool visible;

        public RectTransform Target => target;
        public Rect Aperture => aperture;
        public bool IsVisible => visible && isActiveAndEnabled;

        public static TutorialSpotlight Create(Transform parent)
        {
            var owner = new GameObject("TutorialSpotlight", typeof(RectTransform), typeof(CanvasRenderer));
            var rect = (RectTransform)owner.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var spotlight = owner.AddComponent<TutorialSpotlight>();
            spotlight.color = new Color(0, 0, 0, .38f);
            return spotlight;
        }

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
            maskable = false;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Canvas.willRenderCanvases += RefreshTarget;
        }

        protected override void OnDisable()
        {
            Canvas.willRenderCanvases -= RefreshTarget;
            target = null; visible = false; aperture = default;
            canvasRenderer.Clear();
            base.OnDisable();
        }

        public void SetTarget(RectTransform value)
        {
            target = value;
            RefreshTarget();
        }

        public void Hide()
        {
            target = null;
            UpdateOpening(false, default);
        }

        private void LateUpdate() => RefreshTarget();

        private void RefreshTarget()
        {
            if (!isActiveAndEnabled || target == null || !target.gameObject.activeInHierarchy || canvas == null || !canvas.isActiveAndEnabled)
            { UpdateOpening(false, default); return; }
            var targetCanvas = target.GetComponentInParent<Canvas>();
            if (targetCanvas == null || !targetCanvas.isActiveAndEnabled || !targetCanvas.rootCanvas.isActiveAndEnabled)
            { UpdateOpening(false, default); return; }
            var targetCamera = RenderCamera(targetCanvas);
            var overlayCamera = RenderCamera(canvas);
            target.GetWorldCorners(corners);
            Vector2 screenMin = new(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 screenMax = new(float.NegativeInfinity, float.NegativeInfinity);
            for (int i = 0; i < corners.Length; i++)
            {
                // A world/camera-space target behind its camera must not dim an unrelated screen region.
                if (targetCamera != null && targetCamera.WorldToScreenPoint(corners[i]).z <= 0)
                { UpdateOpening(false, default); return; }
                var screen = RectTransformUtility.WorldToScreenPoint(targetCamera, corners[i]);
                if (!float.IsFinite(screen.x) || !float.IsFinite(screen.y))
                { UpdateOpening(false, default); return; }
                screenMin = Vector2.Min(screenMin, screen); screenMax = Vector2.Max(screenMax, screen);
            }
            Rect bounds = rectTransform.rect;
            if (screenMax.x <= screenMin.x || screenMax.y <= screenMin.y ||
                !TryLocalBounds(screenMin, screenMax, overlayCamera, out var unpadded) || !bounds.Overlaps(unpadded))
            { UpdateOpening(false, default); return; }
            // Expand in screen pixels before projecting into canvas units, including scaled/camera canvases.
            if (!TryLocalBounds(screenMin - Vector2.one * ScreenPadding, screenMax + Vector2.one * ScreenPadding, overlayCamera, out var padded))
            { UpdateOpening(false, default); return; }
            var opening = Rect.MinMaxRect(Mathf.Max(bounds.xMin, padded.xMin), Mathf.Max(bounds.yMin, padded.yMin),
                Mathf.Min(bounds.xMax, padded.xMax), Mathf.Min(bounds.yMax, padded.yMax));
            UpdateOpening(opening.width > 0 && opening.height > 0, opening);
        }

        private bool TryLocalBounds(Vector2 screenMin, Vector2 screenMax, Camera camera, out Rect bounds)
        {
            Vector2 min = new(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new(float.NegativeInfinity, float.NegativeInfinity);
            for (int i = 0; i < 4; i++)
            {
                var screen = new Vector2((i & 1) == 0 ? screenMin.x : screenMax.x, (i & 2) == 0 ? screenMin.y : screenMax.y);
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screen, camera, out var local) ||
                    !float.IsFinite(local.x) || !float.IsFinite(local.y))
                { bounds = default; return false; }
                min = Vector2.Min(min, local); max = Vector2.Max(max, local);
            }
            bounds = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return true;
        }

        private static Camera RenderCamera(Canvas owner)
        {
            var root = owner.rootCanvas;
            return root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
        }

        private void UpdateOpening(bool show, Rect opening)
        {
            Rect bounds = rectTransform.rect;
            if (visible == show && aperture == opening && paintedBounds == bounds) return;
            // Clear() also removes renderer materials. Rebuild them when resuming an interrupted cue.
            if (show && !visible) SetMaterialDirty();
            visible = show; aperture = show ? opening : default; paintedBounds = bounds;
            // Clear immediately on interruption, even when no further canvas render will occur this frame.
            if (!visible) canvasRenderer.Clear();
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();
            if (!IsVisible) return;
            Rect bounds = rectTransform.rect;
            Quad(mesh, bounds.xMin, bounds.yMin, aperture.xMin, bounds.yMax);
            Quad(mesh, aperture.xMax, bounds.yMin, bounds.xMax, bounds.yMax);
            Quad(mesh, aperture.xMin, bounds.yMin, aperture.xMax, aperture.yMin);
            Quad(mesh, aperture.xMin, aperture.yMax, aperture.xMax, bounds.yMax);
        }

        private void Quad(UnityEngine.UI.VertexHelper mesh, float left, float bottom, float right, float top)
        {
            if (right <= left || top <= bottom) return;
            int first = mesh.currentVertCount;
            mesh.AddVert(new Vector3(left, bottom), color, Vector2.zero);
            mesh.AddVert(new Vector3(left, top), color, Vector2.zero);
            mesh.AddVert(new Vector3(right, top), color, Vector2.zero);
            mesh.AddVert(new Vector3(right, bottom), color, Vector2.zero);
            mesh.AddTriangle(first, first + 1, first + 2);
            mesh.AddTriangle(first, first + 2, first + 3);
        }
    }
}
