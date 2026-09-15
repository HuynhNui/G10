# Asset cần chuẩn bị cho minigame bắt sinh vật

Minigame chưa triển khai ở phiên này. Checklist theo phương án MVP trong kế hoạch: giữ chuột/phím để nâng vùng bắt, thả để hạ; giữ sinh vật trong vùng bắt để tăng tiến độ. Thiết kế đề xuất dùng thanh dọc, có thể đổi khi chốt hình.

Thư mục đề xuất: `Assets/_Project/Art/UI/CaptureMinigame/`.

## Ưu tiên gửi trước

| Tên file gợi ý | Nội dung | Kích thước gợi ý |
|---|---|---|
| `Capture_Background.png` | Nền màn hình/thiết bị bắt. Chừa trống nơi chạy minigame, tiến độ và chữ; không vẽ sẵn mục tiêu/kim/reticle | 1920 × 1080 |
| `Capture_Track_Frame.png` | Khung của thanh dọc; lòng khung trong suốt, không gộp phần di chuyển | Khoảng 320 × 760 |
| `Capture_Reticle.png` | Vùng bắt do người chơi điều khiển; viền rõ, lòng trong suốt hoặc bán trong suốt | Khoảng 220 × 160 |
| `Capture_Target.png` | Biểu tượng nhỏ đại diện sinh vật di chuyển trong thanh | 128 × 128 hoặc 256 × 256; có thể dùng lại sprite sinh vật đã xử lý nên không bắt buộc vẽ mới |

Các kích thước là gợi ý cho canvas 1920 × 1080, không phải ràng buộc. Nếu bố cục khác, gửi một ảnh mockup kèm các phần tách riêng.

## Có thể gửi sau, hoặc để code dựng bản đầu

| Tên file gợi ý | Nội dung |
|---|---|
| `Progress_Frame.png`, `Progress_Fill.png` | Khung và phần ruột tiến độ, tách thành hai ảnh; code điều khiển lượng fill |
| `Reticle_Active.png`, `Reticle_Miss.png` | Biến thể vùng bắt khi trúng/trượt; cũng có thể đổi màu bằng code |
| `Button_Idle.png`, `Button_Hover.png`, `Button_Pressed.png` | Nền nút dùng chung cho Bắt / Thử lại / Thoát; không gộp chữ nếu muốn dễ đổi nội dung |
| `Result_Success.png`, `Result_Failure.png` | Biểu tượng kết quả; bảng và chữ có thể dựng bằng UI |
| `catch_start.wav`, `catch_loop.wav`, `catch_success.wav`, `catch_fail.wav` | Âm thanh tùy chọn; loop cần cắt điểm nối liền mạch |

Không cần chuẩn bị thanh tension riêng cho MVP nếu dùng một thanh tiến độ tăng/giảm. Không cần sprite sheet hoặc animation sinh vật để bắt đầu; một sprite tĩnh là đủ.

## Cách xuất ảnh

- PNG có alpha thật cho khung, reticle, mục tiêu và icon; không vẽ nền caro vào file.
- Các ảnh Idle/Hover/Pressed của cùng một bộ phận giữ cùng kích thước canvas và vị trí để không nhảy khi đổi trạng thái.
- Mỗi phần cần di chuyển hoặc thay đổi bằng code phải là một file/layer riêng.
- Giữ một ảnh mockup tổng để xác định bố cục; chỉ dẫn vùng thao tác/điểm neo có thể vẽ trên bản tham chiếu riêng.
- Có thể gửi PSD/layer gốc kèm PNG nếu có; không bắt buộc.
