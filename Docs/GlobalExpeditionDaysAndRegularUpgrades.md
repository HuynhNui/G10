# Global expedition days and regular upgrade scope

Implementation report, 2026-10-06. Workspace: `D:/G10`.
Starting commit: `c5fbf656f7947c6c38b5818d57134a218d158e21`.

## Implemented behavior

`ExpeditionLoop` remains the gameplay/save authority. `SceneFlowController`
remains the presentation/transition owner. No new manager, packages, currency,
creature types, sprites, map coordinates, depth model, or hidden-ending rules
were introduced. The Unity feature-implementation and MCP workflows guided the
integration, Editor scene migration, serialization checks, and live tests.

### One global deadline

- `totalExpeditionDays` is configurable on `ExpeditionLoop`, default 15.
- `DaysLeft = max(0, TotalDays - Day + 1)`.
- Day 1 has 15 days left; Day 15 has 1; advancing to Day 16 fails an
  unfinished expedition, even if the current zone's objectives are complete.
- A recorded ending exempts a completed expedition from deadline failure.
- Moving through Zone01 -> Zone02 -> Zone03 -> Zone04 preserves Day and
  DaysLeft. There are no carry-over bonuses or zone deadline resets.
- Legacy `baseDays`, `maxCarryOverDays`, and saved `zone.deadline` remain for
  serialized compatibility, but do not determine gameplay failure.
- Mission/status text reports the global day and days left.

### Rest / recovery presentation

Confirming Rest uses the existing `ZoneTransitionFade`: fade to black, invoke
the actual day-advance authority, show centered `DAY LEFT: X`, then fade back
to the cabin. The text uses realtime for 1.2 seconds by default, configurable
between 1.0 and 1.5 seconds. Successful Rest no longer opens Journal.

Movement, repeated Rest, Journal restore, purchasing, UI clicks, and normal
Escape/pause handling are blocked during the transition. The shared transition
lock prevents overlapping scene/death/rest transitions. Rest always refills in
place, even at zero Energy; the Rest UI never silently switches to rescue.
Explicit recovery callers retain the existing rescue relocation rules.
If a day-save fails, the day and Journal do not advance; transition locks clear.
The synchronous `Rest()` / `RecoverShip()` gameplay APIs remain available for
existing authority callers and tests; the player-facing confirmation uses
`PresentRest()`.

### Exactly three regular branches

| Order / stable ID | Level 0 | Level 1 | Level 2 / MAX | Lv1 material | Lv2 material |
| --- | --- | --- | --- | --- | --- |
| Hull / `Hull` | 100 hull capacity | 120 | 140 | 2 x `Z2_Creature_02` | 2 x `Z3_Creature_01` |
| Propulsion / `MaxSpeed` | 100% authored base speed | 110% | 120% | 1 x `Z2_Creature_02` | 2 x `Z3_Creature_01` |
| Energy Efficiency / `Energy` | 1.00 energy/sec | 0.90 | 0.80 | 2 x `Z2_Creature_02` | 2 x `Z3_Creature_01` |

Tier 1 total is 5 materials; Tier 2 total is 6. Level 2 requires Level 1.
Level 2 cannot be purchased again. Hull changes capacity without granting a
free heal; current hull is clamped if necessary. Energy capacity stays 100.
Propulsion changes only primary speed, preserving the existing derived reverse
ratio and unrelated acceleration, turn, dive/ascent, radar, photo, and capture
stats. Targets are set directly, not applied as accumulating deltas.

The existing UI layout/artwork is retained. Hull is first and initially
selected. Cards/details use real saved levels and current/next ship values.
Owned/required counts update from the actual inventory. Missing materials
disable the button; MAX disables it and changes its label to MAX.

`TryPurchaseUpgrade` owns validation, ship stat, level, material consumption,
and save commit as one transaction. A failed consumption/action/save leaves
the prior ship, levels, material quantities, and persisted timeline intact.
The UI no longer consumes materials after applying the action.

### Mandatory progression is separate

The separate stable `ExpeditionModule` card uses the existing
`StoryHullUpgradeAction`. `Hull` uses only `ShipUpgradeAction`.
Pressure Hull, Bio Lamp, Rock Breaker, rock destruction, route gating, and
their existing mission/research/item requirements stay in the story system.
Tutorial and mission instructions now point to the module under MODULES.

