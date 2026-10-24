# Zone 02 player-input and passive evidence tools

These tools live entirely in the existing **Editor-only** assembly, which already references `G10.Prototype`, `Unity.InputSystem`, `Unity.ugui` and `Unity.TextMeshPro`. No production caller, scene serialization, mission configuration or save payload is changed. The tool itself does not start a debug preset, advance time, move the ship, set resources, complete an objective, force a cue, invoke UI actions, or call minigame gameplay methods.

## Minimal invocation API

Use the fully qualified class `G10.Prototype.Editor.ZoneTwoExperiencePlaytestTools` from the Unity MCP C# execution tool. The examples below abbreviate it as `Tools`; add `using Tools = G10.Prototype.Editor.ZoneTwoExperiencePlaytestTools;` if the execution context supports using directives, otherwise spell out the class.

1. In **Edit Mode**, before starting the game or clicking its Debug UI:

   ```csharp
   G10.Prototype.Editor.ZoneTwoExperiencePlaytestTools.SetupIsolation(
       @"D:\G10\Docs\QA\Zone02ExperienceLoop\BaselineRun");
   ```

   This creates directories and selects `BaselineRun/isolated-save/timeline.json` and `BaselineRun/isolated-photos/`. The game's legitimate Debug UI uses its normal `.debug` suffix under that isolated path. The tool never writes or creates a save. Old overrides are remembered in Unity `SessionState`; isolation is reapplied across domain reload before scene initialization.

2. Enter **Play Mode**, then interact with the real Debug Start UI to create the legitimate Zone 02 preset. Input tools can be used before recording. Start the evidence capture at the agreed baseline start point (or earlier if setup must also be recorded):

   ```csharp
   G10.Prototype.Editor.ZoneTwoExperiencePlaytestTools.StartRecording(
       @"D:\G10\Docs\QA\Zone02ExperienceLoop\BaselineRun\Evidence", 45);
   ```

   Use a fresh evidence folder. A preexisting `timeline.jsonl` is rejected to protect earlier evidence. Keep Game View visible; Unity's `WaitForEndOfFrame` screen capture can stall when Game View is not rendered.

