# Optional chart input driver

`ZoneTwoChartInputDriver` is editor-only, opt-in control assistance for the Zone 02 experience playtests. It is inactive after script import/domain reload and never runs automatically in a production voyage.

The operator reads the actual chart, chooses a single waypoint, opens the real helm, and calls:

```csharp
G10.Prototype.Editor.ZoneTwoChartInputDriver.DriveTo(x, y, tolerance: 4f, maximumSeconds: 60);
G10.Prototype.Editor.ZoneTwoChartInputDriver.Status();
G10.Prototype.Editor.ZoneTwoChartInputDriver.Cancel();
```

Each waypoint is bounded to 300 real seconds at most. There is no waypoint list, automatic mission route, obstacle lookup, terrain search or path planning. The operator must select the next chart waypoint separately and deal with obstructions through ordinary controls. Start and stop are marked in the active evidence timeline; input decisions are not logged every frame.

The driver reads public position, heading, speed and maximum speed, the displayed helm obstruction/resource availability, current zone identity and visible panel/pause state. It queues only W/A/D keyboard states onto the existing keyboard before ordinary input updates. It never calls `Navigate`, `Brake`, restore methods, mission actions, UI actions, resource writers, `InputSystem.Update` or game-clock ticks. North=0/east=90 follows the existing compass. Releasing W uses the normal game deceleration; the approach estimate uses its current 18 units/s² tuning. Arrival releases keys as soon as the ship enters the requested radius; remaining natural momentum can carry it farther.

The driver releases the existing playtest tooling input at start. **Cancel it before any other keyboard/mouse automation or manual steering.** If another `ZoneTwoExperiencePlaytestTools` input request appears, it cancels rather than retaining keyboard ownership. It also stops on a pause, helm leave/modal, zone change, keyboard loss, an obstruction shown by the helm, unavailable movement resources, deadline, Play Mode exit or assembly reload. Assembly reload removes its subscriptions. Only explicitly starting another waypoint reactivates it.

This is control automation, not evidence of a human's navigation skill or emotional response. It needs the Unity Input System and the existing editor playtest tooling assembly. Importing the file may trigger recompilation/domain reload; import only between measured runs, let Unity finish compiling, and check compiler/console diagnostics before starting recording. Source review alone does not confirm successful Unity compilation or runtime fidelity.
