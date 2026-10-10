using System;
using System.Collections.Generic;
using G10.Prototype.Navigation;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace G10.Prototype.Editor
{
    /// <summary>Checks the real vessel footprint against the current baked terrain before changing an exit.</summary>
    public static class ZoneExitSetupEditor
    {
        private const string ZoneTwoPath = "Assets/_Project/Content/Maps/Zone02.asset";
        private static readonly Vector2 ProposedPosition = new(1830, 75);
        private const float ProposedRadius = 75;

        public static string InspectZoneTwoExitCandidate()
        {
            var config = AssetDatabase.LoadAssetAtPath<ZoneMapConfig>(ZoneTwoPath);
            if (config == null) throw new InvalidOperationException("Zone02 map configuration is missing.");
            var location = Array.Find(config.locations, poi => poi?.id == "zone02-l3");
            if (location == null) throw new InvalidOperationException("Zone02 L3 is missing.");
            bool connected = ValidateConnectivity(config, location.mapPosition, ProposedPosition, out string report);
            return (connected ? "PASS — " : "FAIL — ") + report;
        }

        [MenuItem("G10/Maps/Apply Verified Zone02 Exit")]
        public static void ApplyZoneTwoExit()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before changing the authored exit.");
            string report = InspectZoneTwoExitCandidate();
            if (!report.StartsWith("PASS", StringComparison.Ordinal)) throw new InvalidOperationException(report);
            var config = AssetDatabase.LoadAssetAtPath<ZoneMapConfig>(ZoneTwoPath);
            if (config.exitArea == null) throw new InvalidOperationException("Zone02 exit configuration is missing.");
            if (config.exitArea.mapPosition != ProposedPosition || !Mathf.Approximately(config.exitArea.arrivalRadius, ProposedRadius))
            {
                Undo.RecordObject(config, "Move Zone02 exit to verified water approach");
                config.exitArea.mapPosition = ProposedPosition;
                config.exitArea.arrivalRadius = ProposedRadius;
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssetIfDirty(config);
            }
            Debug.Log(report + $"; exit now {config.exitArea.mapPosition}, radius {config.exitArea.arrivalRadius:0}.");
        }

        public static string InspectAllActivationCandidates()
        {
            var reports = new List<string>();
            bool valid = true;
            for (int zone = 1; zone <= 4; zone++)
            {
                var config = AssetDatabase.LoadAssetAtPath<ZoneMapConfig>($"Assets/_Project/Content/Maps/Zone0{zone}.asset");
                bool result = ValidateActivationArea(config, zone == 4, ProposedRadius, out string report);
                valid &= result;
                reports.Add(report);
            }
            return (valid ? "PASS — " : "FAIL — ") + string.Join("\n", reports);
        }

        [MenuItem("G10/Maps/Apply Verified All-Zone Activation Radii")]
        public static void ApplyAllActivationRadii()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before changing authored activation areas.");
            string report = InspectAllActivationCandidates();
            if (!report.StartsWith("PASS", StringComparison.Ordinal)) throw new InvalidOperationException(report);
            foreach (int zone in new[] { 1, 3, 4 }) // Zone02's existing fix is intentionally untouched.
            {
                var config = AssetDatabase.LoadAssetAtPath<ZoneMapConfig>($"Assets/_Project/Content/Maps/Zone0{zone}.asset");
                var point = zone == 4 ? config.finalHiddenPoint : config.exitArea;
                if (Mathf.Approximately(point.arrivalRadius, ProposedRadius)) continue;
                Undo.RecordObject(config, "Expand verified map activation radius");
                point.arrivalRadius = ProposedRadius;
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssetIfDirty(config);
            }
            Debug.Log(report);
        }

        public static bool ValidateActivationArea(ZoneMapConfig config, bool final, float radius, out string report)
        {
            var point = final ? config?.finalHiddenPoint : config?.exitArea;
            if (point == null || radius <= 0 || !float.IsFinite(radius)) { report = "Missing/invalid activation area."; return false; }
            bool alternate = !string.IsNullOrEmpty(config.alternateObjectiveId) || !string.IsNullOrEmpty(config.alternateWorldFlag);
            if (!ValidateConnectivity(config, config.entryPosition, point.mapPosition, out string connection, alternate))
            { report = connection; return false; }
            var owner = new GameObject("Temporary activation area validation") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                var navigation = owner.AddComponent<ZoneNavigation>();
                config.ApplyTerrain(navigation, alternate);
                bool reachable = TryFindApproach(navigation, point.mapPosition, radius, out Vector2 approach);
                int clear = CountClearSamples(navigation, point.mapPosition, radius);
                int before = clear;
                if (alternate)
                {
                    config.ApplyTerrain(navigation, false);
                    before = CountClearSamples(navigation, point.mapPosition, radius);
                }
                Vector2 low = point.mapPosition - Vector2.one * radius, high = point.mapPosition + Vector2.one * radius;
                bool clipped = low.x < 0 || low.y < 0 || high.x > config.WorldSize.x || high.y > config.WorldSize.y;
                report = $"{connection}; radius {radius:0}, 24-unit clear inward approach {(reachable ? approach.ToString() : "NONE")}; " +
                    $"2-unit lattice clear samples {clear}, before obstacle removal {before}; overlaps map bounds={clipped}.";
                return reachable && clear > 0;
            }
            finally { Object.DestroyImmediate(owner); }
        }

        private static int CountClearSamples(ZoneNavigation navigation, Vector2 center, float radius)
        {
            int clear = 0;
            for (float y = -radius; y <= radius; y += 2)
                for (float x = -radius; x <= radius; x += 2)
                    if (x * x + y * y <= radius * radius && navigation.CanOccupy(center + new Vector2(x, y))) clear++;
            return clear;
        }

        public static bool TryFindApproach(ZoneNavigation navigation, Vector2 center, float radius, out Vector2 approach)
        {
            for (int i = 0; i < 64; i++)
            {
                float angle = i * Mathf.PI * 2 / 64;
                Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 outside = center + direction * (radius + 6);
                bool clear = true;
                for (int walked = 0; walked <= 24 && clear; walked++) clear &= navigation.CanOccupy(outside - direction * walked);
                if (clear) { approach = outside; return true; }
            }
            approach = Vector2.zero;
            return false;
        }

        public static bool ValidateConnectivity(ZoneMapConfig config, Vector2 from, Vector2 to, out string report, bool alternate = false)
        {
            if (config == null) { report = "No map configuration."; return false; }
            var owner = new GameObject("Temporary exit terrain validation") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                var navigation = owner.AddComponent<ZoneNavigation>();
                if (!config.ApplyTerrain(navigation, alternate)) { report = "No baked terrain mask."; return false; }
                if (!navigation.CanOccupy(from) || !navigation.CanOccupy(to))
                {
                    report = $"{config.zoneId}: source water/footprint={navigation.CanOccupy(from)}, destination water/footprint={navigation.CanOccupy(to)} (alternate={alternate}).";
                    return false;
                }
                // A two-map-unit navigation lattice and one-unit segment checks mirror the
                // vessel's runtime movement substeps, not the map's displayed UI grid.
                const float step = 2;
                int columns = Mathf.CeilToInt(config.WorldSize.x / step), rows = Mathf.CeilToInt(config.WorldSize.y / step);
                Vector2Int start = ClosestGridPoint(navigation, from, step, columns, rows);
                Vector2Int target = ClosestGridPoint(navigation, to, step, columns, rows);
                if (start.x < 0 || target.x < 0) { report = "No clear connection to the navigation lattice."; return false; }
                var visited = new bool[columns * rows];
                var frontier = new Queue<int>();
                int first = start.y * columns + start.x, last = target.y * columns + target.x;
                visited[first] = true; frontier.Enqueue(first);
                int reached = 0;
                while (frontier.Count > 0)
                {
                    int current = frontier.Dequeue(); reached++;
                    if (current == last)
                    {
                        report = $"{config.zoneId}: {from} → destination {to} connected through clear water (alternate={alternate}); footprint clearance 2 units, {reached} visited nodes.";
                        return true;
                    }
                    int x = current % columns, y = current / columns;
                    Enqueue(x - 1, y); Enqueue(x + 1, y); Enqueue(x, y - 1); Enqueue(x, y + 1);
                    void Enqueue(int nextX, int nextY)
                    {
                        if (nextX < 0 || nextX >= columns || nextY < 0 || nextY >= rows) return;
                        int next = nextY * columns + nextX;
                        if (visited[next]) return;
                        Vector2 point = new(nextX * step, nextY * step);
                        Vector2 midpoint = new((x + nextX) * step * .5f, (y + nextY) * step * .5f);
                        if (navigation.CanOccupy(point) && navigation.CanOccupy(midpoint))
                        { visited[next] = true; frontier.Enqueue(next); }
                    }
                }
                report = $"{config.zoneId}: no navigable source-to-destination connection ({reached} nodes explored).";
                return false;
            }
            finally { Object.DestroyImmediate(owner); }
        }

        private static Vector2Int ClosestGridPoint(ZoneNavigation navigation, Vector2 point, float step, int columns, int rows)
        {
            int centerX = Mathf.RoundToInt(point.x / step), centerY = Mathf.RoundToInt(point.y / step);
            Vector2Int closest = new(-1, -1);
            float best = float.PositiveInfinity;
            for (int y = centerY - 1; y <= centerY + 1; y++)
                for (int x = centerX - 1; x <= centerX + 1; x++)
                {
                    if (x < 0 || x >= columns || y < 0 || y >= rows) continue;
                    Vector2 candidate = new(x * step, y * step);
                    float distance = Vector2.Distance(point, candidate);
                    if (distance >= best || !navigation.CanOccupy(candidate)) continue;
                    bool clear = true;
                    for (float walked = 0; walked < distance && clear; walked += 1)
                        clear &= navigation.CanOccupy(Vector2.MoveTowards(point, candidate, walked));
                    if (!clear) continue;
                    best = distance; closest = new Vector2Int(x, y);
                }
            return closest;
        }
    }
}
