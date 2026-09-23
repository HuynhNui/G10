using System;
using UnityEngine;

namespace G10.Prototype.Navigation
{
    /// <summary>Finds the ship's connected water region; the other side of chart contours is solid radar terrain.</summary>
    internal static class RadarTerrainMask
    {
        public static byte[] Build(int columns, int rows, Vector2 origin, Func<Vector2, bool> canOccupy)
        {
            var reachable = new byte[columns * rows];
            var queue = new int[reachable.Length];
            Vector2 uv = ZoneNavigation.CoordinatesToUV(origin);
            int originX = Mathf.Clamp(Mathf.FloorToInt(uv.x * columns), 0, columns - 1);
            int originY = Mathf.Clamp(Mathf.FloorToInt(uv.y * rows), 0, rows - 1);
            int head = 0, tail = 0;
            // Seed the actual ship position: its containing cell's centre can be slightly nearer a wall.
            if (!canOccupy(origin)) return reachable;
            int seed = originY * columns + originX;
            reachable[seed] = 1;
            queue[tail++] = seed;
            while (head < tail)
            {
                int cell = queue[head++], x = cell % columns, y = cell / columns;
                if (x > 0) Visit(cell - 1, x - 1, y);
                if (x + 1 < columns) Visit(cell + 1, x + 1, y);
                if (y > 0) Visit(cell - columns, x, y - 1);
                if (y + 1 < rows) Visit(cell + columns, x, y + 1);
            }
            return reachable;

            void Visit(int cell, int x, int y)
            {
                if (reachable[cell] != 0) return;
                Vector2 point = ZoneNavigation.UVToCoordinates(new Vector2((x + .5f) / columns, (y + .5f) / rows));
                // Use existing hull clearance so sub-ship-width gaps in hand-drawn lines cannot leak the fill.
                if (!canOccupy(point)) { reachable[cell] = 2; return; }
                reachable[cell] = 1;
                queue[tail++] = cell;
            }
        }
    }
}
