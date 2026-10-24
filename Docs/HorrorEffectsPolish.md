# Horror effects: generated content and presentation polish

Implementation on `main`. No commit, merge or push.

## Real audio

Ten successful ElevenLabs generation requests, first two files decoded and imported before the remaining eight. No audio generation retries. Model `eleven_text_to_sound_v2`, prompt influence `0.5`, output `mp3_44100_128`; exact prompts, timestamps and source hashes are in each `.provenance.json`. Requested durations: 4 / 6 / 7 / 20 / 20 / 20 / 20 / 3 / 4 / 5 seconds in the user's order. The four ambient requests used `loop=true`.

Converted to stereo 44.1 kHz, 16-bit PCM WAV. One-shots peak at approximately −3 dBFS. Ambience targets −28 dBFS RMS with at least 6 dB peak headroom. A 40 ms wrap crossfade reduces each 20 s ambience to 19.96 s. All files decode, contain nonzero audio, and have zero clipped PCM samples. Per-file measurements are retained in provenance metadata.

| File ID | Project path | Duration | Playback | Billed credits |
|---|---|---:|---|---:|
| `ambient_ancient_roots` | `Assets/_Project/Audio/Horror/Ambience/ambient_ancient_roots.wav` | 19.96 s | Loop | 200 |
| `ambient_deep_abyss` | `Assets/_Project/Audio/Horror/Ambience/ambient_deep_abyss.wav` | 19.96 s | Loop | 200 |
| `ambient_ruins` | `Assets/_Project/Audio/Horror/Ambience/ambient_ruins.wav` | 19.96 s | Loop | 200 |
| `ambient_singing_algae` | `Assets/_Project/Audio/Horror/Ambience/ambient_singing_algae.wav` | 19.96 s | Loop | 200 |
| `horror_distant_impact` | `Assets/_Project/Audio/Horror/Anomalies/horror_distant_impact.wav` | 5.00 s | One-shot | 50 |
| `horror_electrical_interference` | `Assets/_Project/Audio/Horror/Anomalies/horror_electrical_interference.wav` | 3.00 s | One-shot | 30 |
| `horror_hull_scraping` | `Assets/_Project/Audio/Horror/Anomalies/horror_hull_scraping.wav` | 6.00 s | One-shot | 60 |
| `horror_knock_three` | `Assets/_Project/Audio/Horror/Anomalies/horror_knock_three.wav` | 4.00 s | One-shot | 40 |
| `horror_pressure_groan` | `Assets/_Project/Audio/Horror/Anomalies/horror_pressure_groan.wav` | 7.00 s | One-shot | 70 |
| `horror_unidentified_breathing` | `Assets/_Project/Audio/Horror/Anomalies/horror_unidentified_breathing.wav` | 4.00 s | One-shot | 40 |

The account was Free, with 8,400 included credits available before generation. Header `character-cost` totals **1,090 credits**, confirmed by account usage changing from 1,600 to 2,690; **7,310 credits remain**. The conservative documentation-based estimate was 4,360; actual endpoint billing was lower. No top-up, plan purchase or overage was enabled. Immediate per-request quota deltas were delayed, so they are not the final billing total.

