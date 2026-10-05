using System;
using System.Collections.Generic;
using System.IO;
using G10.Prototype.Computer;
using G10.Prototype.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace G10.Prototype.UI
{
    /// <summary>Temporary menu QA surface; persistence remains owned by ExpeditionSaveStore.</summary>
    [DisallowMultipleComponent]
    public sealed class MainMenuTutorialReset : MonoBehaviour
    {
        public const string ConfirmationTitle = "XÓA TRÍ NHỚ HƯỚNG DẪN?";
        public const string ConfirmationBody = "Tiến trình chuyến thám hiểm hiện tại sẽ không bị xóa.\nLần NEW GAME tiếp theo sẽ chạy lại hướng dẫn Zone 1 từ đầu.";
        public const string SuccessText = "Đã xóa dữ liệu hướng dẫn.\nChọn NEW GAME để kiểm tra trải nghiệm người chơi mới.";
        public Button ResetButton { get; private set; }
        public Button ConfirmButton { get; private set; }
        public Button CancelButton { get; private set; }
        public bool IsConfirmationOpen => overlay!=null && overlay.activeSelf;
        public string Feedback => feedback!=null ? feedback.text : string.Empty;
        private GameObject overlay, previousSelection;
        private Text feedback;
        private Button template;
        private readonly Dictionary<Selectable,bool> blocked = new();

        public static bool VisibleForConfiguration(bool editor, bool development) => editor || development;
        public static bool IsDeveloperConfiguration
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                return VisibleForConfiguration(Application.isEditor,Debug.isDebugBuild);
#else
                return false;
#endif
            }
        }
        public static MainMenuTutorialReset Ensure(Button source)
        {
            if(!IsDeveloperConfiguration || source==null || source.gameObject.scene.name!=SceneFlowController.MainMenuScene)return null;
            var existing=source.transform.parent.GetComponentInChildren<MainMenuTutorialReset>(true);
            if(existing!=null)return existing;
            var root=Rect("TutorialResetQA",source.transform.parent,((RectTransform)source.transform).sizeDelta,
                ((RectTransform)source.transform).anchoredPosition+Vector2.down*270);
            var view=root.gameObject.AddComponent<MainMenuTutorialReset>();
            view.template=source;
            view.ResetButton=view.Button(root,"ResetTutorialButton","XÓA TRÍ NHỚ",root.sizeDelta,Vector2.zero,view.RequestReset);
            view.feedback=view.Label(root,"Feedback","",new Vector2(900,90),new Vector2(0,-90),23);
            return view;
        }
        private void Awake(){if(!IsDeveloperConfiguration)gameObject.SetActive(false);}
        private bool CanUse => IsDeveloperConfiguration && isActiveAndEnabled &&
            gameObject.scene.name==SceneFlowController.MainMenuScene &&
            (SceneFlowController.Instance==null || !SceneFlowController.Instance.IsTransitioning);
        public void RequestReset()
        {
            if(!CanUse || IsConfirmationOpen)return;
            if(overlay==null)BuildConfirmation();
            feedback.text="";
            previousSelection=EventSystem.current!=null ? EventSystem.current.currentSelectedGameObject : null;
            foreach(var selectable in transform.parent.GetComponentsInChildren<Selectable>(true))
            {
                if(selectable.transform.IsChildOf(overlay.transform))continue;
                blocked[selectable]=selectable.interactable;selectable.interactable=false;
            }
            overlay.transform.SetAsLastSibling();overlay.SetActive(true);
            if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(CancelButton.gameObject);
        }
        public void ConfirmReset()
        {
            if(!CanUse || !IsConfirmationOpen)return;
            try
            {
                ExpeditionSaveStore.ResetTutorialProgress(waitForNewGame:true);
                CloseConfirmation();feedback.text=SuccessText;
            }
            catch(Exception ex) when(ex is IOException || ex is UnauthorizedAccessException)
            {CloseConfirmation();feedback.text="Không thể xóa dữ liệu hướng dẫn.\n"+ex.Message;}
        }
        public void CancelReset()=>CloseConfirmation();
        private void Update()
        {
            if(IsConfirmationOpen && (Keyboard.current?.escapeKey.wasPressedThisFrame==true ||
                Gamepad.current?.buttonEast.wasPressedThisFrame==true))CancelReset();
        }
        private void CloseConfirmation()
        {
            if(overlay!=null)overlay.SetActive(false);
            foreach(var pair in blocked)if(pair.Key!=null)pair.Key.interactable=pair.Value;
            blocked.Clear();
            if(EventSystem.current!=null && previousSelection!=null && previousSelection.activeInHierarchy)
                EventSystem.current.SetSelectedGameObject(previousSelection);
            previousSelection=null;
        }
        private void OnDisable()=>CloseConfirmation();
        private void OnDestroy(){if(overlay!=null)Destroy(overlay);}
        private void BuildConfirmation()
        {
            var backdrop=Rect("TutorialResetConfirmation",transform.parent,Vector2.zero,Vector2.zero);
            overlay=backdrop.gameObject;overlay.SetActive(false);
            backdrop.anchorMin=Vector2.zero;backdrop.anchorMax=Vector2.one;backdrop.offsetMin=backdrop.offsetMax=Vector2.zero;
            backdrop.gameObject.AddComponent<Image>().color=new Color(.015f,.03f,.06f,.85f);
            var panel=Rect("Dialog",backdrop,new Vector2(860,350),Vector2.zero);
            panel.gameObject.AddComponent<Image>().color=new Color(.055f,.095f,.15f);
            Label(panel,"Title",ConfirmationTitle,new Vector2(800,65),new Vector2(0,110),32);
            Label(panel,"Body",ConfirmationBody,new Vector2(800,125),new Vector2(0,15),26);
            ConfirmButton=Button(panel,"ConfirmReset","XÓA",new Vector2(240,64),new Vector2(-145,-110),ConfirmReset);
            CancelButton=Button(panel,"CancelReset","HỦY",new Vector2(240,64),new Vector2(145,-110),CancelReset);
        }
        private static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 position)
        {
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);
            var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);
            rect.sizeDelta=size;rect.anchoredPosition=position;return rect;
        }
        private Text Label(Transform parent,string name,string value,Vector2 size,Vector2 position,int fontSize)
        {
            var text=Rect(name,parent,size,position).gameObject.AddComponent<Text>();
            var original=template.GetComponentInChildren<Text>(true);
            text.font=original!=null ? original.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.color=original!=null ? original.color : Color.white;
            text.fontSize=fontSize;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;text.text=value;
            return text;
        }
        private Button Button(Transform parent,string name,string title,Vector2 size,Vector2 position,UnityEngine.Events.UnityAction action)
        {
            var rect=Rect(name,parent,size,position);var image=rect.gameObject.AddComponent<Image>();
            var original=template.targetGraphic as Image;
            if(original!=null){image.sprite=original.sprite;image.type=original.type;image.color=original.color;}
            var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;button.colors=template.colors;
            button.onClick.AddListener(action);Label(rect,"Label",title,size,Vector2.zero,28);return button;
        }
    }
}
