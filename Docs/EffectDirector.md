> Current task note (2026-10-20): this document includes historical validation from the earlier effect-director branch. The presentation source was selectively brought into clean `main` at `587d78b` for the Horror Foundation upgrade, without importing that branch's mission, navigation or tutorial changes. Current implementation, authoring API, package decisions and executed results are recorded in [HorrorPresentationFoundation.md](HorrorPresentationFoundation.md). Historical test numbers below are not results for the current checkout.

# Unified cabin effects

Baseline: `587d78bcc5cb5b43d639b3b1b2605cff24af742a`, clean `main`, 2026-10-15.
Final HEAD is the same SHA: changes remain uncommitted on the work branch.
Work branch: `codex/unified-effect-director`. No package installation, commit or push.

## Pre-change inventory

| Class | Existing effect / trigger | Existing owner | Destination / approach |
|---|---|---|---|
| Persistent ambient | Breathing, bob, caustics, seeded blink, needle jitter, sweep, watercolor pulse | CameraBreathing, CabinBob, CausticScroller, RandomBlink, GaugeJitter, RadarSweep, WatercolorBreathing | Retain their animation logic and separate transforms. |
| Resource-dependent | Hull/energy overlays, mood, critical blink, hum | CabinFeedbackController executes; ship owns values | Coordinator derives requests; director composes persistent presentation. |
| One-shot | Terrain impact, severity audio, red flash, short shake | CabinFeedbackController + ImpactShake + AudioManager | Director dispatches; ImpactShake remains transform writer; collision latch/damage unchanged. |
| One-shot | Creak, warning, power-down | Controller timers / energy crossing | Timers stay coordinator; audio execution becomes scoped. |
| Timed | Capture delay, scrape, flicker, subtle jitter | Controller | Capture interpretation/delay stay coordinator; cancellable combined effects execute in director. |
| Timed | 10-second rock event; terrain reveal at 1.5 seconds | RockBreakerSequence | Retain specific timeline, commit/reveal/deadline behavior; delegate shake/audio/message only. |
| UI-specific | Depth/resource/capture/rock notifications | Controller | Text executor with priority and scoped cancellation. |
| UI-specific | Transition fade, day-left pulse, tutorial, radar scan and camera/capture feedback | SceneFlowController / specialized presenters | Intentionally retained; not duplicated or redesigned. |

### Audit boundaries

Navigation emits TerrainImpact and retains damage/contact latch. ExpeditionLoop owns restored resources and Changed events (Rest, rollback, upgrades); CabinZoneSession rebinds the shared cabin to logical zones. Rock objective flag and reward commit precede BeginCommitted. No effects enter the expedition save. AudioManager currently has a global feedback one-shot source and hum source, so stopping all feedback cannot provide scoped cancellation. The new audio executor must own scene-local voices while using the existing master/SFX/ambient mix.

ImpactShake owns outer CabinFrame position; CabinCanvasFrame owns scaling; breathing and bob own child rigs. FeedbackOverlayCanvas remains overlay order 31000, below transition fade 32000, with non-raycast graphics. CabinAtmosphere is the single resource/mood adapter; ambient components keep reading its derived multipliers.

Existing regression coverage includes resource thresholds, scan mood, collision latch/accessibility, immediate flash, real capture escalation, Rest/Journal/death/Continue, pause, ending teardown, rock commit failure/single activation/reveal/timing/interrupted Continue and ambient hierarchy/hotspot alignment. New tests must focus on handles, priority/channel/target isolation, composition, repeated playback and executor cleanup rather than duplicate these gameplay cases.

## Validation baseline

- PlayMode: 24 PASS, 0 FAIL, 0 skipped; job `f24ae5f61248`, 59.48 seconds.
- Fixtures: CabinFeedbackPlayModeTests, RockBreakerPlayModeTests, CabinAtmospherePlayModeTests, TerrainCollisionPlayModeTests.
- Compilation errors: 0 before modifications.
- Raw results: `Logs/EffectDirector/baseline-playmode.xml` (local ignored log).
- EditMode baseline: 25 PASS, 0 FAIL/skipped, job `54bbcd2cb434`, 1.19 seconds. Post-change runs are recorded below.

## Implemented architecture

The shared `Cabin Station` owns one `EffectDirector`, not a global singleton.

