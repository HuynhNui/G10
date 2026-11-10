# Other adopted crop decisions

Following the initial actual Zone2 Photo Lab review, the integration owner reviewed
the labeled contact sheet and these native-group gray/crop previews. Three further
explicit source regions were approved. No complete paint/line layer was hidden:
each implicated layer also contains important organism pixels.

| Creature | Adopted source region | Excluded pixels and provenance | Retained nonzero-alpha source bounds | Padded PNG |
|---|---|---|---|---|
| `Z1_Creature_01` | `[0,0,630,850]` | 1102 pixels in disconnected right-hand faint lines and bottom-right swatch strokes on `[0,2,4]` **net**. All main plant/halo retained. | `[186,106,611,807]` | 473 × 749 |
| `Z1_Creature_02` | `[0,0,888,800]` | 29 pixels in the disconnected bottom-left dot at `[54,1027,62,1033]`, on `[0,2,1]` **Layer 24**. The main body, tail, fins and bubbles remain. | `[140,165,764,750]` | 672 × 633 |
| `Z3_Creature_03` | `[2964,0,3640,1080]` | 1770 pixels in a disconnected white swatch disk at `[3644,323,3691,369]`, on `[3,5,5]` **Layer 96**. All legs, dorsal structure and yellow halo remain. | `[3159,175,3632,649]` | 521 × 522 |

All coordinates are original PSD pixel coordinates, right/bottom exclusive. Native
artboard bounds apply first. The exporter then uses the reviewed region, removes
only completely transparent outer padding and adds 24px of transparent padding.
Pixels inside each final content region are checked byte-for-byte against the
native group render. No alpha threshold, background keying or anatomy erasing is used.

`Z3_Creature_02` deliberately keeps both its isolated cyan disk and whole broad
blue wash. Both belong to `[1,1,2]` **bong**, which also contributes the organism's
shading. Hiding the whole layer would damage the original image; a whole-image
crop cannot remove the disk without also cutting the continuous broad wash behind
and beyond it. The integration owner chose to preserve these authored pixels.

Evidence is retained under each source's `detail/` folder:

- `Z1_01-on-gray.png`, `Z1_01-proposed-crop-review.png`.
- `Z1_02-on-gray.png`, `Z1_02-proposed-crop-review.png`.
- `Z3_03-on-gray.png`, `Z3_03-proposed-crop-review.png`.
- `Z3_02-on-gray.png`, `Z3_02-children-contact.png`, `Z3_02-children.json`.
- `Other_Crop_Review_Data.json` and `Other_Crop_Layer_Trace.json` record the
  inspection counts and exact native layer traces.

The file names retain “proposed” to preserve the inspection history; the final
manifest and mapping config record the adopted status. These are visual export
reviews, not claims that all remaining creatures were already reviewed in Unity.
