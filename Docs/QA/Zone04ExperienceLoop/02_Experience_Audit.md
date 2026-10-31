# Zone04 — audit trải nghiệm từ baseline bị chặn có giới hạn

Baseline đã được khóa trước mọi sửa production: [01 và timeline](01_Baseline_Playtest.md), [JSON](01_Baseline_Timeline.json). Đây là **baseline chưa hoàn tất**, dừng ở việc tìm đường tới L2. Audit không đại diện cho trải nghiệm toàn chương và không xác nhận mức sợ của người chơi. Chỉ L1 PHOTO → SEND → CAPTURE và fragment đã được gặp thật. Các nhận định về phần sau dựa trên mã nguồn và được ghi riêng.

## Bằng chứng và cách đọc

- Hai đoạn ghi thật dài tổng cộng 4.594,984 giây; phần được ghi từ ready tới mẫu cuối là 4.316,164 giây. Khoảng gián đoạn ngoài game 76.178,810 giây và rollback Continue 775,515 m được công khai; không gọi đây là thời lượng nội dung hoặc một phiên liền mạch.
- 495,916 giây có hoạt động điều hướng; 368,346 giây chuyển động ngang; 170,462 giây xoay; 17,725 giây chỉnh sâu. Các nhóm chồng lấn. Có 19 marker dừng vì vật cản, 17 mẫu giảm hull và 6 Rest thật. Thời gian không có input chưa phân loại không được gọi là sợ, do dự hoặc chán.
- Fragment hiện trong Mission Log khoảng 3,629 giây theo tích phân mẫu. [Ảnh thật](baseline/evidence/frames/frame-002879.jpg) cho thấy cả hai tọa độ, hướng dẫn xác minh bằng Radar và thanh tiến trình đọc được. Âm được ghi từ listener thật; nhận biết bằng tai người chưa được đo.
- [Chẩn đoán độc lập sau khi dừng baseline](preparation/chart-diagnosis.md) xác nhận đường hull hợp lệ tới L2 tồn tại, độ rộng được chứng nhận bảo thủ khoảng 19,95 world units; RLE khớp Mapline ở cả 518.400 ô. Không xuất đường đi hoặc dùng kết quả này làm shortcut cho lượt đo.

## Góc nhìn đặt vào người chơi

L1 gần điểm vào tạo một mở đầu có việc làm ngay. Ảnh phải được SEND bằng tay, rồi capture mới cho thông tin trong Mission Log; điều đó nối công cụ với nhiệm vụ rõ hơn một cutscene tự phát. Fragment dẫn tới hai tọa độ mà vẫn cần khám phá thật là cấu trúc tốt cho việc tự quyết định có khám phá tiếp hay không.

Phần yếu được gặp rõ nhất là việc đọc tuyến. Lượt điều khiển đã phải lặp Map → Helm → Radar → lùi tàu → Rest, trong khi chỉ hai objective đầu được hoàn tất. Những va chạm này là sự kiện thật của lượt AI; chúng không chứng minh mọi người mới đều mắc như vậy. Tuy nhiên, thử chỉ lần theo nét cyan sáng đã bỏ qua phần lớn biên va chạm. Chẩn đoán cho thấy 91.566/104.726 ô va chạm không đạt ngưỡng cyan đã dùng. Bản đồ cần giúp phân biệt thông tin địa hình với màu minh họa dễ hơn, mà vẫn giữ cơ chế tự đối chiếu tọa độ; vị trí tàu được tác giả cố ý giữ ngoài chart.

Radar có các chấm địa hình thật trong lúc quét: [frame002365](baseline/continuation-01/frames/frame-002365.jpg). Ảnh xem sau sweep đã làm pilot nhận định sai rằng chỉ có cảnh báo chữ. Đây là giới hạn lấy mẫu/đọc ảnh của công cụ, chưa phải lỗi mất Radar. Dữ liệu Radar dùng vùng nước được flood-fill, còn va chạm dùng đường viền gốc; không được coi mỗi ô tô trên Radar là phép đo trực tiếp cho một hướng di chuyển có thể/không thể đi.

## Nhịp và giả thuyết tension

