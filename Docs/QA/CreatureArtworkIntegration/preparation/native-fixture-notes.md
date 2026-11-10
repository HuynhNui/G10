# Artwork native fixture

Source: `Assets/_Project/Tests/PlayMode/CreatureArtworkIntegrationPlayModeTests.cs`.

Run only `ZoneTwoPrototypeArtworkReachesRealLabSendAndContinueWithoutRepainting` for the presentation checkpoint. It requires the new `Z2_Creature_01` binding but does not require the other creatures to be assigned. After settling the prototype and binding the rest, run `AllElevenCreaturesKeepArtworkThroughPhotoLabGallerySendCaptureAndContinue`.

The fixture explicitly requires the existing `G10.Zone02Experience.isolated` Editor session flag plus both nonempty outer save/photo overrides. Root sets these through `ZoneTwoExperiencePlaytestTools.SetupIsolation` in Edit Mode before starting the native job. Each case additionally selects temporary fixture-local save/photo paths and restores the outer paths only after its scene owners finish saving. Cleanup exits without touching preexisting scene owners if setup failed its isolation preconditions.

Both cases use authored water/contact/depth data, controlled positioning, refills and a debug preset. Hidden reveals are seeded through canonical progression. Capture cases use the existing controlled steering/hit helper `CaptureMinigamePlayModeTests.Win`. These are bounded integration/visual fixtures, not ordinary-input voyages or evidence about player skill or horror pacing.

Outputs are written incrementally to `native-validation/Prototype/` and `native-validation/AllCreatures/`: actual Lab/gallery/continued Lab screenshots, baked photographs, capture/cargo screenshots, and `evidence.json` containing IDs, imported asset paths, measurement results and hashes. Capture verifies the encounter drawing uses the catalog image, preserves its aspect inside the existing visual box and does not alter the collision size. Cargo checks the actual captured target's catalog icon. No old experience-loop output directory is reused.

The all-creatures case also copies one historical PNG+JSON from the verified entry backup into a temporary archive probe, loads it through `PhotoCaptureService`, appends that loaded photo payload to the isolated timeline, then verifies its pixels and all metadata after actual Continue. The real persistent directory is never a test output. Availability of this entry-backup probe is recorded explicitly; it is not silently replaced by a newly generated photograph.

## Hash interpretation

PNG file hashes and decoded image identity are separate. The existing compositor creates RGB24 PNGs; Unity's existing `LoadImage`/`EncodeToPNG` round trip can encode the identical image as RGBA. A byte comparison between the original PNG and a newly encoded image after Continue is therefore not a repainting test.

- `bakedHash`, `archiveHash`, `pendingContinueHash`, `submittedContinueHash`, `durableHash` report actual PNG encodings.
- Pixel hashes include width, height and every RGBA8 pixel. These must match exactly across capture, pending Continue, submitted Continue and the durable timeline payload.
- The **existing loose archived PNG file** must remain byte-identical through Save, SEND and Continue. SEND changes its JSON metadata only.
- The historical probe compares exact decoded pixels/dimensions and a metadata hash covering ID, captured time, result, coordinates, depth, heading, mission status, zone/POI/objective/target IDs and the legacy objective field. Its copied archived PNG must retain its original file hash.

The initial prototype job reached a real GoodPhoto/Lab and matched fixed-clock coverage/occlusion, then failed the overly strict PNG re-encoding assertion. Its unmodified outputs are preserved under `native-validation/Prototype-initial/`. The fixture was corrected to the distinctions above; this correction did not change production save or archive behavior. Later native results must be reported separately.

The first full matrix job reached the first real capture, then failed a fixture assertion that assumed limited capture charges. The actual ship already used unlimited capture attempts, so its five stored charges correctly remained five. That failure's JSON and capture screenshot are preserved in `AllCreatures-initial/`. The fixture now records the existing mode: unlimited attempts retain their stored count; limited attempts consume one. No production charge/resource behavior was changed.

For Zone01/02, fixed-clock `ZoneOneSurveyFrame.Build` compares new artwork against its original measurement image under identical world state, clock, light and response inputs. Only an in-memory content binding is temporarily switched, restored in `finally`; the test never calls `SetDirty` or `SaveAssets`. The separate EditMode compositor fixture covers replacement shape/aspect and foreground measurement behavior more broadly.

For all seven Zone03/04 photographic sites, the fixture additionally takes an actual quick-shutter calibration photograph using the original distance-specific measurement image at the same authored position, depth and bearing. It restores the new artwork binding in `finally`, saves the isolated state, deletes that unsent calibration photograph through the existing deletion authority, verifies its gallery/PNG/JSON removal and unchanged mission progress, then refills fixture resources. The new actual shutter must exactly match calibration coverage, occlusion and result. This branch is time-independent in production; it does not use the time-varying Zone01/02 survey frame. Calibration PNGs are retained only as explicitly named QA evidence, never as gallery photos, and no source asset is saved or marked dirty.

Screenshots use `Canvas.ForceUpdateCanvases`, `WaitForEndOfFrame` and real `ScreenCapture`. Root must keep Game View rendering and independently inspect the actual images. Assertions also verify the Photo Lab's real preview rectangle preserves the baked image's aspect. Tests and the presence of PNG files alone do not establish successful visual review.

## Final UV and modal verification

Full-screen review found the legacy capture RawImage atlas UV still cropping the new standalone PNGs. Production now caches the fallback texture and its UV, uses full `(0,0,1,1)` UV for catalog artwork, and restores the old UV on a default Begin. The focused capture regression simulates a nondefault atlas crop. The full matrix records and asserts full UV for all six encounters. Final screenshots were re-opened after the successful run; all six show complete organisms and settled UI.

The existing capture test fixture originally reset save overrides to null during cleanup, which broke the outer isolation precondition when running fixtures together. It now restores the paths captured on entry, after its scene owners finish saving. No gameplay resource behavior was changed.

A later matrix reached the second mandatory Zone04 SEND/Continue and correctly encountered the authored ending/exploration choice modal. The visual fixture had tried to inspect Lab behind it. `OpenLab` now evaluates the existing progression and selects its continue-exploring callback before station inspection; the choice is disclosed in the fixture scope. Production modal/progression code is unchanged. The final matrix job `c62d20014e23` passed all eleven photographs and six capture/cargo cases in 80.12 seconds, including both hidden creatures and the historical archive probe.
