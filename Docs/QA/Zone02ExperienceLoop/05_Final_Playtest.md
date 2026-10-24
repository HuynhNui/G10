# Final real-input voyage — completed physical Zone03

The single final voyage completed both PHOTO → actual Photo Lab SEND → actual CAPTURE requirements, installed Pressure Hull through Upgrade, used two real Rest confirmations, and physically reached Zone03. No teleport, objective/resource injection, forced cue or manually advanced game clock was used. The new Following opportunity occurred during an ordinary navigation stop. No second redesign/playtest cycle was started after this evaluation.

Baseline and final initial Debug saves are byte-identical:5608 bytes, SHA256 `873E418A7E94A108150FD51FC6F03A49C959E3DE4F82AD76FEDB7F387BC1D0EC`. Day1/Zone02, position(45,595), heading90°, depth230m,100 Energy/100 Hull,10 Radar/20 Photo/5 Capture charges,500m initial maximum depth, regular upgrades NONE, prior Bio Lamp progression, empty Zone02 objectives/receipts and both day1 organism spawns match. See [initial-state comparison](final/initial-state-comparison.json).

## Completion clocks and actual controls

All clocks below are real elapsed seconds within `final/evidence`, not native test duration or a late observer annotation.

| Boundary | Recording elapsed |
|---|---:|
| Whole recording, including menu preparation and post-arrival review |1022.537s|
| First authoritative Zone02 selection |42.003613s|
| First initialized Zone02 position/resources |43.419955s|
| Initial scene-flow transition finished; vessel ready |43.621536s|
| Actual physical Zone03 transition authority |991.634084s|
| First initialized Zone03 position/resources |995.818830s|
| Zone03 scene-flow transition finished; settled arrival |997.227788s|
| Resource-ready → resource-ready duration |**952.398875s (15m52.399s)**|
| Scene-flow-ready → scene-flow-ready duration |**953.606253s (15m53.606s)**|
| Post-settled-arrival recording retained |25.309s|

The `voyage.entry` annotation77.046s, later observed arrival1005.365s, and `final.completed`1022.528s are not start/end clocks. The existing baseline clock1771.125s uses initialized position/resources because the old telemetry lacks an explicit `transitioning` field. The final report therefore preserves both readiness definitions instead of silently treating loading/fade as controlled play. The final resource-ready game-time delta was950.074s and its unscaled-time delta952.203s; real wall time remains the completion measure. All sampled final `timeScale` values were1.0.

Input used the same existing-device InputSystem tools, bounded pointer holds and optional chart-waypoint steering. The chart driver only queued ordinary W/A/D and required operator-selected waypoints; it did not plan a complete exit route or bypass terrain. Only the second capture explicitly activated the visible-position W/S tracking agent. These controls establish an actual playable journey, not a human skill or emotional benchmark.

The learned route and prerequisite return-gate repair avoided the baseline's unintended Zone01 trip and its later wall collision. The final vessel arrived at(11,97),461.444m depth,49.435 Energy/100 Hull on day3. Those route/operator differences materially confound any elapsed-time improvement attributed to horror presentation.

## Observed time categories

Durations integrate sampled state or explicit marker intervals. Categories overlap and must not be added.

| Category | Actual observed duration |
|---|---:|
| Initialized Zone02 interval until Zone03 authority |948.214s|
| Navigation activity, any axis |212.496s|
| Horizontal / depth / turning |155.951 /45.919 /23.525s|
| Chart-driver ownership |158.168s overall;158.140s within initialized Zone02|
| Toolkit pointer / keyboard ownership |49.003 /2.394s overall|
| Capture UI, both attempts |64.849s|
| Second capture tracking-agent ownership |31.240s|
| Computer UI / Photo Lab visible |116.427 /44.500s|
| Rest UI / Upgrade UI visible |48.719 /17.145s|
| Explicit operator gaps,28 paired intervals |540.292s overall;523.129s within initialized Zone02|
| Zone02 time without any recorded input owner |707.678s|
| No input owner outside explicitly paired operator gaps |200.455s, interpretation unknown|