`CabinFeedbackController` still observes navigation/capture/expedition signals, derives ratios, detects the energy crossing, schedules creaks/warnings and the delayed scrape, and cancels that scheduling at existing gameplay boundaries. It no longer writes presentation graphics, lighting, transforms or feedback audio. Its public diagnostic properties remain available.

`RockBreakerSequence` still owns its exact 10-second specific timeline, 1.5-second reveal, deadline calls and interrupted-Continue semantics. It requests rock audio, repeated shake pulses and high-priority notices by handle. The durable flag/reward transaction remains in the unchanged mission/expedition layer, before `BeginCommitted`. Its old public `shake` and `impactAudio` references remain serialized for compatibility/migration.

`EffectDefinition` ScriptableObjects expose stable ID, channel, target, priority, conflict policy, duration/intensity and optional shake, overlay, lighting, clip/loop/fade and message configuration. Thirteen native definitions live in `Assets/_Project/FeedbackEffects`. `EffectContext` can override presentation values per request without modifying the shared definition. A definition can combine these modules; it is not a timeline/mission engine.

Each `EffectSurface` binds an audience, optional visibility root/group and these replaceable execution boundaries:

- `EffectShakeExecutor`: chooses the director's winning request, delegates actual position updates and accessibility to unchanged `ImpactShake`.
- `EffectOverlayExecutor`: only writer for the dedicated Hull/Energy/tint graphics and feedback label; captures/restores their authored colors.
- `EffectLightingExecutor`: resource mood, instability and transient light dip through existing `CabinAtmosphere.SetResourceFeedback`. The specialized ambient components are unchanged.
- `EffectAudioExecutor`: scene-local reusable AudioSources, independent handle per voice, native loop fades and scoped stop. Uses AudioManager's existing master/SFX/ambient gains and AudioListener output volume. Its read-only loop diagnostics include owned scene voices for compatibility with existing tests/UI. Legacy AudioManager feedback APIs remain available to old callers.

The director knows no ship, navigation, mission, inventory, save or expedition types. It accepts only derived presentation requests and suspension policy. No transient field is in save DTOs. No gameplay authority or coordinates changed.

## API examples

```csharp
// Serialized/local reference, not EffectDirector.Instance.
EffectHandle hit = effects.Play(CabinEffects.Impact, new EffectContext {
    ShakePixels = 4, ShakeSeconds = .18f, Alpha = .18f,
    Gain = .5f, Pitch = .98f
});
effects.Stop(hit);                       // Only this request, including its own audio.
effects.StopChannel("scrape");            // Transients only; persistent state is explicit.
effects.StopChannel("screen", EffectTarget.Radar);
effects.ClearTransients();               // Persistent Hull/Energy/hum remain derived.
effects.SetPersistentState(CabinEffects.Energy, true,
    new EffectContext { Alpha = .22f, Instability = 1.3f });
effects.SetPersistentState(CabinEffects.Energy, false); // Hum fades out.
```

For a future local radar blackout, author an `EffectDefinition` with a unique ID, target Radar, overlay Tint, alpha and duration; add it to the director catalog. It uses the Radar surface's black overlay only while Radar is visible. No new gameplay producer, camera, shader or package is necessary. Native light flicker/resource moods use the Global atmosphere adapter; scoped screen dimming/blackout uses Tint so it cannot change the cabin mood behind another screen.

`Play` returns an invalid handle for unknown IDs, missing/disabled/hidden audiences or suppressed playback. Unsupported optional modules are safely skipped; a definition's other bound modules can still execute. `IsActive(handle)` verifies current lifetime and director ownership; `IsValid` merely indicates a handle was issued. Handles must not be serialized. Persistent requests are keyed by definition and target and updated in place, not restarted every frame.

Use `surface.BindVisibility(root, optionalGroup)` when deliberately rebinding an audience; `BindVisibility(null)` means intentionally unbound. A previously required root/group that is destroyed remains unavailable rather than becoming unrestricted. Serialized optional None references do not make Global unavailable.

## Conflict/composition policies

