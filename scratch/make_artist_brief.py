from pathlib import Path
from io import BytesIO
from PIL import Image, ImageOps
from reportlab.pdfgen import canvas
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import Paragraph
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.utils import ImageReader
from reportlab.lib.colors import HexColor
import re

ROOT=Path(r'D:\G10')
OUT=Path(r'D:\hinhchup\Mau_Huong_Dan_Artist_Anh_Sinh_Vat.pdf')
TMP=ROOT/'scratch/artist_brief'
TMP.mkdir(exist_ok=True)
pdfmetrics.registerFont(TTFont('Arial',r'C:\Windows\Fonts\arial.ttf'))
pdfmetrics.registerFont(TTFont('ArialBold',r'C:\Windows\Fonts\arialbd.ttf'))
W,H=595.28,841.89
c=canvas.Canvas(str(OUT),pagesize=(W,H))
c.setTitle('G10 - Mẫu hướng dẫn artist: ảnh sinh vật')
c.setAuthor('G10')
ink='#173D4B'; teal='#007D86'
sty=ParagraphStyle('body',fontName='Arial',fontSize=10.3,leading=15,textColor=HexColor(ink))
small=ParagraphStyle('small',parent=sty,fontSize=8.2,leading=11)
bold=ParagraphStyle('bold',parent=sty,fontName='ArialBold',fontSize=12,leading=17)
def p(text,x,y,width=507,style=sty):
    obj=Paragraph(text,style); _,hh=obj.wrap(width,700); obj.drawOn(c,x,H-y-hh); return y+hh
def title(n,name,sub):
    c.setFillColor(HexColor('#F5F8F9'));c.rect(0,0,W,H,fill=1,stroke=0)
    p('G10 / ARTIST BRIEF / 13.10.2026',44,27,style=small)
    p(name,44,54,style=ParagraphStyle('t',parent=bold,fontSize=23,leading=29))
    p(sub,44,91,style=small)
    c.setStrokeColor(HexColor('#B7D4D8'));c.line(44,H-115,W-44,H-115)
    p('Ảnh sinh vật • Mẫu bàn giao và tham chiếu',44,805,style=small)
    p(f'{n} / 7',510,805,40,small)
def section(text,y):return p(text,44,y,style=bold)+8
def img(source,x,y,w,h,bg=True):
    if bg:
        c.setFillColor(HexColor('#DDE8EB'));c.roundRect(x,H-y-h,w,h,6,fill=1,stroke=0)
    im=Image.open(source) if isinstance(source,(str,Path)) else source
    iw,ih=im.size; scale=min(w/iw,h/ih); dw,dh=iw*scale,ih*scale
    im=im.copy(); im.thumbnail((round(dw*3),round(dh*3)),Image.Resampling.LANCZOS)
    c.drawImage(ImageReader(im),x+(w-dw)/2,H-y-h+(h-dh)/2,dw,dh,mask='auto')
def rows(data,y,widths):
    for i,row in enumerate(data):
        objs=[Paragraph(t,small if i else ParagraphStyle('th',parent=small,fontName='ArialBold',textColor=HexColor('#FFFFFF'))) for t in row]
        heights=[o.wrap(w-14,600)[1] for o,w in zip(objs,widths)]; rh=max(heights)+16
        c.setFillColor(HexColor(teal if i==0 else '#E7F0F2' if i%2 else '#FFFFFF'))
        c.rect(44,H-y-rh,sum(widths),rh,fill=1,stroke=0)
        xx=44
        for o,w,hh in zip(objs,widths,heights):o.drawOn(c,xx+7,H-y-8-hh);xx+=w
        y+=rh
    return y
