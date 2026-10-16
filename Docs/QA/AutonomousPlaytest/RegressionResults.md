# Current-run validation

No new production P0/P1 fix was justified by the capture investigation, so there is no claimed before/after defect regression. Earlier repository test totals are historical and are not results of this run.

Executed:

- Fresh Unity compilation after saved Editor restart: zero errors; new Editor transport type loaded.
- Isolation callback: normal timeline and archive both under unique run folder before New Game.
- Bounded input: actual forward, turn, depth, keyboard Escape, computer menus and SEND traversed production input/UI.
- Mode A photo/SEND, low-energy Rest and Save/Continue: observed actual UI outcomes, captures in PlaytestReport.
- Mode B original capture rejection: reproduced unchanged after Continue; diagnosed exact range/depth. Corrected player approach opened the minigame.
- Real-time capture keyboard controller: earned hits through the normal Update/InputSystem route, returned Caught and one cargo item; no direct Tick, position, completion or reward call.
- Longer helm controller: arrived through ordinary pointer holds and released controls; position/readout screenshots plus half-second telemetry.
- Zone01 progression earned through camera/SEND, capture and upgrade UI. Forward Zone01→02 and backward Zone02→01 gates traversed through helm input.
- Journal Day4→end-Day2 removed abandoned future objectives, recipe, upgrade, Zone02 history and gallery entries; retained L1 photo/L2 item exactly. Rest plus actual camera/SEND/module replay succeeded; subsequent Save/Continue retained 4 objectives and 4 unique receipts. One full rollback/replay cycle; not a claim about all checkpoint shapes.

Pending: final cleanup verification, broader interaction edge cases, forward/backward zones, rollback replay, final prerequisites/ending. Unexecuted scenarios must retain NOT RUN/BLOCKED labels. Do not use these scoped passes as an end-to-end certificate.
