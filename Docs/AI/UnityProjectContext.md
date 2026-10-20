# Unity Project Context

<!-- unity-onboarding:generated:start -->

Inspected 2026-10-20 at a30d5a8. Project root: D:/G10. No AGENTS.md found.

Confirmed: Unity 6000.4.2f1, URP package 17.4, Input System 1.19
(PointAndClickInputController), uGUI/TMP, Unity Test Framework 1.6.
No multiplayer runtime was found in the mission flow.

First party code is Assets/_Project/Scripts, namespace G10.Prototype.
G10.Prototype is the runtime assembly; Editor is a separate editor assembly;
EditModeTests and PlayModeTests are separate test assemblies. Match existing
MonoBehaviour and ScriptableObject patterns; do not add a second mission framework.

Bootstrap -> MainMenu -> persistent GameplayCore + Zone01 cabin. CabinZoneSession
rebinds that cabin to four ZoneMapConfig assets instead of loading placeholder
zone scenes. ZoneMissionRuntime owns ID-based objectives/receipts. ZoneOneStory
adapts cabin collection and upgrade actions. ExpeditionLoop owns save transactions,
day start, journal rollback, photos, cargo and ship state. ExpeditionProgression
opens routes and requires physical arrival. PhotoCaptureService/PhotoLab SEND
owns photo completion; CreatureCatcher owns spatial validation and minigame success.

Data roots: Content/Missions, Content/Maps, Content/Zone01..04. Preserve art,
GUIDs, unrelated scene references, map geometry, return/exit/rock/final areas.
Scene stat overrides are QA options; production saves normally own ship values.
RegularShipUpgradeRules is separate from story modules and uses farm captures.

Tests use isolated ExpeditionSaveStore.PathOverride and photo archive paths.
AB Unity MCP connected to D:/G10 at port 7890; compilation and C# editor execution
are available. Use its unity_* tools for Editor interaction, never direct bridge HTTP.
Baseline compiler reported zero errors. Current refactor validation: 80 EditMode and 136 distinct PlayMode cases passed; see the linked report for scope and unrun checks.

Evidence: Packages/manifest.json, ProjectSettings/EditorBuildSettings.asset,
runtime/editor/test asmdefs, CabinZoneSession.cs, ZoneMissionRuntime.cs,
ZoneOneStory.cs, ExpeditionLoop.cs, ExpeditionSave.cs, CreatureCatcher.cs,
ExpeditionEndToEndPlayModeTests.cs. Older story documentation describes superseded
missions; use current configs and the mission-refactor report for the new contract.

Current layout/report: [MissionLocationsRefactor-2026-10-20.md](MissionLocationsRefactor-2026-10-20.md).

<!-- unity-onboarding:generated:end -->
