# Zone01 — cutscene và hiệu ứng màn hình

Cập nhật 2026-10-21 sau phần nối gameplay âm thanh. Dùng giao diện thiết bị và cabin hiện có theo kịch bản Miền Tảo Hát V1.

## Mở đầu NEW GAME

`SceneFlowController` giữ đồng hồ cảnh 35 giây và khóa input trong thời gian mở đầu. `ZoneOneOpeningView` dựng một canvas tạm cho phần trình bày; âm thanh vẫn do `ZoneOneAudioController` và EffectDirector điều phối theo mốc đang có.

| Mốc | Hình ảnh |
| --- | --- |
| 0–4 s | Terminal của chương trình thám hiểm, thông số nhiệm vụ hiện dần bằng chữ, trạng thái DESCENT IN PROGRESS và thanh tiến trình. |
| 4–12 s | WATER CONTACT CONFIRMED, áp lực tăng và telemetry tiếp nước. |
| 12–18 s | Cảnh báo màu hổ phách DESCENT STABILITY LOST; nhiễu nhẹ trên khung terminal được chụp thực tế, dùng shader display hiện có. |
| 18–18.28 s | Một cú rung nhìn thấy được trên chính terminal, đồng bộ va chạm; tint tối màu rất ngắn rồi mất tín hiệu. Rung tôn trọng tùy chọn accessibility. |
| 18.28–21 s | Blackout. Khoảng im lặng audio 20–21 s của phần trước được giữ nguyên. |
| 21–27 s | EMERGENCY RESTART, thanh phục hồi và trạng thái hệ thống xuất hiện theo nhịp; NAVIGATION/LIFE SUPPORT online, EXTERNAL LIGHT offline, DEPTH SYSTEM limited. |
| 27–30.5 s | Lớp đen và terminal tan dần để lộ cabin. Hiệu ứng đèn reboot sẵn có tiếp tục chạy trong cabin. |
| 30.5–35 s | Cabin đã rõ; mở điều khiển khi kết thúc cảnh. Canvas và ảnh chụp nhiễu tạm được dọn khi kết thúc. |

Canvas cutscene có hệ tọa độ tham chiếu 1920×1080 và tự scale theo Game view. Input shield vẫn thuộc scene flow hiện có; cutscene không tự sửa vị trí, Hull, Energy, radar, nhiệm vụ hoặc receipt của timeline. Không tạo thoại Lily.

## Bio Lamp

Khi INSTALL đã commit thành công, readout trong Upgrade hiển thị lần lượt: khóa module → cấp điện → khôi phục chiếu sáng → ổn định → ONLINE. Thanh tiến trình đi cùng đồng hồ 4 giây của audio; readout tự tan sau khoảng 6 giây.

Readout không bắt raycast, không mở modal và không ép giữ ứng dụng. Đổi sang Photo Lab hoặc đóng Computer thì readout ẩn. Lớp sáng camera vẫn tăng ở mốc 1.5–2.2 giây theo phần gameplay đã nối.

## Giới hạn theo kịch bản

Nhiễu terminal nằm trong sự cố hạ cánh. Three Knocks và các đoạn khảo sát không phát kèm glitch, blackout hoặc bóng người. Bio Lamp không tạo flash trắng toàn màn hình. Trần lặn vẫn giữ 500 m theo lựa chọn đã xác nhận.

## Kiểm tra và xem lại

- Chạy Bootstrap → NEW GAME để xem đầy đủ.
- Báo cáo và ảnh Game view: `Docs/QA/Zone01Cutscene/`.
- `ZoneOneCutsceneVisualTests` kiểm tra các khung hình, shader có render thực, blackout, accessibility và readout Bio Lamp không chiếm điều khiển cửa sổ.
- `ZoneOneAudioGameplayTests` chạy lại để kiểm tra mở đầu, khoảng lặng, trigger, save và Journal không bị ảnh hưởng.

Không phát sinh request ElevenLabs, asset âm thanh hoặc package mới. Không commit/merge/push.
