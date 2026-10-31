# Zone04 — source-only preparation audit

Prepared from local production sources on 2026-10-26. This is a read-only source review. It contains no new playtest observation, measured player emotion, baseline timing, or assertion that historical tests still pass on the current working tree. No Unity MCP call, production edit, or new audio generation was made for this audit.

## Intended journey and ending authority

The current chapter already has a complete production presentation. [Zone04FinalChapterProduction.md](../../Zone04FinalChapterProduction.md) records earlier work and its historical evidence; those historical results must remain distinct from this experience loop's baseline and final validation.

The real mission asset has five objectives at four sites:

| Mission location | Actual POI | Required work | Authored target depth |
| --- | --- | --- | ---: |
| Z4_L1, visible | zone04-l1 | PHOTO, manual SEND, then CAPTURE of Z4_Creature_01 | 525 m |
| Z4_L2, visible | zone04-l2 | PHOTO and manual SEND of Z4_Creature_02 | 590 m |
| Z4_L3, hidden Radar site | zone04-l3 | PHOTO and manual SEND of Z4_Creature_03 | 615 m |
| Z4_L4, hidden Radar site | zone04-l5 | PHOTO and manual SEND of Z4_Creature_04 | 735 m |

The `zone04-l5` identifier is intentional. Hidden sites are not capture objectives. No additional craft/install gate exists in this chapter. L1 capture grants the existing coordinate hint; reading the hint grants no Radar discovery. The current L1 is next to the authored entry position, while L2 requires a long lateral voyage. These are geometry facts, not measured pacing conclusions.

Main completion requires L1 PHOTO + CAPTURE and L2 PHOTO. `ExpeditionProgression.Evaluate()` then brakes the ship and opens the existing choice: end the expedition or continue exploring. Selecting the first option publishes the `normal` ending. Selecting explore durably sets `endingChoice = "explore"` and `hiddenRouteUnlocked`; hidden discovery still requires actual Radar. Choice callbacks reject pause, zero time scale and transitions, and roll back on save publication failure.

The hidden final destination is available only when main work is complete, explore is selected, both hidden locations have actually been revealed, and both hidden locations are complete. Merely receiving the two coordinates or taking an unsent photo is insufficient. The authored final point is `(1500, 150)`, radius 75; it has no separate depth override. `ExpeditionProgression` observes fresh physical movement and actual arrival, then calls `RecordEnding("hidden")` before loading Ending. A restored saved position is not accepted as a new arrival automatically. Zone04 has no forward destination zone; the existing return gate leads to Zone03.

Source authority: `Content/Missions/Zone04.asset`; `Content/Maps/Zone04.asset`; `Scripts/Navigation/ZoneMapConfig.cs:191`; `Scripts/UI/ExpeditionProgression.cs:157`, `:231`, `:263`; `Scripts/Computer/ExpeditionLoop.cs:769`. All source paths here are under `Assets/_Project/`.

## Existing presentation opportunities and ownership

`ZoneFourPresentationController` observes committed gameplay. Its `Safe` guard requires initialized Zone04, no application pause, positive time scale, no expedition block/death/failure, no pause menu and no scene transition. It does not use application focus in this guard.

| Presentation | Eligibility and actual audience | Authored sequence | Current saved receipt rule |
| --- | --- | --- | --- |
| Capture recovery notice | Successful actual L1 capture | 2.5 s notice directing the player to Mission Log | Notice itself has no once receipt |
| Recovered coordinate fragment | L1 CAPTURE already complete; real Mission Log app visible | 3.6 s fragment card; coordinate A appears at 0.85 s, B at 1.65 s; processing reaches 1 at 2.5 s | `fragment` is committed as soon as a valid sequence handle is created |
| Hidden A | First inspection of a mission-bound `zone04-l3` photo in actual Photo Lab | 9 s `HiddenA` signal, on PhotoLab surface | `hidden.a` is committed at valid sequence launch |
| Hidden B | First inspection of a mission-bound `zone04-l5` photo in actual Photo Lab | 9 s `HiddenB` signal, on PhotoLab surface | `hidden.b` is committed at valid sequence launch |
| Final approach | Hidden destination available; moving ship (`abs(speed) > .1`), Helm visible, no modal/direct interaction, within 360 map units | 9 s local Helm water tint; final signature begins at sequence age 1.2 s, audio duration 5.8 s | `final.signal` is committed at valid sequence launch |

