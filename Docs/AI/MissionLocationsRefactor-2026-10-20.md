# Imported reference — mission refactor synchronization

Source: local codex/unified-effect-director, commit 43b1a26. Imported 2026-10-22 for selective synchronization with newer local audio and visual work. The validation claims below describe that source branch, not this merged working tree; current verification must be reported separately. Newer local authored POI geometry and depths are preserved where already within the 500 m cap.

## Current merged geometry — 2026-10-23

The installed local maps retain Zone 1 interaction radii of **35 units** at depths **230 / 460 m**. Zone 2 retains interaction radii of **20 units** at depths **280 / 460 m**. Zone 2's second site is still `zone02-l3`, now paired with canonical objective prefix `Z2_L2`; it is reachable with the initial 500 m hull. The historical source-branch paragraph below describing a 510 → 500 m adjustment is not the current local depth setting.

Current scope is 2 / 2 / 3 / 4 sites: **9 mandatory + 2 hidden organisms**. The historical validation totals below must not be counted as verification of the current audio/visual integration; current results are recorded in `Docs/QA/Zone03Cinematic/results.md`.

# Mission locations and progression — 2026-10-20

Implementation in D:/G10. No commit or push. Validation results and evidence are recorded below.

## Final mission configuration

PHOTO means a valid creature photo followed by SEND in Photo Lab. CAPTURE uses the existing spatial validation and capture minigame, after the required photo is submitted. Each objective has its own receipt. Cargo/material IDs deliberately use the captured creature ID so existing inventory/save and regular upgrade recipes remain compatible.

| Location ID | Retained POI ID | Creature | Required objectives | Capture reward / unlock |
| --- | --- | --- | --- | --- |
| Z1_L1 | zone01-left | Z1_Creature_01 | PHOTO + CAPTURE | 1 lamp material; RECIPE_BIO_LAMP |
| Z1_L2 | zone01-north | Z1_Creature_02 | PHOTO | None |
| Z2_L1 | zone02-l1 | Z2_Creature_01 | PHOTO + CAPTURE | 1 hull material A |
| Z2_L2 | zone02-l3 | Z2_Creature_02 | PHOTO + CAPTURE | 1 hull material B; RECIPE_PRESSURE_HULL |
| Z3_L1 | zone03-l1 | Z3_Creature_01 | PHOTO + CAPTURE | 1 explosive material; RECIPE_ROCK_BREAKER |
| Z3_L2 | zone03-l2 | Z3_Creature_02 | PHOTO | None |
| Z3_L3 | zone03-l4 | Z3_Creature_03 | PHOTO + CAPTURE | 1 detonator material |
| Z4_L1 | zone04-l1 | Z4_Creature_01 | PHOTO + CAPTURE | 1 captured sample; hidden coordinate hint in Mission Log |
| Z4_L2 | zone04-l2 | Z4_Creature_02 | PHOTO | None |
| Z4_L3 | zone04-l3 | Z4_Creature_03 | PHOTO; hidden radar location | None |
| Z4_L4 | zone04-l5 | Z4_Creature_04 | PHOTO; hidden radar location | None |

Objective IDs are `<Location ID>_PHOTO` and, where present, `<Location ID>_CAPTURE`. Gate IDs are Z1_GATE_INSTALL_BIO_LAMP, Z2_GATE_INSTALL_PRESSURE_HULL, and Z3_GATE_CRAFT_ROCK_BREAKER / Z3_GATE_INSTALL_ROCK_BREAKER / Z3_GATE_DESTROY_ROCK_BARRIER.

Z2_Creature_02 and Z3_Creature_01 remain repeatable farming sources for regular upgrades. Other captures are one-time. Stack/slot limits stay owned by CreatureInventory (12 distinct stacks). Capture adds cargo immediately on success; PHOTO and SEND never grant capture cargo. Repeated SEND cannot repeat objective rewards.

## Material, recipe and route rules

