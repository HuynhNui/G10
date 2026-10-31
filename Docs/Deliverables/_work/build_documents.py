from pathlib import Path
import math, re, json
from docx import Document
from docx.shared import Cm, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_CELL_VERTICAL_ALIGNMENT
from PIL import Image, ImageDraw, ImageFont

ROOT = Path('D:/G10')
OUT = ROOT/'Docs/Deliverables'
WORK = OUT/'_work'
WORK.mkdir(parents=True, exist_ok=True)
FONT = 'Times New Roman'
DATE = '29 tháng 10 năm 2026'
sources = {
 'R01':'IEEE Std 830-1998, IEEE Recommended Practice for Software Requirements Specifications. https://standards.ieee.org/ieee/830/1222/ ; bản tiêu chuẩn tham khảo: https://people.eecs.ku.edu/~hossein/Teaching/Stds/0830.pdf (truy cập 29/10/2026).',
 'R02':'ProjectSettings/ProjectVersion.txt; ProjectSettings/ProjectSettings.asset; Packages/manifest.json; ProjectSettings/EditorBuildSettings.asset.',
 'R03':'Assets/_Project/Content/Missions/Zone01.asset đến Zone04.asset; Assets/_Project/Content/Maps/Zone01.asset đến Zone04.asset.',
 'R04':'Assets/_Project/Scripts/Computer/ExpeditionLoop.cs; ExpeditionSave.cs; RegularShipUpgradeRules.cs.',
 'R05':'Assets/_Project/Scripts/Computer/PhotoCaptureService.cs; PhotoCaptureProfile.cs; PhotoLabView.cs; ZoneFourPhotoComposition.cs.',
 'R06':'Assets/_Project/Scripts/Navigation/ZoneNavigation.cs; ShipResources.cs; CreatureCatcher.cs; CreatureInventory.cs; CaptureMinigameController.cs; CaptureMinigameProfile.cs.',
 'R07':'Assets/_Project/Scripts/UI/ExpeditionProgression.cs; CabinZoneSession.cs; CabinStationView.cs; EndingPresentation.cs; Scripts/Core/SceneFlowController.cs.',
 'R08':'Assets/_Project/Scenes/Gameplay/GameplayCore.unity; Zone01.unity; Assets/_Project/Data/Computer/Zone01CaptureMinigame.asset.',
 'R09':'Docs/AI/MissionLocationsRefactor-2026-10-20.md; Docs/AI/ReturnGatePlacement-2026-10-14.md; Docs/AI/UnityProjectContext.md.',
 'R10':'Docs/Zone01AudioGameplay.md; Docs/Zone01Cutscene.md; Docs/Zone01Observation.md; Docs/Zone02EnvironmentalHorror.md.',
 'R11':'Docs/Zone03CinematicProduction.md; Docs/Zone04FinalChapterProduction.md; Docs/QA/Zone04ExperienceLoop/04_Implementation_Validation.md.',
 'R12':'Assets/_Project/Tests/EditMode và Assets/_Project/Tests/PlayMode; Docs/QA/Zone04FinalChapter/results.md; Docs/QA/Zone04ExperienceLoop/native-validation/results.json.',
}

def font(size=30):
    return ImageFont.truetype('C:/Windows/Fonts/arial.ttf',size)

def wrapped(draw, text, box, size=28, fill='#172d3a'):
    f=font(size); lines=[]
    for block in text.split('\n'):
        line=''
        for word in block.split():
            if draw.textlength((line+' '+word).strip(),font=f)>box[2]-box[0]-24 and line:
                lines.append(line); line=word
            else: line=(line+' '+word).strip()
        lines.append(line)
    lh=size*1.3; y=(box[1]+box[3]-lh*len(lines))/2
    for line in lines:
        draw.text(((box[0]+box[2]-draw.textlength(line,font=f))/2,y),line,font=f,fill=fill);y+=lh

def diagram(name, boxes, arrows, size=(1600,650)):
    im=Image.new('RGB',size,'white');d=ImageDraw.Draw(im)
    for a,b in arrows:
        d.line((a,b),fill='#4b6f80',width=5)
        ang=math.atan2(b[1]-a[1],b[0]-a[0]); L=18
        d.polygon([b,(b[0]-L*math.cos(ang-.45),b[1]-L*math.sin(ang-.45)),(b[0]-L*math.cos(ang+.45),b[1]-L*math.sin(ang+.45))],fill='#4b6f80')
    for box,text in boxes:
        d.rounded_rectangle(box,radius=16,fill='#eff5f7',outline='#5d7c8b',width=3)
        wrapped(d,text,box,30)
    p=WORK/(name+'.png');im.save(p);return p

context=diagram('system_context',[
 ((15,210,305,400),'Người chơi\nChuột và bàn phím'),
 ((465,175,1090,435),'G10\nĐiều khiển tàu và khảo sát\nNhiệm vụ và hành trình'),
 ((1245,65,1580,245),'Lưu cục bộ\nTimeline và bản dự phòng'),
 ((1245,395,1580,575),'Kho ảnh cục bộ\nPNG và siêu dữ liệu')],
 [((305,305),(465,305)),((1090,245),(1245,155)),((1090,365),(1245,485))])
loop=diagram('core_loop',[
 ((20,65,470,200),'Đọc bản đồ\nLái tàu đến độ sâu phù hợp'),
 ((580,65,1030,200),'Radar và định hướng\nTiếp cận tín hiệu'),
 ((1135,65,1580,200),'Chụp ảnh nhanh\nKiểm tra trong Photo Lab'),
 ((1135,365,1580,520),'SEND hợp lệ\nGhi nhận PHOTO'),
 ((580,365,1030,520),'Bắt mẫu khi được yêu cầu\nNhận vật liệu và công thức'),
 ((20,365,470,520),'Nâng cấp và mở lối\nNghỉ để tiếp tục hành trình')],
 [((470,130),(580,130)),((1030,130),(1135,130)),((1350,200),(1350,365)),((1135,440),(1030,440)),((580,440),(470,440)),((245,365),(245,200))])
flow=diagram('progression',[
 ((10,50,360,225),'Zone 01\n2 điểm bắt buộc\nBio Lamp'),
 ((415,50,765,225),'Zone 02\n2 điểm bắt buộc\nPressure Hull 750 m'),
 ((820,50,1170,225),'Zone 03\n3 điểm bắt buộc\nRock Breaker'),
 ((1225,50,1585,225),'Zone 04\n2 điểm bắt buộc\nLựa chọn kết thúc'),
 ((580,400,985,565),'Normal Ending\nKết thúc sau khảo sát chính'),
 ((1120,400,1585,565),'Hidden Ending\n2 điểm ẩn và đến tín hiệu cuối')],
 [((360,140),(415,140)),((765,140),(820,140)),((1170,140),(1225,140)),((1305,225),(800,400)),((1440,225),(1360,400))])
photo=diagram('photo_state',[
 ((20,70,440,220),'Chụp ảnh\nẢnh chưa gửi'),
 ((590,70,1010,220),'Photo Lab\nĐúng mục tiêu và đủ điều kiện'),
 ((1160,70,1580,220),'SEND\nGhi timeline thành công'),
 ((1160,390,1580,550),'PHOTO hoàn thành\nKhông tự sinh cargo'),
 ((590,390,1010,550),'Lỗi ghi lưu\nGiữ ảnh chưa gửi để thử lại'),
 ((20,390,440,550),'Ảnh không hợp lệ\nKhông được ghi tiến độ')],
 [((440,145),(590,145)),((1010,145),(1160,145)),((1370,220),(1370,390)),((1200,220),(940,390)),((230,220),(230,390))])

def init_doc(kind):
    d=Document();s=d.sections[0]
    for st in d.styles:
        for node in list(st.element.iter(qn('w:pBdr'))):
            node.getparent().remove(node)
    s.page_width=Cm(21);s.page_height=Cm(29.7)
    s.top_margin=Cm(1.9);s.bottom_margin=Cm(1.8);s.left_margin=Cm(2.25);s.right_margin=Cm(2.0)
    s.header_distance=Cm(.7);s.footer_distance=Cm(.8)
    for name,size in [('Normal',12),('Title',26),('Subtitle',14),('Heading 1',16),('Heading 2',13.5),('Heading 3',12),('Caption',10)]:
        st=d.styles[name];st.font.name=FONT;st.font.size=Pt(size);st.font.color.rgb=RGBColor(0,0,0)
        st._element.get_or_add_rPr().append(OxmlElement('w:lang'));st._element.rPr[-1].set(qn('w:val'),'vi-VN')
        st.paragraph_format.space_after=Pt(6)
        if name.startswith('Heading'):
            st.font.bold=True;st.paragraph_format.space_before=Pt(10);st.paragraph_format.keep_with_next=True
    n=d.styles['Normal'].paragraph_format;n.line_spacing=1.10;n.widow_control=True
    d.styles['Caption'].font.italic=True
    for name in ['TOC 1','TOC 2']:
        if name not in d.styles:
            d.styles.add_style(name,1)
        st=d.styles[name];st.font.name=FONT;st.font.size=Pt(11);st.font.color.rgb=RGBColor(0,0,0);st.paragraph_format.space_after=Pt(3)
    p=s.footer.paragraphs[0];p.alignment=WD_ALIGN_PARAGRAPH.RIGHT
    r=p.add_run(kind+'  |  G10  |  ');r.font.size=Pt(9)
    fld=OxmlElement('w:fldSimple');fld.set(qn('w:instr'),'PAGE');p._p.append(fld)
    d.core_properties.title=kind+' G10';d.core_properties.subject='Tài liệu dự án G10';d.core_properties.author='';d.core_properties.last_modified_by=''
    settings=d.settings.element;u=OxmlElement('w:updateFields');u.set(qn('w:val'),'true');settings.append(u)
    return d

def p(d,text,style=None):
    x=d.add_paragraph(text,style);return x
def h(d,text,level=1):return d.add_heading(text,level)
def page(d,title,level=2):d.add_page_break();h(d,title,level)
def table(d,heads,rows,widths=None):
    t=d.add_table(rows=1,cols=len(heads));t.alignment=WD_TABLE_ALIGNMENT.CENTER;t.autofit=False
    if widths:
        for c,w in zip(t.columns,widths):c.width=Cm(w)
    for c,txt in zip(t.rows[0].cells,heads):c.text=txt
    for vals in rows:
        for c,txt in zip(t.add_row().cells,vals):c.text=str(txt)
    for ri,row in enumerate(t.rows):
        pr=row._tr.get_or_add_trPr();pr.append(OxmlElement('w:cantSplit'))
        if ri==0:pr.append(OxmlElement('w:tblHeader'))
        for ci,c in enumerate(row.cells):
            if widths:c.width=Cm(widths[ci])
            c.vertical_alignment=WD_CELL_VERTICAL_ALIGNMENT.CENTER
            cp=c._tc.get_or_add_tcPr();b=OxmlElement('w:tcBorders')
            for side in ['top','left','bottom','right']:
                e=OxmlElement('w:'+side);e.set(qn('w:val'),'single');e.set(qn('w:sz'),'4');e.set(qn('w:color'),'D9D9D9');b.append(e)
            cp.append(b);mar=OxmlElement('w:tcMar')
            for side in ['top','left','bottom','right']:
                e=OxmlElement('w:'+side);e.set(qn('w:w'),'80');e.set(qn('w:type'),'dxa');mar.append(e)
            cp.append(mar)
            sh=OxmlElement('w:shd');sh.set(qn('w:fill'),'DFEAF0' if ri==0 else ('F4F7F9' if ri%2==0 else 'FFFFFF'));cp.append(sh)
            for para in c.paragraphs:
                para.paragraph_format.space_after=Pt(2);para.paragraph_format.space_before=Pt(1);para.paragraph_format.line_spacing=1.02
                if ci==0 and len(heads)>2:para.alignment=WD_ALIGN_PARAGRAPH.CENTER
                for r in para.runs:r.font.name=FONT;r.font.size=Pt(10.5);r.bold=ri==0
    d.add_paragraph().paragraph_format.space_after=Pt(0)
    return t
