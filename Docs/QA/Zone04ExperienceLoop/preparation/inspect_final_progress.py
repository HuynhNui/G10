"""Read-only partial final recorder inspection; never opens WAVs or Unity.

Raw live files are only read. A trailing incomplete JSON line is ignored and
reported; it is never repaired. This command makes no sealed/completion claim.
"""
from __future__ import annotations

import collections
import json
import pathlib

from final_resume_evidence import boundaries, initialized, termination

ROOT = pathlib.Path(__file__).resolve().parents[1]
KEEP = ("kind", "utc", "elapsedWall", "scene", "zone", "panel", "app", "action", "day", "x", "y", "depth", "heading", "speed", "distance", "energy", "hull", "photosTaken", "capturesToday", "completedObjectives", "cueReceipts", "revealedLocationIds", "experienceStage", "pendingCue", "focused", "editorPaused", "timeScale", "transitioning", "committedEndingReached", "endingReceiptSource", "endingPresentationId", "endingPresentationVisible", "endingPresentationElapsed", "image")


def main():
    first_ready = last = first_committed = full = None
    firsts, segments, boundary_segments, review_candidates = {}, [], [], {}
    for timeline in sorted((ROOT / "final").rglob("timeline.jsonl")):
        path = timeline.parent
        kinds, frames, incomplete = collections.Counter(), 0, []
        first_row = last_row = first_initialized = last_initialized = None
        boundary_rows = []
        for source in (timeline, path / "frames.jsonl"):
            if not source.exists():
                incomplete.append(dict(path=str(source), meaning="Not yet present in the live segment; no evidence invented."))
                continue
            with source.open(encoding="utf-8-sig") as stream:
                for number, line in enumerate(stream, 1):
                    if not line.strip():
                        continue
                    try:
                        row = json.loads(line)
                    except json.JSONDecodeError:
                        incomplete.append(dict(path=str(source), line=number, meaning="Unreadable concurrent/trailing line; original remains unchanged."))
                        continue
                    point = {key: row[key] for key in KEEP if key in row}
                    point["segment"] = str(path.relative_to(ROOT / "final"))
                    if row.get("kind") in ("recording.start", "recording.stop", "recording.gap.disclosure"):
                        boundary_rows.append(row)
                    if initialized(row):
                        if first_initialized is None or row["utc"] < first_initialized["utc"]:
                            first_initialized = row
                        if last_initialized is None or row["utc"] > last_initialized["utc"]:
                            last_initialized = row
                    if source == timeline:
                        kinds[row.get("kind")] += 1
                    else:
                        frames += 1
                        for field in ("fragmentVisual", "hiddenAPhotoAudience", "hiddenBPhotoAudience", "finalWaterTintVisible", "endingPresentationVisible"):
                            if row.get(field) is True:
                                item = review_candidates.setdefault(field, dict(first=point, last=point, actual_frame_count=0))
                                if point["utc"] < item["first"]["utc"]:
                                    item["first"] = point
                                if point["utc"] > item["last"]["utc"]:
                                    item["last"] = point
                                item["actual_frame_count"] += 1
                    if first_row is None or row["utc"] < first_row["utc"]:
                        first_row = point
                    if last_row is None or row["utc"] > last_row["utc"]:
                        last_row = point
                    if last is None or row["utc"] > last["utc"]:
                        last = point
                    if row.get("zone") == "Zone04" and row.get("zoneFourTelemetryObserved") is True and row.get("transitioning") is False and row.get("energy", 0) > 0 and row.get("hull", 0) > 0 and row.get("depth", 0) > 0:
                        if first_ready is None or row["utc"] < first_ready["utc"]:
                            first_ready = point
                    if row.get("committedEndingReached") in ("hidden", "normal") and (first_committed is None or row["utc"] < first_committed["utc"]):
                        first_committed = point
                    if row.get("scene") == "Ending" and row.get("transitioning") is False and row.get("endingPresentationVisible") and row.get("endingPresentationId") == row.get("committedEndingReached") == "hidden" and row.get("endingPresentationElapsed", 0) >= 16:
                        if full is None or row["utc"] < full["utc"]:
                            full = point
                    if row.get("zone") == "Zone04":
                        for field in ("completedObjectives", "cueReceipts", "revealedLocationIds"):
                            for value in row.get(field, []):
                                key = (field, value)
                                if key not in firsts or row["utc"] < firsts[key]["observation"]["utc"]:
                                    firsts[key] = dict(field=field, value=value, observation=point)
        boundary_segment = dict(name=str(path.relative_to(ROOT / "final")), rows=boundary_rows + [row for row in (first_initialized, last_initialized) if row])
        segments.append(dict(path=str(path), closed_observed=bool(kinds["recording.stop"] == 1 and (path / "summary.txt").exists()), kinds=kinds, actual_frames_observed=frames, first=first_row, last=last_row, termination=termination(boundary_segment), unreadable_lines=incomplete))
        boundary_segments.append(boundary_segment)
    segments.sort(key=lambda segment: segment["first"]["utc"] if segment["first"] else "\uffff")
    print(json.dumps(dict(status="PARTIAL OFFLINE OBSERVATION ONLY; no sealed completed-voyage claim", segments=segments, recorder_resume_boundaries=boundaries(boundary_segments), first_ready=first_ready, first_committed_ending=first_committed, full_hidden_ending_observation=full, last=last, first_observations=sorted(firsts.values(), key=lambda item: item["observation"]["utc"]), actual_presentation_review_candidates=review_candidates), ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
