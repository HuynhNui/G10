# Zone03 evidence-tool reuse and isolation audit

Audited against entry HEAD665bf42 and the current shared workspace on2026-10-25. Root is the sole Unity operator. This work inspected source/files and made the narrowly authorized instrumentation/offline-parser changes; it did not call Unity, edit production gameplay, run a preset, advance a clock or trigger an event.

## Reuse decision

Reuse `ZoneTwoExperiencePlaytestTools` and `ZoneTwoChartInputDriver` under their historical class names. Their keyboard/mouse, bounded realtime controls, screenshot/listener capture and chart input logic are not Zone02 gameplay shortcuts. The chart driver records a starting zone and stops when that actual zone changes; no Zone03 blocker was found in its implementation. It was not changed.

The original recorder was insufficient for Zone03 presentation analysis: `cueReceipts` always read ZoneTwo state and its controller counters were ZoneTwo-only. `EffectVisualExecutor.SignalVisible/AnomalyVisible` also do not measure the Zone03 procedural drawing layer. The original Python parser selected Zone02 readiness/departure and named all scoped metrics `zone02_*`, even when given Zone03 raw data.

## Authorized editor-only patch, completed before baseline

Only `Assets/_Project/Scripts/Editor/ZoneTwoExperiencePlaytestTools.cs` was edited in Assets. Its assembly is Editor-only and already references the required runtime/InputSystem/UI assemblies. Existing input APIs,5Hz capture/audio processing, save-isolation callbacks and normal Zone02 readouts were retained.

- Active Zone03 `cueReceipts` now reads `ExpeditionLoop.ZoneThreePresentation.played`. Common Radar/Reflection counters select the current ZoneThree controller while in Zone03; Zone02 behavior remains unchanged.
- Cached ZoneThree controller/rock/procedural component references refresh with the existing discovery pass. New passive fields are `presentationZone`, `zoneThreeTelemetryObserved`, `engineEvents`, `stoneEvents`, `environmentLevel`, `rockActive`, `rockElapsed`, `rockDestroyed` and `visibleProceduralPresets`.
- Actual `ZoneThreeVisualSurface.Visible/ActivePreset` produces `stoneVisual`, `falseRadarVisual`, `proceduralReflectionVisual`, `bioelectricVisual` and `rockDebrisVisual`, plus target/preset labels. The reader never calls `Render`, `Preview`, `Tick`, effect playback, objective actions or state setters.
- `rockDestroyed` reads the actual mission world flag. It is distinct from the active presentation/elapsed lock and the real RockDebris drawing layer. A flag commit does not prove that the delayed terrain reveal or ten-second presentation has finished.
- Zone02 `radarVisual`, `reflectionVisual`, `photoMotion` and other legacy fields retain their meanings. In Zone03, use the new procedural flags; a false legacy flag is not proof that the procedural shader was invisible. Root separately marks Zone03 stage/pending readouts as unobserved until there is an actual Zone03 stage/pending API, avoiding a misleading inherited ZoneTwo “Normal”.

`git diff --check` passed. Root subsequently reported native compilation with zero errors. All Assets source is now frozen for the measured baseline. No new assembly, package, global horror framework or scene serialization was introduced by this tooling patch.

## Offline parser, backward-compatible Zone03 mode

`Tools/QA/summarize_zone02_experience.py` now accepts `--zone Zone03`; default remains Zone02. Zone03 readiness, first departure, maximum distance, control/operator metrics and surface intervals use `zone03_*` labels and expect physical Zone04. First departure and any observed expected-destination arrival are separate facts; an unintended return is not silently renamed successful completion.

New optional fields are watched and copied into event rows. Engine/stone starts, procedural preset changes, actual visible-surface intervals, rock lock/elapsed/destroyed observations and frame examples are available. Unknown/missing fields remain unknown, never zero-exposure proof. Stage/pending state changes are still excluded from candidate dramatic-gap counts. Ordinary stone recurrence is an environmental observation, not automatically a meaningful scare.

