# Playable expedition flow

## Ownership and setup

The existing `GameplayCore` and `Zone01` cabin scene remain loaded. `Zone01` contains all four authored map panels. Zone02–04 placeholder scenes are not loaded during expedition travel.

- `SceneFlowController`: New Game, Continue, fade, shared cabin loading, ending scene.
- `ExpeditionLoop` / `ExpeditionSaveStore`: existing JSON timeline, inventory, photos, ship, journal, per-zone mission and route state. Save version 2 reads version 1.
- `CabinZoneSession`: binds the active map, survey, mission config, terrain and content catalog to the same cabin.
- `ExpeditionProgression`: observes required mission completion, unlocks exits, checks physical arrival, offers the ending choice. Missions do not change scenes.
- `ZoneMissionRuntime` / `ZoneOneStory`: existing stable-ID objectives and existing Upgrade app actions.
- `DialogueController` / `DialogueView`: existing modal extended with two explicit choices.
- `EndingPresentation`: text on the existing Ending canvas, retaining its Main Menu button.

No manual scene wiring is required for the checked-in configuration. Runtime adapters bind from existing scene references. GameplayCore's existing content catalog now includes all 15 active definitions. No images or textures are replaced.

## Map configuration

Edit `Assets/_Project/Content/Maps/Zone01.asset` through `Zone04.asset` in the Inspector. The Expedition Route section contains entry position/heading/depth, exit POI/radius, destination zone, rock interaction area, hidden location IDs and final point. A destination uses the destination map's entry configuration as its spawn.

| Zone | Entry | Exit / destination |
|---|---|---|
| Zone01 | (960, 154), depth 230 | (1875, 680), radius 35 → Zone02 |
| Zone02 | (45, 595), depth 230 | (1740, 460), radius 35 → Zone03 |
| Zone03 | (50, 630), depth 230 | (1750, 25), radius 20 → Zone04 |
| Zone04 | (290, 950), depth 230 | Ending decision after main fieldwork |

Zone03 rock approach: (1480, 150), radius 65. Its exit is blocked by the original terrain and passable with the existing destroyed-rock terrain. All existing mission POIs are retained.

Zone04 hidden IDs: `Z4_L3`, `Z4_L4`, `Z4_L5`. Final point: `Z4_FINAL_SIGNAL`, (1500, 150), radius 30. The final point is not a new mission or creature. Discovery of all three existing locations unlocks it; arriving there is a separate action.

## Save/reset behavior

New Game explicitly writes a blank timeline and replaces its recovery backup. It then reconstructs Zone01/day 1 with authored base ship resources and configured spawn. Old mission states, photos, cargo, upgrades, journal, recipes, discoveries, hidden flags and ending choice are not imported. Loose legacy photo files are left untouched but are not gameplay authority.

Continue reads the existing timeline; boot does not erase it. Ending saves reopen the correct ending. Audio/display PlayerPrefs are unchanged. Returning to Main Menu, resting and transitions use the same save owner. A safe explicit developer entry is the ExpeditionLoop component's `Reset Save Data (Play Mode)` context menu.

## Manual acceptance walkthrough

