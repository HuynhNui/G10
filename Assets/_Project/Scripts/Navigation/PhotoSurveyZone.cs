using System;
using System.Collections.Generic;
using G10.Prototype.Computer;
using G10.Prototype.Missions;
using UnityEngine;

namespace G10.Prototype.Navigation
{
    [Serializable]
    public sealed class MapPoi
    {
        public string id;
        public Vector2 mapPosition;
        [Min(.1f)] public float arrivalRadius = 20f;
        public bool overrideDepth;
        [Min(0)] public float targetDepth;
        public bool Contains(Vector2 point) => arrivalRadius > 0 && (point - mapPosition).sqrMagnitude <= arrivalRadius * arrivalRadius;
    }

    /// <summary>Authored POIs and persisted contacts. No mission location is selected globally.</summary>
    public sealed class PhotoSurveyZone : MonoBehaviour
    {
        public ZoneOneStory Story;
        [SerializeField] private ZoneMissionRuntime missionRuntime;
        public ZoneMissionRuntime MissionRuntime { get => missionRuntime != null ? missionRuntime : Story; set => missionRuntime = value; }
        public MissionDefinition mission;
        public MapPoi[] locations = Array.Empty<MapPoi>();

        // Compatibility for template missions only; mission gameplay resolves a POI per interaction.
        public MapPoi TargetPoi => mission == null ? null : FindPoi(mission.targetPoiId);
        public Vector2 center => TargetPoi != null ? TargetPoi.mapPosition : Vector2.zero;
        [Min(0)] public float targetDepth = 230;
        [Tooltip("Ship depth must be between the target depth and this many metres deeper.")]
        [Min(0)] public float deeperInteractionRange = 30f;
        public float DeeperInteractionRange => float.IsFinite(deeperInteractionRange) ? Mathf.Max(0, deeperInteractionRange) : 30f;
        public bool IsInteractionDepth(float shipDepth, MapPoi poi) => poi != null && float.IsFinite(shipDepth) &&
            shipDepth >= DepthFor(poi) && shipDepth <= DepthFor(poi) + DeeperInteractionRange;
        /// <summary>Single POI depth rule; invalid values are sanitized at use, never written back to content.</summary>
        public float DepthFor(MapPoi poi)
        {
            float depth = poi != null && poi.overrideDepth ? poi.targetDepth : targetDepth;
            return float.IsFinite(depth) ? Mathf.Max(0, depth) : 0;
        }
        public string creatureId = "Creature01";
        public bool creaturePresent = true;
        public enum TaskKind { Photograph, Capture }
        public TaskKind[] tasks = { TaskKind.Photograph, TaskKind.Capture };

        private readonly HashSet<TaskKind> completed = new();
        private int creatureSpawnDay;
        private readonly Dictionary<string, Vector2> creatureSpawns = new();

        public int CompletedCount
        {
            get
            {
                if (MissionRuntime != null) return MissionRuntime.CompletedCount;
                int count = 0;
                if (tasks != null) foreach (var task in tasks) if (completed.Contains(task)) count++;
                return count;
            }
        }
        public bool IsComplete => MissionRuntime != null ? MissionRuntime.Complete : tasks != null && tasks.Length > 0 && CompletedCount == tasks.Length;
        public bool CanCapture
        {
            get { if (tasks != null) foreach (var task in tasks) if (task != TaskKind.Capture && !completed.Contains(task)) return false; return true; }
        }
        public bool IsTaskComplete(TaskKind task) => completed.Contains(task);
        public void CompleteTask(TaskKind task) => completed.Add(task);
        public TaskKind[] ExportProgress() { var result = new TaskKind[completed.Count]; completed.CopyTo(result); return result; }
        public void RestoreProgress(TaskKind[] progress, bool present)
        {
            completed.Clear();
            if (progress != null) foreach (var task in progress) completed.Add(task);
            creaturePresent = present;
        }

        public string TaskDescription() => MissionRuntime != null ? MissionRuntime.MissionText() : TemplateTaskDescription();

        public MapPoi FindPoi(string id)
        {
            if (locations == null || string.IsNullOrEmpty(id)) return null;
            return Array.Find(locations, poi => poi != null && poi.id == id);
        }

        /// <summary>Finds the independently authored POI whose gameplay arrival radius contains the position.</summary>
        public MapPoi FindPoiContaining(Vector2 position)
        {
            if (locations == null) return null;
            MapPoi nearest = null;
            float nearestDistance = float.PositiveInfinity;
            foreach (var poi in locations)
            {
                if (poi == null || !poi.Contains(position)) continue;
                Vector2 delta = position - poi.mapPosition;
                float distance = delta.sqrMagnitude;
                if (distance < nearestDistance) { nearest = poi; nearestDistance = distance; }
            }
            return nearest;
        }

