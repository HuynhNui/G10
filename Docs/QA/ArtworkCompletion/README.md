# Tích hợp artwork PSD vào G10

Hoàn tất ngày 08.11.2026 trên Unity 6000.4.2f1. Cả 11 tranh sinh vật đã có binding đúng và đã đi qua luồng CHỤP → Photo Lab → GỬI DỮ LIỆU → Continue. Hai artwork ma trước đây chỉ nằm trong Reserve đã được nối vào các nhịp trình diễn có sẵn. Photo Lab được giữ nguyên source và bố cục.

## Artwork được dùng ở đâu

Mỗi ID bên dưới có tranh nguyên artboard tại `Assets/_Project/Art/SurveyPaintings/ID.png`. Nền, ánh sáng, foreground, trang trí và khung phim nằm trong raster lưu khi chụp. Sprite riêng ở `Creatures/Finished` tiếp tục phục vụ minigame/cargo, không được dùng thay tranh khảo sát.

| ID | Địa điểm thực tế | PSD và artboard |
| --- | --- | --- |
| Z1_Creature_01 | Z1_L1 / zone01-left | sinh vật 1 remake / board 1 |
| Z1_Creature_02 | Z1_L2 / zone01-north | sinh vật 2 khu vực 1 / board 1 |
| Z2_Creature_01 | Z2_L1 / zone02-l1 | SINH VẬT ẢI 2 VÀ 3 / board 1 |
| Z2_Creature_02 | Z2_L2 / zone02-l3 | cùng PSD / board 5 |
| Z3_Creature_01 | Z3_L1 / zone03-l1 | cùng PSD / board 3 |
| Z3_Creature_02 | Z3_L2 / zone03-l2 | cùng PSD / board 2 |
| Z3_Creature_03 | Z3_L3 / zone03-l4 | cùng PSD / board 4 |
| Z4_Creature_01 | Z4_L1 / zone04-l1 | sinh vật và hù ma ải 4 / board 1, có xác tàu |
| Z4_Creature_02 | Z4_L2 / zone04-l2 | cùng PSD / board 2 |
| Z4_Creature_03 | Z4_L3 / zone04-l3, ẩn A | cùng PSD / board 3 |
| Z4_Creature_04 | Z4_L4 / zone04-l5, ẩn B | cùng PSD / board 4 |

Số board là thứ tự từ 1; file mapping gốc dùng chỉ số từ 0. Mapping giữ nguyên 9 mẫu bắt buộc + 2 mẫu ẩn, ID PHOTO/CAPTURE và các POI không liên tục đã được chủ ý giữ trong code. Tên file Zone01 và group xác tàu tạo căn cứ trực tiếp; nhóm Zone02/03 được đối chiếu hình thái với vật liệu kết dính, giáp, áp suất, tinh thể và điện sinh học. PSD chung không ghi creature ID riêng, nên các liên hệ đó là quyết định triển khai có căn cứ, không phải nhãn được tác giả tranh xác nhận. `SINH VAT 1.psd` cũ là bản tham chiếu được remake thay thế, không tạo thêm sinh vật.

Audit live qua AssetDatabase: `catalog-mission-audit.json`. Kiểm tra độc lập mở lại PSD: `full-paintings-export-verification.json` — 11/11 PNG khớp RGBA với saved composite; 11 cutout + 3 group dự phòng không đổi.

## Phần vừa bổ sung

| Artwork | Sự kiện và nhịp |
| --- | --- |
| Zone02_03_Ghost, group `con ma` | Zone02 Reflection sau Pressure Hull, đang lái gần cửa ra và đủ khoảng yên. Ba bước hình từ 0.90–1.63 s; giữ electric fizz bắt đầu 0.75 s. Thay TemporaryCrewSilhouette, giữ nguyên màu PSD và mask kính. |
| cùng artwork | Zone03 Reflection sau cơ hội radar/đoạn đi sâu. Giữ chuẩn bị, nhiễu kính, hình 2.90–5.00 s, soft movement lúc 3.00 s, close click 4.90 s và kết thúc chuỗi 6.80 s. Hình nguồn thay khối người procedural trong đúng module/mask hiện có. |
| Zone04_HelmetGhost, group `ma` | Tín hiệu cuối nhánh ẩn, chỉ sau các điều kiện khám phá và chuyển động tiếp cận thật. Hình trên kính bàn lái từ 1.20–2.10 s, đồng thời signature audio có sẵn. Hình tắt trước đuôi âm thanh; chuỗi vẫn kết thúc 9.00 s. |
| Zone04_Wreck, group `tau ngam` | Đã có trong tranh nguyên board của Z4_Creature_01, được chụp/lưu thật; không chồng thêm xác tàu procedural lên tranh. |

Các hình ma là nguyên group RGBA do tác giả vẽ, dùng như phản chiếu thoáng qua trên kính; chúng không phải sinh vật mới hoặc ảnh chụp thêm vào gallery. Shader contain theo tỷ lệ, giữ RGB gốc, chỉ áp dụng fade. Definition/sequence và hai reference texture đã lưu trong scene Zone01 dùng chung bằng Unity Editor API. Có installer lặp lại `NativeScareArtworkSetup.Install()`.

