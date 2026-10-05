# Mission depths and Mission Log scroll

Base: `main`, `04071f2fd756361f365a4aecc24bb09d848353fb` (depth plumbing).
No commit or push performed.

## Authored content

| Zone | POIs in location order | Target depths (m) | Floor / entry (m) |
| --- | --- | --- | --- |
| Zone01 | left, east, north | 230, 330, 460 | 0 / 230 |
| Zone02 | l1, l2, l3 | 280, 390, 510 | 230 / 230 |
| Zone03 | l1, l2, l3, l4 | 360, 470, 560, 640 | 230 / 230 |
| Zone04 | l1, l2, l3, l4, l5 | 525, 590, 615, 680, 735 | 500 / 500 |

All 15 mission POIs use `overrideDepth = true`. Zone03 rock also overrides to
590 m, and its existing XY radius remains unchanged. Its progression action
now requires both XY arrival and depth within the serialized 10 m tolerance.
The same objective, rewards and world flag are retained. Exit/final triggers
remain XY-only. Entry coordinates/headings, terrain and mission rules are unchanged.

`ZoneMapEditor.Configure` routes regenerated locations through
`RebuildLocations`, which preserves depth metadata by stable ID, not array order.
Generated XY and radius are not copied from old locations. `ClonePoi` includes
both depth fields. Editor-compatible validation called this rebuild path twice
for all four maps with reversed POI order and deliberately different XY/radius:
30 POI checks passed; cloning all fields passed. Designer asset values are the
source of truth; no depth table was added to the installer.

Visible/revealed Mission Log locations use `PhotoSurveyZone.DepthFor`.
Hidden locations remain excluded until revealed. CabinStationView's existing
hover resolver was left unchanged. The rendered watercolor map uses
PhotoSurveyMap instead; its hover readout now uses the same resolver for
visible/revealed POIs, without revealing unknown hidden depths or markers.

## Scroll layout

```text
MissionLogPanel
  ContentRoot (1240 x 680, uniformly scaled by ComputerWindow)
    MissionScroll (1180 x 510 at 30,15)
      Viewport (RectMask2D; transparent raycast surface)
        Body (top aligned, wrapped, ContentSizeFitter preferred height)
          TMP (existing presentation adapter)
    ExpeditionRoute (740 x 120 at 30,545)
    NextExpeditionZone (optional legacy button, at 800,560)
```

Vertical-only clamped ScrollRect, sensitivity 35. Opening/reopening resets to
the top; active text refresh preserves normalized scroll position. Height is
measured from the actual TMP renderer, since the skin disables the legacy Text
component. Layout rebuilds occur only on changed text, opening or explicit
layout setup, not every frame. No independent Canvas or responsive layout was added.

Install Phase B Apps upgrades existing apps even on its early-return path.
Running the actual installer twice in Zone01 produced one ScrollRect, one
RectMask2D and one ContentSizeFitter; the upgraded scene was saved.

Important local wiring difference: there is no production NextExpeditionZone
button in this HEAD. Routes use physical XY exit arrival. No new teleport or
route action was invented. Fixed route status is retained; compatibility with
an optional legacy button is tested using a UI-only fixture.

## Validation

Initial focused run: 35/35 passed after fixing test fixtures for the absent
legacy map readout and absent optional route button. Additional rendered-map
hover and real Zone03/Zone04 scroll coverage subsequently added.
Expanded authoring and scroll tests subsequently passed (3/3 and 2/2) during
the helm minimap-removal batch. TutorialPlayModeTests passed 6/6 and
MainMenuTutorialResetPlayModeTests passed 7/7: 18/18 total, no failures/skips.
The remaining broader depth regression suites have not yet been rerun.
`git diff --check` passed after trimming Unity-generated trailing whitespace
in the saved Zone01 scene. No unrelated failures were observed in that focused
run; the broader suites have not yet been rerun for this batch.

## Exact changed files

- `Assets/_Project/Content/Maps/Zone01.asset`
- `Assets/_Project/Content/Maps/Zone02.asset`
- `Assets/_Project/Content/Maps/Zone03.asset`
- `Assets/_Project/Content/Maps/Zone04.asset`
- `Assets/_Project/Scenes/Gameplay/Zone01.unity`
- `Assets/_Project/Scripts/Computer/ComputerDesktopSkin.cs`
- `Assets/_Project/Scripts/Computer/ComputerDesktopText.cs`
- `Assets/_Project/Scripts/Computer/ExpeditionComputerView.cs`
- `Assets/_Project/Scripts/Computer/MissionLogView.cs`
- `Assets/_Project/Scripts/Editor/ComputerStationEditor.cs`
- `Assets/_Project/Scripts/Editor/ZoneMapEditor.cs`
- `Assets/_Project/Scripts/Missions/ZoneMissionRuntime.cs`
- `Assets/_Project/Scripts/Missions/ZoneOneStory.cs`
- `Assets/_Project/Scripts/UI/PhotoSurveyMap.cs`
- `Assets/_Project/Tests/PlayMode/DirectCabinInteractionPlayModeTests.cs`
- `Assets/_Project/Tests/PlayMode/ExpeditionEndToEndPlayModeTests.cs`
- `Assets/_Project/Tests/PlayMode/PhotoSubmissionPlayModeTests.cs`
- `Assets/_Project/Tests/PlayMode/TutorialPlayModeTests.cs`
- `Assets/_Project/Tests/PlayMode/VesselDeathPlayModeTests.cs`
- `Assets/_Project/Tests/PlayMode/ZoneOneStoryPlayModeTests.cs`
- `Assets/_Project/Tests/PlayMode/MissionDepthAuthoringPlayModeTests.cs` and `.meta`
- `Assets/_Project/Tests/PlayMode/MissionLogScrollPlayModeTests.cs` and `.meta`
- `Docs/AI/MissionDepthAndScroll.md`

Generated/retained font cache changes (not intentional artwork edits):

- `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset`
- `Assets/_Project/Art/UI/Fonts/AlegreyaSansSC-Regular SDF.asset`

## Deferred depth-energy coupling

ZoneNavigation centralizes simultaneous-axis movement energy accounting;
the future depth multiplier should use actual vessel depth, not mission target
depth or entryDepth. Map POI depth stays uncapped authored content, while ship
capability caps navigation. Deeper goals therefore depend on existing upgrades.
No Energy/stat/reward/deadline/tutorial/save-schema changes were made here.

Unity dynamically updates font cache assets during Editor/tests. Those generated
changes are retained rather than reverting potentially shared/user changes.
