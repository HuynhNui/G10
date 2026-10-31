# Zone04 chart-only route advice (2026-10-27)

Evidence: actual rendered continuation-01 frames 003578 (full chart), 002365, 004202, 004356, and 004874 (Radar), with their public ship position/heading records in frames.jsonl. No Unity calls, runtime input, collision geometry, source masks, RLE, or terrain-line queries were used.

The axes calibrate approximately as screenX = 240 + 0.75 * worldX; screenY = 1025 - (7/9) * worldY (top-origin screenshot). JPEG interpolation, line width, and chart grids limit precision to several world units. Annotated crops and threshold images in this folder derive only from the rendered JPEG.

The L2 target near 1772,693 sits in a west-facing notch. The bright eastern contour has a northern lip across roughly x1630..1850, y880..800, a right wall near x1850 beside L2, and a lower arm beginning near x1700,650. The observed hits near 1592,854; 1708,867; 1797,840; and 1826,828 align with the northern lip/approach. Known y900 travel is north of it. Thus a diagonal from 1862,853 to 1825,800 crosses visible contour; descending the eastern edge then moving west at target height also crosses the right wall.

Shortest visually plausible entry: return by the previously traversed northern water to approximately 1640,900; angle to 1610,875; probe south to 1610,835; continue through 1640,795, then 1700,750, then L2. The short 1610,875 to 1610,835 segment is the least certain: its apparent corridor is only about 20..30 units between the western dark shore and the cyan tip. Use small normal-input legs, observe actual displacement, and stop/reassess on contact; do not hold movement against a boundary. The cyan-only pixel route used about 8 world units of visible clearance, but its threshold is an interpretation of JPEG color, not collision truth.

A wide southern wrap would be much longer (east of x1860, around y350 and back north near x1650) and crosses additional faint/dark chart contours that have not been validated; it is not the first choice after finding the western-tip gap. A simplistic x1630 descent from y900 is too far east and crosses the northern bright lip.

The Radar images show filled regions. They support nearby obstruction direction but do not prove every filled pixel is an impassable raw line. Conversely, treating every dark decorative/contour line in the chart as a collision barrier trapped a pixel search even around known traversable positions. Neither image interpretation is a guarantee. Final control and all resource/collision costs remain with the normal-input pilot.