def pic(d,path,caption,width=16.2):
    x=d.add_paragraph();x.paragraph_format.keep_with_next=True;x.add_run().add_picture(str(path),width=Cm(width))
    shape=d.inline_shapes[-1];shape._inline.docPr.set('descr',caption)
    p(d,caption,'Caption')
def req(d,rid,title,body,accept,source,priority='Bắt buộc'):
    x=p(d,'');x.paragraph_format.keep_with_next=True
    r=x.add_run(rid+'  '+title);r.bold=True
    p(d,body)
    x=p(d,'Kiểm chứng: '+accept+'  ['+source+'; '+priority+']');x.paragraph_format.space_after=Pt(7)
    for r in x.runs:r.font.size=Pt(10.5)
def cover(d,title,sub,code,description,image_path=None):
    p(d,'G10','Subtitle');p(d,title,'Title');p(d,sub,'Subtitle');p(d,description)
    if image_path:pic(d,image_path,'Không gian cabin của G10 trong ảnh kiểm tra giao diện thực tế.',16.2)
    table(d,['Thuộc tính','Giá trị'],[
        ('Mã tài liệu',code),('Phiên bản','1.0'),('Ngày lập',DATE),('Dự án','G10'),('Ngôn ngữ','Tiếng Việt'),('Đơn vị sử dụng','Nhóm phát triển, thiết kế và kiểm thử G10')],[4,12.7])
def toc(d,levels='1-2'):
    d.add_page_break();p(d,'Mục lục','Title')
    fld=OxmlElement('w:fldSimple');fld.set(qn('w:instr'),'TOC \\o "'+levels+'" \\h \\z \\u')
    r=OxmlElement('w:r');tx=OxmlElement('w:t');tx.text='Mục lục';r.append(tx);fld.append(r);d.add_paragraph()._p.append(fld)
def refs(d,ids):
    for k in ids:p(d,k+'  '+sources[k])

# SOFTWARE REQUIREMENTS SPECIFICATION
d=init_doc('SRS')
cover(d,'Đặc tả yêu cầu phần mềm G10','Software Requirements Specification theo IEEE 830 1998','G10 SRS 1.0',
      'Đặc tả hành vi của game khảo sát biển sâu G10, làm cơ sở thống nhất phạm vi, phát triển tính năng và thiết kế kiểm thử nghiệm thu.')
h(d,'Kiểm soát phiên bản',2)
table(d,['Phiên bản','Ngày','Nội dung'],[('1.0','29/10/2026','Thiết lập đặc tả chức năng, dữ liệu, chất lượng và truy vết yêu cầu cho hành trình bốn vùng.')],[2,3,11.7])
p(d,'Phạm vi baseline: cấu hình và mã nguồn đang có trong workspace tại ngày lập tài liệu. Các mục tiêu chất lượng cần đo trên bản phát hành được ghi riêng trong mục 3.3 và phụ lục A.')
toc(d)
page(d,'1 Giới thiệu',1)
h(d,'1.1 Mục đích',2)
p(d,'Tài liệu xác định những gì G10 phải thực hiện khi người chơi vận hành tàu, khảo sát sinh vật, gửi ảnh, thu thập mẫu, nâng cấp và hoàn thành hành trình. Lập trình viên dùng các yêu cầu để triển khai; thiết kế dùng để kiểm soát luật chơi; kiểm thử viên dùng mã yêu cầu và tiêu chí kiểm chứng để xây dựng ca kiểm thử.')
h(d,'1.2 Phạm vi',2)
p(d,'G10 là game 2D một người chơi, tương tác bằng các thiết bị trong cabin tàu ngầm. Sản phẩm kết hợp khám phá, khảo sát sinh học, quản lý năng lượng và kinh dị môi trường. Phạm vi gồm bốn vùng logic, 11 địa điểm khảo sát, hai kết thúc và hệ thống lưu tiến trình cục bộ. Chín địa điểm thuộc tuyến chính và hai địa điểm ẩn ở Zone04.')
p(d,'Phiên bản này không đặt yêu cầu cho tài khoản trực tuyến, chơi nhiều người, giao dịch trong game, bảng xếp hạng, lưu đám mây hoặc dịch vụ máy chủ. SEND là thao tác nộp dữ liệu nhiệm vụ trong game; không hàm ý tải ảnh ra Internet.')
h(d,'1.3 Thuật ngữ và chữ viết tắt',2)
table(d,['Thuật ngữ','Ý nghĩa'],[
 ('SRS','Đặc tả yêu cầu phần mềm.'),('PHOTO','Biên nhận nhiệm vụ sau khi SEND ảnh đủ điều kiện thành công.'),('CAPTURE','Biên nhận bắt mẫu thành công; tách biệt với PHOTO.'),('POI','Địa điểm tương tác có ID ổn định, vị trí, độ sâu và bán kính.'),('Timeline','Trạng thái hành trình hiện tại, đầu ngày và các checkpoint Journal.'),('Receipt','Dấu ghi nhận một sự kiện đã được lưu bền vững.'),('Module','Nâng cấp bắt buộc để mở khả năng hoặc tuyến hành trình.'),('Cargo','Kho vật phẩm và vật liệu, tối đa 12 loại stack.')],[3,13.7])