- Channel conflicts are scoped to `(target, channel)`. `ReplaceLowerPriority` rejects a lower priority, otherwise replaces the channel's transients; equal priority favors the latest request. `IgnoreWhileActive` rejects repeats. `Stack` retains independent requests.
- Shake arbitration is separate across channels on each bound surface: highest priority, newest on ties; never additive. Rock pulses (100) beat collision (50), which beats capture jitter (10). Stopping the winner may resume a still-live lower request, never an expired pulse. `ImpactShake` remains bounded to 8 pixels and .28 seconds per pulse and restores the exact outer-frame origin.
- Hull/Energy overlays compose independently. Each layer uses maximum alpha rather than summing flashes; Energy ripple is capped at .30. Collision flash peaks immediately and decays back to current persistent Hull state. Resource changes ease with the existing .4-second default.
- Mood priority preserves Danger (Hull 100) over LowPower (Energy 50); scan/manual mood is the existing fallback. Transient flicker multiplies that current resource baseline, not a stale captured mood. Removing an override immediately updates the atmosphere input; ambient smoothing remains its own implementation.
- Notifications choose highest priority/latest; rock notices do not get overwritten by ordinary messages. Removing a notice restores the next still-live message or blank.
- Hum and one-shots use separate scene-owned voices. Stopping one handle/channel never calls the old global StopFeedback. Persistent loop deactivation fades out; transient stop/clear is immediate. An individual clip's lifetime can outlast its shorter visual module without extending shake/flash duration.
- Request storage is bounded to 128 live requests, audio to 16 reused voices. At audio capacity an additional sound is skipped, not stolen from another request. Normal cabin traffic is far below either bound. No LINQ, hierarchy searches, request reconstruction or collection allocation in the per-frame executor path. A request allocates once when played; voices allocate only up to pool capacity.

## Lifecycle policy

| Boundary | Behavior |
|---|---|
| Normal play | Coordinator derives persistent values; director ages native transient modules with scaled time. |
| Pause / application pause | Pending scrape and active transients cancel. Persistent overlays remain; owned loop pauses. Rock timeline freezes; it does not replay impact audio on resume. |
| Transition / death / failure / ending | Coordinator suspends requests and hides persistent overlays below the existing transition fade; teardown releases scene voices and restores transforms. |
| Rest / Journal / authoritative Changed event | Cancel old transients/delayed scrape, reset coordinator timers, derive resources from the actual restored ship. Never award or restore gameplay from an effect. |
| Continue / zone reload | Old scene director dies; new director derives current resources. No transient save/load or completed-rock replay. |
| First unlocked frame | Resume changes the gate only. Old requests are cleared on entry, not again on exit; a new reaction cannot be discarded by the next Update. |
| Hidden/disabled audience | Reject new transients; clear existing audience transients. Persistent state is retained but its audio stops and its graphics restore until visible again. Other audiences remain independent. |
| Disable / destroy | Clear all requests/owned audio, restore graphic colors, reset atmosphere override and exact shake origin; balanced existing coordinator subscriptions. |
| Screen-shake OFF | Existing `G10.Accessibility.ScreenShake` suppresses transform motion only, not audio, flash, message or lighting. |

No coroutine or transient request is serialized. No new static mutable director, cameras, RenderTextures, shaders, particles or third-party packages were added. Canvas scaling and breathing/bob writers are unchanged.

## Scene setup and migration

The saved shared Zone01 scene is migrated. No hand wiring should be required.

To reproduce: outside Play Mode, open Zone01 and run **G10 → Feedback → Install Unified Effect Director**, then save. **Apply Gameplay Feedback Setup** also invokes the migration. The installer validates old bindings before mutation, uses Undo/Editor/AssetDatabase APIs and preserves existing definition assets/custom catalog entries. It does not reinitialize authored definition tuning on subsequent runs.

The existing feedback canvas remains order **31000**. Its old Hull/Energy/notification objects retain their references. Four new transparent scoped surfaces bind CabinViewport visibility, Helm panel, Radar panel and Photo Lab panel. Each scoped surface projects its audience RectTransform bounds into its existing canvas, immediately on dispatch and at Canvas.willRenderCanvases after layout/motion. Thus a Photo Lab window effect occupies the window region, not the whole desktop. This does not move or reparent the actual audience. The four-corner buffer is reused and event subscription is removed on disable. All new graphics are non-raycast. Global owns the only shake and atmosphere adapters; scoped surfaces own only their dedicated screen overlay. Transition fade remains **32000**. No existing art/audio asset or metadata was replaced.

