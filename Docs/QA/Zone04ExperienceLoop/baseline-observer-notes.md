# Baseline observer notes — provisional while voyage is live

These are image/source observations, not human reactions. Root took over the delayed Main Menu start, clicked actual DEBUG → Zone04 → START DEBUG RUN with regular upgrades NONE, copied the initialized save, then used ordinary Helm depth/turn controls and a real Radar scan. Root's two read-only C# probes failed compilation (nonexistent Ship.MaxDepth and inaccessible internal telemetry DTO); neither executed gameplay. Control then passed exclusively to zone4_input_pilot. The failed probes are tool limitations, not game defects.

## L1 Photo Lab, UTC 2026-10-26T16:23:51.1008103Z

Root reviewed actual [frame-002508.jpg](baseline/evidence/frames/frame-002508.jpg). The real Photo Lab shows image 2/2, GoodPhoto, x290/y950, depth526.4m, heading348.2°, MISSION DATA SUBMITTED and disabled SEND. The photograph is dark blue water with a large illustrated flowering/branching plant in the foreground and darker forms behind it. The plant dominates the image. Root cannot confidently identify the source-authored wreck from this single frame; compositor execution is not proof of recognizability. The UI labels and SEND state are readable. Both archived photos exist.

Source documents say the four Zone04 creature bindings currently share the generic algae fallback. Compare the actual later photos before judging visual repetition. The wreck's composition and generic subject dominance suggest a recognition/identity question for human review; this single image does not establish that a player noticed or missed the wreck or experienced fear.

## Actual chart and navigation

Root reviewed [frame-003802.jpg](baseline/evidence/frames/frame-003802.jpg), UTC 16:28:16.7672824Z: the full illustrated chart, grid, completed L1 and return region are visible. `PhotoSurveyMap` explicitly keeps vessel position off the chart; its upper X/Y field follows pointer coordinates, not the ship. Preserve that authored instrument-reading mechanic. A human readability question remains around distinguishing the colored formations/contours while remembering Helm coordinates, but an automated route error alone is not proof of a map defect. The pilot retained two actual collisions and normal key-driven retreats rather than resetting the voyage.
