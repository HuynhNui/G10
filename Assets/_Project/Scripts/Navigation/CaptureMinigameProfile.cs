using UnityEngine;
using UnityEngine.Serialization;

namespace G10.Prototype.Navigation
{
    [CreateAssetMenu(menuName = "G10/Capture/Minigame profile")]
    public sealed class CaptureMinigameProfile : ScriptableObject
    {
        [Header("Hook steering")]
        [FormerlySerializedAs("horizontalSpeed"), Min(1)] public float hookMoveSpeed = 300;
        [Min(1)] public float hookTurnSpeed = 220;
        [Range(-89, 0)] public float minHookHeading = -65;
        [Range(0, 89)] public float maxHookHeading = 65;
        [Header("Fish steering")]
        [FormerlySerializedAs("creatureMoveSpeed"), Min(0)] public float fishMoveSpeed = 100;
        [Min(1)] public float fishTurnSpeed = 100;
        [Min(.1f)] public float fishMinDecisionInterval = 1;
        [Min(.1f)] public float fishMaxDecisionInterval = 1.8f;
        [Min(1)] public float fishFleeSpeedMultiplier = 1.6f;
        [Min(.1f)] public float fishFleeDuration = .6f;
        [Min(0)] public float fishBoundaryMargin = 80;
        [Min(1)] public float ropeThickness = 3;
        [Min(1)] public int requiredHits = 5;
        [Min(1)] public float attemptDuration = 45;
        [Min(.01f)] public float hitCooldown = .35f;
        [Range(.1f, .2f)] public float hitPause = .15f;
        [Min(.1f)] public float resultDuration = .8f;
        [Header("UI-space collision boxes (map coordinates are not used)")]
        public Vector2 hookSize = new(110, 51);
        public Vector2 creatureSize = new(100, 62);
        public Vector2 hookHitbox = new(45, 34);
        public Vector2 hookHitboxOffset = new(28, 0);
        public Vector2 creatureHitbox = new(76, 44);
    }
}
