# Unity Project Context

## Current integration status — 2026-10-24

Zone4 final chapter is installed on the existing shared cabin and Ending scene. It adds original sparse abyss audio, a distant wreck baked into the real L1 photograph, recovered coordinates in the actual Mission Log, distinct hidden A/B photographic environments and first-observation audio, a durable-SEND 09/09 notice, physical final-approach presentation and independent 12-second normal/16-second hidden ending sequences. Quick shutter → Photo Lab → manual SEND remains the only photo flow. Presentation never grants objectives, capture cargo, hidden discoveries or arrival.

Native final selected PlayMode job ba233e51b04e: 82 passed, 0 failed, including all17 distinct Zone4/ending cases and current four-zone regression. Entire EditMode job9cf7887efdec: 103 passed, 0 failed, 1 optional Kino skip. This is not an entire-repository PlayMode or standalone player-build certification. Actual screenshots, raw listener WAVs, exact selected results, initial fixture/visual corrections and human-review boundaries are in Docs/QA/Zone04FinalChapter/results.md; implementation/reproduction is in Docs/Zone04FinalChapterProduction.md.

Mission/map Zone04 bytes match task-entry snapshots. The canonical9 mandatory +2 hidden layout, mandatory captures, intentional zone04-l5 POI, savev6, normal/explore choice and physical final-arrival authority are preserved. New photograph totals count canonical PHOTO receipts, including historical completed photos whose images were deleted. No package, original illustration or preexisting local refactor was replaced. Integration was validated from main base3862225 plus retained local work. Publication to main was explicitly authorized after acceptance; consult Git HEAD for the published revision.

Important art/license boundary: all four Zone4 creature definitions still have null illustration/sprite/prefab bindings and use the existing generic photographic fallback. Environmental presentation is implemented; distinct creature illustrations are not claimed. Zone4 audio used530 actual successful-header ElevenLabs credits (590 with conservative failed-request reservation), 13attempts/54.5requested seconds, no top-up. Sources were generated under the recorded Free noncommercial/attribution terms. Human headset/aesthetic review remains necessary.

Editor is stopped on clean Bootstrap. Final native audit:0 missing scripts,0 duplicate catalog IDs, all final bindings present, separate cabin/ending directors. Dynamic font caches were restored to exact task-entry snapshots, preserving their source/atlas configuration and preexisting changes. TimeManager and Zone04 mission/map also match entry bytes.

## Previous integration status — Zone3, 2026-10-23

Native integration is complete on G10:7890. The existing shared Zone01 cabin is saved with canonical mission/map/marker bindings and Zone3 production:18 real WAV clips,30 unique effect definitions,6 sequences. Native C#/shader errors are zero; zero missing scene scripts. Procedural graphics use the real instrument/window audiences. Quick shutter → Photo Lab → manual SEND is the current flow; the live Camera panel remains inactive.

Native EditMode job1e9f8cd54b9c:98passed,0failed,1optionalKinoskip. Latest results across50distinct selected PlayMode cases:50passed,0failed, including all12Zone3 cases. Source runs2dcef966a072,75340202b238 and1f586e60f70e are retained separately; this is an aggregate of selected regressions and targeted reruns, not a single entire-repository run. PHOTO/SEND across11sites, physicalZone4 travel, normal/hidden endings, craft/install costs, rollback,Continue/rest/Journal, natural radar/reflection triggers and saved no-replay receipts pass. Actual GameView and AudioListener evidence, human-review boundaries and performance limits are in Docs/QA/Zone03Cinematic/results.md.

Editor left stopped on clean Bootstrap. Dynamic font caches were restored to exact pre-install copies, preserving preexisting local changes. Bootstrap's serialized bytes were unchanged by clearing its Editor dirty flag. The old policy/modal blockers are resolved. Use AnkleBreaker unity_* tools for Editor interaction; never bypass its bridge or queue.

Main HEAD3862225 retains substantial uncommitted Zone1/2 audio, quick-photo and presentation work. Mission source was selectively reconciled with local codex/unified-effect-director43b1a26, preserving newer local work. No branch switch/commit/merge/push occurred. Do not replace local files wholesale from that older branch.

Canonical layout:2/2/3/4 sites,9mandatory organisms plus2hidden. Zone3 retains zone03-l1/l2/l4 mapped to Z3_L1/L2/L3. Creature05/07 materials are consumed once at CRAFT; INSTALL consumes neither again. Original1.5s terrain reveal,10s gameplay stabilization and physical exit stay authoritative. Savev6 migrates current/day-start/Journal/photos/legacy lamp cargo, preserving historical route entitlement separately from genuine capture receipts. Current Zone2 depths280/460 and radius20 take precedence over historical reference geometry.

