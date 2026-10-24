# Zone 3 native integration and verification — 2026-10-23

The existing shared cabin scene is installed and saved. Zone 3 uses 18 real audio clips, 30 effect definitions and 6 sequences under the existing EffectDirector. Canonical mission sites are 2/2/3/4 across the four zones. No live Camera panel was restored: quick shutter → Photo Lab → manual SEND remains the gameplay flow.

## Native results

| Check | Result | Evidence |
| --- | --- | --- |
| Native C# / shader compilation | Zero errors at installation | [Final installation audit](native-final-installation.json) |
| EditMode, job 1e9f8cd54b9c | 98 passed, 0 failed, 1 skipped | [Full native results](editmode-final.json) |
| Focused Zone3 tests | All 12 current cases passed, including natural radar/reflection triggers and Continue | [Latest result per test](playmode-latest-by-test.json) |
| Broad PlayMode, job 2dcef966a072 | 38 passed / 49 selected; 11 fixture follow-ups | [Full original results](playmode-regression-initial.json) |
| Targeted final follow-up, job 75340202b238 | 14 passed, 0 failed | [Full native results](playmode-final-followup.json) |
| Final pressure-frame capture, job 1f586e60f70e | 1 passed, 0 failed | [Native result](playmode-final-pressure-capture.json), [rendered metrics](D-pressure.json) |

The optional skip is the isolated KinoGlitch compatibility evaluation; KinoGlitch is intentionally absent from production. These are current native results, separate from historical source-branch totals and the earlier offline Roslyn compile.

The [latest results across 50 distinct selected PlayMode cases](playmode-latest-by-test.json) are **50 passed, 0 failed**. This combines the original regression run with the targeted reruns; it is not a claim that every repository test ran in a single green suite. It covers all-eleven-site PHOTO/manual SEND, normal/hidden endings and physical Zone4 travel, capture lifecycle, rollback/Continue/rest/Journal, prior Zone1/2 presentation and selected tutorial cases.

Seven E2E and three ZoneExitFlow fixtures initially stopped in setup because their old 25-second deadline was shorter than the real 35-second opening. Their 45-second deadlines now pass. The Rock Breaker test now verifies actual director-owned pressure/fracture pulses, rest between impacts, bounded displacement, no-shake accessibility and exact frame restoration. The new natural-trigger case uses real PHOTO/SEND, a single charged radar scan and actual navigation distance; saved radar/reflection receipts prevent replay after Continue. Production timing and mission rules were not bypassed.

The first pressure still captured the scheduling frame, and a later synchronous PNG-encoding hitch skipped the entire short pulse. The final capture drains that QA frame before firing the sequence, then captures the real pressure attack. Its upper-half comparison changes 99.28% of pixels with 10.93% mean channel difference, independently of the status label. No production runtime repair was needed for that capture-induced hitch.

## Actual demonstration evidence

These are real 1920×1080 Game View captures using production assets and surfaces. A and D also record the actual Unity AudioListener output, stereo 48 kHz PCM16, without replacing it with source WAVs.

| Demo | Captures / recording | Verified behavior |
| --- | --- | --- |
| A — delayed engine | [Moving](A-01-real-engine-moving.png), [stopped](A-02-stopped-engine-reflection.png), [listener WAV](A-gameplay-listener.wav), [mix metrics](A-gameplay-listener.json) | Real held propulsion is audible for 2.2 seconds before braking; direct thrust stops, designed reflection continues once; no gameplay-authority changes during the echo. |
| B — false radar | [Before](B-01-radar-before.png), [secondary sweep and three ghost rings](B-02-false-secondary-sweep.png), [recovered](B-03-radar-recovered.png), [metrics](B-false-radar.json) | Real circular scope changes without an extra scan, contact discovery, charge or mission write by the preview. |
| C — reflection | [Before](C-01-monitor-before.png), [blurred reflection](C-02-procedural-glass-reflection.png), [recovered after pause](C-03-monitor-recovered-after-pause.png), [metrics](C-reflection.json) | Soft head/shoulder shape is confined to the authored tilted map glass; production sound plays; pause cancels owned transients and resumes normal machinery. |
| D — demolition | [Before](D-01-cabin-before-break.png), [pressure pulse](D-02-pressure-impact.png), [collapse/debris](D-03-layered-collapse-debris.png), [stabilized](D-04-cabin-stabilized.png), [listener WAV](D-gameplay-listener.wav), [mix metrics](D-gameplay-listener.json) | Real craft/install/destroy authority, multiple sound layers, pause audio seek, terrain reveal at 1.5s, gameplay stabilization at 10s and bounded resonance tail afterward. |