There are no unmatched final operator-gap markers. Their coverage is better than the partly marked baseline; this does not make the two processing totals directly comparable. One final operator gap overlaps active capture tracking. Neutral control time includes quiet gameplay, review, comprehension and other unknown activity. No fair confusion/fear/recovery duration is inferred. Pre-ready43.622s includes menu selection/operator preparation as well as loading; it is not43s of pure loading. Scene-flow flags identify four transition spans totaling11.188s across initial load, two Rest operations and exit; these are not horror exposures.

## Actual mission journey and operator interpretation incident

| Moment | Actual observation |
|---|---|
| First organism |Photo count1 at188.379s; SEND commits `Z2_L1_PHOTO`214.956s; actual capture UI231.466–263.602s; `Z2_L1_CAPTURE`/adhesive receipt263.602s.|
| First-capture operator latency |A231.449s status/observer sample was still inactive, about17ms before the first recorded active modal. The operator later interpreted that stale sample as a failure and clicked the Helm hotspot at261.812s while capture was active. The existing modal blocked the click; no navigation occurred. Normal hook motion reached five hits by262.214s and capture completed263.602s. This is a premature sample/operator interpretation issue, not a demonstrated new game defect or a hidden completion action.|
| Inter-site navigation and Rest |Normal waypoint steering crossed the repaired outbound corridor. Real Rest confirmation advanced to day2 at531.463s, preserving cargo/progression.|
| Second organism |Photo count2 at698.995s; actual SEND commits `Z2_L2_PHOTO`716.585s. Capture UI721.036–753.749s; visible-position tracking722.536–753.776s; five actual hits and `Z2_L2_CAPTURE`753.749s.|
| Pressure Hull |Real Upgrade commits `Z2_GATE_INSTALL_PRESSURE_HULL` and installation receipt792.471s. No mission count/location/cost was altered for this measured run.|
| Resource recovery and physical exit |Second actual Rest confirmation advances to day3 at818.309s. Learned normal detour clears the narrow bend without collision; original Hull remains100. Actual Zone03 authority991.634s and settled readiness997.228s.|

Both mission photograph submissions, captures, installation, Rest and physical transfer completed. There was no final blocking gameplay defect observed along this journey. This observation does not establish absence of defects on untested routes. Functional cancellation, Continue/Journal, accessibility and archive/material correctness remain separately documented in `04_Implementation.md`.

## Presentation encountered: requests, delivery and actual surfaces

| Beat | Actual start / durable receipt | Exposure evidence and context |
|---|---|---|
| Following |Start counter333.363s; receipt334.225s|One actual stop after normal propulsion. `following_tail` source is observed playing333.566–336.994s, estimated3.428s,volume0.650/pan+0.25. Root/water bed withdrew while engine/ventilation sources stayed at0.500. This establishes contextual playback opportunity; it does not prove rear positioning or human recognition.|
| Radar |Start361.124s; receipt361.944s|Actual Radar signal flag361.728–362.347s,estimated0.620s,three captured true-flag frames. [Frame001739](final/evidence/frames/frame-001739.jpg) was visually reviewed: scoped grain/tear is present on the real disk, while scan/charge text remains visible. Normal scan/contact authority stayed intact.|
| Knocks |Start394.425s; receipt395.431s|Moving Helm; actual `knocks_rear` source-state window394.425–398.044s,estimated3.619s. Retained listener PCM supplies actual audio, without a claim that an AI/human heard or feared its direction.|
| Scrape |Start505.395s; receipt509.223s|Computer opened during actual resource management, then Rest app. Actual interference flag506.857–507.601s,estimated0.744s/three frames. [Frame002444](final/evidence/frames/frame-002444.jpg) was reviewed: broad analog bands/RGB separation remain visible over Rest; text/buttons remain recognizable but still compete with interference. `hull_scrape` source-state window is estimated5.596s.|
| Hull recovery |Objective/receipt792.471s|Installation is a real mission success anchor. Actual `hull_install` source-state exposure estimated3.856s. Second Rest then returns normal control.|
| Reflection |Start827.973s; receipt829.085s|Actual anomaly flag828.778–829.489s,estimated0.711s/three captured frames. [Frame003984](final/evidence/frames/frame-003984.jpg) was reviewed: a faint upright dark form appears on the small Helm chart glass with viewport dimming. Its legibility/attention remains a human-validation question.|
| Photo Lab motion |Two true-flag intervals201.432–228.794s and700.903–718.041s|Preview displayed with motion active for44.500s,212 captured true-flag frames. Independent comparisons of actual JPEG pairs000972→001007 and001007→001054 found49,928 central pixels unchanged, while painted edge regions changed. This establishes recorded edge motion beyond material-binding telemetry; it does not establish human noticeability. See final/visual-review-notes.md and separate native UV/archive checks.|
| Departure |Zone03 authority991.634s; scene-flow ready997.228s|Physical gate, actual new zone position/resources, no forced departure or objectives. Final current surface flags are false and dramatic receipts belong to the prior zone; normal Zone03 sources remain.|

