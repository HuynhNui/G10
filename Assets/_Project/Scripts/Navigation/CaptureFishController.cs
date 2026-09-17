using UnityEngine;

namespace G10.Prototype.Navigation
{
    public enum CaptureFishState { Swim, Hit, Flee }

    /// <summary>Chooses readable curved swim paths and handles the post-hit escape.</summary>
    public sealed class CaptureFishController
    {
        private readonly SteeringMotor2D motor = new();
        private CaptureMinigameProfile profile;
        private Vector2 fieldSize;
        private float decisionTime;
        private float fleeTime;

        public Vector2 Position => motor.Position;
        public float Heading => motor.Heading;
        public CaptureFishState State { get; private set; }

        public void Begin(CaptureMinigameProfile settings, Vector2 playfieldSize, Vector2 position, float heading)
        {
            profile = settings;
            fieldSize = playfieldSize;
            motor.Reset(position, heading, profile.fishMoveSpeed, profile.fishTurnSpeed);
            State = CaptureFishState.Swim;
            ChooseSwimHeading();
        }

        public void Step(float seconds)
        {
            if (State == CaptureFishState.Hit) return;
            if (State == CaptureFishState.Flee)
            {
                fleeTime -= seconds;
                if (fleeTime <= 0)
                {
                    State = CaptureFishState.Swim;
                    motor.Speed = profile.fishMoveSpeed;
                    ChooseSwimHeading();
                }
            }
            else
            {
                decisionTime -= seconds;
                if (decisionTime <= 0) ChooseSwimHeading();
            }

            SteerAwayFromBoundary();
            motor.Step(seconds);

            // Boundary steering starts early; this clamp is only a numerical safety net.
            float halfWidth = profile.creatureSize.x / 2;
            float halfHeight = profile.creatureSize.y / 2;
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
        }

        private void SteerAwayFromBoundary()
        {
            float margin = Mathf.Max(profile.fishBoundaryMargin, profile.creatureSize.y / 2);
            Vector2 p = motor.Position;
            if (p.x < margin || p.x > fieldSize.x - margin || p.y < margin || p.y > fieldSize.y - margin)
            {
                Vector2 towardSafeRegion = fieldSize / 2 - p;
                motor.DesiredHeading = Mathf.Atan2(towardSafeRegion.y, towardSafeRegion.x) * Mathf.Rad2Deg;
            }
        }
    }
}