Docs/AI/MissionLocationsRefactor-2026-10-20.md records reference provenance/historical totals; current local geometry and native results take precedence. Detailed synchronization:Docs/QA/MissionSynchronization/report.md. Production/reproduction:Docs/Zone03CinematicProduction.md. No packages or original creature art were replaced.

The generated onboarding snapshot below describes an older September state. Its missing persistence/tests/tooling findings are historical and superseded by this context.

<!-- unity-onboarding:generated:start -->

## Project Summary

- Project root: `D:\G10`
- Last analyzed: 2026-09-04
- Last analyzed commit: `a0599fe6645be70f0490d3dd6b65a670fd0f6fa0`
- State: Unity 2D point-and-click project with a persistent scene-flow controller, additive gameplay zones, imported room/character art, and automated Play Mode coverage.

## Confirmed Environment

- Unity version: Unity 6.4 (`6000.4.2f1`, revision `7a4c1aeef971`).
- Render pipeline: Universal Render Pipeline 17.4.0 with a configured 2D renderer asset.
- Input system: Unity Input System 1.19.0 only (`activeInputHandler: 1`).
- Target platforms: Unknown; no repository-owned build target documentation was found.

## Important Packages And Frameworks

| Area | Finding | Confidence | Evidence |
| --- | --- | --- | --- |
| Rendering | URP 17.4.0 and Unity 2D packages | Confirmed | `Packages/manifest.json`, `ProjectSettings/QualitySettings.asset` |
| Input | Input System 1.19.0; project action asset has Player and UI maps | Confirmed | `Packages/manifest.json`, `Assets/InputSystem_Actions.inputactions` |
| UI | uGUI 2.0.0 | Confirmed | `Packages/manifest.json` |
| Testing | Unity Test Framework 1.6.0 installed with project Play Mode tests | Confirmed | `Packages/manifest.json`, `Assets/_Project/Tests/PlayMode` |
| Networking | No first-party networking usage found | Confirmed | package and source scan |
| Unity Version Control | Collab/Plastic proxy package installed, but local Plastic metadata is ignored by Git | Confirmed | `Packages/manifest.json`, `.gitignore` |

## Directory Structure

| Path | Purpose | Confidence | Evidence |
| --- | --- | --- | --- |
| `Assets/_Project/Art` | First-party room and character source art | Confirmed | imported image assets |
| `Assets/_Project/Scripts` | Intended first-party runtime code root | Confirmed | project folder convention and implementation plan |
| `Assets/_Project/Scenes` | Intended first-party scene root | Confirmed | project folder convention and implementation plan |
| `Assets/_Project/Prefabs` | Intended first-party prefab root | Confirmed | project folder convention |
| `Assets/Settings` | URP/2D template and render-pipeline assets | Confirmed | repository file scan |
| `Assets/Scenes` | Unity template scene root | Confirmed | `Assets/Scenes/SampleScene.unity` |

## Assembly Boundaries

| Assembly | Responsibility | Key references | Notes |
| --- | --- | --- | --- |
| `G10.Prototype` | Scene flow, point-and-click input, generic interactions, placeholder UI | Input System, uGUI | Runtime assembly under `Assets/_Project/Scripts` |
| `G10.Prototype.Editor` | Deterministic point-and-click scene setup and validation | `G10.Prototype`, UnityEditor | Editor-only |
| `G10.Prototype.PlayModeTests` | Bootstrap and additive zone-flow integration tests | `G10.Prototype`, Unity Test Framework | Test assembly, not auto-referenced by runtime |

## Scenes And Startup Flow

- Intended build-scene order: `Bootstrap`, `MainMenu`, `GameplayCore`, `Zone01`, `Zone02`, `Zone03`, `Zone04`, `Ending`.
- Startup scene: `Bootstrap`; its persistent `SceneFlowController` loads `MainMenu`.
- Gameplay flow: `GameplayCore` loads in Single mode, then exactly one Zone scene loads additively. Zone transitions keep the core loaded. `Ending` and `MainMenu` load in Single mode.

## Architecture

