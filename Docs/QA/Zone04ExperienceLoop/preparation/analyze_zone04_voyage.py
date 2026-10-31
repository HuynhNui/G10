"""Offline actual-evidence aggregation for Zone04; never connects to Unity.

Run only after every recorder segment is closed. --allow-live produces an
explicit partial preview without reading an open WAV or muxing video. It never
substitutes late pilot annotations for actual authority/sample/frame clocks.
"""
from __future__ import annotations

import argparse
import bisect
import collections
import datetime as dt
import importlib.util
import json
import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[1]
REPO = ROOT.parents[2]
FIELDS = (
    "kind", "utc", "elapsedWall", "realtime", "gameTime", "unscaledTime", "frame", "zone", "scene", "day",
    "panel", "app", "x", "y", "depth", "heading", "speed", "distance", "energy", "hull", "photosTaken",
    "capturesToday", "radarCharges", "photoCharges", "captureCharges", "completedObjectives", "cueReceipts",
    "captureActive", "captureHits", "captureRemaining", "focused", "editorPaused", "timeScale", "transitioning",
    "modal", "fragmentEvents", "hiddenAEvents", "hiddenBEvents", "finalSignalEvents", "fragmentVisual",
    "fragmentSequenceActive", "fragmentProgress", "environmentLevel", "missionLogAudience", "photoLabAudience",
    "helmAudience", "hiddenAPhotoAudience", "hiddenBPhotoAudience", "selectedPhotoPoi", "activePresentationSequences",
    "fragmentAudioTime", "hiddenAAudioTime", "hiddenBAudioTime", "finalSignalAudioTime", "finalWaterRequested",
    "finalWaterTintVisible", "finalWaterTintAlpha", "revealedLocationIds", "mainObjectivesComplete",
    "hiddenRouteUnlocked", "hiddenRouteComplete", "endingChoice", "committedEndingReached", "endingReceiptSource",
    "endingPresentationId", "endingPresentationVisible", "endingPresentationPlaying", "endingPresentationElapsed",
    "endingTitle", "endingSurvey", "activeSequences", "activeEffects", "audioVoices",
)
SURFACE_FIELDS = (
    "fragmentVisual", "fragmentSequenceActive", "missionLogAudience", "photoLabAudience", "helmAudience",
    "hiddenAPhotoAudience", "hiddenBPhotoAudience", "finalWaterRequested", "finalWaterTintVisible",
    "endingPresentationVisible", "endingPresentationPlaying",
)


def read_rows(path, allow_live=False):
    values = []
    lines = path.read_text(encoding="utf-8-sig").splitlines()
    for i, line in enumerate(lines):
        if not line.strip():
            continue
        try:
            values.append(json.loads(line))
        except json.JSONDecodeError:
            if not allow_live or i != len(lines) - 1:
                raise
    return values


def stamp(row):
    return dt.datetime.fromisoformat(row["utc"].replace("Z", "+00:00")).timestamp()


