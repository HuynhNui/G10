# Zone 02 — đánh giá sau một vòng cải thiện

**Từ góc nhìn thiết kế cho người chơi: Zone 02 đã có mạch nghi ngờ rõ hơn, nhưng phần thu thập nhiệm vụ vẫn lặp và còn những khoảng dài thiếu diễn biến chính.** Điểm tiến bộ thuyết phục nhất là dấu hiệu bên ngoài gắn với một lần tàu thực sự dừng, rồi nối sang radar, tiếng gõ, tiếng cào và bóng phản chiếu. Khả năng người chơi được thấy/nghe cảnh đã được cải thiện; cảm giác sợ chưa được kiểm chứng bằng người chơi thật.

Đã thực hiện đúng một vòng baseline → audit → redesign → implementation → final voyage → comparison. Cả hai hành trình đều tới Zone 03 bằng di chuyển thật. Baseline bị gián đoạn bởi lỗi cổng quay về Zone 01, được sửa riêng rồi tiếp tục cùng save; không sửa horror trong baseline. Không bắt đầu vòng thiết kế thứ hai sau lượt cuối. **Yêu cầu bảo toàn toàn bộ save ban đầu chưa đạt:** save thường đã phục hồi đúng byte, nhưng backup thường cũ và hai file debug gốc chưa phục hồi được. Chi tiết ở cuối báo cáo.

## Cảm nhận thiết kế của một người đang chơi

Vòng khám phá → chụp → gửi dữ liệu → bắt mẫu → nâng cấp tạo mục tiêu cụ thể. Khi thành công, vật liệu có công dụng rõ: Pressure Hull mở giới hạn 500→750 m. Việc phải tự lái qua địa hình giữ được cảm giác mình đang vận hành tàu. Tuy nhiên, lần gặp sinh vật thứ hai vẫn lặp nhiều thao tác chuyển màn hình và dùng hình ảnh sinh vật/thực vật khá giống lần đầu. Cảm giác “phát hiện mới” chưa mạnh tương xứng với quãng đường.

Ở bản gốc, các cảnh radar/gõ/cào/phản chiếu có chung chất liệu nhưng dễ giống những tiết mục rời nhau. Bản mới thêm một quan hệ có thể hiểu bằng hành động: tàu chạy, tàu dừng, bên ngoài vẫn có phản ứng muộn. Điều đó cung cấp lý do để diễn giải những tiếng động tiếp theo là cùng một sự hiện diện. Lần kiểm tra lại radar của người điều khiển trong thử nghiệm này được lên kế hoạch để quan sát; nó không chứng minh người chơi tự kiểm tra vì sợ.

Hai lần Rest vẫn cắt hành trình. Cảnh nhiễu lại xảy ra trên Rest: nút và hướng dẫn lớn nhận diện được, chữ nhỏ có hiện tượng nhân đôi. Bóng phản chiếu có cửa sổ xuất hiện tốt hơn nhưng vẫn nhỏ, mờ và cạnh tranh với việc đọc thông số. Chưa có dữ liệu người thật về mức dễ nhận ra, thoải mái khi nhìn hoặc chất lượng nghe tai nghe.

## Điều kiện và cách đo

[Hai snapshot khởi đầu giống hệt byte](final/initial-state-comparison.json): cùng preset DEBUG Zone02, regular upgrades NONE, Zone01 hoàn tất, Zone02 chưa làm, chưa có receipt, vị trí (45,595), sâu230m, Energy/Hull100, Radar10, Photo20, Capture5. Cả hai dùng tốc độ bình thường, UI thật và InputSystem; không teleport, bơm tài nguyên, hoàn thành objective trực tiếp hoặc ép phát cảnh.

Có hỗ trợ chọn waypoint từ bản đồ; tàu vẫn chạy bằng WASD, chịu địa hình và tiêu hao thật. Baseline có hỗ trợ bắt mẫu bằng hình hook/fish đang hiển thị; lượt cuối chỉ lần bắt thứ hai có tracking-agent được ghi nhận. Lần đầu lượt cuối hoàn thành bằng chuyển động hook bình thường khi không có lệnh lái. Một status lấy quá sớm khiến người điều khiển tưởng capture chưa mở và thử bấm Helm; modal chặn thao tác, tàu không di chuyển. Đây là lỗi diễn giải/độ trễ của người điều khiển, không phải bằng chứng lỗi game.

