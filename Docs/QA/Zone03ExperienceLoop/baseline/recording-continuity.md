# Tính liên tục của baseline Zone03

Trạng thái của đoạn đầu khi đóng: **dừng vì giới hạn ghi 45 phút trong lúc có chậm trễ bên ngoài; chuyến baseline tiếp tục trong cùng phiên Play**. Đoạn này chưa hoàn thành mission đầu và chưa tới Zone04. Nó không phải một lượt baseline hoàn chỉnh, cũng không chứng minh người chơi phải chờ 45 phút vì thiết kế. Baseline sau đó đã hoàn thành trong đoạn thứ ba; xem phần đóng chuyến cuối tài liệu và báo cáo01.

## Đồng hồ và ranh giới bằng chứng

Các mốc dưới đây lấy từ JSONL gốc. `elapsedWall` của mỗi bản ghi có gốc riêng. UTC dùng để nối đồng hồ giữa hai đoạn; không nối ảnh/PCM giả vào khoảng thiếu. Giờ Việt Nam là UTC + 07:00.

| Mốc | UTC ngày 25/10/2026 | Giây trong đoạn đầu |
| --- | --- | ---: |
| `recording.start`, MainMenu | 14:52:10.1162801Z | 0.023133 |
| Zone03 thành authority, còn loading | 14:52:53.3681339Z | 43.276022 |
| Tài nguyên/vị trí Zone03 đã khởi tạo, `transitioning=true` | 14:52:58.8438908Z | 48.750820 |
| Khởi tạo xong và `transitioning=false` lần đầu | 14:52:59.2226315Z | 49.130080 |
| `recording.stop` | 15:37:10.1018574Z | 2700.008629 |
| `continuation/recording.start` | 15:44:11.9978242Z | 0.006417 trong đoạn tiếp nối |

`summary.txt` chốt **2700.011 giây**, 13.305 ảnh và 44 slot ảnh bỏ qua. Mốc stop nằm ngay ranh giới 45 phút; nguyên nhân hết cap được người vận hành xác nhận. Chuỗi `reason=requested-stop` trong summary không tự phân biệt thao tác dừng với cap. Ảnh đầu/cuối ở 0.032989/2699.900263 giây; sự kiện stop không phải một ảnh mới.

Parser mặc định gọi mốc 48.750820 giây là `zone03_ready_elapsed_seconds` vì dùng điều kiện có zone và tài nguyên. Khi so sánh flow cần ghi riêng mốc **49.130080 giây có `transitioning=false`**; không lấy annotation muộn thay cho mốc này. Scene hiển thị `Zone01` là tên scene cabin dùng chung; authority trong telemetry vẫn là `zone=Zone03`.

## Điều khiển, focus và khoảng xử lý

Đây là thời lượng trạng thái lấy mẫu được giữ tới quan sát kế tiếp, không phải thời gian phản ứng của người chơi. Các loại có thể chồng lấp.

| Quan sát trong đoạn đã đóng | Giây |
| --- | ---: |
| Navigation hoạt động, gồm di chuyển/quay/đổi độ sâu | 30.111843 |
| Tốc độ ngang khác 0 | 4.473836 |
| Đổi độ sâu | 25.440369 |
| Quay | 0.804731 |
| Một cặp start/stop của chart input driver | 4.372057 |
| 4 khoảng `operator.gap` có đủ hai mốc | 1791.467609 |
| `focused=false`, hai khoảng được quan sát | 1260.078982 |
| Editor pause hoặc `timeScale=0` được quan sát | 0 |

Hai khoảng mất focus là [137.225381, 143.645238] và [145.321936, 1398.981061] giây. Lần giữ nút dive yêu cầu 25,35 giây ở 138.026221–163.374657 không làm đổi depth: hai đầu đều 230 m. Đây là yêu cầu input đã ghi, không phải 25 giây chuyển động thành công. Sau khi lấy lại focus, lần dive ở 1963.774577–1989.124223 chuyển depth 230 → 358.396210 m. Không có căn cứ để quy khoảng chậm trễ ở giữa thành vấn đề hiểu UI, nỗi sợ hay nhịp thiết kế.

