# P01 objective list

P01 has two required tasks: Photograph, Capture. Configure the list on Cabin Station > Photo Survey Zone > Tasks.
Hover over P01 on the main map to read the checklist and progress count. When every task completes,
check.png replaces the gold square on both the map and minimap. Empty task lists never auto-complete.

Photograph is credited only for a new GoodPhoto or LifeDetected photo of this survey creature.
NoSubject, TooFar, Obstructed and LowVisibility do not count. Existing archive photos are not credited.
Capture is credited after the creature is successfully added to inventory. Capture requires any configured
photograph task first, preventing permanent loss of the only subject before the photo objective completes.
Out-of-range attempts still report nothing found. Duplicate actions do not duplicate progress.

The inventory uses Assets/_Project/Art/Sprites/Creature/Zone1/Item1_real.png.
The completion marker uses Assets/_Project/Art/UI/Map/check.png. Original source assets are unchanged.
Run G10 > Zone 1 > Install Survey Objectives outside Play, then save Zone01.
Progress is voyage-local, matching current creature/inventory lifetime; it resets with the scene.
The separate Computer Mission Log template is unchanged. Automated tests skipped as requested.
