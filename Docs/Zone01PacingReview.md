# Zone 1 — đánh giá nhịp và đề xuất cải thiện

Ngày 2026-10-21. Đây là đánh giá và các đề xuất thiết kế ban đầu. Sau khi triển khai, người dùng xác nhận giữ luồng chụp nhanh → Photo Lab → SEND; khung quan sát trực tiếp và các nút mới đã được gỡ. Phản hồi tảo, pose nhẹ tại L2, nhịp E06 và hiệu quả Bio Lamp được giữ trong âm thanh/ảnh chụp. Những đề xuất bên dưới có nhắc mở Camera là lịch sử thiết kế, không phải luồng hiện tại. Chi tiết hành vi ở `Docs/Zone01Observation.md`; artwork sinh vật thứ hai vẫn là placeholder. Phạm vi kịch bản vẫn là Bình yên → Tò mò → Cô lập → Nghi ngờ, hai địa điểm khảo sát, không thoại Lily mới.

## Nhận định

Zone 1 có đủ các mốc mở đầu, khám phá tảo, thu mẫu, ba tiếng gõ, vùng nước sâu, khôi phục đèn và giai điệu chia tay. Điểm còn mỏng là hoạt động giữa các mốc và phần thưởng thị giác cho việc sửa đèn. Phần lớn phản hồi đang nằm ở âm thanh; dừng tàu/mở Camera thường đưa người chơi về một hình ảnh khá tĩnh.

Ước tính dưới đây dùng tọa độ config, tốc độ tối đa 18 đơn vị/s và lặn 5 m/s. Đây là thời gian đi thẳng lý tưởng, chưa tính tăng tốc, quay hướng, địa hình, tutorial, thiết bị, chụp/SEND và nghỉ phục hồi; không phải thời gian đã đo qua playtest.

| Đoạn | Di chuyển lý tưởng | Nội dung hiện có / khoảng trống |
| --- | --- | --- |
| Entry `(100,400)` → L1 `(440,115.71)` | Khoảng 25 s | Tutorial và các lớp tảo tăng theo khoảng cách/độ sâu. Chưa có nhiều dấu hiệu môi trường nhìn thấy được khi tiếp cận. |
| L1 → L2 `(1000,732.86)` | Khoảng 46 s; đổi 230 → 460 m cũng cần 46 s, có thể làm đồng thời | Tảo lùi xa, khoảng yên, ba tiếng gõ nếu đủ điều kiện, âm trầm ngắn khi tới gần L2. Chủ yếu là giữ lái/lặn. |
| L2 → Exit `(1875,680)` | Khoảng 49 s | Nếu lắp đèn ngay tại L2, phải đi khoảng 34 s mới vào vùng giai điệu cuối cách Exit 260 đơn vị. Chưa có hoạt động dùng đèn để quan sát một điều mới trên đường này. |

E06 hiện đợi 24 giây lái ở độ sâu từ 380 m trước tiếng co giãn đầu tiên. Nếu bắt đầu lặn tại L1, đi từ 380 tới 460 m chỉ cần 16 giây; vì vậy tiếng này có thể chưa xuất hiện trong chính đoạn lặn mà nó cần hỗ trợ. Đây là nhận định từ điều kiện code, không phải lỗi bắt buộc tái hiện trên mọi tuyến.

## Các thay đổi nên ưu tiên

### 1. Làm phần thưởng Bio Lamp nhìn thấy rõ

Sau INSTALL, giữ quyền điều khiển và cho người chơi chủ động mở Camera để kiểm tra ánh sáng. Đèn nên hé lộ cấu trúc/chi tiết môi trường đã có nhưng trước đó khó nhìn: vân địa hình, hạt lơ lửng, màu sinh vật. Cần artwork/lớp hiển thị tương ứng; lớp tối dịu đi và readout hiện tại chưa tạo được toàn bộ cảm giác này.

Tiêu chí: người chơi nhận ra sự khác biệt ngay trong một lần mở Camera; không phải quay về L1, không thêm địa điểm bắt buộc, không phải chờ thêm cutscene. Một nút kiểm tra đèn có thể dẫn vào Camera theo thao tác tự nguyện.

### 2. Cho L1 phản hồi khi người chơi dừng và quan sát

Giữ các lớp tảo hiện có, bổ sung chuyển động nhỏ hoặc nhịp phát sáng theo cộng hưởng khi người chơi ở đúng khoảng cách/độ sâu. Khi dừng máy, các lớp tinh tế tự lộ rõ nhờ lực đẩy nhỏ đi. Có thể thêm một đáp âm sinh học nhẹ sau radar, có cooldown; không đổi dữ liệu radar hoặc giả tín hiệu nhiệm vụ.

Tiêu chí: người chơi tự thấy việc dừng tàu có ích; PHOTO/SEND và CAPTURE vẫn là hai thao tác thật, độc lập. Thu một mẫu không làm biến mất rừng tảo. Không thêm câu hỏi/modal bắt buộc.

