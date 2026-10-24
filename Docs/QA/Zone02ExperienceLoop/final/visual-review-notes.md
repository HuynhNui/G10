# Final voyage: partial visual evidence review

Read-only review while the final recording was still running. This is not a completion report, an encoded-video review or a second improvement cycle. No Unity calls, source edits, raw evidence changes, summarizer or mux were performed. Times below are `elapsedWall` seconds from `final/evidence/frames.jsonl`.

## Radar preparation and delivered anomaly

The operator used the real Radar scan. These actual captured images were opened and inspected:

| Elapsed | Actual frame | Observation |
|---:|---|---|
| 361.329 | [001736](evidence/frames/frame-001736.jpg) | Normal green sweep and readable scan controls. `pendingCue=radar`, no radar receipt; the preparation root-friction source is logged with pan−0.28. A source entry is not a subjective listening claim. |
| 361.735 | [001738](evidence/frames/frame-001738.jpg) | Clearly visible static and horizontal tear scoped to the circular Radar display. Real scan/radar-charge text remains visible. `radarVisual=true`; durable receipt still absent in this frame. |
| 361.944 | [001739](evidence/frames/frame-001739.jpg) | Scoped static remains visible; `radar` receipt is now present. This provides actual visual evidence before receipt publication. |
| 362.145 | [001740](evidence/frames/frame-001740.jpg) | Metadata records the same visible-audience interval. This adjacent image is indexed for follow-up video review; it was not separately opened in this partial inspection. |

This observed order supports preparation → rendered disturbance → durable delivery. The real frames demonstrate delivery more strongly than a counter or material flag alone. They do not establish whether a human notices the quiet preparation, interprets the static as pursuit, or trusts the instrument afterward. The Radar event was encountered in this final voyage; it should not be classified as an omitted optional opportunity.

## Photo Lab: actual recorded image differences

Opened and inspected [frame000972 at203.001s](evidence/frames/frame-000972.jpg), [frame001007 at210.058s](evidence/frames/frame-001007.jpg) and [frame001054 at219.964s](evidence/frames/frame-001054.jpg). All show the same first photograph in the real Photo Lab, `GoodPhoto`; the last shows `MISSION DATA SUBMITTED`. Metadata records `photoMotion=true`. The photographic composition and center subject remain stable. The movement is subtle in individual stills, so a read-only pixel comparison was also made on the original 1920×1080 JPEGs, without cropping or rewriting any evidence file.

Screen-space rectangular samples use Python/Pillow's exclusive right/bottom bounds: center `(1027,450)-(1343,608)` contains49,928pixels; left painted edge `(822,411)-(948,648)` and right painted edge `(1422,411)-(1548,648)` each contain29,862pixels. These regions sit inside the photo preview, away from the task text, SEND button and desktop clock.

| Actual frame pair | Center mean absolute RGB difference | Left edge mean absolute RGB difference | Right edge mean absolute RGB difference |
|---|---:|---:|---:|
| 000972→001007;7.057s apart | 0.00000/255; all49,928pixels identical | 4.49172/255 | 2.97118/255 |
| 001007→001054;9.906s apart | 0.00000/255; all49,928pixels identical | 4.60668/255 | 3.04563/255 |

For the first pair,17,021/29,862left-edge pixels and12,219/29,862right-edge pixels changed by more than3 in at least one RGB channel. For the second pair, the corresponding counts are16,942 and12,041. Whole-preview mean differences were1.19732/255 and1.32895/255. The localized edge changes alongside an exactly stable center support actual rendered display motion during this measured voyage, rather than merely a bound shader. JPEG encoding and post-render sampling limit this to evidence about these captured screen regions; the measurements are not a perception threshold, motion-quality rating or proof about every frame.

Archive/texture byte preservation is established separately by the focused native functional test, not by JPEGs. No live photo archive was changed or rewritten in this review. At228.995s metadata records Desktop with `photoMotion=false`, supporting cleanup after leaving Photo Lab; lifecycle/allocation claims belong to the functional tests.

