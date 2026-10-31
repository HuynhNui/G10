# Zone04 — một redesign sau baseline bị chặn

Thiết kế này được lập **sau** [baseline đã khóa](01_Baseline_Playtest.md) và [audit](02_Experience_Audit.md). Đây là một vòng duy nhất. Baseline chưa tới ending; thiết kế không biến các phần chưa gặp thành thất bại đã quan sát. Quyền tự triển khai nằm trong brief của người dùng; không có bước xin duyệt mới hoặc mua tài nguyên.

## Trục trải nghiệm

**Khảo sát quen thuộc → đầu mối được phục hồi → tự chọn khám phá → hai quan sát khó giải thích → khoảng nước rút âm → tín hiệu cuối → dữ liệu đã lưu nhưng tín hiệu còn tiếp diễn.**

Giữ bí ẩn ở dụng cụ và môi trường; không thêm lời giải thích, người đuổi theo, jumpscare cuối, objective hay sinh vật mới. Gameplay vẫn quyết định mọi receipt PHOTO/SEND/CAPTURE, Radar reveal, explore choice và physical ending.

## 1. Đọc chart chính xác hơn, giữ cơ chế tự định vị

Nút thắt baseline nằm ở cách diễn giải hình, không phải đường không tồn tại. Thêm một lớp **ĐỊA HÌNH** riêng cho chart Zone04: biểu diễn đúng các ô vật cản từ static map configuration đã được tác giả vẽ, căn cùng UV/rect với base map. Màu vàng nhạt nhất quán với chấm địa hình Radar; art vẫn ở dưới và người chơi có nút bật/tắt lớp này. Một chú giải ngắn nói vàng nhạt là vật cản.

Không hiện tàu trên chart, không vẽ đường đi, không tìm đường tự động, không thêm marker chưa khám phá hoặc live Radar. Giữ toàn bộ tọa độ/geometry/cost/exit. Các bản đồ Zone01–03 không đổi. Lớp này cho người chơi một cách đọc thông tin vốn có, không thay luật điều hướng.

Triển khai trong existing `ZoneMapPresentation` với một helper uGUI nhỏ nếu cần; không sửa serialized scenes. Cache texture/graphic theo map, không giải mã hoặc dựng lại mỗi frame; dispose khi owner bị hủy. UI chỉ dùng từ cho người chơi, không hiển thị tên mask/thuật toán. Native phải chứng minh UV, dữ liệu source không đổi, toggle, cache/lifetime, pointer/POI/hidden marker rules và quy tắc không hiện vị trí tàu.

## 2. Cơ hội chỉ được tiêu sau checkpoint tiếp nhận

Thay played-at-launch bằng một pending presentation cục bộ có handle riêng. Không tạo global horror framework. Điều kiện audience gắn với chính Mission Log, ảnh A/B đang chọn hoặc Helm; mất audience, focus, pause, modal/transition trước checkpoint phải hủy tail và giữ khả năng thử lại. Không gọi Scan/SEND/capture hoặc tính phí để retry.

| Cue | Cơ hội và checkpoint mới |
|---|---|
| fragment | Sau CAPTURE thật, trong Mission Log. Card đã hiện cả hai tọa độ và progress hoàn tất, giữ đọc được ít nhất0,25s qua các frame thực. Không yêu cầu voice1,48s còn phát ở thời điểm card2,5s. |
| hidden.a / hidden.b | Ảnh đúng site trong Photo Lab thật. Voice production thực đang tiến ít nhất0,65s liên tục qua nhiều frame, đúng audience và output/category cho phép. Đây là checkpoint đoạn đầu, không chứng minh nghe trọn9s hoặc nhận ra ý nghĩa. |
| final.signal | Earn từ chuyển động thật trong vùng approach đã mở; lưu opportunity trong existing played-list nếu cần. Sau offset1,2s, voice thật tiến0,65s cùng Helm/tint thực. Braking vẫn cho sequence tiếp tục; đổi dụng cụ trước checkpoint không mất cơ hội. Không chọn hoặc ghi ending. |

Không dùng giá trị `Tick(seconds)` do fixture truyền để chứng minh exposure. Legacy primary played receipts tiếp tục được tôn trọng, không replay hàng loạt save cũ. Publication thất bại trước delivery retry tại chỗ có backoff; đã delivery nhưng ghi receipt thất bại giữ trạng thái chờ publish trong cùng phiên, không phát lại attack. Không hứa exactly-once xuyên crash/reload khi storage vẫn không ghi được; giới hạn này phải ghi trong04/06.

