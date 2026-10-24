# Remaining final evidence and comparison obligations

This is a preparation checklist/template, not a final result. It was checked against the user's attached one-cycle brief and the recorded baseline. Root remains the sole Unity operator. Do not start another redesign cycle after the final evaluation.

## Before the single final voyage

- Finish relevant native regression checks and record exact results in `04_Implementation.md`, including any remaining failures. Tests prove functional behavior only.
- Use a fresh isolated final save/photo folder and the same legitimate Zone02 Debug preset: Zone01 complete, Zone02 missions/receipts untouched, regular upgrades NONE, default resources/difficulty, normal speed/controls. Preserve the original baseline and user saves. Record the initial ready-state snapshot, not merely a Debug click or observer annotation.
- State that the prerequisite return-intent repair is already installed. Final navigation therefore differs from the original baseline failure; the repair's effect cannot be attributed to horror changes.
- Start the actual frame/audio/telemetry recording before gameplay setup if practical. Keep Game View rendered. Retain original JSONL, frame JPEGs, actual WAV and recorder summary, even if a review MP4 is produced.
- Keep the same input philosophy: actual keyboard/mouse, normal ship movement, real SEND/Upgrade/Rest/capture UI. Chart-waypoint and capture tracking assistance must be disclosed. No teleport, objective/resource injection, forced cue, manual ticking or direct exit shortcut.

## During the final voyage

- Complete first organism PHOTO → actual Photo Lab SEND → actual CAPTURE; repeat for the second organism; install Pressure Hull through Upgrade; physically cross the exit and observe settled Zone03.
- Track actual position/depth, panel/app, objective/receipt, source playback and real surface exposure. Record unexpected audience changes, cancellation, retries, missed or barely visible cues without silently correcting the measured result.
- Log operator processing with paired `operator.gap.start/end`; missing boundaries remain unknown. Do not label neutral control time as fear, confusion, recovery or interface comprehension without evidence.
- Separate setup/loading/transition, normal navigation, interface visibility, actual capture UI, Rest, pause and operator processing. These durations overlap unless explicitly partitioned.
- Preserve actual collision/resource consequences and ordinary recovery. In the baseline the one 18HP wall impact, reverse and chart detour were navigation recovery, not a fear response.
- At ordinary waypoint stops, observe whether the new optional Following opportunity occurs naturally. A Following start counter is distinct from a durable receipt and from perceptible audio. Do not force playback to fill an exposure quota.
- Check Radar remains usable, real contacts/charges unchanged, SEND/capture/Hull progression intuitive, relevant controls readable, Photo Lab motion bounded and departure cleanup intact. Record evidence; do not infer subjective sound quality from PCM peaks or source flags.
- Stop measured completion at both the first authoritative Zone03 transition and its first settled ready state. A later completion marker does not define either clock. Retain post-arrival evidence separately.

## Exact offline command after recording

The following assumes the final evidence folder is `D:\G10\Docs\QA\Zone02ExperienceLoop\final\evidence`. Use the actual folder if root chooses another path. The status text deliberately does not assert completion; the real transition/ready rows establish that result.

```powershell
python 'D:\G10\Tools\QA\summarize_zone02_experience.py' 'D:\G10\Docs\QA\Zone02ExperienceLoop\final\evidence' --segment final-real-input-voyage --status 'final recording; completion determined from observed transition and ready state' --mux > 'D:\G10\Docs\QA\Zone02ExperienceLoop\final\evidence\analysis-console.json'
```

Do **not** pass `--initial-report-dir`: that option writes an original-baseline template and would overwrite the preserved baseline report. Root must separately author `05_Final_Playtest.md` and `05_Final_Timeline.json`; the generated `event-index.json` can provide the latter's source data.

The command writes `event-index.json`, a genuine VFR `gameplay.mp4`, `frames-vfr.ffconcat`, `mux-command.json`, `mux-result.json` and an ffmpeg log. Review the mux result, original frame gaps, audio callbacks/listener replacements and dropped samples before linking it as evidence. A/V alignment remains approximate; original PCM is not padded with generated missing audio.

Optional final instrumentation is reported in `optional_presentation_telemetry`: stage/pending fields, Following starts, actual Radar/Computer/Reflection surface and Photo Lab motion intervals, and scene-flow transition state. Missing baseline fields are **unknown**, not evidence of zero baseline exposure. A true surface flag is not proof that a figure was readable, captured in a JPEG or attended by a human; inspect the cited actual images. `following_start_counter_observations` and `presentation_delivery_receipt_observations` are separate evidence lists. Stage/pending changes are excluded from candidate event-gap metrics.

## Remaining artifacts

- `05_Final_Playtest.md`: exact initial state and input protocol; actual completed journey or honest blocker; real clocks/category definitions; event-by-event exposure/context; screenshots/audio/video links; defects; operator/automation limits.
- `05_Final_Timeline.json`: actual timestamped final state/input/event evidence, with exposure observations and completion boundaries. Preserve the generated raw event index as supporting evidence.
- `06_Comparison.md`: required metric table, two annotated experience timelines, specific before/after moments, final verdict, resource/dependency provenance, technical regressions and unresolved human-validation hypotheses.
- Keep `04_Implementation.md` current with the completed native regression results and asset/source change log. Link any functional fixes separately from the creative redesign.

## Baseline values to carry forward

