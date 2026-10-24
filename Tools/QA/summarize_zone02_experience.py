#!/usr/bin/env python3
"""Summarize real Zone02 evidence and optionally mux its actual VFR frames/audio.

Uses only Python's standard library and an installed ffmpeg/ffprobe executable.
No Unity connection, gameplay mutation, inferred emotional measurement, fake audio,
interpolated image, or constant-frame-rate timing substitution is performed.

Example: python Tools/QA/summarize_zone02_experience.py RUN/evidence --mux
         --segment baseline-segment-1 --initial-report-dir Docs/QA/Zone02ExperienceLoop
For later segments use --segment/--status and omit --initial-report-dir so the
initial blocked-voyage report is not overwritten.
"""
from __future__ import annotations

import argparse
import array
import bisect
import collections
import json
import math
import pathlib
import shutil
import statistics
import subprocess
import sys
import wave

OPTIONAL_PRESENTATION_FIELDS = (
    "experienceStage", "pendingCue", "followingEvents", "radarVisual",
    "computerVisual", "reflectionVisual", "photoMotion", "transitioning",
)
OPTIONAL_SURFACE_FIELDS = ("radarVisual", "computerVisual", "reflectionVisual", "photoMotion", "transitioning")


def read_jsonl(path):
    with path.open(encoding="utf-8-sig") as source:
        return [json.loads(line) for line in source if line.strip()]


