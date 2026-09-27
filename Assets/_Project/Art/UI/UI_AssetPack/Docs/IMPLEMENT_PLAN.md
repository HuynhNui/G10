# Iron Lung-like UI Asset Pack — Implement Plan

## 1) Pack overview
This pack contains watercolor UI sheets and reference mockups for these gameplay screens:
- Zone Select Map
- World Map
- Radar
- Helm / Ship Control
- Camera

Important: several PNGs are **asset sheets**, so they must be sliced into smaller sprites before use.

---

## 2) Recommended Unity folder structure
```text
Assets/UI/
  Common/
    Bars/
    Buttons/
    Cards/
    Icons/
    Badges/
    Ornaments/
  Map/
  Radar/
  Helm/
  Camera/
```

---

## 3) Files in this pack
### Common
- `UI_Common_CoreSheet.png` — reusable bars/cards/buttons sheet
- `UI_Common_TopHUD_Reference.png` — preferred radar-style top HUD reference
- `UI_Common_IconsSheet.png` — icon sheet

### Map
- `UI_Map_AssetSheet.png` — map markers, point bubble, map panel parts
- `UI_Map_WorldMap_Mockup.png` — reference for world map final look
- `UI_Map_ZoneSelect_Mockup.png` — reference for zone select final look

### Radar
- `UI_Radar_Mockup.png` — latest desired radar screen style
- `UI_Radar_OlderMockup.png` — older reference, optional

### Helm
- `UI_Helm_Mockup.png` — helm screen reference (keep only top HUD style)

### Camera
- `UI_Camera_AssetSheet.png` — camera-specific asset sheet
- `UI_Camera_Mockup.png` — camera final look reference

### Ornaments
- `UI_Ornaments_Sheet.png` — decorative seaweed/coral/bubble/divider sheet

---

## 4) What to slice from each sheet

### A. `UI_Common_CoreSheet.png`
Slice these into separate sprites:
- Top HUD long bar base
- Small horizontal capsule panel
- Medium rounded panel / card
- Large rounded footer panel
- Square thumbnail frame / empty frame
- Save status strip / toast strip
- Capture footer base
- Camera shutter button states (normal / hover / pressed / disabled)
- Viewfinder corners / focus bracket if present
- Plus badge / focus badge if present

### B. `UI_Common_IconsSheet.png`
Slice each icon separately:
- Map
- Helm
- Radar
- Cabin / Home
- Camera
- Image / Photo
- Location pin
- Target / focus
- Energy
- Speed
- Depth
- Compass / heading
- Back arrow
- Close / X
- Confirm / check
- Lock
- Warning

### C. `UI_Map_AssetSheet.png`
Slice separately:
- Point marker default
- Point marker selected / active
- Point marker locked
- Selection glow / ring
- World map point info bubble
- Small map chip/button variants
- Optional CTA buttons

### D. `UI_Camera_AssetSheet.png`
Slice separately:
- Top camera header bar (if needed as reference)
- Photo count badge
- Empty textless badge
- Metadata card
- Capture footer
- Shutter button states
- 4 viewfinder corners
- Center crosshair
- Focus ring / target reticle
- Saved thumbnail frame
- Saved status strip / toast

### E. `UI_Ornaments_Sheet.png`
Slice separately:
- Left / right corner seaweed clusters
- Coral clusters
- Bubble clusters
- Horizontal dividers
- Tiny accent ornaments

---

## 5) What should be 9-sliced in Unity
Use Sprite Editor -> 9-slice for these:
- Top HUD bar base
- Small capsule panel
- Medium card panel
- Large footer / bottom bar
- Metadata card
- Photo count badge
- Capture footer
- CTA button bases
- Saved toast strip
- Point info bubble (only if center region is stretch-safe)

Do **not** 9-slice:
- Icons
- Markers
- Selection rings / glow
- Viewfinder corners
- Crosshair
- Coral / seaweed ornaments
- Bubble clusters

---

## 6) Text workflow
Prefer using **textless sprites** and render all text in Unity with TextMeshPro.
Suggested TMP objects:
- Top nav labels: `BẢN ĐỒ`, `BÀN LÁI`, `RADAR`, `CABIN / ESC`
- Camera title: `CAMERA · ZONE 1`
- Photo count: `ẢNH CÒN 19 / 20`
- Metadata: `X`, `Y`, `Z`, `HƯỚNG`, `KHU VỰC`
- Map point label: `ĐỊA ĐIỂM 01`
- Radar scan count: `LƯỢT QUÉT CÒN`, `0 / 10`
- Capture button: `CHỤP`

This makes localization and iteration much easier.

---

## 7) Screen-by-screen implementation notes

### Zone Select Map
Use:
- Common top HUD bar
- Map marker sprites
- Zone info card
- CTA button
Keep the watercolor map artwork as background and overlay only minimal UI.

### World Map
Use:
- Common top HUD bar
- Center title plate
- Selected point marker
- Point info bubble
Keep the contour/grid map visible; do not over-cover it with UI.

### Radar
Use:
- Preferred top HUD style from `UI_Common_TopHUD_Reference.png`
- Small scan count card near/below radar
Do not add large side cards; keep radar scene mostly unchanged.

### Helm
Use:
- Only the top HUD from common assets
Do not add left/right stat cards in the final helm version, per scope.

### Camera
Use:
- Top header bar
- Photo count badge
- Metadata card
- 4 viewfinder corners + center crosshair
- Bottom capture footer + shutter button states
- Optional saved thumbnail/status strip after shooting

---

## 8) Import settings recommendation
For each sprite in Unity:
- Texture Type: Sprite (2D and UI)
- Mesh Type: Full Rect
- Filter Mode: Bilinear (or Point only if you want crisp pixels, but watercolor usually prefers Bilinear)
- Compression: None / Low compression for UI
- Generate Mip Maps: Off
- Pixels Per Unit: keep consistent across the whole UI pack

For UI panels, use Canvas scaling:
- Canvas Scaler -> Scale With Screen Size
- Reference Resolution: e.g. 1920x1080

---

## 9) Suggested implementation order
1. Import Common sheet and slice reusable panels/buttons/icons.
2. Build the shared top HUD prefab.
3. Build Camera screen UI prefab.
4. Build World Map and Zone Select prefabs.
5. Build Radar screen prefab.
6. Build Helm screen prefab (top HUD only).
7. Add final TMP text and hook dynamic values.

---

## 10) Notes / caveat
These PNGs are much better than a single tiny overview sheet, but several files are still **asset sheets** rather than already pre-cut individual sprites.
So the expected workflow is:
1. import sheets,
2. slice them in Sprite Editor,
3. create prefabs from the sliced pieces.
