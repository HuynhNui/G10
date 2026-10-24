# Zone02 Environmental Horror V2 — 2026-10-22

Completed the requested ElevenLabs audio and installed-library screen effects in the shared cabin, with the Zone02 mission conditions needed to drive the script. The whole player viewport dims, including Map/Radar/Computer/Photo Lab. Local signal interference stays scoped to the relevant instrument. Quick photo → Photo Lab → SEND remains the UI.

## Implemented and inspected

- **14 ElevenLabs source requests + 1 derived rear-knock clip**, 15 bound production WAVs. Exact prompts, original MP3s, processing, plan/license and SHA provenance retained under `Assets/_Project/Audio/Zone02/`. Billed response headers total **527 credits**. No speech or music generated. Decode/format checks: stereo PCM16/44.1 kHz, no clipped samples; loop seam maximum approximately 0.000383. Human listening/balance approval is still pending.
- Root/water bed, intermittent root flex/friction, moist/dry specimen layers, collection actions, radar static, outside movement, long hull scrape, cabin tick, electrical fizz, hull installation and final distant root groan are connected to gameplay.
- Radar interference, Computer analog split, two whole-screen dips with a deeper second dip, one Lily subtitle, a .2 s stationary reflection clipped to existing instrument glass, and arrival/departure fades. Zone02 knocks are intentionally audio-only. No invented contacts, photo subjects, Hull damage or control failure.
- Coffee UIEffect 5.11.7 Grayscale/RGB Shift is applied to the reflection; existing EffectDirector/EffectSequence/HorrorDisplay handle presentation. Native capture exposed the Editor asynchronous shader's cyan placeholder during the short glimpse. A referenced two-variant collection now warms at scene load; the installer also compiles those passes synchronously. Pixel checks reject cyan placeholders; final images contain zero cyan debug pixels. Diagnostic-only grayscale registration was removed from the global UIEffect settings after the probe.
- Two locations, both SEND then CAPTURE, material list in the actual Upgrade UI, one atomic Pressure Hull purchase, 500 → 750 m and Zone03 unlock. Second contact is at 460 m, reachable before the upgrade. Old L1/L3 IDs and cargo are kept; completed legacy Zone02 routes migrate without new cargo grants.

## Validation

**12/12 Zone02 tests passed** in the broad regression, including actual shutter/archive/SEND, minigame capture, the actual module INSTALL button, persistence, failed-write rollback, ordered opportunities, audio-only knocks, Computer signal rendering, two screen dips, brief library reflection, clipping/cleanup, pause/Journal rollback, old completed mission migration, and physical arrivals/exits with titles and voice cleanup.

**83 distinct Play Mode checks passed after reruns** (`playmode-final.json`), across eight fixtures. The initial broad run passed 81/83. One existing generic capture-escalation test was moved from Zone02 to Zone03 because the new Zone02 explicitly suppresses random scares after capture; its remaining behavior passed. The held-control source probe intermittently stopped when actual navigation stopped (turning/depth movement false, resources available); an isolated diagnostic rerun passed for all three controls across clip boundaries and release/re-press. No production control or loop code was changed to conceal that result. Raw failed and passing runs are retained.

**24/24 EditMode checks passed** for EffectDirector, mission progress and expedition economy. Zero project C# compile errors. Native inspection confirms 15 clips, no duplicate effect IDs, no missing scripts, one Zone02 adapter, 1920×1080 Global tint, the correct hull gate, 280/460 m sites, inactive legacy CameraPanel, and null temporary save/photo overrides. Native UIEffect shader error count is zero.

The whole-screen pixel check measures mean rendered luminance in four actual panels. With the authored dim active, measured ratios to baseline were approximately **0.844 Map, 0.842 Radar, 0.843 Computer and 0.845 Photo Lab** (`global-screen-pixels.json`). This verifies rendered UI pixels, beyond inspecting a light/alpha property.

Gameplay test drivers position the vessel and use open-water chart cells to isolate gate/presentation behavior; they do not establish human navigation difficulty or a complete hand-played voyage. The existing terrain artwork and prototype creature/background art are reused. No standalone release build or subjective audio-device listening claim is made.

## Artifacts

- `01-specimen-photo-lab.png`: new quick photograph in the real Photo Lab.
- `02a-pressure-hull-materials.png`, `02-pressure-hull-installed.png`: actual material requirements and installed readout.
- `03-first-screen-dip.png`, `04-second-screen-dip.png`: transient event captures; these can fall late in a brief pulse. `07-global-screen-dim.png` and `global-screen-pixels.json` are the stable rendered-coverage proof.
- `05-glass-reflection.png`: in-sequence glimpse. `06-reflection-library-ready.png`: longer diagnostic exposure of the same graphic/material to inspect the library result. `06a/06b/06c` images are historical shader diagnosis, not final presentation.
- `native-inspection.json`, `playmode-final.json`, `editmode.json` and raw initial/rerun reports.

Tests used isolated temporary saves/photos and restored overrides/preferences. Editor returned to Bootstrap, stopped and clean. Existing user changes were preserved. No commit, merge or push.
