# Zone03 — kết quả một vòng cải tiến

Đã hoàn tất **baseline → audit → redesign → implementation → final → comparison**, cả hai lượt đều dùng input bình thường tới **Zone04 thật**. Kết quả được chứng minh rõ nhất là vị trí của nhịp phản chiếu và khả năng giữ cơ hội qua đổi tác vụ. Chưa có phản hồi từ người chơi thật để kết luận game gây sợ hơn.

## Góc nhìn người chơi

Zone03 có đích đến đáng công: khảo sát sinh vật cung cấp vật liệu, tự chế tạo/lắp Rock Breaker rồi mở một con đường có thật. Điểm thứ hai chỉ yêu cầu ảnh giúp vòng nhiệm vụ bớt lặp. Radar và bóng trên kính dùng chính thiết bị người chơi đang vận hành, hợp với bí ẩn về tín hiệu/quan sát của vùng khoáng vật.

Nhược điểm còn lại là việc lái tàu, đọc địa hình và Rest chiếm nhiều sự chú ý. Cả hai tuyến đều cần 5 lần Rest; lượt cuối vẫn có một va chạm đầu chuyến. Ở góc nhìn thiết kế đặt vào người chơi, chuyến này vẫn thiên về thám hiểm và quản lý tàu; chưa đủ bằng chứng về cảm giác bất an liên tục. Bản sửa giúp nhịp lớn có ngữ cảnh muộn hơn và cho phép đổi việc mà không mất cơ hội chưa giao. Khoảng cách giữa các nhịp chính vẫn cần người chơi thật đánh giá.

## Những gì đã thay đổi

- Engine/Radar/Reflection ghi delivered receipt sau checkpoint voice/preset thực, thay cho ghi ngay lúc yêu cầu sequence. Mất audience/focus/modal/pause trước checkpoint giữ khả năng thử lại; quiet bắt đầu sau drama.
- Scan thật sau SEND L2 lưu `radar.opportunity` với distance gốc. Retry/Continue không tự Scan hoặc thu thêm charge. Đạt passage và 600 m lưu `reflection.opportunity`; cơ hội này còn khi người chơi lên 590 m để phá đá.
- Reflection bắt đầu trên Helm khi không đang chỉnh lái/chỉnh sâu, vẫn cho điều khiển sau khi bắt đầu. CRAFT/INSTALL nói đúng thời điểm tiêu vật liệu.
- Tái sử dụng 18 WAV, 30 definitions/6 sequences và native procedural surfaces hiện có. Không thêm audio/art/shader/package hoặc generation mới; lý do và provenance/license cũ nằm trong [redesign](03_Horror_Redesign.md). Vấn đề được xác nhận thuộc điều phối, không cần tăng số scare.

## So sánh số liệu

Đồng hồ chính: first initialized, nontransitioning Zone03 → first initialized, nontransitioning Zone04, lấy từ UTC raw. Baseline 14:52:59.2226315Z→16:56:13.0716568Z ngày 25/10; final 07:18:58.6368293Z→07:58:14.6443951Z ngày 26/10. Không dùng thời gian Test Runner hoặc MP4 làm completion clock.

