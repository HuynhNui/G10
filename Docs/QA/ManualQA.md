# Manual QA and non-automated boundaries

This pass runs Windows Unity Editor tests against isolated temporary timelines/archives. It does not use the player's timeline. A compile/build success is not a human playtest, and deterministic vessel positioning/minigame steering is not human difficulty evidence.

## Safety setup for a future manual session

Use a development/debug timeline with a separate archive, or a disposable Windows user/profile. Do not launch a normal standalone player against the current user's persistentDataPath: runtime save overrides are in-process test settings, not a documented command-line isolation flag. Do not delete/reset the player's save to make QA convenient. Snapshot the disposable timeline only. No real-save inspection or repair was performed here.

## Manual cases

Every case below is **NOT RUN** in this pass; these are procedures and expected outcomes, not completed verification. The Windows build result, if produced, is separately recorded in RegressionReport.

| ID | Given / When | Then / observations to record |
|---|---|---|
| M01 | Disposable timeline, valid photo; genuinely unwritable archive or timeline via ACL/offline drive, SEND and retry | Exact unsent state on rejected timeline, no cargo/receipt leak; visible retry error. Ancillary archive failure leaves committed SEND intact with warning. Record runtime, both JSON copies, archive and restart. Engineered file locks already have automated coverage. |
| M02 | Disposable primary/backup, terminate process/power at temp flush, replace and deletion quarantine boundaries | Continue recovers a complete valid timeline or explains failure without overwriting copies; no partially pruned permanent deletion. Process-kill/power-loss not simulated. |
| M03 | Same-zone Journal checkpoint; deny publication, confirm restore, retry | Old runtime/photo/cargo/history and both files retained, error visible, successful retry exact checkpoint. Automated new Journal fault covers cross-zone, not this same-zone variant. |
| M04 | Disposable unsupported future-version timeline and backup | Continue rejects without reset/overwrite; clear error. Corrupt/invalid v5 and legacy v1–4 representative states have automated coverage; future-version rejection is not claimed tested. |
| M05 | Sail manually through Zone02→01, Zone03→02, Zone04→03, then Continue/Rest/Journal | Backward arrivals outside forward gates, no instant loop, carried resources/receipts intact, coordinates legible. Automated all-three gate/state tests are not a human sailing test. |
| M06 | Day4 checkpoint, Day5 actual L3 camera→SEND→capture→Rest→Journal→Continue; repeat PHOTO-only checkpoint | Human-selected images/objectives/cargo/checkmarks reflect exact restored timeline; unsent image cannot satisfy capture. Automation covers these state oracles; record actual user navigation/feedback. |
| M07 | Human keyboard input in Zone01–04, depleted budgets, screen shake on/off | Judge capture difficulty/fun, deadline recovery, rock audio/pacing/accessibility. No balance changes made during QA. |
| M08 | Different resolutions/UI scales, Photo Lab failure/retry/DELETE, pause/transition overlays | Error text readable, preview not overlapped, modal ownership and selection consistent. Automated screenshot/TMP assertions inspected only the test resolution. |
| M09 | Release Windows player in isolated profile, Main Menu and ending | DEBUG controls/selection unavailable in release; normal Continue/ending/restart works. Editor compile policy checks do not prove built-player behavior. |
| M10 | Active minigame/delayed direct interaction, focus loss/window close during fade/death | No double rewards/charges, modal/input released and persisted state consistent; record exceptions and actual focus behavior. Automated cancel/disable/rebind/death paths exist, OS/window lifecycle variants remain manual. |

## Visual inspection actually performed

**PASS (one scoped inspection):** generated screenshot `D:/G10/Temp/TestCaptures/qa-send-rejected.png` inspected after focused tests. Photo Lab retains existing panel/artwork; rejection text fits the existing status row, preview unobstructed, SEND visibly available for retry. This is not counted among M01–M10 and is not a human end-to-end playtest.
