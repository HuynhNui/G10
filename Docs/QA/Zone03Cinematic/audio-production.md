# Zone 3 original audio production

Produced on 2026-10-22 with ElevenLabs `eleven_text_to_sound_v2`, using the installed sound-effects skill and the project's private authenticated workflow. No voice or music generation. There are **20 original source requests, 44.5 requested seconds, 445 actual credits from response headers**. The conservative preflight bound was1780 credits within an1800-credit ceiling and available included quota5121. No top-up or overage. The account quota endpoint lagged the response ledger; see `audio-budget.json` rather than inferring cost from its partial delta.

## Production bindings

Every production WAV is stereo44100Hz PCM16 in `Assets/_Project/Audio/Horror/Zone03/`. Each has a `.provenance.json` with prompt, source receipt/hash, processing, output hash, metrics, and license status. Sources and alternate candidates live outside Unity's imported asset set in `Docs/QA/Zone03Cinematic/audio-candidates/`.

| Cue | Files | Intended use |
|---|---|---|
| A01 Stone resonance | `stone_resonance_01/02/03.wav` | Uneven spaced mineral events, vary distance/pan; no hull-creak reuse. |
| A02 Engine reflection | `engine_reflection.wav` | Trigger on real propulsion stopping after sustained motion; a5.4s reflected texture continues afterward. |
| A03 Acoustic void | **No file** | Runtime mix automation reduces environmental layers while retaining machinery and essential interface/impacts. |
| A04 Creature05 | `creature05_pressure.wav` | Irregular wet micro-bursts, local photo/capture presentation. |
| A05 Creature06 | `creature06_crystal_motion.wav` | Restrained hard biological material motion. |
| A06 Creature07 | `creature07_bioelectric.wav` | Unequal bioelectric pulses, synchronized local screen treatment. |
| A07 False radar | `false_radar_echo.wav` | Presentation-only ghost response; normal radar source remains untouched. |
| A08 Reflection movement | `soft_movement.wav`, `close_click.wav` | Near-field restrained physical motion then intimate click. |
| A09 Assembly | `rock_breaker_assembly.wav` | Functional mechanical latches, actuator and engagement. |
| A10 Detonation | `underwater_detonation.wav` |6.3s heavy pressure event; keep existing terrain transaction authoritative. |
| A11 Collapse | `rock_collapse_01/02/03.wav` | Separate large block, medium rock, small grit layers; stagger offsets. |
| A12 Aftermath | `post_explosion_resonance.wav` |7.2s receding mineral cavity without exit scare. |
| Environment | `ambient_stone.wav`, `sparse_water.wav` | Quiet18s/15s beds; no engine baked in. |

The reflected engine uses the real propulsion recording plus a newly generated mineral cavity texture. It independently levels45–210Hz,310–940Hz and1100–2400Hz bands, then applies different delays/pans, unequal flutter rates, mild soft saturation and dark diffusion. This changes spatial, temporal and spectral structure; it is more than gain/pitch or a plain delayed copy. Runtime should retain direct propulsion authority and must not continue its actual machinery source after input stops.

The radar ghost uses a derived version of the familiar real ping, band filtering, irregular delayed stereo components and a new generated flutter texture. It does not replace the original ping or authorize scan contacts/charges.

The selected detonation had negative stereo correlation in its low-frequency source. Bass below180Hz was centered before the cavity design to retain impact on mono playback. Production headroom peaks at−4dBFS for the blast, with separate lower-level debris layers. Runtime buses must retain headroom when layering them.

## Candidates and review

Four distinct generated stone prompts and two distinct generated detonation prompts were retained. The near stone candidate first-pass selection uses lower high-frequency energy; the blast selection uses sub250Hz energy with a prompt onset. Selection details and unaltered source metrics are in `audio-selection.json`. This is a measurable first-pass production decision, **not a claim that a person or listening model auditioned the sounds**. No audio-listening analysis tool was available in this session.

Use the local WAVs for human audition:

