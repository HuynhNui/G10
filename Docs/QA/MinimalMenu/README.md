# Menu tối giản — 10/11/2026

Đổi menu theo mẫu mới: giữ nguyên nền và wordmark artist; bỏ khung/nền nút và phụ đề. Ba mục chữ căn trái là **Chơi mới / Tiếp tục / Thoát**. Chữ, ảnh và controls độc lập; vẫn dùng các navigation callbacks cũ. Hover/keyboard selection đổi màu chữ nhẹ, Continue bị khóa vẫn thể hiện bằng màu chữ.

Font: **UTM Avo Regular**, tải trực tiếp từ [file font trên Drive](https://drive.google.com/file/d/1K_Ap49BT05-9EiQWFwNniVgpAeUF5z7u/view). Font sans hình học gần mẫu, có đủ glyph tiếng Việt cho menu trong atlas TMP riêng. Không đổi font Photo Lab/cabin. SVN-Beast được PSD ghi tên nhưng chưa có file trong thư mục UI; không gọi UTM Avo là cùng font đó.

- [1920×1080](1920-menu.png)
- [1366×768](1366-menu.png)

Đã tự xem cả hai screenshot Unity. Native PlayMode job `40dc3dd7b79a`: 1/1 ca sẵn có qua, gồm bounds/raycast/chữ hai độ phân giải và Continue/hồ sơ/Escape. Kiểm tra thêm trong Play Mode: cả ba button không có sprite hoặc nền nhìn thấy, target graphic là TMP; F8 mở rồi ẩn Debug/Reset Tutorial đúng, input settings được phục hồi. Các nút kỹ thuật ẩn mặc định, F8 chỉ hoạt động ở Editor/dev build.

Scene MainMenu đã lưu qua Unity Editor API. Entry sửa riêng menu: `LilyArtistIntegrationSetup.InstallMenuOnly()` / menu `G10/Artwork/Apply Minimal Lily Menu`. Không chạy lại installer cabin/tài liệu. Nguồn chính: `LilyMenuPresentation.cs`; font gốc/SDF trong `Assets/_Project/Art/Lily25Days/`. Save thật được bảo vệ bằng outer isolation. Không commit/push.
