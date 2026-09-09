# Nơi nạp asset — Cabin và Zone 1

Bạn đặt ảnh vào thư mục tương ứng bên dưới rồi báo mình đã nạp xong. Không cần đổi tên file đang có. Ưu tiên nạp nhóm 01–05 trước để làm point-and-click và điều khiển tàu thô.

Đây là thư mục nhận file gốc, nằm ngoài Assets để Unity không tự import các bản nháp hoặc file nguồn lớn. Khi triển khai, Codex sẽ đưa các file dùng trong game vào Assets/_Project/Art và thiết lập import phù hợp. Asset hiện có trong project được giữ nguyên, không cần nạp lại.

| Thư mục | Đặt gì vào đây |
|---|---|
| 01_Cabin | Cabin gốc nếu có bản mới/PSD; ảnh cabin đánh dấu vị trí click và tên chức năng; các thiết bị rời nếu có |
| 02_Navigation | Nền bảng lái cận cảnh; nút, cần ga, núm xoay, mặt la bàn và kim rời |
| 03_Zone01_Map | Bản đồ Miền Tảo Hát; bản phác đường đi/vật cản; bản chú thích vị trí trạm, điểm xuất phát và ranh giới zone |
| 04_Radar | Nền bảng radar, khung màn hình trống, nút Scan |
| 05_Power | Mặt đồng hồ điện, kim hoặc thanh pin rời, đèn báo; nền Power Management |
| 06_Camera | Khung camera, nút chụp, ảnh cảnh ngoài tàu Zone 1 để thử |
| 07_Capture | Nền bảng Capture Array, khung vùng Signal Lock |
| 08_Containment | Khoang chứa trống; hình mẫu/khoang có mẫu khi có |
| 09_Inventory | Nền kho đồ, ô chứa, icon vật phẩm nếu có |
| 10_Log_Archive | Nền sổ hoặc màn hình nhiệm vụ, ảnh, hồ sơ sinh vật |
| 11_Damaged_Modules | Hình hoặc lớp phủ trạng thái hỏng của bộ áp suất, đèn sâu, khoan |
| 12_Shared_UI | Nút dùng chung, con trỏ, icon, font có quyền sử dụng, bảng màu hoặc ảnh tham chiếu phong cách |

## Chuẩn bị file

- Nền: PNG hoặc JPG. Chi tiết rời: PNG nền trong suốt.
- Có PSD hoặc file nguồn giữ layer thì gửi kèm trong cùng nhóm; nên có PNG xem nhanh.
- Không vẽ cố định tọa độ, số pin, lượt scan lên nền. Chừa vùng trống để game cập nhật chữ/số.
- Kim, cần gạt, đèn và các phần cần chuyển động nên tách riêng nếu có thể.
- Giữ độ phân giải gốc. Các lớp phủ cabin nên cùng kích thước canvas và khớp vị trí với ảnh nền.
- Bản có chú thích nên thêm hậu tố `_annotated`; bản tham khảo thêm `_reference` để phân biệt với ảnh dùng trong game.
- Nếu có nhiều phương án, ghi bản muốn dùng vào GHI_CHU.md. Không cần làm đủ tất cả thư mục mới bắt đầu được.

## Phần Codex có thể dựng tạm

Vùng click, hover, chữ/số, tia quét radar, chấm tín hiệu, pin bản đồ, nút cơ bản và thông báo hết điện có thể dựng bằng Unity. Chưa cần chuẩn bị ảnh riêng cho các phần này.
