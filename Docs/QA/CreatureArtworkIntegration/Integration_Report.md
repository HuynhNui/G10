# Tích hợp artwork sinh vật G10

Hoàn tất ngày 03/11/2026 trên nhánh `codex/creature-artwork-integration`, từ HEAD `2f9451c`. Đã tải và xác minh 5 PSD thật từ Drive, xuất native RGBA, gắn đủ **11 sinh vật** vào catalog hiện có và kiểm tra trong Unity 6000.4.2f1. Không commit, merge hoặc push.

## Mapping đã triển khai

Chỉ số artboard trong bảng bắt đầu từ 1; đường dẫn group chính xác, Drive ID, SHA, crop và căn cứ đầy đủ nằm trong [manifest](D:/G10/Docs/QA/CreatureArtworkIntegration/artwork-manifest.json). Mức chắc chắn thể hiện đánh giá tích hợp, không phải xác nhận của họa sĩ.

| Creature ID giữ nguyên | Artwork / artboard | Vai trò và căn cứ | Chắc chắn |
| --- | --- | --- | --- |
| Z1_Creature_01 | sinh vật 1 remake / 1 | Tảo Hát; đối chiếu hình dây/hoa và artwork cũ, mẫu Bio Lamp | Cao |
| Z1_Creature_02 | sinh vật 2 khu vực 1 / 1 | Loài nhút nhát ẩn trong rong, đúng caption và zone | Cao |
| Z2_Creature_01 | SINH VẬT ẢI 2 VÀ 3 / 1 | Thân mềm, layer túi/chất tiết, vật liệu kết dính | Vừa, suy luận |
| Z2_Creature_02 | Cùng PSD / 5 | Vảy/giáp xếp lớp, mẫu vỏ chịu áp lực | Vừa, suy luận |
| Z3_Creature_01 | Cùng PSD / 3 | Thân trong với nếp đỏ bên trong, mẫu áp lực/chất nổ | Vừa, suy luận |
| Z3_Creature_02 | Cùng PSD / 2 | Cấu trúc pha lê khớp cue crystal motion | Vừa–cao, suy luận |
| Z3_Creature_03 | Cùng PSD / 4 | Cấu trúc sáng trên lưng, mẫu điện sinh học/detonator | Vừa, suy luận |
| Z4_Creature_01 | sinh vật và hù ma ải 4 / 1 | Có group xác tàu riêng ngay cùng tranh, khớp L1 | Cao |
| Z4_Creature_02 | Cùng PSD / 2 | Sinh vật thường thứ hai, vây dài xanh | Vừa, suy luận |
| Z4_Creature_03 | Cùng PSD / 3 | Sinh vật ẩn A, cụm phát sáng gần trầm tích | Vừa, suy luận |
| Z4_Creature_04 | Cùng PSD / 4 | Sinh vật ẩn B, hàm/lure bất đối xứng | Vừa, suy luận |

Không gán theo thứ tự trái–phải: bundle Zone2/3 được chọn theo đặc điểm và vai trò nhiệm vụ. PSD `SINH VAT 1` cũ chỉ dùng so sánh; hình trái tim Tảo Hát là vật phẩm, không tạo thêm creature ID.

![Contact sheet có ID](D:/G10/Docs/QA/CreatureArtworkIntegration/creature-contact-sheet.png)

## Những nơi đã tích hợp

- PHOTO dùng ảnh mới từ catalog, giữ tỷ lệ pixel trong khung chiếu hiện có. Coverage/occlusion dùng footprint cũ; không sửa threshold, depth, bearing, tài nguyên hoặc nhiệm vụ.
- PhotoLab/gallery/SEND hiển thị cùng PNG đã chụp. Sửa khung preview để ảnh 16:9 không bị kéo thành 2:1. Không dựng lại ảnh lịch sử từ catalog.
- Capture minigame lấy đúng artwork theo target, fit trong visual box hiện có và dùng UV đầy đủ. Đã phát hiện và sửa crop UV kế thừa từ texture cũ; default encounter khôi phục cả texture lẫn UV ban đầu. Collider, tốc độ, số hit và thời gian không đổi.
- Cargo dùng ảnh catalog; 4 icon vật liệu/alias có ID sinh vật được gắn cùng sprite. Hai prefab Zone01 cũng tham chiếu sprite tương ứng. Vật phẩm có minh họa riêng giữ nguyên.

Nguồn có group sinh vật riêng nên không cần tách nền bằng màu hoặc vẽ lại. Xuất giữ nguyên RGBA, màu, halo và nét gốc; thêm 24px padding, không resize. Bốn crop có tọa độ đã kiểm tra loại riêng các nét thử/swatch rời, giữ toàn bộ thân, vây và phần phát sáng. Wash và disk của Z3_Creature_02 được giữ vì chung layer với phần bóng của sinh vật, không thể bỏ riêng bằng một crop an toàn.

Hai hình ma và group xác tàu được xuất trong `Finished/Reserve`, chưa gắn runtime. Các sequence hiện có dùng reflection/procedural cues với vai trò riêng; không có điểm gắn mới đủ rõ để thay bằng những hình này mà vẫn giữ ý nghĩa và bố cục hiện tại. Không thêm scare sequence hoặc ép chúng thành sinh vật. Xem [reserve sheet](D:/G10/Docs/QA/CreatureArtworkIntegration/reserve-contact-sheet.png).

