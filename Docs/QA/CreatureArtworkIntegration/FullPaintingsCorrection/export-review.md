# Full painting export correction

The first integration exported only a named organism group, cropped its nonzero
alpha and added transparent padding. That retained anatomy but discarded sibling
groups containing the painting's environment, foreground, lighting and film
frame. Using those cutouts in the survey photo therefore presented a different
composition from the PSD. Those files remain separate capture/cargo assets.

## Corrected source and export policy

The eleven existing creature IDs and source-artboard assignments are unchanged.
`Tools/Artwork/export_survey_paintings.py` now reads the original PSD's **saved
merged image** (`PSDImage.topil`, PSD Image Data), not an isolated group or a
reconstructed per-layer render. This preserves the artist's saved appearance,
including blend effects, masks, opacity and texture that a third-party live
compositor might interpret differently. The saved composite was visually checked
in all four source PSDs used by the eleven mapped organisms.

Each output is the entire original 888 x 1080 artboard rectangle. The film frame,
caption area, dark outer paper and any authored foreground remain. No crop by
anatomy/alpha, old reviewed cutout crop, resizing, recoloring, background removal,
new paint or changed layer visibility is applied. Standard embedded ICC decoding
to sRGB preserves the source's display appearance. Hidden reference artwork is
not made visible. The full multi-artboard strip is never assigned to a creature.

All PNGs are opaque (`alpha` range 255–255). This is expected for complete
paintings. Their background alpha must never be used for survey gameplay
measurement; runtime presentation has its own catalog field.

## Visual review of every source painting and exported PNG

The source native merged images and the PNG contact sheet were inspected for
every mapped board. The per-zone source/export comparisons preserve full framing.
Pixel verification independently reopens the PSD and requires raw RGBA equality
between each original artboard crop and its exported PNG.

| Creature ID | PSD board | Restored content visibly checked beyond the organism |
| --- | --- | --- |
| Z1_Creature_01 | remake board 1 | Yellow/teal watery environment, drawn seabed/kelp, foreground, lighting wash, paper/film texture and handwritten caption/date |
| Z1_Creature_02 | Zone1 second organism board 1 | Broad diagonal foreground seaweed, green/yellow background, illumination, film frame and two-line Vietnamese caption |
| Z2_Creature_01 | Zone2/3 board 1 | Cyan watercolor light and surface texture, seabed/mushroom outlines, film frame and dark lower paper |
| Z2_Creature_02 | Zone2/3 board 5 | Dark overhead vegetation, outlined seabed foreground, full purple/blue environment, original visible `con ma` group and film frame |
| Z3_Creature_01 | Zone2/3 board 3 | Layered purple/blue environment and lighting wash, watercolor texture, foreground and full frame |
| Z3_Creature_02 | Zone2/3 board 2 | Cyan/yellow light wash, lower foreground growth and sweeping environmental lines, watercolor texture and frame |
| Z3_Creature_03 | Zone2/3 board 4 | Purple watercolor background, warm dorsal illumination, foreground/seabed linework, additional visible pixel layers and frame |
| Z4_Creature_01 | Zone4 board 1 | Authored wreck/submarine and seabed lines, dark textured environment, white/blue lighting and frame |
| Z4_Creature_02 | Zone4 board 2 | Dark environment, lower debris/seabed detail and sweeping contour lines, cyan halo, paper texture and frame |
| Z4_Creature_03 | Zone4 board 3 | Bubbles, suspended violet lighting, lower debris and environmental contour lines, texture and frame |
| Z4_Creature_04 | Zone4 board 4 | Separate authored `bong bong` bubbles, environmental contour lines, lure/eye illumination, dark texture and frame |

The shared Zone2/3 file has no embedded creature IDs. Its earlier inferred mapping
is retained and remains explicitly described as inferred in the manifest. The
Zone4 hidden A/B assignments also remain inferred. This correction changes the
presentation scope, not those identity decisions.

## Reproduction and file checks

From the repository root:

```powershell
& '.tools\creature-artwork-venv\Scripts\python.exe' -X utf8 'Tools\Artwork\export_survey_paintings.py'
& '.tools\creature-artwork-venv\Scripts\python.exe' -X utf8 'Tools\Artwork\verify_survey_paintings.py'
```

`full-paintings-manifest.json` records source SHA-256/size, saved-composite RGBA
hash, full original artboard bounds, PNG hash/dimensions, and the complete layer
inventory outside the former creature group. Thus the layers omitted by the
first pipeline can be audited per painting. The previous reviewed subject crops
are recorded but explicitly ignored for these outputs.

`full-paintings-export-verification.json` confirms eleven fresh PSD crop/PNG pixel
matches and that the eleven existing cutouts and three reserve PNGs still match
their previous manifest hashes. This document records export validation only;
actual PHOTO, saved file, Lab/gallery, Continue and SEND validation are reported
separately by the Unity runtime fixtures.

Comparison files:

- `full-paintings-contact-sheet.png`
- `source-versus-export-Zone01.png`
- `source-versus-export-Zone02.png`
- `source-versus-export-Zone03.png`
- `source-versus-export-Zone04.png`

The pairs compare a full source-composite artboard crop on the left with the
runtime PNG read back from disk on the right. They are intentionally identical.
They are not Unity screenshots.
