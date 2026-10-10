# G10 — Playtest feedback fixes (2026-10-09)

Working tree based on `ea4d5aa`. No source artwork, mission objectives, rewards, energy balance or global save schema changed. Existing unrelated graphics, font and project-settings edits were preserved. Changes remain local; no GitHub push or release build was performed.

## 1. Tutorial and energy feedback

Before: repeated modal instructions; Helm checked displacement/final heading/depth from one starting state; Upgrade could wait without explanation.

After: one shortened Intro, then passive contextual hints and highlighted controls. Helm displays three actual-motion progress bars/check marks. Movement uses changes in `DistanceTravelled`; turns and depth use actual applied changes. Returning to an earlier position/heading/depth does not undo progress. Restore, blocked motion and empty-energy input do not grant progress. Additive save fields preserve partial progress across disable/rebind/panels/Continue; saves are debounced rather than written every frame.

Locked controls show a badge and a short reason. Upgrade explains missing submitted mission photos/recipe. SEND completion is learned synchronously, so immediately deleting the submitted image cannot strand Photo Lab. Once completed, tutorial chrome disappears; the existing expedition-upgrade description directs the player to the configured exit. Helm displays remaining energy and measured drain, without introducing a depth multiplier.

Files: `Assets/_Project/Scripts/Tutorial/{TutorialConfig,TutorialProgressState,TutorialManager,ZoneOneTutorialDirector,TutorialGuidanceView}.cs`, `Scripts/Navigation/ZoneNavigation.cs`, `Scripts/UI/{CabinStationView,ShipEnergyBar}.cs`, `Scripts/Missions/ZoneOneStory.cs`, `Data/Tutorial/ZoneOneTutorialConfig.asset` (all under `Assets/_Project`).

Tests: updated `TutorialPlayModeTests` retains the real full photo/SEND/capture/hull-upgrade flow; adds return-trip motion, invalid controls, partial Continue, passive layout and SEND→DELETE→Continue regressions. New `TutorialHelmProgressRulesTests` covers old JSON, normalization and round-trip fields.

## 2. Photo Lab deletion

Before: Previous / SEND / Next only; storage full of unwanted images.

After: distinct DELETE button with CANCEL/DELETE confirmation and an unsent-mission warning. The operation targets a stable ID, selects the next/previous remaining photo and disables empty-list controls. Archive files are staged before save commit; failed archive/save publication restores the existing photo. Successful deletion prunes current, day-start, journal and recovery-save photo copies, removes matching archive files and releases textures. It does not refund charges, roll back mission rewards or reset historical capture counts. Storage remains bounded at 24 readable photos. Backup publication now rolls back if primary save publication fails.

Files: `Assets/_Project/Scripts/Computer/{ComputerDataContracts,ComputerDesktopSkin,PhotoLabView,PhotoCaptureService,ExpeditionLoop,ExpeditionSave}.cs`.

New `PhotoDeletionPlayModeTests`: 11 tests, including ten real captures→seven deletions→Continue with three photos, primary/backup and archive locks, selected-ID changes, empty state, rewards/checkpoints, interrupted staging, unsafe IDs and 24-photo cap.

## 3. Zone exit

Before: Zone02 exit `(1740,460)`, radius35, close to L3; a fixed-size diamond did not communicate the arrival region. Loading/unlocking inside could leave automatic progression unarmed.

After: Zone02 exit `(1830,75)`, radius75. Unity's real baked terrain and 2-unit vessel footprint validated connectivity from unchanged L3 `(1715.52,486.46)` (70,351 lattice nodes visited). No other zone exit was moved. The diamond remains; its surrounding approach outline derives from the same map position/radius as `Contains`. Ready/locked/approach prompts reuse the map task card and Helm status. Load/unlock inside requires fresh physical horizontal travel, not a restore/turn/depth change; one accepted transition is latched. Rest and same-zone checkpoint restoration reset arrival observation.

