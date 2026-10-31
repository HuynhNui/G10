# 05 — Final Zone03 → Zone04

**Final hoàn thành:** một chuyến live duy nhất, sau đúng một vòng audit → redesign → implementation, đã làm đủ8objective và đi qua physical exit tới Zone04. Mốc Zone03 sẵn sàng đầu tiên **2026-10-26T07:18:58.6368293Z** → Zone04 đã khởi tạo và `transitioning=false` **07:58:14.6443951Z** là **2356.007566s — 39 phút16,008 giây**. Recorder đóng sau2408.086s, dưới cap45phút; không có rotation hoặc gap giữa recording.

Lượt này dùng Keyboard/Mouse InputSystem, UI thật, WASD, thời gian/tài nguyên/va chạm bình thường; có chart waypoint assistance và capture tracking dựa trên hook/fish đang hiển thị. Tuyến đã được học trong baseline. [Initial equivalence](initial-equivalence.json) xác nhận các gameplay fields đã chọn bằng nhau khi bắt đầu; đây không phải hai lượt của người mới độc lập. Không suy diễn nỗi sợ, nghe chủ quan hay hiệu quả cảm xúc từ telemetry.

## Authority, tiến trình và chi phí thực

Resource-ready xuất hiện07:18:58.4350063Z nhưng còn `transitioning=true`; không dùng nó thay mốc chính. First authority Zone04 là **07:58:13.9403644Z**, trước settled0.704031s; khi đó tài nguyên bằng0 trong handoff. Settled Zone04: day6,(290,950),depth589.299561m,energy47.608528,hull100. Tên scene cabin `Zone01` không thay authority `zone`. Annotation/snapshot07:58:25 muộn hơn không dùng làm finish time.

| Quan sát đầu tiên | UTC26/10 | Giây từ Zone03 sẵn sàng |
|---|---|---:|
| L1 PHOTO/manual SEND | 07:29:32.4307785Z | 633.793949 |
| L1 CAPTURE | 07:29:59.8361072Z | 661.199278 |
| L2 PHOTO/manual SEND | 07:41:23.6069113Z | 1344.970082 |
| L3 PHOTO/manual SEND | 07:53:04.2900326Z | 2045.653203 |
| L3 CAPTURE | 07:53:45.4804745Z | 2086.843645 |
| CRAFT Rock Breaker | 07:55:16.7204529Z | 2178.083623 |
| INSTALL Rock Breaker | 07:55:17.7973094Z | 2179.160480 |
| DESTROY ROCK BARRIER, tương tác thật | 07:57:46.4131144Z | 2327.776285 |
| Zone04 ổn định | 07:58:14.6443951Z | 2356.007566 |

Có3ảnh và2capture, không thêm capture cho L2. **5lần Rest**, day1→2 lúc07:31:28.8822781Z;2→3 lúc07:41:27.1817974Z;3→4 lúc07:44:48.4562310Z;4→5 lúc07:48:11.1757891Z;5→6 lúc07:55:22.2657864Z. Distance tiếp tục qua Rest, chỉ reset khi Zone04 khởi tạo.

Một va chạm thật ở đoạn đầu được quan sát07:21:30.5614147Z,(141.947678,220.890671),depth367.146667m: hull100→82. Pilot phục hồi đường đi bằng Radar/corridor và input thật sau obstruction latch của helper; giữ nguyên lượt, không reset tài nguyên. Rest đầu tiên chữa hull trở lại100. Đây là chi phí gameplay/navigation thực, không gán cho presentation.

Rock objective và `rockDestroyed=true` đã có từ07:57:46.4131144Z. `rockActive` kết thúc07:57:56.0346424Z với `rockElapsed=10.0`; tích phân sampled active là9.621551s. Hai ranh lấy mẫu không chứng minh lock chỉ dài9,62s hoặc thay đổi hợp đồng10s. Debris preset có8.004597s; sound tail còn tiếp sau lock. Gate chỉ đổi authority sau destruction và chuyển động tới exit. Không dùng snapshot rock muộn07:57:59 làm first completion.

