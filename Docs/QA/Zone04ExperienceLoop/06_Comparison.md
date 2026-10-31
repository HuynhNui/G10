# Zone04 — so sánh một vòng cải tiến

**Đã hoàn tất đúng một vòng audit → redesign → triển khai → native validation → lượt chơi cuối.** Lượt cuối tự PHOTO/SEND cả A/B, đọc marker được mở thật, lái tàu tới đích và gặp **HIDDEN ENDING · BIOLOGICAL SURVEY11/11**; actual presentation đạt16s lúc **2026-10-31T03:54:05.8115200Z** (10:54:05 ngày31/10/2026, giờ Việt Nam). Không mở redesign thứ hai sau kết quả này.

**Góc nhìn người chơi:** phần mạnh nhất là tự nối được chuỗi đầu mối bằng công việc của mình: SEND ảnh, capture, mở dữ liệu trong Mission Log, chọn TIẾP TỤC KHÁM PHÁ, Radar kiểm chứng rồi tự chụp/nộp A và B. Tín hiệu cuối đến khi vẫn đang lái tàu, trước khi Ending commit. Quyền tiến trình vẫn nằm ở hành động người chơi. Tuy vậy, Zone04 vẫn nghiêng về khảo sát và vận hành tàu; quãng di chuyển yên lặng, việc tìm depth/bearing, ảnh NoSubject và Rest lặp lại còn hút chú ý khỏi nỗi bất an. Đây là đánh giá thiết kế từ lượt điều khiển có trợ giúp, chưa phải đo cảm giác của người thật.

Baseline **vẫn incomplete trước L2 do control/route-assurance blocker**, không phải chứng minh hình học không thể đi. Downstream A/B/final/Ending của baseline là **N/A**. Final đi xa hơn không đủ để gán toàn bộ khác biệt cho patch hoặc gọi thời gian nhanh hơn. [Baseline](01_Baseline_Playtest.md), [audit](02_Experience_Audit.md), [redesign duy nhất](03_Horror_Redesign.md), [implementation/native](04_Implementation_Validation.md), [final playtest đã seal](05_Final_Playtest.md), [timeline/quality/SHA](05_Final_Timeline.json).

## Điều đã thay đổi và điều thực sự quan sát được

| Thay đổi | Kết quả có bằng chứng | Giới hạn khi đánh giá như người chơi |
| --- | --- | --- |
| Chart Zone04 có terrain layer mặc định bật, vàng nhạt và toggle ON/OFF | Cả hai trạng thái xuất hiện trên ảnh thật. Ordinary-input voyage đã qua L2/A/B/final. Native kiểm tra mask/UV/cache/toggle/marker và không có ship marker. | Screenshot perception, route knowledge và điều khiển đã học cùng góp phần. Tooltip vẫn che một mảng chart. Không phải thử A/B độc lập với người mới. |
| Receipt ghi sau checkpoint tiếp nhận | Fragment có card đầy đủ đọc được qua frame; A/B có đúng ảnh đang chọn và actual voice tiến rồi mới receipt. Actual SEND được ghi riêng, không do cue làm hộ. Native pause/focus/mute/storage/restore/legacy/retry đạt. | Receipt không chứng minh nghe/hiểu trọn cue. Không hứa exactly-once xuyên crash khi storage vẫn không ghi được. Actual final cue lần này không bị Rest cắt; retry là bằng chứng native riêng. |
| Environmental withdrawal/recovery có khoảng thở | A có actual bed giảm tới0.12, giữ sau voice rồi hồi1. Final có actual voice và purple tint trong Helm, bed0.12 lúc voice. Engine/vent/control/warning giữ bus hiện có. | Tại B bed đã0.025 trước cue; final sau đó còn đi vào vùng yên hơn. Không khẳng định mọi cue đều newly duck từ1 hoặc actual recovery luôn trở lại1. Gain/source clock không đo headset hearing hoặc anticipation. |
| Wreck L1 tách khỏi foreground hơn | Native matched capture đổi2064pixels,0 ngoài hợp background rect; GoodPhoto/coverage/occlusion và archive authority giữ nguyên. Ảnh final vẫn có chi tiết nền mờ. | Wreck còn tinh tế; chưa chứng minh người chơi nhận ra. Bốn creature definition vẫn dùng art fallback chung, làm A/B thiếu nhận diện riêng. |

