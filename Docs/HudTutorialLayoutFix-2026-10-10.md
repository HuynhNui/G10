# HUD / tutorial layout fix — 2026-10-10

Scope: remove CABIN / ESC from the top HUD and tidy the existing tutorial cards. No artwork, tutorial thresholds, access gates, mission, energy, save or input bindings were changed.

## Changes

- Removed eight top-bar Cabin buttons from the shared Zone01 cabin scene: Helm, Radar, Camera, world overview and all four zone maps. Other cabin/computer controls remain. WatercolorUIEditor no longer recreates these buttons.
- Tutorial cards reuse RadarInfo's existing Panel_Large sprite **and** its sliced-border density. The previous default density distorted corners on the much smaller hint card.
- Widened the compact card from 480 to 880 reference-canvas units in the newly free right-hand HUD space. Instructions render as one line at 24pt instead of two cramped lines. Its height/position still keep it above computer-window chrome.
- Checklist keeps its existing 340 × 245 reserved area. Added consistent padding, a separate completion counter, aligned label/value columns and three numbered step badges. Completed badges become connected green checks; progress bars and the ENERGY footer no longer collide with text or panel edges. Replaced midline alignment with ordinary middle alignment.
- While tutorial guidance occupies the world overview header, its centered heading is temporarily hidden to avoid overlap. Leaving the overview or hiding the tutorial restores the heading's original enabled state.

Runtime owner: `Assets/_Project/Scripts/Tutorial/TutorialGuidanceView.cs`. Defaults and authored dimensions: `TutorialConfig.cs` and `Assets/_Project/Data/Tutorial/ZoneOneTutorialConfig.asset`. Authoring owner: `Assets/_Project/Scripts/Editor/WatercolorUIEditor.cs`. Scene: `Assets/_Project/Scenes/Gameplay/Zone01.unity`.

## Verification

- Final scoped PlayMode run: **20/20 passed**, no failures or skipped tests, job `2775b91fe8b6`, 101.26 seconds. Fixtures: TutorialPlayModeTests, WorldMapPlayModeTests, ScopeUIPresentationPlayModeTests and CabinNavigationPlayModeTests.
- Includes the real full tutorial photo → SEND → capture → Pressure Hull flow, locked actions, partial progress/Continue, passive graphics, actual glyph padding/overflow, checklist row separation, overview heading restoration, and real synthetic Escape handling from Helm, Radar, overview and all four zone maps.
- Unity compilation: zero entries, not compiling. Final Console error check: zero entries. Scene inspection: zero remaining top-bar Cabin buttons and zero missing scripts. Editor restored to its original clean Bootstrap scene in EditMode.
- `git diff --check` passed. Existing unrelated working-tree changes were preserved; no commit/push was requested or made.

Inspected Unity-rendered images under `D:/G10/Temp/TestCaptures`: tutorial-helm-{1920,1366}.png, tutorial-helm-progress-1920.png, tutorial-radar-hint-{1920,1366}.png and tutorial-world-hint-1920.png.

An initial run caught insufficient vertical padding in the two-line compact hint. The final one-line layout passed without weakening the padding checks. These are Unity Editor tests/offscreen renders, not a human playtest or a standalone Windows build; no full-project test suite was rerun for this localized UI change.
