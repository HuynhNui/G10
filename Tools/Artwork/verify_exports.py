"""Verify saved PNG/provenance receipts without changing any artwork or Unity data."""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]


def sha256(data):
    return hashlib.sha256(data).hexdigest()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--manifest", type=Path, default=ROOT / "Docs/QA/CreatureArtworkIntegration/artwork-manifest.json")
    parser.add_argument("--sources", type=Path, default=ROOT / ".tools/creature-artwork-sources/sources.json")
    parser.add_argument("--output", type=Path, default=ROOT / "Docs/QA/CreatureArtworkIntegration/export-verification.json")
    args = parser.parse_args()
    manifest = json.loads(args.manifest.read_text(encoding="utf-8"))
    sources = json.loads(args.sources.read_text(encoding="utf-8-sig"))
    results = []
    for entry in manifest["creatures"] + manifest["reserves"]:
        path = ROOT / entry["output_path"]
        with Image.open(path) as png:
            image = png.convert("RGBA")
        content = image.crop(entry["output_content_bounds"])
        alpha = image.getchannel("A")
        x0, y0, x1, y1 = entry["output_content_bounds"]
        padding_clear = all(region.getbbox() is None for region in (
            alpha.crop((0, 0, image.width, y0)), alpha.crop((0, y1, image.width, image.height)),
            alpha.crop((0, y0, x0, y1)), alpha.crop((x1, y0, image.width, y1))))
        results.append({
            "id": entry.get("creature_id", entry.get("reserve_id")),
            "png_sha_matches": sha256(path.read_bytes()) == entry["output_sha256"],
            "dimensions_match": list(image.size) == entry["output_size"],
            "rgba_content_matches": sha256(content.tobytes()) == entry["native_crop_rgba_sha256"] == entry["output_content_rgba_sha256"],
            "padding_fully_transparent": padding_clear,
            "excluded_nonzero_alpha_pixels": entry["excluded_nonzero_alpha_pixels"]
        })
    source_checks = [{"id": source["id"], "sha_matches": sha256(Path(source["path"]).read_bytes()) == source["sha256"]}
                     for source in sources]
    ids = [r["id"] for r in results]
    report = {
        "creature_count": len(manifest["creatures"]), "reserve_count": len(manifest["reserves"]),
        "unique_ids": len(set(ids)),
        "unique_png_sha256": len({entry["output_sha256"] for entry in manifest["creatures"] + manifest["reserves"]}),
        "source_checks": source_checks, "output_checks": results,
        "script_sha_matches": sha256((ROOT / manifest["export_script"]).read_bytes()) == manifest["export_script_sha256"],
        "mapping_sha_matches": sha256((ROOT / manifest["mapping_path"]).read_bytes()) == manifest["mapping_sha256"]
    }
    checks = [r["png_sha_matches"] and r["dimensions_match"] and r["rgba_content_matches"] and r["padding_fully_transparent"] for r in results]
    report["all_passed"] = (len(manifest["creatures"]) == 11 and len(manifest["reserves"]) == 3 and len(set(ids)) == 14
                            and all(checks) and all(r["sha_matches"] for r in source_checks)
                            and report["script_sha_matches"] and report["mapping_sha_matches"])
    args.output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report, ensure_ascii=False, indent=2))
    if not report["all_passed"]:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
