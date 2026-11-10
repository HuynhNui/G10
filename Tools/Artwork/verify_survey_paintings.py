"""Independently validate full painting PNGs against fresh source PSD decoding."""
from __future__ import annotations

import argparse
import io
import json
from pathlib import Path

from PIL import Image
from psd_tools import PSDImage

from export_creatures import ROOT, output_file, relative, sha256


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--sources", type=Path, default=ROOT / ".tools/creature-artwork-sources/sources.json")
    parser.add_argument("--manifest", type=Path, default=ROOT / "Docs/QA/CreatureArtworkIntegration/FullPaintingsCorrection/full-paintings-manifest.json")
    args = parser.parse_args()
    manifest = json.loads(args.manifest.read_text(encoding="utf-8"))
    sources = {source["id"]: source for source in json.loads(args.sources.read_text(encoding="utf-8-sig"))}
    assert sha256((ROOT / manifest["mapping_path"]).read_bytes()) == manifest["mapping_sha256"]
    assert sha256((ROOT / manifest["export_script"]).read_bytes()) == manifest["export_script_sha256"]
    cache, results = {}, []
    for entry in manifest["paintings"]:
        source = sources[entry["source_id"]]
        if source["id"] not in cache:
            data = Path(source["path"]).read_bytes()
            assert data[:4] == b"8BPS" and len(data) == source["bytes"]
            assert sha256(data) == source["sha256"] == entry["source_sha256"]
            psd = PSDImage.open(io.BytesIO(data))
            native = psd.topil(apply_icc=True).convert("RGBA")
            cache[source["id"]] = (psd, native)
        psd, native = cache[source["id"]]
        board = psd[entry["artboard_index"]]
        assert board.kind == "artboard" and board.is_visible()
        assert list(board.bbox) == entry["artboard_bounds_source"]
        assert list(native.size) == entry["source_document_size"]
        assert sha256(native.tobytes()) == entry["saved_composite_rgba_sha256"]
        expected = native.crop(board.bbox)
        payload = (ROOT / entry["output_path"]).read_bytes()
        assert sha256(payload) == entry["output_sha256"]
        exported = Image.open(io.BytesIO(payload)).convert("RGBA")
        assert exported.size == expected.size
        assert exported.tobytes() == expected.tobytes()
        assert sha256(expected.tobytes()) == entry["source_composite_crop_rgba_sha256"] == entry["output_rgba_sha256"]
        results.append({"creature_id": entry["creature_id"], "passed": True,
                        "size": list(exported.size), "full_artboard_crop": list(board.bbox),
                        "alpha_range": list(exported.getchannel("A").getextrema()),
                        "fresh_psd_composite_equals_export": True})
    assert len(results) == 11 and len({entry["creature_id"] for entry in results}) == 11
    # Ensure the correction does not replace or alter the separate capture art.
    cutouts = json.loads((ROOT / "Docs/QA/CreatureArtworkIntegration/artwork-manifest.json").read_text(encoding="utf-8"))
    capture_checks = []
    for entry in cutouts["creatures"] + cutouts["reserves"]:
        assert sha256((ROOT / entry["output_path"]).read_bytes()) == entry["output_sha256"]
        capture_checks.append(entry.get("creature_id", entry.get("reserve_id")))
    report = {"passed": True, "manifest": relative(args.manifest), "manifest_sha256": sha256(args.manifest.read_bytes()),
              "fresh_psd_decode_count": len(cache), "painting_count": len(results), "paintings": results,
              "existing_capture_cutouts_and_reserves_unchanged": capture_checks,
              "scope": "File export pixel/provenance validation; runtime PHOTO/Lab/SEND tests are separate."}
    destination = args.manifest.parent / "full-paintings-export-verification.json"
    output_file(destination, (json.dumps(report, ensure_ascii=False, indent=2) + "\n").encode("utf-8"))
    print(json.dumps({"passed": True, "full_paintings": len(results), "existing_cutouts_and_reserves_unchanged": len(capture_checks), "report": str(destination)}), flush=True)


if __name__ == "__main__":
    main()
