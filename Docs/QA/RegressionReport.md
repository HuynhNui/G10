# State QA regression — 2026-10-12/13

## Repository and environment

- Baseline / final HEAD: `13a859a13326b016b32eb29b2ae3c23c5e2b3937` (initial local main and origin/main advertised by ls-remote match). Final SHA is unchanged because this pass leaves its implementation as an uncommitted working-tree patch, not a new commit.
- Branch: `codex/state-qa-hardening-2026-10-12`; changes currently uncommitted. No push/merge.
- Initial worktree clean. Preserved Unity-generated dynamic TMP font caches. Build temporarily added an Input System preload entry; native post-build PlayMode cleanup removed it again (final ProjectSettings has no diff). Neither was manually reset. Exact generated paths and build-time diff are in Logs/windows-build.md.
- Windows Unity 6000.4.2f1, AnkleBreaker 2.39.7 on port 7890, Test Framework 1.6.0.
- New and existing gameplay fixtures select temporary timelines/archives before loading gameplay; legacy presentation fixtures use LegacyCabinTestSession. DEBUG tests assert temporary normal sentinel bytes unchanged. No player's real save read or written as QA data.

## Coverage delta

Existing: **60 EditMode + 265 PlayMode = 325** cases. Added: **3 EditMode + 9 PlayMode = 12**, total **337**. Reused existing rollback neither/PHOTO-only/both, backup/migration, permanent deletion, all-three return pairs, economy, rock, death/deadline, tutorial, maps/modal/ending and scene-binding tests; no balance or mission coordinates altered to pass.

New tests:

| Fixture | Added coverage |
|---|---|
| MissionProgressStateTests (3) | Copy isolation for all nine lists; normalize idempotence/deduplication and unknown IDs; null legacy lists without manufactured progress |
| PhotoSubmissionPlayModeTests (6) | Timeline write rejection before staging and at primary replace; seven-reward/cargo/counter/event rollback; ancillary sidecar failure; paused stale SEND; transition/death stale SEND; paused device services |
| ExpeditionLoopPlayModeTests (1) | Paused Rest/Journal/upgrade/ending mutation authorities |
| TutorialPlayModeTests (1) | Real tutorial/device flow learns PhotoLab only after durable SEND |
| ExpeditionEndToEndPlayModeTests (1) | Three actual camera/SEND/capture/Rest/Journal/Continue/death/return cycles; historical checks before/after rollback, cross-zone failed Journal publication and exact JSON/cargo/receipt oracles |

The seven-reward fixture uses a cloned configuration expressly to test transaction fan-out, not authored progression. The E2E test earns objective completion through actual devices (ReachZone(2,true)); vessel positioning and capture steering are deterministic drivers, not human-play claims.

## Execution ledger

PASS/FAIL counts below are raw NUnit outcomes. Interpret harness/intermittent classifications separately; do not add repeated runs to obtain a unique-test coverage count.

| Run | Job | PASS / FAIL / skipped | Duration | XML / classification |
|---|---|---:|---:|---|
| Unchanged full EditMode | 25417b91aead | 60 / 0 / 0 | 2.374 s | baseline-editmode.xml — PASS |
| Unchanged full PlayMode | fb7731743249 | 264 / 1 / 0 | 853.805 s | baseline-playmode.xml — radar intermittent; cause INCONCLUSIVE |
| Unchanged radar focused rerun | 51608044bbc1 | 1 / 0 / 0 | 7.399 s | baseline-radar-rerun.xml — PASS without modifying radar |
| New pre-fix reproductions | c1d2dfb935bc | 1 / 7 / 0 | 38.030 s | prefix-reproductions.xml — six confirmed failing assertions; one harness INCONCLUSIVE |
| Corrected new stress, unchanged production | daaae580444a | 1 / 0 / 0 | 31.483 s | prefix-stress-rerun.xml — PASS |
| New EditMode, unchanged production | 8a844b16d30d | 3 / 0 / 0 | .017 s | prefix-editmode-new.xml — PASS |
| First post-fix focused | 2e1b3865b9d9 | 7 / 1 / 0 | 58.445 s | postfix-first.xml — introduced feedback row overflow detected, then fixed |
| Paused devices before navigation guard fix | 55de7a25230a | 0 / 1 / 0 | 2.377 s | prefix-paused-device.xml — confirmed FAIL |
| Final focused + Loop/tutorial neighbors | ef8e69bd55f1 | 26 / 0 / 0 | 126.261 s | postfix-focused.xml — PASS |
| Final full EditMode | 2e04c2b0de5f | 63 / 0 / 0 | .396 s | regression-editmode.xml — PASS |
| Final full PlayMode | 5a99bad55ce4 | 274 / 0 / 0 | 816.740 s | regression-playmode.xml — PASS |
| Post-build scene-flow smoke | 8244ac773c48 | 2 / 0 / 0 | 4.290 s | postbuild-scene-smoke.xml — PASS, existing cases rerun |

**Latest unique automated-test classification: 337 PASS, 0 FAIL, 0 INCONCLUSIVE, 0 NOT RUN, 0 skipped.** Per-test classification is the result on each test-case node in the two regression XML files, not an extrapolation from focused tests. Comparing baseline/final test names confirms exactly nine added PlayMode cases and three added EditMode cases, with no removed existing cases.

Separate from this latest automated count: QA-R01 has **one INCONCLUSIVE intermittent issue**, despite final radar PASS (5.963 s); **10 NOT RUN** manual procedure families; **one NOT RUN** invalid runner dispatch; the early new stress fixture's production inference was INCONCLUSIVE but its corrected fixture passed on unchanged production and final code. These are different denominators and are not folded into 337.

