# Capture and cargo visual review

Initial runtime evidence set: `native-validation/AllCreatures`. All 13 images below were opened individually at their original 1920×1080 resolution using `view_image`; this review does not rely on a contact sheet. Controlled fixture evidence, not an ordinary-input voyage.

## Findings

- Cargo: pass for all six captured organisms. Detail previews and storage icons show the expected catalog organisms, preserve native aspect and artist color/glow, and retain visible anatomy within their existing boxes. No film-frame, PSD caption, adjacent-panel, or opaque-background leakage was observed.
- Initial capture evidence: correction required. All six target drawings retain the scene’s legacy cropped UV rectangle; Z2_Creature_02, Z3_Creature_01, and Z3_Creature_03 clearly show rectangular middle-body fragments instead of complete organisms. The fit box itself is bounded, but its texture sampling is incorrect.
- Confirmed cause: Zone01 scene creature RawImage `545010732` stores UV `(0.21132377, 0.284689, 0.546252, 0.40271133)` from the existing alpha-cropped generic texture. New standalone artwork needs full `(0,0,1,1)` UVs.
- Minimal correction implemented after this review: cache the original UV with the fallback texture; use full UVs for encounter artwork and restore original UVs on a default Begin. The focused portrait/landscape/fallback regression now starts from a nondefault atlas UV and checks both branches. No profile, movement, collider, hit count, charge, or reward change. Runtime retest and replacement capture visual review remain pending at the time this note is written.
- Historical photo: the continued Photo Lab screenshot retains the earlier blue/pink algae illustration, clearly distinct from the warm remake. Its visual content is not repainted from the current catalog. Exact archive/pixel preservation is covered by the separate native fixture evidence.

## Material caveats

The existing minigame profile makes targets small, especially in later zones. Body shape and color can identify the organism once full UVs are used, while fine ink/watercolor detail belongs in cargo and Lab views. This review does not request altered collision boxes, speeds, zone scaling, or UI layout. White/colored halos and a few faint native brush details are original artwork, not background removal artifacts.

## Files actually viewed (initial evidence SHA-256)

| File | SHA-256 |
| --- | --- |
| `Z1_Creature_01-04-capture.png` | `a993a513ec9e26a85a6cf22e8558fe1ec509bb897af437fa6971fa3d79241462` |
| `Z1_Creature_01-05-cargo.png` | `531beb69a47b4af76ee1828f6b211ae462c620c320517952e3899fd572c28f06` |
| `Z2_Creature_01-04-capture.png` | `7df6a4a202a6767115fdc7d125b34ba31621e33a71294634c729955e57c278f0` |
| `Z2_Creature_01-05-cargo.png` | `0c3a45b20c9002d086c5dd6cc889aafc785bd51bcee279bbadf0e5ea2ea7855e` |
| `Z2_Creature_02-04-capture.png` | `b88761ac071f84f14a373288ca685729c5b60b211f1be42a359a3dd30a77c7c9` |
| `Z2_Creature_02-05-cargo.png` | `d3b9f63b76d82e3c6d4aec696dda10affebea9b6b351caf4ad48e1157dd01638` |
| `Z3_Creature_01-04-capture.png` | `43bf8dd47a9f320cb9700b6a4e9a99897cf44f5921aff9c0da304e17f91e7410` |
| `Z3_Creature_01-05-cargo.png` | `83acc0f603bff435d21ac0476371a33d08690dd0fe83627b18f1f7d9a21699aa` |
| `Z3_Creature_03-04-capture.png` | `3776889758326cd47148df25155f4c20f6122d8e79738b4d773cc69ade7342eb` |
| `Z3_Creature_03-05-cargo.png` | `949eeb340331559ac42506504e2fab4a7d09fe2573b355852fb0a133d1266ddd` |
| `Z4_Creature_01-04-capture.png` | `6308edd19ab0c71e4b87d4be8019de70e3ae411fa39b772134fe30c341e7990d` |
| `Z4_Creature_01-05-cargo.png` | `a66b710cba4085a1753f57fce8b9c7787aa59d43190f5f0e37b14f1394e17088` |
| `historical-original-continues-unchanged.png` | `2e3db13bd8d96af38b82a9362694846df33caa85239e45c9d612fde5a3ee46df` |

