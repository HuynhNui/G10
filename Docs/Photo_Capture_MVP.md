# Zone01 photo capture

Installation: open GameplayCore + Zone01 outside Play Mode, then use
G10 > Zone 1 > Install Photo Capture and save Zone01. The survey foundation must
already exist. The installer creates readable 512px runtime copies of selected
PhotoKit textures; original art and import settings remain intact.

Cabin camera hotspot opens a still-photo panel. CHỤP captures and automatically
saves the image. COMPUTER > PHOTO LAB browses the newest 24 loaded images using
previous/next buttons. Camera status in SHIP STATUS reflects the installed service.

The current survey centre is X575 Y125 at depth230m. From the initial X600 Y100
position, turn toward heading315° (northwest) and stay near 230m depth. The camera
uses 0° North, 90° East, clockwise headings, matching the helm. FOV70°, range65m,
and vertical separation at most30m are configurable in the profile/code.
Move closer for larger subject coverage. Turning away gives NoSubject.

Capture state comes from ZoneNavigation.WorldPosition and the same PhotoSurveyZone
creature record that radar detects. Two fixed local props provide stable landmark
and occluder placement. No global RNG is used: unchanged world state yields the
same image composition. Depth selects shallow/mid/deep backgrounds at 200/300m,
adapted to the current helm's 230m starting depth. Screen-Y depth projection remains
deferred as requested; vertical separation gates visibility instead.

PhotoLayerComposer blends cached texture pixels into a 640×360 image once per
capture. Layers sort by world distance. Subject alpha forms a silhouette mask;
nearer props accumulate coverage as a union, so overlaps are not counted twice.
Fog remains separate. XY terrain blockage uses the existing chart mask and returns
an obstructed view; it is a conservative approximation without seabed bathymetry.

Results: NoSubject, LifeDetected, GoodPhoto, TooFar, Obstructed, LowVisibility.
GoodPhoto is an image-quality result, not automatic mission completion.
PhotoCaptureProfile exposes art, FOV, distance, size and quality thresholds.

PNG and JSON metadata are saved below Application.persistentDataPath/Zone01Photos.
Metadata includes capture time, XY, depth, heading and result. Reopening Zone01 loads
the newest24 saved entries. Older files remain on disk; the in-memory gallery is
capped to24. No cloud upload. File errors preserve the current image in memory and
show an error message. Generated textures are released when the zone unloads.

Automated tests are skipped at the user's request. Earlier Phase B empty-photo
fixtures predate the live camera and need updating before a future regression run.
Installed into Zone01 through the native Editor menu and saved. Unity compiled
without reported C# errors. A native capture at initial heading0° produced NoSubject,
saved successfully, and appeared in Photo Lab with the correct position/depth/time.
The full set of quality outcomes and archive reload have not been regression-tested.
