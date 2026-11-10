# Entry preservation

Snapshot taken 2026-11-01 17:18:02–17:18:42 UTC (2026-11-02, Asia/Saigon), before native tests or production artwork integration.

- Branch: `codex/creature-artwork-integration`, HEAD `2f9451cd44018d23259945261d1d3490de9f4b96`.
- Root reported the entry checkout clean. The snapshot's later status contains only the new task's `Docs/QA/CreatureArtworkIntegration/` and `Tools/Artwork/` folders; no tracked modifications at that point.
- Real persistent directory: `C:/Users/Artermis/AppData/LocalLow/DefaultCompany/G10`.
- 168 persistent files, 12,278,067 bytes: all existing saves, backup/debug timelines, archived photographs, TestResults.xml, logs and Unity metadata. These categories are recorded individually rather than assuming every future Unity metadata change is gameplay damage.
- 2,210 protected project files, 373,818,118 bytes: all tracked `Assets/_Project/` files (including existing art, source, content catalog, missions, maps, scenes and metadata), all tracked `ProjectSettings/` and `Packages/`, plus TMP font assets and metadata outside the first-party folder.
- Every file was hashed before copying, in its backup, and again at the source. All 2,378 copies matched. An independent backup verification also found zero missing or different files.
- Backups live in the ignored `preserved-local/entry/`. The manifest and verification result remain reviewable outside that directory.
- PSD source downloads live separately under the repository's already ignored `.tools/creature-artwork-sources/`, with root-owned `sources.json` checksums. No PSD download is copied into runtime Assets by this preservation step.

Reproduce checks from the repository root:

```powershell
python Docs/QA/CreatureArtworkIntegration/preparation/preservation_snapshot.py verify
python Docs/QA/CreatureArtworkIntegration/preparation/preservation_snapshot.py compare
```

`snapshot` refuses to replace the existing entry snapshot. The helper never restores or deletes files. A live comparison after integration is expected to identify intentional source/art/content changes and possibly generated font or Unity metadata changes; inspect them against the task's allowed scope before any narrowly targeted restoration. Do not bulk-copy the backup over later user changes.

Native validation must use the existing outer isolation before entering Play Mode. Keep overrides active through scene cleanup and the quit-save; `ZoneTwoExperiencePlaytestTools` restores them on entering Edit Mode. Recheck both `ExpeditionSaveStore.PathOverride` and `PhotoCaptureService.ArchivePathOverride` after Play exits. Copy new native TestResults.xml into this task's QA before considering restoration of its original bytes.
