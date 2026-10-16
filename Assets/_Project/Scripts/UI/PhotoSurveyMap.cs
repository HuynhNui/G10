using G10.Prototype.Navigation;
using G10.Prototype.Missions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace G10.Prototype.UI
{
    /// <summary>POI and route markers. Vessel position stays off the chart during real gameplay.</summary>
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
        private SavedZoneMapProgress savedProgress;
        public SavedZoneMapProgress SavedProgress
        {
            get => savedProgress;
            set { savedProgress = value; shownLocation = -2; SetVerticesDirty(); }
        }
        private bool MainComplete => Runtime != null ? Runtime.MainObjectivesComplete : SavedProgress?.MainObjectivesComplete == true;
        private bool PoiComplete(string id) => Runtime != null ? Runtime.IsPoiComplete(id) : SavedProgress?.IsPoiComplete(id) == true;
        [System.Serializable]
        public sealed class LocationTasks { public string[] tasks = System.Array.Empty<string>(); }
        public LocationTasks[] locationTasks;
        [Header("Legacy labels retained for older scene installers")]
        public Text taskReadout;
        public Text destinationReadout;
        [Header("Zone exit approach area")]
        public Color exitApproachColor = new(.25f, 1f, .7f, .32f);
        [Range(.5f, 4f)] public float exitApproachLineWidth = 1.8f;
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
        public bool ExitApproachVisible => mapConfig?.exitArea != null && mapConfig.exitArea.arrivalRadius > 0 &&
            !string.IsNullOrEmpty(mapConfig.destinationZone) && MainComplete;
        /// <summary>One map-coordinate circle becomes an ellipse when the chart stretches non-uniformly.</summary>
        public Vector2 ExitApproachRadii => ApproachRadii(mapConfig?.exitArea);
        public bool ReturnApproachVisible => mapConfig?.returnArea != null && mapConfig.returnArea.arrivalRadius > 0 &&
            !string.IsNullOrEmpty(mapConfig.returnDestinationZone) && (Runtime != null || SavedProgress?.ReturnAvailable == true);
        public Vector2 ReturnApproachRadii => ApproachRadii(mapConfig?.returnArea);
        public string ReturnPrompt => navigation != null ? ExpeditionProgression.DescribeReturn(mapConfig, navigation.Position) :
            $"RETURN GATE — READY · {mapConfig?.returnDestinationZone} ({mapConfig?.returnArea?.mapPosition.x:0}, {mapConfig?.returnArea?.mapPosition.y:0})";
        private bool HasFlag(string id) => Runtime != null ? Runtime.HasWorldFlag(id) : SavedProgress?.HasWorldFlag(id) == true;
        public bool RockCompleted => Runtime != null ? mapConfig?.UsesAlternate(Runtime) == true : SavedProgress?.UsesAlternate == true;
        public bool RockApproachVisible => mapConfig?.zoneId == "Zone03" && mapConfig.rockInteractionArea != null &&
            (HasFlag("ROCK_BARRIER_DISCOVERED") || RockCompleted ||
             (Runtime != null ? Runtime.HasRecipe("RECIPE_ROCK_BREAKER") : SavedProgress?.HasRecipe("RECIPE_ROCK_BREAKER") == true));
        public Vector2 RockApproachRadii => ApproachRadii(mapConfig?.rockInteractionArea);
        public string RockPrompt => RockCompleted ? "ROCK BARRIER — DESTROYED" : navigation != null
            ? ExpeditionProgression.DescribeRock(mapConfig, Runtime, navigation.Position, navigation.Depth)
            : $"ROCK BARRIER · TARGET DEPTH: {mapConfig?.rockInteractionArea?.targetDepth:0} M";
        public bool FinalApproachVisible => (Runtime != null ? mapConfig?.HiddenDestinationAvailable(Runtime) == true : SavedProgress?.HiddenDestinationAvailable == true) &&
            mapConfig.finalHiddenPoint.arrivalRadius > 0;
        public Vector2 FinalApproachRadii => ApproachRadii(mapConfig?.finalHiddenPoint);
        private Vector2 ApproachRadii(MapPoi point) => point == null ? Vector2.zero :
            Vector2.Scale(rectTransform.rect.size, new Vector2(point.arrivalRadius / WorldSize.x, point.arrivalRadius / WorldSize.y));
        public string ExitPrompt => Runtime != null ? ExpeditionProgression.DescribeExit(mapConfig, Runtime,
            navigation != null ? navigation.Position : Vector2.zero) : MainComplete
            ? $"ZONE EXIT — READY · {mapConfig?.destinationZone}"
            : SavedProgress?.MainLocationsComplete == true ? "EXIT LOCKED · Hoàn tất nâng cấp tuyến" : "EXIT LOCKED · Hoàn thành khảo sát";
        public string FinalPrompt => Runtime != null ? ExpeditionProgression.DescribeFinal(mapConfig, Runtime,
            navigation != null ? navigation.Position : Vector2.zero) : FinalApproachVisible ? "FINAL SIGNAL — READY" : "FINAL SIGNAL LOCKED";
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
                    icon.enabled = IsPoiVisible(Locations[i].id) && !PoiComplete(Locations[i].id);
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
                        (runtime != null || SavedProgress != null ? PoiComplete(Locations[i].id) :
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
            if (SavedProgress != null) return SavedProgress.IsPoiVisible(poiId);
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
            bool exitHovered = hoverUV.HasValue && mapConfig?.exitArea != null &&
                mapConfig.exitArea.Contains(UVToCoordinates(hoverUV.Value));
            bool exitNearby = navigation != null && mapConfig?.exitArea != null &&
                Vector2.Distance(navigation.Position, mapConfig.exitArea.mapPosition) <= mapConfig.exitArea.arrivalRadius + GridSize;
            bool showExit = index < 0 && (Runtime != null || SavedProgress != null) && !string.IsNullOrEmpty(mapConfig?.destinationZone) && (exitHovered || exitNearby);
            bool finalHovered = hoverUV.HasValue && mapConfig?.finalHiddenPoint != null &&
                mapConfig.finalHiddenPoint.Contains(UVToCoordinates(hoverUV.Value));
            bool finalNearby = navigation != null && mapConfig?.finalHiddenPoint != null &&
                Vector2.Distance(navigation.Position, mapConfig.finalHiddenPoint.mapPosition) <= mapConfig.finalHiddenPoint.arrivalRadius + GridSize;
            bool showFinal = index < 0 && !showExit && FinalApproachVisible && (finalHovered || finalNearby);
            bool returnHovered = hoverUV.HasValue && mapConfig?.returnArea != null && mapConfig.returnArea.Contains(UVToCoordinates(hoverUV.Value));
            // Vessel proximity is persistent state; return information is a transient map-hover tooltip.
            bool showReturn = index < 0 && !showExit && !showFinal && ReturnApproachVisible && returnHovered;
            bool rockHovered = hoverUV.HasValue && mapConfig?.rockInteractionArea != null && mapConfig.rockInteractionArea.Contains(UVToCoordinates(hoverUV.Value));
            bool rockNearby = navigation != null && mapConfig?.rockInteractionArea != null &&
                Vector2.Distance(navigation.Position, mapConfig.rockInteractionArea.mapPosition) <= mapConfig.rockInteractionArea.arrivalRadius + GridSize;
            bool showRock = index < 0 && !showExit && !showFinal && !showReturn && RockApproachVisible && (rockHovered || rockNearby);
            taskTransform.parent.gameObject.SetActive(index >= 0 || showExit || showFinal || showReturn || showRock);
            if (showExit || showFinal || showReturn || showRock)
            {
                PositionTaskCard(taskTransform, showExit ? mapConfig.exitArea.mapPosition : showReturn ? mapConfig.returnArea.mapPosition : showRock ? mapConfig.rockInteractionArea.mapPosition : mapConfig.finalHiddenPoint.mapPosition);
                string prompt = showExit ? ExitPrompt : showReturn ? ReturnPrompt : showRock ? RockPrompt : FinalPrompt;
                int routeIndex = showExit ? -3 : showReturn ? -5 : showRock ? -6 : -4;
                if (shownLocation != routeIndex || shownMissionId != prompt) SetTaskText(prompt);
                shownLocation = routeIndex;
                shownMissionId = prompt;
                return;
            }
            if (index < 0) { shownLocation = -2; return; }
            PositionTaskCard(taskTransform, DisplayPosition(Locations[index].mapPosition));
            string missionId = survey != null && survey.mission != null ? survey.mission.targetPoiId : mapConfig != null ? mapConfig.zoneId : null;
            int progress = survey != null ? survey.CompletedCount : Runtime != null ? Runtime.CompletedCount : SavedProgress?.CompletedCount ?? 0;
            if (shownLocation == index && shownMissionId == missionId && shownProgress == progress) return;
            shownMissionId = missionId;
            shownProgress = progress;
            if (Runtime != null)
                SetTaskText(Runtime.LocationText(Locations[index].id));
            else if (SavedProgress != null) SetTaskText(SavedProgress.LocationText(Locations[index].id));
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
        private void PositionTaskCard(Transform taskTransform, Vector2 coordinate)
        {
            if (!positionTaskCardAtMarker || !(taskTransform.parent is RectTransform card) || !(card.parent is RectTransform parent)) return;
            Vector2 marker = parent.InverseTransformPoint(rectTransform.TransformPoint(Point(coordinate)));
            Vector2 half = card.rect.size * .5f;
            bool returnCard = mapConfig?.returnArea != null && coordinate == mapConfig.returnArea.mapPosition;
            Vector2 clearance = returnCard ? ReturnApproachRadii + Vector2.one * 12 : Vector2.one * 25;
            Vector2 center = marker + new Vector2(half.x - 55, half.y + clearance.y);
            center.x = Mathf.Clamp(center.x, parent.rect.xMin + half.x + 70, parent.rect.xMax - half.x - 70);
            center.y = Mathf.Clamp(center.y, parent.rect.yMin + half.y + 70, parent.rect.yMax - half.y - 185);
            if (returnCard && Mathf.Abs(center.x - marker.x) < half.x + clearance.x &&
                Mathf.Abs(center.y - marker.y) < half.y + clearance.y)
            {
                // At the top edge, clamping a card above the gate placed it over the marker itself.
                center.y = Mathf.Clamp(marker.y - half.y - clearance.y,
                    parent.rect.yMin + half.y + 70, parent.rect.yMax - half.y - 185);
            }
            card.anchoredPosition = center - new Vector2(parent.rect.xMin, parent.rect.yMax);
        }
        private void UpdateCoordinateReadout(Vector2 uv)
        {
            if (coordinateReadout == null && destinationReadout == null) return;
            Vector2 coordinate = UVToCoordinates(uv);
            Vector2 worldSize = WorldSize;
            if (coordinate.x < 0 || coordinate.x >= worldSize.x || coordinate.y < 0 || coordinate.y >= worldSize.y) return;
            string value = $"X {coordinate.x:0.0}  |  Y {coordinate.y:0.0}";
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
            DrawRouteMarkers(vh);
        }
        private void DrawRouteMarkers(VertexHelper vh)
        {
            if (mapConfig == null || Runtime == null && SavedProgress == null) return;
            if (RockApproachVisible)
            {
                Color rockColor = RockCompleted ? new Color(.25f, 1f, .7f) :
                    Runtime?.HasObjective("Z3_GATE_INSTALL_ROCK_BREAKER") == true ? new Color(1f, .72f, .3f) : new Color(.7f, .6f, .7f);
                if (!RockCompleted) DrawApproach(vh, mapConfig.rockInteractionArea,
                    new Color(rockColor.r, rockColor.g, rockColor.b, exitApproachColor.a));
                Vector2 rock = Point(mapConfig.rockInteractionArea.mapPosition);
                if (RockCompleted)
                { Line(vh, rock + new Vector2(-8, 0), rock + new Vector2(-2, -6), 3, rockColor); Line(vh, rock + new Vector2(-2, -6), rock + new Vector2(9, 8), 3, rockColor); }
                else
                { Line(vh, rock + new Vector2(-8, -8), rock + new Vector2(8, 8), 3, rockColor); Line(vh, rock + new Vector2(-8, 8), rock + new Vector2(8, -8), 3, rockColor); }
            }
            if (!string.IsNullOrEmpty(mapConfig.returnDestinationZone) && mapConfig.returnArea != null)
            {
                Color returnColor = new(.45f, .8f, 1f);
                Color outline = new(.08f, .16f, .32f, .92f);
                if (ReturnApproachVisible)
                {
                    DrawApproach(vh, mapConfig.returnArea, outline, exitApproachLineWidth + 2f, true);
                    DrawApproach(vh, mapConfig.returnArea, new Color(returnColor.r, returnColor.g, returnColor.b, .85f), clipChart: true);
                }
                RouteMarker(vh, mapConfig.returnArea.mapPosition, outline, 5);
                RouteMarker(vh, mapConfig.returnArea.mapPosition, ReturnApproachVisible ? returnColor : Color.gray);
                var center = Point(mapConfig.returnArea.mapPosition);
                Line(vh, center + Vector2.right * 6, center + Vector2.left * 6, 4, outline);
                Line(vh, center + Vector2.left * 6, center + new Vector2(-1, 5), 4, outline);
                Line(vh, center + Vector2.right * 6, center + Vector2.left * 6, 2, returnColor);
                Line(vh, center + Vector2.left * 6, center + new Vector2(-1, 5), 2, returnColor);
            }
            if (!string.IsNullOrEmpty(mapConfig.destinationZone) && mapConfig.exitArea != null)
            {
                if (ExitApproachVisible) DrawApproach(vh, mapConfig.exitArea, exitApproachColor);
                RouteMarker(vh, mapConfig.exitArea.mapPosition, MainComplete
                    ? new Color(.25f, 1f, .7f) : new Color(.65f, .65f, .7f));
            }
            if (!FinalApproachVisible) return;
            if (FinalApproachVisible) DrawApproach(vh, mapConfig.finalHiddenPoint, new Color(1f, .85f, .35f, exitApproachColor.a));
            RouteMarker(vh, mapConfig.finalHiddenPoint.mapPosition, new Color(1f, .85f, .35f));
        }
        private void DrawApproach(VertexHelper vh, MapPoi point, Color color, float width = -1f, bool clipChart = false)
        {
            const int segments = 64;
            Vector2 previous = Point(point.mapPosition + Vector2.right * point.arrivalRadius);
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2 / segments;
                Vector2 next = Point(point.mapPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * point.arrivalRadius);
                float stroke = width > 0 ? width : exitApproachLineWidth;
                if (clipChart) ClippedApproachLine(vh, previous, next, stroke, color);
                else Line(vh, previous, next, stroke, color);
                previous = next;
            }
        }
        private void ClippedApproachLine(VertexHelper vh, Vector2 from, Vector2 to, float width, Color color)
        {
            // Corner gates can extend outside the world. Clip only the drawing, not their trigger.
            Rect bounds = rectTransform.rect;
            float inset = width * .5f, low = 0, high = 1;
            Vector2 delta = to - from;
            if (Clip(-delta.x, from.x - bounds.xMin - inset, ref low, ref high) &&
                Clip(delta.x, bounds.xMax - inset - from.x, ref low, ref high) &&
                Clip(-delta.y, from.y - bounds.yMin - inset, ref low, ref high) &&
                Clip(delta.y, bounds.yMax - inset - from.y, ref low, ref high))
                Line(vh, from + delta * low, from + delta * high, width, color);
        }
        private static bool Clip(float direction, float distance, ref float low, ref float high)
        {
            if (Mathf.Abs(direction) < .00001f) return distance >= 0;
            float amount = distance / direction;
            if (direction < 0) { if (amount > high) return false; low = Mathf.Max(low, amount); }
            else { if (amount < low) return false; high = Mathf.Min(high, amount); }
            return true;
        }
        private void RouteMarker(VertexHelper vh, Vector2 coordinates, Color color, float width = 3f)
        {
            Vector2 p = Point(coordinates);
            Line(vh, p + Vector2.up * 12, p + Vector2.right * 12, width, color);
            Line(vh, p + Vector2.right * 12, p + Vector2.down * 12, width, color);
            Line(vh, p + Vector2.down * 12, p + Vector2.left * 12, width, color);
            Line(vh, p + Vector2.left * 12, p + Vector2.up * 12, width, color);
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
