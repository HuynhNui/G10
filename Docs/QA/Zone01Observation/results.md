# Zone 1 observation and pacing — validation

**Historical / superseded:** the user rejected the added live Camera workflow. Its UI, components and installer have been removed. These screenshots and results describe that earlier iteration only. Current quick-shutter → Photo Lab → SEND validation is in `Docs/QA/Zone01QuickPhoto/`.

Date: 2026-10-21. Unity 6000.4.2f1, G10 on main, Unity MCP instance 7890. No commit or push.

## Result

All 60 distinct PlayMode tests have passing results after the focused fixture rerun. C# compilation reports zero errors. Test save/archive paths were isolated temporary directories; the player's real timeline was not reset.

| Evidence | Result |
| --- | --- |
| `feature-playmode.json` | First five observation/pressure feature tests passed after fixing the initial UI binding. |
| `regression-initial.json` | 59/60 passed across PhotoDeletion, PhotoSubmission, PhotoSurvey, Tutorial, ZoneOneAudioGameplay, ZoneOneCutsceneVisual and ZoneOneObservation fixtures. |
| `regression-layout-rerun.json` | 7/8 passed after the final visual layout pass; all six observation tests passed. The remaining PhotoSurvey test exposed another retired POI reference in the same old fixture. |
| `radius-fixture-rerun.json` | The remaining PhotoSurvey test passed after correcting the fixture. |
| `native-inspection.json` | Reinstall leaves 828 hierarchy objects unchanged, nine pooled image layers, both shortcuts bound, zero missing scripts, Zone 1 two sites and initial depth cap 500 m. Zone 2–4 still have their existing 3/4/5 sites. |

The broader run includes the full 35-second NEW GAME opening, complete tutorial progression, real SEND/CAPTURE/Bio Lamp gameplay, held steering/dive/ascent audio across clip boundaries, failed saves, archive deletion/recovery, Continue, Journal rollback, pause, and presentation keyframes. The observation tests cover charge-free voluntary entry, old-photo immutability after lamp installation, shared live/still composition, lit scenery on the exit route, subject motion without contact relocation, pause, first-pressure cue persistence and its quiet gap after knocks. The new Upgrade shortcut is checked against the module card's world rectangle for overlap and exercised through its real Button listener.

## Failures found and resolved

- The first feature run (`initial-playmode.json`) reported missing CanvasGroup references on the new links. The installer now explicitly creates/binds them; the component also requires/caches its dependencies and tolerates teardown order. No log suppression was added.
- An earlier broad run was interrupted after the existing PhotoDeletion setup repeatedly waited only 25 seconds for a 35-second NEW GAME opening. The fixture had already seeded a returning-player save; it now Continues that save. All 11 deletion tests passed in the completed regression. Production transition timing was unchanged.
- The existing PhotoSurvey template test assumed a 20-unit radius and the removed `zone01-east` POI. It now tests inside/outside the actual authored radius and retargets to the remaining second POI. Production geometry and mission configs were unchanged by these fixture corrections.
- Visual review found the first lamp-check button position covered the expedition module. The button was moved into the blank area above Modules, and a rectangle-overlap assertion was added. The final screenshot shows the corrected layout.

## Visual evidence

- `01-algae-observation.png`: live L1, before lamp.
- `02-algae-light-restored.png`: live L1 after lamp.
- `03-lit-exit-route.png`: seabed/particles visible on the route without a mission subject.
- `04-deep-subject.png`: L2 preview; subject art is still the existing placeholder, with rock decoration rather than the algae cluster.
- `05-helm-observation-entry.png`: voluntary Helm shortcut.
- `06-upgrade-check-light.png`: corrected Upgrade layout and lamp-check shortcut.
- `07-saved-photo-mode.png`: frozen captured photo and explicit return-to-live control.

All images are actual 1920×1080 Game View captures, not mockups. Existing watercolor textures are reused; no new artwork, sound generation, packages or renderer settings were introduced. No release player build or subjective headphone/speaker mix evaluation was performed. The second creature still requires final artist artwork. Behavior and setup are documented in `Docs/Zone01Observation.md`.