h(d,'1.4 Tài liệu tham khảo',2)
p(d,'R01 là chuẩn bố cục. R02–R12 là cấu hình, mã nguồn và hồ sơ của G10; danh mục đầy đủ ở phụ lục C. Mã nguồn và asset đang dùng được ưu tiên khi khác ghi chú triển khai lịch sử.')
h(d,'1.5 Cấu trúc và quy ước',2)
p(d,'Mục 2 mô tả bối cảnh; mục 3 đặc tả yêu cầu kiểm chứng được; phụ lục cung cấp truy vết, điểm cần chốt và nguồn. FR là yêu cầu chức năng, IF là giao diện, PF là hiệu năng, QA là thuộc tính chất lượng và DC là ràng buộc. Từ “phải” biểu thị yêu cầu bắt buộc trong phạm vi nêu kèm.')
page(d,'2 Mô tả tổng quan',1)
h(d,'2.1 Bối cảnh sản phẩm',2)
p(d,'G10 chạy như ứng dụng game độc lập. Người chơi quan sát cabin cố định và mở các trạm Bản đồ, Bàn lái, Radar, Camera, Capture và Computer. Computer cung cấp Photo Lab, Ship Status, Mission Log, Upgrade, Journal, Rest và Cargo. Một cabin dùng chung được gắn lại dữ liệu theo vùng logic; việc sang vùng không làm mất trạng thái hành trình.')
pic(d,context,'Hình 1  Ngữ cảnh hệ thống và hai nhóm dữ liệu lưu cục bộ.',14.2)
h(d,'2.2 Chức năng chính',2)
p(d,'Vòng chơi gồm đọc bản đồ và định vị, điều khiển tàu theo vị trí và độ sâu, quét radar, chụp và gửi ảnh, bắt mẫu khi nhiệm vụ yêu cầu, quản lý vật liệu, lắp module rồi di chuyển thật đến lối ra. Nghỉ chuyển sang ngày mới; Journal và checkpoint đầu ngày hỗ trợ phục hồi. Zone04 cho phép kết thúc tuyến chính hoặc tiếp tục tìm tín hiệu ẩn.')
h(d,'2.3 Đặc điểm người sử dụng',2)
p(d,'Người chơi cần thao tác chuột, bàn phím và hiểu hướng dẫn bằng tiếng Việt cùng một số nhãn thiết bị tiếng Anh. Tutorial giới thiệu các trạm và luật khảo sát. Người kiểm thử cần tách save thử nghiệm khỏi hành trình cá nhân. Người thiết kế nội dung cấu hình POI, nhiệm vụ và hiệu ứng trong Unity, không phải vai trò người chơi trong bản phát hành.')
h(d,'2.4 Ràng buộc tổng thể',2)
p(d,'Dự án dùng Unity 6000.4.2f1, URP 17.4.0, Input System 1.19.0 và uGUI 2.0.0. Kích thước giao diện tham chiếu là 1920 × 1080. Dữ liệu nhiệm vụ dùng ID ổn định; save hiện tại là phiên bản 6. Nền tảng nghiệm thu đề xuất là Windows máy tính; cấu hình phần cứng tối thiểu cần chốt theo phụ lục A. Thiết bị cần màn hình, chuột, bàn phím và đầu ra âm thanh; game không dùng camera vật lý. [R02, R04, R08]')
h(d,'2.5 Giả định và phụ thuộc',2)
p(d,'Thiết bị phải có quyền ghi vào vùng dữ liệu ứng dụng, thiết bị nhập và đầu ra âm thanh hoạt động. Tệp cấu hình bốn vùng, mission catalog và tài nguyên được đóng gói hợp lệ. Ngày trong game chỉ tăng qua nghỉ hoặc phục hồi ngày; không đồng nhất với ngày lịch. Phân loại độ tuổi, thời lượng chơi mục tiêu và điều khoản phát hành thương mại còn thuộc quyết định sản phẩm.')
page(d,'3 Yêu cầu cụ thể',1)
h(d,'3.1 Giao diện bên ngoài',2)
req(d,'IF-01','Giao diện cabin và máy tính','Hệ thống phải cho phép mở các trạm bằng điểm tương tác trong cabin và mở các ứng dụng tương ứng trên Computer; chỉ cho phép thao tác trên cửa sổ hoặc modal đang có quyền nhận nhập.','Mở lần lượt các trạm; khi capture modal hoạt động, thao tác phía sau không điều khiển tàu hoặc chạy ứng dụng khác.','R07, R08')
req(d,'IF-02','Nhập liệu và dừng điều khiển','Hệ thống phải nhận chuột và bàn phím qua Input System. Rời Bàn lái, mất focus hoặc tạm dừng phải phanh tàu; quay lại Bàn lái phải đợi trạng thái nhập trung tính trước khi nhận phím đang giữ.','Giữ phím di chuyển khi đổi tab; tàu dừng và không tự chạy lại đến khi thả rồi nhập mới.','R06, R07')
req(d,'IF-03','Thông tin hiển thị','Bàn lái phải hiển thị vị trí X Y, hướng, độ sâu, tốc độ và trạng thái nguồn lực. Mission Log phải hiển thị đúng nhiệm vụ, độ sâu mục tiêu và hướng dẫn tiến trình của vùng đang hoạt động.','So sánh giá trị hiển thị với trạng thái thực sau lái tàu, đổi vùng, Continue và Restore Day.','R04, R07')
req(d,'IF-04','Tệp và truyền thông','Hệ thống phải lưu timeline dạng JSON tại vùng Expedition và ảnh dạng PNG kèm siêu dữ liệu tại vùng ảnh của ứng dụng. Các chức năng chơi cốt lõi phải thực hiện được khi không có Internet.','Chạy offline; chụp ảnh, SEND, lưu và Continue vẫn hoạt động; không yêu cầu đăng nhập.','R04, R05')
pic(d,ROOT/'Docs/QA/Zone01QuickPhoto/01-photo-lab-before-send.png','Hình 2  Photo Lab thực tế với thông tin ảnh và thao tác SEND. Ảnh minh họa giao diện, không thay thế tiêu chí kiểm chứng.',15.5)
page(d,'3.2 Chức năng hệ thống',2)
h(d,'3.2.1 Bắt đầu và điều khiển hành trình',3)
req(d,'FR-01','Bắt đầu hành trình mới','NEW GAME phải thay thế timeline đang chơi bằng hành trình sạch tại Zone01 ngày 1, xóa tiến độ nhiệm vụ, ảnh có thẩm quyền, vật liệu, nâng cấp và Journal của hành trình trước. Tùy chọn âm thanh và hiển thị được giữ.','Tạo save đã tiến xa rồi NEW GAME; kiểm tra các dữ liệu hành trình trở về ban đầu và cấu hình đầu ra vẫn giữ.','R04, R07')
req(d,'FR-02','Tiếp tục hành trình','CONTINUE phải phục hồi vùng logic, ngày, vị trí, hướng, độ sâu, nguồn lực, nhiệm vụ, cargo, nâng cấp và trạng thái kết thúc đã lưu. Việc khởi động ứng dụng không được tự xóa timeline.','Lưu ở từng vùng rồi tải lại; trạng thái khớp snapshot. Save đã kết thúc mở đúng Ending.','R04, R07')
req(d,'FR-03','Hướng dẫn người chơi','Tutorial phải chỉ cho phép các trạm và ứng dụng phù hợp từng bước, ghi nhận kiến thức đã học và tránh phát lại các bước đã hoàn thành sau Continue.','Hoàn thành một phần tutorial rồi tải lại; chỉ bước còn thiếu được hướng dẫn, không nhận thưởng tutorial lặp.','R04, R09')
req(d,'FR-04','Điều khiển tàu','Người chơi phải có thể tiến, lùi, đổi hướng, lặn và nổi trong giới hạn bản đồ cùng khả năng tàu. Quy ước hướng là 0 độ Bắc, tăng theo chiều kim đồng hồ. Độ sâu không được vượt khoảng cho phép.','Thử các đầu vào và biên bản đồ, biên độ sâu trước và sau lắp Pressure Hull.','R06')
req(d,'FR-05','Năng lượng và va chạm','Thời gian vận hành động cơ, quay hoặc đổi độ sâu phải tiêu hao năng lượng theo tốc độ tiêu hao hiện tại, không cộng chồng phí khi thực hiện đồng thời. Va chạm địa hình làm mất Hull theo độ lớn tốc độ va chạm.','So sánh vận hành 1 giây cho các tổ hợp nhập; va chạm tốc độ v làm giảm v Hull trong điều kiện kiểm thử tương ứng.','R06')
req(d,'FR-06','Phân biệt cạn năng lượng và hỏng tàu','Hết Energy phải ngăn di chuyển và cho phép luồng nghỉ hoặc cứu hộ hợp lệ. Hull bằng 0 phải chạy luồng tàu bị phá hủy và phục hồi checkpoint đầu ngày; không tạo thêm Journal day.','Thử hai tình huống riêng; kiểm tra vùng, cargo, ảnh và mission sau phục hồi do Hull.','R04, R06')
page(d,'3.2.2 Radar bản đồ và di chuyển giữa vùng',3)
req(d,'FR-07','Quét radar','Quét radar phải kiểm tra lượt quét và trạng thái hành trình, tiêu hao đúng một lượt khi hợp lệ và hiện tín hiệu theo vị trí tương đối, hướng quét, địa hình cùng điều kiện phát hiện.','Quét hợp lệ, hết lượt, bị modal chặn và bị địa hình cản; đối chiếu contact và lượt còn lại.','R06, R07')
req(d,'FR-08','Bản đồ và địa điểm','Bản đồ phải thể hiện các địa điểm được phép thấy và checklist từ biên nhận nhiệm vụ thực. Bản đồ Zone04 phải cho phép bật tắt lớp vật cản tĩnh; lớp này không tiết lộ địa điểm ẩn chưa khám phá hoặc tự tạo tuyến đi.','Đối chiếu checklist sau SEND và rollback; bật tắt địa hình Zone04 và kiểm tra marker ẩn.','R03, R07, R11')
req(d,'FR-09','Điều kiện mở lối','Hoàn thành mục tiêu bắt buộc và module của vùng phải mở quyền đi tiếp, nhưng không tự chuyển vùng. Hệ thống chỉ thực hiện chuyển khi có điều kiện tiếp cận lối ra bằng chuyển động mới của tàu.','Hoàn tất mission khi ở xa lối ra; vùng không đổi. Đi vào khu vực lối ra sau mở khóa mới chuyển.','R03, R07')
req(d,'FR-10','Hành trình và quay lại','Chuyển vùng phải giữ ngày, tài nguyên, nâng cấp, trạng thái từng vùng và ngăn thao tác chuyển trùng. Cổng quay về vùng đã thăm phải yêu cầu quan sát tàu ở ngoài rồi đi vào lại.','Continue trong cổng quay về không tự chuyển; di chuyển ra ngoài rồi trở lại mới thực hiện.','R04, R07, R09')
req(d,'FR-11','Độ sâu và tiếp cận mục tiêu','Chụp, quét và bắt mẫu phải dùng cùng dữ liệu vị trí, POI và độ sâu của vùng. Chỉ dẫn tọa độ không được tính như đã phát hiện hoặc đã hoàn thành địa điểm.','Thử cùng X Y ở độ sâu đúng và sai; đọc hint không tạo radar discovery hoặc PHOTO.','R03, R05, R06')
page(d,'3.2.3 Chụp ảnh và gửi dữ liệu',3)
req(d,'FR-12','Chụp nhanh từ cabin','Camera hotspot phải thực hiện chụp nhanh, ghi ảnh cùng thời gian, vị trí, độ sâu, hướng và kết quả chất lượng. Người chơi xem ảnh trong Photo Lab; không cần mở một camera live riêng.','Chụp từ cabin; ảnh mới xuất hiện trong Photo Lab với metadata khớp thời điểm chụp.','R05, R07, R10')
req(d,'FR-13','Phân loại ảnh','Hệ thống phải phân loại ảnh theo chủ thể, che khuất, khả năng nhìn thấy và tỷ lệ phủ: NoSubject, LifeDetected, GoodPhoto, TooFar, Obstructed hoặc LowVisibility. Chỉ GoodPhoto hoặc LifeDetected đúng định danh nhiệm vụ mới có thể là ứng viên SEND.','Dùng các tình huống đúng chủ thể, quay sai hướng, xa và bị che; kiểm tra candidate theo kết quả và ID.','R05')
req(d,'FR-14','Nộp ảnh có chủ đích','Chụp ảnh không được tự hoàn thành PHOTO. Người chơi phải chọn SEND cho ảnh đủ điều kiện ở đúng vùng; PHOTO chỉ được ghi sau khi giao dịch timeline thành công.','Chụp hợp lệ rồi mở Mission Log trước SEND và sau SEND; trạng thái chỉ thay đổi ở lần ghi lưu thành công.','R03, R04, R05')
req(d,'FR-15','Không nhận thưởng lặp','SEND lại ảnh đã nộp hoặc ảnh của objective đã hoàn thành không được tăng tiến độ, cấp thêm vật liệu, tạo CAPTURE hoặc mở khóa lặp.','Gọi SEND nhiều lần trên cùng objective; so sánh receipt, cargo và số lượng thưởng.','R04, R05')
req(d,'FR-16','Lỗi lưu và quản lý ảnh','Khi timeline ghi lỗi, ảnh phải giữ trạng thái chưa gửi để thử lại. Xóa ảnh phải có xác nhận, cảnh báo khi ảnh chưa SEND, không hủy receipt nhiệm vụ đã được lưu và không làm ảnh đã xóa tự trở lại từ archive hiện tại hoặc backup hiện hành.','Mô phỏng lỗi ghi khi SEND và xóa; thử Continue sau xóa; phục hồi Journal cũ theo snapshot riêng.','R04, R05')
pic(d,photo,'Hình 3  Trạng thái PHOTO phụ thuộc thao tác SEND và ghi lưu bền vững.',14.2)
page(d,'3.2.4 Bắt mẫu và kho vật liệu',3)
req(d,'FR-17','Kiểm tra trước khi bắt mẫu','Bắt mẫu phải kiểm tra POI, sinh vật, cự ly, độ sâu, điều kiện quan sát và PHOTO tiên quyết, trạng thái modal cùng sức chứa. Khi không hợp lệ, hệ thống phải trả lý do và không cấp vật phẩm.','Thử thiếu PHOTO, sai độ sâu, ngoài tầm, kho đầy loại mới và hành trình bị khóa.','R03, R06')
req(d,'FR-18','Minigame bắt mẫu','Lượt hợp lệ phải mở minigame độc quyền. W S hoặc mũi tên lên xuống điều khiển góc móc; người chơi phải đạt số lần chạm cấu hình trước khi hết giờ. Profile hiện tại yêu cầu 5 lần chạm trong 45 giây.','Kiểm tra điều khiển, đếm chạm, cooldown, hết giờ, Escape và nút thoát; không ghi nhiều hit từ một tiếp xúc liên tục.','R06, R08')
req(d,'FR-19','Kết quả bắt mẫu','Chỉ kết quả thành công sau kiểm tra lại điều kiện mới được thêm mẫu và ghi CAPTURE. Thất bại hoặc hủy không được hoàn thành objective. Callback lặp không được cấp vật liệu lần nữa.','Thử thành công, hết giờ, hủy và callback trùng; cargo và receipt chỉ tăng đúng một lần.','R04, R06')
req(d,'FR-20','Chế độ lượt bắt','Hệ thống phải hỗ trợ cấu hình lượt bắt hữu hạn hoặc không giới hạn. Cấu hình cabin hiện tại bật unlimitedCaptureAttempts, nên bắt mẫu không bị từ chối vì chỉ số lượt bắt bằng 0; các điều kiện khác vẫn có hiệu lực.','Kiểm tra cả hai chế độ; chế độ hữu hạn tiêu hao một lượt khi bắt đầu hợp lệ, chế độ hiện tại không giảm lượt.','R06, R08')
req(d,'FR-21','Stack và vật liệu lặp','Cargo phải chứa tối đa 12 loại stack, cộng số lượng vào stack đã có kể cả khi đủ 12 loại. Chỉ Z2_Creature_02 và Z3_Creature_01 là nguồn bắt lặp để lấy vật liệu; receipt và thưởng objective không lặp.','Lấp đủ 12 loại rồi cộng loại đã có và loại mới; bắt lại hai nguồn farm và một nguồn một lần.','R03, R04, R06')
req(d,'FR-22','Tiêu thụ nguyên tử','Giao dịch dùng vật liệu phải cộng gộp yêu cầu cùng item ID, kiểm tra đủ tổng lượng rồi mới thay đổi. Nếu hành động hoặc ghi lưu lỗi, số lượng vật liệu, cấp nâng cấp và chỉ số tàu phải trở về trạng thái trước giao dịch.','Dùng yêu cầu trùng ID và lỗi ghi lưu có chủ đích; không được tiêu thụ một phần hoặc cấp nâng cấp miễn phí.','R04, R06')
page(d,'3.2.5 Nội dung nhiệm vụ và module',3)
p(d,'Danh mục dưới đây là phạm vi chuẩn của bản hiện tại. PHOTO luôn bao gồm SEND thành công. Mỗi CAPTURE yêu cầu PHOTO tại cùng địa điểm trước đó. Hai địa điểm ẩn không thuộc chín ảnh tuyến chính. [R03]')
table(d,['Vùng và điểm','Sinh vật','Công việc','Vai trò'],[
 ('Z1 L1','Z1_Creature_01','PHOTO + CAPTURE','Vật liệu Bio Lamp'),('Z1 L2','Z1_Creature_02','PHOTO','Khảo sát chính'),
 ('Z2 L1','Z2_Creature_01','PHOTO + CAPTURE','Hull material A'),('Z2 L2','Z2_Creature_02','PHOTO + CAPTURE','Hull material B; farm'),
 ('Z3 L1','Z3_Creature_01','PHOTO + CAPTURE','Explosive; farm'),('Z3 L2','Z3_Creature_02','PHOTO','Khảo sát chính'),('Z3 L3','Z3_Creature_03','PHOTO + CAPTURE','Detonator'),
 ('Z4 L1','Z4_Creature_01','PHOTO + CAPTURE','Mẫu và hint tọa độ'),('Z4 L2','Z4_Creature_02','PHOTO','Khảo sát chính'),('Z4 L3 ẩn','Z4_Creature_03','PHOTO','Nhánh ẩn'),('Z4 L4 ẩn','Z4_Creature_04','PHOTO','Nhánh ẩn')],[2.3,4.1,4.2,6.1])