Pressure Hull still unlocks depth and the route, but no longer adds its legacy
25 HP bonus: HP progression belongs exclusively to the exact 100/120/140
regular Hull branch. This deliberate conflict resolution was explained before
implementation. The serialized `hullBonus` field is retained but unused.

### Stacks and farmable captures

`CreatureInventory` directly implements `IUpgradeMaterialInventory`. Capacity
remains 12 unique item stacks, not 12 individuals. Adding to an existing stack
works even when all unique slots are occupied. Atomic consumption aggregates
duplicate requirement IDs before validating/removing anything. Cargo displays
the actual stack quantity.

Only these authored Capture objectives have `repeatableCapture` enabled:

- Zone02: `Z2_L3_CAPTURE`, POI `zone02-l3`, `Z2_Creature_02`.
- Zone03: `Z3_L1_CAPTURE`, POI `zone03-l1`, `Z3_Creature_01`.

First success records the normal mission objective/reward once and adds one
material. Subsequent successes add exactly one to the same stack without
recording the objective/reward again. They use the same capture minigame,
position/contact/depth checks, required photograph, and Capture Attempt charge.
Rest refills attempts through existing ship refill behavior. Other Capture
and Collect objectives stay one-time. Legacy location/compatibility rewards
cannot duplicate a one-time item merely because inventory now supports stacks.

## Save schema and compatibility

- Schema version is 5 (previously 4).
- Each `SavedCreature` stores positive `quantity`; old missing quantities
  migrate to 1.
- Each snapshot stores explicit `ShipUpgradeProgress`: hull, propulsion,
  efficiency levels (0..2), and the authored base movement speed.
- Missing old levels migrate to 0; they are never inferred from ship stats.
- New Game starts all three at 0. Continue, Journal checkpoints, and day-start
  death snapshots persist/restore levels, ship stats, and quantities together.
- `capturesTaken` is cumulative so crafting does not reduce Journal capture
  totals. Legacy snapshots initialize it from their old unique cargo count.
- Migration applies to current, day-start, and every Journal snapshot. Existing
  tutorial/progression migration and atomic primary/backup writes remain.
- Reload does not reapply an upgrade. Explicit upgraded saves preserve their
  ship stats even with the scene-stat preview override enabled.
- Legacy saves retain existing stored ship stats when the existing load policy
  uses saved stats. They are not retroactively rebalanced or assigned inferred
  levels. Their next purchase sets the selected branch's new exact target.
- New-format invalid quantities/levels are rejected rather than silently
  repaired. Older game versions cannot read version 5 saves.

## Validation

Actual Unity Editor tests were run with isolated save/photo cache paths;
the player's normal timeline was not used by the test fixtures.

| Run | Result | Notes |
| --- | --- | --- |
| Before changes, selected PlayMode baseline (`6e6a1b0edb40`) | 25/30 pass, 5 fail | Three Cargo tutorial/setup failures, one inactive ShipStatus lookup, one Scope UI tutorial lock. |
| Initial focused PlayMode (`2cbfc8638a5a`) | 15/17 pass, 2 fail | Fixed real cargo icon/catalog reload issue and farm test's incorrect cross-zone charge assumption. |
| Intermediate regression (`2d4b8fcb5208`) | 82/91 pass, 9 fail | Seven expected save-v4 assertions became stale; Capture fixture needed returning-player tutorial setup; Photo test confused content existence with detection range. |
| Final EditMode (`9a8455928c50`) | 12/12 pass, 0 fail | Exact/idempotent targets, recipe totals, stack capacity, aggregate atomic consumption, migration across all snapshots, invalid v5 data. |
| Final PlayMode (`dd47895cd5ac`) | 94/94 pass, 0 fail, 0 skipped | 301.08 seconds; all 17 selected fixtures. |

The final PlayMode selection covers these complete fixtures:

