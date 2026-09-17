using G10.Prototype.Navigation;
using UnityEngine;
using UnityEngine.UI;

namespace G10.Prototype.UI
{
    public sealed class CaptureMinigameView : MonoBehaviour, IPanelBackHandler
    {
        public CaptureMinigameController controller;
        public RectTransform playfield;
        public RawImage hook;
        public RawImage creature;
        public RawImage impact;
        public RawImage spark;
        public RectTransform progressClip;
        public float progressWidth = 800;
        public Text hitCounter;
        public Text timer;
        public Text stateLabel;
        public RawImage success;
        public RawImage failure;
        private Image rope;
        private Image launcher;

        public void Render(CaptureMinigameController game)
        {
            EnsureRopeVisuals();
            bool hit = game.State == CaptureMinigameState.HitFeedback;
            bool won = game.State == CaptureMinigameState.Success;
            bool lost = game.State == CaptureMinigameState.Failure;
            Vector2 origin = playfield.rect.size / 2;
            hook.rectTransform.sizeDelta = game.profile.hookSize;
            creature.rectTransform.sizeDelta = game.profile.creatureSize;
            hook.rectTransform.anchoredPosition = game.HookPosition - origin;
            creature.rectTransform.anchoredPosition = game.CreaturePosition - origin;
            hook.rectTransform.localRotation = Quaternion.Euler(0, 0, game.HookHeading);
            hook.rectTransform.localScale = Vector3.one;
            ApplyFishOrientation(game.CreatureHeading);
            RenderRope(game.RopeAnchorPosition - origin, hook.rectTransform.anchoredPosition, game.profile.ropeThickness);
            impact.rectTransform.anchoredPosition = hook.rectTransform.anchoredPosition;
            spark.rectTransform.anchoredPosition = creature.rectTransform.anchoredPosition;
            hook.enabled = !hit && !won && !lost;
            impact.enabled = hit;
            spark.enabled = hit;
            creature.enabled = !won && !lost;
            success.enabled = won;
            failure.enabled = lost;
            hitCounter.text = $"{game.CurrentHits} / {game.profile.requiredHits}";
            timer.text = $"{Mathf.CeilToInt(game.RemainingTime):00}s";
            progressClip.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, progressWidth * Mathf.Clamp01((float)game.CurrentHits / game.profile.requiredHits));
            stateLabel.text = won ? "CAPTURE COMPLETE" : lost ? "TARGET ESCAPED" : hit ? "CONTACT" : "INTERCEPT TARGET";
        }

        private void EnsureRopeVisuals()
        {
            if (rope != null && launcher != null) return;
            rope = CreateSolidImage("Cable", new Vector2(1, 3));
            rope.rectTransform.pivot = new Vector2(0, .5f);
            rope.transform.SetAsFirstSibling();
            launcher = CreateSolidImage("LauncherAnchor", new Vector2(12, 12));
            launcher.transform.SetSiblingIndex(1);
        }

        private Image CreateSolidImage(string objectName, Vector2 size)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(playfield, false);
            var image = go.GetComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = false;
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(.5f, .5f);
            image.rectTransform.sizeDelta = size;
            return image;
        }

        private void RenderRope(Vector2 anchor, Vector2 target, float thickness)
        {
            Vector2 delta = target - anchor;
            rope.rectTransform.anchoredPosition = anchor;
            rope.rectTransform.sizeDelta = new Vector2(delta.magnitude, Mathf.Max(1, thickness));
            rope.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            launcher.rectTransform.anchoredPosition = anchor;
        }

        private void ApplyFishOrientation(float heading)
        {
            bool movingLeft = Mathf.Cos(heading * Mathf.Deg2Rad) < 0;
            float tilt = movingLeft ? Mathf.DeltaAngle(180, heading) : Mathf.DeltaAngle(0, heading);
            creature.rectTransform.localScale = new Vector3(movingLeft ? -1 : 1, 1, 1);
            creature.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Clamp(tilt, -55, 55));
        }

        public void Exit() => controller.Cancel();
        public bool TryHandleBack() { Exit(); return true; }
        private void OnDisable() { if (controller != null) controller.Cancel(); }
    }
}
