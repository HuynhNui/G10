using G10.Prototype.Navigation;
using G10.Prototype.Missions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace G10.Prototype.UI
{
    /// <summary>POI markers and live ship marker. Overlay never intercepts chart pointer events.</summary>
    public sealed class PhotoSurveyMap : MaskableGraphic
    {
        public PhotoSurveyZone survey;
        [Tooltip("Map data used by non-current zone panels and for marker/grid calibration.")]
        public ZoneMapConfig mapConfig;
        [Tooltip("Optional runtime override. ZoneMapPresentation binds the matching runtime when it exists.")]
        public ZoneMissionRuntime missionRuntime;
        public ZoneNavigation navigation;
        [Header("Square display grid")]
        public bool showGrid = true;
        public Color gridColor = new(.72f, .85f, .79f, .42f);
        [Range(.5f, 3f)] public float gridLineWidth = 1.2f;
        public Color majorGridColor = new(.72f, .9f, .84f, .68f);
        [Range(.5f, 5f)] public float majorGridLineWidth = 1.8f;
        [Tooltip("Minimum line thickness on screen, after the cabin Canvas is scaled down.")]
        [Range(1f, 3f)] public float minimumGridPixels = 1.25f;
        public Color gridOutlineColor = new(.035f, .055f, .045f, .65f);
        public RawImage completionIcon;
        public RawImage[] completionIcons;
        public RawImage locationIcon;
        public RawImage[] locationIcons;
        public MapPoi[] Locations => survey != null ? survey.locations : mapConfig != null ? mapConfig.locations : null;
        private ZoneMissionRuntime Runtime => survey != null && survey.MissionRuntime != null ? survey.MissionRuntime : missionRuntime;
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
        private const float MajorGridInterval = 100f;
        private Vector2 WorldSize => mapConfig != null ? mapConfig.WorldSize :
            navigation != null ? navigation.MapWorldSize : Vector2.one * ZoneNavigation.DefaultGridSize;
        private float GridSize => mapConfig != null ? mapConfig.GridSize :
            navigation != null ? navigation.MapGridSize : ZoneNavigation.DefaultGridSize;
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
                    var runtime = Runtime;
                    icon.enabled = IsPoiVisible(Locations[i].id) && (runtime == null || !runtime.IsPoiComplete(Locations[i].id));
                    icon.color = Color.white;
                    var rect = icon.rectTransform;
                    rect.anchorMin = rect.anchorMax = CoordinatesToUV(DisplayPosition(Locations[i].mapPosition));
                    rect.anchoredPosition = Vector2.zero;
                    rect.sizeDelta = MarkerSize(icon);
                }
            UpdateTaskReadout();
            if (completionIcons != null && completionIcons.Length > 0 && Locations != null)
            {
                for (int i = 0; i < completionIcons.Length; i++)
                {
                    var icon = completionIcons[i];
                    if (icon == null) continue;
                    bool valid = i < Locations.Length && Locations[i] != null;
                    var runtime = Runtime;
                    icon.enabled = valid && IsPoiVisible(Locations[i].id) &&
                        (runtime != null ? runtime.IsPoiComplete(Locations[i].id) :
                        survey != null && Locations[i] == survey.TargetPoi && survey.IsComplete);
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
            position = DisplayPosition(position);
            var rect = icon.rectTransform;
            rect.anchorMin = rect.anchorMax = CoordinatesToUV(position);
            rect.anchoredPosition = Vector2.zero;
            Vector2 halfCell = Vector2.one * (GridSize * .5f);
            Vector2 size = Point(position + halfCell) - Point(position - halfCell);
            float side = Mathf.Max(16, Mathf.Min(size.x, size.y));
            float aspect = icon.texture != null ? (float)icon.texture.width / icon.texture.height : 1;
            rect.sizeDelta = aspect >= 1 ? new Vector2(side, side / aspect) : new Vector2(side * aspect, side);
        }
        private bool IsPoiVisible(string poiId)
        {
            if (Runtime != null) return Runtime.IsPoiVisible(poiId);
            var location = mapConfig != null && mapConfig.missionConfig != null
                ? mapConfig.missionConfig.FindLocationByPoi(poiId) : null;
            return location == null || location.visibility == LocationVisibility.Visible;
        }
        private Vector2 MarkerSize(RawImage icon)
        {
            return Vector2.Scale(rectTransform.rect.size,
                new Vector2(GridSize / WorldSize.x, GridSize / WorldSize.y));
        }
        private Vector2 DisplayPosition(Vector2 position) => mapConfig != null ? mapConfig.GridCellCenter(position) :
            navigation != null ? navigation.MapCellCenter(position) : ZoneNavigation.CellCenter(position);
        public int LocationAt(Vector2 uv)
        {
            if (uv.x < 0 || uv.x >= 1 || uv.y < 0 || uv.y >= 1 || Locations == null) return -1;
            Vector2 point = UVToCoordinates(uv);
            for (int i = 0; i < Locations.Length; i++)
            {
                if (Locations[i] == null) continue;
                if (!IsPoiVisible(Locations[i].id)) continue;
                Vector2 delta = point - DisplayPosition(Locations[i].mapPosition);
                // The visual hitbox follows the authored marker file; gameplay arrival range stays unchanged.
                Vector2 cell = Vector2.one * GridSize;
                if (Mathf.Abs(delta.x) <= cell.x * .5f && Mathf.Abs(delta.y) <= cell.y * .5f ||
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
                Vector2 marker=parent.InverseTransformPoint(rectTransform.TransformPoint(Point(DisplayPosition(Locations[index].mapPosition))));
                Vector2 half=card.rect.size*.5f;
                Vector2 center=marker+new Vector2(half.x-55,half.y+25);
                center.x=Mathf.Clamp(center.x,parent.rect.xMin+half.x+70,parent.rect.xMax-half.x-70);
                center.y=Mathf.Clamp(center.y,parent.rect.yMin+half.y+70,parent.rect.yMax-half.y-185);
                card.anchoredPosition=center-new Vector2(parent.rect.xMin,parent.rect.yMax);
            }
            string missionId = survey != null && survey.mission != null ? survey.mission.targetPoiId : mapConfig != null ? mapConfig.zoneId : null;
            int progress = survey != null ? survey.CompletedCount : Runtime != null ? Runtime.CompletedCount : 0;
            if (shownLocation == index && shownMissionId == missionId && shownProgress == progress) return;
            shownMissionId = missionId;
            shownProgress = progress;
            if (Runtime != null)
                SetTaskText(Runtime.LocationText(Locations[index].id));
            else if (survey != null && Locations[index] == survey.TargetPoi)
                SetTaskText(survey.TaskDescription());
            else if (mapConfig != null && mapConfig.missionConfig != null)
            {
                var location = mapConfig.missionConfig.FindLocationByPoi(Locations[index].id);
                string value = location != null ? location.displayName : $"ĐỊA ĐIỂM {index + 1:00}";
                if (location?.objectives != null)
                    foreach (var objective in location.objectives)
                        if (objective != null) value += $"\n• {objective.type}: {objective.targetId}";
                SetTaskText(value);
            }
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
            Vector2 coordinate = UVToCoordinates(uv);
            Vector2 worldSize = WorldSize;
            if (coordinate.x < 0 || coordinate.x >= worldSize.x || coordinate.y < 0 || coordinate.y >= worldSize.y) return;
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
            Vector2 uv = CoordinatesToUV(coordinates);
            return rectTransform.rect.min + Vector2.Scale(uv, rectTransform.rect.size);
        }
        private Vector2 CoordinatesToUV(Vector2 coordinates) => mapConfig != null
            ? mapConfig.CoordinatesToUV(coordinates)
            : navigation != null ? navigation.MapCoordinatesToNormalized(coordinates)
            : ZoneNavigation.CoordinatesToUV(coordinates, WorldSize);
        private Vector2 UVToCoordinates(Vector2 normalized) => mapConfig != null
            ? mapConfig.UVToCoordinates(normalized)
            : navigation != null ? navigation.NormalizedToMapCoordinates(normalized)
            : ZoneNavigation.UVToCoordinates(normalized, WorldSize);
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            DrawGrid(vh);
            if (navigation == null) return;
            DrawRouteMarkers(vh);
            Vector2 shipPoint = Point(navigation.Position); float h = navigation.Heading * Mathf.Deg2Rad;
            Line(vh,shipPoint-Vector2.right*4,shipPoint+Vector2.right*4,8,Color.white);
            Line(vh,shipPoint,shipPoint+new Vector2(Mathf.Sin(h),Mathf.Cos(h))*18,3,Color.white);
        }
        private void DrawRouteMarkers(VertexHelper vh)
        {
            if (mapConfig == null || Runtime == null) return;
            if (!string.IsNullOrEmpty(mapConfig.destinationZone) && mapConfig.exitArea != null)
                RouteMarker(vh, mapConfig.exitArea.mapPosition, Runtime.MainObjectivesComplete
                    ? new Color(.25f, 1f, .7f) : new Color(.65f, .65f, .7f));
            if (!Runtime.HiddenRouteAvailable || mapConfig.hiddenLocationIds.Length == 0) return;
            foreach (string id in mapConfig.hiddenLocationIds)
                if (!Runtime.IsLocationRevealed(id) && !Runtime.IsLocationComplete(id)) return;
            if (mapConfig.finalHiddenPoint != null) RouteMarker(vh, mapConfig.finalHiddenPoint.mapPosition, new Color(1f, .85f, .35f));
        }
        private void RouteMarker(VertexHelper vh, Vector2 coordinates, Color color)
        {
            Vector2 p = Point(coordinates);
            Line(vh, p + Vector2.up * 12, p + Vector2.right * 12, 3, color);
            Line(vh, p + Vector2.right * 12, p + Vector2.down * 12, 3, color);
            Line(vh, p + Vector2.down * 12, p + Vector2.left * 12, 3, color);
            Line(vh, p + Vector2.left * 12, p + Vector2.up * 12, 3, color);
        }
        private void DrawGrid(VertexHelper vh)
        {
            // The outer frame owns the axis labels; the grid spans the full image.
            Vector2 worldSize = WorldSize;
            Vector2 min = Point(Vector2.zero), max = Point(worldSize);
            if (!showGrid) return;
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
            for (float x = 0; x <= worldSize.x + .001f; x += GridSize)
            {
                float lineWidth = IsMajorLine(x) ? Mathf.Max(width, majorGridLineWidth) : width;
                Line(vh, Point(new Vector2(x, 0)), Point(new Vector2(x, worldSize.y)), lineWidth + 2f * unitsPerPixel, gridOutlineColor);
            }
            for (float y = 0; y <= worldSize.y + .001f; y += GridSize)
            {
                float lineWidth = IsMajorLine(y) ? Mathf.Max(width, majorGridLineWidth) : width;
                Line(vh, Point(new Vector2(0, y)), Point(new Vector2(worldSize.x, y)), lineWidth + 2f * unitsPerPixel, gridOutlineColor);
            }
            for (float x = 0; x <= worldSize.x + .001f; x += GridSize)
            {
                bool major = IsMajorLine(x);
                Line(vh, Point(new Vector2(x, 0)), Point(new Vector2(x, worldSize.y)),
                    major ? Mathf.Max(width, majorGridLineWidth) : width, major ? majorGridColor : gridColor);
            }
            for (float y = 0; y <= worldSize.y + .001f; y += GridSize)
            {
                bool major = IsMajorLine(y);
                Line(vh, Point(new Vector2(0, y)), Point(new Vector2(worldSize.x, y)),
                    major ? Mathf.Max(width, majorGridLineWidth) : width, major ? majorGridColor : gridColor);
            }
            if (!hoverUV.HasValue) return;
            Vector2 p = rectTransform.rect.min + Vector2.Scale(hoverUV.Value,rectTransform.rect.size);
            if (p.x < min.x || p.x >= max.x || p.y < min.y || p.y >= max.y) return;
            Color cursor = new(.8f,1,.9f,.85f);
            Line(vh,new Vector2(Mathf.Max(min.x,p.x-9),p.y),new Vector2(Mathf.Min(max.x,p.x+9),p.y),1.5f,cursor);
            Line(vh,new Vector2(p.x,Mathf.Max(min.y,p.y-9)),new Vector2(p.x,Mathf.Min(max.y,p.y+9)),1.5f,cursor);
        }
        private static bool IsMajorLine(float coordinate) =>
            Mathf.Abs(coordinate / MajorGridInterval - Mathf.Round(coordinate / MajorGridInterval)) < .001f;
        private static void Line(VertexHelper vh, Vector2 a, Vector2 b, float width, Color color)
        {
            Vector2 n = new Vector2(a.y-b.y,b.x-a.x).normalized*width/2; int i=vh.currentVertCount;
            vh.AddVert(a-n,color,Vector2.zero); vh.AddVert(a+n,color,Vector2.zero);
            vh.AddVert(b+n,color,Vector2.zero); vh.AddVert(b-n,color,Vector2.zero);
            vh.AddTriangle(i,i+1,i+2); vh.AddTriangle(i,i+2,i+3);
        }
    }
}
