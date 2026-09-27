DIALOGUE UI ASSETS — UNITY IMPLEMENTATION NOTES

FILES
- DialoguePanel_Base.png
  Clean dialogue background only. No avatar, no name, no controls.
- Tab_Story.png
  Story/day tab. Planet icon is baked in; render DAY / chapter text with TextMeshPro.
- Tab_Character.png
  Character speaker tab. Sprout icon is baked in; render character name dynamically.
- Tab_Guide.png
  AI guide tab. Gear icon is baked in; render GUIDE / AI name dynamically.
- Button_Log.png
- Button_Auto.png
- Button_Skip.png
- Button_Next.png

RECOMMENDED UNITY SETUP
1. Import all PNGs as Sprite (2D and UI).
2. Texture Type: Sprite (2D and UI)
3. Sprite Mode: Single
4. Mesh Type: Full Rect
5. Filter Mode: Bilinear
6. Compression: None or High Quality (the watercolor texture loses detail with aggressive compression).
7. Keep Alpha Is Transparency enabled.

DIALOGUE HIERARCHY
DialogueRoot
├── PanelBackground        -> DialoguePanel_Base.png
├── SpeakerTab
│   ├── TabImage           -> one of Tab_Story / Tab_Character / Tab_Guide
│   └── SpeakerText        -> TextMeshProUGUI
├── DialogueText           -> TextMeshProUGUI
└── Controls
    ├── LogButton          -> Button_Log.png
    ├── AutoButton         -> Button_Auto.png
    ├── SkipButton         -> Button_Skip.png
    └── NextButton         -> Button_Next.png

LAYOUT
- Anchor DialogueRoot: bottom-center.
- Keep the panel around a 3:1 aspect ratio.
- Put DialogueText inside a safe region that avoids the painted foliage:
  left/right padding ~12–15% of panel width and top padding ~20%.
- Controls: bottom-right.
- Speaker tab: overlap the top-left border by roughly half of the tab height.

TEXT
- Do NOT bake dialogue text or speaker names into the image.
- Use TextMeshPro so localization and dynamic dialogue remain easy.
- Use the game's existing handwritten / sketch-like Vietnamese-capable font.

BUTTON STATES
You do not need extra painted assets initially:
- Normal: white tint.
- Hover: very slight brightness increase + scale 1.03.
- Pressed: scale 0.96 and slightly darker tint.
This keeps the watercolor asset visually consistent instead of swapping to a different-looking sprite.

9-SLICE
The panel is safest at a fixed aspect ratio. If you need horizontal resizing, use a conservative 9-slice and keep the decorated ends inside the fixed side regions. Because the foliage is painted into the corners, do not heavily stretch vertically.

VARIANTS
Use the SAME DialoguePanel_Base for all dialogue types.
Only swap the top tab:
- Story / narration -> Tab_Story
- Character dialogue -> Tab_Character
- AI / tutorial -> Tab_Guide

This prevents the UI system from turning into three separate prefabs and keeps dialogue logic much simpler.