The historical `--initial-report-dir` template is Zone02-specific and is rejected for `--zone Zone03` before evidence is read or written. Author Zone03 phase reports from their actual voyage evidence. No prior Zone02 report, raw recording or video was overwritten/recomputed.

Checks performed offline:

- Compiled Python source and exercised the CLI/help/template rejection.
- Read old Zone02 original/final metadata into memory and compared edited output against the HEAD parser, substituting the same audio placeholder to avoid rereading PCM. Every existing nested field/value was preserved; new fields are additive.
- An in-memory parser fixture verified Zone03→Zone04 selection, engine/stone observation, rock/procedural exposure and missing-schema unknown behavior. This fixture is parser verification, not gameplay evidence.

After recording, use the actual evidence path, for example:

```powershell
python 'D:\G10\Tools\QA\summarize_zone02_experience.py' 'D:\G10\Docs\QA\Zone03ExperienceLoop\baseline\evidence' --zone Zone03 --segment zone03-baseline --status 'baseline recording; completion assessed from actual authority and ready state' --mux > 'D:\G10\Docs\QA\Zone03ExperienceLoop\baseline\evidence\analysis-console.json'
```

For the final recording replace the folder/segment/status accordingly. Omit `--initial-report-dir`. The generated index and MP4 support the authored phase reports; they do not themselves establish subjective effectiveness.

## Isolation lifecycle and remaining native-runner risk

The prior early-restore path is absent: `StopRecording()` closes evidence/releases input and does not restore overrides. Normal restoration occurs on explicit `EnteredEditMode`; explicit teardown is Edit-mode-only. Active isolation is remembered in `SessionState`, reapplied by the InitializeOnLoad constructor, before scenes, and on EnteredPlayMode. Thus an ordinary measured shutdown keeps the override through `ExpeditionLoop.OnApplicationQuit`.

These are nominal lifecycle safeguards, not proof that every native fixture/import failure is safe. `PathOverride` is ordinary static state. Fixtures can replace it, some legacy teardowns explicitly set it null, and an unexpected live source/domain reload can invalidate fixture/runtime state. The active SessionState flag alone does not verify the actual resolved save/photo destinations. The helper is not a production-wide save-path interceptor and cannot guarantee against a same-frame foreign write after an override is cleared.

The selected practical safety protocol is therefore:

1. Preserve full current-entry bytes of **all** persistent G10 files before Unity; hashes alone cannot recover displaced backups. Root has backed up/verified152 files and13 protected assets for this task. These current entry bytes are the authority, not unrecovered older Zone02-task hashes.
2. Set a fresh isolated run folder before Play and verify the actual resolved save/photo paths before Debug Start and before stopping. Keep isolation active until full EnteredEditMode. Do not call explicit teardown during exit.
3. Freeze source/imports for the complete measured voyage. Root uses no native TestRunner as a substitute for gameplay; compilation/MainMenu capability checks precede baseline.
4. Later native regressions require an outer isolated folder as well as each fixture's own temporary isolation. Freeze all Assets source for a native job. Treat an unexpected domain reload/runner abort as interrupted evidence, then inspect actual destinations before further work.
5. Compare the complete persistent-file manifest after every voyage/native run. If anything differs, preserve the incident bytes first and recover exact entry copies before proceeding. Record the incident honestly instead of claiming successful preservation from intended isolation alone.

## Evidence limits that must stay visible

Actual controls still pass through InputSystem and normal UI/game Update. Chart/capture assistance measures neither novice wayfinding nor human motor skill. Five-Hz frames and sampled flags may miss brief visuals; real images must be inspected. Source counters/receipts/visible components are not proof of human attention or fear. Actual listener WAV is streamed without generated replacement PCM; listener replacement, DSP/device buffering, dropped buffers and capture jitter limit A/V synchronization. Use real VFR durations and retain originals. Readiness must distinguish authority, initialized resources, transition completion and late observer annotations. Unknown no-input gaps remain unclassified.
