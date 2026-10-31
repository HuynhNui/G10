"""Offline advice from a pair of actual chart screenshots; no game/project APIs."""
from __future__ import annotations

import argparse
import hashlib
import heapq
import json
import math
import sys
from datetime import datetime, timezone
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage


DISCLOSURE = (
    "OFFLINE SCREENSHOT PERCEPTION / OPERATOR ADVICE ONLY. The only terrain evidence is "
    "the supplied actual toggle-ON/OFF screenshot pair. No Unity, asset terrain, RLE, live "
    "contacts, gameplay state, input injection, teleport or game-clock advancement is used. "
    "Gold pixels are inferred from RGB differences; missed/animated/occluded pixels, chart "
    "alignment, unknown outside-crop geometry and control momentum can invalidate advice. "
    "The operator must inspect the inference overlay and use ordinary game controls and "
    "collision feedback. This does not establish human wayfinding skill or tension."
)


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as source:
        for chunk in iter(lambda: source.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def parser() -> argparse.ArgumentParser:
    p = argparse.ArgumentParser(description=__doc__ + " No route is produced without all explicit inputs.")
    p.add_argument("--on", required=True, type=Path, help="Actual full-resolution chart screenshot, terrain toggle ON")
    p.add_argument("--off", required=True, type=Path, help="Same chart/camera/crop screenshot, terrain toggle OFF")
    p.add_argument("--crop", required=True, nargs=4, type=int, metavar=("LEFT", "TOP", "RIGHT", "BOTTOM"),
                   help="Screen pixels, top-left origin, right/bottom exclusive; exclude frame, labels and controls")
    p.add_argument("--extent", required=True, nargs=4, type=float, metavar=("XMIN", "YMIN", "XMAX", "YMAX"),
                   help="Visible world coordinates at crop corner pixel centers; must describe a north-up, unskewed chart")
    p.add_argument("--start", required=True, nargs=2, type=float, metavar=("X", "Y"), help="Operator-supplied current Helm position")
    p.add_argument("--target", required=True, nargs=2, type=float, metavar=("X", "Y"), help="Operator-supplied visible target coordinates")
    p.add_argument("--clearance", type=float, default=10.0, help="Required distance from inferred blocked pixels, in supplied world units (default 10)")
    p.add_argument("--cell-pixels", type=int, default=3, help="A* cell side; a cell is safe only if EVERY pixel has clearance (default 3)")
    p.add_argument("--minimum-difference", type=float, default=24.0, help="Minimum maximum RGB channel change (default 24)")
    p.add_argument("--minimum-gold-gain", type=float, default=20.0, help="Minimum gain in R+G-2B, rejecting small lighting changes (default 20)")
    p.add_argument("--minimum-component-pixels", type=int, default=9, help="Discard smaller isolated changed-gold components; set 1 to retain all (default 9)")
    p.add_argument("--gap-close-pixels", type=int, default=1, help="Bridge tiny detector gaps conservatively; retain all original accepted pixels (default 1)")
    p.add_argument("--maximum-expanded", type=int, default=1000000, help="Bounded A* node expansion count (default 1000000)")
    p.add_argument("--out", required=True, type=Path, help="New output directory; must not already exist, preserving previous evidence")
    return p


def infer_blocked(on: np.ndarray, off: np.ndarray, args: argparse.Namespace):
    a, b = on.astype(np.float32), off.astype(np.float32)
    difference = np.max(np.abs(a - b), axis=2)
    red, green, blue = a[..., 0], a[..., 1], a[..., 2]
    gold = (red >= 110) & (green >= 90) & (red >= green - 25) & (red - blue >= 30) & (green - blue >= 20)
    warmth_gain = (red + green - 2 * blue) - (b[..., 0] + b[..., 1] - 2 * b[..., 2])
    raw = gold & (difference >= args.minimum_difference) & (warmth_gain >= args.minimum_gold_gain)
    labels, count = ndimage.label(raw, structure=np.ones((3, 3), dtype=bool))
    sizes = np.bincount(labels.ravel())
    keep = sizes >= args.minimum_component_pixels
    keep[0] = False
    accepted = keep[labels]
    blocked = accepted.copy()
    if args.gap_close_pixels:
        closed = ndimage.binary_closing(accepted, structure=np.ones((3, 3), dtype=bool), iterations=args.gap_close_pixels)
        blocked |= closed  # Closing never deletes a detected, accepted obstacle.
    return blocked, raw, {
        "raw_changed_gold_pixels": int(raw.sum()), "raw_components": int(count),
        "discarded_small_component_pixels": int(raw.sum() - accepted.sum()),
        "gap_fill_pixels": int(blocked.sum() - accepted.sum()),
        "inferred_blocked_pixels": int(blocked.sum()), "blocked_fraction": float(blocked.mean()),
        "maximum_rgb_difference": float(difference.max()),
        "difference_p50_p95_p99": [float(v) for v in np.percentile(difference, [50, 95, 99])],
    }


def line_clear(a, b, safe: np.ndarray) -> bool:
    """Conservative subpixel sampling: all four floor/ceil neighbors must be safe."""
    length = math.dist(a, b)
    steps = max(1, math.ceil(length * 4))
    xs = np.linspace(a[0], b[0], steps + 1)
    ys = np.linspace(a[1], b[1], steps + 1)
    height, width = safe.shape
    for xx in (np.floor(xs).astype(int), np.ceil(xs).astype(int)):
        for yy in (np.floor(ys).astype(int), np.ceil(ys).astype(int)):
            if np.any(xx < 0) or np.any(xx >= width) or np.any(yy < 0) or np.any(yy >= height):
                return False
            if not np.all(safe[yy, xx]):
                return False
    return True


def astar(safe: np.ndarray, start, target, cell: int, world_per_pixel, maximum_expanded: int):
    height, width = safe.shape
    rows, cols = math.ceil(height / cell), math.ceil(width / cell)
    padded = np.pad(safe, ((0, rows * cell - height), (0, cols * cell - width)), constant_values=False)
    cells = padded.reshape(rows, cell, cols, cell).all(axis=(1, 3))

    def node(point):
        return min(rows - 1, int((point[1] + .5) // cell)), min(cols - 1, int((point[0] + .5) // cell))

    def center(value):
        row, col = value
        return ((col + .5) * cell - .5, (row + .5) * cell - .5)

    source, goal = node(start), node(target)
    for name, value, point in (("start", source, start), ("target", goal, target)):
        if not cells[value] or not line_clear(point, center(value), safe):
            raise ValueError(f"{name} or its A* cell lacks requested clearance; inspect overlay or explicitly revise clearance/cell size/endpoint")

    dx, dy = world_per_pixel

    def heuristic(value):
        return math.hypot((value[1] - goal[1]) * cell * dx, (value[0] - goal[0]) * cell * dy)

    queue = [(heuristic(source), 0.0, source)]
    scores = {source: 0.0}
    parent, settled = {}, set()
    directions = [(dr, dc) for dr in (-1, 0, 1) for dc in (-1, 0, 1) if dr or dc]
    while queue:
        _, distance, current = heapq.heappop(queue)
        if current in settled:
            continue
        settled.add(current)
        if len(settled) > maximum_expanded:
            raise ValueError("A* expansion budget exhausted; no advice issued")
        if current == goal:
            path = [goal]
            while path[-1] != source:
                path.append(parent[path[-1]])
            path = [tuple(start)] + [center(p) for p in reversed(path)] + [tuple(target)]
            compact = [path[0]]
            for point in path[1:]:
                if math.dist(compact[-1], point) > 1e-8:
                    compact.append(point)
            return compact, len(settled), [rows, cols]
        row, col = current
        for dr, dc in directions:
            nr, nc = row + dr, col + dc
            if nr < 0 or nr >= rows or nc < 0 or nc >= cols or not cells[nr, nc]:
                continue
            if dr and dc and (not cells[row, nc] or not cells[nr, col]):
                continue  # No cutting across a blocked diagonal corner.
            neighbor = (nr, nc)
            candidate = distance + math.hypot(dc * cell * dx, dr * cell * dy)
            if candidate < scores.get(neighbor, math.inf):
                scores[neighbor], parent[neighbor] = candidate, current
                heapq.heappush(queue, (candidate + heuristic(neighbor), candidate, neighbor))
    raise ValueError("No route exists in the screenshot-inferred grid at the requested clearance; do not treat this as authoritative game blockage")


def simplify(path, safe):
    result, index = [path[0]], 0
    while index < len(path) - 1:
        # Search farther-to-nearer. Visibility is not monotonic along a winding path.
        following = next((i for i in range(len(path) - 1, index, -1) if line_clear(path[index], path[i], safe)), None)
        if following is None:
            raise ValueError("A* segment failed conservative pixel LOS validation; no advice issued")
        result.append(path[following])
        index = following
    return result


def run(args):
    if not args.out.resolve().is_relative_to(Path(__file__).resolve().parent):
        raise ValueError("Output must remain under this helper's preparation directory")
    if args.out.exists():
        raise ValueError("Output directory already exists; select a fresh evidence directory")
    left, top, right, bottom = args.crop
    xmin, ymin, xmax, ymax = args.extent
    numeric = args.extent + args.start + args.target + [args.clearance, args.minimum_difference, args.minimum_gold_gain]
    if not all(math.isfinite(v) for v in numeric) or xmax <= xmin or ymax <= ymin or args.clearance <= 0:
        raise ValueError("Use finite coordinates, increasing extent bounds and positive clearance")
    if args.cell_pixels < 1 or args.cell_pixels > 32 or args.minimum_component_pixels < 1 or args.gap_close_pixels < 0 or args.gap_close_pixels > 8 or args.maximum_expanded < 1:
        raise ValueError("Invalid bounded detector/grid parameters")
    if args.minimum_difference < 0 or args.minimum_gold_gain < 0:
        raise ValueError("RGB detector thresholds must be nonnegative")
    with Image.open(args.on) as source:
        on_full = source.convert("RGB")
    with Image.open(args.off) as source:
        off_full = source.convert("RGB")
    if on_full.size != off_full.size:
        raise ValueError("Screenshot dimensions differ; matching full-resolution chart framing is required")
    if not (0 <= left < right <= on_full.width and 0 <= top < bottom <= on_full.height) or right - left < 3 or bottom - top < 3:
        raise ValueError("Crop must fit both full screenshots and have at least 3 pixels per side")
    width, height = right - left, bottom - top
    dx, dy = (xmax - xmin) / (width - 1), (ymax - ymin) / (height - 1)

    def to_pixel(point):
        x, y = point
        if not (xmin <= x <= xmax and ymin <= y <= ymax):
            raise ValueError("Start/target lies outside supplied visible extent")
        return ((x - xmin) / dx, (ymax - y) / dy)

    def to_world(point):
        return [xmin + point[0] * dx, ymax - point[1] * dy]

    start, target = to_pixel(args.start), to_pixel(args.target)
    on, off = np.asarray(on_full.crop(args.crop)), np.asarray(off_full.crop(args.crop))
    blocked, raw, detector = infer_blocked(on, off, args)
    args.out.mkdir(parents=True, exist_ok=False)
    document = {
        "schema": "g10.screenshot-route-advice.v1", "created_utc": datetime.now(timezone.utc).isoformat(),
        "disclosure": DISCLOSURE, "status": "inference-only", "route_issued": False,
        "sources": {name: {"path": str(path.resolve()), "sha256": sha256(path), "bytes": path.stat().st_size}
                    for name, path in (("terrain_on_screenshot", args.on), ("terrain_off_screenshot", args.off))},
        "helper_sha256": sha256(Path(__file__)),
        "inputs": {key: str(value.resolve()) if isinstance(value, Path) else value for key, value in vars(args).items()},
        "coordinate_contract": "North-up, axis-aligned chart. Screenshot/crop pixels: top-left origin. Extent supplied at crop corner pixel centers; world Y increases upwards.",
        "detector": detector, "world_units_per_pixel": [dx, dy], "operator_waypoints": [],
    }
    Image.fromarray((raw * 255).astype(np.uint8)).save(args.out / "raw-changed-gold.png")
    Image.fromarray((blocked * 255).astype(np.uint8)).save(args.out / "inferred-blocked.png")
    distance = ndimage.distance_transform_edt(np.pad(~blocked, 1, constant_values=False), sampling=(dy, dx))[1:-1, 1:-1]
    safe = distance >= args.clearance
    Image.fromarray((safe * 255).astype(np.uint8)).save(args.out / "clearance-safe.png")
    annotated = on_full.convert("RGBA")
    tint = np.zeros((height, width, 4), dtype=np.uint8)
    tint[blocked] = (255, 35, 35, 155)
    tint[~safe & ~blocked] = (255, 170, 30, 65)
    annotated.alpha_composite(Image.fromarray(tint), (left, top))
    draw = ImageDraw.Draw(annotated)
    draw.rectangle((left, top, right - 1, bottom - 1), outline=(255, 255, 255, 220), width=2)
    try:
        if detector["inferred_blocked_pixels"] < 16:
            raise ValueError("Too few changed-gold pixels to establish toggle evidence; inspect pair/crop/thresholds")
        if detector["blocked_fraction"] > .75:
            raise ValueError("Most of the crop is inferred blocked; inspect alignment, framing, toggle and detector")
        path, expanded, shape = astar(safe, start, target, args.cell_pixels, (dx, dy), args.maximum_expanded)
        reduced = simplify(path, safe)
        waypoints = [to_world(point) for point in reduced]
        screen = [(point[0] + left, point[1] + top) for point in reduced]
        draw.line(screen, fill=(40, 255, 100, 255), width=3)
        for i, point in enumerate(screen):
            x, y = point
            draw.ellipse((x - 5, y - 5, x + 5, y + 5), fill=(5, 25, 15, 255), outline=(100, 255, 150, 255), width=2)
            draw.text((x + 7, y - 14), str(i), fill=(0, 0, 0, 255), stroke_width=2, stroke_fill=(255, 255, 255, 255))
        document.update(status="advisory-route", route_issued=True, operator_waypoints=[
            {"index": i, "world_x": round(world[0], 4), "world_y": round(world[1], 4),
             "screenshot_x": round(point[0], 2), "screenshot_y": round(point[1], 2)}
            for i, (world, point) in enumerate(zip(waypoints, screen))],
            route_length_world=sum(math.dist(a, b) for a, b in zip(waypoints, waypoints[1:])),
            expanded_nodes=expanded, grid_shape_rows_columns=shape,
            minimum_required_clearance_world=args.clearance,
            endpoint_policy="Exact supplied endpoints retained; no silent snapping or clearance relaxation")
    except ValueError as error:
        document.update(status="no-advice", error=str(error))
    annotated.convert("RGB").save(args.out / "annotated-original.png")
    (args.out / "disclosure.txt").write_text(DISCLOSURE + "\n\n" +
        "Red: inferred gold obstacles. Amber: clearance exclusion. Green: advisory segments. "
        "This is the original ON screenshot with diagnostic overlays; inspect it before use.\n", encoding="utf-8")
    document["outputs"] = {p.name: {"sha256": sha256(p), "bytes": p.stat().st_size}
                           for p in sorted(args.out.iterdir()) if p.is_file()}
    (args.out / "advice.json").write_text(json.dumps(document, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(json.dumps({"status": document["status"], "route_issued": document["route_issued"],
                      "output": str(args.out.resolve()), "waypoints": document["operator_waypoints"],
                      "error": document.get("error")}, ensure_ascii=False))
    return 0 if document["route_issued"] else 2


def main():
    args = parser().parse_args()
    try:
        return run(args)
    except (ValueError, OSError) as error:
        print(json.dumps({"status": "invalid-input", "route_issued": False, "error": str(error)}), file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
