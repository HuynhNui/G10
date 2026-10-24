# Zone 4 final chapter acceptance — 2026-10-24

Status: implemented and verified in native Unity. Final PlayMode regression: **82 passed, 0 failed**. Entire EditMode suite: **103 passed, 0 failed, 1 optional Kino skip**. This is a selected PlayMode regression, not a full-repository PlayMode run or standalone player build.

## What is installed

- The existing shared cabin has sparse original abyss audio, preserved ship/control/warning sounds, a distant wreck inside the actual L1 PNG, recovered coordinates in the real Mission Log, two distinct hidden photographic environments and first-observation signals, a durable-SEND 09/09 notice, and a physical final-approach signature.
- The actual Ending canvas has independent bounded normal/hidden sequences, committed receipt counts, Vietnamese text, a local feathered depth field, a minimal receipt diagram and real audio. Pause seeks back to the same cue time. Existing ending save transactions, IDs, background artwork and Menu/Continue remain authoritative.
- No new objective, teleport, automatic SEND, hint-driven discovery, live Camera panel, humanoid silhouette or runtime generation was added. Capture remains mandatory where authored. The intentional `zone04-l5` ID is preserved.

Implementation, milestone-specific tests/limitations and exact source paths: [production record](../../Zone04FinalChapterProduction.md). New runtime code is `Scripts/Feedback/ZoneFour*`, `Scripts/Computer/ZoneFourPhotoComposition.cs`, `ExpeditionPhotoSurvey.cs` and the extended `UI/EndingPresentation.cs`. Authoring is `Scripts/Editor/ZoneFourSetup.cs`. Actual authored assets: `Assets/_Project/FeedbackEffects/Zone04/` (10 definitions, 6 sequences) and `Assets/_Project/Audio/Horror/Zone04/` (8 WAVs).

## Review actual gameplay

| Demo | Screenshot / photographic evidence | Actual listener mix |
| --- | --- | --- |
| A — L1 distant wreck | [archived photograph](../Zone04VerticalSlice/A-wreck-photograph.png), [real Photo Lab](../Zone04VerticalSlice/A-01-wreck-photo-in-real-lab.png) | Sparse bed and machinery in [M3 mix](M3-abyss-sparse-water-listener.wav) |
| B — recovered fragment | [readable actual Mission Log](../Zone04VerticalSlice/B-02a-readable-fragment-card-in-real-mission-log.png), [two readable coordinates](../Zone04VerticalSlice/B-02-recovered-coordinates-in-real-mission-log.png) | [fragment](../Zone04VerticalSlice/B-fragment-listener.wav) |
| C — Hidden A | [photograph](../Zone04HiddenScenes/C-A-authored-photograph.png), [actual radar/photo Lab](../Zone04Production/C-A-actual-radar-photo-lab.png) | [complete first observation](../Zone04HiddenScenes/C-A-first-observation-listener.wav) |
| C — Hidden B | [photograph](../Zone04HiddenScenes/C-B-authored-photograph.png), [actual radar/photo Lab](../Zone04Production/C-B-actual-radar-photo-lab.png) | [complete first observation](../Zone04HiddenScenes/C-B-first-observation-listener.wav) |
| D — Final Signal | [actual helm approach outside final radius](../Zone04Production/D-01-actual-final-approach.png) | [actual approach](../Zone04Production/D-final-approach-listener.wav) |
| E — Normal Ending | [final actual canvas](../Zone04Production/E-normal-02-final-ending-canvas.png) | [normal](../Zone04Production/E-normal-ending-listener.wav) |
| E — Hidden Ending | [initial actual canvas](../Zone04Production/E-hidden-01-authored-ending-canvas.png), [final local depth field](../Zone04Production/E-hidden-02-final-ending-canvas.png) | [hidden](../Zone04Production/E-hidden-ending-listener.wav) |

All listener WAVs are raw game output with sidecar metrics; none were normalized or replaced with an offline clip. Before/after photographic bytes, unchanged exact coverage/occlusion and archive/timeline SHA256 equality are retained in `../Zone04HiddenScenes/`. Original source auditions are separately labelled offline in [audio production](audio-m3-production.md).

The hidden visual-only fixture seeds its route and says so explicitly. Its photos are real; its seed is not proof of Radar/progression. Separate `ZoneFourProductionTests` use the actual choice, consume Radar charges, discover both sites, take photographs, manually SEND them and move through the authored water chart to reach the final point. The whole-game end-to-end regression starts from NEW GAME.