| Material ID / quantity | Module or recipe | Required fieldwork and resulting route |
| --- | --- | --- |
| Z1_Creature_01 x1 | RECIPE_BIO_LAMP -> install UPGRADE_BIO_LAMP | Both Zone01 PHOTO + Z1L1 CAPTURE -> Zone02 |
| Z2_Creature_01 x1 + Z2_Creature_02 x1 | RECIPE_PRESSURE_HULL -> install UPGRADE_PRESSURE_HULL | All Zone02 PHOTO/CAPTURE -> max depth 750 m, Zone03 |
| Z3_Creature_01 x1 + Z3_Creature_03 x1 | RECIPE_ROCK_BREAKER -> assemble EXPLOSIVES + DETONATOR -> install | All Zone03 fieldwork; then interact at Rock Barrier -> alternate terrain/map/effect and Zone04 |
| Z4_Creature_01 x1 | Captured sample + hint; no module cost | Hint only; neither hidden discovery nor ending choice is automatically unlocked |

Story module materials are consumed only on the relevant successful craft/install transaction. Capture, recipe receipts, cargo, ship depth and save publication are committed together, with rollback if writing fails. Pressure Hull changes maximum depth, independently of the regular Hull HP branch. Installed depth capability also survives scene-stat preview overrides and revisits.

Retained positions, interaction radii and depths are preserved. The only depth exception is zone02-l3: 510 -> 500 m, matching the pre-Pressure-Hull maximum. Zone04's retained hidden points stay at (1006.56, 450.51428) and (358.56, 119.57143); hint text uses (1006.56, 450.51) and (358.56, 119.57).

All main fieldwork (including Z4L1 CAPTURE), radar discovery and PHOTO SEND at both hidden locations are required before the final signal becomes available, including saves with a pre-existing exploration choice. The existing normal/continue-exploring choice and fresh physical arrival at the final destination are retained.

## Legacy data and installers

Removed mission POIs: zone01-east, zone02-l2, zone03-l3, zone04-l4 (527.04, 452.05713). Mission config, map arrays, scene survey arrays, marker/check arrays and task arrays use 2/2/3/4 entries. Exit, return, rock, final signal and art references are preserved. The cabin is not rebuilt; only affected marker objects and serialized mission bindings are updated. Three untracked Zone01 artwork markers (SurveyLocation, MapLocation2, MapLocation3) were also removed; the installer and scene test prevent their return.

Emma Blueprint, Bio Energy Core, Mechanical Module and Z4_ITEM_HIDDEN_01 no longer gate progression. Their existing content assets/catalog references remain to preserve old cargo and any other consumers. Unknown legacy inventory IDs retain their stacks with a fallback icon instead of being discarded.

Save version is 6. Versions 1–5 migrate every current snapshot, day-start and Journal checkpoint. Original progress is archived in legacyMissionProgressJson. Old Z1L3 -> new Z1L2, old Z2L3 -> new Z2L2, old Z3L4 -> new Z3L3. Old Z3L3 photo and old Z4L4 photo are removed from active credit because those POIs were deleted. Old Z4L5 discovery maps to new Z4L4 discovery, while its collection objective does not become a photo objective. Pending photos are remapped with their original POI identity; removed candidates cannot be sent for new objectives. Existing cargo is never re-granted during restore. Historical Pressure Hull capability is preserved even when an older save has no ship snapshot. Invalid legacy bitmasks remain rejected instead of being silently reset.

Already-earned legacy route entitlements are preserved separately from new capture completion. No new CAPTURE objective is fabricated. Existing completed endings are retained. A version-6 reload never applies the mapping twice.

Build Configs and Placeholders, Install Zone 1–4 Maps and the story installer now use the new layout. The map installer preserves POI position/radius/depth by stable ID. G10/Missions/Apply 11 Location Layout is the targeted asset/marker migration command used for this change.

## Validation

