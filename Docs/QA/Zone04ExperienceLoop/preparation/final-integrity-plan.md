# Zone04 final handoff integrity — read-only audit and cleanup plan

Audit time: 2026-10-30 16:49 UTC. The final voyage is still live. This audit invoked no Unity tools, changed no game assets/source/configuration, restored no data, ran no tests, and stopped no recording. Only this report was created. Every observation below is a point-in-time check; the root operator must repeat the relevant checks after the voyage stops.

## Current integrity

- **467/467 candidate-freeze files match** `candidate-freeze.json`. No frozen source, test, production configuration or scene changed during the measured voyage at audit time.
- Against the 390-file entry production manifest, exactly seven expected files differ: `ZoneFourPhotoComposition.cs`, `ZoneTwoExperiencePlaytestTools.cs`, `ZoneFourFragmentView.cs`, `ZoneFourPresentationController.cs`, `ZoneFourPresentationState.cs`, `ZoneMapConfig.cs`, and `ZoneMapPresentation.cs`. The remaining 383 match entry, including the prior Zone03 runtime work covered by that manifest.
- **All 14 protected entry backups still match their recorded SHA256.** Current files match entry in 13/14 cases. The sole difference is the generated Alegreya font feature cache, detailed below. Liberation fallback already matches the exact task-entry bytes, despite being modified relative to Git HEAD before this task.
- Re-verified **156/156 entry persistent backups** and **196/196 resume persistent backups** against their own manifests: zero damaged/missing copies.
- The real persistent authority is `C:/Users/Artermis/AppData/LocalLow/DefaultCompany/G10`. Do not confuse it with the separate `G10IsolatedValidation/G10` test directory.
- In the real persistent directory, all **56 non-Unity-metadata files other than TestResults.xml** match both entry and resume manifests. These include the existing expedition saves/backups, photos and player logs. The only non-Unity-metadata difference is the native-generated TestResults.xml.
- Relative to entry, Unity analytics has 72 archived files missing and one changed file. Relative to the later resume manifest, it has 112 archived files missing and one changed file. This agrees with the previously disclosed Unity archive purge/session update; gameplay save/photo bytes are unchanged. The verified backups retain those old analytics bytes.
- `native-validation/TestResults.xml` independently parses as **33 unique cases, all Passed**. Its SHA256 is `49F2FD83951C00AE75A65F50AFACCA8C1A1A361A89FABE3B0785604F6DBA94CD`, identical to the current real persistent TestResults.xml and `verification.json`.
- `Assets/Screenshots/` exists and is empty, including hidden entries, at audit time. Its untracked folder meta has GUID `edabfb6ec50fcb242b9b0ca7a3beae60`. The entry git status did not contain this folder/meta. No removal was made.
- `Docs/Deliverables/` is current untracked user work absent from entry status. Preserve it. `ProjectSettings/ProjectSettings.asset` is not currently modified in git status; preserve its current user state, not the transient earlier backup.

## Font cache classification

Compared all 99 top-level YAML fields, preserving field order, between the exact entry copy and the current Alegreya asset. Field names/order are identical. **Only `m_FontFeatureTable` differs**; all other top-level field slices are byte-equivalent after identical UTF-8 text decoding. Feature records were compared as multisets of complete YAML records, so reordering and duplicate counts are handled.

| Feature group | Entry | Current | Removed | Added |
| --- | ---: | ---: | ---: | ---: |
| Ligature substitution | 9 | 10 | 0 | 1 |
| Glyph pair adjustment | 41,337 | 43,221 | 0 | 1,884 |
| Mark-to-base adjustment | 9,180 | 9,724 | 0 | 544 |

Multiple substitution and mark-to-mark groups are unchanged. Total additions: **2,429 feature records**, zero removals. This supersedes the earlier pre-native 968-addition snapshot because the final voyage generated more cache entries. The asset grew from 19,410,451 to 20,316,701 bytes.

- Exact entry backup: `preserved-local/entry-files/Assets/_Project/Art/UI/Fonts/AlegreyaSansSC-Regular SDF.asset`.
- Entry SHA256: `860EF25C76359946063E7413BD2C1344AD7A5330F080667AD4845BE565256BBA`.
- Current audit SHA256: `C61E865C858A2831EF27989C578C6C4DB177D9612EE151BA05318793B77453CD`.
- Liberation fallback entry/current SHA256: `F53586850A70C4F308E7E71B99B63881A49F38DE4BD40F4EDB7FE7AE6ECF8A52`. No restoration is currently needed for it.

## Precise safe final sequence for the root operator

1. Finish the authorized actual-input voyage and full ending observation. Release normal injected inputs, close every evidence recorder, and retain all raw data/gaps. Keep save/photo isolation active through Play Mode quit-save. Verify stopped/unpaused/clean intended editor scene and null/original save/photo overrides afterwards. This audit cannot infer that state from filesystem evidence.
2. Recheck all 467 frozen hashes and the 14 protected files. If any unexpected source/config/user file changed, preserve and identify it rather than reverting it. Preserve all current isolated saves/photos as final evidence; do not substitute an earlier checkpoint.
3. Before font cleanup, archive the final current font asset into ignored `preserved-local/` with UTC and SHA256, and repeat the field/record comparison. If the difference remains solely additive generated feature-cache data, restore **only Alegreya from its verified task-entry backup**, never from Git HEAD. Recheck hashes after the Unity import settles. Both fonts should then equal their task-entry hashes and remain in their original preexisting dirty Git state. If any new non-cache change appears, preserve it and do not overwrite it based on this earlier audit.
4. Preserve the selected 33-pass XML at `native-validation/TestResults.xml` and verify its SHA256. The old task-entry and resume copies of `TestResults.xml` are identical, SHA256 `59463879BF6DA1D600AC8636A02FC159BFF11C82106945CFF57DA1D7D208CB70`. Once no test job is active, restore the **real persistent** `DefaultCompany/G10/TestResults.xml` from either exact verified copy only if its current bytes still equal this task's known 33-pass XML. If another process/user has produced a different result meanwhile, preserve that result and do not overwrite it.
5. Leave real expedition saves/photos/player logs alone because they already match. Do not copy the whole old persistent folder back: doing so would resurrect expired analytics records, overwrite newer legitimate metadata and affect files this task did not author. Keep verified entry/resume backups; disclose the Unity-managed analytics difference. No analytics restoration is needed for gameplay preservation.
6. Reinspect `Assets/Screenshots/` immediately before cleanup. If still empty and the meta GUID still matches, the root may remove this task-owned empty asset folder through a Unity asset-delete tool, retaining a copy of the meta in ignored preservation storage first. If any content appeared, preserve the folder and investigate rather than deleting it. The previous owned screenshot is already archived and removed; this audit does not authorize deleting any new screenshot.
7. Preserve the original four PerformanceTestRun deletions; previous Zone03 source/test/QA edits; the existing `Tools/QA/summarize_zone02_experience.py` changes; `Docs/Deliverables/`; the existing Zone03 report folder; and all original font modifications. `ZoneTwoExperiencePlaytestTools.cs` already had entry changes and now contains task instrumentation: retain the combined result, never reset it wholesale.
8. Produce a final preservation JSON/report with UTC, verified backup/current hashes, actual cleanup operations, remaining analytics differences and final git status. Do not claim “all persistent files unchanged”: the honest narrower claim is save/photo gameplay bytes unchanged, old test result restored if performed, and Unity metadata differences archived/disclosed. Do not commit, push or clean other untracked user work.

The current status is an audit/plan, not completed cleanup or a claim that the voyage has reached the hidden ending.
