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
        [SerializeField, Min(0.01f)] private float ascentSpeed = 5f;
        [Header("Ship resources — base values before upgrades")]
        [SerializeField] private ShipResourceSettings resourceSettings = new();
        [Tooltip("Designer preview: use these scene stats instead of saved ship stats on load/checkpoint restore. Leave OFF for normal progression.")]
        [SerializeField] private bool useSceneShipSettingsOnLoad;
        public bool UseSceneShipSettingsOnLoad => useSceneShipSettingsOnLoad;
        [ContextMenu("Apply Scene Ship Settings (Play Mode)")]
        public void ApplySceneShipSettings()
        {
            if (!Application.isPlaying) return;
            Ship.Restore(CreateInitialShipState());
            Depth = Mathf.Clamp(Depth, 0, Ship.MaximumDepth); Brake();
        }
        private ShipResources ship;
        private bool terrainContact;
        private Vector2 impactPosition;
        public ShipResources Ship => ship ??= new ShipResources(CreateInitialShipState());
        public bool IsMoving { get; private set; }
        public ShipState CreateInitialShipState()
        {
            var settings = resourceSettings ?? new ShipResourceSettings();
            var state = new ShipState {
                speed = Mathf.Max(.01f, maximumSpeed), diveSpeed = Mathf.Max(.01f, depthSpeed),
                ascentSpeed = Mathf.Max(.01f, ascentSpeed), maximumDepth = Mathf.Max(.01f, maximumDepth),
                energyCapacity = Mathf.Max(1, settings.energyCapacity), energyPerSecond = Mathf.Max(.01f, settings.energyPerSecond),
                hullCapacity = Mathf.Max(1, settings.hullCapacity), collisionDamagePerSpeed = 1,
                radarCapacity = Mathf.Max(0, settings.radarCapacity), photoCapacity = Mathf.Max(0, settings.photoCapacity),
                captureCapacity = Mathf.Max(0, settings.captureCapacity) };
            state.Refill(); return state;
        }
        public float Depth { get; private set; }
        public Vector3 WorldPosition => new(Position.x, Position.y, -Depth);
        public const float DefaultGridSize = 50f;
        public const float ChartCellSize = DefaultGridSize;
        public static Vector2 CellCenter(Vector2 point, float gridSize = DefaultGridSize) => new(
            (Mathf.Floor(point.x / gridSize) + 0.5f) * gridSize,
            (Mathf.Floor(point.y / gridSize) + 0.5f) * gridSize);
        /// <summary>Positive input dives; negative input ascends. Releasing holds depth.</summary>
        public void StepDepth(float input, float seconds)
        {
            IsMoving = false;
            if (ExpeditionBlocked) return;
            seconds = Ship.AvailableMovementSeconds(seconds);
            Ship.ConsumeMovement(seconds * MoveDepth(input, seconds));
        }
        [SerializeField, HideInInspector] private byte[] water;
        [SerializeField, HideInInspector] private int columns;
        [SerializeField, HideInInspector] private int rows;
        [SerializeField, HideInInspector] private int mapCoordinateVersion;
        private byte[] radarWaterRegion;
        private Vector2 mapWorldSize = Vector2.one * DefaultGridSize;
        private float mapGridSize = DefaultGridSize;

        public Vector2 MapWorldSize => mapWorldSize;
        public float MapGridSize => mapGridSize;

        public static Vector2 UVToCoordinates(Vector2 uv, Vector2 worldSize) =>
            new(uv.x * worldSize.x, uv.y * worldSize.y);
        public static Vector2 CoordinatesToUV(Vector2 point, Vector2 worldSize) =>
            new(point.x / Mathf.Max(.01f, worldSize.x), point.y / Mathf.Max(.01f, worldSize.y));

        public Vector2 NormalizedToMapCoordinates(Vector2 normalized) =>
            new(normalized.x * mapWorldSize.x, normalized.y * mapWorldSize.y);
        public Vector2 MapCoordinatesToNormalized(Vector2 point) =>
            new(point.x / mapWorldSize.x, point.y / mapWorldSize.y);
        public Vector2 MapCellCenter(Vector2 point) => new(
            (Mathf.Floor(point.x / mapGridSize) + .5f) * mapGridSize,
            (Mathf.Floor(point.y / mapGridSize) + .5f) * mapGridSize);

        public void ConfigureMapCoordinates(Vector2 worldSize, float gridSize)
        {
            mapWorldSize = new Vector2(Mathf.Max(.01f, worldSize.x), Mathf.Max(.01f, worldSize.y));
            mapGridSize = Mathf.Max(.01f, gridSize);
            radarWaterRegion = null;
        }

        public bool MigrateCoordinates(Vector2 originalWorldSize, Vector2 locationSizedWorldSize,
            Vector2 displayWorldSize, Vector2 nextWorldSize)
        {
            if (mapCoordinateVersion >= ZoneMapConfig.CurrentCoordinateVersion) return false;
            Vector2 previousWorldSize = mapCoordinateVersion switch
            {
                1 => locationSizedWorldSize,
                2 => displayWorldSize,
                _ => originalWorldSize
            };
            Vector2 scale = new(
                nextWorldSize.x / Mathf.Max(.01f, previousWorldSize.x),
                nextWorldSize.y / Mathf.Max(.01f, previousWorldSize.y));
            startPosition = Vector2.Scale(startPosition, scale);
            mapCoordinateVersion = ZoneMapConfig.CurrentCoordinateVersion;
            return true;
        }

        public Vector2 Position { get; private set; }
        public float Heading { get; private set; }
        public float Speed { get; private set; }
        public bool Obstructed { get; private set; }
        public bool HasChart => water != null && water.Length == columns * rows && columns > 0 && rows > 0;
        private bool expeditionBlocked;
        public bool TransitionBlocked { get; set; }
        public bool ExpeditionBlocked { get => expeditionBlocked || TransitionBlocked; set => expeditionBlocked = value; }
        public float DistanceTravelled { get; private set; }
        public void RestoreVoyage(Vector2 position, float heading, float depth, float distance)
        {
            radarWaterRegion = null;
            Position = position; Heading = Mathf.Repeat(heading, 360); Depth = Mathf.Clamp(depth, 0, Ship.MaximumDepth);
            DistanceTravelled = Mathf.Max(0, distance); terrainContact = false; Brake();
        }

        private void Awake() => ResetVoyage();

        public void ResetVoyage()
        {
            radarWaterRegion = null;
            Position = startPosition;
            Depth = Mathf.Clamp(startDepth, 0, Ship.MaximumDepth);
            Heading = Mathf.Repeat(startHeading, 360f);
            DistanceTravelled = 0;
            terrainContact = false;
            Brake();
        }

        public void Brake()
        {
            Speed = 0f;
            Obstructed = false;
            IsMoving = false;
        }

        public void Step(float throttle, float turn, float seconds)
            => Navigate(throttle, turn, 0, seconds);

        /// <summary>One movement tick for all axes; diagonal travel spends one second of energy, not two.</summary>
        public void Navigate(float throttle, float turn, float vertical, float seconds)
        {
            IsMoving = false;
            if (seconds <= 0f || ExpeditionBlocked || !Ship.CanMove) { Brake(); return; }
            seconds = Ship.AvailableMovementSeconds(seconds);
            Heading = Mathf.Repeat(Heading + Mathf.Clamp(turn, -1f, 1f) * turnSpeed * seconds, 360f);
            Speed = Mathf.MoveTowards(Speed, Mathf.Clamp(throttle, -1f, 1f) * Ship.Speed, acceleration * seconds);
            float horizontalFraction = MoveHorizontal(seconds);
            float verticalFraction = Ship.Hull > 0 ? MoveDepth(vertical, seconds) : 0;
            Ship.ConsumeMovement(seconds * Mathf.Max(horizontalFraction, verticalFraction));
            if (!Ship.CanMove) Speed = 0;
        }

        private float MoveDepth(float input, float seconds)
        {
            float intended = Mathf.Clamp(input, -1, 1) * (input < 0 ? Ship.AscentSpeed : Ship.DiveSpeed) * seconds;
            if (Mathf.Abs(intended) <= .000001f) return 0;
            float before = Depth;
            Depth = Mathf.Clamp(Depth + intended, 0, Ship.MaximumDepth);
            float fraction = Mathf.Clamp01(Mathf.Abs((Depth - before) / intended));
            IsMoving |= fraction > 0; return fraction;
        }

        /// <summary>Integrate current velocity; called by Step while the helm is active.</summary>
        public void Coast(float seconds)
        {
            IsMoving = false;
            if (seconds <= 0f || ExpeditionBlocked || !Ship.CanMove) { Brake(); return; }
            seconds = Ship.AvailableMovementSeconds(seconds);
            Ship.ConsumeMovement(seconds * MoveHorizontal(seconds));
            if (!Ship.CanMove) Speed = 0;
        }

        private float MoveHorizontal(float seconds)
        {
            float radians = Heading * Mathf.Deg2Rad;
            Vector2 movement = new Vector2(Mathf.Sin(radians), Mathf.Cos(radians)) * (Speed * seconds);
            if (movement.sqrMagnitude <= .00000001f) return 0;
            int steps = Mathf.Max(1, Mathf.CeilToInt(movement.magnitude / 1f));
            Vector2 increment = movement / steps;
            Obstructed = false;
            int completed = 0;
            for (int i = 0; i < steps; i++)
            {
                Vector2 next = Position + increment;
                if (!CanOccupy(next))
                {
                    Obstructed = true;
                    // Latch contact until the ship actually moves clear. Holding into a wall is one impact.
                    if (!terrainContact) { Ship.HitTerrain(Speed); terrainContact = true; impactPosition = Position; }
                    Speed = 0f;
                    break;
                }
                Position = next;
                DistanceTravelled += increment.magnitude;
                completed++;
                if (terrainContact && Vector2.Distance(Position, impactPosition) >= 2f) terrainContact = false;
            }
            IsMoving |= completed > 0;
            return (float)completed / steps;
        }

        public bool IsWater(Vector2 point)
        {
            if (!HasChart || point.x < 0f || point.x >= mapWorldSize.x || point.y < 0f || point.y >= mapWorldSize.y) return false;
            Vector2 uv = MapCoordinatesToNormalized(point);
            int x = Mathf.FloorToInt(uv.x * columns);
            int y = Mathf.FloorToInt(uv.y * rows);
            return x >= 0 && x < columns && y >= 0 && y < rows && water[y * columns + x] != 0;
        }

        /// <summary>Filled display mask only. The authored contour remains authoritative for collisions.</summary>
        public bool IsRadarTerrain(Vector2 point)
        {
            if (!HasChart || point.x < 0 || point.x >= mapWorldSize.x || point.y < 0 || point.y >= mapWorldSize.y) return true;
            if (radarWaterRegion == null)
            {
                // An old save/designer spawn can be inside a contour. Keep showing the raw lines until clear,
                // rather than caching an all-solid scan from an invalid flood-fill seed.
                if (!CanOccupy(Position)) return !IsWater(point);
                radarWaterRegion = RadarTerrainMask.Build(water, columns, rows, Position, mapWorldSize);
            }
            Vector2 uv = MapCoordinatesToNormalized(point);
            int x = Mathf.FloorToInt(uv.x * columns), y = Mathf.FloorToInt(uv.y * rows);
            return radarWaterRegion[y * columns + x] != 1;
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
            radarWaterRegion = null;
            water = cells;
            columns = width;
            rows = height;
        }
    }
}

