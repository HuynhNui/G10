# Bounded artwork integration validation

This is a fixture plan, not a claim of tests or runtime review already completed. Root owns Unity and chooses the final artwork presentation architecture. No existing native fixture or runtime code was changed for this plan.

## Proposed new fixture

Use a new `CreatureArtworkIntegrationPlayModeTests.cs` under `Assets/_Project/Tests/PlayMode/`, with outputs under this task's `native-validation/`. Do not overwrite the sealed Zone02–04 experience reports or reuse the old hidden fixture's outdated fallback limitation text.

Use the established `G10.Prototype.Tests` namespace and existing PlayMode assembly. Suggested cases:

1. **ZoneTwoSampleShowsAuthoredArtworkInRealLab**: start a controlled Zone02 fixture, aim at its first existing photo contact, use the actual quick camera, open real Photo Lab, screenshot after `WaitForEndOfFrame`, and record source texture/creature ID, photo ID, result, coverage and occlusion. Review this image before proceeding with the whole matrix. This is the presentation checkpoint required by the user, without user approval gating.
2. **EveryAuthoredCreatureKeepsItsArtworkThroughPhotoGallerySendAndContinue**: iterate current photograph objectives across Zones01–04. Expect 11 total, including two hidden sites, using the existing canonical data. Capture through the real camera, preserve the pending candidate before SEND, open Lab and verify its current `SelectedPhotoId` and `preview.texture` match the new `PhotoRecord`. Exercise Previous/Next after two images exist and verify the selection by ID rather than trusting list indices. SEND through the actual Lab callback, verify the matching objective/target and no duplicate reward. Save/Continue and compare PNG bytes, photo identity and mission metadata. Take and review one screenshot for each creature; a contact sheet cannot replace the full-frame review.
3. **CaptureEncounterAndCargoUseCorrectCatalogContentWithoutChangingCollisionRules**: six current photo/capture combinations. After SEND, invoke existing capture entry, verify the new encounter image and its aspect fit inside the existing presentation box. Preserve profile, collision sizes, charges and hit behavior. Take representative capture/cargo screenshots across applicable zones. Cancel one attempt to verify clean visual reset; use the established controlled `CaptureMinigamePlayModeTests.Win` helper for the rest, explicitly identifying it as fixture assistance. Inventory material icons remain material definitions unless their catalog design intentionally uses the creature image.
4. **HistoricalPhotographsKeepExactPixelsAndIdentityAfterCatalogArtworkChanges**: copy a pre-integration photograph and its metadata from the verified entry backup into isolated storage. Verify loose archive loading, Lab selection, Save/Continue and snapshot restoration preserve the same image hash and IDs. Use the matching old image payload in both PNG archive and timeline `SavedPhoto.png`, rather than regenerating a new photo from the changed catalog. Never alter the real persistent archive.
5. **ArtworkPresentationDoesNotGrantPhotoSuccessOutsideExistingRules**: validate a valid subject plus facing-away, too shallow and terrain-blocked cases through existing camera/validation APIs. Check charges remain consumed by actual attempts and bad records are stored but not submittable. If full paintings are presented separately from alpha masks, explicitly test that presentation pixels do not inflate the coverage mask. Do not assert that a changed watercolor subject's raw alpha coverage must equal the old fallback unless the chosen architecture intentionally preserves that mask.

## Existing reusable APIs and helpers

- `PhotoSubmissionPlayModeTests.StartPhotoAuditZone` demonstrates creating a prior-zone debug preset, then `SceneFlowController.ContinueGame`; its `Aim` searches bounded authored water angles and uses `PhotoSurveyZone.ContactPosition`/`DepthFor`. These state/position setters are permitted in an explicitly disclosed visual fixture, not ordinary-input voyage evidence.
- `PhotoSubmissionPlayModeTests.EveryAuthoredPhotographAcrossFourZonesRequiresManualSendAfterContinue` already checks all 11 photos, six capture combinations and two hidden sites. Reuse its structure instead of inventing mission IDs.
- Hidden sites: current fixture setup uses canonical progression `hiddenRouteUnlocked`, `endingChoice`, `ZoneOneStory.RevealPoi`; record these as fixture seeding. This does not prove radar discovery gameplay.
- Actual shutter: `CabinStationView.OpenCamera`, wait for `PhotoCaptureService.Photos` to grow and direct interaction to end. Read `MissionTargetId`, `MissionPoiId`, `MissionObjectiveId`, `Result`, `LastSubjectCoverage`, `LastSubjectOcclusion`.
- Actual Lab: `cabin.OpenComputer`, `ComputerScreenController.OpenPhotoLab`, read `PhotoLabView.SelectedPhotoId`, `preview.texture`, `sendButton.interactable`; SEND uses `lab.sendButton.onClick.Invoke` in existing controlled fixtures. `Previous`, `Next`, `SubmitCurrent` are existing UI callbacks.
- Capture: `CreatureCatcher.TryCapture`, `CaptureMinigameController.IsActive`, `CaptureMinigameView.creature.texture`; cancel through `minigame.Cancel`, controlled win through the existing test helper. Inventory reads `CreatureInventory.Items`, `CreatureInventoryView.icons`, `detailPreview`, `SelectedItemId`.
- Archive authority: `PhotoCaptureService.ExportPhotos` contains actual encoded PNG payloads; `RestorePhotos` decodes them. `LoadArchive` reads actual loose PNG+JSON. Catalog replacement must affect only future captures.
- Screenshots: `Canvas.ForceUpdateCanvases`, yield `WaitForEndOfFrame`, then `ScreenCapture.CaptureScreenshotAsTexture`, save PNG and destroy the temporary texture. Keep Game View rendering; direct Editor-update captures previously produced black frames.
- Every await uses a bounded realtime deadline and a descriptive assertion. Cleanup first loads MainMenu and destroys the old flow owner, then restores fixture overrides. Outer isolation remains until actual Play exit.

## Bounded regression selection

At minimum run the new fixture plus these existing cases when the final change touches the corresponding code:

- `PhotoSubmissionPlayModeTests.EveryAuthoredPhotographAcrossFourZonesRequiresManualSendAfterContinue`
- `PhotoSubmissionPlayModeTests.CandidateAndSubmittedStateSurviveContinueAndArchiveReload`
- `PhotoSubmissionPlayModeTests.LegacyPhotoSaveAndArchiveWithoutOptionalFieldsRemainReadable`
- `PhotoSubmissionPlayModeTests.FailedSendKeepsRuntimeJsonArchiveAndUiRetryable`
- `PhotoSubmissionPlayModeTests.BadPhotoIsStoredButCannotBeSubmitted`
- `ZoneOneQuickPhotoPlayModeTests.LampChangesNewQuickPhotosWithoutRepaintingArchivedPhotos`
- `CaptureMinigamePlayModeTests.FiveSeparateContactsCommitExactlyOnceAndRemoveRadarAndPhotoSubject`
- `ZoneFourHiddenVisualTests.ActualHiddenAAndBKeepDistinctBackgroundEvidenceThroughLabAndManualSend` only after checking its numeric/assertion assumptions still apply; do not rerun merely to obtain screenshots under its old output path.

Inspect imported texture settings and missing serialized references, then review full screenshots for distortion, painting boundaries/film-strip leakage, clipped details, subject scale, brightness, material color and consistency with the existing cabin/Lab. Record which images were actually viewed and any revisions. Compiler/test passes alone do not establish aesthetic quality. No full horror cycle or hours-long voyage is required.
