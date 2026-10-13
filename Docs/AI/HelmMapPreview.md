# Helm current-zone map preview — 2026-10-13

## Scope and behavior

Restored the map screen inside the authored helm's lower-middle pocket in the
canonical Zone01 cabin. All four logical zones share this cabin. The preview
reads `CabinStationView.MapPanel` and its authoritative `ZoneMapPresentation`
configuration/runtime; it does not follow the last historical chart viewed.

- Map artwork only: no `PhotoSurveyMap`, grid, coordinate labels, POIs, checkmarks,
  vessel icon or pointer-coordinate readout.
- Full map aspect/UV preserved with `AspectRatioFitter.FitInParent`. Letterboxing
  preserves the whole map rather than stretching/cropping it into the pocket.
- Opaque blue backing covers the 3x3 lines already painted into the helm art.
  Blue/lavender glass, faint upper reflection, top glint and lower shade integrate
  the screen with the existing palette. Original art textures are unchanged.
- Clicking opens the active zone's full chart through `OpenCurrentMap()`. The
  existing top HUD Map tab still reopens the remembered historical/world chart.
- Tutorial Map access, pause, modal, panel lock, transition and direct-interaction
  guards apply to the new entry point. Merely seeing the preview does not teach
  Map practice; opening the full chart does. Tutorial step IDs/state are unchanged.
- Zone03 terrain artwork follows the rock-breaker reveal boundary and persisted
  completion after Continue, including while the full chart is inactive.
- No coordinate, mission, resource, balancing or save-format changes.

The Unity feature/uGUI guidance was applied as a targeted hierarchy addition,
with cached references, balanced button listeners and passive glass layers.
No cabin rebuild or unrelated scene/asset save was performed.

## Authoring

`NavigationPanel/MiniMap` has one button, one map RawImage and five passive glass
layers; the root Image is its opaque backing and only raycast target. Colors can
be adjusted on the individual UI components in the Inspector. The pocket is
276x218, centered at authored (1063, 811.5) with a +1-degree local Z rotation
in the 1920x1080 cabin frame. This aligns the entire screen (map, backing and
glass) with the hand-painted pocket rather than the screen axes.

`G10/Zone 1/Restore Helm Map Preview` installs this pocket only. The installer is
also called by the cabin builder and navigation artwork installer. Two repeated
targeted installs in Unity retained exactly one preview, six children, valid
serialized bindings and zero persistent callbacks/survey overlays. Only Zone01
was saved via Unity Editor APIs; scene diff is an additive subtree.

## Verification

This section records the initial restoration validation, before the alignment
follow-up below.

Repository: `D:/G10`; branch `codex/state-qa-hardening-2026-10-12`.
Baseline/final HEAD (no commit): `13a859a13326b016b32eb29b2ae3c23c5e2b3937`.
Existing QA edits, untracked artist files and Unity-generated fonts were preserved.
Every gameplay fixture redirects save and photo archive paths before scene load.
The four-zone Debug Start fixture additionally verifies its isolated normal
timeline sentinel remains byte-identical.

MCP commands (Unity 6000.4.2f1, instance port 7890):

```text
unity_testing_run_tests(mode="PlayMode", filter="AuthoredArtwork|FreshSpawnGates|FourZonePresets|OverviewHotspots")
unity_testing_run_tests(mode="PlayMode", filter="HelmMapPreviewPlayModeTests|HelmPreviewRequiresOpeningFullChartToTeachMap")
unity_testing_run_tests(mode="PlayMode", filter="HelmMapPreviewPlayModeTests|CabinNavigationPlayModeTests|WorldMapPlayModeTests|TutorialPlayModeTests|DebugStartPlayModeTests|RockBreakerPlayModeTests")
unity_testing_run_tests(mode="EditMode")
unity_testing_get_job(jobId=..., includeFailedOnly=true, waitTimeout=20)
unity_get_compilation_errors(severity="error")
```

Baseline job `b75760e7ca0f`: 4 PASS / 0 FAIL / 0 skipped, 24.58s.
The broad regex matched these actual tests, not all artwork/world-map fixtures:
FourZonePresetsUseRealProgressAndIsolatedContinue;
EndingVariantsKeepAuthoredArtworkAndMenuButton;
CaptureMapLightsBeforeAfterWithoutMutatingAuthoredArtwork;
FreshSpawnGatesSkipCancelAndDisableAreSafe.

Focused new coverage job `fba9f8c89ff6`: 4 PASS / 0 FAIL / 0 skipped, 10.55s:

- Art-only hierarchy, full UV/aspect, painted-grid occlusion, real EventSystem
  pointer raycast/click, render captures at 1920x1080 and 1366x768.