Invalid/stopped runner job `55f358c86a4b` had no trustworthy execution during a compile-error state: **NOT RUN**, not a completed suite. Initial Assert.Multiple compile incompatibility and nested button lookup were fixed in test code without deleting assertions. Only the initial UI implementation's fifth-row overflow required a production feedback correction.

## Exact commands and evidence handling

Repository preflight (PowerShell, working directory `D:\G10`):

```powershell
git status --short
git branch --show-current
git rev-parse HEAD
git ls-remote origin refs/heads/main
git switch -c codex/state-qa-hardening-2026-10-12
```

Unity is already open; use AnkleBreaker MCP rather than starting a second batch-mode Editor or HTTP bridge. Calls (port 7890):

```javascript
unity_editor_state({port:7890})
unity_get_compilation_errors({port:7890,severity:"error",count:8})
unity_advanced_tool({port:7890,tool:"unity_testing_run_tests",params:{mode:"EditMode"}})
unity_advanced_tool({port:7890,tool:"unity_testing_run_tests",params:{mode:"PlayMode"}})
unity_advanced_tool({port:7890,tool:"unity_testing_get_job",params:{jobId:"<ledger ID>",includeFailedOnly:true,waitTimeout:20}})
```

Focused filters (same run_tests call with `filter` in params):

```text
Baseline radar: DepthButtonsChartAndRealRadarContact
Pre-fix / first post-fix 8: FailedSend|ArchiveWriteFailureCannotUndo|PausedSendRejects|TransitionAndDeathReject|PausedMutationAuthorities|QaRepeatedGameplayRollback
Stress rerun: QaRepeatedGameplayRollback
New EditMode: MissionProgressStateTests
Paused device pre-fix: PausedDeviceServicesCannotConsumeChargesOrMoveVessel
Final focused 26: PhotoSubmissionPlayModeTests|ExpeditionLoopPlayModeTests|TutorialPlayModeTests.(FailedSend|FullTutorial)|QaRepeatedGameplayRollback
Post-build smoke 2: ProjectSceneFlowPlayModeTests
```

Preserve each native Test Runner XML **after completion and before starting the next run**, substituting the filename in the ledger:

```powershell
Copy-Item -LiteralPath 'C:\Users\Artermis\AppData\LocalLow\DefaultCompany\G10\TestResults.xml' -Destination 'D:\G10\Docs\QA\Logs\regression-editmode.xml'
git diff --check -- Assets/_Project/Scripts Assets/_Project/Tests Docs/QA
```

Full per-test outcomes, assertion messages/stacks and test output are in XML. File locks/parent-file paths are test-only faults. Expected archive warnings are asserted with LogAssert, not silenced globally. Source import uses MCP execute_code `UnityEditor.AssetDatabase.Refresh();` followed by compilation checks before testing.

## Defects, remaining risks and completion

QA-001 transactional SEND, QA-002 pause authority guards, QA-003 feedback: focused and full regression PASS. See BugReports for exact reproducer/root cause/files.

Baseline radar scheduling failure remains INCONCLUSIVE even if final run passes. Sidecar repair after ancillary failure is not implemented/claimed; authoritative timeline is coherent. Power loss/ACL/future-version/same-zone Journal fault/human difficulty and resolution sweep are not exhaustively covered. M01–M10 in ManualQA are **10 NOT RUN** manual cases. One generated feedback screenshot inspection PASS is separately scoped. No standalone player or human end-to-end playtest has been run.

After full PlayMode: 0 compilation errors, not compiling; Editor stopped in clean Bootstrap, not paused. Save/Archive overrides both null and DEBUG timeline false (read-only inspection of static fields, no access to normal timeline content). Build Settings have eight enabled existing scene assets, StandaloneWindows64 supported, active backend Mono2x. Existing scene tests cover bindings and canonical transitions; no scenes were re-saved as part of QA.

Windows Release build dispatched through MCP:

```javascript
unity_build({port:7890,target:"StandaloneWindows64",outputPath:"D:/G10/Temp/QAWindows-2026-10-13-001/G10.exe",developmentBuild:false})
```

**Windows Release build PASS:** result Succeeded, 0 errors, 3 warnings, 76.982 s, BuildReport size 463520717 bytes. EXE/data/dependencies verified present, no test assemblies in Managed. Full metadata, existing-source warnings and checksum: `Logs/windows-build.md`. No executable launched. The output path was verified not to exist before dispatch; it is ignored by Git and temporary, not published. No Build Settings/backend/packages were authored. Unity itself temporarily added the existing Input System preload entry and changed font caches; native post-build PlayMode cleanup removed the preload again. Font changes remain; no generated files were manually reset.

Post-build console: 0 error entries; compilation: 0 errors, not compiling. Three warnings are unchanged-source obsolete API/unused-field warnings; older archive warnings are expected fault tests. No radar code/assertions changed. Final source/tests diff whitespace check exits 0 (Git's LF/CRLF notices are not whitespace failures). No scene/art/config balancing authoring changes.

Post-build isolated scene-flow smoke **2/2 PASS**, job `8244ac773c48`, 4.290 s. Verified Bootstrap→MainMenu→GameplayCore+Zone01, single EventSystem, required UI/input bindings and locked-zone rejection after build/import side effects. This is not a standalone-player launch. Final handoff: clean Bootstrap, EditMode, not paused/compiling, timeScale=1, both test overrides null, DEBUG=false, 0 compilation/console errors. Final HEAD and branch verified again; no commit/push/merge.
