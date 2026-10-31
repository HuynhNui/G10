# Zone04 input and evidence tooling audit

Prepared on 2026-10-26 after root verified the entry snapshot (156 persistent files, 14 protected assets, 390 production source/art/config hashes). This work made no Unity calls and changed no production component, scene, map, mission, package or effect asset.

## Narrow tooling exceptions before baseline

Only `Assets/_Project/Scripts/Editor/ZoneTwoExperiencePlaytestTools.cs` changed inside Assets. It remains Editor-only. Existing isolation, bounded keyboard/mouse injection, visible-hook capture assistance, 5 Hz real Game View JPEGs and pass-through listener PCM recording are reused unchanged.

The prior recorder incorrectly selected ZoneTwo presentation receipts in Zone04. The patch selects `ZoneFourPresentation.played` in Zone04, uses its real environment level, and reports Zone04 stage/pending as `unobserved` because no Zone04 public stage/pending API exists. Zone02 and Zone03 branches retain their existing values.

Added passive observations:

- Fragment, hidden A/B and final signal controller counters; fragment sequence active, actual fragment view active state and progress.
- Actual Mission Log, Photo Lab and helm audiences; selected captured photo POI ID, hidden A/B matching audience and actual bound EffectSurface availability.
- Registered EffectDirector sequence IDs/ages, read with Editor-only reflection. This is read-only diagnostic inspection of the existing private `sequences` list; no handle, condition, request, clock or gameplay data is changed. `presentationSequenceTelemetryObserved` reports whether this internal shape is readable. A registered sequence does not prove exposure.
- Real fragment/hidden A/hidden B/final signal voice playback times through the existing `TryGetAudioTime` read API (`-1` means no currently measurable voice), final-water request state and the actual bound helm tint graphic alpha/active state.
- Revealed mission location IDs, main-complete/hidden-unlocked/hidden-complete/ending-choice state. No contact coordinates, undiscovered target positions, creature AI state or future trajectories are recorded.
- Committed ending state cached across cabin destruction, plus actual EndingPresentation ID, active/playing state, elapsed clock, rendered title and survey text. If the ending transition misses the 5 Hz live receipt sample, the selected isolated save is read once after EndingPresentation exists; the existing `TryRead` normalizes an in-memory copy and does not write. Receipt source distinguishes live state, primary save and backup read.

The component flags establish observed component state, not legibility, player attention, audible recognition or fear. Fragment `Visible` indicates its actual card is active; the real screenshot is still required to verify content and fade. Sequence/voice/counter/receipt observations remain distinct. Existing modal, focus, transition and time-scale observations supply context.

Offline source exception: `Tools/QA/summarize_zone02_experience.py`. `--zone Zone04` now uses actual Ending scene and committed ending receipt, preserving first unintended zone departure separately. It exposes first committed receipt, first settled matching EndingPresentation, and full presentation clock completion (hidden >=16 seconds; normal >=12 seconds). A normal ending is not silently called hidden. Zone04 counters and optional surfaces are included. Existing genuine VFR/audio mux logic is unchanged; there is no invented PCM, interpolated frame or hidden state mutation.

## Offline verification

- Python source compiled; CLI help advertises Zone02/Zone03/Zone04 and rejects the historical Zone02 initial-report template in Zone04 mode.
- Existing real Zone02 final and Zone03 final event indexes were recomputed in memory with their own stored PCM statistics, avoiding rereading large audio files. Every existing nested key/value remained identical; new optional coverage fields are additive. Existing reports/raw files were not rewritten.
- An in-memory parser fixture verified hidden Ending authority, separate settled/full clocks, an earlier unintended Zone03 departure remaining unsuccessful, absent telemetry remaining unknown, fragment counter observations and normal 12-second completion. Fixture data is parser validation only and is not gameplay evidence.
- Scoped `git diff --check` passed. Native compilation/import remains root's responsibility before the baseline. All Assets source must then stay frozen throughout the live voyage.

## Legitimate start and exact APIs

Reuse the historical `G10.Prototype.Editor.ZoneTwoExperiencePlaytestTools` class name. In Edit Mode, before any Play or Debug action:

```csharp
ZoneTwoExperiencePlaytestTools.SetupIsolation(@"D:\G10\Docs\QA\Zone04ExperienceLoop\baseline");
```

Root starts normal Play with the AnkleBreaker Unity MCP. Once the actual Main Menu is visible, start real evidence before clicking DEBUG:

```csharp
ZoneTwoExperiencePlaytestTools.StartRecording(@"D:\G10\Docs\QA\Zone04ExperienceLoop\baseline\evidence", 45);
```

