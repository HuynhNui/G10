# Zone04 controlled chart and terrain diagnosis

Prepared 2026-10-27 after the bounded actual-input baseline was stopped. This is an offline source/asset diagnosis, not measured navigation and not a completed voyage. Only this report and its companion [JSON](chart-diagnosis.json) were written. No Unity call, production edit, persistent-state write, path reconstruction, route coordinates or shortcut artifact was produced. Existing Zone03 dirty files were retained.

## Decision for the experience audit

**L2 is geometrically reachable from entry under the actual hull footprint, with a broad-clearance connection.** The stored collision mask exactly matches the current terrain-line bake. The static map and terrain art are aligned; no mirrored map, moving collision wall, stale mask or impossible route was demonstrated. Cyan-only tracing is not a valid interpretation of the complete collision authority. The observed failure to navigate remains a control/route-assurance blocker, rather than evidence that the game is impossible.

The chart deliberately omits vessel position (`PhotoSurveyMap.cs:9`). Preserve that design. These findings provide no route for the final actual-input playthrough, which must still plan using the actual visible chart, Helm and Radar.

## Mask, footprint and clearance certificate

| Check | Offline result |
| --- | ---: |
| World/art dimensions | 1920 × 1080 |
| Serialized mask | 960 × 540; 518,400 cells; 2 × 2 world units/cell |
| RLE | 25,695 bytes; 8,565 runs; decoded length 518,400; values only 0/1 |
| Re-bake versus stored RLE | **0 mismatched cells** |
| Blocked / water cells | 104,726 / 413,674 |
| Raw water components, four-neighbour | 11 |
| Hull-valid cells / components | 396,475 / 11 |
| Entry / exact L2 point hull valid | true / true |
| Entry → exact L2 connected | **true** |
| Entry → L2 arrival disk connected | **true** |
| Entry hull-valid component | 218,893 cells |
| L2 radius-20 arrival-disk sampled valid cells | 317; all 317 in entry component |
| Best connected conservative centre-sample clearance | 20.946 world units |
| Certified continuous axis-segment clearance lower bound | 19.946 world units |

Method: decode the RLE exactly as `ZoneMapConfig.Decode` does, with Unity bottom-left pixel order. Reproduce `NavigationArtworkEditor.BakeBoundaries`: any of the four sampled source pixels with alpha > 100 and RGB sum < 500 blocks a cell. The result matches every stored cell. `CanOccupy` checks the centre and four cardinal samples at ±2 world units (`ZoneNavigation.cs:298–325`). With this mask's 2-unit square cells, cross erosion is its exact interior-cell occupancy model. Four-neighbour component labels establish connectivity without constructing a path; each adjacent-centre segment remains valid.

For clearance, compute Euclidean distance to nearest blocked-cell centre, including blocked padding outside world limits, then subtract half the mask-cell diagonal. Threshold connected components by that conservative distance. Connectivity survives a 20-unit centre threshold, but not 25. The maximum threshold is 20.946; subtract another 1 unit for the worst midpoint between adjacent 2-unit-spaced centres, yielding a continuous lower bound of 19.946. Entry and L2 endpoint joins have much greater clearance. This is a conservative existence certificate, not an exact corridor width or a claim about an operator's chosen route. It shows that reaching L2 does not require an unavoidable gap close to the 2-unit footprint. It does not establish resource sufficiency or successful input control.

## What the chart actually draws

The shared cabin scene's Zone04 presentation binds `Mapingame.png` as the static map, `Light.png` as `MapLight01`, and the Zone04 map config/overlay (`Zone01.unity:25525–25544`, `:18066–18087`). The config's separate `Mapline.png` supplies collision baking; `ZoneMapPresentation` does not draw it as a canonical contour overlay. `PhotoSurveyMap` draws grid and mission/route markers, not vessel position or terrain contours (`PhotoSurveyMap.cs:315–321`).

The **thin bright cyan contours already exist in static `Mapingame.png`**, alongside dark boundaries and filled rock forms. They are not the animated light layer. `Mapline.png` also contains cyan portions and dark forms; the bake uses alpha/RGB threshold, not a cyan classifier. A deliberately explicit descriptive cyan threshold—G > R + 45, B > R + 45, G/B > 160—finds cyan in only 13,160 of the 104,726 blocked cells. **91,566 collision cells have no such bright cyan.** Conversely some cyan cells are water under the actual bake. Cyan-only tracing therefore loses most collision information. These counts describe colour selection, not a replacement terrain definition.

The full map visibly carries the corresponding dark geometry. Source Mapline and Mapingame are both 1920 × 1080. Luminance-gradient correlation at matching pixels is 0.714, versus 0.019 horizontally flipped and 0.009 vertically flipped; 160,145 of 189,639 strong line-art gradient pixels retain chart gradient above the stated lower threshold. This supports alignment, while not claiming pixel identity: the colour/background treatment is different. Exact source-pixel RGB equality is not expected and is zero. Mask re-bake identity alone would not prove readable displayed terrain; the separate base-map inspection and edge checks address that distinction.

The serialized Zone04 RawImage uses UV (0,0,1,1); map and ancestor rectangles have identity rotations and positive unit scale up to the Canvas. The Canvas is Screen Space Overlay, so a world camera cannot invert this chart. `ZoneMapConfig.CoordinatesToUV` and navigation use the same increasing-X/increasing-Y mapping (`ZoneMapConfig.cs:136–145`, `ZoneNavigation.cs:108–124`). No global flip or offset defect was found. Runtime layout was not queried.

## Intentional light motion and readability limits

