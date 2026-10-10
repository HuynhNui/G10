# Playtest Round 2 — implementation and verification

Repository: local `D:\G10`; starting HEAD `ffe0883af32da815f6fb4758f29f7ebb3741b902`.
No commit, push, package installation or Windows build. Work followed Unity feature implementation, evidence-first bug investigation, MCP workflow and uGUI skills. Changes preserve existing artwork/controller, mission IDs, recipes, costs, rewards and hidden-route criteria.

## Reproduced causes / before → after

1. **Depth disagreement:** radar/camera used 3D distance with no target-depth gate; capture used a separate ±10 m tolerance. Baseline regression detected target 260 at ship depths 250 and 300 (both wrong). One authoritative inclusive band now lives in `PhotoSurveyZone`: target depth through target + 30 m. Radar retains XY range and terrain LOS; camera retains its 3D range, FOV and terrain LOS in addition to this band. Capture independently requires the original XY arrival radius and LOS, not an obsolete ±10 m gate.
2. **Zone02 farm → Zone03 L1:** reproduced in the actual camera/SEND/capture/upgrade/transition flow. Zone02 repeats consumed the shared five-charge budget; the third repeat was `NoCharges`. Zone03 L1 was `PhotoRequired` before SEND, then `NoCharges` after SEND. Day-1 L1 marker `(446.08,214.46)` versus persisted contact approximately `(461.08,190.01)` is a **28.68447-unit offset**, larger than the unchanged **20-unit capture radius**. Cargo stacking and mission/POI binding were not the cause. No radius enlargement or spawn/POI repositioning was used.
3. **Unhelpful EMPTY:** out-of-range/depth/absent/occluded targets were collapsed to one result. Each now has a distinct result and short feedback, including `TARGET TOO SHALLOW`, `TARGET TOO DEEP`, `TOO FAR FROM TARGET`, `PHOTO DATA REQUIRED`, `NO CAPTURE TARGET`, `CARGO FULL` and terrain blockage. A consumed one-time contact is reported as absent rather than selecting a distant remaining creature. Editor/development-build rejection logs include zone, POI/objective, marker/contact/ship XY, distance/radius, depth/band, presence, photo submission, charges/unlimited mode and cargo/stack capacity. No per-frame logging.
4. **Daily capture cap:** default-enabled Inspector setting bypasses capture-charge gating/deduction only. Saved capacity/current-charge fields remain unchanged/readable. Limited mode is still supported. Failed/cancelled attempts give no cargo; successful repeatable materials use the existing runtime resolver; one-time objectives/rewards remain one-time. Status/capture UI says `UNLIMITED`; tutorial recovery does not depend solely on zero captures in unlimited mode.
5. **Transition reset:** first-time destinations formerly used `entryDepth`. The departure depth is now persisted in the destination's initial voyage before committing/loading. XY/heading still come from authored entry. Destination minimum depth and actual ship ceiling clamp only when required; existing feedback explains `DEPTH ADJUSTED`. Existing destination voyage snapshots are not overwritten. Continue/Journal/death restore still use their snapshots; New Game clears pending transient notices.
6. **Zone03 entry:** `(50,630)` → `(11,97)`, heading 90° retained. Unity navigation footprint is clear; 25 units ahead are clear; a 5-unit footprint-aware flood search reaches all four POIs through the original terrain mask (26,273 reachable nodes). The named Zone03 entry rescue point is aligned with the new entry. Mission POIs, exit and terrain unchanged.
7. **Empty-energy warning:** reproduced by emitting a legitimate depth-limit event, then depleting Energy. An old timed `DEPTH LIMIT` remained although no new depth-limit event was emitted. Blocked commands now emit a latched resource rejection, clear stale depth-limit latch state, and replace the old warning. Priority: destroyed vessel, `NO ENERGY`, actual `DEPTH LIMIT`. Helm has the same persistent resource status. Tests cover intermediate/minimum/maximum depths and destroyed + empty state.
8. **Balance:** shared fish profile, 15-day C# default, absent explicit serialized deadline, and Level 2 = base × 1.2 were the previous behaviors. Profiles now bind through active `ZoneMapConfig`; actual GameplayCore deadline is explicitly 25; purchased Level 2 speed becomes exactly 25, with idempotent load/Journal/day-start migration, no purchase/material cost or refill. Acceleration, reverse ratio, turn/dive/ascent, upgrade costs and maximum level unchanged.
9. **Radar/hover:** radar previously persisted 6 s total (4 s after the sweep), only cleared on close while actively sweeping, and could leave its final cached mesh. Now duration = 2 s sweep + configurable 5 s results; fade begins after sweep and ends at expiry; close/reset clears results and echoes; expiry triggers a final rebuild; a successful later sweep replaces results. Only actual detection reveals a POI. Map cursor text now contains only X/Y; separate mission `TARGET DEPTH`, Helm depth and depth data remain intact.

