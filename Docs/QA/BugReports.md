# State QA bug reports — 2026-10-12/13

Baseline production: `13a859a13326b016b32eb29b2ae3c23c5e2b3937`. All reproduction uses temporary timeline/archive paths, never the player's save. Test XML is in `Docs/QA/Logs`; exact commands and final outcomes are in RegressionReport.

## QA-001 — P0: SEND publishes completion despite failed timeline save

**Reproducer:** `PhotoSubmissionPlayModeTests.FailedSendKeepsRuntimeJsonArchiveAndUiRetryable`. In a real new-game camera/Photo Lab flow, take a valid candidate, save its unsent state, set the temporary save override to a child of the existing timeline file, then invoke the real SEND button. A second boundary locks the primary against atomic replacement. `FailedSendRollsBackEveryRewardListCargoAndCounter` additionally stages a clone-only seven-reward fan-out. `TutorialPlayModeTests.FailedSendCannotTeachPhotoLabBeforeDurableCompletion` follows actual onboarding/device actions.

**Expected:** rejected SEND returns false, leaves the photo unsent and all objectives/receipts/cargo/cumulative counts unchanged, emits no success notifications, preserves primary/backup/sidecar, remains retryable; a successful retry is durable exactly once and restores coherently on Continue.

**Actual before fix:** SubmitPhoto called RecordObjective, marked submitted and wrote archive metadata before SaveCurrent. SaveCurrent false was ignored and SubmitPhoto returned true. Pre-fix failed-SEND UI and tutorial tests found submitted=true; fan-out found return=true. Primary remained old while runtime/sidecar could contain abandoned completion. Tutorial mission observers ran before durability. The UI test stopped at the first parent-path failure; the primary-lock variant was fully exercised after the fix, not claimed as a second independent pre-fix failure.

**Root cause / mutation boundary:** detached SaveCurrent protects only its candidate, not prior caller mutations. Mission reward evaluation also synchronously emitted ItemAdded/Changed, exposing speculative state and incrementing the old cumulative capture counter.

**Smallest authoritative correction:** Loop owns CommitPhotoSubmission; MissionRuntime stages ordinary reward evaluation with exact prior-state snapshots; inventory buffers events and ItemAdded count. Failed publication restores mission/cargo/photo/timeline and discards notifications. Success includes the buffered count once and publishes notifications after commit. Sidecar follows the timeline. Restoration still never evaluates rewards.

**Changed files:** `Scripts/Computer/{ExpeditionLoop,PhotoCaptureService,ComputerDataContracts}.cs`, `Scripts/Missions/ZoneMissionRuntime.cs`, `Scripts/Navigation/CreatureInventory.cs` (all below `Assets/_Project`).

**Regression / verification:** pre-fix job `c1d2dfb935bc`, `prefix-reproductions.xml`; focused final job `ef8e69bd55f1`, 26/26 PASS including failure/retry/Continue, exact JSON and archive, unique seven receipts, one cargo stack/counter/event, tutorial only after durable SEND. Full final regression: 63 EditMode + 274 PlayMode PASS, including these tests.

## QA-002 — P1: pause blocks presentation but not gameplay authorities

**Reproducer:** open PauseMenuController, invoke an existing SEND callback; call Loop.CanRest/Rest/RestoreDay/TryPurchaseUpgrade/RecordEnding directly. Separately, `PausedDeviceServicesCannotConsumeChargesOrMoveVessel` opens pause at a valid device location and calls actual camera/radar/capture/navigation services.

**Expected:** no photo/objective/JSON/day/cargo/ending/resource/voyage mutation while paused; resume restores availability. This is a state-boundary test, not a claim that normal raycasts always pass through the overlay.

**Actual before fix:** CanRest was true and stale SEND marked its photo submitted. Before changing the common navigation guard, camera Capture returned a PhotoRecord instead of null and consumed a charge (`55de7a25230a`). TimeScale=0 only stops ordinary frame deltas; direct APIs and supplied timesteps remained actionable.

**Root cause / mutation boundary:** pause was owned by UI overlay/timeScale and omitted from Loop's availability guards and ZoneNavigation.ExpeditionBlocked, which device services already consult.

