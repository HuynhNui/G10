using System;
using System.Collections.Generic;
using G10.Prototype.Missions;
using UnityEngine;

namespace G10.Prototype.Navigation
{
    /// <summary>Art, POIs and authored terrain data for one zone map.</summary>
    [CreateAssetMenu(menuName = "G10/Navigation/Zone Map Config")]
    public sealed class ZoneMapConfig : ScriptableObject
    {
        public string zoneId;
        public ZoneMissionConfig missionConfig;
        public Texture2D map;
        public Texture2D terrainLine;
        public Texture2D alternateMap;
        public Texture2D alternateTerrainLine;
        [Tooltip("The alternate map becomes active after this objective is complete.")]
        public string alternateObjectiveId;
        [Tooltip("The alternate map also becomes active when this world flag exists.")]
        public string alternateWorldFlag;
        public Texture2D locationMarker;
        public Texture2D[] lightLayers = Array.Empty<Texture2D>();
        public MapPoi[] locations = Array.Empty<MapPoi>();

        [Header("Expedition route (map coordinates)")]
        public Vector2 entryPosition;
        public float entryHeading;
        [Min(0)] public float entryDepth = 230f;
        public MapPoi exitArea = new() { id = "exit", arrivalRadius = 45f };
        public string destinationZone;
        [Tooltip("The rock-breaker action is only available within this approach area.")]
        public MapPoi rockInteractionArea = new() { id = "rock", arrivalRadius = 65f };
        [Tooltip("Stable mission location IDs, not POI display names.")]
        public string[] hiddenLocationIds = Array.Empty<string>();
        public MapPoi finalHiddenPoint = new() { id = "final-signal", arrivalRadius = 35f };

        [Header("Map coordinate system")]
        [Tooltip("Size of one displayed source-image grid cell and its matching map-coordinate interval.")]
        [SerializeField, Min(.01f)] private float gridSize = 50f;
        [SerializeField, HideInInspector] private Vector2 mapDisplaySize = new(1440f, 840f);
        [SerializeField, HideInInspector] private int coordinateVersion;

        [SerializeField, HideInInspector] private int terrainColumns;
        [SerializeField, HideInInspector] private int terrainRows;
        [SerializeField, HideInInspector] private byte[] terrainMaskRle = Array.Empty<byte>();
        [SerializeField, HideInInspector] private byte[] alternateTerrainMaskRle = Array.Empty<byte>();

        public const int CurrentCoordinateVersion = 3;
        public float GridSize => Mathf.Max(.01f, gridSize);
        public int GridColumns => GridCount(WorldSize.x, GridSize);
        public int GridRows => GridCount(WorldSize.y, GridSize);
        public Vector2 WorldSize => map != null
            ? new Vector2(Mathf.Max(GridSize, map.width), Mathf.Max(GridSize, map.height))
            : LegacyDisplayWorldSize;
        public Vector2 LegacyDisplayWorldSize => new(
            Mathf.Max(GridSize, mapDisplaySize.x), Mathf.Max(GridSize, mapDisplaySize.y));
        public Vector2 LegacyLocationSizedWorldSize => new(
            LegacyLocationGridCount(map != null ? map.width : 0, locationMarker != null ? locationMarker.width : 0) * GridSize,
            LegacyLocationGridCount(map != null ? map.height : 0, locationMarker != null ? locationMarker.height : 0) * GridSize);
        public Vector2 GridCoordinateStep => Vector2.one * GridSize;
        public int CoordinateVersion => coordinateVersion;

        public void ConfigureGrid(float cellSize) => gridSize = Mathf.Max(.01f, cellSize);
        public void ConfigureDisplaySize(Vector2 size) => mapDisplaySize = new(
            Mathf.Max(GridSize, size.x), Mathf.Max(GridSize, size.y));

        public bool MigrateCoordinates(Vector2 previousWorldSize)
        {
            if (coordinateVersion >= CurrentCoordinateVersion) return false;
            Vector2 source = coordinateVersion switch
            {
                1 => LegacyLocationSizedWorldSize,
                2 => LegacyDisplayWorldSize,
                _ => previousWorldSize
            };
            Vector2 next = WorldSize;
            Vector2 scale = new(
                next.x / Mathf.Max(.01f, source.x),
                next.y / Mathf.Max(.01f, source.y));
            foreach (MapPoi location in locations ?? Array.Empty<MapPoi>())
                if (location != null) location.mapPosition = Vector2.Scale(location.mapPosition, scale);
            coordinateVersion = CurrentCoordinateVersion;
            return true;
        }

        public void MarkCoordinatesCurrent() => coordinateVersion = CurrentCoordinateVersion;