Đã tái sử dụng8WAV, catalog/sequences và artwork/procedural surfaces hiện có. Không thêm package, mua/generate tài nguyên hoặc đổi map/mission/shared scene để hoàn tất. Giới hạn license/provenance audio cũ vẫn giữ, không tạo commercial clearance mới. Rare Radar-seed risk được ghi riêng, chưa xác nhận là nguyên nhân baseline và không sửa trong vòng này.

## Rubric thiết kế sau toàn tuyến

Thang1–5 theo rubric02; mức5 cần người đại diện xác nhận. Điểm baseline chỉ áp dụng đoạn L1/tìm tuyến đã gặp, còn final phủ toàn tuyến có trợ giúp. **Các điểm là judgment thiết kế, không phải fear score hoặc phép trừ chứng minh tác động nhân quả.**

| Chiều | Baseline phần đã gặp | Final phần đã quan sát | Lý do chốt |
| --- | ---: | ---: | --- |
| Không khí | 3 | 3 | Art/audio nhất quán; rút bed có ngữ cảnh, nhưng màu cabin dịu và generic creature art chưa tạo đe dọa rõ. |
| Chờ đợi | 2 | 3 | Fragment→choice→A/B→tín hiệu khi lái tạo chuỗi hẹn có payoff; không dùng external gap/operator latency làm suspense. |
| Bất định | 2 | 3 | Thông tin được tìm qua thiết bị và ảnh của chính chuyến đi; chưa đo motif recognition hoặc người diễn giải mối đe dọa. |
| Nhịp | 2 | 2 | Chore loop, NoSubject/depth/bearing và nhiều Rest vẫn chiếm công việc; tín hiệu cuối chưa xóa được tải vận hành. Unknown idle không tự động gọi là chán. |
| Tác động sự kiện | 3 | 3 | Card, photo voice, tint và hidden Ending đều delivery thật; wreck tinh tế và thiếu distinct creature art giới hạn độ nhớ. |
| Agency | 3 | 4 | SEND/capture/explore/Radar/A/B SEND/physical arrival giữ quyền riêng; native retry tránh mất cơ hội khi audience ngắt. Chưa xác nhận perceived agency của người thật. |

**Phán đoán sau vòng này:** flow hoàn tất được bằng hành động thật và các cue chính đã có checkpoint kiểm chứng. Chưa đủ bằng chứng gọi Zone04 gây sợ hơn hoặc chốt cảm xúc trọn chương. Trọng tâm khi bàn giao là một khảo sát có bí ẩn và lựa chọn đáng theo đuổi, với ma sát vận hành còn rõ. Không tự sửa thêm sau final playtest.

## So sánh progression và thời gian

| Chỉ số | Baseline đã khóa | Final đã seal |
| --- | --- | --- |
| Khởi tạo | DEBUG Zone04, regular upgrades NONE | Toàn bộ parsed snapshot **và byte file** bằng baseline; không chỉ whitelist resources. |
| L1 | PHOTO/SEND→capture→fragment thật | Đủ thật; một capture attempt chưa hoàn tất được giữ trong dữ liệu trước visible-hook retry thành công. |
| L2/explore/A/B/final | Không tới L2; downstream N/A | L2 SEND, explore, A/B reveal/PHOTO/SEND, final opportunity/cue, physical hidden arrival và presentation16s thật. |
| Obstruction-stop markers | 19 | 0 |
| Sampled hull-decrease events | 17 | 0 (tổng giảm0.000) |
| Rest ghi thực trong recorder | 6 | 9; thêm day4→5 được pilot khai báo ngoài recorder, không cộng thành raw-recorded Rest. |
| Completion time | Censored/incomplete, không có completion duration | Có raw clocks bên dưới; không trừ baseline để gọi cải thiện tốc độ. |
| Human fear/immersion/hearing/recognition | Chưa đo | Chưa đo |