### 3. Đưa E06 vào đúng đoạn lặn và tạo dấu mốc môi trường

Cân nhắc một tiếng co giãn nhỏ khi thực sự lặn qua ngưỡng sâu lần đầu, đợi nếu ba tiếng gõ còn đang phát; các lần sau dùng khoảng nghỉ dài. Thêm sự thay đổi dần của nước/hạt và màu Camera theo độ sâu để người chơi cảm nhận hành trình 230 → 460 m.

Tiêu chí: đi sâu tạo khác biệt có thể nhận ra mà không dùng cảnh báo Hull giả, damage giả hay rung toàn màn hình. Không chèn sự kiện sát ngay sau ba tiếng gõ. Trigger dựa trên chuyển động và trạng thái thiết bị, không đếm giờ cưỡng ép lúc đang dùng Computer.

### 4. Tạo khác biệt cho lần chụp thứ hai

Dựa trên artwork sinh vật thứ hai, dùng một chu kỳ chuyển động/đổi hướng rất nhẹ để người chơi chủ động giữ khung trước khi chụp. Thông tin về độ sâu và khung ảnh cần dễ đọc; không biến thành minigame phản xạ khó.

Tiêu chí: L2 có một khoảnh khắc quan sát riêng thay vì lặp nguyên thao tác tại L1. Giữ âm trầm E07 chỉ 2–3 s và dừng khi tàu dừng hoặc vào Camera. Không thêm khả năng siêu nhiên, bóng người hay manh mối hồn ma cho sinh vật.

### 5. Cho đoạn ra Exit một việc quan sát nhỏ

Ưu tiên một lớp cảnh quan hiện rõ dưới Bio Lamp hoặc một mốc địa hình dễ nhận biết trên tuyến ra. Không cần thêm objective, vật phẩm hay kéo người chơi vòng lại. Giai điệu E09 tiếp tục là điểm kết; chỉ thêm phản hồi thị giác nhẹ trước đó nếu artwork hỗ trợ.

Tiêu chí: đèn có ích trước khi sang Zone 2, tuyến ra dễ hiểu, phần cuối không trở thành một chuỗi jumpscare.

## Khoảng lặng cần giữ

- Tutorial chỉ có âm vận hành bình thường để người chơi học tiếng tàu.
- Sau khi rời tảo phải có khoảng nghe rõ máy/nước trước sự kiện đầu tiên.
- Ba tiếng gõ không kéo theo nhiễu màn hình, đèn, radar, thoại giải thích hoặc tiếng gõ lặp lại. Người chơi dừng kiểm tra chỉ nghe vận hành bình thường.
- Không đặt quota sự kiện kinh dị theo mỗi N giây. Dùng playtest để tìm đoạn người chơi thiếu mục đích/không thấy phản hồi; rút ngắn hoặc bổ sung phản hồi môi trường tại đúng đoạn đó.
- Không thêm cutscene cưỡng ép giữa hai địa điểm. Cảnh mở đầu và readout lắp đèn hiện đã tạo hai mốc trình bày đủ rõ.

## Tình trạng refactor các zone

Bản refactor toàn bộ bốn zone ở yêu cầu đầu tiên chưa hoàn tất trên `main`. Zone 1 giữ trần lặn cơ bản 500 m. Đợt ngày 2026-10-22 đã cập nhật Zone 2 theo kịch bản Environmental Horror V2; Zone 3–4 vẫn chờ công việc nội dung riêng.

| Zone | Config hiện tại | Thiết kế refactor ban đầu còn phải làm |
| --- | --- | --- |
| 1 | 2 địa điểm; hai ảnh SEND, mẫu tảo, Bio Lamp → Zone 2 | Đã áp dụng kịch bản Tảo Hát mới nhất. |
| 2 | 2 địa điểm; PHOTO + CAPTURE mỗi nơi, hai mẫu, Pressure Hull 500 → 750 m → Zone 3 | Đã áp dụng kịch bản V2 và các sự kiện âm thanh/màn hình. Giữ ID mẫu sinh vật cũ để tương thích cargo/save. |
| 3 | 4 địa điểm; còn Mechanical Module, recipe Rock Breaker cũ | Còn 3 địa điểm; vật liệu thuốc nổ + kíp nổ từ hai CAPTURE, giữ phá đá/mở tuyến. |
| 4 | 5 địa điểm; 3 địa điểm ẩn; còn hidden item | Còn 4 địa điểm, đúng 2 điểm ẩn; CAPTURE L1 cấp hint tọa độ; cập nhật điều kiện ending. |

Điểm nghẽn 510 m của Zone 2 đã giải quyết bằng cách đưa sinh vật giáp cứng về 460 m, rồi mở trần 750 m bằng giao dịch Pressure Hull. Capture, recipe, UI, installer và migration/save của Zone 2 được nối cùng đợt. Chi tiết: `Docs/Zone02EnvironmentalHorror.md`. Zone 3–4 vẫn chưa đáp ứng toàn bộ thiết kế refactor mới.