Photo mission bindings are only assigned by `PhotoCaptureService` for real GoodPhoto or LifeDetected results with matching objective and subject. The first-observation sequences therefore are not triggered by arbitrary obstructed/no-subject pictures. Inspection still does not SEND or advance mission state.

`Once()` starts the sequence and attempts `CommitZoneFourCue()`. Publication failure stops the sequence and grants no receipt. A successful publication records a presentation receipt before any durable proof of frame exposure or audible playback. `ZoneFourPresentationState` is just a normalized deduplicated `played` list. There is no separate earned, pending, observed or delivered state in this controller.

The final approach's continuation condition only requires Safe + visible Helm. Its initial speed/modal/direct-interaction/distance tests are not continuously required: braking can retain the bounded sequence, and changing audience cancels it. Current source uses no global quiet-time gate for this opportunity. Hidden photo sequences likewise own their actual selected-photo condition and stop when that window changes.

Environment: Zone04 owns the existing engine, cabin ventilation, propulsion, steering/depth machinery, capture mechanism and sparse water persistent states. Sparse water is multiplied by `.48 * environment` and filtered from 900 to 7800 Hz. Environmental withdrawal targets `.025` near revealed Hidden B (within 150 map units and 45 m depth), then `.015` to `.35` over the final approach's 90–600 map unit range. Gain moves over time at `dt / 4`; this affects the environment, not ship controls, warnings or mission authority. A 6.8 s distant-structure sound is eligible on Helm without modal/direct interaction/capture and with no active sequence. Its first countdown is 18 s, subsequent countdown 47 s; the timer advances only in eligible frames. This can make a repeated ambient pattern, but actual perceptibility cannot be inferred from source alone.

Existing photo composition already adds the L1 distant wreck, Hidden A sediment fins, Hidden B asymmetric water shadow and Zone04 environmental color grade through the real archived PNG compositor. These do not add contacts or change subject coverage/occlusion authority. The four Zone04 creature definitions' fallback art limitation is documented in the historical production report; this audit has not produced new creature illustrations.

Source authority: `Scripts/Feedback/ZoneFourPresentationController.cs:49`, `:87`, `:119`, `:164`, `:181`, `:204`; `ZoneFourFragmentView.cs:78`; `Scripts/Computer/PhotoCaptureService.cs:131`; `ZoneFourPhotoComposition.cs:25`; `FeedbackEffects/Zone04/` sequences.

## Save, interruption, Rest and Journal semantics

Closing a sequence's audience, pause, scene destruction or timeline restore cancels its owned transients through controller/EffectDirector cleanup. A saved `played` receipt remains after that cancellation. Consequently the existing semantics are **once per launch in a saved timeline**, rather than once per verified delivered observation.

`attemptedFragment` is reset when Mission Log is not the audience. `observedPhoto` is reset when the selected Photo Lab audience is unavailable. A failed fragment publication does not retry while that same Mission Log stays open; a failed hidden-photo publication does not retry while that same photo remains selected. Leaving and reopening permits an attempt again if no receipt was published. FinalSignal's unsuccessful attempt can retry on later eligible ticks. These are source branches to inspect with native tests if they become relevant, not baseline reports of an encountered failure.

`CommitZoneFourCue` copies the complete save, captures current gameplay state, adds the receipt to current Zone04 presentation and uses the existing transaction commit. It does not modify missions or inventory. Rest journals the current complete snapshot and copies current presentation into the new day-start snapshot. Journal restoration replaces the current snapshot from its historical checkpoint and invokes `TimelineRestored`, which cancels owned effects and clears local attempt latches. Historical receipts present in that checkpoint remain spent; future receipts disappear with the future timeline. Continue loads durable presentation state. These paths should be verified using isolated copies, never the user's live saves.

The cabin notice `BIOLOGICAL SURVEY — 09 / 09` is only emitted after a durable L1/L2 photo submission when canonical cross-zone mandatory PHOTO count reaches nine. It does not waive L1 capture or select a route. It is a transient 4.5 s notice and could overlap the immediate main-completion choice; this is a baseline observation target.

