# Playtest Round 3 — implementation and verification log

Baseline: local HEAD `43e020ee7b91034711a4a7e86c7c1c1d43e01115`, Unity 6000.4.2f1 / AnkleBreaker MCP 2.39.7, Windows Editor. Bootstrap clean, EditMode, zero compilation errors. The only pre-existing dirty file was `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset`; preserve it. No commit/push/build authorized.

## Phase A — progression integrity

Hypotheses tested:

| Hypothesis | Evidence / result |
|---|---|
| Day-5 completion contaminates a coherent Day-4 snapshot | Original player-facing Journal → camera/SEND/capture → Continue tests passed before changes for neither, PHOTO-only and both-completed checkpoints. Cross-zone rollback also passed. Reported ghost completion is **not yet reproduced** on this HEAD. |
| Restoring progress can create rewards/items | **Confirmed**: `RestoreProgress()` called `EvaluateRules()`. A completed snapshot with missing receipts acquired three reward receipts during restoration. New regression failed before the fix. |
| Historical maps lose completion source | **Confirmed**: switching zones nulls inactive runtime references; old map checkmarks failed despite completed persisted objectives. |

Corrections: snapshot restoration no longer evaluates reward rules; gameplay completion still does. Journal restoration suppresses gameplay during apply/commit, clears transient radar/camera/capture/encounter state, restores exact persisted objective/reward/item/photo state and reverts runtime if publication fails. Historical maps use a detached read-only projection of the current timeline, never another mission runtime. Active maps retain live runtime authority.

Validation:

- Baseline relevant suites: 19/19 passed, job `145f102c6abb`.
- Baseline added regressions: 3/4 passed, historical map failed, job `87e870d36378`; 1/2 passed, reward-on-restore failed, job `ef3c7b1933fa`.
- Phase A rollback/history/Loop/PhotoDeletion/ZoneOneStory/WorldMap suites: **40/40 passed**, job `3a35ebe173f6`, 130.59 seconds.

Changed files: `ZoneMissionRuntime.cs`, `ExpeditionLoop.cs`, `SavedZoneMapProgress.cs`, `ZoneMapPresentation.cs`, `PhotoSurveyMap.cs`, `CabinZoneSession.cs`, `ExpeditionEndToEndPlayModeTests.cs`.

## Phase B — backward navigation

Root cause / before: travel/save APIs accepted only an unlocked forward destination; no authored return region or backward arrival existed. Reusing an exit/entry position would place the vessel inside a gate and risk automatic re-travel.

After: `ZoneTravelKind` distinguishes first forward visit, forward revisit and backward revisit. Journal restore keeps its separate API. Backward travel requires a visited adjacent zone, captures current ship/resources/cargo/day, keeps destination objective/reward/reveal state, merges only ship-wide unlocks, and clamps depth to destination limits. Arrival observation resets on voyage restoration and requires actual XY travel, not turning/depth movement. Backward arrivals are outside the previous zone's forward gate.

| Source return gate | XY | Radius | Destination backward arrival | Heading |
|---|---|---:|---|---:|
| Zone02 → Zone01 | 205, 635 | 75 | 1735, 680 | 90° |
| Zone03 → Zone02 | 125, 215 | 75 | 1690, 60 | 83.8845° |
| Zone04 → Zone03 | 455, 950 | 75 | 1610, 25 | 90° |

Placement validated against actual contour terrain, the vessel's ±2-unit footprint, 25 units of forward clearance, mission-marker separation and 5-unit BFS paths. No artwork/mission coordinates changed. Zone03 backward arrival uses its already-destroyed rock terrain.

Validation: round-trip test covers all three pairs, immediate return after entry, Save/Continue, Rest, depth/turn/load non-triggers, resource/mission preservation and destroyed Zone03 revisit. Phase B suite job `0795197e9ab2`: 24/25 passed; the failure was an old test relying on reward creation during `RestoreProgress`. Updated that fixture to record objectives through the real authority, then its focused rerun `cb99fc98b7a6` passed 1/1. Terrain/navigation authoring test `67b16d37b116` passed 1/1.

Compatibility review found and reproduced an additional backward-travel defect: an inactive legacy destination retained its old coordinate version after a new backward arrival was assigned. On bind, the fresh arrival was scaled again. Baseline `2b76f1894132` failed with expected `(1735,680)` but actual `(2776,1049.14)`. Fixed destination-aware migration of existing voyage/spawns **before** assigning the new arrival. Expanded coverage to coordinate versions 0/1/2 and exact migrated spawn position.

Changed files: `ZoneMapConfig.cs`, four map assets, `ExpeditionLoop.cs`, `SceneFlowController.cs`, `ExpeditionProgression.cs`, `PhotoSurveyMap.cs`, `CabinStationView.cs`, `RoundThreeNavigationTests.cs`, `ZoneExitFlowPlayModeTests.cs`, `ExpeditionEndToEndPlayModeTests.cs`.

