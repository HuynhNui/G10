"""Offline aggregation of the closed, actual Zone03 final evidence only."""
import bisect
import collections
import datetime as dt
import json
import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[1]
EVIDENCE = ROOT / "final" / "evidence"

def read_jsonl(name):
    return [json.loads(line) for line in (EVIDENCE / name).read_text(encoding="utf-8-sig").splitlines() if line.strip()]

timeline = read_jsonl("timeline.jsonl")
frames = read_jsonl("frames.jsonl")
rows = sorted(timeline + frames, key=lambda r: r["elapsedWall"])
assert any(r["kind"] == "recording.stop" for r in timeline), "Raw recorder must be closed first."
starts = [r for r in timeline if r["kind"] == "recording.start"]
stops = [r for r in timeline if r["kind"] == "recording.stop"]
assert len(starts) == len(stops) == 1, "This helper requires one closed segment."
summary = dict(line.split("=", 1) for line in (EVIDENCE / "summary.txt").read_text().splitlines() if "=" in line)
end = max(float(summary["wallSeconds"]), rows[-1]["elapsedWall"])
resource_ready = next(r for r in rows if r.get("zone") == "Zone03" and r.get("energy", 0) > 0 and r.get("hull", 0) > 0 and r.get("depth", 0) > 0)
ready = next(r for r in rows if r.get("zone") == "Zone03" and r.get("energy", 0) > 0 and r.get("hull", 0) > 0 and r.get("depth", 0) > 0 and r.get("transitioning") is False)
authority = next(r for r in rows if r["elapsedWall"] > ready["elapsedWall"] and r.get("zone") == "Zone04")
settled = next(r for r in rows if r["elapsedWall"] > ready["elapsedWall"] and r.get("zone") == "Zone04" and r.get("energy", 0) > 0 and r.get("hull", 0) > 0 and r.get("depth", 0) > 0 and r.get("transitioning") is False)
last_valid = next(r for r in reversed(rows) if r["elapsedWall"] < authority["elapsedWall"] and r.get("zone") == "Zone03" and r.get("transitioning") is False and r.get("energy", 0) > 0 and r.get("hull", 0) > 0)
frame_times = [r["elapsedWall"] for r in frames]

def utc_seconds(a, b):
    return (dt.datetime.fromisoformat(b.replace("Z", "+00:00")) - dt.datetime.fromisoformat(a.replace("Z", "+00:00"))).total_seconds()

FIELDS = ("kind", "utc", "elapsedWall", "realtime", "frame", "zone", "scene", "day", "panel", "app", "x", "y", "depth", "heading", "speed", "distance", "energy", "hull", "photosTaken", "capturesToday", "radarCharges", "photoCharges", "captureCharges", "completedObjectives", "cueReceipts", "captureActive", "captureHits", "captureRemaining", "rockActive", "rockElapsed", "rockDestroyed", "transitioning", "stoneEvents", "engineEvents", "radarEvents", "reflections", "focused", "editorPaused", "timeScale", "experienceStage", "pendingCue", "activeSequences", "activeEffects", "visibleProceduralPresets", "audioVoices")
def point(row):
    value = {k: row[k] for k in FIELDS if k in row}
    nearest_index = bisect.bisect_left(frame_times, row["elapsedWall"])
    candidates = frames[max(0, nearest_index - 1):min(len(frames), nearest_index + 1)]
    nearest = min(candidates, key=lambda f: abs(f["elapsedWall"] - row["elapsedWall"]))
    value.update(segment="evidence", flow_elapsed_seconds_utc=utc_seconds(ready["utc"], row["utc"]), nearest_actual_image=str(EVIDENCE / nearest["image"]), image_elapsed_seconds=nearest["elapsedWall"], image_time_delta_seconds=nearest["elapsedWall"] - row["elapsedWall"])
    return value