| Chỉ số | Baseline | Final | Diễn giải |
|---|---:|---:|---|
| Completion thật |7393.849s ·2h03m13.849s|2356.008s ·39m16.008s|Chờ công cụ/agent, focus và học tuyến khác nhau; không quy mức giảm này cho horror patch.|
| Navigation trong phần được ghi |387.526s|395.527s|Final không giảm hoạt động lái; baseline thiếu một đoạn recording.|
| Di chuyển ngang được ghi |276.991s|284.242s|Final có detour đầu chuyến và recovery công cụ.|
| Chỉnh sâu được ghi |93.336s|93.588s|Độ sâu/chi phí giữ nguyên.|
| Capture UI |60.938s|61.436s|Hai lần bắt thành công mỗi lượt, yêu cầu không đổi.|
| Photo Lab hiển thị |670.434s|96.781s|Thời gian đọc/chờ agent và kinh nghiệm UI gây nhiễu mạnh.|
| Rest app hiển thị |11.704s|9.735s|Chỉ thời gian app, không phải toàn bộ fade/recovery; cả hai có 5 Rest, day 1→6.|
| Upgrade UI |39.265s|6.780s|Wording đã đúng; không thể tách tác động wording khỏi thao tác đã học.|
| Ảnh / capture thành công |4 /2|3 /2|Baseline chụp lại L1; không đổi camera hoặc minigame để làm dễ.|
| Primary engine/radar/Helm gặp được |3/3|3/3|Số nhịp gặp không tăng; hai opportunity marker mới không tính là scare.|
| Khoảng giữa primary exposure dài nhất, end→next onset, không tính hai biên |226.075s|839.055s|Final dời bóng tới cuối chuyến nên khoảng này dài hơn; bên trong có nhiệm vụ, ambience, Rest và processing. Không phải literal silence.|
| Khoảng onset→onset lớn nhất gồm ready/arrival |6520.049s|841.743s|Baseline phần trước engine bị ngoại cảnh chi phối; không phải đo suspense.|
| Idle/confusion của người |Chưa đo|Chưa đo|Không có người chơi để phân loại cảm xúc/hành vi.|
| No-input chưa phân loại được ghi |4398.802s|1906.225s|Baseline còn 2106.707 s operator gaps có cặp; 1260.079 s unfocused chồng lấn. Không cộng các nhóm hoặc gọi tất cả là sợ/confusion.|
| Khoảng thiếu giữa recording |424.104s qua 2 gap|0|Baseline có cap 45 phút và rotate; final một đoạn liên tục. Không tạo PCM/hình thiếu.|
| Va chạm đã biết |1,−18Hull trong gap|1,−18Hull có recording|Normal Rest hồi Hull. Final còn driver obstruction latch, đã né bằng phím thường; không sửa mã giữa lượt.|
| Blocking defect trong game được gặp |0|0|Cả hai đi qua physical exit. Giới hạn driver/recorder được giữ riêng.|

Các duration hoạt động là nonexclusive, lấy từ sampled state; không cộng thành tổng completion. Cách đo khoảng primary dùng engine voice-on, Radar preset frame đầu và Helm preset frame đầu, tách PhotoLab reflection. Native checkpoint mới và receipt-launch cũ có ý nghĩa khác nhau; so sánh hình/voice thực là chính.

## Hai timeline trải nghiệm có chú thích

Thời gian tính từ ready Zone03 của từng lượt. Tension 1–4 dưới đây là **giả thuyết đóng góp thiết kế**, không đo cảm xúc người hoặc cảm giác của AI. Processing/unrecorded gap không được tô thành suspense.

### Baseline

| Mốc flow | Sự kiện / cơ hội phơi lộ | Tension giả thuyết và chú thích |
|---|---|---|
|0s|Ruins, máy và nước|1 — thiết lập hành vi bình thường.|
|5526.253–5561.174s|SEND L1 → capture|2 — vật liệu gắn với khảo sát; wall time trước đó chủ yếu là hạn chế vận hành.|
|6520.049s|Engine voice-on sau di chuyển/dừng|2 — phản hồi âm máy chậm, chưa xác nhận người nghe nhận ra.|
|6751.534s|Radar frame thật sau scan|3 — vòng sáng lạ cùng dữ liệu địa hình thật.|
|6769.169s onset; peak 6770.175 s|Bóng Helm ở 478.696 m, 31.084% distance trước exit|4 — peak rất gần Radar, còn nhiều hành trình phía sau.|
|7169.195–7207.376s|SEND/capture mẫu07|3 — pulse/photo và thao tác sinh học; nhận biết pulse chưa xác định bằng một ảnh xanh.|
|7249.125–7262.195s|Craft/install|2 — chuẩn bị hành động rõ mục đích.|
|7355.608s→7393.849s|Commit phá đá, sequence rồi physical Zone04|4→1 — payoff do người chơi thực hiện, phục hồi và thoát.|

