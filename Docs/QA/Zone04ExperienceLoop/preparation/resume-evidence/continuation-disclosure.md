# Zone04 baseline resume: durable checkpoint versus recorder cap

This is a read-only evidence assessment made on 2026-10-27. It does not load, edit, normalize, or repair a save. Root owns Unity; this evidence worker made no Unity calls. The baseline is still incomplete and no redesign or final audit is inferred here.

The first segment is sealed at 2026-10-26T17:00:15.3561257Z after 2700.009 recorded seconds. Its final state is day 3, position (1826.144897, 828.059143), depth 526.427063 m, distance 2392.184570 m, energy 36.296326, hull 28.011368 and Radar 8. Only `Z4_L1_PHOTO`, `Z4_L1_CAPTURE` and `fragment` have been committed; the hidden route and ending remain absent.

The isolated primary save inspected before the new Continue action was written at 2026-10-26T16:39:11.905967Z, SHA256 `96c56a0d90bd0b19843dfba0211a7ebd6eb02479f715359d262fdf64bf631082`. It contains the actual day-3 Rest checkpoint at (1246.914429, 925.299255), depth 526.427063 m, heading 93.686050 degrees and distance 1616.670044 m, with energy/hull 100 and Radar/photo/capture charges 10/20/5. The first recorded state matching that checkpoint is 2026-10-26T16:39:11.9484807Z, 1436.598736 seconds into the first recording. Journal contains days 1 and 2.

A later read after Continue wrote the file at 2026-10-27T14:10:15.935925Z has SHA256 `30cb3e00a12643e51acb3c16516369bccbd7ff8967f35a3de25746e7ac30ac11`, with the same selected gameplay checkpoint fields. The worker attempted a guarded evidence copy of the initial bytes, but aborted before copying when the file hash changed; no original-byte copy is asserted by this note. The root may have its own prior snapshot. Actual continuation recorder samples, not this later save hash, establish the loaded voyage state.

Consequently, normal DEBUG CONTINUE is a legitimate checkpoint restore rather than a seamless same-live voyage. It rolls back 775.514526 m of already recorded travel, the subsequent resource use and collisions. No mission or primary cue receipt progress is lost. Both the earlier travel and its later replay remain part of the baseline evidence. The rollback must be disclosed; do not remove earlier damage or count resource restoration as a new Rest.

Report the raw UTC wall span, the recorded gameplay/window intervals, the long external interruption gap, the legitimate load and checkpoint replay separately. Neither the external gap nor unknown no-input time is evidence of fear, pacing, boredom, confusion, or designed content length. Do not concatenate generated PCM/images over the gap, replace the completed first segment, or label its cap as a completed baseline. The measured target remains physical hidden arrival plus the complete 16-second hidden Ending presentation.

Machine-readable selected fields and exact sample boundaries: [checkpoint-vs-cap.json](checkpoint-vs-cap.json).
