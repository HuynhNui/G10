using System.Collections.Generic;
using G10.Prototype.Computer;
using G10.Prototype.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace G10.Prototype.Tutorial
{
    /// <summary>Passive contextual chrome; no input, mission or navigation authority.</summary>
    public sealed class TutorialGuidanceView : MonoBehaviour
    {
        private CabinStationView cabin;
        private TutorialManager manager;
        private TutorialConfig config;
        private RectTransform compact, checklist, highlight;
        private TMP_Text hint, title, completedCount;
        private TMP_Text overviewTitle;
        private GameObject overviewPanel;
        private bool ownsOverviewTitle, overviewTitleWasEnabled;
        private readonly TMP_Text[] values = new TMP_Text[3];
        private readonly TMP_Text[] numbers = new TMP_Text[3];
        private readonly RectTransform[] fills = new RectTransform[3];
        private readonly GameObject[] checks = new GameObject[3];
        private readonly Dictionary<RectTransform, GameObject> locks = new();
        private readonly Dictionary<string, RectTransform> targets = new();
        private readonly Dictionary<RectTransform, TutorialStation> stationTargets = new();
        private int lockMask = -1;
        private float bindingRetryAt;
        private readonly float[] thresholds = new float[3], progress = new float[3];
        private static readonly string[] StationNames = { "NavigationHotspot", "MapHotspot", "RadarHotspot", "CameraHotspot", "MonitorHotspot", "CaptureHotspot" };
        private static readonly TutorialStation[] Stations = { TutorialStation.Helm, TutorialStation.Map, TutorialStation.Radar, TutorialStation.Camera, TutorialStation.Computer, TutorialStation.Capture };
        private static readonly string[] AppNames = { "PhotoLabIcon", "UpgradeIcon", "CargoIcon", "RestIcon", "JournalIcon" };
        private static readonly ComputerAppId[] Apps = { ComputerAppId.PhotoLab, ComputerAppId.Upgrade, ComputerAppId.Cargo, ComputerAppId.Rest, ComputerAppId.Journal };
        private TMP_FontAsset font;
        private Sprite cardSprite;
        private float cardPixelsPerUnit = 2;
        private string notice;
        private float noticeUntil;
        public string DisplayedHint => hint != null ? hint.text : "";
        public bool IsVisible => compact != null && (compact.gameObject.activeSelf || checklist.gameObject.activeSelf);
        public bool ChecklistVisible => checklist != null && checklist.gameObject.activeSelf;
        private static readonly Color Ink = new(.10f, .19f, .35f);

        public static TutorialGuidanceView Create(CabinStationView cabin, TutorialManager owner, TutorialConfig config)
        {
            if (cabin == null || config == null) return null;
            Transform frame = null;
            foreach (var rect in cabin.GetComponentsInChildren<RectTransform>(true))
                if (rect.name == "CabinFrame") { frame = rect; break; }
            if (frame == null) return null;
            var root = new GameObject("TutorialGuidance", typeof(RectTransform)); root.transform.SetParent(frame, false);
            var rectRoot = (RectTransform)root.transform;
            rectRoot.anchorMin = Vector2.zero; rectRoot.anchorMax = Vector2.one;
            rectRoot.offsetMin = rectRoot.offsetMax = Vector2.zero;
            var view = root.AddComponent<TutorialGuidanceView>();
            view.cabin = cabin; view.manager = owner; view.config = config;
            view.font = cabin.GetComponentInChildren<ComputerDesktopSkin>(true)?.font ?? TMP_Settings.defaultFontAsset;
            var card = cabin.RadarPanel?.transform.Find("RadarInfo")?.GetComponent<UnityEngine.UI.Image>()
                ?? cabin.NavigationPanel?.transform.Find("NavigationInfo")?.GetComponent<UnityEngine.UI.Image>();
            view.cardSprite = card?.sprite;
            view.cardPixelsPerUnit = card != null ? card.pixelsPerUnitMultiplier : 2;
            view.overviewPanel = cabin.GetComponent<WorldMapController>()?.worldPanel;
            view.overviewTitle = view.overviewPanel?.transform.Find("WatercolorHUD/Title")?.GetComponent<TMP_Text>();
            view.Build(); view.BindTargets(); view.Hide(); return view;
        }
        private void Build()
        {
            compact = Card(transform, "CompactHint", config.compactPosition, config.compactSize);
            hint = Text(compact, "Hint", new Vector2(20, 8), config.compactSize - new Vector2(40, 16), 24);
            checklist = Card(transform, "HelmChecklist", config.helmPosition, config.helmSize);
            title = Text(checklist, "Title", new Vector2(20, 16), new Vector2(config.helmSize.x - 82, 30), 22);
            title.text = "LÀM QUEN BÀN LÁI";
            title.fontStyle = FontStyles.Bold;
            completedCount = Text(checklist, "CompletedCount", new Vector2(config.helmSize.x - 58, 16), new Vector2(38, 30), 22);
            completedCount.alignment = TextAlignmentOptions.Right;
            Graphic(Rect(checklist, "HeaderDivider", new Vector2(20, 51), new Vector2(config.helmSize.x - 40, 1)), new Color(.2f, .3f, .5f, .15f));
            string[] names = { "Di chuyển", "Xoay hướng", "Đổi độ sâu" };
            for (int i = 0; i < 3; i++)
            {
                float y = 62 + i * 47;
                var icon = Rect(checklist, "Icon" + i, new Vector2(20, y + 2), new Vector2(26, 26));
                Graphic(icon, new Color(.2f, .3f, .5f, .09f));
                numbers[i] = Text(icon, "Number", Vector2.zero, icon.sizeDelta, 20);
                numbers[i].text = (i + 1).ToString(); numbers[i].alignment = TextAlignmentOptions.Center;
                Text(checklist, names[i], new Vector2(58, y), new Vector2(config.helmSize.x - 181, 28), 22).text = names[i];
                values[i] = Text(checklist, "Value" + i, new Vector2(config.helmSize.x - 119, y), new Vector2(99, 28), 21);
                values[i].alignment = TextAlignmentOptions.Right;
                var track = Rect(checklist, "Track" + i, new Vector2(58, y + 32), new Vector2(config.helmSize.x - 78, 4));
                Graphic(track, new Color(.2f, .3f, .5f, .2f));
                fills[i] = Rect(track, "Fill", Vector2.zero, track.sizeDelta); Graphic(fills[i], new Color(.18f, .65f, .56f));
                var check = Rect(icon, "Check" + i, new Vector2(3, 5), new Vector2(20, 18));
                // A check replaces the step number in the same column, never the progress text.
                Stroke(check, "Short", new Vector2(1, 8), new Vector2(8, 3), -45, new Color(.05f, .55f, .35f));
                Stroke(check, "Long", new Vector2(6, 14), new Vector2(16, 3), 45, new Color(.05f, .55f, .35f));
                checks[i] = check.gameObject;
            }
            Text(checklist, "EnergyHint", new Vector2(20, config.helmSize.y - 38), new Vector2(config.helmSize.x - 40, 22), 18)
                .text = "Điều khiển tàu tiêu hao ENERGY";
        }
        public void Notice(string value) { notice = value; noticeUntil = Time.unscaledTime + 3; Refresh(); }
        public void Hide()
        {
            compact?.gameObject.SetActive(false); checklist?.gameObject.SetActive(false);
            RestoreOverviewTitle();
            if (highlight != null) highlight.gameObject.SetActive(false);
            foreach (var badge in locks.Values) if (badge != null) badge.SetActive(false);
            lockMask = -1;
        }
        public void Refresh()
        {
            if (!manager.IsRunning) { Hide(); return; }
            var state = manager.Progress;
            thresholds[0]=config.MovementThreshold;thresholds[1]=config.HeadingThreshold;thresholds[2]=config.DepthThreshold;
            progress[0]=state.helmDistance;progress[1]=state.helmTurn;progress[2]=state.helmDepth;
            int done = 0;
            for (int i = 0; i < 3; i++)
            {
                bool complete = progress[i] >= thresholds[i]; if (complete) done++;
                checks[i].SetActive(complete);
                numbers[i].gameObject.SetActive(!complete);
                fills[i].sizeDelta = new Vector2((config.helmSize.x - 78) * Mathf.Clamp01(progress[i] / thresholds[i]), 4);
                string unit = i == 1 ? "°" : i == 2 ? " m" : "";
                values[i].text = $"{Mathf.Min(progress[i], thresholds[i]):0.#} / {thresholds[i]:0.#}{unit}";
            }
            completedCount.text = $"{done}/3";
            bool helm = manager.CurrentStep == TutorialStepId.Helm && cabin.Panels.CurrentPanel == cabin.NavigationPanel;
            checklist.gameObject.SetActive(helm);
            compact.gameObject.SetActive(!helm || Time.unscaledTime < noticeUntil);
            // The freed HUD width fits the instruction on one readable line instead of two cramped lines.
            hint.text = (Time.unscaledTime < noticeUntil ? notice : Hint(manager.CurrentStep, done)).Replace("\n", "  ·  ");
            // The overview has a centered heading instead of the three station tabs. Avoid drawing both in the same space.
            if (overviewTitle != null && compact.gameObject.activeSelf && cabin.Panels.CurrentPanel == overviewPanel)
            {
                if (!ownsOverviewTitle)
                {
                    overviewTitleWasEnabled = overviewTitle.enabled; ownsOverviewTitle = true;
                    overviewTitle.enabled = false;
                }
            }
            else RestoreOverviewTitle();
            UpdateLocks(); Highlight(Target(manager.CurrentStep));
        }
        private void RestoreOverviewTitle()
        {
            if (!ownsOverviewTitle) return;
            if (overviewTitle != null) overviewTitle.enabled = overviewTitleWasEnabled;
            ownsOverviewTitle = false;
        }
        private string Hint(TutorialStepId step, int done)
        {
            switch (step)
            {
                case TutorialStepId.Intro: return "Chào mừng lên tàu!\nLàm quen BÀN LÁI trước khi khảo sát.";
                case TutorialStepId.Helm: return $"BÀN LÁI · {done}/3 thao tác\nMở BÀN LÁI để xem checklist.";
                case TutorialStepId.Map: return "BẢN ĐỒ · mở bản đồ khảo sát\nĐọc tọa độ địa điểm để lên đường.";
                case TutorialStepId.Radar: return "RADAR · mở radar, bấm QUÉT\nMột lượt quét hợp lệ dùng 1 lượt radar.";
                case TutorialStepId.Camera: return ContactHint("CAMERA", "zone01-left", "Tiếp cận, hướng vào mục tiêu rồi chụp.");
                case TutorialStepId.PhotoLab: return "PHOTO LAB · chọn MISSION DATA DETECTED\nBấm SEND để gửi ảnh nhiệm vụ.";
                case TutorialStepId.Capture: return ContactHint("CAPTURE", "zone01-east", "Thắng minigame để lấy bản thiết kế.");
                case TutorialStepId.Upgrade: return manager.MissingUpgradeRequirements;
                default: return "";
            }
        }
        private string ContactHint(string device, string id, string action)
        {
            var survey = cabin.GetComponent<PhotoCaptureService>()?.survey;
            var poi = survey?.FindPoi(id);
            if (poi == null) return device + " · đến địa điểm khảo sát\n" + action;
            var position = survey.ContactPosition(poi);
            return $"{device} · ({position.x:0}, {position.y:0}) · {survey.DepthFor(poi):0} m\n{action}";
        }
        private RectTransform Find(string name)
        {
            if (targets.TryGetValue(name, out var target) && target != null) return target;
            if (Time.unscaledTime >= bindingRetryAt) BindTargets();
            return targets.TryGetValue(name, out target) ? target : null;
        }
        private void BindTargets()
        {
            bindingRetryAt = Time.unscaledTime + 1;
            foreach (var rect in cabin.GetComponentsInChildren<RectTransform>(true))
            {
                if (!targets.TryGetValue(rect.name, out var prior) || prior == null) targets[rect.name] = rect;
                if (rect.parent?.name != "WatercolorHUD" || rect.GetComponent<Button>() == null) continue;
                TutorialStation? station = rect.name switch { "Map" => TutorialStation.Map, "Helm" => TutorialStation.Helm,
                    "Radar" => TutorialStation.Radar, _ => null };
                if (station.HasValue) stationTargets[rect] = station.Value;
            }
            foreach (var entry in cabin.GetComponentsInChildren<UpgradeEntryConfig>(true))
                if(entry.UpgradeId=="ExpeditionModule")targets["TutorialExpeditionModule"]=(RectTransform)entry.transform;
            lockMask = -1;
        }
        private RectTransform Target(TutorialStepId step)
        {
            if(step==TutorialStepId.Upgrade)
            {
                var module=Find("TutorialExpeditionModule");
                if(module!=null&&module.gameObject.activeInHierarchy)return module;
            }
            if (step == TutorialStepId.Map || step == TutorialStepId.Radar)
            {
                if (step == TutorialStepId.Radar && cabin.Panels.CurrentPanel == cabin.RadarPanel)
                    return Find("ScanHotspot") ?? Find("RadarScanHotspot");
                var active = cabin.Panels.CurrentPanel?.transform.Find("WatercolorHUD/" + (step == TutorialStepId.Map ? "Map" : "Radar"));
                if (active != null) return (RectTransform)active;
            }
            return Find(step switch { TutorialStepId.Intro or TutorialStepId.Helm => "NavigationHotspot",
                TutorialStepId.Map => "MapHotspot", TutorialStepId.Radar => "RadarHotspot", TutorialStepId.Camera => "CameraHotspot",
                TutorialStepId.PhotoLab => "SendPhoto", TutorialStepId.Capture => "CaptureHotspot",
                TutorialStepId.Upgrade => "UpgradeIcon", _ => "" });
        }
        private void Highlight(RectTransform target)
        {
            if (highlight != null) highlight.gameObject.SetActive(false);
            if (target == null || !target.gameObject.activeInHierarchy) return;
            if (highlight == null)
            {
                highlight = Rect(target, "TutorialTarget", Vector2.zero, target.rect.size);
                Border(highlight, new Color(.1f, .85f, .64f, .8f));
            }
            highlight.SetParent(target, false); Stretch(highlight); highlight.gameObject.SetActive(true);
        }
        private void UpdateLocks()
        {
            int mask=0;
            for(int i=0;i<Stations.Length;i++)if(!manager.Allows(Stations[i]))mask|=1<<i;
            for(int i=0;i<Apps.Length;i++)if(!manager.AllowsComputerApp(Apps[i]))mask|=1<<(i+Stations.Length);
            if(mask==lockMask)return;
            for(int i=0;i<Stations.Length;i++)Lock(Find(StationNames[i]),(mask & 1<<i)!=0);
            foreach(var target in stationTargets)if(target.Key!=null)Lock(target.Key,!manager.Allows(target.Value));
            for(int i=0;i<Apps.Length;i++)Lock(Find(AppNames[i]),(mask & 1<<(i+Stations.Length))!=0);
            lockMask=mask;
        }
        private void Lock(RectTransform target, bool locked)
        {
            if (target == null) return;
            if (!locks.TryGetValue(target, out var badge))
            {
                var label = Text(target, "TutorialLock", new Vector2(4, 2), new Vector2(90, 25), 16);
                label.text = "KHÓA"; label.fontStyle = FontStyles.Bold; label.color = new Color(.46f, .2f, .3f);
                badge = label.gameObject; locks.Add(target, badge);
            }
            badge.SetActive(locked);
        }
        private RectTransform Card(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var rect = Rect(parent, name, position, size);
            var image = Graphic(rect, new Color(1, 1, 1, .96f));
            image.sprite = cardSprite; image.type = UnityEngine.UI.Image.Type.Sliced;
            // Match the radar card's border density; default slices consume a small hint's entire height.
            image.pixelsPerUnitMultiplier = cardPixelsPerUnit;
            return rect;
        }
        private TMP_Text Text(Transform parent, string name, Vector2 position, Vector2 size, float fontSize)
        {
            var text = Rect(parent, name, position, size).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.fontSize = fontSize; text.color = Ink; text.raycastTarget = false;
            text.enableAutoSizing = true; text.fontSizeMin = fontSize - 2; text.fontSizeMax = fontSize;
            text.textWrappingMode = TextWrappingModes.Normal; text.overflowMode = TextOverflowModes.Ellipsis;
            text.alignment = TextAlignmentOptions.Left; return text;
        }
        private static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform; rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(position.x, -position.y); rect.sizeDelta = size; return rect;
        }
        private static UnityEngine.UI.Image Graphic(RectTransform rect, Color color)
        { var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = color; image.raycastTarget = false; return image; }
        private static void Stretch(RectTransform rect)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        private static void Stroke(Transform parent, string name, Vector2 position, Vector2 size, float angle, Color color)
        { var rect = Rect(parent, name, position, size); rect.localEulerAngles = new Vector3(0, 0, angle); Graphic(rect, color); }
        private static void Border(RectTransform rect, Color color)
        {
            var top = Rect(rect, "Top", Vector2.zero, Vector2.zero); Stretch(top); top.anchorMin = new Vector2(0, 1); top.offsetMin = new Vector2(0, -3); Graphic(top, color);
            var bottom = Rect(rect, "Bottom", Vector2.zero, Vector2.zero); Stretch(bottom); bottom.anchorMax = new Vector2(1, 0); bottom.offsetMax = new Vector2(0, 3); Graphic(bottom, color);
            var left = Rect(rect, "Left", Vector2.zero, Vector2.zero); Stretch(left); left.anchorMax = new Vector2(0, 1); left.offsetMax = new Vector2(3, 0); Graphic(left, color);
            var right = Rect(rect, "Right", Vector2.zero, Vector2.zero); Stretch(right); right.anchorMin = new Vector2(1, 0); right.offsetMin = new Vector2(-3, 0); Graphic(right, color);
        }
        private void OnDestroy()
        { RestoreOverviewTitle(); if (highlight != null) Destroy(highlight.gameObject); foreach (var badge in locks.Values) if (badge != null) Destroy(badge); }
    }
}
