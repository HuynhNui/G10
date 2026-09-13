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
            if (catcher != null && status != null)
                status.text = $"THIẾT BỊ SẴN SÀNG • TẦM BẮT {catcher.captureRadius:0} m\nDùng vòng xanh lá mạ trên radar để căn vị trí.\nĐộ sâu lệch tối đa {catcher.depthTolerance:0} m.";
        }
        public void Catch()
        {
            status.text = catcher.TryCapture() switch
            {
                CreatureCatcher.Result.Caught => "Đã bắt được sinh vật!\nHãy kiểm tra balô.",
                CreatureCatcher.Result.Empty => "Không có gì cả.",
                CreatureCatcher.Result.Full => "Balô đã đầy.",
                CreatureCatcher.Result.PhotoRequired => "Hãy chụp ảnh nhận diện sinh vật trước khi bắt.\nXem nhiệm vụ P01 trên bản đồ.",
                _ => "Thiết bị bắt chưa sẵn sàng."
            };
        }
    }
}
