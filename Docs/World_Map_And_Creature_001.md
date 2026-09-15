# World Map and Creature 001

## World Map

Open Map from the cabin or any station shortcut to show **MAP TỔNG** first, then choose a zone to open its detailed map. Inside a zone, **MAP TỔNG / ESC** returns to World Map. The full-color `Assets/_Project/Art/Environment/Map/Map.png` is the game artwork. Four editable polygon hotspots are hand-traced from the region annotations in `Mapline.jpg`; the annotation itself is never displayed in game.

Temporary region numbering follows the supplied drawing: Zone01 = upper-left yellow region; Zone02 = central turquoise region; Zone03 = right-hand ruins; Zone04 = lower vortex. Zone01 opens the existing detailed map. Zone02–04 open explicit unavailable-map panels until their art/UI is authored; they do not trigger scene travel.

Hover fills the region with a translucent highlight and shows its name. Back/Escape from a zone map returns to the world map; back/Escape from the world map closes to the cabin. The **MỞ LẠI KHU VỰC** button returns to the most recently opened zone. Clicking a location on the Zone01 chart selects it; selection and its task readout are restored when reopening that chart from World Map. State lasts for the current scene/session, with no disk save or invented pan/zoom system.

Runtime: `WorldMapController`, `WorldMapZoneHotspot`, `ZoneMapBackHandler`. The existing `PhotoSurveyMap` retains selection and `CabinPointerTarget` handles marker clicks. No scene-flow or creature-catching logic is replaced.

Installer: **G10 → Zone 1 → Install World Map UI**. It adds UI to the existing cabin and skips an already installed controller, preserving authored edits. Edit each hotspot's `polygon`, `zoneLabel`, and `highlight` in the Inspector; `zoneMaps` on the controller holds each zone's target panel.

## Creature 001

Source: `Assets/_Project/Art/Sprites/Creature/Zone1/001.png`, unchanged. Its byte content matches the existing `Item1.png`; the separately authored `Item1_real.png` remains the caught-item icon.

Outputs under `Assets/_Project/Art/Sprites/Creature/Zone1/Processed/`:

- `Creature001_Clean.png`: isolated living organism, transparent RGBA.
- `Creature001_Silhouette.png`: dark silhouette with the same alpha and bounds.

The existing `Zone01CaptureProfile` references Clean for both normal/close views and Silhouette for distant views. Readable textures are required by the existing CPU photo compositor; these imports use uncompressed RGBA, no mipmaps, alpha transparency, clamp wrapping, and max size 1024. The original caught-item icon is a different, intentionally retained depiction of the captured organism.

The built-in ImageGen tool extracted the organism; its RGB output had a painted checkerboard. `CreatureArtPrepTool.Process` converts that neutral matte to actual alpha, retains colored tendrils, crops with padding, and derives the silhouette. This is a subject-specific blue/pink chroma cleanup, not a general background-removal algorithm. The AI-assisted extraction is not pixel-preserving: the silhouette follows the source, with brighter color and some reconstructed line details. Raw art remains available for further hand refinement.

The extraction intermediate is `AssetInbox/Creature001_Extraction.png`. Reprocessing is explicit through `CreatureArtPrepTool.Process(path)`. **G10 → Zone 1 → Install Clean Creature 001** imports/assigns existing outputs and only processes the intermediate when Clean is absent.

ImageGen prompt used: “Use case: background-extraction. Edit the supplied watercolor creature illustration into a usable transparent PNG game sprite. Preserve this exact single branching blue/turquoise aquatic organism, with its curled pink and peach flower-like fins, its twisting long blue body, delicate antennae and trailing roots/tendrils. Preserve hand drawn blue linework, watercolor colors, anatomy and original pose. Remove ALL environment: the blue canyon rocks, horizontal contour lines, stone ledges and blue background washes, including environment lines currently connected to the animal silhouette. Reconstruct only the short interrupted creature contour where required. Keep slender creature tendrils, do not confuse background rock outlines for tendrils. Subject fully inside canvas with slight clear padding; genuinely transparent alpha background, no checkerboard, no white background, no ground, no shadows, no text. Output one clean isolated creature, no redesign.”

## Deferred minigame

No minigame runtime or catch-result behavior is changed in this session. See `Capture_Minigame_Asset_Checklist.md` for required/optional art and export guidance.

## Validation

Unity 6000.4.2f1 imports and builds the scene in an isolated validation copy because the live Editor has no MCP connection. Photo capture tests use a separate validation company name so they do not write into the user's photo archive; that project setting is not copied back.

`WorldMapPlayModeTests` covers real UI raycast eligibility, hover reset, correct target panel, marker selection restoration, most-recent zone, back flow without scene changes, actual alpha coverage, profile references and capture through PhotoCaptureService. Generated previews and XML reports are saved under `Logs/WorldMapAndCreature/` in the main workspace after validation.

### Hover polish

WorldMapZoneHotspot now fades in/out over 0.2 seconds, keeps the focused region at its original brightness and shades the surrounding map black at 72% opacity, with a rounded feathered boundary. Only one region owns the shade, preventing stacked dark overlays when crossing between regions. The original polygon still controls clicks. Tune Fade Duration, Surrounding Dim Opacity, Outline Opacity, Outline Width and Feather Width under Hover Appearance on each hotspot. Verified in the live Unity Game view with Computer Use: Zone01/Zone02 hover, Zone01 entry, and return to World Map.


