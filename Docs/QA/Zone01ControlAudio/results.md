# Zone 1 control audio — 2026-10-21

## Fixed

- Steering and depth adjustment previously played non-looping three-second clips on a three-second ControlApplied cooldown. There was no release edge to stop the sound.
- ZoneNavigation now exposes actual turning/depth activity from the latest movement operation. Zero input, braking, restore, blocked movement and depth limits clear the corresponding activity.
- ZoneOneAudioController reads this activity in LateUpdate, after cabin input/braking. Both machinery definitions now use persistent looping sources and an 80 ms fade, so playback survives clip boundaries and release promptly fades out. Other ambience/cutscene/mission behavior is retained.
- ZoneOneAudioSetup preserves these loop settings when reinstalled.

## Validation

Unity Editor 6000.4.2f1, G10 instance port 7890, native Unity Test Runner through Unity MCP. Test saves and photo archives are isolated temporary paths.

| Run | Result | Evidence |
| --- | --- | --- |
| New control-audio regressions | 2/2 passed | `control-regression.json` |
| Existing Zone 1 audio/story/cutscene regressions | 8/8 passed | `existing-audio-regression.json` |
| Cabin navigation, shared energy budget, depth behavior | Initially 39/40 passed | `navigation-initial.json` |
| Corrected stale map fixture | 1/1 passed | `navigation-rerun.json` |

The map fixture still iterated exactly three Zone 1 POIs and accessed index 2 after the earlier two-location story update. It now checks the actual authored locations, validates spare pooled markers stay hidden, and exercises hover/alignment for every remaining POI. Production map behavior was not changed for this fixture correction. All 50 distinct tests have passing results after this rerun.

The new tests hold each steering, dive and ascent control beyond two complete clip durations, observe actual AudioSource sample wrapping and a single uninterrupted voice, then release and rapidly re-press. They also verify depth floor/ceiling, panel changes, pause/resume, application focus, transition lock and exhausted energy stop or prevent actual playing sources.

An initial combined test-name/group filter selected zero tests; that invocation is not included in passing counts. The test runs above use separate filters.

Final native inspection: no C# compilation errors; Bootstrap remains open, stopped and clean. The two bound clips are still the original three-second assets, with `loop=true` and `audioFade=0.08`. No new audio generation, mission progression changes for Zone 2–4, release build, commit or push in this fix. These checks verify source playback/lifetime; listening balance on the player's output device is not measured by automated tests.

Zone 1 pacing proposals and the unfinished broader mission-refactor comparison are in `Docs/Zone01PacingReview.md`.

## Legacy control click removal

AudioManager automatically attached its old ButtonClick sound to every Button, including the six held Helm controls. Auto-binding now skips CabinPointerTarget.IsHelmControl. No serialized audio callbacks were present on those six authored buttons; their runtime auto-binding was the source of the unwanted sound.

`legacy-click-removal.json`: 3/3 PlayMode tests passed. The new regression dispatches actual pointer down/up/click events to all six controls and confirms they still move the ship while the legacy UI SFX source remains silent. A temporary half-second test clip makes accidental playback detectable; an ordinary menu button serves as a positive playback control. The test restores the original clip and removes its temporary objects. The existing held-audio loop and stop/pause/limit regressions also passed. No scene or audio-asset edits were needed for this fix.
