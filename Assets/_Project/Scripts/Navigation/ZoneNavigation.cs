using UnityEngine;

namespace G10.Prototype.Navigation
{
    /// <summary>Zone-local navigation. North is 0 degrees; clockwise headings increase.</summary>
    public sealed class ZoneNavigation : MonoBehaviour
    {
        [SerializeField] private Vector2 startPosition = new(600f, 100f);
        [SerializeField] private float startHeading;
        [SerializeField] private float maximumSpeed = 18f;
        [SerializeField] private float acceleration = 18f;
        [SerializeField] private float turnSpeed = 40f;
        [SerializeField, HideInInspector] private byte[] water;
        [SerializeField, HideInInspector] private int columns;
        [SerializeField, HideInInspector] private int rows;

        // Calibrated against the tick labels of the supplied 1672 x 941 Zone 1 chart.
        public static Vector2 UVToCoordinates(Vector2 uv) =>
            new((uv.x * 1672f - 48f) / 1.31f, (uv.y * 941f - 76f) / (762f / 700f));
        public static Vector2 CoordinatesToUV(Vector2 point) =>
            new((point.x * 1.31f + 48f) / 1672f, (point.y * (762f / 700f) + 76f) / 941f);

        public Vector2 Position { get; private set; }
        public float Heading { get; private set; }
        public float Speed { get; private set; }
        public bool Obstructed { get; private set; }
        public bool HasChart => water != null && water.Length == columns * rows && columns > 0;

        private void Awake() => ResetVoyage();

        public void ResetVoyage()
        {
            Position = startPosition;
            Heading = Mathf.Repeat(startHeading, 360f);
            Brake();
        }

        public void Brake()
        {
            Speed = 0f;
            Obstructed = false;
        }

        public void Step(float throttle, float turn, float seconds)
        {
            if (seconds <= 0f) return;
            Heading = Mathf.Repeat(Heading + Mathf.Clamp(turn, -1f, 1f) * turnSpeed * seconds, 360f);
            Speed = Mathf.MoveTowards(Speed, Mathf.Clamp(throttle, -1f, 1f) * maximumSpeed, acceleration * seconds);
            float radians = Heading * Mathf.Deg2Rad;
            Vector2 movement = new Vector2(Mathf.Sin(radians), Mathf.Cos(radians)) * (Speed * seconds);
            int steps = Mathf.Max(1, Mathf.CeilToInt(movement.magnitude / 1f));
            Vector2 increment = movement / steps;
            Obstructed = false;
            for (int i = 0; i < steps; i++)
            {
                Vector2 next = Position + increment;
                if (!CanOccupy(next))
                {
                    Obstructed = true;
                    Speed = 0f;
                    break;
                }
                Position = next;
            }
        }

        public bool IsWater(Vector2 point)
        {
            if (!HasChart || point.x < 0f || point.x > 1200f || point.y < 0f || point.y > 780f) return false;
            Vector2 uv = CoordinatesToUV(point);
            int x = Mathf.FloorToInt(uv.x * columns);
            int y = Mathf.FloorToInt(uv.y * rows);
            return x >= 0 && x < columns && y >= 0 && y < rows && water[y * columns + x] != 0;
        }

        public bool CanOccupy(Vector2 point) => IsWater(point)
            && IsWater(point + Vector2.left * 2f) && IsWater(point + Vector2.right * 2f)
            && IsWater(point + Vector2.up * 2f) && IsWater(point + Vector2.down * 2f);

        public bool HasNearbyObstacle(float distance)
        {
            for (int i = 0; i < 16; i++)
            {
                float angle = i * Mathf.PI / 8f;
                if (!IsWater(Position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance)) return true;
            }
            return false;
        }

        public void SetChart(byte[] cells, int width, int height)
        {
            water = cells;
            columns = width;
            rows = height;
        }
    }
}