- Repeated reopening after historical/world chart selection; current-zone route;
  unchanged voyage and persisted timeline JSON.
- Pause/modal/locked-panel rejection and resumed click behavior.
- Real Intro/Helm/Map/Radar tutorial path through the new preview button;
  passive preview cannot complete Map, successful full-chart practice persists.

Existing Debug Start and rock-breaker tests were extended (not duplicated) to
check all four zone textures/routing and Continue, delayed alternate terrain
reveal and interrupted-event Continue. Existing tutorial tests still exercise
the top Map button and Camera -> PhotoLab SEND -> Capture -> actual hull upgrade.

First broad run `00dd12c4061c`: 29 PASS / 1 FAIL / 0 skipped, 147.54s.
The failing new Debug Start assertion attempted map access during Zone01 Intro.
This was a fixture precondition error: Zone01 Debug Start intentionally starts
fresh onboarding. The fixture now skips Intro text through the real dialogue,
moves/turns/changes depth through actual helm actions and earns Map access.
Production tutorial guards and assertions were not weakened.

Final broad run `fa63403a7bc5`: 30 PASS / 0 FAIL / 0 skipped, 166.26s.
CabinNavigation 4, DebugStart 3, HelmMapPreview 3, RockBreaker 2, Tutorial 15,
WorldMap 3. Four new tests plus extended existing four-zone/terrain/tutor coverage.

All EditMode tests, job `46518958f6ca`: 63 PASS / 0 FAIL / 0 skipped, 2.26s.
Final verified selection: 93 PASS / 0 FAIL / 0 INCONCLUSIVE. The earlier fixture
failure is retained above and in its XML, not hidden by the final results.

Native NUnit XML copied after each completed job from
`C:/Users/Artermis/AppData/LocalLow/DefaultCompany/G10/TestResults.xml` to:

```text
D:/G10/Logs/HelmMapPreview/baseline-playmode.xml
D:/G10/Logs/HelmMapPreview/focused-playmode.xml
D:/G10/Logs/HelmMapPreview/regression-first-playmode.xml
D:/G10/Logs/HelmMapPreview/regression-final-playmode.xml
D:/G10/Logs/HelmMapPreview/regression-editmode.xml
```

Render captures: `D:/G10/Temp/TestCaptures/helm-preview-1920.png`,
`helm-preview-1366.png`, and `helm-preview-zone1.png` through
`helm-preview-zone4.png` (four-zone run). Captures use a real Canvas/camera render,
not a mockup. Visual inspection is not a human gameplay session.

Compilation: 0 errors, not compiling at final check.
Scene inspection: 0 missing scripts; preview bindings are valid. Test overrides
were restored and normal timeline selection reset. The clean Zone01 additive
inspection scene was closed, leaving the original clean Bootstrap in EditMode.
Windows build, human playtest and the entire PlayMode suite: NOT RUN for this
UI-only request. No commit, push or merge.

`git diff --check` passes for C#; Unity's scene serializer adds blank `m_Name: `
lines with trailing spaces. `git -c core.whitespace=-blank-at-eol diff --check`
passes for the full diff without hand-editing Unity YAML.

## Follow-up: align with the painted pocket

The original UI root had zero rotation, while the artwork's horizontal pocket
edges rise slightly to the right. Adjusted only the MiniMap RectTransform to
local Z +1 degree and center (1063, -811.5), 1.5 authored units higher. The backing,
map, glass and clickable region inherit this transform together. Size, aspect,
colors, artwork and gameplay are unchanged. The targeted editor installer now
retains this pose when reapplied.

Using the Unity uGUI targeted-edit guidance, no hierarchy was rebuilt and no
runtime/map-coordinate code was changed. Updated the existing preview test to
assert the root angle/position and zero independent rotations on its children.

Post-alignment validation, job `6ef21f015740`: 4 PASS / 0 FAIL / 0 skipped,
11.84s, with the same focused PlayMode filter as the initial restoration. This
includes the real pointer raycast/click, remembered-map behavior, pause/modal
guards and actual Map/Radar tutorial practice. Rendered and visually inspected
1920x1080 and 1366x768. Compilation: 0 errors.

The launch request timed out while the Editor was not responding; the already
accepted job was recovered from Editor.log and queried after the user focused
Unity. No duplicate test run was launched and no stale XML was counted.

```text
D:/G10/Logs/HelmMapPreview/alignment-playmode.xml
D:/G10/Temp/TestCaptures/helm-preview-aligned-1920.png
D:/G10/Temp/TestCaptures/helm-preview-aligned-1366.png
```

The full suites/Windows build/human playtest were not rerun for this transform-only
follow-up. Test saves remained isolated. No commit/push/merge.