ART=ROOT/'Assets/_Project/Art'
KIT=ART/'Zone1_PhotoKit_Package/Zone1_PhotoKit'
CURRENT=ROOT/'Assets/_Project/Content/Zone01/Creatures/001/Preview.png'
BG=ART/'PhotoCaptureRuntime/Z1_BG_OpenWater_Mid_A.png'
ROCK=ART/'PhotoCaptureRuntime/Z1_Prop_SmallRock_A.png'
KELP=ART/'PhotoCaptureRuntime/Z1_Landmark_SingingKelp_Cluster_A.png'
FLOOR=ART/'PhotoCaptureRuntime/Z1_Seabed_DistantFloor_A.png'
def comp(size=220,covered=False,fog=False,empty=False):
    im=Image.open(BG).convert('RGBA').resize((640,360))
    def lay(path,w,pos,alpha=1):
        o=Image.open(path).convert('RGBA');o=o.resize((w,round(w*o.height/o.width)))
        if alpha!=1:o.putalpha(o.getchannel('A').point(lambda a:round(a*alpha)))
        im.alpha_composite(o,pos)
    lay(FLOOR,640,(0,245),.65)
    if not empty:lay(CURRENT,round(size*.75),(int(320-size*.375),int(310-size)))
    if covered:lay(ROCK,220,(280,110))
    if fog:im=Image.blend(im,Image.new('RGBA',im.size,(54,96,132,255)),.73)
    return im.convert('RGB')