req(d,'FR-23','Module bắt buộc theo vùng','Zone01 phải lắp Bio Lamp bằng 1 Z1_Creature_01. Zone02 phải lắp Pressure Hull bằng 1 mẫu mỗi loại Z2_Creature_01 và Z2_Creature_02, nâng trần sâu từ 500 lên ít nhất 750 m. Quyền sang vùng chỉ có khi đủ fieldwork và gate.','Thiếu từng thành phần phải bị chặn; giao dịch đúng tiêu hao một lần và giữ quyền sau Continue.','R03, R04')
req(d,'FR-24','Lắp và sử dụng Rock Breaker','Zone03 phải tách CRAFT, INSTALL và phá rào đá tại vị trí tương tác thực. CRAFT dùng 1 Z3_Creature_01 và 1 Z3_Creature_03; INSTALL không tiêu thụ lại. Phá rào cập nhật bản đồ, địa hình và trạng thái tuyến.','Thử sai thứ tự và sai vị trí; sau phá rào và reload vẫn dùng địa hình đã thay đổi.','R03, R04, R07')
page(d,'3.2.6 Nâng cấp thường ngày nghỉ và thất bại',3)
req(d,'FR-25','Ba nhánh nâng cấp thường','Hệ thống phải cung cấp Hull, Propulsion và Energy Efficiency, mỗi nhánh từ cấp 0 đến 2; cấp 2 yêu cầu cấp 1, MAX không thể mua tiếp. Chỉ số mục tiêu phải được gán đúng, không cộng dồn qua mỗi lần reload.','Đối chiếu bảng chỉ số; mua, Continue và Restore Day không nhân thêm tác dụng.','R04')
table(d,['Nhánh','Cấp 0','Cấp 1','Cấp 2'],[
 ('Hull capacity','100','120','140'),('Tốc độ tiến tối đa','18','25','36'),('Năng lượng mỗi giây vận hành','1.00','0.80','0.50'),
 ('Chi phí Hull và Energy','Không áp dụng','2 Z2_Creature_02','2 Z3_Creature_01'),('Chi phí Propulsion','Không áp dụng','1 Z2_Creature_02','2 Z3_Creature_01')],[5.2,2.6,4.4,4.5])
p(d,'Các giá trị trên là cấu hình hiện tại; tốc độ dùng đơn vị bản đồ mỗi giây. Hull upgrade tăng sức chứa, không hồi máu miễn phí. Pressure Hull mở độ sâu riêng, không thay vai trò của nhánh Hull. [R04, R08]')
req(d,'FR-26','Giới hạn ngày chung','Hành trình mới phải bắt đầu ngày 1 với tổng 25 ngày theo cấu hình hiện tại. DaysLeft = max(0, TotalDays − Day + 1). Đổi vùng không đặt lại ngày; sang ngày 26 mà chưa ghi ending phải chuyển trạng thái thất bại.','Kiểm tra ngày 1, 25, 26 và chuyển vùng; hoàn tất riêng nhiệm vụ vùng không miễn deadline.','R04, R08')
req(d,'FR-27','Nghỉ và hồi nguồn lực','REST có xác nhận phải lưu checkpoint cuối ngày, tăng một ngày và hồi nguồn lực tại chỗ; giữ hành trình, nhiệm vụ và nâng cấp. Luồng phải khóa thao tác chồng trong chuyển cảnh nghỉ. Lỗi ghi lưu không được tăng ngày hoặc tạo checkpoint mới.','Nghỉ khi cạn Energy và khi đang tương tác; thử nhấn kép và lỗi lưu. REST không tự đổi sang cứu hộ dịch chuyển.','R04, R07')
req(d,'FR-28','Phục hồi Journal','Restore Day có xác nhận phải trả về snapshot cuối ngày đã chọn và xóa các Journal entry tương lai. Tiến độ, cargo, ảnh và receipt hiệu ứng phải trở về trạng thái ở checkpoint; trạng thái thất bại chỉ hết khi checkpoint còn chơi được.','Tạo các ngày khác nhau rồi rollback; Continue giữ timeline sau rollback và không phục sinh dữ liệu tương lai.','R04')
page(d,'3.2.7 Lưu tiến trình và độ tin cậy giao dịch',3)
req(d,'FR-29','Timeline thống nhất','Hệ thống phải dùng một chủ sở hữu timeline cho trạng thái hiện tại, checkpoint đầu ngày và Journal. Các giao dịch SEND, capture, nâng cấp, nghỉ, chuyển vùng và ending phải lưu nhất quán các dữ liệu liên quan.','Đối chiếu snapshot sau mỗi giao dịch và sau khởi động lại; không có objective đã nhận nhưng thiếu cập nhật thuộc cùng giao dịch.','R04')
req(d,'FR-30','Ghi an toàn và phục hồi bản dự phòng','Ghi timeline phải thay thế an toàn cùng bản dự phòng phục hồi. Nếu primary không đọc được, hệ thống phải thử backup; khi cả hai không hợp lệ, phải giữ tệp và báo lỗi thay vì tự ghi đè bằng hành trình mới.','Tạo primary hỏng và backup hợp lệ, rồi làm hỏng cả hai; kiểm tra dữ liệu được bảo toàn.','R04')
req(d,'FR-31','Tương thích save','Hệ thống phải đọc save phiên bản 1–5 và chuyển sang phiên bản 6 cho current, dayStart và Journal. Việc đổi ID nhiệm vụ phải giữ quyền tuyến cũ, số lượng vật liệu và kết thúc đã đạt mà không tự tạo CAPTURE mới hoặc cấp cargo lại.','Dùng fixture từng phiên bản; tải hai lần không áp dụng migration hai lần; dữ liệu v6 không hợp lệ bị từ chối.','R04, R09')
req(d,'FR-32','Điểm lưu ứng dụng','Hệ thống phải lưu trạng thái khi rời gameplay và tại các sự kiện vòng đời ứng dụng đã hỗ trợ, đồng thời báo lỗi ghi cho người chơi. Không được hiển thị giao dịch thành công nếu timeline chưa được ghi thành công.','Mô phỏng rời game, pause ứng dụng và lỗi quyền ghi; kiểm tra thông báo và dữ liệu trên đĩa.','R04, R05, R07')
h(d,'3.2.8 Kết thúc và trình diễn kinh dị',3)
req(d,'FR-33','Normal Ending','Khi đủ mục tiêu chính Zone04, gồm Z4 L1 CAPTURE, hệ thống phải cho người chơi chọn kết thúc hoặc tiếp tục khám phá. Chọn kết thúc phải lưu ID normal và mở Ending tương ứng. Nhãn 09 trên 09 chỉ phản ánh ảnh, không thay thế CAPTURE.','Thử thiếu CAPTURE dù đủ chín ảnh; sau đủ mục tiêu, chọn kết thúc và Continue về normal.','R03, R07, R11')
req(d,'FR-34','Hidden Ending','Nhánh ẩn phải yêu cầu chọn tiếp tục khám phá, khám phá radar và SEND ở cả hai điểm ẩn, đủ công việc chính, rồi di chuyển thật tới điểm tín hiệu cuối. Hint hoặc đủ hai ảnh ẩn không tự ghi ending.','Thử thiếu từng điều kiện; mở tín hiệu khi đủ và chỉ ghi hidden sau tiếp cận hợp lệ.','R03, R07')
page(d,'3.2.9 Tách hiệu ứng trình diễn khỏi luật chơi',3)
req(d,'FR-35','Quyền hạn hiệu ứng','Âm thanh, ánh sáng, bóng phản chiếu và nhiễu thiết bị chỉ được thay đổi trình diễn. Hiệu ứng không được tự tạo contact, hoàn thành PHOTO, cấp mẫu, tiêu hao lượt hoặc giả lập chuyển động đến đích.','Chụp trạng thái trước và sau từng cue khi đứng yên; chỉ presentation state được phép thay đổi.','R10, R11')
req(d,'FR-36','Audience và receipt hiệu ứng','Cue một lần phải gắn với đúng thiết bị hoặc ảnh đang xem, có khả năng hủy khi pause, mất focus hoặc đóng audience. Với Zone04, gián đoạn trước checkpoint tiếp nhận phải giữ cơ hội thử lại; receipt chỉ lưu sau checkpoint quy định.','Đóng Photo Lab trước checkpoint rồi mở đúng ảnh; cue được thử lại. Sau receipt bền vững, Continue không lặp cue một lần.','R11')
req(d,'FR-37','Đầu ra và điều khiển khi kinh dị','Hệ thống phải tôn trọng cài đặt âm lượng, mute và giảm rung. Khi rút âm môi trường, máy tàu, thao tác và cảnh báo phải giữ kênh của chúng. Cue không được tự mở hoặc cưỡng ép đổi cửa sổ để trình diễn.','Mute từng nhóm, tắt rung và đổi thiết bị; đối chiếu điều khiển, cảnh báo cùng dữ liệu gameplay.','R10, R11')
req(d,'FR-38','Trình diễn kết thúc','Normal Ending phải dùng chuỗi trình diễn 12 giây; Hidden Ending 16 giây theo cấu hình hiện tại. Pause và resume phải giữ tuổi chuỗi, không phát lại tiếng tấn công đầu chuỗi; nút về Main Menu vẫn khả dụng.','Pause giữa mỗi ending rồi resume; kiểm tra thứ tự cue, số ảnh theo receipt và Continue không tạo tiến độ mới.','R07, R11')
h(d,'Quy tắc khi thiết bị lưu không khả dụng',3)
p(d,'Nếu cue đã được tiếp nhận nhưng chưa ghi được receipt, phiên hiện tại phải tránh phát lại phần mở đầu và thử ghi lại với khoảng chờ. Không cam kết phát đúng một lần xuyên crash hoặc reload khi thiết bị lưu vẫn lỗi. Giới hạn này áp dụng cho presentation receipt; giao dịch nhiệm vụ vẫn tuân thủ yêu cầu ghi nguyên tử. [R11]')
h(d,'Nguồn lực khởi tạo đang áp dụng',3)
table(d,['Tham số','Giá trị','Ý nghĩa'],[
 ('Energy và Hull','100 và 100','Hồi đầy theo luồng REST hợp lệ'),('Radar và Photo','10 và 20','Lượt dùng thiết bị giữa các lần hồi nguồn lực'),('Capture','Không giới hạn','unlimitedCaptureAttempts đang bật'),('Cargo','12 loại stack','Mỗi loại có số lượng dương')],[4.6,4.6,7.5])
