using UnityEngine;

namespace G10.Prototype.Navigation
{
    /// <summary>Authored Zone01 survey cell and stationary Creature01 world record; not a randomized scan result.</summary>
    public sealed class PhotoSurveyZone : MonoBehaviour
    {
        public Vector2 center = new(625, 125);
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
        public bool Contains(Vector2 point) => Mathf.Abs(point.x - center.x) < 25 && Mathf.Abs(point.y - center.y) < 25;
        public bool Detectable(ZoneNavigation navigation, float range)
        {
            if (!creaturePresent || Vector3.Distance(navigation.WorldPosition, CreaturePosition) > range) return false;
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(navigation.Position, center) / 2));
            for (int i = 1; i <= steps; i++)
                if (!navigation.IsWater(Vector2.Lerp(navigation.Position, center, (float)i / steps))) return false;
            return true;
        }
    }
}
