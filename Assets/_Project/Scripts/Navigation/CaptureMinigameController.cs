using System;
using G10.Prototype.Audio;
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
        public Vector2 HookPosition => hook.Position;
        public Vector2 CreaturePosition => fish.Position;
        public float HookHeading => hook.Heading;
        public float CreatureHeading => fish.Heading;
        public CaptureFishState FishState => fish.State;
        public Vector2 RopeAnchorPosition => new(0, fieldSize.y / 2);
        public bool IsActive => completion != null;
        // Axis-aligned colliders in playfield-local units; independent of Canvas scale and physics.
        public Rect HookCollider
        {
            get
            {
                Vector2 offset = Quaternion.Euler(0, 0, HookHeading) * profile.hookHitboxOffset;
                return new Rect(HookPosition + offset - profile.hookHitbox / 2, profile.hookHitbox);
            }
        }
        public Rect CreatureCollider => new(CreaturePosition - profile.creatureHitbox / 2, profile.creatureHitbox);
        private Action<CaptureMinigameResult> completion;
        private UIManager panels;
        private Vector2 fieldSize;
        private float feedbackTime;
        private float cooldown;
        private bool overlapActive;
        private bool finishing;
        private readonly CaptureHookController hook = new();
        private readonly CaptureFishController fish = new();

        public bool Begin(Action<CaptureMinigameResult> onComplete)
        {
            if (!isActiveAndEnabled || IsActive || onComplete == null || profile == null || view == null || view.playfield == null ||
                cabin == null || !cabin.isActiveAndEnabled ||
                cabin.Panels == null || profile.requiredHits < 1 || profile.attemptDuration <= 0 ||
                profile.hookMoveSpeed <= 0 || profile.hookTurnSpeed <= 0 || profile.fishTurnSpeed <= 0 ||
                profile.fishMinDecisionInterval <= 0 || profile.fishMaxDecisionInterval < profile.fishMinDecisionInterval) return false;
            fieldSize = view.playfield.rect.size;
            if (fieldSize.x < 500 || fieldSize.y < 150) return false;
            panels = cabin.Panels;
            if (!panels.TryOpenModal(view.gameObject)) return false;
            cabin.Brake();
            cabin.SetHover("");
            completion = onComplete;
            CurrentHits = 0;
            RemainingTime = profile.attemptDuration;
            cooldown = 0;
            overlapActive = false;
            State = CaptureMinigameState.Playing;
            hook.Begin(profile, fieldSize);
            BeginCreature();
            view.Render(this);
            AudioManager.Instance?.PlayCaptureGrab();
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
            float maximumSpeed = Mathf.Max(profile.hookMoveSpeed, profile.fishMoveSpeed * profile.fishFleeSpeedMultiplier);
            float maxStep = Mathf.Min(1f / 120, 8f / Mathf.Max(1, maximumSpeed));
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
                        else { fish.BeginFlee(); State = CaptureMinigameState.Playing; }
                    }
                }
                else if (State == CaptureMinigameState.Playing)
                {
                    hook.Step(dt, verticalInput);
                    fish.Step(dt);
                    bool overlaps = HookCollider.Overlaps(CreatureCollider);
                    if (cooldown <= 0 && overlaps && !overlapActive)
                    {
                        CurrentHits++;
                        cooldown = Mathf.Max(profile.hitCooldown, profile.hitPause);
                        feedbackTime = Mathf.Clamp(profile.hitPause, .1f, .2f);
                        fish.OnHit(HookPosition);
                        State = CaptureMinigameState.HitFeedback;
                        AudioManager.Instance?.PlayCaptureGrab();
                    }
                    overlapActive = overlaps;
                }
                if (RemainingTime <= 0 && CurrentHits < profile.requiredHits) EndAttempt(false);
            }
            if (IsActive) view.Render(this);
        }

        private void BeginCreature()
        {
            float right = fieldSize.x - profile.creatureSize.x / 2 - 30;
            const float lead = 260;
            float left = Mathf.Max(fieldSize.x * .3f, HookPosition.x + lead);
            float y = UnityEngine.Random.Range(profile.creatureSize.y / 2 + 20, fieldSize.y - profile.creatureSize.y / 2 - 20);
            // Always offer a fresh vertical interception instead of placing it on the hook's line.
            if (Mathf.Abs(y - HookPosition.y) < fieldSize.y * .2f)
                y = HookPosition.y < fieldSize.y / 2 ? fieldSize.y * .8f : fieldSize.y * .2f;
            y = Mathf.Clamp(y, profile.creatureSize.y / 2, fieldSize.y - profile.creatureSize.y / 2);
            var position = new Vector2(UnityEngine.Random.Range(left, Mathf.Min(right, left + 350)), y);
            fish.Begin(profile, fieldSize, position, UnityEngine.Random.Range(-25, 25));
        }

        private void EndAttempt(bool success)
        {
            State = success ? CaptureMinigameState.Success : CaptureMinigameState.Failure;
            feedbackTime = Mathf.Max(.1f, profile.resultDuration);
            if (success) AudioManager.Instance?.PlayCaptureSuccess();
            else AudioManager.Instance?.PlayCaptureFail();
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
