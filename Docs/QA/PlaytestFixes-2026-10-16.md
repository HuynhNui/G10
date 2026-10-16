# Playtest bugs 01–04 and tutorial UX 05–06

Date: 2026-10-16 (Asia/Saigon). Project: `D:/G10`, Unity 6000.4.2f1, AnkleBreaker MCP port 7890.

## Implementation

| Task | Cause / missing behavior | Result |
| --- | --- | --- |
| BUG 01: submarine sticks | A restored position or replaced chart can leave the vessel centre in water while a 2 m clearance sample overlaps terrain. Every small outward tick was rejected until the entire footprint was clear, making escape impossible. Ordinary valid-water impacts already allowed reversing and turning away. | Permit only movement that reduces an existing clearance overlap. Both centres must remain water; previously clear samples cannot enter terrain, existing overlaps cannot deepen, and at least one overlap must improve. Normal collision, speed/acceleration/reverse limits, Energy and depth rules stay authoritative. Fully solid-centre positions are deliberately not teleported through terrain. |
| BUG 02: automatic photography completion | The current checkout already creates pending photo evidence and records photography only through the durable `ExpeditionLoop.CommitPhotoSubmission` SEND transaction. Inspection found no production `RecordPhoto`/photo-taken completion binding. | Preserve the manual SEND implementation and add real-camera regression coverage for every authored photograph across Zones 1–4, including combined photo/capture and hidden-route objectives, pending/submitted Continue, duplicates, legacy completion and previous-day restore. No speculative mission rewrite. |
| BUG 03: return tooltip stays visible | `returnHovered || returnNearby` opened the return information without pointer input. Zone02's entry is 100 units from the gate, inside the 125-unit nearby threshold. Pointer exit therefore reopened the tooltip. | Return information now requires marker hover. Other exit/rock/final route hints and the existing gate placement changes are preserved. |
| BUG 04: tutorial Energy | The original report requested a Helm-completion refill; the user canceled that behavior on 2026-10-17. | Automatic tutorial Energy restoration is removed. Movement Energy remains consumed through Helm/tutorial completion, reopening, rebind and Continue. Previous-day rollback restores its normal checkpoint Energy without a tutorial bonus. Root-level tutorial knowledge remains outside rollback snapshots. |
| UX 05: computer tutorial | Existing station progression explained reaching Photo Lab and SEND, but its hint did not follow desktop navigation, selected-photo controls, mission information or exit. Mission Log and Ship Status were also excluded by the tutorial app permission switch. | Extend the existing director/view with contextual access, desktop, photo selection/SEND, optional Mission Log/Ship Status/Inventory and exit instructions. Allow Mission Log/Ship Status whenever the computer is accessible. Use existing application controls; add no required tutorial stages or app tour. |
| UX 06: spotlight | The old view parented a colored border to its target; it did not dim the surrounding screen. | One reusable pointer-transparent `TutorialSpotlight` draws four non-overlapping dark quads around an unpainted rectangular opening (black, alpha 0.38, 6 screen-pixel padding). It follows target corners, screen/camera projection, scaling, resizing and rotation, and hides for unavailable/offscreen/destroyed targets. |

The guidance card renders above the dimmer. The dimmer has no raycast target, so highlighted and other available controls retain their existing input rules. A covered desktop shortcut is replaced by the visible taskbar app switch target. Closing/exit controls remain available. Hide/disable, modal/direct interaction, pause/transition, completion and scene destruction clear the spotlight; resuming a cue restores its renderer material as well as geometry.

Computer knowledge uses namespaced IDs (`Computer:Navigate`, `Computer:MissionLog`, `Computer:Cargo`, `Computer:ShipStatus`, `Computer:Exit`) in the existing completed-ID list. Main step enumeration and save schema are unchanged. Lessons complete from real app/close/exit observations, and transient save failures are queued for retry. Existing completed/migrated tutorials remain completed. The Photo Lab tutorial still learns only from an actual durable SEND; capture does not submit evidence.

## Files changed for this request

Runtime:

- `Assets/_Project/Scripts/Navigation/ZoneNavigation.cs` — bounded shallow-overlap escape.
- `Assets/_Project/Scripts/UI/PhotoSurveyMap.cs` — hover-only return tooltip; this file already had unrelated local gate changes.
- `Assets/_Project/Scripts/Computer/ExpeditionLoop.cs` — initial Helm Energy restoration removed per the 2026-10-17 user request; tutorial progress saves preserve the current Energy budget.
- `Assets/_Project/Scripts/Tutorial/TutorialManager.cs` — persisted optional computer knowledge and informational app access.
- `Assets/_Project/Scripts/Tutorial/ZoneOneTutorialDirector.cs` — observe real computer interactions and retry failed lesson persistence.
- `Assets/_Project/Scripts/Tutorial/TutorialGuidanceView.cs` — contextual computer copy/targets and shared spotlight integration.
- `Assets/_Project/Scripts/Tutorial/TutorialSpotlight.cs` and `.meta` — reusable dimmer/opening.