## Native validation

| Run | Result | Scope |
| --- | --- | --- |
| `99de684654ae` | 4/4 passed | M2 L1 archive/SEND/capture/fragment, pause/Continue/Journal and rejected writes |
| `8c65fe83cdcd` | 1/1 passed | M3 eight imported bindings, six real mixes, category/output/withdrawal/pause/lifetime |
| `d3adfb62c171` | 8/8 passed | M4 four hidden/photo cases plus M2 regressions |
| `ac34c776a6d3` | 8/8 passed | Six production/progression cases plus two ending cases, including audio seek on resume |
| `9b57b4a8caba` | 4/4 passed | Final ending contrast/local-veil polish and actual normal/hidden pipelines |
| `9cf7887efdec` | 103 passed, 0 failed, 1 skipped | Entire EditMode suite; optional Kino evaluation is intentionally absent from production |
| `ba233e51b04e` | 82/82 passed, 714.8 s | Selected final PlayMode regression, including all 17 distinct Zone4/ending cases |

These rows overlap; they must not be summed as distinct tests. Raw native responses are beside this file. Early M2/M3 failed fixtures and the first visually weak ending capture are retained rather than hidden. Native tests alone did not accept the weak ending: visual review caused font, contrast and local-depth refinements before the final pass. The final run contains 82 distinct named results, with [exact filters](m6-selected-filters.json). An unrelated old Economy fixture with an outdated 20-second intro deadline was not selected; this report does not claim an entire PlayMode suite pass.

Final native scene audit found **0 missing scripts, 0 duplicate catalog IDs**, all fragment/hidden/final bindings, separate 12/16-second ending directors, the existing Vietnamese font and all four canonical count configs. Both scenes were saved/clean. Zone04 mission SHA256 `A103A1170D1022FE7AD7FF02C09381EAF5DF5BE778B76993E4CB08DDA730700E` and map SHA256 `156F4FCBB8D7020ACEB3E54B21F52C23CBD69373F2AFD20DB8570424C0650A58` are byte-identical to task-entry snapshots.

Test-generated dynamic font glyph/atlas caches were audited and restored to exact entry snapshots, preserving their original source/atlas configuration and prior local changes. TimeManager also matches entry. [Protected-file hashes](protected-files-final.json), [cache audit](font-cache-audit.json), [scene bindings](final-scene-bindings.json). Editor is stopped on clean Bootstrap; no commit, merge, push or branch switch was performed.

## Budget, provenance and release boundary

The user authorized at most 2,400 existing ElevenLabs credits, 14 attempts and 60 requested source seconds; no purchase/top-up. Actual total: **13 attempts, 54.5 seconds, 530 successful-header credits**. An extra conservative 60-credit rejected-request reservation makes budget accounting **590 spent/reserved**. No further generation is planned. The initial prompt-length rejection returned no source/billing header and is included in the attempt limit. [Ledger](audio-ledger.json), [source manifest](audio-m3-manifest.json), [byte verification](audio-m3-verification.json).

The source account was **Free** when generated. These assets carry the recorded noncommercial/attribution restrictions; they are not represented as commercially licensed release audio. No license was purchased or changed. See [production/license provenance](audio-m3-production.md).

All four Zone4 creature definitions still lack their own image/sprite/prefab binding. The existing generic algae fallback appears in the actual photos above. The new environments are integrated and replaceable, but these are not finished Zone4 creature illustrations. No artist binding was overwritten.

Root inspected actual PNGs, checked composition/readability and reviewed measured runtime/source evidence. Human headset/aesthetic review remains for the wreck's subtlety, A/B identities, sparse timing and emotional effect of the final signature. No human or listening model has certified those qualities. No standalone player build, target-hardware performance certification or commercial release sign-off is claimed.

## Reproduce and review

Start from Bootstrap. Play the real route, or use the existing Debug Start Zone4 on its separate test timeline. The installed production is already serialized; no installer is needed to play. `G10/Zone 4/Install Final Chapter` is an idempotent targeted authoring command for the saved shared cabin and existing Ending scene; save authored scene changes first.

In PlayMode, `G10/Zone 4` offers A (select an existing valid L1 photo), B (real captured hint), C1/C2 (select the respective real hidden photo and replay its production sequence), D (real helm) and E1/E2 (actual Ending canvas). Reviews fabricate no image, discovery or saved ending. Pause/modal/transition guards apply. Ending Continue intentionally replays presentation from its beginning; it does not repeat rewards or change the recorded ending.
