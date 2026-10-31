"""Measure same-recorder intervals without a new documented milestone.

This is a timing proxy, not proof of silence, inactivity, boredom or fear.
Never spans an external gap or counts loaded checkpoint receipts twice.
"""
from __future__ import annotations

import json
import pathlib

from final_resume_evidence import initialized, stamp

ROOT = pathlib.Path(__file__).resolve().parents[1]


def inspect(phase):
    segments = []
    for p in (ROOT / phase).rglob("timeline.jsonl"):
        rows = [json.loads(line) for file in (p, p.parent / "frames.jsonl") for line in file.read_text(encoding="utf-8-sig").splitlines() if line.strip()]
        rows.sort(key=lambda row: row["utc"])
        assert len([row for row in rows if row.get("kind") == "recording.stop"]) == 1
        segments.append(dict(name=str(p.parent.relative_to(ROOT / phase)), rows=rows))
    segments.sort(key=lambda part: part["rows"][0]["utc"])
    allrows = sorted([dict(row, segment=part["name"]) for part in segments for row in part["rows"]], key=lambda row: row["utc"])
    ready = next(row for row in allrows if initialized(row))
    full = next((row for row in allrows if row.get("scene") == "Ending" and row.get("committedEndingReached") == row.get("endingPresentationId") == "hidden"
                 and row.get("endingPresentationVisible") and row.get("transitioning") is False and row.get("endingPresentationElapsed", 0) >= 16), None)
    terminal = full or allrows[-1]
    milestones, seen = [], set()
    for row in allrows:
        if not stamp(ready) <= stamp(row) <= stamp(terminal):
            continue
        values = []
        if row.get("zone") == "Zone04" and row.get("zoneFourTelemetryObserved") is True:
            for field in ("completedObjectives", "cueReceipts", "revealedLocationIds"):
                values.extend((field, value) for value in row.get(field, []))
            if row.get("endingChoice"):
                values.append(("endingChoice", row["endingChoice"]))
        if row.get("committedEndingReached"):
            values.append(("committedEndingReached", row["committedEndingReached"]))
        for value in values:
            if value not in seen:
                milestones.append(dict(utc=row["utc"], segment=row["segment"], field=value[0], value=value[1]))
                seen.add(value)
    intervals = []
    for part in segments:
        first = next(row for row in part["rows"] if row.get("kind") == "recording.start")
        last = next(row for row in reversed(part["rows"]) if row.get("kind") == "recording.stop")
        begin = ready if stamp(ready) >= stamp(first) else first
        end = terminal if stamp(terminal) <= stamp(last) else last
        if stamp(end) <= stamp(begin):
            continue
        points = [dict(utc=begin["utc"], segment=part["name"], field="boundary", value="first-ready or recorder start")]
        points += [point for point in milestones if point["segment"] == part["name"] and stamp(begin) <= stamp(point) <= stamp(end)]
        points += [dict(utc=end["utc"], segment=part["name"], field="boundary", value="full Ending, censored terminal or recorder stop")]
        points.sort(key=stamp)
        for before, after in zip(points, points[1:]):
            intervals.append(dict(segment=part["name"], seconds=stamp(after)-stamp(before), before=before, after=after))
    return dict(phase=phase, first_ready_utc=ready["utc"], terminal_utc=terminal["utc"], full_ending_observed=full is not None,
                definition="Longest interval within a single actual recorder, bounded by first initialized state, recorder boundary, full-ending/censored terminal, or a first observed mission objective, Radar reveal, primary/opportunity receipt, actual choice or committed Ending. Rest, failed/unsent photographs, navigation and ordinary audiovisual ambience can continue inside an interval. Loaded duplicate receipts never count as new beats; no interval spans a gap.",
                not_measured="Literal uneventfulness, audible silence, confusion, boredom, human waiting or fear. This coarse milestone proxy can be dominated by tool/operator processing and unknown no-input time; it is not evidence for the Pacing rating by itself.",
                longest_same_recorder_without_new_milestone=max(intervals, key=lambda item: item["seconds"]),
                milestones=milestones, all_intervals=intervals)


def main():
    result = dict(schema="zone04-milestone-gap-proxy-v1", baseline=inspect("baseline"), final=inspect("final"))
    out = ROOT / "preparation/milestone-gap-comparison.json"
    out.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({phase: result[phase]["longest_same_recorder_without_new_milestone"] for phase in ("baseline", "final")}, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
