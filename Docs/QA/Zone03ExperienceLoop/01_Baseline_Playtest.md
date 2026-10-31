# 01 — Baseline Zone03 → Zone04

**Baseline hoàn thành:** cùng một chuyến live đã làm đủ ba site, craft/install Rock Breaker, phá rock bằng tương tác thực và đi qua gate tới Zone04. Mốc Zone03 sẵn sàng đầu tiên UTC2026-10-25T14:52:59.2226315Z → Zone04 sẵn sàng UTC16:56:13.0716568Z là **7393.849025 giây — 2 giờ 03 phút 13,849 giây**. Đây là đồng hồ của lượt vận hành có nhiều chậm trễ bên ngoài, không phải benchmark thời lượng của người chơi thông thường.

Toàn bộ baseline dùng presentation gốc; không có horror redesign hoặc gameplay mutation trong chuyến này. Input đi qua Keyboard/Mouse InputSystem, UI thật, WASD và chi phí/va chạm bình thường. Có trợ lý đọc bản đồ gốc và capture tracking dựa trên hình học hook/fish công khai đang hiển thị. Không có đánh giá sợ hãi hay nghe chủ quan của con người.

## Đồng hồ, ba đoạn ghi và phần thiếu

| Đoạn | `recording.start` UTC25/10 | `recording.stop` UTC25/10 | Summary wall | Trạng thái lúc đóng |
| --- | --- | --- | ---: | --- |
| `baseline/evidence` | 14:52:10.1162801Z | 15:37:10.1018574Z | 2700.011s | Hết cap45 phút; chưa hoàn thành objective nào |
| `baseline/continuation` | 15:44:11.9978242Z | 16:25:34.0965884Z | 2482.109s | L1 PHOTO đã SEND; capture đang chạy,5hits |
| `baseline/continuation-02` | 16:25:36.3048814Z | 16:56:53.8601752Z | 1877.567s | Đã tới Zone04 và ổn định |

Ba recording có tổng wall **7059.687s**. Trong cửa sổ đồng hồ chính, các khoảng quan sát đã ghi được tích phân theo realtime là6969.744676s. Hai khoảng thiếu ảnh/PCM là **421.895967s** và **2.208293s**, tổng424.104260s. Không tạo PCM hoặc ảnh thay thế, không ghép ba video thành một bản liên tục không chú thích. Sai khác dưới1ms khi đối chiếu tổng realtime/UTC tới từ đồng hồ và các mốc lấy mẫu; không trộn chúng thành thời lượng chính xác hơn bằng chứng.

Đoạn45 phút bị chi phối bởi queue/focus/việc đọc và vận hành công cụ trước mission đầu. Chỉ có30.111843s hoạt động navigation được quan sát trong đoạn đó. Nó có1260.078982s `focused=false`,4operator gap có đủ cặp1791.467609s và1start không có end. Các loại này chồng lấp. Một lần giữ nút dive trong khi mất focus không đổi depth; lần thử sau khi lấy lại focus mới chuyển230→358.396210m. Không gọi45 phút này là thời gian chờ được thiết kế, sự bối rối hay nỗi sợ.

Khoảng thiếu thứ nhất giữ nguyên phiên Play nhưng hai đầu thay đổi vị trí(63.143867,133.535629)→(81.978271,138.053589), distance+19.570503,energy−1.653847,hull100→82,stone counter102→118. Thời điểm/đường đi/input/va chạm/exposure trong421.895967s này không được ghi. Lời kể vận hành về leg(160,145) và recovery được giữ riêng với bằng chứng video. Khoảng thiếu2.208293s xảy ra giữa lúc capture L1 vẫn active,5hits; timer23.679873→21.841551s, không chứng minh một capture mới hay hoàn thành trong gap. [Chi tiết continuity](/D:/G10/Docs/QA/Zone03ExperienceLoop/baseline/recording-continuity.md).