page(d,'3.2.10 Các trường hợp sử dụng chính',3)
p(d,'Tác nhân chính trong các ca sử dụng là người chơi. Hệ thống tệp là bên ngoài hỗ trợ lưu; nó không phải người dùng có tài khoản. Các bước dưới đây mô tả hành vi quan sát được để kiểm thử chấp nhận.')
h(d,'UC 01 Khảo sát và gửi ảnh',3)
p(d,'Tiền điều kiện: hành trình chơi được; còn lượt ảnh; địa điểm có objective PHOTO đang khả dụng. Kích hoạt: người chơi chọn camera hotspot. Hậu điều kiện thành công: có receipt PHOTO và ảnh đã SEND trong timeline.')
for txt in ['1. Người chơi đọc vị trí và độ sâu mục tiêu, điều khiển tàu tiếp cận và hướng về chủ thể.','2. Camera chụp nhanh và lưu ảnh cùng kết quả chất lượng.','3. Người chơi mở Computer rồi Photo Lab, chọn ảnh vừa chụp.','4. Hệ thống kiểm tra định danh, trạng thái objective và vùng của ảnh.','5. Người chơi chọn SEND; hệ thống ghi timeline, sau đó cập nhật checklist và phản hồi.']:p(d,txt)
p(d,'Ngoại lệ: ảnh không đủ điều kiện hoặc objective đã hoàn thành thì không cấp tiến độ; lỗi ghi timeline giữ ảnh chưa gửi; lỗi archive phụ sau khi timeline đã thành công không rút lại PHOTO. Yêu cầu liên quan: FR-12–FR-16.')
h(d,'UC 02 Bắt mẫu và lắp module',3)
p(d,'Tiền điều kiện: PHOTO đã SEND; đến đúng cự ly và độ sâu; kho nhận được mẫu. Hậu điều kiện: CAPTURE và cargo được lưu; sau lắp hợp lệ module và vật liệu nhất quán.')
for txt in ['1. Người chơi chọn trạm Capture; hệ thống kiểm tra điều kiện rồi mở minigame.','2. Người chơi điều khiển móc để đạt 5 lần chạm trong 45 giây theo profile hiện tại.','3. Hệ thống kiểm tra kết quả và điều kiện cuối lượt, thêm mẫu và receipt.','4. Người chơi mở Upgrade, chọn Expedition Module và thực hiện hành động đang được mở.','5. Hệ thống kiểm tra công thức và vật liệu, tiêu thụ một lần, cập nhật khả năng tàu, lưu rồi mở quyền tuyến.']:p(d,txt)
p(d,'Ngoại lệ: hủy hoặc hết giờ không cấp mẫu; vật liệu thiếu không lắp; lỗi ghi lưu rollback toàn bộ giao dịch. Rock Breaker có thêm các bước CRAFT, INSTALL và phá rào tại chỗ. Yêu cầu liên quan: FR-17–FR-24.')
page(d,'3.2.11 Các trường hợp sử dụng phục hồi và hoàn tất',3)
h(d,'UC 03 Nghỉ và phục hồi ngày',3)
p(d,'Tiền điều kiện: hành trình không bị khóa bởi giao dịch hoặc modal, người chơi xác nhận REST. Hậu điều kiện: cuối ngày N có checkpoint, bắt đầu ngày N cộng 1 với nguồn lực hồi đầy tại vị trí nghỉ.')
p(d,'Luồng chính: mở REST → xem và xác nhận → khóa thao tác chuyển cảnh → ghi checkpoint và ngày mới → hiển thị số ngày còn → trả quyền điều khiển. Lỗi lưu giữ nguyên ngày và Journal, đồng thời giải phóng khóa để người chơi thử lại. Nếu sang ngày 26 mà chưa có ending, áp dụng trạng thái thất bại. [FR-26, FR-27]')
p(d,'Restore Day: chọn Journal entry → xem cảnh báo mất tiến độ tương lai → xác nhận → áp dụng snapshot và cắt Journal tương lai → lưu lại → mở đúng vùng và trạng thái. Tàu bị phá hủy dùng checkpoint đầu ngày, tách biệt với thao tác tự chọn checkpoint cuối ngày. [FR-06, FR-28]')
h(d,'UC 04 Hoàn tất nhánh ẩn',3)
p(d,'Tiền điều kiện: đủ mục tiêu chính và chọn tiếp tục khám phá tại Zone04. Hậu điều kiện: sau khi đủ điều kiện và tiếp cận thật điểm tín hiệu cuối, timeline lưu hidden và mở Ending.')
p(d,'Luồng chính: đọc hint trong Mission Log → tự điều hướng tới từng tọa độ → radar phát hiện điểm ẩn → chụp ảnh và SEND ở cả hai điểm → xem tín hiệu cuối được mở → lái tàu tới khu vực đích → hệ thống lưu ending → chạy chuỗi kết thúc. Đang đứng sẵn trong khu vực khi Continue không được tính như một lần tiếp cận mới. [FR-09, FR-11, FR-34]')
h(d,'Các tình huống nghiệm thu biên',3)
table(d,['Tình huống','Kết quả cần quan sát'],[
 ('Đủ 09 ảnh nhưng thiếu Z4 L1 CAPTURE','Không bỏ qua mục tiêu bắt mẫu để kết thúc.'),('Đã biết tọa độ ẩn nhưng chưa quét radar','Không có discovery hoặc PHOTO tự sinh.'),('Lỗi lưu khi mua nâng cấp','Vật liệu, cấp và chỉ số tàu giữ trạng thái trước mua.'),('Hủy cue trước checkpoint tiếp nhận','Giữ cơ hội trình diễn lại theo audience.'),('Hỏng primary và backup','Giữ tệp lỗi, báo không thể tải; không reset ngầm.'),('Restore checkpoint trước khi mở vùng','Quyền tuyến và các receipt trở về checkpoint.')],[6.1,10.6])
page(d,'3.3 Yêu cầu hiệu năng',2)
p(d,'PF-01 đến PF-04 là mục tiêu nghiệm thu đề xuất, chưa phải số đo đã đạt. Áp dụng trên máy tham chiếu Windows 64 bit, độ phân giải 1920 × 1080, chất lượng mặc định và bản Player release; cấu hình CPU GPU RAM cụ thể phải được ghi trong biên bản nghiệm thu trước khi đo. [TBD A01]')
table(d,['Mã','Mục tiêu đề xuất','Phép đo'],[
 ('PF-01','Tốc độ khung hình trung vị ít nhất 60 FPS; phân vị 95 thời gian khung hình không quá 33,3 ms.','Ghi 10 phút vận hành, quét, chụp và hiệu ứng tại cả bốn vùng; tách màn tải.'),
 ('PF-02','Phản hồi trực quan thao tác nút thông thường không quá 200 ms ở phân vị 95.','Đo ít nhất 100 lần mở trạm hoặc chọn chức năng hợp lệ.'),
 ('PF-03','SEND và lưu checkpoint hoàn tất không quá 2 giây ở phân vị 95 với timeline 25 ngày và thư viện hiện tại.','Đo tối thiểu 30 lần trên ổ lưu trữ tham chiếu; có hiển thị trạng thái khi chờ.'),
 ('PF-04','Chuyển vùng hoặc Continue tới trạng thái điều khiển được không quá 10 giây ở phân vị 95.','Đo ít nhất 20 lần, ghi rõ cache nóng hoặc lạnh và kích thước save.')],[1.7,7.5,7.5])
h(d,'3.4 Yêu cầu dữ liệu logic',2)
p(d,'Kho dữ liệu là tệp cục bộ; không yêu cầu DBMS. Timeline là nguồn xác thực tiến độ. Ảnh, nhiệm vụ, cargo và presentation receipts phải liên kết bằng ID ổn định, không serialize tham chiếu tới GameObject trong scene. Bảng dưới quy định ràng buộc logic, không bắt buộc tên lớp triển khai.')
table(d,['Thực thể','Dữ liệu chính','Bất biến'],[
 ('Hành trình','Ngày, vùng, ship, ending, tutorial','Ngày dương; vùng hợp lệ; ending normal hoặc hidden khi đã đạt.'),('Tiến độ vùng','Objective, discovery, gate, voyage, cue','Không nhầm receipt PHOTO với CAPTURE hoặc hint với discovery.'),('Ảnh','ID, PNG, thời gian, X Y, sâu, hướng, result, objective','Ảnh chỉ gửi khi định danh khớp; giữ receipt nếu xóa ảnh đã gửi.'),('Kho mẫu','Item ID và quantity','Tối đa 12 loại; quantity dương; không tiêu thụ một phần.'),('Checkpoint','Snapshot đầu ngày và Journal cuối ngày','Rollback khôi phục thống nhất và cắt các entry tương lai.')],[3.2,6.6,6.9])
page(d,'3.5 Ràng buộc thiết kế',2)
req(d,'DC-01','Nền tảng công nghệ','Bản phát triển phải tương thích Unity 6000.4.2f1, URP 17.4.0, Input System 1.19.0 và uGUI 2.0.0 đang dùng. Thay đổi nền tảng hoặc package ảnh hưởng hành vi phải cập nhật baseline và kiểm thử liên quan.','Biên dịch và khởi động bằng cấu hình dự án; không có lỗi C# hoặc shader ngăn chơi.','R02')
req(d,'DC-02','Cấu hình và định danh nội dung','Danh mục nhiệm vụ phải giữ 2, 2, 3, 4 điểm theo vùng và ID ổn định. Z2 L2 dùng POI zone02-l3; Z3 L3 dùng zone03-l4; Z4 L4 dùng zone04-l5. Mọi thay đổi ID phải có migration dữ liệu.','Đối chiếu bốn mission asset, map và pending photo sau migration.','R03, R09')
req(d,'DC-03','Tách trình diễn và thẩm quyền','Logic nhiệm vụ, giao dịch lưu và chuyển vùng phải có chủ sở hữu riêng với trình diễn. Các cue dùng hệ thống EffectDirector và audience hiện có, không trở thành nguồn quyết định hoàn thành gameplay.','Đối chiếu code và chạy các ca FR-35; hủy hiệu ứng không sửa receipt nhiệm vụ.','R04, R07, R11')
h(d,'3.6 Thuộc tính chất lượng phần mềm',2)
req(d,'QA-01','Toàn vẹn dữ liệu','Mọi lỗi ghi lưu được xử lý phải bảo toàn trạng thái trước giao dịch hoặc công bố trọn vẹn trạng thái mới, kèm thông báo đủ để thử lại hoặc phục hồi.','Fault injection tại SEND, upgrade, REST, deletion và ending; kiểm tra save cùng trạng thái sống.','R04, R05')
req(d,'QA-02','Khả dụng và khả năng tiếp cận','Chữ tiếng Việt, số liệu và nút xác nhận phải đọc được tại 1920 × 1080; nội dung dài phải cuộn thay vì bị cắt. Xóa ảnh và Restore Day phải phân biệt hành động xác nhận và hủy. Cài đặt mute và giảm rung phải có hiệu lực khi chơi.','Kiểm tra trực quan các ứng dụng, nhiệm vụ dài, modal, ending và thao tác chỉ bằng các đầu vào được hỗ trợ.','R07, R10, R11')
req(d,'QA-03','Khả năng kiểm thử và bảo trì','Yêu cầu phải truy vết tới module, nội dung và ca kiểm chứng. Kiểm thử tự động phải dùng đường dẫn save và ảnh riêng, không ghi đè hành trình người chơi.','Kiểm tra cấu hình fixture và ma trận phụ lục B trước chạy kiểm thử.','R12')
page(d,'3.7 Yêu cầu khác và nghiệm thu',2)
req(d,'QA-04','Dữ liệu và hoạt động ngoại tuyến','Ảnh và tiến trình phải lưu cục bộ theo thiết kế hiện tại. Bản game không được yêu cầu bí mật API hoặc tài khoản dịch vụ tạo nội dung để thực hiện hành trình.','Tắt mạng trên bản Player, chạy các chức năng cốt lõi và kiểm tra cấu hình đóng gói không chứa credential.','R02, R04, R05')
req(d,'QA-05','Tài nguyên phát hành','Bản dùng để phát hành thương mại phải có quyền sử dụng thích hợp cho hình, font và âm thanh đi kèm. Tài nguyên có giới hạn noncommercial hoặc attribution phải được xử lý theo điều khoản nguồn trước phát hành.','Đối chiếu asset manifest và hồ sơ provenance; ghi rõ các tài nguyên phải thay thế hoặc bổ sung giấy phép.','R10, R11','Bắt buộc khi phát hành thương mại')
h(d,'3.7.1 Điều kiện nghiệm thu chức năng',3)
p(d,'Chấp nhận chức năng khi các FR và IF bắt buộc có kết quả đạt trên bản Player tham chiếu, đi được tuyến normal và hidden bằng luồng người chơi, không tồn tại lỗi chặn tiến trình hoặc mất dữ liệu có thể tái hiện trong phạm vi kiểm tra. Test có quyền đặt vị trí hoặc seed state phải ghi là kiểm tra logic, không thay thế lượt chơi đầu vào thực.')
p(d,'Bộ kiểm thử phải bao gồm bắt đầu mới, Continue, tutorial, 11 PHOTO qua chụp và SEND thật, 6 CAPTURE bắt buộc, cả hai nguồn farm, module, ba nhánh nâng cấp, bốn vùng, quay lại, ngày 25 và 26, Hull death, Energy depletion, Journal restore, lỗi lưu, migration và hai ending. Các nhánh hủy hoặc điều kiện thiếu phải được kiểm thử riêng.')
h(d,'3.7.2 Hồ sơ kiểm chứng hiện có',3)
p(d,'Hồ sơ Zone04 final chapter ghi 103 EditMode đạt và 1 skip tùy chọn; 82 PlayMode được chọn đạt. Hồ sơ ExperienceLoop kế tiếp ghi 33 PlayMode được chọn đạt. Đây là các lần chạy lịch sử, có phạm vi giao nhau và không được cộng thành tổng ca duy nhất hoặc xem là chứng nhận cho mọi thay đổi workspace hiện tại. [R11, R12]')
p(d,'Kết quả đo hiệu năng bản Player, cấu hình phần cứng tối thiểu, chất lượng âm thanh qua tai nghe và cảm giác kinh dị với người thật cần có biên bản nghiệm thu riêng. Hình minh họa trong tài liệu là ảnh giao diện đã có; không chứng minh toàn bộ tiêu chí đã được đáp ứng.')
h(d,'3.7.3 Kiểm soát thay đổi',3)
p(d,'Mỗi thay đổi yêu cầu phải ghi mã yêu cầu, lý do, ảnh hưởng dữ liệu và kiểm thử; cập nhật phiên bản tài liệu cùng Game Concept khi ảnh hưởng trải nghiệm. Thay đổi cân bằng như tổng ngày, tốc độ, chi phí và chế độ lượt bắt phải cập nhật cả cấu hình và bảng baseline; thay đổi POI phải xem xét migration.')
page(d,'Phụ lục A Các quyết định cần chốt',1)
table(d,['Mã','Điểm cần chốt','Trách nhiệm và tiêu chí đóng'],[
 ('A01','Nền tảng phát hành và máy tham chiếu','Chủ dự án và kỹ thuật chọn Windows version, CPU, GPU, RAM, ổ đĩa; đo PF-01–PF-04.'),('A02','Phạm vi chất lượng đồ họa và sinh vật','Thiết kế hình ảnh chốt creature art riêng, nhất là bốn định nghĩa Zone04 đang dùng fallback; nghiệm thu trong Photo Lab.'),('A03','Phân loại độ tuổi và thông tin nội dung','Chủ dự án chốt theo kênh phát hành; chưa coi nhãn độ tuổi concept là kết quả thẩm định.'),('A04','Thời lượng chơi và cân bằng','Thiết kế xác nhận 25 ngày, bắt không giới hạn và chỉ số nâng cấp qua playtest người thật.'),('A05','Quyền tài nguyên phát hành','Chủ dự án đối chiếu nguồn âm thanh, font, hình và điều kiện thương mại trước đóng gói phát hành.')],[1.6,6.1,9])