The Ending scene reads canonical saved PHOTO receipts and never records or selects an ending. Normal uses mandatory count; hidden uses all count. It does not invent 9/9 or 11/11 for partial/legacy saves. Normal lasts 12 s with settling audio; hidden lasts 16 s, signature offset 2 s, tail offset 6 s, two late marks at 6.4 and 9.6 s, then quiet. The Main Menu button remains available. Continue replays the ending presentation from its saved ending state. Ending pause stores central sequence age and resumes/seeks from it; it should not repeat the attack. No human headset assessment is implied by these source rules.

Source authority: `Scripts/Computer/ExpeditionLoop.cs:61`, `:505`, `:527`, `:564`; `ExpeditionSave.cs:212`; `Scripts/Feedback/EffectDirector.cs:243`, `:277`; `Scripts/UI/EndingPresentation.cs:37`, `:127`, `:148`.

## Source risks to test against the actual baseline

1. **Lost first observation after a very short audience visit.** Fragment receipt can be spent before either coordinate is revealed; FinalSignal can be spent before the 1.2 s signature begins; Hidden A/B can be spent before a meaningful listen. A fast switch/pause after launch may remove the chapter's only such presentation in that saved timeline. Existing durable mission hint remains accessible, so fragment loss is not the same as loss of route authority. This is a reliability concern from source; no actual baseline loss has yet been observed.
2. **Silent or unfocused launch still counts.** `Safe` has no focus check and no actual voice/surface exposure checkpoint. Muted categories/output or lost focus may satisfy launch receipt without a player receiving the intended signal. Accessibility settings must remain authoritative; do not bypass mute to manufacture delivery.
3. **No in-place retry after transient publication failure for fragment/photo.** Attempt latches are set before publication. Retry currently depends on leaving/reopening the audience; a reader who stays could see no recovery. No gameplay authority is granted on failed publication.
4. **Final approach can arrive before its full presentation completes.** Launch threshold is 360 units and arrival radius 75. Actual ship speed/route/turning and panel use determine available exposure. The physical ending remains authoritative and can unload the chapter. Do not assume the whole 9 s sequence is delivered merely because `FinalSignalCount` increments.
5. **Chapter structure may divide the player experience sharply.** The normal route ends after two visible sites; the optional route adds two hidden sites and the final voyage. A baseline intended to cover the full horror chapter must deliberately select the existing explore choice through real UI. A native normal-ending fixture does not substitute for the actual optional voyage; nor does choosing explore certify the normal presentation.
6. **Potential administrative drag and ambient regularity.** Main L2 traversal, return depth change to 735 m for hidden B, photo SEND loops, possible Rest and fixed 47 s eligible structure cadence may dominate felt pacing. Need actual navigation/window/activity clocks and recorded audio to decide; source alone cannot label time boring, tense or frightening.
7. **Transient survey notice can compete with the ending choice.** Both are consequences of the final required work. Actual screenshot/video must establish readability and timing. Do not remove the required capture or alter endings to solve presentation timing.

These are audit hypotheses, not a redesign. Any chosen change must follow the actual baseline evidence and keep authored map geometry, mission count, manual SEND/capture authority, costs, physical final arrival, ending IDs and user save history intact.

## Existing native coverage inventory

There are **17 distinct existing Zone04/Ending PlayMode cases** in the following five classes (setup/teardown excluded):

- `ZoneFourVerticalSliceTests` — 4: real L1 photograph/capture/fragment, first display pause/Continue/Journal semantics, failed capture publication, failed manual SEND publication.
- `ZoneFourHiddenVisualTests` — 4: distinct actual A/B archival background evidence and SEND, environmental grade/validation isolation, cache lifetime, real first-observation audio for each site.
- `ZoneFourAudioProductionTests` — 1: imported source families mixed with actual machinery, environmental withdrawal, category/output mute, pause and scene lifetime.
- `ZoneFourProductionTests` — 6: stale paused/transition choice, failed explore publication, failed ending publication with normal retry, ninth-photo notice while capture still required, hidden Radar/real-photo/manual-SEND/physical final route, hidden audio pause/scene/Journal restoration.
- `EndingPresentationPlayModeTests` — 2: hidden pause/seek without attack/save rewrite; both authored variants retain artwork and Main Menu button.