Thang 1–4 trong bảng này là đóng góp thiết kế giả định, không đo cảm xúc. Thời gian A/B là đồng hồ từng recorder; không lấp khoảng ngắt bằng suspense.

| Mốc thật | Quan sát | Tension giả thuyết / giới hạn |
|---|---|---|
| A 278,806 s — ready | Tàu ngày 1, L1 ở gần; hệ thống hoạt động bình thường | 1: thiết lập công cụ. |
| A 511,106 s — SEND L1 | Ảnh được xác nhận qua Photo Lab | 2: bằng chứng khảo sát gắn với hành động. |
| A 569,728 s — CAPTURE | Minigame thật hoàn tất | 2: hoàn thành công việc, nhận đầu mối. |
| A 589,818 s — fragment | Hai tín hiệu được giải mã trong Mission Log | 3: mở câu hỏi về nguồn dữ liệu; chưa biết người có diễn giải là đe dọa. |
| Sau fragment, A/B tìm L2 | Nhiều tuyến thử, va chạm, kiểm tra thiết bị và Rest | Chưa phân loại: không nâng tension dựa trên processing hoặc va chạm. Có nguy cơ sự chú ý chuyển sang giải bài toán điều hướng. |
| Điểm dừng B 1894,973 s | Chưa SEND L2, chưa chọn explore/ending | Không có climax/aftermath toàn chương để đánh giá từ baseline này. |

Không thể tính một khoảng “không có sự kiện horror có ý nghĩa dài nhất của toàn Zone04” từ baseline chưa hoàn tất. Khoảng giữa các primary không áp dụng vì chỉ một primary đã gặp; ambience và công việc vẫn diễn ra.

## Audit từng sự kiện

| Sự kiện | Bằng chứng baseline | Đánh giá |
|---|---|---|
| Ảnh L1 / wreck | [Photo Lab thật](baseline/evidence/frames/frame-002508.jpg), GoodPhoto, SEND đã xong | Sinh vật fallback lớn chiếm hình; root và reviewer không xác định chắc wreck trong ảnh xem này. Việc compositor chạy không chứng minh chi tiết được nhận ra. |
| Fragment | Card, hai tọa độ, voice clock, WAV và receipt đều có | Gặp thành công; receipt tồn tại qua Continue, không thấy lặp. |
| Hidden A / B | **NOT OBSERVED** | Chưa tới điều kiện; không gọi là bị bỏ lỡ hoặc lỗi delivery trong baseline. |
| Final Signal | **NOT OBSERVED** | Chưa mở tuyến; vị trí/nhịp kết chưa được đánh giá bằng lượt này. |
| Normal / Hidden Ending | **NOT OBSERVED** | Kết quả native cũ chỉ là lịch sử, không thay bằng chứng thực của vòng này. |

## Âm thanh, hình và tài nguyên

Màu nước xanh tối của ảnh thống nhất với không khí vực sâu; cabin và giao diện vẫn giữ minh họa có sẵn, controls đọc được. L1 có foreground lớn làm chi tiết hậu cảnh khó xác định. Bốn creature binding Zone04 vẫn dùng fallback chung theo source; chưa có dữ liệu thực cho thấy người phân biệt bốn site bằng hình sinh vật. Đây là giới hạn art, không phải thành phẩm creature mới.

Tám WAV và các sequence Zone04 đã có sparse water, machinery, fragment, A/B, final signature và hai ending. Baseline WAV tổng 4.594,197 giây PCM stereo, RMS −31,585 dBFS, 19 giá trị chạm rail trong 11 sample frames, không có ring drop. Chưa có human listening để kết luận cân bằng, hướng âm, clipping nghe được hoặc tính đáng nhớ của motif. Fixed ambient structure timer 18/47 giây là một đặc tính source cần cân nhắc; baseline không đo được tính dự đoán bởi người.

Không đủ bằng chứng để cần mua/generate thêm âm hoặc thay thư viện. Tài nguyên hiện có có thể được khai thác tốt hơn qua delivery/recovery và độ rõ của chi tiết. Quyền/license gốc giữ theo [production record](../../Zone04FinalChapterProduction.md), không được suy thành commercial clearance mới.

## Rủi ro source, khác với lỗi đã gặp