Baseline chưa ghi cờ transition nên mốc sẵn sàng được suy từ trạng thái và hình ảnh; lượt cuối ghi cả trạng thái tài nguyên lẫn transition. Lượt cuối: resource-ready43.420→995.819s =952.399s; flow-ready43.622→997.228s =953.606s. Mốc quan sát1005.365s và annotation1022.528s đều muộn, không dùng làm thời gian hoàn thành. Các khoảng review của agent diễn ra khi game tiếp tục; thời gian không có lệnh không được gọi là sợ hoặc bối rối.

## Đối chiếu số liệu

| Chỉ số | Baseline | Sau cải thiện | Diễn giải |
|---|---:|---:|---|
| Sẵn sàng Zone02 → Zone03 ổn định |1771.125s —29m31.125s, cộng hai đoạn |953.606s —15m53.606s, theo flow-ready |Giảm khoảng13m38s; có ảnh hưởng lớn của lỗi cổng đã sửa, biết đường và xử lý của agent. Không quy cho horror. |
| Toàn bộ recording |1799.928s |1022.537s |Có setup và quan sát sau đích; khác với completion. |
| Di chuyển/xoay/đổi độ sâu thực tế |220.357s |212.496s |Giảm7.861s; quãng nhiệm vụ cốt lõi vẫn dài gần như cũ. Các trục chồng nhau. |
| Chuyển động ngang / đổi độ sâu |158.640 /45.829s |155.951 /45.919s |Không rút map hoặc tăng tốc tàu để làm đẹp số. |
| Capture UI thực sự mở |41.081s |64.849s |Lượt cuối lâu hơn; pha chuyển động và thời gian quan sát khác nhau, không suy ra độ khó với người. |
| Computer / Photo Lab mở |216.572 /16.687s |116.427 /44.500s |Có review trong các khoảng này; lượt cuối chủ động quan sát mép ảnh. |
| Rest |2 lần |2 lần, Rest UI48.719s |Giữ nguyên kinh tế tài nguyên và đổi ngày. |
| Idle/bối rối của người chơi |Không xác định |Không xác định |Không có người chơi thật hoặc phân loại quan sát đầy đủ. |
| Khoảng xử lý agent đánh dấu được |321.253s, chỉ một phần continuation |540.292s toàn recording;523.129s trong Zone02 |Khả năng ghi marker khác nhau; không dùng như hai tổng overhead tương đương. |
| Nhịp chính được gặp |Radar, gõ, cào, phản chiếu |Cùng bốn nhịp + Following sau dừng |Có hình thật cho ba cảnh hình; âm thanh có source và PCM thật, chưa có đánh giá nghe. |
| Khoảng dài nhất giữa request nhịp chính |517.456s, cào→phản chiếu |322.577s, cào→phản chiếu |Có nhiệm vụ, ambience và xử lý agent bên trong; không phải thời gian im lặng/trống tuyệt đối. Thêm Following không làm ngắn giả khoảng chung này. |
| Lỗi chặn hành trình quan sát được |1 lỗi quay nhầm Zone01; sửa riêng trước continuation |0 lỗi chặn trong lượt cuối |Hull82 sau một va chạm baseline; lượt cuối Hull100. Khác đường lái không chứng minh horror tốt hơn. |

Các số có định nghĩa đầy đủ ở [baseline](01_Baseline_Playtest.md), [hiệu chỉnh](baseline-metrics-addendum.md), [final](05_Final_Playtest.md) và hai timeline JSON. Không cộng các loại thời gian chồng nhau thành tổng gameplay.

## Hai timeline trải nghiệm có chú thích

Mức căng1–5 dưới đây là **giả thuyết thiết kế**, không phải cảm xúc đo được. Baseline S1/S2 là đồng hồ từng recording; khoảng sửa lỗi giữa hai recording bị loại khỏi tổng.

| Baseline | Hành động / bằng chứng | Giả thuyết căng |
|---|---|---:|
|S1 5.163s |Khởi đầu sẵn sàng; đọc map, radar và độ sâu |1 |
|S1 326.837 /393.662s |SEND / bắt mẫu thứ nhất |2 |
|S1 459.067s |Đi tới nhưng bị quay về Zone01; đây là lỗi flow |Không chấm thành horror |
|S2 47.205s |Cùng save quay lại Zone02 sau sửa lỗi cổng |1 |
|S2 146.831 /185.415s |Radar nhiễu / gõ khi lái; [ảnh radar](baseline/continuation/frames/frame-000713.jpg) |2→3 |
|S2 303.175s |Cào/nhiễu trên Rest; [ảnh](baseline/continuation/frames/frame-001484.jpg) |4 |
|S2 652.478–778.215s |SEND/bắt mẫu2, lắp Hull; kéo dài mạch công việc |2 |
|S2 820.630s |Phản chiếu; chỉ một khung5Hz thấy bóng, [ảnh](baseline/continuation/frames/frame-003992.jpg) |3 |
|S2 830.891–1266.356s |Va chạm, lùi/đi vòng, tới Zone03; hồi phục bị kéo dài bởi thao tác |1–2 |