Validation:

- `TerrainCollisionPlayModeTests.cs` — 24 cases including reverse/turn-away at multiple depths, restored overlap at 30/60/144 Hz, corner/solid-centre/edge guards.
- `ZoneExitMapPlayModeTests.cs` — default/hover/exit/reopen/historical map regression.
- `ReturnGatePlacementPlayModeTests.cs` — extended existing local fixture for actual Zones 2–4 hover, passive graphics, reopening and Continue.
- `PhotoSubmissionPlayModeTests.cs` — all 11 authored photos, two combined photo/capture sites, two hidden photo sites, legacy completion and rollback.
- `TutorialPlayModeTests.cs` — Energy consumption preserved through completion, Continue/rebind and previous-day rollback, failed-save behavior, real computer navigation/photo browsing/SEND/exit/persistence and renderer/raycast checks. The earlier refill assertions are superseded by the 2026-10-17 change.
- `TutorialSpotlightPlayModeTests.cs` and `.meta` — geometry, screen/camera/scaling, actual pointer passthrough and lifecycle.
- `CabinNavigationPlayModeTests.cs` — position the offscreen capture camera before rebuilding camera-dependent UI.

No new scene/prefab wiring, packages, project settings, artwork or shader assets are required. Substantial pre-existing local changes were retained, including map/scene/feedback work and dynamic font caches. No commit, push or destructive Git operation was performed.

## Validation log

These runs record the implementation at the time they executed. Their Energy-refill assertions describe the earlier behavior, superseded by the 2026-10-17 user request. Validation of the replacement behavior is tracked in the dated follow-up below.

- Pre-fix map run `a0320f342ae8`: 2 tests, 2 failed (expected reproduction).
- Pre-fix movement run `521a339d26d2`: 20 tests, 17 passed, 3 failed; all three shallow-overlap cases remained at x=598.5 instead of retreating below 598.
- Intermediate run `abb033e64fdd`: 38/41 passed; three scene tests exposed a missing CanvasRenderer on the new graphic. Fixed with an explicit component requirement/creation.
- Intermediate tutorial run `8fb0d4e6ff96`: 32/38 passed; fixed the authored exit-name lookup and a test reflection ambiguity between Graphic mesh overloads.
- Focused follow-up `c1f49c17b4c3`: 6/6 passed (computer flow plus five spotlight cases).
- One attempted broader run did not execute tests because a renderer test used an unavailable GetMesh overload; the assertion was corrected, the stale runner cleared, and the verification run restarted. No result from that attempt is counted as passed.
- Focused run `2e4771361195`: **96 passed, 0 failed, 0 skipped**, 235.36 seconds. Includes the complete targeted movement, map, return placement, photo submission, tutorial/save and spotlight fixtures.
- Full EditMode run `2aafaa5d082e`: **71 passed, 0 failed, 0 skipped**, 0.67 seconds.
- Related run `e860eb791aad`: **7 passed, 0 failed, 0 skipped**, 74.15 seconds. Covers ComputerApps, ComputerShell, three journal photo/capture rollback scenarios, the repeated gameplay/historical-map rollback scenario and the added Helm previous-day refill regression.
- Final tutorial/spotlight rerun `ac20d9e7d2af`: **25 passed, 0 failed, 0 skipped**, 141.12 seconds, including the refined unusable-newest-photo arrow, manual selection/SEND and rollback tests. This repeats relevant coverage on the final source; it is not counted as 25 additional distinct tests.

Original 2026-10-16 coverage: **71 distinct EditMode + 103 distinct PlayMode tests passed (174 total)**. That source compiled with zero errors. Its status was **Ready with limitations** (Editor validation; no standalone player build). These historical results do not validate the subsequent removal of the tutorial Energy refill.

Visual captures are inspected separately from logical mesh assertions. The existing offscreen helper initially rebuilt while its temporary camera was on the target plane, hiding the spotlight; the helper now positions the camera before its final layout/render tick.

## Evidence and limits

Actual Game View and offscreen images were inspected: [1920×1080](PlaytestFixes-2026-10-16/spotlight-send-1920.png), [1366×768](PlaytestFixes-2026-10-16/spotlight-send-1366.png). SEND retains its original brightness, the surrounding desktop/app is dimmed, and the instruction card is readable. Geometry/projection tests additionally exercise changed scaler aspect ratios, moved/resized/rotated targets and screen-camera canvases. Pointer tests exercise actual GraphicRaycaster results and click handlers.

Machine-readable MCP job summaries: [focused](PlaytestFixes-2026-10-16/focused-results.json), [EditMode](PlaytestFixes-2026-10-16/editmode-results.json), [related](PlaytestFixes-2026-10-16/related-results.json), [final tutorial rerun](PlaytestFixes-2026-10-16/tutorial-final-results.json). The installed bridge preserves aggregate counts across domain reload but drops per-test detail arrays; fixture filters and intermediate failures are recorded above rather than inventing XML results.

