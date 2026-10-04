# Photo submission, HUD and asset cleanup

2026-10-04. Started on `main` at `7bc39a6b12b41a99b9f687d0086dd7d1ec6f5905`, with a clean working tree. No commit or push.

## Implementation

- Physical cabin camera/shutter, photo composition, classification, cooldown, charge consumption and archive remain intact. A matching GoodPhoto/LifeDetected now stores four stable candidate IDs without recording a mission objective.
- PhotoCaptureService owns current-state validation and SEND. Repository membership, logical mission zone, stable objective ID/type/target, completion, visibility and ship availability are checked before RecordObjective. Only a successful record marks the selected photo submitted. Duplicate candidates stay unsent and cannot grant another reward.
- Photo Lab has one persistent SEND callback, normal/submittable/submitted/completed/unavailable states, and named left/center/right controls. Existing Previous/Next and preview are retained. The installer helper was applied twice to verify idempotency; broad installers were not rerun.
- Optional candidate IDs and submitted state round-trip through expedition saves and PNG sidecars. Save version remains 3. Existing day-start rollback is unchanged.
- Six authored station WatercolorHUD/Camera objects were removed from Zone01, and SharedHud no longer generates them. Physical CameraHotspot/OpenCamera and the inactive legacy camera panel/scripts remain.
- Every Photograph objective in all three four-zone tests now uses physical camera capture followed by Photo Lab SEND, including normal and hidden endings. Test positions are set programmatically; this is not a complete manual sailing playthrough.
- All four screenshot-writing sites use Temp/TestCaptures, including callers that previously supplied a Temp/ prefix.

The Unity Feature Implementation and uGUI skills guided the service/UI separation, targeted scene edits, stable-ID serialization and PlayMode verification. No navigation/death/deadline/stat/minigame redesign was made.

## Exact changed / added files

Paths are relative to D:/G10.

```text
.gitignore
Assets/_Project/Scenes/Gameplay/Zone01.unity
Assets/_Project/Scripts/Computer/ComputerDataContracts.cs
Assets/_Project/Scripts/Computer/ComputerDesktopSkin.cs
Assets/_Project/Scripts/Computer/ExpeditionSave.cs
Assets/_Project/Scripts/Computer/PhotoCaptureService.cs
Assets/_Project/Scripts/Computer/PhotoLabView.cs
Assets/_Project/Scripts/Editor/PhotoCaptureEditor.cs
Assets/_Project/Scripts/Editor/WatercolorUIEditor.cs
Assets/_Project/Tests/PlayMode/CabinNavigationPlayModeTests.cs
Assets/_Project/Tests/PlayMode/ExpeditionEndToEndPlayModeTests.cs
Assets/_Project/Tests/PlayMode/ScopeUIPresentationPlayModeTests.cs
Assets/_Project/Tests/PlayMode/WorldMapPlayModeTests.cs
Assets/_Project/Tests/PlayMode/ZoneOneStoryPlayModeTests.cs
Assets/_Project/Tests/PlayMode/PhotoSubmissionPlayModeTests.cs (new)
Assets/_Project/Tests/PlayMode/PhotoSubmissionPlayModeTests.cs.meta (new)
Docs/AI/PhotoSubmissionAndCleanup.md (new)
```

## Exact deleted / untracked files

23 tracked generated root screenshots removed with git rm (recoverable from Git):

```text
capture-failure.png
capture-hit.png
capture-playing.png
capture-success.png
creature-camera.png
creature-photo.png
expedition-failure.png
expedition-journal.png
expedition-rest.png
expedition-status.png
helm-art.png
map-art.png
map-location-hover.png
radar-art.png
radar-filled-complete.png
radar-filled-sweeping.png
scope-camera.png
scope-chart.png
scope-dialogue.png
scope-helm.png
scope-radar.png
scope-world-map.png
world-map-hover.png
```

Four exact Artboard assets and their matching metadata removed:

```text
Assets/_Project/Art/Environment/Map/Artboard 1.png
Assets/_Project/Art/Environment/Map/Artboard 1.png.meta
Assets/_Project/Art/Environment/Map/Artboard 2.png
Assets/_Project/Art/Environment/Map/Artboard 2.png.meta
Assets/_Project/Art/PhotoCaptureRuntime/Artboard 1.png
Assets/_Project/Art/PhotoCaptureRuntime/Artboard 1.png.meta
Assets/_Project/Art/PhotoCaptureRuntime/Artboard 2.png
Assets/_Project/Art/PhotoCaptureRuntime/Artboard 2.png.meta
```

Audit: each GUID appeared only in its own meta; no path/basename references in project code/import tooling; AssetDatabase.GetDependencies(path, false) across every imported Assets file returned zero references to these four assets. The runtime importer copies explicitly named Z1 files, not an Artboard directory wildcard. All four were tracked and initially unmodified.

| Asset | GUID |
|---|---|
| Environment/Map/Artboard 1 | 2813469fd67a11a4d9251e09ba8b5e09 |
| Environment/Map/Artboard 2 | 4212ec45c6d4a90419e58de4bc4e1695 |
| PhotoCaptureRuntime/Artboard 1 | 2f07dbfced261d24dacebc20754b843d |
| PhotoCaptureRuntime/Artboard 2 | 4cb4a1b43af45994084bd7193b57fcd1 |