All four common dramatic starts and the new optional Following were encountered in this final route. Every listed durable delivery followed its request/source or visible-surface opportunity. A receipt alone is not evidence of actual image content or human attention. Baseline optional flags are missing/unknown; their absence must not be counted as zero baseline exposure. Compared with the baseline's one reviewed faint-figure frame, this run contains three true-flag figure frames, with the actual dark form visible on review. A single run does not establish population-level detection probability.

The planned radar recheck after the Following stop was an operator action, explicitly labelled as planned in the log. It is not spontaneous fear/uncertainty behavior. No emotional reaction, hesitation-as-fear or subjective auditory quality is claimed.

## Event gaps and remaining quiet periods

For comparison with the baseline's request-time receipts, use first start counters separately from the redesigned delayed delivery receipts:

| Interval | Start-counter wall gap | Delivery-receipt wall gap |
|---|---:|---:|
| Radar → knocks |33.302s|33.487s|
| Knocks → scrape |110.970s|113.792s|
| Scrape → reflection |**322.577s**|319.862s|

Following starts27.760s before Radar. Reflection start→settled Zone03 remains169.255s. The largest common four-beat start gap is322.577s; baseline's comparable inter-dramatic proxy was517.456s. These gaps contain fieldwork, background roots, Rest and external reasoning. They are not literal silence, idle time or a measured emotional curve, and a faster learned route contributes to their reduction. Literal longest meaningful-gameplay-free time has not been established from this agent run. Stage/pending state changes are not counted as extra scares to shorten these gaps.

## Actual recording and preservation limits

- [Real VFR gameplay video](final/evidence/gameplay.mp4), [actual listener WAV](final/evidence/actual-game-audio.wav), [frame metadata](final/evidence/frames.jsonl), [raw telemetry/input log](final/evidence/timeline.jsonl).
- [Final annotated timeline](05_Final_Timeline.json), [generated event index](final/evidence/event-index.json), [mux metadata](final/evidence/mux-result.json), [exact recording summary](final/evidence/summary.txt).

The recording retains4920 genuine full-resolution source images; VFR review is960×540 H264 with actual AAC audio. The mux uses real frame durations and a final hold of the last source image, not interpolated/constant5fps imagery. Recorder reports87 skipped target slots; largest actual image gap0.995s overall/0.719s after resource readiness. Capture overhead and sampled-state interpolation limit exact short-event duration estimates.

Actual WAV:49067008 stereo sample frames at48kHz,1022.229333s of captured PCM,98,134,016 sample values. There are10 saturation-rail values in8 sample frames (0.00001019%); RMS−26.321dBFS. This does not support a broad clipping claim or establish subjective mix quality. No dropped ring samples or format changes were reported. Listener replacement creates an inferred≈0.300338s timestamp discontinuity; no substitute PCM silence was generated. DSP/device latency, JPEG sampling and AAC block/gap behavior prevent sample-accurate sensory alignment. The original WAV/frames remain authoritative.

Root's post-final hash audit found **all six persistent files unchanged from their pre-final hashes**: [save-hashes-after-final.json](save-hashes-after-final.json). This supports the corrected isolation for this final shutdown. It does not recover the earlier missing original older normal backup/debug pair: [save-preservation-incident.md](save-preservation-incident.md). The original normal primary had already been recovered byte-exact before final. These facts must remain distinct in the final comparison.

This completes the actual final voyage and recording portion of the one authorized cycle. The evidence supports delivery/context and retained gameplay authority. Human fear, sustained anticipation, sound localization, clarity of the faint figure and perceptibility of subtle photo motion still require human/headset playtesting. No further redesign was performed after the final observations.
