# Zone 02 — Cổ Thụ Linh Hồn, Environmental Horror V2

Implemented against the user's 2026-10-22 script and clarification: **cabin lighting means the whole player viewport**, including the current HUD, Map, Radar, Computer and Photo Lab. Global tint/dips use the full-screen EffectDirector surface. Signal corruption remains scoped to its visible Radar/Computer display. No live Camera panel is introduced: quick shutter → Photo Lab → SEND remains the photo flow.

## Presentation and triggers

| Scene | Gameplay condition and presentation |
| --- | --- |
| 01 | Physical travel from Zone01 retains the existing dark transition and Zone02 title. On arrival, a cool full-screen tint and a quiet root/water bed replace the singing colony. Machinery, ventilation, water and movement loops reuse the existing controlled voices. |
| 02 | Distant root flex/friction alternates at 28–42 s intervals outside specimen proximity and dramatic sequences. Existing organic art is reused as large, still background forms in newly captured photographs. |
| 03 | First organism at `zone02-l1`, 280 m. Quick photo → actual SEND → CAPTURE adhesive sample. Quiet wet specimen layer nearby, collection sound and material readout. |
| 04 | First sample acquired, 150 travel units later, at least 8 quiet seconds, actual Radar sweep visible: <1 s signal tear/static, brief whole-screen dip, then a low outside movement sound. No contact or mission data is added. |
| 05 | After the radar event, 300 travel units since sampling and 10 quiet seconds, moving at Helm: familiar three knocks, center/rear impression, a longer final echo. **Audio only: no shake, dim, interference or new contact.** |
| 06 | After knocks, 500 travel units since sampling and 12 quiet seconds, Computer visible: long exterior scrape, reduced machinery mix, analog split/RGB drift on Computer, two whole-screen dips (second deeper), quiet cabin tick and one Lily subtitle. No Lily speech was generated. No Hull damage or control fault. |
| 07 | Second organism at preserved POI ID `zone02-l3`, moved from 510 m to **460 m** so both materials are reachable with the existing 500 m hull. SEND precedes CAPTURE. Soft dry shell layer and collection clack. Generic random post-capture scares are suppressed for Zone02. |
| 08 | Upgrade → Expedition Module now shows **PRESSURE HULL**, one adhesive sample and one hardened sample. A single save transaction consumes both, installs the 750 m limit and unlocks Zone03. Passive installation diagram/readout with three progressively lit hull plates; the full-screen baseline stays stable. |
| 09 | Hull installed, moving at Helm within 400 units of the exit, at least 8 quiet seconds: dim at 1.0 s, still faint reflected figure at 1.2–1.4 s, restore afterward. The figure is clipped to the existing MiniMap instrument glass with RectMask2D and UIEffect, never added to a photograph or radar contact. |
| 10 | Physical Zone02→03 travel fades root/machinery layers, gives the final distant root groan time to end, then the existing full-screen transition presents the Zone03 title. |

These are opportunity-based triggers. They wait for the player to view the relevant instrument; they never open or lock a window. Dramatic events are spaced and stored as per-zone timeline receipts, and are not replayed by Continue or an ordinary panel switch. Restoring an earlier Journal checkpoint restores its exact receipt set. Pausing, disabling the adapter or a timeline change cancels owned transients. A modal/direct interaction/capture cancels running visual sequences and delays new dramatic cues; ordinary environment and an already-started one-shot can continue.

## Existing effects library

- `EffectDirector`, `EffectSequence`, pooled `EffectAudioExecutor`, `EffectSurface`, `EffectMotion` and `HorrorDisplay.mat` are reused.
- The installed **Coffee UIEffect 5.11.7** supplies Grayscale + RGB Shift treatment for the transient reflection graphic, using the existing registered shader variant. No new package, renderer feature or render-pipeline setting is required.
- `EffectDefinition.tintColor` extends the existing Tint overlay; old definitions default to black. A dedicated Reflection target is appended to the enum, preserving the numeric values of existing targets.
- The reflective instrument uses the existing temporary silhouette asset; the underwater photographs reuse current prototype art. This task generates audio, rather than new creature/environment illustrations.

## Missions, saves and ownership

The existing `ZoneOneStory` component is the project's shared mission adapter for all logical zones. It now recognizes the Zone02 Pressure Hull gate; `ZoneMissionRuntime` remains the authority for objective/reward transactions. Stable L1/L3 photo and existing L3 capture IDs remain unchanged. The unused old L2 collection location is removed from the authored map/config; old IDs and cargo in saves are preserved.

Capture sample cargo retains the existing `Z2_Creature_01` / `Z2_Creature_02` item IDs so previously collected shell samples, inventory serialization and regular upgrade costs remain compatible. The Pressure Hull UI names them adhesive/hardened materials.

A completed legacy Zone02 Bio Lamp gate migrates to the new hull gate and added first capture requirement without granting cargo or consuming materials again. Loading a completed hull enforces the earned 750 m capability even when scene preview ship settings are enabled. Failed installation writes roll back cargo, ship depth and gate together.

`ZoneTwoPresentationState` is an additive save field with empty defaults for old timelines. It stores cue receipts and first sample distance, never playback handles. `ZoneTwoPresentationController` owns presentation only. The shared scene is `Zone01.unity`; logical Zone02 continues through `CabinZoneSession`.

## Audio

Generated through ElevenLabs v2: **14 new source requests**, **527 credits from response headers**, plus one derived rear-knock clip from the existing Zone01 single-knock source. The quota endpoint did not advance immediately, so the credit count is based on individual response headers rather than a quota-delta claim. All 15 production WAVs are stereo PCM16/44.1 kHz; objective decode/level checks found no clipping. Loop joins use a 150 ms overlap.

Sources, exact prompts, hashes, processing and plan/license provenance are retained under `Assets/_Project/Audio/Zone02/`. The account is Free at generation; existing noncommercial license restrictions remain recorded with each file. No speech, music, top-up or package installation was performed. Subjective listening balance is still for human review.

## Authoring and validation

Open the shared Zone01 cabin and run **G10 → Zone 2 → Install Environmental Horror V2** to recreate references and effect assets idempotently. MissionConfigEditor also produces the new Zone02 mission. The installer preserves existing GUIDs, unrelated catalogs and surfaces.

QA reports and native screenshots are in `Docs/QA/Zone02EnvironmentalHorror/`. Test saves/photos are isolated in temporary folders; the user's real timeline is not reset. No commit, merge or push is part of this task.
