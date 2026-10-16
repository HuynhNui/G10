# Return-gate recovery — 2026-10-16

## Confirmed cause

The user's old Windows build `G:/1` contains the requested return-gate placement.
The source currently open in Unity did not: `codex/unified-effect-director` was
created at `587d78bcc5cb5b43d639b3b1b2605cff24af742a`, before the return-gate commit.

That commit still exists locally as
`93951261e6c2e2a5370a6ac042e01e797140d8e3` (`add return gate`), whose parent is
`587d78b`, on `codex/state-qa-hardening-2026-10-12`. Local `main` and the locally
recorded `origin/main` also point to `587d78b`. No remote fetch/push was performed.
This was a missing branch change, not a cache problem or a player-save reset.
The EffectDirector task selected the earlier baseline and omitted this change.

## Source/build comparison (read only)

| Value | Source before recovery | Old build G:/1 / recovered value |
|---|---|---|
| Zone02 entry | (45, 595) | (100, 900) |
| Zone02 return → Zone01 | (205, 635) | (100, 1000) |
| Zone03 return → Zone02 | (125, 215) | (25, 25) |
| Zone04 return → Zone03 | (455, 950) | (100, 1000) |

The old build was not launched, rewritten, imported into Assets or used as a
source of DLLs/scenes. A focused binary inspection found each serialized
`zone0N-return` identifier in `G10_Data/sharedassets1.assets`, followed by the
aligned little-endian X/Y/radius floats:

| Identifier | File byte offset | X | Y | Radius |
|---|---:|---:|---:|---:|
| zone02-return | 2529328 | 100 | 1000 | 75 |
| zone03-return | 2552332 | 25 | 25 | 75 |
| zone04-return | 2589488 | 100 | 1000 | 75 |

The Zone02 record also contains the (100, 900) entry immediately before its gate
fields. `G10_Data/Managed/G10.Prototype.dll` contains the
`get_ReturnOverlapsEntry` method name, which is absent in the pre-recovery source
and present in commit 9395126. This is a targeted placement/metadata check, not a
claim that every compiled feature has been fully decompiled or compared.

Build files SHA256 (checked without modification):

- `sharedassets1.assets`: `492516428BB2204177FCA994B8FF732AF9F7C36790E2CAC6EEA4BB31FF055989`
- `Managed/G10.Prototype.dll`: `558D997409281763677CE57507650FC1634D51A6F93BE7E3447353511891F09D`

## Recovery scope

Recovered only the requested feature from 9395126, on the existing working
branch, without switching/resetting branches or cherry-picking unrelated files:

- The four coordinate values in `Content/Maps/Zone02.asset`, `Zone03.asset`,
  `Zone04.asset`, authored with Unity Editor APIs/Undo/SaveAssetIfDirty.
- `Scripts/Navigation/ZoneMapConfig.cs`: derived `ReturnOverlapsEntry` property.
- `Scripts/UI/ExpeditionProgression.cs`: overlapping spawn/return regions require
  leaving the region before reentry can trigger return; explicit feedback text.
- `Scripts/UI/PhotoSurveyMap.cs`: clearer return outline/arrow/circle, chart-edge
  clipping, historical-map prompt and top-edge tooltip clearance.
- Existing coordinate/terrain, map mesh and three-pair return-trip regression
  assertions, plus the original `ReturnGatePlacementPlayModeTests` and metadata.
- [The original implementation report](ReturnGatePlacement-2026-10-14.md), as a
  historical record; its old test runs are not claimed as this recovery's runs.

Tracked source/map files above match the selected 9395126 versions. The recovered
new PlayMode test's Git blob is `7147114d6503e0f1528c687155980fb17e4ad5a8`, matching
that commit. No old binary was copied into the project.

Excluded the old commit's unrelated PlayerSettings edits, Recovery scene and
performance-test artifact deletions. Preserved all pre-existing EffectDirector
work, dynamic font caches, generated files, AutonomousPlaytest files and other
dirty/untracked work. No package changes, commit, merge or push.

## Safety and intended behavior

All three requested centers and the new Zone02 spawn have navigable vessel
footprints and connected clear water. Nearest POI activation-region separation,
after subtracting both radii: Zone02 317.003, Zone03 366.738, Zone04 99.510 units.
Return radius remains 75; POIs, terrain, forward exits, backwards arrivals,
heading/depth rules and save schema are unchanged.

