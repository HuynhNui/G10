> Historical foundation report. Real ElevenLabs audio, display distortion and final validation from the follow-up task are documented in [HorrorEffectsPolish.md](HorrorEffectsPolish.md).

# Horror audio and presentation foundation

Date: 2026-10-20. Checkout: `main`, baseline `587d78b`. No commit, merge or push. The requested EffectDirector files had not been brought into local or remote main, so this change imports only presentation-related source and native effect tests from the existing local `codex/unified-effect-director` branch. Its mission/save/navigation/tutorial refactors and scene changes were not imported. Zone01 was migrated by the idempotent Editor installer against its current main-branch scene.

## Audio production and inventory

See [audio workflow](../Tools/Audio/README.md), [20-concept manifest](../Tools/Audio/sound-manifest.json) and [existing inventory](../Tools/Audio/existing-audio-inventory.json). Twenty-five audio files exist in the original authoring directory; the existing resource copies of ambient/UI clips are retained. Hull impact, creak, warning, scrape, low-power hum, power-down, radar, camera, capture and UI sounds are reused at their original references. No artist asset was moved or replaced.

The official ElevenLabs sound-effects skill was installed and read. `ELEVENLABS_API_KEY` was absent. Actual newly generated audio: **none**; remote generation calls: **zero**. No authentication onboarding or credentials were written into the project. The standalone Python tool supports offline inspection and a separately approved, quota-checked one-asset generation, records provenance and refuses overwrites. A billing quote is a human-reviewed estimate, not an enforced provider spending cap. The pipeline is not included in the Unity player.

Missing production audio comprises the 20 manifest concepts, including the first-priority three knocks, long scrape, singing algae, pressure groan and abyss bed. Existing short exterior scrape/creak clips are usable foundation samples, not replacements claimed to fulfill those new concepts. ThreeMetallicKnocks leaves its production clip unassigned. The preview can use the existing creak to audition timing, clearly labeled as a sample. Source/licensing of preexisting audio is not inferred from filenames.

## Mixer and audio ownership

`Assets/_Project/Audio/Horror/G10Horror.mixer` is loaded through the Resources `HorrorAudioMix` asset by the existing AudioManager. Groups: Master > Machinery, UnderwaterAmbient, Anomalies, Interface, Impacts. Normal, Suspense and NearSilence snapshots transition smoothly. NearSilence sets UnderwaterAmbient to -32 dB and Anomalies to -48 dB; Machinery, Interface and Impacts remain at 0 dB. Suspense attenuates the environmental groups by -4/-2 dB. Critical warnings, collision/capture/UI feedback and the submarine motor/hum are protected.

AudioManager retains its existing master/SFX/ambient gains, output preference and sources. `SetCategoryVolume(AudioCategory, float)` supplies independent persisted category gain; `SetOutputVolume` remains final AudioListener output gain. No category change writes the final output setting. Legacy feedback methods remain callable. The scene director binds to AudioManager's mixer rather than creating a singleton.

EffectAudioExecutor keeps up to 16 reusable scene-owned voices. Each voice now has its own child object so optional low-pass/reverb filtering is independent; removed sources are recreated on the same child. Existing 2D behavior remains spatialBlend=0; stereo pan adds direction without assuming 3D listener geometry. Loop and one-shot gains use existing player gains and category gain. Optional fade envelopes, low-pass and room reverb are authored per definition; filters disable when not used. Stopping one effect/sequence releases only its owned voices. Mixer requests are keyed by director owner, priority and recency; releasing one owner restores the next mix request or Normal.

## Package evaluation

