# Independent final design assessment

This is an independent designer reading of the one completed final input-driven voyage, using the same rubric as `02_Experience_Audit.md`, the actual images reviewed in `visual-review-notes.md`, and read-only raw final frame metadata. It is not human feedback or subjective listening. It introduces no second redesign cycle.

## Verdict from the player-facing design

The clearest improvement is that the presentation now connects to an ordinary action: after sustained propulsion, a real stop receives an exterior response, before later instrument disturbances. The game supplies an additional reason to reinterpret the space outside the cabin. Radar distortion still occurs during a genuine scan, the Computer disturbance is actually rendered, and the reflection now survives across several real captured frames. These are concrete improvements in delivery and relationship between moments, while normal missions and travel remain playable.

The stronger claim—that players remain afraid, believe they are being followed, or voluntarily check instruments because of that suspicion—is unproven. The operator's Radar recheck was explicitly planned, not a demonstrated fear response. The final result is a better-supported horror design hypothesis and more reliable opportunity to experience it; it is not a human-validated frightening chapter.

## Same rubric, conservative independent ratings

Scale retained from the baseline:1 absent/broken;2 isolated or inconsistent;3 coherent and perceptible with limitations;4 sustained/contextual with tested recovery;5 consistently effective across representative human playtests. Five is unavailable without that human evidence. Scores are ordinal design judgments, not measured emotional quantities.

| Dimension | Baseline | Final | Reason for this independent score |
|---|---:|---:|---|
| Atmosphere |3|3|The painted cabin remains consistent, and actual Photo Lab edge changes are established with an identical sampled center. Local animation improves the image, but ordinary travel remains largely instrument/art driven; no human soundtrack or sustained-atmosphere judgment is available. |
| Anticipation |2|3|A real normal Radar sweep/preparation precedes visible corruption. The new exterior response follows a genuine stop. The preparation is short/subtle and its recognition has not been tested with a listener. |
| Uncertainty |3|3|Following, knocks, unseen scraping and the faint figure form a stronger related vocabulary. No genuine human expectation change, source search or voluntary rechecking was measured, so an extra number would overstate the evidence. |
| Pacing |2|3|Contextual stop response, actual delivered sequence, recovery and delayed receipt semantics address concrete baseline brittleness. Long task stretches, two Rest actions and operator-confounded gaps remain; the chapter has not established sustained tension throughout them. |
| Event impact |3|3|Radar/Computer visibility is confirmed, and three actual reflection samples replace one sampled baseline glimpse. The figure remains small/faint; Rest distortion remains substantial. More reliable exposure is established, while subjective impact is not. |
| Player agency |3|4|Actual PHOTO→SEND→CAPTURE, authored Hull installation and physical exit all succeed. Presentation does not fabricate contacts or lock controls. Focused native tests establish cancellation/retry/rollback without burning an unseen beat. The separate return-gate prerequisite repair must not be credited as horror effectiveness. |

These intentionally do not raise every score. A dimension can improve within its existing band. The final comparison may adopt its own justified scores, but should preserve the distinction between delivery evidence and emotional inference.

## Actual chronology and long gaps

From the captured frame index, first dramatic request samples occur at following333.378s, Radar361.127s, knocks394.434s, scrape505.398s and reflection827.984s. First corresponding durable receipts appear at334.225s,361.944s,395.444s,509.223s and829.085s. These are sampled observation times, not sample-accurate cue onsets. Actual Radar/Computer/reflection image evidence is linked in the visual review; an audio-source entry alone does not establish recognition or spatial perception.

The largest sampled inter-dramatic-request gap remains about322.586recorded wall seconds, from scrape to reflection. The baseline audit's comparable raw gap was517.456s. The final interval contains second-site travel, PHOTO/SEND/CAPTURE, upgrade, Rest and numerous explicit operator-review gaps, including one about38s while inspecting capture/material requirements. Thus it is not322.586s of silence or game-imposed waiting. Shorter raw gaps and faster completion are confounded by route learning, fewer navigation recoveries, the repaired prerequisite blocker and operator overhead. Use the dedicated measurement report for adjusted clocks; do not infer fear or confusion from neutral time.

## Remaining player-experience risks

- Both specimen visits still repeat PHOTO → leave to Photo Lab → SEND → return to cabin → capture. The final first capture also required operator clarification of visible UI/modal state. This remains an attention/flow cost even though progression works.
- The two specimen photographs use closely related repeated plant/creature imagery: compare [first photo203.001s](evidence/frames/frame-000972.jpg) with [second photo702.044s](evidence/frames/frame-003384.jpg). Edge movement does not create a substantially different discovery at the second site.
- Two genuine Rest actions still interrupt traversal. Their resource/day decisions are meaningful, but the scrape again overlaps reading the Rest app. Large controls are identifiable; small text visibly doubles. Readability/comfort was not established by merely reducing an effect parameter.
- The longer glass figure offers a better notice window, while its small placement competes with coordinate/heading reading. No measured human gaze or recognition evidence exists.
- Panned/filter-based2D audio is a presentation hint. Whether it feels external, rearward, organic, repeated, well mixed or frightening needs actual headset listening; PCM/source statistics cannot answer those questions.
- The long inter-site mission stretch still asks whether sustained curiosity carries the chapter between beats. The added following relationship improves that question's premise, not its proven answer.

These are remaining uncertainties and human-testing hypotheses, not instructions to begin another implementation cycle.

## Zone03 handoff: source-state cleanup check

Authoritative `zone=Zone03` first appears at991.634s while `transitioning=true` and the departure root/groan sources are still present. That is the intended departure overlap, not settled-zone bleed.

The first actual settled captured state is [frame004798 at997.227788s](evidence/frames/frame-004798.jpg): `transitioning=false`, position `(11,97)`, depth461.4444m, energy49.4353, Hull100. Its playing-source list contains only Zone03 `sparse_water`, `ambient_stone` and shared cabin ventilation/engine. The same check across all122settled Zone03 captured frames through1022.075473s finds zero entries for `ambient_roots`, `root_flex`, `root_friction`, `following_tail`, `hull_scrape`, `knocks_rear`, `outside_movement`, `far_root_groan`, `specimen_adhesive` or `specimen_armor`.

This supports correct scene-local source cleanup for the observed24.848s settled interval. It is not an auditory proof about reverb tails or a listening-device measurement. The coordinator's1005.365s inspection is a later observation marker, not the earliest captured settled-arrival clock; the final timing report should use the consistently defined clock.