## Hoạt động và thời gian chưa phân loại

Các thời lượng bên dưới tích phân trạng thái thật trong phần Zone03 của cửa sổ chính; chúng **không loại trừ nhau**. Input owner là khoảng điều khiển được instrument, không phải đánh giá kỹ năng người chơi.

| Quan sát | Giây |
|---|---:|
| Navigation: đi/quay/đổi depth | 395.526995 |
| Horizontal motion | 284.241935 |
| Đổi depth | 93.587698 |
| Quay | 42.389251 |
| Chart-driver input owner | 283.747114 |
| Keyboard riêng | 8.348585 |
| Pointer owner | 98.442509 |
| Capture tracking owner | 58.540711 |
| Capture UI active | 61.436078 |
| Computer visible | 123.106039 |
| PhotoLab visible | 96.780501 |
| Rest app visible | 9.734694 |
| Upgrade app visible | 6.780289 |

Không có paired `operator.gap` hoặc observed pause/unfocused trong cửa sổ chính; điều này không chứng minh không có chờ công cụ. **1906.224738s không có input owner** vẫn chưa phân loại, bao gồm quan sát, thao tác công cụ, UI và gameplay khác. Không gọi khoảng này là confusion, fear, thiết kế im lặng hoặc thời gian chơi thuần. Không tự tạo marker cho processing/focus chưa được ghi.

Partition loại trừ nhau riêng: navigation395.526995s,capture61.436078s,computer121.609114s,transition/loading8.878274s,station khác/chưa phân loại1768.557514s. Tổng elapsed-state2356.007975s khác UTC chính khoảng0.409ms do đồng hồ/lấy mẫu. Parser gốc còn có scope phụ từ resource-ready sớm hơn0.201823s tới authority departure; JSON giữ cả hai scope thay vì trộn mẫu số.

## Delivery, opportunity và recovery tự nhiên

Primary durable receipts quan sát được: `capture05` tại recorder t701.418302s,`engine`814.928278s,`radar`1394.684233s,`reflection`2236.514031s. `radar.opportunity` tại1391.989495s và `reflection.opportunity`1886.007715s là **saved earned intent**, không phải hai scare bổ sung. Counter tăng mới: stone47,engine1,radar1,reflection1; stone là ambience, không phải47scare có hiệu quả.

Engine source thật bắt đầu814.525178s, còn receipt tới814.928278s sau khoảng0.403s voice progression. Radar preset/source bắt đầu1394.417647s và receipt tới1394.684233s. Đây phù hợp checkpoint mới; baseline đã dùng launch receipt, nên không so receipt timestamp như cùng định nghĩa exposure. Cơ hội Radar được giữ từ scan thật, không đổi distance anchor sang lúc commit.

Reflection opportunity được ghi khi depth thực **600.897705m**,distance4025.821045,ở L3 approach. Nó còn tồn tại qua workflow Radar → PhotoLab/SEND → capture L3 → craft/install → Rest, khi primary reflection vẫn chưa delivered. Có3launch observations:

| Episode | Recorder t, giây | Bằng chứng |
|---|---|---|
| Lead thứ nhất | 1895.103913→1897.635508 | Helm chuyển sang Radar; pending `playing→armed`,activeSequences1→0; chưa có Reflection preset/voice hoặc receipt được ghi. |
| Lead thứ hai | 2078.205390→2081.373448 | Rời Helm; pending `playing→armed`,activeSequences1→0. Có một sample-only preset window7.632ms, **0frame ảnh true** trong episode; không có `soft_movement`/`close_click` ở đây, chưa có receipt. |
| Retry cuối | launch2233.134287; surface2236.003777→2238.125598 | L3,capture,craft,install,Rest đã xong. Helm hiện bóng thực; accompanying voice và một durable receipt. |

Các chuyển trạng thái, audience và sequence cleanup hỗ trợ việc hai lead chưa hoàn tất được re-arm. Không biến3launch thành3delivered scares, hoặc biến sample-only7.6ms thành peak đã được người chơi thấy. Bản thân lượt này **không cần retry ở590m**: delivery thực vẫn tại646.313232m; khả năng giữ opportunity khi lên590 là hợp đồng triển khai/functional validation, không phải tình huống đã chứng minh bằng live final này.