| Pattern | Finding | Confidence | Evidence |
| --- | --- | --- | --- |
| Runtime architecture | Small composition of focused MonoBehaviours split between persistent flow, gameplay core, and additive zone content | Confirmed | `SceneFlowController`, `PointAndClickInputController`, `Interactable`, `UIManager` |
| Input priority | Point-and-click controller ignores clicks over UI or while a panel is open, then resolves scene interactables; background clicks have no movement behavior | Confirmed | `Assets/_Project/Scripts/Core/PointAndClickInputController.cs` |
| Scene composition | `GameplayCore` owns camera/input/UI/EventSystem; each Zone owns environment, content, and interactables | Confirmed | `Assets/_Project/Scripts/Editor/ProjectSceneSetupBuilder.cs` |
| Persistence/networking | Not present in current scope | Confirmed | package/source scan and implementation plan |

## Coding Conventions

- Namespace style: `G10.Prototype` with feature sub-namespaces.
- Serialized fields: Project uses Force Text and Visible Meta Files; use `[SerializeField] private` fields.
- Async: No convention established or needed for the current prototype.
- Comments/docs: No convention established; document only non-obvious constraints.

## Testing And Validation

- EditMode tests: None found.
- PlayMode tests: 2 tests cover Bootstrap-to-MainMenu-to-Zone01 flow and additive Zone01-to-Zone02 transition while preserving GameplayCore.
- CI/build validation: None found.
- Strongest completed validation: Unity 6000.4.2f1 batchmode compile, deterministic scene validation, and 2 passing Play Mode tests.

## Available Unity Tooling

| Capability | Status | Evidence |
| --- | --- | --- |
| `unity.connection.status` | unavailable | Unity MCP reports zero running Editor instances |
| `unity.editor.version` | available via local executable | matching Unity 6000.4.2f1 installation confirmed |
| `unity.console.read` | available via batchmode logs | live MCP remains unavailable |
| `unity.scene.list` | unavailable | no running Editor instance |
| `unity.scene.inspect` | unavailable | no running Editor instance |
| `unity.buildsettings.read` | available via repository | `ProjectSettings/EditorBuildSettings.asset` |
| `unity.gameobject.inspect` | unavailable | no running Editor instance |
| `unity.asset.search` | available via repository | filesystem and `.meta` inspection |
| `unity.package.read` | available via repository | package manifest and lock file |
| `unity.tests.list` | unavailable | no running Editor instance |
| `unity.tests.run` | available via batchmode | 2 Play Mode tests passed |
| `unity.playmode.read` | unavailable | no running Editor instance |
| `unity.profiler.read` | unavailable | no running Editor instance |

## Important Constraints

- Preserve every Unity `.meta` file and move assets from inside Unity when possible.
- Do not commit `Library`, `Temp`, `Logs`, `UserSettings`, or Plastic workspace metadata.
- Extend the existing Input System action asset instead of introducing legacy input.
- Do not destructively edit imported source art.
- Scene and prefab assets require Editor import plus missing-reference validation.

## Unknowns And Confidence

- Intended shipping platform and reference resolution are unknown.
- An AnkleBreaker Unity MCP client is available to Codex, but no Editor instance is registered with its bridge. Editor automation falls back to the matching local Unity executable when the project is not already open.
- `temp.png` contains a non-transparent room background around the character; a clean character cutout is not currently available.

## Source Files Inspected

- `ProjectSettings/ProjectVersion.txt`
- `ProjectSettings/ProjectSettings.asset`
- `ProjectSettings/GraphicsSettings.asset`
- `ProjectSettings/QualitySettings.asset`
- `ProjectSettings/EditorBuildSettings.asset`
- `Packages/manifest.json`
- `Packages/packages-lock.json`
- `Assets/InputSystem_Actions.inputactions`
- `Assets/Scenes/SampleScene.unity`
- `Assets/_Project/Art/Environment/*.jpg` and `.meta`
- `Assets/_Project/Art/Sprites/Character/temp.png` and `.meta`
- `Assets/_Project/Scripts/**/*.cs` and assembly definitions
- `Assets/_Project/Scripts/Editor/ProjectSceneSetupBuilder.cs`
- `Assets/_Project/Tests/PlayMode/ProjectSceneFlowPlayModeTests.cs`

<!-- unity-onboarding:generated:end -->

## Zone02 environmental horror — 2026-10-22

