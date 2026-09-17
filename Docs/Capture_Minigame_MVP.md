# Creature capture minigame

The Zone01 Catch button validates the existing encounter before opening a monochrome interception game. W/S or Up/Down steers the hook's heading while it continuously moves forward. Turning is gradual, producing curved paths; releasing the keys preserves the current heading. A visual cable runs from the fixed launcher to the hook. The hook resets at the right boundary and continues until five contacts or the 45-second timer expires. Escape or the Exit button cancels. Missed passes do not immediately fail the attempt.

## Setup and tuning

The authored Zone01 scene includes `CaptureMinigamePanel`, a cabin-owned `CaptureMinigameController`, its view and the catcher reference. No scene rebuild is required. For another compatible copy of Zone01, run **G10 → Zone 1 → Install Capture Minigame** and save. Re-running the installer reuses the panel and preserves a configured profile.

Tune `Assets/_Project/Data/Computer/Zone01CaptureMinigame.asset` in the Inspector:

| Setting | Default |
| --- | --- |
| hookMoveSpeed | 300 UI units/s |
| hookTurnSpeed | 220 degrees/s |
| minHookHeading / maxHookHeading | -65 / +65 degrees |
| fishMoveSpeed | 100 UI units/s |
| fishTurnSpeed | 100 degrees/s |
| fishMinDecisionInterval / fishMaxDecisionInterval | 1.0 / 1.8 seconds |
| fishFleeSpeedMultiplier | 1.6 |
| fishFleeDuration | 0.6 seconds |
| fishBoundaryMargin | 80 UI units |
| ropeThickness | 3 UI units |
| requiredHits | 5 |
| attemptDuration | 45 seconds |
| hitCooldown | 0.35 seconds |
| hitPause | 0.15 seconds |
| resultDuration | 0.8 seconds |

The profile also contains visual sizes and collision boxes. Collision uses overlapping axis-aligned rectangles in playfield-local units, including an offset to place the hook hitbox on its tip. Small simulation steps prevent tunnelling. Canvas scaling and the navigation grid do not affect the minigame. No world physics objects or second encounter system are created.

## Ownership and lifecycle

`CreatureCatcher.TryCapture()` → validate → `CaptureMinigameController.Begin()` → result callback → `CreatureCatcher.ResolveCapture()`.

- Invalid range, depth, terrain, missing creature, photograph prerequisite or full inventory returns the existing reason without opening the modal.
- A valid start returns `CreatureCatcher.Result.Started`; consumers receive the final result through `CaptureResolved` / `LastResult`.
- The controller owns Idle, Playing, HitFeedback, Success, Failure and Cancelled; the view only renders state.
- `SteeringMotor2D` performs only gradual rotation and forward movement. `CaptureHookController` translates player steering into a limited heading, while `CaptureFishController` owns timed swim decisions, boundary avoidance and flee state.
- Each contact increments once, freezes movement for 0.15 seconds and shows the supplied impact/spark. The fish then turns away from the hook and temporarily swims faster; it is never teleported after a hit. Continued overlap cannot register another hit.
- When the hook reaches the right edge it returns to the launcher with a forward heading. Hit progress, timer and fish state continue unchanged, and the cable returns to its short length automatically.
- Only a Success callback commits the existing inventory, task and encounter mutations. Conditions, capacity and encounter identity are rechecked at that point. Duplicate starts/callbacks cannot grant another item.
- Failure, Escape and disabling the modal/controller cancel or fail without removing the creature or completing Capture. Retrying starts a fresh hit count and timer.
- UIManager temporarily owns an exclusive modal and restores the previous panel. Cabin panel commands, radar scan and world-map/computer navigation are blocked while it is active. Closing brakes the helm, preserving its existing neutral-input requirement.

## Art

Uses the supplied PNGs from `Assets/_Project/Art/Sprites/CaptureMinigame_Assets`, with `Fish.png` replacing `Capture_Target_Creature.png` as the creature. They already have real alpha. PNG bytes and sprite slicing are preserved; the installer crops transparent margins with RawImage UVs and selects point filtering / uncompressed textures. Re-running the installer also refreshes the existing creature's artwork. The playfield background and text are black and white.

## Implementation files

New: `CaptureMinigameController.cs`, `CaptureMinigameProfile.cs`, `CaptureMinigameResult.cs`, `SteeringMotor2D.cs`, `CaptureHookController.cs` and `CaptureFishController.cs` under Scripts/Navigation; `CaptureMinigameView.cs` under Scripts/UI; `CaptureMinigameEditor.cs` under Scripts/Editor; `CaptureMinigamePlayModeTests.cs`; profile asset and scene wiring.

Updated: `CreatureCatcher`, `CreatureCaptureView`, `UIManager`, `CabinStationView`, `WorldMapController`, `ComputerScreenController`; the existing POI capture test now plays the minigame before expecting inventory. Scene-flow tests use a real-time timeout instead of a frame budget that expires too quickly in uncapped batch mode.

## Verification

Play Mode tests cover invalid conditions; W/S and arrow-key reading; modal isolation and Escape restoration; hit freeze/cooldown and progress; wrapping; timeout/retry; success-only inventory/task/encounter changes; radar/photo removal; cancellation on disable; capacity revalidation. See local results and rendered UI screenshots in `Logs/CaptureMinigame`.

### Verified result — 2026-09-17

Unity 6000.4.2f1 compiled the final files and ran all 18 project Play Mode tests in a clean isolated project: 15 passed, 3 failed. All 5 new minigame tests passed, including synthetic keyboard input through the real controller and Escape through UIManager. Both scene-transition tests passed with the real-time timeout. The 3 remaining failures match the recorded pre-change baseline: `Zone01CabinOpensPanelsAndKeepsNavigationState` (unfocused keyboard fixture), `AppsReadProvidersWithoutInventingGameplayData` (expects an empty/offline archive), and `ShellBlocksCabinKeepsVoyageAndRoutesEscape` (expects pre-existing ship speed to remain instead of braking). Their runtime behavior was not altered to satisfy those outdated assertions.

Final result: `Logs/CaptureMinigame/clean-tests.xml`. Final log: `clean-tests.log`. Screenshots: `capture-playing.png`, `capture-hit.png`, `capture-success.png`, `capture-failure.png`. These are local ignored verification artifacts. Rendered playfield/progress/results were visually inspected. Tests ran against an isolated product name so the player's photo archive was not used by the final verification. No manual playthrough in the user's open Editor was claimed; the live Unity bridge was unavailable.
