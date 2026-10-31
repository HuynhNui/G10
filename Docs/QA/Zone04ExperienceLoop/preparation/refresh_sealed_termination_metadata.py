"""Refresh only derived recorder-termination metadata after a strict seal.

The completed Ending must be distinguished from an unknown teardown when its
Zone04 controller is absent. Reuses verified sealed audio/video statistics,
hashes and muxes; never opens WAV as audio or encodes again. Archives the input
seal, verifies its artifact hashes, reads only timeline/frame metadata, updates
termination fields and their derived index hashes, then renders the report.
"""
from __future__ import annotations

import hashlib
import json
import pathlib
import shutil

import build_final_report as builder
import final_resume_evidence as resume

ROOT = pathlib.Path(__file__).resolve().parents[1]


def main():
    target = ROOT / "05_Final_Timeline.json"
    d = builder.read(target)
    assert d["status"] == "SEALED actual full hidden ending"
    assert len(d["segments"]) == 7 and all(part["closed"] for part in d["segments"])
    assert all(part["mux"]["status"] == "success" for part in d["segment_quality_and_control"])
    assert d["clocks"]["full_ending"]["endingPresentationElapsed"] >= 16
    # Hash existing sealed artifacts; do not repeat PCM analysis, ffprobe or mux.
    for expected in d["raw_artifact_manifest"]:
        actual = builder.digest(ROOT / expected["path"])
        assert actual == expected, (actual, expected)
    for entry in builder.read(ROOT / "candidate-freeze.json")["files"]:
        assert builder.digest(builder.REPO / entry["path"])["sha256"] == entry["sha256"].lower()
    archived = ROOT / "preserved-local/first-strict-seal-before-termination-refresh.json"
    assert not archived.exists(), "Refusing to overwrite a prior archived seal."
    archived.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(target, archived)
    archive_digest = builder.digest(archived)
    terms, updated = {}, []
    for segment in d["segments"]:
        path = pathlib.Path(segment["path"])
        name = str(path.relative_to(ROOT / "final"))
        rows = [json.loads(line) for file in (path / "timeline.jsonl", path / "frames.jsonl")
                for line in file.read_text(encoding="utf-8-sig").splitlines() if line.strip()]
        term = resume.termination(dict(name=name, rows=rows))
        terms[name] = term
        segment["analysis"]["recorder_termination"] = term
        builder.dump(path / "event-index.json", segment["analysis"])
        updated.append(str((path / "event-index.json").relative_to(ROOT)))
    for segment in d["segment_quality_and_control"]:
        segment["termination"] = terms[segment["name"]]
    for boundary in d["recorder_resume_boundaries"]:
        boundary["previous_termination"] = terms[boundary["before_segment"]]
    for expected in d["raw_artifact_manifest"]:
        if expected["path"] in updated:
            expected.update(builder.digest(ROOT / expected["path"]))
    provenance = d["analysis_provenance"]
    provenance["metadata_termination_refresh"] = dict(
        input_strict_seal_archive=archive_digest,
        refresh_source=builder.digest(pathlib.Path(__file__).resolve()),
        current_termination_helper=builder.digest(pathlib.Path(resume.__file__).resolve()),
        scope="Recompute recorder termination only from actual closed metadata after the helper gained explicit recognition of the known completed matching Ending. All original raw artifacts verified before refresh; no mission/cue/clock/interval, PCM statistic, source playback, visual review or mux result changed. Audio/video are reused, not analyzed/encoded again.",
        changed_derived_indexes=updated)
    for source in provenance["sources"]:
        if source["path"].replace("\\", "/") == "preparation/final_resume_evidence.py":
            source.update(builder.digest(pathlib.Path(resume.__file__).resolve()))
    builder.dump(ROOT / "final/offline-metrics.json", d)
    builder.dump(target, d)
    builder.write_report(d)
    print(json.dumps(dict(status="SEALED; termination-only metadata refresh complete", archived_input=archive_digest,
                          completed_ending_at_stop=terms["continuation-06"]["completed_matching_ending_observed_at_stop"],
                          reused_successful_muxes=7, pcm_reanalyses=0, reencodes=0), ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
