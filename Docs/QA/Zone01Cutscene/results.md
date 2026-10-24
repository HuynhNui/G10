# Kết quả cutscene/màn hình — 2026-10-21

- Unity G10 thật, `6000.4.2f1`, điều khiển bằng Unity MCP.
- `ZoneOneAudioGameplayTests` + `ZoneOneCutsceneVisualTests`: **10/10 Play Mode pass**, gồm cảnh NEW GAME thật 35 giây và test âm thanh/save/Journal đang có (`gameplay-and-visual-tests.json`).
- Sau khi kiểm tra ảnh và chuyển readout Bio Lamp khỏi nút nâng cấp: **2/2 test visual chạy lại pass** (`visual-layout-rerun.json`). Không đổi logic âm thanh/progression trong bước chỉnh layout đó.
- Đã xem ảnh chụp Game view boot, instability và Bio Lamp. Text không tràn; readout cuối nằm cạnh ô module, không che nút nâng cấp.
- Chín ảnh `01-boot.png` đến `09-biolamp-online.png` được chụp từ Unity Game view ở 1920×1080, không phải ảnh thiết kế giả lập. Chưa kiểm tra trên thiết bị/player build hoặc mọi tỷ lệ màn hình.
- Shader display thực sự có lớp render/captured frame trong test, không chỉ kiểm tra enum/preset. Blackout được kiểm tra mẫu màu ở ba vị trí trong khung hình. Rung terminal tắt khi setting Screen Shake tắt.
- Bảng Bio Lamp chỉ hiện sau install đã lưu thành công; ẩn khi đổi app, không modal/raycast; kết thúc tự ẩn.
- Biên dịch Editor không có lỗi C# ở lần kiểm tra cuối. Không đổi package/project settings, không tạo âm thanh mới, không commit/merge/push.