**Passed: 80 EditMode + 136 distinct PlayMode cases. Failed: 0 unresolved within the executed scope. Skipped: 0.** PlayMode totals use the latest result for each current test name; repeat runs are counted once. Unity compilation: zero errors.

| Run | Passed | Failed | Disposition |
| --- | ---: | ---: | --- |
| Initial real-device flow | 5 | 0 | Normal/Hidden Ending, devices, tutorial and SEND/capture order |
| Broad PlayMode regression | 98 | 3 | Three obsolete fixture expectations corrected and verified in later runs |
| Expanded PlayMode verification | 44 | 1 | Remaining death/capture fixture used deleted zone01-east; corrected and rerun |
| Final focused rerun | 11 | 0 | Vessel death, ordered rock actions and the complete tutorial |
| Final-source EditMode suite | 80 | 0 | Complete EditMode assembly, including migration/config/marker/material checks |

Machine-readable results with case names and XML evidence paths: [results.json](D:/G10/Docs/QA/MissionLocationsRefactor-2026-10-20/results.json).

Coverage includes all 11 real-camera photos and manual SEND, separate captures, both farm sources and one-time captures, material consumption, rejected save writes and rollback, reload/Rest/Journal/Restore Day, historical World Map checks, Normal/Hidden Ending, two radar discoveries and hidden PHOTO work, actual Mission Log hint binding, legacy pending-photo IDs, old Pressure Hull saves without ship snapshots, tutorial completion, regular upgrades, rock timing/terrain effects, photo deletion/backup and vessel death.

Installer rerun probe: 11 POIs, exactly 2 hidden locations, and all retained position/radius/depth records unchanged. Final Editor scene probe: zero missing scripts, 11 mission markers, zero untracked markers. Asset-reference audit: 10 affected asset/scene files, zero unresolved GUIDs, including package scripts. Twenty-one art/non-mission/terrain fields per map were compared with HEAD and preserved. Final diff whitespace check passed.

The real-device ship log retains 100/100 Hull HP throughout, with maximum depth 500 m in Zones 1–2 and 750 m after Pressure Hull. No changes to ProjectSettings, Packages or creature/item artwork were required. Dynamic font cache changes created by tests were discarded.

**Not Run:** PlayMode fixtures outside the listed runs, including the older ExpeditionLoopPlayModeTests fixture; standalone player build; target hardware; a full manual sailing playthrough. Existing user timeline files were not exercised: migration tests use isolated representative saves. No claim is made that the entire repository test suite is green.

## Manual limits

Automated flow tests position the vessel at validated interaction/arrival points while using real camera, SEND, capture minigame, upgrade, Mission Log hint binding, save and scene-flow services. They do not constitute a complete manual sailing playthrough. Standalone player build and target hardware checks are outside the executed checks. Designer review of Mission Log hint typography and material names remains useful.

## Changed files

