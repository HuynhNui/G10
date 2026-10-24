# One-cycle redesign — a presence follows the vessel

Boundary: baseline audit `02_Experience_Audit.md` is closed before this document. This is the sole authorized redesign, followed by one final input-driven voyage. Creative implementation choices are authorized by the user's brief; no extra approval, purchase, credit use, commit or publication is required.

## Experience contract

Normal operation → sample disturbs normality → instrument anomaly → familiar knocks → a delayed exterior movement after propulsion stops → scrape/escalation → hull success/recovery → uncertain reflection → quiet physical departure. The player may omit an optional instrument and retain agency. Real contacts, scan charges, archived photographs, captures, hull health, navigation and mission transactions remain authoritative.

The chapter should suggest an outside presence without confirming its identity. This is a design hypothesis; final agent play can establish delivery/context and correctness, not human fear.

## Orchestration

- Extend only ZoneTwoPresentationController and its existing per-zone receipt list; derive a small public experience-stage readout from receipts. No global framework or save-version bump.
- Maintain a single pending dramatic delivery. Start is transient; commit a durable receipt only after the relevant visual/audio clock has reached a defined exposure threshold while its audience remains available. Closing a required audience/pause cancels before delivery and leaves the opportunity retryable after recovery; committed beats do not replay. Failed save publication stops owned presentation and leaves receipt absent.
- Radar stays a real scan opportunity after sample/travel; its sequence gains a brief preparation before the existing scoped tear. The anomaly never adds a contact or spends another charge.
- Knocks can follow Radar or the exterior-tail clue. They remain audio only and occur while moving at Helm. Their receipt follows actual voice playback rather than request creation.
- One optional exterior-tail clue observes sustained actual propulsion, a real subsequent stop, sample receipt and travel. After a short stopped interval it plays a filtered/panned derivative of the existing outside/root vocabulary. It never stops the vessel or prompts the operator. Exterior environmental bed withdraws briefly; machinery/warnings retain normal authority. This replaces reliance on more periodic root starts with one behavior-related connection.
- Scrape follows knocks at the actual Computer, including after Hull installation if the player has not yet encountered it. Its receipt waits until the main dip interval has passed while Computer stayed visible. No forced UI, locks or explanatory new Lily dialogue.
- Keep the final reflection tied to real exit approach. Its audience is Helm; the faint figure lasts roughly0.6s instead of0.2s, with a slow small position drift. Commit after a perceptible portion of the actual figure interval. Pause/close cannot burn an unseen delayed figure.
- Quiet/recovery accumulates when owned dramatic playback has ceased. Ambient root intervals vary deterministically from progression/travel instead of a fixed three-interval cycle; no uniform scare quota. Pauses, restore, disable and zone departure clear transient clocks/handles. Journal restores the original receipt set through the existing save owner.

## Production plan and resources

Audio: reuse all existing Zone02 production clips/mixer groups. Produce one offline derivative `following_tail.wav` from existing outside-movement/root-friction sources with restrained lowpass, timing and stereo balance; preserve source/license hashes and processing command. No paid generation or new resource quota is assumed. Source terms remain the recorded ElevenLabs Free noncommercial/attribution terms. No commercial license is implied.

Visual: add a native lightweight uGUI shader/material for a very slow bounded edge motion on the real Zone02 Photo Lab preview. Center subject and image data remain unchanged; only display-edge water/roots are displaced slightly. The actual archived texture/PNG is never edited. The controller owns one lazily created runtime material and restores the original immediately on close/pause/restore/zone change. Controls, real map geometry and Radar data are excluded. Respect existing screen-shake accessibility settings; no new flashing/high-frequency movement.

Improve authored Radar/Scrape/Reflection sequences through a narrow presentation-only installer. Do not invoke the broad ZoneTwoSetup installer, which reauthors mission/map/content. Assign new presentation references in the shared cabin through Unity serialization and verify protected mission/map/art assets are byte-identical. Native shader, EffectDirector/EffectSequence, existing UIEffect and one material are sufficient; no external library/package is justified.

## Technical validation

Targeted tests must cover interruption before exposure and retry; committed no-replay; failure rollback; pause/Rest/Continue/Journal; source-only vs actual audience exposure; optional Radar and post-Hull opportunities; real scan charge/data preservation; PHOTO→SEND→CAPTURE/Hull/physical exit; preview center/archive stability, material cleanup and bounded allocations. Prior functional return-gate tests remain separate from experience changes.

After relevant checks pass, repeat the same legitimate Zone02 DEBUG preset with regular upgrades NONE and fresh isolated save/photos. Use the same real input tools and normal physics. Capture actual frames/audio/telemetry; no forced cues or state injection. Record route learning and the repaired blocker as confounds, not proof that horror alone improved completion speed. Unexpected behavior in this final voyage must be recorded rather than silently redesigning again.