        private static int GridCount(float displayPixels, float cellPixels) =>
            Mathf.Max(1, Mathf.CeilToInt(displayPixels / Mathf.Max(.01f, cellPixels)));

        private static int LegacyLocationGridCount(int mapPixels, int locationPixels) =>
            mapPixels > 0 && locationPixels > 0 ? Mathf.Max(1, Mathf.RoundToInt((float)mapPixels / locationPixels)) : 1;

        public Vector2 CoordinatesToUV(Vector2 coordinate)
        {
            Vector2 size = WorldSize;
            return new Vector2(coordinate.x / size.x, coordinate.y / size.y);
        }

        public Vector2 UVToCoordinates(Vector2 normalized)
        {
            Vector2 size = WorldSize;
            return new Vector2(normalized.x * size.x, normalized.y * size.y);
        }

        /// <summary>
        /// Returns the visual center of the grid cell nearest to an authored POI.
        /// The authored coordinate remains the gameplay coordinate; only map art is aligned.
        /// </summary>
        public Vector2 GridCellCenter(Vector2 coordinate)
        {
            Vector2 size = WorldSize;
            return new Vector2(
                SnapToCellCenter(coordinate.x, GridSize, size.x),
                SnapToCellCenter(coordinate.y, GridSize, size.y));
        }

        private static float SnapToCellCenter(float value, float step, float extent)
        {
            if (step <= Mathf.Epsilon) return Mathf.Clamp(value, 0f, extent);
            float center = (Mathf.Round((value - step * .5f) / step) + .5f) * step;
            float firstCenter = Mathf.Min(step * .5f, extent * .5f);
            float lastCenter = Mathf.Max(firstCenter, extent - step * .5f);
            return Mathf.Clamp(center, firstCenter, lastCenter);
        }

        private void OnValidate()
        {
            gridSize = Mathf.Max(.01f, gridSize);
            mapDisplaySize = new Vector2(Mathf.Max(gridSize, mapDisplaySize.x), Mathf.Max(gridSize, mapDisplaySize.y));
        }

        public bool UsesAlternate(ZoneMissionRuntime runtime)
        {
            if (runtime == null) return false;
            return !string.IsNullOrEmpty(alternateObjectiveId) && runtime.HasObjective(alternateObjectiveId) ||
                !string.IsNullOrEmpty(alternateWorldFlag) && runtime.HasWorldFlag(alternateWorldFlag);
        }

        public Texture2D MapFor(ZoneMissionRuntime runtime) => UsesAlternate(runtime) && alternateMap != null ? alternateMap : map;
        public Texture2D TerrainLineFor(ZoneMissionRuntime runtime) => UsesAlternate(runtime) && alternateTerrainLine != null ? alternateTerrainLine : terrainLine;

        public void StoreTerrainMask(byte[] cells, int columns, int rows, bool alternate)
        {
            if (cells == null || columns <= 0 || rows <= 0 || cells.Length != columns * rows)
                throw new ArgumentException("Terrain mask dimensions do not match its data.");
            terrainColumns = columns;
            terrainRows = rows;
            if (alternate) alternateTerrainMaskRle = Encode(cells);
            else terrainMaskRle = Encode(cells);
        }

        public bool ApplyTerrain(ZoneNavigation navigation, bool alternate)
        {
            if (navigation == null) return false;
            navigation.ConfigureMapCoordinates(WorldSize, GridSize);
            if (terrainColumns <= 0 || terrainRows <= 0) return false;
            byte[] source = alternate && alternateTerrainMaskRle != null && alternateTerrainMaskRle.Length > 0
                ? alternateTerrainMaskRle : terrainMaskRle;
            byte[] cells = Decode(source, terrainColumns * terrainRows);
            if (cells == null) return false;
            navigation.SetChart(cells, terrainColumns, terrainRows);
            return true;
        }

        private static byte[] Encode(byte[] source)
        {
            var result = new List<byte>();
            for (int i = 0; i < source.Length;)
            {
                byte value = source[i];
                int length = 1;
                while (i + length < source.Length && source[i + length] == value && length < ushort.MaxValue) length++;
                result.Add(value);
                result.Add((byte)length);
                result.Add((byte)(length >> 8));
                i += length;
            }
            return result.ToArray();
        }

        private static byte[] Decode(byte[] source, int expectedLength)
        {
            if (source == null || source.Length == 0 || source.Length % 3 != 0) return null;
            var result = new byte[expectedLength];
            int destination = 0;
            for (int i = 0; i < source.Length; i += 3)
            {
                int length = source[i + 1] | source[i + 2] << 8;
                if (length <= 0 || destination + length > result.Length) return null;
                Array.Fill(result, source[i], destination, length);
                destination += length;
            }
            return destination == result.Length ? result : null;
        }
    }
}
