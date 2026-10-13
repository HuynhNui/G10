# State QA inventory — 2026-10-12

Baseline: `13a859a13326b016b32eb29b2ae3c23c5e2b3937`, verified local `main` and `git ls-remote origin refs/heads/main`. Initial worktree clean. QA branch: `codex/state-qa-hardening-2026-10-12`. Unity 6000.4.2f1, Windows64 Editor, AnkleBreaker 2.39.7, Test Framework 1.6.0. No push/merge; player saves are out of scope.

## Evidence and scope

Reviewed Round 3 report, Round 2 verification, PlaytestFeedbackFixes, PhotoSubmissionAndCleanup, RestPositionFix, NavigationDeathCabinChanges and PlayableExpeditionFlow; the older UnityProjectContext/DayRestJournal documents describe obsolete deadlines, entry points and placeholder zones. Current code/configuration wins over those historical descriptions. Existing Round 3 reports are evidence of earlier runs, **not** this pass's baseline results.

## Ownership and mutation boundaries

Paths below are relative to `Assets/_Project/Scripts`.

| Owner | Authoritative state / entry points | Side effects and boundary |
|---|---|---|
| `Computer/ExpeditionLoop` | In-memory `ExpeditionSave`, day, day-start, Journal, logical zone, cumulative counts, upgrade levels, ending choice; `Rest`, `RestoreDay`, `PrepareTravel`, `RecordEnding`, `SaveCurrent` | Captures live providers into snapshots, writes via SaveStore, rebinds providers and refreshes historical maps. `Blocked` is not the same as scene transition or pause. Inventory `ItemAdded` increments cumulative captures. |
| `ExpeditionSaveStore` in `Computer/ExpeditionSave.cs` | Version-5 JSON, selected normal/debug timeline, `.bak` recovery | Deep-copy through JSON. Read normalizes/migrates then validates each current/day-start/Journal snapshot. Write flushes `.tmp`, then atomic replace. Destructive history operations publish truncated backup first, restoring old backup if primary publication fails. |
| `Missions/ZoneMissionRuntime` | Completed objective IDs, receipt IDs and research/item/recipe/flag/reveal/zone/ending lists | `RecordObjective` validates ID/POI and evaluates all reward rules; rewards may add inventory and emit events. `RestoreProgress` copies/normalizes **without evaluating rewards**. `ResolveCapture` separately allows repeatable materials. |
| `Missions/MissionProgressState` | Serializable ID lists; no scene references | Normalize removes empty/duplicate IDs; Copy detaches every list. Exposed live ProgressState is mutable; production callers must respect the owner. |
| `Computer/PhotoCaptureService` | Bounded PhotoRecord list, image textures, cumulative photo count, candidate identity | Actual camera composition consumes charge; capture alone is not PHOTO completion. SEND revalidates repository membership, zone, target, objective, visibility and operational state. PNG/sidecar is ancillary to timeline-embedded image data. DELETE stages file moves and commits removal from every checkpoint before disposing textures. |
| `Navigation/PhotoSurveyZone` | Generic task flags plus transient/daily contacts; delegates configured missions to MissionRuntime | Restores daily encounter coordinates from snapshot. Contacts/radar discovery are not map-hover authority. Photo/capture resolve using common map coordinates and authored depth. |
| `Navigation/CreatureInventory` | Immutable item stacks in a mutable owned list; 12 distinct slots, stack quantities | TryAdd emits ItemAdded and Changed; consumes validate all costs before mutating; RestoreItems validates all stacks and does not emit ItemAdded. A reward receipt is not permission to synthesize missing cargo during restore. |
| `Navigation/ZoneNavigation` / ShipState | Vessel XY, heading, depth, distance and resources | Charge use, collision/death, movement budget. `TransitionBlocked` and `ExpeditionBlocked` are separate. Designer scene-state override is intentional and normally OFF. |
| `Computer/ShipUpgradeAction`, Loop / RegularShipUpgradeRules | Regular levels/material cost/stats; story module installed flags belong to mission runtime | Regular purchase stages ship + inventory, commits, restores both on failure; no refill. Story modules and rock action have their own prerequisites and receipt/flag ownership. No balancing changes are part of QA. |
| `Computer/ExpeditionComputerView`, `Core/SceneFlowController` | Presentation/confirmation, not checkpoint content | Rest presentation owns a transition token and delegates the one day increment. Journal uses end-of-day checkpoints and warns/truncates future history. Repeated callbacks must not repeat mutations. |
| Loop death coroutine / SceneFlow | Genuine same-day `dayStart`, not last ordinary Save | Cancels capture/dialogue/direct work; blocks gameplay; commits copied day-start before canonical reload. No valid day-start means explicit explanation + menu, never fabricated rollback. |
| `UI/ExpeditionProgression`, `UI/CabinZoneSession`, SceneFlow | Logical travel, route flags, active provider binding | Travel saves departure + carried resources and merges only global unlocks into destination; objectives/receipts/reveals stay zone-local. Backward arrival migration occurs before new coordinate assignment. Fresh XY arrival latch prevents re-travel on load/turn/depth alone. |
| `UI/SavedZoneMapProgress`, `UI/ZoneMapPresentation`, `UI/PhotoSurveyMap` | Read-only detached projection of current timeline for historical maps | Active map reads live mission runtime; historical maps read saved-zone copy. Opening a map must never grant receipts, cargo, objectives or reveals. |
| `Tutorial/TutorialManager` / ZoneOneTutorialDirector | Persistent learned/presented steps on `save.tutorial` | Tutorials observe real tasks/devices; learned knowledge intentionally survives Journal/death/New Game. Failed tutorial save does not publish cloned knowledge. Helm accumulation uses live state with retry. |
| `UI/ExpeditionProgression`, `UI/EndingPresentation` | Saved end/explore choice, hidden route/endingReached | Choice is explicit, save-backed; cancelled dialogue makes no decision. Hidden reveal uses radar, final signal requires movement; presentation never grants completion. |
| SceneFlow / `UI/PauseMenuController` / UIManager | Scene loading, transitions, modal/input ownership | Canonical gameplay uses GameplayCore + cabin Zone01 with logical zone configs, not four independent mission runtimes. Transition fade brakes and blocks navigation; pause is its own overlay and timeScale. APIs need authority guards in addition to raycast blocking. |

