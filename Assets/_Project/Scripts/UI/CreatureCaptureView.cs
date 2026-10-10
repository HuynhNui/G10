using G10.Prototype.Navigation;
using UnityEngine;
using UnityEngine.UI;

namespace G10.Prototype.UI
{
    public sealed class CreatureCaptureView : MonoBehaviour
    {
        public CreatureCatcher catcher;
        public Text status;
        private void OnEnable()
        {
            if (catcher != null) catcher.CaptureResolved += ShowResult;
            if (catcher != null && catcher.LastResult.HasValue) { ShowResult(catcher.LastResult.Value); return; }
            if (catcher != null && status != null)
                status.text = $"THIẾT BỊ SẴN SÀNG • TẦM BẮT {catcher.captureRadius:0} m\nDùng vòng xanh lá mạ trên radar để căn vị trí.\nĐộ sâu: TARGET DEPTH đến TARGET + {catcher.survey?.DeeperInteractionRange:0} M.";
        }
        private void OnDisable() { if (catcher != null) catcher.CaptureResolved -= ShowResult; }
        public void Catch() => ShowResult(catcher.TryCapture());
        private void ShowResult(CreatureCatcher.Result result)
        {
            if (status == null) return;
            status.text = result switch
            {
                CreatureCatcher.Result.Caught => catcher.LastSuccessText,
                CreatureCatcher.Result.NoCharges => "Đã hết lượt thiết bị bắt. Hãy nghỉ để tiếp tế.",
                CreatureCatcher.Result.Started or CreatureCatcher.Result.Busy => "Đang điều khiển thiết bị bắt…",
                CreatureCatcher.Result.Failed => "Bắt chưa thành công. Mục tiêu vẫn còn ở đây.\nNhấn BẮT để thử lại.",
                CreatureCatcher.Result.Cancelled => "Đã hủy lượt bắt. Mục tiêu vẫn còn ở đây.",
                _ => CreatureCatcher.FeedbackText(result)
            };
            if (catcher != null && catcher.navigation != null) status.text += "\nLƯỢT BẮT: " + (catcher.navigation.Ship.UnlimitedCaptureAttempts ? "UNLIMITED" : $"{catcher.navigation.Ship.Captures}/{catcher.navigation.Ship.CaptureCapacity}");
        }
    }
}
