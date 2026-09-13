# Computer system — Phase A/B

## Scope and inspection (2026-09-09)

Brief read in full: `G:/downlaod/COMPUTER_SYSTEM_IMPLEMENTATION_BRIEF.md`.
User authorized only Phase A (shell) and Phase B (three apps). Coordinate foundation,
navigable polygons, spawn regions, camera and capture remain deferred.

The existing scene flow is Bootstrap → MainMenu → GameplayCore + Zone01.
GameplayCore owns the camera, EventSystem with InputSystemUIInputModule, UIManager,
and collider-based PointAndClickInputController. Zone01 owns Cabin Station and its
uGUI cabin, helm, map and radar panels. The prefab folders contain no functional
prefabs; this implementation follows the existing editable scene-object pattern.

CabinStationView handles panel selection and helm input. ZoneNavigation owns X/Y,
heading, speed and collision queries. RadarDisplay owns scan timing and renders
terrain from ZoneNavigation; there is no radar-use budget/counter, energy model,
photo repository, creature entity system or capture implementation.

Input comes from the existing InputSystem_Actions asset, including NavigateShip.
The central MonitorHotspot previously opened a camera placeholder. It now opens
the computer. Physical radar, map, helm, backpack and capture hotspots remain separate.

## Conflict resolved for Phase A

The old CabinStationView applied Brake whenever the helm was closed. That directly
conflicted with the brief's background-voyage rule. Open/close now releases held
UI input without braking; outside the helm, ZoneNavigation.Coast advances at the
existing speed and heading through the same collision checks. Returning to the
helm resumes its existing acceleration/deceleration and turn inputs. DỪNG still
brakes immediately. Scene unload still brakes. No timeScale change is introduced.

The computer is a modal panel in the current UIManager. Its opaque raycast surface
blocks cabin UI and the manager already suppresses world interactions. Escape is
delegated to the active panel: app → desktop → cabin. Other panels retain their
existing Escape-to-close behavior. EXIT always returns to the cabin.

## Deferred coordinate blockers

- Current coordinate conversion contains constants calibrated to the 1672×941 map.
- The movement bound is currently X 0–1200 / Y 0–780, while the brief proposes Y 0–700.
- Collision uses a serialized 320×180 occupancy mask baked from map pixels in the
  Editor. Runtime does not read image pixels, but gameplay data still derives from art.
- No independent world/map coordinate service, authored navigable polygons,
  species spawn regions or true creature radar targets exist.

These are Phase C/D work, not completed foundation. Camera/capture must wait.

## Editor workflow

Scene changes use Unity APIs invoked through native Editor menus, with Undo;
scene YAML is not edited directly. Existing art references were assigned using
Inspector drag-and-drop. Source art remains in AssetInbox, imported PNG composites
in Assets/_Project/Art/PrototypeCabin.

`G10 > Computer > Install Phase A Shell` adds the shell to the loaded Zone01 once.
Save Zone01 after installation. Do not use the older global scene rebuild menu:
it regenerates the project's scenes and would replace authored content.

## Files

- Scripts/Computer: shell controller, app views and data contracts/providers.
- Scripts/Editor/ComputerStationEditor.cs: additive, Undo-aware installation.
- Scripts/UI/UIManager.cs: optional Escape routing to modal panels.
- Scripts/UI/CabinStationView.cs: monitor entry point and background voyage integration.
- Scripts/Navigation/ZoneNavigation.cs: Coast using the existing collision integrator.
- Tests/PlayMode/ComputerShellPlayModeTests.cs: shell, input blocking and voyage tests.
- Tests/PlayMode/CabinNavigationPlayModeTests.cs: existing navigation regression coverage.

Paths above are relative to Assets/_Project.

## Phase B data boundaries

- PHOTO LAB uses IPhotoRepository with EmptyPhotoRepository. It returns no records
  and CameraOnline=false. PhotoRecord reserves image, thumbnail, timestamp,
  map-coordinate metadata and mission-photo flag for a future real camera provider.
  Phase B renders only the empty state; gallery browsing remains future work.
- SHIP STATUS uses IShipStatusProvider with ExistingShipStatusProvider. It references
  the same RadarDisplay as the physical radar and reads IsScanning. No second scan
  timer or use counter is added. Nullable energy, drain, radar-use and warning values
  render as `--`; absent modules are OFFLINE / NOT INSTALLED. It polls only while open.
- MISSION LOG uses IMissionProvider and ZoneMissionProvider, backed by
  `Assets/_Project/Data/Computer/Zone01Mission.asset`. The asset contains zone, objective,
  status, checklist, optional target coordinates and a preview/integration notice.
  Default mission and all steps are LOCKED. Editing it in Inspector changes the view
  when reopened. There is no fake progress trigger and no save system.

Use `G10 > Computer > Install Phase B Apps` after installing Phase A. Installation
is additive and refuses to overwrite existing app views. The sample mission asset
is created only if absent. All panels and source references remain Inspector-editable.

The terminal uses an installed monospace font (Consolas / Liberation Mono / Courier
New), falling back to the Unity font if unavailable. No CRT animation or new artwork
is needed for these phases.

## Validation

- Phase A: 6/6 Play Mode tests passed in the running Unity 6000.4.2f1 Editor.
  Includes existing navigation and scene flow, persistent monitor wiring, exactly
  three apps, exclusive app activation, BACK/EXIT, Escape through actual Input System
  events, foreground UI raycast, continued voyage, unchanged scene count/timeScale,
  disabled helm commands inside computer and physical radar regression.
- Phase B: passed in the full 8/8 Play Mode suite on 2026-09-11 in Unity
  6000.4.2f1. Checks cover the empty photo repository, shared real radar state,
  unavailable telemetry, mission configuration and provider-driven display states.
  Native Editor visual checks covered all three apps and return navigation.
