using UnityEngine;

namespace G10.Prototype.Navigation
{
    /// <summary>Deterministic forward movement with gradual heading changes.</summary>
    public sealed class SteeringMotor2D
    {
        public Vector2 Position { get; set; }
        public float Speed { get; set; }
        public float TurnSpeed { get; set; }
        public float Heading { get; set; }
        public float DesiredHeading { get; set; }

        public Vector2 Forward => new(Mathf.Cos(Heading * Mathf.Deg2Rad), Mathf.Sin(Heading * Mathf.Deg2Rad));

        public void Reset(Vector2 position, float heading, float speed, float turnSpeed)
        {
            Position = position;
            Heading = heading;
            DesiredHeading = heading;
            Speed = speed;
            TurnSpeed = turnSpeed;
        }

        public void Step(float seconds)
        {
            Heading = Mathf.MoveTowardsAngle(Heading, DesiredHeading, TurnSpeed * seconds);
            Position += Forward * (Speed * seconds);
        }
    }
}