Bốn cặp `operator.gap` là [49.116827,63.334102], [63.335425,137.137606], [165.319566,1399.592929], [1994.259534,2463.434324]. Còn một start ở 2473.212061 giây, nội dung “inspect short entry leg real obstruction feedback”, không có end trước khi recorder đóng. **Không tự điền end hoặc cộng phần đuôi vào tổng operator gap đã xác nhận.** Các khoảng chưa đánh dấu vẫn chưa phân loại.

Chart driver bắt đầu ở 2464.392314 và dừng ở 2468.764371 giây, waypoint do người vận hành chọn (65,135), normal WASD, lý do dừng `waypoint-radius-reached`. Telemetry vẫn có tốc độ ngang sau mốc dừng driver, nên thời lượng chuyển động không đồng nhất với thời lượng owner của input. Không có capture UI, mission/photo/capture thành công hoặc receipt trong đoạn này. Tổng owner của pointer là 51.429984 giây, gồm cả lần giữ nút không tạo chuyển động khi mất focus. Parser đo 2595.697111 giây không có owner input trong Zone03, trong đó 804.312827 giây nằm ngoài các operator gap đã ghép cặp; số này vẫn chưa phân loại và không phải số đo confusion.

## Trạng thái trước và sau khoảng thiếu

| Trường | Khi dừng đoạn đầu | Khi bắt đầu đoạn tiếp nối |
| --- | ---: | ---: |
| Authority/day | Zone03 / 1 | Zone03 / 1 |
| Vị trí x,y | 63.143867, 133.535629 | 81.978271, 138.053589 |
| Depth | 358.396210 | 358.396210 |
| Heading | 51.438347° | 83.450508° |
| Distance tích lũy | 63.746552 | 83.317055 |
| Energy | 69.362038 | 67.708191 |
| Hull | 100 | 82 |
| Stone counter | 102 | 118 |
| Engine/radar/reflection counter | 0 / 0 / 0 | 0 / 0 / 0 |
| Objective/receipt | rỗng / rỗng | rỗng / rỗng |
| Photo đã chụp/capture trong ngày | 0 / 0 | 0 / 0 |

Khoảng không được ghi giữa hai sự kiện là **421.895967 giây UTC (7 phút 1,896 giây)**. Realtime của cùng phiên tăng từ 2716.097547 tới 3137.993712, tương đương **421.896165 giây**; chênh lệch khoảng 0,2 ms giữa hai phép đo không phải bằng chứng reset. Operator xác nhận vẫn cùng phiên Play, không load lại save hoặc tạo lượt mới.

Trong khoảng thiếu, displacement thẳng giữa hai đầu là 19.368705 đơn vị, distance tăng 19.570503, energy giảm 1.653847 và hull giảm 18. Hai đầu chứng minh các thay đổi này, nhưng **không cho biết chính xác input, đường đi, thời điểm va chạm hay nội dung nhìn/nghe trong khoảng thiếu**. Operator cho biết một leg tới (160,145) và xử lý va chạm có thể xảy ra sau cap; đó là lời kể vận hành, không phải nội dung được video đoạn đầu chứng minh. Không gán thay đổi hull cho sợ hãi.

Không ghép video hai đoạn thành một chuyến liên tục không chú thích. Không tạo PCM thay cho 421.895967 giây thiếu. Baseline cuối phải giữ đoạn cap, khoảng mất bằng chứng và đoạn tiếp nối như ba phần có ranh giới rõ.

## Ảnh, âm thanh và giới hạn diễn giải

WAV gốc chứa 129.580.032 frame âm thanh stereo 48 kHz, tức **2699.584 giây PCM thực**; recorder báo 0 sample bị bỏ vì ring buffer. Phân tích WAV đếm 254.853.468 giá trị PCM khác 0 trong 259.160.064 giá trị; RMS −32.238140 dBFS. Có **2 giá trị ở rail âm, thuộc 1 frame PCM**, không có rail dương. Đây là candidate saturation/limiting ở dữ liệu đã clamp; không chứng minh méo nghe được hay chỉ ra nguồn gây saturation. Số nonSilent của recorder đếm miền float với ngưỡng riêng, không phải cùng phép đo với PCM khác 0.

