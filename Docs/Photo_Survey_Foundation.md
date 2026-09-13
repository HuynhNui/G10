# Zone01 depth and photo survey foundation

Heading remains 0° North, 90° East, increasing clockwise. Future photo projection
must use `Atan2(delta.x, delta.y)` and `DeltaAngle(shipHeading, targetHeading)`.
Positive angular offset is right of the bow. Screen-Y projection is deferred.

`ZoneNavigation` owns XY, heading and depth. Depth is positive metres below the
surface; `WorldPosition.z` is negative depth. Default start is 230 m to match the
existing helm artwork, bounds 0–500 m, speed 5 m/s. Tune these in Inspector.
The upper painted depth button ascends, the lower dives. Hold to move; release,
pointer exit, panel change, focus loss or Stop cancels the command. No depth inertia.
XY steering retains its existing input bindings. This is a clamped depth prototype,
not bathymetry, pressure damage or 3D terrain collision.

Use G10 > Zone 1 > Install Depth And Photo Survey outside Play Mode, with Zone01
loaded. Save Zone01. The additive, Undo-aware installer preserves the existing
artwork and panels and refuses to duplicate installed controls.

The survey cell is 50×50 chart units, outlined gold on the main map and minimap.
The main map draws visually square grid cells within X=0–1200 and Y=0–700.
Use G10 > Zone 1 > Fix Square Map Grid once and save Zone01. SquareChartContent
fits the artwork to the calibrated aspect ratio (about 1.4765), with side margins.
Artwork, pointer target and survey overlay share that rectangle. Both axes now
use the same display length per metre: all 24 by 14 cells represent 50×50 m,
including the final row. The original painted grid is also corrected by this fit.
PhotoSurveyMap exposes Show Grid, Grid Color, Grid Line Width
and Hover Color. Fine grid lines are suppressed on the small minimap.
Grid lines have a configurable minimum screen-pixel thickness and a dark outline.
This prevents subpixel lines disappearing when the cabin Canvas is scaled down,
and keeps the 50 m divisions legible on both pale terrain and dark water.
Hovering highlights the cell and draws a small crosshair at the pointer. The readout
reports the actual pointer X/Y to one decimal, without the old cell-size prefix
or snapping to the cell centre; hovering the survey region also reports
P01 and its target depth. White marks the ship. Chart conversion uses the existing
calibration; the printed chart ends at Y=700. Legacy navigation's Y=780 limit is
unchanged and still needs resolution in the broader coordinate-foundation work.

`PhotoSurveyZone` holds a stationary Creature01 world record at the cell centre.
The installer picks a centre reachable along a clear path from the configured start.
This is an authored test encounter, not random spawning or a completed photo camera.
Tune centre/depth/presence on Cabin Station. Map markers indicate an investigation
area; they do not certify a detected animal.

Radar samples creature presence when Scan is pressed, using 3D range (85 m) and
the existing XY terrain mask along the line of sight. Gold diamond = creature;
pale green points = terrain. The echo appears as the scan expands and expires
after eight seconds. A scan is a snapshot from its original XY position.
This terrain check is a conservative 2D approximation, not underwater acoustics.

## Proposed photo occlusion algorithm (not implemented here)

Project the creature and all nearer occluding props into the same photo frame.
Rasterize a small binary/alpha mask (e.g. 128×128) for the creature's visible silhouette.
Combine the nearer rock/kelp masks without double-counting overlaps. Compute
occlusion = covered creature pixels / creature silhouette pixels inside the frame.
Keep fog attenuation separate. Ignore farther objects and transparent sprite margins.
Cache asset masks, and calculate once per capture; no per-frame ReadPixels is needed.
This yields a useful MVP and can be tested with 0%, partial and fully covered cases.

Capture composition, photo save/gallery, creature movement and mission completion
remain future work. This step provides the navigation state and test encounter only.

## Validation

Installed through the Unity Editor menu and saved in Zone01 after the Editor recovered.
P01 is at X=575, Y=125, depth=230 m. Native Play Mode inspection confirmed both
depth buttons change the readout, main map and minimap markers are visible, hovering
P01 reports the correct cell centre/depth, and scanning shows a gold creature diamond
separate from the terrain points. Console showed no errors during these checks.

The automated suite was not run for this change, at the user's request to save time.
PhotoSurveyPlayModeTests is available for a later run. The earlier 8/8 suite predates
this change and is not evidence for it. No standalone player build was performed.