[Photo/window evidence](E-03-foreground-window-over-effect.png) and its baseline E-04 prove the background Photo Lab effect does not paint over a foreground mission window. E-05 records reflection on the actual tilted glass. Isolated GPU tests also check circular clipping, moving sweep pixels, distinct head/shoulder volumes and cleanup after sixty activations.

The A capture initially found clipping caused by overlapping capture-success/grab playback. The dedicated capture-device voice now replaces its prior clip, and CreatureCatcher is the sole validated success cue owner. The failed original recording/metrics remain under initial-failed-evidence/. The latest A and D listener metric files report zero clipped samples; source-file checks alone were not used to make that mix claim.

## Preservation and lifecycle

- [Final native audit](native-final-installation.json): saved scene, 30 unique definition IDs, all sequence cues assigned, all 18 audio references present, Rock Breaker bound, zero missing scripts, zero shader errors and inactive legacy Camera panel.
- [Final map comparison](map-preservation-final.json): line endings normalized; all non-mission top-level YAML fields preserved, including artwork, terrain, entry/exit/rock geometry and lighting. Only intended Zone3/4 location/hidden-location lists changed. Pre-install copies remain under scene-integration-before/.
- Tests use isolated temporary save/archive paths. The user's actual timeline was not migrated or rewritten by these tests.
- Dynamic font cache changes from native verification were restored to the exact pre-install copies, preserving their preexisting local changes. The Editor was left stopped on clean Bootstrap; its serialized bytes were unchanged by clearing the Editor dirty flag.
- The production recorder does not modify the audio buffer. EffectAudioExecutor remains bounded to its existing sixteen pooled voices; the capture device reuses its one existing source.
- Procedural surfaces have no Update/coroutine/full-screen readback; each activated surface owns one reused graphic/material. Repeated activation, suspension and destruction tests verify the real rendered baseline returns and the owned material is released. Disabled graphics do not remain visibly composited. This is not a target-hardware GPU-time measurement.
- No packages, render pipeline or original creature art were replaced. Existing UIEffect/captured-frame distortion is retained; authored EffectSequence assets remain the central clock under EffectDirector.

## Production sources and review limits

[Audio production/provenance](audio-production.md), [independent verification](audio-verification.json), [budget receipts](audio-budget.json) and [production overview](../../Zone03CinematicProduction.md) document all 18 stereo 44.1kHz source assets, 20 original ElevenLabs requests and 445 header-reported credits within the bounded 1,800-credit ceiling. The files total 15,364,524 bytes; hashes, silence, clipping and loop boundaries pass independent checks. No voice or music was generated.

The offline reels audio-demo-A-engine-then-reflection.wav, audio-demo-D-demolition.wav and audio-review-reel.wav remain audition aids; they are not Unity recordings. No compatible Recorder package was installed, and no video was fabricated.

Human headphone/speaker review is still needed for mineral-versus-bell timbre, Creature06 avoiding chimes, engine/reflection clarity at player volume, apparition ambiguity and demolition balance. Still-frame/pixel checks do not certify the subjective horror effect. Active shader GPU/audio CPU cost, target-hardware frame rate and a standalone player build have not been measured. Audio-generation plan/license provenance is retained in the audio report.

## Resolved integration issues

The original MCP policy block was resolved. A later Unity save dialog blocked the first PlayMode transition; the user dismissed it and the queued run completed. No direct HTTP bridge, alternate Editor controller, unsafe queue cancellation or unconfirmed test success was used. The first shader probe also found reserved HLSL identifier point; it was corrected and the final shader compiled cleanly. Original failed test logs are retained as playmode-initial.json and playmode-refined.json.
