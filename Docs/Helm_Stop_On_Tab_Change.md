# Stop the ship when leaving the helm

CabinStationView now brakes immediately on opening/closing a station panel. Its Update also
brakes whenever the helm is not the active visible panel, including Escape and direct UIManager changes.
Position, heading and depth stay unchanged outside the helm; off-helm Coast calls were removed.
Losing application focus or pausing also clears pointer holds and brakes. Keyboard input must
return to neutral before it can propel the ship again after a stop/tab change.

This supersedes the earlier deliberate off-helm coasting behavior in the Computer Phase A/B notes.
No scene wiring changes. Automated tests skipped per user instruction.
