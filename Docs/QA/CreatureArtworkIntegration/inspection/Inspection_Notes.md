# PSD inspection (source audit, not final integration mapping)

All five input files passed the supplied SHA-256 and `8BPS` signature checks.
`Tools/Artwork/inspect_psd.py` opens the original PSD read-only and emits each source's
layer hierarchy, Photoshop merged reference, artboard references and selected native
group composites. `index.json` contains all five inspection records. Output pixels
come from the PSD; no image generation, chroma keying or automatic background removal
is used. Hidden artboards are explicitly marked in the hierarchy/contact sheets and
are inspection references, not proposed additional organisms.

## Native layer availability

| Source | Native export groups | Excluded decorative/reference content |
|---|---|---|
| `sinh vật 1 remake.psd` | `[0,2]` `sinh vật` | Separate `nền`, `tiền cảnh`, light effects, `khung`, film, old-film effects, caption; hidden Artboard2 is a color map |
| `sinh vật 2 khu vực 1.psd` | `[0,2]` `sinh vật` | Same separate environmental/frame construction; hidden Artboard2 is a color map |
| `SINH VẬT ẢI 2 VÀ 3.psd` | `[0,2]`, `[1,1]`, `[2,1]`, `[3,5]`, `[4,2]` `SINH VẬT` | Five separate artboards; each has its own `KHUNG`/film. Artboard5 also contains a separate `[4,1]` `con ma` group |
| `sinh vật và hù ma ải 4.psd` | Subjects `[0,3]`, `[1,2]`, `[2,2]`, `[3,2]`; wreck `[0,2]` `tau ngam,`; ghost `[4,1]` `ma` | All frames/film separate. Four subject paintings plus a fifth clearly distinct helmet/diver apparition |
| `SINH VAT 1.psd` | No single isolated `sinh vật` group | Original blue Singing Algae painting; hidden artboard of visual references; third artboard is explicitly captioned the Singing Algae heart/light item. Historical comparison, not a third organism |

Array paths are zero-based PSD child indices and do not assert creature numbering.
Each hierarchy records names, absolute source bounds, visibility, opacity, blend
mode, and text content. Export entries record exact viewport and resulting alpha
bounds. Artboards are 888 × 1080 for the remake/zone bundles; each isolated group
export retains that artboard viewport and native transparent pixels. Do not treat
the entire horizontal PSD canvas or filmstrip as one subject.

## Visual findings

- Remake01 shows a long warm ochre/teal ribbon-and-flower colony. It is consistent
  with the old long blue ribbon Singing Algae and should replace that creature,
  not create another ID. The old PSD's heart/light item remains historical context.
- Zone1 second shows an orange flower/shell-like shy organism near vegetation.
  Its PSD caption explicitly describes a shy species hiding behind seaweed.
- Zone2/3 Artboard1 is a long-snouted blue/teal organism. Its subject layers include
  `túi nhựa'` (bag) and `giap` (armor). Artboard2 is distinctly crystalline, cyan
  with yellow hard edges. Artboard3 is a translucent purple fish with red/pink
  internal folds. Artboard4 is a rounded purple arthropod with a glowing yellow
  dorsal structure. Artboard5 is a segmented/scaled armored organism with warm
  glowing fins.
- Zone4 Artboard1 contains its own wreck group, making the match to the existing
  L1 wreck photograph especially strong. Artboard2 is a long blue finned organism;
  Artboard3 is a purple glowing cluster above sediment; Artboard4 is a broad,
  asymmetric anglerfish with bright lure and jaw. Artboard5 is explicitly `ma`
  and depicts a helmet/diver apparition; it must not become a creature catalog ID.

Native subject groups still include original brush marks/glows. Zone2/3 Artboard1
has dangling paint marks; Artboard2 includes a broad blue glow/gradient and a small
isolated painted dot; Artboards3/4 include halos. Zone4 Artboard2 has a broad cyan
wash; Artboard4 includes its bubble painting. These are authored pixels, not film
or exporter-generated backgrounds. Isolated does not mean a pristine cutout.
Compare the group with the full artboard before deciding whether to retain all
halos or instead use the complete frameless painting. Do not key out colors or
erase faint marks automatically.

## Proposed semantic mapping for the ambiguous Zone2/3 bundle

This is a recommendation for the integration owner, not confirmed author intent.
No artboard in this bundle is labeled with a creature ID or individual zone.

| Artboard / subject index path | Proposed ID | Basis | Confidence |
|---|---|---|---|
| 1 / `[0,2]` | `Z2_Creature_01` | Bag/secretion layers and soft elongated organism suit the adhesive material role | Medium |
| 5 / `[4,2]` | `Z2_Creature_02` | Large overlapping external scales/plates suit hardened pressure-resistant shell material | Medium |
| 3 / `[2,1]` | `Z3_Creature_01` | Translucent body with pressurized-looking red internal folds suits existing pressure/explosive presentation | Medium |
| 2 / `[1,1]` | `Z3_Creature_02` | Explicit crystalline body suits existing `creature06_crystal_motion` presentation | Medium–high |
| 4 / `[3,5]` | `Z3_Creature_03` | Bright dorsal node/stack suits existing bioelectric/detonator presentation | Medium |

This mapping deliberately does not assign the first two drawings to Zone2 solely
by position. The production cues and mission material descriptions provide the
additional basis. IDs and mission IDs must remain unchanged. The final manifest
should record the integration owner's actual selection and this inference limit.

Zone4 proposed mapping is Artboard1 → `Z4_Creature_01` (strong wreck context),
Artboard2 → `Z4_Creature_02` (inferred), Artboard3 → `Z4_Creature_03` hidden A
(inferred sediment context), Artboard4 → `Z4_Creature_04` hidden B (inferred
asymmetric/deep silhouette context). The Zone4 ghost and Zone2/3 separate `con ma`
are reserve scare assets unless an existing compatible presentation slot is found.

## Runtime constraints discovered separately

The current subject alpha drives PHOTO coverage. Aspect or alpha changes require
an explicit separation between presentation art and original measurement inputs;
do not loosen thresholds. The existing catalog is the correct source of artwork
bindings. Photo Lab/SEND must keep displaying each photograph's baked PNG, including
old saves, rather than resolving new artwork from its creature ID on display.

The capture minigame currently uses one generic target image with collision sizes
independent of texture. Artwork may change inside its existing visual box without
changing the collider, movement, required hits or time. Cargo uses current catalog
images and already fits their aspect ratio. No Unity/runtime verification is claimed
by this inspection document.
