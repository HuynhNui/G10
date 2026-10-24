# Zone 1 — tạo ảnh và phần thưởng Bio Lamp

Đã sửa ngày 2026-10-21 theo xác nhận của người dùng: giữ luồng chụp nhanh → Photo Lab → SEND. Màn hình quan sát trực tiếp và các nút được thêm nhầm đã gỡ khỏi scene, runtime và editor installer. Phần nhiệm vụ Zone 2–4, số lượng địa điểm Zone 1 và trần lặn 500 m giữ nguyên.

## Cách dùng

- Bấm hotspot chụp trong cabin: tàu phanh, chụp đúng một ảnh và quay về cabin. Không mở màn hình Camera hoặc preview trực tiếp.
- Mở Computer → **PHOTO LAB** để xem ảnh vừa chụp, rồi bấm **SEND** để ghi nhận PHOTO. Bấm chụp đơn thuần không hoàn thành nhiệm vụ.
- Hiệu quả Bio Lamp xuất hiện trong những ảnh chụp sau khi lắp đèn. Ảnh đã lưu không đổi; không có nút quan sát hoặc kiểm tra đèn riêng.

## Phản hồi đã thêm

- L1: tảo có nhịp đung đưa/co giãn nhỏ. Khi tàu yên, phản hồi màu cộng hưởng tăng dần khoảng 1.4 s; quét radar tạo một đáp ứng màu nhỏ rồi dịu xuống. Vị trí contact, dữ liệu radar và quần thể sau CAPTURE không đổi.
- L2: pose của chủ thể tại thời điểm chụp có biến thiên nhẹ, không dời contact đã lưu và không thêm điều kiện timing bắt buộc. Không có thông báo từ Camera trực tiếp. Artwork sinh vật thứ hai vẫn là placeholder của dự án.
- Độ sâu: nền nước chuyển sắc tối hơn, sương và lớp đáy thay đổi theo độ sâu. Vùng sâu dùng đá, không mang nguyên cụm tảo trang trí từ L1 sang L2.
- Bio Lamp: ánh sáng tăng theo clock lắp đèn đã commit, làm rõ màu, giảm sương, tăng độ rõ của sinh vật và lớp đáy trong ảnh mới. Chụp trên đường ra Exit vẫn có nền đáy/hạt dù không có chủ thể nhiệm vụ. Đây là cảnh quan dùng lại artwork hiện có, không thêm POI hay phần thưởng bắt buộc.
- E06: tiếng co giãn đầu tiên chờ một chuyển động lặn thật từ 380 m trở xuống. Nếu ba tiếng gõ đang phát, nó đợi thêm khoảng yên 8 s sau đó; tránh chồng lên âm trầm L2. Receipt `pressure-descent` nằm trong danh sách cue hiện có và được lưu theo timeline. Các tiếng co giãn tiếp theo dùng cooldown dài 28 s khi đang di chuyển. Thời gian mở Computer không chạy bộ đếm này.
- Ba tiếng gõ giữ nguyên ánh sáng, màn hình, radar và sự im lặng của Lily. Không thêm gõ, bóng người hay cào sau lưng.

## Triển khai

`ZoneOneSurveyFrame` tạo các lớp ảnh bằng texture sẵn có khi PhotoCaptureService chụp: pose, thứ tự che khuất, độ sâu và ánh sáng được tính ở thời điểm đó. Không có runtime hiển thị Camera trực tiếp. ZoneOneSurveyCamera, ZoneOneObservationLink và ZoneOneObservationSetup đã được xóa bằng Unity AssetDatabase sau khi tháo reference trong scene.

Độ rõ và che khuất vẫn được đánh giá khi chụp. SEND, archive, cấp thưởng và lưu tiến trình tiếp tục do hệ thống cũ quản lý. Thay đổi đèn hoặc pose không tô lại ảnh cũ trong Photo Lab. Pause dừng thời gian của chuyển động.

Trình cài Singing Forest Gameplay and Audio không còn gọi installer quan sát; chạy lại không tạo các nút hoặc live layers đã gỡ. CameraPanel cũ tiếp tục nằm ẩn để giữ các reference legacy, không phải luồng gameplay có thể mở bằng nút mới.

## Kiểm tra

Kiểm thử hiện tại và ảnh Photo Lab nằm ở `Docs/QA/Zone01QuickPhoto/`. Thư mục `Docs/QA/Zone01Observation/` là bằng chứng lịch sử của phương án Camera đã bị gỡ, không mô tả giao diện hiện tại. Các bài kiểm tra dùng save/photo archive tạm riêng.
