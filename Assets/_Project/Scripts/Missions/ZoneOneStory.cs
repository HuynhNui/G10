using System;
using G10.Prototype.Navigation;
using G10.Prototype.UI;
using UnityEngine;

namespace G10.Prototype.Missions
{
    /// <summary>Zone01's ordered objectives. Radar/photo/capture owners report successful gameplay only.</summary>
    [DisallowMultipleComponent]
    public sealed class ZoneOneStory : MonoBehaviour
    {
        [Flags] public enum Progress
        { None = 0, RadarOne = 1, PhotoOne = 2, Analysis = 4, RadarTwo = 8, Tube = 16, Recipe = 32, PhotoTwo = 64, Adhesive = 128, Installed = 256 }
        public PhotoSurveyZone survey;
        public ZoneNavigation navigation;
        public CreatureInventory inventory;
        public CabinStationView cabin;
        public SurveyContentDefinition creatureOne, creatureTwo, emmaTube, adhesive;
        [Tooltip("POI IDs ordered left to right, not reordered by runtime position.")]
        public string[] poiIds = Array.Empty<string>();
        [Min(1)] public float upgradedMaximumDepth = 750;
        [Min(1)] public float hullBonus = 25;
        [SerializeField] private Progress progress;
        public int SavedProgress => (int)progress;
        public int ActiveLocation => !Has(Progress.Analysis) ? 0 : !Has(Progress.Recipe) ? 1 : 2;
        public bool Complete => Has(Progress.Installed);
        public int CompletedCount => CountProgress((int)progress);
        public static int CountProgress(int value) { int n = 0; for (int i = 0; i < 9; i++) if ((value & (1 << i)) != 0) n++; return n; }
        public bool Has(Progress flag) => (progress & flag) == flag;
        public MapPoi ActivePoi => survey == null || poiIds.Length <= ActiveLocation ? null : Array.Find(survey.locations, p => p != null && p.id == poiIds[ActiveLocation]);
        public SurveyContentDefinition Subject => ActiveLocation == 0 ? creatureOne : ActiveLocation == 1 ? emmaTube : creatureTwo;
        public string LastMessage { get; private set; }
        private const string AnalysisText = "Lily: “Nó không chống lại áp lực. Cấu trúc cơ thể của nó đang phân tán áp lực. Nếu có thể tái tạo cấu trúc này, chúng ta có thể sử dụng để gia cố vỏ tàu.”\nĐã giải mã hải lưu an toàn, mở tọa độ Rãnh San Hô Cổ.";
        private const string RecipeText = "Đã nạp bản thảo E.A: công thức liên kết mô sinh học chịu áp lực. Emma từng ở đây và nghiên cứu thích nghi với đại dương.\nMở công thức vỏ tàu và tọa độ Thềm Biển Sâu. Không có bản ghi âm.";
        public string DiscoveryNotes => Has(Progress.Recipe) ? RecipeText : Has(Progress.Analysis) ? AnalysisText :
            "Phân tích ảnh mô sinh học, nạp bản vẽ E.A, rồi kết hợp mẫu dịch để chế tạo vỏ tàu.";
        public bool SubjectPresent => ActiveLocation != 1 || !Has(Progress.Tube);
        public bool CanResearch => !Blocked && (Has(Progress.PhotoOne) && !Has(Progress.Analysis) || Has(Progress.Tube) && !Has(Progress.Recipe));
        public bool CanInstall => !Blocked && Has(Progress.Analysis | Progress.Recipe | Progress.PhotoTwo | Progress.Adhesive) && !Complete && inventory.Contains(adhesive.id);
        private bool Blocked => navigation == null || navigation.ExpeditionBlocked || cabin != null && cabin.Panels != null && cabin.Panels.IsModalOpen;
        public event Action Changed;
        private void Awake() { if (survey != null) survey.Story = this; }
        public void Restore(int value) { progress = (Progress)(value & 511); LastMessage = null; Sync(); }
        private void Sync()
        {
            if (survey != null) survey.creaturePresent = SubjectPresent;
            Changed?.Invoke();
        }
        private void Award(Progress flag, string message)
        { progress |= flag; LastMessage = message; Sync(); }
        public void RecordRadar()
        {
            if (Blocked) return;
            if (ActiveLocation == 0) Award(Progress.RadarOne, "Đã quét tín hiệu sinh học. Mở CAMERA, hướng mũi tàu về Sinh vật 001 và chụp ảnh.");
            else if (ActiveLocation == 1) Award(Progress.RadarTwo, "Tín hiệu kim loại nhân tạo trong kẽ đá. Dùng CÁNH TAY GẮP / THU THẬP.");
        }
        public void RecordPhoto()
        {
            if (Blocked || ActivePoi == null || !ActivePoi.Contains(navigation.Position)) return;
            if (ActiveLocation == 0 && Has(Progress.RadarOne)) Award(Progress.PhotoOne, "Ảnh Sinh vật 001 đã lưu. Về máy tính → RESEARCH để phân tích cấu trúc mô.");
            else if (ActiveLocation == 2) Award(Progress.PhotoTwo, "Đã chụp Sinh vật 002. Dùng cánh tay gắp thu mẫu dịch tiết; không bắt cả sinh vật.");
            else LastMessage = "Hãy dùng radar xác nhận tín hiệu tại địa điểm trước khi chụp.";
        }
        public CreatureCatcher.Result Collect(float depthTolerance)
        {
            if (Blocked || navigation.Ship.Hull <= 0) return CreatureCatcher.Result.Unavailable;
            if (ActivePoi == null || !ActivePoi.Contains(navigation.Position) || Mathf.Abs(navigation.Depth - survey.targetDepth) > depthTolerance ||
                !survey.Detectable(navigation, Mathf.Sqrt(ActivePoi.arrivalRadius * ActivePoi.arrivalRadius + depthTolerance * depthTolerance))) return CreatureCatcher.Result.Empty;
            bool tube = ActiveLocation == 1;
            if (ActiveLocation == 0 || tube && !Has(Progress.RadarTwo) || !tube && !Has(Progress.PhotoTwo)) return CreatureCatcher.Result.PhotoRequired;
            var flag = tube ? Progress.Tube : Progress.Adhesive;
            var item = tube ? emmaTube : adhesive;
            if (Has(flag) || item == null || inventory.Contains(item.id)) return CreatureCatcher.Result.Empty;
            if (inventory.IsFull) return CreatureCatcher.Result.Full;
            if (navigation.Ship.Captures <= 0) return CreatureCatcher.Result.NoCharges;
            if (!inventory.TryAdd(item.id, item.displayName, item.Image)) return CreatureCatcher.Result.Empty;
            navigation.Ship.TryUse(ShipCharge.Capture);
            Award(flag, tube ? "Thu được ống mẫu vỡ của Emma. Bản vẽ ép plastic ký E.A — không có ghi âm. Về RESEARCH để nạp bản vẽ." : "Đã thu mẫu dịch kết dính. Về RESEARCH để chế tạo và lắp lớp vỏ chịu áp lực Tầng 1.");
            return CreatureCatcher.Result.Caught;
        }
        public bool Research()
        {
            if (!CanResearch) return false;
            if (!Has(Progress.Analysis))
                Award(Progress.Analysis, AnalysisText);
            else if (inventory.Contains(emmaTube.id))
                Award(Progress.Recipe, RecipeText);
            else return false;
            return true;
        }
        public bool InstallHull()
        {
            if (!CanInstall) return false;
            var next = navigation.Ship.Export();
            next.maximumDepth = Mathf.Max(next.maximumDepth, upgradedMaximumDepth);
            next.hullCapacity += hullBonus;
            if (!next.IsValid || !inventory.Remove(adhesive.id)) return false;
            navigation.Ship.Restore(next);
            Award(Progress.Installed, "Đã chế tạo và lắp Lớp vỏ chịu áp lực Tầng 1. Mở Khu vực 2: Cổ Thụ Linh Hồn.\nVào MISSION LOG → NEXT ZONE để tiếp tục.");
            return true;
        }
        public string LocationText(int index)
        {
            if (index == 0) return "01 • RẠN TẢO ĐỎ\n" + Check(Progress.RadarOne, "Quét radar sinh học") + Check(Progress.PhotoOne, "Chụp Sinh vật 001 (Camera FPP)") + Check(Progress.Analysis, "Phân tích tại RESEARCH → mở địa điểm 2");
            if (index == 1) return "02 • RÃNH SAN HÔ CỔ" + (!Has(Progress.Analysis) ? " • CHƯA GIẢI MÃ\n" : "\n") + Check(Progress.RadarTwo, "Quét phản xạ kim loại") + Check(Progress.Tube, "Gắp ống mẫu vỡ của Emma") + Check(Progress.Recipe, "Nạp bản vẽ E.A → mở công thức + địa điểm 3");
            return "03 • THỀM BIỂN SÂU" + (!Has(Progress.Recipe) ? " • CHƯA GIẢI MÃ\n" : "\n") + Check(Progress.PhotoTwo, "Chụp Sinh vật 002") + Check(Progress.Adhesive, "Thu mẫu dịch kết dính") + Check(Progress.Installed, "Chế tạo/lắp vỏ Tầng 1 → Cổ Thụ Linh Hồn");
        }
        private string Check(Progress flag, string text) => (Has(flag) ? "[x] " : "[ ] ") + text + "\n";
        public string MissionText() => LocationText(ActiveLocation) + (Complete ? "\nHOÀN THÀNH ZONE 1" : "\nPhần thưởng: công thức vỏ chịu áp lực, +" + hullBonus + " vỏ tàu, độ sâu tối đa " + upgradedMaximumDepth + " m; mở Zone 2.");
    }
}