### Final

| Mốc flow | Sự kiện / cơ hội phơi lộ | Tension giả thuyết và chú thích |
|---|---|---|
|0s|Cùng initial state đã đối chiếu|1 — ship/upgrades/resources/missions/receipts khớp baseline.|
|633.794–661.199s|SEND L1 → capture|2 — giữ va chạm và recovery thật trong dữ liệu.|
|774.306s|Engine voice-on sau Helm stop|2 — checkpoint thực; panel brake không tiêu cơ hội.|
|1344.970s SEND;1354.199s Radar frame|Một scan sau SEND, opportunity rồi delivered|3 — không thu thêm charge để retry.|
|2045.653–2086.844s|SEND/capture mẫu07, reflection đã earned nhưng chưa delivered|3 — normal workflow ngắt phần chuẩn bị; cơ hội còn lại.|
|2178.084–2179.160s|Craft/install|2 — vật liệu tiêu tại craft, install không thu thêm.|
|2195.942s onset; peak 2196.902 s|Helm 646.313 m, 89.499% distance, sau craft/install và Rest|4 — nhịp bất định nối vào chặng tiếp cận rock; điều khiển vẫn đọc và dùng được.|
|2327.776s→2356.008s|Commit phá đá, 10 s event rồi physical Zone04|4→1 — đường thật mở, không thêm exit scare.|

## Đối chiếu khoảnh khắc cụ thể

[Baseline peak thật](D:/G10/Docs/QA/Zone03ExperienceLoop/baseline/continuation-02/frames/frame-005948.jpg): x 201.522, y 548.332, 478.696 m, cumulative distance 1421.868/4574.283 = **31.084%**, sau L2 và trước L3. [Final peak thật](D:/G10/Docs/QA/Zone03ExperienceLoop/final/evidence/frames/frame-010897.jpg): x 1275.284, y 281.595, 646.313 m, 4187.583/4678.921 = **89.499%**, sau L3/craft/install, trước demolition. Mẫu số cùng định nghĩa: last valid Zone03, transitioning=false trước exit. Không lấy stale distance từ Zone04 transition.

Đã xem cả hai ảnh: cùng bóng người mờ trên màn nhỏ, controls đọc được. Thay đổi rõ nằm ở vị trí trong hành trình. Final có hai episode chuẩn bị bị ngắt/re-arm qua normal UI; một interval preset chỉ 7.632 ms trong sample, không có captured frame hay voice/receipt, nên không tính là scare đã gặp. Episode cuối có frame, voice và checkpoint. Đây là bằng chứng thực về cơ hội sống qua việc chuyển tác vụ, phù hợp các controlled interruption tests.

Khoảng primary end→next tăng lên 839 s trong final là hạn chế cần giữ trong kết luận. Nhiệm vụ/ambience vẫn diễn ra, nhưng một lượt AI có nhiều processing không chứng minh người chơi được duy trì căng thẳng xuyên đoạn này. Người chủ động dive sớm tới 600 m cũng có thể kiếm cơ hội reflection sớm hơn tuyến final này; depth gate không bảo đảm cùng tỷ lệ 89.5% cho mọi phong cách chơi.

## Điểm thiết kế và kết luận

Dùng cùng rubric tại [02 audit](02_Experience_Audit.md): 1 thiếu/hỏng; 2 rời rạc/cơ hội yếu; 3 nhất quán và nhận thấy được nhưng còn hạn chế; 4 đặt theo ngữ cảnh với recovery/persistence đã kiểm chứng; 5 cần kết quả ổn định từ playtest người đại diện. Không kết luận mức 5.

