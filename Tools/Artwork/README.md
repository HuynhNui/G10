# G10 artwork export

## Lily 25 Days items, documents and menu

`export_lily_assets.py` exports the newly supplied Zone02/03 materials, Lily/Emma documents, coordinate paper and MainMenu PSD layers into `Assets/_Project/Art/Lily25Days/`. Native saved composites preserve item/document art; only menu Lorem ipsum and coordinate placeholder type layers are excluded where dynamic UI supplies the real content. The exporter checks the local source manifest and records all crops/checksums. Sources stay in `.tools/lily-artist-sources/`, outside Unity.

Run with the existing creature-artwork Python environment, then call `G10.Prototype.Editor.LilyArtistIntegrationSetup.Install()` in a clean Edit Mode through Unity MCP. It binds material presentation independently of encounter/PHOTO art and authors the existing MainMenu/shared cabin/Ending. It does not rebuild gameplay or change stable IDs. Full mapping, source Drive IDs, typography limitations and native evidence: `Docs/QA/LilyArtistIntegration/README.md`.

## Complete survey paintings (current PHOTO presentation)

The survey photo uses the **complete authored painting**, separately from the
transparent creature art used by capture/cargo. The earlier group-only exporter
below omits environmental and frame siblings and is not an appropriate source for
survey PHOTO presentation.

```powershell
& '.tools\creature-artwork-venv\Scripts\python.exe' -X utf8 'Tools\Artwork\export_survey_paintings.py'
& '.tools\creature-artwork-venv\Scripts\python.exe' -X utf8 'Tools\Artwork\verify_survey_paintings.py'
```

For the initial prototype, append `--only Z2_Creature_01` to the export command.
The existing `creature_mapping.json` supplies the same eleven IDs and source
artboard indices. Its subject group and reviewed subject-crop fields are recorded
for provenance but **never used to trim the full painting**.

`export_survey_paintings.py` verifies each source's original PSD signature, size and
SHA-256, then decodes the PSD Image Data section with `PSDImage.topil` rather than
reconstructing artist blend effects from isolated groups. This saved merged image
already contains the artist's complete visible stack, masks, opacity, lighting,
foreground, background, texture, film frame and captions. Its original artboard
rectangle is the only crop. Each output is exactly 888 x 1080, without trimming,
resizing, keying, manual recoloring or adding paint. Embedded ICC conversion to
sRGB preserves display appearance. Hidden reference boards remain hidden.

Outputs live in `Assets/_Project/Art/SurveyPaintings/`. Export manifest, saved
composite references, independent artboard crops, contact sheet and verification
report live in `Docs/QA/CreatureArtworkIntegration/FullPaintingsCorrection/`.
The verifier opens fresh PSD sources, extracts the original artboard crop and
requires byte-for-byte RGBA equality with every PNG. It also checks that all
eleven existing creature cutouts and three reserve PNGs remain unchanged.

These checks validate source-to-PNG fidelity. Actual Unity photo saving, gallery
and SEND behavior are verified separately in that report folder.

## Separate transparent creature art (capture/cargo)

The runtime PNGs come from the supplied artwork's native named PSD groups. No AI
generation, repainting, background keying, silhouette reconstruction or color
replacement is involved. PSDs remain local under `.tools/creature-artwork-sources/`
and are not runtime dependencies. `creature_mapping.json` is the single explicit
export mapping; Unity catalog bindings are maintained by the integration step.

## Reproduce

From `D:\G10`, with the existing isolated Python environment:

```powershell
& '.tools\creature-artwork-venv\Scripts\python.exe' -X utf8 'Tools\Artwork\inspect_psd.py'
& '.tools\creature-artwork-venv\Scripts\python.exe' -X utf8 'Tools\Artwork\export_creatures.py'
& '.tools\creature-artwork-venv\Scripts\python.exe' -X utf8 'Tools\Artwork\verify_exports.py'
```

The exporter accepts `--sources`, `--mapping`, `--output` and `--report` paths.
`requirements.txt` pins the three installed Python packages for a fresh environment.
Its default source manifest is `.tools/creature-artwork-sources/sources.json`,
which records Drive IDs, original names, absolute local paths, sizes and SHA-256.
Before exporting each source, it verifies its `8BPS` signature, byte size and
checksum. Input PSDs are opened from memory; they are never saved or modified.
To reproduce on another machine, download the same Drive IDs, verify their hashes
against `Docs/QA/CreatureArtworkIntegration/artwork-manifest.json`, and create a
local source manifest with those files' paths. Do not commit the large PSDs.

For just the initial Zone2 sample:

```powershell
& '.tools\creature-artwork-venv\Scripts\python.exe' -X utf8 'Tools\Artwork\export_creatures.py' --only Z2_Creature_01
```

A one-ID invocation writes a separate `export-result-ID.json` receipt and does not
replace the final all-creature manifest. A full invocation produces 11 organism
PNGs and 3 reserve PNGs, the final manifest and two labeled contact sheets.
Identical reruns do not rewrite unchanged asset bytes. The scripts never write
Unity `.meta` files or modify catalog, mission, scene or save data.

## Pixel and crop policy

Each selected group is composited with its original visible child layers, masks,
blend modes and opacity in its original artboard viewport. The artboard's own
clipping boundary is retained; brush strokes outside that artboard are not newly
revealed. Four entries have explicit manually reviewed source regions documented
in `creature_mapping.json` and `inspection/Crop_Review_*.md`: they exclude only
visually disconnected paint/swatch marks that otherwise impair subject framing.
There is no automatic erase or cropping within anatomy. `getchannel('A').getbbox()`
then finds every pixel with any nonzero alpha inside the selected region,
including faint watercolor marks. Only fully transparent outer pixels are
discarded. Exactly 24 transparent pixels are added around every side.

