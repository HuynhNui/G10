# Cabin gameplay feedback

Implemented against local `main` at `1d459058c0043f3cd8931c7b274468d9a0f77448` on 2026-10-07.

## Architecture and scope

`CabinFeedbackController` reads authoritative ship, capture and expedition state and owns only unsaved presentation: overlay targets, warning timers, transient messages, scrape scheduling and flicker. Navigation emits actual terrain-impact and depth-limit events. Existing gameplay systems still decide damage, movement, consumption, rewards, mission completion, upgrades, deadlines and rollback.

`ImpactShake` owns only the existing outer `CabinFrame.localPosition`. `CabinCanvasFrame` retains scale ownership; `CameraBreathing` and `CabinBob` retain their separate child transforms. Art and matching UI hitboxes move together. Shake restores its origin exactly, never accumulates, and uses private random state rather than gameplay's Unity random state.

No changes to `ShipResources`, `ExpeditionSave`, save schema version 5, content catalogs, mission assets, map/depth configuration, upgrade recipes, package settings or ending authority. No combat, morality, new currency, capture punishment or permanent HUD was added.

## Exact implementation files

Modified:

- `Assets/_Project/Scenes/Gameplay/Zone01.unity`
- `Assets/_Project/Scripts/Atmosphere/CabinAtmosphere.cs`
- `Assets/_Project/Scripts/Audio/AudioManager.cs`
- `Assets/_Project/Scripts/Computer/ComputerDesktopSkin.cs`
- `Assets/_Project/Scripts/Computer/ExpeditionLoop.cs`
- `Assets/_Project/Scripts/Core/SceneFlowController.cs`
- `Assets/_Project/Scripts/Navigation/CreatureCatcher.cs`
- `Assets/_Project/Scripts/Navigation/ZoneNavigation.cs`
- `Assets/_Project/Scripts/UI/CreatureCaptureView.cs`

Added:

- `Assets/_Project/Scripts/Feedback.meta`
- `Assets/_Project/Scripts/Feedback/CabinFeedbackController.cs` and `.meta` (also contains pure `CabinFeedbackRules`)
- `Assets/_Project/Scripts/Feedback/ImpactShake.cs` and `.meta`
- `Assets/_Project/Scripts/Editor/CabinFeedbackSetupEditor.cs` and `.meta`
- `Assets/_Project/Tests/EditMode/CabinFeedbackRulesTests.cs` and `.meta`
- `Assets/_Project/Tests/PlayMode/CabinFeedbackPlayModeTests.cs` and `.meta`
- `Docs/CabinGameplayFeedback.md`

The two TMP font assets already dirty at task start remain user/Editor-generated state, not intentional feature edits. Unity test/import activity can update their dynamic caches. User-provided `edgecases` files and their existing metadata were preserved, not renamed, regenerated or reencoded. No commit or push was performed.

## Scene setup and referenced assets

The shared Zone01 cabin now has:

```text
Cabin Station [CabinFeedbackController]
  CabinCanvas
    CabinFrame [ImpactShake; existing hierarchy otherwise preserved]
  FeedbackOverlayCanvas [Canvas + CanvasScaler]
    LowEnergyOverlay [RawImage, no raycasts]
    LowHullOverlay [RawImage, no raycasts]
    FeedbackNotification [TMP, no raycasts]
```

The feedback Canvas uses Screen Space Overlay, sorting order **31000**, below the transition fade's **32000**. Full-stretch resource overlays scale with the existing 1920 x 1080 reference; they remain above normal map/computer UI and never intercept clicks. No source-image opacity was baked in. The LowHull PNG's center alpha was inspected as 0, so the vignette does not black out the center.

Inspector references use the actual supplied filenames:

| Role | Asset path |
| --- | --- |
| Hull vignette | `Assets/_Project/Art/UI/edgecases/LowHull.png` |
| Energy darkening | `Assets/_Project/Art/UI/edgecases/Anh-nen-den-13.jpg` |
| Impact | `Assets/_Project/Audio/edgecases/Hull_Impact.wav_Dura_#1-1791386262257.wav` |
| Creak | `Assets/_Project/Audio/edgecases/Hull_Creak.wav_—_Dur_#1-1791386285536.wav` |
| Critical warning | `Assets/_Project/Audio/edgecases/Critical_Warning.wav_#1-1791386321841.wav` |
| Hum | `Assets/_Project/Audio/edgecases/owPower_Hum.wav_—_Du_#4-1791386351675.wav` |
| Power down | `Assets/_Project/Audio/edgecases/Power_Down.wav_Durat_#3-1791386369324.wav` |
| Scrape A | `Assets/_Project/Audio/edgecases/Exterior_Scrape.wav__#4-1791386380560.wav` |
| Scrape B | `Assets/_Project/Audio/edgecases/Exterior_Scrape.wav__#3-1791386447766.wav` |