| Khoảng đo bổ sung | Baseline đã quan sát, giây | Final đã quan sát, giây | Cách hiểu |
| --- | ---: | ---: | --- |
| Navigation: moving/turning/depth change | 495.916 | 629.371 | Nonexclusive sampled activity; final đi toàn tuyến dài hơn, không suy engagement/speed từ phép trừ. |
| Capture UI active | 33.826 | 72.094 | Chứa hai final attempts; không đo human minigame skill. |
| Visible Photo Lab | 15.092 | 1,591.450 | Visible UI, không phải toàn mission-interaction effort hoặc human comprehension. |
| Visible Rest app | 68.779 | 253.753 | Khác Rest count; không gồm untimed day4→5 ngoài recorder. |
| Explicit paired operator processing | 186.496 | 0.000 | Chỉ matched markers; không gán hết unknown time thành processing. |
| No injected-input owner | 3,786.479 | 12,888.472 | Unknown idle proxy, **confusion/fear/boredom chưa đo**. |
| Longest same-recorder gap không có mốc mới | 2,110.189 | 2,700.022 | Mốc=objective/reveal/receipt/choice/Ending; navigation/Rest/failed photos/loading/processing vẫn có thể diễn ra. Không phải literal silence/uneventfulness. |
| Meaningful primary checkpoints | Fragment gặp thật; A/B/final N/A | Fragment+A+B+final đều có actual audience/delivery | Khác coverage; không coi baseline unvisited cue là missed scare. |
| Blocking condition/bugs | Bounded control/route-assurance blocker trước L2; chưa chứng minh geometry/controller defect | Không có blocker mới ngăn final hidden Ending trong lượt này | Depth/bearing/NoSubject vẫn là ma sát; completion không chứng minh game không còn bug. |

Longest-gap proxy được tính lại từ raw metadata trong từng recorder, không bắc qua external gap hoặc đếm loaded receipt trùng; [định nghĩa, endpoints và toàn interval list](preparation/milestone-gap-comparison.json). Scalar này chịu ảnh hưởng mạnh từ tool/operator/unknown time, **không được dùng làm căn cứ cho điểm Pacing2**. Điểm nhịp dựa vào actual travel, lặp photo/depth/bearing và Rest đã quan sát; một quãng yên cụ thể gây chờ đợi hay chán vẫn cần người thật.

Obstruction marker, sampled hull loss và physical collision là ba đại lượng khác nhau. Không gọi số marker là số va chạm; không đổi loaded checkpoint refill thành Rest. Final physical arrival còn hull100/energy95.842636 ởday11. Ending controller báo ship/resource0 vì Zone04 controller đã rời scene, không phải chết hoặc cạn tài nguyên.

| Đồng hồ final | UTC / giây sau first-ready |
| --- | --- |
| First initialized Zone04 | 2026-10-28T00:48:25.1750858Z |
| Committed hidden receipt | 2026-10-31T03:53:47.6922445Z / 270,322.517s |
| Settled matching hidden presentation | 2026-10-31T03:53:49.7199696Z / 270,324.545s |
| Full hidden presentation≥16s | 2026-10-31T03:54:05.8115200Z / 270,340.636s |
| Recorded first-ready→full coverage | 13,650.374s |
| Tổng wall của7 recorder / tổng marker gaps | 13,927.212s / 256,690.286s |

UTC là authority raw để đối chiếu; thời gian container video là một đồng hồ khác. No-owner/unmarked idle vẫn unknown; không gọi hết là sợ, bối rối, chán hoặc processing. Input/navigation/UI/activity intervals không exclusive. Các thời gian trên bao gồm detour, ảnh thất bại và replay đã quan sát, đồng thời giữ riêng các external gap.

## Timeline các điểm thực

Baseline: khảo sát/L1 SEND lúc2026-10-26T16:23:46.4559218Z → capture16:24:45.0772793Z → fragment16:25:05.1676246Z → detour/Rest/damage → bounded stop trướcL2 lúc2026-10-27T14:41:29.1227486Z. Không vẽ climax/aftermath giả cho phần chưa gặp.

