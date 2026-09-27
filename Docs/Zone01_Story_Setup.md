# Zone 1 — missions, ship tuning and UI

## Scene authoring

Open `Assets/_Project/Scenes/Gameplay/Zone01.unity`. Select **Cabin Station → Zone Navigation** in the Inspector. It exposes initial position/heading, maximum speed, acceleration, turning, dive/ascent speed, maximum depth, energy, hull, collision damage and device charges.

Saved voyages normally keep their ship values and upgrades. For deliberate designer testing, enable **Use Scene Ship Settings On Load**, or use **Áp dụng chỉ số scene & hồi đầy (Play Mode)**. The latter changes the current session; change the authored values outside Play Mode to retain them. Keep the override OFF for normal progression.

`ZoneOneStory` on the cabin contains the three POI IDs ordered by their existing X coordinates: `zone01-left` (275,75), `zone01-north` (625,475), `zone01-east` (725,175). It does not relocate those three landmarks. New voyages start near the leftmost landmark at (275,61), heading north, 230 m deep. Existing saves keep their position. Both the new starting site and the old dock remain rest locations.

## Actual gameplay

1. **Rạn Tảo Đỏ:** scan a detectable contact with RADAR; point the ship toward the subject and take a valid FPP CAMERA photo. The successful photograph immediately opens the second site's coordinates.
2. **Rãnh San Hô Cổ:** take a valid photo of Creature 002, then use **THU THẬP** for its adhesive secretion. Collecting the sample opens the third site. Only the secretion enters Cargo; the creature is not captured.
3. **Thềm Biển Sâu:** scan the metal contact, approach within the existing arrival radius and depth tolerance, then **THU THẬP** using the capture station. The arm takes Emma's broken tube and the protected sketch signed E.A. The blueprint becomes available immediately. Open **UPGRADE** and install the Tier 1 pressure hull. The adhesive is consumed once; Emma's tube is retained. Default reward: +25 hull capacity, maximum depth at least 750 m, and Zone02 unlock. Both reward values are scene-configurable on `ZoneOneStory`.
4. **MISSION LOG → NEXT ZONE** enters Zone02 / Cổ Thụ Linh Hồn through the existing scene-flow system. Run from Bootstrap for inter-zone scene transitions.

Missions advance only from successful in-range radar/photo/collection actions. Cargo-full and out-of-charge attempts do not award objectives or consume extra charges. Seven gameplay milestones and ship upgrades persist in expedition saves and day checkpoints. The former Research app and its manual analysis/blueprint gates are removed. The original numeric `Analysis` and `Recipe` save bits remain compatible and are inferred from completed photography/collection when restoring old checkpoints; they no longer count as objectives. Previously collected tubes remain credited, while the moved Creature002 objective still needs completion. Old saves without story flags start at site 1 without deleting cargo, photos, ship upgrades or position.

## Content folders / replacement artwork

`Assets/_Project/Content/Zone01/` contains:

- `Creatures/001/`: definition, preview and Creature01 prefab.
- `Creatures/002/`: definition, temporary preview and Creature02 prefab.
- `Items/001_EmmaTube/`: definition, temporary preview and EmmaTube01 prefab.
- `Items/002_Adhesive/`: definition, temporary preview and Adhesive02 prefab.

Each `Definition.asset` has a stable ID, display name, description, Sprite, fallback Texture2D, prefab and **Placeholder Art** flag. Creature002, Emma's tube and the adhesive currently reuse clearly marked placeholder icons, as requested; they are not final creature/item illustrations.

To replace art, assign a standalone **Sprite (Single)** to **Sprite**, then turn off **Placeholder Art**. Do not change the stable ID in existing saves. Packed atlases/multi-sprite sheets are not supported by this still-photo compositor; use a standalone sprite. Non-readable textures are copied once for CPU photo compositing and cached. The same definition drives the prefab's SpriteRenderer, photo subject and restored item icon. Descriptions appear when selecting a collected inventory item.

Cargo is a computer app backed by the existing inventory and expedition save data, with 12 storage slots and an item detail panel. Energy is anchored at the top-center of the helm, above the controls. Radar echoes reveal clockwise at the same bearing as the needle. Window movement no longer reapplies content layout each pointer update and has no initial drag threshold; resize still uses the existing bounds.

## Editor installer / validation

`G10 → Zone 1 → Install Story and Polish` updates the existing scene in place. It preserves existing content definition edits on reruns. Load GameplayCore additively when updating its cross-zone content catalog/rest areas. The batch entry is `G10.Prototype.Editor.ZoneOneStoryEditor.InstallBatch`.

PlayMode fixture: `ZoneOneStoryPlayModeTests`. Uses isolated temporary save/photo paths. Covers the scan/photo001 → photo002/adhesive → radar/Emma tube → upgrade path, legacy checkpoints, reloads, single-use reward, zone unlock, Cargo limit, checkpoint rollback, radar angle timing, UI screenshots/text overflow and pointer-driven window movement.

Validation on 2026-09-23: all 5 fixture tests passed in a separate Unity 6000.4.2f1 project copy; generated scene/prefab files were then copied into the main project and hash-compared. This is not a claim that the entire older prototype test suite was run. Runtime screenshots are in `Logs/ZoneOneValidation`; test results in `Logs/zone-one-tests-final.xml`. Original scene/prefab backups are in `Logs/ZoneOneBeforeInstall`. The open Editor is not controlled by this workflow; reload the authored scene after leaving Play Mode if it is still showing the previous in-memory version. Zone02's existing content is unchanged; this feature implements its unlock, not the rest of Zone02.

### Map tasks vs gameplay guidance (2026-09-24 clarification)

Map tooltips show only these local objectives, in left-to-right site order:

- Rạn Tảo Đỏ: Bật Radar; Chụp Sinh vật 001.
- Rãnh San Hô Cổ: Chụp Sinh vật 002.
- Thềm Biển Sâu: Bật Radar; Dùng nút THU THẬP để lấy vật phẩm.

`ZoneOneStory.MapLocationText` is separate from full mission guidance. Adhesive collection, Emma's blueprint, upgrades, rewards and unlock requirements appear in MISSION LOG; hull installation is in UPGRADE. The September 25 scope change swaps the task/subject content of sites 2 and 3 while preserving their names, POI IDs, positions and arrival radii.

`PhotoSurveyMap` shows tasks only while the pointer hovers over a location. Leaving the marker, entering empty chart space, or closing/reopening the map hides the tooltip; clicking does not pin it. The selected location can still be remembered internally. UI hit detection uses the visible marker's 50×50 footprint without changing gameplay arrival radius. Checkboxes refresh from the full saved flag mask, including checkpoint changes with the same number of completed steps.

The PlayMode regression covers actual UI raycasts, exact 2/1/2 map task lists, hover-only visibility, close/reopen, text fit and live checkpoint updates. The mission swap does not require a save reset or POI relocation. UI authoring changes require the current scope installer.

Verified on 2026-09-24: 6/6 `ZoneOneStoryPlayModeTests` pass in the isolated Unity 6000.4.2f1 project copy (`Logs/map-hover-only.xml`). Inspected all three hover screenshots plus the initially hidden state in `Logs/MapHoverValidation`. This supersedes the earlier persistent-selection behavior and its validation artifacts; the full legacy test suite was not run.