## Final configuration

| Setting | Value |
|---|---|
| Interaction depth | `[DepthFor(poi), DepthFor(poi) + deeperInteractionRange]`, default 30 m deeper |
| Capture XY radius | Original POI radius (normally 20), unchanged |
| Capture attempts | `resourceSettings.unlimitedCaptureAttempts = true`, explicitly serialized in Zone01 |
| Zone03 entry / heading | `(11,97)` / 90° |
| Expedition days | Inspector field `totalExpeditionDays = 25`, explicit GameplayCore serialization |
| Propulsion (base 18) | Level 0 = 18; Level 1 = 19.8; Level 2 = 25 units/s |
| Radar | Sweep 2 s; post-sweep 5 s; default total 7 s |

| Fish parameter | Zone01 | Zone02 | Zone03 | Zone04 (preparatory) |
|---|---:|---:|---:|---:|
| Move speed | 100 | 125 | 155 | 175 |
| Turn speed | 100 | 145 | 190 | 220 |
| Decision interval (s) | 1–1.8 | .7–1.3 | .45–.9 | .35–.75 |
| Flee multiplier | 1.6 | 1.8 | 2.0 | 2.2 |
| Hits | 5 | 5 | 6 | 6 |
| Attempt time (s) | 45 | 45 | 45 | 45 |
| Short evasion chance per decision | 0 | .12 | .18 | .22 |

Evasions last .3 s at ×1.25 speed and request a 35–85° turn through the same bounded steering motor; no instant turn/teleport. Existing hook settings, hitboxes and art unchanged.

## Exact implementation files

Paths below are relative to `D:\G10`.

- `Assets/_Project/Scripts/Navigation/PhotoSurveyZone.cs`
- `Assets/_Project/Scripts/Navigation/CreatureCatcher.cs`
- `Assets/_Project/Scripts/Navigation/ShipResources.cs`
- `Assets/_Project/Scripts/Navigation/ZoneNavigation.cs`
- `Assets/_Project/Scripts/Navigation/ZoneMapConfig.cs`
- `Assets/_Project/Scripts/Navigation/CaptureMinigameProfile.cs`
- `Assets/_Project/Scripts/Navigation/CaptureFishController.cs`
- `Assets/_Project/Scripts/Missions/ZoneOneStory.cs`
- `Assets/_Project/Scripts/Computer/ExpeditionLoop.cs`
- `Assets/_Project/Scripts/Computer/RegularShipUpgradeRules.cs`
- `Assets/_Project/Scripts/Computer/ShipStatusView.cs`
- `Assets/_Project/Scripts/Feedback/CabinFeedbackController.cs`
- `Assets/_Project/Scripts/Tutorial/TutorialManager.cs`
- `Assets/_Project/Scripts/UI/CabinStationView.cs`
- `Assets/_Project/Scripts/UI/CabinZoneSession.cs`
- `Assets/_Project/Scripts/UI/CreatureCaptureView.cs`
- `Assets/_Project/Scripts/UI/RadarDisplay.cs`
- `Assets/_Project/Scripts/UI/PhotoSurveyMap.cs`
- `Assets/_Project/Content/Maps/Zone01.asset`
- `Assets/_Project/Content/Maps/Zone02.asset`
- `Assets/_Project/Content/Maps/Zone03.asset`
- `Assets/_Project/Content/Maps/Zone04.asset`
- `Assets/_Project/Data/Computer/Zone01CaptureMinigame.asset`
- New `Assets/_Project/Data/Computer/Zone02CaptureMinigame.asset` + Unity-generated `.meta`
- New `Assets/_Project/Data/Computer/Zone03CaptureMinigame.asset` + Unity-generated `.meta`
- New `Assets/_Project/Data/Computer/Zone04CaptureMinigame.asset` + Unity-generated `.meta`
- `Assets/_Project/Scenes/Gameplay/GameplayCore.unity`
- `Assets/_Project/Scenes/Gameplay/Zone01.unity`

