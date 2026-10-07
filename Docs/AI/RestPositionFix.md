# Rest position jump investigation

Date: 2026-10-07. Local `main`: `1d459058c0043f3cd8931c7b274468d9a0f77448`.
Unity 6000.4.2f1 / AnkleBreaker MCP 2.39.7, Windows Editor. Initial scene: clean Bootstrap, not playing; Console errors: 0. Existing feature edits, user assets and font-cache changes were preserved.

## Defect contract

Reported: Rest sometimes moves the submarine back near `(900, 160)` instead of keeping its end-of-day position.

Expected: Rest advances the day and refills resources in place, preserving position, heading, depth and travelled distance regardless of Energy. Saving/loading or death rollback after Rest must use that same position. Explicit rescue remains separate.

## Hypotheses and evidence

| Hypothesis | Supporting evidence / prediction | Discriminating check | Status |
| --- | --- | --- | --- |
| Rest UI silently selects rescue at zero Energy | `RequestRest` set `recoveryPending = NeedsRecovery && CanRecover`; configured Zone01 rescue position is `(960, 154)` | Use the same UI flow away from entry with Energy 5, then 0 | Confirmed: positive Energy kept position; zero Energy jumped to exactly `(960, 154)` |
| Snapshot lost position and fell back to map entry | Could explain a fixed reset position | Compare entry, rescue and saved voyage; inspect `CaptureInto` | Contradicted: entry was `(100, 400)`, not the observed rescue destination; snapshots explicitly capture voyage |
| Fade timing / new feedback effects reset navigation | Report sounded intermittent | Compare identical fade/UI sequence changing only Energy; inspect feedback authority | Contradicted as the cause of this reproduction: feedback never writes navigation; only the explicit recovery branch relocates |

The apparent randomness depends on Energy reaching zero and a valid rescue area being available, not a random Rest roll.

## Confirmed execution path

Before fix:

```text
Rest UI RequestRest
  -> Energy == 0: recoveryPending = true
  -> ConfirmRest / PresentRest(loop, true)
  -> AdvanceRestDay / AdvanceDay(recovery: true)
  -> candidate.current zone position = RecoveryArea.mapPosition
  -> Navigation.RestoreVoyage(rescue position)
```

The dedicated failing regression reproduced `(240, 415) -> (960, 154)` at Energy 0. The preceding Rest at Energy 5 kept its expected position. The assertion failed against the original production code, before any fix was applied.

## Smallest fix

- The Rest UI now always requests ordinary `PresentRest(loop)`, including at zero Energy.
- Removed the implicit `recoveryPending` selection and the automatic `REQUEST RESCUE` label.
- Rest availability and confirmation consistently use `CanRest`.
- Rest description explicitly states that submarine position is preserved.
- Existing `RecoverShip()` and explicit recovery presentation/authority remain unchanged; rescue callers still retain their existing relocation behavior.

No gameplay/save schema, scene, map, depth, refill amount, deadline, recovery position or rollback rule was changed. No player save was edited. Tests use isolated save/photo directories.

## Changed files for this fix

- `Assets/_Project/Scripts/Computer/ExpeditionComputerView.cs`: remove silent Rest-to-rescue routing.
- `Assets/_Project/Tests/PlayMode/ExpeditionLoopPlayModeTests.cs`: add `RestUiPreservesVoyageAtZeroEnergyAcrossRepeatedDaysAndReload`.
- `Docs/GlobalExpeditionDaysAndRegularUpgrades.md`: distinguish in-place Rest from explicit rescue.
- `Docs/AI/RestPositionFix.md`: investigation and validation record.

## Validation

- Before fix: new regression **0/1 passed**, failure at Energy 0, expected `(240, 415)`, actual `(960, 154)`; job `44e839fa5536`, 5.02 seconds.
- After fix: original regression **1/1 passed**, 0 failed/skipped; job `b7514a14bb7c`, 9.07 seconds. Four consecutive UI Rest operations at Energy `[5, 0, 0, 100]` preserved position/heading/depth/distance and refilled Energy. Current save, day-start snapshot and Journal checkpoint agreed; reload preserved the voyage.
- Broad PlayMode regression: **106/106 passed**, 0 failed, 0 skipped; job `b11aa3b807c9`, 334.33 seconds. This includes a second complete run of the four-Rest reproduction, so both post-fix runs preserved the voyage across eight UI Rest operations in total.
- Final project compilation: zero errors. Console error check: zero errors. Diff whitespace check passed; existing save/resource authority and content/package/settings files were unchanged.

The broad run covers expedition day/save/load, economy, vessel death and explicit rescue, cargo, tutorial, navigation, capture, photographs, scene flow, feedback and both ending routes.

## Remaining limits

This is a live Windows Editor validation, not a standalone player build. It prevents future Rest teleportation; it does not silently rewrite an existing save already moved by rescue. Such a timeline can be restored deliberately through the existing Journal if desired.
