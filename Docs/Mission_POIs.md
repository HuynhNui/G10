# Mission POIs

Zone01's existing three black location images now use the shared `PhotoSurveyZone.locations` list. Their positions and art are unchanged:

| POI ID | Map position |
| --- | --- |
| `zone01-north` | (625, 475) |
| `zone01-east` | (725, 175) |
| `zone01-left` | (275, 75) — current mission |

`Zone01Mission.targetPoiId` selects the destination by ID. `PhotoSurveyZone.TargetPoi` resolves it from the location list; its read-only `center` accessor keeps the existing radar and camera aligned with that POI. There is no independent survey coordinate or grid-cell target. Each POI has an arrival radius (currently 20 map units). Capture checks that circle plus the existing depth, visibility and photograph prerequisites. Photo objectives require arrival as well as a successful photograph. The existing voyage task set still owns progress; the computer mission log's template/progress behavior is otherwise unchanged.

To change destinations, select `Assets/_Project/Data/Computer/Zone01Mission.asset` and change **Target Poi Id** to another ID above. Edit position/radius on **Photo Survey Zone → Locations** on the Zone01 cabin if needed. Both maps share this list; keep its order aligned with their existing Location Icons arrays. An unknown ID disables arrival, capture and camera targeting rather than falling back to a grid cell. Assign destinations before starting a voyage; live retargeting does not reset task progress.

The removed gold square was drawn in `PhotoSurveyMap.OnPopulateMesh` around the old `PhotoSurveyZone.center`. The survey installer chose that position by searching nearby grid centers. Both behaviors are removed, as is the pointer's filled cell. Existing chart lines, pointer crosshair, ship marker, movement and coordinate display remain. No replacement colored tile or debug tile exists.

## Changed files

- `Assets/_Project/Scripts/UI/PhotoSurveyMap.cs`: removes the gold outline and hover-cell fill; uses shared POIs for marker positions, selection and task readout.
- `Assets/_Project/Scripts/Navigation/PhotoSurveyZone.cs`: defines POI data and resolves the mission ID; circular arrival checks.
- `Assets/_Project/Scripts/Navigation/CreatureCatcher.cs`: capture radius comes from the target POI.
- `Assets/_Project/Scripts/Computer/MissionDefinition.cs`: replaces independent target coordinates with `targetPoiId`.
- `Assets/_Project/Scripts/Computer/MissionLogView.cs`: displays the target POI ID.
- `Assets/_Project/Scripts/Computer/PhotoCaptureService.cs`: validates the POI and gates photo-task completion on arrival.
- `Assets/_Project/Scripts/Editor/PhotoSurveyEditor.cs`: removes automatic grid-cell mission selection.
- `Assets/_Project/Scripts/Editor/NavigationArtworkEditor.cs`: preserves explicit POI positions when installing marker artwork.
- `Assets/_Project/Data/Computer/Zone01Mission.asset`: targets `zone01-left`.
- `Assets/_Project/Scenes/Gameplay/Zone01.unity`: shares the existing three marker positions through POI records; wires the mission and updates the legacy legend coordinates.
- `Assets/_Project/Tests/PlayMode/PhotoSurveyPlayModeTests.cs`: arrival, capture, tile-free rendering, retargeting and missing-ID regression coverage.
- `Assets/_Project/Tests/PlayMode/CabinNavigationPlayModeTests.cs`: verifies unchanged marker positions and POI hover tasks.
- `Assets/_Project/Tests/PlayMode/WorldMapPlayModeTests.cs`: uses POIs and places the camera fixture near the relocated creature.
- `Assets/_Project/Tests/PlayMode/ComputerAppsPlayModeTests.cs`: verifies POI target presentation.
- `Docs/Mission_POIs.md`: assignment instructions and implementation notes.

## Validation (2026-09-16)

Unity 6000.4.2f1 compiled the modified project in an isolated copy. Seven of nine selected Play Mode tests passed: POI radius/capture/retargeting/missing-ID and mesh regression, depth/radar, chart coordinates, movement boundaries, authored marker alignment, map selection/navigation, and camera capture. The two failures match the saved pre-change baseline: `Zone01CabinOpensPanelsAndKeepsNavigationState` (simulated keyboard movement in batch mode) and `AppsReadProvidersWithoutInventingGameplayData` (expects an empty/offline photo library while the live photo service has archived photos). The latter fails before its mission-log assertions. These unrelated behaviors were not changed.

Rendered map and left-POI hover screenshots were inspected: no yellow square or replacement colored tile, unchanged black marker positions, left marker shows the two existing survey tasks, and the legend displays X 275 Y 75. The existing collision map also has a ship-clear route from (600,100) to (275,75). Results and screenshots: `Logs/PoiValidation/` (local, ignored by Git).
