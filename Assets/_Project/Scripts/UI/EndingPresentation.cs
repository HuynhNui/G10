using System;
using System.Linq;
using G10.Prototype.Audio;
using G10.Prototype.Computer;
using G10.Prototype.Feedback;
using G10.Prototype.Missions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace G10.Prototype.UI
{
    /// <summary>Scene-local ending reads committed receipts; never chooses or records an ending.</summary>
    [DisallowMultipleComponent]
    public sealed class EndingPresentation : MonoBehaviour
    {
        [SerializeField] private Text title;
        [SerializeField] private TMP_Text body;
        public EffectDirector effects;
        public EffectSequence normalSequence, hiddenSequence;
        public ZoneMissionConfig[] surveyConfigs = Array.Empty<ZoneMissionConfig>();
        public TMP_FontAsset presentationFont;
        private TMP_Text survey, caption;
        private RectTransform diagram;
        private Image line;
        private RawImage depthVeil;
        private Texture2D veilMask;
        private readonly Image[] marks = new Image[11];
        private EffectHandle sequence;
        private Color titleColor;
        private bool capturedTitle, ownsAudio, applicationPaused, resumePending;
        private float elapsed;
        private int visibleReceipts;
        public string EndingId { get; private set; }
        public string TitleText => title != null ? title.text : "";
        public string SummaryText => body != null ? body.text : "";
        public string SurveyText => survey != null ? survey.text : "";
        public float Elapsed => elapsed;
        public bool IsPlaying => effects != null && effects.IsActive(sequence);

        public void Show(string endingId)
        {
            StopSequence();
            resumePending = false;
            EndingId = string.Equals(endingId, "hidden", StringComparison.OrdinalIgnoreCase) ? "hidden" : "normal";
            if (title == null)
                foreach (var label in GetComponentsInChildren<Text>(true))
                    if (label.name == "Title") { title = label; break; }
            if (title == null) { Debug.LogError("EndingPresentation requires the existing Ending canvas Title.", this); return; }
            if (!capturedTitle) { titleColor = title.color; capturedTitle = true; }
            bool hidden = EndingId == "hidden";
            title.text = hidden ? "HIDDEN ENDING" : "EXPEDITION COMPLETE";
            if (body == null) body = Label("EndingSummary", new Vector2(0, 22), new Vector2(1000, 52), 21);
            body.text = hidden ? "Dữ liệu đã được lưu.\nTín hiệu vẫn tiếp diễn." : "Khảo sát sinh học đã được lưu.\nChuyến thám hiểm kết thúc.";
            if (survey == null) survey = Label("SurveyReceipt", new Vector2(0, 155), new Vector2(1000, 35), 20);
            if (caption == null) caption = Label("EndingCaption", new Vector2(0, -150), new Vector2(1000, 40), 16);
            var counts = SavedCounts();
            int count = hidden ? counts.Completed : counts.MandatoryCompleted;
            int total = hidden ? counts.Total : counts.MandatoryTotal;
            visibleReceipts = Mathf.Clamp(count, 0, 11);
            survey.text = total > 0 ? $"BIOLOGICAL SURVEY — {count:00} / {total:00}" : "BIOLOGICAL SURVEY — ARCHIVED";
            BuildDiagram(); diagram.gameObject.SetActive(true);
            BuildDepthVeil(); depthVeil.gameObject.SetActive(true);
            elapsed = 0;
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.AcquireZoneAudio(GetInstanceID(), null, null); ownsAudio = true;
                effects?.BindAudio(AudioManager.Instance);
            }
            sequence = effects != null ? effects.PlaySequence(hidden ? hiddenSequence : normalSequence,
                condition: () => isActiveAndEnabled && !applicationPaused && Time.timeScale > 0) : default;
            Render(0);
        }
        private PhotoSurveyCounts SavedCounts()
        {
            if (!ExpeditionSaveStore.TryRead(out var save, out _) || save?.current == null)
                return ExpeditionPhotoSurvey.Count(surveyConfigs, (_, _) => false);
            return ExpeditionPhotoSurvey.Count(surveyConfigs, (zone, objective) =>
                save.current.zones?.FirstOrDefault(z => z.zone == zone)?.missionProgress?.completedObjectives?.Contains(objective) == true);
        }
        private TMP_Text Label(string name, Vector2 position, Vector2 size, float fontSize)
        {
            var node = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); node.transform.SetParent(title.transform.parent, false);
            var text = node.GetComponent<TextMeshProUGUI>(); text.font = presentationFont != null ? presentationFont : TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize; text.color = new Color(.65f, .80f, .80f); text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false; text.richText = false; text.textWrappingMode = TextWrappingModes.NoWrap;
            text.outlineColor = new Color32(4, 17, 36, 255); text.outlineWidth = .18f;
            var rect = text.rectTransform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position; rect.sizeDelta = size; return text;
        }
        private void BuildDiagram()
        {
            if (diagram != null) return;
            diagram = (RectTransform)new GameObject("ArchivedSurveyDiagram", typeof(RectTransform)).transform;
            diagram.SetParent(title.transform.parent, false); diagram.anchorMin = diagram.anchorMax = diagram.pivot = new Vector2(.5f, .5f);
            diagram.anchoredPosition = new Vector2(0, 285); diagram.sizeDelta = new Vector2(560, 105);
            line = Mark("SurveyBaseline", new Vector2(0, -12), new Vector2(540, 1));
            for (int i = 0; i < marks.Length; i++)
                marks[i] = Mark("Receipt" + (i + 1).ToString("00"), new Vector2(-240 + i * 48, -12), new Vector2(2, 20));
        }
        private void BuildDepthVeil()
        {
            if (depthVeil != null) return;
            // A feathered local presentation area, not a fullscreen blackout or a replacement background.
            var node = new GameObject("EndingDepthVeil", typeof(RectTransform), typeof(RawImage));
            node.transform.SetParent(title.transform.parent, false); node.transform.SetSiblingIndex(1);
            depthVeil = node.GetComponent<RawImage>(); depthVeil.raycastTarget = false;
            var rect = depthVeil.rectTransform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(0, 85); rect.sizeDelta = new Vector2(1120, 640);
            const int width = 96, height = 64; var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                var p = new Vector2((x + .5f) / width * 2 - 1, (y + .5f) / height * 2 - 1);
                float alpha = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(1, .2f, p.magnitude));
                pixels[y * width + x] = new Color(1, 1, 1, alpha);
            }
            veilMask = new Texture2D(width, height, TextureFormat.RGBA32, false)
            { name = "Ending local feathered depth", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            veilMask.SetPixels32(pixels); veilMask.Apply(false); depthVeil.texture = veilMask;
        }
        private Image Mark(string name, Vector2 position, Vector2 size)
        {
            var node = new GameObject(name, typeof(RectTransform), typeof(Image)); node.transform.SetParent(diagram, false);
            var image = node.GetComponent<Image>(); image.raycastTarget = false;
            var rect = image.rectTransform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position; rect.sizeDelta = size; return image;
        }
        private void LateUpdate()
        {
            if (EndingId == null || title == null || body == null) return;
            if (applicationPaused || Time.timeScale <= 0)
            {
                if (IsPlaying) { if (effects.TryGetSequenceTime(sequence, out var pausedAt)) elapsed = pausedAt; resumePending = true; }
                // The central director may have observed pause earlier in this frame.
                else if (elapsed < (EndingId == "hidden" ? 16 : 12)) resumePending = true;
                return;
            }
            if (resumePending)
            {
                resumePending = false;
                var authored = EndingId == "hidden" ? hiddenSequence : normalSequence;
                if (effects != null && authored != null && elapsed < authored.Duration)
                    sequence = effects.PlaySequence(authored, condition: () => isActiveAndEnabled && !applicationPaused && Time.timeScale > 0, startAtSeconds: elapsed);
            }
            if (effects != null && effects.TryGetSequenceTime(sequence, out var age)) elapsed = age;
            else elapsed = EndingId == "hidden" ? 16 : 12;
            Render(elapsed);
        }
        private void Render(float age)
        {
            bool hidden = EndingId == "hidden";
            depthVeil.color = new Color(.015f, .045f, .09f, hidden ? Mathf.Lerp(.20f, .68f, Mathf.Clamp01(age / 8)) : .08f);
            title.color = new Color(titleColor.r, titleColor.g, titleColor.b, Mathf.Lerp(.65f, titleColor.a, Mathf.Clamp01(age / 2)));
            body.color = new Color(.83f, .94f, .97f, Mathf.Lerp(.70f, 1, Mathf.Clamp01(age / 3)));
            survey.color = new Color(.78f, .92f, .95f, Mathf.Lerp(.75f, 1, Mathf.Clamp01(age / 2)));
            caption.text = hidden ? age < 6 ? "TÍN HIỆU NGOÀI VÙNG KHẢO SÁT" : age < 12 ? "ĐÃ GHI NHẬN" : "—" : "NHIỆM VỤ KHẢO SÁT HOÀN TẤT";
            caption.color = new Color(.75f, .87f, .90f, hidden && age >= 13 ? .45f : .95f);
            line.color = hidden ? new Color(.4f, .64f, .73f, .3f) : new Color(.02f, .08f, .14f, .38f);
            for (int i = 0; i < marks.Length; i++)
            {
                bool extra = i >= 9; float reveal = Mathf.Clamp01((age - (extra ? i == 9 ? 6.4f : 9.6f : i * .24f)) / 1.2f);
                if (!hidden && extra) reveal = 0;
                if (i >= visibleReceipts) reveal = 0;
                if (hidden && extra) reveal *= 1 - Mathf.Clamp01((age - 12.4f) / 1.3f);
                var rect = marks[i].rectTransform;
                rect.anchoredPosition = new Vector2(hidden ? -240 + i * 48 : -192 + i * 48, extra ? i == 9 ? 19 : -29 : -12);
                rect.sizeDelta = new Vector2(extra ? 3 : 2, extra ? 7 : 20);
                marks[i].color = hidden ? new Color(.5f, .74f, .81f, reveal * .65f) : new Color(.02f, .08f, .14f, reveal * .85f);
            }
        }
        private void StopSequence() { effects?.Stop(sequence); sequence = default; }
        private void OnApplicationPause(bool value)
        {
            applicationPaused = value;
            if (value && EndingId != null)
            {
                if (effects != null && effects.TryGetSequenceTime(sequence, out var age)) elapsed = age;
                resumePending = elapsed < (EndingId == "hidden" ? 16 : 12); StopSequence();
            }
        }
        private void OnDisable()
        {
            StopSequence(); resumePending = false; if (ownsAudio) AudioManager.Instance?.ReleaseZoneAudio(GetInstanceID()); ownsAudio = false;
            if (title != null && capturedTitle) title.color = titleColor;
            if (diagram != null) diagram.gameObject.SetActive(false);
            if (depthVeil != null) depthVeil.gameObject.SetActive(false);
        }
        private void OnDestroy()
        { if (veilMask != null) { if (Application.isPlaying) Destroy(veilMask); else DestroyImmediate(veilMask); } }
    }
}
