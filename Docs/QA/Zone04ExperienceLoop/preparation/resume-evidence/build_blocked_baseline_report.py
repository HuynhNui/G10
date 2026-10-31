"""Build the explicitly censored Zone04 baseline report from sealed raw evidence.

Offline only. Does not connect to Unity or edit Assets/Tools/save/raw media.
Run after analyze_zone04_voyage.py baseline --blocked-baseline --mux.
"""
from __future__ import annotations
import bisect
import datetime as dt
import hashlib
import json
import math
import pathlib

ROOT = pathlib.Path(__file__).resolve().parents[2]
BASE = ROOT / "baseline"


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def dump(path, data):
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def stamp(text):
    return dt.datetime.fromisoformat(text.replace("Z", "+00:00")).timestamp()


def main():
    data = read(BASE / "offline-metrics.json")
    assert data["status"].startswith("SEALED CONTROL/ROUTE-ASSURANCE BLOCKED")
    assert data["clocks"]["full_ending"] is None
    segments = []
    allrows = []
    raw_manifest = []
    for segment in data["segments"]:
        path = pathlib.Path(segment["path"])
        rows = []
        frames = []
        for name in ("timeline.jsonl", "frames.jsonl"):
            values = [json.loads(line) for line in (path / name).read_text(encoding="utf-8-sig").splitlines() if line.strip()]
            for row in values:
                row["segment"] = path.name
                row["_path"] = path
            rows.extend(values)
            if name == "frames.jsonl":
                frames = values
        rows.sort(key=lambda r: r["utc"])
        allrows.extend(rows)
        stops = [r for r in rows if r.get("kind") == "input.navigation.stop" and "obstruction" in r.get("detail", "")]
        damage = []
        previous = None
        for row in rows:
            if previous and row.get("zone") == "Zone04" == previous.get("zone") and row.get("hull", 0) < previous.get("hull", 0) and not row.get("transitioning"):
                damage.append(dict(utc=row["utc"], before=previous["hull"], after=row["hull"], decrease=previous["hull"]-row["hull"], x=row["x"], y=row["y"], day=row["day"]))
            previous = row
        analysis = segment["analysis"]
        analysis["status"] = "actual sealed partial segment; CONTROL/ROUTE-ASSURANCE BLOCKED baseline; target ending not reached"
        dump(path / "event-index.json", analysis)
        segment["analysis"] = analysis
        local = dict(name=path.name, path=str(path), frame_count=len(frames), summary=segment["recorder_summary"],
                     video=analysis["video"], audio=analysis["audio"], listener_segments=analysis["audio_listener_segments"],
                     first_callback_bracket=analysis["first_audio_callback_elapsed_bracket_seconds"], mux=analysis["mux"],
                     obstruction_stops=[{k:r.get(k) for k in ("utc","elapsedWall","x","y","day","hull","detail")} for r in stops],
                     distinct_obstruction_stop_positions=len({(round(r["x"],3),round(r["y"],3)) for r in stops}),
                     observed_hull_decreases=damage)
        segments.append(local)
        for name in ("timeline.jsonl", "frames.jsonl", "summary.txt", "actual-game-audio.wav", "gameplay.mp4"):
            artifact = path / name
            digest = hashlib.sha256()
            with artifact.open("rb") as stream:
                while chunk := stream.read(1024*1024):
                    digest.update(chunk)
            raw_manifest.append(dict(path=str(artifact.relative_to(ROOT)), bytes=artifact.stat().st_size, sha256=digest.hexdigest()))
    allrows.sort(key=lambda r:r["utc"])
    ready = next(r for r in allrows if r.get("zone")=="Zone04" and r.get("zoneFourTelemetryObserved") is True and r.get("transitioning") is False and r.get("energy",0)>0 and r.get("hull",0)>0 and r.get("depth",0)>0)
    assert ready["utc"] == "2026-10-26T16:19:54.1556075Z"
    origin = stamp(ready["utc"])

    def point(row):
        value = {k:v for k,v in row.items() if k not in ("_path",)}
        value["flow_utc_elapsed_seconds"] = stamp(row["utc"])-origin
        return value

    firsts = []
    seen = set()
    for row in allrows:
        if row.get("zone") != "Zone04":
            continue
        for field in ("completedObjectives", "cueReceipts"):
            for receipt in row.get(field,[]):
                key=(field,receipt)
                if key not in seen:
                    firsts.append(dict(kind=field, value=receipt, observation=point(row)))
                    seen.add(key)
    expected = ["hidden.a", "hidden.b", "final.signal"]
    reviewed = read(ROOT / "preparation/reviewed-baseline-frames.json")["reviewed_actual_frames"]
    reviewed.extend([
        dict(image=str(BASE/"evidence/frames/frame-001359.jpg"), utc=ready["utc"], interpretation="Earliest initialized, nontransitioning Zone04 telemetry frame; cabin artwork is actually rendered. This boundary is initialized state, not proof that all station controls have already appeared."),
        dict(image=str(BASE/"evidence/frames/frame-013224.jpg"), utc="2026-10-26T17:00:15.1881141Z", interpretation="Actual helm displays X1826.1/Y828.1, depth526.4, heading179.2, speed0 and VAT CAN obstruction indication; energy36/100. This corroborates the first cap state, not an ending."),
        dict(image=str(BASE/"continuation-01/frames/frame-009161.jpg"), utc="2026-10-27T14:41:28.6859440Z", interpretation="Actual final full chart shows completed L1 check and still gray L2 marker. Header X1773.3/Y693.0 is chart cursor/selected coordinate, not the logged ship position1747.217/899.920. Dark contours are visible; this frame alone does not prove a navigable corridor or an impossible route.")])
    clock = data["clocks"]
    pcm_frames=sum(s["audio"]["sample_frames"] for s in segments)
    sample_values=sum(s["audio"]["sample_values"] for s in segments)
    rms=math.sqrt(sum(s["audio"]["rms_normalized"]**2*s["audio"]["sample_values"] for s in segments)/sample_values)
    audio_total=dict(sample_frames=pcm_frames, sample_values=sample_values, channels=2, sample_rate=48000, duration_seconds=pcm_frames/48000,
                     rms_dbfs=20*math.log10(rms), rail_sample_values=sum(s["audio"]["rail_sample_values"]for s in segments),
                     sample_frames_with_any_rail=sum(s["audio"]["sample_frames_with_any_rail"]for s in segments),
                     dropped_sample_values=sum(int(s["summary"]["droppedAudioSamples"])for s in segments))
    disclosure = read(ROOT / "preparation/resume-evidence/checkpoint-vs-cap.json")
    data.update(schema="zone04-blocked-baseline-evidence-v1", route_completion="NOT REACHED; bounded control/route-assurance blocker; route impossibility unproven", censored_boundary=True,
                target_ending_not_observed=True, primary_presentations_not_encountered=[dict(id=x,status="NOT OBSERVED; route eligibility/audience not reached, not evidence of failed scare delivery")for x in expected],
                segment_quality_and_control=segments, aggregate_audio_quality=audio_total, checkpoint_resume_disclosure=disclosure,
                observed_control_summary=dict(obstruction_stop_markers=sum(len(s["obstruction_stops"])for s in segments), observed_hull_decrease_events=sum(len(s["observed_hull_decreases"])for s in segments), sum_observed_hull_decreases=sum(e["decrease"]for s in segments for e in s["observed_hull_decreases"]), rest_count=len(data["rest_day_transitions"]), Continue_is_not_Rest=True),
                earliest_ready_criterion="Merged actual timeline+frame UTC; Zone04; zoneFourTelemetryObserved true; transitioning false; positive hull, energy and depth. Earliest frame107197 precedes timeline-only sample107198 by41.1763ms.",
                reviewed_actual_frames=reviewed, receipt_first_observations=firsts,
                human_emotion_ratings=None, human_emotion_reason="AI-controlled actual input does not measure human fear, uncertainty, boredom, hearing or immersion; unencountered content remains unassessed.",
                raw_artifact_manifest=raw_manifest)
    dump(ROOT / "01_Baseline_Timeline.json", data)
    dump(BASE / "offline-metrics.json", data)

    metric=data["interval_metrics_nonexclusive_ready_to_terminal_sample"]
    def val(key):return f"{metric.get(key,0):,.3f}"
    def t(row):return f"{row['flow_utc_elapsed_seconds']:,.3f}"
    timeline=[]
    timeline.append(f"| A / {ready['elapsedWall']:.3f} s | {ready['utc']} | First initialized Zone04; day1, (290,950), depth500, energy/hull100. |")
    for value in firsts:
        row=value["observation"]
        timeline.append(f"| {'A' if row['segment']=='evidence' else 'B'} / {row['elapsedWall']:.3f} s | {row['utc']} | First actual `{value['value']}` receipt. |")
    for event in data["rest_day_transitions"]:
        row=event["observation"]
        if row["segment"] != "evidence":
            continue
        timeline.append(f"| {'A' if row['segment']=='evidence' else 'B'} / {row['elapsedWall']:.3f} s | {row['utc']} | Actual Rest day{event['before']} → day{event['after']}; resources refilled by ordinary game flow. |")
    timeline.append("| A / 2700.006 s | 2026-10-26T17:00:15.3561257Z | Recording closed after external tool usage interruption. Last ship (1826.145,828.059), hull28.011; no L2 completion. |")
    timeline.append("| External gap | 76,178.809543 s | No recording or replacement PCM/images; not gameplay or horror pacing. |")
    timeline.append("| B / 22.076 s | 2026-10-27T14:10:16.2259492Z | Real DEBUG CONTINUE restores day3 Rest checkpoint, (1246.914,925.299), full resources; 775.514526m later travel rolls back. No new Rest or mission receipt. |")
    for event in data["rest_day_transitions"]:
        row=event["observation"]
        if row["segment"] != "continuation-01":
            continue
        timeline.append(f"| B / {row['elapsedWall']:.3f} s | {row['utc']} | Actual Rest day{event['before']} → day{event['after']}; resources refilled by ordinary game flow. |")
    timeline.append("| B / 1884.552 s | 2026-10-27T14:41:18.7031551Z | Explicit bounded-stop marker: repeated ineffective actual visual route attempts. Route impossibility/controller defect not proven. |")
    timeline.append("| B / 1894.973 s | 2026-10-27T14:41:29.1227486Z | Sealed final sample: day7, (1747.217,899.920), depth591.362, energy62.049, hull69.665. L1 only; hidden/normal ending unobserved. |")
    quality=[]
    for s in segments:
        au=s["audio"];vi=s["video"];ls=s["listener_segments"];br=s["first_callback_bracket"]
        gap=ls[1]["pts_shift_seconds_estimated"]-ls[0]["pts_shift_seconds_estimated"] if len(ls)>1 else 0
        quality.append(f"| {'A' if s['name']=='evidence' else 'B'} | {float(s['summary']['wallSeconds']):,.3f} | {s['frame_count']:,} / {vi['reported_skipped_slots']} | {au['sample_frames']:,} / {au['duration_seconds']:,.6f} s | {au['rms_dbfs']:.6f} dBFS | {au['rail_sample_values']} values / {au['sample_frames_with_any_rail']} frames | {gap:.6f} s |")
    text=f'''# Zone04 — baseline actual input, sealed at a bounded blocker

**Status: CONTROL / ROUTE-ASSURANCE BLOCKED. This is an incomplete baseline, not a completed Zone04 experience cycle.** Both real recorder segments are closed and preserved. The actual-input run completed L1 PHOTO, manual SEND and CAPTURE, and displayed the recovered-coordinate fragment. Repeated visually chosen navigation attempts did not establish reliable access to L2 before the root's bounded stop. The ship never reached the main route choice, hidden photographs, final physical arrival or Ending. This does not prove the authored route impossible or identify a controller defect.

The report can be extended if a concrete verified repair or faithful input method permits continuation. No horror redesign, final voyage, native test result or before/after improvement is claimed by this seal. Existing source risks are separate from encountered baseline facts.

## Actual route and boundaries

Root started the real Main Menu DEBUG Zone04 UI with regular upgrades NONE in isolated save/photo folders. Normal InputSystem keyboard/pointer events, visible-map chart assistance and visible-hook capture assistance were used. No teleport, gameplay setter, artificial resource refill, forced cue, hidden objective injection or accelerated clock was used. Six ordinary Rest actions refilled resources through normal game rules. Earlier visible-map advisory failures are retained in [navigation assistance notes](preparation/navigation-assistance-notes.md).

The initialized-state boundary is the earliest merged actual frame/timeline UTC satisfying Zone04, observed Zone04 controller telemetry, nontransitioning and positive ship hull/energy/depth: **2026-10-26T16:19:54.1556075Z**, frame107197, [actual frame001359](baseline/evidence/frames/frame-001359.jpg). The timeline-only sample follows at .1967838Z, 41.1763ms later. The frame renders the cabin artwork; the boundary does not assert that every station control has appeared.

Raw UTC first-ready → terminal sample is **{clock['ready_to_terminal_sample_seconds']:,.6f} s**, including the **76,178.809543 s external intersegment gap**. Total recorded wall duration is **{clock['total_recorded_wall_seconds']:,.3f} s** including menu/loading. Recorded first-ready → terminal coverage is **{val('recorded_ready_to_terminal_sample_seconds')} s**. These are distinct clocks; none is completed Zone04 content length. All committed/settled/full Ending times remain **null**.

Segment A stopped at day3 `(1826.144897,828.059143)`, distance2392.184570, depth526.427063, energy36.296326 and hull28.011368. Its durable save was the earlier legitimate day3 Rest checkpoint at `(1246.914429,925.299255)`, distance1616.670044, full resources. Segment B's real DEBUG CONTINUE loaded exactly that checkpoint: **775.514526m of recorded later travel and resource use rolled back**, while L1 objectives and fragment receipt persisted. Continue is not an additional Rest. Retain the original detours/damage and the replay; do not call this a seamless same-live voyage. [Exact checkpoint/sample disclosure](preparation/resume-evidence/checkpoint-vs-cap.json).

## Annotated observation timeline

Times A/B are recorder-local seconds. UTC remains authoritative across the real gap. The table annotates observable work and control events, with no inferred human tension curve.

| Local clock | Actual UTC / gap | Observation |
| --- | --- | --- |
{chr(10).join(timeline)}

There were **six actual Rest transitions**: original day1→2→3, resumed day3→4→5→6→7. The first recording contains **11 obstruction-stop markers and nine observed hull decreases**; the continuation contains **eight distinct obstruction-stop positions and eight observed hull decreases**. Thus **19 stop markers ≠ 17 sampled damage events**. A latched obstruction may stop a new driver request without a new impact. Observed decreases total **{data['observed_control_summary']['sum_observed_hull_decreases']:.6f} hull units** across refills/rollback; this is neither final net damage nor a proven count of all physical contacts. Exact locations, days and before/after hull values are in [the timeline JSON](01_Baseline_Timeline.json).

Final actual state: **day7, X1747.217163 / Y899.919678, depth591.361572m, energy62.049240, hull69.665268**. Only `Z4_L1_PHOTO`, `Z4_L1_CAPTURE` and presentation receipt `fragment` are present. L2 completion, route choice, hidden unlock/discovery/photo/SEND, final approach and endings remain **NOT OBSERVED**. Hidden A/B and FinalSignal were not eligible/encountered in this observed route; this is not evidence that their scare delivery failed.

## Observed presentation and actual images

The fragment appears once in the first segment, after real capture and actual Mission Log selection. [Actual frame002879](baseline/evidence/frames/frame-002879.jpg) shows both coordinate lines, Radar-verification instruction and completed progress bar legibly. The card/sequence flags span an estimated **3.629269s** within the **16.235523s** Mission Log audience, and the measurable recovery-voice clock/positive-gain source spans an estimated **1.612865s**. These integrate left-held samples and may overrun the authored clip boundary; they are not exact clip lengths. The corresponding source voice and card flags remain separate facts; no human reading speed, hearing or fear is measured. Its receipt survives legitimate Continue, and no second fragment presentation is observed in segment B.

[Original cap helm frame013224](baseline/evidence/frames/frame-013224.jpg) shows X1826.1/Y828.1, depth526.4, speed0, `VẬT CẢN` and energy36/100. [Final chart frame009161](baseline/continuation-01/frames/frame-009161.jpg) shows a completed L1 check and still gray L2 marker. Its header X1773.3/Y693.0 is a chart cursor/selected coordinate, **not the logged ship position**. A static chart image does not establish a collision-free corridor or an impossible route.

![Actual recovered fragment]({str(BASE / 'evidence/frames/frame-002879.jpg').replace(chr(92), '/')})

## Activity and limits

The following sampled, nonexclusive intervals cover recorded first-ready → terminal observations only. UI, navigation, input owners and explicit operator processing can overlap. Frame gaps and the 21-hour external interruption are not filled.

| Observed activity/state | Seconds |
| --- | ---: |
| Navigation (moving, turning or changing depth) | {val('navigation_activity_seconds')} |
| Horizontal motion | {val('horizontal_motion_seconds')} |
| Depth adjustment | {val('changing_depth_seconds')} |
| Turning | {val('turning_seconds')} |
| Chart input driver owner | {val('owner_navigation_driver_seconds')} |
| Capture UI active | {val('capture_ui_active_seconds')} |
| Visible Photo Lab | {val('photo_lab_visible_seconds')} |
| Visible Mission Log | {val('mission_log_visible_seconds')} |
| Visible Rest app | {val('rest_ui_visible_seconds')} |
| Explicit paired operator processing | {val('explicit_operator_gap_matched_recorded_seconds')} |
| Observed unfocused | {val('unfocused_seconds')} |
| Editor pause or nonpositive timeScale | {val('observed_pause_seconds')} |
| No active injected input owner | {val('no_input_owner_seconds')} |
| No owner outside paired operator processing | {val('no_input_owner_outside_paired_operator_gap_seconds')} |

Unmarked no-owner time remains **unknown**; it is not labelled player indecision, fear, waiting, recovery, boredom or all agent processing. Unmatched `operator.gap.end` markers also remain unmatched rather than being backfilled. Tool latency, automated input, chart assistance, route familiarity and capture overhead prevent a novice-human performance or subjective fear claim.

## Raw media, audio and synchronization

| Segment | Recorded wall seconds | Real JPEG / skipped slots | PCM frames / duration | Captured RMS | Saturation rails | Inferred listener replacement gap |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
{chr(10).join(quality)}

Both WAV files are actual 48kHz stereo PCM16, total **{audio_total['sample_frames']:,} frames / {audio_total['duration_seconds']:,.6f} s**, aggregate RMS **{audio_total['rms_dbfs']:.6f} dBFS**. There are **{audio_total['rail_sample_values']} PCM rail values across {audio_total['sample_frames_with_any_rail']} sample frames**, **zero recorder ring drops** and zero format changes. Rails identify saturation/limiting candidates and do not establish perceptual distortion, source cause, intelligibility or headset balance. Source `AudioSource.isPlaying`, voice clocks, receipts and recorded listener PCM establish different facts.

The two segments retain **22,387 actual JPEGs** and report **251 skipped capture slots**. Maximum actual frame gaps are 2.998762s / 4.926060s, including loading; the parser's after-resource-ready maxima are 1.082574s / 1.198946s. Initial callback brackets span 2.5781ms / 7.5026ms, independent of unknown device/DSP latency. The two verified MP4 files are 67,028,837 / 64,494,155 bytes with container durations 2699.979000s / 1894.887292s. Those packet-rounded container durations never replace raw recorder/UTC clocks.

VFR videos contain the real JPEG sequence with millisecond timestamps, each held to the next actual capture; the last real image is held to recorder stop. No interpolated frame or generated PCM is used. Each actual WAV is encoded AAC with piecewise timestamp offsets for inferred listener replacement gaps; the overnight gap remains between separate video files. Decoder handling and AAC packet/block rounding can differ; original PCM and raw UTC are authoritative. Initial callback brackets, frame-gap maxima, ffprobe results and exact commands are preserved per segment. DSP/device output latency remains unknown.

- Segment A: [actual video](baseline/evidence/gameplay.mp4), [actual WAV](baseline/evidence/actual-game-audio.wav), [mux result](baseline/evidence/mux-result.json), [event index](baseline/evidence/event-index.json).
- Segment B: [actual video](baseline/continuation-01/gameplay.mp4), [actual WAV](baseline/continuation-01/actual-game-audio.wav), [mux result](baseline/continuation-01/mux-result.json), [event index](baseline/continuation-01/event-index.json).
- [Full timeline, metrics, control event lists and raw SHA256 manifest](01_Baseline_Timeline.json). [Offline aggregator](preparation/analyze_zone04_voyage.py) uses an explicit `--blocked-baseline` flag; its default still rejects an incomplete voyage as completed evidence.

The actual stop was a bounded control/route-assurance decision. The downstream horror content, normal/hidden Ending and subjective experience remain unassessed. This report preserves a bounded incomplete baseline for diagnosis; it does not claim the requested cycle finished.
'''
    (ROOT / "01_Baseline_Playtest.md").write_text(text,encoding="utf-8")
    print(json.dumps(dict(report=str(ROOT/"01_Baseline_Playtest.md"), ready=ready["utc"], quality=audio_total, control=data["observed_control_summary"], metric=metric),indent=2))


if __name__ == "__main__":
    main()