## Phase C — Debug Start

Before: QA had to play all prerequisite missions to test later zones. After: a Main Menu DEBUG control (Editor/Development compile guard) offers four presets, optional regular MAX, Start/Continue Debug and Cancel. Presets use actual objective/reward structures, including mandatory Pressure Hull/Bio Lamp/Rock Breaker progression, prior-zone maps, entry coordinates/depth and unlocks. Hidden ending stays incomplete. Runtime provisioning never edits ScriptableObject assets.

Debug writes to `timeline.json.debug` with its own recovery file. Normal Continue reads/selects the normal timeline; New Game selects normal as before. Test temporary normal save bytes remain identical across all debug runs and Continue operations. The player can resume a debug timeline through CONTINUE DEBUG.

Validation: `DebugStartPlayModeTests` job `eadde6d3803f` passed **2/2**, 16.48 seconds: all four actual menu→game presets, prior-zone checks/alternate terrain, mandatory state, reward receipt uniqueness, isolated Continue, regular MAX and Release visibility policy. No actual Release build was made; absence there is compile-guard/code-policy verified, not claimed as a built-player test.

Changed files: `DebugExpeditionPreset.cs`, `MainMenuDebugStart.cs`, MainMenu scene, `ExpeditionSave.cs`, `SceneFlowController.cs`, `SceneNavigationButton.cs`, `DebugStartPlayModeTests.cs`.

## Phase D — capture, rock event and upgrade balance

### Capture difficulty

Before: larger predictable fish with speeds 100/125/155/175. After, zone profiles expose:

| Parameter | Zone01 | Zone02 | Zone03 | Zone04 |
|---|---:|---:|---:|---:|
| Speed / turn | 100 / 100 | 165 / 190 | 220 / 270 | 280 / 340 |
| Decision interval | 1–1.8 s | .5–1 s | .25–.65 s | .18–.5 s |
| Body and collider scale | 1 | .85 | .7 | .6 |
| Evasion probability | 0 | .2 | .35 | .5 |
| Hits / timer | 5 / 45 s | 5 / 45 s | 6 / 45 s | 6 / 45 s |

Later fish have .25-second dashes (1.8×), .55-second zigzag/feint paths (1.25×), .45-second slower recovery (.7×), and a 1.5-second reaction cooldown. Hook-near reactions are edge-triggered, not probability trials on every substep. All use capped smooth steering; the existing stronger post-hit flee also recovers. Zone01's zero evasion retains its original beginner behavior. Substeps account for the maximum dash/flee relative speed and smaller collision boxes; hook mechanics/art are unchanged.

Validation: all nine capture tests passed in job `d7aad9ee85bb`, including all four profiles × seeds 17/81/1709 × 30/60/144 FPS with a steering interception driver, bounded displacement/turns/field coordinates, every advanced pattern/recovery and exactly-once capture. Zone04 is preparatory; it currently has no capture mission. Human difficulty/fun assessment remains necessary; deterministic steering success is not a human balance claim.

Changed files: `CaptureFishController.cs`, `CaptureMinigameController.cs`, `CaptureMinigameProfile.cs`, `CaptureMinigameView.cs`, four capture profile assets, configuration/capture tests.

### Rock breaker

Before: no distinct approach marker; the short hull shake cannot represent a 10-second event. After: discovered/relevant rock is marked at the authored (1480,150), radius 70, target 590±10 m; marker/hover/helm prompts derive from that same area. Locked/ready/completed states are distinct and do not replace nearby POIs.

The authoritative action validates prerequisites/location/depth, commits the objective and world flag first, then starts `RockBreakerSequence` (10 seconds configurable). Terrain/map reveal occurs at 1.5 seconds. Dedicated repeated short pulses respect `ImpactShake`'s .28-second clamp, using the 8→7→5→1.5→0 envelope at 0/1.5/4/7/10 seconds. Existing hull-impact audio is reused; no suitable dedicated destruction clip was present. Accessible text feedback still plays when screen shake is off. Rest, upgrades, capture, Journal and travel are blocked during the event. Pause stops presentation time; interruption clears the frame/input state. Continue restores committed terrain without replaying the event. Normal hull-impact code is unchanged. Failed save publication restores the rock objective/flag and does not start presentation; `SaveCurrent` now publishes a detached candidate.

Validation: both new `RockBreakerPlayModeTests` passed in job `d7aad9ee85bb`: exact marker/range/depth, failed-save rollback, one committed trigger/reward, blocked major actions, live 10-second shake/reveal, exact frame restoration, no-shake/pause and interrupted Continue. Screenshot visually inspected: cabin artwork preserved, stabilization notice visible. Manual audio feel/impact pacing assessment remains.

