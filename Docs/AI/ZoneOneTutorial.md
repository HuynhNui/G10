# Zone01 first-time tutorial

Base: `main` / `15f5aa644602bdcd9badf963ef2349c9b36b8d2f` (Photo Lab → SEND).

## Ownership and integration

`TutorialManager` exposes cumulative station/app permissions and persists learned/presented step IDs through `ExpeditionLoop`. `ZoneOneTutorialDirector` observes real navigation, panel/app state, radar resources, photos and mission objectives. `TutorialConfig` holds guide text and validated thresholds, never player state. Existing `DialogueController` renders transient Guide/AI lines; its implementation is unchanged.

The existing Cabin Station in Zone01 has one manager/director pair. The shared cabin can represent later logical zones: permissions only apply while `ExpeditionLoop.Zone == "Zone01"`, initialization is complete, and tutorial knowledge is incomplete. Missing configuration or disabled components fail open. Cleanup cancels only the director's own dialogue and removes event subscriptions. References are cached; practical observations run at the configured low-frequency interval.

Cabin `OpenNavigation`, `OpenMap`, `OpenRadar`, `OpenCamera`, `OpenCapture`, `OpenComputer`, `OpenCargo` are the station choke points. `ComputerScreenController.OpenApp` is the app choke point. Closing/back navigation is never tutorial-gated. Rest/Journal are available for exhausted resources/recovery and after Photo Lab is learned; existing failure UI remains accessible.

## Save v4

`ExpeditionSave.tutorial` is a root-level `TutorialProgressState` with version, completion, learned IDs and presented IDs. It is deliberately absent from `ExpeditionSnapshot`, day-start and journal checkpoints. Existing rollback copies the root while replacing only the snapshot, retaining knowledge without changing death logic.

Versions 1–3 migrate as already taught if any reasonable progression evidence exists: later zone/day, mission IDs or legacy flags/tasks, photos, inventory, journal, progression flags, or voyage movement/depth/heading away from the old authored start. An empty or untouched historical start stays fresh. Historical coordinate versions are considered only for migration; gameplay spawns always use `ActiveMap`.

New Game clears the expedition but preserves all tutorial knowledge. Only `G10/Tutorial/Reset Tutorial Progress` explicitly clears it; this does not erase the expedition. Existing corrupt-save handling is retained rather than silently discarding unreadable knowledge.

## Flow

Intro text → Helm (move ≥20, turn ≥15°, change depth ≥5) → Map opened → Radar charge spent → valid unsent mission photo → Photo Lab opened and actual SEND/submission → real `Z1_L2_COLLECT` → wait for real Pressure Hull fieldwork/recipe → real `Z1_GATE_INSTALL_PRESSURE_HULL` → immediate persisted completion, unrestricted access, final AI dialogue.

Text completion/Skip starts practice, not mechanic completion. Cancel leaves presentation pending. A presented instruction can resume practice after reload without repeating text. L3 uses the already-unlocked camera and Photo Lab; no separate instruction loop is created. The tutorial grants no objectives, items, charges, upgrades or rewards.

## Authored setup and QA

Config: `Assets/_Project/Data/Tutorial/ZoneOneTutorialConfig.asset`.

Open Zone01 and run `G10/Tutorial/Install Zone01 Tutorial` if repairing wiring. Repeating the command reuses the existing asset/components; save the scene afterwards. The install was run twice and verified to have one manager and one director.

Zone01 map entry alone changes from `(960,154)` to `(100,400)`; heading 90 and depth 230 remain. Terrain-mask validation confirmed `CanOccupy((100,400)) == true`. Existing `hasVoyage` positions are restored, not replaced.

## Validation

New fixtures: `TutorialSaveTests`, `TutorialPlayModeTests`. Regression fixtures seed a completed tutorial for returning-player gameplay; fresh onboarding is tested separately. Their actual gameplay assertions are retained, except the save version assertion now uses `CurrentVersion`.

Unity compilation reported zero errors. Focused PlayMode run `7565825b2097`: **20 passed, 0 failed, 0 skipped** (54 seconds), comprising 14 save/config cases and 6 runtime scenarios. This validates fresh spawn/navigability, initial/cumulative/direct-event/app gates, Skip/Cancel/disable/rebind, real helm thresholds, actual Map panel, radar consumption, bad/candidate/submitted photos, real collection, L3 independence, actual upgrade and synchronous/idempotent completion, death/reload knowledge, saved position, New Game/debug reset, resource recovery and unrestricted Zone02–04 with incomplete knowledge.