Mốc có tài nguyên Zone03 là14:52:58.8438908Z, còn `transitioning=true`. Dùng mốc này cho đồng hồ phụ sẽ ra7394.227766s. Đồng hồ chính bắt đầu ở hàng có `transitioning=false`14:52:59.2226315Z. Authority Zone04 xuất hiện sớm nhất ở **sample16:56:12.4750817Z**, frame1573639, `continuation-02/elapsedWall=1836.177859`; quan sát16:56:12.5016668Z là hàng muộn hơn. Sau handoff, hàng đã có tài nguyên/vị trí Zone04 và `transitioning=false` là16:56:13.0716568Z. Không dùng annotation muộn hoặc duration MP4 để thay các mốc này. Tên scene cabin `Zone01` không thay authority `zone`.

## Tiến trình đã xảy ra

Các mốc là quan sát đầu tiên, không phải thời điểm bên trong frame chưa được nhìn thấy. `PHOTO` hoàn thành sau manual SEND; chụp một ảnh đơn thuần không được tính thay cho SEND.

| Kết quả | UTC25/10 | Giây từ Zone03 sẵn sàng |
| --- | --- | ---: |
| L1 PHOTO/SEND | 16:25:05.4760989Z | 5526.253467 |
| L1 CAPTURE | 16:25:40.3967657Z | 5561.174134 |
| L2 PHOTO/SEND, không yêu cầu capture | 16:45:10.4509195Z | 6731.228288 |
| L3 PHOTO/SEND | 16:52:28.4181100Z | 7169.195479 |
| L3 CAPTURE | 16:53:06.5985894Z | 7207.375958 |
| CRAFT Rock Breaker | 16:53:48.3474100Z | 7249.124779 |
| INSTALL Rock Breaker | 16:54:01.4176713Z | 7262.195040 |
| DESTROY ROCK BARRIER, tương tác thật | 16:55:34.8302779Z | 7355.607646 |
| Zone04 ổn định | 16:56:13.0716568Z | 7393.849025 |

Có **5 lần Rest**, day1→2 lúc16:32:04.0448988Z,2→3 lúc16:45:14.7617738Z,3→4 lúc16:47:45.7908225Z,4→5 lúc16:49:59.7696756Z,5→6 lúc16:54:05.9484699Z. Đây là day transition thực qua app Rest, không phải refill bằng công cụ. Lượt ghi 4 ảnh và hoàn thành 2 capture; không thêm capture cho L2. Distance của chuyến Zone03 đạt4577.929199 trước reset distance bình thường khi khởi tạo Zone04. State ổn định Zone04: day6,(290,950),depth589.700195m,energy47.581348,hull100. Handoff có hàng `transitioning=true` với tài nguyên bằng0 trước khi Zone04 khởi tạo xong; không tính các giá trị chuyển scene này là va chạm, hết energy hay tử vong.

Rock được quan sát active khoảng9.711169s theo sampled state; `rockElapsed` đạt10.0. Debris surface có7.896926s báo visible. First/last active sample không có độ phân giải để suy ra lock thực chỉ dài9,7s; không coi chênh lấy mẫu là thay đổi hợp đồng reveal/lock. Gate chỉ đổi authority sau rockDestroyed và chuyển động tiếp tới exit.

## Hoạt động chơi và thời gian chưa phân loại

Các số dưới đây là tổng parser của những khoảng Zone03 có tài nguyên tới authority departure, lấy mẫu được giữ tới quan sát kế tiếp. Đây là các loại **không loại trừ nhau**, khác mẫu số với đồng hồ chính tới settled Zone04.

| Quan sát | Giây |
| --- | ---: |
| Navigation activity: đi/quay/đổi depth | 387.526325 |
| Horizontal motion | 276.991214 |
| Đổi depth | 93.335651 |
| Quay | 42.680424 |
| Owner chart-driver trong Zone03 | 277.190387 |
| Owner keyboard riêng | 8.568375 |
| Owner pointer, gồm cả yêu cầu bị bỏ qua khi mất focus | 124.245871 |
| Capture UI active | 60.937557 |
| Owner capture tracking agent | 54.341677 |
| Computer visible | 738.849540 |
| PhotoLab visible | 670.433516 |
| Rest app visible | 11.704153 |
| Upgrade app visible | 39.265333 |