Callback đầu được quan sát trong bracket 0.023133–0.030665 giây; mux dùng midpoint 0.026899. Có 2 listener segment; segment sau bắt đầu ước tính ở 43.294443 giây, boundary PCM 2.057.216 frame. Chênh PTS giữa hai segment tương đương khoảng listener thiếu 0.408877 giây. 0 dropped sample không xóa khoảng này. Video review dùng duration từng ảnh trong ffconcat, không ép một chuỗi 5 fps đều đặn; khoảng ảnh lớn nhất 4.615602 giây trong loading và 0.630039 giây sau khi có tài nguyên. Mux đặt PTS theo đồng hồ callback được quan sát; DSP/device latency, ranh giới callback suy ra và làm tròn packet AAC vẫn giới hạn độ chính xác đồng bộ. Không tổng hợp âm thanh thiếu.

Mux hoàn tất thành công: `gameplay.mp4` H264 960×540 + AAC, 63.411.089 byte, duration container 2699.986771 giây. 13.305 ảnh thực tạo 13.306 frame mux vì ảnh cuối được lặp để giữ tới ranh giới cuối. Đây là video review riêng cho đoạn cap, không chứa khoảng trống tới continuation. `mux-result.json` giữ ffprobe và giới hạn xử lý gap của player.

Stone counter 102 và các cờ `stoneVisual`/`visibleProceduralPresets` là trạng thái component được ghi. Khoảng đứng yên dài làm tăng số lần ambience có thể khởi phát; không được dùng tổng counter để tuyên bố 102 scare hoặc nhịp kinh dị hiệu quả. Chưa có đánh giá nghe/nhìn của con người hay số đo cảm xúc trong tài liệu này.

Nguồn và sản phẩm offline của đoạn cap: `evidence/timeline.jsonl`, `evidence/frames.jsonl`, `evidence/summary.txt`, `evidence/actual-game-audio.wav`, `evidence/continuity-metrics.json`, `evidence/event-index.json`, `evidence/gameplay.mp4`, `evidence/mux-result.json`. Đoạn `continuation` đã đóng và được xử lý riêng trong phần bổ sung bên dưới. Ở lần xử lý đầu chỉ một số hàng đầu của `continuation-02` được đọc; đoạn đó sau này đã đóng và được tổng hợp/mux riêng khi chuyến hoàn thành.

Lệnh cho đoạn đã đóng:

```powershell
python 'D:\G10\Tools\QA\summarize_zone02_experience.py' 'D:\G10\Docs\QA\Zone03ExperienceLoop\baseline\evidence' --zone Zone03 --segment zone03-baseline-capped-segment --status 'external-delay cap; baseline voyage continues same live session' --mux > 'D:\G10\Docs\QA\Zone03ExperienceLoop\baseline\evidence\analysis-console.json'
```

Không truyền `--initial-report-dir`; tài liệu 01/02 chưa được viết hoặc sửa bởi bước này.

## Bổ sung: continuation-1 đóng khi capture còn chạy

`baseline/continuation` bắt đầu UTC15:44:11.9978242Z và có `recording.stop` UTC16:25:34.0965884Z (`elapsedWall=2482.105162`). Summary chốt **2482.109 giây**, 12.246 ảnh, 23 slot ảnh bỏ qua. Đây là đoạn tiếp nối chưa hoàn thành baseline. Không quan sát `focused=false` trong đoạn này.

Thời lượng sampled state, không cộng các loại chồng lấp: navigation41.345467 giây; horizontal34.629391; chart driver34.270689; depth1.415670; computer632.211701; PhotoLab624.766619; capture UI21.875098; owner của capture tracking agent20.838783. Operator gap có đủ cặp315.239725 giây và còn1 marker không đủ cặp. No-owner input2421.267496 giây, trong đó2106.027772 nằm ngoài các operator gap được ghép cặp; phần này chưa đủ dữ liệu để quy cho thiết kế, hành vi con người, confusion hoặc fear.

L1 PHOTO được quan sát hoàn tất lần đầu UTC16:25:05.4760989Z, ở2453.484476 giây; đây là trạng thái sau manual SEND, không phải annotation muộn. Capture UI hoạt động lần đầu UTC16:25:12.2251814Z, ở2460.233902 giây. Lúc đóng: capture vẫn chạy,5hits,23.679873 giây còn lại, capturesToday0, objective chỉ có `Z3_L1_PHOTO`; không tuyên bố capture đã hoàn thành. Vị trí(457.604004,189.890259),depth367.052338,distance614.101440,energy24.494684,hull82,day1,photosTaken2,receipts rỗng.

