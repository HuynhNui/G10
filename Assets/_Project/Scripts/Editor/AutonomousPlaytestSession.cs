using System;
using System.IO;
using G10.Prototype.Computer;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace G10.Prototype.Editor
{
    // Opt-in, Editor-only transport. No mission, navigation or puzzle APIs.
    [InitializeOnLoad]
    public static class AutonomousPlaytestSession
    {
        private const string SessionKey = "G10.AutonomousPlaytest.Root";
        private static double until;
        private static Vector2 position;
        private static bool pressed, pendingRelease;
        private static Key[] keys = Array.Empty<Key>();
        public static string Root => SessionState.GetString(SessionKey, "");

        static AutonomousPlaytestSession()
        {
            InputSystem.onBeforeUpdate += Pump;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingPlayMode) Release();
                if (state == PlayModeStateChange.EnteredEditMode)
                {
                    ExpeditionSaveStore.PathOverride = null;
                    PhotoCaptureService.ArchivePathOverride = null;
                }
            };
        }

        public static string Arm(string root)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Arm before Play.");
            string evidence = Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/QA/AutonomousPlaytest/Evidence"));
            root = Path.GetFullPath(root);
            if (!root.StartsWith(evidence + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Session must be inside the QA evidence folder.");
            if (Directory.Exists(root)) throw new IOException("Use a new session directory; existing sessions are never overwritten.");
            Directory.CreateDirectory(root);
            SessionState.SetString(SessionKey, root);
            return root;
        }

        [InitializeOnEnterPlayMode]
        private static void Isolate(EnterPlayModeOptions options)
        {
            if (string.IsNullOrEmpty(Root)) return;
            ExpeditionSaveStore.PathOverride = Path.Combine(Root, "timeline.json");
            PhotoCaptureService.ArchivePathOverride = Path.Combine(Root, "photos");
            Log("isolation", ExpeditionSaveStore.PathOverride);
        }

        public static void Disarm()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first.");
            SessionState.EraseString(SessionKey);
        }

        // x/y are normalized screenshot coordinates, top-left origin.
        public static void Input(float x, float y, bool left, string keyNames, float seconds)
        {
            if (!EditorApplication.isPlaying || string.IsNullOrEmpty(Root) ||
                ExpeditionSaveStore.PathOverride != Path.Combine(Root, "timeline.json"))
                throw new InvalidOperationException("Isolated Play session required.");
            if (EditorApplication.timeSinceStartup < until) throw new InvalidOperationException("Previous action is still running.");
            if (seconds < .04f || seconds > 15 || x < 0 || x > 1 || y < 0 || y > 1)
                throw new ArgumentOutOfRangeException("Bounded input: normalized coordinates, .04–15 seconds.");
            keys = string.IsNullOrWhiteSpace(keyNames) ? Array.Empty<Key>() :
                Array.ConvertAll(keyNames.Split(','), value => (Key)Enum.Parse(typeof(Key), value.Trim(), true));
            position = new Vector2(x * Screen.width, (1-y) * Screen.height);
            pressed = left;
            until = EditorApplication.timeSinceStartup + seconds;
            pendingRelease = true;
            Log("input", $"x={x:R};y={y:R};left={left};keys={keyNames};seconds={seconds:R};screen={Screen.width}x{Screen.height}");
        }

        private static void Pump()
        {
            if (!EditorApplication.isPlaying || !pendingRelease) return;
            if (EditorApplication.timeSinceStartup >= until) { Release(); return; }
            if (Mouse.current != null)
                InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = position }.WithButton(MouseButton.Left, pressed));
            if (Keyboard.current != null) InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(keys));
        }

        public static void Release()
        {
            if (pendingRelease)
            {
                if (Mouse.current != null) InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = position });
                if (Keyboard.current != null) InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
                Log("release", "all");
            }
            until = 0; pressed = false; pendingRelease = false; keys = Array.Empty<Key>();
        }

        [Serializable] private sealed class Entry { public string utc, kind, detail; }
        public static void Log(string kind, string detail)
        {
            if (string.IsNullOrEmpty(Root)) return;
            File.AppendAllText(Path.Combine(Root, "actions.jsonl"), JsonUtility.ToJson(new Entry
                { utc = DateTime.UtcNow.ToString("O"), kind = kind, detail = detail }) + Environment.NewLine);
        }
    }
}
