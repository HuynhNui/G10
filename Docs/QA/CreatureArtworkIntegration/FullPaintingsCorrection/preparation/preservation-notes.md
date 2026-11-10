# Correction-entry preservation

This snapshot protects the real persistent state present at the start of the full-painting correction, rather than assuming the older integration-entry save is still current. It never restores or deletes live data.

- Current real persistent root: `C:/Users/Artermis/AppData/LocalLow/DefaultCompany/G10`.
- All 116 existing persistent files were copied and verified with SHA256 before and after copying.
- Gameplay state is the 54 files below `Expedition/` and `Zone01Photos/` (12,223,523 bytes). Logs, analytics, and TestResults are listed separately by path in the same entry manifest.
- All 556 existing first-party Scripts/Content/Data/cutout Creatures files, ProjectSettings, and Packages were protected. The cutout set includes all 14 PNGs and their metadata. The 11 canonical definitions are under Content and were captured.
- The photo capture profile and metadata under `Art/PhotoCaptureRuntime` are additionally recorded in `photo-profile-entry.json`; capture-minigame profiles, missions, and maps are covered by the main manifest.
- Newly exported `Art/SurveyPaintings` assets are correction outputs, intentionally not treated as pre-existing artwork.
- Copies are in ignored `preserved-local/entry/`. The prior integration-entry backup remains untouched.

Main entry manifest SHA256: `c13ad7754b3cacfa6057921ddb23bf4c3a27f067d8889ccea24ed7f1302eb919`.

`preservation_snapshot.py verify` checks the immutable backup copies. `compare` reports current gameplay changes separately from intentional production-source changes and newly created runtime metadata. It performs no restoration. A runtime test must use the project's outer isolation override before entering Play mode. If TestResults or an unrelated transient file changes, the root agent must review that exact path; no bulk save or analytics restoration is appropriate.
