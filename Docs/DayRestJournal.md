# Day / Rest / Journal

The existing Central Computer now exposes REST and JOURNAL. SHIP STATUS includes day, zone, coordinates and rest availability; MISSION LOG reads the actual `PhotoSurveyZone` tasks and deadline. Its NEXT ZONE action becomes available after those tasks are complete.

## Configuration

Open `Assets/_Project/Scenes/Gameplay/GameplayCore.unity`, select the UIManager object, and edit its **ExpeditionLoop** component:

- **Zones / Base Days**: allowed days per zone, default 5. The final deadline day is playable; failure starts the following day if tasks are incomplete.
- **Zones / Rest Areas**: existing map coordinates and arrival radius. Zone01 defaults to Dock, X 600 / Y 100, radius 30 m.
- **Max Carry Over Days**: default 3. Days left when the real objectives first complete are awarded once on entering a new zone.
- **Creature Catalog**: stable creature IDs and icon assets used to rebuild inventory after loading. Creature01 is already assigned. Add future creature assets here when authoring them.

The component is already installed. `G10 > Computer > Install Day Rest Journal` can install it in a rebuilt GameplayCore scene. Start the full scene flow from **Bootstrap** when testing travel or loading a save from another zone.

## Timeline semantics

Rest requires confirmation at a valid rest area and cannot run during capture. It checkpoints the end of Day N, then starts Day N+1. Inventory, photographs, encounter presence, task progress, position, heading, depth and travelled distance are preserved. Restoring Day N returns to that end-of-day state before rest, deletes later journal entries, and replaces the Day N checkpoint on the next rest. The restore warning requires a separate confirmation.

The single save is `Application.persistentDataPath/Expedition/timeline.json`, with an atomic replacement and recovery backup. Photos in checkpoints include their image data; rolling back cannot reload discarded future photos from the legacy photo archive. No scene object references or additional save slots are serialized. Save also runs when leaving gameplay, pausing the application or quitting normally.

Deadline failure locks movement, scanning, photography, capture and zone progression, opens the existing computer's JOURNAL, and persists until a playable checkpoint is restored. A corrupt primary save falls back to the backup; if both are unreadable, the files are preserved and gameplay is blocked rather than overwritten.

## Existing-content limits

There is currently no finite energy, daily photo/capture allowance, upgrade system or separate unlock/discovery system. No replacement resource system was added; photo/capture attempts remain unlimited. Only reliable distance/photo/capture/task totals appear in journal summaries.

Zone02–04 are currently placeholder scenes without the cabin, computer or required task content. Zone01 supports the complete playable loop. Cross-zone state routing and rollback use the existing scene flow, but those later zones need their normal gameplay content before they can support the same player-facing loop.

## Verification

`ExpeditionLoopPlayModeTests` exercises rest gating and confirmation, preservation of actual inventory/photos/tasks, save reload, rollback truncation, failure locking/reload/recovery, carry-over caps, backup recovery, real zone travel and cross-zone rollback. Tests use isolated temporary save and photo directories. Rendering checks cover the computer's status, rest, journal and failure confirmation text.