The user defined cabin lighting as the entire player viewport, including the active HUD and application. Zone02 now uses two photographed/captured organisms, existing item IDs for adhesive/shell samples, and a transactional Pressure Hull install from 500 m to 750 m. Existing stable L1/L3 IDs and completed legacy Zone02 routes are retained; the second organism is reachable at 460 m. Zone03/04 mission content is unchanged by this phase.

Presentation extends EffectDirector/EffectSequence and Coffee UIEffect 5.11.7 with whole-screen tint/dips, scoped instrument interference and a brief clipped glass reflection. A referenced shader variant collection is warmed during loading to prevent the asynchronous Editor cyan placeholder during the .2 s glimpse. Zone02 three knocks remain audio-only, per V2. Quick shutter → Photo Lab → SEND remains the photo UI. Fourteen ElevenLabs source requests and one derived knock clip live under Audio/Zone02, with provenance; Lily speech remains deferred. Installer: G10/Zone 2/Install Environmental Horror V2. Details: `Docs/Zone02EnvironmentalHorror.md`; QA: `Docs/QA/Zone02EnvironmentalHorror/`.

## Presentation foundation update — 2026-10-20

The generated onboarding above is historical. The Horror Presentation task ran on `main` at `587d78b` and selectively imported only the existing unified EffectDirector presentation source, then extended it. No mission/save/navigation/tutorial branch refactor was imported. Current feature reference: `Docs/HorrorPresentationFoundation.md`.

The shared Zone01 cabin now owns one scene-scoped EffectDirector, six surfaces (including Computer), native audio/lighting/overlay/shake executors and a scoped UIEffect visual adapter. EffectSequence ScriptableObjects author timed, cancellable multi-stage presentation; the director remains independent of gameplay/save authority. Existing ImpactShake is the only transform writer and retains accessibility behavior.

AudioManager retains output settings and existing sources; a Resources HorrorAudioMix binds five category groups and three protected snapshots. UIEffect is pinned to 5.11.7, uses dedicated RawImage signal layers, and preloads its registered shader variant settings. KinoGlitch 1.0.0 passed an isolated copied-2D-renderer RenderTexture probe but was removed because production cabin screens are Screen Space Overlay. No production renderer was replaced.

`Tools/Audio` contains a 20-concept manifest, checksummed existing audio inventory and offline-default ElevenLabs workflow. No API key or paid generation was used. `G10 > Horror > Presentation Preview` opens an isolated scene and nine controls; missing new audio/silhouette artwork is explicitly optional rather than represented as finished content. Exact current validation and limitations are in `Docs/QA/HorrorFoundation/results.json`.


## Horror content polish — 2026-10-20

Ten real ElevenLabs Sound Effects v2 assets now live under `Assets/_Project/Audio/Horror/Anomalies` and `Ambience`, with per-file provenance. The account was Free: these are non-commercial candidates, not commercially licensed release assets. All nine presets have default serialized clips; the four ambience loops are selectable.

Display interference uses the actual rendered screen captured once per activation, then the scoped `G10/UI/HorrorDisplay` uGUI shader; it works with Overlay canvases and the current 2D renderer. Zone01 has serialized display/material bindings and a direct illustrated-light response. No gameplay or save authority changed. Temporary crew silhouette art is explicitly replaceable. See `Docs/HorrorEffectsPolish.md` and `Docs/QA/HorrorPolish` for measured output, exact usage, screenshots and tests.

## Zone 1 observation update — 2026-10-21

The latest Zone 1 story has two sites, SEND then actual algae collection at L1, and Bio Lamp after both photographs. Scope remains Zone 1 with a 500 m initial depth cap; the broader Zone 2–4 mission refactor is unfinished. Current behavior is documented in `Docs/Zone01AudioGameplay.md`, `Docs/Zone01Cutscene.md` and `Docs/Zone01Observation.md`.

The player flow is quick cabin shutter → Photo Lab → SEND. The mistakenly added live Camera, observation shortcuts and their installer were removed after the user's correction. ZoneOneSurveyFrame is evaluated only to compose a still photograph; Bio Lamp affects new captures, never existing archived images. The second species' art remains a placeholder. ZoneOneAudioSetup does not recreate observation UI. Current correction tests and Photo Lab screenshots are in `Docs/QA/Zone01QuickPhoto/`.

Movement audio uses persistent control loops driven by actual navigation activity in LateUpdate. The first deep-pressure creak follows actual descent and a timeline receipt; the three-knock cue keeps an eight-second quiet gap before that creak. Validation and screenshots for the observation pass are in `Docs/QA/Zone01Observation/`.
