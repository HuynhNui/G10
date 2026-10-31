"""Partial/live C6 metadata audit; never reads an open WAV or contacts Unity."""
from __future__ import annotations

import json
import pathlib

from final_resume_evidence import boundaries, point, stamp

ROOT = pathlib.Path(__file__).resolve().parents[1]
FIELDS = ("utc", "elapsedWall", "kind", "image", "selectedPhotoPoi", "hiddenBAudioTime", "finalSignalAudioTime", "environmentLevel", "pendingCue", "experienceStage", "hiddenBPhotoAudience", "finalWaterTintVisible", "finalWaterTintAlpha", "cueReceipts", "completedObjectives", "committedEndingReached", "hiddenRouteComplete", "endingPresentationElapsed", "endingPresentationVisible", "endingPresentationId")


def read(path):
    result, incomplete = [], []
    for i, line in enumerate(path.read_text(encoding="utf-8-sig").splitlines(), 1):
        if not line.strip():
            continue
        try:
            result.append(json.loads(line))
        except json.JSONDecodeError:
            incomplete.append(i)
    return result, incomplete


def select(row):
    return {key: row[key] for key in FIELDS if key in row} if row else None


def main():
    parts = []
    for name in ("continuation-05", "continuation-06"):
        path = ROOT / "final" / name
        ts, ti = read(path / "timeline.jsonl")
        fs, fi = read(path / "frames.jsonl")
        parts.append(dict(name=name, rows=sorted(ts + fs, key=lambda x: x["utc"]), timeline=ts, frames=fs,
                          unreadable_lines=dict(timeline=ti, frames=fi)))
    old, current = parts
    rows, frames = current["rows"], current["frames"]
    def first(predicate):
        return next((row for row in rows if predicate(row)), None)
    highlights = dict(
        first_b_receipt=select(first(lambda row: "hidden.b" in row.get("cueReceipts", []))),
        first_b_send_objective=select(first(lambda row: "Z4_L4_PHOTO" in row.get("completedObjectives", []))),
        first_final_opportunity=select(first(lambda row: "final.signal.opportunity" in row.get("cueReceipts", []))),
        first_final_signal_receipt=select(first(lambda row: "final.signal" in row.get("cueReceipts", []))),
        first_committed_ending=select(first(lambda row: bool(row.get("committedEndingReached")))),
        first_full_hidden_ending=select(first(lambda row: row.get("committedEndingReached") == row.get("endingPresentationId") == "hidden"
                                             and row.get("endingPresentationVisible") and row.get("endingPresentationElapsed", 0) >= 16)))
    sources = {}
    for field in ("hiddenBAudioTime", "finalSignalAudioTime"):
        observations = [row for row in rows if row.get("zone") == "Zone04" and row.get("zoneFourTelemetryObserved") is True and row.get(field, -1) >= 0]
        image_observations = [row for row in frames if row.get("zone") == "Zone04" and row.get("zoneFourTelemetryObserved") is True and row.get(field, -1) >= 0]
        sources[field] = dict(first=select(observations[0]) if observations else None, last=select(observations[-1]) if observations else None,
                              minimum=min((row[field] for row in observations), default=None), maximum=max((row[field] for row in observations), default=None),
                              first_actual_image=select(image_observations[0]) if image_observations else None,
                              last_actual_image=select(image_observations[-1]) if image_observations else None,
                              exact_human_listening_duration=None)
    # The cumulative writer counter and current-listener production counter have
    # different lifetimes. Record listener switches instead of calling a normal
    # per-listener reset a missing-PCM event.
    listeners = {}
    for row in rows:
        clock = row.get("firstAudioCallbackStopwatchSeconds", 0)
        if not clock:
            continue
        group = listeners.setdefault(clock, [])
        group.append(row)
    listener_groups = []
    for clock, values in sorted(listeners.items()):
        keep = ("utc", "elapsedWall", "audioFramesWritten", "audioSampleFrames", "droppedAudioSamples", "audioSampleRate", "audioChannels")
        listener_groups.append(dict(first_audio_callback_stopwatch_seconds=clock,
                                    first={key: values[0].get(key) for key in keep},
                                    last={key: values[-1].get(key) for key in keep},
                                    maximum_observed_drop_sample_values=max(row.get("droppedAudioSamples", 0) for row in values),
                                    inferred_prior_pcm_frame_boundary=max(0, max(row.get("audioFramesWritten", 0)-row.get("audioSampleFrames", 0) for row in values))))
    result = dict(status="PARTIAL metadata audit; final voyage recorder may be live; no seal or mux",
                  boundary=boundaries(parts)[0], observed_highlights=highlights, actual_voice_clock_observations=sources,
                  actual_listener_groups=listener_groups, trailing_unreadable_lines=current["unreadable_lines"],
                  cue_clock_scope="Actual observed Zone04 only; shared final-signal clip plays again in Ending, which is not a second approach cue.",
                  last_observation=point(rows[-1], "continuation-06"),
                  audio_caveat="WAV has not been opened while recording. audioFramesWritten is cumulative while audioSampleFrames is scoped to the current listener; a reset on replacement is not itself dropped PCM. Existing muxer uses first callback clocks and inferred cumulative-written minus current-listener-produced boundaries. Actual closed-WAV/counter validation remains required.",
                  interpretation="The C5 stop and first initialized ordinary Continue state are compared directly. Same resources/receipts do not establish seamless runtime or fill the external media gap. B receipt is a presentation checkpoint; only actual SEND earns the B objective. At B the environment was already0.025 from the existing near-contact quietness, so do not claim this candidate cue newly reduced it to0.12 or restored it to1.")
    out = ROOT / "preparation/final-continuation-06-presentation-audit.json"
    out.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(dict(output=str(out), marker_gap_seconds=result["boundary"]["marker_gap_seconds"],
                          actual_state_changes=result["boundary"]["actual_state_changes"], highlights=highlights,
                          voice_clock_observations=sources, listener_groups=listener_groups), ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
