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
                status.text = $"THIẾT BỊ SẴN SÀNG • TẦM BẮT {catcher.captureRadius:0} m\nDùng vòng xanh lá mạ trên radar để căn vị trí.\nĐộ sâu lệch tối đa {catcher.depthTolerance:0} m.";
        }
        private void OnDisable() { if (catcher != null) catcher.CaptureResolved -= ShowResult; }
        public void Catch() => ShowResult(catcher.TryCapture());
        private void ShowResult(CreatureCatcher.Result result)
        {
            if (status == null) return;
            status.text = result switch
            {
                CreatureCatcher.Result.Caught => "Đã bắt được sinh vật!\nHãy kiểm tra balô.",
                CreatureCatcher.Result.Empty => "Không có gì cả.",
                CreatureCatcher.Result.Full => "Balô đã đầy.",
                CreatureCatcher.Result.PhotoRequired => "Hãy chụp ảnh nhận diện sinh vật trước khi bắt.\nXem nhiệm vụ P01 trên bản đồ.",
                CreatureCatcher.Result.Started or CreatureCatcher.Result.Busy => "Đang điều khiển thiết bị bắt…",
                CreatureCatcher.Result.Failed => "Bắt chưa thành công. Sinh vật vẫn còn ở đây.\nNhấn BẮT để thử lại.",
                CreatureCatcher.Result.Cancelled => "Đã hủy lượt bắt. Sinh vật vẫn còn ở đây.",
                _ => "Thiết bị bắt chưa sẵn sàng."
            };
        }
    }
}