def paired_markers(start_kind, stop_kind):
    pending, intervals, unmatched = None, [], []
    for row in rows:
        if row["kind"] == start_kind:
            if pending:
                unmatched.append({"reason": "new-start-before-end", "marker": point(pending)})
            pending = row
        elif row["kind"] == stop_kind:
            if pending:
                intervals.append({"start": pending["elapsedWall"], "end": row["elapsedWall"], "seconds": row["elapsedWall"] - pending["elapsedWall"], "start_detail": pending.get("detail"), "end_detail": row.get("detail")})
                pending = None
            else:
                unmatched.append({"reason": "end-without-start", "marker": point(row)})
    if pending:
        unmatched.append({"reason": "start-without-end", "marker": point(pending)})
    return {"matched_intervals": intervals, "matched_seconds": sum(i["seconds"] for i in intervals), "unmatched_markers": unmatched}

operator = paired_markers("operator.gap.start", "operator.gap.end")
navigation = paired_markers("input.navigation.start", "input.navigation.stop")
owner = dict(keyboard=False, pointer=False, capture_agent=False, navigation_driver=False)
changes = {"input.keys.down": ("keyboard", True), "input.keys.up": ("keyboard", False), "input.pointer.down": ("pointer", True), "input.pointer.up": ("pointer", False), "input.agent.start": ("capture_agent", True), "input.agent.stop": ("capture_agent", False), "input.navigation.start": ("navigation_driver", True), "input.navigation.stop": ("navigation_driver", False)}
metrics = collections.defaultdict(float)
partition = collections.defaultdict(float)
for i, row in enumerate(rows):
    if row["kind"] in changes:
        key, active = changes[row["kind"]]
        owner[key] = active
    elif row["kind"] == "input.release-all":
        owner.update(keyboard=False, pointer=False, capture_agent=False)
    finish = rows[i + 1]["elapsedWall"] if i + 1 < len(rows) else end
    start = max(row["elapsedWall"], ready["elapsedWall"])
    finish = min(finish, settled["elapsedWall"])
    duration = max(0, finish - start)
    if not duration:
        continue
    metrics["recorded_primary_window_seconds"] += duration
    gap = sum(max(0, min(finish, p["end"]) - max(start, p["start"])) for p in operator["matched_intervals"])
    metrics["explicit_operator_gap_matched_seconds"] += gap
    if row.get("focused") is False:
        metrics["unfocused_seconds"] += duration
    if row.get("editorPaused") or row.get("timeScale", 1) <= 0:
        metrics["observed_pause_seconds"] += duration
    if row.get("zone") != "Zone03":
        partition["transition_loading"] += duration
        continue
    metrics["zone03_primary_recorded_seconds"] += duration
    for key, active in owner.items():
        if active:
            metrics["owner_" + key + "_seconds"] += duration
    flags = {"navigation_activity": row.get("moving") or row.get("turning") or row.get("changingDepth"), "horizontal_motion": abs(row.get("speed", 0)) > .001, "turning": row.get("turning"), "changing_depth": row.get("changingDepth"), "capture_ui_active": row.get("captureActive"), "computer_ui_visible": row.get("panel") == "ComputerScreen", "photo_lab_visible": row.get("panel") == "ComputerScreen" and row.get("app") == "PhotoLab", "rest_ui_visible": row.get("panel") == "ComputerScreen" and row.get("app") == "Rest", "upgrade_ui_visible": row.get("panel") == "ComputerScreen" and row.get("app") == "Upgrade"}
    for key, active in flags.items():
        if active:
            metrics[key + "_seconds"] += duration
    if not any(owner.values()):
        metrics["no_input_owner_seconds"] += duration
        metrics["no_input_owner_outside_paired_operator_gaps_seconds"] += max(0, duration - gap)
    category = "transition_loading" if row.get("transitioning") else "unfocused" if row.get("focused") is False else "navigation_activity" if flags["navigation_activity"] else "capture_ui_active" if flags["capture_ui_active"] else "computer_ui_visible" if flags["computer_ui_visible"] else "other_visible_station_or_unclassified"
    partition[category] += duration