## Final visual retest — 2026-11-03

Final native matrix: `c62d20014e23`, completed and passed, with 11 photo cases and six capture cases. The final output is `native-validation/AllCreatures`; the initial hashes above describe the earlier review and are superseded by the table below.

All six final capture PNGs were reopened individually at their original 1920×1080 resolution after the matrix completed. Verdict: **pass**. Each shows the complete expected organism, including fins, tail, antennae, or the algae colony silhouette as applicable, with the native aspect retained within the existing visual box. The earlier rectangular middle-body UV crop is absent. No PSD film frame, caption, adjacent panel, or opaque background appears around a target. All six also show the settled capture title, timer, exit button, arena, hook, progress, and instructions. A provisional concern about incomplete UI while the output folder was being replaced does not reproduce in the final images.

The six final cargo previews and their storage icons were also opened individually at original resolution, together with the historical continued Lab screenshot. Cargo verdict: **pass** for complete anatomy, aspect, color/glow, and bounds. The historical screenshot visibly retains the earlier blue/pink algae artwork rather than the warm remake. Image-byte and archive preservation remain covered by the native fixture assertions, not inferred solely from this visual comparison.

The target-size caveat above still applies: fine watercolor detail is difficult to read at the existing minigame scale, especially in Zone04, but the organism silhouette and color are visible. No enlargement, collider/profile change, or unrelated UI redesign is requested by this review. This evidence comes from bounded native fixtures and is not an ordinary-input voyage.

### Final files viewed and SHA-256

| File | SHA-256 |
| --- | --- |
| `Z1_Creature_01-04-capture.png` | `2640e20b28b1b6fd1a17b51bf448f586b7de671922c875b1e3bb3a54cce11cc0` |
| `Z1_Creature_01-05-cargo.png` | `5204e00a20c6acae15a8b8661fa52d55f8cab752cef2e1896994c07ac476816a` |
| `Z2_Creature_01-04-capture.png` | `03906e6c1e8bf1bd2b42126523e809086055689a6f108cefc241373f1250cf88` |
| `Z2_Creature_01-05-cargo.png` | `0d2046c8b09248b440fee1e1564ff5aa6a107627ab28ab7c04e056e2e032a2d0` |
| `Z2_Creature_02-04-capture.png` | `8f638ac190604ffdf908cae5fc4cf579fba588702cad28546f42fe63a7cf9d26` |
| `Z2_Creature_02-05-cargo.png` | `e966f2816957438f4a8721b9389b4053a4c9e09d1ea4d6bc067bdb18f594a62a` |
| `Z3_Creature_01-04-capture.png` | `1d96812c3f46a23320e2fa4331d7079262fb64c693c5edb314e548accfba6a70` |
| `Z3_Creature_01-05-cargo.png` | `9a7949a319c2c02f0861b671cd0471fb73c6d050e43da08f2ea19bed5f236b3c` |
| `Z3_Creature_03-04-capture.png` | `8290dbaaa26337fdd0a626816de74f0ba60527aa12fdd1c918a5a57f97650268` |
| `Z3_Creature_03-05-cargo.png` | `3a2f6116e7a18a79142fc8ca6a8cf54d23d93960aa1ee4bef349037b35342eb8` |
| `Z4_Creature_01-04-capture.png` | `3767031dcf1d26bbf5603baf476311016b09b4c162fc8c0ed3f1d92c48a4f6be` |
| `Z4_Creature_01-05-cargo.png` | `23f87f47398f666e301fd0c1e7a5955eb384b59d9f5eab6dc71768903d91e4fc` |
| `historical-original-continues-unchanged.png` | `bd228d5ff0d215f77be99ffd05c7f91b27ca6de31640668bfa376a4897d49b20` |
