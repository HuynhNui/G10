# Unity Project Context

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
