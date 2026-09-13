# Zone 1 creature catching

Installed via G10 > Zone 1 > Install Creature Catching, outside Play with Zone01 loaded; save the scene.
The cabin capture hotspot opens a dedicated capture screen, with Catch, Open Inventory,
and the existing Cabin / Escape exit. Radar only displays the capture range circle;
capture controls and result messages belong to CapturePanel.
The lime circle is a 20 m horizontal capture radius, configurable on Cabin Station > Creature Catcher.
Depth tolerance defaults to 10 m and the existing chart terrain mask must allow a clear path.
Scan reveals the actual P01 creature; capture checks its current state even if the echo has expired.
The circle follows the ship, with scan samples displayed relative to the current ship position.

Successful capture adds Item1.png to the four-slot inventory, removes the encounter creature,
and reports “Đã bắt được sinh vật! Hãy kiểm tra balô.” Further attempts report “Không có gì cả.”
Radar echoes disappear and subsequent photos no longer contain that creature.
Full inventory does not remove the creature. Missing configuration reports device unavailable.
Item state belongs to Cabin Station for this voyage; changing panels retains it, scene reload / new Play resets it.
Disk saves, multiple encounters, item use/drop and mission completion are outside this MVP.

Tune captureRadius and depthTolerance on CreatureCatcher; captureRingColor on RadarDisplay.
Automated tests are skipped at the user's request. No standalone build performed.
Native UI inspection confirmed the empty result at the default 20 m radius, then
successful capture and Item1 in the bag with radius temporarily set to 40 m in Play.
The saved scene retains 20 m. The depth/terrain boundary cases were not played through.
