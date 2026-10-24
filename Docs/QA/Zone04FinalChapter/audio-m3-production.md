# Zone 4 M3 audio production — 2026-10-24

Final native integration is now verified: M3 job8c65fe83cdcd passed its real listener/mix/volume/cleanup probe, and final regression ba233e51b04e passed82/82 selected PlayMode cases, including real hidden observations and both ending listeners. See [final results](results.md) for actual game recordings and the acceptance boundaries. The source/processing account below records the production stage, not a claim of human aesthetic approval.

M3 began only after root confirmed the actual M2 wreck/fragment vertical slice had passed its four native tests, including archival photo validation, manual SEND/capture, hidden-discovery authority and a readable Mission Log. Its actual listener recording was non-silent without clipping. This technical review did not claim human headphone/aesthetic approval.

Exactly **10 new original ElevenLabs requests / 50 requested seconds** completed successfully. No M3 request failed or was retried. All prompts were validated at or below 450 characters before submission. Two original candidates were retained for each of abyss water, distant structure, Hidden A, Hidden B and Final Signal.

## Production library

Files are under `Assets/_Project/Audio/Horror/Zone04/`. All production WAVs are stereo 44.1 kHz PCM16 with source hashes, processing, level metrics and license provenance.

| Asset | Length / peak | Intended binding |
| --- | --- | --- |
| `abyss_sparse_water.wav` | 24 s loop / −22 dBFS | Sparse original water motions separated by long quiet intervals; no machinery or constant drone baked in. |
| `distant_structure.wav` | 6.8 s / −18 dBFS | Rare nonpercussive distant fluid/enclosure resonance; no stone, root or hull-source reuse. |
| `hidden_signal_a.wav` | 9 s / −15 dBFS | Unequal soft midrange pockets at 0.2/5.4 s with a long empty interval. |
| `hidden_signal_b.wav` | 9 s / −17 dBFS | Separate fine high transients at 0.25/7.2 s and low lateral motion at 3.55 s; different original material from A. |
| `final_signal.wav` | 5.8 s / −13 dBFS | Minimal original signature with a balanced fluid body and upper thread; gentle edges, no impact/stinger. |
| `normal_ending_settle.wav` | 9 s / −23 dBFS | Derived familiar machinery and restrained abyss-water release; no new API request or music. |
| `hidden_ending_tail.wav` | 7.2 s / −27 dBFS | Derived sparse water and final-source residual tail; no replay of the complete signature and no new API request. |
| `fragment_recovery.wav` | 1.48 s / −14 dBFS | Existing M2 production copy is unchanged, including its SHA256. |

Original MP3s, request receipts and equal-peak candidate WAVs are in `audio-candidates/`. [audio-m3-manifest.json](audio-m3-manifest.json) records source choices and processing. The sparse cues intentionally contain long quiet gaps; the whole assets are valid nonzero audio. **Acoustic Withdrawal itself remains runtime gain/filter/mixer automation**, preserving essential ship machinery. It is not implemented by an empty WAV.

## First-pass choices and local design

The abyss choice favors the candidate with more quiet/nonstationary water movement. The distant structure choice favors a changing soft envelope. Hidden A retains two unequal sections of its own original source with deliberately long spacing. Hidden B combines the real fine transients from the film candidate with the complementary low movement from the drift candidate, rather than amplifying weak upper residuals from a bass-dominated source. It is not Hidden A transposed to another pitch.

The Final Signal initially favored the fold candidate, but waveform/spectral inspection showed it was overwhelmingly sub-bass. The finalized production asset uses the richer thread candidate, independently balances its 60–220 Hz fluid body and 260–2400 Hz thread, and applies restrained soft saturation and a weak darker residual. Its measured spectral centroid is approximately 272 Hz and about 45% of energy is above 250 Hz, improving measurable separation from the familiar low idle machinery without raising its −13 dBFS output peak. This is an engineering choice, not a claim that a listener has judged it memorable or emotionally convincing.

The retained unmodified final-thread MP3 decodes with 17 floating-point samples slightly above full scale (approximately +0.056 dBFS). The processed candidate and production WAV are levelled **before PCM conversion**, and contain zero saturated samples. No gameplay listener recording was normalized or substituted. Raw provider responses remain available for audit.

For live approach/final scenes, avoid leaving the existing `NearSilence` snapshot active over an `Anomalies` final cue: it ducks that bus by 48 dB. Prefer owned environmental gain/filter withdrawal while respecting the final cue's correct category and the player's category settings. Do not move it into Interface to bypass the mix or turn up master volume. Root owns scene-local EffectDirector bindings, pause/restore/scene cleanup and both ending flows.

## Review material and acceptance boundary

These files are offline audition material built from the exact produced sources; they are **not Unity captures**:

- [Abyss alternatives](audio-m3-comparison-abyss.wav)
- [Distant structure alternatives](audio-m3-comparison-structure.wav)
- [Hidden A alternatives](audio-m3-comparison-hidden-a.wav)
- [Hidden B alternatives](audio-m3-comparison-hidden-b.wav)
- [Final Signal alternatives](audio-m3-comparison-final.wav)
- [All seven new production cues](audio-m3-production-review.wav), with [cue timings](audio-m3-review-timings.json)

Each comparison has candidate one at 0.5 seconds, then a one-second gap before candidate two. Both candidates within a pair use the same peak target. Human review should assess nonmusical material identity, A/B distinction, the Final Signal against normal player-volume machinery, ending contrast and whether the quiet gaps feel intentional. No person or listening model has certified those qualities during this production step.

[audio-m3-verification.json](audio-m3-verification.json) checks the actual output bytes, source/output hashes, PCM shape, finite/nonzero data, zero saturation, exact loop endpoint, Hidden A/B quiet intervals, B's separate high/low events, final upper-band presence and aggregate budget. Native import, real gameplay playback, actual listener-mix headroom and ending/save regressions remain separate root-owned acceptance work.

## Shared authorization and cost

[audio-ledger.json](audio-ledger.json) retains all M2 and M3 attempts, including the initial rejected M2 HTTP 400. M3 used **500 actual header-reported credits**. Cumulative successful response headers are **530 credits** (M2 30 + M3 500); the rejected attempt conservatively retains its 60-credit reservation, for **590 spent/reserved credits**. The latest quota response reports **4,146 included credits remaining**, exactly matching the successful-header deduction from the initial 4,676; no top-up or overage was used.

Cumulative authorization usage is **13 submitted attempts / 54.5 requested seconds**, within the human-approved absolute limits of **14 / 60 seconds / 2,400 existing credits**. At most one further attempt and 5.5 requested seconds remain, regardless of the larger unused credit allowance. No further API call is planned or automatically enabled for a failed request.

The account was Free at generation. Every source retains the same noncommercial/attribution limitation described in the M2 documentation. No existing creature illustration or external asset license was changed. See the official [publication terms](https://help.elevenlabs.io/hc/en-us/articles/13313564601361-Can-I-publish-the-content-I-generate-on-the-platform) and [published sound-effects pricing](https://elevenlabs.io/docs/overview/capabilities/sound-effects).
