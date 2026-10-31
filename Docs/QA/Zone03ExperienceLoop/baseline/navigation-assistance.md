# Source-chart navigation assistance

This is offline map-reading assistance for the active baseline pilot. It changes no gameplay state and does not drive input. The pilot retains exclusive Unity control and must confirm each corner against the actual displayed chart before ordinary WASD travel. No live contact coordinates, creature state, physics calls, resource setters or teleportation were used.

## Calibration

- Authored `MapingamewithStone.png` and `MapLinewithStone.png` are 1920×1080. `ZoneMapConfig.WorldSize` uses those native dimensions; current world x increases right and y increases upwards. For viewing the PNG, image y = 1080 − world y. The older 1200×700 authoring coordinates and 1440×840 display size are not the current navigation coordinate system.
- Current `Assets/_Project/Content/Maps/Zone03.asset` is coordinateVersion3: L1(446.08,214.45714), depth360; L2(48,630.56573), depth470; L3/zone03-l4(1114.08,249.48), depth640; rock(1480,150), depth590; exit(1750,25), destinationZone04. POI marker art snaps visually to grid-cell centers; the authored coordinates remain the gameplay targets.
- Serialized contour mask is 960×540, or 2×2 source pixels per cell. `NavigationArtworkEditor.BakeBoundaries` thresholds dark visible line pixels, not watercolor shading. `ZoneNavigation.CanOccupy` probes the point plus offsets ±2 on each axis. Radar's filled region is a display interpretation of those contours and is not a separate collision map.

## Advisory corridors

Coordinates below are source/world coordinates, not screen click positions. Start from the real observed position near each POI; do not assume the ship is exactly at its authored center. Approach each bend with a small tolerance and let actual speed settle before changing direction. Driver heading changes and residual motion are not simulated in this offline derivation.

| Leg | Ordered advisory points after its starting POI |
| --- | --- |
| L1 → L2 | (203,375) → (179,539) → L2(48,630.56573) |
| L2 → L3 | (269,517) → (773,635) → (849,745) → (1091,693) → (1129,707) → (1245,859) → (1453,893) → (1641,681) → (1557,331) → (1143,263) → L3(1114.08,249.48) |
| L3 → rock approach | (1341,297) → rock(1480,150) |
| Destroyed rock → exit | After actual destruction switches the chart, exit(1750,25) has a direct source-contour line from the rock approach. |

L1→L2 passes around the western peninsula tips; a direct diagonal crosses a visible contour. L2→L3 is a substantial north/east detour around the central landform, then comes back west along its south side. The listed route goes above the upper crystal shape rather than through the tighter gap between the upper crystal pair. L3→rock stays north of the lower crystal formation before approaching the barrier. Do not use the alternate opened-rock chart to bypass the actual Rock Breaker event.

The source-only derivation decoded the existing visible-line mask, computed a bounded cell-distance buffer, preferred wider cells in A*, and rechecked simplified straight segments every 0.5 world unit after coordinate conversion. It did not query the live navigation chart or runtime obstacle/contact state. This is an offline aid to human-style chart reading, not a tested collision-free dynamic route.

Straight-segment conservative clearance lower bounds are 10 world units for L1→L2, 12 for L2→L3, 10 for L3→rock and 22 for rock→exit on the post-destruction mask. These subtract one 2-unit source cell from the measured distance; they do not bound a curved turn, starting-position offset or arrival-radius error. The L2 segment (1245,859)→(1453,893) is the narrowest sampled segment at 7 cells; other L2 segments are at least 8 cells. Large corner tolerances can cut across contours. An actual obstruction requires stopping and reading the visible chart again, not retrying this advisory sequence as if it were authoritative.

Polyline lengths are approximately 616.8, 2683.2, 433.3 and 298.0 world units respectively. They describe this source-chart proposal only, not actual distance, completion time, resource cost or a shortest-route benchmark. Retain all real collisions, detours, rests and input gaps in the voyage report. The separately recorded unobserved collision interval before continuation is not reconstructed by this route.

Sources inspected read-only: `Assets/_Project/Content/Maps/Zone03.asset`; `Assets/_Project/Art/Environment/Zone3/MapingamewithStone.png`; `MapLinewithStone.png`; `MaplineAfterStoneBreaked.png` via its authored mask; `Assets/_Project/Scripts/Navigation/ZoneMapConfig.cs`; `ZoneNavigation.cs`; `RadarTerrainMask.cs`; `Assets/_Project/Scripts/Editor/NavigationArtworkEditor.cs`; `ZoneTwoChartInputDriver.cs`. Numeric derivation is retained in `baseline/navigation-assistance.json`. No Assets were edited and no Unity calls were made.