| Lượt cuối — giây từ đầu recording | Hành động / bằng chứng | Giả thuyết căng |
|---|---|---:|
|43.622s |Sẵn sàng cùng trạng thái ban đầu |1 |
|201–228s |Photo Lab1: mép ảnh thay đổi, vùng giữa giữ nguyên; [ảnh](final/evidence/frames/frame-000972.jpg) |1–2 |
|263.602s |Bắt mẫu1 hoàn tất |2 |
|333.363→334.225s |Following bắt đầu→receipt sau một lần chạy và dừng thật |2–3 |
|361.124→361.944s |Quét thật rồi nhiễu→receipt; [ảnh](final/evidence/frames/frame-001739.jpg) |3 |
|394.425→395.431s |Gõ trong lúc tiếp tục lái |3 |
|505.395→509.223s |Cào/nhiễu trên Rest; [ảnh](final/evidence/frames/frame-002444.jpg) |4 |
|≈531–792.471s |Rest, lặn, tới mẫu2, SEND/CAPTURE, Hull; vẫn là đoạn nhiệm vụ dài |2 |
|827.973→829.085s |Phản chiếu trên đường ra; [ảnh](final/evidence/frames/frame-003984.jpg) |3 |
|991.634 /997.228s |Zone03 đổi quyền / hết chuyển cảnh, vị trí(11,97) |2 |

Các timestamp request và receipt có thể nằm ở sample khác nhau; xem [05_Final_Timeline.json](05_Final_Timeline.json) để lấy giá trị gốc. Mức căng không dùng số lần event làm phép đo cảm xúc.

## Những thay đổi nào có bằng chứng cải thiện?

- **Gắn cảnh với hành động:** Following xuất hiện sau propulsion→stop thật. Motif bên ngoài có mối liên hệ với hành vi vận hành tàu, thay vì chỉ tăng số scare.
- **Đảm bảo cơ hội nhìn thấy:** ảnh Radar có nhiễu trước receipt; bóng phản chiếu được thấy trong ba khung liên tiếp, thay cho một khung baseline. Ba sample828.787–829.287s trải0.500s; không suy ra duration liên tục chính xác từ5Hz. Nó vẫn mờ và nhỏ.
- **Chuyển động phù hợp tranh:** hai cặp ảnh Photo Lab thật có49,928pixel trung tâm giống hệt, mép đổi rõ ở phép so ảnh; không chỉ là cờ material đã gắn. Không thay PNG lưu hoặc bóp méo tọa độ/contacts. [Phân tích ảnh](final/visual-review-notes.md).
- **Hủy và khôi phục:** test native kiểm tra đóng sớm, pause, Continue/Journal, lỗi ghi save, cleanup, một material được tái dùng và không mất cảnh chưa được giao. Lượt cuối không cố hủy mọi cảnh để lấy bằng chứng; không thay thế test bằng lời đoán.
- **Kết thúc sạch:**122khung Zone03 ổn định trong24.848s không còn source đặc thù Zone02. Không dùng kết quả này để khẳng định không có đuôi reverb nghe được.

Tái dùng EffectDirector, EffectSequence, UIEffect và tranh cũ; thêm shader2D native cho mép ảnh, chuỗi Following và WAV3.4s dựng offline từ outside_movement/root_friction. Không thêm package hoặc mua/gọi dịch vụ trả phí. [Provenance âm thanh](D:/G10/Assets/_Project/Audio/Zone02/following_tail.provenance.json) giữ điều khoản nguồn ElevenLabs Free phi thương mại/attribution; không cấp quyền thương mại mới. [Chi tiết thay đổi và test](04_Implementation.md).

## Chấm thiết kế với cùng tiêu chí baseline

Thang chung:1 thiếu/hỏng;2 rời rạc/không ổn định;3 mạch lạc và nhận thấy được nhưng có giới hạn;4 gắn ngữ cảnh, duy trì và có kiểm tra khôi phục;5 cần bằng chứng nhất quán từ người chơi đại diện. Điểm là nhận định thứ bậc, không phải phép đo cảm xúc.

