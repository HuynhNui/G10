# Local mission synchronization audit — updated 2026-10-23

## Correction — 2026-10-25

The earlier geometry-preservation decision below missed four explicit designer
edits already committed in `9395126` and retained on reference `43b1a26`: Zone02
entry `(100,900)`, Zone02 return `(100,1000)`, Zone03 return `(25,25)`, Zone04 return
`(100,1000)`. `b28dcc9` retained the earlier main values, so this was an omitted
part of synchronization, not a runtime/save rollback. The placement document and
regression tests were also omitted; they are restored alongside those four asset
fields. The POI geometry/depth claims below remain specific to POIs, not return
gates. Current validation is recorded in `../ReturnGateSynchronization/results.md`.

`git status`, branch, last five commits, full source/config diffs and local reference were inspected. Current branch: `main`, HEAD `3862225` (horror event system). Reference branch: local `codex/unified-effect-director`, `43b1a26`. No remote assumption, branch switch, commit, merge or push.

The requested `Docs/AI/MissionLocationsRefactor-2026-10-20.md`, MissionLayoutMigration and MissionLayoutEditor were absent from main but present on the reference branch. Main retained old Zone 3 four-site/Mechanical Module progression and old Zone 4 five-site/hidden collection progression. Newer uncommitted local work already implemented Zone 1 Bio Lamp, Zone 2 Pressure Hull and much more audio/presentation than the reference branch; copying the branch wholesale would have removed that work.

The reference document was imported with a leading provenance/status notice. Selected mission/save/UI/editor/tutorial source was reconciled while retaining newer quick-photo, audio/effects, intro, and device behavior. `*.before.txt` snapshots here preserve root-owned source before merging; TutorialBefore preserves tutorial originals. The targeted native installer has now applied and saved the canonical map, mission and marker bindings. No source-branch test result has been presented as a test of this working tree.

| Zone | Serialized layout before synchronization | Current installed layout |
| --- | --- | --- |
| 1 | 2 sites; local lamp cargo alias | 2 sites; Z1_Creature_01 cargo; Bio Lamp |
| 2 | 2 sites; second location still named Z2_L3 | 2 sites; Z2_L2 at retained zone02-l3; Pressure Hull |
| 3 | 4 sites; Mechanical Module; third creature photo only | 3 sites: zone03-l1/l2/l4; two capture materials; Rock Breaker |
| 4 | 5 sites; extra hidden photo and hidden item collect | 4 sites; 2 main + 2 hidden; main capture grants hint only |

There are 11 canonical mission locations and 9 mandatory organisms plus 2 hidden organisms. PHOTO means a valid quick shutter followed by manual SEND. No live Camera panel was restored. Explosive/detonator materials are consumed at CRAFT; INSTALL does not consume them twice. Normal/hidden ending choice and physical final arrival remain required.

The saved maps contain 2/2/3/4 locations. Retained POI geometry is preserved by stable ID: Zone 1 has depths 230/460 m and radius 35; Zone 2 has depths 280/460 m and radius 20; Zone 3 has depths 360/470/640 m and radius 20. The newer local Zone 2 second-site depth remains 460 m; the historical reference's 500 m target was not reapplied. The [map preservation comparison](../Zone03Cinematic/map-preservation.json) records only the intended Zone 3 `locations` and Zone 4 `locations`/`hiddenLocationIds` changes, with other map fields, art and terrain preserved. Removed mission marker identities were canonicalized without rebuilding the cabin.

Migration protects all snapshots and image bytes; historical routes are separate from new capture receipts. Invalid source cargo/legacy bitmasks remain invalid. Local lamp cargo quantities merge safely. Runtime layout validation rejects incompatible serialized mission data. The installed Zone 3 presentation binds all 18 generated WAV clips, and the live Camera panel remains inactive.

No user's actual timeline was opened or rewritten by a migration run during this task; tests use isolated temporary timelines. Native integration is complete. The initial 49-case PlayMode run passed38; seven E2E and three ZoneExitFlow fixtures needed45-second deadlines for the real35-second opening, and the old Rock assertion needed the actual director-owned pulse target. Targeted follow-up75340202b238 passed14/14. The final pressure-frame capture also passed. The latest result per distinct selected case is50passed/0failed, including all12 Zone3 cases, all11 PHOTO/manual SEND sites, normal/hidden endings, physical Zone4 travel, save rollback, Continue/rest/Journal, prior Zone1/2 presentation and selected tutorial coverage. EditMode passed98 with one optional Kino skip. These are selected native runs, not a claim that the entire repository suite ran green. Full results and scope: [the Zone 3 results report](../Zone03Cinematic/results.md).

All four project assemblies compiled offline with zero errors before native installation. This checks C# APIs against recorded Unity references; the native import and later test runs provide separate evidence. Exact offline inputs/results are in `Docs/QA/Zone03Cinematic/offline-build/`.
