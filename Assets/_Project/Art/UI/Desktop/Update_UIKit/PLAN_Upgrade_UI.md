# PLAN — Submarine Upgrade UI (Unity)

## Goal

Implement the Submarine Upgrade application using the supplied watercolor UI assets.

This implementation must be **scene-configurable** and **data-driven from prefab instances**.

The layout/spec below is the source of truth.  
The mockup image is visual reference only and contains some elements that are NOT required.

---

## Important differences from the mockup

Do NOT blindly copy every element shown in the reference image.

Required behavior/layout:

- Left side contains two fixed sections:
  - `Ship Systems`
  - `Modules`
- Neither left section needs its own ScrollRect.
- Designers can add/remove upgrade entries by placing prefab instances under the appropriate section in the scene.
- No mandatory large submarine preview or decorative before/after image.
- Right side is a single reusable detail inspector for the selected upgrade.
- `Required Materials` is the only area that needs its own vertical ScrollRect.
- Comparison rows are configurable per upgrade.
- Material requirements are configurable per upgrade.
- Main action button text:
  - Ship System -> `UPGRADE`
  - Module -> `ADD`

Do NOT hard-code specific upgrades such as Depth System, Radar, Floodlight, etc.

---

# 1. Scene hierarchy

Recommended hierarchy:

```text
SubmarineUpgradeWindow
├── WindowFrame
├── LeftPanel
│   ├── ShipSystemsHeader
│   ├── ShipSystemsRoot
│   │   ├── UpgradeEntryPrefab...
│   │   └── ...
│   ├── ModulesHeader
│   └── ModulesRoot
│       ├── UpgradeEntryPrefab...
│       └── ...
│
└── DetailPanel
    ├── SelectedName
    ├── SelectedLevel
    ├── DescriptionText
    │
    ├── ComparisonRoot
    │   └── ComparisonRowPrefab...
    │
    ├── RequiredMaterials
    │   └── ScrollView
    │       ├── Viewport
    │       │   └── Content
    │       │       └── MaterialRequirementPrefab...
    │       └── VerticalScrollbar
    │
    └── ActionButton
        └── ActionText
```

Use the existing central-computer app/window system if available.

Do not create a second desktop/window framework.

---

# 2. Upgrade Entry Prefab

Create ONE reusable prefab:

`UpgradeEntryPrefab`

Visible UI:

```text
UpgradeEntry
├── Button
├── Background
├── Icon
├── NameText
└── LevelText
```

Attach a component such as:

`UpgradeEntryConfig`

Serialized Inspector fields:

```text
Category
Icon
Display Name
Level
Description

Comparison Rows[]
Material Requirements[]

Upgrade ID / existing gameplay reference
OnApply / existing upgrade callback
```

Category enum:

```text
ShipSystem
Module
```

The prefab itself is the editable scene configuration.

To add another upgrade:

1. duplicate/add `UpgradeEntryPrefab`
2. place under `ShipSystemsRoot` or `ModulesRoot`
3. configure its fields in Inspector

Do not maintain a second hard-coded list in code.

Scene hierarchy order determines display order.

---

# 3. UpgradeEntryConfig data

Suggested serialized structures:

```csharp
enum UpgradeCategory
{
    ShipSystem,
    Module
}

[Serializable]
class ComparisonData
{
    string statName;
    string currentValue;
    string nextValue;
}

[Serializable]
class MaterialRequirement
{
    MaterialDefinition material;
    int requiredAmount;
}
```

Each upgrade entry can define any number of comparison rows.

Examples:

```text
Max Depth     -80m       -> -160m
Energy        100        -> 125
Radar Uses    2          -> 3
Status        Damaged    -> Repaired
```

The UI must not know which stats exist in advance.

It simply renders whatever rows are configured.

---

# 4. Material definition

Materials should also be configurable.

Use the project's existing item/material definition system if one exists.

Otherwise use a lightweight definition component/prefab such as:

`MaterialDefinition`

Fields:

```text
Material ID
Icon
Display Name
```

Example material prefabs:

```text
Material_ScrapMetal
Material_DeepCrystal
Material_EnergyCell
Material_AlloyPlate
Material_CorePart
Material_CircuitChip
```

An upgrade entry stores:

```text
MaterialDefinition reference
Required amount
```

Do NOT duplicate the material name/icon manually in every upgrade.

---

# 5. Material Requirement UI Prefab

Create ONE UI prefab:

`MaterialRequirementPrefab`

Hierarchy:

```text
MaterialRequirement
├── Background
├── Icon
├── NameText
└── QuantityText
```

When an upgrade is selected:

1. clear existing material rows
2. read selected upgrade's `MaterialRequirements`
3. instantiate one `MaterialRequirementPrefab` for each requirement
4. populate icon/name/amount

Preferred quantity display if inventory data exists:

```text
owned / required
```

Example:

```text
5 / 5
2 / 3
```

Otherwise display:

```text
x5
x3
```

Use green/normal state when sufficient and pink/red state when insufficient.

Reuse the existing inventory/material count system if available.

---

# 6. Required Materials ScrollRect

ONLY the Required Materials section should scroll.

Use:

