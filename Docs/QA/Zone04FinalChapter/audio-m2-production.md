# Zone 4 M2 fragment audio — 2026-10-23

Historical M2 snapshot below. Root confirmed its working native vertical slice on 2026-10-24 (4/4 tests and a nonzero listener recording without clipping), then explicitly released the M3 gate. Current cumulative production and spend are documented in [audio-m3-production.md](audio-m3-production.md) and the shared ledger; human headphone/aesthetic review remains separate.

Only the two fragment-recovery candidates were generated. Wider Zone 4 ambience, hidden signals and ending audio remain gated on review of the working wreck/fragment vertical slice.

## Assets and first-pass selection

The provisional production binding is `Assets/_Project/Audio/Horror/Zone04/fragment_recovery.wav`: stereo 44.1 kHz PCM16, 1.48 seconds, −14 dBFS peak, zero clipped samples. Its `.provenance.json` retains the original prompt receipt, source/output SHA256 hashes, processing, plan/license status and a human-review limitation.

Both candidates remain available:

- [Packet candidate](audio-candidates/fragment_recovery_packet.wav): practical locking/data-packet ticks and a damped mechanical settle. This is the provisional production selection.
- [Relay candidate](audio-candidates/fragment_recovery_relay.wav): interrupted electronic read/chirr and a relay completion; a different requested acoustic structure rather than a pitch variant.
- [Equal-peak comparison](audio-m2-candidate-comparison.wav): packet begins at 0.5 seconds; relay at 3.0 seconds. This is an offline comparison, not Unity gameplay output.

Sources were generated with ElevenLabs `eleven_text_to_sound_v2`, explicit 1.5-second durations, prompt influence 0.7, no voice or music. The retained MP3s are unaltered provider responses. Processing trims leading silence with a 6 ms lead, applies 90 Hz high-pass/6.5 kHz low-pass and short edge ramps, then sets −14 dBFS source headroom. The cue is deliberately sparse: integrated RMS is −41.318 dBFS for packet and −37.767 dBFS for relay. Actual audibility against cabin machinery must therefore be checked in the native M2 slice; peak/nonzero checks are not listening approval.

No listening model or person has certified the choice. The first-pass packet choice follows its intended functional signature and technical validity. Human comparison in the real Computer/Mission Log presentation remains required. An alternative can be selected offline with `Tools/Audio/generate_zone04.py --process-m2 --select fragment_recovery_relay`; this makes no provider request. The root owns Unity import, playback assignment and actual listener-mix evidence.

## Shared authorization and spend

[audio-ledger.json](audio-ledger.json) is the persistent authorization ledger across all Zone 4 milestones: at most **14 submitted requests, 60 requested seconds and 2,400 existing included credits**, with no top-up or overage. The starting account was Free with 4,676 included credits available.

The first request received HTTP 400 and returned no asset. The outgoing prompt was 589 characters, exceeding the vendor's documented 450-character limit. Both prompts were shortened to 403/412 characters and the tool now validates the limit before submission. The failed attempt is retained, not erased or automatically retried. Root explicitly authorized two corrected manual submissions within the already approved aggregate ceiling; M2's conservative bound became 180 credits including the failed attempt's reservation.

The two successful response billing headers are 15 + 15 = **30 actual credits**. The failed attempt conservatively retains a 60-credit reservation because it returned no billing header. Current aggregate accounting is therefore **90 billed/reserved credits, 3 attempts and 4.5 requested seconds**. The quota endpoint still reported the old balance immediately afterward; the ledger uses response headers/reservations rather than treating a delayed quota update as free generation.

No new failure was retried and no wider generation was made. Remaining aggregate authorization is at most 11 submitted requests, 55.5 requested seconds and 2,310 billed/reserved credits, still subject to the vertical-slice review gate and a fresh included-quota check before later production.

## Verification and remaining acceptance

[audio-m2-verification.json](audio-m2-verification.json) independently checks PCM shape, finite/nonzero samples, zero clipping, output/source hashes and aggregate accounting. [audio-m2-manifest.json](audio-m2-manifest.json) records both candidates and the provisional production selection. These checks passed for both candidates and the production copy. Unity import, real playback binding and gameplay headroom have not been claimed by this audio-production step.

The account was Free at generation. Provenance records no commercial license and the applicable attribution requirement; the existing Zone 3 licensing limitation also applies to these new sources. See the official [publication terms](https://help.elevenlabs.io/hc/en-us/articles/13313564601361-Can-I-publish-the-content-I-generate-on-the-platform), [sound-effects pricing](https://elevenlabs.io/docs/overview/capabilities/sound-effects), and [450-character prompt limit](https://elevenlabs.io/docs/help-center/product/core-capabilities/sound-effects/what-is-sound-effects).
