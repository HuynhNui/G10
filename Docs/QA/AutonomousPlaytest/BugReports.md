# Findings

No confirmed new P0/P1 progression defect as of the recorded milestones. Do not infer that the whole game is completable.

## AP-001 — suspected capture softlock, disproved

Mode A reproduced capture rejections after camera/SEND, sailing, Rest and Continue. Evidence: run-20261015-A01 screenshots 035, 039, 045, 056, 060 and mode-b-diagnosis.json.

Mode B verified ordinary validation: arrival radius20, first rejection distance62.34; later distance17.66 but depth324.8353/306.0355<330; after Rest distance21.6558>20 at depth337.3629. Invalid requests left five attempts, zero cargo. Correcting the approach through helm input opened the real minigame. A W/S input controller then earned five hits and cargo1. Root cause of the observed block was insufficient precision and misinterpretation by the agent. No production fix warranted.

UX observation: `TARGET TOO SHALLOW` actually means the submarine is shallower than the contact. The agent initially ascended, making the condition worse. The hint displays a target depth but no valid band/radius; terse failure feedback increases trial and error. This is not evidence of an impossible game state or a confirmed P1. The third Zone01 photo is also still required after collecting the item; generic tutorial missing-requirements text is not a claim that the successful SEND was lost.

## AP-H01 — automation/editor failures

- First Windows snapshot captured an occluding app until Unity activation. No input was sent to that app; do not archive unrelated desktop captures as game evidence.
- Editor bridge welcome overlay blocked the first input; closing it and observing again resolved the miss.
- New Editor source was not loaded because compilation/reload was stalled. Source compilation requests and normal reload did not resolve it; restarting saved Unity with its recompile command did. Fresh compilation subsequently reported zero errors.
- A probe selected Play after an unsuccessful transport type lookup; stopped immediately, before New Game or gameplay mutations. Later startup correctly verifies isolation before input. This failed dispatch is NOT RUN as gameplay.
- Initial navigation script used Action for an Editor callback; dynamic compilation rejected it. Changed to EditorApplication.CallbackFunction before execution. This is a harness compile failure, not a game failure.

Failures remain in the ledger; retries are not counted as independent successes or deleted from history.
