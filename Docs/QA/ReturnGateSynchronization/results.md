# Return-gate synchronization repair — 2026-10-25

## Confirmed cause

The user's corrected placement was committed in `9395126` (`add return gate`,
2026-10-15) and retained by reference branch `codex/unified-effect-director` at
`43b1a26`. That commit includes the explicit requested/after table in
`Docs/AI/ReturnGatePlacement-2026-10-14.md` and placement regression tests.

The selective mission integration into `b28dcc9` preserved `3862225` main's
earlier geometry, including these four fields, and omitted the placement document
and tests. The earlier synchronization report explicitly records that geometry
preservation. This was an omitted branch change, not a later runtime/save reset.
Read-only Unity inspection matched the saved obsolete asset coordinates and
reported all maps clean before this repair. No runtime/editor writer that resets
`returnArea` was found in project source.

## Scoped correction

| Field | Before repair | Restored user request |
| --- | --- | --- |
| Zone02 forward entry | (45,595) | (100,900) |
| Zone02 return | (205,635) | (100,1000) |
| Zone03 return | (125,215) | (25,25) |
| Zone04 return | (455,950) | (100,1000) |

Only these four serialized coordinates changed in map assets via Unity Editor
APIs. Each return radius remains 75. POIs, depths, terrain, forward exits, backward
arrivals, mission/capture requirements, audio and scene bindings are preserved.
Current branch at entry and completion: `codex/zone02-experience-loop`, HEAD
`b28dcc9`; no checkout, commit or push was performed by this repair.

The local Zone02 far-tangent-plane workaround depended on the obsolete gate being
across the survey corridor. With the restored corner gate, that rule would block
a normal approach from the playable interior. It was removed. All return gates
retain the newer outside-observation then physical-reentry requirement, so saved
positions inside cannot turn a nudge, turn, depth change or Continue into travel.
The intentional Zone03 spawn overlap remains safe. Existing voyage revisions,
unlock/reset handling, transaction, resource and progression authority remain.

The omitted placement document and tests were restored selectively from `43b1a26`.
RoundThreeNavigationTests now asserts the explicit four-coordinate contract and
the requested Zone03 overlap. Newer ZoneTwoReturnIntentTests and existing
DebugStart/E2E approaches were adapted to restored geometry without discarding
their saved-inside, resource, Journal, legacy migration or no-loop assertions.

## Native validation

Unity 6000.4.2f1, Windows Editor/AnkleBreaker MCP port 7890, project `D:/G10`.

| Run | Job | Result |
| --- | --- | --- |
| Restored placement contracts before asset fix | 6e2e9bff2f05 | 0 passed / 2 failed; expected entry (100,900), actual (45,595), expected Zone03 overlap absent |
| Same EditMode contracts after fix | a4d32c031d94 | 2 passed / 0 failed, 0.79 s |
| Selected PlayMode regressions | f4aab5b215b1 | 8 passed / 0 failed, 123.57 s |

PlayMode covers all three authored markers after DebugStart/Continue, no immediate
Zone03 bounce, Continue/Rest inside Zone03 followed by physical reentry, Zone02
real-navigation travel away from entry then return, Continue/Journal inside Zone02
without invented arrival, saved outside approaches, all three return-trip pairs
with resources/timeline/cargo preserved, and legacy destination coordinate
migration. Exact native results are in `baseline-editmode.json`, `editmode.json`
and `playmode.json`. Tests use isolated temporary save/photo folders; this repair
does not migrate, reset or teleport the player's existing voyage.

Read-only candidate checks independently verified footprint-clear water and
terrain connectivity for each restored gate and the Zone02 entry. Current maps
are clean after native validation. C# compilation errors: 0. Editor stopped on
clean Bootstrap. Dynamic font-cache output was restored to the exact task-entry
snapshot, preserving preexisting font edits; TimeManager matches entry bytes.
Map diff contains exactly the four coordinate replacements. Existing unrelated
Zone02 work remains local and untouched by this repair except its return-approach
workaround/tests described above.

Native render captures: `D:/G10/Temp/TestCaptures/return-gate-zone2.png`,
`return-gate-zone2-1366.png`, `return-gate-zone3.png`, `return-gate-zone4.png`.
The Zone02 capture was visually inspected: gate center is at (100,1000) near the
upper-left chart corner with the outlined radius. These captures are temporary
local test artifacts, not an entire-game visual certification.

No standalone build, entire repository suite, fresh horror pacing measurement or
human gameplay review was performed for this coordinate repair. The earlier
Zone02 experience-loop baseline used obsolete placement and remains historical;
its blocker report is annotated as superseded.