Hidden legacy controller fields are retained to support serialized references and repeatable migration. Runtime clips now come from definitions, except the coordinator's preserved two scrape choices (its gameplay-driven random selection is passed as context). Do not delete those legacy fields until a separate versioned asset migration is justified.

## Files

Added runtime: `Feedback/EffectDefinition.cs`, `EffectDirector.cs`, `EffectSurface.cs`, `EffectOverlayExecutor.cs`, `EffectLightingExecutor.cs`, `EffectShakeExecutor.cs`, `EffectAudioExecutor.cs` under `Assets/_Project/Scripts`, with Unity-generated metadata.

Added authoring: `Scripts/Editor/EffectDirectorSetupEditor.cs`, `FeedbackEffects/*.asset` (13 definitions), folder/file metadata.

Modified runtime: `Feedback/CabinFeedbackController.cs`, `Feedback/RockBreakerSequence.cs`, `Audio/AudioManager.cs`. Modified authoring: `Editor/CabinFeedbackSetupEditor.cs`, `Scenes/Gameplay/Zone01.unity` (additive bindings only).

Added tests: `Tests/EditMode/EffectDirectorEditModeTests.cs`, `Tests/PlayMode/EffectDirectorPlayModeTests.cs`. Strengthened `CabinFeedbackPlayModeTests` with director-lifetime/handle cancellation assertions around existing Rest/Journal/Continue integration; no original assertions removed.

Retained unchanged: ImpactShake, all ambient animation components and installer, navigation, gameplay resources, mission/save/transaction/progression code, map configs and package manifests.

Unity test/import activity updated dynamic font caches (`LiberationSans SDF - Fallback.asset`, `AlegreyaSansSC-Regular SDF.asset`) and serialized the existing default fade fields in Bootstrap; these were retained, not reverted over Editor state. The runner removed four tracked generated JSON/metadata files under Assets/Resources; their baseline contents/GUIDs were recovered after testing (text encoding/final-newline normalization may remain in their diff). These are separate from intentional feature changes, not new gameplay work.

## Regression findings and results

Initial new-system EditMode: **31/31 PASS**, job `caf70c648286`, .06 seconds (includes 6 new director contracts and 25 existing feedback rule cases).

Initial focused PlayMode: **31 PASS, 1 FAIL**, job `357aeab34e81`, 58.65 seconds. Existing `RatiosMoodScanAndCriticalOverlaysSurvivePanelsWithoutChargeTriggers` expected Danger but observed ScanActive for one frame. Root cause: resource state was accepted by the director but applied to CabinAtmosphere only in its later Update. Fixed in `EffectDirector.SetPersistentState`: apply the composed lighting input synchronously while preserving normal UI easing. Added `ResourceMoodIsAvailableToConsumersBeforeTheNextDirectorUpdate`; no assertion was weakened.

Full post-fix EditMode: **69/69 PASS**, job `1579532af56e`, 4.05 seconds. Raw results: `Logs/EffectDirector/regression-editmode.xml`.

Full post-fix PlayMode: **288/288 PASS**, job `26635d97ddf3`, 811.32 seconds. Includes all existing PlayMode fixtures, the first ten new executor tests, all 13 ExpeditionEndToEnd tests, both rock tests, tutorial, death, historical maps, Continue, Rest and Journal flows. Raw results: `Logs/EffectDirector/regression-playmode.xml`.

Final hardening added four additional target-boundary cases after this full run. Three were first demonstrated failing on the then-current implementation: **0 PASS, 3 FAIL**, job `43a2bfdc2841`, .12 seconds, raw `target-boundary-prefinal-failures.xml`:

| ID | Reproducer / actual | Root cause / fix | Regression |
|---|---|---|---|
| FX-02 (P2) | Small 240×120 audience; scoped overlay stayed at its unrelated 100×100 rect (scene binding otherwise full-canvas). | Visibility gating was not geometric scoping. EffectSurface now projects the actual audience bounds, including resize/movement, without writing the audience. | ScopedOverlayFitsTheActualWindowRatherThanTheWholeCanvas |
| FX-03 (P2) | Destroy audience while surface survives; Play incorrectly still accepted and old tint remained eligible. | Unity's destroyed-object `== null` was treated as an intentionally unbound audience. Track an explicit visibility-binding requirement; `BindVisibility(null)` intentionally removes it, destruction does not. | DestroyedAudienceDoesNotBecomeAnUnrestrictedGlobalSurface; VisibilityBindingDistinguishesExplicitNoneFromDestroyedAudience |
| FX-04 (P2) | Destroy native ImpactShake target, then render/restore executor; MissingReferenceException. | C# `?.` does not implement Unity destroyed-object null semantics. Explicit Unity null checks in the native shake adapter; the pooled audio executor also releases/recreates removed AudioSources safely. | DestroyedNativeShakeTargetIsSafeToRenderAndRestore; RemovedPooledAudioSourceIsRecreatedWithoutLeakingOrThrowing |

