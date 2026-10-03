using System;
using G10.Prototype.Missions;
using G10.Prototype.Navigation;
using G10.Prototype.UI;
using G10.Prototype.Computer;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace G10.Prototype.Editor
{
    /// <summary>Builds the four zone-map configs and replaces the three placeholder map panels in place.</summary>
    public static class ZoneMapEditor
    {
        private const string Environment = "Assets/_Project/Art/Environment/";
        private const string MapRoot = "Assets/_Project/Content/Maps";
        private static readonly Vector2 MapDisplaySize = new(1440f, 840f);
        private static readonly Vector2 LegacyAuthoredWorldSize = new(24f * ZoneNavigation.DefaultGridSize, 14f * ZoneNavigation.DefaultGridSize);

        [MenuItem("G10/Maps/Install Zone 1-4 Maps")]
        public static void Install()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before installing zone maps.");
            var cabin = Object.FindAnyObjectByType<CabinStationView>();
            if (cabin == null || cabin.gameObject.scene.name != "Zone01")
                throw new InvalidOperationException("Open Zone01 before installing zone maps.");
            var world = cabin.GetComponent<WorldMapController>();
            if (world == null || world.zoneMaps == null || world.zoneMaps.Length < 4)
                throw new InvalidOperationException("Install the World Map UI first.");

            EnsureFolder(MapRoot);
            ZoneMapConfig[] configs = BuildConfigs();
            Undo.RegisterFullObjectHierarchyUndo(cabin.gameObject, "Install Zone 1-4 maps");
            InstallZoneOne(cabin, configs[0]);
            for (int i = 1; i < configs.Length; i++) InstallPanel(cabin, world, world.zoneMaps[i].transform, configs[i]);
            foreach (var hotspot in world.worldPanel.GetComponentsInChildren<WorldMapZoneHotspot>(true))
            {
                hotspot.zoneLabel = $"ZONE {hotspot.zoneIndex + 1:00} • MỞ BẢN ĐỒ";
                EditorUtility.SetDirty(hotspot);
            }
            world.zone01Overlay = cabin.MapPanel.GetComponentInChildren<PhotoSurveyMap>(true);
            EditorUtility.SetDirty(world);
            EditorSceneManager.MarkSceneDirty(cabin.gameObject.scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = cabin.MapPanel;
            Debug.Log("Installed Zone01-Zone04 maps, light animation, POIs, terrain masks and Zone03 rock-state art.");
        }

        public static void InstallBatch()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Gameplay/Zone01.unity");
            Install();
            EditorSceneManager.SaveOpenScenes();
        }

        [MenuItem("G10/Maps/Refresh 50-Pixel Source Grid")]
        public static void RefreshPixelCoordinateGrid()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before refreshing map coordinates.");
            foreach (string guid in AssetDatabase.FindAssets("t:ZoneMapConfig", new[] { MapRoot }))
            {
                var config = AssetDatabase.LoadAssetAtPath<ZoneMapConfig>(AssetDatabase.GUIDToAssetPath(guid));
                if (config == null) continue;
                config.ConfigureGrid(ZoneNavigation.DefaultGridSize);
                config.ConfigureDisplaySize(MapDisplaySize);
                config.MigrateCoordinates(LegacyAuthoredWorldSize);
                EditorUtility.SetDirty(config);
            }
            foreach (var presentation in Object.FindObjectsByType<ZoneMapPresentation>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (presentation == null || presentation.config == null) continue;
                ConfigureAxisLabels(presentation.transform.Find("ChartOuterFrame"), presentation.config);
                if (presentation.overlay != null) ConfigureMarkers(presentation.overlay, presentation.config);
                if (presentation.navigation != null)
                {
                    presentation.navigation.MigrateCoordinates(LegacyAuthoredWorldSize,
                        presentation.config.LegacyLocationSizedWorldSize,
                        presentation.config.LegacyDisplayWorldSize, presentation.config.WorldSize);
                    presentation.navigation.ConfigureMapCoordinates(presentation.config.WorldSize, presentation.config.GridSize);
                    EditorUtility.SetDirty(presentation.navigation);
                }
                if (presentation.overlay != null && presentation.overlay.survey != null && presentation.config.zoneId == "Zone01")
                {
                    presentation.overlay.survey.locations = Array.ConvertAll(presentation.config.locations, ClonePoi);
                    EditorUtility.SetDirty(presentation.overlay.survey);
                }
                EditorUtility.SetDirty(presentation);
            }
            foreach (var loop in Object.FindObjectsByType<ExpeditionLoop>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                loop.MigrateAuthoredCoordinates(zone => {
                    var config = AssetDatabase.LoadAssetAtPath<ZoneMapConfig>($"{MapRoot}/{zone}.asset");
                    return config != null ? config.WorldSize : Vector2.one * ZoneNavigation.DefaultGridSize;
                }, zone => {
                    var config = AssetDatabase.LoadAssetAtPath<ZoneMapConfig>($"{MapRoot}/{zone}.asset");
                    return config != null ? config.LegacyLocationSizedWorldSize : LegacyAuthoredWorldSize;
                }, zone => {
                    var config = AssetDatabase.LoadAssetAtPath<ZoneMapConfig>($"{MapRoot}/{zone}.asset");
                    return config != null ? config.LegacyDisplayWorldSize : MapDisplaySize;
                }, LegacyAuthoredWorldSize);
                EditorUtility.SetDirty(loop);
            }
            foreach (var scene in EnumerableScenes())
                if (scene.isLoaded) EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
        }

        private static ZoneMapConfig[] BuildConfigs()
        {
            var missions = new[] {
                Mission("Zone01"), Mission("Zone02"), Mission("Zone03"), Mission("Zone04")
            };
            var configs = new[]
            {
                Configure("Zone01", missions[0], "Zone1/Mapingame.png", "Zone1/mapline.png", "Zone1/location.png",
                    new[] { "Zone1/Light.png" }, new[] {
                        Poi("zone01-left", 275, 75), Poi("zone01-east", 725, 175), Poi("zone01-north", 625, 475) }),
                Configure("Zone02", missions[1], "Zone2/Mapingame.png", "Zone2/Mapline.png", "Zone2/L3.png",
                    new[] { "Zone2/Light.png" }, new[] {
                        Poi("zone02-l1", 22.2f, 384.4f), Poi("zone02-l2", 280.6f, 397.7f), Poi("zone02-l3", 1072.2f, 315.3f) }),
                Configure("Zone03", missions[2], "Zone3/MapingamewithStone.png", "Zone3/MapLinewithStone.png", "Zone3/Location.png",
                    new[] { "Zone3/Light1.png", "Zone3/Light2.png" }, new[] {
                        Poi("zone03-l1", 278.8f, 139f), Poi("zone03-l2", 30f, 408.7f),
                        Poi("zone03-l3", 966.3f, 520.1f), Poi("zone03-l4", 696.3f, 161.7f) },
                    "Zone3/MapafterStoneBreaked.png", "Zone3/MaplineAfterStoneBreaked.png", "Z3_GATE_DESTROY_ROCK_BARRIER"),
                Configure("Zone04", missions[3], "Zone4/Mapingame.png", "Zone4/Mapline.png", "Zone4/Location.png",
                    new[] { "Zone4/Light.png" }, new[] {
                        Poi("zone04-l1", 181f, 620f), Poi("zone04-l2", 1107.8f, 449.5f),
                        Poi("zone04-l3", 629.1f, 292f), Poi("zone04-l4", 329.4f, 293f), Poi("zone04-l5", 224.1f, 77.5f) })
            };
            for (int i = 0; i < configs.Length; i++)
            {
                var missionLocations = missions[i].locations;
                for (int p = 0; p < Mathf.Min(missionLocations.Length, configs[i].locations.Length); p++)
                    missionLocations[p].poiId = configs[i].locations[p].id;
                EditorUtility.SetDirty(missions[i]);
            }
            return configs;
        }

        private static ZoneMapConfig Configure(string zone, ZoneMissionConfig mission, string map, string line, string marker,
            string[] lights, MapPoi[] locations, string alternateMap = null, string alternateLine = null, string alternateObjective = null)
        {
            string path = $"{MapRoot}/{zone}.asset";
            var config = AssetDatabase.LoadAssetAtPath<ZoneMapConfig>(path);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ZoneMapConfig>();
                config.name = zone;
                AssetDatabase.CreateAsset(config, path);
            }
            else config.name = zone;
            config.zoneId = zone;
            config.ConfigureGrid(ZoneNavigation.DefaultGridSize);
            config.ConfigureDisplaySize(MapDisplaySize);
            config.missionConfig = mission;
            config.map = Texture(map);
            config.terrainLine = Texture(line);
            config.locationMarker = Texture(marker);
            config.lightLayers = Array.ConvertAll(lights, Texture);
            Vector2 scale = new(config.WorldSize.x / LegacyAuthoredWorldSize.x, config.WorldSize.y / LegacyAuthoredWorldSize.y);
            foreach (MapPoi location in locations)
                if (location != null) location.mapPosition = Vector2.Scale(location.mapPosition, scale);
            config.locations = locations;
            config.MarkCoordinatesCurrent();
            config.alternateMap = string.IsNullOrEmpty(alternateMap) ? null : Texture(alternateMap);
            config.alternateTerrainLine = string.IsNullOrEmpty(alternateLine) ? null : Texture(alternateLine);
            config.alternateObjectiveId = alternateObjective;
            config.alternateWorldFlag = string.Empty;
            const int columns = 960, rows = 540;
            config.StoreTerrainMask(NavigationArtworkEditor.BakeBoundaries(config.terrainLine, columns, rows), columns, rows, false);
            if (config.alternateTerrainLine != null)
                config.StoreTerrainMask(NavigationArtworkEditor.BakeBoundaries(config.alternateTerrainLine, columns, rows), columns, rows, true);
            EditorUtility.SetDirty(config);
            return config;
        }

        private static void InstallZoneOne(CabinStationView cabin, ZoneMapConfig config)
        {
            var content = cabin.MapPanel.transform.Find("SquareChartContent");
            var overlay = content != null ? content.GetComponentInChildren<PhotoSurveyMap>(true) : null;
            if (content == null || overlay == null) throw new InvalidOperationException("Zone01 chart content is missing.");
            if (content.GetComponent<RectMask2D>() == null) Undo.AddComponent<RectMask2D>(content.gameObject);
            overlay.mapConfig = config;
            var lights = CreateLights(content, config);
            overlay.transform.SetAsLastSibling();
            var presentation = cabin.MapPanel.GetComponent<ZoneMapPresentation>() ?? Undo.AddComponent<ZoneMapPresentation>(cabin.MapPanel);
            presentation.config = config;
            presentation.mapImage = content.GetComponent<RawImage>();
            presentation.lightImages = lights;
            presentation.overlay = overlay;
            presentation.navigation = cabin.GetComponent<ZoneNavigation>();
            ConfigureAxisLabels(cabin.MapPanel.transform.Find("ChartOuterFrame"), config);
            EditorUtility.SetDirty(overlay);
            EditorUtility.SetDirty(presentation);
        }

        private static void InstallPanel(CabinStationView cabin, WorldMapController world, Transform panel, ZoneMapConfig config)
        {
            Remove(panel, "Title");
            Remove(panel, "Unavailable");
            Remove(panel, "WorldMap");
            Remove(panel, "ZoneTaskCard");
            Remove(panel, "ZoneMapTitle");

            Transform zoneOne = cabin.MapPanel.transform;
            CloneZoneOneElement(zoneOne, panel, "WatercolorChartBackground", 0);
            Transform axisFrame = CloneZoneOneElement(zoneOne, panel, "ChartOuterFrame", 1);
            ConfigureAxisLabels(axisFrame, config);
            CloneZoneOneElement(zoneOne, panel, "SurveyLegend");
            Transform worldButton = CloneZoneOneElement(zoneOne, panel, "WatercolorWorld");
            Transform taskCard = CloneZoneOneElement(zoneOne, panel, "SurveyTasks");
            if (worldButton != null)
            {
                worldButton.gameObject.SetActive(true);
                WireButton(worldButton.GetComponent<Button>(), world.OpenWorld);
            }

            var content = FindOrCreate("MapChartContent", panel, typeof(RawImage), typeof(RectMask2D), typeof(CabinPointerTarget));
            CopyRect(zoneOne.Find("SquareChartContent") as RectTransform, content as RectTransform);
            var art = content.GetComponent<RawImage>();
            art.texture = config.map;
            art.color = Color.white;
            art.raycastTarget = true;
            content.GetComponent<CabinPointerTarget>().Configure(cabin, string.Empty, isChart: true);
            var lights = CreateLights(content, config);

            var overlayTransform = FindOrCreate("PhotoSurveyOverlay", content, typeof(CanvasRenderer), typeof(PhotoSurveyMap));
            Stretch(overlayTransform);
            var overlay = overlayTransform.GetComponent<PhotoSurveyMap>();
            overlay.mapConfig = config;
            overlay.survey = null;
            overlay.navigation = null;
            overlay.raycastTarget = false;
            BuildMarkers(overlay, config);
            overlayTransform.SetAsLastSibling();

            overlay.taskReadout = null;
            overlay.styledTaskReadout = taskCard != null ? taskCard.Find("TaskText")?.GetComponent<TMP_Text>() : null;
            overlay.coordinateReadout = panel.Find("SurveyLegend/Coordinate")?.GetComponent<TMP_Text>();
            overlay.positionTaskCardAtMarker = true;
            if (taskCard != null) taskCard.gameObject.SetActive(false);

            var presentation = panel.GetComponent<ZoneMapPresentation>() ?? Undo.AddComponent<ZoneMapPresentation>(panel.gameObject);
            presentation.config = config;
            presentation.mapImage = art;
            presentation.lightImages = lights;
            presentation.overlay = overlay;
            presentation.navigation = null;
            var hud = panel.Find("WatercolorHUD");
            if (hud != null) hud.SetAsLastSibling();
            EditorUtility.SetDirty(overlay);
            EditorUtility.SetDirty(presentation);
        }

        private static void BuildMarkers(PhotoSurveyMap overlay, ZoneMapConfig config)
        {
            RemoveStartingWith(overlay.transform, "LocationMarker");
            RemoveStartingWith(overlay.transform, "CompletedCheck");
            var checks = new RawImage[config.locations.Length];
            var markers = new RawImage[config.locations.Length];
            Texture2D check = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/UI/Map/check.png")
                ?? throw new InvalidOperationException("Missing completion check texture.");
            for (int i = 0; i < config.locations.Length; i++)
            {
                Vector2 uv = config.CoordinatesToUV(config.GridCellCenter(config.locations[i].mapPosition));
                var marker = FindOrCreate($"LocationMarker{i + 1:00}", overlay.transform, typeof(RawImage)).GetComponent<RawImage>();
                marker.texture = config.locationMarker;
                marker.raycastTarget = false;
                var location = config.missionConfig != null ? config.missionConfig.FindLocationByPoi(config.locations[i].id) : null;
                marker.enabled = location == null || location.visibility == LocationVisibility.Visible;
                marker.rectTransform.anchorMin = marker.rectTransform.anchorMax = uv;
                marker.rectTransform.anchoredPosition = Vector2.zero;
                marker.rectTransform.sizeDelta = MarkerUISize(overlay.rectTransform, marker, config);
                markers[i] = marker;
                var complete = FindOrCreate($"CompletedCheck{i + 1:00}", overlay.transform, typeof(RawImage)).GetComponent<RawImage>();
                complete.texture = check;
                complete.raycastTarget = false;
                complete.enabled = false;
                checks[i] = complete;
            }
            overlay.locationIcons = markers;
            overlay.locationIcon = markers.Length > 0 ? markers[0] : null;
            overlay.completionIcons = checks;
            overlay.completionIcon = checks.Length > 0 ? checks[0] : null;
            overlay.locationTasks = new PhotoSurveyMap.LocationTasks[config.locations.Length];
            for (int i = 0; i < overlay.locationTasks.Length; i++) overlay.locationTasks[i] = new PhotoSurveyMap.LocationTasks();
        }

        private static void ConfigureMarkers(PhotoSurveyMap overlay, ZoneMapConfig config)
        {
            if (overlay.locationIcons == null || config.locations == null) return;
            for (int i = 0; i < Mathf.Min(overlay.locationIcons.Length, config.locations.Length); i++)
            {
                RawImage marker = overlay.locationIcons[i];
                if (marker == null || config.locations[i] == null) continue;
                marker.rectTransform.anchorMin = marker.rectTransform.anchorMax =
                    config.CoordinatesToUV(config.GridCellCenter(config.locations[i].mapPosition));
                marker.rectTransform.anchoredPosition = Vector2.zero;
                marker.rectTransform.sizeDelta = MarkerUISize(overlay.rectTransform, marker, config);
                EditorUtility.SetDirty(marker);
            }
            EditorUtility.SetDirty(overlay);
        }

        private static Vector2 MarkerUISize(RectTransform mapRect, RawImage marker, ZoneMapConfig config)
        {
            return Vector2.Scale(mapRect.rect.size,
                new Vector2(config.GridSize / config.WorldSize.x, config.GridSize / config.WorldSize.y));
        }

        private static void ConfigureAxisLabels(Transform frame, ZoneMapConfig config)
        {
            if (frame == null || config == null) return;
            Vector2 worldSize = config.WorldSize;
            var labels = new System.Collections.Generic.List<Transform>();
            foreach (Transform child in frame) labels.Add(child);
            Vector2 displaySize = config.LegacyDisplayWorldSize;
            if (!TryGetAxisBounds(labels, 'X', displaySize.x, out Vector2 xStart, out Vector2 xEnd) ||
                !TryGetAxisBounds(labels, 'Y', displaySize.y, out Vector2 yStart, out Vector2 yEnd)) return;
            EnsureMajorLabels(frame, labels, 'X', Mathf.FloorToInt(worldSize.x / 100f) * 100);
            EnsureMajorLabels(frame, labels, 'Y', Mathf.FloorToInt(worldSize.y / 100f) * 100);
            PositionAxis(labels, 'X', worldSize.x, xStart, xEnd);
            PositionAxis(labels, 'Y', worldSize.y, yStart, yEnd);
            foreach (Transform label in labels)
            {
                if (label.name.Length < 2 || label.name[0] != 'X' && label.name[0] != 'Y' ||
                    !int.TryParse(label.name.Substring(1), out int coordinate)) continue;
                float extent = label.name[0] == 'X' ? worldSize.x : worldSize.y;
                bool major = coordinate >= 0 && coordinate <= extent + .001f && coordinate % 100 == 0;
                label.gameObject.SetActive(major);
                if (!major) continue;
                var legacy = label.GetComponentInChildren<Text>(true); if (legacy != null) legacy.text = coordinate.ToString();
                var tmp = label.GetComponentInChildren<TMP_Text>(true); if (tmp != null) tmp.text = coordinate.ToString();
                EditorUtility.SetDirty(label);
            }
        }

        private static void EnsureMajorLabels(Transform frame, System.Collections.Generic.List<Transform> labels, char axis, int maximum)
        {
            Transform template = labels.Find(label => label.name == axis + "0");
            if (template == null) return;
            for (int coordinate = 0; coordinate <= maximum; coordinate += 100)
            {
                string name = axis + coordinate.ToString();
                if (labels.Exists(label => label.name == name)) continue;
                GameObject clone = Object.Instantiate(template.gameObject, frame);
                clone.name = name;
                Undo.RegisterCreatedObjectUndo(clone, "Add map axis label");
                labels.Add(clone.transform);
            }
        }

        private static bool TryGetAxisBounds(System.Collections.Generic.List<Transform> labels, char axis, float displayLength,
            out Vector2 start, out Vector2 end)
        {
            var axisLabels = labels.FindAll(label => label.name.Length > 1 && label.name[0] == axis &&
                int.TryParse(label.name.Substring(1), out _));
            start = end = default;
            if (axisLabels.Count < 2) return false;
            axisLabels.Sort((a, b) => int.Parse(a.name.Substring(1)).CompareTo(int.Parse(b.name.Substring(1))));
            var first = axisLabels[0] as RectTransform;
            var second = axisLabels[1] as RectTransform;
            if (first == null || second == null) return false;
            start = end = first.anchoredPosition;
            if (axis == 'X') end.x += Mathf.Sign(second.anchoredPosition.x - start.x) * displayLength;
            else end.y += Mathf.Sign(second.anchoredPosition.y - start.y) * displayLength;
            return true;
        }

        private static void PositionAxis(System.Collections.Generic.List<Transform> labels, char axis, float extent,
            Vector2 start, Vector2 end)
        {
            if (extent <= 0) return;
            foreach (Transform label in labels)
            {
                if (label.name.Length < 2 || label.name[0] != axis || !int.TryParse(label.name.Substring(1), out int coordinate)) continue;
                if (label is RectTransform rect)
                    rect.anchoredPosition = Vector2.LerpUnclamped(start, end, coordinate / extent);
            }
        }

        private static System.Collections.Generic.IEnumerable<UnityEngine.SceneManagement.Scene> EnumerableScenes()
        {
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                yield return UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
        }

        private static RawImage[] CreateLights(Transform content, ZoneMapConfig config)
        {
            RemoveStartingWith(content, "MapLight");
            var result = new RawImage[config.lightLayers?.Length ?? 0];
            for (int i = 0; i < result.Length; i++)
            {
                var transform = FindOrCreate($"MapLight{i + 1:00}", content, typeof(RawImage));
                Stretch(transform);
                var image = transform.GetComponent<RawImage>();
                image.texture = config.lightLayers[i];
                image.color = Color.white;
                image.raycastTarget = false;
                transform.SetSiblingIndex(i);
                result[i] = image;
            }
            return result;
        }

        private static ZoneMissionConfig Mission(string zone) =>
            AssetDatabase.LoadAssetAtPath<ZoneMissionConfig>($"Assets/_Project/Content/Missions/{zone}.asset")
            ?? throw new InvalidOperationException("Missing mission config: " + zone);
        private static Texture2D Texture(string relative) =>
            AssetDatabase.LoadAssetAtPath<Texture2D>(Environment + relative)
            ?? throw new InvalidOperationException("Missing texture: " + relative);
        private static MapPoi Poi(string id, float x, float y) => new() { id = id, mapPosition = new Vector2(x, y), arrivalRadius = 20f };
        private static MapPoi ClonePoi(MapPoi source) => source == null ? null : new MapPoi {
            id = source.id, mapPosition = source.mapPosition, arrivalRadius = source.arrivalRadius };

        private static Transform FindOrCreate(string name, Transform parent, params Type[] components)
        {
            var child = parent.Find(name);
            if (child != null) return child;
            var types = new Type[components.Length + 1];
            types[0] = typeof(RectTransform);
            Array.Copy(components, 0, types, 1, components.Length);
            var go = new GameObject(name, types);
            Undo.RegisterCreatedObjectUndo(go, "Create zone map UI");
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static Transform CloneZoneOneElement(Transform sourcePanel, Transform targetPanel, string name, int siblingIndex = -1)
        {
            Remove(targetPanel, name);
            Transform source = sourcePanel.Find(name);
            if (source == null) return null;
            var clone = Object.Instantiate(source.gameObject, targetPanel, false);
            clone.name = name;
            Undo.RegisterCreatedObjectUndo(clone, "Copy Zone01 map UI");
            if (siblingIndex >= 0) clone.transform.SetSiblingIndex(siblingIndex);
            return clone.transform;
        }

        private static void CopyRect(RectTransform source, RectTransform target)
        {
            if (source == null || target == null) return;
            target.anchorMin = source.anchorMin;
            target.anchorMax = source.anchorMax;
            target.pivot = source.pivot;
            target.anchoredPosition = source.anchoredPosition;
            target.sizeDelta = source.sizeDelta;
            target.localScale = Vector3.one;
        }

        private static void WireButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null) return;
            button.onClick = new Button.ButtonClickedEvent();
            UnityEventTools.AddPersistentListener(button.onClick, action);
            EditorUtility.SetDirty(button);
        }

        private static void Place(Transform target, float x, float y, float width, float height)
        {
            var rect = (RectTransform)target;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x + width * .5f, -y - height * .5f);
            rect.localScale = Vector3.one;
        }

        private static void Stretch(Transform target) => Stretch(target, Vector2.zero, Vector2.zero);
        private static void Stretch(Transform target, Vector2 minimum, Vector2 maximum)
        {
            var rect = (RectTransform)target;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = minimum;
            rect.offsetMax = maximum;
            rect.localScale = Vector3.one;
        }

        private static void Remove(Transform parent, string name)
        {
            var item = parent.Find(name);
            if (item != null) Undo.DestroyObjectImmediate(item.gameObject);
        }

        private static void RemoveStartingWith(Transform parent, string prefix)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                if (parent.GetChild(i).name.StartsWith(prefix, StringComparison.Ordinal))
                    Undo.DestroyObjectImmediate(parent.GetChild(i).gameObject);
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
