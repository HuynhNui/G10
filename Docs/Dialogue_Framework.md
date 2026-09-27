# Dialogue framework

The dialogue UI is installed in `GameplayCore` and remains hidden until explicitly invoked. There is no story content, automatic conversation, or demo dialogue in the game.

Create content later using **Assets > Create > G10 > Dialogue > Sequence**. Each line has a speaker name, text, and `Story`, `Character`, or `Guide` kind; the kind selects the supplied watercolor tab. Vietnamese text uses the shared dynamic Alegreya Sans SC TMP font. Names and dialogue stay editable text.

Gameplay code can keep a reference to the `DialogueController` on the existing `UIManager` and call:

```csharp
bool opened = dialogue.TryBegin(sequence, reason =>
{
    // Completed, Skipped, and Cancelled are distinct outcomes.
});
```

`Play(DialogueSequence)` is the equivalent Inspector/UnityEvent entry point. The caller should only advance a story condition on the intended result. An empty sequence, another active modal, or a locked panel returns `false` without changing the UI. Runtime-generated content can use `TryBegin(IReadOnlyList<DialogueLine>, callback)`.

- **Next** first reveals the complete current line, then advances on the next activation. The last line completes the session.
- **Auto** toggles automatic advancement after typewriter completion and a reading delay.
- **Log** reveals the current line and opens a scrollable transcript of reached lines. It pauses typing/auto until closed. Unseen future lines are never logged.
- **Skip** ends with `Skipped`. Escape closes the log first, then ends the session with `Cancelled`.
- The existing EventSystem handles keyboard/gamepad navigation and Submit; pointer buttons use subtle hover/pressed scaling. Long text and history can scroll vertically.
- Modal ownership blocks other game panels while dialogue is open. Ending or externally disabling the view releases ownership and restores the previous panel. History resets on the next conversation.

The supplied `Art/UI/Dialouge` README is asset/layout guidance. Source PNGs, GUIDs, and authored sprite slices are preserved; the installer uses alpha-bound UVs to remove transparent margins without changing the images.

To install on a fresh GameplayCore scene, use **G10 > Dialogue > Install Dialogue Framework** with GameplayCore loaded, then save. Automation can call `G10.Prototype.Editor.DialogueUIEditor.InstallBatch()`. Repeated installs validate and reuse the configured overlay. `EnsureFont()` creates/reuses `Assets/_Project/Art/UI/Fonts/AlegreyaSansSC-Regular SDF.asset` for other UI installers.

`DialoguePlayModeTests` covers empty/busy requests, typewriter/advance, tab selection, history, auto/log behavior, completion results, repeated opens, modal restoration, and cancellation on disable. Test strings only exist in the test assembly.