def dump(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def compact(row):
    result = {key: row.get(key) for key in (
        "utc", "elapsedWall", "realtime", "gameTime", "unscaledTime", "frame", "zone", "scene",
        "panel", "app", "action", "x", "y", "depth", "heading", "distance", "energy", "hull",
        "completedObjectives", "cueReceipts", "captureActive", "captureHits", "captureRemaining",
    )}
    # Absence is meaningful: old recordings did not instrument these surfaces.
    result.update({key: row[key] for key in OPTIONAL_PRESENTATION_FIELDS if key in row})
    return result


def wav_statistics(path):
    if not path.exists():
        return {"status": "no WAV available"}
    with wave.open(str(path), "rb") as audio:
        channels, rate, width, frames = audio.getnchannels(), audio.getframerate(), audio.getsampwidth(), audio.getnframes()
        if width != 2:
            return {"status": "unsupported exact amplitude analysis", "sample_width": width}
        count = rails = negative_rails = positive_rails = nonzero = clipped_frames = 0
        squares = 0
        peak = 0
        while True:
            data = audio.readframes(65536)
            if not data:
                break
            values = array.array("h", data)
            if sys.byteorder != "little":
                values.byteswap()
            count += len(values)
            chunk_positive = values.count(32767)
            chunk_negative = values.count(-32767) + values.count(-32768)
            positive_rails += chunk_positive
            negative_rails += chunk_negative
            nonzero += len(values) - values.count(0)
            peak = max(peak, abs(min(values)), abs(max(values)))
            squares += sum(sample * sample for sample in values)
            if chunk_positive or chunk_negative:
                clipped_frames += len({index // channels for index, sample in enumerate(values) if abs(sample) >= 32767})
        rails = positive_rails + negative_rails
        rms = math.sqrt(squares / count) / 32767 if count else 0
        return {
            "status": "actual PCM analyzed", "sample_rate": rate, "channels": channels,
            "sample_width_bytes": width, "sample_frames": frames, "sample_values": count,
            "duration_seconds": frames / rate, "nonzero_sample_values": nonzero,
            "peak_integer": peak, "peak_normalized": peak / 32767,
            "rms_normalized": rms, "rms_dbfs": 20 * math.log10(rms) if rms else None,
            "positive_rail_sample_values": positive_rails, "negative_rail_sample_values": negative_rails,
            "rail_sample_values": rails, "rail_fraction": rails / count if count else 0,
            "sample_frames_with_any_rail": clipped_frames,
            "interpretation": "Exact PCM values at +/-32767 (or -32768) indicate saturation/limiting candidates. Recorder clamps floats to [-1,1]; this count cannot identify where upstream saturation occurred or establish perceptual distortion.",
        }


def source_id(value):
    return ":".join(value.split(":")[:2])


def marker_intervals(observations, start_kind, stop_kind):
    """Only pair explicit markers; never guess a missing start/end or human intent."""
    intervals, anomalies = [], []
    pending = None
    for row in observations:
        kind = row.get("kind")
        if kind == start_kind:
            if pending is not None:
                anomalies.append({"kind": "unmatched-start-before-next-start", "marker": compact(pending), "detail": pending.get("detail")})
            pending = row
        elif kind == stop_kind:
            if pending is None:
                anomalies.append({"kind": "unmatched-end-unknown-start", "marker": compact(row), "detail": row.get("detail")})
            else:
                intervals.append({"start": pending["elapsedWall"], "end": row["elapsedWall"], "seconds": row["elapsedWall"] - pending["elapsedWall"], "start_detail": pending.get("detail"), "end_detail": row.get("detail"), "start_zone": pending.get("zone"), "end_zone": row.get("zone")})
                pending = None
    if pending is not None:
        anomalies.append({"kind": "unmatched-start-unknown-end", "marker": compact(pending), "detail": pending.get("detail")})
    return {"matched_intervals": intervals, "matched_seconds": sum(value["seconds"] for value in intervals), "unmatched_markers": anomalies, "caveat": "Only explicitly paired boundaries are measured. Unmarked/unpaired time remains unknown, never classified as confusion, fear, recovery or all operator processing."}


def optional_presentation_exposure(observations, frames, end, ready_time, exit_time):
    """Integrate sampled reported surface state, preserving uninstrumented periods as unknown.

    This uses the same left-observation interval estimate as other time metrics.
    It establishes component-reported state, not human perception or what a video
    frame caught between samples. Captured frame flags are counted separately.
    """
    coverage, surfaces = {}, {}
    first_observed = observations[0]["elapsedWall"] if observations else end
    ready_start = ready_time if ready_time is not None else end
    for field in OPTIONAL_PRESENTATION_FIELDS:
        known = [row for row in observations if field in row]
        known_seconds = 0
        for index, row in enumerate(observations):
            finish = observations[index + 1]["elapsedWall"] if index + 1 < len(observations) else end
            if field in row:
                known_seconds += max(0, finish - row["elapsedWall"])
        coverage[field] = {
            "availability": "observed" if known else "absent-unknown",
            "reported_observations": len(known), "unreported_observations": len(observations) - len(known),
            "reported_interval_seconds": known_seconds, "unknown_interval_seconds": max(0, end - known_seconds),
            "first_reported_elapsed_seconds": known[0]["elapsedWall"] if known else None,
            "last_reported_elapsed_seconds": known[-1]["elapsedWall"] if known else None,
        }
    for field in OPTIONAL_SURFACE_FIELDS:
        known_seconds = true_seconds = false_seconds = zone_true = zone_false = zone_known = 0
        intervals, active = [], None
        for index, row in enumerate(observations):
            start = row["elapsedWall"]
            finish = observations[index + 1]["elapsedWall"] if index + 1 < len(observations) else end
            duration = max(0, finish - start)
            overlap = max(0, min(finish, exit_time) - max(start, ready_start))
            reported = isinstance(row.get(field), bool)
            if reported:
                known_seconds += duration
                zone_known += overlap
                if row[field]:
                    true_seconds += duration
                    zone_true += overlap
                else:
                    false_seconds += duration
                    zone_false += overlap
            if reported and row[field] and duration > 0:
                if active is None:
                    active = {"start_elapsed_seconds": start, "end_elapsed_seconds": finish, "seconds_estimated": duration, "start_utc": row.get("utc"), "start_panel": row.get("panel"), "start_app": row.get("app"), "start_zone": row.get("zone"), "maximum_observed_step_seconds": duration}
                else:
                    active["end_elapsed_seconds"] = finish
                    active["seconds_estimated"] += duration
                    active["maximum_observed_step_seconds"] = max(active["maximum_observed_step_seconds"], duration)
            elif active is not None:
                active["end_utc_observed"] = row.get("utc")
                active["end_state"] = "reported-false" if reported else "unknown-unreported"
                intervals.append(active)
                active = None
        if active is not None:
            active["end_state"] = "recording-ended-while-reported-true"
            intervals.append(active)
        true_frames = [{"image": row.get("image"), "elapsed_seconds": row["elapsedWall"], "panel": row.get("panel"), "app": row.get("app"), "zone": row.get("zone")} for row in frames if row.get(field) is True]
        has_field = any(isinstance(row.get(field), bool) for row in observations)
        surfaces[field] = {
            "availability": "observed" if has_field else "absent-unknown",
            "reported_true_seconds_estimated": true_seconds if has_field else None,
            "reported_false_seconds_estimated": false_seconds if has_field else None,
            "unknown_seconds": max(0, end - known_seconds),
            "zone02_ready_reported_true_seconds_estimated": zone_true if has_field else None,
            "zone02_ready_reported_false_seconds_estimated": zone_false if has_field else None,
            "zone02_ready_unknown_seconds": max(0, exit_time - ready_start - zone_known),
            "true_intervals": intervals, "captured_frame_rows_reported_true": len(true_frames) if has_field else None,
            "captured_true_frame_examples": true_frames[:100], "frame_example_limit": 100,
            "meaning": "SceneFlow transition state, not visual horror exposure" if field == "transitioning" else "Component-reported actual surface state; verify cited real images for rendered content and context",
        }
    return {"field_coverage": coverage, "surface_state_intervals": surfaces, "unknown_prefix_seconds": first_observed,
            "caveat": "Missing optional fields are unknown, never evidence of false/zero exposure. Times integrate sampled left-held state and can miss brief events or span observation gaps. A true surface flag is not proof of legible content, actual human attention or fear. Frame flags are reported separately; manual image/audio review remains required. Transitioning measures loading/scene flow and must not be classified as a horror event."}


def summarize(evidence, segment, status):
    timeline = read_jsonl(evidence / "timeline.jsonl")
    frames = read_jsonl(evidence / "frames.jsonl")
    observations = sorted(timeline + frames, key=lambda row: row["elapsedWall"])
    frame_times = [row["elapsedWall"] for row in frames]
    summary_text = {}
    if (evidence / "summary.txt").exists():
        summary_text = dict(line.split("=", 1) for line in (evidence / "summary.txt").read_text().splitlines() if "=" in line)
    end = max(float(summary_text.get("wallSeconds", 0)), observations[-1]["elapsedWall"])
    ready = next((row for row in observations if row.get("zone") == "Zone02" and row.get("energy", 0) > 0 and row.get("hull", 0) > 0 and row.get("depth", 0) > 0), None)
    zone_exit = next((row for row in observations if ready and row["elapsedWall"] > ready["elapsedWall"] and row.get("zone") not in ("Zone02", "none", None)), None)
    ready_time = ready["elapsedWall"] if ready else None
    exit_time = zone_exit["elapsedWall"] if zone_exit else end
    # Durations are observed state intervals, never an assertion of fear/confusion.
    categories = collections.defaultdict(float)
    inputs = {"keyboard": False, "pointer": False, "capture_agent": False, "navigation_driver": False}
    navigation_intervals = marker_intervals(observations, "input.navigation.start", "input.navigation.stop")
    operator_intervals = marker_intervals(observations, "operator.gap.start", "operator.gap.end")
    categories["explicit_operator_gap_matched_whole_recording_seconds"] = operator_intervals["matched_seconds"]
    markers = collections.defaultdict(list)
    for index, row in enumerate(observations):
        start = row["elapsedWall"]
        finish = observations[index + 1]["elapsedWall"] if index + 1 < len(observations) else end
        duration = max(0, finish - start)
        kind = row.get("kind", "")
        if kind != "sample" and kind != "video-frame" and kind != "input.agent.key":
            markers[kind].append({**compact(row), "detail": row.get("detail", "")})
        if kind == "input.keys.down":
            inputs["keyboard"] = True
        elif kind == "input.keys.up":
            inputs["keyboard"] = False
        elif kind == "input.pointer.down":
            inputs["pointer"] = True
        elif kind == "input.pointer.up":
            inputs["pointer"] = False
        elif kind == "input.agent.start":
            inputs["capture_agent"] = True
        elif kind == "input.agent.stop":
            inputs["capture_agent"] = False
        elif kind == "input.navigation.start":
            inputs["navigation_driver"] = True
        elif kind == "input.navigation.stop":
            inputs["navigation_driver"] = False
        elif kind == "input.release-all":
            # The chart driver is a distinct keyboard owner and records its own stop.
            # Toolkit release-all cannot silently erase its measured start/stop span.
            inputs.update(keyboard=False, pointer=False, capture_agent=False)
        for key, active in inputs.items():
            if active:
                categories["actual_injected_" + key + "_seconds"] += duration
        if ready_time is None or start < ready_time:
            categories["setup_loading_seconds"] += duration
            continue
        if start >= exit_time:
            categories["after_zone02_departure_seconds"] += duration
            continue
        categories["zone02_ready_wall_seconds"] += duration
        if inputs["navigation_driver"]:
            categories["actual_injected_navigation_driver_zone02_seconds"] += duration
        operator_overlap = sum(max(0, min(finish, gap["end"]) - max(start, gap["start"])) for gap in operator_intervals["matched_intervals"])
        categories["explicit_operator_gap_matched_zone02_seconds"] += operator_overlap
        if row.get("editorPaused") or row.get("timeScale", 1) <= 0:
            categories["observed_pause_seconds"] += duration
        if row.get("captureActive"):
            categories["capture_ui_active_seconds"] += duration
        if row.get("moving") or row.get("turning") or row.get("changingDepth"):
            categories["navigation_motion_seconds"] += duration
        # ZoneNavigation.IsMoving includes turning and depth-control activity.
        # Its separate signed Speed is the observed horizontal translation rate.
        if abs(row.get("speed", 0)) > .001:
            categories["horizontal_motion_seconds"] += duration
        if row.get("turning"):
            categories["turning_seconds"] += duration
        if row.get("changingDepth"):
            categories["changing_depth_seconds"] += duration
        if row.get("panel") == "ComputerScreen":
            categories["computer_ui_visible_seconds"] += duration
            if row.get("app") == "PhotoLab":
                categories["photo_lab_visible_seconds"] += duration
            elif row.get("app") == "Rest":
                categories["rest_ui_visible_seconds"] += duration
            elif row.get("app") == "Upgrade":
                categories["upgrade_ui_visible_seconds"] += duration
        if not any(inputs.values()):
            categories["no_injected_control_zone02_seconds"] += duration
            categories["no_injected_control_unmarked_zone02_seconds"] += max(0, duration - operator_overlap)
    state_fields = ("scene", "zone", "day", "panel", "app", "photosTaken", "completedObjectives", "cueReceipts", "captureActive", "captureHits", "environmentCount", "radarEvents", "knocks", "scrapes", "reflections", "timeScale", "editorPaused", "focused", "hull", "radarCharges", "photoCharges", "captureCharges") + OPTIONAL_PRESENTATION_FIELDS
    previous = {}
    previous_present = set()
    previous_voices = set()
    events = []
    for row in observations:
        changes = {key: {"before": previous.get(key), "after": row.get(key)} for key in state_fields if row.get(key) != previous.get(key) or (key in OPTIONAL_PRESENTATION_FIELDS and ((key in row) != (key in previous_present)))}
        for field in OPTIONAL_PRESENTATION_FIELDS:
            if field in changes:
                changes[field].update(before_reported=field in previous_present, after_reported=field in row)
        voices = {source_id(value) for value in row.get("audioVoices", [])}
        started = sorted(voices - previous_voices)
        ended = sorted(previous_voices - voices)
        explicit = row.get("kind") not in ("sample", "video-frame", "input.agent.key")
        if changes or started or ended or explicit:
            nearest = min(max(0, bisect.bisect_left(frame_times, row["elapsedWall"])), len(frames) - 1)
            event = {**compact(row), "kind": row.get("kind"), "changes": changes}
            if row.get("detail"):
                event["detail"] = row["detail"]
            if started:
                event["source_started_observed"] = started
            if ended:
                event["source_stopped_observed"] = ended
            event["next_or_nearest_image"] = frames[nearest]["image"]
            event["image_elapsed_seconds"] = frames[nearest]["elapsedWall"]
            events.append(event)
        previous.update({key: row.get(key) for key in state_fields})
        previous_present = set(row)
        previous_voices = voices
    audio = wav_statistics(evidence / "actual-game-audio.wav")
    # Callback clocks are absolute Stopwatch seconds, shared across transient listeners.
    # Infer first callback's recording-relative bracket from observed 0 -> nonzero.
    first_audio_index = next((i for i, row in enumerate(observations) if row.get("firstAudioCallbackStopwatchSeconds", 0) > 0), None)
    audio_segments = []
    if first_audio_index is not None:
        first = observations[first_audio_index]
        prior = observations[first_audio_index - 1] if first_audio_index else observations[0]
        bracket = [prior["elapsedWall"], first["elapsedWall"]]
        origin_estimate = statistics.mean(bracket)
        first_clock = first["firstAudioCallbackStopwatchSeconds"]
        groups = collections.defaultdict(list)
        for row in observations:
            clock = row.get("firstAudioCallbackStopwatchSeconds", 0)
            if clock:
                groups[clock].append(row)
        for clock, group in sorted(groups.items()):
            # Main-thread framesWritten cannot exceed all producer frames plus prior
            # listener frames. Maximum observed difference recovers that PCM boundary.
            boundary = max(0, max(row.get("audioFramesWritten", 0) - row.get("audioSampleFrames", 0) for row in group))
            start_wall = origin_estimate + clock - first_clock
            shift = start_wall - boundary / audio.get("sample_rate", 48000)
            audio_segments.append({"callback_stopwatch_seconds": clock, "pcm_start_sample_frame_inferred": boundary, "recording_start_seconds_estimated": start_wall, "pts_shift_seconds_estimated": shift, "first_observed_seconds": group[0]["elapsedWall"], "last_observed_seconds": group[-1]["elapsedWall"]})
    else:
        bracket = None
    frame_gaps = [right - left for left, right in zip(frame_times, frame_times[1:])]
    candidate_beats, following_starts, delivery_receipts, optional_state_changes = [], [], [], []
    for event in events:
        changed = event.get("changes", {})
        for counter in ("environmentCount", "radarEvents", "knocks", "scrapes", "reflections"):
            transition = changed.get(counter)
            if transition and (transition.get("after") or 0) > (transition.get("before") or 0):
                candidate_beats.append({"elapsed_seconds": event["elapsedWall"], "observation": counter + " increment", "value": transition["after"]})
        following = changed.get("followingEvents")
        if following and following.get("after_reported"):
            before, after = following.get("before"), following.get("after")
            if isinstance(before, int) and isinstance(after, int) and following.get("before_reported") and after > before:
                observation = {"elapsed_seconds": event["elapsedWall"], "observation": "following start counter increment; distinct from durable receipt", "before": before, "after": after, "delta": after - before, "image": event["next_or_nearest_image"], "panel": event.get("panel"), "app": event.get("app")}
                following_starts.append(observation)
                candidate_beats.append(observation)
        for field in ("experienceStage", "pendingCue"):
            transition = changed.get(field)
            if transition and transition.get("after_reported"):
                optional_state_changes.append({"elapsed_seconds": event["elapsedWall"], "observation": field + " state observed", "before": transition.get("before"), "after": transition.get("after"), "initial_or_restored": not transition.get("before_reported"), "image": event["next_or_nearest_image"]})
        transition = changed.get("cueReceipts")
        if transition:
            for receipt in sorted(set(transition.get("after") or []) - set(transition.get("before") or [])):
                receipt_event = {"elapsed_seconds": event["elapsedWall"], "observation": "receipt added", "value": receipt, "image": event["next_or_nearest_image"], "panel": event.get("panel"), "app": event.get("app"), "classification": "initial-or-restored-after-zone-load" if "zone" in changed or transition.get("before") is None else "observed-durable-receipt-addition"}
                candidate_beats.append(receipt_event)
                delivery_receipts.append(receipt_event)
    beat_gaps = [{"from": left, "to": right, "seconds": right["elapsed_seconds"] - left["elapsed_seconds"]} for left, right in zip(candidate_beats, candidate_beats[1:])]
    result = {
        "segment": segment, "status": status, "definition": "Observed actual segment. Completion must be established from its real final transition; automatic metrics never prove emotional effectiveness or a completed improvement cycle.",
        "recording_wall_seconds": end, "first_authoritative_zone02_elapsed_seconds": next((r["elapsedWall"] for r in observations if r.get("zone") == "Zone02"), None),
        "zone02_ready_elapsed_seconds": ready_time, "zone02_ready_state": compact(ready) if ready else None,
        "zone_departure_elapsed_seconds": zone_exit["elapsedWall"] if zone_exit else None,
        "zone_departure_destination": zone_exit.get("zone") if zone_exit else None,
        "departure_state": compact(zone_exit) if zone_exit else None,
        "late_observer_entry_markers": markers.get("voyage.entry", []),
        "interval_metrics_nonexclusive": dict(categories),
        "time_category_caveat": "State durations integrate observed intervals. Categories overlap. Separate chart-driver ownership is included from input.navigation.start to stop. Explicit paired operator.gap markers are measured separately; missing boundaries/unmarked time remain unknown. No injected input is not proof of confusion or fear; operator processing, visual review, actual quiet gameplay and UI comprehension are not fully separable. No processing or pause markers were inferred.",
        "navigation_driver_marker_intervals": navigation_intervals,
        "explicit_operator_gap_intervals": operator_intervals,
        "idle_confusion_seconds": None,
        "zone02_max_distance": max((r.get("distance", 0) for r in observations if r.get("zone") == "Zone02"), default=0),
        "all_observed_time_scales": sorted({r.get("timeScale") for r in observations}),
        "editor_pause_markers": markers.get("editor.pause", []),
        "agent_processing_markers": {key: value for key, value in markers.items() if "processing" in key},
        "video": {"images": len(frames), "first_image_elapsed_seconds": frame_times[0], "last_image_elapsed_seconds": frame_times[-1], "target_fps": 5, "reported_skipped_slots": int(summary_text.get("skippedVideoSlots", 0)), "maximum_actual_frame_gap_seconds": max(frame_gaps), "maximum_after_ready_frame_gap_seconds": max((b-a for a,b in zip(frame_times, frame_times[1:]) if ready_time is not None and a >= ready_time), default=None), "median_actual_frame_gap_seconds": statistics.median(frame_gaps), "gaps_over_0_3_seconds": [{"start": a, "duration": b-a} for a,b in zip(frame_times, frame_times[1:]) if b-a > .3]},
        "audio": audio, "audio_listener_segments": audio_segments,
        "first_audio_callback_elapsed_bracket_seconds": bracket,
        "audio_sync_caveat": "Callback absolute Stopwatch clocks identify listener replacement gaps. The first callback is bracketed by actual observations; its midpoint estimates the initial video offset. PCM boundary is inferred from maximum cumulative-written minus current-listener-produced sample frames. DSP block latency/device output latency remains unknown. No missing PCM is synthesized. Review originals before interpreting exact A/V exposure.",
        "recorder_summary": summary_text,
        "receipt_exposure_caveat": "Receipt, event counter and source.isPlaying changes are objective state observations. Perceptible visual/audio exposure requires manual review; this report claims no human response or fear.",
        "optional_presentation_telemetry": optional_presentation_exposure(observations, frames, end, ready_time, exit_time),
        "following_start_counter_observations": following_starts,
        "following_start_counter_availability": "observed" if any("followingEvents" in row for row in observations) else "absent-unknown",
        "following_start_counter_total_observed_increments": sum(value["delta"] for value in following_starts) if any("followingEvents" in row for row in observations) else None,
        "presentation_delivery_receipt_observations": delivery_receipts,
        "optional_presentation_state_observations": optional_state_changes,
        "candidate_presentation_observations_not_confirmed_exposure": candidate_beats,
        "candidate_presentation_intervals": beat_gaps,
        "candidate_interval_comparison_caveat": "Counter increments and durable receipts are only candidate presentation observations. Optional experienceStage/pendingCue state changes are listed separately and excluded from these interval gaps. Initial/restored receipts are classified separately; neither receipt nor counter proves delivery, visible surface or attention. Optional surface intervals and actual frames must be reviewed before comparing meaningful exposure or uneventful time.",
        "event_index": events,
    }
    return result, frames


def concat_quote(path):
    return "'" + str(path.resolve()).replace("\\", "/").replace("'", "'\\''") + "'"


def mux(evidence, result, frames):
    ffmpeg = shutil.which("ffmpeg")
    if not ffmpeg:
        return {"status": "ffmpeg unavailable"}
    first_time = frames[0]["elapsedWall"]
    manifest = evidence / "frames-vfr.ffconcat"
    lines = ["ffconcat version 1.0"]
    for index, row in enumerate(frames):
        end = frames[index + 1]["elapsedWall"] if index + 1 < len(frames) else result["recording_wall_seconds"]
        lines.extend(("file " + concat_quote(evidence / row["image"]), "option framerate 1000", f"duration {max(.001,end-row['elapsedWall']):.9f}"))
    lines.extend(("file " + concat_quote(evidence / frames[-1]["image"]), "option framerate 1000"))
    manifest.write_text("\n".join(lines) + "\n", encoding="utf-8")
    command = [ffmpeg, "-hide_banner", "-y", "-f", "concat", "-safe", "0", "-i", str(manifest)]
    audio_file = evidence / "actual-game-audio.wav"
    segments = result["audio_listener_segments"]
    if audio_file.exists() and segments:
        command += ["-i", str(audio_file)]
        first_shift = segments[0]["pts_shift_seconds_estimated"] - first_time
        expression = f"PTS+({first_shift:.9f})/TB"
        old_shift = segments[0]["pts_shift_seconds_estimated"]
        for segment in segments[1:]:
            delta = segment["pts_shift_seconds_estimated"] - old_shift
            expression += f"+if(gte(N\\,{segment['pcm_start_sample_frame_inferred']})\\,{delta:.9f}/TB\\,0)"
            old_shift = segment["pts_shift_seconds_estimated"]
        command += ["-map", "0:v:0", "-map", "1:a:0", "-af", "asetpts=" + expression, "-c:a", "aac", "-b:a", "160k"]
    else:
        command += ["-an"]
    command += ["-vf", "scale=960:-2", "-c:v", "libx264", "-preset", "veryfast", "-crf", "24", "-pix_fmt", "yuv420p", "-fps_mode", "vfr", "-video_track_timescale", "1000", "-movflags", "+faststart", str(evidence / "gameplay.mp4")]
    (evidence / "mux-command.json").write_text(json.dumps(command, indent=2), encoding="utf-8")
    with (evidence / "mux-ffmpeg.log").open("w", encoding="utf-8") as log:
        completed = subprocess.run(command, stdout=log, stderr=subprocess.STDOUT, check=False)
    status = {"status": "success" if completed.returncode == 0 else "failed", "returncode": completed.returncode, "output": "gameplay.mp4", "video_method": "Real ffconcat frame durations, millisecond input timebase, VFR H264 at 960px width. Each actual frame is held until the next actual timestamp; last real image held until recording stop. No interpolation.", "audio_method": "Actual WAV encoded AAC. Piecewise PCM sample-clock timestamp offsets preserve inferred listener replacement gap; no apad/aresample/async, generated PCM or missing-audio synthesis. AAC packet/block rounding and player gap handling remain limitations."}
    ffprobe = shutil.which("ffprobe")
    if completed.returncode == 0 and ffprobe:
        probe = subprocess.run([ffprobe, "-v", "error", "-show_entries", "format=duration,size:stream=index,codec_name,width,height,time_base,duration,nb_frames", "-of", "json", str(evidence / "gameplay.mp4")], capture_output=True, text=True, check=False)
        status["ffprobe"] = json.loads(probe.stdout) if probe.returncode == 0 else probe.stderr
        timing = subprocess.run([ffprobe, "-v", "error", "-select_streams", "a:0", "-read_intervals", "%+2", "-show_entries", "packet=pts_time,duration_time,size", "-of", "json", str(evidence / "gameplay.mp4")], capture_output=True, text=True, check=False)
        if timing.returncode == 0:
            status["initial_audio_long_packets"] = [packet for packet in json.loads(timing.stdout).get("packets", []) if float(packet.get("duration_time", 0)) > .03]
            status["packet_timing_caveat"] = "MP4 represents AAC gaps by extended packet duration until the next packet timestamp. Decoder/player gap behavior can differ; original WAV and callback-clock evidence are authoritative. No gap-filling PCM filter was used."
    return status


def initial_reports(report_dir, evidence, result):
    report_dir.mkdir(parents=True, exist_ok=True)
    metrics = result["interval_metrics_nonexclusive"]
    audio = result["audio"]
    events = result["event_index"]
    def first_change(field, predicate):
        return next((event for event in events if field in event.get("changes", {}) and predicate(event["changes"][field]["after"])), None)
    photo = first_change("completedObjectives", lambda values: "Z2_L1_PHOTO" in (values or []))
    capture = first_change("completedObjectives", lambda values: "Z2_L1_CAPTURE" in (values or []))
    def seconds(value):
        return "unavailable" if value is None else f"{value:.3f} s"
    text = f"""# Baseline real playtest — original voyage, segment 1

**Status: BLOCKED, continuation pending. This is not a completed Zone 02 baseline or a completed improvement cycle.** The original voyage used the legitimate Zone 02 debug preset and real InputSystem keyboard/mouse events. It completed the first organism's PHOTO → actual Photo Lab SEND → actual capture minigame. Forward navigation toward the second site crossed the return gate and transitioned to Zone 01. No teleportation or objective/resource injection was used to claim completion.

The root agent is investigating a prerequisite progression defect separately from horror redesign. No final baseline experience conclusions are established by this segment alone. A continuation must preserve this failure and clearly identify any repair/restart; it cannot silently replace the original voyage.

## Clock and fidelity

| Observation | Measured value |
|---|---:|
| Recording wall time including setup and post-return investigation | {seconds(result['recording_wall_seconds'])} |
| First authoritative expedition zone = Zone02 | {seconds(result['first_authoritative_zone02_elapsed_seconds'])} after recording start |
| First ready default position/resources | {seconds(result['zone02_ready_elapsed_seconds'])} after recording start |
| Late `voyage.entry` observer annotation | {seconds(result['late_observer_entry_markers'][0]['elapsedWall']) if result['late_observer_entry_markers'] else 'not present'} |
| Actual departure into Zone01 | {seconds(result['zone_departure_elapsed_seconds'])} after recording start |
| Ready Zone02 wall interval until departure | {seconds(metrics.get('zone02_ready_wall_seconds'))} |
| Setup/loading before ready default state | {seconds(metrics.get('setup_loading_seconds'))} |
| After-return investigation before stop | {seconds(metrics.get('after_zone02_departure_seconds'))} |
| Navigation motion (translation OR turning OR depth) | {seconds(metrics.get('navigation_motion_seconds',0))} |
| Horizontal movement | {seconds(metrics.get('horizontal_motion_seconds',0))} |
| Depth movement | {seconds(metrics.get('changing_depth_seconds',0))} |
| Turning | {seconds(metrics.get('turning_seconds',0))} |
| Actual capture UI active | {seconds(metrics.get('capture_ui_active_seconds',0))} |
| Automated visible-position capture steering | {seconds(metrics.get('actual_injected_capture_agent_seconds',0))} |
| Computer UI visible | {seconds(metrics.get('computer_ui_visible_seconds',0))} |
| Photo Lab visible | {seconds(metrics.get('photo_lab_visible_seconds',0))} |
| Zone02 time without injected controls | {seconds(metrics.get('no_injected_control_zone02_seconds',0))} |
| Idle/confusion time | **Not separable from operator processing/visual review with this log** |

Durations integrate actual observed state intervals; listed categories overlap and must not be added. No-injected-control time includes operator screenshot review, interface comprehension, waiting, quiet gameplay and unknown gaps. It is not a fear, confusion or recovery metric. There are no explicit operator-processing or Editor-pause markers in this segment; all observed `timeScale` samples are 1.0. The game continued during external reasoning. `scene=Zone01` was already present while `ExpeditionLoop.Zone=Zone02`; scene name alone is not authoritative progression evidence. The entry annotation at ~54 seconds is late and is not used as the start clock.

## Observed journey and limitation

- Default ready state appears at {seconds(result['zone02_ready_elapsed_seconds'])}: position (45,595), depth 230 m, 100 energy / 100 hull, 10 radar / 20 photo charges, untouched Zone02 objectives and receipts.
- Map opened at ~73.550 s; radar at ~106.276 s; depth changed through the real pointer-held control around 162.563–172.714 s.
- A photograph was taken at ~274.676 s. First photograph SEND committed `Z2_L1_PHOTO` at {seconds(photo['elapsedWall']) if photo else 'unavailable'}.
- Escape from Photo Lab returned to the computer desktop. The first capture hotspot click was therefore occluded by the computer, as explicitly annotated at ~364.445 s. This is a recorded interface misunderstanding, not a failed capture attempt.
- Real capture UI became active at ~366.460 s. The optional input-driving agent began at ~382.335 s and chose W/S from displayed hook/fish position and hook heading only. `Z2_L1_CAPTURE` and the `adhesive` cue receipt were observed at {seconds(capture['elapsedWall']) if capture else 'unavailable'}. Human motor skill/balance was not measured.
- W was actually held from ~452.613 s to ~460.012 s toward the corridor. The authoritative zone became Zone01 at {seconds(result['zone_departure_elapsed_seconds'])}; the blocker was annotated at ~509.930 s. The second organism, Pressure Hull installation and Zone03 transition were not completed.

Environment event counter reached 9 before departure; radar/knocks/scrape/reflection counters remained 0 and `adhesive` was the only persisted Zone02 cue observed. These are **receipts/counters/source-state evidence**, not yet confirmed sensory exposure. The exact event index links adjacent real images. No human fear or hesitation is claimed.

## Recording evidence and audio checks

- [Real variable-frame-timestamp gameplay video](baseline/evidence/gameplay.mp4)
- [Original frame metadata](baseline/evidence/frames.jsonl), [actual telemetry and input log](baseline/evidence/timeline.jsonl), [unmodified actual listener WAV](baseline/evidence/actual-game-audio.wav)
- [Derived event index and clock/audio metrics](baseline/evidence/event-index.json), [recording summary](baseline/evidence/summary.txt), [mux details](baseline/evidence/mux-result.json)

There are {result['video']['images']} actual frames; recorder reported {result['video']['reported_skipped_slots']} skipped target 5 Hz slots. Longest actual frame gap is {result['video']['maximum_actual_frame_gap_seconds']:.3f} s. Images are held for their true recorded duration in the VFR review video, with no interpolation. Original evidence is retained.

Actual audio is {audio.get('duration_seconds',0):.6f} seconds of {audio.get('sample_rate',0)} Hz / {audio.get('channels',0)}-channel PCM. Exact amplitude analysis found **{audio.get('rail_sample_values',0):,} sample values at PCM saturation rails** out of {audio.get('sample_values',0):,} ({100*audio.get('rail_fraction',0):.8f}%), across {audio.get('sample_frames_with_any_rail',0):,} sample frames. Two rail samples do not support a broad clipping problem or establish audible distortion. Captured RMS is {audio.get('rms_dbfs',0):.2f} dBFS. No auditory perception or mix-quality judgment is claimed from these statistics.

The listener was replaced during loading. Callback Stopwatch timestamps and cumulative PCM counters expose this discontinuity; the review video preserves the estimated timestamp gap without synthesizing replacement audio. Initial callback alignment is bracketed by real observations. DSP block latency, device latency, inferred segment boundary and AAC packet rounding prevent sample-accurate sync. Recorder reports zero dropped ring samples, which does not erase listener replacement gaps. See event-index JSON and mux notes for exact clocks.

## Outstanding baseline work

Document the prerequisite transition repair independently. Complete or honestly block a clearly identified continuation using real controls, retaining the original failure. Review actual video/audio around the receipts to classify exposure and context. Only then close baseline analysis before redesign. Emotion ratings remain designer hypotheses; human feedback is absent.
"""
    (report_dir / "01_Baseline_Playtest.md").write_text(text, encoding="utf-8")
    dump(report_dir / "01_Baseline_Timeline.json", result)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("evidence", type=pathlib.Path)
    parser.add_argument("--segment", default="baseline-segment-1")
    parser.add_argument("--status", default="blocked original voyage; continuation pending")
    parser.add_argument("--mux", action="store_true")
    parser.add_argument("--initial-report-dir", type=pathlib.Path)
    args = parser.parse_args()
    evidence = args.evidence.resolve()
    result, frames = summarize(evidence, args.segment, args.status)
    if args.mux:
        result["mux"] = mux(evidence, result, frames)
        dump(evidence / "mux-result.json", result["mux"])
    elif (evidence / "mux-result.json").exists():
        result["mux"] = json.loads((evidence / "mux-result.json").read_text(encoding="utf-8"))
    dump(evidence / "event-index.json", result)
    if args.initial_report_dir:
        initial_reports(args.initial_report_dir.resolve(), evidence, result)
    print(json.dumps({key: value for key, value in result.items() if key not in ("event_index", "late_observer_entry_markers", "zone02_ready_state", "departure_state")}, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
