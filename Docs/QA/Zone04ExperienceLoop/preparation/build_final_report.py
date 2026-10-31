"""Seal the completed Zone04 final voyage from real, closed recorder segments.

Offline only. Imports the historical parser/muxer without changing it. Rejects
live/incomplete evidence, a normal ending, unequal initial snapshots, missing
reviewed real images, failed muxes or a changed frozen candidate. Never connects
to Unity, generates PCM/images, or changes Assets/Tools/save files.

Run after the sole pilot has closed every final recorder:
  python preparation/build_final_report.py --mux
Manual visual observations belong in preparation/reviewed-final-frames.json.
"""
from __future__ import annotations

import argparse
import collections
import hashlib
import importlib.util
import json
import math
import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
REPO = ROOT.parents[2]


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def dump(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def digest(path):
    value = hashlib.sha256()
    with path.open("rb") as stream:
        while block := stream.read(1024 * 1024):
            value.update(block)
    return dict(path=str(path.relative_to(ROOT)) if path.is_relative_to(ROOT) else str(path),
                bytes=path.stat().st_size, sha256=value.hexdigest())


def load_module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def preflight_final():
    """Reject open/incomplete routes using metadata before opening WAVs/muxing."""
    segments, first_committed, full = [], None, None
    timelines = list((ROOT / "final").rglob("timeline.jsonl"))
    assert timelines, "No actual final recorder timelines found."
    for timeline in timelines:
        path = timeline.parent
        frame_log = path / "frames.jsonl"
        assert frame_log.is_file(), f"Recorder has no actual frame log: {path}"
        counts = collections.Counter()
        frames = 0
        for source in (timeline, frame_log):
            with source.open(encoding="utf-8-sig") as stream:
                for number, line in enumerate(stream, 1):
                    if not line.strip():
                        continue
                    row = json.loads(line)  # closed evidence may not hide malformed lines
                    if source == timeline:
                        counts[row.get("kind")] += 1
                    else:
                        frames += 1
                    if row.get("committedEndingReached") in ("hidden", "normal"):
                        if first_committed is None or row["utc"] < first_committed["utc"]:
                            first_committed = {key: row.get(key) for key in ("utc", "elapsedWall", "committedEndingReached", "endingReceiptSource")}
                    if (row.get("scene") == "Ending" and row.get("transitioning") is False
                            and row.get("endingPresentationVisible") is True
                            and row.get("endingPresentationId") == row.get("committedEndingReached") == "hidden"
                            and row.get("endingPresentationElapsed", 0) >= 16):
                        if full is None or row["utc"] < full["utc"]:
                            full = {key: row.get(key) for key in ("utc", "elapsedWall", "endingPresentationId", "endingPresentationElapsed", "committedEndingReached")}
        assert counts["recording.start"] == counts["recording.stop"] == 1 and (path / "summary.txt").is_file(), f"Every actual final recorder must be closed: {path}"
        assert frames > 0, f"No actual captured frames: {path}"
        segments.append(dict(path=str(path), actual_frames=frames))
    assert first_committed and first_committed["committedEndingReached"] == "hidden", "Intended actual physical hidden ending is not yet committed."
    assert full, "Actual matching hidden presentation has not reached >=16s."
    return dict(status="closed recorder metadata and full hidden presentation verified before audio/mux", segments=segments,
                first_committed_ending=first_committed, full_hidden_presentation=full)


def write_report(data):
    """Render facts already verified by main; no missing ending is invented."""
    clock = data["clocks"]
    quality = data["segment_quality_and_control"]
    audio = data["aggregate_audio_quality"]
    control = data["observed_control_summary"]
    metrics = data["interval_metrics_nonexclusive_ready_to_terminal_sample"]

    def num(value, places=3):
        return "N/A" if value is None else f"{value:,.{places}f}"

    def link(path, label):
        path = pathlib.Path(path)
        target = str(path.relative_to(ROOT)).replace("\\", "/") if path.is_absolute() and path.is_relative_to(ROOT) else str(path).replace("\\", "/")
        return f"[{label}]({target})"

    def point_text(point):
        return f"{point['utc']} / {point['segment']} +{point['elapsedWall']:.3f}s"

    lines = [
        "# Zone04 — final actual-input playthrough",
        "",
        "**SEALED: actual physical hidden Ending and the full 16-second presentation were observed.** This is the final actual-input voyage after the single redesign in [03](03_Horror_Redesign.md). The [baseline](01_Baseline_Playtest.md) remains incomplete before L2; native tests in [04](04_Implementation_Validation.md) remain separate controlled regressions.",
        "",
        "## Start, controls and timing authority",
        "",
        f"The actual Main Menu DEBUG Zone04 UI started with regular upgrades NONE and fresh isolated save/photo directories. The **entire parsed initialized save snapshot equals baseline**, with no differing field; exact file hashes and the comparison are in [05 timeline](05_Final_Timeline.json). All **{data['candidate_freeze_verification']['checked_files']} frozen candidate hashes** still match at offline analysis.",
        "",
        "Queued ordinary keyboard/pointer input, the existing WASD chart driver and visible-hook CaptureInputAgent assisted the voyage. Screenshot perception used freshly rendered actual terrain-toggle ON/OFF pairs; source screenshots, exact hashes, parameters and advice artifacts are listed in the timeline. The advice used rendered pixels and publicly available UI coordinates, not private terrain geometry or undiscovered contacts. Ordinary Rest actions and every observed detour/damage are retained. This method does not establish novice-human performance or subjective experience.",
        "",
        "During continuation-02 the operator corrected desktop click targeting from each icon's transform pivot to the center of its actual RectTransform. Bounded Editor callbacks then queued ordinary input. Earlier ignored/mistargeted attempts remain in the timing and do not establish a game UI defect or human hesitation. The first actual capture attempt did not complete; a subsequent visible-hook input-agent attempt earned the receipt. These operator-method changes and latency remain comparison limits.",
        "",
        "During L2 recovery, the operator consulted the read-only photo eligibility rule after NoSubject attempts at depth588.9575m, identified the minimum590m, then descended through ordinary controls to595.863m before obtaining LifeDetected. That threshold check supplied no actor or terrain coordinates and changed no rule, resource or position. This additional operator knowledge, the preceding attempts and all ordinary Rest/refills remain disclosed; novice-human performance is not measured.",
        "",
        f"First initialized, nontransitioning Zone04 is **{clock['initial_flow_ready']['utc']}**. Recorded menu/loading and external focus attempts precede this boundary by **{num(data['pre_ready_boundary']['seconds'])}s**. Initially unfocused clicks were ignored until window activation; those attempts remain in the raw recording and do not move the voyage clock.",
        "",
        "| Clock boundary | Actual UTC / segment-local clock | Seconds after first-ready |",
        "| --- | --- | ---: |",
    ]
    for label, key in (("First-ready", "initial_flow_ready"), ("First committed ending receipt", "first_committed_ending"), ("First Ending scene", "first_ending_scene"), ("Settled matching hidden presentation", "settled_ending"), ("Full hidden presentation (elapsed ≥16s)", "full_ending")):
        point = clock[key]
        lines.append(f"| {label} | {point_text(point)} | {num(point['flow_elapsed_seconds_utc'])} |")
    lines.extend([
        "",
        f"Total recorded wall duration is **{num(clock['total_recorded_wall_seconds'])}s**; actual recorder-boundary gaps total **{num(clock['intersegment_marker_gap_seconds'])}s**. First-ready→committed is **{num(clock['ready_to_committed_seconds'])}s**, first-ready→settled is **{num(clock['ready_to_settled_seconds'])}s**, and first-ready→full Ending is **{num(clock['ready_to_full_ending_seconds'])}s** of raw UTC. Recorded first-ready→full Ending coverage is **{num(metrics.get('recorded_ready_to_full_ending_seconds'))}s**. These are distinct clocks; external interruption is not gameplay or horror pacing. Sampled Ending clocks are authoritative; video container duration is separate. No gap is filled with replacement images/audio.",
        "",
        "## External gap and checkpoint replay",
        "",
        "The final timeline includes every original segment and any real DEBUG CONTINUE replay. Closing a recorder does not create a new baseline or reset the first-ready UTC. Continue is not an additional Rest. State changes across segments are retained as discontinuities and are excluded from ordinary Rest, damage, cue-counter and choice-action counts.",
        "",
    ])
    for boundary in data["recorder_resume_boundaries"]:
        before, after = boundary.get("comparison_before", boundary["prior_last_initialized"]), boundary.get("comparison_after", boundary["resumed_first_initialized"])
        lines.append(f"- **{boundary['before_segment']} → {boundary['after_segment']}**: recorder stop **{boundary['previous_stop']['utc']}**, next start **{boundary['next_start']['utc']}**, real marker gap **{num(boundary['marker_gap_seconds'])}s**. No replacement audio/image is present in the gap.")
        if boundary.get("live_recorder_rotation_observed"):
            lines.extend(["  **Live recorder rotation:** actual stop/start Zone04 state and the continuing runtime clock match. No checkpoint reload or rollback is observed. Any earlier Rest/refill is counted only where its actual transition was observed, and is not counted again at this boundary.", ""])
            continue
        if boundary.get("unrecorded_live_actions_disclosed"):
            lines.extend([
                f"  **Unrecorded live actions, disclosed by the pilot:** the runtime clock continued. The recording endpoints change from day{before['day']}, depth{num(before['depth'], 6)}, heading{num(before['heading'], 6)}, energy{num(before['energy'], 6)}, photos{before['photosTaken']} to day{after['day']}, depth{num(after['depth'], 6)}, heading{num(after['heading'], 6)}, energy{num(after['energy'], 6)}, photos{after['photosTaken']}. The start-of-segment disclosure reports compass-photo attempts and actual Rest outside recording; the tool transcript/checkpoint supports that account, while no intervening frame, PCM or exact action timestamp is available here.",
                "  This is not classified as a checkpoint load or replay. The day difference is not counted as a raw-recorded Rest, and the photographs/Rest are not assigned invented times. The full disclosure and endpoint differences remain in JSON.", "",
            ])
            continue
        termination = boundary.get("previous_termination", {})
        if termination.get("stop_zone04_telemetry_unavailable") and termination.get("last_initialized_zone04"):
            lines.append(f"  The stop row has no Zone04 telemetry. The last initialized state is **{termination['last_initialized_zone04']['utc']}**, **{num(termination['last_initialized_to_stop_seconds'], 6)}s** before recorder destruction. Its empty cleanup values do not establish game death, depleted resources or lost photos; the before/after comparison uses the actual last initialized state.")
        if not before or not after:
            lines.extend(["  One side has no initialized Zone04 observation; no state continuity or rollback is inferred.", ""])
            continue
        lines.extend([
            f"  Actual initialized observations change from day{before['day']}, X{num(before['x'], 6)}/Y{num(before['y'], 6)}, depth{num(before['depth'], 6)}, heading{num(before['heading'], 6)}, distance{num(before['distance'], 6)}, energy{num(before['energy'], 6)}, hull{num(before['hull'], 6)}, photos{before['photosTaken']} to day{after['day']}, X{num(after['x'], 6)}/Y{num(after['y'], 6)}, depth{num(after['depth'], 6)}, heading{num(after['heading'], 6)}, distance{num(after['distance'], 6)}, energy{num(after['energy'], 6)}, hull{num(after['hull'], 6)}, photos{after['photosTaken']}. Distance-counter rollback: **{num(boundary['distance_counter_rollback_metres'], 6)}m**; photo-count rollback: **{boundary['photo_count_rollback']}**. Exact receipt/resource differences and corresponding actual samples are in JSON.",
            "",
        ])
    lines.extend([
        "A lower resumed photo count records a loss from the live game state, not deletion of loose PNG archives. Unsubmitted photographs and the original resource use remain part of the recorded attempt even when a durable checkpoint does not retain them. The resumed route is a continuation of this final evaluation with checkpoint replay, not a seamless same-live voyage or a fresh comparison run.",
        "",
        "## Annotated actual route",
        "",
        "| Observable receipt or event | Actual UTC / segment-local clock | Seconds after first-ready |",
        "| --- | --- | ---: |",
    ])
    highlights = []
    for group, label in (("mission_first_observations", "Objective"), ("primary_receipt_first_observations", "Presentation receipt"), ("revealed_location_first_observations", "Radar reveal")):
        highlights.extend((item["observation"], f"{label}: `{item['value']}`") for item in data[group])
    highlights.extend((item["observation"], f"Rest day{item['before']}→day{item['after']}") for item in data["rest_day_transitions"])
    highlights.extend((item["observation"], f"Actual ending choice: `{item['after']}`") for item in data["ending_choice_changes"])
    for point, label in sorted(highlights, key=lambda item: item[0]["utc"]):
        lines.append(f"| {label} | {point_text(point)} | {num(point['flow_elapsed_seconds_utc'])} |")
    lines.extend([
        "",
        f"There were **{control['rest_count']} Rest day transitions observed within recordings**, **{control['obstruction_stop_markers']} obstruction-stop markers** and **{control['observed_hull_decrease_events']} sampled hull decreases**, totalling **{num(control['sum_observed_hull_decreases'])} hull units across refills and any replay**. Pilot-disclosed Rest outside a recorder is listed separately above and is not added to that raw-recorded count. These are different quantities: a latched obstruction may stop a driver without a new impact, and sampled hull loss is not a complete physical-contact count. Checkpoint-boundary and unrecorded-live changes are listed separately above. Exact positions, days, values and input markers remain in the timeline.",
        "",
        "## Presentation evidence",
        "",
        "Actual rendered images were reviewed independently of receipts, sequence registration and source playback clocks. A receipt establishes the implemented checkpoint, not human attention, full-clip listening, recognition or fear. Exposure windows below integrate actual left-held samples and can miss brief boundaries.",
        "",
        "| Cue | First actual durable receipt | Measurable source-clock window, estimated seconds | Corresponding actual audience/visual seconds |",
        "| --- | --- | ---: | ---: |",
    ])
    cue_specs = (("fragment", "fragmentAudioTime", "fragmentVisual"), ("hidden.a", "hiddenAAudioTime", "hiddenAPhotoAudience"), ("hidden.b", "hiddenBAudioTime", "hiddenBPhotoAudience"), ("final.signal", "finalSignalAudioTime", "finalWaterTintVisible"))
    for cue, source, surface in cue_specs:
        found = next((item["observation"] for item in data["primary_receipt_first_observations"] if item["value"] == cue), None)
        exposure = data["actual_measurable_voice_clock_windows"].get(source, {}).get("seconds_estimated")
        visual = data["actual_boolean_surface_windows"].get(surface, {}).get("seconds_estimated")
        lines.append(f"| `{cue}` | {found['utc'] if found else 'NOT OBSERVED'} | {num(exposure)} | {num(visual)} |")
    lines.append("")
    for item in data["reviewed_actual_frames"]:
        lines.extend([f"- **{item['category']}** — {link(item['image'], 'actual image')}: {item['interpretation']}", ""])
    for item in data.get("non_evidentiary_capture_artifacts", []):
        lines.extend([f"- **Excluded capture artifact** — {link(item['image'], 'retained file')}: {item['reason']}", ""])
    lines.extend([
        "The timeline separately retains observed stage/pending changes, environmental-level samples, actual sequence windows and actual listener PCM. Reduced environmental level and a recovery stage are technical observations; spatial anticipation, headset balance and the experience of being watched remain unmeasured. Existing creature-art limitations from [04](04_Implementation_Validation.md) remain.",
        "",
        "## Activity and unknown time",
        "",
        "These sampled intervals cover recorded first-ready→full Ending only. Categories are nonexclusive. The separate state partition in JSON is observational, not a human behavior classification.",
        "",
        "| Observed state/activity | Seconds |",
        "| --- | ---: |",
    ])
    for label, key in (("Navigation: moving/turning/depth change", "navigation_activity_seconds"), ("Horizontal motion", "horizontal_motion_seconds"), ("Turning", "turning_seconds"), ("Depth change", "changing_depth_seconds"), ("Chart driver owner", "owner_navigation_driver_seconds"), ("Capture UI active", "capture_ui_active_seconds"), ("Visible Photo Lab", "photo_lab_visible_seconds"), ("Visible Mission Log", "mission_log_visible_seconds"), ("Visible Rest app", "rest_ui_visible_seconds"), ("Explicit paired operator processing", "explicit_operator_gap_matched_recorded_seconds"), ("Observed unfocused", "unfocused_seconds"), ("Editor pause/nonpositive timeScale", "observed_pause_seconds"), ("No active injected-input owner", "no_input_owner_seconds"), ("No owner outside paired operator processing", "no_input_owner_outside_paired_operator_gap_seconds")):
        lines.append(f"| {label} | {num(metrics.get(key, 0))} |")
    lines.extend([
        "",
        "Unmarked no-owner time remains unknown. It is not called indecision, fear, boredom, recovery or all agent processing. Unmatched processing markers remain unmatched. Input-owner observations reset at each recorder boundary rather than inventing continuity across unobserved gaps.",
        "",
        "## Actual media and audio quality",
        "",
        "| Segment | Wall seconds | Actual JPEG / skipped slots | Actual PCM frames / duration | RMS dBFS | PCM rail values / frames | Observed dropped values |",
        "| --- | ---: | ---: | ---: | ---: | ---: | ---: |",
    ])
    for segment in quality:
        value = segment["audio"]
        lines.append(f"| {segment['name']} | {num(float(segment['summary']['wallSeconds']))} | {segment['frame_count']:,} / {segment['video']['reported_skipped_slots']:,} | {value['sample_frames']:,} / {num(value['duration_seconds'], 6)}s | {num(value['rms_dbfs'], 6)} | {value['rail_sample_values']} / {value['sample_frames_with_any_rail']} | {segment['audio_counter_evidence']['observed_dropped_sample_values']:,} |")
    lines.extend([
        "",
        f"Actual audio totals **{audio['sample_frames']:,} frames / {num(audio['duration_seconds'], 6)}s**, 48kHz stereo PCM16, aggregate RMS **{num(audio['rms_dbfs'], 6)}dBFS**; **{audio['rail_sample_values']} rail values across {audio['sample_frames_with_any_rail']} sample frames**, **{audio['dropped_sample_values']:,} observed dropped sample values**, and **{audio['recorder_format_changes']} format changes in raw recorder summaries**. Drop counts use maximum actual counters per listener, retaining raw summary values separately; unavailable cleanup values do not erase prior loss. Inferred positive listener-replacement gaps total **{num(audio['inferred_positive_listener_replacement_gap_seconds'], 6)}s**. Saturation rails identify limiting/clipping candidates; they do not establish perceptual distortion, intelligibility or hearing.",
        "",
        "The VFR MP4s use actual JPEG timestamps at a millisecond input timebase, holding each real image to the next capture and the last to recorder stop. Actual WAVs are AAC-encoded with piecewise timestamp offsets for inferred listener-replacement gaps. No interpolated image, synthetic PCM, copied audio from another clip, resampled gap-fill or silent replacement is used. Internal buffer loss has no exact dropped-chunk timestamps: the mux does not invent a precise gap location. Such segments have degraded synchronization, and later audio can lead images by the cumulative loss duration until a later listener boundary. A successful encode does not establish synchronized exposure. Initial callback alignment has a sampled bracket; PCM boundaries are inferred from counters. DSP/device latency, AAC packet rounding and decoder gap handling limit sync; originals remain authoritative.",
        "",
    ])
    for segment in quality:
        path = pathlib.Path(segment["path"])
        bracket = segment["first_callback_bracket"]
        bracket_width = (bracket[1]-bracket[0]) if len(bracket) == 2 and None not in bracket else None
        lines.append(f"- **{segment['name']}**: {link(path / 'gameplay.mp4', 'actual video')}, {link(path / 'actual-game-audio.wav', 'actual WAV')}, {link(path / 'mux-result.json', 'mux/ffprobe')}, {link(path / 'event-index.json', 'event index')}. Maximum frame gap {num(segment['video']['maximum_actual_frame_gap_seconds'], 6)}s; after resource-ready {num(segment['video']['maximum_after_ready_frame_gap_seconds'], 6)}s. First-callback bracket width {num(bracket_width, 7)}s; inferred listener gap {num(segment['inferred_positive_listener_replacement_gap_seconds'], 6)}s. Exact clocks remain in JSON.")
        counter = segment["audio_counter_evidence"]
        if counter["observed_dropped_sample_values"]:
            lines.append(f"  **Degraded audio synchronization:** {counter['observed_dropped_sample_values']:,} observed dropped sample values correspond to **{num(counter['missing_pcm_seconds'], 6)}s** missing PCM. Raw summary reports {counter['raw_summary_dropped_sample_values']:,}; the counter evidence is retained. No replacement PCM or precise inferred loss-gap timestamp is inserted.")
            for event in counter["observed_loss_brackets"]:
                lines.append(f"  Loss first appears between **{event['before']['utc']} / +{num(event['before']['elapsedWall'], 6)}s** and **{event['after']['utc']} / +{num(event['after']['elapsedWall'], 6)}s**, a **{num(event['location_uncertainty_seconds'], 6)}s** observation bracket; exact missing chunk positions are unknown.")
    lines.extend([
        "",
        "Exact raw-file, actual-JPEG and review-image SHA256 manifests, commands, per-segment listener offsets and container probes are in [05 timeline](05_Final_Timeline.json). Reproduce from the repository root with `python Docs/QA/Zone04ExperienceLoop/preparation/build_final_report.py --mux`; the script verifies the complete ending, matching initial snapshots, candidate hashes and manually reviewed source images before publishing this report.",
        "",
        "## Comparison limits",
        "",
        "The baseline ended before L2, hidden A/B, final signal or either Ending. Those baseline downstream fields remain **N/A**, not zero exposure or a failed scare. The final route provides actual downstream evidence, but no elapsed-time subtraction supports a speed improvement: learning, changed chart interpretation/screenshot assistance, baseline and final checkpoint rollback/replay, real external interruptions, automated controls and recording/operator overhead confound the comparison. Native 33/33 results validate selected regressions and never substitute for this voyage.",
        "",
        "Human fear, immersion, reading speed, cue recognition and headset listening remain unmeasured. This report records actual state, pixels, input and listener PCM after one redesign; it makes no subjective improvement claim.",
        "",
    ])
    (ROOT / "05_Final_Playtest.md").write_text("\n".join(lines), encoding="utf-8")


def main():
    cli = argparse.ArgumentParser(description=__doc__)
    cli.add_argument("--mux", action="store_true", help="Mux actual VFR frames and listener WAV for each closed segment.")
    cli.add_argument("--review", type=pathlib.Path, default=ROOT / "preparation/reviewed-final-frames.json")
    args = cli.parse_args()

    # Whole parsed snapshots are compared. No field whitelist or gameplay-only
    # normalization can silently permit a different initialized state.
    baseline_initial = ROOT / "baseline/initial-debug-save.json"
    final_initial = ROOT / "final/initial-debug-save.json"
    assert read(baseline_initial) == read(final_initial), "Initial snapshots differ."
    assert baseline_initial.read_bytes() == final_initial.read_bytes(), "Initial snapshot file bytes differ."
    comparison = read(ROOT / "final/initial-state-comparison.json")
    assert comparison.get("equivalent") is True and comparison.get("differences") == []

    freeze = read(ROOT / "candidate-freeze.json")
    differences = []
    for entry in freeze["files"]:
        path = REPO / entry["path"]
        actual = digest(path)["sha256"] if path.exists() else None
        if actual != entry["sha256"].lower():
            differences.append(dict(path=entry["path"], expected=entry["sha256"], actual=actual))
    assert not differences, f"Frozen candidate differs: {differences}"

    aggregate = load_module("zone04_actual_final_aggregate", ROOT / "preparation/analyze_zone04_voyage.py")
    resume = load_module("zone04_final_resume_evidence", ROOT / "preparation/final_resume_evidence.py")
    preflight = preflight_final()
    aggregate.FIELDS += ("experienceStage", "pendingCue", "audioDspTime", "blocked", "moving", "turning", "changingDepth")
    data = aggregate.analyze("final", mux=args.mux)
    assert data["status"] == "SEALED actual full hidden ending"
    assert data["clocks"]["first_committed_ending"]["committedEndingReached"] == "hidden", "First actual ending must be the intended hidden route."
    assert all(segment["closed"] for segment in data["segments"])
    for segment in data["segments"]:
        assert segment["analysis"].get("mux", {}).get("status") == "success", "Successful actual-media mux required."

    review = read(args.review)
    reviewed = review["reviewed_actual_frames"]
    assert reviewed, "Manual actual-image review is required before sealing."
    for item in reviewed:
        path = pathlib.Path(item["image"])
        assert path.is_file() and path.is_relative_to(ROOT / "final"), path
        actual = digest(path)
        if "sha256" in item:
            assert actual["sha256"] == item["sha256"].lower(), path
        item.update(sha256=actual["sha256"], bytes=actual["bytes"])
    excluded_captures = review.get("non_evidentiary_capture_artifacts", [])
    for item in excluded_captures:
        path = pathlib.Path(item["image"])
        assert path.is_file() and path.is_relative_to(ROOT / "final"), path
        actual = digest(path)
        if "sha256" in item:
            assert actual["sha256"] == item["sha256"].lower(), path
        item.update(sha256=actual["sha256"], bytes=actual["bytes"])
    required_reviews = {"chart-terrain-on", "chart-terrain-off", "fragment", "hidden.a", "hidden.b", "final.signal", "hidden-ending", "full-aftermath"}
    assert required_reviews <= {item.get("category") for item in reviewed}, "Review all actual route presentation categories."

    raw_manifest, frame_manifest, local_quality, allrows, resume_segments = [], [], [], [], []
    for segment in data["segments"]:
        path = pathlib.Path(segment["path"])
        rows, frames = [], []
        for name in ("timeline.jsonl", "frames.jsonl"):
            values = aggregate.read_rows(path / name)
            for row in values:
                row["_segment"] = str(path.relative_to(ROOT / "final"))
            rows.extend(values)
            if name == "frames.jsonl":
                frames = values
        rows.sort(key=lambda row: row["utc"])
        allrows.extend(rows)
        resume_segments.append(dict(name=str(path.relative_to(ROOT / "final")), rows=rows))
        damage = []
        previous = None
        for row in rows:
            if previous and row.get("zone") == previous.get("zone") == "Zone04" and row.get("hull", 0) < previous.get("hull", 0) and not row.get("transitioning"):
                damage.append(dict(utc=row["utc"], before=previous["hull"], after=row["hull"], decrease=previous["hull"]-row["hull"], x=row["x"], y=row["y"], day=row["day"]))
            previous = row
        stops = [{key: row.get(key) for key in ("utc", "elapsedWall", "x", "y", "day", "hull", "detail")}
                 for row in rows if row.get("kind") == "input.navigation.stop" and "obstruction" in row.get("detail", "")]
        analysis = segment["analysis"]
        listener_segments = analysis["audio_listener_segments"]
        counter_evidence = resume.audio_counter_evidence(rows, segment["recorder_summary"], analysis["audio"])
        analysis["audio_counter_evidence"] = counter_evidence
        analysis["recorder_termination"] = resume.termination(dict(name=str(path.relative_to(ROOT / "final")), rows=rows))
        analysis["mux"]["synchronization_status"] = counter_evidence["synchronization_status"]
        analysis["mux"]["internal_pcm_loss_gap_inserted"] = False
        analysis["mux"]["internal_pcm_loss_seconds"] = counter_evidence["missing_pcm_seconds"]
        analysis["mux"]["internal_pcm_loss_brackets"] = counter_evidence["observed_loss_brackets"]
        if counter_evidence["observed_dropped_sample_values"]:
            analysis["audio_sync_caveat"] += " Internal dropped PCM has no exact chunk timestamps; listener-only mux synchronization is degraded. See actual counter brackets; successful encoding is not evidence of aligned exposure."
        # Derived indexes may be enriched only after the full-ending/closure
        # guards above. Original timeline, JPEGs, WAV and summaries are unchanged.
        dump(path / "event-index.json", analysis)
        dump(path / "mux-result.json", analysis["mux"])
        local_quality.append(dict(path=str(path), name=path.name, frame_count=len(frames), summary=segment["recorder_summary"],
                                  video=analysis["video"], audio=analysis["audio"], listener_segments=listener_segments,
                                  audio_counter_evidence=counter_evidence,
                                  termination=resume.termination(dict(name=str(path.relative_to(ROOT / "final")), rows=rows)),
                                  inferred_positive_listener_replacement_gap_seconds=sum(max(0, after["pts_shift_seconds_estimated"]-before["pts_shift_seconds_estimated"]) for before, after in zip(listener_segments, listener_segments[1:])),
                                  first_callback_bracket=analysis["first_audio_callback_elapsed_bracket_seconds"], mux=analysis["mux"],
                                  obstruction_stops=stops, distinct_obstruction_stop_positions=len({(round(row["x"], 3), round(row["y"], 3)) for row in stops}), observed_hull_decreases=damage))
        for name in ("timeline.jsonl", "frames.jsonl", "summary.txt", "actual-game-audio.wav", "gameplay.mp4", "mux-command.json", "mux-result.json", "frames-vfr.ffconcat", "event-index.json"):
            raw_manifest.append(digest(path / name))
        frame_manifest.extend(digest(path / row["image"]) for row in frames)
    allrows.sort(key=lambda row: row["utc"])
    first_ready = data["clocks"]["initial_flow_ready"]
    ready_time = aggregate.stamp(first_ready)
    full_time = aggregate.stamp(data["clocks"]["full_ending"])
    stage_windows, pending_windows = collections.defaultdict(float), collections.defaultdict(float)
    lower_environment = []
    state_events = []
    previous = None
    for row in allrows:
        if previous and previous["_segment"] == row["_segment"]:
            duration = max(0, min(aggregate.stamp(row), full_time) - max(aggregate.stamp(previous), ready_time))
            stage_windows[previous.get("experienceStage", "unobserved")] += duration
            pending_windows[previous.get("pendingCue", "unobserved")] += duration
        changes = {key: dict(before=previous.get(key) if previous else None, after=row.get(key))
                   for key in ("experienceStage", "pendingCue", "environmentLevel", "finalWaterRequested", "finalWaterTintVisible")
                   if previous is None or row.get(key) != previous.get(key)}
        if changes:
            state_events.append(dict(utc=row["utc"], elapsedWall=row["elapsedWall"], segment=row["_segment"], changes=changes))
        if row.get("zone") == "Zone04" and isinstance(row.get("environmentLevel"), (int, float)) and row["environmentLevel"] < .999:
            lower_environment.append(dict(utc=row["utc"], elapsedWall=row["elapsedWall"], segment=row["_segment"], environmentLevel=row["environmentLevel"], pendingCue=row.get("pendingCue"), experienceStage=row.get("experienceStage")))
        previous = row

    formats = {(segment["audio"]["channels"], segment["audio"]["sample_rate"], segment["audio"]["sample_width_bytes"]) for segment in local_quality}
    assert formats == {(2, 48000, 2)}, f"Unexpected actual PCM format: {formats}"
    samples = sum(segment["audio"]["sample_values"] for segment in local_quality)
    rms = math.sqrt(sum(segment["audio"]["rms_normalized"] ** 2 * segment["audio"]["sample_values"] for segment in local_quality) / samples)
    audio = dict(sample_frames=sum(segment["audio"]["sample_frames"] for segment in local_quality), sample_values=samples,
                 channels=2, sample_rate=48000, duration_seconds=sum(segment["audio"]["duration_seconds"] for segment in local_quality),
                 rms_normalized=rms, rms_dbfs=20 * math.log10(rms) if rms else None,
                 rail_sample_values=sum(segment["audio"]["rail_sample_values"] for segment in local_quality),
                 sample_frames_with_any_rail=sum(segment["audio"]["sample_frames_with_any_rail"] for segment in local_quality),
                 dropped_sample_values=sum(segment["audio_counter_evidence"]["observed_dropped_sample_values"] for segment in local_quality),
                 missing_pcm_seconds_from_observed_drop_counters=sum(segment["audio_counter_evidence"]["missing_pcm_seconds"] for segment in local_quality),
                 segments_with_degraded_internal_loss_sync=[segment["name"] for segment in local_quality if segment["audio_counter_evidence"]["observed_dropped_sample_values"]],
                 recorder_format_changes=sum(int(segment["summary"].get("formatChanges", 0)) for segment in local_quality),
                 inferred_positive_listener_replacement_gap_seconds=sum(segment["inferred_positive_listener_replacement_gap_seconds"] for segment in local_quality))
    advice = []
    for path in sorted((ROOT / "preparation").rglob("advice.json")):
        item = read(path)
        # Only actual final screenshot-derived advice participates. Historical
        # baseline, controlled diagnosis and native captures remain excluded.
        sources = item.get("sources", {})
        if sources and all(pathlib.Path(source["path"]).is_relative_to(ROOT / "final") for source in sources.values()):
            for source in sources.values():
                assert digest(pathlib.Path(source["path"]))["sha256"] == source["sha256"].lower()
            advice.append(dict(artifact=digest(path), disclosure=item.get("disclosure"), status=item.get("status"), route_issued=item.get("route_issued"), sources=sources, inputs=item.get("inputs")))
    baseline = read(ROOT / "01_Baseline_Timeline.json")
    boundary_evidence = resume.boundaries(resume_segments)
    data.update(schema="zone04-final-evidence-v2", route_completion="Actual physical hidden Ending and full>=16-second presentation observed",
                initial_state_equivalence=dict(comparison=comparison, whole_parsed_snapshots_equal=True, snapshot_bytes_equal=True, baseline=digest(baseline_initial), final=digest(final_initial)),
                candidate_freeze_verification=dict(manifest=digest(ROOT / "candidate-freeze.json"), checked_files=len(freeze["files"]), differences=differences),
                sealing_preflight=preflight,
                recorder_resume_boundaries=boundary_evidence,
                unrecorded_live_action_gaps=[value for value in boundary_evidence if value.get("unrecorded_live_actions_disclosed")],
                segment_quality_and_control=local_quality, aggregate_audio_quality=audio,
                observed_control_summary=dict(obstruction_stop_markers=sum(len(segment["obstruction_stops"]) for segment in local_quality),
                                              observed_hull_decrease_events=sum(len(segment["observed_hull_decreases"]) for segment in local_quality),
                                              sum_observed_hull_decreases=sum(item["decrease"] for segment in local_quality for item in segment["observed_hull_decreases"]),
                                              rest_count=len(data["rest_day_transitions"])),
                observed_stage_seconds=stage_windows, observed_pending_seconds=pending_windows,
                stage_pending_environment_events=state_events, actual_reduced_environment_samples=lower_environment,
                actual_control_and_operator_markers=[{key: row[key] for key in ("kind", "utc", "elapsedWall", "detail", "_segment", "action", "zone", "scene", "x", "y", "depth", "hull", "day") if key in row}
                                                     for row in allrows if row.get("kind") not in ("sample", "frame")],
                screenshot_perception_assistance=advice, reviewed_actual_frames=reviewed,
                non_evidentiary_capture_artifacts=excluded_captures,
                human_emotion_ratings=None,
                comparison_limits=dict(baseline_status=baseline["status"], baseline_hidden_A_B_final_ending="N/A: not encountered before bounded stop",
                                       time_improvement_claim=False, reason="Incomplete baseline plus learning, new screenshot-perception assistance, checkpoint rollback/replay, external interruption, tool scheduling and recorder overhead prevent a paired time or human-fear improvement claim."),
                raw_artifact_manifest=raw_manifest, actual_jpeg_manifest=frame_manifest,
                supplemental_actual_screenshot_manifest=[digest(path) for path in sorted((ROOT / "final/screens").rglob("*")) if path.is_file()],
                analysis_provenance=dict(python=sys.version, executable=sys.executable,
                                         sources=[digest(pathlib.Path(__file__).resolve()), digest(ROOT / "preparation/analyze_zone04_voyage.py"), digest(ROOT / "preparation/final_resume_evidence.py"), digest(REPO / "Tools/QA/summarize_zone02_experience.py")],
                                         manual_review_artifact=digest(args.review.resolve()),
                                         reproduction="python Docs/QA/Zone04ExperienceLoop/preparation/build_final_report.py --mux"),
                earliest_ready_criterion="Earliest merged actual timeline/frame UTC with Zone04, zoneFourTelemetryObserved true, transitioning false, and positive hull, energy and depth. This is initialized state, not an assertion that every station control has appeared or input focus was acquired.",
                pre_ready_boundary=dict(recording_start_utc=data["clocks"]["recording_start"]["utc"], first_ready_utc=first_ready["utc"],
                                        seconds=ready_time-aggregate.stamp(data["clocks"]["recording_start"]),
                                        meaning="Menu/loading, ignored unfocused click attempts and external window activation precede the initialized Zone04 clock; actual recordings retain them."))
    dump(ROOT / "final/offline-metrics.json", data)
    dump(ROOT / "05_Final_Timeline.json", data)
    write_report(data)
    print(json.dumps(dict(status=data["status"], output=str(ROOT / "05_Final_Timeline.json"), segments=len(local_quality),
                          frames=len(frame_manifest), reviewed_frames=len(reviewed), frozen_files=len(freeze["files"]), clocks={key: value for key, value in data["clocks"].items() if key.endswith("seconds")}), indent=2))


if __name__ == "__main__":
    main()