Đã đọc lại cả `G10_Game_Concept.doc` và `G10_SRS.doc` hiện tại cùng tài liệu production Zone02–04. Các nhịp mới tiếp tục yêu cầu đúng dụng cụ đang được xem, không tạo contact, cấp mẫu, hoàn thành PHOTO, tiêu hao lượt hoặc tự ghi ending.

## Kiểm tra và phạm vi bằng chứng

| Job native | Kết quả và phạm vi |
| --- | --- |
| 30af268e7678 | 7/7 PlayMode: chụp 11 tranh, 3 trigger ma, fixture cancel và 2 bài Photo Lab |
| b852fcacebda | 8/8 PlayMode: chụp lại đỉnh fade Zone03/04 và 6 hồi quy về clip kính, retry/Continue, shader, thứ tự cửa sổ, pause và Journal |
| e1d8513360b7 | 2/2 PlayMode: Bio Lamp giữ nguyên PSD/ảnh cũ; fallback procedural vẫn sáng hơn sau lắp đèn |
| db5c835f20c1 | 11/11 EditMode: aspect fit, mask đo coverage và foreground occlusion |

Hai bài đỉnh fade lặp lại bài chính: tổng cộng 15 ca PlayMode khác nhau, đều qua, cộng 11 ca EditMode. JSON lưu kết quả từng job; plugin cũ có thể chỉ giữ summary sau reload.

**Đã qua đường gameplay sản phẩm:** `gameplay-shutter/evidence.json` ghi 11 lần raycast/EventSystem click vào `CameraHotspot` thật, không gọi trực tiếp Capture để tạo ảnh và không thay/tắt bất kỳ artwork nào trong lượt này. 11/11 ảnh bake khớp source, gửi đúng mục tiêu và giữ pixel qua gallery/save/Continue. Hai điểm ẩn được nút QUÉT và sweep radar thật khám phá, không gọi RevealPoi thay cho radar.

**Điều kiện kiểm thử có kiểm soát:** lượt trên dùng Debug Start theo zone, đặt tàu tại contact hợp lệ trên chart gốc, refill và helper Win cho sáu minigame prerequisite. Đây không phải một chuyến chơi thường từ đầu đến cuối bốn zone. Bài fixture hiệu ứng riêng gọi trực tiếp sequence để thử đổi cửa sổ và hủy scene; không dùng nó làm bằng chứng trigger tự nhiên.

**Trigger ma runtime:** ba bài mới dựng sẵn tiến trình/cue trước đó và vị trí đầu lượt, sau đó chỉ đưa W qua InputSystem và để controller/đồng hồ thực chạy. Không gọi Preview, không Tick(dt) giả để bỏ qua khoảng yên hoặc fade, không thay water chart. Đã ghi screenshot, thời điểm ảnh, AudioSource thật và AudioListener mix ở `scares/`. Các clip tương ứng đang phát khi hình xuất hiện; ba bản thu đều không có mẫu clipping. Mission state trước/sau giống nhau và không có ảnh gallery mới. Pause, đóng audience, scene destruction, Continue không replay receipt và Journal loại cơ hội tương lai đều đã được thử.

Đã tự xem screenshot đầy đủ và ảnh ở đỉnh fade; Zone03 giữ mức phản chiếu mơ hồ theo nhịp cũ, Zone04 thấy rõ mắt mũ lặn trên kính. Photo Lab đã qua lại 1920×1080 và 1366×768. Không có chỉnh sửa source Photo Lab trong tác vụ này. Bài Bio Lamp cũ đòi mọi raster được chiếu sáng lại được thay bằng hai kiểm tra riêng cho PSD và fallback; không thay logic đèn hoặc điều kiện chụp để làm test qua.

## Bảo toàn và phần chưa xác minh

Save/archive được cô lập trước Play Mode. `preservation-audit.json` đối chiếu SHA-256 với snapshot đầu tác vụ: các file save thật, catalog/mission/map, dữ liệu chụp và source Photo Lab được giữ nguyên. Cache font được khôi phục đúng byte đầu tác vụ. Các screenshot cũ bị bài hồi quy ghi đè được trả về bản đầu repo; bằng chứng mới nằm riêng tại đây.

Không còn artwork sinh vật hoặc group ma trong bộ PSD đã tải thiếu vị trí sử dụng. Các phần chưa chứng nhận là một lượt chơi thường xuyên suốt bốn zone không seed/Win helper, cảm nhận âm thanh bằng tai nghe và standalone player build. Đây là giới hạn kiểm thử, không phải binding còn bỏ trống. Không commit/push, không sửa tài liệu Word đang có, save format, ID nhiệm vụ hoặc tài nguyên.

Ảnh tiêu biểu: `gameplay-shutter/Z4_Creature_01-01-lab.png`, `gameplay-shutter/zone04-l3-real-radar-discovery.png`, `scares/Zone02-native-ghost.png`, `scares/Zone03-native-ghost.png`, `scares/Zone04-native-helmet.png` và các ảnh `*-recovered.png`.