Four IDE metadata files removed from the index only (git rm --cached); all local files retained, /.idea/ ignored:

```text
.idea/.idea.G10/.idea/.gitignore
.idea/.idea.G10/.idea/encodings.xml
.idea/.idea.G10/.idea/indexLayout.xml
.idea/.idea.G10/.idea/vcs.xml
```

## Deliberately preserved

- AssetInbox in its original location, including intentional source/staging/reference material. Nothing there moved, renamed or deleted.
- .vscode and local .idea configuration.
- All referenced artwork, existing desktop/window styling, physical camera, legacy camera UI and capture systems.
- CreatureCatcher, navigation, vessel-death, deadline and ship-resource implementation files.
- User save files: new photo tests use unique temporary save/photo folders.

## Validation

Initial photo-submission run: 8 passed, 0 failed (job 125dd5ce4c50).

First combined regression: 34 passed, 3 failed / 37 (job b950efed9ac6). DirectCabinInteraction, ZoneOneStory, CaptureMinigame, VesselDeath and all three ExpeditionEndToEnd routes passed. One new HUD assertion mistakenly treated the WorldMapPanel overview header as station navigation; corrected to retain its existing Resume/Cabin controls. No UI was added to satisfy that assertion.

Two failures in the unchanged PhotoSurveyPlayModeTests are outside this task's photo submission behavior:

- DepthButtonsChartAndRealRadarContact: expects the default spawn to detect a contact within 85 units before moving to it. Current independent daily contact placement/range does not meet that assertion.
- MissionUsesPoiRadiusAndNeverDrawsDestinationTiles: expects 8 overlay vertices (ship strokes only), but current overlay also draws location markers, yielding 24.

These failures occur before any PhotoCaptureService call, and their navigation/map runtime code is unchanged. They were not fixed by changing unrelated gameplay. No pristine-baseline Unity rerun was performed.

Broader regression, job 65c2c95e3ecf: 24 passed, 5 failed / 29. This included all 8 photo-submission tests again, now with a rendered Photo Lab screenshot and TMP overflow assertions. The HUD test reached an additional stale assumption that Map always opens the world overview; updated the test to assert the current remembered-zone behavior, then explicitly enter the overview. Its focused rerun passed 1/1 (job 67addd01715c).

The other four broad-suite failures are retained and reported, without changing their unrelated gameplay:

- ComputerAppsPlayModeTests.AppsReadProvidersWithoutInventingGameplayData expects an empty/offline Photo Lab from EmptyPhotoRepository, although the authored gallery is wired to PhotoCaptureService and can load archived photos.
- ComputerShellPlayModeTests.ShellBlocksCabinKeepsVoyageAndRoutesEscape expects speed 9, observed 0 after entering UI.
- WorldMapPlayModeTests.WorldMapHoverNavigationAndSelectionAreRetained expects WorldMapPanel, observed remembered MapPanel.
- WorldMapPlayModeTests.EscapeClosesEveryZoneWhileOnlyMapButtonReturnsToWorld dereferences a missing legacy control at line 131.

These four plus the two PhotoSurvey failures are outside the requested implementation; they are not claimed as passing. Existing tests were inspected, but a pristine-baseline Unity run was not performed.

Visual evidence: Temp/TestCaptures/photo-lab-candidate.png. Inspected the actual render: existing watercolor artwork/window style retained, preview visible, four metadata/status lines readable, Previous/SEND/Next aligned without text overflow.

Final required-suite rerun, job ab302b561936: **43 passed, 0 failed, 0 skipped** in 184.61 seconds. Fixtures: PhotoSubmission, DirectCabinInteraction, ZoneOneStory, CaptureMinigame, VesselDeath, ScopeUIPresentation and ExpeditionEndToEnd. All final versions of the changed tests were included.

Across distinct tests exercised in this task, latest outcomes are **59 passed / 6 failed / 65 total**: the 43 required tests above plus 16 additional passing tests and the six out-of-scope failures listed above. Earlier failed versions of the corrected HUD test are not counted as outstanding failures.

Final checks: zero compilation errors; git diff --check and git diff --cached --check pass; zero PNG files at repository root; .idea is ignored and no longer tracked, with local files retained; no diff under AssetInbox, .vscode or the navigation/core/ExpeditionLoop implementation. No generated font-cache or unrelated scene changes remain. Deletions are staged by git rm; implementation edits are not committed.

Acceptance confirmed by the required suite:

- Capture creates an unsent candidate and does not complete Photograph; exactly one photo charge is used.
- Photo Lab SEND completes the intended objective and sets submitted, with no additional charge.
- Re-submission and second candidates cannot grant duplicate progress/rewards.
- Candidate and submitted metadata survive Save/Continue and archive reload; legacy optional-field omissions remain readable.
- Capture returns PhotoRequired before SEND and starts the existing minigame after SEND.
- Death restores candidate/submitted state and objective progress from the unchanged day-start snapshot.
- All station HUDs lack Camera and retain functional Map/Helm/Radar/Cabin; the overview retains its distinct Resume/Cabin header.
- Actual captures are written under Temp/TestCaptures, not root. AssetInbox was not moved or deleted.

No requested implementation mismatch remains. Validation caveat: the broader suite is not entirely green because of the six explicitly documented tests. No target-platform build or manual full expedition was performed.
