# Spawn and return-gate placement — 2026-10-14

> Imported from `codex/unified-effect-director` / `43b1a26` on 2026-10-25.
> Commit `9395126` contains the original user-requested four-coordinate correction.
> Those corrections were omitted from the selective integration into `b28dcc9`;
> the current repair restores them without replacing mission/map assets wholesale.
> Test results below are historical source-branch evidence. Current repair results
> are recorded separately in `Docs/QA/ReturnGateSynchronization/results.md`.

## Request and baseline

Interpreted the user's `100 - 900` notation as positive `(X, Y)`, not a negative Y.
Baseline HEAD: `587d78bcc5cb5b43d639b3b1b2605cff24af742a`;
branch `codex/state-qa-hardening-2026-10-12`, Unity 6000.4.2f1, MCP 2.39.7/7890.
Unity was initially playing; asset edits and tests waited until Play Mode stopped.
Existing Resources test-artifact deletions and PlayerSettings edits were preserved.
Dynamic font cache changes produced by Unity tests were not reset.

| Configuration | Before | Requested / after |
|---|---|---|
| Zone02 forward entry | (45, 595) | (100, 900) |
| Zone02 return to Zone01 | (205, 635) | (100, 1000) |
| Zone03 return to Zone02 | (125, 215) | (25, 25) |
| Zone04 return to Zone03 | (455, 950) | (100, 1000) |

Only these four serialized coordinate values changed through Unity Editor APIs.
Return radius stays 75, forward exits/backward arrivals/depths/POIs/terrain/map
size remain unchanged. Existing saves keep their vessel positions; new entries
into Zone02 use the requested spawn, including Debug Start. Continue/Rest are
not repurposed to teleport an existing voyage to the new entry.

## Investigation

| Hypothesis | Evidence / discriminating check | Result |
|---|---|---|
| Zone02 missing configuration/overlay binding | Canonical cabin inspection: overlay enabled, config Zone02, above light layer; real Debug Start -> current map rendered the return diamond | Contradicted: marker existed |
| Old return region lies across normal survey travel and is hard to identify | (205, 635) is near the L1->L2 corridor, baseline render shows pale cyan marker blending with cyan art/grid | Location moved as requested; contrast strengthened |
| Requested Zone03 corner gate bounces fresh arrivals back | Existing spawn (11, 97) is 73.34848 from (25, 25), inside radius 75; cloned-map pre-fix PlayMode test moved .2s from entry and immediately requested backward travel | Confirmed production interaction |

Read-only terrain/footprint probes and BFS connectivity checks confirmed all
requested centers and the Zone02 spawn are clear water and connected to their
entries. Clearance from the nearest POI activation region, after subtracting
both radii: Zone02 317.003, Zone03 366.738, Zone04 99.510 units.

## Fix boundaries

- `ZoneMapConfig.ReturnOverlapsEntry` derives overlap from authoritative map
  coordinates; it adds no persisted field or save migration.
- `ExpeditionProgression.ObserveArrival` requires an outside observation before
  a return region overlapping entry can arm. This also applies after Continue,
  Rest and voyage restoration. Real reentry still performs backward travel.
  Separated return gates, forward exits and final gates keep their existing
  fresh-movement behavior. Progress/resources/receipts are not granted here.
- The current map return prompt explains leaving/reentering an overlapping
  region. Historical maps show READY + coordinates, not a fictitious vessel at
  (0, 0) inside the new corner gate.
- `PhotoSurveyMap` adds dark outlines to return diamonds/arrows and circle
  outlines, with a clearer cyan stroke. The displayed circle still derives from
  the exact gameplay radius. Corner circle strokes are clipped to the chart,
  preventing them from drawing across coordinate labels; trigger area is not
  clipped or shrunk.
- Render inspection detected the top-edge return tooltip covering the new
  Zone02 marker. Return cards reserve circle clearance and flip below a top-edge
  marker when clamping above would cover it. Other task-card positioning is
  unchanged. Cards/overlays remain passive and do not block movement input.
- Helm preview remains artwork-only, without grid, vessel or route markers.

The Unity bug-investigation/uGUI guidance led to isolated reproduction and
targeted state-owner/presentation fixes, not a scene/UI rebuild or terrain edit.

## Tests and execution ledger

