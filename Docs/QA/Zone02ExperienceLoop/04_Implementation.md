# Implemented single redesign and functional validation

This phase follows the completed original/continued baseline and documented02audit/03redesign. No second redesign cycle is authorized after the final voyage.

## Changes

- `ZoneTwoPresentationController`: one pending presentation delivery; real sequence/voice clock, audience and visible-output checks before durable receipt. Radar requires its actual signal image; scrape requires observed Computer interference, main dip and actual scrape voice time; reflection requires actual anomaly visibility; following/knocks require real audio clock. Cancellation before delivery leaves a retryable opportunity after quiet recovery. Committed receipts retain no-replay/Journal semantics through the existing save owner.
- Ordered opportunities remain available after Hull installation. An optional actual propulsion→stop tail provides an alternate narrative prerequisite when Radar is not revisited. It does not grant a contact or change navigation. Pause/modal/capture/restore clears stop context so interruption cannot manufacture the clue. A derived experience-stage readout is observation only.
- Quiet time accumulates after dramatic playback ends. Root recurrence varies with observed travel/progression; post-scrape outside ambience is restrained while engine/ventilation and warnings retain their authority.
- `EffectDirector.TryGetAudioTime` is a passive child-voice clock query, with no change to original playback/mission/save behavior.
- `ZoneTwoPhotoMotion` and native `G10/UI/ZoneTwoWaterMemory` shader: one reused material alters only the outer22% of the actual Photo Lab image very slowly. Center UVs are exact, textures/PNGs untouched, original material restored on audience loss/pause/restore/zone change/disable/destruction. No map/radar/contact/control distortion added.
- Presentation-only `ZoneTwoExperienceSetup.InstallPresentation` authors preparation, following tail, longer0.73s faint glass motion and more restrained Computer interference. Existing EffectDirector, EffectSequence, UIEffect and illustrated art are reused. No new package or replacement creature illustration.

## Resources and licensing

One new3.4s stereoPCM44.1kHz derivative, `Assets/_Project/Audio/Zone02/following_tail.wav`, is built offline by `Tools/Audio/derive_zone02_following.py`. Its exact filter, source hashes, output hash, inherited terms and checks are in `following_tail.provenance.json`. Peak0.124054,0 rail samples. It derives only from existing outside_movement/root_friction sources;0 API calls,0 credits purchased/used. Recorded ElevenLabs Free noncommercial/attribution limitations are retained. Human listening approval remains pending.

New presentation assets: native shader, PhotoEdgeMotion material, Following sequence and four effect definitions. Existing Radar/Reflection sequences, Computer signal and dip assets have intentional narrow changes. The shared cabin diff adds controller references and catalog entries; mission/map files are protected.

## Native installation and validation status

The narrow installer ran on the real shared cabin and saved it. Shader `G10/UI/ZoneTwoWaterMemory` has0 native shader errors; scene inspection found0 missing scripts; C# compilation reported0 errors. All eight protected mission/map files match entrySHA256. Clean Bootstrap was restored before Test Runner.

Initial selected PlayMode job `f62be992a61a`:20/21 passed in171.3s, including all six new experience cases and three return-intent cases. The old reflection-render test used Single() for a sequence that now has three reflection steps; it was corrected to inspect all three distinct authored effects, visible Image/UIEffect, shader support and actual captured pixels. The initial failure is retained in functional/initial-selected-result.json. All fixture screenshots now write into this dedicated report directory, and self-overwritten historical QA artifacts were restored to their clean entry versions.

Post-change EditMode job `6eb8fc616d39`:103 passed,1 optional skip,0 failed,2.38s (functional/editmode-result.json). The earlier pre-change EditMode run is not used as post-change certification. Expanded PlayMode regressions and precise repaired-test results are recorded below when complete. Native tests establish functional behavior, not fear or a final input-driven voyage.

The prerequisite return gate repair remains separately described/tested in `blocker-repair.md`; no gate geometry, mission count/location/capture, upgrade cost or exit coordinate was changed.

## Completed regression evidence and runner limitation

Expanded job e10086a5c1b2 was interrupted by a native TestRunner PlayModeRunTask/ExitPlayModeTask exception and bridge/domain reload. Last retained progress was70 passed/2 failed of88; it is not claimed as a completed88-case pass. Both observed failures were stale pre-entry assertions in ExpeditionLoopPlayModeTests: Zone01 already uses Bio Lamp, and its current rewards do not grant Emma Blueprint. Only those assertions were updated against unchanged mission/story definitions. See functional/expanded-interrupted.json.

With source edits/imports frozen, precise recovery job29c60ec46228 passed21/21 in197.89s: the three-step reflection render test, two corrected loop tests, legacy return migration, round-three return/cargo preservation, remaining Zone03 exit flow cases and all14 ZoneExitMap cases. See functional/selected-repair-result.json. Together with the initial20 successful Zone02 cases and EditMode results, this covers the requested sequencing, persistence, Journal/Continue, pause/rest, Radar charges, SEND/capture, Hull/physical exit, cleanup/accessibility and owned-material reuse. Native regression success is not final experience evidence.

Preservation audit before the final voyage detected changed normal/debug persistent save hashes. The original normal timeline was recovered byte-for-byte from its surviving .bak; its older backup and original debug pair could not be located in the bounded recovery search. See save-preservation-incident.md and save-incident-current.json. Prior statements of untouched saves describe intent, not successful preservation of every original file.

The editor recorder's conditional restoration in StopRecording was removed; only the explicit EnteredEditMode callback restores overrides, and BeforeSceneLoad reapplies persisted isolation. This corrects the likely debug quit-save leak. Source/imports stayed frozen during the final voyage. At final StopRecording, actual save/photo destinations still pointed to final/isolated-save/timeline.json.debug and final/isolated-photos. After Play stopped, overrides were null and all six persistent files matched their pre-final hashes (save-hashes-after-final.json). The incident did not recur in the final voyage; earlier missing originals remain unresolved.

End handoff: eight mission/map files and GameplayCore match entry hashes; both user-modified font assets and TimeManager were restored from exact entry copies, retaining the user's original dirty state. The shared cabin's only diff is six presentation-reference/catalog lines. Clean stopped Bootstrap, zero native shader messages and zero console errors were observed after final playback. No commit, merge or push was made.