`ExpeditionEconomyPlayModeTests`, `ExpeditionLoopPlayModeTests`,
`ExpeditionSaveResetTests`, `ExpeditionNewGamePlayModeTests`,
`VesselDeathPlayModeTests`, `CargoPlayModeTests`, `ZoneOneStoryPlayModeTests`,
`ExpeditionEndToEndPlayModeTests`, `TutorialPlayModeTests`, `TutorialSaveTests`,
`NavigationBudgetPlayModeTests`, `CaptureMinigamePlayModeTests`,
`PhotoSurveyPlayModeTests`, `PhotoSubmissionPlayModeTests`,
`ProjectSceneFlowPlayModeTests`, `EndingPresentationPlayModeTests`,
`ScopeUIPresentationPlayModeTests`.

The new 8 PlayMode tests specifically exercise: Day15/16 and completed-zone
failure, Continue/Journal day restoration, realtime Rest/recovery and double
fire locks, exact purchases/MAX/reload, failed purchase save rollback, Journal
and vessel-death restoration, live card/count refresh, failed Rest save lock
cleanup, both material farms with real minigames/charges/reload, and one-time
Collect preservation. Existing end-to-end tests verify all four zones,
mandatory modules/rock, normal ending, hidden ending, and reload.

The pre-change baseline did not include Capture/Photo fixtures, so their two
intermediate failures are diagnosed setup/assertion issues, not claimed as
separately reproduced baseline failures. No radar/photo/depth tuning was made
to satisfy them. Final Unity compilation reports 0 errors/warnings and the
Console contains 0 errors/warnings. The restored Zone01 Editor scene is clean
(not dirty), outside Play Mode, with 0 missing scripts. Live Editor inspection
confirmed exactly Hull/MaxSpeed/Energy in sibling order 0/1/2, each using
ShipUpgradeAction and both real material definitions; ExpeditionModule alone
uses StoryHullUpgradeAction; the UI material source is CreatureInventory.
No standalone Player build, performance benchmark, or human full playthrough
was performed.

## Unity setup and remaining checks

No required manual Inspector wiring remains for the checked-in Zone01 cabin.
Its three regular cards, module action, real inventory source, and material
definitions were migrated in Unity Editor. All zones share this cabin.
Use Bootstrap as the normal game entry point.

Optional configuration:

- Change `GameplayCore / ExpeditionLoop > Total Expedition Days` to override 15.
- Change `Bootstrap / SceneFlowController > Rest Day Text Seconds` within 1.0..1.5.
- `G10/Computer/Apply Three Branch Upgrade Scope` reapplies the targeted
  idempotent card/action migration if regenerating an old cabin UI; save the
  scene afterwards. Do not run the unrelated visual-layout rebuild for this task.
- Material icons reuse the existing Capture placeholder because these two
  content definitions currently have no specific creature art. Artwork may be
  replaced manually later without changing IDs or recipes.

Recommended human smoke check: Bootstrap -> New Game/tutorial -> Rest ->
DAY LEFT:14 -> capture Tier1 material -> purchase -> Continue -> Journal
restore. Automated tests cover the functional state, not every visual
alignment at every resolution.

## Exact files changed

All paths below are absolute. No other gameplay scenes, sprites, packages,
project settings, or hidden-ending implementation files were edited.

### Runtime code (modified)

```text
D:/G10/Assets/_Project/Scripts/Computer/ExpeditionComputerView.cs
D:/G10/Assets/_Project/Scripts/Computer/ExpeditionLoop.cs
D:/G10/Assets/_Project/Scripts/Computer/ExpeditionSave.cs
D:/G10/Assets/_Project/Scripts/Computer/ShipUpgradeAction.cs
D:/G10/Assets/_Project/Scripts/Computer/SubmarineUpgradeUIController.cs
D:/G10/Assets/_Project/Scripts/Computer/UpgradeData.cs
D:/G10/Assets/_Project/Scripts/Computer/UpgradeEntryConfig.cs
D:/G10/Assets/_Project/Scripts/Core/SceneFlowController.cs
D:/G10/Assets/_Project/Scripts/Missions/ZoneMissionConfig.cs
D:/G10/Assets/_Project/Scripts/Missions/ZoneMissionRuntime.cs
D:/G10/Assets/_Project/Scripts/Missions/ZoneOneStory.cs
D:/G10/Assets/_Project/Scripts/Navigation/CreatureCatcher.cs
D:/G10/Assets/_Project/Scripts/Navigation/CreatureInventory.cs
D:/G10/Assets/_Project/Scripts/Tutorial/TutorialConfig.cs
D:/G10/Assets/_Project/Scripts/UI/CreatureInventoryView.cs
D:/G10/Assets/_Project/Scripts/UI/UIManager.cs
```