Hai annotation dưới dùng cường độ1–4 như **giả thuyết thiết kế**, không đo cảm xúc và không nội suy qua gap:

| Baseline đã gặp | Cường độ giả thuyết | Căn cứ / phần chưa biết |
| --- | ---: | --- |
| Bắt đầu khảo sát → L1 PHOTO/SEND/capture | 1→2 | Công việc quen thuộc và kết quả ảnh tự nộp. |
| Fragment có hai tọa độ | 3 | Dữ liệu đặt câu hỏi mới, card thực đã xem được. |
| Detour/Rest/va chạm khi tìm L2 | Không phân loại | Điều hướng chiếm tác vụ; không đổi agent delay hoặc khó dùng thành suspense. |
| Bounded stop trước L2 | N/A climax/aftermath | Chưa gặp các phần sau. |

| Final đã gặp | Cường độ giả thuyết | Căn cứ / phần chưa biết |
| --- | ---: | --- |
| Khảo sát/L1 → readable fragment | 1→3 | Cùng setup, card thực mở đầu mối. |
| L2/explore → Radar/ảnh A/B | 3 | Choice và kiểm chứng đầu mối bằng thiết bị, voice gắn ảnh vừa chụp. |
| Tín hiệu/tint khi lái ngoài arrival radius | 4 | Escalation có ngữ cảnh trước physical payoff, actual voice/tint. |
| Hidden Ending/16s footer change | 3→2 | Kết quả11/11 và footer thay đổi tạo closure; bright blue result screen/large title có thể làm dịu tông. Lingering unease chưa được người thật xác nhận. |
| Travel/Rest, tool processing và external gaps giữa các mốc | Không nội suy | Chỉ biểu diễn phần có evidence; quiet travel có thể tense hoặc boring. |

| Final UTC | Quan sát thật |
| --- | --- |
| 2026-10-30T14:52:34.8478438Z | L1 manual PHOTO/SEND |
| 2026-10-30T15:03:04.3884851Z | L1 capture sau một lần chưa hoàn tất |
| 2026-10-30T15:10:48.0558870Z | Mission Log card đầy đủ; fragment receipt |
| 2026-10-30T16:26:44.2829126Z | L2 manual PHOTO/SEND |
| 2026-10-30T16:27:05.1283898Z | Actual TIẾP TỤC KHÁM PHÁ choice |
| 2026-10-30T16:45:31.2030451Z | Actual Radar reveal A |
| 2026-10-30T16:46:34.8807157Z | Đúng ảnh A và voice; hidden.a receipt |
| 2026-10-30T16:46:51.4722519Z | Manual A SEND |
| 2026-10-30T16:50:08.2282750Z | Actual Radar reveal B |
| 2026-10-31T03:48:19.5785819Z | Đúng ảnh B và voice; hidden.b receipt |
| 2026-10-31T03:48:40.0378202Z | Manual B SEND |
| 2026-10-31T03:52:05.6507927Z | Tín hiệu cuối/tint trong moving Helm |
| 2026-10-31T03:53:47.6922445Z | Physical arrival commit hidden Ending |
| 2026-10-31T03:53:49.7199696Z | Settled matching hidden presentation |
| 2026-10-31T03:54:05.8115200Z | Hidden presentation đạt16.0s |

Actual chart [frame000724](final/continuation-06/frames/frame-000724.jpg) đã hiển thị FINAL SIGNAL—READY · ĐI ĐẾN(1500,150) trước route advice cuối. Physical arrival dùng marker này, không dùng tọa độ final source-only. [B Lab](final/continuation-06/frames/frame-000387.jpg) có gallery24/24 · GoodPhoto trong khi cumulative photo count31; hai số này không bị trộn. [Final tint](final/continuation-06/frames/frame-001470.jpg) xuất hiện khi còn moving Helm ngoài arrival radius. [Early Ending](final/continuation-06/frames/frame-001980.jpg) có outside-survey footer; [late16s aftermath](final/continuation-06/frames/frame-002157.jpg) giữ Ending/11/11, footer còn dấu nhỏ. Đây là rendered delivery, không đo emotional aftermath.

