# Local autonomous playtest transport

Use the existing AnkleBreaker MCP tools; never call its HTTP bridge directly. No dependency installation or paid service is required. The visual reasoning loop runs in the current Codex session. This is not a stand-alone visual AI executable.

1. Read `Docs/QA/AutonomousPlaytest/ToolSelection.md` and `TestPlan.md`.
2. Verify saved editor scenes, compilation and unique target instance. Record git status, commit, source fingerprint and current build before a run.
3. In Edit Mode call `unity_execute_code` with:

```csharp
return G10.Prototype.Editor.AutonomousPlaytestSession.Arm("D:/G10/Docs/QA/AutonomousPlaytest/Evidence/NEW-UNIQUE-RUN");
```

4. Enter Play via `unity_play_mode`, then independently verify `ExpeditionSaveStore.PathOverride` and `PhotoCaptureService.ArchivePathOverride` are inside that session. Never choose New Game until this succeeds. The opt-in Editor callback sets paths before gameplay loads. Nothing copies/deletes real saves.
5. Observe with `unity_screenshot_game` into that session. Inspect the PNG with the image viewer. For each action call:

```csharp
G10.Prototype.Editor.AutonomousPlaytestSession.Input(.5f,.5f,true,"",.12f);
return "mouse down queued; release is automatic";
```

Coordinates are fractions of the current Game View screenshot, top-left origin. Keyboard examples: `left=false,keyNames="W"` or `"W,A"`. The bounds are .04–15 seconds. This queues actual Input System state events, never gameplay callbacks. Do not call InputSystem.Update manually. Screen dimensions are logged. Reobserve after every action, wait for release before another, and never blindly retry an uncertain action.
6. Save the action history and game captures. Label the start of Mode B before inspecting mission/save/spawn state. A DEBUG checkpoint invalidates any subsequent claim of a full New Game playthrough.
7. Stop Play, verify overrides clear, and call `AutonomousPlaytestSession.Disarm()`. The existing scene setup returns without saving scenes. Keep evidence and isolated saves for reproduction.

The safety controller releases held devices after the deadline and on exiting Play. It refuses an existing session folder, actions outside isolated Play, out-of-bounds coordinates and overlapping holds. Maximum three observed retries and a 120-second no-progress limit are orchestration policy in TestPlan. Screenshots are captured on demand instead of unbounded continuous recording. Logs and save data are diagnosis only, never a source of black-box puzzle solutions.

Mode B helpers are saved as C# method bodies in this folder. Read them with ordinary file tools and pass their exact text to `unity_execute_code`. They compile dynamically through the existing bridge and do not need assembly reloads:

- `ClickUi.cs.txt`: replace `__BUTTON__` with a uniquely observed active Button GameObject name. Requires an unobstructed raycast at its visual center, then sends a pointer press. Does not invoke onClick. Suitable for resolution-independent UI interaction in Mode B.
- `NavigateHelm.cs.txt`: replace `__X__`, `__Y__`, `__DEPTH__`, `__HEADING__` with invariant C# float literals (or `float.NaN` for no final heading). Open the helm first. Uses depth/turn/forward pointer controls, a 7-unit arrival radius, 4-degree heading tolerance, 120-second timeout, 8-second movement watchdog and energy<=3 stop. It has no terrain path planner: inspect stalls and supply waypoints. Aim several meters inside the valid depth band, since arrival tolerance is 1.5m. Auto transition closes the panel and cancels the driver safely.
- `SteerCapture.cs.txt`: arm before a normal capture click. Reads the rendered geometry's runtime positions and sends W/S keyboard state each real input update. Stops after completion/cancellation or 65 seconds. This is **state-aware** control, not a visual-only AI or forced hit. It never calls Tick/Begin/ResolveCapture, changes profiles, grants rewards or sets positions.

Cancel a local driver by setting its SessionState boolean to false through MCP (`G10.AutonomousPlaytest.HelmDriver` or `G10.AutonomousPlaytest.CaptureDriver`); its Editor watchdog unsubscribes and releases devices. Always check the returned result before dependent actions. A button behind an app window is intentionally rejected: close that app, observe the desktop, then use Exit before clicking a cabin hotspot.

Windows focus tests use the installed `@oai/sky` skill and a uniquely returned Unity window. Always activate and observe it before input; an occluded snapshot in this environment can show the foreground application. Do not target unrelated desktop windows.
