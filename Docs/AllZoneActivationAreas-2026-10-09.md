# G10 — All-zone activation areas (2026-10-09)

This extends the preceding local playtest fixes. No commit or push was requested or performed. All earlier working-tree changes were preserved, including Zone02's exit/light fixes and unrelated graphics/font/project settings.

## Final authored configuration

| Area | Center in map coordinates | Radius | Destination |
| --- | --- | --- | --- |
| Zone01 exit | `(1875,680)` — unchanged | `75` (was 35) | Zone02 |
| Zone02 exit | `(1830,75)` — unchanged | `75` — unchanged | Zone03 |
| Zone03 exit | `(1750,25)` — unchanged | `75` (was 20) | Zone04 |
| Zone04 final hidden signal | `(1500,150)` — unchanged | `75` (was 30) | Hidden Ending, not another Zone |

Zone04's unused `exitArea` and empty `destinationZone` were not changed. Mission POIs, objectives, prerequisites, rewards, map artwork, movement, energy balance and persistent save schema were not changed.

## Runtime changes

- The existing exit ellipse/diamond uses each map's `exitArea`. The same normalized circle renderer now also draws the gold final-signal boundary from `finalHiddenPoint`.
- A shared `ZoneMapConfig.HiddenDestinationAvailable` predicate retains the existing hidden-route choice and discovery/completion requirements. Map presentation and gameplay use that same predicate; missing discovery data cannot be replaced by a stale saved completion bit. The final destination stays invisible until the route's discoveries are complete.
- Final-signal hints reuse the existing task card and Helm status. No new transfer button or UI redesign was introduced.
- Both exit and final arrival use the existing configured `MapPoi.Contains()` circle. Continue/unlock inside does not automatically transfer or force exit/reentry: fresh horizontal travel is required. Rotation/depth changes do not count.
- A runtime-only `ZoneNavigation.VoyageRevision` increments on restore/reset. Arrival observation resets when that revision changes, so even an outside-armed route cannot mistake restored position or saved distance for movement. It does not alter ship motion, resources or saved data.
- Accepted zone transitions retain the existing latch/fade/Save/Load flow. Hidden Ending is saved before loading its scene; the existing ending/transition guards prevent repeated requests. Normal Ending choice behavior is unchanged.

## Real terrain validation

Unity's existing baked masks and `ZoneNavigation.CanOccupy` were used, including the actual 2-unit vessel footprint. A 2-unit connectivity lattice with 1-unit intermediate checks connected each authored entry to its destination. Clear 24-unit inward approaches cross each 75-unit circle from outside.

| Zone | Entry-to-center nodes visited | Clear 2-unit circle samples | Example outside approach |
| --- | ---: | ---: | --- |
| Zone01 | 386,465 | 3,688 | `(1913.18,751.44)` |
| Zone02 | 188,502 | 4,211 | `(1911,75)` |
| Zone03, post-rock mask | 170,993 | 3,084 | `(1831,25)` |
| Zone04 | 101,144 | 4,140 | `(1581,150)` |

Zone03's entire expanded circle has **zero** clear samples on the intact-rock mask; after destruction its center/approach are clear. The regression test also checks that fieldwork/craft/install without the actual destroy objective cannot transfer, then executes the real rock-destruction action before entering Zone04.

Terrain limitation: these circles are activation geometry, not a promise that every point inside is navigable. Existing contours remain collidable. Zone01's circle extends 30 units past the right map edge; Zone03's extends 50 units below the bottom edge. The existing chart `RectMask2D` clips the outline to map bounds, while ship movement already prevents travel outside the map. Both areas have reachable water approaches; positions were therefore preserved.

## Files changed in this follow-up

All paths below are under `Assets/_Project/`:

