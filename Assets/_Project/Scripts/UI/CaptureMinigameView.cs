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

        public void Render(CaptureMinigameController game)
        {
            bool hit = game.State == CaptureMinigameState.HitFeedback;
            bool won = game.State == CaptureMinigameState.Success;
            bool lost = game.State == CaptureMinigameState.Failure;
            Vector2 origin = playfield.rect.size / 2;
            hook.rectTransform.sizeDelta = game.profile.hookSize;
            creature.rectTransform.sizeDelta = game.profile.creatureSize;
            hook.rectTransform.anchoredPosition = game.HookPosition - origin;
            creature.rectTransform.anchoredPosition = game.CreaturePosition - origin;
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

        public void Exit() => controller.Cancel();
        public bool TryHandleBack() { Exit(); return true; }
        private void OnDisable() { if (controller != null) controller.Cancel(); }
    }
}
