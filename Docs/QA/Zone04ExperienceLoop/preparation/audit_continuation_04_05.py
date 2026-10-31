"""Offline audit of the closed fourth/fifth final recorder segments.

Reads only actual recorder artifacts. No Unity, source geometry, save or actor
position access; no ending completion, termination cause or missing media is
inferred. Raw recorder artifacts remain unchanged.
"""
from __future__ import annotations

import hashlib
import json
import pathlib
import wave

import numpy as np

from final_resume_evidence import audio_counter_evidence, boundaries, point, termination

ROOT = pathlib.Path(__file__).resolve().parents[1]


def digest(path):
    value = hashlib.sha256()
    with path.open("rb") as stream:
        while block := stream.read(1024 * 1024):
            value.update(block)
    return dict(path=str(path.relative_to(ROOT)), bytes=path.stat().st_size,
                sha256=value.hexdigest())


def rows(path):
    with path.open(encoding="utf-8-sig") as stream:
        return [json.loads(line) for line in stream if line.strip()]


def audit(name):
    path = ROOT / "final" / name
    timeline, frames = rows(path / "timeline.jsonl"), rows(path / "frames.jsonl")
    merged = sorted(timeline + frames, key=lambda row: row["utc"])
    summary = dict(line.split("=", 1) for line in (path / "summary.txt").read_text().splitlines() if "=" in line)
    audio_path = path / "actual-game-audio.wav"
    with wave.open(str(audio_path), "rb") as stream:
        channels, sample_rate, width, frame_count = stream.getnchannels(), stream.getframerate(), stream.getsampwidth(), stream.getnframes()
        assert (channels, sample_rate, width) == (2, 48000, 2)
        square_sum = 0.
        rail_values = rail_frames = nonzero = 0
        peak = 0
        while block := stream.readframes(262144):
            values = np.frombuffer(block, dtype="<i2")
            square_sum += np.square(values.astype(np.float64) / 32768).sum()
            # Unity's float-to-PCM recorder maps negative full scale to -32767;
            # include it as the historical parser does, in addition to the
            # two's-complement minimum and positive maximum.
            rails = (values <= -32767) | (values == 32767)
            rail_values += int(rails.sum())
            rail_frames += int(rails.reshape(-1, channels).any(axis=1).sum())
            nonzero += int(np.count_nonzero(values))
            peak = max(peak, int(np.abs(values.astype(np.int32)).max()))
    rms = float(np.sqrt(square_sum / (frame_count * channels)))
    audio = dict(channels=channels, sample_rate=sample_rate, sample_width_bytes=width,
                 sample_frames=frame_count, sample_values=frame_count * channels,
                 duration_seconds=frame_count / sample_rate, nonzero_sample_values=nonzero,
                 peak_integer=peak, rms_normalized=rms,
                 rms_dbfs=float(20 * np.log10(rms)) if rms else None,
                 rail_sample_values=rail_values, sample_frames_with_any_rail=rail_frames,
                 rail_definition="Values at +32767, -32767 or -32768; includes Unity recorder's negative full-scale mapping, consistent with the historical evidence parser")
    last_frame = frames[-1]
    primary_fields = ("completedObjectives", "cueReceipts", "revealedLocationIds")
    firsts = {}
    for row in merged:
        for field in primary_fields:
            for value in row.get(field, []):
                firsts.setdefault((field, value), point(row, name))
    positive_audio = {}
    for field in ("fragmentAudioTime", "hiddenAAudioTime", "hiddenBAudioTime", "finalSignalAudioTime"):
        observed = [row for row in merged if row.get(field, -1) >= 0]
        positive_audio[field] = dict(observations=len(observed),
                                    first=point(observed[0], name) if observed else None,
                                    last=point(observed[-1], name) if observed else None,
                                    minimum=min((row[field] for row in observed), default=None),
                                    maximum=max((row[field] for row in observed), default=None))
    missing = [row["image"] for row in frames if not (path / row["image"]).is_file()]
    assert not missing, missing
    return dict(name=name, rows=merged), dict(
        name=name, raw_summary=summary,
        recorder_start=point(next(row for row in timeline if row["kind"] == "recording.start"), name),
        termination=termination(dict(name=name, rows=merged)),
        actual_frame_count=len(frames), all_logged_frame_files_present=True,
        last_actual_frame={**point(last_frame, name), "screenWidth": last_frame.get("screenWidth"), "screenHeight": last_frame.get("screenHeight"),
                           "focused": last_frame.get("focused"), "sha256": digest(path / last_frame["image"])["sha256"]},
        observed_hull_range=[min(row.get("hull", 100) for row in merged), max(row.get("hull", 0) for row in merged)],
        first_observations=[dict(field=field, value=value, observation=observation) for (field, value), observation in firsts.items()],
        actual_voice_clock_observations=positive_audio,
        committed_endings_observed=sorted({row.get("committedEndingReached") for row in merged if row.get("committedEndingReached")}),
        full_hidden_ending_observed=any(row.get("scene") == "Ending" and row.get("endingPresentationId") == row.get("committedEndingReached") == "hidden"
                                      and row.get("endingPresentationVisible") and row.get("endingPresentationElapsed", 0) >= 16 for row in merged),
        audio=audio, audio_counter_evidence=audio_counter_evidence(merged, summary, audio),
        raw_metadata_and_audio_manifest=[digest(path / filename) for filename in ("timeline.jsonl", "frames.jsonl", "summary.txt", "actual-game-audio.wav", "recording-notes.txt")],
        interpretation="Actual PCM rail values report digital full-scale samples, not a subjective loudness or audibility assessment. Recorder reason is preserved verbatim and does not establish why the application later stopped Play. No sensory media or gameplay result is invented in any gap.")


def main():
    arows, a = audit("continuation-04")
    brows, b = audit("continuation-05")
    result = dict(status="PARTIAL closed-segment audit only; final hidden Ending not observed; no final seal or mux",
                  segments=[a, b], boundary=boundaries([arows, brows])[0],
                  external_editor_state_report=dict(source="Sole pilot report to offline agent, 2026-10-31 client date", play_mode=False,
                                                   session_state_input_route="none", cause="UNKNOWN; no cause inferred from recorder reason or later Editor state"),
                  remaining="Ordinary Continue comparison against last initialized C5 state, then actual B PHOTO/SEND, actual unlocked final marker, physical arrival and matching hidden Ending>=16s; all real recorder closure, visual reviews, mux/quality manifests and post-run preservation checks remain required.")
    out = ROOT / "preparation/final-continuation-04-05-close-audit.json"
    out.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(dict(output=str(out), boundary_gap_seconds=result["boundary"]["marker_gap_seconds"],
                          live_rotation=result["boundary"]["live_recorder_rotation_observed"],
                          actual_frames=[item["actual_frame_count"] for item in result["segments"]],
                          audio=[item["audio"] for item in result["segments"]],
                          c5_last_frame=b["last_actual_frame"], c5_termination=b["termination"]), ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