Test files: `Assets/_Project/Tests/EditMode/ExpeditionEconomyTests.cs`, new `RoundTwoConfigurationTests.cs` + generated `.meta`; and PlayMode `CabinFeedbackPlayModeTests.cs`, `CaptureMinigamePlayModeTests.cs`, `DirectCabinInteractionPlayModeTests.cs`, `ExpeditionEconomyPlayModeTests.cs`, `ExpeditionEndToEndPlayModeTests.cs`, `MissionDepthAuthoringPlayModeTests.cs`, `PhotoSurveyPlayModeTests.cs`, `RadarTerrainPlayModeTests.cs`, `ScopeUIPresentationPlayModeTests.cs`, `TutorialPlayModeTests.cs`, `ZoneDepthPlayModeTests.cs`, `ZoneExitFlowPlayModeTests.cs`, `ZoneOneStoryPlayModeTests.cs`.

## Verification

- Initial relevant interaction baseline: 8/8 passed.
- New baseline reproduction: target 260 at depths 250/300 incorrectly detected; real Zone02 farming exhausted charges and Zone03 L1 failed after SEND. Stale `DEPTH LIMIT` reproduced separately with Energy zero.
- Staged validation caught and corrected old test assumptions: generic EMPTY, ±10 m, daily charge deduction, 15 days, speed 21.6, hover Z, and test-only later-zone ships missing prerequisite Pressure Hull. Save migration fixture maintains a valid same-day day-start snapshot; production save validation was not relaxed.
- Fish interception succeeds for Zone01–03 with seeds 17/81/1709 at 15/144 FPS; bounded motion checked in all four profiles at the same seeds/rates.
- Regression flow farms Zone02 eight times, transitions at 470 m into Zone03 `(11,97)`, validates marker/contact/SEND, captures L1 with zero legacy charges, Continue restores contact, and performs sixteen further catches over two Rest cycles without duplicated objectives.
- Serialized profile/deadline/spawn tests and existing EditMode tests: **57/57 passed**, job `353ad5e159d5`.
- Full PlayMode run: **246/249 passed**, job `e93cf75b3b91`, 580.36 s. Three tests still expected the old rules: failure after day 15, generic EMPTY, and resetting a departing depth of 231 m to 230 m. Those assertions were updated to the requested behavior; no production code changed after this broad run.
- Rerun of all affected feedback/direct-interaction/zone-exit/end-to-end fixtures, the upgraded-save UI regression and a new zero-capture tutorial regression: **37/37 passed**, job `ba05ef3870af`, 213.23 s. Includes an existing destination voyage retaining depth/heading/distance through entry + Continue and New Game clearing it. The farm-to-Zone03 regression now also reloads immediately after entry to confirm saved depth 470 m.
- **Final latest results: 251 distinct PlayMode tests passed; zero outstanding failures**, merging the broad run with this affected-suite rerun. This is not a claim that one single 251-test run was performed. See `PlaytestRound2-2026-10-11-tests.json` for per-test latest results and run summaries.
- All requested coverage ran: PhotoSurvey, Radar terrain/mask/filled terrain (the terrain fixture names are `RadarTerrainMaskTests` and `RadarFilledTerrainPlayModeTests`), CaptureMinigame, ExpeditionEconomy, ExpeditionEndToEnd, NavigationBudget, ZoneDepth, VesselDeath, CabinFeedback, ScopeUIPresentation, plus tutorial, Photo Lab deletion/SEND, exit, lighting, save/checkpoint/Journal and both endings.
- Inspected Windows Editor screenshots `Temp/TestCaptures/scope-chart.png` and `radar-filled-complete.png`: original artwork/layout preserved; hover X/Y only and separate TARGET DEPTH retained. C# compilation: zero errors. Final Editor state: clean Bootstrap, EditMode. `git diff --check` clean.

Automated tests run in the real Windows Unity Editor, not a standalone Windows player. Human-playtested feel/accessibility/balance, prolonged sessions and a standalone build are not claimed verified. No Windows build was requested or made.

Pre-existing deleted performance-test Resources files and modified ProjectSettings are unrelated user changes and remain preserved. Test-generated dynamic font/glyph/atlas changes were mechanically restored to the pre-test versions in exactly the two previously clean font assets; neither is in the final diff. No artwork redesign.