No resize, color grading, alpha threshold, sharpening, keying, anatomy crop or
noise cleanup is applied. The exporter asserts that the padded content is byte
identical to the native RGBA crop. The manifest records matching raw RGBA hashes,
source/render/alpha bounds, output dimensions, aspect ratio and PNG checksum.

The isolated groups contain some deliberately retained author pixels: broad
colored glows, paint dots and faint strokes outside the main anatomy. In
particular `Z3_Creature_02` retains a broad blue wash and a small dot;
`Z2_Creature_01` retains all body drips and halos while its two disconnected far
marks are outside the approved region. These were visible in the PSD layers and
inspection exports. They are not background-removal errors introduced by this
export. Their transparent bounds can make the main anatomy occupy less of its
canvas. Runtime presentation must fit the exported aspect without stretching;
photographic gameplay measurement must remain independent of new art alpha.

`Reserve/Zone04_HelmetGhost.png`, `Reserve/Zone02_03_Ghost.png` and
`Reserve/Zone04_Wreck.png` are not creature IDs. Exporting them does not add a
scare trigger. The native apparition installation below now binds the two ghosts
to existing story sequences. The older `SINH VAT 1.psd` remains comparison-only: its separate
glowing heart is a Singing Algae item, not another organism.

## Changing a mapping

Edit a creature's `source_id` and zero-based `index_path` in
`creature_mapping.json`, with the corresponding basis/confidence. Find exact
paths in the source's `inspection/DRIVE_ID/hierarchy.json`. Keep the existing
`creature_id`, `location_id`, `poi_id` and objective IDs intact, then rerun the
export and import/validate through Unity. The source grouping has no embedded
individual creature IDs for the shared Zone2/3 bundle, so those assignments are
explicit implementation decisions, not artist-confirmed identities.

## Unity import and catalog binding

`Assets/_Project/Scripts/Editor/CreatureArtworkImportTools.cs` is the repeatable
Editor-only import/binding step. Through the configured Unity MCP execute-code tool,
call `G10.Prototype.Editor.CreatureArtworkImportTools.BindPrototype()` for the
Zone02 sample or `BindAll()` for all eleven. `Audit()` lists the resulting catalog
references. These methods require Edit Mode and use Unity's AssetDatabase; they do
not edit scene, mission, navigation or save data.

Imports use standalone Single sprites, original non-power-of-two dimensions,
sRGB RGBA, no mipmaps, Clamp, Bilinear and uncompressed textures. The existing
catalog remains the runtime authority. Matching captured-creature material icons
use its sprite; distinct item illustrations stay separate. Reserve images are
never bound as creatures. The separate native-story installation binds their
presentation roles without changing objective or creature IDs.

## Native story apparitions

`NativeScareArtworkSetup.Install()` (menu `G10/Artwork/Bind Native Story Apparitions`)
requires the clean shared Zone01 cabin in Edit Mode and saves its bindings through
the Editor API. The supplied `con ma` group from the Zone02/03 PSD replaces the
temporary three-step Zone02 glass silhouette and the procedural Zone03 figure.
The supplied Zone04 `ma` helmet group appears on the Helm glass during the existing
final-signal sequence, from 1.2 to 2.1 seconds, alongside its existing signature
audio. The sequence still ends at 9 seconds. Existing eligibility, receipts,
audience cancellation and audio clocks remain authoritative.

The Zone03/04 composition module samples the native RGBA texture with aspect-fit,
original RGB and opacity envelopes. Other procedural modes retain their behavior.
Zone02 preserves the native sprite colors and bypasses the old grayscale/RGB-shift
treatment only for these authored reflection definitions. Apparitions never enter
the photo repository, radar contacts or mission catalog. The wreck is already
inside the complete Zone04 L1 PSD painting and needs no duplicate overlay.

Current mapping, real-shutter evidence, natural trigger tests and fixture limits:
`Docs/QA/ArtworkCompletion/README.md`.

The initial group-art binding preserves each old survey mask: Zone01 keeps its prior image,
and Zones02–04 keep their existing profile/distance fallback. Rebinding changes
presentation without replacing those masks. The photo compositor fits artwork in
pixel aspect while measuring the original projected footprint. It does not change
GoodPhoto thresholds or depth/bearing/occlusion rules. The complete-painting
correction uses a separate catalog field for survey presentation; it must never
turn the full background into a gameplay measurement mask. Lab reads the baked
photo PNG, not a fresh catalog lookup.

Capture encounters fit the catalog image into the existing visual box and use full
UVs, independently of the old generic texture's atlas crop. A default encounter
restores its original texture and UVs. Collision and movement profiles are unchanged.

## Deliverables

- `Assets/_Project/Art/Creatures/Finished/Z*_Creature_*.png`: runtime content.
- `Assets/_Project/Art/Creatures/Finished/Reserve/`: native environmental and apparition groups.
- `Docs/QA/CreatureArtworkIntegration/artwork-manifest.json`: final mapping and
  exact source/output provenance.
- `Docs/QA/CreatureArtworkIntegration/creature-contact-sheet.png`: all 11 IDs.
- `Docs/QA/CreatureArtworkIntegration/reserve-contact-sheet.png`: environmental
  and scare reserve, clearly distinguished from organisms.
- `Docs/QA/CreatureArtworkIntegration/inspection/`: source/group visual evidence.

The manifest records actual installed `psd-tools`, Pillow and numpy versions.
The PNGs and contact sheets were visually inspected. Source inspection/export
alone is not a claim that the artwork was viewed in the running Unity game.