Files: `Assets/_Project/Scripts/UI/{ExpeditionProgression,PhotoSurveyMap}.cs`, `Scripts/Editor/ZoneExitSetupEditor.cs`, `Content/Maps/Zone02.asset`. Shared scene `Scenes/Gameplay/Zone01.unity` contains the new serialized approach-style defaults.

New `ZoneExitMapPlayModeTests` (4) and `ZoneExitFlowPlayModeTests` (5): normalized ellipse geometry, Inspector radius changes, locked/unlocked/near/inside Continue, same-zone restored travel history, single transition and Zone03 entry position/heading/depth.

## 4. Map lighting

Before: all light layers used alpha1, including both Zone03 layers.

After: Inspector defaults Zone01=1, Zone02=.35, Zone03=.225 per layer, Zone04=.30. Optional per-layer overrides support explicit zero; missing/invalid values fall back safely. Refresh applies only light alpha, preserving RGB, textures, grid/mission UI and scale animation. Reopen, alternate maps and installer reruns retain configured values. Targeted migration is Undo-capable and saves only affected map assets.

Files: `Assets/_Project/Scripts/Navigation/ZoneMapConfig.cs`, `Scripts/UI/ZoneMapPresentation.cs`, `Scripts/Editor/{ZoneMapEditor,MapLightOpacitySetupEditor}.cs`, `Content/Maps/Zone01.asset` through `Zone04.asset`, `Scenes/Gameplay/Zone01.unity`.

New `MapLightOpacityRulesTests` (14) and `MapLightOpacityPlayModeTests` (4) cover fallback/per-layer values, installed defaults, texture/RGB/coordinate invariants, reopen/rebind/alternate maps and animation.

## Verification record

- Pre-integration required fixtures: **25/25 PlayMode passed** (`4ed89a5a6456`). This was not a pristine pre-edit baseline: early independent exit/lighting edits were already present.
- First integrated required/new fixtures: **55/55 PlayMode passed** (`b4e60fc59fcb`). Final UX refinements and the SEND→DELETE regression were added afterward.
- Entire EditMode suite: **54/54 passed**, zero skipped (`2c00da90018b`).
- First entire PlayMode suite: **205/212 passed**, seven failures (`981ffcf47f19`). The required/new feature fixtures passed. Failures exposed old fixture assumptions: fresh/unisolated tutorial saves, northward movement despite 90° entry, obsolete exclusive-window/coasting behavior, retired UI paths and old radar landmark coordinates.
- Legacy fixture rerun after isolation: **9/11 passed** (`2ac41a5cfd1d`). The remaining two World Map fixtures were then updated to the actual remembered-zone startup and visible watercolor controls/TMP labels.
- Final entire PlayMode rerun: **212/212 passed**, zero failures/skipped, 590.62 seconds (`d17c3ddb81d0`).
- Final compiler check: zero entries, not compiling. Final Unity Console error check: zero entries. Scoped `git diff --check` passed. Editor restored to its original clean Bootstrap scene in EditMode.

Fixture maintenance is confined to tests: `LegacyCabinTestSession`, `CabinAtmospherePlayModeTests`, `CabinNavigationPlayModeTests`, `ComputerAppsPlayModeTests`, `ComputerShellPlayModeTests`, `WorldMapPlayModeTests`, `RadarTerrainPlayModeTests`. Runtime behavior was not altered to satisfy these legacy assumptions.

## Visual evidence and limits

Unity offscreen renders in `D:/G10/Temp/TestCaptures` were inspected: `tutorial-helm-{1920,1366}.png`, `photolab-delete-{1920,1366}.png`, `photolab-confirm-{1920,1366}.png`, and `zone02/03/04-light-before/after.png`. The confirmation warning uses an actual unsent mission photo. Tests also check TMP overflow, passive graphics and reference-canvas separation from controls/window chrome.

These are Unity-rendered captures and instrumented production flows, not a claim that a human manually played a newly built Windows executable. No standalone Windows build, hardware/performance benchmark or forced process-kill during a filesystem transaction was performed. Lighting values remain initial designer-tunable playtest values. A fresh human playtest is still recommended before publishing the next release.