h(d,'A.1 Baseline tham số hiện tại',2)
p(d,'Baseline lấy tại 29/10/2026 từ workspace, HEAD 665bf42 kèm thay đổi đang làm. Tổng ngày 25 và chế độ capture không giới hạn khác một số ghi chú cũ. Propulsion cấp 1 và 2 là 25 và 36; Energy Efficiency là 0,80 và 0,50 đơn vị mỗi giây; không dùng bảng 110 phần trăm hoặc 120 phần trăm của tài liệu lịch sử. [R04, R08]')
p(d,'Giá trị profile camera gồm field of view 70 độ, khoảng nhìn 65, goodCoverage 0,035, tooFarCoverage 0,008 và maximumOcclusion 0,35 trong khai báo profile. Asset gắn thực tế có thể ghi đè giá trị mặc định, nên kiểm thử phải ghi rõ profile được sử dụng. [R05]')
h(d,'A.2 Phạm vi áp dụng IEEE 830 1998',2)
p(d,'Cấu trúc tài liệu áp dụng các phần giới thiệu, mô tả tổng quan, yêu cầu cụ thể và phụ lục của IEEE 830-1998. Yêu cầu có mã, tiêu chí kiểm chứng và nguồn truy vết. Tài liệu không phải chứng nhận phù hợp do IEEE cấp. IEEE 830-1998 đã được thay thế bởi ISO IEC IEEE 29148 theo thông tin IEEE SA; phiên bản 1998 vẫn được dùng ở đây theo mục tiêu tài liệu. [R01]')
page(d,'Phụ lục B Ma trận truy vết',1)
p(d,'Ma trận liên kết toàn bộ nhóm yêu cầu với nguồn và cách kiểm tra. Tên fixture là điểm bắt đầu lựa chọn ca; một fixture không mặc nhiên chứng minh tất cả yêu cầu trong nhóm đã đạt.')
table(d,['Yêu cầu','Nguồn','Kiểm chứng hoặc fixture liên quan'],[
 ('IF-01–04','R02, R04–08','ComputerShell, ComputerApps, DirectCabinInteraction; kiểm tra offline và giao diện.'),('FR-01–03','R04, R07, R09','ExpeditionNewGame, ExpeditionSaveReset, TutorialSave, TutorialPlayMode.'),('FR-04–06','R04, R06','NavigationBudget, TerrainCollision, VesselDeath.'),('FR-07–11','R03, R06, R07, R09, R11','RadarTerrain, ZoneMap, ZoneExitFlow, ReturnGatePlacement.'),('FR-12–16','R03–05, R07, R10','PhotoSurvey, PhotoSubmission, PhotoDeletion; fault injection.'),('FR-17–22','R03, R04, R06, R08','CaptureMinigame, Cargo, ExpeditionEconomy; kiểm tra finite và unlimited.'),('FR-23–24','R03, R04, R07','ZoneThreeMission, RockBreaker, ExpeditionEndToEnd.'),('FR-25–28','R04, R07, R08','ExpeditionEconomy, ExpeditionLoop, VesselDeath; kiểm tra ngày 25 và 26.'),('FR-29–32','R04, R05, R07, R09','Migration, ExpeditionSaveReset, PhotoSubmission; backup và lỗi ghi.'),('FR-33–34','R03, R07, R11','ZoneFourProduction, ExpeditionEndToEnd, EndingPresentation.'),('FR-35–38','R07, R10, R11','ZoneTwoEnvironmentalHorror, ZoneThreeCinematic, ZoneFourDelivery, ZoneFourAudioProduction.'),('PF-01–04','A01','Benchmark Player trên máy tham chiếu; chưa có số đo được tuyên bố ở tài liệu này.'),('DC-01–03','R02–04, R07, R09, R11','Build, audit ID, migration và kiểm tra tách presentation.'),('QA-01–03','R04, R05, R07, R10–12','Fault injection, visual QA, kiểm tra đường dẫn fixture.'),('QA-04–05','R02, R04, R05, R10, R11','Offline smoke test, kiểm tra gói phát hành và quyền tài nguyên.')],[3.4,3.6,9.7])
page(d,'Phụ lục C Danh mục nguồn',1)
p(d,'Các đường dẫn dự án dưới đây được hiểu tương đối với thư mục gốc D:\\G10. Tài liệu cùng dự án được dùng để giải thích ngữ cảnh; cấu hình và mã nguồn hiện tại là cơ sở đối chiếu hành vi.')
refs(d,list(sources))
h(d,'Chỉ mục tra cứu',2)
p(d,'Ảnh và SEND: 3.2.3, UC 01. Bắt mẫu và Cargo: 3.2.4, UC 02. Module: 3.2.5. Ngày nghỉ và nâng cấp: 3.2.6. Lưu và migration: 3.2.7. Kết thúc: 3.2.8 và UC 04. Hiệu ứng kinh dị: 3.2.9. Hiệu năng: 3.3. Truy vết: phụ lục B.')
d.save(WORK/'G10_SRS_IEEE_830_1998.docx')

# GAME CONCEPT
c=init_doc('Game Concept')
cover(c,'Game Concept G10','Khám phá và kinh dị môi trường dưới biển sâu','G10 CONCEPT 1.0',
      'Trong cabin một con tàu nghiên cứu, người chơi lần theo tín hiệu, ghi nhận những dạng sống lạ và quyết định đi xa đến đâu khi biển sâu bắt đầu đáp lại.',ROOT/'Docs/QA/Zone01Cutscene/08-cabin-reveal.png')
toc(c,'1-1')
page(c,'1 Tổng quan ý tưởng',1)
h(c,'1.1 Tuyên bố ý tưởng',2)
p(c,'G10 đặt người chơi vào vai người vận hành tàu khảo sát biển sâu. Phần lớn hành động diễn ra qua các công cụ trong cabin: bản đồ để lập kế hoạch, bàn lái để di chuyển, radar để dò tín hiệu, camera để ghi hình và máy tính để kiểm tra kết quả. Mỗi công việc nghiên cứu mở thêm khả năng đi sâu; càng hiểu thiết bị, người chơi càng nhận ra những tín hiệu không còn hoàn toàn đáng tin.')
h(c,'1.2 Hồ sơ trò chơi',2)
table(c,['Thuộc tính','Định hướng'],[
 ('Tên dự án','G10; tên sản phẩm theo cấu hình hiện tại.'),('Thể loại','Khám phá, khảo sát sinh học, quản lý tài nguyên và kinh dị môi trường.'),('Chế độ và góc nhìn','Một người chơi; góc nhìn cabin cố định, tương tác thiết bị kiểu point and click.'),('Phong cách hình ảnh','Minh họa 2D mang chất màu nước, UI thiết bị và desktop nằm trong thế giới game.'),('Nền tảng định hướng','PC với chuột và bàn phím; Windows là nền tảng đề xuất cho nghiệm thu.'),('Quy mô hiện tại','4 vùng; 9 điểm chính và 2 điểm ẩn; 2 ending.'),('Giới hạn hành trình','25 ngày trong game theo baseline hiện tại; thời lượng theo giờ cần playtest.'),('Đối tượng định hướng','Người thích khám phá chậm, tự định vị, suy luận tín hiệu và kinh dị không khí.')],[4.7,12])
