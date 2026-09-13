using System;
using System.Collections.Generic;
using System.IO;
using G10.Prototype.Navigation;
using UnityEngine;

namespace G10.Prototype.Computer
{
    public enum PhotoResultType { NoSubject, LifeDetected, GoodPhoto, TooFar, Obstructed, LowVisibility }
    /// <summary>Zone-local capture and bounded PNG archive. Uses the radar's same authored creature record.</summary>
    public sealed class PhotoCaptureService : MonoBehaviour, IPhotoRepository
    {
        public ZoneNavigation navigation;
        public PhotoSurveyZone survey;
        public PhotoCaptureProfile profile;
        private readonly List<PhotoRecord> photos = new();
        private readonly PhotoLayerComposer composer = new();
        private float nextCapture;
        public bool CameraOnline => profile != null && navigation != null && survey != null;
        public IReadOnlyList<PhotoRecord> Photos => photos;
        public string LastError { get; private set; }
        public string ArchivePath => Path.Combine(Application.persistentDataPath,"Zone01Photos");
        [Serializable] private sealed class Metadata { public string id, time, result; public float x,y,depth,heading; }
        private void Awake() => LoadArchive();
        public PhotoRecord Capture()
        {
            if (!CameraOnline || Time.unscaledTime < nextCapture) return null;
            nextCapture=Time.unscaledTime+.6f; LastError=null;
            Vector3 ship=navigation.WorldPosition; float heading=navigation.Heading;
            composer.Begin();
            var full=new Rect(0,0,1,1);
            composer.Draw(ship.z > -profile.shallowDepth ? profile.shallow : ship.z > -profile.deepDepth ? profile.mid : profile.deep, full,1);
            bool local=Vector2.Distance(navigation.Position,survey.center)<120;
            if(local) composer.Draw(profile.seabed,full,.65f);
            Vector3 creature=survey.CreaturePosition;
            float distance=Vector3.Distance(ship,creature);
            Rect creatureRect=default;
            bool candidate=survey.creaturePresent && Project(ship,heading,creature,profile.creatureHeight,out creatureRect);
            float visibility=Mathf.Clamp01(1-distance/profile.visibleDistance*.7f) * (navigation.Depth>profile.deepDepth?.55f:.9f);
            // Fixed world props relative to the survey site; sort back-to-front with the subject.
            var layers=new List<Layer>(3);
            if(candidate) layers.Add(new Layer { texture=distance<12?profile.closeCreature:distance>40?profile.silhouette:profile.creature,rect=creatureRect,distance=distance,subject=true,alpha=visibility });
            AddProp(layers,ship,heading,creature+new Vector3(-9,7,-3),profile.kelp,20);
            AddProp(layers,ship,heading,creature+new Vector3(7,-8,-5),profile.rock,10);
            layers.Sort((a,b)=>b.distance.CompareTo(a.distance)); bool paintedSubject=false;
            foreach(var layer in layers)
            { composer.Draw(layer.texture,layer.rect,layer.alpha,layer.subject,!layer.subject&&paintedSubject);paintedSubject|=layer.subject; }
            composer.Draw(profile.fog,full,navigation.Depth>profile.deepDepth?.32f:.12f);
            composer.Draw(profile.particles,full,.13f);
            Texture2D image=composer.Finish(out float coverage,out float occlusion);
            bool terrainBlocked=candidate && !survey.Detectable(navigation,profile.visibleDistance);
            // Terrain walls block the camera, too. The local prop masks handle partial image occlusion.
            if(terrainBlocked)
            {
                Destroy(image);composer.Begin();composer.Draw(profile.deep,full,1);composer.Draw(profile.rock,new Rect(-.2f,-.2f,1.4f,1.4f),1);
                composer.Draw(profile.fog,full,.2f);image=composer.Finish(out _,out _);occlusion=1;
            }
            PhotoResultType result=!candidate||coverage<=0?PhotoResultType.NoSubject:occlusion>profile.maximumOcclusion?PhotoResultType.Obstructed:
                visibility<.25f?PhotoResultType.LowVisibility:coverage<profile.tooFarCoverage?PhotoResultType.TooFar:
                coverage>=profile.goodCoverage?PhotoResultType.GoodPhoto:PhotoResultType.LifeDetected;
            var record=new PhotoRecord(Guid.NewGuid().ToString("N"),image,image,DateTimeOffset.UtcNow,navigation.Position,false,
                navigation.Depth,heading,result.ToString());
            photos.Add(record);Save(record);Trim();
            if (result == PhotoResultType.GoodPhoto || result == PhotoResultType.LifeDetected)
                survey.CompleteTask(PhotoSurveyZone.TaskKind.Photograph);
            return record;
        }
        private struct Layer { public Texture2D texture; public Rect rect; public float distance,alpha;public bool subject; }
        private void AddProp(List<Layer> layers,Vector3 ship,float heading,Vector3 world,Texture2D texture,float height)
        { if(Project(ship,heading,world,height,out var rect)) layers.Add(new Layer {texture=texture,rect=rect,distance=Vector3.Distance(ship,world),alpha=.9f}); }
        private bool Project(Vector3 ship,float heading,Vector3 target,float height,out Rect rect)
        {
            Vector3 delta=target-ship;float planar=new Vector2(delta.x,delta.y).magnitude;
            float angle=Mathf.DeltaAngle(heading,Mathf.Atan2(delta.x,delta.y)*Mathf.Rad2Deg);
            float h=Mathf.Clamp(height/Mathf.Max(5,planar),.03f,1.3f), w=h*.48f;
            float x=.5f+angle/profile.fieldOfView;
            // Screen-Y artistic projection intentionally deferred: depth controls visibility instead.
            rect=new Rect(x-w/2,.5f-h/2,w,h);
            return delta.magnitude<=profile.visibleDistance && Mathf.Abs(delta.z)<=30 && Mathf.Abs(angle)<=profile.fieldOfView/2;
        }
        private void Save(PhotoRecord r)
        {
            try
            {
                Directory.CreateDirectory(ArchivePath);
                File.WriteAllBytes(Path.Combine(ArchivePath,r.Id+".png"),r.Image.EncodeToPNG());
                File.WriteAllText(Path.Combine(ArchivePath,r.Id+".json"),JsonUtility.ToJson(new Metadata {id=r.Id,time=r.CapturedAt.ToString("O"),result=r.Result,x=r.MapCoordinate.x,y=r.MapCoordinate.y,depth=r.Depth,heading=r.Heading},true));
            }
            catch(Exception e) when(e is IOException || e is UnauthorizedAccessException) { LastError="Không lưu được ảnh ra ổ đĩa; ảnh vẫn còn trong phiên này.";Debug.LogWarning(LastError); }
        }
        private void LoadArchive()
        {
            if(!Directory.Exists(ArchivePath)) return;
            try
            {
                var files=new DirectoryInfo(ArchivePath).GetFiles("*.json");Array.Sort(files,(a,b)=>a.LastWriteTimeUtc.CompareTo(b.LastWriteTimeUtc));
                for(int i=Mathf.Max(0,files.Length-24);i<files.Length;i++)
                {
                    Texture2D image=null;
                    try
                    {
                        var m=JsonUtility.FromJson<Metadata>(File.ReadAllText(files[i].FullName));
                        string png=Path.ChangeExtension(files[i].FullName,".png"); if(m==null||!File.Exists(png))continue;
                        image=new Texture2D(2,2);if(!image.LoadImage(File.ReadAllBytes(png))) {Destroy(image);continue;}
                        photos.Add(new PhotoRecord(Path.GetFileNameWithoutExtension(png),image,image,DateTimeOffset.Parse(m.time),new Vector2(m.x,m.y),false,m.depth,m.heading,m.result));
                    }
                    catch(Exception e) when(e is IOException||e is ArgumentException||e is FormatException) {if(image!=null)Destroy(image);Debug.LogWarning("Skipped unreadable photo archive entry.");}
                }
            }
            catch(Exception e) when(e is IOException||e is UnauthorizedAccessException) {LastError="Không đọc được thư mục ảnh.";}
        }
        private void Trim() {while(photos.Count>24){Destroy(photos[0].Image);photos.RemoveAt(0);} }
        private void OnDestroy() {foreach(var photo in photos) if(photo.Image!=null)Destroy(photo.Image);}
    }
}
