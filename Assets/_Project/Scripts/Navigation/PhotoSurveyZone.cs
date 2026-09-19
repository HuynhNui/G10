using G10.Prototype.Computer;
using UnityEngine;

namespace G10.Prototype.Navigation
{
    [System.Serializable]
    public sealed class MapPoi
    {
        public string id;
        public Vector2 mapPosition;
        [Min(.1f)] public float arrivalRadius = 20f;
        public bool Contains(Vector2 point) => arrivalRadius > 0 && (point - mapPosition).sqrMagnitude <= arrivalRadius * arrivalRadius;
    }

    /// <summary>Authored Zone01 survey POI and stationary Creature01 world record; not a randomized scan result.</summary>
    public sealed class PhotoSurveyZone : MonoBehaviour
    {
        public MissionDefinition mission;
        public MapPoi[] locations = System.Array.Empty<MapPoi>();
        public MapPoi TargetPoi
        {
            get
            {
                if (mission == null || string.IsNullOrEmpty(mission.targetPoiId) || locations == null) return null;
                foreach (var poi in locations)
                    if (poi != null && poi.id == mission.targetPoiId) return poi;
                return null;
            }
        }
        // Compatibility accessor for radar/camera consumers; the POI owns the position.
        public Vector2 center => TargetPoi != null ? TargetPoi.mapPosition : Vector2.zero;
        [Min(0)] public float targetDepth = 230;
        public string creatureId = "Creature01";
        public bool creaturePresent = true;
        public enum TaskKind { Photograph, Capture }
        [Tooltip("Required objectives for this map location, completed during the current voyage.")]
        public TaskKind[] tasks = { TaskKind.Photograph, TaskKind.Capture };
        private readonly System.Collections.Generic.HashSet<TaskKind> completed = new();
        public int CompletedCount
        {
            get { int count = 0; if (tasks != null) foreach (var task in tasks) if (completed.Contains(task)) count++; return count; }
        }
        public bool IsComplete => tasks != null && tasks.Length > 0 && CompletedCount == tasks.Length;
        public bool CanCapture
        {
            get { if (tasks != null) foreach (var task in tasks) if (task != TaskKind.Capture && !completed.Contains(task)) return false; return true; }
        }
        public bool IsTaskComplete(TaskKind task) => completed.Contains(task);
        public void CompleteTask(TaskKind task) => completed.Add(task);
        public TaskKind[] ExportProgress()
        { var result = new TaskKind[completed.Count]; completed.CopyTo(result); return result; }
        public void RestoreProgress(TaskKind[] progress, bool present)
        {
            completed.Clear();
            if (progress != null) foreach (var task in progress) completed.Add(task);
            creaturePresent = present;
        }
        public string TaskDescription()
        {
            var text = new System.Text.StringBuilder($"P01 • NHIỆM VỤ ({CompletedCount}/{tasks?.Length ?? 0})");
            if (tasks != null) foreach (var task in tasks)
                text.Append("\n").Append(IsTaskComplete(task) ? "[x] " : "[ ] ")
                    .Append(task == TaskKind.Photograph ? "Chụp ảnh nhận diện sinh vật" : "Bắt sinh vật");
            if (IsComplete) text.Append("\nĐÃ HOÀN THÀNH KHU VỰC");
            return text.ToString();
        }
        public Vector3 CreaturePosition => new(center.x, center.y, -targetDepth);
        public bool Contains(Vector2 point) => TargetPoi != null && TargetPoi.Contains(point);
        public bool Detectable(ZoneNavigation navigation, float range)
        {
            if (TargetPoi == null || navigation == null || !creaturePresent || Vector3.Distance(navigation.WorldPosition, CreaturePosition) > range) return false;
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(navigation.Position, center) / 2));
            for (int i = 1; i <= steps; i++)
                if (!navigation.IsWater(Vector2.Lerp(navigation.Position, center, (float)i / steps))) return false;
            return true;
        }
    }
}