- `Assets/_Project/Content/Maps/Zone01.asset`
- `Assets/_Project/Content/Maps/Zone02.asset`
- `Assets/_Project/Content/Maps/Zone03.asset`
- `Assets/_Project/Content/Maps/Zone04.asset`
- `Assets/_Project/Content/Missions/Zone01.asset`
- `Assets/_Project/Content/Missions/Zone02.asset`
- `Assets/_Project/Content/Missions/Zone03.asset`
- `Assets/_Project/Content/Missions/Zone04.asset`
- `Assets/_Project/Data/Tutorial/ZoneOneTutorialConfig.asset`
- `Assets/_Project/Scenes/Gameplay/Zone01.unity`
- `Assets/_Project/Scripts/Computer/ComputerDataContracts.cs`
- `Assets/_Project/Scripts/Computer/DebugExpeditionPreset.cs`
- `Assets/_Project/Scripts/Computer/ExpeditionLoop.cs`
- `Assets/_Project/Scripts/Computer/ExpeditionSave.cs`
- `Assets/_Project/Scripts/Computer/MissionLayoutMigration.cs`
- `Assets/_Project/Scripts/Computer/MissionLayoutMigration.cs.meta`
- `Assets/_Project/Scripts/Computer/PhotoCaptureService.cs`
- `Assets/_Project/Scripts/Computer/SubmarineUpgradeUIController.cs`
- `Assets/_Project/Scripts/Computer/UpgradeMaterialRequirementView.cs`
- `Assets/_Project/Scripts/Editor/MissionConfigEditor.cs`
- `Assets/_Project/Scripts/Editor/MissionLayoutEditor.cs`
- `Assets/_Project/Scripts/Editor/MissionLayoutEditor.cs.meta`
- `Assets/_Project/Scripts/Editor/NavigationArtworkEditor.cs`
- `Assets/_Project/Scripts/Editor/SubmarineUpgradeUIEditor.cs`
- `Assets/_Project/Scripts/Editor/ZoneMapEditor.cs`
- `Assets/_Project/Scripts/Editor/ZoneOneStoryEditor.cs`
- `Assets/_Project/Scripts/Missions/ZoneMissionConfig.cs`
- `Assets/_Project/Scripts/Missions/ZoneMissionRuntime.cs`
- `Assets/_Project/Scripts/Missions/ZoneOneStory.cs`
- `Assets/_Project/Scripts/Navigation/ZoneMapConfig.cs`
- `Assets/_Project/Scripts/Tutorial/TutorialConfig.cs`
- `Assets/_Project/Scripts/Tutorial/TutorialGuidanceView.cs`
- `Assets/_Project/Scripts/Tutorial/TutorialManager.cs`
- `Assets/_Project/Scripts/Tutorial/ZoneOneTutorialDirector.cs`
- `Assets/_Project/Scripts/UI/ExpeditionProgression.cs`
- `Assets/_Project/Scripts/UI/PhotoSurveyMap.cs`
- `Assets/_Project/Scripts/UI/SavedZoneMapProgress.cs`
- `Assets/_Project/Tests/EditMode/ExpeditionEconomyTests.cs`
- `Assets/_Project/Tests/EditMode/MissionLayoutRefactorTests.cs`
- `Assets/_Project/Tests/EditMode/MissionLayoutRefactorTests.cs.meta`
- `Assets/_Project/Tests/EditMode/MissionRefactorTestRunner.cs`
- `Assets/_Project/Tests/EditMode/MissionRefactorTestRunner.cs.meta`
- `Assets/_Project/Tests/PlayMode/DebugStartPlayModeTests.cs`
- `Assets/_Project/Tests/PlayMode/ExpeditionEconomyPlayModeTests.cs`
- `Assets/_Project/Tests/PlayMode/ExpeditionEndToEndPlayModeTests.cs`
- `Assets/_Project/Tests/PlayMode/ExpeditionLoopPlayModeTests.cs`
- `Assets/_Project/Tests/PlayMode/MissionDepthAuthoringPlayModeTests.cs`
- `Assets/_Project/Tests/PlayMode/PhotoSubmissionPlayModeTests.cs`
- `Assets/_Project/Tests/PlayMode/RockBreakerPlayModeTests.cs`
- `Assets/_Project/Tests/PlayMode/TutorialPlayModeTests.cs`
- `Assets/_Project/Tests/PlayMode/VesselDeathPlayModeTests.cs`
- `Assets/_Project/Tests/PlayMode/ZoneExitFlowPlayModeTests.cs`
- `Assets/_Project/Tests/PlayMode/ZoneExitMapPlayModeTests.cs`
- `Assets/_Project/Tests/PlayMode/ZoneMapPlayModeTests.cs`
- `Assets/_Project/Tests/PlayMode/ZoneOneStoryPlayModeTests.cs`
- `Docs/AI/MissionLocationsRefactor-2026-10-20.md`
- `Docs/AI/UnityProjectContext.md`
- `Docs/QA/MissionLocationsRefactor-2026-10-20/results.json`