* **UIEffect** `com.coffee.ui-effect` is pinned to Git tag **5.11.7**, tag commit `ff824c234121cd6dde7038c8e8ca4904d7f4d7f3`. Installed using Unity PackageManager Client.Add, with the resolved hash in packages-lock.json. Official source: https://github.com/mob-sakai/UIEffect . Used only on dedicated RawImage signal layers, never on all UI or TMP text. The installed package supplies Unity 6 TMP support resources; these were not imported because no TMP graphic is modified. If future effects target TMP, use the package's **TextMeshPro Support (Unity 6)** sample.
* A Grayscale + RGB Shift shader variant is explicitly registered and saved in the preloaded UIEffectProjectSettings asset for runtime-created graphics. The corresponding preloadedAssets entry in PlayerSettings is intentional. No complete Windows build has yet been claimed.
* **KinoGlitch URP** `jp.keijiro.kino-glitch.universal@1.0.0` was installed through the official Keijiro scoped registry (`https://registry.npmjs.com`, scope `jp.keijiro`), compiled and tested on an in-memory copy of this project's URP 17.4 2D Renderer with a RenderTexture. Analog ripple/color drift changed 11,470 of 16,384 pixels in the final passing probe. The first run exposed a test cleanup error (render target destroyed while assigned); the probe detached its camera target and passed when rerun. This was a test harness issue, not a package compatibility failure.
* Kino can process that camera texture, but cannot distort the cabin's Screen Space Overlay UI. Its analog pass skips back-buffer targets and zero-valued settings; its digital pass maintains history at intensity zero, so a future camera integration must disable the component entirely when idle. See upstream https://github.com/keijiro/KinoGlitchURP and https://gist.github.com/keijiro/f8c7e8ff29bfe63d86b888901b82644c . No camera-rendered production audience currently needs it. Kino, its transient NoiseShader dependency and the evaluation registry were removed after evaluation. No production renderer asset or render-pipeline selection was replaced. The optional KinoCompatibilityTests probe can be rerun after explicit reinstallation.

UIEffect isolated tests ran before installing Kino, so package regressions could be attributed independently. Final UI interference is scoped procedural static with UIEffect tone/RGB filtering; it does not sample or warp the complete underlying UI framebuffer. This deliberately preserves mission text and unrelated graphics.

## Sequences and authoring API

The same scene-scoped EffectDirector owns definitions, target surfaces and EffectHandles. EffectSequence is a ScriptableObject with stable ID, channel, target, priority/conflict policy and an array of EffectCue entries. Each cue has an EffectDefinition, relative offset, duration, intensity and phase label (Preparation/Disturbance/Peak/Recovery). Phase labels are authoring aids; offsets control timing. Cue order need not be sorted. Duration is the latest cue end, including delays and holds.

```csharp
// References are serialized by the caller. No global effect singleton.
EffectHandle handle = effects.PlaySequence(sequence,
    new EffectContext { Intensity = .7f, Seed = 147 },
    condition: () => audienceStillPermitsPresentation);
// The callback only supplies a presentation condition; the director queries no mission/location state.
effects.Stop(handle);
effects.StopChannel("horror.sequence", EffectTarget.Global);
```

A parent owns its cue handles and cancels every child at its authored end. Unrelated audio/effects remain alive. Invalid/missing audiences suppress cues; hidden/destroyed audiences, caller conditions, pause, Rest/Restore transient reset and scene teardown cancel safely. A long frame skips expired cues rather than replaying old one-shots. Conditions that throw cancel with a warning. Parent sequence priority applies to cues while preserving a higher authored child priority. Existing native priority arbitration, persistent resource effects and scoped channels remain intact. Cap: 16 concurrent sequences, 128 ordinary requests, 16 audio voices. Handles and sequence state never enter save DTOs.

Example `CombinedHorror.asset` implements relative timing: environmental duck at 0.00, scrape at .45, light dip at 1.20, Radar interference at 1.35, local signal jitter at 1.55, recovery from 2.00 and ambience restoration at 3.50. It is a reusable presentation sample, not a mission event. No four-zone horror timeline, location trigger, resource charge or objective update was added.

### Lighting, shake and anomalies

LightBehavior offers SubtleFlicker, PowerInstability, ElectricalFailure and PressureReaction. Seeded low-rate interpolated noise replaces the old periodic-only flicker. Envelopes give onset/hold/recovery without brightening above the resource baseline; the atmosphere remains the existing illustrated-cabin adapter. Persistent critical Hull/Energy moods retain priority. No 3D lights are required.

ImpactShake remains the only transform writer and retains the player's reduced-shake preference. The legacy Shake API retains its .28-second/8-pixel caps. New profiles add short impact, hull vibration, directional scrape, pressure rumble and interface jitter, bounded to 8 pixels and 30 seconds. Global feedback still uses the outer cabin frame, apart from bob/breathing/scaling rigs. Scoped surfaces have dedicated presentation rigs parented inside their actual audience windows; sibling ordering and clipping therefore apply normally, and local jitter affects the signal/anomaly layer rather than moving unrelated UI. Artist-specific safe rigs can be explicitly rebound.

