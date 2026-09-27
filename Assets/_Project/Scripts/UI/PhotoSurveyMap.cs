using G10.Prototype.Navigation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace G10.Prototype.UI
{
    /// <summary>POI markers and live ship marker. Overlay never intercepts chart pointer events.</summary>
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
        public RawImage completionIcon;
        public RawImage[] completionIcons;
        public RawImage locationIcon;
        public RawImage[] locationIcons;
        public MapPoi[] Locations => survey != null ? survey.locations : null;
        [System.Serializable]
        public sealed class LocationTasks { public string[] tasks = System.Array.Empty<string>(); }
        public LocationTasks[] locationTasks;
        [Header("Legacy labels retained for older scene installers")]
        public Text taskReadout;
        public Text destinationReadout;
        [Header("Rendered watercolor UI labels")]
        public TMP_Text styledTaskReadout;
        public TMP_Text coordinateReadout;
        public bool positionTaskCardAtMarker;
        private int shownLocation = -2;
        private string shownMissionId;
        private int shownProgress = -1;
        private Vector2? hoverUV;
        public int SelectedLocation { get; private set; } = -1;
        public void SelectHoveredLocation()
        {
            int index = hoverUV.HasValue ? LocationAt(hoverUV.Value) : -1;
            if (index < 0) return;
            SelectedLocation = index;
            UpdateTaskReadout();
        }
        public void RestoreSelection()
        {
            // Selection is session memory only; task visibility always follows the actual pointer.
            shownLocation = -2;
            UpdateTaskReadout();
        }
        public void SetPointer(Vector2? uv)
        {
            hoverUV = uv;
            if (uv.HasValue) UpdateCoordinateReadout(uv.Value);
            UpdateTaskReadout();
            SetVerticesDirty();
        }
        protected override void OnDisable() { hoverUV = null; UpdateTaskReadout(); shownLocation = -2; base.OnDisable(); }
        protected override void OnEnable() { base.OnEnable(); raycastTarget = false; RestoreSelection(); }
        private void Update()
        {
            SetVerticesDirty();
            if (locationIcons != null && Locations != null)
                for (int i = 0; i < Mathf.Min(locationIcons.Length, Locations.Length); i++)
                {
                    var icon = locationIcons[i];
                    if (icon == null || Locations[i] == null) continue;
                    icon.enabled = survey.MissionRuntime == null || survey.MissionRuntime.IsPoiVisible(Locations[i].id);
                    icon.color = Color.white;
                    var rect = icon.rectTransform;
                    rect.anchorMin = rect.anchorMax = ZoneNavigation.CoordinatesToUV(Locations[i].mapPosition);
                    rect.anchoredPosition = Vector2.zero;
                    rect.sizeDelta = new Vector2(rectTransform.rect.width / 24f, rectTransform.rect.height / 14f);
                }
            UpdateTaskReadout();
            if (completionIcons != null && completionIcons.Length > 0 && Locations != null)
            {
                for (int i = 0; i < completionIcons.Length; i++)
                {
                    var icon = completionIcons[i];
                    if (icon == null) continue;
                    bool valid = i < Locations.Length && Locations[i] != null;
                    icon.enabled = valid && survey != null && (survey.MissionRuntime == null || survey.MissionRuntime.IsPoiVisible(Locations[i].id)) &&
                        (survey.MissionRuntime != null ? survey.MissionRuntime.IsPoiComplete(Locations[i].id) :
                        Locations[i] == survey.TargetPoi && survey.IsComplete);
                    if (valid) PositionCompletionIcon(icon, Locations[i].mapPosition);
                }
                return;
            }
            if (survey == null || survey.TargetPoi == null)
            { if (completionIcon != null) completionIcon.enabled = false; return; }
            if (completionIcon != null)
            {
                completionIcon.enabled = survey.IsComplete;
                PositionCompletionIcon(completionIcon, survey.center);
            }
        }
        private void PositionCompletionIcon(RawImage icon, Vector2 position)
        {
            var rect = icon.rectTransform;
            rect.anchorMin = rect.anchorMax = ZoneNavigation.CoordinatesToUV(position);
            rect.anchoredPosition = Vector2.zero;
            Vector2 size = Point(position + Vector2.one * 25) - Point(position - Vector2.one * 25);
            float side = Mathf.Max(16, Mathf.Min(size.x, size.y));
            float aspect = icon.texture != null ? (float)icon.texture.width / icon.texture.height : 1;
            rect.sizeDelta = aspect >= 1 ? new Vector2(side, side / aspect) : new Vector2(side * aspect, side);
        }
        public int LocationAt(Vector2 uv)
        {
            if (uv.x < 0 || uv.x >= 1 || uv.y < 0 || uv.y >= 1 || Locations == null) return -1;
            Vector2 point = ZoneNavigation.UVToCoordinates(uv);
            for (int i = 0; i < Locations.Length; i++)
            {
                if (Locations[i] == null) continue;
                if (survey != null && survey.MissionRuntime != null && !survey.MissionRuntime.IsPoiVisible(Locations[i].id)) continue;
                Vector2 delta = point - Locations[i].mapPosition;
                // UI markers occupy one 50x50 chart cell. Gameplay arrival range stays unchanged.
                if (Mathf.Abs(delta.x) <= ZoneNavigation.ChartCellSize * .5f && Mathf.Abs(delta.y) <= ZoneNavigation.ChartCellSize * .5f ||
                    Locations[i].Contains(point)) return i;
            }
            return -1;
        }
        private void UpdateTaskReadout()
        {
            var taskTransform = styledTaskReadout != null ? styledTaskReadout.transform : taskReadout != null ? taskReadout.transform : null;
            if (taskTransform == null) return;
            int index = hoverUV.HasValue ? LocationAt(hoverUV.Value) : -1;
            taskTransform.parent.gameObject.SetActive(index >= 0);
            if (index < 0) { shownLocation = -2; return; }
            if(positionTaskCardAtMarker&&taskTransform.parent is RectTransform card&&card.parent is RectTransform parent)
            {
                Vector2 marker=parent.InverseTransformPoint(rectTransform.TransformPoint(Point(Locations[index].mapPosition)));
                Vector2 half=card.rect.size*.5f;
                Vector2 center=marker+new Vector2(half.x-55,half.y+25);
                center.x=Mathf.Clamp(center.x,parent.rect.xMin+half.x+70,parent.rect.xMax-half.x-70);
                center.y=Mathf.Clamp(center.y,parent.rect.yMin+half.y+70,parent.rect.yMax-half.y-185);
                card.anchoredPosition=center-new Vector2(parent.rect.xMin,parent.rect.yMax);
            }
            string missionId = survey.mission != null ? survey.mission.targetPoiId : null;
            int progress = survey.CompletedCount;
            if (shownLocation == index && shownMissionId == missionId && shownProgress == progress) return;
            shownMissionId = missionId;
            shownProgress = progress;
            if (survey.MissionRuntime != null)
                SetTaskText(survey.MissionRuntime.LocationText(Locations[index].id));
            else if (Locations[index] == survey.TargetPoi)
                SetTaskText(survey.TaskDescription());
            else
            {
                string[] tasks = locationTasks != null && index < locationTasks.Length ? locationTasks[index]?.tasks : null;
                string value = $"ĐỊA ĐIỂM {index + 1:00} • NHIỆM VỤ ({tasks?.Length ?? 0})";
                if (tasks != null) foreach (string task in tasks) value += "\n• " + task;
                SetTaskText(value);
            }
            shownLocation = index;
        }
        private void UpdateCoordinateReadout(Vector2 uv)
        {
            if (coordinateReadout == null && destinationReadout == null) return;
            Vector2 coordinate = ZoneNavigation.UVToCoordinates(uv);
            if (coordinate.x < 0 || coordinate.x >= 1200 || coordinate.y < 0 || coordinate.y >= 700) return;
            float depth = navigation != null ? navigation.Depth : survey != null ? survey.targetDepth : 0;
            string value = $"X {coordinate.x:0.0}  |  Y {coordinate.y:0.0}  |  Z {depth:0.0} M";
            if (coordinateReadout != null) coordinateReadout.text = value;
            if (destinationReadout != null) destinationReadout.text = value;
        }
        private void SetTaskText(string value)
        {
            if (styledTaskReadout != null) styledTaskReadout.text = value;
            if (taskReadout != null) taskReadout.text = value;
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
