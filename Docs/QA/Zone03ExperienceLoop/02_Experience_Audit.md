# Zone03 — đánh giá trải nghiệm baseline

Baseline đã tới Zone04 thật trước tài liệu này. 158 file presentation/audio/effect gốc khớp SHA256 chuẩn bị; chưa có cải tiến production. [Báo cáo và timeline](01_Baseline_Playtest.md), [giới hạn ghi hình](baseline/recording-continuity.md), [phân tích mã](source-audit.md) là các nguồn đối chiếu. Không có phản hồi cảm xúc hoặc nghe thử bằng tai người.

## Góc nhìn người chơi

Zone03 có một vòng hành động rõ: tìm dấu sinh vật → chỉnh độ sâu và hướng → chụp → tự SEND → bắt mẫu cần thiết → chế tạo/lắp thiết bị → phá vật cản rồi tự lái qua. Điểm thứ hai chỉ cần ảnh, nên không hoàn toàn lặp lại hai điểm bắt mẫu. Phá đá là phần thưởng hữu hình cho công việc khảo sát: con đường thật mở ra và tàu phải đi qua đó.

Điểm yếu là sự chú ý thường dành cho vận hành hơn cho bí ẩn. Người chơi phải mở Photo Lab mới biết ảnh có đạt hay không; ảnh đầu cần chụp lại do hướng tiếp cận. Việc đóng Photo Lab về Desktop rồi trở lại cabin thêm chuyển ngữ cảnh. Cung đường L2→L3 dài, vòng qua nhiều khối địa hình; route có năm lần Rest, từ day1 tới day6. Năng lượng/ngày tạo quyết định thực, nhưng Rest chia đoạn một câu chuyện âm thanh vốn cần tính liên tục. Không có bằng chứng để tự giảm chi phí hay dời mục tiêu; đây là ràng buộc cần presentation thích nghi.

Điểm mạnh nhất của horror là dùng chính thiết bị đang làm nhiệm vụ. Radar có vòng sáng lạ trong khi dữ liệu và charge vẫn thật; bóng mờ xuất hiện trên kính nhỏ của Helm, không chiếm toàn màn hình. Tranh màu nước và bề mặt thiết bị thống nhất. Tuy vậy, bóng xuất hiện rất sớm trên đường L2→L3; sau đó một phần dài vẫn là lái tàu/Rest trước mẫu sinh học cuối và phá đá. Cấu trúc hiện tại có đủ cảnh, nhưng phân bố chưa hỗ trợ tốt đoạn cuối chuyến đi.

## Thời gian và giới hạn suy luận

Ready Zone03 2026-10-25T14:52:59.2226315Z → settled Zone04 16:56:13.0716568Z: **7393.849025 giây**. Đây là thời gian thật của phiên, không phải ước lượng thời lượng người chơi. Ba đoạn recorder có tổng7059.687s; các khoảng thiếu đã biết421.895967s và2.208293s. Đoạn đầu chạm cap45phút; một va chạm làm mất18Hull nằm trong khoảng thiếu, không thể dựng lại chính xác input/audio/video của va chạm.

Trong phần Zone03 được ghi: hoạt động navigation387.526325s, chuyển động ngang276.991214s, chỉnh sâu93.335651s, capture UI60.937557s, Photo Lab670.433516s, Rest UI11.704153s và Upgrade39.265333s. Các nhóm có thể chồng nhau, không cộng thành một tổng mới. Operator gaps có cặp2106.707334s; unfocused1260.078982s có chồng lấn. Nhiều khoảng còn lại không phân loại được. Chờ công cụ/agent chi phối wall time; tuyệt đối không xem đó là sợ hãi, do dự của người hay pacing thuần của game. Tuyến có trợ giúp từ contour của bản đồ gốc, không chứng minh khả năng tự tìm đường của người mới.

## Từng nhịp được gặp

Thời gian dưới đây là giây trong `baseline/continuation-02`, không phải từ đầu toàn chuyến.

