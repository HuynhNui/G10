# Tool selection — 2026-10-15

Budget: $0 additional spending. Reuse the already available Codex session, local Unity, AnkleBreaker 2.39.7 and Input System 1.19.0. No paid inference API, trial, package installation, or cloud job.

| Approach | Practical assessment |
|---|---|
| Windows Computer Use (`@oai/sky`) | Available and successfully targets Unity after explicit activation. Useful for actual OS focus and UI clicks. Its supported API has key presses but no bounded key-down/key-up operation; frame-sensitive capture and sustained sailing need another transport. Initial occluded capture showed an unrelated foreground app: activation and inspection are mandatory. |
| Existing Unity MCP | Connected to exactly one Editor, D:/G10, Unity 6000.4.2f1, port 7890. Reliable local captures and runtime diagnosis. Calling gameplay authorities alone would repeat the weakness of existing tests. |
| Unity Input System | Already installed; queued mouse/keyboard device events traverse ordinary UI raycasts and gameplay input. Suitable for bounded holds without teleportation or mission callbacks. Editor-only transport can use existing save/archive overrides. |
| Python/PyAutoGUI or other OS macro frameworks | Free, but an unnecessary extra dependency here. Global foreground input risks unrelated apps, fixed coordinates are brittle, and screenshots alone do not provide a progression oracle. |
| InputTestFixture / existing NUnit tests | Useful state regressions; fixture resets input runtime and many existing tests position the vessel or provision prerequisites. These do not establish black-box completion. |
| New open-source automation/learning framework | Installation/training/integration cost offers no demonstrated advantage over the installed input/runtime tools for this small point-and-click game. No framework downloaded. |

Selected: hybrid screenshot-led player interaction, bounded Input System transport, Windows Computer Use for OS lifecycle, and MCP state inspection only in Mode B. The agent interprets captures during a live Codex session; the local transport is reusable without a paid model, but is not a standalone general visual AI or unattended learned gameplay policy.

The transport is Editor-only, opt-in and writes inside a unique evidence directory. It cannot complete objectives, reposition the vessel, alter saves, or expose hidden puzzle state. Preserve existing scene setup and dirty files. Before New Game verify both overrides are selected. Do not launch a normal standalone player into the user's profile.

Sources: installed Computer Use skill/API; installed Unity Input System package; [Unity input events](https://github.com/Unity-Technologies/InputSystem/blob/develop/Packages/com.unity.inputsystem/Documentation~/input-events.md), [Input testing](https://docs.unity.cn/Packages/com.unity.inputsystem%401.5/manual/Testing.html), [PyAutoGUI keyboard API](https://pyautogui.readthedocs.io/en/latest/quickstart.html). The installed package is authoritative for this project's version.
