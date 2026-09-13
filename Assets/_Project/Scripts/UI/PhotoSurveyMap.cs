using G10.Prototype.Navigation;
using UnityEngine;
using UnityEngine.UI;

namespace G10.Prototype.UI
{
    /// <summary>Chart-aligned survey cell and live ship marker. Overlay never intercepts chart pointer events.</summary>
    public sealed class PhotoSurveyMap : MaskableGraphic
    {
        public PhotoSurveyZone survey;
        public ZoneNavigation navigation;
        [Header("Square display grid")]
        public bool showGrid = true;
        public Color gridColor = new(.72f, .85f, .79f, .42f);
        [Range(.5f, 3f)] public float gridLineWidth = 1.2f;
        [Tooltip("Minimum line thickness on screen, after the cabin Canvas is scaled down.")]
        [Range(1f, 3f)] public float minimumGridPixels = 1.25f;
        public Color gridOutlineColor = new(.035f, .055f, .045f, .65f);
        public Color hoverColor = new(.7f, .95f, .85f, .18f);
        public RawImage completionIcon;
        public Text taskReadout;
        private int shownProgress = -1;
        private Vector2? hoverUV;
        public void SetPointer(Vector2? uv) { hoverUV = uv; SetVerticesDirty(); }
        protected override void OnDisable() { hoverUV = null; base.OnDisable(); }
        protected override void OnEnable() { base.OnEnable(); raycastTarget = false; }
        private void Update()
        {
            SetVerticesDirty();
            if (survey == null) return;
            if (completionIcon != null)
            {
                completionIcon.enabled = survey.IsComplete;
                var rect = completionIcon.rectTransform;
                rect.anchorMin = rect.anchorMax = ZoneNavigation.CoordinatesToUV(survey.center);
                rect.anchoredPosition = Vector2.zero;
                Vector2 size = Point(survey.center + Vector2.one * 25) - Point(survey.center - Vector2.one * 25);
                float side = Mathf.Max(16, Mathf.Min(size.x, size.y));
                float aspect = completionIcon.texture != null ? (float)completionIcon.texture.width / completionIcon.texture.height : 1;
                rect.sizeDelta = aspect >= 1 ? new Vector2(side, side / aspect) : new Vector2(side * aspect, side);
            }
            if (taskReadout != null)
            {
                bool visible = hoverUV.HasValue && survey.Contains(ZoneNavigation.UVToCoordinates(hoverUV.Value));
                taskReadout.transform.parent.gameObject.SetActive(visible);
                if (shownProgress != survey.CompletedCount)
                { shownProgress = survey.CompletedCount; taskReadout.text = survey.TaskDescription(); }
            }
        }
        private Vector2 Point(Vector2 coordinates)
        {
            Vector2 uv = ZoneNavigation.CoordinatesToUV(coordinates);
            return rectTransform.rect.min + Vector2.Scale(uv, rectTransform.rect.size);
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            DrawGrid(vh);
            if (survey == null) return;
            Vector2 a = Point(survey.center - Vector2.one * 25), b = Point(survey.center + Vector2.one * 25);
            Color gold = new(0.95f, 0.68f, 0.15f, 0.95f);
            if (!survey.IsComplete)
            {
            Line(vh, a, new(b.x,a.y), 3, gold); Line(vh, new(b.x,a.y), b, 3, gold);
            Line(vh, b, new(a.x,b.y), 3, gold); Line(vh, new(a.x,b.y), a, 3, gold);
            Vector2 p = Point(survey.center);
            Line(vh,p-Vector2.one*5,p+Vector2.one*5,2,gold);
            Line(vh,p+new Vector2(-5,5),p+new Vector2(5,-5),2,gold);
            }
            if (navigation == null) return;
            Vector2 shipPoint = Point(navigation.Position); float h = navigation.Heading * Mathf.Deg2Rad;
            Line(vh,shipPoint-Vector2.right*4,shipPoint+Vector2.right*4,8,Color.white);
            Line(vh,shipPoint,shipPoint+new Vector2(Mathf.Sin(h),Mathf.Cos(h))*18,3,Color.white);
        }
        private void DrawGrid(VertexHelper vh)
        {
            // SquareChartContent fits the calibrated chart so one metre has the
            // same display length on both axes. All markers share this mapping.
            Vector2 min = Point(Vector2.zero), max = Point(new Vector2(1200, 700));
            float step = Mathf.Abs(Point(new Vector2(ZoneNavigation.ChartCellSize, 0)).x - min.x);
            if (!showGrid || step < 12f) return;
            // A 1.2-unit line became less than one screen pixel when the 1920px
            // cabin was fitted into Game view. Rasterization dropped entire lines.
            // Keep a screen-space minimum, with a dark edge for pale chart terrain.
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera : null;
            Vector2 screenOrigin = RectTransformUtility.WorldToScreenPoint(camera, rectTransform.TransformPoint(Vector3.zero));
            float pixelsX = Vector2.Distance(screenOrigin, RectTransformUtility.WorldToScreenPoint(camera, rectTransform.TransformPoint(Vector3.right)));
            float pixelsY = Vector2.Distance(screenOrigin, RectTransformUtility.WorldToScreenPoint(camera, rectTransform.TransformPoint(Vector3.up)));
            float unitsPerPixel = 1f / Mathf.Max(.001f, Mathf.Min(pixelsX, pixelsY));
            float width = Mathf.Max(gridLineWidth, minimumGridPixels * unitsPerPixel);
            // Draw all outlines first so intersections do not erase other lines.
            for (float x = 0; x <= 1200; x += ZoneNavigation.ChartCellSize)
                Line(vh, Point(new Vector2(x,0)), Point(new Vector2(x,700)), width + 2f * unitsPerPixel, gridOutlineColor);
            for (float y = 0; y <= 700; y += ZoneNavigation.ChartCellSize)
                Line(vh, Point(new Vector2(0,y)), Point(new Vector2(1200,y)), width + 2f * unitsPerPixel, gridOutlineColor);
            for (float x = 0; x <= 1200; x += ZoneNavigation.ChartCellSize)
                Line(vh, Point(new Vector2(x,0)), Point(new Vector2(x,700)), width, gridColor);
            for (float y = 0; y <= 700; y += ZoneNavigation.ChartCellSize)
                Line(vh, Point(new Vector2(0,y)), Point(new Vector2(1200,y)), width, gridColor);
            if (!hoverUV.HasValue) return;
            Vector2 p = rectTransform.rect.min + Vector2.Scale(hoverUV.Value,rectTransform.rect.size);
            if (p.x < min.x || p.x >= max.x || p.y < min.y || p.y >= max.y) return;
            Vector2 cell = ZoneNavigation.CellCenter(ZoneNavigation.UVToCoordinates(hoverUV.Value));
            Vector2 a = Point(cell - Vector2.one * (ZoneNavigation.ChartCellSize / 2));
            Vector2 b = Point(cell + Vector2.one * (ZoneNavigation.ChartCellSize / 2));
            int i=vh.currentVertCount;
            vh.AddVert(a,hoverColor,Vector2.zero);vh.AddVert(new Vector2(a.x,b.y),hoverColor,Vector2.zero);
            vh.AddVert(b,hoverColor,Vector2.zero);vh.AddVert(new Vector2(b.x,a.y),hoverColor,Vector2.zero);
            vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
            Color cursor = new(.8f,1,.9f,.85f);
            Line(vh,new Vector2(Mathf.Max(min.x,p.x-9),p.y),new Vector2(Mathf.Min(max.x,p.x+9),p.y),1.5f,cursor);
            Line(vh,new Vector2(p.x,Mathf.Max(min.y,p.y-9)),new Vector2(p.x,Mathf.Min(max.y,p.y+9)),1.5f,cursor);
        }
        private static void Line(VertexHelper vh, Vector2 a, Vector2 b, float width, Color color)
        {
            Vector2 n = new Vector2(a.y-b.y,b.x-a.x).normalized*width/2; int i=vh.currentVertCount;
            vh.AddVert(a-n,color,Vector2.zero); vh.AddVert(a+n,color,Vector2.zero);
            vh.AddVert(b+n,color,Vector2.zero); vh.AddVert(b-n,color,Vector2.zero);
            vh.AddTriangle(i,i+1,i+2); vh.AddTriangle(i,i+2,i+3);
        }
    }
}