        public bool Contains(Vector2 point) => FindPoiContaining(point) != null;
        public MapPoi FindNearestContact(Vector2 position, bool includeAbsent = false)
        {
            MapPoi nearest = null;
            float distance = float.PositiveInfinity;
            foreach (var poi in locations ?? Array.Empty<MapPoi>())
            {
                if (!HasCreatureSpawnAt(poi) || !includeAbsent && !IsRadarContactPresent(poi)) continue;
                float candidate = (position - ContactPosition(poi)).sqrMagnitude;
                if (candidate < distance) { nearest = poi; distance = candidate; }
            }
            return nearest;
        }
        public MapPoi FindContactContaining(Vector2 position)
        {
            if (locations == null) return null;
            MapPoi nearest = null;
            float nearestDistance = float.PositiveInfinity;
            foreach (var poi in locations)
            {
                if (poi == null || !IsRadarContactPresent(poi)) continue;
                float distance = (position - ContactPosition(poi)).sqrMagnitude;
                if (distance > poi.arrivalRadius * poi.arrivalRadius || distance >= nearestDistance) continue;
                nearest = poi; nearestDistance = distance;
            }
            return nearest;
        }
        public Vector2 CreatureMapPosition => TargetPoi != null ? ContactPosition(TargetPoi) : center;
        public Vector2 ContactPosition(MapPoi poi)
            => poi != null && creatureSpawns.TryGetValue(poi.id, out var position) ? position : poi != null ? poi.mapPosition : Vector2.zero;
        public Vector3 CreaturePosition => ContactWorldPosition(TargetPoi);
        public Vector3 ContactWorldPosition(MapPoi poi)
        { Vector2 point = ContactPosition(poi); return new Vector3(point.x, point.y, -DepthFor(poi)); }
        public int CreatureSpawnDay => creatureSpawnDay;
        public string CreatureSpawnPoiId => TargetPoi != null && HasCreatureSpawnAt(TargetPoi) ? TargetPoi.id : null;
        public bool HasCreatureSpawn => TargetPoi != null && HasCreatureSpawnAt(TargetPoi);
        public bool HasCreatureSpawnAt(MapPoi poi) => poi != null && creatureSpawns.ContainsKey(poi.id);

        public void ResetContacts()
        {
            creatureSpawns.Clear();
            creatureSpawnDay = 0;
        }

        public void RestoreCreatureSpawn(int day, string poiId, Vector2 position)
        {
            if (day != creatureSpawnDay) { creatureSpawns.Clear(); creatureSpawnDay = day; }
            if (day <= 0 || string.IsNullOrEmpty(poiId) || !float.IsFinite(position.x) || !float.IsFinite(position.y)) return;
            creatureSpawns[poiId] = position;
        }

        public bool IsRadarContactPresent(MapPoi poi)
        {
            if (poi == null || !creatureSpawns.ContainsKey(poi.id)) return false;
            return MissionRuntime != null ? MissionRuntime.IsContentPresent(poi.id) : poi == TargetPoi && creaturePresent;
        }

        public bool TryGetRadarContact(ZoneNavigation navigation, float range, out MapPoi poi, out Vector2 position)
        {
            poi = null; position = default;
            if (navigation == null || locations == null) return false;
            float nearest = float.PositiveInfinity;
            foreach (var candidate in locations)
            {
                if (!IsRadarContactPresent(candidate) || !IsInteractionDepth(navigation.Depth, candidate)) continue;
                Vector2 coordinate = ContactPosition(candidate);
                float distance = Vector2.Distance(navigation.Position, coordinate);
                if (distance > range || distance >= nearest || !HasClearPath(navigation, coordinate)) continue;
                nearest = distance; poi = candidate; position = coordinate;
            }
            return poi != null;
        }

        public bool TryGetPhotoContact(ZoneNavigation navigation, float range, float fieldOfView, out MapPoi poi, out Vector3 position)
        {
            poi = null; position = default;
            if (navigation == null || locations == null) return false;
            float bestAngle = float.PositiveInfinity;
            float bestDistance = float.PositiveInfinity;
            foreach (var candidate in locations)
            {
                if (!IsRadarContactPresent(candidate) || !IsInteractionDepth(navigation.Depth, candidate)) continue;
                Vector3 world = ContactWorldPosition(candidate);
                Vector3 delta = world - navigation.WorldPosition;
                float angle = Mathf.Abs(Mathf.DeltaAngle(navigation.Heading, Mathf.Atan2(delta.x, delta.y) * Mathf.Rad2Deg));
                float distance = delta.magnitude;
                if (distance > range || angle > fieldOfView * .5f || !HasClearPath(navigation, ContactPosition(candidate))) continue;
                if (angle > bestAngle || Mathf.Approximately(angle, bestAngle) && distance >= bestDistance) continue;
                bestAngle = angle; bestDistance = distance; poi = candidate; position = world;
            }
            return poi != null;
        }

        public bool Detectable(ZoneNavigation navigation, MapPoi poi, float range)
        {
            if (poi == null || navigation == null || !IsRadarContactPresent(poi) ||
                !IsInteractionDepth(navigation.Depth, poi) || Vector2.Distance(navigation.Position, ContactPosition(poi)) > range) return false;
            return HasClearPath(navigation, ContactPosition(poi));
        }

        public bool Detectable(ZoneNavigation navigation, float range) => Detectable(navigation, FindPoiContaining(navigation != null ? navigation.Position : default), range);

        private string TemplateTaskDescription()
        {
            var text = new System.Text.StringBuilder($"P01 • NHIỆM VỤ ({CompletedCount}/{tasks?.Length ?? 0})");
            if (tasks != null) foreach (var task in tasks)
                text.Append("\n").Append(IsTaskComplete(task) ? "[x] " : "[ ] ")
                    .Append(task == TaskKind.Photograph ? "Chụp ảnh nhận diện sinh vật" : "Bắt sinh vật");
            if (IsComplete) text.Append("\nĐÃ HOÀN THÀNH KHU VỰC");
            return text.ToString();
        }

        private static bool HasClearPath(ZoneNavigation navigation, Vector2 target)
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(navigation.Position, target) / 2));
            for (int i = 1; i <= steps; i++)
                if (!navigation.IsWater(Vector2.Lerp(navigation.Position, target, (float)i / steps))) return false;
            return true;
        }
    }
}
