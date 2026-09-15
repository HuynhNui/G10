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
        public RawImage locationIcon;
        public RawImage[] locationIcons;
        public Vector2[] locationCoordinates;
        [System.Serializable]
        public sealed class LocationTasks { public string[] tasks = System.Array.Empty<string>(); }
        public LocationTasks[] locationTasks;
        public Text taskReadout;
        private int shownProgress = -1;
        private int shownLocation = -2;
        private Vector2? hoverUV;
        public int SelectedLocation { get; private set; } = -1;
        public void SelectHoveredLocation() { if (hoverUV.HasValue) SelectedLocation = LocationAt(hoverUV.Value); }
        public void RestoreSelection()
        {
            if (locationCoordinates != null && SelectedLocation >= 0 && SelectedLocation < locationCoordinates.Length)
                SetPointer(ZoneNavigation.CoordinatesToUV(locationCoordinates[SelectedLocation]));
        }
        public void SetPointer(Vector2? uv) { hoverUV = uv; UpdateTaskReadout(); SetVerticesDirty(); }
        protected override void OnDisable() { hoverUV = null; UpdateTaskReadout(); base.OnDisable(); }
        protected override void OnEnable() { base.OnEnable(); raycastTarget = false; }
        private void Update()
        {
            SetVerticesDirty();
            if (locationIcons != null && locationCoordinates != null)
                for (int i = 0; i < Mathf.Min(locationIcons.Length, locationCoordinates.Length); i++)
                {
                    var icon = locationIcons[i];
                    if (icon == null) continue;
                    var rect = icon.rectTransform;
                    rect.anchorMin = rect.anchorMax = ZoneNavigation.CoordinatesToUV(ZoneNavigation.CellCenter(locationCoordinates[i]));
                    rect.anchoredPosition = Vector2.zero;
                    rect.sizeDelta = new Vector2(rectTransform.rect.width / 24f, rectTransform.rect.height / 14f);
                }
            UpdateTaskReadout();
            if (survey == null) return;
            if (locationIcon != null && (locationIcons == null || locationIcons.Length == 0))
            {
                locationIcon.enabled = !survey.IsComplete;
                locationIcon.rectTransform.anchorMin = locationIcon.rectTransform.anchorMax = ZoneNavigation.CoordinatesToUV(survey.center);
                locationIcon.rectTransform.anchoredPosition = Vector2.zero;
            }
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
        }
        public int LocationAt(Vector2 uv)
        {
            if (uv.x < 0 || uv.x >= 1 || uv.y < 0 || uv.y >= 1 || locationCoordinates == null) return -1;
            Vector2 cell = ZoneNavigation.CellCenter(ZoneNavigation.UVToCoordinates(uv));
            for (int i = 0; i < locationCoordinates.Length; i++)
                if (cell == ZoneNavigation.CellCenter(locationCoordinates[i])) return i;
            return -1;
        }
        private void UpdateTaskReadout()
        {
            if (taskReadout == null) return;
            int index = hoverUV.HasValue ? LocationAt(hoverUV.Value) : -1;
            bool oldSurvey = index < 0 && hoverUV.HasValue && survey != null && survey.Contains(ZoneNavigation.UVToCoordinates(hoverUV.Value));
            taskReadout.transform.parent.gameObject.SetActive(index >= 0 || oldSurvey);
            if (index < 0 && !oldSurvey) { shownLocation = -2; return; }
            if (index >= 0)
            {
                if (shownLocation == index) return;
                string[] tasks = locationTasks != null && index < locationTasks.Length ? locationTasks[index]?.tasks : null;
                taskReadout.text = $"ĐỊA ĐIỂM {index + 1:00} • NHIỆM VỤ ({tasks?.Length ?? 0})";
                if (tasks != null) foreach (string task in tasks) taskReadout.text += "\n• " + task;
            }
            else if (oldSurvey && (shownLocation != -1 || shownProgress != survey.CompletedCount))
            { shownProgress = survey.CompletedCount; taskReadout.text = survey.TaskDescription(); }
            shownLocation = index;
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
            if (locationIcon == null)
            {
                Vector2 p = Point(survey.center);
                Line(vh,p-Vector2.one*5,p+Vector2.one*5,2,gold);
                Line(vh,p+new Vector2(-5,5),p+new Vector2(5,-5),2,gold);
            }
            }
            if (navigation == null) return;
            Vector2 shipPoint = Point(navigation.Position); float h = navigation.Heading * Mathf.Deg2Rad;
            Line(vh,shipPoint-Vector2.right*4,shipPoint+Vector2.right*4,8,Color.white);
            Line(vh,shipPoint,shipPoint+new Vector2(Mathf.Sin(h),Mathf.Cos(h))*18,3,Color.white);
        }
        private void DrawGrid(VertexHelper vh)
        {
            // The outer frame owns the axis labels; the grid spans the full image.
            Vector2 min = Point(Vector2.zero), max = Point(new Vector2(1200, 700));
            float step = Mathf.Abs(Point(new Vector2(ZoneNavigation.ChartCellSize, 0)).x - min.x);
            if (!showGrid || step < 8f) return;
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
