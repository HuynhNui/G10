using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace G10.Prototype.Feedback
{
    public enum ZoneOneOpeningPhase { Boot, WaterContact, Instability, Impact, Blackout, Reboot, CabinReveal, Complete }

    /// <summary>Device-screen prologue. SceneFlow owns its clock/input lock; this view owns only temporary UI.</summary>
    public sealed class ZoneOneOpeningView : MonoBehaviour
    {
        private CanvasGroup screen, terminal;
        private TMP_Text title, body, status, counter;
        private Image progress, impactTint;
        private ImpactShake shake;
        private EffectVisualExecutor signal;
        private string currentBody;
        private float textStarted, previousTime = -1;
        public ZoneOneOpeningPhase Phase { get; private set; }
        public string BodyText => body != null ? body.text : "";
        public int VisibleCharacters => body != null ? body.maxVisibleCharacters : 0;
        public float ScreenOpacity => screen != null ? screen.alpha : 0;
        public float TerminalOpacity => terminal != null ? terminal.alpha : 0;
        public bool DistortionVisible => signal != null && signal.SignalVisible;
        public bool ImpactActive => shake != null && shake.IsShaking;
        private static readonly Color Ink = new(.69f, .83f, .82f);
        private static readonly Color Accent = new(.24f, .62f, .61f);
        private static readonly Color Warning = new(.94f, .46f, .25f);

        public static ZoneOneOpeningView Create(Transform owner, Material signalMaterial = null)
        {
            var root = new GameObject("ZoneOneOpening", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
            root.transform.SetParent(owner, false);
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 32001;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new(1920, 1080); scaler.matchWidthOrHeight = .5f;
            var view = root.AddComponent<ZoneOneOpeningView>();
            view.screen = root.GetComponent<CanvasGroup>(); view.screen.interactable = false; view.screen.blocksRaycasts = false;
            ImageFill(root.transform, "Black", Color.black);
            var panel = Rect(root.transform, "Terminal", new(1320, 720), Vector2.zero);
            view.terminal = panel.gameObject.AddComponent<CanvasGroup>();
            view.shake = panel.gameObject.AddComponent<ImpactShake>();
            ImageFill(panel, "Panel", new(.018f, .031f, .037f));
            Line(panel, "TopEdge", new(0, 358), new(1320, 2), Accent);
            Line(panel, "BottomEdge", new(0, -358), new(1320, 2), new(.15f, .3f, .32f));
            Label(panel, "Serial", new(-548, 303), new(930, 36), 20, Accent).text = "E.E.P.  /  VESSEL SYSTEMS  /  REMOTE EXPEDITION";
            view.title = Label(panel, "Title", new(-548, 224), new(1120, 76), 44, Ink);
            view.title.characterSpacing = 4;
            Line(panel, "Divider", new(0, 160), new(1096, 1), new(.13f, .28f, .3f));
            view.body = Label(panel, "Telemetry", new(-548, 123), new(1110, 370), 29, Ink);
            view.body.lineSpacing = 13;
            view.status = Label(panel, "State", new(-548, -258), new(940, 36), 23, Accent);
            view.counter = Label(panel, "Counter", new(414, -258), new(170, 36), 22, Accent);
            Line(panel, "Track", new(0, -310), new(1096, 4), new(.09f, .18f, .2f));
            var bar = Rect(panel, "Progress", new(1096, 4), new(-548, -310));
            bar.pivot = new(0, .5f); view.progress = bar.gameObject.AddComponent<Image>();
            view.progress.color = Accent; view.progress.raycastTarget = false;
            view.signal = panel.gameObject.AddComponent<EffectVisualExecutor>();
            view.signal.displayRegion = panel; view.signal.signalTemplate = signalMaterial;
            view.impactTint = ImageFill(root.transform, "ImpactTint", new(.38f, .055f, .02f, 0));
            view.Render(0);
            return view;
        }
        public static ZoneOneOpeningPhase PhaseAt(float seconds) => seconds >= 35 ? ZoneOneOpeningPhase.Complete :
            seconds >= 27 ? ZoneOneOpeningPhase.CabinReveal : seconds >= 21 ? ZoneOneOpeningPhase.Reboot :
            seconds >= 18.28f ? ZoneOneOpeningPhase.Blackout : seconds >= 18 ? ZoneOneOpeningPhase.Impact :
            seconds >= 12 ? ZoneOneOpeningPhase.Instability : seconds >= 4 ? ZoneOneOpeningPhase.WaterContact : ZoneOneOpeningPhase.Boot;

        public void Render(float seconds)
        {
            float t = Mathf.Max(0, seconds);
            var phase = PhaseAt(t);
            if (t < previousTime) { shake.Clear(); signal.Restore(); }
            if (previousTime < 18 && t >= 18 && t < 18.28f) shake.PlayProfile(ShakeProfile.ShortImpact, 8, .25f, Vector2.right, 147);
            previousTime = t; Phase = phase;
            screen.alpha = 1 - Mathf.InverseLerp(27, 30.5f, t);
            terminal.alpha = phase == ZoneOneOpeningPhase.Blackout ? 0 : phase >= ZoneOneOpeningPhase.CabinReveal ?
                1 - Mathf.InverseLerp(27, 28.5f, t) : 1;
            bool warning = phase == ZoneOneOpeningPhase.Instability || phase == ZoneOneOpeningPhase.Impact;
            title.color = status.color = warning ? Warning : Ink;
            progress.color = warning ? Warning : Accent;
            impactTint.color = new(.38f, .055f, .02f, phase == ZoneOneOpeningPhase.Impact ? .18f * (1 - Mathf.InverseLerp(18, 18.28f, t)) : 0);
            string heading, content, footer;
            float amount;
            switch (phase)
            {
                case ZoneOneOpeningPhase.Boot:
                    heading = "EARTH EXPEDITION PROGRAM";
                    content = "DESTINATION: [CLASSIFIED PLANET ID]\nMISSION TYPE: BIOLOGICAL SURVEY\n\nREQUIRED SPECIMENS: 09\nMISSION WINDOW: 25 DAYS";
                    footer = "STATUS: DESCENT IN PROGRESS"; amount = t / 4; break;
                case ZoneOneOpeningPhase.WaterContact:
                    heading = "WATER CONTACT CONFIRMED";
                    content = "HULL SEAL: CONFIRMED\nEXTERNAL PRESSURE: RISING\n\nDESCENT CONTROL: ACTIVE\nTELEMETRY LINK: RECEIVING";
                    footer = "ENTERING SUBSURFACE ENVIRONMENT"; amount = (t - 4) / 8; break;
                case ZoneOneOpeningPhase.Instability:
                case ZoneOneOpeningPhase.Impact:
                    heading = "WARNING";
                    content = "DESCENT STABILITY LOST\n\nCONTROL RESPONSE: DEGRADED\nSTRUCTURAL VIBRATION: RISING\n\nAWAITING EMERGENCY RESPONSE...";
                    footer = phase == ZoneOneOpeningPhase.Impact ? "TELEMETRY LOST" : "DESCENT CONTROL: UNSTABLE";
                    amount = 1; break;
                case ZoneOneOpeningPhase.Reboot:
                    heading = "EMERGENCY RESTART...";
                    content = t < 23 ? "POWER BUS: RECOVERING\nLIFE SUPPORT: INITIALIZING..." : t < 26 ?
                        "NAVIGATION: ONLINE\nLIFE SUPPORT: ONLINE\n\nEXTERNAL LIGHT: CHECKING...\nDEPTH SYSTEM: CHECKING..." :
                        "NAVIGATION: ONLINE\nLIFE SUPPORT: ONLINE\n\nEXTERNAL LIGHT: OFFLINE\nDEPTH SYSTEM: LIMITED";
                    footer = t < 26 ? "REBUILDING VESSEL TELEMETRY" : "REPAIR REQUIRED: EXTERNAL ILLUMINATION";
                    amount = (t - 21) / 6; break;
                case ZoneOneOpeningPhase.CabinReveal:
                    heading = "SYSTEMS RESTORED";
                    content = "DAY 01 / 25\n\nBIOLOGICAL SURVEY\n0 / 9 SPECIES DOCUMENTED\n\nREPAIR REQUIRED: EXTERNAL ILLUMINATION";
                    footer = "NAVIGATION CONTROL AVAILABLE"; amount = 1; break;
                default:
                    heading = content = footer = ""; amount = 0; break;
            }
            title.text = heading; status.text = footer;
            counter.text = phase == ZoneOneOpeningPhase.Reboot ? $"{Mathf.Clamp01(amount):P0}" : $"T + {t:00.0}";
            if (currentBody != content)
            {
                currentBody = content; body.text = content;
                textStarted = phase == ZoneOneOpeningPhase.Boot ? 0 : phase == ZoneOneOpeningPhase.WaterContact ? 4 :
                    phase == ZoneOneOpeningPhase.Reboot ? t >= 26 ? 26 : t >= 23 ? 23 : 21 : 27;
            }
            body.maxVisibleCharacters = warning ? int.MaxValue : Mathf.FloorToInt((t - textStarted) * (phase == ZoneOneOpeningPhase.Reboot && t >= 26 ? 200 : 95));
            progress.rectTransform.sizeDelta = new(1096 * Mathf.Clamp01(amount), 4);
            // Reuse the display-frame executor only during the descent accident. Normal survey screens remain clean.
            if (Application.isPlaying)
                signal.Render(warning && phase != ZoneOneOpeningPhase.Impact ? ScreenInterference.SubtleDisplayInstability : ScreenInterference.None,
                    Mathf.Lerp(.07f, .18f, Mathf.InverseLerp(12, 18, t)), t, 147, null, 0, Vector2.zero, Vector2.zero, warning);
        }
        private static RectTransform Rect(Transform parent, string name, Vector2 size, Vector2 position)
        {
            var obj = new GameObject(name, typeof(RectTransform)); obj.transform.SetParent(parent, false);
            var rect = (RectTransform)obj.transform; rect.anchorMin = rect.anchorMax = rect.pivot = new(.5f, .5f);
            rect.sizeDelta = size; rect.anchoredPosition = position; return rect;
        }
        private static Image ImageFill(Transform parent, string name, Color color)
        {
            var rect = Rect(parent, name, Vector2.zero, Vector2.zero); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero; var image = rect.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return image;
        }
        private static void Line(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        { var image = Rect(parent, name, size, position).gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; }
        private static TMP_Text Label(Transform parent, string name, Vector2 position, Vector2 size, float fontSize, Color color)
        {
            var rect = Rect(parent, name, size, position); rect.pivot = new(0, 1);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>(); text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize; text.color = color; text.raycastTarget = false;
            text.alignment = TextAlignmentOptions.TopLeft; text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }
        private void OnDisable() { shake?.Clear(); signal?.Restore(); }
    }
}