| Nhịp | Bằng chứng thực | Đánh giá |
|---|---|---|
| Đá, nước và máy tàu | Listener PCM thật; stone one-shots tăng tới197 trước rời vùng. Count lớn chịu ảnh hưởng nặng bởi chờ agent. | Có nền vật liệu đặc trưng. Chu kỳ19/26/33s cố định từ source; không dùng số lần trong phiên dài để tuyên bố người chơi thấy lặp khó chịu. |
| Mẫu05 / capture | L1 PHOTO→SEND→CAPTURE thật; receipt capture05 xuất hiện4.099543s của đoạn này. | Công việc khảo sát gắn phản ứng vật liệu với mẫu. Không có capture07 receipt là đúng logic hiện tại, không tự kết luận mất hiệu ứng. |
| Động cơ trễ | Engine receipt962.974478s sau di chuyển/dừng thật. | Quan hệ nhân quả với thao tác tàu phù hợp: tiếng máy của chính tàu có vẻ tiếp diễn sai nhịp. PCM/voice là cơ hội nghe, chưa chứng minh nhận ra hay định hướng bằng tai. |
| Ảnh mẫu06 | [Khung1138.993s](baseline/continuation-02/frames/frame-005597.jpg), đúng Photo Lab. | Chuyển động/ảnh phản chiếu nằm trong tác vụ thật. Người vận hành giữ app lâu; không chứng minh cửa sổ ngắn của người khác đủ thấy. |
| Radar | Receipt1194.416784s; [khung1195.835s](baseline/continuation-02/frames/frame-005867.jpg) có nhiều vòng sáng giả trên bề mặt Radar sau scan thật. | Có thay đổi thấy được, không ghi đè dữ liệu thật. Cần thêm một scan sau SEND; route này chủ động thực hiện scan để quan sát, không suy ra mọi người sẽ tự làm như vậy. |
| Phản chiếu Helm | Receipt1209.267931s; [khung1213.101s](baseline/continuation-02/frames/frame-005948.jpg) có bóng mờ rõ trên kính nhỏ, đang lái18units/s ở478.7m. | Bóng được render thực, không chỉ tồn tại trong receipt. Nhưng chỉ khoảng15s sau radar và còn khoảng392s tới Photo Lab mẫu07; nên nhịp lớn bị dồn ở đầu cung đường. Thời gian392s có chờ ngoài game, không hoàn toàn là travel. |
| Mẫu07 | [Khung1605.797s](baseline/continuation-02/frames/frame-007830.jpg), ảnh GoodPhoto và MISSION DATA DETECTED; telemetry ghi BioelectricPulse, frame có màu xanh/cyan. SEND và capture thật thành công. | Một ảnh xanh đơn lẻ chưa chứng minh được phần overlay pulse riêng biệt; cần đối chiếu chuyển động và người chơi xác nhận độ nhận biết. Không đủ cơ sở thay toàn bộ art/audio. |
| Craft/install | Hai action thật hoàn tất; vật liệu do capture thật. | Mục tiêu rõ từ hướng dẫn rock. Wording chung nói tiêu hao lúc install dù Zone03 tiêu hao ở craft là mơ hồ cần sửa đúng theo chi phí hiện có. |
| Phá đá và thoát | Rock thật tại1480,150/590±10m; sequence10s rồi đi lối mở tới exit. Settled Zone04 như trên. | Cao trào có agency và kết quả vật lý. Giữ recovery/âm đuôi và đoạn thoát yên; không thêm scare ở cửa ra. |

## Vấn đề đã xác định và thứ tự ưu tiên