[Source audit](source-audit.md) xác nhận `fragment`, `hidden.a`, `hidden.b`, `final.signal` được ghi played ngay khi handle hợp lệ. Fragment B chỉ hiện ở 1,65 giây; FinalSignal audio bắt đầu ở 1,2 giây. Đóng audience hoặc pause sớm có thể tiêu cơ hội trước phần chính. Baseline fragment thực đã hoàn tất; chưa có tình huống mất cue như vậy trong hai đoạn đo. Đây là rủi ro cần kiểm chứng bằng native interruption tests.

Fragment audio chỉ dài 1,48 giây, ngắn hơn thời điểm hai tọa độ đọc xong: không thể dùng điều kiện “card hoàn tất và voice đang phát cùng lúc”. Hidden photo phải giữ đúng identity ảnh, còn final approach phải tiếp tục tách khỏi quyền quyết định ending.

Chẩn đoán phát hiện 14 seed hull hợp lệ có thể bị square padding của Radar giữ trong một vùng 7–8 ô; chưa có bằng chứng baseline rơi vào trường hợp này. Tách thành rủi ro kỹ thuật hẹp, không gán làm nguyên nhân của toàn bộ tìm đường.

## Ưu tiên cho một redesign

1. **P1 — độ rõ thông tin chart:** cung cấp cách nhìn biên địa hình chính xác và giải thích ngắn gọn; giữ nguyên art, tọa độ, hình học, chi phí, hidden discovery và quy tắc không hiện tàu trên chart. Đây là giả thuyết UX cần native/actual validation, không kết luận map vật lý bị sai.
2. **P1 — delivery bền qua đổi tác vụ:** played sau exposure phù hợp từng cue; giữ retry trước checkpoint, đúng audience, không tự SEND/Scan hay thu charge.
3. **P2 — tương phản/đặt chi tiết ảnh:** kiểm tra wreck ở kích thước Photo Lab thực. Ưu tiên điều chỉnh procedural detail hiện có trước khi thêm sinh vật/overlay không thuộc mỹ thuật.
4. **P2 — recovery và khoảng yên:** dùng nguồn âm hiện có, giữ machinery/warnings; bảo vệ nhịp chính khỏi thao tác bận. Tác động tới anticipation phải còn là giả thuyết cho human playtest.
5. **P3 — art creature và Radar seed:** ghi giới hạn riêng; không thay chúng thành nguyên nhân chưa chứng minh hoặc mở rộng framework trong vòng này.

## Rubric và điểm tạm thời

1 = thiếu/hỏng; 2 = rời rạc hoặc khó tiếp nhận; 3 = nhất quán và nhận thấy được nhưng còn hạn chế; 4 = đặt theo ngữ cảnh với recovery/persistence đã kiểm chứng; 5 = ổn định qua playtest người đại diện. Điểm chỉ áp dụng phần đã quan sát L1 → các tuyến thử, **không phải điểm toàn chương**.

| Chiều | Baseline tạm thời | Cơ sở / phần chưa biết |
|---|---:|---|
| Không khí | 3 | Cùng art, ảnh tối, bed/machinery ghi thật; chưa có listening người. |
| Chờ đợi | 2 | Fragment dẫn tới đầu mối; chưa gặp chuỗi escalation tiếp theo. |
| Bất định | 2 | Tọa độ khơi câu hỏi; wreck chưa xác định được từ ảnh xem. |
| Nhịp | 2 | Nút thắt điều hướng được gặp; chưa đánh giá climax/recovery toàn chương. |
| Tác động sự kiện | 3 | Fragment đọc được và có nguồn âm thật; A/B/final chưa biết. |
| Agency | 3 | Điều khiển, SEND, capture, Rest có quyền thật; thông tin chart bị pilot hiểu sai, recovery cue còn rủi ro source. |

Không có phản hồi từ người chơi thật. Mọi kết luận “sợ hơn”, tìm nguồn âm do bất an, hay recheck thiết bị do horror đều chưa được chứng minh. Baseline này đủ để thiết kế một cải tiến có giới hạn và kiểm chứng nó, đồng thời phải giữ trạng thái chưa hoàn tất trong so sánh cuối.