The supplied hum filename lacks its initial `L`; it is still preserved. The Editor utility resolves references before scene mutation; runtime does not guess filenames or load Windows paths.

## Resource and impact behavior

Ratios use current resource / current capacity, never the aggregate `LowResources` flag.

| Hull ratio | Persistent alpha | Feedback |
| --- | --- | --- |
| >50% | 0 | No resource warning |
| >25% through 50% | 0 | Sparse creak every 18–30 active seconds |
| >10% through 25% | 0.15 | Danger; creak every 10–18 active seconds |
| >0 through 10% | 0.25 | Danger; warning every 6–10 active seconds |
| 0 | Suppressed during death flow | Existing vessel-death authority takes over |

| Energy ratio | Persistent alpha | Feedback |
| --- | --- | --- |
| >20% | 0 | Hum fades out |
| >10% through 20% | 0.15 | LowPower; hum fades in |
| >0 through 10% | 0.22 | Hum plus 1.3x decorative blink instability |
| 0 | 0.28 | Power_Down once on each >0 -> 0 crossing |

Mood priority is **Danger > LowPower > ScanActive > Normal**. Hull and Energy overlays remain independent; critical Hull does not silence the low-power hum. Default fade is 0.4 seconds, configurable 0.3–0.6. No continuous low-Hull shake.

Terrain contact preserves the original damage and latch. One impact event includes absolute impact speed and actual damage; held contact does not retrigger until moving clear. Presentation scales with impact speed: gain 0.25–0.8, pitch 1.02–0.94, shake 1–7 pixels for 0.12–0.28 seconds, red flash peaking immediately up to 0.30 before fading to baseline. A lethal contact can react before the existing next-frame death transition clears transients.

Depth-clamp input is edge-triggered; holding against the limit cannot spam events. Release or actual depth movement rearms it. It shows `DEPTH LIMIT` for 1.5 seconds and a quiet creak; it adds no damage or consumption.

## Capture feedback

Successful first mission capture shows `+1 ITEM`, actual content display name and current cargo stack quantity. Repeat farm capture shows `+1 MATERIAL` and the updated quantity without completing the mission again. Existing failure, charge and stack-slot rules are unchanged.

Daily escalation derives from existing `capturesTaken - dayStartCaptures`; there is no new saved counter or visible streak meter:

| Successful capture today | Presentation |
| --- | --- |
| #1 | Normal success only |
| #2 | 35% chance of a subtle scrape, delayed 1–4 seconds |
| #3 | Soft scrape and one 0.22-second flicker, delayed 1–4 seconds |
| #4+ | Random scrape from both supplied clips, 0.5-second flicker and 1.5-pixel / 0.16-second jitter |

Scrapes randomize clip, delay, subtle pitch (0.97–1.03) and volume. Capture failure, NoCharges and Full cannot increment escalation. All 12 unique slots being occupied still permits increasing an existing stack. No Hull, Energy, mission or reward penalties come from horror feedback.

## Lifecycle, deadline and accessibility

- Rest, timeline changes, rollback and scene teardown cancel pending scrapes, shake, flicker, messages and stale audio, then derive warnings from authoritative restored state.
- Continue and zone rebind derive ratios and daily capture tier; no presentation state enters the save. Existing scene ship-load override remains disabled so resource state persists.
- Pause, application pause, transition, death, failure and ending suppress transient reactions. Modal UI cancels inappropriate pending scrapes. Persistent critical overlays remain visible over normal map/computer panels; transition overlays clear below the fade.
- Fresh events in the fade-unlock frame resume presentation before registering the new reaction, preventing a later resume reset from erasing it.
- Existing DAY LEFT presentation holds an extra 0.25 seconds for the last three days. Days 2 and 1 add one 6% / 12% text pulse. Day 0 shows the existing failure panel directly on fade-back; deadline decisions still belong to `ExpeditionLoop`.
- Computer system-tray volume popup adds `SCREEN SHAKE: ON/OFF`. `G10.Accessibility.ScreenShake` defaults ON and persists in PlayerPrefs. OFF removes transform shake only; impact audio and overlay flash still occur.
- AudioManager owns two separate 2D feedback channels. One-shots respect SFX/master gain; hum respects ambient/master gain; both respect the existing global output volume. They do not replace radar, capture or existing ambient channels.

## 28 edge-case coverage map

