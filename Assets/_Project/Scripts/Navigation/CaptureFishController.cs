using UnityEngine;

namespace G10.Prototype.Navigation
{
    public enum CaptureFishState { Swim, Hit, Flee, Dash, Zigzag, Feint, Recovery }

    /// <summary>Chooses readable curved swim paths and handles the post-hit escape.</summary>
    public sealed class CaptureFishController
    {
        private readonly SteeringMotor2D motor = new();
        private CaptureMinigameProfile profile;
        private Vector2 fieldSize;
        private float decisionTime;
        private float fleeTime;
        private float patternTime, patternStep, reactionCooldown, patternHeading, patternElapsed;
        private int zigzagSign;
        private bool hookWasNear;

        public Vector2 Position => motor.Position;
        public float Heading => motor.Heading;
        public CaptureFishState State { get; private set; }

        public void Begin(CaptureMinigameProfile settings, Vector2 playfieldSize, Vector2 position, float heading)
        {
            profile = settings;
            fieldSize = playfieldSize;
            motor.Reset(position, heading, profile.fishMoveSpeed, profile.fishTurnSpeed);
            State = CaptureFishState.Swim;
            patternTime = patternStep = reactionCooldown = patternElapsed = 0;
            hookWasNear = false;
            ChooseSwimHeading();
        }

        public void Step(float seconds, Vector2? hookPosition = null)
        {
            if (seconds <= 0) return;
            if (State == CaptureFishState.Hit) return;
            bool hookNear = hookPosition.HasValue &&
                (motor.Position - hookPosition.Value).sqrMagnitude < profile.fishHookReactionDistance * profile.fishHookReactionDistance;
            reactionCooldown = Mathf.Max(0, reactionCooldown - seconds);
            if (State == CaptureFishState.Flee)
            {
                fleeTime -= seconds;
                if (fleeTime <= 0)
                {
                    if (profile.fishEvasionChance > 0) BeginRecovery();
                    else { State = CaptureFishState.Swim; motor.Speed = profile.fishMoveSpeed; ChooseSwimHeading(); }
                }
            }
            else if (State == CaptureFishState.Dash || State == CaptureFishState.Zigzag || State == CaptureFishState.Feint || State == CaptureFishState.Recovery)
                StepPattern(seconds);
            else
            {
                decisionTime -= seconds;
                if (decisionTime <= 0) ChooseSwimHeading();
                if (State == CaptureFishState.Swim && hookNear && !hookWasNear && reactionCooldown <= 0 &&
                    profile.fishEvasionChance > 0 && Random.value < profile.fishEvasionChance)
                {
                    Vector2 away = motor.Position - hookPosition.Value;
                    StartPattern(Mathf.Atan2(away.y, away.x) * Mathf.Rad2Deg);
                }
            }
            hookWasNear = hookNear;

            SteerAwayFromBoundary();
            motor.Step(seconds);

            // Boundary steering starts early; this clamp is only a numerical safety net.
            float halfWidth = profile.CreatureVisualSize.x / 2;
            float halfHeight = profile.CreatureVisualSize.y / 2;
            var p = motor.Position;
            p.x = Mathf.Clamp(p.x, halfWidth, fieldSize.x - halfWidth);
            p.y = Mathf.Clamp(p.y, halfHeight, fieldSize.y - halfHeight);
            motor.Position = p;
        }

        public void OnHit(Vector2 hookPosition)
        {
            State = CaptureFishState.Hit;
            Vector2 away = motor.Position - hookPosition;
            if (away.sqrMagnitude < .001f) away = Vector2.up;
            motor.DesiredHeading = Mathf.Atan2(away.y, away.x) * Mathf.Rad2Deg;
            motor.Speed = profile.fishMoveSpeed * profile.fishFleeSpeedMultiplier;
            fleeTime = profile.fishFleeDuration;
            reactionCooldown = profile.fishReactionCooldown;
        }

        public void BeginFlee()
        {
            if (State == CaptureFishState.Hit) State = CaptureFishState.Flee;
        }

        private void ChooseSwimHeading()
        {
            decisionTime = Random.Range(profile.fishMinDecisionInterval, profile.fishMaxDecisionInterval);
            Vector2 target = new(
                Random.Range(fieldSize.x * .25f, fieldSize.x * .9f),
                Random.Range(fieldSize.y * .2f, fieldSize.y * .8f));
            Vector2 direction = target - motor.Position;
            motor.DesiredHeading = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            motor.Speed = profile.fishMoveSpeed * Random.Range(.9f, 1.1f);
            if (profile.fishEvasionChance > 0 && Random.value < profile.fishEvasionChance)
            {
                StartPattern(motor.Heading + (Random.value < .5f ? -1 : 1) * Random.Range(35f, 85f));
            }
        }

        private void StartPattern(float heading)
        {
            // Every pattern steers through the same capped motor, then offers an interception window.
            State = (CaptureFishState)((int)CaptureFishState.Dash + Random.Range(0, 3));
            patternTime = State == CaptureFishState.Dash ? profile.fishDashDuration : profile.fishPatternDuration;
            patternElapsed = 0; patternStep = profile.fishZigzagInterval;
            patternHeading = heading; zigzagSign = Random.value < .5f ? -1 : 1;
            reactionCooldown = Mathf.Max(profile.fishReactionCooldown, patternTime + profile.fishRecoveryDuration);
            motor.DesiredHeading = heading;
            motor.Speed = profile.fishMoveSpeed * (State == CaptureFishState.Dash ? profile.fishDashSpeedMultiplier : profile.fishEvasionSpeedMultiplier);
        }
        private void StepPattern(float seconds)
        {
            patternTime -= seconds; patternElapsed += seconds;
            if (patternTime <= 0)
            {
                if (State == CaptureFishState.Recovery) { State = CaptureFishState.Swim; ChooseSwimHeading(); }
                else BeginRecovery();
                return;
            }
            if (State == CaptureFishState.Zigzag)
            {
                patternStep -= seconds;
                if (patternStep <= 0)
                { patternStep += profile.fishZigzagInterval; zigzagSign = -zigzagSign; motor.DesiredHeading = patternHeading + zigzagSign * 55; }
            }
            else if (State == CaptureFishState.Feint && patternElapsed >= profile.fishPatternDuration * .45f)
                motor.DesiredHeading = patternHeading - zigzagSign * 105;
        }
        private void BeginRecovery()
        {
            State = CaptureFishState.Recovery; patternTime = profile.fishRecoveryDuration; patternElapsed = 0;
            motor.Speed = profile.fishMoveSpeed * .7f;
            motor.DesiredHeading = motor.Heading;
        }

        private void SteerAwayFromBoundary()
        {
            float margin = Mathf.Max(profile.fishBoundaryMargin, profile.CreatureVisualSize.y / 2);
            if (profile.fishEvasionChance > 0)
                margin = Mathf.Max(margin, motor.Speed / (profile.fishTurnSpeed * Mathf.Deg2Rad) + profile.CreatureVisualSize.y / 2);
            Vector2 p = motor.Position;
            if (p.x < margin || p.x > fieldSize.x - margin || p.y < margin || p.y > fieldSize.y - margin)
            {
                Vector2 towardSafeRegion = fieldSize / 2 - p;
                motor.DesiredHeading = Mathf.Atan2(towardSafeRegion.y, towardSafeRegion.x) * Mathf.Rad2Deg;
            }
        }
    }
}