3. Drive only real input devices while the game runs at its natural speed:

   ```csharp
   G10.Prototype.Editor.ZoneTwoExperiencePlaytestTools.HoldKeys("W", 3.0, "navigate-forward");
   G10.Prototype.Editor.ZoneTwoExperiencePlaytestTools.HoldKeys("D", .7, "turn-clockwise");
   G10.Prototype.Editor.ZoneTwoExperiencePlaytestTools.MouseHold(1250, 180, 1.5, "depth-control");
   G10.Prototype.Editor.ZoneTwoExperiencePlaytestTools.Click(960, 540, "visible-button");
   G10.Prototype.Editor.ZoneTwoExperiencePlaytestTools.ReleaseAll();
   ```

   Resolve coordinates from the actual current Game View/screenshot. Coordinates use `Screen.width` × `Screen.height`, **bottom-left origin** (convert a screenshot's top-left y to `Screen.height - y`). The numeric examples are placeholders, not known button locations. Durations must be greater than zero and at most 300 real seconds. Keyboard names are Unity InputSystem `Key` names, e.g. `W`, `S`, `LeftArrow`, `Escape`; comma-separated combinations are supported.

   Events are queued on the existing `Keyboard.current` and `Mouse.current` in `InputSystem.onBeforeUpdate`. The normal player action, UIInputModule and normal gameplay `Update` consume them. There is no forced `InputSystem.Update`, manual game ticking, virtual gameplay action or scaled-time loop. Mouse down/up occupy different rendered frames. Mouse holds allow existing pointer down/up depth buttons to behave exactly as designed.

   Automation owns the full keyboard state and mouse left-button state during each gesture; do not type or click concurrently. `ReleaseAll`, duration expiry, entering Editor pause, leaving Play and assembly reload all queue releases. The next normal input update applies releases. A stopped Unity runtime cannot physically process input until it resumes, so realtime deadlines are reapplied before the next game input update.

4. Optional capture steering agent, **only after opening the real capture minigame**:

   ```csharp
   G10.Prototype.Editor.ZoneTwoExperiencePlaytestTools.CaptureInputAgent(55);
   ```

   This reads only publicly exposed, currently displayed hook position, creature position and hook heading to choose `W`/`S` each normal input update. It uses no hidden fish AI state, collision boxes, future trajectory, success method, capture method or `Tick`. It stops when the actual minigame ends or its realtime bound expires. Log and disclose this mode as **automated visible-position tracking**: capture completion still traverses the real gameplay controls, but the result does not measure human motor skill, comprehension or balance.

5. Read passive telemetry and annotate real actions/analysis pauses:

   ```csharp
   G10.Prototype.Editor.ZoneTwoExperiencePlaytestTools.Status();
   G10.Prototype.Editor.ZoneTwoExperiencePlaytestTools.Mark("agent.processing.begin", "Reviewing the actual screenshot; no controls held");
   G10.Prototype.Editor.ZoneTwoExperiencePlaytestTools.Mark("player.observation", "Describe only what the recorded screen/audio supports");
   ```

   `Status` returns JSON. `Mark` adds an observer annotation, never changes gameplay. Agent processing time remains real gameplay time if the game is running; report it separately using these markers. For an intentional Editor pause, mark its reason before pausing and after resuming. The tool automatically records Editor pause/resume and application focus/pause.

6. End the measured run, stop evidence, then leave Play Mode:

   ```csharp
   G10.Prototype.Editor.ZoneTwoExperiencePlaytestTools.StopRecording();
   ```

   **Corrected isolation remains active until Play has fully exited.** `ExpeditionLoop.OnApplicationQuit` writes the game's save; restoring the user's paths while the game still runs can redirect that last write. StopRecording no longer restores overrides through an editor-playing-flag condition; only explicit `EnteredEditMode` automatically restores them. `TeardownIsolation()` explicitly restores a setup in Edit Mode (for example if cancelled before Play). The earlier conditional implementation leaked baseline quit-saves; see save-preservation-incident.md. The final shutdown's protected hashes were verified. Starting a second isolation without tearing down the first is rejected. Use a different run folder and the same legitimate Debug UI preset for final measurement.

## Evidence files and interpretation

- `frames/frame-000000.jpg`, etc.: genuine complete rendered Game View captures, full `Screen` resolution, JPEG quality 62, target 5 Hz. The image is captured after `WaitForEndOfFrame`. There are no fabricated in-between images.
- `frames.jsonl`: one actual frame timestamp per written image, UTC, Unity realtime/unscaled/game time, Stopwatch elapsed wall time, frame number, current resolution, pause/focus/timeScale, telemetry snapshot, actual audio sample clock, DSP snapshot and skipped target capture slots.
- `actual-game-audio.wav`: actual pass-through AudioListener buffers converted to PCM16. The audio callback never modifies its input buffer, allocates, logs, writes disk or acquires locks. It writes to a bounded single-producer/single-consumer ring (16 MiB), drained to WAV on the main thread. No generated or replacement silence is inserted. If no listener/callback is available there is no valid audio recording; this is reported explicitly.
- `timeline.jsonl`: actual passive snapshots at approximately 5 Hz plus input requests, actual key/pointer transitions, capture agent key choices, observer markers, pause/focus changes and start/stop. Snapshot fields include position/depth/heading/speed/distance, mission IDs and cue receipts, visible panel/app, charges/resources, radar state, visible capture state and active AudioSource clip/volume/pan. The last field means a source is playing, **not proof of perceptible audio**.
- `summary.txt`: real wall duration, frame count, skipped target frame slots, written audio frames/sample rate/channels, actual captured peak and non-silent sample count, dropped audio samples, format changes, and an explicit audio state (`NO_LISTENER`, `NO_AUDIO_CALLBACKS`, `ACTUAL_CAPTURE_IS_SILENT` or `ACTUAL_NON_SILENT_CAPTURE`).
- `recording-notes.txt`: the synchronization and interpretation limits recorded with each run.

Capture stops after at most 45 minutes. JPEGs and JSONL are streamed, and only one screenshot is held at a time. PCM storage and the audio ring are fixed-size; overflow drops a whole callback and reports the number of dropped samples. Audio format changes are reported and incompatible callback data is dropped. WAV size remains below its 32-bit format limit at the expected 48 kHz stereo setting.

## Synchronization and fidelity limits

Audio uses a sample clock; video uses Unity realtime and actual rendered frame timestamps. Metadata includes first audio callback Stopwatch clock and per-snapshot `AudioSettings.dspTime`, allowing approximate alignment. Audio device latency, DSP buffering and scheduling jitter prevent sample-accurate synchronization. Missing callbacks and dropped audio are **gaps**, not replaced samples. Inspect these counters before combining images/audio into an encoded video; a fixed `5 fps` slideshow alone would conceal capture gaps. Use the recorded frame timestamps to build a variable-timestamp video and retain the originals.

Screen capture and JPEG encoding add real overhead. Report observed capture jitter, dropped slots and audio gaps. They can affect gameplay performance. Editor pause can suspend normal runtime callbacks and screen rendering; UTC/pause markers distinguish this from scaled game time. Scene loading, pauses, UI comprehension, navigation, automation processing and capture play should be categorized from evidence rather than guessed from total duration.

Cue receipts prove state consumption, not successful sensory exposure. Review actual frames/audio around receipts to determine visibility, audibility, cancellation and context. No automated data here proves fear, subjective tension or human hesitation.

## Preparation validation scope

The tooling author read the project/editor assembly definitions and existing InputSystem API source and inspected save, photo, navigation, UI and capture APIs. No Unity execution, scene edit, import, test or measured gameplay was run while writing this tool. Compile/import and a short **unmeasured** input/audio/video capability check are required before baseline evidence is accepted. That capability check must not be described as the measured baseline, and its isolated Debug state must not be reused after consuming Zone 02 cues.