| Baseline measure | Recorded value / interpretation |
|---|---|
| Whole evidence duration | 509.932 + 1289.996 = **1799.928 s** |
| Initial ready Zone02 → settled Zone03, segmented | **1771.125 s (29m31.125s)**; repair gap excluded, original wrong return/real reentry/operator time retained |
| Continuation first Zone03 authority / settled ready | **1261.859 / 1266.356 s**; late completion marker excluded |
| Navigation activity, any axis | **220.357 s**; subcategories overlap |
| Horizontal / turning / depth | **158.640 / 29.861 / 45.829 s** from raw values |
| Capture UI / Computer UI / Photo Lab visible | **41.081 / 216.572 / 16.687 s**; includes operator waits |
| Rest | Two actual confirmations, day2 at continuation **360.662 s**, day3 at **785.103 s** |
| Chart-driver ownership | **149.782 s** continuation overall, **145.803 s** within its ready Zone02 interval |
| Explicitly matched operator gaps | **321.253 s**, nine spans in continuation; original unmarked and one continuation end marker unmatched |
| Time without any recorded input owner | **1426.921 s** combined ready Zone02 intervals; **not idle/confusion time** |
| Literal idle/confusion or rest/recovery emotion | **Not established**; categories cannot be reconstructed fairly from neutral input alone |
| Dramatic beat exposure | Real Radar/Computer distortion and one faint figure frame reviewed; knock source/PCM exposure opportunity exists. No human recognition/fear measured. |
| Common dramatic intervals, continuation | Radar→knocks **38.584 s**; knocks→scrape **117.760 s**; scrape→reflection **517.456 s**; reflection→settled Zone03 **445.726 s** |
| Longest literal uneventful interval | Not established as literal inactivity/silence. **517.456 s is an inter-dramatic proxy**, containing fieldwork, roots and operator waits. |
| Blocking defects | One original unintended Zone01 return; repaired before same-save continuation. No horror changes during baseline. |
| Baseline design ratings | Atmosphere **3**, anticipation **2**, uncertainty **3**, pacing **2**, event impact **3**, agency **3**; reuse the explicit rubric in `02_Experience_Audit.md`. |

The continuation's historical `setup_loading_seconds` includes real Zone01 reentry routing before ready Zone02 and must not be called pure loading. Read `baseline-metrics-addendum.md` and the corrected segment event indexes, now also refreshed in the combined baseline JSON. No baseline optional-surface absence may be compared against final instrumentation as “0 → N confirmed exposure.”

## Concise `06_Comparison.md` template

```markdown
# Zone02 one-cycle comparison

Outcome: [what actual final evidence supports]. Baseline completed after the
separate return-gate repair; final [completed/blocked]. Human fear was not measured.

| Metric | Baseline | Improved | Interpretation / evidence |
|---|---|---|---|
| Completion, ready→settled Zone03 | 1771.125s segmented | [measured] | [repair, route learning, operator and controller confounds] |
| Navigation activity | 220.357s | [measured] | [same definition; overlapping axes] |
| Idle/confusion | Not separable | [supported / unknown] | [do not substitute neutral input] |
| Meaningful event exposure | [reviewed baseline moments] | [reviewed final moments] | [receipt vs start vs real visual/audio] |
| Longest uneventful interval | Not established literally; 517.456s inter-dramatic proxy | [same proxy plus limitations] | [fieldwork/roots/operator time retained] |
| Atmosphere | 3/5 | [rating] | [same rubric, observed support] |
| Anticipation | 2/5 | [rating] | [actual preparation and later interpretation] |
| Uncertainty | 3/5 | [rating] | [coherent hints, no emotional certainty] |
| Pacing | 2/5 | [rating] | [opportunities, recovery, missed windows] |
| Event impact | 3/5 | [rating] | [actual audience/surface/source review] |
| Player agency | 3/5 | [rating] | [normal controls/data and retry behavior] |
| Blocking bugs | Original return defect, separately repaired | [observed] | [separate repair from horror] |

Annotated baseline timeline: [original + continuation clocks, objective/cue,
actual exposure, inferred tension contribution and supporting frame/audio].
Annotated final timeline: [same fields and rubric; optional Following separate].

Specific comparisons:
- Radar preparation and distortion: [before frame/time] → [after frame/time].
- Knocks / natural stop / Following connection: [observed evidence].
- Computer scrape versus readable resource UI: [evidence].
- Hull success, recovery, figure delivery and physical departure: [evidence].
- Real Photo Lab motion: [archive preserved, actual display visible, limitations].

Final verdict: original problem; implemented change; introduced audio/shader/
material/library and provenance; whether evidence supports delivery/coherence;
actual timing difference with confounds; exposure difference; regressions;
remaining hypotheses requiring human/headset playtests. No second redesign run.

Evidence: [05 report, final timeline, raw/encoded recordings, original baseline,
02 audit, 03 redesign, 04 implementation/regression results, provenance].
```

For gap comparisons, retain the same four common beats separately from the expanded final chain that includes Following. For the two annotated tension timelines, ratings must remain explicitly **designer hypotheses**, each supported by an observation; neither source counts, technical test success nor smoother automated steering proves greater human fear.

The final user response should briefly state the implemented outcome, actual baseline/final completion evidence and relevant limitations, link `06_Comparison.md` plus usable recordings, and state any remaining incomplete requirement honestly. No commit, merge, push, purchase or second improvement cycle is authorized by this checklist.