def state_windows(selector):
    pending, result = {}, collections.defaultdict(list)
    for i, row in enumerate(rows):
        finish = rows[i + 1]["elapsedWall"] if i + 1 < len(rows) else end
        active = set(selector(row))
        for key in list(pending):
            if key not in active:
                pending[key]["end_state_observed"] = point(row)
                result[key].append(pending.pop(key))
        if finish <= row["elapsedWall"]:
            continue
        for key in active:
            if key not in pending:
                pending[key] = {"start": point(row), "end_elapsed_seconds": finish, "seconds_estimated": 0, "maximum_observed_step_seconds": 0}
            value = pending[key]
            value["end_elapsed_seconds"] = finish
            value["seconds_estimated"] += finish - row["elapsedWall"]
            value["maximum_observed_step_seconds"] = max(value["maximum_observed_step_seconds"], finish - row["elapsedWall"])
    for key, value in pending.items():
        value["end_state"] = "recording-ended-while-active"
        result[key].append(value)
    return {key: {"seconds_estimated": sum(i["seconds_estimated"] for i in windows), "intervals": windows} for key, windows in sorted(result.items())}

def playing_clips(row):
    for text in row.get("audioVoices", []):
        parts = text.split(":")
        match = re.search(r"(?:^|:)volume=([0-9.]+)", text)
        if len(parts) >= 2 and match and float(match.group(1)) > 0:
            yield parts[1]

presets = state_windows(lambda r: r.get("visibleProceduralPresets", []))
voices = state_windows(playing_clips)
surfaces = state_windows(lambda r: [k for k in ("stoneVisual", "falseRadarVisual", "proceduralReflectionVisual", "bioelectricVisual", "rockActive", "rockDebrisVisual") if r.get(k) is True])
mission, receipts, opportunities, rest, pending_changes, significant = [], [], [], [], [], []
seen_missions, seen_receipts = set(), set()
previous = None
counter_increments = collections.Counter()
for row in rows:
    if row.get("zone") == "Zone03":
        for value in row.get("completedObjectives", []):
            if value not in seen_missions:
                mission.append({"objective": value, "observation": point(row)})
                seen_missions.add(value)
        for value in row.get("cueReceipts", []):
            if value not in seen_receipts:
                target = opportunities if value.endswith(".opportunity") else receipts
                target.append({"receipt": value, "observation": point(row), "meaning": "saved earned intent, not delivery/exposure" if value.endswith(".opportunity") else "durable delivery receipt; corroborate actual surface/voice"})
                seen_receipts.add(value)
        if previous and previous.get("zone") == "Zone03":
            if row.get("day", 0) > previous.get("day", 0):
                rest.append({"before": previous["day"], "after": row["day"], "observation": point(row)})
            for field in ("stoneEvents", "engineEvents", "radarEvents", "reflections"):
                if row.get(field, 0) > previous.get(field, 0):
                    counter_increments[field] += row[field] - previous.get(field, 0)
        if not previous or row.get("pendingCue") != previous.get("pendingCue"):
            pending_changes.append({"before": previous.get("pendingCue") if previous else None, "after": row.get("pendingCue"), "observation": point(row)})
    if previous:
        changed = {key: {"before": previous.get(key), "after": row.get(key)} for key in ("zone", "day", "completedObjectives", "cueReceipts", "pendingCue", "rockActive", "rockDestroyed", "focused", "editorPaused") if row.get(key) != previous.get(key)}
        if row.get("hull", 0) < previous.get("hull", 0) and row.get("zone") == "Zone03" and not row.get("transitioning"):
            changed["hull"] = {"before": previous.get("hull"), "after": row.get("hull")}
        if changed:
            significant.append({"changes": changed, "observation": point(row)})
    previous = row

