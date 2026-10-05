# Zone / POI depth plumbing — 2026-10-05

Scope: configuration and shared depth resolution only. Baseline HEAD was
`203d1e0ac7d23032a68ea2e319f2a7ade5a6da74`; worktree was clean before this change.
No commit or push was requested or performed.

## Authored data

| Zone | minimumDepth | entryDepth |
| --- | ---: | ---: |
| Zone01 | 0 | 230 |
| Zone02 | 230 | 230 |
| Zone03 | 230 | 230 |
| Zone04 | 500 | 500 |

Entry XY and heading are unchanged, including Zone01 `(100,400)`, heading `90`.
All existing location overrides remain false (missing serialized fields use the
default false/zero). No mission-specific target depth was authored. Effective
legacy POI depths remain 230/230/230/500 respectively.

## Ownership and safety

- `ZoneNavigation.ConfigureDepthRange` owns the environmental floor, separate
  from `ShipState`. One `ClampDepth` helper handles movement, voyage restore,
  reset and scene-stat application. StepDepth and Navigate share MoveDepth.
- `CabinZoneSession.Configure` sets the floor immediately after selecting the
  active config, before terrain binding and ExpeditionLoop snapshot restoration.
- Existing shallow Zone02/03/04 snapshots are clamped on application, including
  Journal restore and death rollback. Saves are neither invalidated nor deleted;
  save version remains 4. Saving current state persists the clamped depth.
- Invalid negative/non-finite floor values resolve to zero. If an authored floor
  exceeds ship maximum depth, the effective minimum is the ship maximum: the
  range collapses to a single reachable depth. The requested floor is retained
  for subsequent capability changes and the asset is not rewritten.
- `PhotoSurveyZone.DepthFor(poi)` is the authoritative target-depth resolver.
  `overrideDepth=true` selects the POI target; otherwise it uses the legacy zone
  fallback. Negative/non-finite target values resolve safely to zero at use
  without rewriting content. Valid targets are not capped to ship capability.
- Contact world positions, radar 3D distance, photo targeting and detection,
  CreatureCatcher validation (including completion revalidation), legacy
  ZoneOneStory collection validation and the POI hover-depth label share this
  resolver. Range, FOV, arrival radius, tolerance and objective rules are unchanged.

No Energy-depth multiplier, energy rebalance, stat rebalance, mission reward,
deadline, tutorial step/dialogue, layout or artwork change was made.

## Exact changed files

Paths are relative to `D:/G10`:

- `Assets/_Project/Content/Maps/Zone01.asset`
- `Assets/_Project/Content/Maps/Zone02.asset`
- `Assets/_Project/Content/Maps/Zone03.asset`
- `Assets/_Project/Content/Maps/Zone04.asset`
- `Assets/_Project/Scripts/Navigation/ZoneMapConfig.cs`
- `Assets/_Project/Scripts/Navigation/ZoneNavigation.cs`
- `Assets/_Project/Scripts/Navigation/PhotoSurveyZone.cs`
- `Assets/_Project/Scripts/Navigation/CreatureCatcher.cs`
- `Assets/_Project/Scripts/Missions/ZoneOneStory.cs`
- `Assets/_Project/Scripts/UI/CabinZoneSession.cs`
- `Assets/_Project/Scripts/UI/CabinStationView.cs`
- `Assets/_Project/Tests/PlayMode/ZoneDepthPlayModeTests.cs` (new)
- `Assets/_Project/Tests/PlayMode/ZoneDepthPlayModeTests.cs.meta` (Unity-generated)
- `Assets/_Project/Tests/PlayMode/VesselDeathPlayModeTests.cs`
- `Assets/_Project/Tests/PlayMode/ExpeditionEndToEndPlayModeTests.cs`
- `Assets/_Project/Tests/PlayMode/PhotoSurveyPlayModeTests.cs`
- `Docs/AI/ZoneDepthPlumbing.md` (this report)

Unity also populated dynamic TMP glyph/atlas caches while running PlayMode tests;
these generated changes were left intact, not reverted over possible editor work:

- `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset`
- `Assets/_Project/Art/UI/Fonts/AlegreyaSansSC-Regular SDF.asset`

The PhotoSurvey fixture now uses an isolated temporary timeline/photo archive
and returning-player tutorial state, preventing test runs from modifying the
user's real save. Its existing assertions were not changed.

## Validation

Unity 6000.4.2f1, Editor PlayMode tests:

