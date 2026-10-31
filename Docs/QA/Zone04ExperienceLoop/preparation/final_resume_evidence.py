"""Compare only actual recorder observations across final Continue boundaries.

No Unity, save, source map, private actor coordinates, masks or images are read.
This module does not infer uninterrupted play or an action in an unrecorded gap.
"""
from __future__ import annotations

import datetime as dt


STATE_FIELDS = (
    "day", "x", "y", "depth", "heading", "distance", "energy", "hull",
    "photosTaken", "capturesToday", "radarCharges", "photoCharges", "captureCharges",
    "completedObjectives", "cueReceipts", "revealedLocationIds", "endingChoice",
    "hiddenRouteUnlocked", "hiddenRouteComplete", "committedEndingReached",
)


def stamp(row):
    return dt.datetime.fromisoformat(row["utc"].replace("Z", "+00:00")).timestamp()


def initialized(row):
    return (row.get("zone") == "Zone04"
            and row.get("zoneFourTelemetryObserved") is True
            and row.get("transitioning") is False
            and all(row.get(key, 0) > 0 for key in ("hull", "energy", "depth")))


def point(row, segment):
    if row is None:
        return None
    return dict(segment=segment, **{key: row[key] for key in
                ("kind", "utc", "elapsedWall", "scene", "zone", "panel", "app", "image", "detail", "zoneFourTelemetryObserved", "transitioning", "realtime", "unscaledTime", "frame") + STATE_FIELDS
                if key in row})


def termination(segment):
    """Keep actual last initialized state separate from unavailable cleanup data."""
    rows = sorted(segment["rows"], key=stamp)
    if not rows:
        return dict(classification="Awaiting recorder observations", last_initialized_zone04=None, recording_stop=None)
    last = next((row for row in reversed(rows) if initialized(row)), None)
    stop = next((row for row in reversed(rows) if row.get("kind") == "recording.stop"), None)
    unavailable = bool(stop and (stop.get("zone") != "Zone04" or stop.get("zoneFourTelemetryObserved") is not True))
    completed_ending = bool(stop and stop.get("scene") == "Ending"
                            and stop.get("endingPresentationId") == stop.get("committedEndingReached") in ("hidden", "normal")
                            and stop.get("endingPresentationVisible") is True
                            and stop.get("endingPresentationElapsed", 0) >= 16)
    return dict(
        classification="Recorder stopped in observed completed matching Ending; Zone04 controller absent from Ending scene" if completed_ending else "Recorder cleanup with Zone04 telemetry unavailable; gameplay cause unknown" if unavailable else "Recorder stop with Zone04 telemetry" if stop else "Live recorder; no stop observed",
        last_initialized_zone04=point(last, segment["name"]), recording_stop=point(stop, segment["name"]),
        last_initialized_to_stop_seconds=stamp(stop)-stamp(last) if stop and last else None,
        stop_zone04_telemetry_unavailable=unavailable,
        completed_matching_ending_observed_at_stop=completed_ending,
        meaning="The stop is in the actual matching Ending after its complete16-second presentation. Zone04 ship/resource zeros are unavailable because its controller is absent in the Ending scene, not game death or resource depletion. The separate actual committed/settled/full Ending observations establish completion." if completed_ending else "An empty teardown sample does not establish game death, resource depletion, photograph deletion or checkpoint rollback. Compare the last actual initialized state with the next initialized Continue state; retain the recorder stop only as a wall-clock boundary.",
    )