### Editor tools and authored data/scene (modified)

```text
D:/G10/Assets/_Project/Scripts/Editor/CargoAppEditor.cs
D:/G10/Assets/_Project/Scripts/Editor/MissionConfigEditor.cs
D:/G10/Assets/_Project/Scripts/Editor/SubmarineUpgradeUIEditor.cs
D:/G10/Assets/_Project/Content/Missions/Zone02.asset
D:/G10/Assets/_Project/Content/Missions/Zone03.asset
D:/G10/Assets/_Project/Data/Tutorial/ZoneOneTutorialConfig.asset
D:/G10/Assets/_Project/Scenes/Gameplay/Zone01.unity
```

The scene migration retains the existing UI containers, layout, and art,
removes excluded cards, reuses the old DepthSystem card for ExpeditionModule,
and rewires actions/materials. Unity also reserialized blank YAML fields with
trailing spaces. Script whitespace checks pass; the full diff passes when
ignoring those generated blank-field trailing spaces.

### Existing tests (modified)

```text
D:/G10/Assets/_Project/Tests/PlayMode/CaptureMinigamePlayModeTests.cs
D:/G10/Assets/_Project/Tests/PlayMode/CargoPlayModeTests.cs
D:/G10/Assets/_Project/Tests/PlayMode/ExpeditionEndToEndPlayModeTests.cs
D:/G10/Assets/_Project/Tests/PlayMode/ExpeditionLoopPlayModeTests.cs
D:/G10/Assets/_Project/Tests/PlayMode/PhotoSurveyPlayModeTests.cs
D:/G10/Assets/_Project/Tests/PlayMode/ScopeUIPresentationPlayModeTests.cs
D:/G10/Assets/_Project/Tests/PlayMode/TutorialSaveTests.cs
D:/G10/Assets/_Project/Tests/PlayMode/ZoneOneStoryPlayModeTests.cs
```

### New implementation, data, tests, and metadata

```text
D:/G10/Assets/_Project/Scripts/Computer/RegularShipUpgradeRules.cs
D:/G10/Assets/_Project/Scripts/Computer/RegularShipUpgradeRules.cs.meta
D:/G10/Assets/_Project/Data/Upgrade/Z2_Creature_02.asset
D:/G10/Assets/_Project/Data/Upgrade/Z2_Creature_02.asset.meta
D:/G10/Assets/_Project/Data/Upgrade/Z3_Creature_01.asset
D:/G10/Assets/_Project/Data/Upgrade/Z3_Creature_01.asset.meta
D:/G10/Assets/_Project/Tests/EditMode.meta
D:/G10/Assets/_Project/Tests/EditMode/G10.Prototype.EditModeTests.asmdef
D:/G10/Assets/_Project/Tests/EditMode/G10.Prototype.EditModeTests.asmdef.meta
D:/G10/Assets/_Project/Tests/EditMode/ExpeditionEconomyTests.cs
D:/G10/Assets/_Project/Tests/EditMode/ExpeditionEconomyTests.cs.meta
D:/G10/Assets/_Project/Tests/PlayMode/ExpeditionEconomyPlayModeTests.cs
D:/G10/Assets/_Project/Tests/PlayMode/ExpeditionEconomyPlayModeTests.cs.meta
D:/G10/Docs/GlobalExpeditionDaysAndRegularUpgrades.md
```

### Unity-generated font cache serialization (modified, preserved)

```text
D:/G10/Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset
D:/G10/Assets/_Project/Art/UI/Fonts/AlegreyaSansSC-Regular SDF.asset
```

These are dynamic TMP glyph/cache serialization from Editor validation, not
new artwork or a UI redesign. Unity-generated changes were preserved rather
than reverted. The pre-existing untracked `Assets/_Recovery/0 (7).unity` and
`1 (5).unity` plus their `.meta` files remain untouched and are not task edits.
No Git commit or push was made.
