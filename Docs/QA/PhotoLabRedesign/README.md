# Photo Lab — triển khai theo mockup, 03.11.2026

Đã triển khai và kiểm tra bằng Unity 6000.4.2f1 qua AnkleBreaker MCP trên dự án G10.

## Chủ sở hữu và cách lưu thay đổi

Scene thật: `Assets/_Project/Scenes/Gameplay/Zone01.unity`, đối tượng `Cabin Station/CabinCanvas/CabinFrame/ComputerScreen/PhotoLabPanel`. Đây là đối tượng scene, không phải instance prefab. Cabin này dùng chung cho các zone.

`PhotoLabView` sở hữu chọn ảnh, gửi và xóa; `PhotoCaptureService` giữ repository và các giao dịch lưu thật. `ComputerScreenController` sở hữu cửa sổ và Back/Escape. `ComputerDesktopSkin` là cấu trúc dựng uGUI sẵn có của dự án; phần nội dung mới được dựng từ code đó mỗi lần khởi tạo cabin. Thay đổi tồn tại ngoài Play Mode trong source, không cần ghi lại đối tượng runtime vào scene.

## Thay đổi

- Nội dung hai cột khoảng 61% / 39%, texture giấy màu nước và nút tái sử dụng từ bộ UI hiện có; desktop, taskbar và vỏ cửa sổ được giữ nguyên.
- Ảnh nằm trong vùng 706 × 548 đơn vị tham chiếu, tăng từ vùng cũ cao 395. Contain theo hai chiều, ưu tiên chiều cao khi đủ rộng; không crop, không kéo giãn, UV đầy đủ. Dùng chính raster ảnh đã lưu, giữ toàn bộ tranh PSD và khung phim. Ảnh ngang được giới hạn theo chiều rộng.
- Các nhãn TMP độc lập cho tên mẫu, chất lượng, tọa độ, độ sâu, hướng, thời điểm, trạng thái khảo sát; các giá trị lấy từ bản ghi đang chọn. Tên chỉ được tra catalog sau khi có receipt PHOTO đã gửi/hoàn thành. Sáu trạng thái chất lượng có nhãn riêng, dữ liệu không biết dùng nhãn chưa đánh giá.
- Cụm trước / số ảnh / sau dưới tranh; vô hiệu hóa ở biên và khi chỉ có một ảnh. Gallery rỗng vô hiệu hóa toàn bộ thao tác.
- Xem lớn dùng cùng texture, nút đóng và Escape, giữ ID đang chọn. Modal chặn raycast trên cửa sổ và taskbar; mở viewer không gọi gửi, xóa hoặc giao dịch tiến trình. Escape đóng modal trước khi đóng ứng dụng, kể cả khi expedition bị chặn.
- Gửi qua repository cũ và kiểm tra lại điều kiện; khóa thao tác và gửi lặp trong callback. Xóa xác nhận theo ID, hủy không đổi gallery, xóa xong chọn ảnh liền kề.
- Tutorial đổi lời hướng dẫn theo nhãn mới và ẩn spotlight khi mở modal. Các controller hiệu ứng Zone02/03/04 không xem viewer là vùng khảo sát đang hoạt động. Vùng ảnh gốc được giữ cùng reference nên hiệu ứng scoped tiếp tục bám ảnh, không phủ bảng thông tin.
- Không sửa logic chụp, mission/creature ID, tài nguyên, định dạng save hoặc luật tiến trình. Các thay đổi artwork đã có trước khi bắt đầu được giữ nguyên.

## Kết quả kiểm tra

| Lượt native PlayMode | Kết quả |
| --- | --- |
| `96d9998379fb` — PhotoSubmission, PhotoDeletion, FullPaintingPhotoIntegration, ZoneOneQuickPhoto, PhotoLabLayout | 35 qua / 36; 1 thất bại được ghi dưới đây |
| `c40fd353c85f` — lượt cuối PhotoLabLayout sau chỉnh ưu tiên modal | 2 qua / 2 |

Hai bài layout là kiểm tra tích hợp trên cabin và repository thật: gallery rỗng, một/nhiều ảnh; tranh dọc PSD và raster ngang legacy; đổi ảnh; metadata theo ảnh; tên chưa/đã định danh; xem lớn; sự kiện bàn phím Escape qua UIManager; hủy và xác nhận xóa; mở lại ứng dụng; gửi thật, receipt bền vững và chặn gửi lặp. Chạy ở đúng 1920×1080 và 1366×768, kiểm tra tỉ lệ/UV/bounds ảnh, TMP overflow, glyph tiếng Việt và raycast đầu tiên trên cả nút gửi lẫn taskbar khi modal mở.

Hồi quy đã qua luồng gửi/xóa thất bại và retry, Continue, ảnh cũ thiếu trường tùy chọn, archive reload, giới hạn kho và giữ nguyên pixel của cả 11 tranh PSD qua gallery/SEND/Continue. JSON lưu tổng kết job, thời lượng và lỗi còn giữ trong progress; plugin không trả lại danh sách chi tiết của job hồi quy sau reload. Không phải chứng nhận toàn bộ PlayMode hoặc standalone build.

Một bài cũ `ZoneOneQuickPhotoPlayModeTests.LampChangesNewQuickPhotosWithoutRepaintingArchivedPhotos` thất bại tại phép đo độ sáng: đòi ảnh sau Bio Lamp > 430.1865, thực tế 391.0786. Luồng artwork đang có trước tác vụ này copy nguyên tranh PSD đã bake; kỳ vọng tái chiếu sáng raster của bài cũ không phù hợp với luồng đó. Không đổi logic chụp hoặc sửa assertion để che thất bại; chưa chạy một lượt baseline riêng với toàn bộ thay đổi artwork được tháo ra.

Unity cuối lượt: 0 lỗi C#; 0 missing scripts trên scene Zone01; Editor dừng ở Bootstrap sạch. Cache font phát sinh trong thử nghiệm được khôi phục đúng trạng thái đầu tác vụ, giữ thay đổi font có sẵn. Hai screenshot QA cũ bị test hồi quy ghi lại cũng đã được khôi phục; bằng chứng mới nằm riêng trong thư mục này.

## Save và screenshot

Isolation được thiết lập trong Edit Mode trước Play Mode bằng guard `ZoneTwoExperiencePlaytestTools.SetupIsolation`; từng fixture dùng thư mục save/archive tạm riêng. Sau lượt cuối, save/archive override đã trở về null. SHA-256 của 54 file persistent có sẵn không đổi; `TestResults.xml` là báo cáo test tái sinh, được loại khỏi phép đối chiếu save. Chi tiết: `save-integrity.json`.

Đã tự mở và xem các ảnh Game View, đối chiếu mockup, chỉnh kích thước/contain ảnh, canh hàng metadata, nine-slice nút và lớp phủ taskbar. Các ảnh không phải mockup ghép hoặc render ngoài Unity.

- `screens/1920-real-capture-before-send.png`: ảnh chụp thật từ cabin, số liệu thật và nút gửi khả dụng.
- `screens/1920-real-capture-after-send.png`: receipt thật và tên được nhận diện.
- `screens/1920-portrait.png`, `screens/1366-portrait.png`: bố cục tranh dọc; giá trị trong fixture này dùng để kiểm tra binding, không phải thông số hardcode của UI.
- `screens/*-landscape-legacy.png`: raster camera ngang khôi phục qua SavedPhoto, không có cờ fullPainting.
- `screens/*-empty.png`, `screens/*-viewer.png`, `screens/*-delete-confirmation.png`: trạng thái trống và modal ở cả hai độ phân giải.