- `ScrollRect`
- vertical movement only
- `GridLayoutGroup` with 2 columns OR a `VerticalLayoutGroup`
- `ContentSizeFitter`
- vertical scrollbar

Recommended behavior:

- 1–4 materials: no visible scrolling needed
- additional materials extend Content vertically
- scrollbar auto-hides when Content fits inside the Viewport
- mouse wheel works while pointer is over the materials area
- scrollbar thumb can be dragged

Do NOT scroll the Ship Systems or Modules sections.

---

# 7. Detail panel

When an upgrade card is selected, update the right panel.

Populate:

```text
Name
Level
Description
Comparison Rows
Required Materials
Action Button
```

No special layout should exist for individual upgrades.

The same inspector renders every upgrade.

---

# 8. Comparison section

Create ONE reusable:

`ComparisonRowPrefab`

Hierarchy:

```text
ComparisonRow
├── StatName
├── CurrentValue
├── Arrow
└── NextValue
```

When selecting an upgrade:

1. clear old rows
2. instantiate rows from the selected upgrade's comparison list
3. populate `Current -> Next`

The number and names of stats are entirely driven by the selected prefab configuration.

Do not hard-code:

- Max Depth
- Energy
- Radar
- Speed
- Status

---

# 9. Main action button

Keep one large fixed button at the bottom of DetailPanel.

Button label is determined by category:

```text
ShipSystem -> UPGRADE
Module     -> ADD
```

Do NOT use `REPAIR` as the generic Module button text.

If materials are insufficient:

- disable the button
- keep Required Materials visible
- visually mark missing materials

If the selected upgrade cannot currently be applied for another gameplay reason,
disable the button and use the existing gameplay rule/state.

---

# 10. Applying an upgrade

UI must not duplicate gameplay logic.

Preferred flow:

```text
Select Upgrade
    ↓
UpgradeUIController renders config
    ↓
Press UPGRADE / ADD
    ↓
Check inventory requirements
    ↓
Call existing upgrade/module gameplay system
    ↓
Consume materials through existing inventory system
    ↓
Refresh entry + detail panel
```

If an upgrade system already exists:
reuse it.

If no generic system exists yet:
each `UpgradeEntryConfig` may expose a serialized `UnityEvent onApply`
or an `upgradeId` consumed by the project's UpgradeManager.

Do not create separate logic branches for every UI card.

---

# 11. Controller responsibilities

Create/extend ONE controller, e.g.:

`SubmarineUpgradeUIController`

Responsibilities only:

- discover/register upgrade entry instances under ShipSystemsRoot and ModulesRoot
- handle entry selection
- display selected config
- spawn comparison rows
- spawn material requirement rows
- update action button text/state
- forward apply action to gameplay logic
- refresh UI after upgrade

Do NOT put actual stat/gameplay implementation inside this UI controller.

---

# 12. Layout

Left side:

```text
SHIP SYSTEMS
[card] [card] [card] [card]
[card] [card] [card]

MODULES
[card] [card] [card]
```

Use `GridLayoutGroup`.

The exact number of entries is configured in the scene.

Right side:

```text
Selected Name / Level
Description

CURRENT -> NEXT
(dynamic comparison rows)

REQUIRED MATERIALS
(dynamic scroll area)

[ LARGE ACTION BUTTON ]
```

Keep the action button outside the material ScrollRect so it always remains visible.

---

# 13. Supplied assets

Suggested usage:

```text
Sprites/Window/
    Upgrade_Window_Frame.png

Sprites/Cards/
    UpgradeCard_Default.png
    UpgradeCard_Hover.png
    UpgradeCard_Selected.png
    UpgradeCard_Disabled.png

Sprites/Panels/
    Header_Section.png
    Panel_Comparison.png
    Panel_RequiredMaterials.png
    Panel_Result.png

Sprites/Controls/
    ActionButton_Normal.png
    ActionButton_Disabled.png
    Scrollbar_Track.png
    Scrollbar_Thumb.png
    Scrollbar_ArrowUp.png
    Scrollbar_ArrowDown.png
    Arrow_Right.png

Sprites/Icons/ShipSystems/
Sprites/Icons/Modules/
Sprites/Icons/Materials/
```

All text should use TextMeshPro.

Do not bake names/levels/quantities into images.

---

# 14. Unity import

For UI PNGs:

```text
Texture Type: Sprite (2D and UI)
Alpha Is Transparency: ON
Mesh Type: Full Rect
```

For scalable panels/buttons/cards:
configure borders in Sprite Editor and use:

```text
Image Type: Sliced
```

Do not stretch icon sprites non-uniformly.

---

# 15. Scope control / token saving

Do NOT:

- redesign existing inventory
- redesign the entire central computer
- create new save systems
- create automated tests
- create test scenes
- implement animations unless already trivial
- refactor unrelated gameplay scripts

Minimal verification only:

1. project compiles
2. cards can be selected
3. detail changes to selected card
4. comparison rows are dynamic
5. material rows are dynamic and scroll
6. button reads UPGRADE for ShipSystem
7. button reads ADD for Module
8. existing upgrade/inventory callback still works

Stop after these pass.
