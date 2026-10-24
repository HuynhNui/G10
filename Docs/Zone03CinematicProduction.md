# Zone 3 production — 2026-10-22

Status (2026-10-23): **scene/assets installed and saved through Unity MCP; native C#/shader compilation clean; EditMode 98 pass/0 fail/1 optional skip; 50 distinct selected PlayMode cases pass, including all 12 Zone3 cases.** Actual Game View captures and AudioListener recordings are available. Run scope and targeted reruns are recorded in [results.md](QA/Zone03Cinematic/results.md).

## Mission authority

The imported local reference is `codex/unified-effect-director` commit `43b1a26`. Work remains on `main`; no checkout, commit, merge or push occurred. Its old audio/render code was not imported. Mission code was selectively reconciled with the newer uncommitted Zone 1/2 presentation work.

The canonical layout has 2/2/3/4 sites: 9 mandatory organisms plus 2 hidden organisms. Zone 3 retains `zone03-l1`, `zone03-l2`, `zone03-l4`, with objective prefixes `Z3_L1`, `Z3_L2`, `Z3_L3`. Creature 05/07 require PHOTO → manual SEND → CAPTURE. Creature 06 requires PHOTO → SEND. CRAFT consumes `Z3_Creature_01` and `Z3_Creature_03` once; INSTALL consumes neither again. The original physical rock interaction, 1.5-second terrain reveal and ten-second gameplay lock remain authoritative.

Save v6 migrates current/day-start/Journal snapshots, preserves historical route entitlements without invented CAPTURE receipts, archives original mission progress and remaps pending photos by original POI. The local lamp-material cargo ID merges into `Z1_Creature_01` without losing quantity. Zone 1/2 cue receipts and intro state remain additive fields. A runtime layout check prevents publishing v6 voyage progress against incompatible serialized mission data; the installed scene now uses the canonical layout.

## Production assets and sequence bindings

All 18 WAVs are in `Assets/_Project/Audio/Horror/Zone03/`. They use 20 original ElevenLabs requests, 445 header-reported credits, no voices. Source receipts/candidates and DSP provenance are in [audio-production.md](QA/Zone03Cinematic/audio-production.md).

| Scene | Production binding / behavior |
| --- | --- |
| Nameless Ruins / structures | Sparse water plus mineral bed; 3 spaced stone resonances, alternating authored pans. Local procedural strata on the real Helm map, without a uniform global tint. |
| Creature 05 | Original irregular pressure bursts on Photo Lab inspection and successful capture. Existing quick shutter/compositor and creature art retained. |
| Delayed Engine | Observes sustained actual forward propulsion then its stop. The processed reflection has multiband delayed components, flutter, stereo diffusion and a new stone cavity source. Never writes navigation/charges. |
| Creature 06 | Original hard biological motion on photo inspection; restrained procedural reflection on the Photo Lab image. The visual is not baked into archival PNGs or added to mission data. |
| False Radar | New distorted ping response plus a secondary sweep and decaying ghost rings on the actual circular scope. Does not call Scan, reveal contacts or consume charges. |
| Acoustic passage | Environmental amplitude fades and low-pass cutoff moves from 11 kHz toward 650 Hz. Existing Suspense mixer request supplements this; machinery/interface/impact buses retain their baseline. No silent file. |
| Reflection | 6.8-second authored sequence: environmental reduction, 0.65-second captured monitor tear, procedural blurred head/shoulder/trunk, near-field movement, close click, recovery. No crew sprite or scream. |
| Creature 07 | Original biological discharge plus irregular branching arcs on the Photo Lab image, compatible with replacement creature art. |
| Assembly | Mechanical latch/actuator source on durable craft/install callbacks; restrained amber computer response. |
| Demolition | Pressure impact, 3 different collapse sources with staggered onsets, 4 short directional vibration pulses, procedural dust/shards, receding 7.2-second resonance. The bounded audio tail may outlast the ten-second gameplay lock. |
| Beyond | Stone layer disappears, local lighting stabilizes, sparse water and normal machinery remain. No exit scare. |

The primary story cues use durable ZoneThreePresentationState receipts. Cancellation/restore clears owned handles; repeat inspection feedback is intentionally transient. The Rock Breaker resumes its remaining authored cues after pause by seeking their audio positions, without replaying expired detonations. Explicit stop/disable cancels all its handles. Screen shake uses the existing accessibility preference.

## Rendering and packages

