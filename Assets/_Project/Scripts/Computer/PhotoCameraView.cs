using UnityEngine;
using UnityEngine.UI;

namespace G10.Prototype.Computer
{
    public sealed class PhotoCameraView : MonoBehaviour
    {
        public PhotoCaptureService capture;
        public RawImage preview;
        public Text status;
        private void OnEnable() { status.text="CAMERA SẴN SÀNG • Ảnh theo hướng mũi tàu\nBấm CHỤP để ghi ảnh; ảnh tự lưu vào PHOTO LAB."; }
        public void TakePhoto()
        {
            var photo=capture.Capture();if(photo==null)return;
            G10.Prototype.Audio.AudioManager.Instance?.PlayCameraShutter();
            preview.texture=photo.Image;preview.color=Color.white;
            status.text=$"{photo.Result} • X {photo.MapCoordinate.x:0.0} Y {photo.MapCoordinate.y:0.0} • {photo.Depth:0.0} m • {photo.Heading:0.0}°\n"+
                (capture.LastError??"ĐÃ LƯU • Xem lại trong COMPUTER → PHOTO LAB");
        }
    }
}
