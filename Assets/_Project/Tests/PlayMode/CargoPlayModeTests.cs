using System;
using System.Collections;
using System.IO;
using G10.Prototype.Computer;
using G10.Prototype.Missions;
using G10.Prototype.Navigation;
using G10.Prototype.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace G10.Prototype.Tests
{
    public sealed class CargoPlayModeTests
    {
        private string folder;
        private CabinStationView cabin;
        private ComputerScreenController screen;
        private CreatureInventory inventory;
        private CreatureInventoryView cargo;
        private ZoneOneStory story;
        private ExpeditionLoop loop;

        [UnitySetUp] public IEnumerator Setup()
        {
            folder = Path.Combine(Application.temporaryCachePath, "CargoTests-" + Guid.NewGuid().ToString("N"));
            ExpeditionSaveStore.PathOverride = Path.Combine(folder, "timeline.json");
            PhotoCaptureService.ArchivePathOverride = Path.Combine(folder, "photos");
            yield return Load();
        }

        private IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("GameplayCore", LoadSceneMode.Single);
            yield return SceneManager.LoadSceneAsync("Zone01", LoadSceneMode.Additive);
            yield return null;
            yield return null;
            cabin = Object.FindAnyObjectByType<CabinStationView>();
            Assert.That(cabin, Is.Not.Null);
            screen = cabin.GetComponentInChildren<ComputerScreenController>(true);
            inventory = cabin.GetComponent<CreatureInventory>();
            story = cabin.GetComponent<ZoneOneStory>();
            loop = Object.FindAnyObjectByType<ExpeditionLoop>();
            var app = Array.Find(screen.Apps, entry => entry.id == ComputerAppId.Cargo);
            Assert.That(app, Is.Not.Null, "The authored Zone01 scene must register Cargo.");
            cargo = app.panel.GetComponent<CreatureInventoryView>();
            Assert.That(cargo, Is.Not.Null);
            Assert.That(cargo.inventory, Is.SameAs(inventory), "Cargo must display the existing expedition inventory.");
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            ExpeditionSaveStore.PathOverride = null;
            PhotoCaptureService.ArchivePathOverride = null;
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }

        [UnityTest] public IEnumerator DesktopAndTaskbarOpenMinimizeRestoreAndCloseCargo()
        {
            Assert.That(Array.FindAll(screen.Apps, app => app.id == ComputerAppId.Cargo).Length, Is.EqualTo(1));
            Assert.That(Array.Exists(screen.Apps, app => (int)app.id == 7), Is.False, "Removed Research ID 7 must stay unregistered.");
            foreach (var child in cabin.GetComponentsInChildren<Transform>(true))
                Assert.That(child.name, Is.Not.EqualTo("ResearchPanel").And.Not.EqualTo("ResearchIcon")
                    .And.Not.EqualTo("Backpack").And.Not.EqualTo("BackpackHotspot"), "Removed UI must not remain in the cabin hierarchy.");
            Assert.That(cabin.GetComponentsInChildren<CreatureInventoryView>(true).Length, Is.EqualTo(1), "There must be one Cargo view, without the old bag panel.");
            cabin.OpenComputer();
            yield return null;
            Assert.That(screen.DesktopWindows, Is.True);
            var shortcut = RequireButton(screen.Desktop.transform, "CargoIcon");
            var taskbar = RequireButton(screen.transform, "Taskbar/Task_Cargo");
            var indicator = taskbar.transform.Find("RunningIndicator");
            Assert.That(indicator, Is.Not.Null);
            Assert.That(indicator.gameObject.activeSelf, Is.False);
            var skin = screen.GetComponent<ComputerDesktopSkin>();
            Assert.That(skin.cargoIcon, Is.Not.Null);
            Assert.That(shortcut.transform.Find("Icon").GetComponent<Image>().sprite.texture, Is.SameAs(skin.cargoIcon));
            Assert.That(taskbar.transform.Find("Icon").GetComponent<Image>().sprite.texture, Is.SameAs(skin.cargoIcon));

            shortcut.onClick.Invoke();
            AssertCargoOpen();
            Assert.That(indicator.gameObject.activeSelf, Is.True);
            RequireButton(cargo.transform, "Minimize").onClick.Invoke();
            Assert.That(screen.IsRunning(ComputerAppId.Cargo), Is.True);
            Assert.That(screen.IsMinimized(ComputerAppId.Cargo), Is.True);
            Assert.That(cargo.gameObject.activeSelf, Is.False);
            Assert.That(indicator.gameObject.activeSelf, Is.True, "Minimized Cargo remains a running app.");
            taskbar.onClick.Invoke();
            AssertCargoOpen();
            taskbar.onClick.Invoke();
            Assert.That(screen.IsMinimized(ComputerAppId.Cargo), Is.True);
            taskbar.onClick.Invoke();
            AssertCargoOpen();
            RequireButton(cargo.transform, "Back").onClick.Invoke();
            Assert.That(screen.IsRunning(ComputerAppId.Cargo), Is.False);
            Assert.That(cargo.gameObject.activeSelf, Is.False);
            Assert.That(indicator.gameObject.activeSelf, Is.False);
            taskbar.onClick.Invoke();
            AssertCargoOpen();
            Assert.That(screen.TryHandleBack(), Is.True);
            Assert.That(screen.CurrentApp, Is.EqualTo(ComputerAppId.Desktop));
            Assert.That(screen.TryHandleBack(), Is.False);
            cabin.ClosePanel();
            cabin.OpenCargo();
            AssertCargoOpen();
        }

        [UnityTest] public IEnumerator ItemDetailsSelectionAndSavedCargoStayConsistent()
        {
            cabin.OpenCargo();
            yield return null;
            AssertEmpty();
            Assert.That(inventory.TryAdd(story.emmaTube.id, story.emmaTube.displayName, story.emmaTube.Image), Is.True);
            AssertDetails(story.emmaTube);
            Assert.That(inventory.TryAdd(story.creatureOne.id, story.creatureOne.displayName, story.creatureOne.Image), Is.True);
            cargo.slots[1].onClick.Invoke();
            AssertDetails(story.creatureOne);
            Assert.That(cargo.selectionFrames[1].enabled, Is.True);
            Assert.That(cargo.selectionFrames[0].enabled, Is.False);
            screen.CloseApp(ComputerAppId.Cargo);
            screen.OpenCargo();
            AssertDetails(story.creatureOne);
            Assert.That(loop.SaveCurrent(), Is.True);
            yield return Load();
            cabin.OpenCargo();
            Assert.That(inventory.Items.Count, Is.EqualTo(2));
            Assert.That(inventory.Contains(story.emmaTube.id), Is.True);
            Assert.That(inventory.Contains(story.creatureOne.id), Is.True);
            cargo.slots[1].onClick.Invoke();
            AssertDetails(story.creatureOne);
            Assert.That(inventory.Remove(story.emmaTube.id), Is.True);
            AssertDetails(story.creatureOne);
            Assert.That(cargo.selectionFrames[0].enabled, Is.True, "Selection follows the item when a preceding slot is removed.");
            Assert.That(cargo.selectionFrames[1].enabled, Is.False);
            Assert.That(inventory.TryAdd(story.emmaTube.id, story.emmaTube.displayName, story.emmaTube.Image), Is.True);
            Assert.That(inventory.Remove(story.creatureOne.id), Is.True);
            AssertDetails(story.emmaTube);
            screen.CloseApp(ComputerAppId.Cargo);
            Assert.That(inventory.Remove(story.emmaTube.id), Is.True);
            screen.OpenCargo();
            AssertEmpty();
        }

        [UnityTest] public IEnumerator TwelveSlotsUseThreeColumnsAndScrollToLastRow()
        {
            Assert.That(CreatureInventory.Capacity, Is.EqualTo(12));
            Assert.That(cargo.icons.Length, Is.EqualTo(CreatureInventory.Capacity));
            Assert.That(cargo.slots.Length, Is.EqualTo(CreatureInventory.Capacity));
            for (int i = 0; i < CreatureInventory.Capacity; i++)
                Assert.That(inventory.TryAdd("cargo-test-" + i, "Cargo item " + i, story.creatureOne.Image), Is.True);
            Assert.That(inventory.IsFull, Is.True);
            Assert.That(inventory.TryAdd("overflow", "Overflow", null), Is.False);
            cabin.OpenCargo();
            yield return null;
            Canvas.ForceUpdateCanvases();
            var scroll = cargo.GetComponentInChildren<ScrollRect>();
            Assert.That(scroll, Is.Not.Null);
            Assert.That(scroll.vertical, Is.True);
            Assert.That(scroll.horizontal, Is.False);
            Assert.That(scroll.viewport.GetComponent<RectMask2D>(), Is.Not.Null);
            Assert.That(scroll.content.rect.height, Is.GreaterThan(scroll.viewport.rect.height));
            var first = (RectTransform)cargo.slots[0].transform;
            var second = (RectTransform)cargo.slots[1].transform;
            var third = (RectTransform)cargo.slots[2].transform;
            var nextRow = (RectTransform)cargo.slots[3].transform;
            Assert.That(first.anchoredPosition.x, Is.LessThan(second.anchoredPosition.x));
            Assert.That(second.anchoredPosition.x, Is.LessThan(third.anchoredPosition.x));
            Assert.That(first.anchoredPosition.y, Is.EqualTo(third.anchoredPosition.y).Within(.1f));
            Assert.That(nextRow.anchoredPosition.x, Is.EqualTo(first.anchoredPosition.x).Within(.1f));
            Assert.That(nextRow.anchoredPosition.y, Is.LessThan(first.anchoredPosition.y));
            var last = (RectTransform)cargo.slots[CreatureInventory.Capacity - 1].transform;
            scroll.verticalNormalizedPosition = 1;
            Canvas.ForceUpdateCanvases();
            Assert.That(scroll.viewport.rect.Contains(CenterInViewport(scroll.viewport, last)), Is.False, "The last row starts below the clipped viewport.");
            scroll.verticalNormalizedPosition = 0;
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.That(scroll.viewport.rect.Contains(CenterInViewport(scroll.viewport, last)), Is.True, "Scrolling must make the last row reachable.");
            cargo.slots[CreatureInventory.Capacity - 1].onClick.Invoke();
            Assert.That(cargo.SelectedItemId, Is.EqualTo("cargo-test-" + (CreatureInventory.Capacity - 1)));
            Assert.That(cargo.detailName.text, Is.EqualTo("Cargo item " + (CreatureInventory.Capacity - 1)));
            Assert.That(inventory.Remove("cargo-test-0"), Is.True);
            Assert.That(inventory.IsFull, Is.False);
            Assert.That(inventory.TryAdd("replacement", "Replacement", story.creatureOne.Image), Is.True);
            Assert.That(inventory.Items.Count, Is.EqualTo(CreatureInventory.Capacity));
        }

        private void AssertCargoOpen()
        {
            Assert.That(cabin.Panels.CurrentPanel, Is.SameAs(screen.gameObject));
            Assert.That(screen.CurrentApp, Is.EqualTo(ComputerAppId.Cargo));
            Assert.That(screen.IsRunning(ComputerAppId.Cargo), Is.True);
            Assert.That(screen.IsMinimized(ComputerAppId.Cargo), Is.False);
            Assert.That(cargo.gameObject.activeInHierarchy, Is.True);
            Assert.That(cargo.GetComponent<ComputerWindow>().screen, Is.SameAs(screen));
        }

        private void AssertEmpty()
        {
            Assert.That(cargo.SelectedItemId, Is.Null);
            Assert.That(cargo.detailName.text, Does.Contain("trống"));
            Assert.That(cargo.detailPreview.texture, Is.Null);
            Assert.That(cargo.detailPreview.enabled, Is.False);
            Assert.That(cargo.detailQuantity.text, Is.Empty);
            foreach (var slot in cargo.slots) Assert.That(slot.interactable, Is.False);
            foreach (var icon in cargo.icons) Assert.That(icon.enabled, Is.False);
            foreach (var selection in cargo.selectionFrames) Assert.That(selection.enabled, Is.False);
        }

        private void AssertDetails(SurveyContentDefinition item)
        {
            Assert.That(cargo.SelectedItemId, Is.EqualTo(item.id));
            Assert.That(cargo.detailName.text, Is.EqualTo(item.displayName));
            Assert.That(cargo.detailDescription.text, Is.EqualTo(item.description));
            Assert.That(cargo.detailPreview.texture, Is.SameAs(item.Image));
            Assert.That(cargo.detailPreview.enabled, Is.True);
            Assert.That(cargo.detailQuantity.text, Is.EqualTo("×1"));
        }

        private static Button RequireButton(Transform parent, string path)
        {
            var target = parent.Find(path);
            Assert.That(target, Is.Not.Null, "Missing Cargo control: " + path);
            var button = target.GetComponent<Button>();
            Assert.That(button, Is.Not.Null, path + " must be clickable.");
            return button;
        }

        private static Vector2 CenterInViewport(RectTransform viewport, RectTransform item)
            => viewport.InverseTransformPoint(item.TransformPoint(item.rect.center));
    }
}
