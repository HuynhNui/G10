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
        [Header("Depth in metres below surface")]
        [SerializeField, Min(0)] private float startDepth = 230f;
        [SerializeField, Min(0)] private float maximumDepth = 500f;
        [SerializeField, Min(0)] private float depthSpeed = 5f;
        public float Depth { get; private set; }
        public Vector3 WorldPosition => new(Position.x, Position.y, -Depth);
        public const float ChartCellSize = 50f;
        public static Vector2 CellCenter(Vector2 point) => new(
            (Mathf.Floor(point.x / ChartCellSize) + 0.5f) * ChartCellSize,
            (Mathf.Floor(point.y / ChartCellSize) + 0.5f) * ChartCellSize);
        /// <summary>Positive input dives; negative input ascends. Releasing holds depth.</summary>
        public void StepDepth(float input, float seconds)
        { if (seconds > 0) Depth = Mathf.Clamp(Depth + Mathf.Clamp(input, -1, 1) * depthSpeed * seconds, 0, maximumDepth); }
        [SerializeField, HideInInspector] private byte[] water;
        [SerializeField, HideInInspector] private int columns;
        [SerializeField, HideInInspector] private int rows;

        // The gameplay chart has no printed margins: every pixel belongs to the map.
        public static Vector2 UVToCoordinates(Vector2 uv) =>
            new(uv.x * 1200f, uv.y * 700f);
        public static Vector2 CoordinatesToUV(Vector2 point) =>
            new(point.x / 1200f, point.y / 700f);

        public Vector2 Position { get; private set; }
        public float Heading { get; private set; }
        public float Speed { get; private set; }
        public bool Obstructed { get; private set; }
        public bool HasChart => water != null && water.Length == columns * rows && columns > 0;

        private void Awake() => ResetVoyage();

        public void ResetVoyage()
        {
            Position = startPosition;
            Depth = Mathf.Clamp(startDepth, 0, maximumDepth);
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
            Coast(seconds);
        }

        /// <summary>Integrate current velocity; called by Step while the helm is active.</summary>
        public void Coast(float seconds)
        {
            if (seconds <= 0f) return;
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
            if (!HasChart || point.x < 0f || point.x >= 1200f || point.y < 0f || point.y >= 700f) return false;
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