Several existing cases explicitly assert that launch receipts persist after interruption and prevent replay. If delivery semantics change, these assertions need an intentional specification update; merely preserving all old assertions would retain the source concern. Existing fixtures use isolated saves plus placement/seeded fieldwork as appropriate: they are native functional checks, not continuous player-input baseline evidence.

Historical production report records final 103 EditMode passes with one optional compatibility skip, and selected 82 PlayMode passes containing these 17 cases. Those are old runs. No new native test has been run by this source audit, and no current run result is claimed here.

## Baseline evidence needed next

Use one isolated Zone04 initial state consistently for baseline and final; retain actual navigation/UI input, photos/SEND, capture, existing explore choice, hidden Radar discoveries, physical final approach and hidden ending. Record raw listener PCM, source timestamps, actual screenshot/video frames and durable receipts without forcing events. Track fragment reveal/audience time, A/B Photo Lab listening windows, FinalSignal launch versus actual voice onset/scene unload, survey notice versus choice, actual route/rest/working time and all cancellations. Verify the normal ending separately with a clearly labelled native fixture if only the explore route is chosen in the continuous voyage.

No source timer/counter is a fear metric. No automated player, listener waveform or selected fixture can certify human uncertainty, dread, headset balance or aesthetic impact.

## Supporting appendix — clip clocks and exposure probes

Read-only follow-up while the continuous baseline was live under another agent's exclusive Unity ownership. No Unity operation, baseline interpretation or production/tooling edit was made in this follow-up.

All eight files below already exist in `Assets/_Project/Audio/Horror/Zone04/`; all effect definitions are in `FeedbackEffects/Zone04/`. Offsets are authored sequence scheduling offsets, **not measured human recognition or acoustic latency**.

| File / exact effect ID | Category | Authored duration | Where it begins |
| --- | --- | ---: | --- |
| `abyss_sparse_water.wav` / `z4.audio.abyss_sparse_water` | UnderwaterAmbient | 24 s, looping | Persistent Zone04 environment state |
| `distant_structure.wav` / `z4.audio.distant_structure` | Anomalies | 6.8 s | Direct play after 18 eligible seconds initially, then 47 eligible seconds |
| `fragment_recovery.wav` / `z4.audio.fragment_recovery` | Interface | 1.48 s audio | `z4.fragment` +0; sequence/card lasts 3.6 s |
| `hidden_signal_a.wav` / `z4.audio.hidden_signal_a` | Anomalies | 9 s | `z4.HiddenA` +0 |
| `hidden_signal_b.wav` / `z4.audio.hidden_signal_b` | Anomalies | 9 s | `z4.HiddenB` +0 |
| `final_signal.wav` / `z4.audio.final_signal` | Anomalies | 5.8 s | `z4.FinalSignal` +1.2 s; also `z4.HiddenEnding` +2 s |
| `normal_ending_settle.wav` / `z4.audio.normal_ending_settle` | Machinery | 9 s | `z4.NormalEnding` +0 |
| `hidden_ending_tail.wav` / `z4.audio.hidden_ending_tail` | UnderwaterAmbient | 7.2 s | `z4.HiddenEnding` +6 s |

The two other Zone04 definitions are `z4.visual.final-water` (Helm tint, 9 s, starts at `z4.FinalSignal` +0) and `z4.clock.ending` (no clip, extends normal/hidden presentation clocks to 12/16 s). Sequence `phase` labels are descriptive; `EffectDirector.AdvanceSequence()` schedules by `offset`. All six Zone04 sequences currently have `synchronizeToAudio = false`.

**Important fragment constraint:** the recovery sound's 1.48 s ends before coordinate B becomes visible at 1.65 s. A criterion requiring both a concurrently advancing fragment voice and the already revealed B coordinate cannot be satisfied by the intact authored sequence. Card readability and prior audio observation must be separate observations if both matter. `FragmentView.Progress == 1` corresponds to age 2.5 s, not the audio's end. This is a source constraint, not a proposed new threshold.

Available production probes already support focused checks without using manually advanced delta time as proof:

