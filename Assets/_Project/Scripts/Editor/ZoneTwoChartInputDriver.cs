using System;
using System.Diagnostics;
using G10.Prototype.Computer;
using G10.Prototype.Navigation;
using G10.Prototype.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

namespace G10.Prototype.Editor
{
    /// <summary>
    /// Opt-in, bounded chart waypoint assistance for editor playtests. Reads the same position,
    /// compass, speed and obstruction/resource indications shown at the helm. Only queues WASD
    /// on the existing keyboard; never writes gameplay state, searches terrain or advances time.
    /// </summary>
    [InitializeOnLoad]
    public static class ZoneTwoChartInputDriver
    {
        static readonly Key[] Neutral = Array.Empty<Key>();
        static readonly Key[] Forward = { Key.W };
        static readonly Key[] Left = { Key.A };
        static readonly Key[] Right = { Key.D };
        static readonly Key[] ForwardLeft = { Key.W, Key.A };
        static readonly Key[] ForwardRight = { Key.W, Key.D };
        const float AssumedBrakingAcceleration = 18f; // Existing normal helm release deceleration.
        static CabinStationView cabin;
        static ZoneNavigation navigation;
        static ExpeditionLoop loop;
        static Vector2 waypoint;
        static float arrivalRadius;
        static string startingZone, state = "idle", lastKeys = "none";
        static double deadline;
        public static bool IsActive { get; private set; }
        static double Now => Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;

        static ZoneTwoChartInputDriver()
        {
            InputSystem.onBeforeUpdate += BeforeInput;
            EditorApplication.update += EditorTick;
            EditorApplication.playModeStateChanged += PlayStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload += BeforeReload;
        }

        /// <summary>
        /// The operator must choose this waypoint from the displayed chart and open the helm.
        /// Call Cancel before using any other automated keyboard/mouse control.
        /// </summary>
        public static string DriveTo(float x, float y, float tolerance = 4f, double maximumSeconds = 60)
        {
            if (!Application.isPlaying || EditorApplication.isPaused || Time.timeScale <= 0)
                throw new InvalidOperationException("Use a running, unpaused Play Mode session.");
            if (!float.IsFinite(x) || !float.IsFinite(y) || !float.IsFinite(tolerance) || tolerance < 1 || tolerance > 100)
                throw new ArgumentOutOfRangeException(nameof(tolerance), "Use finite chart coordinates and a radius between 1 and 100.");
            if (double.IsNaN(maximumSeconds) || double.IsInfinity(maximumSeconds) || maximumSeconds <= 0 || maximumSeconds > 300)
                throw new ArgumentOutOfRangeException(nameof(maximumSeconds), "Use a real duration in (0, 300] seconds.");
            if (Keyboard.current == null) throw new InvalidOperationException("No existing keyboard device.");
            if (IsActive) throw new InvalidOperationException("Cancel the current waypoint before starting another.");
            cabin = Object.FindAnyObjectByType<CabinStationView>();
            navigation = cabin != null ? cabin.Navigation : null;
            loop = Object.FindAnyObjectByType<ExpeditionLoop>();
            if (!HelmVisible()) throw new InvalidOperationException("The actual helm panel must already be open without a modal.");
            if (loop == null || !loop.IsInitialized) throw new InvalidOperationException("No initialized expedition.");
            if (!navigation.Ship.CanMove) throw new InvalidOperationException("Helm indicates no movement resources.");
            // Establish one keyboard owner. Other playtest tooling requests cancel this driver.
            ZoneTwoExperiencePlaytestTools.ReleaseAll();
            waypoint = new Vector2(x, y); arrivalRadius = tolerance; startingZone = loop.Zone;
            deadline = Now + maximumSeconds; state = "driving"; lastKeys = "none"; IsActive = true;
            ZoneTwoExperiencePlaytestTools.Mark("input.navigation.start",
                $"operator-chart-waypoint={waypoint};radius={tolerance};maximumSeconds={maximumSeconds};normal-WASD-only");
            return Status();
        }

        public static string Cancel()
        {
            Stop("operator-cancel");
            return Status();
        }