1. **P1 — receipt sớm hơn nội dung.** Mã `Once` lưu receipt ngay khi bắt đầu sequence. Ví dụ reflection receipt ở1209.268s, bóng được thấy sau đó khoảng3.8s. Baseline này giữ audience và gặp đủ peak; không có bằng chứng peak bị mất trong chính lượt này. Tuy nhiên điều kiện cancellation và save tạo đường lỗi cụ thể: đổi panel/modal/pause trước peak có thể ngắt nội dung rồi Continue coi là đã xem. Cần functional reproduction có chủ đích sau baseline, không viết lại lịch sử lượt chơi.
2. **P1 — cao trào kính xuất hiện sớm.** Baseline thực cho thấy nó diễn ra ở478.7m, ngay sau Radar, trước phần lớn cung đường và descent tới mẫu07. Đưa cơ hội này về đoạn sâu hơn, khi Helm không đang cần chỉnh lái/chỉnh sâu, sẽ nối được hậu quả của chuyến thám hiểm. Đây là giả thuyết thiết kế, phải đối chiếu bằng final.
3. **P1 — cơ hội Radar mong manh.** Scan thật chỉ có cửa sổ ngắn; hiện quiet gate và audience cùng phải trùng cửa sổ. Cần ghi nhớ một scan hợp lệ rồi giao nội dung trên đúng màn hình, thay vì bắt người chơi mất charge lần nữa khi panel bị đóng. Opportunity phải khác delivered receipt và không tự tạo contact/scan.
4. **P2 — quietTime chưa là recovery.** Nó cộng ngay khi unobstructed kể cả trong sequence. Cần khoảng nghỉ tính từ khi nội dung chấm dứt; không dùng chờ agent như tín hiệu cảm xúc.
5. **P2 — thông tin craft/install.** Đổi wording theo action đang hiển thị, giữ nguyên yêu cầu và tiêu hao. Không thêm luồng UI hoặc chi phí.

## Giả thuyết đường căng thẳng và thang điểm

Đường baseline có chú thích (đóng góp căng thẳng dự kiến, không đo cảm xúc): entry/ruins **1** → mẫu05/động cơ trễ **2** → ảnh06/Radar **3** → bóng Helm sớm **4** → hành trình/Rest/descent **2** → mẫu07 **3** → phá đá do người chơi chủ động **4** → recovery/Zone04 **1**. Khoảng trống/operator delay không được tô thành suspense. Final dùng cùng thang và dẫn chứng.

Thang chung1–5:1 thiếu/hỏng;2 rời rạc hoặc cơ hội yếu;3 nhất quán và có thể nhận thấy nhưng còn hạn chế;4 đặt theo ngữ cảnh với recovery/persistence được kiểm chứng;5 hiệu quả ổn định qua playtest đại diện bằng người. Mức5 không thể kết luận ở đây.

| Chiều | Tiêu chí riêng | Baseline | Cơ sở |
|---|---|---:|---|
| Không khí | Nền vật liệu và art xuyên hành trình |3|PCM có lớp, art đồng nhất; chất âm còn cần nghe bằng người.|
| Chờ đợi | Chuẩn bị cho bất thường từ hành động thật |3|Động cơ sau dừng và scan thật; quiet chưa đo recovery.|
| Bất định | Gợi ý liên hệ nhưng không phá dữ liệu |3|Vòng sáng/bóng kính và phản ứng mẫu; cùng chuỗi thiết bị có lý.|
| Nhịp | Phân bố theo tiến trình, tránh dồn sớm/cửa sổ mất nội dung |2|Reflection ngay sau Radar ở478.7m; start receipt và scan gate mong manh.|
| Tác động sự kiện | Thay đổi scoped thấy được, có recovery |3|Radar và kính được xem frame thật; độ nhận biết pulse mẫu07 chưa xác định.|
| Agency | Tự điều khiển và đổi việc mà không mất nội dung một lần |3|Mission/chi phí/lối mở thật, nhưng đổi audience trước peak có rủi ro tiêu receipt.|

Audit đã đóng. Không coi các điểm số là đo nỗi sợ, không hứa cải tiến tốc độ hoàn thành từ wall time rất nhiễu. Một redesign hẹp có thể bắt đầu từ các vấn đề trên.
