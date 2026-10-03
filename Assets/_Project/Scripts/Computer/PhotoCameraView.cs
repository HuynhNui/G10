using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace G10.Prototype.Computer
{
    public sealed class PhotoCameraView : MonoBehaviour
    {
        public PhotoCaptureService capture;
        public RawImage preview;
        public Text status;
        [Header("Watercolor camera UI")]
        public TMP_Text photoCount;
        public TMP_Text metadata;
        public TMP_Text instruction;
        public Button shutter;
        public GameObject savedToast;
        private float refreshAfter;
        private float shutterReady;
        private float toastUntil;
        public void ResetZoneView()
        {
            refreshAfter = shutterReady = toastUntil = 0;
            if (preview != null) preview.texture = null;
            if (savedToast != null) savedToast.SetActive(false);
        }
        private void OnEnable()
        {
            SetStatus("Ảnh theo hướng mũi tàu.\nBấm CHỤP để lưu ảnh vào Photo Lab.");
            if (savedToast != null) savedToast.SetActive(false);
            if (preview != null && preview.texture == null && capture != null && capture.profile != null)
            {
                preview.texture = capture.profile.shallow;
                preview.color = Color.white;
            }
            refreshAfter = 0;
            RefreshReadouts();
        }
        private void Update()
        {
            if (savedToast != null && savedToast.activeSelf && Time.unscaledTime >= toastUntil) savedToast.SetActive(false);
            if (Time.unscaledTime < refreshAfter) return;
            refreshAfter = Time.unscaledTime + .1f;
            RefreshReadouts();
        }
        private void RefreshReadouts()
        {
            if (capture == null || capture.navigation == null) return;
            var nav=capture.navigation;
            if(photoCount!=null)photoCount.text=$"ẢNH CÒN {nav.Ship.Photos} / {nav.Ship.PhotoCapacity}";
            if(metadata!=null)metadata.text=$"X {nav.Position.x:0.0}   |   Y {nav.Position.y:0.0}   |   Z {nav.Depth:0.0} M   |   HƯỚNG {nav.Heading:0.0}°\n{capture.survey?.MissionRuntime?.config?.displayName ?? "ZONE"}";
            if(shutter!=null)shutter.interactable=capture.CameraOnline&&!nav.ExpeditionBlocked&&nav.Ship.Hull>0&&nav.Ship.Photos>0&&Time.unscaledTime>=shutterReady;
        }
        public void TakePhoto()
        {
            if(capture==null)return;
            var photo=capture.Capture();if(photo==null) { if(capture.LastError!=null) SetStatus(capture.LastError); RefreshReadouts(); return; }
            G10.Prototype.Audio.AudioManager.Instance?.PlayCameraShutter();
            preview.texture=photo.Image;preview.color=Color.white;
            SetStatus(capture.LastError??$"{ResultLabel(photo.Result)}\nĐã chụp • Xem lại trong Photo Lab");
            shutterReady=Time.unscaledTime+.6f;
            toastUntil=Time.unscaledTime+3;
            if(savedToast!=null)savedToast.SetActive(capture.LastError==null);
            RefreshReadouts();
        }
        private void SetStatus(string value)
        {
            if (instruction != null) instruction.text = value;
            if (status != null) status.text = value;
        }
        private static string ResultLabel(string result) => result switch
        {
            nameof(PhotoResultType.NoSubject) => "CHƯA THẤY CHỦ THỂ",
            nameof(PhotoResultType.LifeDetected) => "PHÁT HIỆN SINH VẬT",
            nameof(PhotoResultType.GoodPhoto) => "ẢNH RÕ",
            nameof(PhotoResultType.TooFar) => "CHỦ THỂ Ở QUÁ XA",
            nameof(PhotoResultType.Obstructed) => "TẦM NHÌN BỊ CHE",
            nameof(PhotoResultType.LowVisibility) => "TẦM NHÌN THẤP",
            _ => result
        };
    }
}
