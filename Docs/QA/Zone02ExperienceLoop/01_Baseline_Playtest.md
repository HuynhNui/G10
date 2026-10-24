# Baseline real playtest — original failure and same-save continuation

## Completed baseline continuation

The baseline has now reached a settled Zone03 through the actual physical exit. This is one baseline voyage interrupted by a documented prerequisite gate repair, then continued from its original isolated debug save. No horror controller, effect, sound, mission/map geometry or difficulty was changed during the baseline. The original blocked segment below remains part of the evidence.

- Segment1 recorded509.932s. Continuation recorded1289.996s; total1799.928s. The between-recording repair interval is excluded.
- From the first ready Zone02 state to settled Zone03, the sum of recorded wall intervals is1771.125s (29m31.125s), including external operator reasoning, the unintended Zone01 visit and its real return journey. This is not a human completion-time estimate or an unbiased performance benchmark.
- Continuation: authoritative Zone02 reentry46.180s, ready restored state47.205s; physical Zone03 authority1261.859s, settled arrival1266.356s. `baseline.completed`1289.546s is a late annotation and does not define completion.
- Continuation L2 SEND652.478s, L2 CAPTURE669.355s; Pressure Hull objective778.017s/installation receipt778.215s. Both materials were consumed through the real Upgrade UI, depth cap became750m. Two real Rest confirmations advanced to day2 at360.662s and day3 at785.103s.
- One actual terrain impact at830.891s reduced Hull100→82; normal S reverse, map inspection and a small physical detour resolved it. The response is navigation recovery, not fear.
- Summed nonexclusive navigation activity220.357s, horizontal motion158.640s, depth change45.829s, turning29.861s; actual capture UI41.081s. These categories overlap. Computer UI216.572s and Photo Lab16.687s are observed exposures, including reasoning gaps.
- Corrected input accounting includes the separate chart-driver keyboard owner:149.782s in the continuation overall,145.803s within its ready Zone02 interval. Time without any recorded input owner is415.842s in the original segment and1011.079s in the continuation,1426.921s combined. These are not idle/confusion measurements. Nine explicitly paired operator gaps total321.253s; one unmatched end marker and other unmarked time remain unknown. A fair confusion/idle total cannot be reconstructed. No human behavior/emotion feedback exists. The combined timeline now embeds the corrected segment indices; detailed definitions are in `baseline-metrics-addendum.md`.

[Continuation real VFR video](baseline/continuation/gameplay.mp4), [event index](baseline/continuation/event-index.json), [raw timeline](baseline/continuation/timeline.jsonl), [actual listener WAV](baseline/continuation/actual-game-audio.wav), [blocker repair and tests](blocker-repair.md).

Continuation recording:6234 genuine frames,94 skipped target slots; actual PCM1289.664s,48kHz/stereo,7 saturation-rail sample values out of123807744,0 dropped ring samples. Original rendering/audio remain retained; no subjective listening quality is inferred. Listener changes and DSP/device timing make A/V sync approximate, as recorded by the mux metadata.

## Original segment outcome at its stop: blocked (retained historical record)

**The original segment stopped BLOCKED; it was subsequently resumed from the same isolated save after the separate prerequisite repair, as documented above.** That first segment alone did not complete Zone02. The original voyage used the legitimate Zone02 debug preset and real InputSystem keyboard/mouse events. It completed the first organism's PHOTO → actual Photo Lab SEND → actual capture minigame. Forward navigation toward the second site crossed the return gate and transitioned to Zone01. No teleportation or objective/resource injection was used to claim completion.

The prerequisite progression defect was investigated and repaired separately from horror redesign. This failed segment alone did not establish final baseline experience conclusions. The completed same-save continuation preserves the original failure and identifies its repair and real reentry route; it does not replace the original voyage.

## Clock and fidelity — original segment only

| Observation | Measured value |
|---|---:|
| Recording wall time including setup and post-return investigation | 509.932 s |
| First authoritative expedition zone = Zone02 | 0.895 s after recording start |
| First ready default position/resources | 5.163 s after recording start |
| Late `voyage.entry` observer annotation | 54.523 s |
| Actual departure into Zone01 | 459.067 s after recording start |
| Ready Zone02 wall interval until departure | 453.904 s |
| Setup/loading before ready default state | 5.159 s |
| After-return investigation before stop | 50.865 s |
| Navigation motion (translation OR turning OR depth) | 26.704 s |
| Horizontal movement | 8.572 s |
| Depth movement | 10.274 s |
| Turning | 7.859 s |
| Actual capture UI active | 27.202 s |
| Automated visible-position capture steering | 11.346 s |
| Computer UI visible | 71.287 s |
| Photo Lab visible | 15.499 s |
| Zone02 time without injected controls | 415.842 s |
| Idle/confusion time | **Not separable from operator processing/visual review with this log** |

