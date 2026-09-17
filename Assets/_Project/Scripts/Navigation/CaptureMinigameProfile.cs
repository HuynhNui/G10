using UnityEngine;

namespace G10.Prototype.Navigation
{
    [CreateAssetMenu(menuName = "G10/Capture/Minigame profile")]
    public sealed class CaptureMinigameProfile : ScriptableObject
    {
        [Min(1)] public float horizontalSpeed = 260;
        [Min(1)] public float verticalSpeed = 420;
        [Min(0)] public float creatureMoveSpeed = 110;
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
