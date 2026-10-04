# Navigation, vessel death and direct cabin interactions

Based on local HEAD `60a07a0a4165f00480f464f30f34a26d0f86c7f0` (2026-10-03). Existing local artwork and untracked assets are retained. No mission, map, deadline, upgrade balance or cabin hierarchy redesign.

## Behavior

- Navigation: turning joins the existing maximum-of-axis movement budget. Reverse maximum is speed / 3; reverse acceleration is forward acceleration / 6. With authored values, forward reaches 18 in 1 second, reverse reaches -6 in 2 seconds. Changing forward to reverse first brakes with normal acceleration and uses the remainder of that tick for reverse acceleration. Collision damage remains absolute actual impact speed.
- Save v3: one independent `dayStart` snapshot, with `hasDayStart` because Unity JSON can deserialize null inline serializable classes as empty objects. Captured after fresh day-1 initialization and after Rest/recovery has advanced the day, refilled the ship and prepared daily spawns. Ordinary saves, transitions and Continue do not update it.
- Hull death: blocks navigation, cancels delayed cabin work and the existing minigame, shows a full-screen presentation for 1 realtime second through the existing scene fade, deep-copies dayStart into current, truncates future history through the existing atomic save store and rebinds the saved logical zone. Does not increase day. Energy exhaustion retains recovery behavior; deadlines retain their existing failure behavior.
- Direct capture: `OpenCapture()` closes the current panel, swaps the existing CabinArt texture, waits 1 realtime second, then calls the existing catcher once. The art remains behind the minigame until a terminal result. Invalid attempts restore the normal art without opening a panel or consuming a charge.
- Direct photo: `OpenCamera()` swaps the existing CabinArt texture for a rendered frame, calls the existing photo service once, plays the shutter only for a created record, then restores cabin art. Photos still enter the archive, Photo Lab and mission pipeline. Cooldown/charges remain owned by PhotoCaptureService.
- No old camera/capture panel is removed. UnityEvent method signatures are unchanged. CabinStationView no longer additively loads GameplayCore in Awake; SceneFlow remains the loading owner.

## Compatibility limits

v1/v2 saves remain readable and retain their actual ship/progression state. They never stored a true beginning-of-day snapshot; the implementation does not fabricate one from their current state or Journal. Rest creates the first valid checkpoint. A Journal rollback to an older day invalidates a dayStart from the abandoned future for the same reason; Rest establishes the next one. New Game starts with a valid checkpoint immediately.

If hull death happens before a valid day-start exists, the death presentation explains this and returns to Main Menu without saving the dead state, advancing the day or overwriting the previous save. Continue and Rest establishes a proper checkpoint. Exact historical rollback is impossible for data the older format never stored.

Tests use unique temporary save/photo directories. The user's real v2 save was read only: Zone01/day 1 with authored base ship stats. It was not reset or migrated on disk by the tests.

## Measured ship values

The real-device automated four-zone path uses cabin photos, capture minigames and the existing progression card. Ship positions are set by the test (not a manual sailing playthrough), so travel energy remains 100. No optional generic upgrades or Rest were used. Values below were read from runtime immediately before and after transitions and checked against the written save.

| Stage | Hull/cap | Speed | Dive | Ascent | Max depth | Energy/cap | Radar/cap | Photos/cap | Captures/cap |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Zone01 start | 100/100 | 18 | 5 | 5 | 500 | 100/100 | 10/10 | 20/20 | 5/5 |
| Before Zone02 | 100/125 | 18 | 5 | 5 | 750 | 100/100 | 10/10 | 18/20 | 4/5 |
| Entered Zone02 | 100/125 | 18 | 5 | 5 | 750 | 100/100 | 10/10 | 18/20 | 4/5 |
| Before Zone03 | 100/125 | 18 | 5 | 5 | 750 | 100/100 | 10/10 | 16/20 | 2/5 |
| Entered Zone03 | 100/125 | 18 | 5 | 5 | 750 | 100/100 | 10/10 | 16/20 | 2/5 |
| Before Zone04 | 100/125 | 18 | 5 | 5 | 750 | 100/100 | 10/10 | 13/20 | 0/5 |
| Entered Zone04 | 100/125 | 18 | 5 | 5 | 750 | 100/100 | 10/10 | 13/20 | 0/5 |

Sources of numeric changes:

- `ZoneNavigation.CreateInitialShipState` uses the unchanged Zone01 scene values: speed/acceleration 18, turn 40 degrees/sec, dive/ascent 5, depth 500, hull/energy capacity 100, radar/photos/captures 10/20/5.
- `ZoneOneStory.InstallHull`: +25 hull capacity, maxDepth = max(old,750), no current-hull refill. This is why current hull remains 100/125.
- `PhotoCaptureService.Capture` calls `ShipResources.TryUse(Photo)` once per accepted photo: 2, 2 and 3 photographs in Zones1–3.
- `CreatureCatcher.TryCapture` calls `ShipResources.TryUse(Capture)` only after minigame Begin succeeds: 1, 2 and 2 attempts in Zones1–3.
- `ExpeditionLoop.PrepareZone` carries state; `ApplySnapshot` restores it. Transitions themselves do not buff or refill anything.
- Bio Lamp and Rock Breaker go through `ZoneOneStory.ApplyProgressionAction` and record objectives/world flags only. No unexpected Zone3/4 stat inflation was observed.
- Optional `ShipUpgradeAction.TryApply` is repeatable and calls `ShipResources.ApplyUpgrade`; it can intentionally raise stats if invoked by the player. It was not invoked in this measurement. Existing saved values can also override the authored baseline when `useSceneShipSettingsOnLoad` is off (the unchanged normal setting).

