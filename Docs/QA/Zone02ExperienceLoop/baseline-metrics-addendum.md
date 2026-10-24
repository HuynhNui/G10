# Baseline input accounting correction

The reusable offline summarizer now treats `ZoneTwoChartInputDriver` as a separate input owner. Its actual bounded ownership is measured from `input.navigation.start` to `input.navigation.stop`; toolkit keyboard/pointer releases cannot silently remove that interval. These spans include ordinary steering decisions and any neutral frames within the active driver. They are not a count of continuously depressed W frames. Actual navigation motion remains measured separately from passive navigation state.

Both `baseline/evidence/event-index.json` and `baseline/continuation/event-index.json` were recomputed from the preserved original JSONL/WAV. Videos and raw observations were not rewritten. A subsequent documentation refresh updated `01_Baseline_Playtest.md` and both embedded indices in `01_Baseline_Timeline.json` to these corrected values while retaining the original segment labels, blocker observations and29m31.125 completion clock.

| Metric | Original segment | Continuation |
|---|---:|---:|
| Full recording wall time | 509.932 s | 1289.996 s |
| Ready Zone02 interval before its departure | 453.904 s | 1214.654 s |
| Actual navigation activity (all axes; overlapping subcategories) | 26.704 s | 193.653 s |
| Chart-driver ownership, whole recording | 0.000 s | 149.782 s |
| Chart-driver ownership within ready Zone02 | 0.000 s | 145.803 s |
| Explicitly paired operator-gap spans | 0 measured; unmarked | 321.253 s (9 spans) |
| Zone02 time without any recorded input owner, corrected | 415.842 s | 1011.079 s |
| No recorded input owner, outside explicitly paired operator gaps | 415.842 s | 689.987 s |
| Observed paused/scaled-zero Zone02 interval | 0.000 s | 0.642 s |

The continuation has one `operator.gap.end` at 74.598 seconds without a corresponding start. Its preceding duration is unknown and is not included in the 321.253 seconds. Unmarked time cannot be assigned to confusion, fear, recovery or operator processing. Explicit gaps likewise describe operator activity, not human emotional response. Computer/Rest/Upgrade/capture visibility durations can overlap these gaps and must not be added as exclusive categories.

The historical `setup_loading_seconds` field measures *everything before the first ready Zone02 state*. In the continuation that includes loading and the real Zone01 return route, so its 47.164 seconds must not be labelled pure loading. Authoritative Zone02 reentry occurs at 46.180 seconds; ready restored Zone02 appears at 47.205 seconds. The separate chart driver ran for 3.980 seconds outside the ready Zone02 interval, mainly the legitimate Zone01 reentry approach plus the final driver's short stop delay after Zone03 authority changed. These are prerequisite-repair and route confounds.

The combined ready-to-ready duration **29m31.125s is correct to the reported millisecond**: `509.932 − 5.162542 + 1266.355828 = 1771.125286` seconds. Here the first term is the original whole recording, the subtraction removes its initial setup, and the final term stops the continuation at the first settled Zone03 state. It excludes the inter-recording repair gap and the late completion annotation, while retaining original post-return investigation, continuation loading, real Zone01 travel, operator reasoning, Rest and collision recovery. It is not a human completion benchmark or evidence that the horror redesign changes completion speed.

For the final recording, invoke the same summarizer with a distinct segment/status and omit `--initial-report-dir` so the retained original baseline report is not overwritten. Preserve the same input protocol and report differences in route, controller assistance, resources, rest, operator markers and prerequisite fixes before comparing elapsed times. The original audio results remain 2 rail samples; continuation remains 7 rail samples, with no changes to the actual WAV files or encoded VFR review recordings.