        public static string Status()
        {
            return JsonUtility.ToJson(new DriverStatus
            {
                active = IsActive, state = state, keys = lastKeys,
                waypointX = waypoint.x, waypointY = waypoint.y, radius = arrivalRadius,
                x = navigation != null ? navigation.Position.x : 0,
                y = navigation != null ? navigation.Position.y : 0,
                heading = navigation != null ? navigation.Heading : 0,
                speed = navigation != null ? navigation.Speed : 0,
                distance = navigation != null ? Vector2.Distance(navigation.Position, waypoint) : 0,
                remainingWallSeconds = IsActive ? Math.Max(0, deadline - Now) : 0
            });
        }

        static bool HelmVisible() => cabin != null && cabin.isActiveAndEnabled && navigation != null &&
            cabin.NavigationPanel != null && cabin.NavigationPanel.activeInHierarchy && cabin.Panels != null &&
            cabin.Panels.CurrentPanel == cabin.NavigationPanel && !cabin.Panels.IsModalOpen;

        static string StopReason()
        {
            if (!Application.isPlaying) return "play-mode-ended";
            if (EditorApplication.isPaused || Time.timeScale <= 0 || PauseMenuController.Instance?.IsPaused == true) return "paused";
            if (Now >= deadline) return "wall-time-limit";
            if (!HelmVisible()) return "helm-left-or-modal-opened";
            if (loop == null || loop.Zone != startingZone) return "zone-changed";
            if (!navigation.Ship.CanMove) return "no-movement-resources";
            if (navigation.Obstructed) return "helm-obstruction";
            if (ZoneTwoExperiencePlaytestTools.InputLabel != "none") return "other-input-tool-took-ownership";
            return null;
        }

        static void BeforeInput()
        {
            if (!IsActive || InputState.currentUpdateType == InputUpdateType.Editor) return;
            string reason = StopReason();
            if (reason != null) { Stop(reason); return; }
            if (Keyboard.current == null) { Stop("keyboard-unavailable"); return; }
            Vector2 offset = waypoint - navigation.Position;
            float distance = offset.magnitude;
            if (distance <= arrivalRadius) { Stop("waypoint-radius-reached"); return; }
            // The real compass is north=0, east=90; D adds positive clockwise heading.
            float desiredBearing = Mathf.Atan2(offset.x, offset.y) * Mathf.Rad2Deg;
            float error = Mathf.DeltaAngle(navigation.Heading, desiredBearing);
            float speed = Mathf.Abs(navigation.Speed);
            float stopDistance = speed * speed / (2f * AssumedBrakingAcceleration);
            // A short instrument/control margin plus release-to-brake keeps approach conservative.
            float controlMargin = Mathf.Min(navigation.Ship.Speed, speed) * .12f;
            bool forward = Mathf.Abs(error) <= 35f && distance > stopDistance + controlMargin + arrivalRadius * .6f;
            bool left = error < -1.5f, right = error > 1.5f;
            Key[] next = forward ? left ? ForwardLeft : right ? ForwardRight : Forward : left ? Left : right ? Right : Neutral;
            lastKeys = forward ? left ? "W,A" : right ? "W,D" : "W" : left ? "A" : right ? "D" : "none";
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(next));
        }

        static void EditorTick()
        {
            if (!IsActive) return;
            string reason = StopReason();
            if (reason != null) Stop(reason);
        }

        static void Stop(string reason)
        {
            if (!IsActive) return;
            IsActive = false; state = reason; lastKeys = "none";
            if (Keyboard.current != null) InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
            ZoneTwoExperiencePlaytestTools.Mark("input.navigation.stop", reason + ";" + Status());
        }

        static void PlayStateChanged(PlayModeStateChange value)
        {
            if (value == PlayModeStateChange.ExitingPlayMode || value == PlayModeStateChange.EnteredEditMode) Stop("play-mode-ended");
        }

        static void BeforeReload()
        {
            Stop("assembly-reload");
            InputSystem.onBeforeUpdate -= BeforeInput;
            EditorApplication.update -= EditorTick;
            EditorApplication.playModeStateChanged -= PlayStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload -= BeforeReload;
        }

        [Serializable]
        sealed class DriverStatus
        {
            public bool active;
            public string state, keys;
            public float waypointX, waypointY, radius, x, y, heading, speed, distance;
            public double remainingWallSeconds;
        }
    }
}
