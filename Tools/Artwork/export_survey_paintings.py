"""Export complete authored survey paintings from the PSD's saved merged image.

The saved Photoshop composite, not a creature group recomposite, is authoritative.
Only the original artboard rectangle is cropped; no artwork pixels are trimmed.
Run with .tools/creature-artwork-venv/Scripts/python.exe -X utf8 this_file.py.
"""
from __future__ import annotations

import argparse
import importlib.metadata
import io
import json
from pathlib import Path

from PIL import Image, ImageDraw
from psd_tools import PSDImage

from export_creatures import ROOT, encode_png, font, output_file, relative, sha256


def layer_tree(group, path=(), names=()):
    result = []
    for index, layer in enumerate(group):
        own = path + (index,)
        record = {"index_path": list(own), "name_path": list(names + (layer.name,)),
                  "name": layer.name, "kind": layer.kind, "visible": layer.visible,
                  "effective_visible": layer.is_visible(), "opacity": layer.opacity,
                  "blend_mode": str(layer.blend_mode), "bbox": list(layer.bbox)}
        result.append(record)
        if layer.is_group():
            result.extend(layer_tree(layer, own, names + (layer.name,)))
    return result


def contacts(records, path):
    columns, cw, ch, header = 4, 340, 472, 60
    sheet = Image.new("RGB", (columns * cw, ((len(records) + columns - 1) // columns) * ch + header), "#18242d")
    draw = ImageDraw.Draw(sheet)
    draw.text((14, 16), "G10 / complete artist paintings / PSD saved composite", font=font(22), fill="white")
    for i, item in enumerate(records):
        x, y = i % columns * cw, i // columns * ch + header
        with Image.open(ROOT / item["output_path"]) as image:
            image = image.convert("RGBA")
            image.thumbnail((cw - 24, ch - 76), Image.Resampling.LANCZOS)
            sheet.paste(image, (x + (cw - image.width) // 2, y), image)
        draw.text((x + 12, y + ch - 68), item["creature_id"], font=font(21), fill="white")
        draw.text((x + 12, y + ch - 39), f"Board {item['artboard_index'] + 1} / {item['output_size'][0]} x {item['output_size'][1]}", font=font(16), fill="#cad5df")
    output_file(path, encode_png(sheet))


def source_pairs(records, path):
    # Source crops are independent evidence files, read back against runtime PNGs.
    cw, rowh = 720, 440
    sheet = Image.new("RGB", (cw, rowh * len(records)), "#18242d")
    draw = ImageDraw.Draw(sheet)
    for i, item in enumerate(records):
        y = i * rowh
        draw.text((12, y + 7), f"{item['creature_id']} / saved PSD composite vs runtime PNG", font=font(20), fill="white")
        for column, key, label in ((0, "source_composite_crop_reference", "PSD composite crop"), (1, "output_path", "Full painting PNG")):
            with Image.open(ROOT / item[key]) as image:
                image = image.convert("RGBA")
                image.thumbnail((330, 370), Image.Resampling.LANCZOS)
                sheet.paste(image, (column * 360 + (360 - image.width) // 2, y + 35), image)
            draw.text((column * 360 + 25, y + 408), label, font=font(17), fill="white")
    output_file(path, encode_png(sheet))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--sources", type=Path, default=ROOT / ".tools/creature-artwork-sources/sources.json")
    parser.add_argument("--mapping", type=Path, default=ROOT / "Tools/Artwork/creature_mapping.json")
    parser.add_argument("--output", type=Path, default=ROOT / "Assets/_Project/Art/SurveyPaintings")
    parser.add_argument("--report", type=Path, default=ROOT / "Docs/QA/CreatureArtworkIntegration/FullPaintingsCorrection")
    parser.add_argument("--only", default="")
    args = parser.parse_args()
    sources = json.loads(args.sources.read_text(encoding="utf-8-sig"))
    mapping = json.loads(args.mapping.read_text(encoding="utf-8"))
    source_by_id = {source["id"]: source for source in sources}
    cache, entries = {}, []
    for item in mapping["creatures"]:
        if args.only and item["creature_id"] != args.only:
            continue
        source = source_by_id[item["source_id"]]
        if source["id"] not in cache:
            data = Path(source["path"]).read_bytes()
            if data[:4] != b"8BPS" or len(data) != source["bytes"] or sha256(data) != source["sha256"]:
                raise ValueError(f"Source signature/size/hash mismatch: {source['name']}")
            psd = PSDImage.open(io.BytesIO(data))
            # topil reads PSD Image Data (Photoshop's saved merged image). It does
            # not run the psd-tools per-layer compositor; unsupported live blend
            # effects therefore retain their appearance as saved by the artist.
            merged = psd.topil(apply_icc=True)
            if merged is None or merged.size != (psd.width, psd.height):
                raise ValueError(f"No complete saved merged image: {source['name']}")
            merged = merged.convert("RGBA")
            reference = args.report / "source-composites" / f"{source['id']}.png"
            output_file(reference, encode_png(merged))
            cache[source["id"]] = (psd, merged, reference)
        psd, merged, reference = cache[source["id"]]
        board_index = item["index_path"][0]
        board = psd[board_index]
        bounds = tuple(board.bbox)
        if board.kind != "artboard" or not board.is_visible():
            raise ValueError(f"Mapped board is not visible in saved composite: {item['creature_id']}")
        if not (0 <= bounds[0] < bounds[2] <= merged.width and 0 <= bounds[1] < bounds[3] <= merged.height):
            raise ValueError(f"Artboard exceeds saved merged image: {bounds}")
        painting = merged.crop(bounds)
        destination = args.output / f"{item['creature_id']}.png"
        payload = encode_png(painting)
        output_file(destination, payload)
        source_crop = args.report / "source-crops" / f"{item['creature_id']}.png"
        output_file(source_crop, encode_png(merged.crop(bounds)))
        with Image.open(destination) as exported:
            if exported.convert("RGBA").tobytes() != painting.tobytes():
                raise AssertionError("PNG differs from source artboard pixels")
        tree = layer_tree(board, (board_index,), (board.name,))
        subject_prefix = item["index_path"]
        omitted = [node for node in tree if node["index_path"][:len(subject_prefix)] != subject_prefix]
        entry = {key: item[key] for key in ("creature_id", "source_id", "zone", "location_id", "poi_id", "objectives", "role", "confidence", "basis")}
        entry.update({
            "source_name": source["name"], "source_sha256": source["sha256"], "source_bytes": source["bytes"],
            "source_document_size": list(merged.size), "saved_composite_origin": "PSD Image Data, decoded by PSDImage.topil(apply_icc=True); not a per-layer re-render",
            "saved_composite_reference": relative(reference), "saved_composite_rgba_sha256": sha256(merged.tobytes()),
            "source_composite_crop_reference": relative(source_crop), "source_composite_crop_rgba_sha256": sha256(painting.tobytes()),
            "artboard_index": board_index, "artboard_name": board.name, "artboard_bounds_source": list(bounds),
            "previous_subject_index_path": subject_prefix, "previous_reviewed_crop_ignored": item.get("reviewed_crop_source"),
            "previous_omitted_layer_count": len(omitted), "previous_omitted_layers": omitted,
            "full_artboard_layer_inventory": tree,
            "output_path": relative(destination), "output_size": list(painting.size),
            "output_aspect_ratio": painting.width / painting.height, "output_sha256": sha256(payload), "output_bytes": len(payload),
            "output_rgba_sha256": sha256(painting.tobytes()), "pixel_equality_to_saved_composite_crop": True,
            "alpha_range": list(painting.getchannel("A").getextrema()),
            "content_policy": "Entire original artboard including background, environment, foreground, native lighting/texture/blending, film frame and captions. No alpha trim, subject crop, resizing, recoloring, keying or painting.",
        })
        entries.append(entry)
        print(json.dumps({"exported": item["creature_id"], "path": str(destination), "size": list(painting.size), "aspect": entry["output_aspect_ratio"], "rgba_hash": entry["output_rgba_sha256"]}), flush=True)
    if args.only and not entries:
        raise ValueError(f"Unknown creature ID: {args.only}")
    manifest = {"schema_version": 1, "role": "Complete survey-photo paintings; separate from capture cutouts",
                "mapping_authority": mapping["mapping_authority"], "mapping_path": relative(args.mapping), "mapping_sha256": sha256(args.mapping.read_bytes()),
                "export_script": relative(Path(__file__)), "export_script_sha256": sha256(Path(__file__).read_bytes()),
                "tools": {name: importlib.metadata.version(name) for name in ("psd-tools", "Pillow", "numpy")},
                "color_policy": "Decode saved merged RGB with embedded ICC conversion to sRGB to preserve authored display appearance; no manual grading.",
                "sources": [{key: source[key] for key in ("id", "name", "bytes", "sha256")} for source in sources], "paintings": entries}
    name = f"full-paintings-{args.only}.json" if args.only else "full-paintings-manifest.json"
    output_file(args.report / name, (json.dumps(manifest, ensure_ascii=False, indent=2) + "\n").encode("utf-8"))
    if not args.only:
        ordered = sorted(entries, key=lambda entry: entry["creature_id"])
        contacts(ordered, args.report / "full-paintings-contact-sheet.png")
        source_pairs(ordered, args.report / "source-versus-export-all.png")
        for zone in sorted({entry["zone"] for entry in ordered}):
            source_pairs([entry for entry in ordered if entry["zone"] == zone],
                         args.report / f"source-versus-export-{zone}.png")
    print(f"Manifest: {args.report / name}", flush=True)


if __name__ == "__main__":
    main()
