# Persistent-save preservation incident

**The original normal timeline was recovered byte-for-byte. Preservation of its original older backup and original debug pair was not achieved in the available evidence.** The incident must remain disclosed in the final report. Earlier “untouched saves” language describes intent, not a successful preservation result.

This document records offline file/hash and source/log findings. No user save was edited by this document's author. Root backed up the incident files before recovery and restored the known original normal timeline; the final voyage uses a fresh isolated reporting save. The isolated baseline/final initial snapshots are equivalent, which does not undo this incident.

## Known entry, incident and recovered hashes

Entry inventory: [user-saves-before.json](user-saves-before.json). Incident inventory: [save-incident-current.json](save-incident-current.json). Pre-final recovery verification: [save-hashes-before-final.json](save-hashes-before-final.json). Preserved incident bytes remain under `preserved-local/incident-current/`.

| File | Entry state | Incident state / recovery |
|---|---|---|
| `timeline.json` | 338271 bytes; SHA256 `769BE7AC07782F3817E2EB6618A36650F02A9745C305A5190F147550DD0A483B` | Incident primary: 3623 bytes, SHA256 `39F258C3424D3E8026F31F9614E7B437C7F3A4E73F86186F1B796BD0C64EB2E4`. Original 338271-byte primary survived as `.bak` and was restored; pre-final hash equals the entry primary exactly. |
| `timeline.json.bak` | 338269 bytes; SHA256 `20228950269F46DFC97C9A2DEFFA187936B3472F6E65CFE530CBDEB435166DB8` | At incident it contained the original 338271-byte primary, SHA256 `769BE7AC07782F3817E2EB6618A36650F02A9745C305A5190F147550DD0A483B`. Pre-final `.bak` still has that primary hash. The original 338269-byte older backup has **not been recovered in available evidence**. |
| `timeline.json.debug` | 4911 bytes; SHA256 `C7B8743F10B4C01E32396EB090CDCC9A3DD7F3C970BB696EF53F3B59B31CFFA6` | Incident/pre-final: 3791520 bytes; SHA256 `E92DE23106AD95A9A8A635515CF3DA4A4320BB167522BC49ACC4031442599DD4`. Original 4911-byte debug file has **not been recovered in available evidence**. |
| `timeline.json.debug.bak` | 4911 bytes; same original SHA256 `C7B8743F10B4C01E32396EB090CDCC9A3DD7F3C970BB696EF53F3B59B31CFFA6` | Incident/pre-final: 557414 bytes; SHA256 `A659F3E54CB129A8B1970B60BB02FB1C7130002FFD311A1CABA97D579A5A6F0C`. Original debug backup has **not been recovered in available evidence**. |

The two dated `timeline.json.before-zone3-demo-20261011-142140` copies remain 3229 bytes with entry SHA256 `475267EB935E8DC21DCD7442477CEA37BB1E12F0474BC1344639DAC185D9E89B`. They do not substitute for the missing original current backup/debug pair. No reconstruction from later gameplay is claimed to be byte-exact recovery.

## Observed times and causal assessment

Times below use Asia/Saigon (UTC+07:00), 2026-10-24. File modification times were read from the preserved incident copies; they agree with the recorded voyage/native failure windows.

| Time | Observation |
|---|---|
| 13:05:39.010 approximately | Original baseline recording stopped after the unintended Zone01 return. Preserved `timeline.json.debug.bak` modification time is **13:05:39.318107**. |
| 13:36:45.009 approximately | Same-save baseline continuation recording stopped after actual Zone03 arrival. Preserved `timeline.json.debug` modification time is **13:36:45.392378**. |
| 16:03:28 | Interrupted native job `e10086a5c1b2` recorded TestRunner `PlayModeRunTask`/`ExitPlayModeTask` errors following a live compilation/domain reload. |
| 16:03:29.112667 | Preserved incident normal `timeline.json` modification time, after the native failure. |

**Debug-write mechanism: strong timing/code inference, not captured write tracing.** The original editor helper called `StopRecording()` during `ExitingPlayMode`; that method conditionally restored save/photo overrides based on `EditorApplication.isPlayingOrWillChangePlaymode`. This did not enforce an explicit `EnteredEditMode` boundary. Meanwhile, `ExpeditionLoop.OnApplicationQuit()` calls `SaveCurrent()`, which resolves the current static save path at write time. If isolation was restored during exit while the debug timeline selection remained active, the quit-save could target the user's persistent debug file. The two debug modification times closely match the two measured voyage shutdowns. Exact playing-flag values and write call stacks were not logged, so the inference must not be presented as a directly traced fact.

**Native normal-write mechanism: supported reload/failure sequence; exact final write not traced.** `ExpeditionSaveStore.PathOverride` is an ordinary static auto-property. A source/domain reload during a live test can clear it while runtime objects/state survive hot reload, leaving the normal persistent path as fallback. Editor.log records the script compilation, callback re-registration after domain reload, controller NullReferenceException and erroneous exit task. An initialized loop can then quit-save to that fallback. Most relevant normal fixture teardowns first load MainMenu/unload gameplay and only then restore old overrides; their successful ordering is not independently evidence of the leak. The native abort/reload did not follow an established clean completion path.

The store uses rotating `File.Replace(..., path, path + ".bak")` publication for ordinary writes. This explains why one previous normal primary remained recoverable and why an older backup can be displaced. It does not establish that every unrecovered file is permanently unrecoverable.

## Correction before the final measured voyage

Root removed override restoration from `StopRecording()` entirely. The helper restores remembered overrides only on explicit `EnteredEditMode` (or explicit Edit-mode teardown), and adds a `RuntimeInitializeOnLoadMethod(BeforeSceneLoad)` reapplication from persisted `SessionState` isolation. Root checked the actual final `PathOverride` before the final gameplay start. Runtime source imports/native tests are kept separate from the measured voyage. These are instrumentation/preservation corrections, not a second horror redesign.

The pre-final hash check establishes successful recovery of the original normal primary. The completed [post-final persistent-file hash audit](save-hashes-after-final.json) shows all six files unchanged from their protected pre-final hashes. At StopRecording the actual save and photo destinations still pointed into final/isolated-save and final/isolated-photos; after Play stopped both overrides were null. The corrected isolation protected this final shutdown. The original normal primary still matches entry exactly. The older normal backup and original debug pair remain unresolved; a successful final audit does not recover those earlier bytes.

## Initial baseline/final state verification and limits

[final/initial-state-comparison.json](final/initial-state-comparison.json) verifies that both initial Debug save snapshots are **5608 bytes and byte-identical**, SHA256 `873E418A7E94A108150FD51FC6F03A49C959E3DE4F82AD76FEDB7F387BC1D0EC`. Day, Zone01 completion, untouched Zone02 objectives/receipts, entry position/depth, resources, regular upgrades, checkpoint, tutorial state and both organism spawns match exactly. This supports equivalent measured initial states; it is not evidence that earlier persistent-save preservation succeeded.

No paid resource/service, purchase or generation credit was used for this incident investigation/recovery. The creative audio derivative elsewhere in this task uses existing project sources with recorded inherited terms; it does not recover save data. File hashes, tests, playback counters and visual flags do not establish human fear, subjective audio quality or emotional effectiveness.