`MapLight01` is a separate broad, partially transparent watercolor texture. Its edge correlation with Mapline is −0.009 and with the base map is 0.023: it does not reproduce the terrain boundaries. Its full-stretch rectangle is centred, while `AnimateLights` changes only those light rectangles from scale 1 to 1.08 over 5 seconds and back (`ZoneMapPresentation.cs:93–105`). `RefreshMap` changes the static base texture only for the alternate-state selection, and terrain application occurs on that state change (`:65–75`); Zone04 has no alternate map.

The Light PNG has alpha 0–191, multiplied by configured opacity 0.3. Under standard UI source-over blending its maximum contribution is 0.2247, leaving at least 0.7753 of the static base contribution. Its edge displacement can reach 76.8 world-equivalent units horizontally and 43.2 vertically, but those are decorative-field shifts. There is **no moving-wall bug** in this source. Motion can vary local colour/contrast; perceptual interference is a UX hypothesis requiring rendered/human review.

The chart rectangle is 1440 × 840 Canvas units with no AspectRatioFitter, rather than the source aspect 16:9. Thus a 50-unit cell draws 37.5 × 38.889 Canvas units, a 3.7% anisotropy. Art, grid and markers share the same UV scaling, so this is a small display distortion, not collision misregistration. One 2-world-unit mask cell is only 1.5 × 1.556 Canvas units. At a 1280-wide, 16:9 viewport it is approximately 1 screen pixel, and at 960-wide it is approximately 0.75 pixel; this makes fine raster edges easy to lose. These are scale examples, not a claim about baseline viewport size. The broad connectivity certificate does not depend on exploiting subpixel gaps.

## Filled Radar semantics and a bounded static defect

`RadarTerrainMask.Build` pads blockers by a 3×3 square, flood-fills four-neighbour water from the first valid scan seed, then restores only one water-side cell around the filled component (`RadarTerrainMask.cs:12–69`). This is display-only; collision always uses the authored mask. The cache is invalidated by map-coordinate configuration, `SetChart`, restore and reset (`ZoneNavigation.cs:121–125`, `:162–177`, `:337–342`), but not normal movement.

With entry as seed, 228,383 cells render as Radar water. It marks 185,291 raw-water cells as solid because they lie outside that padded flood region. This is intentional filled-terrain semantics, not proof that the raw mask is wrong. Only **6 of 218,893 hull-valid cells in entry's component** render solid; exact L2 and all 317 sampled hull-valid arrival cells render water. No material entry-to-L2 Radar component contradiction was found.

There is a separate **confirmed static seed/cache defect with unobserved runtime occurrence**. The collision footprint is a cardinal cross, while temporary Radar padding is square. Fourteen hull-valid cells globally, including three in entry's component, are inside padding and have no four-neighbour unpadded open cell. If a first cache build occurs there, `Build` overwrites that padded seed with 1 and cannot leave it; the bounded restore returns only 7–8 water cells. `CanOccupy` is true, so the invalid-spawn fallback (`ZoneNavigation.cs:313–316`) does not protect this case. Subsequent ordinary movement retains that tiny cache. A source reproduction demonstrates the precondition and resulting flood size; it is not a Unity runtime test. **There is no evidence that this occurred in the baseline**, and it must not be used to explain the observed contacts without a matching scan/cache record.

## Classification and bounded design hypotheses

| Observation | Classification | Implication |
| --- | --- | --- |
| Entry–L2 hull-valid connection with substantial clearance | Confirmed offline geometry | No impossible-route claim; actual-input assurance remains outstanding |
| Exact RLE/Mapline bake and matching static map alignment | Confirmed asset/source evidence | No stale-mask, global inversion or moving-wall repair is justified |
| Cyan-only interpretation omits most blocking cells | Confirmed colour-selection mismatch; AI interpretation limitation | The chosen visual tracing method cannot assure navigation |
| Static terrain lacks a separate canonical contour overlay/legend | Confirmed presentation structure; likely readability opportunity | UX hypothesis, not proof of a geometry defect or measured human confusion |
| Animated watercolor field | Intentional decoration | Preserve existing art; assess any contrast interference from rendered evidence |
| Rare padded valid Radar seed | Confirmed static defect/precondition; baseline occurrence unknown | Separate bounded correctness risk; do not label primary navigation cause |
| Slight chart stretch and fine cell scale | Confirmed display properties | Potential readability contributors, not proven cause of baseline failure |

For root's audit and later single redesign decision only: consider a narrow, static canonical-boundary readability treatment derived from the existing authored terrain layer, plus a legend distinguishing impassable dark terrain from cyan bioluminescence. Preserve original art, all geometry/locations/depths/costs/gates/capture/progression, hidden destinations and the intentional absent ship marker. Any overlay must stay registered with the same UVs and must not compute or display a route. This is a candidate UX hypothesis, not an implementation or approved redesign. Root must document baseline and experience audit before selecting scope.

## Reproducibility and limits

The companion JSON records source SHA256 hashes, mask decoding and comparison counts, four-neighbour occupancy/clearance summaries, Radar fill/seed counts, static UI bindings, and explicit conclusion booleans. Analysis used local Python 3.11, Pillow 12.1.1, NumPy 1.26.4 and SciPy 1.15.3; no library/package was installed. No path parents were retained or exported. No generated mask image or route visualization was written.

This report does not establish successful navigation, a complete Zone04 baseline, hidden-route completion, listener perception, subjective fear, resource feasibility or a runtime camera/layout test. It does not replace the measured baseline evidence or the final actual-input playthrough.