Operator gap có đủ cặp tổng2106.707334s; mất focus1260.078982s chồng lấp phần lớn với chúng. Hai marker start không đủ end được giữ trong JSON, không tự đóng hoặc gán phần đuôi cho operator. Có6505.425884s không có owner input trong parser scope;4398.801875s trong số đó nằm ngoài các operator gap đã ghép cặp. Những khoảng chưa đánh dấu bao gồm chờ công cụ, quan sát, UI và thời gian gameplay chưa tách được. **Không suy ra idle=confusion/fear, không lấy tổng wall trừ vài nhãn để tạo “thời gian chơi thuần” của con người.**

JSON còn có một partition loại trừ nhau riêng trên khoảng realtime đã ghi trong cửa sổ chính: transition/loading9.154552s;unfocused1260.078978s;navigation387.526335s;capture60.933717s;computer737.343726s;station khác/chưa phân loại4514.707367s. Priority này phục vụ kiểm tra tổng; nhãn cuối không được gọi là im lặng, bế tắc hay chậm nhịp của game.

Sau giai đoạn đầu khó đi đường, pilot nhận các corridor rút từ line art gốc và điều khiển từng waypoint bằng WASD thật; [navigation-assistance.md](/D:/G10/Docs/QA/Zone03ExperienceLoop/baseline/navigation-assistance.md) giữ calibration, khoảng hở và giới hạn góc cua. Route knowledge, việc có trợ lý, quán tính/độ sâu/tài nguyên và các detour thực là confound khi so với final. Final cần khai báo cùng assistance/route; không so như hai người mới chơi độc lập.

## Receipt, surface và cơ hội nghe thực

Bốn durable receipt xuất hiện trong `continuation-02`: `capture05`4.099543s, `engine`962.974478s, `radar`1194.416784s và `reflection`1209.267931s. Khoảng receipt kế tiếp là958.875116s,231.442127s và14.850820s theo UTC. Đây là khoảng giữa các receipt được chọn, không phải toàn bộ khoảng không có âm thanh/ảnh hoặc một phép đo căng thẳng. Khởi tạo lại counter khi đổi recording/zone không được tính là cue mới.

| Surface thực, sampled state | Thời lượng | Frame ảnh có cờ true | Ngữ cảnh |
| --- | ---: | ---: | --- |
| StoneDepth | 3045.136518s | 14980 | Ambience theo environment, không phải14980scare |
| FalseRadar | 2.796208s | 13 | Radar, cont02 t1194.460067–1197.256273 |
| Reflection/photo | 1.957196s | 8 | PhotoLab, t1137.836356–1139.793552 |
| Reflection/helm | 2.084048s | 11 | Navigation, t1212.094588–1214.178635 |
| Bioelectric | 2.190554s | 10 | PhotoLab, t1604.612414–1606.802969 |
| RockDebris | 7.896926s | 37 | Rock event, t1799.134389–1807.031315 |

Đã xem ảnh thực: [radar005867](/D:/G10/Docs/QA/Zone03ExperienceLoop/baseline/continuation-02/frames/frame-005867.jpg) có các vòng/return mờ trong scope cùng scan địa hình; [helm005948](/D:/G10/Docs/QA/Zone03ExperienceLoop/baseline/continuation-02/frames/frame-005948.jpg) hiển thị bóng người đứng mờ trên màn nhỏ, các nút chính vẫn đọc được; [PhotoLab007830](/D:/G10/Docs/QA/Zone03ExperienceLoop/baseline/continuation-02/frames/frame-007830.jpg) có ảnh specimen xanh/cyan, nhưng một ảnh xanh đơn lẻ không tách chắc lớp bioelectric mờ khỏi nền photo. JSON giữ timestamp và từng interval. Cờ visible/receipt không thay bằng chứng pixel; ảnh đã thấy không chứng minh người chơi nhận ra hay sợ.

