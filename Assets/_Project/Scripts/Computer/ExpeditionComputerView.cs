using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace G10.Prototype.Computer
{
    /// <summary>Two small apps inside the existing computer; no separate Canvas or modal manager.</summary>
    public sealed class ExpeditionComputerView : MonoBehaviour
    {
        private ExpeditionLoop loop;
        private ComputerScreenController screen;
        private Text restText, journalText;
        private Button restButton, restoreButton, confirmRest, confirmRestore, nextZone;
        private int page, pendingDay = -1;
        private bool restPending, built;
        private float refreshAt;
        public string JournalText => journalText != null ? journalText.text : "";
        public string RestText => restText != null ? restText.text : "";
        public void Initialize(ExpeditionLoop owner, ComputerScreenController controller)
        {
            loop=owner; screen=controller;
            if (built) return;
            built=true;
            ButtonAt(screen.Desktop.transform,"JournalIcon","JOURNAL",120,540,800,100,OpenJournal);
            ButtonAt(screen.Desktop.transform,"RestIcon","REST",1000,540,800,100,OpenRest);
            var rest=Panel("RestApp",ComputerAppId.Rest);
            restText=Label(rest,"RestDetails",90,110,1720,380,30);
            restButton=ButtonAt(rest,"RestRequest","REST",90,540,480,85,RequestRest);
            confirmRest=ButtonAt(rest,"ConfirmRest","CONFIRM REST",600,540,520,85,ConfirmRest);
            ButtonAt(rest,"CancelRest","CANCEL",1160,540,540,85,CancelConfirmation);
            var journal=Panel("JournalApp",ComputerAppId.Journal);
            journalText=Label(journal,"JournalDetails",90,95,1720,445,28);
            ButtonAt(journal,"PreviousDay","PREVIOUS",90,550,320,85,()=> { page--; CancelConfirmation(); Refresh(); });
            ButtonAt(journal,"NextDay","NEXT",440,550,320,85,()=> { page++; CancelConfirmation(); Refresh(); });
            restoreButton=ButtonAt(journal,"RestoreDay","RESTORE",800,550,350,85,RequestRestore);
            confirmRestore=ButtonAt(journal,"ConfirmRestore","CONFIRM RESTORE",800,550,550,85,ConfirmRestore);
            ButtonAt(journal,"CancelRestore","CANCEL",1390,550,330,85,CancelConfirmation);
            var mission=System.Array.Find(screen.Apps,app=>app.id==ComputerAppId.MissionLog);
            if(mission!=null)
                nextZone=ButtonAt(mission.panel.transform,"NextExpeditionZone","NEXT ZONE",1100,580,610,85,NextZone);
            Refresh();
        }
        private void NextZone()
        {
            var flow=G10.Prototype.Core.SceneFlowController.Instance;
            if(flow==null || !loop.RequiredObjectivesComplete || loop.Blocked)return;
            if(loop.Zone=="Zone04")flow.LoadEnding();
            else if(int.TryParse(loop.Zone.Substring(4),out int index))flow.LoadZone($"Zone{index+1:00}");
        }
        private Transform Panel(string name, ComputerAppId id)
        {
            var panel=Box(transform,name,0,220,1920,760);
            Label(panel,"Title",90,0,1400,70,36).text=id.ToString().ToUpperInvariant();
            ButtonAt(panel,"Back","DESKTOP",1510,0,300,70,()=>{ CancelConfirmation();screen.ShowDesktop(); });
            screen.RegisterApp(id,panel.gameObject); panel.gameObject.SetActive(false); return panel;
        }
        public void OpenJournal() { page=loop.Journal.Count-1;CancelConfirmation();screen.OpenApp(ComputerAppId.Journal);Refresh(); }
        public void OpenRest() { CancelConfirmation();screen.OpenApp(ComputerAppId.Rest);Refresh(); }
        public void ShowFailure() { OpenJournal(); }
        public void RequestRest() { if(!loop.CanRest)return; restPending=true;pendingDay=loop.Day;Refresh(); }
        public void ConfirmRest()
        {
            if(!restPending || pendingDay!=loop.Day)return;
            restPending=false; pendingDay=-1;
            if(loop.Rest() && !loop.Failed) OpenJournal();
            Refresh();
        }
        public void RequestRestore()
        {
            if(loop.Journal.Count==0)return;
            pendingDay=loop.Journal[page].day; Refresh();
        }
        public void ConfirmRestore()
        {
            if(pendingDay<0 || restPending)return;
            int day=pendingDay;pendingDay=-1;
            loop.RestoreDay(day);Refresh();
        }
        public void CancelConfirmation() { restPending=false;pendingDay=-1;Refresh(); }
        private void OnDisable() { restPending=false;pendingDay=-1; }
        private void Update() { if(built && Time.unscaledTime>=refreshAt) { refreshAt=Time.unscaledTime+.25f;Refresh(); } }
        public void Refresh()
        {
            if(restText==null || journalText==null)return;
            if(nextZone!=null)nextZone.interactable=loop.RequiredObjectivesComplete && !loop.Blocked &&
                G10.Prototype.Core.SceneFlowController.Instance!=null && !G10.Prototype.Core.SceneFlowController.Instance.IsTransitioning;
            restText.text=loop.StatusText()+"\n\n"+(restPending ? $"Kết thúc ngày {loop.Day:00} và nghỉ đến ngày {loop.Day+1:00}?" :
                "Về khu nghỉ để ghi nhật ký và sang ngày mới.\nKho đồ, ảnh và tiến trình nhiệm vụ được giữ lại.") + loop.RestAreasText();
            if(!loop.InRestArea) restText.text+="\nREST UNAVAILABLE — RETURN TO REST AREA";
            if(!string.IsNullOrEmpty(loop.LastError)) restText.text+="\n"+loop.LastError;
            restButton.interactable=loop.CanRest && !restPending;
            confirmRest.gameObject.SetActive(restPending);confirmRest.interactable=loop.CanRest;
            page=Mathf.Clamp(page,0,Mathf.Max(0,loop.Journal.Count-1));
            string header=loop.Failed ? "MISSION FAILED — DEADLINE EXCEEDED\nKhôi phục một ngày trước đó để tiếp tục.\n\n" : "";
            if(loop.Journal.Count==0) journalText.text=header+"NO COMPLETED DAYS\nNghỉ tại khu nghỉ để ghi nhật ký đầu tiên.";
            else
            {
                var entry=loop.Journal[page];
                journalText.text=header+$"DAY {entry.day:00}  •  {entry.zone}  ({page+1}/{loop.Journal.Count})\n"+
                    $"Distance travelled: {entry.distance:0.0} m\nPhotos taken: {entry.photos}\nCreatures captured: {entry.captures}\nTasks completed: {entry.tasks}\n"+
                    "Checkpoint: cuối ngày, trước khi nghỉ.";
                if(pendingDay>=0 && !restPending) journalText.text+="\n\nRestoring this day will erase all progress made afterward.";
            }
            if(!string.IsNullOrEmpty(loop.LastError))journalText.text+="\n"+loop.LastError;
            bool confirming=pendingDay>=0 && !restPending;
            restoreButton.gameObject.SetActive(!confirming);restoreButton.interactable=loop.Journal.Count>0;
            confirmRestore.gameObject.SetActive(confirming);
        }
        private RectTransform Box(Transform parent,string name,float x,float y,float w,float h)
        {
            var obj=new GameObject(name,typeof(RectTransform));obj.transform.SetParent(parent,false);
            var rect=(RectTransform)obj.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
            rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);return rect;
        }
        private Text Label(Transform parent,string name,float x,float y,float w,float h,int size)
        {
            var text=Box(parent,name,x,y,w,h).gameObject.AddComponent<Text>();
            var existing=screen.Desktop.GetComponentInChildren<Text>(true);
            text.font=existing!=null ? existing.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize=size;text.color=new Color(.7f,.95f,.74f);text.alignment=TextAnchor.UpperLeft;text.raycastTarget=false;
            return text;
        }
        private Button ButtonAt(Transform parent,string name,string title,float x,float y,float w,float h,UnityAction action)
        {
            var rect=Box(parent,name,x,y,w,h);var image=rect.gameObject.AddComponent<Image>();image.color=new Color(.06f,.15f,.12f);
            var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(action);
            button.navigation=new UnityEngine.UI.Navigation {mode=UnityEngine.UI.Navigation.Mode.None};
            var text=Label(rect,"Label",15,15,w-30,h-30,30);text.alignment=TextAnchor.MiddleCenter;text.text=title;return button;
        }
    }
}
