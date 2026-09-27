# Mission Config System

The mission runtime is data-driven and uses stable string IDs. Location array order is presentation-only and must never gate gameplay.

## Authoring

- Zone configs live in `Assets/_Project/Content/Missions/Zone01.asset` through `Zone04.asset`.
- Placeholder content lives under each zone's `Definitions` folder. Zone01 reuses its existing creature/item definitions.
- Run `G10/Missions/Build Configs and Placeholders` to recreate or refresh the authored configs without requiring final art.
- `poiId` may stay empty while a zone has no authored map. Assigning it later does not change saved objective IDs.

`Visible` locations are shown immediately. `HiddenRadar` locations exist and participate in radar contact queries immediately, but their map marker is hidden until a radar scan reveals the location.

## Runtime ownership

- `ZoneMissionRuntime` owns stable-ID objective, reward, reveal, gate, and ending state.
- `PhotoSurveyZone` owns POIs and daily contact positions. `FindPoi`, `FindPoiContaining`, and `FindContactContaining` resolve each interaction independently.
- Radar reports the scanned POI; Camera reports the framed contact; Capture/Collect reports the contact within reach. None of them reads an active location.
- `ZoneOneStory` only adapts the Zone01 Pressure Hull installation and legacy bitmask migration.

## Save migration

New saves use `ExpeditionZoneState.missionProgress`. On a legacy Zone01 checkpoint with no stable-ID progress:

- `PhotoOne` becomes `Z1_L1_PHOTO` and grants `Z1_PRESSURE_DATA`.
- `Tube` becomes `Z1_L2_COLLECT` and grants `Z1_ITEM_EMMA_BLUEPRINT`.
- `PhotoTwo` becomes `Z1_L3_PHOTO`.
- `Installed` becomes `Z1_GATE_INSTALL_PRESSURE_HULL` and unlocks `Zone02`.
- The retired bit `128` and cargo ID `Adhesive02` are ignored. Pressure Hull installation consumes no Adhesive.

## Zone gates

- Zone01: install `UPGRADE_PRESSURE_HULL` → unlock `Zone02`.
- Zone02: install `UPGRADE_BIO_LAMP` → unlock `Zone03`.
- Zone03: craft/install `UPGRADE_ROCK_BREAKER` and destroy `ROCK_BARRIER` → unlock `Zone04`.
- Zone04 normal and hidden endings are separate completion rules. Hidden locations L3/L4/L5 do not unlock one another.