- `Content/Maps/Zone01.asset`, `Zone03.asset`, `Zone04.asset`: only the relevant activation radius changed relative to the preceding local fixes. Zone02 was not written.
- `Scripts/Navigation/ZoneMapConfig.cs`: shared hidden-destination availability rule.
- `Scripts/Navigation/ZoneNavigation.cs`: runtime-only restore epoch.
- `Scripts/UI/ExpeditionProgression.cs`: shared exit/final observation, restore reset, final hints, live discovery guard.
- `Scripts/UI/PhotoSurveyMap.cs`: shared circle rendering and final-signal visibility/hints.
- `Scripts/UI/CabinStationView.cs`: nearby final-signal hint in existing Helm status.
- `Scripts/Editor/ZoneExitSetupEditor.cs`: all-zone terrain/approach inspection and targeted Undo-capable radius migration. Existing Zone02 migration remains available and unchanged in behavior.
- `Tests/PlayMode/ZoneExitMapPlayModeTests.cs`: geometry/center/radius, locked hidden presentation, Inspector radius changes and invalid discovery IDs.
- `Tests/PlayMode/ZoneExitFlowPlayModeTests.cs`: real gates/rock action, automatic transfers, inside/near Continue, checkpoint/position restores, duplicate guards, terrain and chart masks.
- `Tests/PlayMode/ExpeditionEndToEndPlayModeTests.cs`: automatic-arrival assertions now require physical movement after fixture positioning rather than treating a restore as arrival. The real camera/SEND/capture/upgrade/radar and both ending assertions remain intact.
- `Tests/PlayMode/CabinNavigationPlayModeTests.cs`, `ComputerShellPlayModeTests.cs`: isolated cloned Input System settings for synthetic keyboard tests, matching the existing capture/world-map fixtures. Original settings are restored in `finally`; production focus/movement behavior and project input settings were not changed.

No scene/prefab, art, mission-content or project-settings edits were needed for this follow-up. The broader Git diff still includes the earlier task's local changes.

## Verification

- Before these changes: existing exit suites **9/9 passed** (`3dff9b0dbd39`).
- Extended exit/map/final suites: **24/24 PlayMode passed**, zero skipped (`c2806eb85ac3`, 56.88 seconds). A new coroutine initially lacked a yield; the compile error was fixed and Unity compiled before these 24 tests executed.
- Entire EditMode suite: **54/54 passed**, zero skipped (`f8ec3b40844e`, 1.89 seconds).
- Entire PlayMode run: **225/227 passed**, two keyboard-fixture failures, zero skipped (`f81db9075b61`, 522.5 seconds). All exit/new tests, tutorial, real device end-to-end, save, map and ending fixtures passed. The two failures were synthetic W/Escape input being discarded while the Editor was unfocused (`Application.isFocused=false`, default `ResetAndDisableNonBackgroundDevices` / `PointersAndKeyboardsRespectGameViewFocus`).
- After isolating transient input settings, both previously failed tests **2/2 passed**, zero skipped (`78ac73212651`, 8.49 seconds). Production code was unchanged between the full run and this rerun. This is full-suite coverage plus targeted retries, not a claim that one final 227-test batch was green.
- Final compiler and Unity Console error checks: zero entries. Scoped `git diff --check` passed. Original Input System settings were restored, and the Editor returned to the original clean Bootstrap scene in EditMode without saving any scene.

Unity-rendered visual evidence is under `D:/G10/Temp/TestCaptures`: `zone01-exit-expanded.png`, `zone02-exit-preserved.png`, `zone03-exit-expanded.png` and `zone04-final-expanded.png`. All four were visually inspected, including the clipped edge outlines and gold final boundary, across 1920×1080 and 1366×768 captures.

Configuration migration was applied in the Editor after terrain inspection. No extra Inspector wiring is required. The explicit `G10/Maps/Apply Verified All-Zone Activation Radii` menu revalidates terrain and sets Zone01/03/final04 to 75; it intentionally leaves Zone02 alone. Live Inspector radius edits affect both circles and Contains without rerunning the migration.

Validation used Unity feature-implementation, Unity MCP and uGUI skills: extend existing owners, preserve serialized data, make targeted Editor-safe asset changes, reuse the existing passive map view and verify production flows. These are instrumented Unity PlayMode flows and renders, not a newly built Windows executable or a claim of human manual playtesting. No standalone build or hardware/performance benchmark was run.
