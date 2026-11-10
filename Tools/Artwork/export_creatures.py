"""Reproduce approved native PSD creature exports, with no repainting or keying.

Usage from repository root:
  .tools/creature-artwork-venv/Scripts/python.exe -X utf8 Tools/Artwork/export_creatures.py
Source PSD files stay outside Git under .tools; SHA-256 is verified before reading.
The zero-based group paths are explicit mapping decisions in creature_mapping.json.
"""
from __future__ import annotations

import argparse
import hashlib
import importlib.metadata
import io
import json
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont
from psd_tools import PSDImage


ROOT = Path(__file__).resolve().parents[2]


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def relative(path: Path) -> str:
    try:
        return path.resolve().relative_to(ROOT).as_posix()
    except ValueError:
        return str(path.resolve())


def layer_at(psd, index_path):
    layer = psd
    names = []
    for index in index_path:
        layer = layer[index]
        names.append(layer.name)
    if not layer.is_group():
        raise ValueError(f"Mapped layer is not a group: {names}")
    return layer, names


def encode_png(image: Image.Image) -> bytes:
    buffer = io.BytesIO()
    image.save(buffer, format="PNG", compress_level=9, optimize=False)
    return buffer.getvalue()


def output_file(path: Path, payload: bytes):
    path.parent.mkdir(parents=True, exist_ok=True)
    # Never touch Unity .meta files. Identical reruns leave asset bytes untouched.
    if not path.exists() or path.read_bytes() != payload:
        path.write_bytes(payload)


def font(size=19):
    try:
        return ImageFont.truetype("C:/Windows/Fonts/arial.ttf", size)
    except OSError:
        return ImageFont.load_default()