| # | Case | Validation |
| --- | --- | --- |
| 1 | Actual impact feedback | New wall-impact test |
| 2 | Low-Hull thresholds | New rules and ratio tests |
| 3 | Hull zero / vessel death | New rollback test plus existing vessel-death regression |
| 4 | Low-Energy thresholds | New rules and ratio tests |
| 5 | Combined critical resources | New ratio/panel test |
| 6 | Scan mood priority | New mood rules and live radar test |
| 7 | Held-wall contact latch | New impact test; existing navigation budget tests |
| 8 | Heavy impact while Hull is low | New impact test: Hull 30 -> 12 and lethal 8 -> 0; existing death tests |
| 9 | Turning/dive/reverse exhaust Energy | New three-axis Power_Down test |
| 10 | Held depth limit | New clamp edge/consumption test |
| 11 | First capture message | New real Zone02 capture test |
| 12 | Repeat farm message and quantity | Same real capture test |
| 13 | Daily horror escalation | New tier rules; real #4 scrape/flicker test |
| 14 | Capture failure | New short-duration failed minigame test |
| 15 | No charges | New real farm test; existing capture regressions |
| 16 | Full unique slots vs existing stack | New 12-stack farm test; existing inventory/capture regressions |
| 17 | Hull upgrade while damaged | New 26/100 -> 26/120 threshold test |
| 18 | Energy upgrade while low | New upgrade test: Energy stays 9 |
| 19 | Rest cancels pending/transient state | New farm-Rest and upgrade-Rest tests |
| 20 | Last three days presentation | New pure deadline helpers |
| 21 | Day 15 -> 16 failure | New final-Rest test; existing deadline regressions |
| 22 | Critical state across zone rebind | New authoritative Zone03 Continue/rebind; existing physical route E2E |
| 23 | Continue critical resources | New snapshot/load test |
| 24 | Journal / death rollback | New restore and death test; existing rollback regressions |
| 25 | Critical map/computer warnings | New global overlay/click-through test |
| 26 | Pause / transition / ending suppression | New pause timers, Rest cancellation and Ending teardown tests |
| 27 | Normal and hidden end-to-end routes | Existing ExpeditionEndToEnd and ending tests |
| 28 | Shake accessibility | New disabled-impact test and system-tray toggle test |

## Validation record

Unity 6000.4.2f1, live Editor via Unity MCP. Tests use isolated save/photo folders and restore overridden PlayerPrefs, without touching the player's save.

- Pre-change baseline: 16/16 PlayMode passed (job `b94bf705c16e`).
- EditMode rules + existing economy rules: 37/37 passed, 0 failed, 0 skipped, 0.11 seconds (job `8518c67a9108`).
- Final post-refinement EditMode recheck: 37/37 passed, 0 failed, 0 skipped, 0.06 seconds (job `2d7dafcd4ae3`).
- Initial focused PlayMode: 17/18 passed (job `14a0cdf061bc`); the one failure exposed the fade-unlock/capture resume race. Production ordering was fixed, not the assertion weakened.
- Re-run of all 10 new feedback PlayMode tests: 10/10 passed, 0 failed, 0 skipped, 36.65 seconds (job `acf18bc11d0e`).
- Broad PlayMode regression: 105/105 passed, 0 failed, 0 skipped, 320.35 seconds (job `7a1a13b74c48`).
- Final refinement makes heavy-impact flash peak immediately and strengthens the wall test with a real lethal contact before rollback. Post-refinement feedback/navigation/atmosphere/vessel-death regression: 27/27 passed, 0 failed, 0 skipped, 87.46 seconds (job `516f9a82dbbc`).
- Scene migration ran twice without duplicates. Live post-regression inspection confirmed the saved, clean Zone01 scene has one controller, one shake, two scrape references, zero missing scripts, all Inspector references valid, Canvas order 31000 and click-through graphics. No scene-load ship-settings override. Project compilation and Console error checks returned zero errors.

Broad regression includes ExpeditionLoop, ExpeditionEconomy, VesselDeath, CaptureMinigame, ExpeditionEndToEnd, ProjectSceneFlow, CabinAtmosphere, Tutorial and EndingPresentation, plus Cargo, save/reset/new-game, ZoneOneStory, photo survey/submission, tutorial save, navigation budget, scoped UI and the new feedback fixture.

## Manual setup and remaining risks

No remaining Inspector wiring is required: the shared Zone01 scene was migrated and saved. If the scene setup must be recreated later, open Zone01 outside Play Mode, run `G10/Feedback/Apply Gameplay Feedback Setup`, then save. The menu validates references first and is idempotent; it does not replace existing cabin artwork or rebuild unrelated UI.

Automated validation checks behavior, references and layout invariants, not exact rendered pixels. Final sound balance, subjective flicker strength and comfort should be checked by a human in a normal play session. No standalone player build or human listening test is claimed. All generated feedback state remains runtime-only and adjustable through Inspector clip references/fade and the existing sound controls.
