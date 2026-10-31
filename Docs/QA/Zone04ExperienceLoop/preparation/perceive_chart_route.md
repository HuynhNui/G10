# Screenshot-only route perception helper

`perceive_chart_route.py` accepts two **actual full-resolution chart screenshots**, with the terrain toggle ON and OFF. It has no Unity or project/game-data imports and never sends input. No screenshots or coordinates have been supplied to it during preparation; no route has been generated.

Supply both screenshot paths, a pixel crop, the visible world extent of that crop, the current position read from Helm, and target coordinates available to the operator. The crop uses top-left screenshot coordinates with right/bottom exclusive. The extent is `XMIN YMIN XMAX YMAX` at the crop's corner pixel centers; world Y increases upwards. Only north-up, axis-aligned charts are supported. A tilted or perspective-skewed panel must not be approximated by this extent contract.

```powershell
python Docs/QA/Zone04ExperienceLoop/preparation/perceive_chart_route.py --help
```

For an actual later run, supply `--on`, `--off`, `--crop LEFT TOP RIGHT BOTTOM`, `--extent XMIN YMIN XMAX YMAX`, `--start X Y`, `--target X Y`, `--clearance WORLD_UNITS`, and `--out NEW_OUTPUT_DIRECTORY`. There are deliberately no baked-in routes, world coordinates, map names or screenshot locations. Keep outputs under this preparation directory. Use a fresh output directory for each inference; existing evidence is never overwritten.

Capture both images at identical chart zoom, pan, resolution and framing. Exclude labels, controls, legends and window frames from the crop. A* treats only the supplied screenshot crop as observable; crop boundaries are closed. Start/target coordinates must lie inside the supplied extent. Endpoints are never silently snapped, and required clearance is never silently relaxed.

The detector requires a sufficiently large RGB change, gold color in the ON image, and increased warm chroma (`R+G−2B`). This rejects small animated lighting changes. It removes small isolated changed-gold components (default 9 pixels), fills tiny detector gaps, and retains every accepted obstacle pixel. These are perception heuristics: inspect both `raw-changed-gold.png` and `inferred-blocked.png`. If legitimate thin obstacles are filtered, explicitly reduce `--minimum-component-pixels`; if the pair is misaligned or occluded, recapture it instead of trusting the result.

SciPy computes anisotropic obstacle distance in the supplied world units. Each A* cell is accepted only when every pixel meets clearance. Diagonal corner cutting is rejected. The simplified segments are checked against the inferred safe pixels at subpixel intervals with conservative floor/ceil neighbors. Clearance covers the inferred geometry only; actual ship radius, steering arcs, speed, normal braking and screenshot interpretation remain the operator's responsibility.

Each run writes the original ON screenshot with red inferred obstacles, amber clearance exclusions and green advisory segments; raw/accepted masks; a safe-pixel diagnostic; a disclosure; and `advice.json` containing source/helper/output SHA-256 hashes, explicit inputs, detector statistics and numbered world/screen waypoints. Insufficient evidence, unreachable endpoints, exhausted search or no inferred route produces diagnostic artifacts with `route_issued: false`, exit code 2 and no waypoints. This never proves an authoritative gameplay blockage.

Use advisory waypoints one leg at a time through ordinary game controls, with actual chart/Helm/collision feedback. Do not infer human wayfinding speed or fear from an assisted journey. No route or runtime test is part of preparing this helper.