FX-01 is the initial one-frame resource mood regression described above. These are refactor hardening findings, not new claims about unrelated gameplay/save defects.

Post-hardening full EditMode recheck: **69/69 PASS**, job `282262069d0c`, 3.84 seconds; raw `prebinding-editmode.xml`. After the explicit binding correction, the full EditMode suite is **70/70 PASS**, job `17b103ed2ca7`, 3.11 seconds, raw `final-editmode.xml`.

Targeted post-hardening PlayMode job `8e0b973b215e`: **54 PASS, 9 FAIL**, 167.24 seconds, raw `serialized-null-regression-failure.xml`. The initial destroyed-target fix used ReferenceEquals, but Unity's deserialized optional None references can be non-CLR-null wrappers. Global was mistakenly unavailable, suppressing notices/impact/mood and rock shake. Fixed FX-03 at the binding layer: serialized requirement flags plus explicit BindVisibility, using Unity null checks and retaining the requirement after destruction. The new EditMode binding test covers intentionally unbound → bound → destroyed → explicitly unbound. Existing scene gameplay tests reproduce the deserialized-None case. Original failing assertions remain unchanged.

Final targeted PlayMode: **63/63 PASS**, **0 FAIL, 0 skipped**, job `4f8ae2eb4930`, 193.76 seconds, raw `final-playmode.xml`. This is the same selection as the failing 63-test run: all 14 new executor tests, feedback, rock, atmosphere, collision, Helm preview/navigation, computer shell/apps, scoped UI, scene flow, ending presentation, vessel death and three real-device/end-to-end/Journal scenarios. The entire 288-test suite was run before the last target-boundary hardening; it is not claimed as a second full run on the last source revision. Its other 229 existing PlayMode cases were not rerun after the final binding/native-target changes, and retain their recorded earlier PASS result.

Saved scene migration re-ran twice: **796 → 796 → 796** hierarchy objects, one director, five surfaces, thirteen definitions, zero missing scripts, preserved old Hull graphic reference, order 31000, saved scene clean. Final repeat after tests confirmed Global available, every overlay graphic non-raycast and zero missing scripts. The scene's SHA256 stayed `110925B970270BDF5330B13FBD848B0B8CAB493BDE71CD5251528F6914B76592` before/after final repeat installation. Native shake, breathing/bob rigs and the scene's scaling owner remain separate.

Final compilation error count **0**; Console error count **0**; Editor stopped, not compiling, saved scene clean. No outstanding confirmed presentation defects remain from the reproduced cases. Latest-source executed results are **70 EditMode PASS + 63 targeted PlayMode PASS**, 0 FAIL/INCONCLUSIVE/skipped; do not sum repeated runs. Full-suite and focused timing/coverage above are intentionally distinguished. A second complete 292-case PlayMode run on the final source, Windows build, human visual/audio comfort QA and profiling are **NOT RUN**.

### New coverage versus retained coverage

Seven new EditMode tests cover stable IDs, missing bindings, equal/lower priority, Stack/Ignore policies, cross-director handles, target/channel stop, persistent update identity, disabled/destroyed surfaces and explicit visibility-binding semantics.

Fourteen new PlayMode tests cover actual native dispatch/composition, scoped audio, resource baseline/mood timing, target visibility/geometry/destruction, rock-versus-impact priority, accessibility, pause/application pause/resume-frame safety, scoped dimming, repeated playback/pool bounds/disable/destroy, removed native AudioSources and missing/disabled executors. Existing gameplay tests continue to exercise the actual camera/SEND/capture, Rest, Journal, death, all-zone travel and ending services; these were not replaced with synthetic completed objectives. Total new cases: **21** (7 EditMode + 14 PlayMode), plus strengthened existing integration assertions.