Final voice được quan sát0.085329→5.695986s, không còn vào03:52:09.6861579Z; tint không còn vào03:52:12.5046299Z. Chỉ sau đó driver cancel vì E24 vào03:52:17.1359904Z và ordinary Rest day10→11 vào03:52:21.4337776Z. **Rest không cắt hoặc thử lại final cue trong lượt thật này**; receipt/counter giữ1. Ending có phát lại cùng tên clip trong presentation riêng; Zone04 approach clock được scope riêng để không đếm nhầm thành cue thứ hai. [C6 audit](preparation/final-continuation-06-close-audit.json).

| Cue checkpoint | First actual receipt UTC | Source-clock window ước tính, giây |
| --- | --- | ---: |
| `fragment` | 2026-10-30T15:10:48.0558870Z | 1.615 |
| `hidden.a` | 2026-10-30T16:46:34.8807157Z | 9.047 |
| `hidden.b` | 2026-10-31T03:48:19.5785819Z | 8.990 |
| `final.signal` | 2026-10-31T03:52:05.6507927Z | 5.764 |

Các window là tích phân left-held sampled telemetry, có boundary overrun; không phải exact clip duration, audible duration hoặc human listening. Fragment voice ngắn đã hết trước lúc card hoàn tất; readable checkpoint độc lập có ảnh thật. Fading tail gần trong suốt không được tính là readable evidence.

## Gián đoạn, replay và media

| Boundary | Marker gap, giây | Phân loại có bằng chứng |
| --- | ---: | --- |
| evidence→continuation-01 | 83,330.584249 | Ordinary Continue/checkpoint discontinuity; distance rollback53.584m, cumulative photo-count rollback2. Không phải Rest. |
| continuation-01→continuation-02 | 134,234.904055 | Ordinary Continue/checkpoint discontinuity; distance rollback0.000m, cumulative photo-count rollback1. Không phải Rest. |
| continuation-02→continuation-03 | 0.014618 | Live rotation; state/runtime khớp, không reload/Rest thêm. |
| continuation-03→continuation-04 | 544.364370 | Live compass-photo attempts và Rest day4→5 ngoài recorder; không có timestamp/media thay thế. |
| continuation-04→continuation-05 | 0.012246 | Live rotation; state/runtime khớp, không reload/Rest thêm. |
| continuation-05→continuation-06 | 38,580.405985 | Ordinary Continue sau external gap/new runtime; mọi compared gameplay state khớp. Không chứng minh seamless play. |

C1 stop recorder-destroyed có Zone04 fields rỗng; so sánh resume dùng last initialized state trước đó3.096562s, không dùng cleanup0 để suy chết/photo mất. C5 stop requested-stop vẫn có state thật, sau đó Play dừng vì nguyên nhân chưa rõ; C6 Continue khớp compared state. Không có media thay thế trong gap. Day4→5 ngoài recorder và các ảnh compass ngoài đó giữ lời khai/endpoint riêng, không bịa timestamp. Source-assisted rule check sau L2 NoSubject588.9575m đã làm rõ minimum590m rồi lặn bằng input thật tới595.863m. Tại B, actual Radar contact gần133° và Obstructed photo cùng rules-only source check dẫn tới lời khuyên thử small approach; root intended13m, actual17.526367m bằng normal input rồi GoodPhoto. Hai lần rule guidance không cung cấp tọa độ actor/terrain; route và recovery dùng ảnh/UI thật, không dùng private actor/terrain data để điều khiển hoặc đổi geometry. Kiến thức đó vẫn là confound với người mới. Click-pivot automation error, tool latency và visible-hook capture assistance cũng được giữ, không quy hết cho lỗi UI hay human hesitation.