title(1,'Artist cần vẽ những gì?','Tài liệu tự chứa hình mẫu để gửi trực tiếp cho artist. Ưu tiên hoàn thiện Zone 1 trước.')
y=p('<b>Mục tiêu:</b> ảnh chụp dưới nước cho thấy sinh vật đang sống trong môi trường. Artist bàn giao từng lớp độc lập; game ghép nền, sinh vật, đá/rong và sương thành ảnh.',44,134)
y=section('Một bộ asset tạo được nhiều tình huống',y+16)
examples=[('Rõ sinh vật',comp()),('Ở xa',comp(80)),('Bị đá che',comp(220,True)),('Nước đục / thiếu sáng',comp(220,fog=True))]
for i,(label,im) in enumerate(examples):
    x=44+(i%2)*259; yy=225+(i//2)*169
    img(im,x,yy,247,139,False);p(label,x,yy+144,247,small)
p('Các khung trên là minh họa ghép từ asset dự án, không phải ảnh chụp gameplay. Chỉ dùng để giải thích bố cục và lớp vẽ.',44,575,style=small)
y=section('Bộ bàn giao tối thiểu cho một sinh vật',613)
y=p('1. <b>Main:</b> toàn thân, màu hoàn chỉnh, PNG nền trong suốt.<br/>2. <b>Mockup:</b> một ảnh 16:9 đặt sinh vật vào môi trường để duyệt bố cục.<br/>3. <b>File nguồn:</b> PSD hoặc định dạng gốc có lớp đặt tên rõ ràng.',44,y)
p('<b>Mở rộng sau khi duyệt:</b> Close, Silhouette, tư thế phụ và góc 3/4. Không cần vẽ sáu tranh riêng tương ứng sáu kết quả chụp; game thay đổi kích thước, độ che và độ mờ.',44,y+12)
c.showPage()

title(2,'Tách tranh thành các lớp','Vẽ từ xa tới gần. Nền kín; sinh vật, vật thể và hiệu ứng có alpha trong suốt.')
refs=[('01 • Nền nước',BG),('02 • Đáy biển',FLOOR),('03 • Sinh vật',CURRENT),('04 • Rong / landmark',KELP),('05 • Đá che phía trước',ROCK),('06 • Sương phủ',ART/'PhotoCaptureRuntime/Z1_Overlay_Fog_Mid.png')]
for i,(label,path) in enumerate(refs):
    xx=44+(i%3)*172; yy=136+(i//3)*179
    img(path,xx,yy,162,140);p(label,xx,yy+146,162,small)
y=rows([
    ['Lớp cần vẽ','Yêu cầu cho artist'],
    ['Nền Shallow / Mid / Deep','3 mức nước sáng, trung bình, sâu/tối. Không gộp sinh vật vào nền.'],
    ['Đáy biển','Dải đá/cát xa, chuyển tiếp mềm vào nước; tách alpha để tái sử dụng.'],
    ['Sinh vật','Tách sạch toàn bộ thân, râu, tua và vây. Chừa khoảng trống quanh hình.'],
    ['Rong và đá','File riêng. Rong là môi trường, phải phân biệt được với sinh vật. Đá dùng để che một phần thân.'],
    ['Sương và hạt nước','Overlay trong suốt, cường độ nhẹ; giữ sinh vật đọc được ở bản rõ.']
],513,[133,374])
p('<b>Lưu ý mẫu hiện có:</b> hình rong và sinh vật 001 đang rất giống nhau. Cần phân biệt silhouette, màu hoặc cấu trúc trong bản hoàn thiện. Glow và foreground bổ sung là tùy chọn, chưa cần cho bộ tối thiểu.',44,y+12,style=small)
c.showPage()

title(3,'Mẫu cụ thể: sinh vật 001','Z1_Creature_01 • nhiệm vụ chụp ảnh tại Rạn Tảo Đỏ (Z1_L1).')
img(ART/'Sprites/Creature/Zone1/001.png',44,136,158,223)
img(ART/'Sprites/Creature/Zone1/Processed/Creature001_Clean.png',216,136,158,223)
img(CURRENT,388,136,162,223)
p('Tranh gốc có môi trường',44,366,158,small)
p('Bản tách từ tranh gốc',216,366,158,small)
p('Mẫu đang gán cho nội dung',388,366,162,small)
y=section('Cần giữ và cần làm rõ',405)
y=p('<b>Tham chiếu thị giác:</b> nét xanh mảnh, chất màu nước xanh lam/ngọc, các phần hồng/đào, đường cong và tua dài. Giữ nét thủ công và độ trong của màu; tránh viền cứng hoặc khối 3D bóng.',44,y)
y=p('<b>Việc artist cần làm:</b> làm sạch đường viền; tách sinh vật khỏi vách đá, cát và rong; giữ đủ các tua mảnh; kiểm tra khi thu nhỏ trên nền tối. Phần đá/rễ gắn ở chân phải xác định là bộ phận cơ thể hay lớp môi trường để tách cho đúng.',44,y+12)
y=p('<b>Điểm cần duyệt thiết kế:</b> dữ liệu hiện mô tả “Giáp xác rạn tảo”, còn hình mẫu mang dáng tảo/hoa. Không tự biến hình mẫu này thành một loài mới. Duyệt silhouette và giải phẫu với người phụ trách trước khi vẽ chi tiết hoặc nhiều góc.',44,y+12)
y=section('Các phiên bản của cùng một loài',y+18)
rows([['Phiên bản','Cần thể hiện'],['Main (ưu tiên)','Toàn thân, tư thế dễ đọc; đây là ảnh đầu tiên cần hoàn thiện.'],['Close (bổ sung)','Cùng giải phẫu, chi tiết rõ hơn ở vây/tua/bề mặt.'],['Silhouette (bổ sung)','Cùng đường bao và alpha; khối tối dùng cho khoảng cách xa.'],['AltPose / ThreeQuarter','Tư thế phụ / góc 3/4 sau khi Main được duyệt.'] ],y,[143,364])
c.showPage()

title(4,'Mẫu brief cho từng sinh vật','Có thể dùng nguyên cấu trúc dưới đây cho các loài tiếp theo.')
y=rows([
 ['Trường','Mẫu điền cho Z1_Creature_02'],
 ['Mã file / vị trí','Z1_Creature_02 • Zone 1 • Thềm Biển Sâu (Z1_L3).'],
 ['Vai trò','Đối tượng nhiệm vụ chụp ảnh. Sinh vật sống bám đá; chỉ lấy mẫu dịch, để sinh vật ở lại (theo mô tả nội dung).'],
 ['Mô tả đã có','Sinh vật thân mềm bám đá, dịch tiết kết dính liên kết lớp mô chịu áp lực.'],
 ['Artist cần đề xuất','Silhouette thân mềm; cách bám đá; phần cơ thể nào là dấu hiệu nhận diện. Hình dạng, màu chính và kích thước tương đối chưa có thiết kế hoàn chỉnh.'],
 ['Ảnh Main','Toàn thân trên nền trong suốt. Đá nơi bám ở lớp riêng nếu là môi trường. Tránh vẽ cả vách đá kín nền cùng sinh vật.'],
 ['Mockup duyệt','Đặt sinh vật lên đá, trong nước sâu. Phải thấy rõ cơ thể và điểm bám; nền giảm tương phản để chủ thể nổi bật.'],
 ['Bản bổ sung','Close / Silhouette sau khi Main được duyệt. Không bắt buộc animation cho đợt bàn giao ảnh tĩnh.'],
 ['Tiêu chí duyệt','Đúng mã loài; đọc được khi thu nhỏ; cơ thể không bị cắt; alpha thật; lớp đá tách riêng; tư thế và màu nhất quán.']
],136,[135,372])
img(ROOT/'Assets/_Project/Content/Zone01/Creatures/002/Preview.png',44,y+22,140,125)
p('<b>Hình tạm hiện tại</b><br/>Biểu tượng các khối xanh này đang được dùng làm placeholder. Đây không phải mẫu giải phẫu của sinh vật 002; artist cần tạo concept mới theo mô tả bên trên và gửi duyệt trước.',205,y+32,340)
p('<b>Quy trình:</b> sketch đen trắng → duyệt silhouette → duyệt màu Main → hoàn thiện nét/màu → tách lớp và xuất PNG → mockup trên nền sáng và tối.',44,y+169)
c.showPage()

title(5,'Danh sách sinh vật cần ảnh','11 đối tượng có nhiệm vụ Photograph trong cấu hình hiện tại. Thứ tự đề xuất: Zone 1 trước.')
rows([
 ['Mã sinh vật','Vị trí nhiệm vụ','Tình trạng / hướng xử lý'],
 ['Z1_Creature_01','Z1_L1 • Rạn Tảo Đỏ','Đã có mẫu màu và ảnh gán. Hoàn thiện, duyệt lại mô tả loài.'],
 ['Z1_Creature_02','Z1_L3 • Thềm Biển Sâu','Ảnh tạm. Vẽ mới thân mềm bám đá theo brief trang 4.'],
 ['Z2_Creature_01','Z2_L1 • Location 01','Chưa gán ảnh. Cần duyệt concept; nhiệm vụ thưởng dữ liệu phát quang.'],
 ['Z2_Creature_02','Z2_L3 • Location 03','Chưa gán ảnh. Có nhiệm vụ chụp và bắt; cần hình sống rõ ràng.'],
 ['Z3_Creature_01','Z3_L1 • Location 01','Chưa gán ảnh. Có nhiệm vụ chụp và bắt.'],
 ['Z3_Creature_02','Z3_L3 • Location 03','Chưa gán ảnh. Cần duyệt concept trước khi render màu.'],
 ['Z3_Creature_03','Z3_L4 • Location 04','Chưa gán ảnh. Cần duyệt concept trước khi render màu.'],
 ['Z4_Creature_01','Z4_L1 • Location 01','Chưa gán ảnh. Đối tượng ảnh thuộc nhánh kết thúc thường.'],
 ['Z4_Creature_02','Z4_L2 • Location 02','Chưa gán ảnh. Đối tượng ảnh thuộc nhánh kết thúc thường.'],
 ['Z4_Creature_03','Z4_L3 • Location 03','Chưa gán ảnh. Đối tượng ảnh thuộc nhánh kết thúc ẩn.'],
 ['Z4_Creature_04','Z4_L4 • Location 04','Chưa gán ảnh. Đối tượng ảnh thuộc nhánh kết thúc ẩn.']
],136,[124,148,235])
p('<b>Phạm vi chưa chốt:</b> Zone 2–4 mới có mã nhiệm vụ và định nghĩa placeholder, chưa có mô tả hình dáng đủ để artist tự chốt thiết kế. Các manh mối về phát quang, chống va đập hoặc sóng xung kích là ngữ cảnh nhiệm vụ, không phải concept đã được duyệt.',44,671)
p('<b>Định mức đề xuất:</b> 11 Main + 11 mockup + file nguồn. Close/Silhouette/pose phụ tính riêng sau khi duyệt Main; không mặc định yêu cầu 5 phiên bản cho mọi loài.',44,747,style=small)
c.showPage()

title(6,'Quy cách bàn giao','Các kích thước dưới đây là đề xuất sản xuất; ảnh kết quả trong game hiện là 640 × 360.')
y=rows([
 ['Hạng mục','Quy cách đề xuất'],
 ['Sinh vật / đá / rong','PNG RGBA, alpha thật. Sinh vật: cạnh dài 1600–2048 px; giữ đúng tỉ lệ thân, không kéo giãn cho vừa khung.'],
 ['Nền / mockup','1920 × 1080 px, 16:9. Nền nước kín khung, không chứa sinh vật. Mockup là ảnh ghép để duyệt, không thay thế PNG tách lớp.'],
 ['Fog / particles','1920 × 1080 px, RGBA; nền trong suốt, mật độ nhẹ. Không bake hiệu ứng vào cơ thể.'],
 ['File nguồn','PSD hoặc file gốc có lớp: BG, Seabed, Landmark, Creature, Foreground, Fog, Particles. Giữ nét vẽ và màu để chỉnh sửa.'],
 ['Khoảng đệm / căn chỉnh','Chừa 5–10% quanh tua/vây. Các phiên bản của cùng loài nên cùng canvas và điểm neo để không bị nhảy vị trí khi đổi ảnh.'],
 ['Tên file','Z1_Creature_01_Main.png<br/>Z1_Creature_01_Close.png<br/>Z1_Creature_01_Silhouette.png<br/>Z1_Creature_01_Mockup.jpg'],
 ['Cấu trúc thư mục','Zone1/Creatures/Creature01/<br/>Zone1/Backgrounds/ • Seabed/ • Landmarks/<br/>Props/ • Overlays/ • Mockups/ • Source/']
],136,[139,368])
y=section('Checklist trước khi gửi',y+22)
p('□ PNG trong suốt thật, không nền trắng hoặc ô caro vẽ dính.<br/>□ Không mất râu, tua, vây và không có viền trắng quanh alpha.<br/>□ Sinh vật tách khỏi đá/rong; hình Main đọc rõ trên nền sáng và tối.<br/>□ Kiểm tra mockup khi thu xuống 640 × 360, không chỉ ở bản lớn.<br/>□ Cùng loài giữ thống nhất màu, số nhánh, chi tiết nhận diện.<br/>□ Tên file đúng mã, có file nguồn và mockup để duyệt.',44,y)
c.showPage()

title(7,'Phạm vi và cách sử dụng mẫu','Thông tin dự án được kiểm tra ngày 13/10/2026; phần đề xuất được ghi rõ trong tài liệu.')
y=section('Artist có thể bắt đầu ngay',136)
y=p('Làm Main và mockup của sinh vật 001 theo mẫu đang có; gửi sketch cho sinh vật 002 theo mô tả thân mềm bám đá. Sau khi duyệt hai loài Zone 1, mới mở rộng sang các mã Zone 2–4.',44,y)
y=section('Điểm cần biết khi bàn giao cho người tích hợp',y+23)
y=p('Ảnh nội dung gán cho mỗi loài hiện được ưu tiên hơn ảnh sinh vật chung trong profile. Vì vậy <b>Main là file quan trọng nhất</b>. Close và Silhouette có trong bộ mẫu, nhưng không tự động đổi theo khoảng cách cho mọi loài đã gán ảnh; muốn dùng đủ bộ cần người tích hợp nối thêm cấu hình.',44,y)
y=p('Nền hiện chọn theo độ sâu; vật thể ghép theo khoảng cách. Các kết quả NoSubject, LifeDetected, GoodPhoto, TooFar, Obstructed và LowVisibility do game đánh giá. Artist chỉ cần cung cấp các lớp có alpha chính xác; sương, vật che và kích thước chủ thể tạo ra khác biệt giữa các ảnh.',44,y+12)
y=section('Nguồn tham chiếu trong dự án',y+23)
y=p('Hình trong tài liệu được nhúng trực tiếp; người nhận không cần mở dự án Unity. Các đường dẫn dưới đây chỉ phục vụ đối chiếu nội bộ, đều tính từ thư mục G10.',44,y,style=small)
sources=[
 'Assets/_Project/Content/Missions/Zone01.asset ... Zone04.asset',
 'Assets/_Project/Content/Zone01/Creatures/001 và 002/Definition.asset',
 'Assets/_Project/Content/Zone02 ... Zone04/Definitions/',
 'Assets/_Project/Content/Zone01/Creatures/001/Preview.png',
 'Assets/_Project/Art/Sprites/Creature/Zone1/001.png và Processed/',
 'Assets/_Project/Art/PhotoCaptureRuntime/Zone01CaptureProfile.asset',
 'Assets/_Project/Art/Zone1_PhotoKit_Package/Zone1_PhotoKit/',
 'Assets/_Project/Scripts/Computer/PhotoCaptureService.cs',
 'Assets/_Project/Scripts/Computer/PhotoLayerComposer.cs',
]
for s in sources:y=p('• '+s,44,y+8,style=small)
p('<b>Ghi chú:</b> các mockup ở trang 1 là minh họa bố cục do ghép asset hiện có. Kích thước xuất, khoảng đệm, số lượng mockup và thứ tự sản xuất là đề xuất bàn giao, không phải các thông số art đã khóa của dự án.',44,y+23)
c.showPage();c.save()
print(OUT)