| Chiều | Baseline | Final | Cơ sở |
|---|---:|---:|---|
|Không khí|3|3|Cùng art và audio; chưa có human listening.|
|Chờ đợi|3|3|Engine/scan thật, quiet có recovery; độ dài passage chưa được người xác nhận.|
|Bất định|3|3|Giữ chuỗi dụng cụ/ảnh/kính; chưa đo cách người diễn giải.|
|Nhịp|2|3|Peak dịch về chặng sâu/late journey, vẫn còn khoảng dài giữa nhịp.|
|Tác động sự kiện|3|3|Radar/kính thấy được ở cả hai, số exposure chính bằng nhau.|
|Agency|3|4|Normal UI/capture/Rest không xóa opportunity; native Continue/Journal/failure/cleanup qua.|

**Verdict:** Zone03 có vòng mục tiêu và payoff tốt. Bản sửa cải thiện độ bền của điều phối và đặt phản chiếu vào ngữ cảnh muộn hơn ở lượt đã chơi. Lái tàu/Rest vẫn là phần nặng của flow. Chưa chứng minh người chơi sợ hơn, tự nhiên recheck thiết bị hơn, hoặc thời gian hoàn thành của người giảm. Cần người mới chơi không đọc spec để đánh giá nhận biết âm/bóng, masking/định hướng âm và liệu passage dài tạo chờ đợi hay mệt mỏi.

## Kiểm chứng, dữ liệu và bàn giao

- [00 chuẩn bị](00_Preparation.md), [01 baseline](01_Baseline_Playtest.md)/[JSON](01_Baseline_Timeline.json), [02 audit](02_Experience_Audit.md), [03 redesign](03_Horror_Redesign.md), [04 implementation/native](04_Implementation.md), [05 final](05_Final_Playtest.md)/[JSON](05_Final_Timeline.json).
- [Video final thực](D:/G10/Docs/QA/Zone03ExperienceLoop/final/evidence/gameplay.mp4), [mux/ffprobe](final/evidence/mux-result.json), raw JPEG/JSONL/WAV retained locally. Three baseline videos linked in 01. Final 11,717 JPEG/122 skipped slots, 0 ring audio drops; loading listener gap ≈ 0.346 s. WAV 18 negative rail values/13 PCM frames, RMS −28.228 dBFS; baseline 2 rail values/1 frame, RMS −31.978 dBFS. Different action/idle proportions; không suy méo nghe được hoặc hiệu quả cảm xúc từ các số này. No generated PCM/interpolation; callback/DSP/device/AAC sync limitations trong 05.
- Final recorder dừng 2408.086 s, khoảng 8 s sau rotation buffer 2400, dưới hard cap 2700; settled Zone04 đã xảy ra trước 2400. Không có gap giữa segment final.
- **16 distinct native cases passed, 0 failed/skip**: 15 trước final, 1 Rest/Journal bổ sung sau final. [15 case results](native-validation-results.json), [Rest/Journal XML](native-validation/RestJournal-TestResults.xml). Test bổ sung chỉ đổi fixture; không đổi production hoặc mở vòng redesign thứ hai. Target-hardware profiling/standalone build và human fear chưa kiểm chứng.
- [Initial equivalence](initial-equivalence.json); [162 frozen production hashes](final/frozen-source-check.json); [152 persistent-entry và 13 protected-entry SHA matches](preservation-final.json). Game saves/photos giữ nguyên. Unity-pruned analytics và native TestResults.xml đã phục hồi từ verified byte backups; XML mới giữ trong report. Fonts/user edits, maps/missions/scenes/TimeManager và 4 deletions có từ entry được giữ.
- Branch `codex/zone03-experience-loop`, chưa commit/merge/push. Editor stopped tại Bootstrap clean, save/photo overrides null. Kết thúc đúng một improvement cycle; không tự tiếp vòng thứ hai.