- `EffectDirector.Surface(EffectTarget)` returns the actual bound `EffectSurface`; `.Available` checks component/visibility root and optional group alpha. `IsActive(handle)` and `TryGetSequenceTime(handle, out age)` establish ownership/central sequence time; `IsPlaying(id)` establishes a request, not delivery.
- `EffectDirector.TryGetAudioTime(exactDefinitionId, out seconds)` reads the actual executor voice clock, including sequence children. It is already used by Zone03. Underneath, `EffectAudioExecutor.TryPlaybackTime` returns `AudioSource.time` while playing and `clip.length` for a not-playing source that has not yet been released. It does not itself certify nonzero gain, an enabled listener, category/output settings, or human listening. `PlayingCount` likewise is a voice-ID count.
- Actual executor `AudioSource` children expose `isPlaying`, `clip`, `time`, `volume`, `mute`, and activity/enabled state. Sources are pooled; a retained `AudioSource` reference alone does not prove it still belongs to the same cue. Match the actual production clip and active request/sequence, not only a node index.
- `AudioManager.FeedbackSfxGain`, `.CategoryVolume(AudioCategory)` and `.OutputVolume` are available, as are actual `AudioListener` state/volume. The executor multiplies its voice gain by SFX/ambient gain, category gain and envelope. Existing accessibility preferences must remain authoritative; no gain/routing change should be used to manufacture delivery proof.
- `ZoneFourFragmentView.Visible`, `.Progress`, `.FirstCoordinateText` and `.SecondCoordinateText` expose the real card state. `Visible` means active hierarchy, so it can be true on the initial zero-alpha fade frame. The card's actual `CanvasGroup.alpha` is not exposed as a public property; its real runtime child is `RecoveredCoordinateFragment`. Coordinate strings initially contain placeholders, A reveals at .85 s, B at 1.65 s, and card fades out over its final .5 s. Distinct render frames and unscaled wall time matter for any readable exposure check.
- Final water uses `effects.Surface(EffectTarget.Helm)?.overlay?.tint`, a real `RawImage` with `.enabled`, `.gameObject.activeInHierarchy`, and `.color.a`. There is no procedural `ActivePreset` for this tint; the definition is `EffectOverlay.Tint`, alpha .075, color (.035,.1,.14). Match `effects.IsPlaying("z4.visual.final-water")`, the Helm audience and active tint together. The raw tint alpha alone may be a retained/fading unrelated state.
- The existing evidence recorder now reads those actual fragment/selected-site/audio-time/tint probes. Treat its sampled facts separately from the controller's launch counters. No passive counter proves a whole clip or card was delivered.

If the **actual baseline** supports a delivery/recovery change, focused native coverage would need to distinguish the following behaviors before widening regression:

1. Fragment interrupted before B reveal; reopening/Continue/Rest preserves the earned opportunity, whereas a Journal rollback to before capture must not invent the hint/capture. After a defined readable checkpoint, reopening does not replay. Audio and visual observation are checked independently for the source-duration reason above.
2. Hidden A and B each interrupted early by photo switch/close/pause; the same actual selected photo can recover without another shutter, Radar scan or SEND. Switching sites cannot publish the wrong site's receipt. Completed presentation does not duplicate mission/cargo receipts.
3. Final approach interrupted before its +1.2 s audio onset; reopening eligible Helm can recover. Normal braking may retain the authored tail. No presentation path can unlock the destination, navigate the ship or select an ending. Physical arrival still owns the scene transition.
4. A large manual `Tick(dt)`, duplicate Tick in one frame, skipped render opportunity, late audio request, source loss or vanished surface cannot substitute for real wall-clock/render/voice exposure. The unaltered production clip/sequence should be used for delivery tests.
5. Publication failure before observation and failure after an actually delivered checkpoint are different cases: retry durable state without granting mission/cargo changes, and avoid replaying already received content during a delayed write in the same live timeline. Document limits across a process restart if uncommitted delivery cannot be recovered durably.
6. Pause, focus loss, category mute and final output mute obey a stated receipt policy and never bypass accessibility volume or leave owned tails alive. Assert actual source/output behavior, not only sequence/counter state. Rest/Continue/Journal must round-trip both pending and completed states consistently.
7. Existing saved `fragment`, `hidden.a`, `hidden.b`, `final.signal` receipts remain honored; new state, if any, normalizes safely and does not invalidate legacy endings or source mission/save schema. Retain normal/hidden choice transactions, ninth-photo notice with capture still required, physical arrival and ending pause/seek regressions.

This appendix does not choose any threshold, introduce a redesign, or claim that an interruption actually occurred in the baseline.