h(c,'1.3 Điểm hấp dẫn',2)
p(c,'Công việc nghiên cứu là cách người chơi tiếp cận bí ẩn. Chụp rồi tự tay SEND tạo một quyết định rõ ràng giữa nhìn thấy và xác nhận dữ liệu. Khoang tàu có vẻ thân thuộc nhưng những tiếng gõ, bóng phản chiếu và khoảng im lặng dần thay đổi cách người chơi hiểu nó. Ở cuối tuyến chính, việc dừng lại hoặc tiếp tục là lựa chọn có hệ quả trực tiếp đến hành trình.')
h(c,'1.4 Phạm vi trải nghiệm',2)
p(c,'Thiết kế tập trung vào điều hướng, quan sát và sử dụng thiết bị. Nhịp hành động cao nằm trong minigame bắt mẫu và điều khiển tránh vật cản. Phần kinh dị phát triển qua môi trường và cách dụng cụ phản hồi; không yêu cầu bổ sung hệ thống chiến đấu hoặc kẻ đuổi theo để hoàn thành concept hiện tại.')
page(c,'2 Tầm nhìn trải nghiệm và thế giới',1)
h(c,'2.1 Ba trụ cột thiết kế',2)
p(c,'Tự mình xác định vị trí. Người chơi ghép tọa độ, độ sâu, hướng tàu và radar thành hiểu biết về địa hình. Quyết định di chuyển phải tạo cảm giác có chủ đích, đồng thời tránh biến việc đọc bản đồ thành đoán mò kéo dài.')
p(c,'Khảo sát tạo tiến trình. Ảnh được kiểm tra và gửi, mẫu được thu thập, vật liệu trở thành module giúp mở tuyến. Người chơi nhìn thấy quan hệ giữa thứ vừa tìm được và khả năng mới của con tàu.')
p(c,'Bất an trong không gian quen thuộc. Hình ảnh cabin và tiếng máy tạo nền ổn định. Sự lệch nhẹ trong tín hiệu, âm thanh ngoài vỏ hoặc phản chiếu thoáng qua khiến người chơi đặt câu hỏi mà vẫn có thể tiếp tục công việc.')
h(c,'2.2 Bối cảnh và vai trò',2)
p(c,'Thế giới được khám phá qua bốn vùng biển sâu, mỗi vùng có môi trường sinh học và âm sắc riêng. Người chơi hiện diện như người vận hành một chuyến nghiên cứu, quan sát thế giới bên ngoài thông qua những cửa sổ và thiết bị hữu hạn. Các chỉ dẫn và hội thoại dẫn đường hỗ trợ thao tác; chúng không giải thích hết nguồn gốc các dấu hiệu bất thường.')
p(c,'Lily xuất hiện trong hệ thống chỉ dẫn hoặc phụ đề hiện có. Vai trò câu chuyện đầy đủ của Lily, danh tính người vận hành và nguồn gốc con tàu là các hạng mục biên kịch cần chốt. Concept này giữ chúng như khoảng trống sáng tạo, không gán thêm tiểu sử hoặc tuyến truyện chưa được sản xuất.')
h(c,'2.3 Đường cảm xúc dự kiến',2)
p(c,'Bắt đầu bằng tò mò và học cách điều khiển; chuyển sang cảnh giác khi môi trường có dấu hiệu đáp lại; tăng hoài nghi trong tàn tích; sau đó để khoảng trống và tín hiệu hiếm ở vực sâu thúc đẩy lựa chọn cuối. Đây là ý đồ thiết kế; cảm giác sợ, mức nhận ra chi tiết và nhịp tiếp nhận cần được xác nhận bằng playtest người thật.')
h(c,'2.4 Nguyên tắc kể chuyện',2)
p(c,'Thông tin quan trọng phải tìm được trong hành động thật: ảnh, mẫu, Mission Log và tín hiệu thiết bị. Chi tiết môi trường gợi câu hỏi nhưng không tự hoàn thành nhiệm vụ. Trò chơi nên dành khoảng yên giữa các cue để người chơi có thể suy nghĩ và tự quyết định có tiếp tục hay không.')
page(c,'3 Vòng lặp chơi và lựa chọn',1)
pic(c,loop,'Hình 1  Vòng lặp khảo sát kết nối điều hướng, dữ liệu nghiên cứu và tiến trình.',16.2)
h(c,'3.1 Vòng chơi ngắn',2)
p(c,'Người chơi đọc mục tiêu, chọn hướng và độ sâu, lái tàu tiếp cận tín hiệu rồi dùng radar hoặc camera để kiểm tra. Nếu ảnh đủ điều kiện, Photo Lab cho phép SEND. Tại những điểm yêu cầu mẫu, người chơi tiếp tục minigame bắt mẫu để nhận vật liệu. Phản hồi phải cho biết hành động có hiệu lực và vì sao lần thử bị từ chối.')
h(c,'3.2 Vòng hành trình',2)
p(c,'Các chuyến tiếp cận tiêu hao Energy, lượt radar và ảnh; va chạm làm mất Hull. Người chơi cân nhắc đi tiếp, quay lại, lấy thêm vật liệu hoặc nghỉ. Nghỉ hồi tài nguyên nhưng tăng ngày. Module bắt buộc mở tuyến mới; ba nhánh nâng cấp thường giúp đi nhanh hơn, tiết kiệm năng lượng hoặc chịu va chạm tốt hơn.')
h(c,'3.3 Quyết định tạo giá trị',2)
table(c,['Quyết định','Được và mất'],[
 ('Quét radar hay tiếp cận theo chart','Quét cho thông tin tại chỗ nhưng dùng lượt; tự đọc chart tiết kiệm lượt và đòi hỏi định vị tốt.'),('Tiếp tục chạy hay nghỉ','Đi tiếp tiết kiệm ngày nhưng tăng rủi ro cạn nguồn lực hoặc va chạm.'),('Dùng vật liệu cho nâng cấp thường','Đổi mẫu lấy khả năng vận hành; vật liệu dùng chung phải đủ cho module cần thiết.'),('Kết thúc hay khám phá thêm','Hoàn tất công việc chính để dừng hành trình, hoặc chấp nhận thêm điều hướng và bất định cho nhánh ẩn.')],[6.1,10.6])
p(c,'Thiết kế hiện tại bật bắt mẫu không giới hạn lượt thử. Độ khó tại đây đến từ vị trí, độ sâu, kỹ năng minigame và thời gian.')
page(c,'4 Cơ chế cốt lõi',1)
h(c,'4.1 Điều hướng và đọc dụng cụ',2)
p(c,'Tàu chuyển động trong không gian tọa độ bản đồ và độ sâu. Hướng Bắc là 0 độ, tăng theo chiều kim đồng hồ. Bàn lái trình bày vị trí, độ sâu, tốc độ và tài nguyên; bản đồ cung cấp các mốc để người chơi tự định vị. Chuyển sang công cụ khác phanh tàu để tập trung quan sát.')
p(c,'Radar là công cụ dò tín hiệu theo lần quét và địa hình. Contact hữu ích phải được phân biệt với nhiễu trình diễn. Zone04 có thể bật lớp vật cản tĩnh trên chart để đọc lối đi; lớp này không vạch đường hoặc tiết lộ hai điểm ẩn trước khi được khám phá.')
pic(c,ROOT/'Docs/QA/Zone03Cinematic/B-01-radar-before.png','Hình 2  Radar thực tế nằm trong ngôn ngữ hình ảnh của cabin.',16.2)
h(c,'4.2 Chụp ảnh và xác nhận dữ liệu',2)
p(c,'Người chơi chọn camera hotspot để chụp nhanh theo vị trí, hướng và độ sâu đang có. Ảnh phản ánh chủ thể, khoảng cách, che khuất và khả năng nhìn thấy. Người chơi xem lại trong Photo Lab rồi chọn SEND. Một bức ảnh đẹp chưa đồng nghĩa nhiệm vụ hoàn thành; biên nhận chỉ có sau thao tác gửi hợp lệ.')
h(c,'4.3 Bắt mẫu',2)
p(c,'Những điểm chỉ định yêu cầu PHOTO đã gửi trước CAPTURE. Minigame đen trắng dùng móc chạy liên tục; W S hoặc mũi tên lên xuống điều khiển hướng để chạm sinh vật 5 lần trong 45 giây theo profile hiện tại. Đường móc cong và phản ứng né của mục tiêu tạo sự thử thách. Chỉ thành công mới thêm mẫu; hủy hoặc hết giờ không tạo tiến độ.')
page(c,'5 Tiến trình bốn vùng',1)
pic(c,flow,'Hình 3  Tiến trình module và hai hướng kết thúc. Các mũi tên biểu diễn tuyến logic, không biểu diễn tọa độ bản đồ.',16.2)
h(c,'5.1 Zone01 Miền Tảo Hát',2)
p(c,'Vai trò là làm quen với cabin và quy trình nghiên cứu. Hai địa điểm gồm PHOTO ở cả hai và CAPTURE tại điểm đầu. Mẫu đầu mở công thức Bio Lamp; lắp đèn và hoàn thành khảo sát cho phép sang Zone02. Âm sắc sinh học và nguồn sáng là dấu hiệu nhận diện. Các độ sâu mục tiêu hiện tại là 230 và 460 m, nằm trong trần 500 m ban đầu.')
h(c,'5.2 Zone02 Cổ Thụ Linh Hồn',2)
p(c,'Môi trường chuyển sang các cấu trúc hữu cơ lớn và âm thanh rễ cây. Cả hai điểm yêu cầu PHOTO và CAPTURE để có mẫu kết dính cùng mẫu vỏ cứng. Pressure Hull tiêu thụ hai vật liệu, mở trần sâu 750 m và quyền đi Zone03. Hai điểm ở 280 và 460 m vẫn tiếp cận được trước nâng cấp.')
p(c,'Những tiếng động ngoài vỏ, ba tiếng gõ và phản chiếu ngắn làm cabin bớt an toàn. Các cue chờ người chơi đang xem đúng dụng cụ; chúng không làm hỏng tàu hoặc mở thêm contact thật.')
h(c,'5.3 Zone03 Tàn tích vô danh',2)
p(c,'Ba địa điểm chính tăng yêu cầu phối hợp công cụ. Điểm 1 và 3 yêu cầu mẫu; điểm 2 yêu cầu ảnh. Vật liệu explosive và detonator được ghép thành Rock Breaker qua CRAFT rồi INSTALL; người chơi đến chỗ rào đá để sử dụng. Phá rào thay đổi địa hình và mở hướng tới Zone04.')
p(c,'Dấu ấn kinh dị nằm ở tiếng máy như phản hồi chậm, radar giả và bóng trên mặt kính. Vụ phá đá là điểm nhấn vật lý có âm thanh va đập, mảnh bụi và khoảng ổn định sau đó. Thông tin nhiệm vụ thật vẫn được giữ nguyên.')
page(c,'6 Vực sâu và các kết thúc',1)
h(c,'6.1 Zone04 và đầu mối cuối',2)
p(c,'Hai địa điểm chính đưa người chơi tới đáy của chuyến khảo sát. Điểm 1 yêu cầu ảnh và mẫu, điểm 2 yêu cầu ảnh. Chi tiết xác tàu ở xa xuất hiện trong ảnh thật; dữ liệu thu được dẫn tới hai tọa độ ẩn trong Mission Log. Đọc tọa độ là đầu mối để hành động, chưa phải thành tích đã khám phá.')
p(c,'Màu nước sâu tối hơn và lớp âm môi trường thưa để khoảng trống trở nên có ý nghĩa. Hai địa điểm ẩn có môi trường ảnh và tín hiệu quan sát riêng. Cách thể hiện sinh vật tại đây còn dùng fallback; cần art riêng trước nghiệm thu hình ảnh cuối.')
h(c,'6.2 Normal Ending',2)
p(c,'Sau khi đủ công việc chính, người chơi có thể chọn KẾT THÚC THÁM HIỂM. Ending normal ghi nhận chuyến khảo sát hoàn tất và dùng nhịp máy lắng dần trong chuỗi khoảng 12 giây. Đây là một kết thúc trọn vẹn theo công việc được giao; không cần khám phá toàn bộ phần ẩn.')
h(c,'6.3 Hidden Ending',2)
p(c,'Lựa chọn TIẾP TỤC KHÁM PHÁ giữ người chơi ở Zone04. Người chơi tự đi tới hai tín hiệu, phát hiện bằng radar, chụp và SEND cả hai ảnh. Khi đủ điều kiện, điểm tín hiệu cuối mới mở. Chỉ việc điều khiển tàu tiếp cận thật mới đưa tới hidden ending; không kích hoạt chỉ từ việc đọc hint hoặc xem ảnh.')
p(c,'Chuỗi hidden kéo dài khoảng 16 giây, có âm hiệu lệch nhịp và những dấu hiệu muộn để giữ cảm giác dữ liệu đã lưu nhưng hiện tượng vẫn tiếp diễn. Chủ đích là để lại dư âm khó giải thích. Nội dung lời kết phải giữ nhất quán với văn bản ending đã được biên kịch chốt.')
pic(c,ROOT/'Docs/QA/Zone04ExperienceLoop/native-validation/ZoneMapPlayModeTests/chart-terrain-on.png','Hình 4  Chart Zone04 cho phép nhìn vật cản nhưng vẫn yêu cầu người chơi tự điều hướng.',15.7)
page(c,'7 Tài nguyên nâng cấp và nhịp ngày',1)
h(c,'7.1 Hai lớp phát triển con tàu',2)
p(c,'Module bắt buộc gồm Bio Lamp, Pressure Hull và Rock Breaker, gắn với khả năng đi tiếp. Nâng cấp thường gồm Hull, Propulsion và Energy Efficiency, giúp người chơi tùy chỉnh cách vận hành. Tách hai lớp giúp mục tiêu mở tuyến rõ ràng, đồng thời cho phép dùng vật liệu farm để giảm áp lực điều hướng.')
table(c,['Hệ thống','Mốc hiện tại','Tác động trải nghiệm'],[
 ('Hull','100 → 120 → 140','Chịu va chạm tốt hơn; không hồi máu miễn phí khi mua.'),('Propulsion','18 → 25 → 36','Rút ngắn đoạn di chuyển nhưng đòi hỏi kiểm soát va chạm.'),('Energy Efficiency','1,00 → 0,80 → 0,50 mỗi giây','Cho nhiều thời gian vận hành hơn giữa các lần nghỉ.'),('Pressure Hull','500 → 750 m','Mở khả năng tiếp cận chiều sâu của các vùng sau.'),('Cargo','12 loại stack','Vật liệu có số lượng và có thể tích lũy vào cùng loại.')],[4.1,5.2,7.4])