**Correction:** major Loop mutations derive pause + transition + SEND transaction availability; common navigation/device guard derives actual PauseMenuController state. Ordinary menu SaveCurrent is intentionally still allowed while paused. No changes to resource costs, coordinates or timing balance.

**Changed files:** `Scripts/Computer/{ExpeditionLoop,PhotoCaptureService}.cs`, `Scripts/Navigation/ZoneNavigation.cs`.

**Regression / verification:** `PausedMutationAuthoritiesRejectRestJournalUpgradeAndEnding`, `PausedSendRejectsStaleUiCallbackAndResumesWithoutCharge`, `PausedDeviceServicesCannotConsumeChargesOrMoveVessel`; failing `prefix-reproductions.xml` and `prefix-paused-device.xml`, then PASS in `postfix-focused.xml` and final full PlayMode. Transition/death stale SEND was already PASS before fixes and remains a neighboring regression, not a newly found defect.

## QA-003 — P2: Photo Lab hides persistence failure feedback

**Reproducer:** valid candidate, writable temporary timeline, deny sidecar writes with FileStream(FileShare.Read), invoke SEND. `ArchiveWriteFailureCannotUndoDurableSendOrDuplicateRewards` expects the service warning in the lab.

**Expected:** authoritative SEND remains committed, but lab explains archive metadata could not update; rejected authoritative SEND also explains rejection without overlapping preview. Durable completion must not be rolled back because an ancillary archive write failed.

**Actual before fix:** service logged/set LastError but PhotoLabView displayed only submission state. Pre-fix test failed because DisplayedText omitted the service error.

**Root cause / mutation boundary:** IPhotoSubmissionRepository did not expose feedback and the view did not render it. The ancillary archive and authoritative timeline were not explicitly distinguished by feedback.

**Correction:** expose LastError and cache the current lab submission's message; show it in the existing fourth status row. Initial implementation added a fifth row; the unchanged overflow assertions detected overlap (`postfix-first.xml`, 7/8 PASS). Reusing the fourth row fixed that introduced defect without altering panel size/art or weakening assertions. Cache clears on photo navigation/reopen/delete to avoid leaking unrelated deletion errors.

**Changed files:** `Scripts/Computer/{ComputerDataContracts,PhotoCaptureService,PhotoLabView}.cs`.

**Regression / verification:** archive-write failure/restart/duplicate protection and failed-SEND UI/TMP overflow PASS in focused and final full suites; screenshot visually inspected at `D:/G10/Temp/TestCaptures/qa-send-rejected.png`. Ancillary metadata may remain stale after its write failure; timeline-embedded photos remain authoritative. No automatic sidecar repair is claimed.

## QA-R01 — INCONCLUSIVE: intermittent radar fixture failure

**Baseline:** full unchanged PlayMode job `fb7731743249`: 264/265 PASS. `PhotoSurveyPlayModeTests.DepthButtonsChartAndRealRadarContact` expected one contact on its second scan but saw zero (line 63). Unchanged isolated rerun `51608044bbc1` passed 1/1. Logs: `baseline-playmode.xml`, `baseline-radar-rerun.xml`.

**Hypothesis, not proven root cause:** the radar uses unscaled sweep/fade time while the fixture waits with scaled WaitForSeconds; a long Editor scheduling/focus stall could miss the transient contact window. No radar production code, balance, coordinates or fixture assertions were changed. Final full-run test PASS in 5.963 s (`regression-playmode.xml`); that later PASS does not erase the intermittent baseline failure or confirm the hypothesis.

## Test-harness issues (not production defects)

- Unity's NUnit does not support Assert.Multiple used in the initial new tests. Replaced aggregation wrappers with sequential assertions; none removed. Compilation then clean.
- A run dispatched during that invalid assembly state had no valid progress (`55f358c86a4b`). MCP job was cleared and native PlayMode explicitly stopped. Classified NOT RUN, never counted as a completed suite.
- New historical stress initially looked up a nested PreviousDay button through the wrong transform and raised NullReferenceException. Fixed the test's button lookup; unchanged production then passed three real cycles in `prefix-stress-rerun.xml`. Initial fixture result is INCONCLUSIVE for production, not a product bug.