Durations integrate actual observed state intervals; listed categories overlap and must not be added. No-injected-control time includes operator screenshot review, interface comprehension, waiting, quiet gameplay and unknown gaps. It is not a fear, confusion or recovery metric. There are no explicit operator-processing or Editor-pause markers in this segment; all observed `timeScale` samples are 1.0. The game continued during external reasoning. `scene=Zone01` was already present while `ExpeditionLoop.Zone=Zone02`; scene name alone is not authoritative progression evidence. The entry annotation at ~54 seconds is late and is not used as the start clock.

## Observed journey and limitation

- Default ready state appears at 5.163 s: position (45,595), depth 230 m, 100 energy / 100 hull, 10 radar / 20 photo charges, untouched Zone02 objectives and receipts.
- Map opened at ~73.550 s; radar at ~106.276 s; depth changed through the real pointer-held control around 162.563–172.714 s.
- A photograph was taken at ~274.676 s. First photograph SEND committed `Z2_L1_PHOTO` at 326.837 s.
- Escape from Photo Lab returned to the computer desktop. The first capture hotspot click was therefore occluded by the computer, as explicitly annotated at ~364.445 s. This is a recorded interface misunderstanding, not a failed capture attempt.
- Real capture UI became active at ~366.460 s. The optional input-driving agent began at ~382.335 s and chose W/S from displayed hook/fish position and hook heading only. `Z2_L1_CAPTURE` and the `adhesive` cue receipt were observed at 393.662 s. Human motor skill/balance was not measured.
- W was actually held from ~452.613 s to ~460.012 s toward the corridor. The authoritative zone became Zone01 at 459.067 s; the blocker was annotated at ~509.930 s. The second organism, Pressure Hull installation and Zone03 transition were not completed.

Environment event counter reached 9 before departure; radar/knocks/scrape/reflection counters remained 0 and `adhesive` was the only persisted Zone02 cue observed. These are **receipts/counters/source-state evidence**, not yet confirmed sensory exposure. The exact event index links adjacent real images. No human fear or hesitation is claimed.

## Recording evidence and audio checks

- [Real variable-frame-timestamp gameplay video](baseline/evidence/gameplay.mp4)
- [Original frame metadata](baseline/evidence/frames.jsonl), [actual telemetry and input log](baseline/evidence/timeline.jsonl), [unmodified actual listener WAV](baseline/evidence/actual-game-audio.wav)
- [Derived event index and clock/audio metrics](baseline/evidence/event-index.json), [recording summary](baseline/evidence/summary.txt), [mux details](baseline/evidence/mux-result.json)

There are 2465 actual frames; recorder reported 42 skipped target 5 Hz slots. Longest actual frame gap is 3.596 s. Images are held for their true recorded duration in the VFR review video, with no interpolation. Original evidence is retained.

Actual audio is 509.674667 seconds of 48000 Hz / 2-channel PCM. Exact amplitude analysis found **2 sample values at PCM saturation rails** out of 48,928,768 (0.00000409%), across 1 sample frames. Two rail samples do not support a broad clipping problem or establish audible distortion. Captured RMS is -30.55 dBFS. No auditory perception or mix-quality judgment is claimed from these statistics.

The listener was replaced during loading. Callback Stopwatch timestamps and cumulative PCM counters expose this discontinuity; the review video preserves the estimated timestamp gap without synthesizing replacement audio. Initial callback alignment is bracketed by real observations. DSP block latency, device latency, inferred segment boundary and AAC packet rounding prevent sample-accurate sync. Recorder reports zero dropped ring samples, which does not erase listener replacement gaps. See event-index JSON and mux notes for exact clocks.

## Baseline boundary

The original failure and completed continuation are both preserved. Experience audit follows in `02_Experience_Audit.md`. Emotion ratings remain designer hypotheses; human feedback is absent. Final comparison must separate the gate repair, operator learning/route assistance and horror changes.

Later preservation audit found debug quit-save writes escaping the intended isolation at the two baseline shutdowns. Recorded in-play initial states and the actual journeys remain evidence, but the original user debug pair was overwritten and has not been recovered. The normal primary was separately recovered after a native-test interruption. See save-preservation-incident.md. Isolation intent in preparation/tooling notes must not be read as proof that every entry save was preserved.
