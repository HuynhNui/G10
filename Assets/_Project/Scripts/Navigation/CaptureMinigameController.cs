using System;
using G10.Prototype.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace G10.Prototype.Navigation
{
    /// <summary>Horizontal interception game. Only returns a result; never changes the encounter.</summary>
    public sealed class CaptureMinigameController : MonoBehaviour
    {
        public CaptureMinigameProfile profile;
        public CaptureMinigameView view;
        public CabinStationView cabin;
        public CaptureMinigameState State { get; private set; }
        public int CurrentHits { get; private set; }
        public float RemainingTime { get; private set; }
        public Vector2 HookPosition { get; private set; }
        public Vector2 CreaturePosition { get; private set; }
        public bool IsActive => completion != null;
        // Axis-aligned colliders in playfield-local units; independent of Canvas scale and physics.
        public Rect HookCollider => new(HookPosition + profile.hookHitboxOffset - profile.hookHitbox / 2, profile.hookHitbox);
        public Rect CreatureCollider => new(CreaturePosition - profile.creatureHitbox / 2, profile.creatureHitbox);
        private Action<CaptureMinigameResult> completion;
        private UIManager panels;
        private Vector2 fieldSize;
        private float feedbackTime;
        private float cooldown;
        private float directionTime;
        private float creatureDirection;
        private float creatureHorizontalDirection;
        private bool finishing;

        public bool Begin(Action<CaptureMinigameResult> onComplete)
        {
            if (!isActiveAndEnabled || IsActive || onComplete == null || profile == null || view == null || view.playfield == null ||
                cabin == null || !cabin.isActiveAndEnabled ||
                cabin.Panels == null || profile.requiredHits < 1 || profile.attemptDuration <= 0 ||
                profile.horizontalSpeed <= 0 || profile.verticalSpeed <= 0) return false;
            fieldSize = view.playfield.rect.size;
            if (fieldSize.x < 500 || fieldSize.y < 150) return false;
            panels = cabin.Panels;
            if (!panels.TryOpenModal(view.gameObject)) return false;
            cabin.Brake();
            cabin.SetHover("");
            completion = onComplete;
            CurrentHits = 0;
            RemainingTime = profile.attemptDuration;
            HookPosition = new(profile.hookSize.x / 2, fieldSize.y / 2);
            cooldown = 0;
            State = CaptureMinigameState.Playing;
            RepositionCreature();
            view.Render(this);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            return true;
        }

        private void Update()
        {
            if (!IsActive) return;
            var keyboard = Keyboard.current;
            float input = keyboard == null ? 0 :
                (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1 : 0) -
                (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1 : 0);
            Tick(Mathf.Min(Time.unscaledDeltaTime, .1f), input);
        }

        /// <summary>Deterministic time/input entry point, also used by gameplay tests.</summary>
        public void Tick(float seconds, float verticalInput)
        {
            if (!IsActive || seconds <= 0) return;
            // Small steps prevent fast hooks tunnelling through the creature's collision box.
            float maxStep = Mathf.Min(1f / 120, 8f / Mathf.Max(1, profile.horizontalSpeed + profile.verticalSpeed + profile.creatureMoveSpeed));
            while (seconds > 0 && IsActive)
            {
                float dt = Mathf.Min(seconds, maxStep);
                seconds -= dt;
                if (State == CaptureMinigameState.Success || State == CaptureMinigameState.Failure)
                {
                    feedbackTime -= dt;
                    if (feedbackTime <= 0) Finish(State == CaptureMinigameState.Success ? CaptureMinigameResult.Success : CaptureMinigameResult.Failure);
                    continue;
                }
                // The final earned hit cannot time out during its feedback animation.
                if (CurrentHits < profile.requiredHits) RemainingTime = Mathf.Max(0, RemainingTime - dt);
                cooldown = Mathf.Max(0, cooldown - dt);
                if (State == CaptureMinigameState.HitFeedback)
                {
                    feedbackTime -= dt;
                    if (feedbackTime <= 0)
                    {
                        if (CurrentHits >= profile.requiredHits) EndAttempt(true);
                        else { RepositionCreature(); State = CaptureMinigameState.Playing; }
                    }
                }
                else if (State == CaptureMinigameState.Playing)
                {
                    var hook = HookPosition;
                    hook.x += profile.horizontalSpeed * dt;
                    hook.y = Mathf.Clamp(hook.y + Mathf.Clamp(verticalInput, -1, 1) * profile.verticalSpeed * dt,
                        profile.hookSize.y / 2, fieldSize.y - profile.hookSize.y / 2);
                    if (hook.x > fieldSize.x - profile.hookSize.x / 2) hook.x = profile.hookSize.x / 2;
                    HookPosition = hook;
                    MoveCreature(dt);
                    if (cooldown <= 0 && HookCollider.Overlaps(CreatureCollider))
                    {
                        CurrentHits++;
                        cooldown = Mathf.Max(profile.hitCooldown, profile.hitPause);
                        feedbackTime = Mathf.Clamp(profile.hitPause, .1f, .2f);
                        State = CaptureMinigameState.HitFeedback;
                    }
                }
                if (RemainingTime <= 0 && CurrentHits < profile.requiredHits) EndAttempt(false);
            }
            if (IsActive) view.Render(this);
        }

        private void MoveCreature(float seconds)
        {
            directionTime -= seconds;
            if (directionTime <= 0)
            {
                directionTime = UnityEngine.Random.Range(1.1f, 2.2f);
                creatureDirection = UnityEngine.Random.value < .5f ? -1 : 1;
                creatureHorizontalDirection = UnityEngine.Random.Range(-.2f, .2f);
            }
            Vector2 p = CreaturePosition + new Vector2(creatureHorizontalDirection, creatureDirection) * (profile.creatureMoveSpeed * seconds);
            float halfHeight = profile.creatureSize.y / 2;
            if (p.y < halfHeight || p.y > fieldSize.y - halfHeight) creatureDirection *= -1;
            p.y = Mathf.Clamp(p.y, halfHeight, fieldSize.y - halfHeight);
            p.x = Mathf.Clamp(p.x, fieldSize.x * .25f, fieldSize.x - profile.creatureSize.x / 2);
            CreaturePosition = p;
        }

        private void RepositionCreature()
        {
            float right = fieldSize.x - profile.creatureSize.x / 2 - 30;
            const float lead = 260;
            if (HookPosition.x + lead > right)
                HookPosition = new(profile.hookSize.x / 2, HookPosition.y);
            float left = Mathf.Max(fieldSize.x * .3f, HookPosition.x + lead);
            float y = UnityEngine.Random.Range(profile.creatureSize.y / 2 + 20, fieldSize.y - profile.creatureSize.y / 2 - 20);
            // Always offer a fresh vertical interception instead of placing it on the hook's line.
            if (Mathf.Abs(y - HookPosition.y) < fieldSize.y * .2f)
                y = HookPosition.y < fieldSize.y / 2 ? fieldSize.y * .8f : fieldSize.y * .2f;
            y = Mathf.Clamp(y, profile.creatureSize.y / 2, fieldSize.y - profile.creatureSize.y / 2);
            CreaturePosition = new(UnityEngine.Random.Range(left, Mathf.Min(right, left + 350)), y);
            directionTime = 0;
        }

        private void EndAttempt(bool success)
        {
            State = success ? CaptureMinigameState.Success : CaptureMinigameState.Failure;
            feedbackTime = Mathf.Max(.1f, profile.resultDuration);
        }

        public void Cancel()
        {
            if (!IsActive || finishing) return;
            Finish(CaptureMinigameResult.Cancelled);
        }

        private void Finish(CaptureMinigameResult result)
        {
            if (!IsActive || finishing) return;
            finishing = true;
            var callback = completion;
            completion = null;
            if (result == CaptureMinigameResult.Cancelled) State = CaptureMinigameState.Cancelled;
            if (cabin != null) cabin.Brake();
            if (panels != null) panels.EndModal(view != null ? view.gameObject : null, cabin != null && cabin.isActiveAndEnabled);
            finishing = false;
            callback(result);
        }

        private void OnDisable() => Cancel();
    }
}
