# Main Menu tutorial reset QA control

## Scope

Temporary **XÓA TRÍ NHỚ** button, created at runtime below Quit in Main Menu. The existing `SceneNavigationButton.Start` Main Menu setup calls `MainMenuTutorialReset.Ensure` inside `#if UNITY_EDITOR || DEVELOPMENT_BUILD`. Repeated setup returns the same component. Release builds do not call the creation path; the component also guards creation/actions and disables an accidentally authored instance outside developer configurations.

The button shows the requested Vietnamese title/body with XÓA and HỦY. Opening or cancelling does not write the save. Keyboard/controller focus starts on HỦY, underlying menu selectables are temporarily disabled and restored, and Escape/controller Back cancels. Confirmation persists immediately, closes the modal and shows success without navigating. File errors show failure feedback rather than false success. Styling reuses the Main Menu button's sprite/colors/font and the existing dark menu overlay style. No new dialog framework or scene wiring is needed.

## Existing reset API and activation safety

Calls `ExpeditionSaveStore.ResetTutorialProgress(waitForNewGame: true)`, the existing API used by the Editor reset command, extended with an optional argument. Default calls and the existing in-play reset behavior remain unchanged. No save parsing/copying/writing is duplicated in UI.

The optional `TutorialProgressState.waitForNewGame` flag defaults to false for existing saves. The menu reset clears completion, learned IDs and presented IDs, and sets this tutorial-only flag. It preserves current, zone/day, missions, cargo, photos, ship, endings/unlocks, journal and dayStart. PlayerPrefs are never accessed by this UI.

Mismatch discovered: checking only Zone01 + incomplete knowledge would restart Intro on Continue in an already-progressed Zone01 after clearing knowledge. The flag suppresses activation for the current expedition (including rollback/reload). New Game clears only this deferral flag while retaining the existing knowledge-preservation policy. Following an explicit QA reset the preserved knowledge is empty, so fresh Zone01 starts Intro. No tutorial step, station/app permission rule, dialogue sequencing, SceneFlowController or production mechanic was changed. Save format stays v4 with a backwards-compatible optional boolean.

## Files changed for this request

Modified:

- `Assets/_Project/Scripts/UI/SceneNavigationButton.cs`
- `Assets/_Project/Scripts/Computer/ExpeditionSave.cs`
- `Assets/_Project/Scripts/Tutorial/TutorialManager.cs`
- `Assets/_Project/Scripts/Tutorial/TutorialProgressState.cs`

Created:

- `Assets/_Project/Scripts/UI/MainMenuTutorialReset.cs`
- `Assets/_Project/Scripts/UI/MainMenuTutorialReset.cs.meta`
- `Assets/_Project/Tests/PlayMode/MainMenuTutorialResetPlayModeTests.cs`
- `Assets/_Project/Tests/PlayMode/MainMenuTutorialResetPlayModeTests.cs.meta`
- `Docs/AI/MainMenuTutorialResetQA.md`

The previous tutorial changes, scene/config changes and generated font caches were already in the working tree and were preserved. No Main Menu scene or other artwork edit is required. No commit/push.

## Validation

Focused/required run `f6c86490ee50`: **41 passed, 0 failed, 0 skipped** (116.42 seconds):

- MainMenuTutorialResetPlayModeTests: 7
- TutorialSaveTests + TutorialPlayModeTests: 20 (unchanged)
- ExpeditionSaveResetTests: 3
- ExpeditionNewGamePlayModeTests: 1
- ProjectSceneFlowPlayModeTests: 2
- VesselDeathPlayModeTests: 8

The new fixture covers the four visibility-policy combinations, reset/cancel/idempotent UI, preservation of full serialized non-tutorial state and preferences, no auto-start, Continue in Zone01–04, then New Game/Intro at `(100,400)`, heading 90, depth 230.

Broader regression `eab69f95b8c7`: **34 passed, 0 failed, 0 skipped** (133.11 seconds): DialoguePlayModeTests (6), PhotoSubmissionPlayModeTests (8), DirectCabinInteractionPlayModeTests (5), ZoneOneStoryPlayModeTests (12), ExpeditionEndToEndPlayModeTests (3). Total **75/75 passed**. No existing tutorial tests were changed for this request.

Unity compiled with zero errors. `git diff --check` passed. Main Menu and confirmation were visually inspected at 1920×1080: all Vietnamese text is legible, buttons fit and the menu is dimmed behind confirmation. The real-save visual check only opened and cancelled confirmation, never confirmed deletion. Editor was returned to non-playing Bootstrap. Confirmation screenshot: `C:/Users/Artermis/.codex/visualizations/2026/09/29/01a0ec54-2211-7171-9fa7-20ed3340349e/main-menu-reset-confirmation.png`.

Release visibility is checked via the pure policy and compile-time guards in the creation/action paths; this is not a standalone release-player build test.
