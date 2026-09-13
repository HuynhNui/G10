using UnityEngine;
using UnityEngine.UI;

namespace G10.Prototype.Computer
{
    public sealed class PhotoLabView : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour repositorySource;
        [SerializeField] private Text body;
        private IPhotoRepository repository;
        public RawImage preview;
        private int index = -1;
        public void Previous() { index=Mathf.Max(0,index-1); Refresh(); }
        public void Next() { index=Mathf.Min((repository?.Photos.Count??0)-1,index+1); Refresh(); }
        public string DisplayedText => body.text;
        private void Awake() => repository = repositorySource as IPhotoRepository;
        private void OnEnable() { index=-1; Refresh(); }
        public void Refresh()
        {
            if (repository == null) { body.text = "PHOTO STORAGE OFFLINE"; return; }
            body.text = repository.Photos.Count == 0 ? "NO IMAGES RECORDED" : $"{repository.Photos.Count} IMAGES RECORDED";
            body.text += repository.CameraOnline ? "\n\nCAMERA MODULE ONLINE" : "\n\nCAMERA MODULE OFFLINE";
            if(preview==null)return;
            preview.enabled=repository.Photos.Count>0;
            if(repository.Photos.Count==0)return;
            index=index<0?repository.Photos.Count-1:Mathf.Clamp(index,0,repository.Photos.Count-1);
            var photo=repository.Photos[index];preview.texture=photo.Image;
            body.text=$"ẢNH {index+1}/{repository.Photos.Count} • {photo.Result}\nX {photo.MapCoordinate.x:0.0} Y {photo.MapCoordinate.y:0.0} • {photo.Depth:0.0} m • {photo.Heading:0.0}°\n{photo.CapturedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}";
        }
    }
}