| Mặt đánh giá | Trước | Sau | Căn cứ và giới hạn |
|---|---:|---:|---|
|Không khí |3 |3 |Tranh nhất quán, có chuyển động mép ảnh; di chuyển thường vẫn chủ yếu là thiết bị/tranh. |
|Chờ đợi bất thường |2 |3 |Có chuẩn bị trước nhiễu và phản ứng muộn sau dừng; chưa đo người nghe nhận ra. |
|Bất định |3 |3 |Motif liên kết hơn; chưa có thay đổi kỳ vọng hay tìm nguồn âm từ người thật. |
|Nhịp độ |2 |3 |Cơ hội theo hành động và recovery tốt hơn; quãng nhiệm vụ/Rest vẫn ngắt mạch. |
|Tác động cảnh |3 |3 |Cửa sổ tiếp xúc tốt hơn; figure vẫn nhỏ, Rest còn nhiễu chữ. |
|Quyền chủ động |3 |4 |UI, dữ liệu, tài nguyên và cổng thật giữ quyền; cảnh chưa thấy có thể thử lại qua các test. |

Không tăng tất cả điểm để hợp thức hóa công việc. Nhận định độc lập và các giả thuyết còn thiếu ở [designer-verdict.md](final/designer-verdict.md).

## Xác thực kỹ thuật, sự cố và bàn giao

Zone02/return initial20/21 qua; ca reflection cũ được sửa để kiểm tra cả ba bước. EditMode103qua/1skip. Một lượt mở rộng bị TestRunner lỗi sau domain reload; chỉ giữ được70qua/2assertion cũ lỗi, không tuyên bố88/88. Lượt kiểm tra thu hẹp tiếp theo21/21 qua, gồm reflection, hai assertion đã khớp dữ liệu nhiệm vụ, quay về/migration và các ca còn thiếu. Đây là xác thực chức năng, không phải bằng chứng sợ. Lượt cuối không phát hiện lỗi chặn mới.

Sau cùng, tám file mission/map và GameplayCore giữ đúng hash entry; hai font và TimeManager trở lại đúng bản người dùng đang sửa lúc bắt đầu. Shared cabin chỉ thêm sáu dòng tham chiếu presentation/catalog. Unity dừng ở Bootstrap sạch; shader và Console không có lỗi tại kiểm tra cuối. Không commit, merge hoặc push; thay đổi nằm ở codex/zone02-experience-loop.

**Sự cố save cần được giữ rõ:** công cụ ghi hình đã có lỗ hổng thứ tự trả đường dẫn lúc thoát Play; native test cũng bị reload khi chạy. File debug bị ghi thay trong hai lần dừng baseline; save thường bị ghi khi test lỗi. Save thường chính đã phục hồi khớp SHA256 ban đầu. Backup thường cũ và hai file debug gốc chưa tìm lại được. Toàn bộ file tại thời điểm phát hiện đã được sao lưu riêng. Công cụ đã sửa trước lượt cuối; sáu file persistent giữ nguyên hash qua lượt cuối. Không tuyên bố đã bảo toàn đủ mọi bản gốc. [Báo cáo sự cố và phục hồi](save-preservation-incident.md).

## Kết luận và bằng chứng mở

Thiết kế đã giải quyết phần yếu về giao cảnh và quan hệ giữa các dấu hiệu; chưa chứng minh được nỗi sợ kéo dài. Quãng nhiệm vụ lặp, sự gián đoạn Rest, độ dễ thấy figure và trộn/định hướng âm thanh vẫn cần người chơi thật xác thực. Đặc biệt cần quan sát liệu người mới tự dừng, kiểm tra lại hoặc đổi cách đi vì diễn giải tình huống, đồng thời hỏi để phân biệt sợ với không hiểu UI. Đây là đề xuất kiểm chứng sau này, không phải bắt đầu vòng cải thiện thứ hai.

Bằng chứng chính: [video baseline1](baseline/evidence/gameplay.mp4), [baseline tiếp tục](baseline/continuation/gameplay.mp4), [video lượt cuối](final/evidence/gameplay.mp4), [PCM thật lượt cuối](final/evidence/actual-game-audio.wav), [raw timeline](final/evidence/timeline.jsonl), [event index/clock](final/evidence/event-index.json). Video giữ thời lượng từng frame thật, không nội suy; A/V sync là ước lượng có ghi khoảng đổi AudioListener, không tạo bù PCM. Kết luận cảm xúc vẫn mở vì không có phản hồi người chơi thật.
