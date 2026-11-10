# Final read-only preservation audit

Independent audit after final native matrix `c62d20014e23`. No Unity calls, restoration, deletion or production mutation.

## Root handoff cleanup after this read-only snapshot

Root archived the exact final native PASS XML as `native-validation/final-native-test-results.xml`
(SHA-256 `cb6e2ebea252c4739346ad4bca2c9beab99af185e15a2cfa3c0ac5a825534c09`),
then restored only the verified original persistent `TestResults.xml`
(SHA-256 `59463879bf6da1d600ac8636a02fc159bff11c82106945cff57da1d7d208cb70`).
Current and backup hashes were checked immediately before copying. No gameplay,
font, Insights or Analytics file was restored or deleted.

The [final preservation summary](D:/G10/Docs/QA/CreatureArtworkIntegration/preservation-final-summary.json)
rechecks all 54 original gameplay files after cleanup: every hash matches; the TMP
font matches entry. Unity is stopped in clean Bootstrap, outer isolation disabled,
and both save/photo overrides null. The sections below describe the earlier
read-only snapshot before this narrow root-owned test-output restoration.

Audit UTC: 2026-11-03T09:48:01.674693+00:00

- Reverified all 2378 entry backups: 0 damaged or missing.
- No protected project or original gameplay file is missing. 84 entry Unity Editor Analytics cache payloads are now absent; 28 other entries differ.
- Original Expedition files: 6, all byte-identical: True.
- Original photo PNG/JSON files: 48, all byte-identical: True.
- All 54 original gameplay save/photo files are byte-identical to the entry snapshot.
- Expected project modifications: 26; unknown differences: 0.
- Persistent output differences: 2; new persistent files: 28, all Unity Editor Analytics: True.
- TMP font differences after final run: 0.

## Static protected scope

| Category | Files | Unchanged |
| --- | ---: | ---: |
| ProjectSettings and Packages | 28 | 28 |
| Existing map-named assets and navigation data | 20 | 20 |
| Existing authored mission assets | 5 | 5 |
| Existing profile assets | 5 | 5 |
| Existing scenes | 9 | 9 |
| Existing first-party art | 565 | 565 |
| Existing TMP fonts and metadata | 42 | 42 |

All 17 changed catalog, prefab and upgrade assets were compared after removing only their explicitly allowed artwork fields. Every remaining line is unchanged, including IDs, mission/catalog references, descriptions, material IDs and authored prefab state. The two Zone01 prefabs change only SpriteRenderer `m_Sprite` to their matching new art GUIDs.

## Final native result

The native XML independently identifies one passing case, `AllElevenCreaturesKeepArtworkThroughPhotoLabGallerySendCaptureAndContinue`, from 2026-11-03 09:36:05Z to 09:37:25Z. Its root reports total 1, passed 1, failed 0. The final tool-result JSON is also read and hashed in the JSON audit. Parent reports final six capture visuals pass after UI settles and Editor returned to clean Bootstrap with isolation disabled and path overrides null; this audit makes no independent Unity/UI calls.

## Persistent differences and narrow backup candidates

These are records for root review; this audit performs no restore.

- `TestResults.xml`: task final native matrix test output; copy exact current bytes to QA before any narrow restoration.
  Current SHA-256: `cb6e2ebea252c4739346ad4bca2c9beab99af185e15a2cfa3c0ac5a825534c09`.
  Entry backup: `D:\G10\Docs\QA\CreatureArtworkIntegration\preserved-local\entry\persistent\TestResults.xml`.
  Entry SHA-256: `59463879bf6da1d600ac8636a02fc159bff11c82106945cff57da1d7d208cb70`.

- `Unity/a3aae1b7-25d8-4adc-a7e5-91b44be5748e/Insights/ArchivedEvents/Session/session_end_event`: Unity generated session metadata; preserve across user time gap.
  Current SHA-256: `d7cd48ff2c71b12202f2e98db4df0b1f8634b41674d7e2fcbacd3d468bd52338`.
  Entry backup: `D:\G10\Docs\QA\CreatureArtworkIntegration\preserved-local\entry\persistent\Unity\a3aae1b7-25d8-4adc-a7e5-91b44be5748e\Insights\ArchivedEvents\Session\session_end_event`.
  Entry SHA-256: `71fd617fc272c7c5641be8a26abb84eaa30a4eb33185b4fe80639f342388b136`.

Before any root-owned restoration of native `TestResults.xml`, archive its exact current final-run XML under this task QA and verify source and backup SHA-256. Keep Unity Insights and Analytics across the user time gap. The 84 absent entry files and 28 new files are all Editor Analytics `c/e/g/s` cache payloads. These are observed cache differences; the audit cannot attribute each expiration to a specific actor. Their original bytes remain in the verified entry backup. Do not recreate stale Analytics events. No TMP font restoration is needed at this audit point. Do not overwrite gameplay saves or archived photographs.

## Limits

This snapshot precedes any root-owned narrow TestResults restoration. SHA values describe the exact files at this point. Runtime Editor exit and image review statements are attributed to parent, while this audit independently verifies backups, live files, XML result, stored test result and static asset fields.

Full file-level hashes and classifications: `final-readonly-preservation-audit.json`.
