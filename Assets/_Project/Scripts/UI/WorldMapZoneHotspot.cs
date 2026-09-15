using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

namespace G10.Prototype.UI
{
    /// <summary>Polygon hit area with a feathered spotlight over the map artwork.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class WorldMapZoneHotspot : MaskableGraphic, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public WorldMapController controller;
        public int zoneIndex;
        public string zoneLabel;
        public Vector2[] polygon;
        public Text readout;
        public Color highlight = new(.7f, 1f, .87f, .3f);
        [Header("Hover appearance")]
        [Min(.01f)] public float fadeDuration = .2f;
        [Range(0, 1)] public float outlineOpacity = .65f;
        [Min(.1f)] public float outlineWidth = 1.4f;
        [Min(1)] public float featherWidth = 9f;
        [Range(0, 1)] public float surroundingDimOpacity = .72f;
        private float hoverAmount;
        private readonly List<Vector2> contour = new();
        private readonly List<Vector2> scratch = new();
        private readonly List<float> scanLines = new();
        private readonly List<Vector2> intersections = new();
        public bool Highlighted { get; private set; }
        public void OnPointerEnter(PointerEventData data)
        { controller.FocusRegion(this); Highlighted = true; SetVerticesDirty(); if (readout != null) readout.text = zoneLabel; }
        public void ClearFocusImmediately()
        { Highlighted = false; hoverAmount = 0; SetVerticesDirty(); }
        public void OnPointerExit(PointerEventData data)
        { Highlighted = false; SetVerticesDirty(); if (readout != null) readout.text = "Chọn khu vực để mở bản đồ"; }
        public void OnPointerClick(PointerEventData data)
        { if (data.button == PointerEventData.InputButton.Left) controller.OpenZone(zoneIndex); }
        protected override void OnDisable()
        { Highlighted = false; hoverAmount = 0; if (readout != null) readout.text = "Chọn khu vực để mở bản đồ"; base.OnDisable(); }
        private void Update()
        {
            float next = Mathf.MoveTowards(hoverAmount, Highlighted ? 1 : 0,
                Time.unscaledDeltaTime / Mathf.Max(.01f, fadeDuration));
            if (Mathf.Approximately(next, hoverAmount)) return;
            hoverAmount = next;
            SetVerticesDirty();
        }
        public override bool Raycast(Vector2 screenPoint, Camera eventCamera)
        {
            if (!base.Raycast(screenPoint, eventCamera) || polygon == null || polygon.Length < 3) return false;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out var p)) return false;
            Vector2 uv = (p - rectTransform.rect.min) / rectTransform.rect.size;
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                Vector2 a = polygon[i], b = polygon[j];
                if ((a.y > uv.y) != (b.y > uv.y) && uv.x < (b.x - a.x) * (uv.y - a.y) / (b.y - a.y) + a.x) inside = !inside;
            }
            return inside;
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (polygon == null || polygon.Length < 3) return;
            // Round the visual contour only; authored hit areas remain unchanged.
            contour.Clear();
            foreach (var p in polygon)
                contour.Add(rectTransform.rect.min + Vector2.Scale(p, rectTransform.rect.size));
            for (int pass = 0; pass < 2; pass++)
            {
                scratch.Clear();
                for (int i = 0; i < contour.Count; i++)
                {
                    Vector2 a = contour[i], b = contour[(i + 1) % contour.Count];
                    scratch.Add(Vector2.Lerp(a, b, .25f));
                    scratch.Add(Vector2.Lerp(a, b, .75f));
                }
                contour.Clear(); contour.AddRange(scratch);
            }
            Vector2 center = Vector2.zero;
            foreach (var p in contour) center += p;
            center /= contour.Count;
            float fade = Mathf.SmoothStep(0, 1, hoverAmount);
            // Retain transparent geometry at rest so GraphicRaycaster can still hit it.
            Color tint = highlight; tint.a = 0;
            vh.AddVert(center, tint, Vector2.zero);
            foreach (var p in contour) vh.AddVert(p, tint, Vector2.zero);
            for (int i = 0; i < contour.Count; i++) vh.AddTriangle(0, i + 1, (i + 1) % contour.Count + 1);
            if (fade <= 0) return;
            AddSurroundingShade(vh, surroundingDimOpacity * fade);
            AddBand(vh, center, -featherWidth, 0, 0, surroundingDimOpacity * fade, Color.black);
            AddBand(vh, center, -outlineWidth, 0, 0, outlineOpacity * fade * .45f, highlight);
            AddBand(vh, center, 0, outlineWidth, outlineOpacity * fade * .45f, 0, highlight);
        }
        // Tessellate the map rectangle minus the focused polygon in horizontal strips.
        // Pairing intersections supports concave boundaries without painting over the focus.
        private void AddSurroundingShade(VertexHelper vh, float opacity)
        {
            Rect rect = rectTransform.rect;
            scanLines.Clear(); scanLines.Add(rect.yMin); scanLines.Add(rect.yMax);
            foreach (var p in contour) scanLines.Add(Mathf.Clamp(p.y, rect.yMin, rect.yMax));
            scanLines.Sort();
            Color shade = new(0, 0, 0, opacity);
            for (int row = 0; row < scanLines.Count - 1; row++)
            {
                float bottom = scanLines[row], top = scanLines[row + 1];
                if (top - bottom < .001f) continue;
                float mid = (bottom + top) * .5f;
                intersections.Clear();
                for (int i = 0; i < contour.Count; i++)
                {
                    Vector2 a = contour[i], b = contour[(i + 1) % contour.Count];
                    if ((a.y > mid) == (b.y > mid)) continue;
                    float slope = (b.x - a.x) / (b.y - a.y);
                    intersections.Add(new(a.x + (bottom - a.y) * slope, a.x + (top - a.y) * slope));
                }
                intersections.Sort((a, b) => (a.x + a.y).CompareTo(b.x + b.y));
                Vector2 left = new(rect.xMin, rect.xMin);
                for (int i = 0; i < intersections.Count; i += 2)
                {
                    AddShadeQuad(vh, left, intersections[i], bottom, top, shade);
                    left = intersections[i + 1];
                }
                AddShadeQuad(vh, left, new(rect.xMax, rect.xMax), bottom, top, shade);
            }
        }
        private static void AddShadeQuad(VertexHelper vh, Vector2 left, Vector2 right, float bottom, float top, Color tint)
        {
            int start = vh.currentVertCount;
            vh.AddVert(new Vector2(left.x, bottom), tint, Vector2.zero);
            vh.AddVert(new Vector2(right.x, bottom), tint, Vector2.zero);
            vh.AddVert(new Vector2(right.y, top), tint, Vector2.zero);
            vh.AddVert(new Vector2(left.y, top), tint, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
        }
        private void AddBand(VertexHelper vh, Vector2 center, float inner, float outer, float innerAlpha, float outerAlpha, Color tint)
        {
            int start = vh.currentVertCount;
            Color inside = tint, outside = tint;
            inside.a = innerAlpha; outside.a = outerAlpha;
            for (int i = 0; i < contour.Count; i++)
            {
                Vector2 tangent = (contour[(i + 1) % contour.Count] - contour[(i + contour.Count - 1) % contour.Count]).normalized;
                Vector2 normal = new(-tangent.y, tangent.x);
                if (Vector2.Dot(normal, contour[i] - center) < 0) normal = -normal;
                vh.AddVert(contour[i] + normal * inner, inside, Vector2.zero);
                vh.AddVert(contour[i] + normal * outer, outside, Vector2.zero);
            }
            for (int i = 0; i < contour.Count; i++)
            {
                int a = start + i * 2, b = start + ((i + 1) % contour.Count) * 2;
                vh.AddTriangle(a, b, a + 1); vh.AddTriangle(a + 1, b, b + 1);
            }
        }
    }
}