Raw generated measurement: `Temp/expedition-device-transition-stats.tsv`.

## Changed files

Runtime:

- `Scripts/Navigation/ZoneNavigation.cs`: energy budget and reverse integration.
- `Scripts/Computer/ExpeditionSave.cs`: v3 snapshot persistence/validation, v1/v2 reading.
- `Scripts/Computer/ExpeditionLoop.cs`: checkpoint lifecycle, hull death, energy-only recovery.
- `Scripts/Core/SceneFlowController.cs`: death presentation and existing-fade rebind.
- `Scripts/UI/CabinStationView.cs`: direct photo/capture entry, texture restoration/cancellation; removes duplicate core loader.
- `Scripts/UI/CabinZoneSession.cs`: cancels pending cabin work on rebind.
- `Scripts/UI/PauseMenuController.cs`: does not open pause over a pending direct interaction.
- `Scenes/Gameplay/Zone01.unity`: only three new references: existing CabinArt RawImage, `Art/Environment/Cabin_Capture.png`, `Art/Environment/Canbin_Photo.png` (spelling preserved).

Tests under `Tests/PlayMode`:

- New `NavigationBudgetPlayModeTests`, `VesselDeathPlayModeTests`, `DirectCabinInteractionPlayModeTests` (+ Unity-generated metadata).
- Updated `CaptureMinigamePlayModeTests`, `CabinNavigationPlayModeTests`, `ExpeditionEndToEndPlayModeTests` for the direct entry contract.
- Updated `TerrainCollisionPlayModeTests` to explicitly configure its map and test the new reverse speed while preserving impact semantics.
- Updated `ExpeditionLoopPlayModeTests` to start its shorter-deadline fixture explicitly now that day 1 is persisted immediately.

`CreatureCatcher`, `CaptureMinigameController`, `PhotoCaptureService`, `PhotoCameraView`, `ShipResources` gameplay implementations were not changed.

## Automated validation

Final rerun after the deadline-UI/rollback fix and Editor restart, 2026-10-04: **64 passed, 0 failed, 0 skipped** (131.98 seconds), job `0cedf294ce28`. Includes the assertion that hull rollback does not open deadline-failure UI. The generated transition-stat table matches the measurements above.

Combined PlayMode run on 2026-10-03: **64 passed, 0 failed, 0 skipped** (132.44 seconds). Fixtures: NavigationBudget, TerrainCollision, VesselDeath, DirectCabinInteraction, CaptureMinigame, ExpeditionEndToEnd, ExpeditionLoop, ExpeditionNewGame, ExpeditionSaveReset, ProjectSceneFlow and ZoneOneStory. Phase A ran first (7/7); Phase B ran before cabin changes (6/6 at that stage). Direct cabin/capture/end-to-end integration passed 14/14 before the combined regression run.

Unity reports zero compilation errors. Both normal and hidden end-to-end routes are included. This is automated PlayMode verification, not a target-platform build or a manual complete sailing playthrough. The pre-existing minigame screenshot was inspected; its fullscreen artwork remains unchanged.

Unity's scene whitespace-only reserialization was normalized; the actual scene delta is exactly the three intended references. Existing EditorSettings and untracked user artwork were preserved. When work resumed on 2026-10-04 after an Editor restart, the two font assets were already dirty again; those intervening changes were retained rather than assumed to be disposable test caches.

## Manual checks

1. Bootstrap → New Game. At rest, hold turn for 1 second: energy drops 1; move + turn for 1 second also drops 1. Forward max 18 in 1 second; reverse max 6 in 2 seconds.
2. Make progress/take a photo during day 1, then lose all hull. Confirm the death message/fade and full return to the initial day-1 state, without a new Journal day. Repeat after crossing zones: return to the zone in which the day began.
3. Rest, then change position/cargo/photos and lose hull. Confirm the restored state is the new day's refilled starting state. Energy exhaustion alone must still offer recovery, not death.
4. Capture hotspot: capture cabin art appears immediately; minigame only after 1 second. Repeated clicks do not spend additional charges. Cancel/fail/succeed each returns to normal cabin without the legacy capture panel.
5. Camera hotspot: photo cabin art briefly appears; no camera panel/toast opens. Desktop → Photo Lab shows the new record. A properly aimed valid photograph still checks off its mission.
6. Continue the existing four-zone normal/hidden routes. No new scene or extra EventSystem should appear.