def audio_counter_evidence(rows, summary, audio):
    """Retain nonzero per-listener counters even if recorder cleanup resets them.

    Scalar snapshots identify the amount of PCM loss and a time bracket, not
    exact missing chunk positions. Do not manufacture a precise mux PTS break.
    """
    groups = {}
    events, resets = [], []
    keys = ("kind", "utc", "elapsedWall", "audioFramesWritten", "audioSampleFrames", "droppedAudioSamples", "audioSampleRate", "audioChannels", "firstAudioCallbackStopwatchSeconds")
    for row in sorted(rows, key=stamp):
        clock = row.get("firstAudioCallbackStopwatchSeconds", 0)
        if not clock:
            continue
        group = groups.setdefault(clock, dict(maximum=0, previous=None))
        value = int(row.get("droppedAudioSamples", 0))
        previous = group["previous"]
        group["maximum"] = max(group["maximum"], value)
        if previous and value > previous.get("droppedAudioSamples", 0):
            increase = value-previous.get("droppedAudioSamples", 0)
            events.append(dict(before={key: previous[key] for key in keys if key in previous},
                               after={key: row[key] for key in keys if key in row},
                               increase_sample_values=increase,
                               missing_pcm_seconds=increase/(audio["channels"]*audio["sample_rate"]),
                               location_uncertainty_seconds=stamp(row)-stamp(previous),
                               elapsed_wall_bracket_seconds=row["elapsedWall"]-previous["elapsedWall"],
                               exact_missing_chunk_positions_known=False))
        if previous and value < previous.get("droppedAudioSamples", 0):
            resets.append(dict(utc=row["utc"], callback_clock=clock, before=previous["droppedAudioSamples"], after=value))
        group["previous"] = row
    observed = sum(group["maximum"] for group in groups.values())
    summary_drop = int(summary.get("droppedAudioSamples", 0))
    effective = max(observed, summary_drop)
    return dict(
        maximum_observed_dropped_sample_values_per_listener=[dict(callback_stopwatch_seconds=clock, sample_values=group["maximum"]) for clock, group in sorted(groups.items())],
        observed_dropped_sample_values=effective,
        interpretation="Observed lower bound; same-listener counter reset prevents exact total" if resets else "Sum of maximum actual counters per distinct listener, or larger recorder summary; no exact dropped-chunk placement inferred",
        missing_pcm_seconds=effective/(audio["channels"]*audio["sample_rate"]),
        observed_loss_brackets=events, same_listener_counter_resets=resets,
        raw_summary_dropped_sample_values=summary_drop, raw_summary_sample_rate=int(summary.get("audioSampleRate", 0)),
        raw_summary_audio_status=summary.get("audioStatus"),
        summary_conflicts_with_observed_counters=summary_drop < observed,
        summary_format_unavailable_despite_valid_wav=int(summary.get("audioSampleRate", 0)) <= 0 and audio["sample_rate"] > 0,
        synchronization_status="DEGRADED: listener-only mux does not locate internal PCM loss; later audio can lead images by cumulative loss until a later listener boundary" if effective else "No positive dropped-PCM counter observed; ordinary callback/device uncertainty remains",
        mux_loss_gap_inserted=False,
        meaning="Actual WAV and raw counter samples remain authoritative. The existing VFR/WAV mux retains listener offsets and does not synthesize PCM or invent a precise timestamp gap for an uncertain loss location. A successful encode is not a claim of synchronized exposure inside or after a loss bracket.",
    )


