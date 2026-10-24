# Zone02 experience loop — preparation

Date: 2026-10-24 (Asia/Saigon). Exactly one baseline → analysis → redesign → implementation → final voyage → comparison is authorized. No second redesign cycle will follow the measured final voyage.

## Repository and preservation

- Project: `D:/G10`; Unity 6000.4.2f1, Windows Editor, shared Zone01 cabin, logical Zone02 through CabinZoneSession.
- Starting HEAD: `b28dcc9`, initially `main`; dedicated branch `codex/zone02-experience-loop` created without checkout/reset of local files. No commit, merge or push is authorized.
- Existing local changes: two dynamic font assets, TimeManager, four deleted PerformanceTestRun JSON/meta files. Exact entry status and protected mission/map/scene/font/settings hashes are recorded beside this report.
- Native probe: stopped, clean Bootstrap, not compiling, zero C# compilation errors. Existing save/photo overrides null. Normal save root is `C:/Users/Artermis/AppData/LocalLow/DefaultCompany/G10/Expedition/`; file hashes recorded separately.
- Preserve mission counts, coordinates, captures, costs and exit geometry. Do not run the broad ZoneTwoSetup installer for preparation: it reauthors mission/map content.

## Evidence and control method

Available methods were inspected: the AnkleBreaker Unity MCP connection is active; the installed Windows Computer Use helper exposes the actual Unity window. Native advanced MCP tools provide input-asset authoring, but no bounded gameplay key-hold/recording capability. Windows Computer Use offers individual clicks/keypresses, which is inefficient for continuous ship and interception control.

Selected method: editor-only instrumentation queues bounded keyboard/mouse state into the existing Unity Input System. The normal EventSystem/UI input module, navigation Update and capture Update consume these events at normal time scale. UI clicks must use the current visible interface/actual screen rectangles. No mission APIs, navigation setters, resource refills, synthetic Tick, reward injection or forced presentation are permitted in measured voyages. Any optional capture tracking controller may choose keys using only the hook/fish geometry already drawn on screen; this is an automated input driver, not human difficulty feedback.

Passive evidence records actual GameView frames, real AudioListener output, UTC/realtime/game clocks, inputs and read-only visible/mission/cue state. Available ffmpeg can mux captured frames/audio. Frame rate, capture drops, audio drift, overhead and sensory limitations must be reported from actual capture results; a recording does not constitute human listening or fear feedback.

Instrumentation is the only new source permitted before the baseline. It must remain editor-only and must not alter production game behavior.

## Equivalent initial states

Each voyage uses a separate reporting run directory as save/photo override, configured before Play. Use the existing Main Menu DEBUG UI, select Zone02, regular upgrades NONE, then START DEBUG RUN. The built-in DebugExpeditionPreset completes earlier zones through their reward authority, grants Bio Lamp, preserves the 500m starting depth cap and starts Zone02 with untouched missions, default resources and no horror receipts. Baseline and final must use the same preset and compare initial snapshots.

Record from debug start through physical Zone03 arrival (or an honestly documented blocker). Normal user timeline and pre-existing debug save must remain untouched. No direct save construction is used for measured setup.

End-of-cycle correction: that preservation requirement was not fully achieved during baseline shutdown/native-test interruption. The original normal primary was recovered byte-exact, but its older backup and original debug pair remain unrecovered. Corrected isolation protected the final voyage. See save-preservation-incident.md; the preparation requirement above is not a claim that all entry saves remained untouched.

## Measurement rules

- Report wall elapsed separately from active gameplay, loading, pause and operator/agent processing gaps.
- Do not treat all no-input time as confusion. Tag purposeful observation, interface inspection, recovery and external reasoning separately.
- Distinguish receipt/start from actual visible/audible exposure; capture, UI closure and pause can interrupt existing sequences.
- Read visible state first when blocked; try at most a few distinct recovery actions, then document the condition. No hidden progression shortcuts.
- No subjective human emotion is measured. Tension scores are design hypotheses supported by timestamped observations.

## Audit sources

Read `Docs/Zone02EnvironmentalHorror.md`, its QA results, current project context, shared cabin/navigation/input, DebugExpeditionPreset, ZoneTwoPresentationController/State, EffectDirector/Sequence and existing functional tests. Earlier tests explicitly use position/refill/Tick helpers and therefore do not establish a full voyage or human navigation experience.

Capability check: instrumentation compiled with zero errors. A queued click did not activate DEBUG while Unity lacked application focus; activating the actual Unity window with Windows Computer Use, then repeating the queued click opened the real DEBUG dialog. The check stayed in Main Menu and consumed no Zone02 state. `preparation-input-probe/` contains 117.336 seconds of explicitly unmeasured setup:571 genuine frames,7 skipped video slots, actual listener WAV with silence,0 dropped audio samples. Silent Main Menu capture is reported as silent. The measured voyage uses a fresh `baseline/evidence/` recording.

Automation limitation: chart geometry was inspected during preparation to support efficient physical steering. Machine route preparation is not a human wayfinding-time measurement. All actual voyage travel must still occur through normal ship inputs, with terrain/gates active. A suspected return-gate corridor conflict is a hypothesis until visible runtime transition confirms it.