Changed files: `RockBreakerSequence.cs`, `ZoneOneStory.cs`, `ExpeditionLoop.cs`, `CabinFeedbackController.cs`, `CabinZoneSession.cs`, `ZoneMapPresentation.cs`, `PhotoSurveyMap.cs`, `SavedZoneMapProgress.cs`, `ExpeditionProgression.cs`, `CabinStationView.cs`, Zone03 asset, canonical Zone01 cabin scene, rock/E2E tests.

### Regular upgrades / migration

Before: Propulsion Lv1 19.8, Lv2 25; Efficiency Lv1 .9, Lv2 .8. After: exact Propulsion 25/36 and Efficiency .8/.5. Hull, recipe quantities/costs, energy capacity, reverse 1/3 ratio and acceleration remain unchanged. Every purchased current/day-start/Journal snapshot migrates idempotently during read/apply; no resource refill or second material consumption. Debug MAX uses the same target rules. Upgrade UI already reads that shared authority.

Changed files: `RegularShipUpgradeRules.cs`, `ExpeditionSave.cs`, `ExpeditionLoop.cs`, Edit/Play economy tests. New Play test verifies actual forward/reverse speed, simultaneous XY/depth maximum-of-axes energy drain, Continue, Journal and vessel-death restoration for both purchased levels.

## Final regression / limitations

Full EditMode rerun `f78f880b71e6`: **60/60 passed**, 3.06 seconds. Initial new migration DTO fixture lacked a required zone deadline; corrected the fixture, not save validation. Phase D initial focused run `d7aad9ee85bb`: 19/21 passed; two economy fixture issues were corrected (asynchronous death scene can temporarily have no Loop; unchanged reverse acceleration needs more time to reach -12).

First full PlayMode `d0498e44ab86`: **262/264 passed**, 903.16 seconds. Remaining failures were test-contract changes: death immediately after Journal rollback has no valid day-start, and the prior rock exit fixture assumed immediate unlock instead of the new 10-second blocked event. Corrected those fixtures without changing the existing day-start contract. Focused `15b423d9e9e0`: **7/7 passed**, 93.96 seconds; includes both repairs, debug presets/contrast, round trips after Journal with exact cargo checks and the real rock sequence. DEBUG screenshot visually checked after increasing button text contrast; Continue Debug is disabled until a valid isolated save exists. Rock marker screenshot visually checked: authoritative boundary/point and READY prompt align, distinct from POIs/return/exit.

Second full run `2c2026c05e59` was deliberately stopped during compatibility review to add/fix the reproduced legacy-arrival defect above; it is not a complete regression result. Post-fix `60e84cec187c` passed **2/2**, 52.12 seconds: coordinate versions 0/1/2 retain exact backward arrival and migrated spawn, plus all three return pairs after Journal with cargo checks.

Final state, after all source/config/test changes:

| Verification | Result | Unity job |
|---|---|---|
| CompilationPipeline | 0 errors; not compiling | Checked before and after full run |
| Full EditMode | **60/60 passed**, 2.99 s | `1b220f4bc83a` |
| Full PlayMode | **265/265 passed**, 920.07 s; 0 failures/skips | `c9f94d19bf2a` |
| Editor handoff | EditMode, clean Bootstrap scene | Verified after runner cleanup |
| Diff whitespace | Scripts/tests/report clean | Unity serializer empty-field whitespace retained |

The complete PlayMode run includes all requested suites, tutorial, photo submission/deletion, vessel death, backward travel, historical maps, ending flow, capture and rock presentation. Alegreya's test-generated dynamic glyph/atlas cache was reset through Unity's TMP API (preserving font feature data); its file is now byte-content-equivalent to HEAD under Git. The pre-existing dirty LiberationSans fallback font was **not** reset. An untracked empty Unity recovery scene `Assets/_Recovery/0 (8).unity` and its meta were left untouched, not treated as feature changes.

All results are real Windows Unity Editor tests, not a standalone Windows build or a human balance playtest. No commit, push or build. Existing mission IDs/rewards, SEND/DELETE, 25-day deadline, hook mechanics, hull/reverse/acceleration rules and artwork are unchanged. Unity-generated YAML has empty-field trailing spaces; do not treat that as gameplay failure or rewrite scene serialization by hand.

Manual QA after automated regression: navigate the return gates by hand, inspect DEBUG layout/readability, review smaller fish with real keyboard input in Zone02/03/04, assess rock audio/pacing with screen shake on/off, and retry the originally reported Day4/5 rollback sequence using the player's actual save if it persists (it was not reproduced on this local HEAD).