def boundaries(segments):
    """Segments contain name and rows (actual timeline plus actual frame rows)."""
    # Empty live segments have no actual UTC at which to order a boundary. The
    # caller preserves them as incomplete segments instead of inventing a time.
    ordered = sorted((segment for segment in segments if segment["rows"]),
                     key=lambda segment: min(stamp(row) for row in segment["rows"]))
    result = []
    for earlier, later in zip(ordered, ordered[1:]):
        oldrows = sorted(earlier["rows"], key=stamp)
        newrows = sorted(later["rows"], key=stamp)
        terminal = next((row for row in reversed(oldrows) if initialized(row)), None)
        resumed = next((row for row in newrows if initialized(row)), None)
        stop = next((row for row in reversed(oldrows) if row.get("kind") == "recording.stop"), oldrows[-1])
        start = next((row for row in newrows if row.get("kind") == "recording.start"), newrows[0])
        gap = stamp(start)-stamp(stop)
        # A same-process recorder rotation can happen during an ordinary Rest
        # transition. Its actual stop/start values are better comparison points
        # than an earlier nontransitioning day1 sample and later day2 sample.
        # Main Menu Continue starts fail this criterion regardless of position.
        live_rotation = (stop.get("kind") == "recording.stop" and start.get("kind") == "recording.start"
                         and 0 <= gap <= 1 and stop.get("zone") == start.get("zone") == "Zone04"
                         and stop.get("zoneFourTelemetryObserved") is start.get("zoneFourTelemetryObserved") is True
                         and stop.get("scene") == start.get("scene")
                         and all(stop.get(key) == start.get(key) for key in STATE_FIELDS)
                         and isinstance(stop.get("realtime"), (int, float)) and isinstance(start.get("realtime"), (int, float))
                         and 0 <= start["realtime"]-stop["realtime"] <= 1)
        gap_disclosures = [row for row in newrows if row.get("kind") == "recording.gap.disclosure" and row.get("elapsedWall", 999) <= 5]
        same_runtime = (isinstance(stop.get("realtime"), (int, float)) and isinstance(start.get("realtime"), (int, float))
                        and start["realtime"] >= stop["realtime"]
                        and abs((start["realtime"]-stop["realtime"])-gap) <= 1)
        disclosed_live_gap = (stop.get("kind") == "recording.stop" and start.get("kind") == "recording.start"
                              and stop.get("zone") == start.get("zone") == "Zone04"
                              and stop.get("zoneFourTelemetryObserved") is start.get("zoneFourTelemetryObserved") is True
                              and same_runtime and any("continue live voyage" in row.get("detail", "").lower() for row in gap_disclosures))
        before, after = (stop, start) if live_rotation or disclosed_live_gap else (terminal, resumed)
        changes = {key: dict(before=before.get(key), after=after.get(key))
                   for key in STATE_FIELDS if before and after and before.get(key) != after.get(key)}
        def reduction(key):
            prior, later_value = (before.get(key), after.get(key)) if before and after else (None, None)
            return max(0, prior-later_value) if isinstance(prior, (int, float)) and isinstance(later_value, (int, float)) else None
        lost_receipts = {key: sorted(set(before.get(key, []))-set(after.get(key, [])))
                         for key in ("completedObjectives", "cueReceipts", "revealedLocationIds") if before and after}
        result.append(dict(
            before_segment=earlier["name"], after_segment=later["name"],
            previous_stop=point(stop, earlier["name"]), next_start=point(start, later["name"]),
            previous_termination=termination(earlier),
            marker_gap_seconds=gap,
            prior_last_initialized=point(terminal, earlier["name"]), resumed_first_initialized=point(resumed, later["name"]),
            comparison_before=point(before, earlier["name"]), comparison_after=point(after, later["name"]),
            live_recorder_rotation_observed=live_rotation,
            unrecorded_live_actions_disclosed=disclosed_live_gap,
            gap_disclosure_markers=[point(row, later["name"]) for row in gap_disclosures],
            comparison_basis="Actual stop/start state across a pilot-disclosed live recording gap; intervening event times unknown" if disclosed_live_gap else "Matching actual stop/start Zone04 state and continuing runtime clock; no reload observed" if live_rotation else "Last initialized prior state versus first initialized resumed state",
            initialized_observation_gap_seconds=stamp(resumed)-stamp(terminal) if terminal and resumed else None,
            actual_state_changes=changes,
            distance_counter_rollback_metres=reduction("distance"),
            photo_count_rollback=reduction("photosTaken"), day_counter_rollback=reduction("day"),
            receipts_absent_after_resume=lost_receipts,
            classification="Pilot-disclosed unrecorded live actions with continuing runtime; no checkpoint reload inferred" if disclosed_live_gap else "Live recorder rotation with matching stop/start state; no checkpoint rollback observed" if live_rotation else "Observed checkpoint/state discontinuity across recorder gap" if changes else "No compared state change observed across recorder gap" if terminal and resumed else "Awaiting initialized resumed observation",
            meaning="The pilot's actual gap-disclosure marker reports compass-photo attempts and Rest outside recording while the runtime continued. Stop/start telemetry confirms changed endpoints but supplies no intervening action timestamps or sensory media. Do not classify this as checkpoint reload/replay, synthesize gap media, or count its day difference as a raw-recorded Rest." if disclosed_live_gap else "Two actual recorder files rotate inside the same runtime clock with matching Zone04 state. The short marker gap remains unrecorded and is not filled. An ordinary Rest change already observed within the earlier segment is not a reload or an additional Rest at this boundary." if live_rotation else "Separate recordings with a real external gap. Before/after facts are actual passive telemetry only. No unrecorded action, seamless live continuity, Rest, resource refill action or photo submission is inferred. A lower photo count does not establish deletion of loose PNG files; it shows the resumed game did not retain that live count.",
        ))
    return result