The initial request timed out while Editor was blocked; it was restarted after Editor responded. Early focused runs found a real integration mismatch: Helm embeds a `PhotoSurveyMap` minimap, so searching children for that component incorrectly counted Helm as opening Map. The director now checks actual map-panel identities. A separate test timing issue was fixed by respecting the existing camera cooldown between shots; production photo behavior was not changed.

The eight requested broader regression fixtures finished as job `47fe82ef0d7f`: **45 passed, 0 failed, 0 skipped** (184.24 seconds). This includes the normal ending, hidden ending/reload, and real camera/capture/upgrade/radar route across all four zones. Total final validation: **65/65 passed**, zero compilation errors.

`git diff --check` passed after removing Unity-generated trailing whitespace from Zone01. The scene's semantic diff is only the two tutorial components and config reference. Final Editor inspection found exactly one manager, one director and a valid config. The temporary additive Zone01 scene was closed only after confirming it was clean, leaving the original Bootstrap scene open.

Unity also populated dynamic font caches in `Assets/_Project/Art/UI/Fonts/AlegreyaSansSC-Regular SDF.asset` and `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset` during scene/test rendering. These generated cache changes are retained under the Unity workflow's asset-preservation rule, not reverted. They are not a font/design change, but they are additional modified assets in the handoff. No commit or push was made.

## File inventory

Created (each asset/script below also has its Unity-generated `.meta`):

- `Assets/_Project/Data/Tutorial/ZoneOneTutorialConfig.asset`
- `Assets/_Project/Scripts/Editor/TutorialEditor.cs`
- `Assets/_Project/Scripts/Tutorial/TutorialConfig.cs`
- `Assets/_Project/Scripts/Tutorial/TutorialManager.cs`
- `Assets/_Project/Scripts/Tutorial/TutorialProgressState.cs`
- `Assets/_Project/Scripts/Tutorial/TutorialStepId.cs` (also declares TutorialStation)
- `Assets/_Project/Scripts/Tutorial/ZoneOneTutorialDirector.cs`
- `Assets/_Project/Tests/PlayMode/TutorialPlayModeTests.cs`
- `Assets/_Project/Tests/PlayMode/TutorialSaveTests.cs`
- `Assets/_Project/Tests/PlayMode/TutorialTestSave.cs`
- `Assets/_Project/Data/Tutorial.meta`
- `Assets/_Project/Scripts/Tutorial.meta`
- `Docs/AI/ZoneOneTutorial.md`

Modified:

- `Assets/_Project/Content/Maps/Zone01.asset`
- `Assets/_Project/Scenes/Gameplay/Zone01.unity`
- `Assets/_Project/Scripts/Computer/ComputerScreenController.cs`
- `Assets/_Project/Scripts/Computer/ExpeditionLoop.cs`
- `Assets/_Project/Scripts/Computer/ExpeditionSave.cs`
- `Assets/_Project/Scripts/UI/CabinStationView.cs`
- `Assets/_Project/Tests/PlayMode/DirectCabinInteractionPlayModeTests.cs`
- `Assets/_Project/Tests/PlayMode/ExpeditionEndToEndPlayModeTests.cs`
- `Assets/_Project/Tests/PlayMode/ExpeditionNewGamePlayModeTests.cs`
- `Assets/_Project/Tests/PlayMode/PhotoSubmissionPlayModeTests.cs`
- `Assets/_Project/Tests/PlayMode/VesselDeathPlayModeTests.cs`
- `Assets/_Project/Tests/PlayMode/ZoneOneStoryPlayModeTests.cs`
- `Assets/_Project/Art/UI/Fonts/AlegreyaSansSC-Regular SDF.asset` (Editor-generated dynamic cache)
- `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset` (Editor-generated dynamic fallback cache)

The separately mentioned knowledge-transfer document was not present in the attachment folder. The explicit architecture in the supplied request was used. No separate scene, fake run, spotlight, artwork, mission definition, gameplay balance, photo/capture/radar implementation, ending logic or DialogueController core change was introduced.