## Bề mặt và frame đã xem thực

Thời gian dưới đây là sampled component/preset state; ảnh thật được xem riêng. FalseRadar và hai surface Reflection được giữ độc lập.

| Preset thực | Recorder t, giây | Thời lượng |
|---|---|---:|
| Radar:FalseRadarSweep | 1394.417647→1397.106169 | 2.688522s |
| PhotoLab:ProceduralReflection | 1317.526613→1319.512904 | 1.986291s |
| Reflection:ProceduralReflection, final sustained | 2236.003777→2238.125598 | 2.121821s |
| PhotoLab:BioelectricPulse | 2084.276583→2086.541849 | 2.265265s |
| Global:RockDebris | 2368.404301→2376.408899 | 8.004597s |

- [Radar frame006817](final/evidence/frames/frame-006817.jpg),t1395.691916: các vòng/return mờ hiện trong scope cùng scan địa hình; charge9/10 và labels chính đọc được.
- [Helm frame010897](final/evidence/frames/frame-010897.jpg),t2237.121341: bóng người đứng mờ hiện trên màn nhỏ; depth,coordinates,heading,speed,energy vẫn đọc được. Đây là representative visible peak đã xem, không phải phép đo opacity cực đại.
- [PhotoLab frame006445](final/evidence/frames/frame-006445.jpg),t1318.701232: ảnh specimen06 và SEND UI hiện đúng trong PhotoLab reaction. Một still không tách chắc overlay mờ khỏi art specimen; không gọi nó là Helm reflection.
- [Bioelectric frame010170](final/evidence/frames/frame-010170.jpg),t2085.093389: ảnh specimen07 xanh/cyan với vệt sáng mảnh. Một ảnh không chứng minh độc lập chuyển động pulse hoặc người chơi nhận ra nó; giữ hạn chế tương tự baseline.
- [Debris frame011535](final/evidence/frames/frame-011535.jpg): mảnh sáng nhỏ gần sàn, message stabilizing9s; frame011550 có stabilizing6s. Debris khá mờ trên art cabin; receipt không thay pixel evidence.

Helm frame010897 có **depth646.313232m,(1275.284302,281.594666),distance4187.583008**. Lúc đó L3 PHOTO/CAPTURE và craft/install đã xong, đang di chuyển tới rock trước demolition. Chia distance này cho last valid Zone03 nontransitioning distance4678.920898 được **89.498906%**. Baseline actual frame005948:depth478.695679m,(201.522354,548.332092),distance1421.868286/4574.282715=**31.083962%**,sau L2 và trước L3. Mẫu số bỏ state Zone04 transitioning có distance cũ; cùng một định nghĩa được dùng hai lượt. Đây chứng minh vị trí/phase muộn và sâu hơn trong final này; route/detour vẫn khác, không phải pacing experiment đã kiểm soát hoặc thang điểm sợ.

Khoảng selected-primary onset được định nghĩa từ `engine_reflection` source-on tới first real captured Radar frame rồi first real captured Helm frame. Loại PhotoLab,opportunity và sample-only7.632ms. Baseline engine→Radar231.484869s,Radar→Helm17.634865s; final579.892849s và841.743161s. Lớn nhất nội bộ tương ứng231.484869s và841.743161s. Thêm ready/settled boundaries: baseline lớn nhất6520.049250s từ ready→engine;final lớn nhất841.743161s. Đây là **onset-to-onset**, còn bao gồm duration peak trước đó. Nếu đo từ end trước đến confirmed onset sau, largest internal gap làbaseline226.074734s,final839.055089s. Các khoảng này có missions,ambience,UI,operator/tool time; baseline ready→engine còn đi qua424.104260s không được ghi. Không gọi chúng là hoàn toàn không có âm thanh, không có drama hoặc không có căng thẳng.

## PCM thực, cơ hội nghe và chất lượng ghi

