# Prerequisite repair: unintended Zone02 return during forward navigation

> Superseded placement/approach correction, 2026-10-25: this historical baseline
> used the obsolete main gate `(205,635)` and entry `(45,595)`. The user's earlier
> requested gate `(100,1000)` and entry `(100,900)` were recovered from `9395126`.
> The far-tangent-plane workaround described below is removed because it blocks
> approach from the playable interior to that restored corner gate. Current
> returns require an outside observation followed by physical reentry; restoring
> inside still cannot turn a nudge into travel. See
> `../ReturnGateSynchronization/results.md` for current coordinates and tests.

This is a functional blocking-defect repair, separated from the horror redesign. The original baseline remains preserved. No Zone02 presentation, audio, mission, map, costs, coordinates, capture requirements or exit geometry changes belong to this repair.

## Observed reproduction

The measured baseline used the real Zone02 debug preset and normal player-equivalent inputs. Entry was `(45,595)` at230m. L1 PHOTO → actual Photo Lab SEND → CAPTURE completed. The subsequent forward departure from approximately `(21.9,597.7)`, heading77.8°, holding W for7.4s automatically returned to Zone01 at `(1735,680)`. Recording stopped in Zone01 at509.932 real elapsed seconds. This is an interrupted baseline, not a completed Zone02 voyage or completion-time measurement.

Evidence: `baseline/evidence/timeline.jsonl`, `frames.jsonl`, `frames/`, `actual-game-audio.wav` and `summary.txt`. The coordinator observed the runtime reproduction. Read-only mask analysis independently found the return region across the corridor; this static analysis did not substitute for the recorded reproduction.

## Cause and bounded correction

The authored return circle is `(205,635)`, radius75. The entry lies outside it. Previously, `ExpeditionProgression.ObserveArrival` armed every separated return gate immediately when observing any outside position. `Evaluate` then treated the first forward crossing of this unavoidable circle as intentional return travel because Zone01 had legitimately been visited.

The return gate now requires an observed approach on the interior side. For an entry separated from the gate, this means reaching beyond the circle's far tangent plane along the entry→return-center direction. The initial outbound crossing stays unarmed; entering the same existing circle while backtracking from the interior activates the existing return transaction. The flag remains runtime-only. Saved interior positions can arm on observation; restoring inside or on the entry side cannot manufacture an arrival. Existing voyage-revision, Journal, unlock/reset and failed-transition retry handling remains in use. Entry-overlapping return regions retain their previous leave/reenter policy. Forward and final gates retain their existing movement policy.

Changed production source: `Assets/_Project/Scripts/UI/ExpeditionProgression.cs`. No new package, serialized field or save migration is required.

## Functional verification prepared

`ZoneTwoReturnIntentTests` uses the existing debug UI/preset and actual authored Zone02 terrain. Its direct navigation calls and deliberate restore setup are functional test drivers, not experience-playtest evidence. Three cases cover:

- Outbound crossing through the original circle without transition, followed by intentional inbound return with day/progression preservation and no loop.
- Restore inside the gate, movement/turn/depth nudges, Rest and Journal rollback; travel to the interior remains able to arm.
- Save/Continue on the entry side and interior side; only actual inbound entry activates return.

Coordinator native validation: zero C# compilation errors; PlayMode job `d0272943d54e` passed all three focused cases (11.43 seconds). Exact results are in `return-repair-tests.json`. Test duration is not voyage time. Baseline continuation retains its existing save: Zone01 after unintended return, day1, energy72.3475, Zone02 L1 PHOTO/CAPTURE complete. Original horror controller/assets are unchanged. Real-input corridor replay remains part of that continuation. Final comparison must distinguish this prerequisite gate repair from horror presentation improvements.

## Existing regression expectations that change

Two older functional tests intentionally restored directly inside a separated return circle and expected a small forward nudge to return. That shortcut encoded the defect and must be replaced by restoration on a clear interior-side outside approach followed by physical entry:

- `ExpeditionEndToEndPlayModeTests.RoundThreeReturnTripsPreserveTimelineResourcesAndDoNotLoop`: separated-entry `else` branch near line516.
- `DebugStartPlayModeTests.ReturningToLegacyInactiveZoneDoesNotScaleNewArrivalTwice`: restore-to-center and nudge near line121.

Their resource, timeline, migration and no-loop assertions remain applicable. Both fixtures now choose a clear interior-side outside approach and physically enter the same authored circle; additional assertions require an armed return and actual boundary crossing. Existing cargo/save/migration assertions were preserved. Forward/final test approaches were not changed. All current Zone02/03/04 return entries are separated, so exercise all three return routes during regression. An overlapping-entry fixture remains useful for the preserved legacy policy.