## Save / restore order

1. Continue selects timeline and validates JSON (primary, otherwise backup); schema/regular upgrade migration is detached, not an automatic disk rewrite.
2. Loop reads timeline, binds canonical cabin to logical zone config, migrates old coordinates before applying current-coordinate arrivals.
3. ApplySnapshot validates/reconstructs known cargo, cancels direct/minigame work, resets radar/camera/contacts, restores photos → cargo → survey tasks → exact mission lists → terrain → daily contacts → canonical ship stats → voyage; brakes last.
4. Fresh New Game alone records a true Day-1 start. Rest captures end-of-day, clones Journal checkpoint, increments day once, refills in place, generates next-day contacts, copies next-day start and commits before publishing runtime ship state.
5. Journal captures prior runtime for failure recovery, applies target while binding is blocked, commits truncated history/backup, then unlocks/rebinds logical zone. A day-start from another day is invalidated.
6. Death copies a valid day-start, truncates future history and commits before scene reload. Ordinary Save/Continue/travel never replaces day-start.
7. SaveCurrent captures into a detached candidate and publishes `save` only after a successful write. This alone does **not** roll back mutations performed by a caller before SaveCurrent.

### SEND boundary after QA-001

Loop now owns SEND publication. MissionRuntime snapshots every objective/receipt/reward list and cargo, then stages ordinary objective evaluation with mission/inventory notifications deferred. The photo is marked only in that transaction, and buffered ItemAdded counts are included once in the candidate. Failure restores the exact prior mission, cargo, photo and timeline in memory and discards events; success publishes events only after the timeline is durable. Archive sidecar writing follows the authoritative commit. Ancillary sidecar failure reports a warning but cannot undo that committed SEND. No restore path evaluates reward rules or manufactures receipts.

After QA-002, Loop guards major gameplay mutations against pause, transitions and an in-flight SEND; the navigation/device guard also derives pause from PauseMenuController. Ordinary SaveCurrent remains permitted during pause for menu persistence, but cannot publish an in-flight SEND. Persistent tutorial knowledge is still outside checkpoints; tutorial observers see the mission success only after durability and can save that knowledge synchronously.

## Intentional persistence exceptions

- Learned tutorial knowledge and user preferences are outside gameplay snapshots.
- DELETE is permanent across current, day-start, Journal and recovery copies; it removes image references, not earned mission progress/receipts or cumulative charges/counters.
- Ordinary rollback may leave loose future PNG/sidecars on disk. The timeline, not loose archive files, determines restored gallery/progress. New Game does not delete the loose archive directory.
- Legacy saves and a Journal rollback without a same-day start cannot reconstruct an unstored day-start. Death returns to menu; Continue + Rest establishes a genuine new start.
- Legacy item aliases are converted; removed Adhesive02 cargo is intentionally discarded. Purchased speed/efficiency targets migrate without purchases/refills.
- Designer preview ship settings can intentionally override saved resources when explicitly enabled. This is not enabled for normal gameplay.
- Debug saves use `.debug` and separate backup; DEBUG provisioning is a test/development preset, not proof that gameplay objectives were earned.