1. Start Play from **Bootstrap**. Select **NEW GAME**. Confirm Zone01, day 1, empty inventory/photos/checklist and spawn (960,154). NEW GAME replaces the current gameplay save; CONTINUE does not.
2. Zone01: photograph L1/L3, recover the Emma blueprint at L2 with the existing capture minigame. In Computer → Upgrade, select **Hull / Expedition Upgrade** and install Pressure Hull. Verify the zone does not change at completion. Sail to (1875,680) for the fade to Zone02.
3. Zone02: photograph L1; recover L2 energy core; photograph then capture L3. Install Bio Lamp on the same progression card. Sail to (1740,460); verify Zone03 entry.
4. Zone03: photograph/capture L1, recover L2 module, photograph L3. Craft then install Rock Breaker on the progression card. Sail to (1480,150), reopen Upgrade and choose BREAK ROCK BARRIER. Verify the map/terrain changes without teleporting. Complete remaining required fieldwork (L4), then sail to (1750,25) for Zone04.
5. Save/reload after breaking the rock (Escape → Main Menu → CONTINUE). Verify the destroyed map, missions, cargo, ship and position persist.
6. Zone04: photograph the two visible main locations. Select **KẾT THÚC THÁM HIỂM** for Normal Ending and return to Main Menu. CONTINUE should return to that ending.
7. On a separate playthrough/checkpoint, choose **TIẾP TỤC KHÁM PHÁ**. Stay in Zone04. Hidden contacts were unavailable before this decision. Use radar near L3 (1007,451), L4 (527,452), L5 (359,120), all at survey depth 230. Creature contacts vary within their authored cell each day.
8. After three discoveries, verify the gold final marker and Mission Log instruction appear, with no immediate ending. Sail to (1500,150) to reach Hidden Ending.
9. Save/reload after choosing exploration, after one discovery and after all three discoveries. Verify the choice, discoveries and final destination remain unlocked. Rest if resources run low; completion of the main survey removes the incomplete-objective deadline failure condition.

## Validation scope

Automated tests use unique temporary save/photo paths, not the user's timeline. The end-to-end flow tests inject mission events and position the test ship to check state/arrival boundaries. A separate device-service path uses real camera composition, capture/minigame logic, upgrade actions and radar discovery; test navigation still positions the ship programmatically. This is not a claim that a human-input playthrough or target-platform build has been completed.

On 2026-10-03 the focused PlayMode run passed **35/35** tests across ExpeditionEndToEnd, ExpeditionLoop, ProjectSceneFlow, ZoneOneStory, ExpeditionNewGame, ExpeditionSaveReset, Dialogue and EndingPresentation. The 1920×1080 ending-choice capture was visually inspected; both Vietnamese choices fit the existing dialogue artwork. The all-zone device path also caught and fixed rechecking the final capture charge after an already-paid successful minigame.

The default scene keeps `useSceneShipSettingsOnLoad` OFF so saved upgrades/resources persist. The existing explicit designer-preview override remains supported; keep it OFF for normal playthroughs.

### Main changed files

- `Scripts/Core/SceneFlowController.cs`: New Game/Continue, fade, physical exit checks, shared-cabin and ending flow.
- `Scripts/Computer/ExpeditionSave.cs`, `ExpeditionLoop.cs`: reset, backward-compatible route/ending persistence, logical zone binding and journal reconstruction.
- `Scripts/UI/CabinZoneSession.cs`, `ExpeditionProgression.cs`, `EndingPresentation.cs`: new focused adapters described above.
- `Scripts/Navigation/ZoneMapConfig.cs`, four `Content/Maps/Zone*.asset`: configurable route coordinates; existing `Scenes/Gameplay/GameplayCore.unity`: full content catalog and safe entry rescue points.
- `Scripts/Missions/ZoneMissionRuntime.cs`, `ZoneOneStory.cs`: main vs hidden progress and existing gate actions.
- `Scripts/Computer/StoryHullUpgradeAction.cs`, `SubmarineUpgradeUIController.cs`, `ExpeditionComputerView.cs`, `ZoneMissionProvider.cs`, `PhotoCameraView.cs`: current-zone action/readout binding and route instructions.
- `Scripts/Dialogue/DialogueController.cs`, `DialogueView.cs`: explicit modal choices.
- `Scripts/UI/SceneNavigationButton.cs`, `CabinStationView.cs`, `WorldMapController.cs`, `PhotoSurveyMap.cs`, `RadarDisplay.cs`: continue button, active-map binding, route markers and transient reset.
- `Scripts/Navigation/PhotoSurveyZone.cs`, `ZoneNavigation.cs`, `CreatureCatcher.cs`: reset contacts, transition input lock and final-charge completion fix.
- Focused new and updated tests under `Tests/PlayMode`; this document. Earlier grid/art/font working-tree changes are not reverted.