review_candidates = {}
for preset in ("Radar:FalseRadarSweep", "Reflection:ProceduralReflection", "PhotoLab:ProceduralReflection", "PhotoLab:BioelectricPulse", "Global:RockDebris"):
    selected = [r for r in frames if preset in r.get("visibleProceduralPresets", [])]
    if selected:
        review_candidates[preset] = [point(selected[i]) for i in sorted(set([0, len(selected) // 3, len(selected) // 2, (2 * len(selected)) // 3, len(selected) - 1]))]

result = {
    "schema": "zone03-experience-final-closed-aggregate-v1",
    "status": "FINAL COMPLETE: same live normal-input voyage physically reached initialized, settled Zone04; one closed segment",
    "target_zone": "Zone03", "expected_destination": "Zone04",
    "clocks": {"primary_definition": "First initialized Zone03 observation with transitioning=false to first initialized Zone04 observation with transitioning=false, UTC authority; no late annotation or MP4 duration substitution.", "recording_start": point(starts[0]), "recording_stop": point(stops[0]), "initial_resource_ready": point(resource_ready), "initial_flow_ready": point(ready), "first_Zone04_authority": point(authority), "settled_Zone04": point(settled), "flow_ready_to_settled_utc_seconds": utc_seconds(ready["utc"], settled["utc"]), "flow_ready_to_settled_realtime_seconds": settled["realtime"] - ready["realtime"], "resource_ready_to_settled_utc_seconds": utc_seconds(resource_ready["utc"], settled["utc"]), "all_recordings_wall_seconds": end, "interrecording_gap_seconds": 0},
    "segments": [{"path": str(EVIDENCE), "recorder_summary": summary}], "unrecorded_intersegment_gaps": [],
    "interval_metrics_nonexclusive_primary_window": dict(metrics), "recorded_flow_exclusive_state_partition_seconds": dict(partition),
    "explicit_operator_gap_intervals": operator, "navigation_input_marker_intervals": navigation,
    "unknown_and_external_delay_accounting": {"idle_confusion_seconds": None, "human_fear_measurement": None, "caveat": "State categories overlap; no-input and unmarked time are unknown. Only paired external-gap markers count. Route knowledge, chart/input assistance, scheduling, camera/PCM capture overhead and tool latency confound wall time."},
    "mission_first_observations": mission, "rest_day_transitions": rest, "rest_day_transition_count": len(rest),
    "primary_durable_receipt_first_observations": receipts, "saved_opportunity_first_observations_not_delivery": opportunities,
    "primary_receipt_separations_seconds": [{"from": a["receipt"], "to": b["receipt"], "seconds": utc_seconds(a["observation"]["utc"], b["observation"]["utc"])} for a, b in zip(receipts, receipts[1:])],
    "counter_positive_changes_in_recorded_zone03_intervals": dict(counter_increments),
    "actual_preset_surface_windows": presets, "actual_boolean_surface_state_windows": surfaces,
    "actual_audio_source_playing_opportunities": voices, "primary_pending_cue_state_changes": pending_changes,
    "review_candidate_actual_frames": review_candidates, "reviewed_actual_frames": [],
    "last_valid_Zone03_nontransitioning_preexit_state": point(last_valid), "event_timeline": significant,
    "limitations": ["Receipts, state flags, source.isPlaying and actual pixel/PCM evidence have distinct meanings; none measures hearing, attention or fear.", "Sample-held windows may miss brief starts/stops and are bounded by real observation cadence; exact rendered frames and original listener PCM remain review authority.", "Final initial selected gameplay fields match baseline in initial-equivalence.json; final route was already learned, so faster wall time does not isolate a presentation effect.", "Reflection opportunity is earned at actual depth>=600 after delivered radar plus100 distance and is retained across normal workflow; eventual retry delivery may occur near590. Opportunity is not a delivered scare."]
}
analysis = json.loads((EVIDENCE / "analysis-console.json").read_text(encoding="utf-8-sig"))
result["actual_pcm_analysis"] = analysis["audio"]
result["audio_listener_segments"] = analysis["audio_listener_segments"]
result["first_audio_callback_elapsed_bracket_seconds"] = analysis["first_audio_callback_elapsed_bracket_seconds"]
result["audio_sync_caveat"] = analysis["audio_sync_caveat"]
result["video_actual_capture"] = analysis["video"]
result["actual_vfr_mux"] = analysis["mux"]
result["interval_metrics_nonexclusive_parser_resource_ready_scope"] = analysis["interval_metrics_nonexclusive"]
reviews = [
    ("frame-006817.jpg", "False Radar: soft circular returns are visible inside the scope over the real terrain scan. Scan charge9/10 and main labels remain readable; this does not prove attention or confusion."),
    ("frame-010897.jpg", "Helm reflection: a pale standing human-like silhouette is visible in the small dark screen. Main depth, coordinates, heading, speed and energy controls remain readable. Selected representative visible peak, not a measured pixel-opacity maximum."),
    ("frame-006445.jpg", "PhotoLab specimen06: actual specimen image and mission-data/SEND UI are visible during PhotoLab:ProceduralReflection. This is a separate PhotoLab surface, not the later Helm reflection; this single frame does not cleanly isolate the subtle overlay from specimen artwork."),
    ("frame-010170.jpg", "PhotoLab specimen07: actual cyan/blue specimen image with faint thin bright traces is visible during BioelectricPulse. A still image alone does not establish pulse motion, legibility or recognition independently of the underlying photo."),
    ("frame-011535.jpg", "Demolition: small light fragments are visible near the lower floor beneath the stabilizing9s message. The later frame011550 shows the stabilizing6s message; debris remains a subtle layer over cabin art.")
]
for name, interpretation in reviews:
    actual = next(r for r in frames if pathlib.Path(r["image"]).name == name)
    result["reviewed_actual_frames"].append({"observation": point(actual), "visual_review": interpretation})
helm_peak = next(r for r in frames if pathlib.Path(r["image"]).name == "frame-010897.jpg")
baseline = json.loads((ROOT / "01_Baseline_Timeline.json").read_text(encoding="utf-8-sig"))
baseline_peak_distance = 1421.8682861328125
baseline_preexit_distance = baseline["final_Zone03_state_before_departure"]["distance"]
result["helm_peak_route_normalization"] = {"definition": "Representative actually reviewed visible Helm frame cumulative distance divided by last valid initialized Zone03 transitioning=false cumulative distance before authority departure. Rest does not reset this voyage distance; Zone04 initialization does. This is route context, not a perceptual score or controlled pacing comparison.", "final_reviewed_peak": point(helm_peak), "final_denominator_distance": last_valid["distance"], "final_fraction": helm_peak["distance"] / last_valid["distance"], "final_phase": "L3 photo/capture complete; Rock Breaker crafted+installed; moving on the rock route before demolition", "baseline_reviewed_peak_path": str(ROOT / "baseline" / "continuation-02" / "frames" / "frame-005948.jpg"), "baseline_peak_depth": 478.6956787109375, "baseline_peak_distance": baseline_peak_distance, "baseline_denominator_distance": baseline_preexit_distance, "baseline_fraction": baseline_peak_distance / baseline_preexit_distance, "baseline_phase": "L2 photo/SEND complete; before L3 photo/capture"}
baseline_engine = baseline["actual_audio_source_playing_opportunities"]["engine_reflection"]["intervals"][0]
baseline_radar = baseline["actual_surface_state_exposure_intervals"]["falseRadarVisual"]["intervals"][0]
baseline_helm = next(i for i in baseline["actual_surface_state_exposure_intervals"]["proceduralReflectionVisual"]["intervals"] if i["start_panel"] == "NavigationPanel")
final_engine = voices["engine_reflection"]["intervals"][0]
final_radar = review_candidates["Radar:FalseRadarSweep"][0]
final_helm = review_candidates["Reflection:ProceduralReflection"][0]
baseline_onsets = [("ready", baseline["clocks"]["initial_flow_ready"]["utc"]), ("engine-source-on", baseline_engine["start_utc"]), ("first-real-Radar-frame", baseline_radar["start_utc"]), ("first-real-Helm-frame", baseline_helm["start_utc"]), ("settled", baseline["clocks"]["settled_Zone04"]["utc"])]
final_onsets = [("ready", ready["utc"]), ("engine-source-on", final_engine["start"]["utc"]), ("first-real-Radar-frame", final_radar["utc"]), ("first-real-Helm-frame", final_helm["utc"]), ("settled", settled["utc"])]
def spacings(points):
    intervals = [{"from": a[0], "to": b[0], "seconds_utc": utc_seconds(a[1], b[1])} for a, b in zip(points, points[1:])]
    return {"onsets_and_boundaries": [{"kind": k, "utc": u} for k, u in points], "intervals": intervals, "largest_inter_primary_without_edges_seconds": max(i["seconds_utc"] for i in intervals[1:-1]), "largest_with_ready_settled_boundaries_seconds": max(i["seconds_utc"] for i in intervals)}
result["selected_primary_onset_spacing_comparison"] = {"definition": "UTC onset-to-onset spacing across actual engine_reflection source-on and first actually captured Radar/Helm preset frames. PhotoLab reactions, opportunities and the final7.632ms sample-only Helm preset episode without a true captured frame are excluded. The preceding peak duration is INCLUDED, so this is not literally sound-free or exposure-free time.", "baseline": spacings(baseline_onsets), "final": spacings(final_onsets), "caveat": "Missions, ambience, other presentation, UI, tool scheduling and operator time occur inside these intervals. The long baseline ready-to-engine interval also crosses424.104260s of unrecorded intersegment gaps. No claim that those gaps contain no primary exposure, no tension score or causal wall-time improvement."}
result["selected_primary_end_to_next_confirmed_onset_comparison"] = {"definition": "Largest internal gap from the previous selected primary source/preset reported end to the next selected primary onset corroborated by a real captured surface frame. Still excludes other sound/presentation and does not measure human experience.", "baseline_largest_seconds_utc": utc_seconds(baseline_engine["end_observed_utc"], baseline_radar["start_utc"]), "final_largest_seconds_utc": utc_seconds(presets["Radar:FalseRadarSweep"]["intervals"][0]["end_state_observed"]["utc"], final_helm["utc"]), "baseline_pair": "engine end to Radar first", "final_pair": "Radar end to Helm first"}
reflection_starts = [i for i in pending_changes if i["after"] == "reflection.playing"]
reflection_returns = [i for i in pending_changes if i["before"] == "reflection.playing" and i["after"] == "reflection.armed"]
result["reflection_retry_evidence"] = {"sequence_launch_observations": reflection_starts, "audience_loss_and_rearmed_observations": reflection_returns, "interpretation": "Two incomplete lead episodes returned playing→armed with activeSequences1→0 and panel/audience change, without reflection receipt. The second has one reported preset window7.632ms and zero actual captured true frames; no soft_movement/close_click window there. The third reaches actual Helm surface+voice and one receipt after L3/craft/install/Rest. Three launch attempts are not three delivered scares.", "sample_only_interrupted_preset_seconds": presets["Reflection:ProceduralReflection"]["intervals"][0]["seconds_estimated"], "verified_final_helm_preset_seconds": presets["Reflection:ProceduralReflection"]["intervals"][1]["seconds_estimated"]}
assert len(mission) == 8, "Expected all eight actual objectives before completion."
(ROOT / "05_Final_Timeline.json").write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
compact = {"clocks": result["clocks"], "metrics": dict(metrics), "partition": dict(partition), "mission": mission, "rest_count": len(rest), "receipts": receipts, "opportunities": opportunities, "pending_changes": pending_changes, "review_candidates": review_candidates, "last_valid": point(last_valid), "presets": {k: {"seconds": v["seconds_estimated"], "intervals": len(v["intervals"])} for k, v in presets.items()}, "voices": {k: {"seconds": v["seconds_estimated"], "intervals": len(v["intervals"])} for k, v in voices.items()}}
(ROOT / "final" / "offline-metrics.json").write_text(json.dumps(compact, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(json.dumps({"clock_seconds": result["clocks"]["flow_ready_to_settled_utc_seconds"], "metrics": dict(metrics), "rest_count": len(rest), "mission_count": len(mission), "presets": compact["presets"], "last_valid_distance": last_valid["distance"]}, ensure_ascii=False, indent=2))