## Existing coverage (reuse, do not duplicate)

Baseline discovery: 60 EditMode + 265 PlayMode cases, including parameterized cases.

| Existing fixture(s) | Relevant coverage |
|---|---|
| ExpeditionEndToEnd | Real camera/SEND/capture/module/radar routes; RoundThreeJournal variants (neither/PHOTO-only/both), future repeat stacks, cross-zone rollback + Continue; historical Zone01 checks; restore does not create missing receipts; all three return gate pairs + Rest/Journal/Continue/cargo/legacy destination. |
| PhotoSubmission | Charges, bad photos, candidate identity/zone/target revalidation, duplicate SEND/candidates, archive/Continue, legacy photo fields, unsent/submitted day-start death, Zone02 required PHOTO before capture. |
| PhotoDeletion | Permanent prune of all checkpoints; UI ID selection/modal/index shifts; timeline/move/primary publish failures; corrupt primary backup recovery; interrupted quarantine; traversal rejection; bounded gallery. |
| ExpeditionLoop, VesselDeath | Rest confirmation/in-place zero-energy position, deadline + Journal, daily encounter persistence, atomic backup/corruption, version 1; cross-zone death/day-start/deep copy, legacy no-start, pending/active capture interruption. |
| ExpeditionEconomy Edit/Play | Stack capacity/consumption, exact upgrade costs/stats, save failure transaction, Rest failure unlock, repeatable farm/one-time collection, version 4/5 migration/rejection, current/day-start/Journal purchased-target migration, real movement/energy axes. |
| ZoneExitFlow/Map, RoundThreeNavigation, DebugStart | Every authored route radius/depth, arrival anti-loop, all gate pairs, legacy coordinate versions 0/1/2, isolated debug menu/Continue, prior-zone progress and required modules. |
| RockBreaker | Controlled I/O rollback; committed exactly once; 10-second blocking, terrain reveal, shake/no-shake, pause/interrupted Continue and no replay. |
| CaptureMinigame, DirectCabinInteraction | Actual resolution, duplicate/cancel/disable/rebind, capacity revalidation, multiple seeds/FPS, steering/collision, cooldown and delayed direct interaction. |
| Tutorial / TutorialSave / MainMenuTutorialReset | Real onboarding, zero-charge recovery, panel gates, repeated listeners, legacy knowledge signals, New Game/reset boundaries, missing/duplicate/unknown knowledge. |
| Dialogue, EndingPresentation, WorldMap, Cargo, MissionLogScroll, scene/UI/feedback fixtures | Explicit/cancelled choices, ending art/menu, overview/zone navigation, no hover reveal, current selection/cargo, modal routing, scroll preservation, errors/prompts and scene binding. |

## Genuine gaps selected for this pass

- SEND failure before or during primary publication: in-memory objective/receipts/photo, cumulative reward counter, archive metadata, lab feedback and retry/restart must agree.
- SEND reward fan-out must not emit success-state notifications before persistence (synthetic cloned mission rules, explicitly not an authored progression integration test).
- Ancillary sidecar failure after authoritative SEND commit must not erase/duplicate committed progress.
- Re-entrant SEND while paused/transitioning/dying; UI event callbacks must not bypass state guards.
- Paused Rest/Journal/upgrade/ending and camera/radar/capture/navigation authority calls must not mutate or consume charges even when invoked directly with stale callbacks or a supplied timestep.
- Historical checkmarks after rollback across zones, including repeated camera → SEND → capture → Rest → Journal → Continue cycles and failed Journal publication.
- All nine MissionProgressState lists: copy isolation, idempotent deduplication, unknown ID retention and missing legacy lists.

Existing E2E fixtures sometimes provision prerequisites directly to isolate later-zone tests. The new repeated-history test explicitly uses `ReachZone(2, true)` and actual devices for prerequisites and L3 objectives. It positions the vessel through a test driver and wins the actual capture minigame through a deterministic steering driver; this is integration evidence, not human navigation/difficulty validation.

Matrix, repro/fixes and final run evidence are recorded separately. Static inspection is not counted as PASS; OS process-kill/power-loss and human play quality remain explicit manual cases.
