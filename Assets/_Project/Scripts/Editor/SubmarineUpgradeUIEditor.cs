using System;
using System.Linq;
using G10.Prototype.Computer;
using G10.Prototype.Navigation;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace G10.Prototype.Editor
{
    public static class SubmarineUpgradeUIEditor
    {
        private const string Art = "Assets/_Project/Art/UI/Desktop/Update_UIKit/";
        private const string Sprites = Art + "Sprites/";
        private const string PrefabFolder = "Assets/_Project/Prefabs/UI/Computer/Upgrade";
        private const string DataFolder = "Assets/_Project/Data/Upgrade";
        private static readonly Color Ink = new(0.08f, 0.22f, 0.48f);

        private sealed class EntrySeed
        {
            public string id, name, description, icon;
            public UpgradeCategory category;
            public ShipUpgrade? shipUpgrade;
            public float amount;
            public string[][] comparisons;
            public string[] materials;
            public int[] materialAmounts;
        }

        [MenuItem("G10/Computer/Install Submarine Upgrade App")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            ConfigureTextures();
            EnsureFolder(PrefabFolder);
            EnsureFolder(DataFolder);

            var font = TMP_Settings.defaultFontAsset;
            if (font == null) throw new InvalidOperationException("TMP Essentials must be imported before installing the Upgrade app.");

            UpgradeMaterialDefinition[] materials = CreateMaterialDefinitions();
            UpgradeEntryConfig entryPrefab = CreateEntryPrefab(font);
            UpgradeComparisonRowView comparisonPrefab = CreateComparisonPrefab(font);
            UpgradeMaterialRequirementView materialPrefab = CreateMaterialPrefab(font);

            ComputerScreenController screen = UnityEngine.Object.FindAnyObjectByType<ComputerScreenController>(FindObjectsInactive.Include);
            if (screen == null) throw new InvalidOperationException("Load Zone01 with its existing ComputerScreen before installing the Upgrade app.");
            if (screen.transform.Find("UpgradePanel") != null)
            {
                CutRegularUpgradeScope(screen);
                AssignDesktopIcon(screen);
                Selection.activeGameObject = screen.transform.Find("UpgradePanel").gameObject;
                Debug.Log("Submarine Upgrade app is already installed.", screen);
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(screen.gameObject, "Install Submarine Upgrade app");
            CreateDesktopShortcut(screen);
            RectTransform panel = BuildUpgradePanel(screen, font, entryPrefab, comparisonPrefab, materialPrefab, materials);
            RegisterApp(screen, panel.gameObject);
            CutRegularUpgradeScope(screen);
            AssignDesktopIcon(screen);
            panel.gameObject.SetActive(false);

            EditorUtility.SetDirty(screen);
            EditorSceneManager.MarkSceneDirty(screen.gameObject.scene);
            EditorSceneManager.SaveScene(screen.gameObject.scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = panel.gameObject;
            Debug.Log("Installed the data-driven Submarine Upgrade app, reusable prefabs, and desktop shortcut.", panel);
        }

        [MenuItem("G10/Computer/Apply Three Branch Upgrade Scope")]
        public static void ApplyThreeBranchScope()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            var screen = UnityEngine.Object.FindAnyObjectByType<ComputerScreenController>(FindObjectsInactive.Include);
            if (screen == null) throw new InvalidOperationException("Load the existing Zone01 cabin first.");
            CutRegularUpgradeScope(screen);
            AssetDatabase.SaveAssets();
        }

        // Targeted data/action migration. Existing containers, prefabs, artwork and layout are retained.
        public static void CutRegularUpgradeScope(ComputerScreenController screen)
        {
            var controller = screen.GetComponentInChildren<SubmarineUpgradeUIController>(true);
            if (controller == null) throw new InvalidOperationException("Existing Upgrade app is required.");
            Undo.RegisterFullObjectHierarchyUndo(controller.gameObject, "Scope regular upgrades and separate expedition module");
            var ui = new SerializedObject(controller);
            var shipRoot = (Transform)ui.FindProperty("shipSystemsRoot").objectReferenceValue;
            var moduleRoot = (Transform)ui.FindProperty("modulesRoot").objectReferenceValue;
            var existing = controller.GetComponentsInChildren<UpgradeEntryConfig>(true);
            var materials = CreateMaterialDefinitions();
            var keep = new System.Collections.Generic.HashSet<UpgradeEntryConfig>();
            foreach (var seed in Seeds())
            {
                var entry = existing.FirstOrDefault(value => value.UpgradeId == seed.id);
                if (entry == null && seed.id == "ExpeditionModule") entry = existing.FirstOrDefault(value => value.UpgradeId == "DepthSystem");
                if (entry == null)
                    entry = ((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/UpgradeEntry.prefab"),
                        seed.category == UpgradeCategory.ShipSystem ? shipRoot : moduleRoot)).GetComponent<UpgradeEntryConfig>();
                entry.transform.SetParent(seed.category == UpgradeCategory.ShipSystem ? shipRoot : moduleRoot, false);
                entry.name = seed.id + "Entry";
                ConfigureEntry(entry, seed, materials);
                entry.transform.SetAsLastSibling();
                keep.Add(entry);
            }
            foreach (var entry in existing) if (!keep.Contains(entry)) Undo.DestroyObjectImmediate(entry.gameObject);
            foreach (var action in shipRoot.GetComponentsInChildren<StoryHullUpgradeAction>(true)) Undo.DestroyObjectImmediate(action);
            var cabin = screen.GetComponentInParent<G10.Prototype.UI.CabinStationView>(true);
            ui.FindProperty("materialInventorySource").objectReferenceValue = cabin.GetComponent<CreatureInventory>();
            ui.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(screen.gameObject.scene);
        }

        [MenuItem("G10/Computer/Fix Submarine Upgrade Visual Layout")]
        public static void FixVisualLayout()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            var font = TMP_Settings.defaultFontAsset;
            if (font == null) throw new InvalidOperationException("TMP Essentials must be imported before fixing the Upgrade layout.");

            ConfigureEntryPrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/UpgradeEntry.prefab"), font);
            ConfigureComparisonPrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/ComparisonRow.prefab"), font);
            ConfigureMaterialPrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/MaterialRequirement.prefab"), font);

            ComputerScreenController screen = UnityEngine.Object.FindAnyObjectByType<ComputerScreenController>(FindObjectsInactive.Include);
            Transform panelTransform = screen != null ? screen.transform.Find("UpgradePanel") : null;
            if (panelTransform == null) throw new InvalidOperationException("Load Zone01 with its installed UpgradePanel before fixing the visual layout.");

            Undo.RegisterFullObjectHierarchyUndo(panelTransform.gameObject, "Fix Submarine Upgrade visual layout");
            DestroyNamed(panelTransform, "UpgradeFrameArt");
            DestroyNamed(panelTransform, "UpgradeHeading");

            RectTransform leftPanel = Rect(panelTransform, "LeftPanel");
            RectTransform detailPanel = Rect(panelTransform, "DetailPanel");
            ConfigurePanel(leftPanel, "Panels/Panel_Large.png");
            ConfigurePanel(detailPanel, "Panels/Panel_Large.png");
            Place(leftPanel, 8, 8, 550, 664);
            Place(detailPanel, 568, 8, 664, 664);

            ConfigureHeader(Rect(leftPanel, "ShipSystemsHeader"), 18, 14, 514, 48, font);
            RectTransform shipRoot = Rect(leftPanel, "ShipSystemsRoot");
            Place(shipRoot, 18, 72, 514, 338);
            ConfigureGrid(shipRoot, new Vector2(121, 164), new Vector2(10, 10), 4);
            ConfigureHeader(Rect(leftPanel, "ModulesHeader"), 18, 420, 514, 40, font);
            RectTransform modulesRoot = Rect(leftPanel, "ModulesRoot");
            Place(modulesRoot, 18, 474, 514, 172);
            ConfigureGrid(modulesRoot, new Vector2(164, 172), new Vector2(11, 0), 3);
            ConfigureEntryInstances(shipRoot, false);
            ConfigureEntryInstances(modulesRoot, true);

            RectTransform selectedFrame = Rect(detailPanel, "SelectedIconFrame");
            Place(selectedFrame, 18, 14, 148, 148);
            Place(Rect(selectedFrame, "SelectedIcon"), 24, 24, 100, 100);
            ConfigureText(Rect(detailPanel, "SelectedName"), font, 34, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            Place(Rect(detailPanel, "SelectedName"), 182, 14, 462, 46);
            ConfigureText(Rect(detailPanel, "SelectedLevel"), font, 22, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            Place(Rect(detailPanel, "SelectedLevel"), 182, 62, 200, 34);
            ConfigureText(Rect(detailPanel, "DescriptionText"), font, 21, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            Place(Rect(detailPanel, "DescriptionText"), 182, 101, 462, 62);

            RectTransform comparisonPanel = Rect(detailPanel, "ComparisonPanel");
            ConfigurePanel(comparisonPanel, "Panels/Panel_Large.png");
            Place(comparisonPanel, 18, 174, 628, 134);
            RectTransform comparisonBand = EnsureImage(comparisonPanel, "ComparisonHeaderBand", "Panels/Header_Section.png");
            Place(comparisonBand, 12, 8, 604, 32);
            comparisonBand.SetAsFirstSibling();
            ConfigureText(Rect(comparisonPanel, "ComparisonHeader"), font, 19, FontStyles.Bold, TextAlignmentOptions.Center);
            Place(Rect(comparisonPanel, "ComparisonHeader"), 20, 8, 588, 32);
            Place(Rect(comparisonPanel, "ComparisonRoot"), 20, 47, 588, 76);

            RectTransform requiredPanel = Rect(detailPanel, "RequiredMaterials");
            ConfigurePanel(requiredPanel, "Panels/Panel_Large.png");
            Place(requiredPanel, 18, 320, 628, 232);
            RectTransform materialsBand = EnsureImage(requiredPanel, "MaterialsHeaderBand", "Panels/Header_Section.png");
            Place(materialsBand, 12, 8, 604, 34);
            materialsBand.SetAsFirstSibling();
            ConfigureText(Rect(requiredPanel, "MaterialsHeader"), font, 20, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            Place(Rect(requiredPanel, "MaterialsHeader"), 28, 8, 520, 34);
            ConfigureMaterialScroll(requiredPanel);

            Place(Rect(detailPanel, "ActionButton"), 18, 566, 628, 82);
            ConfigureText(Rect(Rect(detailPanel, "ActionButton"), "ActionText"), font, 30, FontStyles.Bold, TextAlignmentOptions.Center);
            Place(Rect(Rect(detailPanel, "ActionButton"), "ActionText"), 12, 8, 604, 66);
            AssignDesktopIcon(screen);

            EditorUtility.SetDirty(panelTransform.gameObject);
            EditorSceneManager.MarkSceneDirty(panelTransform.gameObject.scene);
            EditorSceneManager.SaveScene(panelTransform.gameObject.scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = panelTransform.gameObject;
            Debug.Log("Fixed the Submarine Upgrade visual layout without changing upgrade data or callbacks.", panelTransform);
        }

        private static RectTransform BuildUpgradePanel(ComputerScreenController screen, TMP_FontAsset font,
            UpgradeEntryConfig entryPrefab, UpgradeComparisonRowView comparisonPrefab,
            UpgradeMaterialRequirementView materialPrefab, UpgradeMaterialDefinition[] materials)
        {
            RectTransform panel = Box("UpgradePanel", screen.transform, 0, 220, 1920, 760);
            Text("AppTitle", panel, "SUBMARINE UPGRADE", 90, 0, 1400, 70, 36, font);
            var back = Button("Back", panel, "DESKTOP", 1510, 0, 300, 70, font, null, null, null);
            UnityEventTools.AddPersistentListener(back.onClick, screen.ShowDesktop);

            RectTransform leftPanel = Image("LeftPanel", panel, Sprite("Panels/Panel_Large.png"), 8, 8, 550, 664, false).rectTransform;
            leftPanel.GetComponent<UnityEngine.UI.Image>().type = UnityEngine.UI.Image.Type.Sliced;
            RectTransform detailPanel = Image("DetailPanel", panel, Sprite("Panels/Panel_Large.png"), 568, 8, 664, 664, false).rectTransform;
            detailPanel.GetComponent<UnityEngine.UI.Image>().type = UnityEngine.UI.Image.Type.Sliced;

            Header(leftPanel, "ShipSystemsHeader", "SHIP SYSTEMS", 18, 14, 514, 48, font);
            RectTransform shipRoot = Box("ShipSystemsRoot", leftPanel, 18, 72, 514, 310);
            Grid(shipRoot, new Vector2(121, 150), new Vector2(10, 10), 4);
            Header(leftPanel, "ModulesHeader", "MODULES", 18, 394, 514, 48, font);
            RectTransform modulesRoot = Box("ModulesRoot", leftPanel, 18, 454, 514, 184);
            Grid(modulesRoot, new Vector2(164, 184), new Vector2(11, 0), 3);

            UnityEngine.UI.Image selectedFrame = Image("SelectedIconFrame", detailPanel, Sprite("Panels/Icon_Bubble_Frame.png"), 18, 14, 148, 148, false);
            selectedFrame.preserveAspect = true;
            UnityEngine.UI.Image selectedIcon = Image("SelectedIcon", selectedFrame.transform, null, 24, 24, 100, 100, false);
            selectedIcon.preserveAspect = true;
            TextMeshProUGUI selectedName = Text("SelectedName", detailPanel, "SELECT AN UPGRADE", 182, 14, 462, 46, 34, font);
            selectedName.fontStyle = FontStyles.Bold;
            TextMeshProUGUI selectedLevel = Text("SelectedLevel", detailPanel, "", 182, 62, 200, 34, 22, font);
            selectedLevel.fontStyle = FontStyles.Bold;
            TextMeshProUGUI description = Text("DescriptionText", detailPanel, "", 182, 101, 462, 62, 21, font);
            description.alignment = TextAlignmentOptions.TopLeft;
            description.textWrappingMode = TextWrappingModes.Normal;

            RectTransform comparisonPanel = Image("ComparisonPanel", detailPanel, Sprite("Panels/Panel_Large.png"), 18, 174, 628, 134, false).rectTransform;
            comparisonPanel.GetComponent<UnityEngine.UI.Image>().type = UnityEngine.UI.Image.Type.Sliced;
            UnityEngine.UI.Image comparisonBand = Image("ComparisonHeaderBand", comparisonPanel, Sprite("Panels/Header_Section.png"), 12, 8, 604, 32, false);
            comparisonBand.type = UnityEngine.UI.Image.Type.Simple;
            TextMeshProUGUI comparisonHeader = Text("ComparisonHeader", comparisonPanel, "CURRENT                  →                  NEXT", 20, 8, 588, 32, 19, font);
            comparisonHeader.fontStyle = FontStyles.Bold;
            comparisonHeader.alignment = TextAlignmentOptions.Center;
            RectTransform comparisonRoot = Box("ComparisonRoot", comparisonPanel, 20, 47, 588, 76);
            var comparisonLayout = comparisonRoot.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            comparisonLayout.spacing = 2;
            comparisonLayout.childControlHeight = true;
            comparisonLayout.childControlWidth = true;
            comparisonLayout.childForceExpandHeight = false;
            comparisonLayout.childForceExpandWidth = true;

            RectTransform requiredPanel = Image("RequiredMaterials", detailPanel, Sprite("Panels/Panel_Large.png"), 18, 320, 628, 232, false).rectTransform;
            requiredPanel.GetComponent<UnityEngine.UI.Image>().type = UnityEngine.UI.Image.Type.Sliced;
            UnityEngine.UI.Image materialsBand = Image("MaterialsHeaderBand", requiredPanel, Sprite("Panels/Header_Section.png"), 12, 8, 604, 34, false);
            materialsBand.type = UnityEngine.UI.Image.Type.Simple;
            Text("MaterialsHeader", requiredPanel, "REQUIRED MATERIALS", 28, 8, 520, 34, 20, font).fontStyle = FontStyles.Bold;
            BuildMaterialScroll(requiredPanel, out RectTransform materialsContent, out UnityEngine.UI.Scrollbar materialsScrollbar);

            UnityEngine.UI.Button actionButton = Button("ActionButton", detailPanel, "UPGRADE", 18, 566, 628, 82, font,
                Sprite("Controls/ActionButton_Normal.png"), Sprite("Controls/ActionButton_Selected.png"), Sprite("Controls/ActionButton_Disabled.png"));
            TextMeshProUGUI actionText = actionButton.GetComponentInChildren<TextMeshProUGUI>(true);
            actionText.fontSize = 30;
            actionText.fontStyle = FontStyles.Bold;

            var controller = panel.gameObject.AddComponent<SubmarineUpgradeUIController>();
            var controllerSo = new SerializedObject(controller);
            SetReference(controllerSo, "shipSystemsRoot", shipRoot);
            SetReference(controllerSo, "modulesRoot", modulesRoot);
            SetReference(controllerSo, "selectedIcon", selectedIcon);
            SetReference(controllerSo, "selectedName", selectedName);
            SetReference(controllerSo, "selectedLevel", selectedLevel);
            SetReference(controllerSo, "descriptionText", description);
            SetReference(controllerSo, "comparisonRoot", comparisonRoot);
            SetReference(controllerSo, "comparisonRowPrefab", comparisonPrefab);
            SetReference(controllerSo, "materialsContent", materialsContent);
            SetReference(controllerSo, "materialRequirementPrefab", materialPrefab);
            SetReference(controllerSo, "materialsScrollbar", materialsScrollbar);
            SetReference(controllerSo, "actionButton", actionButton);
            SetReference(controllerSo, "actionText", actionText);
            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            foreach (EntrySeed seed in Seeds())
            {
                Transform parent = seed.category == UpgradeCategory.ShipSystem ? shipRoot : modulesRoot;
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(entryPrefab.gameObject, parent);
                instance.name = seed.id;
                ConfigureEntry(instance.GetComponent<UpgradeEntryConfig>(), seed, materials);
                ConfigureEntryInstanceVisual(instance.transform, seed.category == UpgradeCategory.Module);
            }
            return panel;
        }

        private static void BuildMaterialScroll(Transform parent, out RectTransform content, out UnityEngine.UI.Scrollbar scrollbar)
        {
            RectTransform scrollRoot = Box("ScrollView", parent, 18, 50, 592, 170);
            var scrollRect = scrollRoot.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 26;

            RectTransform viewport = Box("Viewport", scrollRoot, 0, 0, 558, 170);
            var viewportImage = viewport.gameObject.AddComponent<UnityEngine.UI.Image>();
            viewportImage.color = new Color(1, 1, 1, 0.01f);
            viewportImage.raycastTarget = true;
            var mask = viewport.gameObject.AddComponent<UnityEngine.UI.Mask>();
            mask.showMaskGraphic = false;

            content = Box("Content", viewport, 0, 0, 558, 170);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            var layout = content.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.spacing = 8;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            var fitter = content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            fitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

            UnityEngine.UI.Image track = Image("VerticalScrollbar", scrollRoot, Sprite("Controls/Scrollbar_Track.png"), 568, 0, 22, 170, true);
            track.type = UnityEngine.UI.Image.Type.Sliced;
            RectTransform slidingArea = Box("SlidingArea", track.transform, 3, 4, 16, 162);
            UnityEngine.UI.Image handle = Image("Handle", slidingArea, Sprite("Controls/Scrollbar_Thumb.png"), 0, 0, 16, 52, true);
            handle.type = UnityEngine.UI.Image.Type.Sliced;
            scrollbar = track.gameObject.AddComponent<UnityEngine.UI.Scrollbar>();
            scrollbar.targetGraphic = handle;
            scrollbar.handleRect = handle.rectTransform;
            scrollbar.direction = UnityEngine.UI.Scrollbar.Direction.BottomToTop;

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = UnityEngine.UI.ScrollRect.ScrollbarVisibility.Permanent;
            scrollRect.verticalScrollbarSpacing = 0;
        }

        private static UpgradeEntryConfig CreateEntryPrefab(TMP_FontAsset font)
        {
            const string path = PrefabFolder + "/UpgradeEntry.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                ConfigureEntryPrefab(existing, font);
                return AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<UpgradeEntryConfig>();
            }

            RectTransform root = Box("UpgradeEntry", null, 0, 0, 121, 150);
            var button = root.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
            UnityEngine.UI.Image background = Image("Background", root, Sprite("Cards/UpgradeCard_Default.png"), 0, 0, 121, 150, true);
            background.type = UnityEngine.UI.Image.Type.Simple;
            button.targetGraphic = background;
            var state = button.spriteState;
            state.highlightedSprite = Sprite("Cards/UpgradeCard_Hover.png");
            state.pressedSprite = Sprite("Cards/UpgradeCard_Selected.png");
            state.selectedSprite = Sprite("Cards/UpgradeCard_Selected.png");
            state.disabledSprite = Sprite("Cards/UpgradeCard_Disabled.png");
            button.spriteState = state;
            UnityEngine.UI.Image icon = Image("Icon", root, null, 22.5f, 10, 76, 72, false);
            icon.preserveAspect = true;
            TextMeshProUGUI name = Text("NameText", root, "UPGRADE", 8, 84, 105, 28, 22, font);
            name.alignment = TextAlignmentOptions.Center;
            name.fontStyle = FontStyles.Bold;
            name.enableAutoSizing = true;
            name.fontSizeMin = 16;
            name.fontSizeMax = 22;
            name.textWrappingMode = TextWrappingModes.NoWrap;
            TextMeshProUGUI level = Text("LevelText", root, "Lv. 1", 12, 118, 97, 23, 18, font);
            level.alignment = TextAlignmentOptions.Center;
            var config = root.gameObject.AddComponent<UpgradeEntryConfig>();
            var so = new SerializedObject(config);
            SetReference(so, "button", button);
            SetReference(so, "background", background);
            SetReference(so, "iconImage", icon);
            SetReference(so, "nameText", name);
            SetReference(so, "levelText", level);
            SetReference(so, "defaultSprite", Sprite("Cards/UpgradeCard_Default.png"));
            SetReference(so, "selectedSprite", Sprite("Cards/UpgradeCard_Selected.png"));
            SetReference(so, "disabledSprite", Sprite("Cards/UpgradeCard_Disabled.png"));
            so.ApplyModifiedPropertiesWithoutUndo();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, path);
            UnityEngine.Object.DestroyImmediate(root.gameObject);
            return prefab.GetComponent<UpgradeEntryConfig>();
        }

        private static UpgradeComparisonRowView CreateComparisonPrefab(TMP_FontAsset font)
        {
            const string path = PrefabFolder + "/ComparisonRow.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                ConfigureComparisonPrefab(existing, font);
                return AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<UpgradeComparisonRowView>();
            }
            RectTransform root = Box("ComparisonRow", null, 0, 0, 588, 38);
            var layout = root.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            layout.preferredHeight = 38;
            TextMeshProUGUI stat = Text("StatName", root, "STAT", 0, 0, 188, 38, 18, font);
            TextMeshProUGUI current = Text("CurrentValue", root, "CURRENT", 193, 0, 128, 38, 18, font);
            current.alignment = TextAlignmentOptions.Center;
            TextMeshProUGUI arrow = Text("Arrow", root, "→", 326, 0, 50, 38, 22, font);
            arrow.alignment = TextAlignmentOptions.Center;
            TextMeshProUGUI next = Text("NextValue", root, "NEXT", 381, 0, 207, 38, 18, font);
            next.alignment = TextAlignmentOptions.Center;
            next.color = new Color(0.03f, 0.55f, 0.42f);
            var view = root.gameObject.AddComponent<UpgradeComparisonRowView>();
            var so = new SerializedObject(view);
            SetReference(so, "statName", stat);
            SetReference(so, "currentValue", current);
            SetReference(so, "nextValue", next);
            so.ApplyModifiedPropertiesWithoutUndo();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, path);
            UnityEngine.Object.DestroyImmediate(root.gameObject);
            return prefab.GetComponent<UpgradeComparisonRowView>();
        }

        private static UpgradeMaterialRequirementView CreateMaterialPrefab(TMP_FontAsset font)
        {
            const string path = PrefabFolder + "/MaterialRequirement.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                ConfigureMaterialPrefab(existing, font);
                return AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<UpgradeMaterialRequirementView>();
            }
            UnityEngine.UI.Image background = Image("MaterialRequirement", null, Sprite("Panels/Panel_Small.png"), 0, 0, 558, 58, false);
            background.type = UnityEngine.UI.Image.Type.Sliced;
            var layout = background.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            layout.preferredHeight = 58;
            UnityEngine.UI.Image icon = Image("Icon", background.transform, null, 10, 5, 48, 48, false);
            icon.preserveAspect = true;
            TextMeshProUGUI name = Text("NameText", background.transform, "MATERIAL", 70, 6, 338, 46, 20, font);
            TextMeshProUGUI quantity = Text("QuantityText", background.transform, "x1", 420, 6, 124, 46, 20, font);
            quantity.alignment = TextAlignmentOptions.MidlineRight;
            quantity.fontStyle = FontStyles.Bold;
            var view = background.gameObject.AddComponent<UpgradeMaterialRequirementView>();
            var so = new SerializedObject(view);
            SetReference(so, "icon", icon);
            SetReference(so, "nameText", name);
            SetReference(so, "quantityText", quantity);
            so.ApplyModifiedPropertiesWithoutUndo();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(background.gameObject, path);
            UnityEngine.Object.DestroyImmediate(background.gameObject);
            return prefab.GetComponent<UpgradeMaterialRequirementView>();
        }

        private static void ConfigureEntryPrefab(GameObject asset, TMP_FontAsset font)
        {
            if (asset == null) return;
            string path = AssetDatabase.GetAssetPath(asset);
            GameObject rootObject = PrefabUtility.LoadPrefabContents(path);
            try
            {
                RectTransform root = rootObject.GetComponent<RectTransform>();
                root.sizeDelta = new Vector2(121, 164);

                RectTransform background = Rect(root, "Background");
                Stretch(background, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                UnityEngine.UI.Image backgroundImage = background.GetComponent<UnityEngine.UI.Image>();
                backgroundImage.sprite = Sprite("Panels/Panel_Large.png");
                backgroundImage.type = UnityEngine.UI.Image.Type.Simple;
                backgroundImage.preserveAspect = false;
                var card = new SerializedObject(rootObject.GetComponent<UpgradeEntryConfig>());
                foreach (string property in new[] { "defaultSprite", "selectedSprite", "disabledSprite" })
                    card.FindProperty(property).objectReferenceValue = backgroundImage.sprite;
                card.ApplyModifiedPropertiesWithoutUndo();

                RectTransform icon = Rect(root, "Icon");
                icon.anchorMin = icon.anchorMax = new Vector2(0.5f, 1);
                icon.pivot = new Vector2(0.5f, 1);
                icon.anchoredPosition = new Vector2(0, -10);
                icon.sizeDelta = new Vector2(68, 64);

                TextMeshProUGUI name = Rect(root, "NameText").GetComponent<TextMeshProUGUI>();
                BottomStretch(name.rectTransform, 8, 8, 40, 38);
                name.font = font;
                name.fontSize = 22;
                name.enableAutoSizing = true;
                name.fontSizeMin = 16;
                name.fontSizeMax = 22;
                name.textWrappingMode = TextWrappingModes.Normal;
                name.overflowMode = TextOverflowModes.Overflow;
                name.alignment = TextAlignmentOptions.Center;
                name.fontStyle = FontStyles.Bold;

                TextMeshProUGUI level = Rect(root, "LevelText").GetComponent<TextMeshProUGUI>();
                BottomStretch(level.rectTransform, 12, 12, 9, 23);
                level.font = font;
                level.fontSize = 18;
                level.alignment = TextAlignmentOptions.Center;

                PrefabUtility.SaveAsPrefabAsset(rootObject, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(rootObject);
            }
        }

        private static void ConfigureComparisonPrefab(GameObject asset, TMP_FontAsset font)
        {
            if (asset == null) return;
            string path = AssetDatabase.GetAssetPath(asset);
            GameObject rootObject = PrefabUtility.LoadPrefabContents(path);
            try
            {
                RectTransform root = rootObject.GetComponent<RectTransform>();
                root.sizeDelta = new Vector2(588, 38);
                var layout = rootObject.GetComponent<UnityEngine.UI.LayoutElement>();
                if (layout == null) layout = rootObject.AddComponent<UnityEngine.UI.LayoutElement>();
                layout.preferredHeight = 38;

                ConfigureRowText(Rect(root, "StatName").GetComponent<TextMeshProUGUI>(), font, 0, 188, TextAlignmentOptions.MidlineLeft);
                ConfigureRowText(Rect(root, "CurrentValue").GetComponent<TextMeshProUGUI>(), font, 193, 128, TextAlignmentOptions.Center);
                ConfigureRowText(Rect(root, "Arrow").GetComponent<TextMeshProUGUI>(), font, 326, 50, TextAlignmentOptions.Center, 22);
                ConfigureRowText(Rect(root, "NextValue").GetComponent<TextMeshProUGUI>(), font, 381, 207, TextAlignmentOptions.Center);

                PrefabUtility.SaveAsPrefabAsset(rootObject, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(rootObject);
            }
        }

        private static void ConfigureMaterialPrefab(GameObject asset, TMP_FontAsset font)
        {
            if (asset == null) return;
            string path = AssetDatabase.GetAssetPath(asset);
            GameObject rootObject = PrefabUtility.LoadPrefabContents(path);
            try
            {
                RectTransform root = rootObject.GetComponent<RectTransform>();
                root.sizeDelta = new Vector2(558, 58);
                UnityEngine.UI.Image background = rootObject.GetComponent<UnityEngine.UI.Image>();
                background.sprite = null;
                background.type = UnityEngine.UI.Image.Type.Simple;
                background.color = new Color(.84f, .95f, .97f, .9f);
                var layout = rootObject.GetComponent<UnityEngine.UI.LayoutElement>();
                if (layout == null) layout = rootObject.AddComponent<UnityEngine.UI.LayoutElement>();
                layout.preferredHeight = 58;
                layout.flexibleWidth = 1;

                Place(Rect(root, "Icon"), 10, 5, 48, 48);
                TextMeshProUGUI name = Rect(root, "NameText").GetComponent<TextMeshProUGUI>();
                Place(name.rectTransform, 70, 6, 338, 46);
                name.font = font;
                name.fontSize = 20;
                name.alignment = TextAlignmentOptions.MidlineLeft;

                TextMeshProUGUI quantity = Rect(root, "QuantityText").GetComponent<TextMeshProUGUI>();
                Place(quantity.rectTransform, 420, 6, 124, 46);
                quantity.font = font;
                quantity.fontSize = 20;
                quantity.alignment = TextAlignmentOptions.MidlineRight;

                PrefabUtility.SaveAsPrefabAsset(rootObject, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(rootObject);
            }
        }

        private static void ConfigureRowText(TextMeshProUGUI text, TMP_FontAsset font, float x, float width,
            TextAlignmentOptions alignment, float size = 18)
        {
            Place(text.rectTransform, x, 0, width, 38);
            text.font = font;
            text.fontSize = size;
            text.alignment = alignment;
        }

        private static void ConfigureMaterialScroll(RectTransform requiredPanel)
        {
            RectTransform scrollRoot = Rect(requiredPanel, "ScrollView");
            Place(scrollRoot, 18, 50, 592, 170);
            RectTransform viewport = Rect(scrollRoot, "Viewport");
            Place(viewport, 0, 0, 558, 170);
            RectTransform content = Rect(viewport, "Content");
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;

            var grid = content.GetComponent<UnityEngine.UI.GridLayoutGroup>();
            if (grid != null) UnityEngine.Object.DestroyImmediate(grid);
            var layout = content.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            if (layout == null) layout = content.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.spacing = 8;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            var fitter = content.GetComponent<UnityEngine.UI.ContentSizeFitter>();
            if (fitter == null) fitter = content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            fitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

            RectTransform track = Rect(scrollRoot, "VerticalScrollbar");
            Place(track, 568, 0, 22, 170);
            RectTransform slidingArea = Rect(track, "SlidingArea");
            Place(slidingArea, 3, 4, 16, 162);
            Place(Rect(slidingArea, "Handle"), 0, 0, 16, 52);

            var scrollRect = scrollRoot.GetComponent<UnityEngine.UI.ScrollRect>();
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.verticalScrollbar = track.GetComponent<UnityEngine.UI.Scrollbar>();
            scrollRect.verticalScrollbarVisibility = UnityEngine.UI.ScrollRect.ScrollbarVisibility.Permanent;
            scrollRect.verticalScrollbarSpacing = 0;
        }

        private static UpgradeMaterialDefinition[] CreateMaterialDefinitions()
        {
            string[][] definitions = {
                new[] { RegularShipUpgradeRules.TierOneMaterial, "Zone 2 Creature 02", "Z2_Creature_02.asset" },
                new[] { RegularShipUpgradeRules.TierTwoMaterial, "Zone 3 Creature 01", "Z3_Creature_01.asset" }
            };
            var result = new UpgradeMaterialDefinition[definitions.Length];
            for (int i = 0; i < definitions.Length; i++)
            {
                string path = $"{DataFolder}/{definitions[i][2].Replace(".png", ".asset")}";
                var definition = AssetDatabase.LoadAssetAtPath<UpgradeMaterialDefinition>(path);
                if (definition == null)
                {
                    definition = ScriptableObject.CreateInstance<UpgradeMaterialDefinition>();
                    AssetDatabase.CreateAsset(definition, path);
                }
                var so = new SerializedObject(definition);
                so.FindProperty("materialId").stringValue = definitions[i][0];
                so.FindProperty("displayName").stringValue = definitions[i][1];
                string zone = i == 0 ? "Zone02" : "Zone03";
                var content = AssetDatabase.LoadAssetAtPath<G10.Prototype.Missions.SurveyContentDefinition>($"Assets/_Project/Content/{zone}/Definitions/{definitions[i][2]}");
                so.FindProperty("icon").objectReferenceValue = content?.sprite != null ? content.sprite : Sprite("Icons/ShipSystems/Capture.png");
                so.ApplyModifiedPropertiesWithoutUndo();
                result[i] = definition;
            }
            return result;
        }

        private static void ConfigureEntry(UpgradeEntryConfig entry, EntrySeed seed, UpgradeMaterialDefinition[] materials)
        {
            var so = new SerializedObject(entry);
            so.FindProperty("category").enumValueIndex = (int)seed.category;
            so.FindProperty("icon").objectReferenceValue = Sprite(seed.icon);
            so.FindProperty("displayName").stringValue = seed.name;
            so.FindProperty("level").intValue = 1;
            so.FindProperty("description").stringValue = seed.description;
            so.FindProperty("upgradeId").stringValue = seed.id;

            SerializedProperty comparisons = so.FindProperty("comparisonRows");
            comparisons.arraySize = seed.comparisons?.Length ?? 0;
            for (int i = 0; i < comparisons.arraySize; i++)
            {
                SerializedProperty row = comparisons.GetArrayElementAtIndex(i);
                row.FindPropertyRelative("statName").stringValue = seed.comparisons[i][0];
                row.FindPropertyRelative("currentValue").stringValue = seed.comparisons[i][1];
                row.FindPropertyRelative("nextValue").stringValue = seed.comparisons[i][2];
            }

            SerializedProperty requirements = so.FindProperty("materialRequirements");
            requirements.arraySize = seed.materials?.Length ?? 0;
            for (int i = 0; i < requirements.arraySize; i++)
            {
                SerializedProperty requirement = requirements.GetArrayElementAtIndex(i);
                requirement.FindPropertyRelative("material").objectReferenceValue = materials.First(x => x.MaterialId == seed.materials[i]);
                requirement.FindPropertyRelative("requiredAmount").intValue = seed.materialAmounts[i];
            }

            if (seed.shipUpgrade.HasValue)
            {
                var action = entry.GetComponent<ShipUpgradeAction>() ?? entry.gameObject.AddComponent<ShipUpgradeAction>();
                var actionSo = new SerializedObject(action);
                actionSo.FindProperty("upgrade").enumValueIndex = (int)seed.shipUpgrade.Value;
                actionSo.FindProperty("amount").floatValue = seed.amount;
                actionSo.ApplyModifiedPropertiesWithoutUndo();
                so.FindProperty("actionSource").objectReferenceValue = action;
                so.FindProperty("tierOneMaterial").objectReferenceValue = materials[0];
                so.FindProperty("tierTwoMaterial").objectReferenceValue = materials[1];
            }
            else if (seed.id == "ExpeditionModule")
            {
                var action = entry.GetComponent<StoryHullUpgradeAction>() ?? entry.gameObject.AddComponent<StoryHullUpgradeAction>();
                action.story = entry.GetComponentInParent<G10.Prototype.Missions.ZoneOneStory>(true);
                so.FindProperty("actionSource").objectReferenceValue = action;
                EditorUtility.SetDirty(action);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(entry);
        }

        private static EntrySeed[] Seeds() => new[]
        {
            Ship("Hull", "HULL REINFORCEMENT", "Reinforce hull integrity: 100 → 120 → 140. Two levels; no free refill.", "Icons/ShipSystems/Hull.png", ShipUpgrade.Hull, 20, "Hull", "100", "120", RegularShipUpgradeRules.TierOneMaterial, 2),
            Ship("MaxSpeed", "PROPULSION", "Primary movement speed: 100% → 110% → 120% of authored base speed.", "Icons/ShipSystems/Max_Speed.png", ShipUpgrade.Speed, 0, "Speed", "Base", "110%", RegularShipUpgradeRules.TierOneMaterial, 1),
            Ship("Energy", "ENERGY EFFICIENCY", "Movement consumption: 1.00 → 0.90 → 0.80 Energy/sec. Capacity is unchanged.", "Icons/ShipSystems/Energy.png", ShipUpgrade.Energy, 0, "Energy/sec", "1.00", "0.90", RegularShipUpgradeRules.TierOneMaterial, 2),
            new EntrySeed { id="ExpeditionModule", name="EXPEDITION MODULE", description="Mandatory mission progression: Pressure Hull, Bio Lamp and Rock Breaker.",
                icon="Icons/Modules/Depth_System.png", category=UpgradeCategory.Module, comparisons=Array.Empty<string[]>(), materials=Array.Empty<string>(), materialAmounts=Array.Empty<int>() }
        };

        private static EntrySeed Ship(string id, string name, string description, string icon, ShipUpgrade upgrade,
            float amount, string stat, string current, string next, string material, int required)
            => new() { id=id, name=name, description=description, icon=icon, category=UpgradeCategory.ShipSystem,
                shipUpgrade=upgrade, amount=amount, comparisons=new[] { new[] { stat, current, next } },
                materials=new[] { material }, materialAmounts=new[] { required } };

        private static EntrySeed Module(string id, string name, string description, string icon,
            string stat, string current, string next, string material, int required)
            => new() { id=id, name=name, description=description, icon=icon, category=UpgradeCategory.Module,
                comparisons=new[] { new[] { stat, current, next } }, materials=new[] { material }, materialAmounts=new[] { required } };

        private static void CreateDesktopShortcut(ComputerScreenController screen)
        {
            if (screen.Desktop.transform.Find("UpgradeIcon") != null) return;
            RectTransform rect = Box("UpgradeIcon", screen.Desktop.transform, 0, 0, 190, 220);
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = Color.clear;
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
            UnityEventTools.AddPersistentListener(button.onClick, screen.OpenUpgrade);
        }

        private static void RegisterApp(ComputerScreenController screen, GameObject panel)
        {
            var so = new SerializedObject(screen);
            SerializedProperty apps = so.FindProperty("apps");
            for (int i = 0; i < apps.arraySize; i++)
                if (apps.GetArrayElementAtIndex(i).FindPropertyRelative("id").enumValueIndex == (int)ComputerAppId.Upgrade)
                    return;
            int index = apps.arraySize;
            apps.InsertArrayElementAtIndex(index);
            SerializedProperty app = apps.GetArrayElementAtIndex(index);
            app.FindPropertyRelative("id").enumValueIndex = (int)ComputerAppId.Upgrade;
            app.FindPropertyRelative("panel").objectReferenceValue = panel;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignDesktopIcon(ComputerScreenController screen)
        {
            ComputerDesktopSkin skin = screen.GetComponent<ComputerDesktopSkin>();
            if (skin == null) return;
            var so = new SerializedObject(skin);
            so.FindProperty("upgradeIcon").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "icon_desktop.png");
            so.FindProperty("upgradeTitleIcon").objectReferenceValue = Sprite("Icons/Materials/Core_Part.png");
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(skin);
        }

        private static void ConfigureTextures()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Sprites });
            foreach (string guid in guids) ConfigureTexture(AssetDatabase.GUIDToAssetPath(guid));
            ConfigureTexture(Art + "icon_desktop.png");
            AssetDatabase.SaveAssets();
        }

        private static void ConfigureTexture(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.spriteBorder = BorderFor(path);
            importer.SaveAndReimport();
        }

        private static Vector4 BorderFor(string path)
        {
            if (path.Contains("/Window/")) return new Vector4(80, 80, 80, 80);
            if (path.Contains("/Cards/")) return new Vector4(28, 28, 28, 28);
            if (path.Contains("/Panels/")) return new Vector4(24, 24, 24, 24);
            if (path.Contains("ActionButton") || path.Contains("Scrollbar")) return new Vector4(20, 20, 20, 20);
            return Vector4.zero;
        }

        private static Sprite Sprite(string relativePath)
            => AssetDatabase.LoadAssetAtPath<Sprite>(Sprites + relativePath);

        private static RectTransform Header(Transform parent, string name, string title, float x, float y, float w, float h, TMP_FontAsset font)
        {
            UnityEngine.UI.Image image = Image(name, parent, Sprite("Panels/Header_Section.png"), x, y, w, h, false);
            image.type = UnityEngine.UI.Image.Type.Simple;
            TextMeshProUGUI label = Text("Label", image.transform, title, 18, 3, w - 36, h - 6, 24, font);
            label.fontStyle = FontStyles.Bold;
            return image.rectTransform;
        }

        private static void ConfigurePanel(RectTransform panel, string spritePath)
        {
            UnityEngine.UI.Image image = panel.GetComponent<UnityEngine.UI.Image>();
            image.sprite = Sprite(spritePath);
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.preserveAspect = false;
        }

        private static void ConfigureHeader(RectTransform header, float x, float y, float width, float height, TMP_FontAsset font)
        {
            UnityEngine.UI.Image image = header.GetComponent<UnityEngine.UI.Image>();
            image.sprite = Sprite("Panels/Header_Section.png");
            image.type = UnityEngine.UI.Image.Type.Simple;
            image.preserveAspect = false;
            Place(header, x, y, width, height);
            RectTransform label = Rect(header, "Label");
            Place(label, 18, 3, width - 36, height - 6);
            ConfigureText(label, font, 24, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        }

        private static RectTransform EnsureImage(Transform parent, string name, string spritePath)
        {
            Transform existing = parent.Find(name);
            UnityEngine.UI.Image image;
            if (existing == null)
            {
                image = Image(name, parent, Sprite(spritePath), 0, 0, 100, 40, false);
                Undo.RegisterCreatedObjectUndo(image.gameObject, "Add Upgrade UI header band");
            }
            else
            {
                image = existing.GetComponent<UnityEngine.UI.Image>();
                if (image == null) image = existing.gameObject.AddComponent<UnityEngine.UI.Image>();
                image.sprite = Sprite(spritePath);
                image.raycastTarget = false;
            }
            image.type = UnityEngine.UI.Image.Type.Simple;
            image.preserveAspect = false;
            return image.rectTransform;
        }

        private static void ConfigureText(RectTransform rect, TMP_FontAsset font, float size, FontStyles style,
            TextAlignmentOptions alignment)
        {
            TextMeshProUGUI text = rect.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = Ink;
            text.raycastTarget = false;
        }

        private static void ConfigureEntryInstances(RectTransform root, bool module)
        {
            foreach (Transform child in root) ConfigureEntryInstanceVisual(child, module);
        }

        private static void ConfigureEntryInstanceVisual(Transform entry, bool module)
        {
            RectTransform background = Rect(entry, "Background");
            Stretch(background, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            UnityEngine.UI.Image backgroundImage = background.GetComponent<UnityEngine.UI.Image>();
            backgroundImage.type = UnityEngine.UI.Image.Type.Simple;
            backgroundImage.preserveAspect = false;
            backgroundImage.sprite = Sprite("Panels/Panel_Large.png");
            var card = new SerializedObject(entry.GetComponent<UpgradeEntryConfig>());
            foreach (string property in new[] { "defaultSprite", "selectedSprite", "disabledSprite" })
                card.FindProperty(property).objectReferenceValue = backgroundImage.sprite;
            card.ApplyModifiedPropertiesWithoutUndo();

            RectTransform icon = Rect(entry, "Icon");
            icon.anchorMin = icon.anchorMax = new Vector2(0.5f, 1);
            icon.pivot = new Vector2(0.5f, 1);
            icon.anchoredPosition = new Vector2(0, module ? -12 : -10);
            icon.sizeDelta = module ? new Vector2(78, 74) : new Vector2(68, 64);

            TextMeshProUGUI name = Rect(entry, "NameText").GetComponent<TextMeshProUGUI>();
            BottomStretch(name.rectTransform, 8, 8, 40, 38);
            name.fontSize = module ? 22 : 22;
            name.fontSizeMin = 16;
            name.fontSizeMax = 22;
            name.enableAutoSizing = true;
            name.textWrappingMode = TextWrappingModes.Normal;
            name.overflowMode = TextOverflowModes.Overflow;
            name.fontStyle = FontStyles.Bold;

            TextMeshProUGUI level = Rect(entry, "LevelText").GetComponent<TextMeshProUGUI>();
            BottomStretch(level.rectTransform, 12, 12, module ? 10 : 9, module ? 27 : 23);
            level.fontSize = module ? 19 : 18;
        }

        private static void Grid(RectTransform root, Vector2 cellSize, Vector2 spacing, int columns)
        {
            var grid = root.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>();
            ConfigureGrid(grid, cellSize, spacing, columns);
        }

        private static void ConfigureGrid(RectTransform root, Vector2 cellSize, Vector2 spacing, int columns)
        {
            var grid = root.GetComponent<UnityEngine.UI.GridLayoutGroup>();
            if (grid == null) grid = root.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>();
            ConfigureGrid(grid, cellSize, spacing, columns);
        }

        private static void ConfigureGrid(UnityEngine.UI.GridLayoutGroup grid, Vector2 cellSize, Vector2 spacing, int columns)
        {
            grid.cellSize = cellSize;
            grid.spacing = spacing;
            grid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.startCorner = UnityEngine.UI.GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = UnityEngine.UI.GridLayoutGroup.Axis.Horizontal;
        }

        private static RectTransform Rect(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child == null) throw new InvalidOperationException($"Missing expected Upgrade UI object '{parent.name}/{name}'.");
            return (RectTransform)child;
        }

        private static void DestroyNamed(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null) Undo.DestroyObjectImmediate(child.gameObject);
        }

        private static void Place(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
            rect.localScale = Vector3.one;
        }

        private static void BottomStretch(RectTransform rect, float left, float right, float bottom, float height)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.right;
            rect.pivot = new Vector2(0.5f, 0);
            rect.anchoredPosition = new Vector2((left - right) * 0.5f, bottom);
            rect.sizeDelta = new Vector2(-(left + right), height);
            rect.localScale = Vector3.one;
        }

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localScale = Vector3.one;
        }

        private static RectTransform Box(string name, Transform parent, float x, float y, float w, float h)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            if (parent != null) gameObject.transform.SetParent(parent, false);
            var rect = (RectTransform)gameObject.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
            return rect;
        }

        private static UnityEngine.UI.Image Image(string name, Transform parent, Sprite sprite, float x, float y, float w, float h, bool raycast)
        {
            RectTransform rect = Box(name, parent, x, y, w, h);
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite = sprite;
            image.raycastTarget = raycast;
            return image;
        }

        private static TextMeshProUGUI Text(string name, Transform parent, string value, float x, float y, float w, float h, float size, TMP_FontAsset font)
        {
            RectTransform rect = Box(name, parent, x, y, w, h);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = size;
            text.color = Ink;
            text.text = value;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.raycastTarget = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        private static UnityEngine.UI.Button Button(string name, Transform parent, string title, float x, float y, float w, float h,
            TMP_FontAsset font, Sprite normal, Sprite selected, Sprite disabled)
        {
            UnityEngine.UI.Image image = Image(name, parent, normal, x, y, w, h, true);
            image.type = normal != null ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            if (normal == null) image.color = new Color(0.7f, 0.9f, 1, 0.2f);
            var button = image.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
            if (selected != null || disabled != null)
            {
                button.transition = UnityEngine.UI.Selectable.Transition.SpriteSwap;
                var state = button.spriteState;
                state.highlightedSprite = selected;
                state.pressedSprite = selected;
                state.selectedSprite = selected;
                state.disabledSprite = disabled;
                button.spriteState = state;
            }
            TextMeshProUGUI label = Text("ActionText", image.transform, title, 12, 8, w - 24, h - 16, 24, font);
            label.alignment = TextAlignmentOptions.Center;
            return button;
        }

        private static void SetReference(SerializedObject serializedObject, string name, UnityEngine.Object value)
            => serializedObject.FindProperty(name).objectReferenceValue = value;

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
