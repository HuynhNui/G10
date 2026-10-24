# Hull collision audio and three-knock screen feedback — 2026-10-21

The requested terrain collision sound was generated through ElevenLabs and installed in the existing global `hull.impact` definition. Three-knock gameplay now plays the existing baked stereo audio and three short screen impulses from one `HullKnocks` sequence.

## Audio

- New `Assets/_Project/Audio/Zone01/SFX/sfx_terrain_impact.wav`: 2.0 s, stereo PCM16, 44.1 kHz, peak −3.000 dBFS, RMS −21.933 dBFS, no clipped samples. Source MP3 and SHA256 provenance retained.
- One generation request, response header `character-cost=20`. Existing included quota; no top-up. Plan at generation: Free; rights recorded in provenance. Human listening review is still pending.
- The previous clip measured peak −19.6 dBFS and RMS −33.7 dBFS. New clip routes to Impacts instead of the default Machinery group. Existing speed-based gain/pitch, damage, contact latch and collision screen feedback remain authoritative.
- Play Mode triggered a real navigation collision against a chart wall. The new non-looping AudioSource played with nonzero volume, correctly routed to the Impacts mixer, remained playing after the visual ended, stopped after its tail and played again only after clearing and re-hitting the wall. This verifies playback state/routing, not subjective listening quality.

## Screen feedback

- Baked knock attacks: 0.12 / 0.72 / 1.32 s. Observed visual onset against actual AudioSource playback: **0.128 / 0.725 / 1.323 s**.
- Three 0.24 s ShortImpact pulses, configured amplitude at most 7 px and black tint at most 10%. Captured runtime peak displacement approximately **3.01 px**. No resource loss, lighting change, false radar contact or new camera interface.
- Initial testing exposed clock drift when save work stalled a frame. The optional sequence audio clock now follows AudioSource.time; sequence children use their parent's age and are stopped together. A missing clock voice cancels the sequence; expired cues after a hitch are not backfilled.
- Accessibility: disabling Screen Shake prevents all frame motion while retaining the restrained tint. Pause, timeline rollback and disabling the adapter cancel audio plus all visual children. Resume does not replay committed cues; explicit rollback to an eligible earlier checkpoint permits the event again.
- Screenshots: `01-before-knocks.png`, `02-second-knock.png`, `03-after-knocks.png`. Native 1920×1080 captures inspected; cabin controls remain legible during the pulse.

## Validation

**54 distinct Play Mode tests passed** across CabinFeedbackPlayModeTests, ZoneOneAudioGameplayTests, ZoneOneQuickPhotoPlayModeTests, EffectDirectorPlayModeTests and HorrorPresentationPlayModeTests. `playmode-final.json` lists every result and its source runs.

The first broad run passed 53/54. The existing legacy-click test assumed two frames were enough for slow reverse acceleration; very short Editor frames did not cross the navigation movement epsilon. Its probe now waits up to 0.5 s for actual movement. That test passed on rerun; production code was unchanged between those runs. Both raw reports are retained.

Zero C# compile errors. Native serialized inspection confirms the new clip, Impacts category, audio-clock sequence reference and zero missing scripts. The legacy CameraPanel remains inactive with no observation controls. Save/photo overrides are null. Editor returned to Bootstrap, stopped and clean. The actual user save was not reset. No commit, merge or push.
