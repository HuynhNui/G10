using UnityEngine;

namespace G10.Prototype.Navigation
{
    /// <summary>Finds the ship's connected water region; the other side of chart contours is solid radar terrain.</summary>
    internal static class RadarTerrainMask
    {
        public static byte[] Build(byte[] water, int columns, int rows, Vector2 origin, Vector2 worldSize)
        {
            var reachable = new byte[columns * rows];
            var expandedLine = new byte[reachable.Length];
            // Temporarily pad contours by one cell to seal diagonal seams and small hand-drawn breaks.
            // Flood before removing padding: eroding first can reopen a thin diagonal bridge.
            // Collision data is never modified. The whole chart is used, not just the radar circle.
            for (int y = 0; y < rows; y++)
            for (int x = 0; x < columns; x++)
            {
                bool touchesLine = false;
                for (int dy = -1; dy <= 1 && !touchesLine; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int sx = x + dx, sy = y + dy;
                    if (sx >= 0 && sx < columns && sy >= 0 && sy < rows && water[sy * columns + sx] == 0)
                    { touchesLine = true; break; }
                }
                if (touchesLine) reachable[y * columns + x] = 2;
            }
            var queue = new int[reachable.Length];
            worldSize.x = Mathf.Max(.01f, worldSize.x);
            worldSize.y = Mathf.Max(.01f, worldSize.y);
            Vector2 uv = new(origin.x / worldSize.x, origin.y / worldSize.y);
            int originX = Mathf.Clamp(Mathf.FloorToInt(uv.x * columns), 0, columns - 1);
            int originY = Mathf.Clamp(Mathf.FloorToInt(uv.y * rows), 0, rows - 1);
            int head = 0, tail = 0;
            int seed = originY * columns + originX;
            reachable[seed] = 1;
            queue[tail++] = seed;
            while (head < tail)
            {
                int cell = queue[head++], x = cell % columns, y = cell / columns;
                if (x > 0) Visit(cell - 1);
                if (x + 1 < columns) Visit(cell + 1);
                if (y > 0) Visit(cell - columns);
                if (y + 1 < rows) Visit(cell + columns);
            }
            // Restore the temporary padding only on the water side, keeping every original line pixel solid.
            // This is a bounded one-cell restoration, never a second flood through the repaired seam.
            for (int y = 0; y < rows; y++)
            for (int x = 0; x < columns; x++)
            {
                int cell = y * columns + x;
                if (water[cell] == 0) continue;
                bool touchesWater = false;
                for (int dy = -1; dy <= 1 && !touchesWater; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int sx = x + dx, sy = y + dy;
                    if (sx >= 0 && sx < columns && sy >= 0 && sy < rows && reachable[sy * columns + sx] == 1)
                    { touchesWater = true; break; }
                }
                if (touchesWater) expandedLine[cell] = 1;
            }
            return expandedLine;

            void Visit(int cell)
            {
                if (reachable[cell] != 0) return;
                reachable[cell] = 1;
                queue[tail++] = cell;
            }
        }
    }
}