Continue/Rest/Journal dùng cùng timeline authority. Rollback hủy owned effects và loại tương lai theo snapshot. Mọi stop chỉ tác động handle của owner, giữ controls/warnings và các effect ngoài scope.

## 3. Khoảng yên phục vụ tín hiệu, dùng nguồn hiện có

Giữ engine, ventilation, propulsion, steering, depth, warnings và minigame trên bus của chúng. Dùng environmental bed gain/filter đã có để rút nhẹ lớp nước trong hidden/final primary và giữ recovery khoảng4s sau khi sequence dừng, rồi phục hồi theo fade hiện có. Không chèn silent WAV, không bypass mute, không khóa điều khiển. Distant structure không chồng vào primary/recovery; không tăng số scare để lấp thời gian điều hướng.

Trong báo cáo, mức gain và khoảng recovery là dữ liệu kỹ thuật; anticipation, spatial recognition và cảm giác bị quan sát vẫn cần người thật nghe/chơi.

## 4. Chi tiết ảnh L1

Kiểm tra và điều chỉnh có giới hạn vị trí/kích thước/contrast của wreck procedural hiện có để nó tách khỏi cây foreground ở kích thước Photo Lab thực, vẫn xa và không giải thích nguồn tọa độ. Giữ subject texture/alpha, validation coverage/occlusion, camera geometry và PNG archive authority. Native matched captures trước/sau phải chứng minh thay đổi chỉ trong vùng background detail, không đổi photo result/coverage/occlusion. Không nâng hạng ảnh hoặc auto SEND.

Không thay generic creature bindings trong vòng này: bốn identity art chưa được tác giả định nghĩa thành phẩm, và thay silhouette có thể ảnh hưởng framing/coverage. Giới hạn art được giữ công khai; không coi nó đã được giải quyết.

## Tài nguyên, kiến trúc và xác minh

- Reuse tám WAV, fragment/hidden/final/ending sequences, existing illustrated art, procedural textures, EffectDirector/EffectSequence và native uGUI. Không thêm package, external source, paid generation hoặc license claim. Lớp chart là texture runtime từ dữ liệu map hiện có, không phải artwork thay thế. Provenance/license audio cũ giữ nguyên.
- Runtime scope dự kiến: `ZoneFourPresentationController`/state, `ZoneFourPhotoComposition`, `ZoneMapPresentation` + helper nhỏ; existing `ExpeditionLoop.CommitZoneFourCue` đủ nếu không cần schema mới. Editor telemetry có thể đọc public pending/stage mới; baseline fields cũ vẫn unknown. Không sửa mission/map assets hoặc shared serialized scenes.
- Cập nhật native fixture kỳ vọng launch receipt cũ, thêm meaningful precheckpoint interruption, focus/mute, same-photo retry, failed publication, Continue/Rest/Journal và legacy-no-replay. Giữ actual PHOTO→Lab→SEND, capture, Radar charge/reveal, choice, physical hidden ending và normal ending regressions. Redirect QA output để không ghi đè evidence lịch sử.
- Chạy native compile/tests trong outer isolation. Review actual map/Photo Lab/fragment/ending images và raw listener capture; test không thay lượt cuối.
- Lượt cuối bắt đầu DEBUG Zone04 NONE với state tương đương `baseline/initial-debug-save.json`, ordinary input và lớp chart thật. Có thể dùng perception/planning từ **ảnh UI mới được render thật**, không dùng path/coordinates từ controlled private-mask diagnosis. Ghi rõ mọi assistance, va chạm, Rest, thời gian processing và frame/audio gaps. Đi tới physical hidden ending và đủ16s aftermath.

## Tiêu chí chấp nhận và giới hạn so sánh

Chart rõ hơn và recovery cue phải được kiểm chứng bằng hành vi/ảnh thực; không chỉ có cờ boolean. Không đổi mission counts, vị trí, captures, costs, endings, return gates hoặc original user changes. Bản cuối phải hoàn tất hành trình hoặc công khai blocker thật; không mở vòng redesign thứ hai sau đánh giá cuối.

So sánh cuối giữ baseline là **incomplete/blocked**, các metric ending và A/B/final baseline là N/A. Thời gian cuối không thể trừ cho baseline để tuyên bố nhanh hơn: có learning, thay phương pháp đọc chart, checkpoint replay và21h gián đoạn. Điểm cảm xúc là giả thuyết thiết kế, chưa có human feedback.