Source có volume>0 và báo playing tạo cơ hội âm thanh, không chứng minh nổi rõ trong mix hoặc con người nghe thấy. Các window sampled chính:

| Source | Recorder window, giây | Tổng |
|---|---|---:|
| engine_reflection | 814.525178→819.965986 | 5.440809s |
| false_radar_echo | 1394.417647→1397.106169 | 2.688522s |
| soft_movement | 2236.161590→2237.560594 | 1.399004s |
| close_click | 2238.125598→2238.528072 | 0.402474s |
| creature05_pressure | 648.727493→650.163952 và701.418302→702.830066 | 2.848222s |
| creature06_crystal_motion | 1317.526613→1318.894198 | 1.367585s |
| creature07_bioelectric | 2084.276583→2085.871873 và2127.062530→2128.271672 | 2.804432s |

Demolition có detonation6.034320s,collapse01 tổng5.783562s qua2window,collapse02 2.970037s,collapse03 1.815883s,resonance7.426701s; các source có thể chồng nhau. JSON giữ start/end UTC,position/context và các ambience windows đầy đủ.

Actual WAV: stereo48kHz PCM16,**115571712sample frames =2407.744s**,231143424sample values,RMS**−28.228088dBFS**. Exact analysis thấy **18negative rail values trong13PCM frames**,0positive rail; recorder báo**0ring-buffer drops**,0format changes. Rails là saturation/limiting candidates; không đủ xác định upstream source hoặc méo nghe được. Không có subjective listening/fear report trong lượt này.

Listener thay trong loading trước ready; callback-clock inference giữ khoảng thiếu khoảng0.346384s. First callback bracket[0.0193978,0.0217136]s; mux dùng midpoint, riêng bracket tương đương±0.001158s,cộng DSP/device latency chưa biết. 0drops không xóa listener gap hoặc bất định alignment. Không tạo PCM để lấp khoảng thiếu; WAV/callback/raw clocks là authority trước AAC/player rounding.

Có**11717JPEG thật**,122slot bỏ qua,max actual frame gap0.780571s,median0.201564s. [Video gameplay](final/evidence/gameplay.mp4) mux thành công H264960×540+AAC,VFR theo timestamp gốc,70,978,933bytes; ffprobe duration2408.089208s. Ảnh cuối thật được giữ tới recording stop, không nội suy cảnh hoặc tạo replacement frame/PCM. Duration MP4 không thay đồng hồ voyage. Capture/disk overhead có thể ảnh hưởng lịch frame và thời gian vận hành.

## Giới hạn đối chiếu và sản phẩm

Baseline7393.849025s bị chi phối chậm trễ ngoài game,2recording gaps424.104260s,45phút cap đầu,corridor assistance và học đường; final2356.007566s đã biết route và ít thời gian công cụ hơn. Navigation activity final395.526995s so baseline387.526325s không cho phép kể rằng redesign làm di chuyển nhanh hơn. Nội dung được giao muộn/sâu hơn và recovery qua UI được chứng minh trong chuyến này; cải thiện sợ hãi, khả năng nhận ra pulse hoặc pacing chủ quan vẫn chưa được người chơi đánh giá.

- [Timeline tổng hợp](05_Final_Timeline.json): clocks đầu tiên,8objective,Rest,input/focus scopes,preset/voice windows,retry evidence,actual-frame review,normalized route position và định nghĩa khoảng primary.
- [Event index](final/evidence/event-index.json),[analysis console](final/evidence/analysis-console.json),[mux result](final/evidence/mux-result.json).
- [Raw timeline](final/evidence/timeline.jsonl),[actual frame index](final/evidence/frames.jsonl),[actual listener WAV](final/evidence/actual-game-audio.wav).
- [Offline aggregation helper](final/aggregate_closed_final.py) chỉ đọc raw đã đóng và ghi Docs/QA; không kết nối Unity hoặc sửa gameplay/source.

Phase05 đóng final voyage của đúng một vòng cải tiến. Báo cáo không bắt đầu vòng redesign mới và không thay native functional evidence hoặc bản so sánh06.
