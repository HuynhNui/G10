# Prioritized Given / When / Then matrix

Baseline `13a859a`. Each row includes initial state (Given), operation (When), persisted/runtime/UI expectations (Then). `existing` means run the named suite rather than duplicate it; `new` identifies coverage gaps. Latest execution statuses belong in RegressionReport, not inferred from this design.

Abbreviations: E2E=ExpeditionEndToEndPlayModeTests; PS=PhotoSubmissionPlayModeTests; PD=PhotoDeletionPlayModeTests; EL=ExpeditionLoopPlayModeTests; VD=VesselDeathPlayModeTests; EP=ExpeditionEconomyPlayModeTests; ZE=ZoneExitFlowPlayModeTests. All setup uses unique temporary timelines/archives; engineered fault states never edit real save files or mission assets.

| ID / priority | Given | When | Then: saved state / runtime / UI | Coverage |
|---|---|---|---|---|
| P0-01 | End-Day4 Zone02 L3 neither objective; real Day5 PHOTO+CAPTURE | Journal restore 4 | JSON drops future PHOTO/CAPTURE/receipts/cargo/images; runtime exact checkpoint, content present; checks revert, replay + Continue remains correct | existing E2E RoundThreeJournalDayFour… |
| P0-02 | Day4 checkpoint PHOTO-only, Day5 capture complete | Restore 4, capture again | PHOTO remains, CAPTURE/receipt/cargo future removed; capture permitted, exactly one new stack/receipt; PHOTO UI remains checked | existing E2E RoundThreeJournalKeepsEarlierPhoto… |
| P0-03 | Day4 both complete, Day5 repeat captures | Restore 4 | Original receipt/cargo quantity retained, future stacks gone; repeat capture still works; no duplicate check/reward | existing E2E RoundThreeJournalKeepsEarlierCompletion… |
| P0-04 | Photo candidate created by camera; coherent saved unsent state | SEND with parent-path I/O failure | No objective/receipt/cargo/counter or submitted flag survives; primary/backup/archive unchanged; lab error and SEND retry available | new PS failed-SEND |
| P0-05 | Same candidate; existing primary locked against replace | SEND after temp flush | Same atomic rejection, no transient success notification; retry commits exactly once; Continue exact state | new PS failed-SEND |
| P0-06 | Clone-only PHOTO rule has item + research + recipe + flag + reveal + zone + ending rewards | Controlled failed SEND then retry | Every mission list, inventory stack and cumulative counter rolls back; no rewards-on-restore; receipt IDs unique after one success | new PS reward fan-out (engineered authority fixture) |
| P0-07 | Valid candidate, writable timeline but sidecar locked | SEND | Timeline is submitted and objective committed; no false rollback of durable progress; explicit ancillary warning; Continue restores committed state, no duplicate reward | new PS archive failure |
| P0-08 | Candidate, wrong repository object / changed zone/target/objective ID | SEND | No mutation or charge; saved candidate stays unsent; SEND disabled/unavailable | existing PS SubmissionRevalidates… |
| P0-09 | Two valid candidates for one objective | SEND both / duplicate callbacks | One objective and one receipt; second candidate not falsely marked sent; button reports already completed | existing PS DuplicateCandidates… |
| P0-10 | Zone02 capture objective requires PHOTO, image taken but unsent | Try capture, SEND, try capture | Before SEND no cargo/charge/minigame; after SEND actual minigame completion creates one stack; required status correct | existing PS RealZoneTwoCapture… + E2E |
| P0-11 | Day4 Zone01 checkpoint, future Zone02 fieldwork | Rollback → Continue → Rest → death | Future zone removed; canonical logical zone/providers correct; Rest creates genuine start, death copies that exact start; future maps lose checks | existing E2E cross-zone + VD; new historical stress |
| P0-12 | Day4 complete snapshot with missing receipts | Restore progress only | Missing receipts/cargo stay missing, no EvaluateRules on restore; restored checks follow snapshot | existing E2E RoundThreeRestoreProgress… |
| P0-13 | Valid current/backup, changed runtime | Failed same-zone Journal publish | Primary + backup unchanged, previous runtime/photo/cargo restored; no truncated history/UI leak; retry remains possible | manual variant NOT RUN; new automated fault is cross-zone |
| P0-14 | Valid checkpoint in another zone | Failed cross-zone Journal save | No scene transition/publication, original zone/history/runtime retained; error visible | new E2E historical stress, three repetitions |
| P0-15 | Corrupt primary, valid backup | Continue | Reads backup with recovery notice, normalizes/validates; no invented progress, binding coherent | existing EL AtomicSave… / PD corrupt recovery |
| P0-16 | Both save copies corrupt / invalid version-5 quantities or levels | Read/Continue | Failure preserves both files, no reset/overwrite; gameplay blocked/error | existing EL, economy invalid data; unsupported future-version variant NOT RUN |
| P0-17 | Legacy versions 1–4, representative progress signals and purchased/quantity fields | Load → write → reload | Version5 compatible defaults/aliases, canonical purchased stats in every snapshot, no second cost/refill/receipt | existing EL/VD/Economy/TutorialSave; representative shapes, not every version × field combination |
| P0-18 | DELETE submitted photo retained in several checkpoints | Confirm delete → rollback/death/Continue | ID removed current/day-start/Journal/backup/archive; objectives/rewards/counters retained; no resurrection | existing PD SubmittedDeletion… |
| P0-19 | DELETE candidate and file staging/publish blocked | Confirm | Exact original image/texture/selection/timeline retained; no accidental mission completion, feedback/retry | existing PD failure trio |
| P0-20 | Journal restores discarded future photo; loose archive still exists | Continue | Timeline image list authoritative; loose future photo does not resurrect; gallery/objectives exact checkpoint | existing E2E/EL, new stress |
| P1-01 | Visited Zone01, unlocked Zone02 | Return Zone02→01 via authored gate | Exact backward arrival/heading, carried resources/day/cargo; destination receipts intact; no immediate forward loop | existing E2E RoundThreeReturnTrips… |
| P1-02 | Visited Zone02, Zone03 | Return Zone03→02 | Same invariants, arrival outside forward gate and depth clamped only if required | existing E2E |
| P1-03 | Visited Zone03 with destroyed rock, Zone04 | Return Zone04→03 | Alternate terrain persists, no event replay, exact cargo/receipts; movement needed for next travel | existing E2E |
| P1-04 | Inactive destination has coordinate version 0/1/2 | Return then Continue | Old spawns migrated once; new backward arrival never scaled twice; map marker at correct world location | existing DebugStart ReturningToLegacy… |
| P1-05 | Revisit, loaded inside gate or restored voyage | Turn/change depth/no XY, then move | No auto-travel from stale saved distance; fresh XY enables arrival once | existing ZE/E2E |
| P1-06 | Historical Zone01/02 viewed while in later zone | Open map before/after rollback repeatedly | JSON byte-identical unless legitimate operation; no receipts/items/reveals; checks mirror current timeline, not abandoned future | existing history + new E2E stress |
| P1-07 | Stackable material, 12 distinct cargo slots | Capture/consume/upgrade | Existing stack can grow, new distinct ID cannot overflow; insufficient recipe costs change nothing; quantities remain positive | existing Economy / Capture |
| P1-08 | Day/resource limits; zero energy away from rescue | Rest twice → Continue | One day each, refill in place; no implicit rescue relocation, voyage/cargo/objectives retained | existing EL RestUi… |
| P1-09 | Upgrade affordable, primary save unwritable | Purchase | Ship/level/cost cargo revert; old JSON and UI values intact; retry consumes once | existing EP |
| P1-10 | Rock prerequisites/range/depth satisfied, failed save | Trigger breaker | Objective + world flag revert; no effect/terrain change/receipt; retry once | existing RockBreaker |
| P1-11 | Rock committed and presentation running | Rest/travel/upgrade/capture/Journal or pause/disable/Continue | Major actions blocked, timer pauses, cleanup restores frame/input; durable rock survives without replay | existing RockBreaker |
| P1-12 | Valid day-start with unsent then submitted photo | Death on same day / after Continue / after zone travel | Exact genuine day-start resources/objectives/cargo/photos, no extra day; active/pending minigame cancelled | existing PS/VD |
| P1-13 | Legacy or restored Journal has no genuine start | Death then Continue + Rest | Explained menu return, no fabricated snapshot/no dead-state save; Rest establishes subsequent start | existing VD LegacyDeath… |
| P1-14 | Day25 still playable; day26 without ending | Rest then restore playable day | Deadline locks every gameplay device; Journal recovery releases lock; day budget is global | existing EL/EP |
| P1-15 | Valid candidate, overlay/scene transition/death | Invoke stale SEND callback | No objective/archive/JSON mutation or extra charge; lab never claims submitted; resume later can SEND | new PS interruption tests |
| P1-16 | Delayed/direct or active capture | Cancel/disable/rebind/death | No delayed second charge or capture reward; art, modal, input released | existing DirectCabinInteraction/Capture/VD |
| P1-17 | Finite photo/capture/radar allowance | Deplete, invalid retry, Rest | Charge costs unchanged; invalid requests don't consume; recovery available, required objective not softlocked | existing EP/Tutorial/Capture/feedback |
| P1-18 | Valid device prerequisites and affordable upgrade; pause open | Call camera/radar/capture/navigation and Rest/Journal/upgrade/ending authorities directly | No charge, voyage, day, cargo, ending or JSON change; resume releases guard | new PS paused-device + EL paused-authority |
| P2-01 | Tutorial active/completed, rollback/New Game | Journal/death/reset | Knowledge persists except explicit reset; no repeated listeners; actual SEND success, not candidate/failure, advances PhotoLab step | existing Tutorial + new SEND neighbor regression |
| P2-02 | Photo delete confirmation active | SEND/navigation/reopen | Modal wins; no submission/deletion of wrong ID; reopening clears pending operation | existing PD/PhotoLab |
| P2-03 | Ending decision modal | Cancel/reopen/select repeatedly | No implicit choice; saved explicit choice once; hidden route/reveals follow choice, failure cannot fake ending | existing Dialogue/E2E |
| P2-04 | Rest/Journal confirmation or scene fade | Double-click/interrupted operation | No repeated day/reward/travel; fade/modal restored; errors visible | existing EP/EL/ZE |
| P2-05 | Mission lists/cargo changed then UI reopened | Refresh/scroll/map switches | Correct counts/icons, no mutation by display; scroll clipping/selection coherent | existing Cargo/WorldMap/MissionLog/feedback |
| P2-06 | Isolated DEBUG runs, normal timeline sentinel | Four presets → Continue Debug → normal Continue | Normal bytes unchanged, optional regular MAX does not replace mandatory module; no debug control in release policy | existing DebugStart; release player verification manual |

## Pairwise selection

Rather than multiply every axis, combine: checkpoint (none/PHOTO/both) × operation (Journal/Continue/death) in existing tests; travel (three pairs) × restore/resource/migration state; persistence fault (parent creation/primary replace/sidecar write) × receipt/UI/restart; interruption (pause/transition/death) × candidate SEND. Repeat historical rollback three times with actual camera/SEND/capture, Rest, failed/successful Journal, Continue and death. No assertion may be satisfied by injecting completed objectives into a test labelled gameplay.

There are 44 matrix rows. They are scenario families, not NUnit test counts. A family with an explicitly manual variant must not be represented as exhaustively tested merely because its automated neighbor passes. Individual NUnit outcomes and unresolved/manual variants are separated in RegressionReport and ManualQA.

## Explicitly non-automated boundaries

Power loss/process kill between filesystem steps, genuinely inaccessible ACL/storage/network-drive errors, human difficulty/audio/readability and standalone-player release guards require ManualQA. Engineered file locks validate deterministic failure paths, not every possible OS storage outage.