h(c,'7.2 Nhịp ngày và phục hồi',2)
p(c,'Baseline hiện tại cho 25 ngày. Ngày không tăng theo đồng hồ thực; REST hoặc recovery mới chuyển ngày. Nghỉ hồi đầy nguồn lực tại chỗ và tạo checkpoint cuối ngày, nên người chơi cân đối mức an toàn với số ngày còn lại. Sang ngày 26 khi chưa có ending là thất bại của hành trình.')
p(c,'Journal cho phép phục hồi về cuối ngày cũ và bỏ phần tiến trình sau đó. Hull về 0 dùng mốc đầu ngày thay cho tạo một ngày mới. Cạn Energy là vấn đề vận hành riêng, cho phép nghỉ hoặc cứu hộ theo luồng phù hợp. Các cách phục hồi cần được trình bày rõ để người chơi hiểu mình mất tiến độ nào.')
h(c,'7.3 Chủ đích cân bằng',2)
p(c,'Hai nguồn vật liệu farm hiện tại là Z2_Creature_02 và Z3_Creature_01. Thiết kế cần kiểm tra liệu farm giúp người chơi sửa sai hay kéo dài nhịp nghiên cứu quá mức. Các giá trị 25 ngày, capture không giới hạn và tốc độ nâng cấp là baseline triển khai, chưa phải bằng chứng đã cân bằng tốt cho mọi nhóm người chơi.')
page(c,'8 Mỹ thuật âm thanh và giao diện',1)
h(c,'8.1 Ngôn ngữ hình ảnh',2)
p(c,'Giữ nét vẽ tay, mảng màu nước và đường viền mềm của cabin. Tông xanh tím, cyan và ánh sáng ấm ở thiết bị làm không gian có sức sống. Từng vùng thay đổi môi trường và mức hiện diện của chi tiết, giữ các nút điều khiển quen thuộc để bất thường dễ được nhận ra.')
p(c,'Sinh vật và props cần có silhouette đọc được trong ảnh, đồng thời phù hợp các ngưỡng che khuất và tỷ lệ phủ. Art thay thế phải được kiểm tra bằng ảnh chụp thật trong Photo Lab, vì một hình đẹp độc lập có thể làm thay đổi khả năng xác nhận PHOTO. Không dùng ảnh concept để tuyên bố một asset đã có trong gameplay.')
pic(c,ROOT/'Docs/QA/Zone01QuickPhoto/01-photo-lab-before-send.png','Hình 5  Chất màu nước tiếp tục trong desktop và Photo Lab. Minh họa được lấy từ dự án.',16.2)
h(c,'8.2 Âm thanh như thông tin',2)
p(c,'Máy tàu, thao tác và cảnh báo tạo tín hiệu tin cậy cho người chơi. Tiếng sinh học, kết cấu ngoài vỏ và âm môi trường tạo khoảng không gian bên ngoài cabin. Kinh dị dùng giảm lớp nền, tiếng vọng lệch và cue hiếm thay vì tăng âm lượng liên tục. Khi môi trường rút âm, tiếng điều khiển và cảnh báo vẫn phải nghe được.')
h(c,'8.3 Giao diện và khả năng tiếp cận',2)
p(c,'Tên chức năng và trạng thái phải nhất quán giữa điểm tương tác, desktop và Mission Log. Chữ tiếng Việt, thông số độ sâu, cảnh báo và lựa chọn cuối phải rõ trên độ phân giải tham chiếu. Các hiệu ứng nhiễu cần giới hạn đúng thiết bị; cài đặt âm lượng, mute và giảm rung phải có hiệu lực trong các đoạn kinh dị và kết thúc.')
page(c,'9 Phạm vi sản xuất và đánh giá concept',1)
h(c,'9.1 Phạm vi hiện tại',2)
p(c,'Giữ cabin dùng chung, bốn vùng logic, 11 điểm khảo sát, các giao dịch PHOTO và CAPTURE, ba module, ba nhánh nâng cấp thường, save v6 và hai ending. Không bổ sung nền kinh tế tiền tệ, hệ thống online hoặc nhánh chiến đấu trong phạm vi concept này. Nền công nghệ hiện tại là Unity 6000.4.2f1 với URP 2D, Input System và uGUI.')
h(c,'9.2 Các mốc hoàn thiện đề xuất',2)
table(c,['Mốc','Sản phẩm bàn giao','Điều kiện đạt'],[
 ('M1 Hoàn thiện nội dung','Art sinh vật còn thiếu; lời thoại và văn bản ending thống nhất.','Đúng nhận diện, đọc được trong ảnh thật; không còn placeholder ngoài danh mục chấp nhận.'),('M2 Lượt chơi đầy đủ','Tuyến normal và hidden bằng đầu vào người chơi, có ghi hình và nhật ký lỗi.','Không dùng seed mission hoặc đặt vị trí để thay thế điều hướng thật.'),('M3 Cân bằng và trải nghiệm','Playtest người mới, hiệu chỉnh chỉ dẫn, thời gian và tài nguyên.','Đạt tiêu chí định hướng ở mục 9.3; công bố số người và điều kiện thử.'),('M4 Sẵn sàng phát hành','Player build, đo hiệu năng, kiểm tra save và quyền tài nguyên.','Chốt máy tham chiếu, đạt SRS và xử lý giấy phép tài nguyên.')],[3.4,6.7,6.6])
h(c,'9.3 Tiêu chí đánh giá đề xuất',2)
p(c,'Với một nhóm thử nghiệm ban đầu ít nhất 5 người chưa biết game, ghi tỷ lệ hiểu luồng chụp rồi SEND, tỷ lệ tự hoàn thành tutorial, điểm mắc điều hướng, số lần nghỉ, va chạm và lý do bỏ cuộc. Mục tiêu thử đầu: ít nhất 4 trên 5 người hoàn thành lần SEND đầu mà không cần người điều phối thao tác giúp sau tutorial; mọi điểm bế tắc phải được ghi nguyên nhân.')
p(c,'Ở phần kinh dị, hỏi người chơi nhận ra dấu hiệu nào, ở thời điểm nào và họ hiểu thiết bị bị nhiễu hay nhiệm vụ đã thay đổi. Thang mức căng thẳng 1–5 chỉ là dữ liệu phản hồi; không suy từ âm lượng, pixel thay đổi hoặc test tự động ra mức sợ đã đạt. Thời lượng toàn game và đối tượng phù hợp sẽ chốt sau lượt chơi đầy đủ.')
page(c,'10 Rủi ro và căn cứ thiết kế',1)
h(c,'10.1 Rủi ro cần quản lý',2)
table(c,['Rủi ro','Ảnh hưởng','Hướng xử lý'],[
 ('Điều hướng khó đọc','Mất nhịp, bỏ cuộc trước phần truyện quan trọng.','Kiểm tra chỉ dẫn độ sâu, chart và radar; giữ quyết định định vị nhưng giảm mơ hồ không chủ đích.'),('Chi tiết kinh dị quá kín','Người chơi bỏ lỡ cue hoặc không nhận ra liên hệ.','Đánh giá trên ảnh, âm thanh và audience thật; cho thử lại khi cue bị ngắt trước checkpoint.'),('Farm lấn phần khảo sát','Lặp minigame làm loãng khám phá.','Đo số lượt farm và ngày nghỉ; cân nhắc chi phí sau dữ liệu playtest.'),('Art fallback tại vùng cuối','Khó phân biệt sinh vật và điểm ẩn.','Làm art riêng, kiểm tra lại framing và chất lượng ảnh.'),('Save và rollback sai','Mất tin cậy vào tiến độ hoặc phát thưởng lặp.','Kiểm thử giao dịch, backup, migration và timeline độc lập với hiệu ứng.'),('Giới hạn giấy phép âm thanh','Không đáp ứng điều kiện phát hành thương mại.','Kiểm tra provenance; thay tài nguyên hoặc có quyền sử dụng phù hợp trước release.')],[4.1,5.3,7.3])
h(c,'10.2 Các quyết định sản phẩm còn mở',2)
p(c,'Cần chốt tên thương mại nếu khác G10, vai trò truyện đầy đủ của Lily và người vận hành, thời lượng chơi theo giờ, phân loại độ tuổi, cấu hình máy phát hành và mô hình phân phối. Đây là các quyết định chưa được ấn định; tài liệu không gán số liệu doanh thu, quy mô thị trường hoặc lịch phát hành chưa có cơ sở.')
h(c,'10.3 Căn cứ và nguồn hình',2)
p(c,'Game Concept liên kết với tài liệu G10 SRS 1.0 cùng ngày. Cơ chế và số lượng nội dung lấy từ mission asset, mã nguồn, scene đang dùng; bầu không khí dựa trên hồ sơ sản xuất các vùng. Nhãn “dự kiến”, “đề xuất” và “cần chốt” chỉ ý đồ hoặc công việc sản phẩm tiếp theo.')
p(c,'Nguồn đối chiếu: R02, R03, R04, R06, R07 và R08 trong danh mục SRS; Docs/Zone02EnvironmentalHorror.md; Docs/Zone03CinematicProduction.md; Docs/Zone04FinalChapterProduction.md; Docs/QA/Zone04ExperienceLoop/04_Implementation_Validation.md.')
p(c,'Nguồn hình: bìa từ Docs/QA/Zone01Cutscene/08-cabin-reveal.png; Hình 2 từ Docs/QA/Zone03Cinematic/B-01-radar-before.png; Hình 4 từ Docs/QA/Zone04ExperienceLoop/native-validation/ZoneMapPlayModeTests/chart-terrain-on.png; Hình 5 từ Docs/QA/Zone01QuickPhoto/01-photo-lab-before-send.png. Hình 1 và 3 là sơ đồ giải thích logic được dựng cho tài liệu, không phải bản đồ hoặc ảnh gameplay.')
c.save(WORK/'G10_Game_Concept.docx')
print(json.dumps({'authored':[str(WORK/'G10_SRS_IEEE_830_1998.docx'),str(WORK/'G10_Game_Concept.docx')]},ensure_ascii=False))