### Exact test/tool calls

All Unity operations used the configured Unity MCP provider (port 7890), not its internal HTTP bridge:

```text
unity_testing_run_tests {mode:PlayMode, filter:CabinFeedbackPlayModeTests|RockBreakerPlayModeTests|CabinAtmospherePlayModeTests|TerrainCollisionPlayModeTests}  baseline f24ae5f61248
unity_testing_run_tests {mode:EditMode, filter:CabinFeedbackRulesTests}  baseline 54bbcd2cb434 (25 PASS, 1.19s)
unity_testing_run_tests {mode:EditMode, filter:EffectDirectorEditModeTests|CabinFeedbackRulesTests}  caf70c648286
unity_testing_run_tests {mode:PlayMode, filter:EffectDirectorPlayModeTests|CabinFeedbackPlayModeTests|RockBreakerPlayModeTests|CabinAtmospherePlayModeTests|TerrainCollisionPlayModeTests}  357aeab34e81
unity_testing_run_tests {mode:EditMode}  1579532af56e
unity_testing_run_tests {mode:PlayMode}  26635d97ddf3
unity_testing_run_tests {mode:PlayMode, testNames:[<three target-boundary tests listed above>]}  43a2bfdc2841
unity_testing_run_tests {mode:EditMode}  282262069d0c
unity_testing_run_tests {mode:PlayMode, testNames:<Logs/EffectDirector/final-test-names.json>}  8e0b973b215e
unity_testing_run_tests {mode:EditMode}  17b103ed2ca7
unity_testing_run_tests {mode:PlayMode, testNames:<same final-test-names.json>}  4f8ae2eb4930
unity_testing_get_job {jobId:<above>, includeFailedOnly:true, waitTimeout:20}
unity_get_compilation_errors {severity:error, count:20}
unity_execute_code: G10.Prototype.Editor.EffectDirectorSetupEditor.Install(); EditorSceneManager.SaveScene(cabin.gameObject.scene);
```

Each completed native result XML was copied before starting the next runner job:

```powershell
Copy-Item -LiteralPath 'C:/Users/Artermis/AppData/LocalLow/DefaultCompany/G10/TestResults.xml' -Destination 'D:/G10/Logs/EffectDirector/<run>.xml'
git diff --check
git -c core.whitespace=-blank-at-eol diff --check
```

Local ignored logs include `baseline-playmode.xml`, `baseline-editmode.xml`, `focused-editmode.xml`, `initial-playmode-failure.xml`, `regression-editmode.xml`, `regression-playmode.xml`, `target-boundary-prefinal-failures.xml`, `serialized-null-regression-failure.xml`, `prebinding-editmode.xml`, `final-editmode.xml`, `final-playmode.xml` and the exact 63-name `final-test-names.json`. Test counts should not add repeated baseline/focused runs to unique coverage totals. Plain `git diff --check` flags Unity's generated empty `m_Name: ` lines in the scene; ignoring only blank-at-EOL makes the check pass. No fragile manual scene YAML cleanup was performed.

### Manual checks still NOT RUN

Use a development/debug timeline, never the player's real save: compare normal cabin/Helm/Radar/desktop appearance; listen to low-energy hum during collision; test shake OFF while flash/audio still play; pause/resume during rock destruction; review flicker comfort and Photo Lab window-region effects at supported resolutions. Automated tests do not substitute for this human visual/listening pass.

## Limitations / future integration

No Windows player build, human playtest, listening/comfort evaluation or profiler measurement is claimed. Native automated tests validate state, execution and scene invariants, not subjective sound balance or exact pixel equivalence. Existing audio gains are respected; no new mixer routing asset is introduced.

Executors have replaceable virtual execution methods and explicit bindings. A future Cinemachine/FEEL transform executor, glitch screen executor or alternate audio executor can implement those boundaries without gameplay producers changing effect IDs. There are deliberately no speculative third-party adapters, assemblies or manifest dependencies today. Unsupported modules/over-capacity requests are nonfatal presentation omissions, not gameplay failures. Scoped screen regions use axis-aligned UI bounds; complex rotated/masked/overlapping-window compositing would need a dedicated screen executor. This is not a general timeline, shader system or arbitrary surface editor.