EffectVisualExecutor binds one dedicated signal layer and one optional sprite layer per surface. ScreenSignalInterference, AnalogSignalFailure and SubtleDisplayInstability have configurable intensity/duration/fades. At zero intensity or cancellation their UIEffect component and graphic are disabled; no background glitch render pass is installed. Anomalies support Sprite, target, normalized opacity, pixel position/size and fade profile, and use visibility gating. All graphics are non-raycast. No ghost artwork was generated; the silhouette preset remains empty until an artist assigns a Sprite or caller supplies EffectContext.Sprite.

### Add a new effect without runtime code

1. Use Create > G10 > Feedback > Effect Definition. Give it a unique ID/channel and choose target, priority/conflict, duration, intensity and optional audio/light/visual/shake settings. Keep gameplay-critical warnings in protected audio groups.
2. Add the definition to the cabin director catalog for string-ID playback, or supply the ScriptableObject directly to Play/cues. Avoid assigning a child effect to its parent sequence channel; this invalid configuration is rejected.
3. Create an Effect Sequence and add timed cues; assign it directly to the future caller's serialized field. Target overrides in EffectContext redirect the whole request; by default each cue retains its authored target. Caller conditions do not transfer gameplay authority to the director.
4. Use the preview, then cancel, pause, hide the audience and unload the scene. Check that persistent resource feedback survives. Do not author rapid flashing, large cabin shake or loud stingers as defaults.
5. For a new audience, explicitly bind an EffectSurface visibility root/group and a dedicated presentation rig. Native tint uses FitToAudience projected bounds. New visual rigs are children of the real audience to respect window ordering and masks; unusual nested canvases still require layout QA.

## Demonstration

Open **G10 > Horror > Presentation Preview**. The already-created scene is `Assets/_Project/Scenes/Testing/HorrorPresentationPreview.unity`. Enter Play Mode there and use nine preview controls. The scene reuses Cabin.png and has no expedition, navigation, mission, Day, damage or save component. Optional sample audio and artist silhouette are assigned through the Editor window; the window refuses playback outside this preview scene. Recovery clears all preview transients and returns the mixer to Normal. Preview values are not saved as gameplay progress. Do not add this scene to production Build Settings.

To reapply bindings, open Zone01 and run **G10 > Horror > Install Foundation In Open Cabin**. Existing authored definition tuning is preserved on repeated installation. Setup creates/extends presentation children only and preserves native graphics/artist references. Mixer creation, definitions and sequence assets are idempotent.

## Validation

Executed run details and latest results are recorded in `Docs/QA/HorrorFoundation/results.json` after completion. Logs/HorrorFoundation contains native XMLs and probe artifacts (ignored local files). Final full EditMode: **81 PASS, 0 FAIL, 1 SKIP** (Kino absent by design). Earlier broader PlayMode regression: **77 PASS**. Final runtime-focused PlayMode: **23 PASS**. Latest generated-scene smoke: **5 PASS** (impact/hitboxes, Rest/pause, Continue/death, RockBreaker and tutorial). These are overlapping runs, not additive coverage. Latest-per-case PlayMode coverage is 80 distinct passing cases; 52 retain the earlier broader run and were not rerun after final profile/binding hardening. No full-repository final PlayMode run is claimed.

The native pipeline remains Assets/Settings/UniversalRP.asset. Production and preview have zero missing scripts; 221 GUIDs resolve. Reinstalling keeps the production hierarchy at 812 transforms with one director and six surfaces. New visual rigs are confirmed children of their audience windows. Actual existing scrape playback is loaded, panned -0.6 into Anomalies; a temporary loop diagnostic produced nonzero listener RMS 0.020219. Recovery produced zero sequences/transients/voices/enabled glitches and Normal mix.

Three offline tool checks passed (dry-run, approval guard, missing-key guard), without remote calls. Two earlier test-harness errors are recorded in results.json with passing retests. Final compilation and Console error counts are zero. Unity-generated font caches, test-run settings deletions and experimental Quality/PackageManager serialization were restored to the clean baseline. PlayerSettings retains only the intentional UIEffect settings preload entry. Whitespace validation ignores only Unity-generated blank-at-EOL scalars. See [all changed files](QA/HorrorFoundation/modified-files.txt), [machine-readable results](QA/HorrorFoundation/results.json), [normal preview](QA/HorrorFoundation/preview-normal.png) and [combined preview](QA/HorrorFoundation/preview-interference.png). Human headphone/speaker listening balance, flashing comfort, standalone Windows build and profiler measurements remain separate manual validation; automated state assertions do not establish subjective production audio quality.
