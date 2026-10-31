# Zone04 static terrain chart implementation

Implemented after the locked baseline, audit and `03_Horror_Redesign.md`. This is the approved chart readability treatment. The diagnosis establishes physical reachability and aligned source data; this change does not claim or repair an impossible route.

## Source scope

- `Assets/_Project/Scripts/UI/ZoneMapPresentation.cs`: adds a runtime child RawImage plus a compact header control in the existing map panel. No new scene or authored asset is required.
- `Assets/_Project/Scripts/Navigation/ZoneMapConfig.cs`: one narrow `TryCopyTerrainMask(out byte[] cells, out int columns, out int rows)` read API. It validates positive dimensions, integer bounds and exact RLE run length before using the existing decoder. Its returned array is detached. Serialized fields, storage schema and existing terrain application remain unchanged.

The existing serialized hierarchy and `ZoneMapEditor`/`WatercolorUIEditor` builders were inspected before placement. Map art is the parent RawImage with stretched decorative lights and a stretched `PhotoSurveyOverlay` child. The header has an unused right-hand area beside the coordinate readout. The new layer is placed after lights and before the existing overlay; no existing object is rebuilt or removed. Header position is within that authored spare area, preserving the coordinate readout, MAP TỔNG and station HUD.

## Presentation and data contract

Only `config.zoneId == "Zone04"` builds the layer. It starts enabled, preserves that local toggle choice when the panel is closed/reopened, and exposes the ordinary uGUI Button action `ĐỊA HÌNH: BẬT` / `ĐỊA HÌNH: TẮT`. The nearby legend reads `Vàng nhạt: vật cản`. Labels use the existing map coordinate label's Vietnamese TMP font (existing panel TMP/default font only as a missing-reference fallback).

The texture uses the base authored collision-mask cells directly in Unity bottom-left order. Zero cells alone receive pale gold: RGB 242/222/153, alpha 238 on the blocked-side cardinal contour, alpha 50 in the remaining blocked interior. Every nonzero cell is fully transparent, including water enclosed by walls. There is no dilation, Radar region filling, gap closing, terrain query, route computation or source-image classification. Point filtering, Clamp wrapping and no mipmaps preserve exact cell membership. Radar itself is unchanged.

The terrain RawImage stretches over the map RawImage's exact rectangle and copies its UV rectangle, including a non-default crop/flip. It inherits the map transform without copying decorative light scaling. The underlying art remains present. The layer, legend and label graphics do not receive raycasts; only the normal toggle Button receives them. Existing map pointer, grid, POIs, completion icons and hidden-destination rules remain owned by the existing components. No vessel marker, new destination, route or current-position read is added.

One texture (or failed-source result) is cached per exact `ZoneMapConfig` reference for the presentation's lifetime. Refresh and toggling reuse it; there are no per-frame mask decodes, texture allocations or terrain queries. `OnDestroy` removes the owned button listener, clears the RawImage reference, destroys owned runtime objects/textures and clears the cache. Disabling/reopening keeps the same cached texture.

## Native inspection APIs

`ZoneMapPresentation` provides read-only `TerrainEnabled` (toggle intent), `TerrainVisible` (active image), `TerrainMapConfig` (exact source binding), `TerrainImage`, `TerrainToggleButton`, `TerrainToggleLabel`, `TerrainLegend`, `TerrainTextureBuildCount`, and `TerrainCachedTextureCount`. `SetTerrainVisible(bool)` and `ToggleTerrainVisibility()` are UI intent actions only. These APIs do not expose or alter navigation/gameplay state.

## Validation and seal

Filesystem review and `git diff --check` passed. This implementation agent did not call Unity, import assets or run tests. Native compilation, cell/UV assertions, pointer interactions, lifecycle tests and actual rendered readability remain with the root/native-test workflow; no runtime result is claimed here. Final navigation must be planned from newly rendered UI, not from the private diagnosis.

Initial sealed source SHA256:

- `ZoneMapPresentation.cs`: `DE349BF5D711601C1EA5E7E2F64AAF9E9A8DA6D995375AFECE6C657792FD1CE1`
- `ZoneMapConfig.cs`: `C91ACB27C6FD43DE9524874A5881B254E4F57EB181EC8455D158758048C1726F`

No scene, map/mission asset, collision, navigation, cost, charge, hidden marker, gate, geometry or source image was changed by this implementation.