Stone counter đầu/cuối118→188, tức +70. Danh sách tự động có71 candidate counter observations vì quan sát đầu118 là counter được khôi phục trong cùng phiên, không phải118 khởi phát mới. Khi tổng hợp phải loại initialization này khỏi số lần tăng mới; counter/source receipts vẫn không chứng minh exposure hoặc cảm xúc. Engine/radar/reflection counter và receipt đều không tăng trong đoạn này.

`baseline/continuation-02` bắt đầu UTC16:25:36.3048814Z: khoảng thiếu giữa hai sự kiện là **2.208293 giây UTC**, tương ứng2.2084535 giây realtime. Hai đầu giữ nguyên vị trí/tài nguyên/objective/5hits, captureActive=true; captureRemaining đổi23.679873→21.841551. Không có ảnh/PCM của khoảng này và không tự điền chúng. Lúc ghi phần này đoạn tiếp nối còn mở; nay đoạn đã đóng được xử lý riêng và aggregate cuối giữ các video/ranh giới này.

Lệnh cho continuation-1 đã đóng:

```powershell
python 'D:\G10\Tools\QA\summarize_zone02_experience.py' 'D:\G10\Docs\QA\Zone03ExperienceLoop\baseline\continuation' --zone Zone03 --segment zone03-baseline-continuation-1 --status 'closed partial continuation; capture active at stop; same live baseline voyage continues in next recording' --mux > 'D:\G10\Docs\QA\Zone03ExperienceLoop\baseline\continuation\analysis-console.json'
```

Số đo chi tiết của đoạn này giữ ở `continuation/continuity-metrics.json`; video/index/mux metadata có cùng tên với đoạn cap trong thư mục `continuation` riêng. Mux/index đã hoàn tất và các JSON được kiểm tra đọc thành công. WAV có119.140.352 frame/238.280.704 giá trị PCM, stereo48kHz, duration2482.090667 giây,238.157.785 giá trị khác0,peak32658/32767,RMS−32.236874dBFS,**0 giá trị ở rail**,0ring drops và1listener segment. Đây là số đo tín hiệu, không phải kết luận nghe chủ quan.

Callback đầu chỉ được bracket rộng **0.014512–0.409064 giây**; mux dùng midpoint0.211788 với bất định nửa bracket khoảng0.197276 giây, ngoài DSP/device latency chưa biết. Không diễn giải thời điểm âm thanh từ mux với độ chính xác cao hơn giới hạn này. MP4 riêng thành công,57.739.978byte,H264960×540+AAC,container2482.287938 giây (video stream2481.861,audio stream2482.112). Endpoint mux có rounding/offset khác recorder2482.109 giây; UTC/JSONL gốc tiếp tục là authority của đồng hồ. Không tạo PCM trong khoảng2.208293 giây giữa hai đoạn.

## Đóng chuyến sau continuation-02

Đoạn thứ ba chốt `recording.stop` UTC2026-10-25T16:56:53.8601752Z,elapsed1877.562921s,summary wall1877.567s. First Zone04 authority là sample16:56:12.4750817Z (quan sát16:56:12.5016668Z muộn hơn); settled Zone04 có tài nguyên/vị trí mới và `transitioning=false` lúc16:56:13.0716568Z. Mọi objective Zone03, craft/install và rock destruction đã xuất hiện trước transfer. Baseline hoàn thành trong cùng phiên, không reset sau cap hoặc rotation.

Mux thứ ba hoàn tất thành công:9.102ảnh thật/109skipped slots,MP4H264960×540+AAC107.505.471byte. WAV90.123.264frame stereo48kHz=1877.568s,0rail,0ring drops,RMS−31.331856dBFS. Bracket callback0.013825–0.210722s giới hạn đồng bộ; không dùng MP4 duration1877.666437s làm đồng hồ gameplay.

[01_Baseline_Playtest.md](/D:/G10/Docs/QA/Zone03ExperienceLoop/01_Baseline_Playtest.md) và [01_Baseline_Timeline.json](/D:/G10/Docs/QA/Zone03ExperienceLoop/01_Baseline_Timeline.json) tổng hợp đủ ba đoạn, hai gap, timeline mission/receipt/Rest, surface/audio opportunities và confound vận hành. Không đổi trạng thái lịch sử của hai đoạn đóng trước thành đã hoàn thành riêng lẻ.
