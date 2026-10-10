using System;
using G10.Prototype.Dialogue;
using UnityEngine;

namespace G10.Prototype.Tutorial
{
    [Serializable] public sealed class TutorialInstruction
    {
        public TutorialStepId step;
        [TextArea(3,8)] public string text;
    }
    [CreateAssetMenu(menuName="G10/Tutorial/Zone One Config")]
    public sealed class TutorialConfig : ScriptableObject
    {
        public string guideDisplayName = "AI";
        [Min(.1f)] public float movementDistance = 20;
        [Range(.1f,180)] public float headingDelta = 15;
        [Min(.1f)] public float depthDelta = 5;
        [Min(.05f)] public float pollInterval = .15f;
        [Min(.5f)] public float retryDelay = 3;
        [Header("Contextual guidance — reference canvas coordinates")]
        public Vector2 compactPosition = new(975, 8);
        public Vector2 compactSize = new(880, 56);
        public Vector2 helmPosition = new(975, 725);
        public Vector2 helmSize = new(340, 245);
        public TutorialInstruction[] instructions = {
            new() {step=TutorialStepId.Intro,text="Chào mừng bạn lên tàu. Tôi là AI hỗ trợ hành trình. Chúng ta sẽ làm quen các thiết bị ngay trong chuyến khảo sát Zone01. Bạn có thể bỏ qua lời thoại, nhưng hãy tự thực hành từng thao tác."},
            new() {step=TutorialStepId.Helm,text="Mở BÀN LÁI. Hãy cho tàu di chuyển, đổi hướng và thay đổi độ sâu. Quan sát tọa độ, la bàn và độ sâu; quay về cabin bất cứ lúc nào bằng ESC."},
            new() {step=TutorialStepId.Map,text="Mở BẢN ĐỒ để xem các địa điểm khảo sát. Bạn vẫn có thể quay lại bàn lái. Không cần bấm hết các địa điểm; hãy dùng tọa độ để lên đường."},
            new() {step=TutorialStepId.Radar,text="Mở RADAR và bấm QUÉT. Một lượt quét thật sẽ dùng một lượt radar. Tín hiệu giúp bạn tìm mục tiêu khi tiến gần địa điểm trên bản đồ."},
            new() {step=TutorialStepId.Camera,text="Tìm một mục tiêu chụp ảnh ở Zone01, đưa tàu tới gần và hướng camera về phía nó ở độ sâu khảo sát. Dùng máy ảnh vật lý trong cabin để chụp. Chỉ ảnh hợp lệ mới có dữ liệu nhiệm vụ; chụp ảnh chưa hoàn thành nhiệm vụ."},
            new() {step=TutorialStepId.PhotoLab,text="Mở COMPUTER → PHOTO LAB. Chọn ảnh có dòng MISSION DATA DETECTED rồi bấm SEND. Gửi dữ liệu thành công mới hoàn thành nhiệm vụ chụp ảnh."},
            new() {step=TutorialStepId.Capture,text="Đến địa điểm 02, tiếp cận vật phẩm ở độ sâu khảo sát và dùng thiết bị CAPTURE trong cabin. Hoàn thành thao tác thu thập để nhận bản thiết kế Emma. Sau đó tự chụp và SEND ảnh ở địa điểm còn lại bằng các thao tác vừa học."},
            new() {step=TutorialStepId.Upgrade,text="Dữ liệu và bản thiết kế đã đủ. Mở COMPUTER → UPGRADE, chọn EXPEDITION MODULE trong mục MODULES rồi lắp PRESSURE HULL. Thẻ HULL REINFORCEMENT là nâng cấp HP riêng; chỉ mở ứng dụng chưa hoàn thành bước này."},
            new() {step=TutorialStepId.Complete,text="Pressure Hull đã được lắp. Bạn đã biết cách điều khiển, khảo sát, gửi ảnh và thu thập. Tất cả thiết bị và ứng dụng đã mở; hãy đi tới lối ra để tiếp tục chuyến thám hiểm."}
        };
        public float MovementThreshold => Positive(movementDistance,20);
        public float HeadingThreshold => Mathf.Min(180,Positive(headingDelta,15));
        public float DepthThreshold => Positive(depthDelta,5);
        public float PollInterval => Mathf.Max(.05f,Positive(pollInterval,.15f));
        public float RetryDelay => Mathf.Max(.5f,Positive(retryDelay,3));
        private static float Positive(float value,float fallback) => float.IsFinite(value) ? Mathf.Max(.1f,value) : fallback;
        public DialogueLine[] Lines(TutorialStepId step)
        {
            var instruction=Array.Find(instructions ?? Array.Empty<TutorialInstruction>(),i=>i!=null&&i.step==step);
            return instruction==null || string.IsNullOrWhiteSpace(instruction.text) ? Array.Empty<DialogueLine>() :
                new[]{new DialogueLine(DialogueSpeakerKind.Guide,guideDisplayName,instruction.text)};
        }
        public bool IsConfigured
        {
            get { foreach(TutorialStepId step in Enum.GetValues(typeof(TutorialStepId))) if(Lines(step).Length==0)return false; return true; }
        }
        private void OnValidate()
        {movementDistance=MovementThreshold;headingDelta=HeadingThreshold;depthDelta=DepthThreshold;pollInterval=PollInterval;retryDelay=RetryDelay;}
    }
}