## Scrape during real Computer/Rest use

Additional read-only inspection while the voyage remained live opened [frame002437 at505.398s](evidence/frames/frame-002437.jpg), [frame002444 at507.060s](evidence/frames/frame-002444.jpg), [frame002451 at508.610s](evidence/frames/frame-002451.jpg), [frame002452 at508.811s](evidence/frames/frame-002452.jpg), [frame002453 at509.014s](evidence/frames/frame-002453.jpg), [frame002454 at509.223s](evidence/frames/frame-002454.jpg) and [frame002469 at512.249s](evidence/frames/frame-002469.jpg).

The first is the normal Computer desktop with `pendingCue=scrape`. At507.060s the actual Rest window and desktop visibly show horizontal breaks, fine static and RGB separation. The large Rest/CANCEL controls, warning symbol and main instruction remain identifiable; small status text is visibly doubled. This is actual rendered signal failure, not merely `computerVisual=true`. Frames at506.859s,507.060s and507.402s are indexed by metadata as the active Computer distortion interval, with `hull_scrape` playback source recorded.

By508.610–509.223s the inspected Rest views are clean and readable again. The `scrape` receipt first appears in a captured frame at509.223s, after the inspected corruption. At512.249s the actual screen shows `LILY · Không có va chạm...` over the recovered Rest window. The sequence remained in ordinary resource-management context rather than forcing a new screen.

The0.14/0.18s dips can fall between5Hz captures. These inspected stills do not clearly isolate both dark pulses; do not claim two individually verified final-voyage dip images from this subset. Their authored/runtime functional verification is separate. Nor does reducing the effect parameter establish human reading comfort: the visible Rest/status distortion remains substantial, and the event still overlaps practical Rest reading, as it did in baseline. Successful continuation supports absence of a blocking defect, not that the interface distraction was eliminated.

## Reflection during actual exit approach

Opened [frame003982 at828.587s](evidence/frames/frame-003982.jpg), [003983 at828.787s](evidence/frames/frame-003983.jpg), [003984 at829.085s](evidence/frames/frame-003984.jpg), [003985 at829.287s](evidence/frames/frame-003985.jpg) and [003986 at829.489s](evidence/frames/frame-003986.jpg). All are the real Helm during motion toward the exit. The recorded X/Y readings continue to change; depth remains461.4m and controls remain readable.

The initial frame has the darkened Helm without a recognizable figure. The next three images contain a faint upright gray figure on the small chart-glass area; it is absent from the final frame as the whole-screen dim recovers. This is direct image inspection in addition to `reflectionVisual=true`. The figure remains small and translucent rather than dominating the instrument or appearing as a real map/radar contact.

| Elapsed | Frame | Visible figure | Durable receipt |
|---:|---|---|---|
| 828.587 |003982|Absent|Absent|
| 828.787 |003983|Present|Absent|
| 829.085 |003984|Present|Present|
| 829.287 |003985|Present|Present|
| 829.489 |003986|Absent|Present|

Three actual visible samples span0.500s, compared with the single sampled glimpse in the baseline. Assuming a continuous glimpse between samples, the adjacent absent/present frames bracket its duration between roughly0.500s and0.902s; they do not measure exact onset/offset. The native functional duration test provides the finer engine-level check. Small position drift is difficult to judge confidently from these full-size stills and is not claimed as subjectively obvious. The images support a longer opportunity to notice the reflection and receipt publication after initial visibility, not proof that a human noticed it or found it frightening.

## Interpretation boundary

Actual visual delivery is supported for the inspected Photo Lab, Radar, Computer disturbance and reflection moments. Sustained fear, human attentional capture, whether the motion is mistaken for something alive, and subjective audio spatialization remain untested human hypotheses. Operator waiting while reviewing these frames must remain external processing time, not be labeled fear, hesitation or game confusion. Physical exit and final completion still require their own recorded evidence; this partial inspection does not assert them.