## Kiểm tra Unity và hình ảnh

Đây là **fixture tích hợp có hỗ trợ**, dùng contact/depth thật, debug preset, đặt tàu có kiểm soát, refill, callback lựa chọn khám phá và helper thắng capture. Không phải lượt chơi hoàn thành bằng input bình thường và không đánh giá lại horror pacing.

| Kiểm tra | Kết quả cuối |
| --- | --- |
| EditMode compositor: mask, alpha, aspect và foreground occlusion | 11/11 đạt |
| Mẫu Zone2 trong Lab → SEND → Continue trước khi gắn toàn bộ | Đạt |
| Matrix cuối: 11 ảnh, 2 loài ẩn, 6 capture/cargo, gallery, SEND và Continue | Đạt, job `c62d20014e23`, 80,12 giây |
| Hồi quy ảnh lỗi, SEND lỗi, legacy archive, pending/submitted Continue, Bio Lamp, capture và fallback | 7/7 đạt; hai case capture được xác minh lại sau sửa UV |
| Compiler / `git diff --check` | 0 lỗi / đạt |
| Export: 14 PNG riêng, pixel RGBA/padding/SHA và 5 PSD nguồn | Đạt |

Matrix so sánh chính xác coverage/occlusion trước–sau cho cả 11 loài: Zone1/2 cố định clock; Zone3/4 dùng shutter hiệu chuẩn tại cùng vị trí/depth/bearing. Kết quả không đổi. Ảnh mới giữ nguyên pixel qua pending Continue, SEND, submitted Continue và timeline. Một PNG lịch sử thật được sao chép vào archive isolation để chứng minh ảnh và metadata cũ không bị thay.

PNG có thể được Unity mã hóa RGB24 → RGBA khi đọc/ghi lại, nên kiểm tra pixel và kích thước tách khỏi hash encoding. File PNG archive gốc vẫn giữ nguyên byte. Các lần fixture thất bại ban đầu và nguyên nhân sửa được ghi trong [fixture notes](D:/G10/Docs/QA/CreatureArtworkIntegration/preparation/native-fixture-notes.md); không sửa production save để làm test qua.

Đã mở screenshot toàn khung của 11 sinh vật trong Lab, mẫu trước/sau, 6 capture, 6 cargo và ảnh lịch sử. Sáu capture cuối hiển thị đủ thân, đúng tỷ lệ và UI đầy đủ; lỗi UV không còn. [Visual QA và SHA cuối](D:/G10/Docs/QA/CreatureArtworkIntegration/visual-review-capture.md). Mục tiêu capture vẫn nhỏ theo profile hiện có; chi tiết artwork thể hiện rõ hơn trong Cargo/Lab. Ánh sáng trước Bio Lamp và sắc tối của Zone4 được giữ theo gameplay hiện tại.

![Zone2 trong PhotoLab](D:/G10/Docs/QA/CreatureArtworkIntegration/native-validation/AllCreatures/Z2_Creature_01-01-lab.png)

![Sinh vật ẩn Zone4 sau Continue và SEND](D:/G10/Docs/QA/CreatureArtworkIntegration/native-validation/AllCreatures/Z4_Creature_04-03-continued-submitted-lab.png)

## Bảo toàn và tái tạo

Save isolation đã tắt sau khi thoát Play; cả hai path override trở về null, Bootstrap sạch. Toàn bộ 6 file Expedition và 48 PNG/JSON gốc giữ nguyên SHA. Đã kiểm tra 2.378 backup; chỉ thay đổi source/asset đúng phạm vi. Map, mission, profile, scene, ProjectSettings và Packages giữ nguyên. Không phục hồi/xóa Analytics phát sinh hoặc luân chuyển trong thời gian Editor hoạt động. XML test cuối được lưu trong QA rồi khôi phục riêng `TestResults.xml` gốc đã xác minh.

Nguồn PSD nằm trong `.tools` đã được ignore. [Hướng dẫn và script tái xuất/import](D:/G10/Tools/Artwork/README.md), [mapping có thể sửa](D:/G10/Tools/Artwork/creature_mapping.json), [bằng chứng runtime cuối](D:/G10/Docs/QA/CreatureArtworkIntegration/native-validation/AllCreatures/evidence.json), [audit bảo toàn](D:/G10/Docs/QA/CreatureArtworkIntegration/final-readonly-preservation-audit.md).

Không còn lỗi tích hợp đã biết sau kiểm tra cuối. Giới hạn còn lại là mapping suy luận ở các tranh không ghi ID, nét/wash gốc được giữ có chủ đích và kích thước target theo profile cũ.

[Xác minh sau dọn kết quả test](D:/G10/Docs/QA/CreatureArtworkIntegration/preservation-final-summary.json): 54/54 file gameplay giữ nguyên, font khớp bản đầu và TestResults.xml gốc đã được khôi phục sau khi lưu bằng chứng PASS vào QA.
