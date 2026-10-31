# Zone04 preservation audit — 31 October 2026

Read-only filesystem snapshot at **2026-10-31 03:51:27 UTC** (10:51:27 Asia/Saigon); font record comparison completed at the `fontRecheckedUtc` value in the accompanying JSON. The final voyage was still live. No Unity APIs, source/asset/configuration changes, cleanup, restoration, tests or recording controls were invoked. Only the JSON and this Markdown report were written. This is not a completed final cleanup report.

- **467/467 candidate-freeze hashes match.** Against the 390-file production entry manifest, exactly the seven expected Zone04 implementation/instrumentation files differ; the remaining 383 match. The prior Zone03 work remains preserved.
- **14/14 protected entry backups match their manifests.** Current protected files match entry in 13/14 cases. The only difference is the Alegreya generated font feature cache, classified below. Liberation fallback already equals its exact task-entry bytes; preserve its preexisting modified Git state.
- **156/156 entry persistent backups and 196/196 resume persistent backups match their recorded hashes.** No damaged or missing backup was found.
- All **56 real persistent non-Unity files excluding TestResults.xml** match both entry and resume manifests. No new non-Unity file was found. This covers the existing saves, photographs and player logs in `C:/Users/Artermis/AppData/LocalLow/DefaultCompany/G10`; no live isolated game save was substituted for real player data.
- Current real `TestResults.xml` still exactly matches the archived selected native run: SHA256 `49F2FD83951C00AE75A65F50AFACCA8C1A1A361A89FABE3B0785604F6DBA94CD`, **33 unique cases, all Passed**. Entry/resume XML backups both remain SHA256 `59463879BF6DA1D600AC8636A02FC159BFF11C82106945CFF57DA1D7D208CB70`.
- Unity-managed metadata differs as previously disclosed: compared with entry, 72 archived files are missing and one file changed; compared with resume, 112 are missing and one changed. There are 68 new files, all under `Unity/`. Keep the verified old backups and current Unity metadata; do not restore the entire persistent folder.
- `Assets/Screenshots/` remains empty, including recursive/hidden contents. Its untracked folder meta retains owned GUID `edabfb6ec50fcb242b9b0ca7a3beae60` and SHA256 `5506AB62D91EDB1A0F27FF50B57CE8E0E5B0543D600F400B0F28639D16E10E9B`. No removal was made.
- `Docs/Deliverables/` remains user-owned untracked work. `ProjectSettings/ProjectSettings.asset` is not currently dirty. The original four PerformanceTestRun deletions, Zone03 source/tests/reports, QA parser changes and existing font modifications remain in Git status. Do not overwrite or clean them.

## Font byte and record audit

The font bytes are unchanged from the 30 October audit: current SHA256 `C61E865C858A2831EF27989C578C6C4DB177D9612EE151BA05318793B77453CD`, 20,316,701 bytes. The exact task-entry backup is:

`D:/G10/Docs/QA/Zone04ExperienceLoop/preserved-local/entry-files/Assets/_Project/Art/UI/Fonts/AlegreyaSansSC-Regular SDF.asset`

Its verified entry SHA256 is `860EF25C76359946063E7413BD2C1344AD7A5330F080667AD4845BE565256BBA`, 19,410,451 bytes. This is the correct potential restoration source after Play Mode ends; Git HEAD is not the entry state.

All 100 exact two-space mapping field boundaries match in name/order; **only `m_FontFeatureTable` differs**. This count includes the `image data` field containing a space, which the previous audit's 99-field regular expression omitted. All other field slices are byte-identical, and the feature section is the sole change across the asset. Sequence entries are excluded from mapping-field detection. Complete feature records were compared as multisets, retaining duplicate counts.

| Feature group | Entry | Current | Removed | Added |
| --- | ---: | ---: | ---: | ---: |
| Multiple substitution | 0 | 0 | 0 | 0 |
| Ligature substitution | 9 | 10 | 0 | 1 |
| Glyph pair adjustment | 41,337 | 43,221 | 0 | 1,884 |
| Mark-to-base adjustment | 9,180 | 9,724 | 0 | 544 |
| Mark-to-mark adjustment | 0 | 0 | 0 | 0 |

The change remains **2,429 added generated feature records, zero removals**, with no non-feature field change. Archive the final current asset before any later restore and recheck this classification after the voyage stops, because the live Editor may add cache records.

## Remaining root-only finalization

Follow `final-integrity-plan.md` after the actual voyage and recording have ended: retain isolation through quit-save, recheck hashes and overrides, archive final font bytes, restore only the verified task-entry Alegreya copy if the differences still meet the cache classification, conditionally restore the original real TestResults only if the current file still matches the known selected 33-pass XML, and remove only the still-empty owned screenshot folder through Unity after preserving its meta. Preserve unexpected user changes and all current gameplay save/photo bytes. Report actual cleanup operations and remaining Unity metadata differences separately. No cleanup has been performed by this audit.

Machine-readable details and full point-in-time Git status are in `final-integrity-20261031-audit.json`.
