# Scope revision — Cargo, dialogue and watercolor UI

The requested scope removes the Research app and the separate backpack interface. Cargo is an app inside the existing computer, using `Art/UI/Desktop/Cargo_icon.png`, with a three-column scrolling item grid, selected-item preview/description, and 12 storage slots. It reads the same `CreatureInventory` and expedition save; existing unique items show their actual quantity of one. It does not create sample inventory items.

Zone 1 progresses directly from completed fieldwork: location 1 radar/photo → location 2 Creature 002 photo/adhesive → location 3 radar/Emma tube. Research clicks are removed. The existing Hull entry in UPGRADE installs the earned pressure hull once. Legacy progress bits and item IDs stay readable.

Dialogue is installed dormant on GameplayCore. Author future content as a DialogueSequence and call DialogueController.TryBegin. See Dialogue_Framework.md for the API. No authored dialogue or automatic trigger is added.

WatercolorUIEditor slices the supplied sheets into individual sprites and restyles the existing map, radar, helm and camera. Dynamic labels and controls remain live objects, using the current gameplay providers. Original PNG files are preserved.

Editor installation: `G10 > UI > Install Scope Revision`. This updates GameplayCore and Zone01 without rebuilding all scenes, validates missing scripts and required references, then restores the previously open scene setup. Save any in-progress scene edits before running it. Individual Cargo, Dialogue and Watercolor menu entries remain available.

Validation (Unity 6000.4.2f1, 2026-09-26):
- Runtime/editor/test assemblies compile. The targeted scene installer completes successfully, including a second idempotent run.
- 15/15 PlayMode tests pass: Cargo 3, Dialogue 4, ZoneOneStory 7, UI presentation 1.
- Tested real scene wiring, Cargo desktop/taskbar/minimize/close/scroll/selection/save/reload; story progression and actual Hull upgrade entry; dialogue controls/modal lifecycle; live Camera count/cooldown and shared HUD callbacks.
- Inspected rendered 1920x1080 screenshots of Cargo, Helm, map task cards, Camera, Radar and dialogue. A final cosmetic fix separates Camera badge icons from their stretchable backgrounds; the presentation test passed again after this fix and the updated Camera screenshot was inspected.
- Editor validation found no missing scripts in GameplayCore or Zone01.
- Validation ran in `Temp/ScopeValidation` because the already-open editor was unavailable to MCP/UI automation. Only the resulting two scenes, related import metadata, shared TMP font and Creature002 description were copied back; file hashes were verified. No standalone player build was run.
- Existing unrelated navigation changes were retained.

Feedback revision (2026-09-26):
- Removed the world-map region card and restored the original polygon highlight/click interaction.
- Restored the original `location.png` markers on the zone chart and helm mini-map.
- Moved the watercolor chart decoration behind the complete map surface so the margins are no longer black.
- Reduced sliced-button corner radii to about 10% of button height.
- Removed the helm Stop button and combined `TỐC ĐỘ | ENERGY` in the bottom status card.
- Removed the circular Back glyph from Camera's `CABIN / ESC` button.
- The focused UI presentation PlayMode test passed after these changes and rendered screenshots were inspected.

MAP / CAMERA clarity revision:
- The UI sheet imports were already correct: Sprite UI, 2048 max size for 1448px sheets, no mipmaps, no compression, NPOT scaling disabled, bilinear filtering. Blur came from stretching the 520x236 decorative `card` slice to nearly 1920x960 and reusing similarly ornate slices on small information panels.
- `WatercolorChartBackground` is now a crisp paper surface with a thin outline. `SurveyLegend` is the compact TMP hover-coordinate readout. The clipped duplicate `ChartCoordinate` object was removed.
- `CabinPointerTarget` still computes pointer UV from `SquareChartContent`. `PhotoSurveyMap` converts that UV through the existing `ZoneNavigation.UVToCoordinates` function and renders `X | Y | Z`; leaving the chart keeps the last value to avoid flicker. Mission marker/task data remains separate.
- The map hover task bubble is now a smaller secondary paper panel. Visible map labels created by the watercolor installer use TMP; retained legacy Text references are disabled and updated only for backward compatibility with older installers/tests.
- Camera now uses the large sliced watercolor art only for its primary viewport/header. `PhotoCountBadge` is a compact chip; `MetadataCard`, `CaptureFooter`, and `SavedToast` are distinct flat paper/readout/action surfaces with thin outlines.
- The camera instruction is a live TMP label, the shutter remains the primary circular CTA, and the four viewfinder brackets plus crosshair are aligned inside the actual preview rectangle.