1. Focused model/budget/terrain batch: **31 passed, 0 failed**.
   Job `c483cb8aac79`: ZoneDepthPlayModeTests (19), NavigationBudgetPlayModeTests
   (7), RadarTerrainMaskTests (5).
2. Focused integration batch: **43 passed, 5 failed**, 48 total.
   Job `823c1340b3ad`:

   | Fixture | Passed | Failed |
   | --- | ---: | ---: |
   | CaptureMinigamePlayModeTests | 5 | 1 |
   | ExpeditionLoopPlayModeTests | 6 | 1 |
   | ExpeditionSaveResetTests | 3 | 0 |
   | PhotoSurveyPlayModeTests | 0 | 2 |
   | RadarFilledTerrainPlayModeTests | 0 | 1 |
   | TutorialPlayModeTests | 6 | 0 |
   | TutorialSaveTests | 14 | 0 |
   | VesselDeathPlayModeTests | 9 | 0 |

   The new VesselDeath test verifies all four authored configs, unchanged
   Zone01 entry XY/heading, every existing POI's disabled override, and actual
   Continue / Journal / death rollback from old shallow Zone02/03/04 snapshots.

3. Broad regression batch: **43 passed, 0 failed**.
   Job `9cfd11b49ed9`:

   | Fixture | Passed | Failed |
   | --- | ---: | ---: |
   | DirectCabinInteractionPlayModeTests | 5 | 0 |
   | ExpeditionEndToEndPlayModeTests | 3 | 0 |
   | PhotoSubmissionPlayModeTests | 8 | 0 |
   | TutorialPlayModeTests | 6 | 0 |
   | VesselDeathPlayModeTests | 9 | 0 |
   | ZoneOneStoryPlayModeTests | 12 | 0 |

   All three end-to-end routes/services passed, including normal and hidden
   endings. The helpers now use resolved POI depth for interactions and active
   entry depth for route travel; fresh Zone04 arrival is asserted at 500m.

Totals: **122 test executions, 117 passed / 5 failed**, including 15 repeated
tutorial/death cases between batches. Across unique cases: **107 total,
102 passed / 5 failed**. All 20 newly added test cases passed.

Final `git diff --check`: passed (exit 0). No compiler errors reported.
Editor returned to clean Bootstrap, outside Play Mode. Changes remain uncommitted.

Unchanged assertions which failed in batch 2 (not rewritten to make tests green):

- CaptureMinigamePlayModeTests.KeyboardSteersHeadingCableTracksHookAndEscapeRestoresCabin:
  capture UI is not active at line 94. The fixture starts with fresh tutorial
  knowledge, then attempts the cabin capture shortcut.
- ExpeditionLoopPlayModeTests.RestConfirmationRollbackAndReloadPreserveActualGameplay:
  null ShipStatusView at line 55, before its depth/rollback assertions.
- PhotoSurveyPlayModeTests.DepthButtonsChartAndRealRadarContact: expects a radar
  contact to be detectable at the entry position before moving to the POI.
- PhotoSurveyPlayModeTests.MissionUsesPoiRadiusAndNeverDrawsDestinationTiles:
  overlay has 24 vertices; legacy assertion expects 8.
- RadarFilledTerrainPlayModeTests.AuthoredLineMapFillsTerrainAndRadarStillRevealsWithSweep:
  terrain assertion at fixed XY `(200,500)` differs from the current map mask.

These failures concern tutorial/UI assumptions or XY rendering/terrain; this
patch does not change those assertions or the relevant terrain/layout code.
No separate pre-change baseline run was performed in this session, so the
complete regression suite is **not** reported as green.

The initial test attempt was canceled before executing tests because a missing
test namespace prevented compilation. It was fixed before the completed runs
above; this canceled attempt is not counted as a test pass/fail.

## Remaining design couplings for later balancing

- The intentional legacy fallback still derives from `entryDepth` until a
  designer enables a POI override. Overrides below zone floor or beyond current
  ship capability remain authored as-is and may be unreachable; choose future
  mission values with the available capabilities in mind.
- Map coordinate readout Z is current ship depth, not hovered POI target depth;
  its no-navigation preview fallback remains the zone depth.
- The legacy `PhotoSurveyEditor` setup command still seeds fallback depth from
  scene start depth and prints a global template label. It is not the runtime
  contact-depth authority.
- Terrain/arrival regions, route exits, rock interaction and hidden final-arrival
  checks remain XY-based. This pass changes POI radar/photo/capture depth only;
  it does not invent depth gates for zone transitions or rock destruction.
- Camera visibility already depends on absolute ship depth through the existing
  photo profile. That behavior was preserved, not rebalanced.