Counter stone kết thúc197;181lần tăng nằm trong các khoảng Zone03 được ghi và16lần tăng nằm giữa hai đầu gap đầu. Tổng118 khôi phục ở đầu continuation không phải118cue mới; candidate list tự động của đoạn đó có một observation initialization cần loại khỏi số tăng mới. Engine/radar/reflection mỗi loại1lần tăng được ghi. Không biến số counter ambience thành số scare có hiệu quả.

Listener PCM thực được lưu; các source có volume>0 được quan sát playing tạo cơ hội âm thanh: `engine_reflection`5.410581s;`false_radar_echo`2.635676s;`soft_movement`1.554886s;`close_click`0.403385s;`creature05_pressure`2.830920s qua2interval;`creature06_crystal_motion`1.358302s;`creature07_bioelectric`2.587756s qua2interval. Rock có detonation6.080092s,collapse01/02/03 lần lượt5.470200/3.045671/2.229138s và resonance7.175142s; các source có thể chồng nhau. Đây là sampled `isPlaying`/volume, không kết luận chúng nổi rõ trong mix hoặc được con người nghe thấy. Không có subjective listening trong baseline này.

## Chất lượng ghi và sản phẩm đã chốt

Tổng **34.653 ảnh thật**,176slot ảnh bỏ qua. Cả3VFR mux thành công, H264960×540+AAC, duration từng ảnh lấy từ timestamp gốc. Có338.843.648frame PCM stereo48kHz, tương đương7059.242667s;677.687.296giá trị PCM. Phân tích chính xác thấy2giá trị ở rail âm trong1frame PCM (ở đoạn đầu),0ở hai đoạn tiếp; RMS gộp−31.977624dBFS. Recorder báo0ring-buffer drops. Hai giá trị rail không đủ để tuyên bố méo nghe được hoặc vị trí upstream gây saturation.

Đoạn đầu thay listener lúc loading, khoảng đồng hồ thiếu suy ra≈0.408877s. Bracket callback đầu lần lượt [0.023133,0.030665], [0.014512,0.409064], [0.013825,0.210722]s. Mux dùng midpoint nên riêng continuation-1 có bất định bracket≈±0.197276s, continuation-2≈±0.098449s, cộng DSP/device latency chưa biết. AAC/player rounding và duration MP4 có thể vượt hoặc thiếu chút so với recorder; original UTC/JSONL/WAV là authority. 0ring drops không xóa listener swap hoặc hai gap giữa recording. Không tạo âm thanh thiếu.

- [Timeline tổng hợp có nguồn và mốc thật](/D:/G10/Docs/QA/Zone03ExperienceLoop/01_Baseline_Timeline.json)
- [Video đoạn cap](/D:/G10/Docs/QA/Zone03ExperienceLoop/baseline/evidence/gameplay.mp4), [index](/D:/G10/Docs/QA/Zone03ExperienceLoop/baseline/evidence/event-index.json)
- [Video continuation-1](/D:/G10/Docs/QA/Zone03ExperienceLoop/baseline/continuation/gameplay.mp4), [index](/D:/G10/Docs/QA/Zone03ExperienceLoop/baseline/continuation/event-index.json)
- [Video continuation-2, hoàn tất](/D:/G10/Docs/QA/Zone03ExperienceLoop/baseline/continuation-02/gameplay.mp4), [index](/D:/G10/Docs/QA/Zone03ExperienceLoop/baseline/continuation-02/event-index.json)

Mỗi thư mục giữ JSONL/frame JPEG/WAV gốc, `analysis-console.json`, `mux-result.json`, ffconcat/ffmpeg command/log và ffprobe. Script sử dụng `--zone Zone03`; không dùng template Zone02 `--initial-report-dir`. Phase01 này ghi lại baseline đã hoàn thành và giới hạn bằng chứng, không chấm cảm xúc, không viết audit02 hoặc redesign03.