No packages were added. UIEffect 5.11.7 and existing captured-frame distortion remain in the project. New silhouettes/sweeps/electric arcs/debris need procedural masks rather than a conventional UI filter; `G10/UI/ZoneThreeComposition` extends the existing visual executor for those modes. The existing EffectSequence assets remain the sole authored clock under EffectDirector, so no competing Timeline manager was introduced. KinoGlitch camera processing would not cover the actual Screen Space Overlay audiences.

`ZoneThreeVisualSurface` has no Update/coroutine/readback. It creates at most one graphic/material per activated surface, disables drawing when hidden and destroys owned material/graphic with its owner. Stone/debris fragment cost while active still needs profiling on target hardware. There is no measured GPU or disabled-render-cost claim yet.

## Reproduce in the Editor

The native installer has already run and saved the shared cabin. To reproduce or deliberately re-author it:

1. Check that the real Editor is stopped, not compiling, and that the authored shared cabin has no unsaved edits.
2. Import/compile source; fix any Unity-only compiler/shader diagnostics.
3. Open the existing shared Zone01 scene and run `G10.Prototype.Editor.ZoneThreeSetup.Install()`. The idempotent installer applies the canonical 11-site layout/markers, creates real preset assets, imports/assigns all WAVs and binds the real Helm/Radar/Photo Lab/Global audiences. It refuses a dirty cabin scene.
4. Run relevant EditMode and PlayMode fixtures, including migration/material/hidden-ending tests, ZoneThreeMissionTests, ZoneThreeVisualTests and ZoneThreeCinematicTests; then regress Zone 1/2, tutorial, photos, rock timing and travel.
5. Produce actual before/after Game View captures for demos A–D. The editor menu `G10/Zone 3/Preview ...` uses those same production sequences in actual logical Zone 3 gameplay, without writing story receipts or pretending to be mission completion.

Current native results and demonstration evidence belong in [results.md](QA/Zone03Cinematic/results.md). Imported historical branch test totals are not current results. Close the clean additive authoring cabin before starting the Test Runner; an Editor save dialog previously blocked a transition until the user dismissed it.

## Main project changes

| Area | Files |
| --- | --- |
| Audio library | `Assets/_Project/Audio/Horror/Zone03/`, `Tools/Audio/generate_zone03.py`; receipts, candidates and verification under `Docs/QA/Zone03Cinematic/` |
| Zone3 observations and state | `ZoneThreePresentationController.cs`, `ZoneThreePresentationState.cs`; additive save fields in `ExpeditionSave.cs` / `ExpeditionLoop.cs` |
| Procedural presentation | `ZoneThreeVisualSurface.cs`, `ZoneThreeComposition.shader` / material, `ZoneThreeVisualSetup.cs`, scoped preset support in `EffectVisualExecutor.cs` / `EffectDefinition.cs` / `EffectMotion.cs` |
| Sequence/audio ownership | `EffectDirector.cs`, `EffectSequence.cs`, `EffectAudioExecutor.cs`, `RockBreakerSequence.cs`; original gameplay timing remains authoritative |
| Scene authoring | `ZoneThreeSetup.cs`, `Assets/_Project/FeedbackEffects/Zone03/`, existing shared `Scenes/Gameplay/Zone01.unity` |
| Mission synchronization | Four mission and map assets, `MissionLayoutEditor.cs`, `MissionLayoutMigration.cs`, runtime/editor/tutorial and upgrade-material UI compatibility; details in `Docs/QA/MissionSynchronization/report.md` |
| Capture mix fix | `AudioManager.cs`, `CaptureMinigameController.cs`: bounded capture-device voice and a single validated success cue from `CreatureCatcher` |
| Native evidence | `ZoneThreeCinematicTests.cs`, `ZoneThreeMissionTests.cs`, `ZoneThreeVisualTests.cs`, `ZoneThreeAudioEvidence.cs`; relevant preexisting mission/photo/exit/tutorial tests synchronized |

## Review boundaries

The offline audio reels are listenable assets, not Unity recordings. Separate `A-gameplay-listener.wav` and `D-gameplay-listener.wav` files record the actual Unity listener mix. The Game View stills show the real instrument surfaces and demolition; automated tests check rendered pixels, scope/occlusion, cancellation, mission authority and measured listener clipping. A human still needs to judge whether stone sounds avoid bells/metal, Creature 06 avoids chimes, the reflected engine remains distinct at player volume, and the apparition feels ambiguous within the painted monitor. Recorder is absent from the current package manifest; no video was fabricated. Target-hardware profiling and standalone player builds remain outside the verified Editor results.

The ElevenLabs account was Free at generation, and provenance records the resulting noncommercial/attribution limitation. A commercial release needs appropriately licensed replacement generations. Creature artwork and existing external asset licenses were not changed.