The changed runtime/test/report files pass scoped `git diff --check`. A whole-worktree check also encounters pre-existing trailing spaces in the already modified Zone01 scene; those unrelated scene lines were preserved. No package or ProjectSettings diff was introduced.

No Inspector setup is required. A standalone player build and extended human playthrough were not run. Shallow clearance recovery deliberately cannot rescue a vessel centre actually inside solid terrain. The spotlight uses rectangular bounds around rotated controls, preserving a small amount of adjacent area. No TRUE GATE code was copied.

## Follow-up — contextual photo/capture spotlight (2026-10-17)

Camera and Capture previously selected their cabin hotspot as soon as the tutorial reached that step. The spotlight now requires both an actual radar detection of that specific POI and current actionable interaction conditions. Before arrival, guidance remains visible without darkening the screen. On arrival while Radar/Helm is open, it says to press ESC to return to the cabin; it never spotlights a device hidden behind another panel.

`RadarDisplay.ContactDetected` fires only when the real sweep reaches a contact. The director persists `RadarContact:<poiId>` in the existing tutorial knowledge list, retaining it after radar echoes expire/close and after Continue. An empty scan or a different POI cannot unlock the cue. Events received during pause are queued until safe to present; failed saves retry without consuming another radar charge. Tutorial step order and normal mission/device permissions are unchanged.

`PhotoCaptureService.CanPhotographPoi` checks the existing camera's contact/framing rules, current contact position, distance, depth, forward field of view, terrain visibility, photo charge, hull and cooldown without taking a photograph. `CreatureCatcher.CanStartCaptureAt` reuses the real capture validator for radius/depth/terrain, target presence, cargo, charges and required prior SEND without starting a minigame. The actual photo compositor still determines image quality; the hint does not promise an automatically successful image or submit it.

Additional changed files: `Scripts/UI/RadarDisplay.cs`, `Scripts/Computer/PhotoCaptureService.cs`, `Scripts/Navigation/CreatureCatcher.cs` under `Assets/_Project/`; extended existing TutorialManager, ZoneOneTutorialDirector, TutorialGuidanceView and TutorialPlayModeTests. No serialized assets, save schema version, balance, new mandatory tutorial stages or automatic photo submission were introduced.

- Focused run `81c97510e4de`: **4 passed, 0 failed**, 37.84 seconds. Actual sweeps, unavailable/available states, direction/depth/LOS/charges, specific-POI discovery, Continue, paused detection and failed-save retry.
- Related run `211f1856b8f6`: **10 passed, 0 failed**, 51.02 seconds. Four existing tutorial/computer scenarios, all-zone authored depth integration and five spotlight geometry/input/lifecycle tests.
- Final run `f38093f7c3ba`: **11 passed, 0 failed, 0 skipped**, 59.3 seconds. Reruns the four focused cases, including actual detection from beyond camera range followed by entry into the photo region, and all seven RadarFilledTerrainPlayModeTests. Follow-up coverage totals **21 distinct passing PlayMode tests**; repeated executions are not counted twice. Final compilation reports zero errors; scoped diff check passes.

Job summaries: [focused](PlaytestFixes-2026-10-16/contextual-survey-results-2026-10-17.json), [related](PlaytestFixes-2026-10-16/survey-regression-results-2026-10-17.json), [final](PlaytestFixes-2026-10-16/survey-final-results-2026-10-17.json). Inspected visual evidence: [Camera ready](PlaytestFixes-2026-10-16/camera-ready-1920.png), [Capture ready](PlaytestFixes-2026-10-16/capture-ready-1920.png). Both spotlight only the intended cabin device while its interaction is available. Tests also prove the dimmer is absent before detection, before arrival, behind other panels, during pause and during capture interaction.

## Follow-up — remove tutorial Energy restoration (2026-10-17)

Per the user's request, automatic Energy restoration at Helm/tutorial completion is removed. Normal movement continues to consume Energy, and completing the tutorial preserves that budget through save/load. The existing tutorial milestones still persist; reopening or rebinding tutorial interfaces grants no Energy. Previous-day rollback restores the checkpoint's ship resources while retaining learned tutorial knowledge.

The `ExpeditionLoop.SaveTutorialProgress` refill behavior and the corresponding refill-focused assertions listed in the historical runs above are superseded. `TutorialPlayModeTests` now checks preservation of consumed Energy at completion, after a failed save/retry, across Continue/rebind and after previous-day rollback.

Validation: run `1533c9254ee9` passed **4/4 PlayMode tests**, 0 failed/skipped, 33.1 seconds: `HelmCompletionPreservesConsumedEnergyAcrossSaveAndContinue`, `FailedHelmCompletionSaveRetryPreservesConsumedEnergy`, `HelmCompletionAndPreviousDayRollbackKeepTheirNormalEnergyBudgets`, and the complete real tutorial/photo/SEND/capture/upgrade flow. Unity compilation reports zero errors. [Job summary](PlaytestFixes-2026-10-16/tutorial-energy-preservation-results-2026-10-17.json).
