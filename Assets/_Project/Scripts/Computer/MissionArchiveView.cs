using G10.Prototype.Missions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace G10.Prototype.Computer
{
    /// <summary>Read-only artist documents. Availability is derived from the existing timeline.</summary>
    public sealed class MissionArchiveView : MonoBehaviour
    {
        public GameObject reader;
        public Image document;
        public TMP_Text heading, transcription, pageLabel, paperA, paperB;
        public Sprite lily, emma, coordinatePaper;
        public MissionLogView log;
        public G10.Prototype.Navigation.ZoneMapConfig abyssMap;
        public bool IsOpen => reader != null && reader.activeSelf;
        public int Page { get; private set; }
        private ExpeditionLoop observedLoop;
        private ExpeditionLoop Loop => log != null ? log.Expedition : null;
        public bool EmmaAvailable => Loop?.ReadZoneState("Zone04")?.missionProgress?.completedObjectives?.Contains("Z4_L1_CAPTURE") == true;
        public bool CoordinatesAvailable => EmmaAvailable && Loop?.ReadZoneState("Zone04")?.zoneFourPresentation?.Has("fragment") == true;

        public void Open() => Show(0);
        public void Next() => Show((Page + 1) % 3);
        public void Previous() => Show((Page + 2) % 3);
        public void Close()
        {
            if (reader != null) reader.SetActive(false);
            if (log != null && log.Scroll != null) log.Scroll.gameObject.SetActive(true);
            if (observedLoop != null) observedLoop.Changed -= RefreshTimeline;
            observedLoop = null;
        }
        public bool TryHandleBack() { if (!IsOpen) return false; Close(); return true; }
        private void OnDisable() => Close();
        private void RefreshTimeline() { if (IsOpen) Show(Page); }
        public void Layout()
        {
            if (log?.Scroll == null) return;
            var rect = (RectTransform)log.Scroll.transform;
            rect.anchoredPosition = new Vector2(30, -68); rect.sizeDelta = new Vector2(1180, 457);
        }
        public void Show(int page)
        {
            Page = Mathf.Clamp(page, 0, 2);
            if (reader == null) return;
            if (observedLoop != Loop)
            {
                if (observedLoop != null) observedLoop.Changed -= RefreshTimeline;
                observedLoop = Loop;
                if (observedLoop != null) observedLoop.Changed += RefreshTimeline;
            }
            reader.SetActive(true); reader.transform.SetAsLastSibling();
            if (log?.Scroll != null) log.Scroll.gameObject.SetActive(false);
            bool available = Page == 0 || Page == 1 && EmmaAvailable || Page == 2 && CoordinatesAvailable;
            document.sprite = Page == 0 ? lily : Page == 1 ? emma : coordinatePaper;
            document.enabled = available; document.preserveAspect = true;
            paperA.gameObject.SetActive(Page == 2 && available); paperB.gameObject.SetActive(Page == 2 && available);
            pageLabel.text = $"{Page + 1} / 3";
            heading.text = Page == 0 ? "HỒ SƠ LILY" : Page == 1 ? "THẺ EMMA" : "TỌA ĐỘ PHỤC HỒI";
            transcription.text = !available ? "CHƯA CÓ DỮ LIỆU\n\nTài liệu sẽ xuất hiện khi dữ liệu liên quan được thu hồi trong chuyến khảo sát.\n\nĐọc mảnh dữ liệu trong NHIỆM VỤ sau khi thu hồi để giải mã tọa độ." : Page == 0 ?
                "Lily Darwin\n22 tuổi · Nhà sinh vật học\nNATRA24010102\n\nKhảo sát sự sống trên hành tinh nước và tìm hiểu về cái chết của phi hành gia Emma Armstrong.\n\nThời hạn chuyến đi: 25 ngày." : Page == 1 ?
                "Emma Armstrong\nMã nhân viên E2303872\nPhi hành gia · Nữ\nNATRA24010102\n\nNgày sinh: không xác định.\n\nThẻ được lưu cùng dữ liệu đã thu hồi tại xác tàu." : CoordinateText();
        }
        private string CoordinateText()
        {
            if (abyssMap == null) return "Dữ liệu bản đồ không khả dụng.";
            var a = System.Array.Find(abyssMap.locations, p => p != null && p.id == "zone04-l3");
            var b = System.Array.Find(abyssMap.locations, p => p != null && p.id == "zone04-l5");
            paperA.text = Line("TÍN HIỆU A", a); paperB.text = Line("TÍN HIỆU B", b);
            return "Hai tín hiệu trong mảnh giấy đã được phục hồi.\n\nChọn tiếp tục khám phá, tiếp cận tọa độ và dùng RADAR để xác minh.\n\nĐọc tài liệu không đánh dấu phát hiện hoặc gửi dữ liệu khảo sát.";
        }
        private static string Line(string label, G10.Prototype.Navigation.MapPoi poi) => poi == null ? label + " · —" :
            label + "\nX " + poi.mapPosition.x.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) +
            "\nY " + poi.mapPosition.y.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
    }
}