- `audio-demo-A-engine-then-reflection.wav`: direct thrust0.7–3.4s; direct source stops; designed reflection3.7–9.1s.
- `audio-demo-D-demolition.wav`: assembly0s, pressure4s, large blocks4.38s, medium rocks5.12s, grit6.05s, aftermath6.2s.
- `audio-review-reel.wav`: all18 production assets in the order recorded in `audio-demo-timings.json`, with one-second gaps.
- `audio-candidates/stone_near.wav` and `stone_alternative.wav`; `detonation_pressure.wav` and `detonation_cavity.wav`: equal-peak candidate comparisons.

These are offline audition mixes built from the exact production assets. They are **not Unity output recordings or proof of runtime timing**. Actual gameplay evidence is listed separately below. Review on headphones and speakers for stone identity, engine distinguishability at the player's volume, Creature06 avoiding a chime impression, and restrained apparition loudness. The filters/metrics cannot certify those perceptions.

## Actual Unity listener captures — 2026-10-23

The following files were recorded from the enabled scene `AudioListener` during actual shared-cabin gameplay tests. The test-only recorder copies the real mixed samples without modifying Unity's audio data or changing volume preferences. It writes stereo PCM16 WAV at the actual output rate of 48 kHz. These recordings were not normalized or replaced with source WAVs; the JSON records the measured floating-point mix peak and out-of-range sample count before PCM conversion.

- [A-gameplay-listener.wav](A-gameplay-listener.wav), [measured A metadata](A-gameplay-listener.json): the final comparison includes **2.2 seconds of real held propulsion** before the stop edge, followed by the distinct production reflection while normal cabin machinery remains. The captured duration is 7.616 seconds, peak −4.6294 dBFS and RMS −22.9077 dBFS, with zero clipped samples.
- [D-gameplay-listener.wav](D-gameplay-listener.wav), [latest measured D metadata](D-gameplay-listener.json): actual layered demolition output using the installed production sequence. The latest JSON is authoritative for duration, peak, RMS and clipping after a rerun; its values can vary with the captured gameplay and sequence timing. The recording available at this update is non-silent with zero clipped samples.

The first A recording exposed overlapping capture feedback at the start of the mix: 281 samples exceeded digital range, with a floating-point peak of approximately +4.89 dBFS. That original WAV and metadata are preserved in [initial-failed-evidence](initial-failed-evidence/A-gameplay-listener.json). The correction gives the capture device one dedicated feedback voice: a new grab/result interrupts its previous capture sound instead of stacking `PlayOneShot` voices. `CreatureCatcher` now owns the sole success cue after the actual reward is validated and committed; the minigame no longer emits an optimistic duplicate success. Its failure cue remains once. The change preserves player volume preferences and other machinery, interface and impact sounds.

These captures verify real output and headroom for the measured scenarios. They do not certify timbre, dramatic quality or every possible combination of user levels and simultaneous sounds. Human listening review remains required, and the offline candidate/review reels above remain separate audition material.

## Verification and reproducibility

Run `Tools/Audio/generate_zone03.py --process` for offline DSP rebuild only. Run `Docs/QA/Zone03Cinematic/audio-verify.py` for independent file-format, count, hashes, silence, clipping, loop-boundary, engine spectral difference and blast mono-compatibility checks. `--execute` may resume only missing original requests; it refuses to replace a source or spend on an automatic retry. It always enforces the call/credit bounds first.

The verification covers18 production cues and20 source receipts. Every WAV is non-silent, zero clipping, with matching source/output hashes; both ambient loop endpoints match to PCM precision. Full metrics are in `audio-verification.json`.

The account plan at generation was **Free**. Under ElevenLabs' current published terms, these generated sources lack a commercial license; noncommercial sharing requires attribution. This provenance is retained so a later commercial-release review does not mistake them for paid-plan generations. See [ElevenLabs publication terms](https://help.elevenlabs.io/hc/en-us/articles/13313564601361-Can-I-publish-the-content-I-generate-on-the-platform) and the [Sound Effects price reference](https://elevenlabs.io/docs/overview/capabilities/sound-effects).
