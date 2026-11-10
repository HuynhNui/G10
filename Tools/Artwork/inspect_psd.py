"""Inspect supplied PSDs without changing them; reproducible hierarchy and previews.

Run with .tools/creature-artwork-venv/Scripts/python.exe -X utf8
    Tools/Artwork/inspect_psd.py
"""
from __future__ import annotations

import argparse
import hashlib
import json
import re
import unicodedata
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont
from psd_tools import PSDImage


def normalized(name: str) -> str:
    return "".join(c for c in unicodedata.normalize("NFD", name.lower())
                   if unicodedata.category(c) != "Mn").strip()


def alpha_stats(im: Image.Image) -> dict:
    a = np.asarray(im.convert("RGBA"))[:, :, 3]
    return {"transparent_fraction": float(np.mean(a == 0)),
            "opaque_fraction": float(np.mean(a == 255)),
            "nonzero_alpha_bbox": list(im.convert("RGBA").getchannel("A").getbbox() or (0, 0, 0, 0))}


def sheet(items: list[tuple[str, Image.Image]], path: Path, width=280, height=350):
    cols = min(5, max(1, len(items)))
    out = Image.new("RGB", (cols * width, ((len(items) + cols - 1) // cols) * (height + 50)), "#20272c")
    draw = ImageDraw.Draw(out)
    try:
        font = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 17)
    except OSError:
        font = ImageFont.load_default()
    for i, (label, im) in enumerate(items):
        pic = im.convert("RGBA").copy()
        pic.thumbnail((width - 16, height - 10))
        x = (i % cols) * width + (width - pic.width) // 2
        y = (i // cols) * (height + 50)
        out.paste(pic, (x, y), pic)
        draw.text(((i % cols) * width + 6, y + height), label, fill="white", font=font)
    out.save(path)


def inspect(source: dict, output: Path) -> dict:
    src = Path(source["path"])
    data = src.read_bytes()
    digest = hashlib.sha256(data).hexdigest()
    if data[:4] != b"8BPS" or digest.lower() != source["sha256"].lower():
        raise ValueError(f"Source signature/checksum mismatch: {src}")
    folder = output / source["id"]
    folder.mkdir(parents=True, exist_ok=True)
    psd = PSDImage.open(src)
    metadata = {"source": source, "verified_sha256": digest, "width": psd.width,
                "height": psd.height, "color_mode": str(psd.color_mode),
                "depth": psd.depth, "layers": [], "exports": []}
    merged = psd.topil()
    if merged:
        merged.save(folder / "merged-native.png")
        thumb = merged.copy()
        thumb.thumbnail((2200, 1100))
        thumb.save(folder / "merged-preview.png")

    def tree(group, path=()):
        for index, layer in enumerate(group):
            own = path + (index,)
            node = {"index_path": list(own), "name": layer.name, "kind": layer.kind,
                    "bbox": list(layer.bbox), "visible": layer.visible,
                    "effective_visible": layer.is_visible(), "opacity": layer.opacity,
                    "blend_mode": str(layer.blend_mode), "is_group": layer.is_group()}
            if layer.kind == "type":
                node["text"] = layer.text
            metadata["layers"].append(node)
            if layer.is_group():
                tree(layer, own)
    tree(psd)

    boards, subjects = [], []
    for board_index, board in enumerate(psd):
        if board.kind != "artboard":
            continue
        viewport = tuple(board.bbox)
        board_name = f"board-{board_index + 1:02d}"
        # The merged Adobe composite is retained above as the visual reference.
        # Compositing hidden boards is explicitly identified as inspection only.
        def visible_in_board(layer):
            if layer is board:
                return True
            return layer.visible
        im = board.composite(viewport=viewport, layer_filter=visible_in_board)
        if im:
            im.save(folder / f"{board_name}-with-frame.png")
            boards.append((f"{board_name} / visible={board.visible}", im))
            metadata["exports"].append({"kind": "artboard_reference", "index_path": [board_index],
                "viewport": list(viewport), "file": f"{board_name}-with-frame.png", **alpha_stats(im)})
        for child_index, child in enumerate(board):
            key = normalized(child.name)
            if not child.is_group() or key not in {"sinh vat", "ma", "con ma", "tau ngam,"}:
                continue
            kind = "subject" if key == "sinh vat" else "scare" if key in {"ma", "con ma"} else "wreck"
            selected = child.composite(viewport=viewport, layer_filter=lambda layer: layer.visible)
            if selected is None:
                continue
            filename = f"{board_name}-{kind}-group-{child_index:02d}.png"
            selected.save(folder / filename)
            metadata["exports"].append({"kind": kind, "index_path": [board_index, child_index],
                "name": child.name, "viewport": list(viewport), "layer_bbox": list(child.bbox),
                "file": filename, **alpha_stats(selected)})
            subjects.append((f"{board_name} {kind} [{child_index}]", selected))
    if boards:
        sheet(boards, folder / "artboards-contact.png")
    if subjects:
        sheet(subjects, folder / "groups-contact.png")
    (folder / "hierarchy.json").write_text(json.dumps(metadata, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({"source": source["name"], "folder": str(folder),
                      "layers": len(metadata["layers"]), "exports": len(metadata["exports"])}, ensure_ascii=False), flush=True)
    return metadata


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--sources", type=Path, default=Path(".tools/creature-artwork-sources/sources.json"))
    parser.add_argument("--output", type=Path, default=Path("Docs/QA/CreatureArtworkIntegration/inspection"))
    parser.add_argument("--only", default="", help="Optional source Drive ID")
    args = parser.parse_args()
    sources = json.loads(args.sources.read_text(encoding="utf-8-sig"))
    args.output.mkdir(parents=True, exist_ok=True)
    results = [inspect(s, args.output) for s in sources if not args.only or s["id"] == args.only]
    (args.output / "index.json").write_text(json.dumps(results, ensure_ascii=False, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