**License limitation:** these files were generated on Free and are for non-commercial use. Attribution is required on publication. They are real playable candidates, but they do not have a commercial release license. Obtain the appropriate rights before shipping commercially; upgrading later should not be assumed to retroactively license these generations. [ElevenLabs publication policy](https://help.elevenlabs.io/hc/en-us/articles/13313564601361-Can-I-publish-the-content-I-generate-on-the-platform).

API verified against the [official conversion reference](https://elevenlabs.io/docs/api-reference/text-to-sound-effects/convert) and [Sound Effects documentation](https://elevenlabs.io/docs/overview/capabilities/sound-effects). Credentials remain outside the repository and are not required at game runtime.

## Wiring and playback

All nine preview presets have authored defaults. Optional overrides apply to individual presets; the combined demonstration always retains its synchronized authored content. Three knocks plays the generated four-second motif once. Scrape, pressure, electrical interference, breathing and impact use the matching generated clips. Recover clears transient sounds/visuals and restores the selected ambient loop without a stinger. All four ambient loops are selectable.

The existing AudioManager, category mixer groups, pooled EffectAudioExecutor voices, user volumes and pause/cancellation contracts remain in use. Machinery carries hull pressure; Anomalies carries knocks/scrape/breathing; Interface carries electrical sounds; Impacts carries distant impact; UnderwaterAmbient carries loops. `EffectContext.AudioMultiplier` permits preview audio volume to be independent of visual intensity while preserving legacy intensity behavior for existing callers.

Production audio uses serialized AudioClip references. The preview scene contains no ship, mission, Day, resource, inventory or save components. Zone01's presentation catalog and screen/illustration bindings were updated without changing gameplay authorities.

## Visual behavior

- Flicker directly modulates the illustrated CabinArt color. Mood changes still ease; irregular electrical dips reach the painting immediately. Subtle/Medium/Strong controls are available.
- Radar and Computer capture their actual rendered display once per activation, then a scoped uGUI shader applies tearing, RGB separation, scanline noise and narrow signal loss. This works with Overlay canvases and the existing 2D renderer. It temporarily holds the display frame during interference; detection logic and live UI data continue underneath. No render pipeline replacement or camera post-processing dependency.
- Radar is restricted to its scope and circular clipping; Computer uses its own screen region and rounded edge. All graphics ignore raycasts and turn off after playback/cancellation. No generic dark plates cover the cabin.
- Hull pressure combines the seven-second groan, a slower sustained vibration, illustrated light dips and stress-driven decorative needle jitter. The existing screen-shake preference suppresses vibration and added needle jitter. Navigation values are untouched.
- The replaceable transparent crew silhouette is temporary AI artwork: `Assets/_Project/Art/Horror/TemporaryCrewSilhouette.png`. Source prompt and status are in the adjacent README. It is not final artist artwork.

Combined sequence: ambience continues at 0; duck begins at 0.8 s; faint structural movement at 1.5; scrape at 2.2; lighting at 2.7; Radar interference at 3.5; Computer interference at 3.65; impact at 4.2; vibration at 4.25; silhouette at 4.5. Lighting recovers at 5.0; normal ambient mix starts returning at 6.5. Total duration is 9.2 s to preserve the scrape/impact tails.

## Verification

Runtime measurements and screenshots: `Docs/QA/HorrorPolish/preset-runtime.json`, `ambient-runtime.json`, `frame-comparisons.json`, and `preset-N-before/peak/after.png`. Test XML is under `Docs/QA/HorrorPolish/results/`.

Validation completed in the visible Unity Editor:

- Edit Mode: **81 passed, 1 skipped**, 0 failures. The skip is optional Kino compatibility because Kino is not installed.
- Play Mode regression run: **53 passed**, 0 failures. Covers director, presentation, cabin feedback, Rock Breaker, atmosphere and tutorial.
- Final focused run: **3 passed**, 0 failures, after the optional override safeguards. Covers all nine real presets, capture teardown, and actual Zone01 Radar/Computer bindings. Across both Play Mode runs, **54 distinct test cases** passed.
- Every sound-related preset produced nonzero source and listener output. Four individual ambient loops also produced nonzero source output and had AudioSource.loop enabled.
- Maximum measured mixed output sample across the nine presets: **0.7334**, below digital clipping. Source WAV files have zero clipped samples.
- Pressure test observed decorative needle movement and global vibration. The production check kept pressure audio when screen shake was disabled.
- All nine before/after preview image pairs were **pixel-identical** after restoration. During-effect changes are recorded in `frame-comparisons.json`.
- Production Radar effect changed pixels only within approximately x390–806/y326–726, matching the scope. Radar contact/terrain-echo counts and Hull/Energy stayed unchanged. Computer effects rendered on the actual fullscreen desktop; closing it cancelled the effect.
- No C# compilation errors or Console errors at final validation. `git diff --check` passed with Unity empty-scalar trailing-space handling. No Package or Project Settings content changes.

| Visual evidence | Before | During |
|---|---|---|
| Illustrated cabin lighting | [Normal](QA/HorrorPolish/preset-3-before.png) | [Flicker](QA/HorrorPolish/preset-3-peak.png) |
| Real Radar panel | [Normal](QA/HorrorPolish/production-radar-before.png) | [Interference](QA/HorrorPolish/production-radar-during.png) |
| Real Computer desktop | [Normal](QA/HorrorPolish/production-computer-before.png) | [Disturbance](QA/HorrorPolish/production-computer-during.png) |
| Combined presentation | [Normal](QA/HorrorPolish/preset-8-before.png) | [Peak](QA/HorrorPolish/preset-8-peak.png) |

These are measured audio output and inspected frame captures, not a claim of human headphone listening. Human review is still needed for timbre, the exact three-knock rhythm, subjective tension, loop seam perception and final art approval. Main visual evidence was captured at 1920 × 1080; other aspect ratios/platforms have not been aesthetically reviewed.
