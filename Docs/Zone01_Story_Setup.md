# Zone 1 — missions, ship tuning and UI

## Scene authoring

Open `Assets/_Project/Scenes/Gameplay/Zone01.unity`. Select **Cabin Station → Zone Navigation** in the Inspector. It exposes initial position/heading, maximum speed, acceleration, turning, dive/ascent speed, maximum depth, energy, hull, collision damage and device charges.

Saved voyages normally keep their ship values and upgrades. For deliberate designer testing, enable **Use Scene Ship Settings On Load**, or use **Áp dụng chỉ số scene & hồi đầy (Play Mode)**. The latter changes the current session; change the authored values outside Play Mode to retain them. Keep the override OFF for normal progression.

`ZoneOneStory` on the cabin contains the three POI IDs ordered by their existing X coordinates: `zone01-left` (275,75), `zone01-north` (625,475), `zone01-east` (725,175). It does not relocate those three landmarks. New voyages start near the leftmost landmark at (275,61), heading north, 230 m deep. Existing saves keep their position. Both the new starting site and the old dock remain rest locations.

## Actual gameplay

1. **Rạn Tảo Đỏ:** scan a detectable contact with RADAR; point the ship toward the subject and take a valid FPP CAMERA photo. Open the cabin computer → **RESEARCH** → **PHÂN TÍCH / NẠP BẢN VẼ**. Tissue analysis/Lily's discovery unlocks the second site's coordinates.
2. **Rãnh San Hô Cổ:** scan the metal contact, approach within the existing arrival radius and depth tolerance, then **THU THẬP** using the existing capture station. The arm takes Emma's broken tube. Return to **RESEARCH** and load the protected sketch signed E.A. This unlocks the recipe and third site. There is no Emma voice recording.
3. **Thềm Biển Sâu:** take a valid photo of Creature 002, then use **THU THẬP** for its adhesive secretion. The creature stays present. At **RESEARCH**, choose **CHẾ TẠO & LẮP VỎ TẦNG 1**. The sample is consumed once; the blueprint and analysis are retained. Default reward: +25 hull capacity, maximum depth at least 750 m, and Zone02 unlock. Both reward values are scene-configurable on `ZoneOneStory`.
4. **MISSION LOG → NEXT ZONE** enters Zone02 / Cổ Thụ Linh Hồn through the existing scene-flow system. Run from Bootstrap for inter-zone scene transitions.

Missions advance only from successful in-range radar/photo/collection actions. Bag-full and out-of-charge attempts do not award objectives or consume extra charges. Nine milestone flags and ship upgrades persist in expedition saves and day checkpoints. Old saves without these flags start the new story at site 1 without deleting cargo, photos, ship upgrades or position.

## Content folders / replacement artwork

`Assets/_Project/Content/Zone01/` contains:

- `Creatures/001/`: definition, preview and Creature01 prefab.
- `Creatures/002/`: definition, temporary preview and Creature02 prefab.
- `Items/001_EmmaTube/`: definition, temporary preview and EmmaTube01 prefab.
- `Items/002_Adhesive/`: definition, temporary preview and Adhesive02 prefab.

Each `Definition.asset` has a stable ID, display name, description, Sprite, fallback Texture2D, prefab and **Placeholder Art** flag. Creature002, Emma's tube and the adhesive currently reuse clearly marked placeholder icons, as requested; they are not final creature/item illustrations.

To replace art, assign a standalone **Sprite (Single)** to **Sprite**, then turn off **Placeholder Art**. Do not change the stable ID in existing saves. Packed atlases/multi-sprite sheets are not supported by this still-photo compositor; use a standalone sprite. Non-readable textures are copied once for CPU photo compositing and cached. The same definition drives the prefab's SpriteRenderer, photo subject and restored item icon. Descriptions appear when selecting a collected inventory item.

The 10-slot bag is a 5×2 grid. Energy is anchored at the top-center of the helm, above the controls. Radar echoes reveal clockwise at the same bearing as the needle. Window movement no longer reapplies content layout each pointer update and has no initial drag threshold; resize still uses the existing bounds.

## Editor installer / validation

`G10 → Zone 1 → Install Story and Polish` updates the existing scene in place. It preserves existing content definition edits on reruns. Load GameplayCore additively when updating its cross-zone content catalog/rest areas. The batch entry is `G10.Prototype.Editor.ZoneOneStoryEditor.InstallBatch`.

PlayMode fixture: `ZoneOneStoryPlayModeTests`. Uses isolated temporary save/photo paths. Covers the real scan/photo/grab/research/craft path, reloads, single-use reward, zone unlock, inventory limit, checkpoint rollback, radar angle timing, UI screenshots/text overflow and pointer-driven window movement.

Validation on 2026-09-23: all 5 fixture tests passed in a separate Unity 6000.4.2f1 project copy; generated scene/prefab files were then copied into the main project and hash-compared. This is not a claim that the entire older prototype test suite was run. Runtime screenshots are in `Logs/ZoneOneValidation`; test results in `Logs/zone-one-tests-final.xml`. Original scene/prefab backups are in `Logs/ZoneOneBeforeInstall`. The open Editor is not controlled by this workflow; reload the authored scene after leaving Play Mode if it is still showing the previous in-memory version. Zone02's existing content is unchanged; this feature implements its unlock, not the rest of Zone02.

### Map mission visibility correction

The map originally hid the mission panel unless the cursor was within the gameplay arrival circle; clicking a location did not keep its tasks visible. Reproduced by opening World Map → Zone01: the new regression test failed because the mission panel was inactive (`Logs/map-missions-before.xml`). Scene story references were valid; mission text itself was not missing.

`PhotoSurveyMap` now initially selects the current story site, retains the clicked site's tasks after mouse exit/reopening, uses the visible marker's 50×50 footprint for UI hit detection, and refreshes checkboxes from the full saved flag mask (including same-count checkpoint changes). Gameplay arrival radius and mission gates are unchanged. Locked markers remain visible, with the lock state written in their mission heading. The three site captures also confirm that the authored panel fits the full new task text, so no scene or panel resize was necessary.

Verified: 6/6 `ZoneOneStoryPlayModeTests` pass after the map fix (`Logs/map-missions-final.xml`), including real UI raycasts at each marker, persistent selection and reopen, all three exact site headings, text fit and live checkpoint updates. Screenshots are in `Logs/MapMissionValidation`. No save reset or scene regeneration is needed; restart Play Mode after Unity recompiles the changed script.
