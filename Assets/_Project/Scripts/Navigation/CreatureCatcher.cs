using UnityEngine;

namespace G10.Prototype.Navigation
{
    /// <summary>Capture checks the live encounter, never a stale radar echo.</summary>
    public sealed class CreatureCatcher : MonoBehaviour
    {
        public ZoneNavigation navigation;
        public PhotoSurveyZone survey;
        public CreatureInventory inventory;
        public Texture2D itemIcon;
        public string itemName = "Sinh vật Zone 1";
        [Min(.1f)] public float captureRadius = 20f;
        [Min(0f)] public float depthTolerance = 10f;
        public enum Result { Caught, Empty, Full, Unavailable, PhotoRequired }
        public Result TryCapture()
        {
            if (navigation == null || survey == null || inventory == null || itemIcon == null)
                return Result.Unavailable;
            if (!survey.creaturePresent || Vector2.Distance(navigation.Position, survey.center) > captureRadius ||
                Mathf.Abs(navigation.Depth - survey.targetDepth) > depthTolerance ||
                !survey.Detectable(navigation, Mathf.Sqrt(captureRadius * captureRadius + depthTolerance * depthTolerance)))
                return Result.Empty;
            if (inventory.IsFull) return Result.Full;
            if (!survey.CanCapture) return Result.PhotoRequired;
            if (!inventory.TryAdd(survey.creatureId, itemName, itemIcon)) return Result.Empty;
            survey.creaturePresent = false;
            survey.CompleteTask(PhotoSurveyZone.TaskKind.Capture);
            return Result.Caught;
        }
    }
}