Read actual rendered button RectTransforms to obtain screen coordinates and call `Click(x,y,label)`. Mouse coordinates use Unity bottom-left origin. Click actual `MainMenuDebugStart.DebugButton`, actual overlay child button `Zone04`, leave regular upgrades at NONE, and actual `StartButton` (`StartDebugRun`). Do not call `DebugExpeditionPreset.Create`, `SelectZone`, `StartRun`, button `onClick.Invoke` or SceneFlow start directly in the measured voyage. The existing real UI already provisions all prior-zone requirements for a Zone04 start; no setup installer is needed.

Example read-only button-center calculation followed by ordinary queued mouse input:

```csharp
var rect = (UnityEngine.RectTransform)button.transform;
var point = UnityEngine.RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
ZoneTwoExperiencePlaytestTools.Click(point.x, point.y, "actual-menu-button");
```

The actual MainMenu uses an overlay Canvas. For other canvases, use that canvas's actual render camera instead of assuming null.

Input APIs after actual helm/UI selection:

```csharp
ZoneTwoExperiencePlaytestTools.HoldKeys("W", 1.0, "normal-forward");
ZoneTwoExperiencePlaytestTools.Click(x, y, "actual-ui-control");
ZoneTwoExperiencePlaytestTools.CaptureInputAgent(55); // only while actual capture UI is active
ZoneTwoExperiencePlaytestTools.Mark("operator.gap.start", "operator processing");
ZoneTwoExperiencePlaytestTools.Mark("operator.gap.end", "operator resumed");
ZoneTwoChartInputDriver.DriveTo(chartX, chartY, 4, 60);
ZoneTwoChartInputDriver.Cancel();
ZoneTwoExperiencePlaytestTools.Status();
```

The chart driver reads current helm indications and only queues normal WASD; the operator chooses every waypoint from the displayed chart. Cancel it before another input owner. It stops on obstruction. `ZoneNavigation.Obstructed` can stay latched after a collision: repeatedly requesting the same driver does not clear it. Retain the collision, cancel the driver, use actual A/D turn and short W to leave the visible obstacle, then retry a revised waypoint after the actual obstruction indication clears. Do not clear the flag or call gameplay setters.

At a recorder buffer rotation, close one segment and immediately start a fresh segment while retaining the same live voyage and isolation. Stop the final recorder only after the actual full hidden Ending is captured (elapsed >=16); stop input, then exit Play and wait for EnteredEditMode before inspecting restored overrides. `StopRecording` does not restore paths early. No Assets/source imports during Play. Native tests later require their own outer isolated folder.

## Actual Zone04 contract

The main measured route is actual L1 PHOTO -> actual Photo Lab SEND -> CAPTURE, L2 PHOTO -> SEND, actual dialogue choice TIẾP TỤC KHÁM PHÁ, real RADAR discoveries for hidden A/B, actual PHOTO -> SEND at both, and physical final signal approach -> committed hidden Ending -> full 16-second ending presentation. Normal ending coverage belongs in native tests so the actual voyage can reach every Zone04 presentation opportunity. Mission counts, depths, coordinates, unlock conditions, 09/09 mandatory and 11/11 full survey requirements remain authored.

L1 is near the actual entry and should be inspected immediately using actual survey/Radar/Photo UI. After L1 capture, open real Mission Log long enough to read the recovered fragment and persistent hint. Do not navigate to undiscovered source coordinates. Choose continue in the actual ending dialogue after the mandatory survey, then use the displayed recovered coordinates, actual Radar and visible chart. The final point becomes visible through the real route after both hidden photo submissions. The return gate to Zone03 remains active and must not be mistaken for an ending.

The source map is native 1920x1080; world X increases right, world Y increases upward, and PNG image Y is 1080-worldY. Root/driver should inspect real chart/Radar before each turn. The dark contours, rather than watercolor tone alone, define visible obstruction. No offline route is guaranteed collision-free; it is important to retain real mistakes, rests and detours in both voyages.

## Offline close/mux command

After the recorder closes, with Python and ffmpeg available:

```powershell
python 'D:\G10\Tools\QA\summarize_zone02_experience.py' 'D:\G10\Docs\QA\Zone04ExperienceLoop\baseline\evidence' --zone Zone04 --segment zone04-baseline --status 'actual baseline; ending completion assessed from committed state and visible Ending clock' --mux > 'D:\G10\Docs\QA\Zone04ExperienceLoop\baseline\evidence\analysis-console.json'
```

For the final run, use a fresh final isolation/evidence folder and matching segment/status. Omit `--initial-report-dir`. Listener replacements, device/DSP buffering, sampled clocks, dropped buffers and frame capture overhead limit timing precision; inspect actual images and actual WAV/video before interpreting exposure.