def contact_sheet(entries, destination, title, columns=4):
    width, height, footer = 340, 350, 76
    rows = (len(entries) + columns - 1) // columns
    sheet = Image.new("RGB", (columns * width, rows * (height + footer) + 58), "#17252d")
    draw = ImageDraw.Draw(sheet)
    draw.text((18, 14), title, fill="#f1e8ce", font=font(23))
    for index, entry in enumerate(entries):
        x = (index % columns) * width
        y = 58 + (index // columns) * (height + footer)
        draw.rectangle((x + 6, y + 3, x + width - 6, y + height), fill="#253c48")
        with Image.open(ROOT / entry["output_path"]) as original:
            image = original.convert("RGBA")
            image.thumbnail((width - 28, height - 18), Image.Resampling.LANCZOS)
        sheet.paste(image, (x + (width - image.width) // 2, y + (height - image.height) // 2), image)
        name = entry.get("creature_id", entry.get("reserve_id"))
        description = entry.get("zone", "Reserve")
        if "location_id" in entry:
            description += f" / {entry['location_id']}"
        draw.text((x + 12, y + height + 7), name, fill="white", font=font(20))
        draw.text((x + 12, y + height + 32), description, fill="#d1cbb7", font=font(17))
        draw.text((x + 12, y + height + 53), f"Mapping: {entry['confidence']} confidence", fill="#b7c8d0", font=font(14))
    output_file(destination, encode_png(sheet))


def export_one(mapping, source, psd, destination, padding):
    group, name_path = layer_at(psd, mapping["index_path"])
    board = psd[mapping["index_path"][0]]
    if board.kind != "artboard":
        raise ValueError(f"Group is not under an artboard: {name_path}")
    viewport = tuple(board.bbox)
    # Native artboard clipping is authoritative. Preserve each visible child,
    # blend mode, opacity and mask; isolate the explicitly mapped group only.
    native = group.composite(viewport=viewport, layer_filter=lambda layer: layer.visible)
    if native is None:
        raise ValueError(f"Group has no rendered pixels: {name_path}")
    native = native.convert("RGBA")
    original_bbox = native.getchannel("A").getbbox()
    reviewed_crop = mapping.get("reviewed_crop_source")
    crop_origin = (0, 0)
    selected = native
    if reviewed_crop is not None:
        if not mapping.get("crop_basis") or len(reviewed_crop) != 4:
            raise ValueError("An explicit reviewed crop requires its documented basis")
        if not (viewport[0] <= reviewed_crop[0] < reviewed_crop[2] <= viewport[2] and
                viewport[1] <= reviewed_crop[1] < reviewed_crop[3] <= viewport[3]):
            raise ValueError("Reviewed source crop must stay inside the authored artboard")
        local_crop = (reviewed_crop[0] - viewport[0], reviewed_crop[1] - viewport[1],
                      reviewed_crop[2] - viewport[0], reviewed_crop[3] - viewport[1])
        selected = native.crop(local_crop)
        crop_origin = local_crop[:2]
    bbox = selected.getchannel("A").getbbox()  # Any nonzero alpha, including faint brush marks.
    if bbox is None:
        raise ValueError(f"Mapped group is fully transparent: {name_path}")
    cropped = selected.crop(bbox)
    bbox = (bbox[0] + crop_origin[0], bbox[1] + crop_origin[1],
            bbox[2] + crop_origin[0], bbox[3] + crop_origin[1])
    output = Image.new("RGBA", (cropped.width + 2 * padding, cropped.height + 2 * padding), (0, 0, 0, 0))
    # No mask argument: paste exact native RGBA, without a second alpha multiply.
    output.paste(cropped, (padding, padding))
    # Assert the content pixels are exactly the native group pixels after cropping.
    if output.crop((padding, padding, padding + cropped.width, padding + cropped.height)).tobytes() != cropped.tobytes():
        raise AssertionError("Output pixels differ from native group crop")
    payload = encode_png(output)
    output_file(destination, payload)
    bx, by = viewport[:2]
    record = dict(mapping)
    record.update({
        "source_name": source["name"], "source_sha256": source["sha256"],
        "source_bytes": source["bytes"], "source_path": relative(Path(source["path"])),
        "source_document_size": [psd.width, psd.height],
        "artboard_name": board.name, "artboard_bounds_source": list(viewport),
        "group_name_path": name_path, "group_original_bounds_source": list(group.bbox),
        "rendered_viewport_source": list(viewport),
        "untrimmed_alpha_bounds_artboard": list(original_bbox),
        "reviewed_crop_source": reviewed_crop,
        "excluded_nonzero_alpha_pixels": sum(native.getchannel("A").histogram()[1:]) - sum(selected.getchannel("A").histogram()[1:]),
        "nonzero_alpha_bounds_artboard": list(bbox),
        "nonzero_alpha_bounds_source": [bbox[0] + bx, bbox[1] + by, bbox[2] + bx, bbox[3] + by],
        "transparent_padding_pixels": padding,
        "output_content_bounds": [padding, padding, padding + cropped.width, padding + cropped.height],
        "output_path": relative(destination), "output_size": list(output.size),
        "output_aspect_ratio": output.width / output.height,
        "output_sha256": sha256(payload), "output_bytes": len(payload),
        "native_crop_rgba_sha256": sha256(cropped.tobytes()),
        "output_content_rgba_sha256": sha256(output.crop((padding, padding, padding + cropped.width, padding + cropped.height)).tobytes()),
        "pixel_preservation_verified": True,
        "edge_handling": f"Apply only an explicitly documented reviewed source crop if present; then remove alpha==0 outer bounds and retain every nonzero-alpha native pixel inside that region. {padding}px transparent padding added. No resize, color keying, repainting, alpha threshold, film frame or text.",
        "artboard_clips_group_bounds": not (group.bbox[0] >= viewport[0] and group.bbox[1] >= viewport[1] and group.bbox[2] <= viewport[2] and group.bbox[3] <= viewport[3])
    })
    print(json.dumps({"exported": mapping.get("creature_id", mapping.get("reserve_id")),
                      "path": str(destination), "source_alpha_bbox": record["nonzero_alpha_bounds_source"],
                      "size": record["output_size"], "aspect": record["output_aspect_ratio"]}, ensure_ascii=False), flush=True)
    return record


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--sources", type=Path, default=ROOT / ".tools/creature-artwork-sources/sources.json")
    parser.add_argument("--mapping", type=Path, default=ROOT / "Tools/Artwork/creature_mapping.json")
    parser.add_argument("--output", type=Path, default=ROOT / "Assets/_Project/Art/Creatures/Finished")
    parser.add_argument("--report", type=Path, default=ROOT / "Docs/QA/CreatureArtworkIntegration")
    parser.add_argument("--only", default="", help="Export one ID and a separate prototype receipt; do not replace the final manifest")
    args = parser.parse_args()
    sources = json.loads(args.sources.read_text(encoding="utf-8-sig"))
    mapping = json.loads(args.mapping.read_text(encoding="utf-8"))
    source_by_id = {source["id"]: source for source in sources}
    cache = {}
    records = {"creatures": [], "reserves": []}
    for category in records:
        for item in mapping[category]:
            name = item.get("creature_id", item.get("reserve_id"))
            if args.only and name != args.only:
                continue
            source = source_by_id[item["source_id"]]
            if source["id"] not in cache:
                payload = Path(source["path"]).read_bytes()
                if payload[:4] != b"8BPS" or len(payload) != source["bytes"] or sha256(payload) != source["sha256"]:
                    raise ValueError(f"Source signature/size/checksum mismatch: {source['name']}")
                cache[source["id"]] = PSDImage.open(io.BytesIO(payload))
            destination = args.output / ("Reserve" if category == "reserves" else "") / f"{name}.png"
            records[category].append(export_one(item, source, cache[source["id"]], destination, mapping["padding_pixels"]))
    if args.only and not any(records.values()):
        raise ValueError(f"Unknown requested export ID: {args.only}")
    args.report.mkdir(parents=True, exist_ok=True)
    manifest = {
        "schema_version": 1,
        "mapping_authority": mapping["mapping_authority"],
        "tools": {name: importlib.metadata.version(name) for name in ("psd-tools", "Pillow", "numpy")},
        "mapping_path": relative(args.mapping), "mapping_sha256": sha256(args.mapping.read_bytes()),
        "export_script": relative(Path(__file__)),
        "export_script_sha256": sha256(Path(__file__).read_bytes()),
        "source_manifest": relative(args.sources),
        "source_manifest_sha256": sha256(args.sources.read_bytes()),
        "comparison_only_source_ids": mapping["comparison_only_source_ids"],
        "sources": [{k: s[k] for k in ("id", "name", "bytes", "sha256")} for s in sources],
        "export_policy": "Native named PSD groups; native artboard clipping and original visible-child blend/mask/opacity retained. Explicit reviewed source crop only when documented per entry; otherwise the full artboard. Then crop fully transparent outer pixels and add 24px transparent padding. No resizing, painting, AI extraction, chroma keying or alpha threshold.",
        **records
    }
    name = f"export-result-{args.only}.json" if args.only else "artwork-manifest.json"
    output_file(args.report / name, (json.dumps(manifest, ensure_ascii=False, indent=2) + "\n").encode("utf-8"))
    if not args.only:
        ordered = sorted(records["creatures"], key=lambda r: r["creature_id"])
        contact_sheet(ordered, args.report / "creature-contact-sheet.png", "G10 / native PSD creature artwork / selected mapping")
        contact_sheet(records["reserves"], args.report / "reserve-contact-sheet.png", "G10 / reserve artwork / no creature catalog IDs", columns=3)
    print(f"Manifest: {args.report / name}", flush=True)


if __name__ == "__main__":
    main()