| Segment | Actual JPEG | Actual PCM, giây | Review media | Sync |
| --- | ---: | ---: | --- | --- |
| evidence | 13315 | 2,699.605 | [video](final/evidence/gameplay.mp4) | Không thấy positive dropped-PCM counter; listener/device uncertainty vẫn còn |
| continuation-01 | 12380 | 2,574.165 | [video](final/continuation-01/gameplay.mp4) | Degraded: loss14.912s, vị trí chỉ bracket |
| continuation-02 | 11937 | 2,416.981 | [video](final/continuation-02/gameplay.mp4) | Không thấy positive dropped-PCM counter; listener/device uncertainty vẫn còn |
| continuation-03 | 13373 | 2,700.032 | [video](final/continuation-03/gameplay.mp4) | Không thấy positive dropped-PCM counter; listener/device uncertainty vẫn còn |
| continuation-04 | 11555 | 2,350.037 | [video](final/continuation-04/gameplay.mp4) | Không thấy positive dropped-PCM counter; listener/device uncertainty vẫn còn |
| continuation-05 | 3473 | 700.651 | [video](final/continuation-05/gameplay.mp4) | Không thấy positive dropped-PCM counter; listener/device uncertainty vẫn còn |
| continuation-06 | 2193 | 465.301 | [video](final/continuation-06/gameplay.mp4) | Không thấy positive dropped-PCM counter; listener/device uncertainty vẫn còn |

**7/7 mux thành công và được ffprobe kiểm tra**, từ ảnh thật giữ VFR tới timestamp kế tiếp và WAV listener thật;960px H264/AAC, cùng presetveryfast. Không interpolation, apad/aresample/async hoặc PCM bù. C1 raw counters chứng minh **1,431,552sample values mất=14.912s**, dù summary destruction reset0. Chỉ bracket được vị trí loss trong58.779522s; listener-only mux segment này **degraded synchronization**, có thể audio lead ảnh14.912s sau loss tới listener boundary sau. Encode thành công không sửa hay chứng minh sync đúng. Listener replacement gaps, DSP/device/block/AAC packet timing và skipped video slots vẫn được giữ trong [05 timeline/quality/SHA manifests](05_Final_Timeline.json). Tổng actual PCM13,906.773s, RMS-34.07dBFS, rail values16; đây là signal statistics, không kết luận headset balance hay human hearing.

File đen030-L2-radar-scan.png là failed Update capture được giữ và loại khỏi sensory evidence; actual recorder Radar frame009683 cung cấp ảnh thật. Không kể file đó thành blackout/scare. [Gaps và phase notes](preparation/final-resume-notes.md), [actual image reviews](preparation/reviewed-final-frames.json).

## Validation và bàn giao

Selected native **33/33 Passed,0fail/skip**,33 tên test duy nhất, gồm17 regressions Zone04/ending,7 delivery cases và9 map cases. Đây là controlled PlayMode tests, không phải toàn suite, standalone build hoặc actual voyage. [XML giữ nguyên](native-validation/TestResults.xml), [native results](native-validation/results.json).

Sau actual Ending, recorder đóng trước khi stop Play; isolation giữ qua quit-save rồi được xác nhậnfalse, save/photo overridesnull. Editor ở clean unpaused Bootstrap và0compile errors: [final Editor evidence](preparation/final-editor-verification.json). **467/467 frozen files khớp;14/14 protected files bằng entry** sau guarded cleanup. Generated font cache đã archive rồi chỉ restore exact entry bytes; old real TestResults được restore, selected33-pass XML vẫn archive riêng.57 non-Unity persistent files, gồm save/photo/log/test XML, bằng entry/resume; Unity analytics tự đổi/purge được backup và khai báo, không copy đè toàn thư mục. User Zone03 edits/deletions/fonts/Docs Deliverables được giữ. [Preservation audit](preparation/final-preservation-20261031.md), [hashes/operations](preparation/final-preservation-20261031.json).

Chỉ thư mục Screenshots rỗng do task tạo được Unity chuyển vào OS trash sau kiểm tra path/GUID và backup meta: [cleanup evidence](preparation/owned-screenshot-folder-cleanup.json). Không commit/push/merge, không mua hoặc cài package. Một cycle đã kết thúc; các giới hạn player experience ở trên được bàn giao nguyên trạng, không tự mở vòng sửa tiếp.
