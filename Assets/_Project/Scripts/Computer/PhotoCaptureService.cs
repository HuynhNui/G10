using System;
using System.Collections.Generic;
using System.IO;
using G10.Prototype.Navigation;
using G10.Prototype.Missions;
using UnityEngine;

namespace G10.Prototype.Computer
{
    public enum PhotoResultType { NoSubject, LifeDetected, GoodPhoto, TooFar, Obstructed, LowVisibility }
    /// <summary>Zone-local capture and bounded PNG archive. Uses the radar's same authored creature record.</summary>
    public sealed class PhotoCaptureService : MonoBehaviour, IPhotoSubmissionRepository
    {
        public ZoneNavigation navigation;
        public PhotoSurveyZone survey;
        public PhotoCaptureProfile profile;
        private readonly List<PhotoRecord> photos = new();
        private readonly PhotoLayerComposer composer = new();
        private float nextCapture;
        public bool CameraOnline => profile != null && navigation != null && survey != null && survey.locations != null && survey.locations.Length > 0;
        public IReadOnlyList<PhotoRecord> Photos => photos;
        public string LastError { get; private set; }
        public int TotalPhotosTaken { get; private set; }
        public static string ArchivePathOverride { get; set; }
        public string ArchivePath => ArchivePathOverride ?? Path.Combine(Application.persistentDataPath,"Zone01Photos");
        [Serializable] private sealed class Metadata
        {
            public string id, time, result, missionZoneId, missionPoiId, missionObjectiveId, missionTargetId;
            public float x,y,depth,heading;
            public bool mission;
        }
        private void Awake() => LoadArchive();
        public PhotoRecord Capture()
        {
            if (!CameraOnline || navigation.ExpeditionBlocked || Time.unscaledTime < nextCapture) return null;
            if (!navigation.Ship.TryUse(ShipCharge.Photo))
            { LastError = navigation.Ship.Hull <= 0 ? "TÀU ĐÃ HỎNG" : "HẾT LƯỢT CHỤP ẢNH"; return null; }
            nextCapture=Time.unscaledTime+.6f; LastError=null;
            Vector3 ship=navigation.WorldPosition; float heading=navigation.Heading;
            composer.Begin();
            var full=new Rect(0,0,1,1);
            composer.Draw(ship.z > -profile.shallowDepth ? profile.shallow : ship.z > -profile.deepDepth ? profile.mid : profile.deep, full,1);
            bool hasTarget = survey.TryGetPhotoContact(navigation, profile.visibleDistance, profile.fieldOfView, out var targetPoi, out var creature);
            bool local=hasTarget && Vector2.Distance(navigation.Position, targetPoi.mapPosition)<120;
            if(local) composer.Draw(profile.seabed,full,.65f);
            float distance=Vector3.Distance(ship,creature);
            Rect creatureRect=default;
            bool candidate=hasTarget && Project(ship,heading,creature,profile.creatureHeight,out creatureRect);
            float visibility=Mathf.Clamp01(1-distance/profile.visibleDistance*.7f) * (navigation.Depth>profile.deepDepth?.55f:.9f);
            // Fixed world props relative to the survey site; sort back-to-front with the subject.
            var layers=new List<Layer>(3);
            var subject = targetPoi != null ? survey.MissionRuntime?.ContentForPoi(targetPoi.id) : null;
            var subjectImage = subject != null ? subject.Image : null;
            if(candidate) layers.Add(new Layer { texture=subjectImage != null ? subjectImage : distance<12?profile.closeCreature:distance>40?profile.silhouette:profile.creature,rect=creatureRect,distance=distance,subject=true,alpha=visibility });
            AddProp(layers,ship,heading,creature+new Vector3(-9,7,-3),profile.kelp,20);
            AddProp(layers,ship,heading,creature+new Vector3(7,-8,-5),profile.rock,10);
            layers.Sort((a,b)=>b.distance.CompareTo(a.distance)); bool paintedSubject=false;
            foreach(var layer in layers)
            { composer.Draw(layer.texture,layer.rect,layer.alpha,layer.subject,!layer.subject&&paintedSubject);paintedSubject|=layer.subject; }
            composer.Draw(profile.fog,full,navigation.Depth>profile.deepDepth?.32f:.12f);
            composer.Draw(profile.particles,full,.13f);
            Texture2D image=composer.Finish(out float coverage,out float occlusion);
            bool terrainBlocked=candidate && !survey.Detectable(navigation,targetPoi,profile.visibleDistance);
            // Terrain walls block the camera, too. The local prop masks handle partial image occlusion.
            if(terrainBlocked)
            {
                Destroy(image);composer.Begin();composer.Draw(profile.deep,full,1);composer.Draw(profile.rock,new Rect(-.2f,-.2f,1.4f,1.4f),1);
                composer.Draw(profile.fog,full,.2f);image=composer.Finish(out _,out _);occlusion=1;
            }
            PhotoResultType result=!candidate||coverage<=0?PhotoResultType.NoSubject:occlusion>profile.maximumOcclusion?PhotoResultType.Obstructed:
                visibility<.25f?PhotoResultType.LowVisibility:coverage<profile.tooFarCoverage?PhotoResultType.TooFar:
                coverage>=profile.goodCoverage?PhotoResultType.GoodPhoto:PhotoResultType.LifeDetected;
            var runtime = survey.MissionRuntime;
            var objective = targetPoi != null ? runtime?.FindObjective(targetPoi.id, MissionObjectiveType.Photograph) : null;
            bool missionCandidate = objective != null && !string.IsNullOrEmpty(subject?.id) && objective.targetId == subject.id &&
                (result == PhotoResultType.GoodPhoto || result == PhotoResultType.LifeDetected);
            var record=new PhotoRecord(Guid.NewGuid().ToString("N"),image,image,DateTimeOffset.UtcNow,navigation.Position,false,
                navigation.Depth,heading,result.ToString(), missionCandidate ? runtime.config.zoneId : null,
                missionCandidate ? targetPoi.id : null, missionCandidate ? objective.id : null,
                missionCandidate ? objective.targetId : null);
            photos.Add(record);Save(record);Trim();
            TotalPhotosTaken++;
            return record;
        }
        public PhotoSubmissionState GetSubmissionState(PhotoRecord photo)
        {
            if (photo == null || !photos.Contains(photo)) return PhotoSubmissionState.Unavailable;
            if (photo.IsMissionPhoto) return PhotoSubmissionState.Submitted;
            if (string.IsNullOrEmpty(photo.MissionObjectiveId)) return PhotoSubmissionState.NotCandidate;
            var runtime = survey != null ? survey.MissionRuntime : null;
            if (runtime?.config == null || runtime.config.zoneId != photo.MissionZoneId ||
                string.IsNullOrEmpty(photo.MissionPoiId) || string.IsNullOrEmpty(photo.MissionTargetId)) return PhotoSubmissionState.Unavailable;
            var objective = runtime.FindObjective(photo.MissionPoiId, MissionObjectiveType.Photograph, photo.MissionTargetId);
            if (objective == null || objective.id != photo.MissionObjectiveId || objective.targetId != photo.MissionTargetId)
                return PhotoSubmissionState.Unavailable;
            if (runtime.HasObjective(objective.id)) return PhotoSubmissionState.ObjectiveCompleted;
            if (navigation == null || navigation.ExpeditionBlocked || navigation.Ship.Hull <= 0 || !runtime.IsPoiVisible(photo.MissionPoiId))
                return PhotoSubmissionState.Unavailable;
            return PhotoSubmissionState.Available;
        }
        public bool CanSubmitPhoto(PhotoRecord photo) => GetSubmissionState(photo) == PhotoSubmissionState.Available;
        public bool SubmitPhoto(PhotoRecord photo)
        {
            if (!CanSubmitPhoto(photo) || !survey.MissionRuntime.RecordObjective(photo.MissionPoiId,
                MissionObjectiveType.Photograph, photo.MissionTargetId)) return false;
            photo.MarkSubmitted();
            Save(photo, metadataOnly: true);
            var loop = FindAnyObjectByType<ExpeditionLoop>();
            if (loop != null && !loop.SaveCurrent()) LastError = loop.LastError;
            return true;
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
        private void Save(PhotoRecord r, bool metadataOnly = false)
        {
            try
            {
                Directory.CreateDirectory(ArchivePath);
                if (!metadataOnly) File.WriteAllBytes(Path.Combine(ArchivePath,r.Id+".png"),r.Image.EncodeToPNG());
                File.WriteAllText(Path.Combine(ArchivePath,r.Id+".json"),JsonUtility.ToJson(new Metadata {id=r.Id,time=r.CapturedAt.ToString("O"),result=r.Result,x=r.MapCoordinate.x,y=r.MapCoordinate.y,depth=r.Depth,heading=r.Heading,
                    mission=r.IsMissionPhoto, missionZoneId=r.MissionZoneId, missionPoiId=r.MissionPoiId,
                    missionObjectiveId=r.MissionObjectiveId, missionTargetId=r.MissionTargetId},true));
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
                        photos.Add(new PhotoRecord(Path.GetFileNameWithoutExtension(png),image,image,DateTimeOffset.Parse(m.time),new Vector2(m.x,m.y),m.mission,m.depth,m.heading,m.result,
                            m.missionZoneId,m.missionPoiId,m.missionObjectiveId,m.missionTargetId));
                    }
                    catch(Exception e) when(e is IOException||e is ArgumentException||e is FormatException) {if(image!=null)Destroy(image);Debug.LogWarning("Skipped unreadable photo archive entry.");}
                }
            }
            catch(Exception e) when(e is IOException||e is UnauthorizedAccessException) {LastError="Không đọc được thư mục ảnh.";}
        }
        private void Trim() {while(photos.Count>24){Destroy(photos[0].Image);photos.RemoveAt(0);} }
        public List<SavedPhoto> ExportPhotos()
        {
            var result = new List<SavedPhoto>();
            foreach (var p in photos) result.Add(new SavedPhoto { id=p.Id, time=p.CapturedAt.ToString("O"), result=p.Result,
                coordinate=p.MapCoordinate, depth=p.Depth, heading=p.Heading, mission=p.IsMissionPhoto,
                missionZoneId=p.MissionZoneId, missionPoiId=p.MissionPoiId, missionObjectiveId=p.MissionObjectiveId, missionTargetId=p.MissionTargetId,
                png=Convert.ToBase64String(p.Image.EncodeToPNG()) });
            return result;
        }
        public void RestorePhotos(List<SavedPhoto> saved, int total)
        {
            var restored = new List<PhotoRecord>();
            try
            {
                foreach (var p in saved)
                {
                    var capturedAt=DateTimeOffset.Parse(p.time);
                    var texture = new Texture2D(2,2);
                    try { if (!texture.LoadImage(Convert.FromBase64String(p.png))) throw new InvalidDataException("Invalid saved photo."); }
                    catch { Destroy(texture); throw; }
                    restored.Add(new PhotoRecord(p.id,texture,texture,capturedAt,p.coordinate,p.mission,p.depth,p.heading,p.result,
                        p.missionZoneId,p.missionPoiId,p.missionObjectiveId,p.missionTargetId));
                }
            }
            catch { foreach(var p in restored) Destroy(p.Image); throw; }
            foreach (var p in photos) if(p.Image!=null) Destroy(p.Image);
            photos.Clear(); photos.AddRange(restored); TotalPhotosTaken=total; nextCapture=0;
        }
        private void OnDestroy() {foreach(var photo in photos) if(photo.Image!=null)Destroy(photo.Image);}
    }
}
