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

        [SerializeField, HideInInspector] private int terrainColumns;
        [SerializeField, HideInInspector] private int terrainRows;
        [SerializeField, HideInInspector] private byte[] terrainMaskRle = Array.Empty<byte>();
        [SerializeField, HideInInspector] private byte[] alternateTerrainMaskRle = Array.Empty<byte>();

        public Vector2 GridCoordinateStep
        {
            get
            {
                if (map == null || locationMarker == null || map.width <= 0 || map.height <= 0)
                    return Vector2.one * ZoneNavigation.ChartCellSize;
                return new Vector2(1200f * locationMarker.width / map.width, 700f * locationMarker.height / map.height);
            }
        }

        /// <summary>
        /// Returns the visual center of the grid cell nearest to an authored POI.
        /// The authored coordinate remains the gameplay coordinate; only map art is aligned.
        /// </summary>
        public Vector2 GridCellCenter(Vector2 coordinate)
        {
            Vector2 step = GridCoordinateStep;
            return new Vector2(
                SnapToCellCenter(coordinate.x, step.x, 1200f),
                SnapToCellCenter(coordinate.y, step.y, 700f));
        }

        private static float SnapToCellCenter(float value, float step, float extent)
        {
            if (step <= Mathf.Epsilon) return Mathf.Clamp(value, 0f, extent);
            float center = (Mathf.Round((value - step * .5f) / step) + .5f) * step;
            return Mathf.Clamp(center, step * .5f, extent - step * .5f);
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
            if (navigation == null || terrainColumns <= 0 || terrainRows <= 0) return false;
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