Zone03 retains its old entry (11, 97), which lies 73.348 units from the new
(25, 25) gate center, inside radius 75. Recovering only coordinates would cause
an immediate bounce on initial movement. The recovered guard requires an outside
observation before reentry, including after Continue/Rest/voyage restore. Gates
whose entry is outside the return region retain their original movement policy.

Existing saves keep the vessel's saved position. Continue/Rest intentionally do
not teleport it to the new entry. The Zone02 spawn change is visible on a new
forward arrival or Debug Start; return markers always read the current map asset.
All four scene map overlays were checked to reference their canonical map asset,
with raycastTarget=false and an existing RectMask2D ancestor. No UI hierarchy was
rebuilt. The uGUI guidance kept marker/hit-region/tooltip changes localized.

## Reproduction and verification

| Run | Job | Result | Seconds |
|---|---|---|---:|
| Existing terrain/navigation baseline | 27511fb70b5c | 1 PASS, 0 FAIL | .67 |
| Recovered original PlayMode tests, before production recovery | 52f911bbdadf | 0 PASS, 3 FAIL | 6.44 |
| Full EditMode after recovery | baa23550258b | 71 PASS, 0 FAIL/skipped | 4.30 |
| Combined return-gate and EffectDirector PlayMode regression | 7d4340cfebd3 | 64 PASS, 0 FAIL/skipped | 196.84 |

The three reproduced failures were the old Zone02/Zone03 coordinates and an
actual transition back from initial movement at a cloned requested Zone03 gate.
No production asset was changed to fake the pre-fix overlap reproduction.

The combined PlayMode selection covers current-map rendering/tooltip clearance,
new spawn, all three gates, Continue/Rest/Journal, clear-water travel and actual
reentry, existing resource/cargo/day assertions, historical maps, Helm preview,
Debug Start, EffectDirector modules, feedback, ambient hierarchy and rock breaker.
Gameplay tests isolate save/photo paths to GUID temporary directories before
loading gameplay. No real player save is used or overwritten.

Final selected regression total: **135 PASS, 0 FAIL, 0 skipped/inconclusive**
(71 full EditMode + 64 selected PlayMode; baseline and pre-fix runs are not added
to this total). Compilation errors: 0. Console errors: 0. Editor returned to
Bootstrap, outside Play Mode, not compiling, with a clean scene. All four map
assets were re-read through Unity after the run: the recovered values above are
present and none is dirty. The final source/map comparison against the selected
9395126 files is empty, and `git -c core.whitespace=-blank-at-eol diff --check`
passes (ignoring existing Unity YAML empty-value trailing spaces).

Inspected the newly generated Zone02, Zone03 and Zone04 captures: return markers
are at the requested coordinates; the Zone02 tooltip clears the top-left marker
and the Zone03 ring is clipped to the chart edge. Both old-build SHA256 values
were rechecked after validation and are unchanged. Baseline/final HEAD remains
587d78b: this recovery and the existing EffectDirector work are still uncommitted
on `codex/unified-effect-director`.

Exact MCP calls (provider port 7890):

```text
unity_testing_run_tests(mode="EditMode", filter="RoundThreeNavigationTests")
unity_testing_run_tests(mode="PlayMode", filter="ReturnGatePlacementPlayModeTests")
unity_testing_run_tests(mode="EditMode")
unity_testing_run_tests(mode="PlayMode", filter="ReturnGatePlacementPlayModeTests|ZoneExitMapPlayModeTests|ZoneExitFlowPlayModeTests|DebugStartPlayModeTests|RoundThreeReturnTripsPreserveTimelineResourcesAndDoNotLoop|HelmMapPreviewPlayModeTests|EffectDirectorPlayModeTests|CabinFeedbackPlayModeTests|CabinAtmospherePlayModeTests|RockBreakerPlayModeTests")
unity_testing_get_job(jobId=..., includeFailedOnly=true, waitTimeout=20)
unity_get_compilation_errors(severity="error")
unity_console_log(type="error", includeStackTrace="errors")
unity_editor_state()
```

Local raw NUnit results: `D:/G10/Logs/ReturnGateRecovery-2026-10-16/`, copied from
the native TestResults.xml after each completed run. Captures generated by the
placement fixture: `D:/G10/Temp/TestCaptures/return-gate-zone2.png`,
`return-gate-zone2-1366.png`, `return-gate-zone3.png`, `return-gate-zone4.png`.

A new standalone build, launching the old build, full PlayMode rerun and human
playtest are NOT RUN for this scoped recovery. The old build is preserved as
evidence. Changes are not yet committed/pushed; future branch work must include
the return-gate commit/change rather than restart from the older main baseline.
