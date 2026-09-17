using UnityEngine;

namespace G10.Prototype.Navigation
{
    /// <summary>Owns player steering and repeated left-to-right hook passes.</summary>
    public sealed class CaptureHookController
    {
        private readonly SteeringMotor2D motor = new();
        private CaptureMinigameProfile profile;
        private Vector2 fieldSize;

        public Vector2 Position => motor.Position;
        public float Heading => motor.Heading;

        public void Begin(CaptureMinigameProfile settings, Vector2 playfieldSize)
        {
            profile = settings;
            fieldSize = playfieldSize;
            ResetPass(playfieldSize.y / 2);
        }

        public void Step(float seconds, float steeringInput)
        {
            float input = Mathf.Clamp(steeringInput, -1, 1);
            if (Mathf.Abs(input) > .001f)
                motor.DesiredHeading = Mathf.Clamp(motor.Heading + input * 90, profile.minHookHeading, profile.maxHookHeading);
            else
                motor.DesiredHeading = motor.Heading;

            float margin = Mathf.Max(profile.hookSize.y, 40);
            if (motor.Position.y < margin)
                motor.DesiredHeading = Mathf.Max(motor.DesiredHeading, Mathf.Atan2(fieldSize.y / 2 - motor.Position.y, fieldSize.x * .25f) * Mathf.Rad2Deg);
            else if (motor.Position.y > fieldSize.y - margin)
                motor.DesiredHeading = Mathf.Min(motor.DesiredHeading, Mathf.Atan2(fieldSize.y / 2 - motor.Position.y, fieldSize.x * .25f) * Mathf.Rad2Deg);

            motor.DesiredHeading = Mathf.Clamp(motor.DesiredHeading, profile.minHookHeading, profile.maxHookHeading);
            motor.Step(seconds);
            var p = motor.Position;
            p.y = Mathf.Clamp(p.y, profile.hookSize.y / 2, fieldSize.y - profile.hookSize.y / 2);
            motor.Position = p;
            if (p.x > fieldSize.x - profile.hookSize.x / 2) ResetPass(p.y);
        }

        private void ResetPass(float y)
        {
            motor.Reset(new Vector2(profile.hookSize.x / 2, y), 0, profile.hookMoveSpeed, profile.hookTurnSpeed);
        }
    }
}