All gameplay fixtures use GUID temporary save/photo folders before loading
gameplay. New tests use real Debug Start, UI map routing, navigation, Save,
Continue, Rest and gate transitions, not injected completed objectives.

New coverage: one EditMode coordinate contract, three PlayMode placement/
overlap/rest-continue cases, two return rendering/prompt/corner clipping cases.
Existing terrain/POI separation checks and three-pair return regression were
extended without removing resource, cargo, day, persisted-progress or arrival
assertions. The explicit requested overlap replaces the old assumption that
every return center must be outside entry; an integration guard test now checks
the safe behavior of that overlap.

| Run | Job | Result | Duration | Log |
|---|---|---|---:|---|
| Existing terrain baseline | 9e0a41dada97 | 1 PASS / 0 FAIL | .78s | baseline-editmode.xml |
| Existing all-three return-trip baseline | 31a7d2ea4eba | 1 PASS / 0 FAIL | 28.04s | baseline-return-trips-playmode.xml |
| New tests before production/config fix | 92d170264790 | 0 PASS / 3 FAIL | 6.43s | prefix-new-cases-playmode.xml |
| Focused updated terrain/config | 169097f56437 | 2 PASS / 0 FAIL | .84s | focused-editmode.xml |
| First broad post-fix | e2b86e6901f7 | 34 PASS / 1 FAIL | 135.27s | postfix-first-playmode.xml |
| Final broad post-fix | a89205dd712d | 36 PASS / 0 FAIL | 138.88s | regression-playmode.xml |
| Full EditMode regression | 4435ecff1740 | 64 PASS / 0 FAIL | 3.72s | regression-editmode.xml |

Final verification: 100 PASS / 0 FAIL / 0 skipped / 0 INCONCLUSIVE in the
final selected suites (not adding repeated executions). PlayMode groups:
DebugStart 3, existing three-pair ExpeditionEndToEnd 1, HelmMapPreview 3,
ReturnGatePlacement 3, ZoneExitFlow 14, ZoneExitMap 12.

Pre-fix failures: two coordinate-contract mismatches and the confirmed immediate
return from movement at the requested overlapping Zone03 spawn.
The first post-fix failure was a test-driver assumption: it tried to leave the
corner gate heading east, but the authored contour blocks that ray 30 units
away. A read-only probe found north clear; the driver now searches footprint-
clear rays, physically leaves the region, turns and physically reenters.
No terrain/gameplay assertion was weakened to bypass that wall.

Exact MCP commands:

```text
unity_testing_run_tests(mode="EditMode", filter="RoundThreeNavigationTests")
unity_testing_run_tests(mode="EditMode")
unity_testing_run_tests(mode="PlayMode", filter="RoundThreeReturnTripsPreserveTimelineResourcesAndDoNotLoop")
unity_testing_run_tests(mode="PlayMode", filter="ReturnGatePlacementPlayModeTests")
unity_testing_run_tests(mode="PlayMode", filter="ReturnGatePlacementPlayModeTests|ZoneExitMapPlayModeTests|ZoneExitFlowPlayModeTests|DebugStartPlayModeTests|RoundThreeReturnTripsPreserveTimelineResourcesAndDoNotLoop|HelmMapPreviewPlayModeTests")
unity_testing_get_job(jobId=..., includeFailedOnly=true, waitTimeout=20)
unity_get_compilation_errors(severity="error")
```

Logs: `D:/G10/Logs/ReturnGatePlacement/`, copied from native NUnit TestResults.xml
after each completed run. Baseline visual: `baseline-zone2.png` in that folder.
Post-fix real Canvas render captures in `D:/G10/Temp/TestCaptures/`:
`return-gate-zone2.png`, `return-gate-zone2-1366.png`, `return-gate-zone3.png`,
`return-gate-zone4.png`.

Compilation: 0 errors. All three requested return markers rendered with clear
outlines; native render captures confirm Zone02's card no longer covers its
return circle and Zone03's clipped region no longer crosses the chart axes.
No scene asset changes, and no art/POI/terrain/forward-exit edits. Script diff
whitespace check passes. Full diff check passes with Unity serializer blank-line
whitespace excluded (`git -c core.whitespace=-blank-at-eol diff --check`).
Windows build, human playtest and full
PlayMode suite: NOT RUN for this scoped change. No commit/push/merge requested
for this turn; the earlier push request was already fulfilled before these edits.
