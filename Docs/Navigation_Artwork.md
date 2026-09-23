# Navigation artwork and full chart

Zone01 uses `Art/PrototypeCabin/Navigation/Idle_No_Compassneedle.png` as the helm background. `Idlle.png` is a reference image only. Six non-interactive RawImage patches sample the supplied full-frame pressed textures at the button rectangles, so steering and movement can light simultaneously without copying the background or painting over the compass. CabinStationView drives them from the same input that moves the ship.

`HeadingPivot/CompassArtwork` uses the separate Compassneedle texture. The pivot is the dial center at (1478, 319) in the 1920 × 1080 artwork. The 76-degree art offset aligns its angled tip with the ship's north at heading zero; heading increases clockwise. The numeric heading and game convention remain 0° north / 90° east.

The main chart uses all of `Art/Environment/Zone1/Mapingame.png`, mapping UV (0,0)–(1,1) to coordinates (0,0)–(1200,700). A 1536 × 896 display gives 24 × 14 square 50 m cells. The separate ChartOuterFrame owns axis labels outside the image. The miniature map, pointer coordinates, ship, survey marker and radar use the same mapping. The three location markers are snapped from the bracket positions in `Map.png` to 50 m cell centers: (625,475), (725,175), and (275,75). Each marker spans exactly one 50 m grid cell on both map views and resizes with the chart. Existing P01 gameplay coordinates remain unchanged; these geographic markers do not create new missions.

`location.png` marks all three geographic locations. The separate gold P01 task outline and completion check retain their existing gameplay meaning. `mapline.png` is baked to a 960 × 540 boundary mask for navigation. Dark opaque contour pixels block movement; white/transparent regions remain navigable. Colors in Mapingame are not interpreted as blocked terrain or bathymetric depth.

### Filled radar terrain and impact damage (2026-09-24)

Radar now fills the other side of these contours with dots, instead of showing only the line pixels. `RadarTerrainMask` flood-fills the ship's connected navigable water region using the existing 2 m hull clearance; the remaining cells are radar terrain. This also seals tiny hand-drawn gaps narrower than the ship. The fill is presentation-only and does not replace the authored collision mask or recolor the source images. It is cached until the chart or voyage is reset/restored. An invalid old-save spawn falls back to raw contours until the ship moves clear. Newly authored passages must be wide enough for the hull, and contour endpoints must meet another contour or the chart edge to enclose a terrain region.

`RadarDisplay` samples the filled mask once per sweep. Dots still appear as the clockwise needle reaches their bearing, then fade using the existing timing. The ship's water area and all three Zone01 mission sites remain clear.

Terrain impact removes **absolute actual speed** from Hull, before velocity is stopped: an 18-speed collision removes 18 Hull, a 4.5-speed collision removes 4.5 Hull. Reverse and coasting collisions use the same rule. Contact is latched until the ship moves at least 2 m clear, so holding throttle into a wall does not cause damage every frame. Hull clamps at zero and the existing disabled-ship/recovery flow applies. Legacy save/scene damage multipliers remain readable but are normalized to 1; no save reset is required.

To reapply artwork after editing the UI, open Zone01 and use **G10 → Zone 1 → Install Navigation Artwork**, then save. The installer updates the existing hierarchy and preserves station/panel/gameplay references. Original PNG files are not modified.

## Validation (2026-09-13)

Unity 6000.4.2f1 compiled and saved the updated scene in an isolated project copy because the open Editor had no active MCP connection. The resulting Zone01 scene was copied back to this project. Reopen Zone01 in the existing Editor to load its updated on-disk hierarchy.

The final Play Mode suite passed 7/10 tests, including the new artwork/compass/grid integration test and the existing depth/chart/radar contact test. A control run using the pre-change scripts and scene passed 6/9 tests and reproduced the same three failures: synthetic keyboard movement, computer album expecting no saved photos, and the older computer-shell test expecting continued movement after closing the helm. These failures predate the artwork change.

Unity-rendered previews and XML results are in `Logs/NavigationArtwork/`: `helm-art.png`, `map-art.png`, `radar-art.png`, `all-tests.xml`, and `baseline-tests.xml`. The previews render the real Canvas through a temporary camera in the test; they are not mockups.

The three-location correction was recompiled and visually checked in Unity. The focused Play Mode test passed, verifying three markers on both charts, reference-image positions, and marker width/height equal to one grid cell (`locations-tests.xml`).

Hovering any part of one of these three cells opens the existing task panel with its location number. Each location has a separate serialized task list, initially empty; the panel shows only its heading and zero tasks. Leaving the cell hides the panel. The old P01 survey retains its separate gameplay tasks. The focused test covers cell centers, edges, pointer exit, empty cells, marker alignment and empty task lists.