def dump(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def analyze(phase, allow_live=False, mux=False, blocked_baseline=False):
    spec = importlib.util.spec_from_file_location("zone_evidence_parser", REPO / "Tools/QA/summarize_zone02_experience.py")
    parser = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(parser)
    base = ROOT / phase
    # Every discovered recorder timeline is authoritative, including a newly
    # started/broken segment that has not produced its first image yet.
    paths = [p.parent for p in base.rglob("timeline.jsonl")]
    assert paths, "No real recorder segments found."
    segments = []
    for path in paths:
        assert (path / "frames.jsonl").is_file(), f"Recorder has no actual frame log yet: {path}"
        timeline = read_rows(path / "timeline.jsonl", allow_live)
        frames = read_rows(path / "frames.jsonl", allow_live)
        assert timeline and frames, f"Empty recording: {path}"
        starts = [r for r in timeline if r.get("kind") == "recording.start"]
        stops = [r for r in timeline if r.get("kind") == "recording.stop"]
        assert len(starts) == 1, f"Expected one actual recording start: {path}"
        closed = len(stops) == 1 and (path / "summary.txt").exists()
        assert closed or allow_live, f"Recorder still live: {path}"
        summary = dict(line.split("=", 1) for line in (path / "summary.txt").read_text().splitlines() if "=" in line) if closed else {}
        rows = sorted(timeline + frames, key=lambda r: r["elapsedWall"])
        end = max(float(summary.get("wallSeconds", 0)), rows[-1]["elapsedWall"])
        origin = stamp(starts[0]) - starts[0]["elapsedWall"]
        frame_times = [stamp(r) for r in frames]
        for row in rows:
            row["_segment"] = str(path.relative_to(base))
            row["_path"] = path
            row["_time"] = stamp(row)
        segment = dict(path=path, timeline=timeline, frames=frames, rows=rows, frame_times=frame_times,
                       starts=starts, stops=stops, closed=closed, summary=summary, end=end, origin=origin)
        if closed:
            analysis, _ = parser.summarize(path, f"zone04-{phase}-{path.name}", "actual sealed segment; aggregate separately assesses target completion or explicit blocked boundary", "Zone04")
            if mux:
                analysis["mux"] = parser.mux(path, analysis, frames)
                dump(path / "mux-result.json", analysis["mux"])
            elif (path / "mux-result.json").exists():
                analysis["mux"] = json.loads((path / "mux-result.json").read_text(encoding="utf-8-sig"))
            dump(path / "event-index.json", analysis)
            segment["analysis"] = analysis
        segments.append(segment)
    segments.sort(key=lambda s: s["origin"])
    rows = sorted([r for s in segments for r in s["rows"]], key=lambda r: r["_time"])
    frames = sorted([r for s in segments for r in s["frames"]], key=lambda r: r["_time"])
    by_path = {s["path"]: s for s in segments}
    resource_ready = next((r for r in rows if r.get("zone") == "Zone04" and r.get("hull", 0) > 0 and r.get("energy", 0) > 0 and r.get("depth", 0) > 0), None)
    ready = next((r for r in rows if r.get("zone") == "Zone04" and r.get("zoneFourTelemetryObserved") is True and r.get("hull", 0) > 0 and r.get("energy", 0) > 0 and r.get("depth", 0) > 0 and r.get("transitioning") is False), None)
    assert ready, "No initialized nontransitioning Zone04 sample yet."

    def point(row):
        if row is None:
            return None
        value = {k: row[k] for k in FIELDS if k in row}
        seg = by_path[row["_path"]]
        i = bisect.bisect_left(seg["frame_times"], row["_time"])
        candidates = seg["frames"][max(0, i - 1):min(len(seg["frames"]), i + 1)]
        actual = min(candidates, key=lambda r: abs(r["_time"] - row["_time"]))
        value.update(segment=row["_segment"], flow_elapsed_seconds_utc=row["_time"] - ready["_time"],
                     nearest_actual_image=str(row["_path"] / actual["image"]), image_utc=actual["utc"],
                     image_time_delta_seconds=actual["_time"] - row["_time"])
        return value

    after = [r for r in rows if r["_time"] >= ready["_time"]]
    committed = next((r for r in after if r.get("committedEndingReached") in ("normal", "hidden")), None)
    ending_scene = next((r for r in after if r.get("scene") == "Ending"), None)
    authority = next((r for r in after if r.get("scene") == "Ending" and r.get("committedEndingReached") in ("normal", "hidden")), None)
    settled = next((r for r in after if r.get("scene") == "Ending" and r.get("transitioning") is False and r.get("endingPresentationVisible") and r.get("endingPresentationId") == r.get("committedEndingReached")), None)
    full = next((r for r in after if r.get("scene") == "Ending" and r.get("transitioning") is False and r.get("endingPresentationVisible") and r.get("endingPresentationId") == r.get("committedEndingReached") and r.get("endingPresentationElapsed", 0) >= (16 if r.get("endingPresentationId") == "hidden" else 12)), None)
    complete = bool(full and full.get("committedEndingReached") == "hidden")
    if blocked_baseline:
        assert phase == "baseline", "Only a baseline may be explicitly sealed blocked."
        assert not complete, "A completed route is not a blocked baseline."
        assert all(s["closed"] for s in segments), "Blocked seal requires every recorder segment closed."
    if not allow_live and not blocked_baseline:
        assert complete, "The measured route requires actual full hidden Ending, not a normal/missing ending."
    window_end = full["_time"] if full else rows[-1]["_time"]

    def paired(start_kind, stop_kind):
        pending, intervals, unmatched = None, [], []
        previous_segment = None
        for row in rows:
            if previous_segment is not None and row["_segment"] != previous_segment and pending:
                unmatched.append(dict(reason="segment-ended-before-matching-end; no interval across external gap", observation=point(pending)))
                pending = None
            previous_segment = row["_segment"]
            if row.get("kind") == start_kind:
                if pending:
                    unmatched.append(dict(reason="start-before-end", observation=point(pending)))
                pending = row
            elif row.get("kind") == stop_kind:
                if pending:
                    intervals.append(dict(start_utc=pending["utc"], end_utc=row["utc"], start=pending["_time"], end=row["_time"], seconds=row["_time"]-pending["_time"], start_detail=pending.get("detail"), end_detail=row.get("detail")))
                    pending = None
                else:
                    unmatched.append(dict(reason="end-without-start", observation=point(row)))
        if pending:
            unmatched.append(dict(reason="start-without-end", observation=point(pending)))
        return dict(matched_intervals=intervals, matched_seconds=sum(i["seconds"] for i in intervals), unmatched_markers=unmatched)

    operator = paired("operator.gap.start", "operator.gap.end")
    navigation = paired("input.navigation.start", "input.navigation.stop")
    metric, partition = collections.defaultdict(float), collections.defaultdict(float)
    owner = dict(keyboard=False, pointer=False, capture_agent=False, navigation_driver=False)
    changes = {"input.keys.down": ("keyboard", True), "input.keys.up": ("keyboard", False), "input.pointer.down": ("pointer", True), "input.pointer.up": ("pointer", False), "input.agent.start": ("capture_agent", True), "input.agent.stop": ("capture_agent", False), "input.navigation.start": ("navigation_driver", True), "input.navigation.stop": ("navigation_driver", False)}
    for seg in segments:
        # A sealed recorder/Continue boundary supplies no observation of active input.
        # Never extend an owner from the previous process over loading or a real gap.
        owner.update(keyboard=False, pointer=False, capture_agent=False, navigation_driver=False)
        for i, row in enumerate(seg["rows"]):
            if row.get("kind") in changes:
                key, value = changes[row["kind"]]
                owner[key] = value
            elif row.get("kind") == "input.release-all":
                owner.update(keyboard=False, pointer=False, capture_agent=False)
            start = max(row["_time"], ready["_time"])
            finish = min(seg["rows"][i+1]["_time"] if i+1 < len(seg["rows"]) else seg["origin"]+seg["end"], window_end)
            duration = max(0, finish-start)
            if not duration:
                continue
            metric["recorded_ready_to_" + ("full_ending" if full else "terminal_sample") + "_seconds"] += duration
            overlap = sum(max(0, min(finish, p["end"])-max(start, p["start"])) for p in operator["matched_intervals"])
            metric["explicit_operator_gap_matched_recorded_seconds"] += overlap
            if row.get("focused") is False:
                metric["unfocused_seconds"] += duration
            if row.get("editorPaused") or row.get("timeScale", 1) <= 0:
                metric["observed_pause_seconds"] += duration
            if row.get("scene") == "Ending":
                partition["ending_scene"] += duration
                continue
            if row.get("zone") != "Zone04":
                partition["other_zone_or_loading"] += duration
                continue
            metric["recorded_zone04_before_ending_scene_seconds"] += duration
            for key, active in owner.items():
                if active:
                    metric["owner_"+key+"_seconds"] += duration
            flags = dict(navigation_activity=row.get("moving") or row.get("turning") or row.get("changingDepth"), horizontal_motion=abs(row.get("speed", 0))>.001, turning=row.get("turning"), changing_depth=row.get("changingDepth"), capture_ui_active=row.get("captureActive"), computer_ui_visible=row.get("panel")=="ComputerScreen", photo_lab_visible=row.get("panel")=="ComputerScreen" and row.get("app")=="PhotoLab", rest_ui_visible=row.get("panel")=="ComputerScreen" and row.get("app")=="Rest", mission_log_visible=row.get("missionLogAudience"), journal_visible=row.get("panel")=="ComputerScreen" and row.get("app")=="Journal", upgrade_visible=row.get("panel")=="ComputerScreen" and row.get("app")=="Upgrade")
            for key, active in flags.items():
                if active:
                    metric[key+"_seconds"] += duration
            if not any(owner.values()):
                metric["no_input_owner_seconds"] += duration
                metric["no_input_owner_outside_paired_operator_gap_seconds"] += max(0, duration-overlap)
            category = "transitioning" if row.get("transitioning") else "unfocused" if row.get("focused") is False else "navigation_activity" if flags["navigation_activity"] else "capture_ui_active" if flags["capture_ui_active"] else "computer_ui_visible" if flags["computer_ui_visible"] else "other_station_or_unknown"
            partition[category] += duration

    def state_windows(selector):
        result = collections.defaultdict(list)
        for seg in segments:
            pending = {}
            for i, row in enumerate(seg["rows"]):
                finish = seg["rows"][i+1]["_time"] if i+1 < len(seg["rows"]) else seg["origin"]+seg["end"]
                active = set(selector(row))
                for key in list(pending):
                    if key not in active:
                        pending[key]["end_observation"] = point(row)
                        result[key].append(pending.pop(key))
                if finish <= row["_time"]:
                    continue
                for key in active:
                    if key not in pending:
                        pending[key] = dict(start=point(row), seconds_estimated=0, maximum_observed_step_seconds=0)
                    value = pending[key]
                    value["seconds_estimated"] += finish-row["_time"]
                    value["maximum_observed_step_seconds"] = max(value["maximum_observed_step_seconds"], finish-row["_time"])
                    value["last_true_observation"] = point(row)
            for key, value in pending.items():
                value["end_state"] = "segment-ended-while-reported-active; no interpolation across gap"
                result[key].append(value)
        return {key: dict(seconds_estimated=sum(v["seconds_estimated"] for v in values), intervals=values) for key, values in sorted(result.items())}

    def clips(row):
        for text in row.get("audioVoices", []):
            parts = text.split(":")
            match = re.search(r"(?:^|:)volume=([0-9.]+)", text)
            if len(parts)>=2 and match and float(match.group(1))>0:
                yield parts[1]

    surfaces = state_windows(lambda r: [k for k in SURFACE_FIELDS if r.get(k) is True])
    sequences = state_windows(lambda r: [v.split(":seconds=")[0] for v in r.get("activePresentationSequences", [])])
    voices = state_windows(clips)
    all_audio_clocks = state_windows(lambda r: [k for k in ("fragmentAudioTime", "hiddenAAudioTime", "hiddenBAudioTime", "finalSignalAudioTime") if isinstance(r.get(k), (int, float)) and r[k]>=0])
    # Ending reuses the final-signal clip, so its positive source-name clock is
    # not another Zone04 approach cue. Keep the unrestricted diagnostic windows
    # separately and scope cue exposure to actual observed Zone04 telemetry.
    audio_clocks = state_windows(lambda r: [k for k in ("fragmentAudioTime", "hiddenAAudioTime", "hiddenBAudioTime", "finalSignalAudioTime") if r.get("zone") == "Zone04" and r.get("zoneFourTelemetryObserved") is True and isinstance(r.get(k), (int, float)) and r[k]>=0])
    mission, receipts, reveals, rests, collisions, choice_changes, events = [], [], [], [], [], [], []
    seen_missions, seen_receipts, seen_reveals = set(), set(), set()
    previous = None
    initialized_action_segments = set()
    counters = collections.Counter()
    for row in rows:
        can_count_action_delta = bool(previous and previous.get("zone") == row.get("zone") == "Zone04"
                                      and previous["_segment"] == row["_segment"]
                                      and row["_segment"] in initialized_action_segments)
        if row.get("zone")=="Zone04":
            for key, seen, target in (("completedObjectives", seen_missions, mission), ("cueReceipts", seen_receipts, receipts), ("revealedLocationIds", seen_reveals, reveals)):
                for value in row.get(key, []):
                    if value not in seen:
                        target.append(dict(value=value, observation=point(row)))
                        seen.add(value)
            # Loading a saved checkpoint across a recorder/Continue boundary is
            # not an observed Rest, collision, cue delivery or route choice.
            # Keep first receipts in the aggregate, but count action changes
            # only when adjacent observations belong to the same real segment.
            if can_count_action_delta:
                if row.get("day", 0)>previous.get("day", 0):
                    rests.append(dict(before=previous["day"], after=row["day"], observation=point(row)))
                if row.get("hull", 0)<previous.get("hull", 0) and not row.get("transitioning"):
                    collisions.append(dict(before=previous["hull"], after=row["hull"], observation=point(row), meaning="observed hull decrease; cause requires actual-frame/input review"))
                for field in ("fragmentEvents", "hiddenAEvents", "hiddenBEvents", "finalSignalEvents"):
                    if row.get(field, 0)>previous.get(field, 0):
                        counters[field] += row[field]-previous.get(field, 0)
        if previous:
            changed = {key: dict(before=previous.get(key), after=row.get(key)) for key in ("zone", "day", "completedObjectives", "cueReceipts", "revealedLocationIds", "endingChoice", "committedEndingReached", "hiddenRouteUnlocked", "hiddenRouteComplete", "focused", "editorPaused", "endingPresentationVisible") if row.get(key)!=previous.get(key)}
            if changed:
                cleanup_unknown = row.get("kind") == "recording.stop" and row.get("zoneFourTelemetryObserved") is not True
                events.append(dict(changes=changed, observation=point(row),
                                   crosses_recorder_boundary=previous["_segment"] != row["_segment"],
                                   meaning="Recorder cleanup with unavailable Zone04 telemetry; zero values do not establish a gameplay terminal cause." if cleanup_unknown else "Checkpoint/loading discontinuity; not an observed gameplay action." if previous["_segment"] != row["_segment"] else "Adjacent recorded state change."))
            if can_count_action_delta and row.get("endingChoice") and row.get("endingChoice")!=previous.get("endingChoice"):
                choice_changes.append(dict(before=previous.get("endingChoice"), after=row.get("endingChoice"), observation=point(row)))
        # The first initialized row is a loaded checkpoint, never an action.
        # Keep the gate through later ordinary Zone04 transitions so a real Rest
        # day change emitted during transition is still observed. Leaving the
        # zone/menu resets it for any new load in the same recorder segment.
        if row.get("zone") != "Zone04" or row.get("scene") == "MainMenu":
            initialized_action_segments.discard(row["_segment"])
        elif (row.get("zoneFourTelemetryObserved") is True and row.get("transitioning") is False
              and all(row.get(key, 0) > 0 for key in ("hull", "energy", "depth"))):
            initialized_action_segments.add(row["_segment"])
        previous = row

    review = {}
    for field in ("fragmentVisual", "hiddenAPhotoAudience", "hiddenBPhotoAudience", "finalWaterTintVisible", "endingPresentationVisible"):
        selected = [r for r in frames if r.get(field) is True]
        if selected:
            review[field] = [point(selected[i]) for i in sorted(set([0, len(selected)//3, len(selected)//2, 2*len(selected)//3, len(selected)-1]))]
    gaps = [dict(before_segment=a["path"].name, after_segment=b["path"].name, previous_stop_utc=a["stops"][0]["utc"] if a["stops"] else a["rows"][-1]["utc"], next_start_utc=b["starts"][0]["utc"], marker_boundary_seconds=b["starts"][0]["_time"]-(a["stops"][0]["_time"] if a["stops"] else a["rows"][-1]["_time"]), meaning="No observations or replacement PCM/images are invented across this real segment boundary.") for a,b in zip(segments, segments[1:])]
    def delta(row):
        return row["_time"]-ready["_time"] if row else None
    return dict(
        schema="zone04-actual-voyage-aggregate-v1", phase=phase,
        status="SEALED CONTROL/ROUTE-ASSURANCE BLOCKED BASELINE; target ending NOT REACHED" if blocked_baseline else "SEALED actual full hidden ending" if complete and all(s["closed"] for s in segments) else "PARTIAL LIVE PREVIEW; no completed baseline/final claim",
        target_zone="Zone04", expected_destination="hidden Ending",
        clocks=dict(definition="First initialized nontransitioning Zone04 to actual committed hidden receipt, settled matching Ending scene, then full>=16-second Ending clock. UTC sample authority; no pilot annotation/video duration substitution. If blocked, all ending clocks remain null; terminal sample is a separate censored boundary, never an ending.", recording_start=point(segments[0]["starts"][0]), initial_resource_ready=point(resource_ready), initial_flow_ready=point(ready), first_committed_ending=point(committed), first_ending_scene=point(ending_scene), first_ending_authority=point(authority), settled_ending=point(settled), full_ending=point(full), terminal_observed_sample=point(rows[-1]), ready_to_terminal_sample_seconds=delta(rows[-1]), ready_to_committed_seconds=delta(committed), ready_to_settled_seconds=delta(settled), ready_to_full_ending_seconds=delta(full), total_recorded_wall_seconds=sum(s["end"] for s in segments), intersegment_marker_gap_seconds=sum(g["marker_boundary_seconds"] for g in gaps)),
        segments=[dict(path=str(s["path"]), closed=s["closed"], recorder_summary=s["summary"], first_actual_utc=s["rows"][0]["utc"], last_actual_utc=s["rows"][-1]["utc"], video_frames=len(s["frames"]), analysis=s.get("analysis")) for s in segments],
        unrecorded_intersegment_gaps=gaps, interval_metrics_nonexclusive_ready_to_terminal_sample=dict(metric), recorded_exclusive_state_partition_seconds=dict(partition), explicit_operator_gap_intervals=operator, navigation_input_marker_intervals=navigation,
        mission_first_observations=mission, primary_receipt_first_observations=receipts, revealed_location_first_observations=reveals, rest_day_transitions=rests, observed_hull_decreases=collisions, ending_choice_changes=choice_changes, positive_counter_increments=dict(counters), actual_boolean_surface_windows=surfaces, actual_sequence_windows=sequences, actual_audio_source_playing_windows=voices, actual_measurable_voice_clock_windows=audio_clocks, all_scene_source_name_clock_windows=all_audio_clocks, cue_clock_scope="Observed Zone04 telemetry only; shared final-signal clip playback in Ending is retained separately in all_scene_source_name_clock_windows and actual_audio_source_playing_windows.", review_candidate_actual_frames=review, reviewed_actual_frames=[], event_timeline=events,
        limitations=["No input owner and unmarked time remain unknown; no confusion, attention, hearing or fear is inferred.", "Input activity, navigation, UI, focus and explicit operator-gap categories overlap. Exclusive partition is observational state only.", "Windows integrate actual left-held samples within each segment only; they may miss brief events. Segments are never interpolated across a gap.", "Source voices, sequence registration, receipts, component flags, real pixels and actual listener PCM establish different facts; manual actual-media review is required.", "Chart/capture assistance, route knowledge, tool/operator scheduling and recording overhead confound wall-time comparisons."])


if __name__ == "__main__":
    cli = argparse.ArgumentParser(description=__doc__)
    cli.add_argument("phase", choices=("baseline", "final"))
    cli.add_argument("--allow-live", action="store_true")
    cli.add_argument("--mux", action="store_true")
    cli.add_argument("--blocked-baseline", action="store_true", help="Explicitly seal closed incomplete baseline as control/route-assurance blocked; ending authority remains absent.")
    cli.add_argument("--output", type=pathlib.Path)
    args = cli.parse_args()
    assert not (args.allow_live and args.mux), "Do not mux an open recorder/WAV."
    output = args.output or ROOT / args.phase / ("live-preview.json" if args.allow_live else "offline-metrics.json")
    assert not (args.allow_live and args.blocked_baseline), "A blocked seal is not a live preview."
    result = analyze(args.phase, args.allow_live, args.mux, args.blocked_baseline)
    dump(output, result)
    print(json.dumps(dict(status=result["status"], output=str(output), clocks={k:v for k,v in result["clocks"].items() if k.endswith("seconds")}, mission_count=len(result["mission_first_observations"]), receipt_count=len(result["primary_receipt_first_observations"]), segments=len(result["segments"])), ensure_ascii=False, indent=2))
