# Zone04 — preparation for one experience improvement cycle

User request: repeat the completed Zone03 workflow for Zone04. Exactly one baseline → audit → redesign → implementation/native validation → final actual-input playthrough → comparison. Zone04 is the final chapter: the main measured route ends after the physical hidden arrival and full hidden Ending presentation, rather than a nonexistent Zone05. Normal Ending remains a separate native regression target.

## Entry and preservation

- Workspace `D:/G10`, Unity 6000.4.2f1, existing AnkleBreaker bridge at port 7890. Initial native read: stopped, unpaused, not compiling, clean Bootstrap. Use only `unity_*` MCP for Editor actions.
- Entry branch `codex/zone03-experience-loop`, HEAD 665bf42, including retained uncommitted Zone03 work. Created `codex/zone04-experience-loop` without reset, stash, commit or push. `git-status-entry.txt` and `git-diff-entry.patch` retain the entry state. Keep the four existing PerformanceTestRun deletions and both modified font assets.
- Before play or instrumentation edits, copied all **156 persistent files / 12,275,786 bytes** from the real G10 persistent directory to ignored `preserved-local/persistent-G10-entry/`; every copy SHA256 verified. `persistent-entry-manifest.json` is this task's preservation authority.
- Copied and verified **14 protected files**, including maps, missions, shared cabin/core/ending scenes, fonts and TimeManager. `protected-entry-manifest.json` records their exact current bytes. These include the completed Zone03 changes as they stand at this task's entry.
- `production-before-baseline.json` records **390 source/config/scene hashes**. Only Editor evidence and offline analysis tooling may change before the baseline. Record those exceptions explicitly; no runtime presentation, map, mission, resource or progression simplification is allowed.

## Actual journey and authority

Use the existing Main Menu DEBUG START UI, Zone04, regular upgrades NONE. The legitimate debug preset grants previous-zone progression and required story modules. Use separate empty baseline/final isolated save and photo folders; compare the actual initialized ship/resources/upgrades/mission/presentation state before interpreting results.

Main measured route: ordinary chart/Helm navigation → L1 PHOTO → actual Photo Lab SEND → actual CAPTURE → inspect recovered coordinates in the actual Mission Log → L2 PHOTO → SEND → choose CONTINUE EXPLORING → real Radar discovery and PHOTO/SEND at both hidden sites → physically travel into the final area → observe the complete hidden ending. Hidden POI `zone04-l5` is intentional. Presentation must never grant discovery, SEND, capture, choice or final arrival. Do not use source-only hidden coordinates to bypass the player's recovered information.

Preserve the canonical 9 mandatory + 2 hidden photograph layout, capture requirements, coordinates/depths, costs, normal/hidden ending IDs and authored return gates. Do not run broad setup/install commands to prepare play.

## Input, evidence and timing

Reuse the established Editor-only InputSystem driver, GameView JPEG recorder and actual AudioListener PCM recorder. Bounded keyboard/pointer events pass through normal Update and EventSystem. Chart assistance may use operator-chosen visible-map waypoints; capture assistance may use currently rendered hook/fish geometry. No gameplay setters, teleportation, resource edits, forced effects, objective injection or accelerated simulation during measured voyages.

Extend only passive Zone04 telemetry where necessary: fragment/hidden/final approach receipts, actual sequence/audio/visual audiences, mission/choice/ending state. Keep original Zone02/03 analysis compatible. Freeze source/imports while a voyage or native test is live. Rotate recordings before their cap if necessary; retain real gaps and do not synthesize replacement audio/images.

Use raw UTC for first initialized nontransitioning Zone04 through committed hidden Ending and full authored aftermath. Report loading, input activity, external operator processing, pause/unfocus and unknown no-input time separately. Normal test execution duration is never gameplay time. Estimated tension is a design hypothesis; AI control does not establish human fear, hearing, confusion or subjective immersion.

## Isolation and verification

Call `SetupIsolation` only in Edit Mode, verify resolved save/photo paths before actual debug start, and retain isolation through OnApplicationQuit. `StopRecording` alone must not restore the user's save paths. Verify again after exiting Play; restore null/original overrides only after quit-save. Run native tests in an outer isolated folder as well as each fixture's own isolation. Recheck the persistent and protected manifests after each phase. Keep generated native XML in this report before restoring any entry TestResults file.

## Phase order and resources

Seal baseline recording and write `01_Baseline_Playtest.md`/timeline and `02_Experience_Audit.md` before one `03_Horror_Redesign.md`. Then implement the evidence-supported scope, run focused regressions, repeat the same actual route once and produce `04`–`06` with linked evidence and two annotated timelines.

Inspect existing Zone04 sparse audio, photographic environments, fragment view, environmental withdrawal, final signature and independent ending director before choosing resource work. New assets are allowed where justified; no paid calls, purchases or package changes without existing authorization. Prior generated candidates retain their recorded source/license limitations. No automatic second redesign after the final evaluation; no commit, merge or push.
