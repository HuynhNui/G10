# Autonomous playtest — 2026-10-15

Execution in progress; this report records observed milestones, not an ending claim.

Current baseline: `codex/unified-effect-director`, HEAD `587d78bcc5cb5b43d639b3b1b2605cff24af742a`, with pre-existing local EffectDirector, feedback, audio, scene and test changes. Initial status and tracked patch are preserved in Evidence. Unity 6000.4.2f1, AnkleBreaker 2.39.7, Input System 1.19.0, product version 1.0. Old documented Windows build is absent. This run uses the freshly compiled working tree in the Editor, not the old test binaries or a standalone build.

No additional services, packages or spending. No commit, push, merge, save editing, teleportation or forced mission completion. Save/archive overrides verified before selecting New Game. All gameplay save files and photos belong to `Evidence/run-20261015-A01/`.

## Evidence and interpretation

`actions.jsonl` records UTC timestamps, normalized input coordinates, hold durations, dimensions, mode boundary and local controller decisions. PNG filenames preserve observation order. Original Game View: 1920×1080. Screenshots are actual game rendering. Mode A observations use screenshots only; Mode B explicitly starts after screenshot 060 and reads state for diagnosis. It never retroactively converts a failure into a black-box PASS.

Mode A reached New Game → helm checklist → map → radar → camera → Photo Lab SEND → navigation to capture → low-energy Rest → Main Menu → Continue. It did not complete capture or the game. Mode B found positioning errors and used ordinary input to enter/win capture. Its geometry-based W/S controller reads rendered hook/fish runtime positions, so capture is state-aware input evidence, not black-box completion.

| ID / mode | Starting state and actions | Expected | Observed / status | Evidence / reliability |
|---|---|---|---|---|
| A01 / A | New isolated menu; click New Game, advance welcome | Fresh playable tutorial | PASS: cabin and helm checklist | 001–005; 1 run; first click blocked by editor welcome window, retried |
| A02 / A | Day1; hold forward, turn, depth buttons | Checklist learns all 3 controls | PASS: all checks learned, map unlocked | 005–009; 1 run |
| A03 / A | Map, then radar, SCAN | Consume one radar use; next tutorial step | PASS: 10→9 scans, camera coordinates displayed | 009–011; 1 run |
| A04 / A | Sail from (159,400) toward displayed (436,121), photograph | Valid unsent mission candidate | PASS: MISSION DATA DETECTED | 012–021; 1 run |
| A05 / A | Candidate in Photo Lab; SEND | Submitted photo, tutorial advances | PASS: MISSION DATA SUBMITTED; SEND disabled; capture step | 021–022; 1 run |
| A06 / A | Capture target approach; lever at ~62-unit range | Reject invalid attempt without fake progress | PASS: TOO FAR; no capture completion claimed | 033–035; diagnosed later |
| A07 / A | ~18-unit range, depths 324.8 then 306 against displayed 330 | Explain invalid depth; allow retry | PASS rejection; feedback initially misinterpreted by agent | 039–045; 3 rejected attempts; not a game softlock |
| A08 / A | Energy3; computer → Rest → confirmation | Day2; refill in place; photo retained | PASS: energy100, same (1136,270.7); daily target moved | 048–052; 1 run |
| A09 / A | Depth337.4, ~22 units from Day2 target; capture | Capture only inside valid range | BLOCKED progression attempt: TooFar remained after Continue; later diagnosed as 21.66>20 | 053–060; repeated twice; positioning error |
| A10 / A | Pause → Main Menu → Continue | Recover same voyage/tutorial | PASS scoped restore; capture still correctly rejected | 057–060; 1 run |
| B01 / B | Inspect saved/runtime capture rejection | Distinguish bug from driver mistake | PASS diagnosis: no contradiction; old depth<330, new distance21.66>20 | mode-b-diagnosis.json; developer logs |
| B02 / B | Correct heading/position with helm controls; lever | Real minigame opens | PASS | 061–065; 1 run |
| B03 / B | Active minigame, no steering while inspecting | Finite attempt, ordinary retry | FAIL player attempt: timeout; input returns to cabin | 065–066; 1 attempt, not product defect |
| B04 / B | Retry, local W/S controller, no Tick or hit/reward calls | Earn five hits, one item | PASS: 2/5 captured mid-run, then Caught, cargo1 | 067–068, b-capture-hits*.png, actions; 1 attempt |
| B05 / B | Remaining Zone01 objective; automatic helm input route | Navigate without direct position writes | PASS first leg: (1179.8,250.1)→(999.7,726.3), depth231.5; stopped | 072–074, helm trace; 1 leg |
| B06 / B | Energy32 at northern location; Rest | Day3, same position and earned cargo | PASS scoped UI flow; further checks pending | 076–078; 1 run |
| B07 / B | Northern photo approach, SEND, Modules → Install Pressure Hull | Zone01 fieldwork and gate earned | PASS: candidate then submitted, upgrade COMPLETE and route open | 079–089; 1 run; initial photo NoSubject at68.37>65 range, corrected through helm |
| B08 / B | Rest Day4; sail into (1875,680) exit region | Zone02 loads with ship/resources retained | PASS: Zone02 (45,595), depth463.5, energy52.039 | 090–092 and action trace; 1 run |
| B09 / B | Sail Zone02 into return region (205,635) | Safe Zone01 arrival without gate loop | PASS: Zone01 (1735,680), Day4, depth463.5, energy46.15; stable cabin | 093 and trace; 1 run |
| B10 / B | Journal previous Day2 → Restore → Confirm | Future north photo/recipe/module/route/Zone02 removed; early photo/item retained | PASS: exact Day2 objectives L1_PHOTO/L2_COLLECT, one cargo item, one photo, no unlocks; tutorial knowledge retained intentionally | 094–097; b-before-journal.json, b-after-journal.json; 1 cycle |
| B11 / B | Rest, helm dive/approach, north camera → SEND → module install | Objective repeatable, route reopened without duplicate rewards | PASS: 4 objectives, 4 unique receipts; route unlocked; gallery2 excludes abandoned-future photos | 098–101, b-after-replay.json; 1 replay |
| B12 / B | Pause → Main Menu → Continue after replay | Replayed completion persists | PASS: Day3 Zone01, same 4 objectives/receipts and gallery2 | 102–103, runtime observation; 1 restart |

## Explicit limits

No full black-box playthrough or normal ending reached. No claim about human capture difficulty. Runtime geometry steering is labelled Mode B. Standalone OS lifecycle and release-only behavior remain unverified. The first Editor play probe occurred before transport compilation; no New Game was selected in that probe. A saved Editor restart resolved a stale assembly reload/compilation state. That preflight issue is not a game defect.
