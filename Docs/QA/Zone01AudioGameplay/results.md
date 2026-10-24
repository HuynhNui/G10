# Zone01 gameplay audio — kết quả 2026-10-21

- Unity Editor thật: `D:/G10`, Unity `6000.4.2f1`, thông qua AnkleBreaker Unity MCP.
- Biên dịch: không có lỗi C# tại lần kiểm tra cuối.
- Kiểm tra native Editor: 40 AudioClip đọc được với stereo / 44100 Hz / samples > 0; 32 definition audio được bind vào EffectDirector; 2 địa điểm; không có definition null hoặc Missing Script trong Zone01/GameplayCore; scene đã lưu, trần lặn 500 m.
- `ZoneOneAudioGameplayTests`: **8/8 pass** sau cùng (`final-audio-playmode.json`). Bao gồm NEW GAME thật chạy mở đầu 35 s; khoảng lặng/reboot; học tutorial trước âm sinh học; SEND/CAPTURE; âm thiết bị hủy/thành công; Bio Lamp; tiếng gõ chờ đúng UI; save/reload; rollback Journal; đoạn cuối không phát bù tiếng gõ đã bỏ lỡ.
- Hồi quy: 64 test trong 6 fixture đã chạy (`regression-initial.json`): 62 pass, 2 test cần sửa cách chuẩn bị dữ liệu. Một test chờ Intro thực sự chuyển sang Helm; test SEND/CAPTURE riêng của Zone02 được cấp tàu đủ độ sâu trong fixture. Hai test chạy lại **2/2 pass** (`regression-rerun.json`). Không sửa trần lặn production để làm test Zone02 vượt qua.
- EditMode: **10/10 pass** ở `MissionProgressStateTests` và `EffectDirectorEditModeTests` (`editmode.json`).
- Một lần khởi chạy Test Runner ban đầu bị hủy do Unity biên dịch lại khi đang chuẩn bị Play Mode. Không có test gameplay nào hoàn tất ở lần đó; đã làm sạch job, đợi compile ổn định và chạy lại. Các báo cáo JSON ở đây là các lần thực thi có kết quả từng test.
- Kiểm tra diff C#/test được chỉnh không có lỗi whitespace. File scene/asset được Unity serialize theo định dạng native; không đổi package/project settings để phục vụ tính năng.
- Các test dùng save/photo tạm. Ba ảnh QA có sẵn dưới `Docs/QA/HorrorPolish/production-*.png` được fixture hồi quy tạo lại; đây là ảnh đầu ra test, không phải asset game. Không thay đổi 40 WAV hoặc phát sinh request ElevenLabs mới.
- Không chạy toàn bộ test suite, build player hoặc duyệt âm sắc bằng tai. Không commit/merge/push.

## Giới hạn được người dùng xác nhận

Giữ trần lặn 500 m và chỉ hoàn thiện Zone01. Chuyển zone vẫn hoạt động. Mục tiêu cuối Zone02 ở 510 m sẽ cần progression/depth phù hợp ở công việc sau. Bio Lamp chỉ khôi phục ánh sáng, không tự tăng độ sâu tối đa.
