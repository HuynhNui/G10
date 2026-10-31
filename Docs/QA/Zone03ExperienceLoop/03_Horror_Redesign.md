# Zone03 — một vòng redesign

Quyết định sau baseline tới Zone04 và [audit đã đóng](02_Experience_Audit.md). Chỉ thực hiện một vòng cải tiến rồi final; không tự bắt đầu redesign thứ hai.

## Hướng trải nghiệm

Giữ bản sắc Zone03: tàn tích khoáng vật → âm máy của chính tàu trả lời muộn → dụng cụ có dấu hiệu khó giải thích → đi sâu hơn mới thấy phản chiếu → sinh học/điện → tự chế tạo và phá đá → khoảng thoát yên. Trọng tâm là sự bất định của quan sát, không xác nhận quái vật đang theo tàu và không làm sai dữ liệu nhiệm vụ.

Baseline đã thấy đủ các peak. Mục tiêu không phải tăng số scare hay hứa rút ngắn7393s wall time bị nhiễu. Mục tiêu là giao nội dung vào ngữ cảnh phù hợp hơn, và không tiêu mất một cơ hội chỉ vì người chơi đổi việc trước khi thấy/nghe phần chính.

## Điều phối được chọn

| Nhịp | Điều kiện/đổi mới | Checkpoint và recovery |
|---|---|---|
| Nền đá/nước/máy | Giữ art, lớp âm và acoustic passage100distance sau scan gốc. | Quiet gate chỉ cộng khi không có sequence/chướng ngại thao tác; reset sau drama/cancel thay vì cộng trong drama. Không tăng âm lượng hoặc tần suất scare. |
| Động cơ trễ | L1 capture thật + sustained propulsion rồi dừng thật, đúng Helm. Việc chuyển panel tự phanh không phải cơ hội đủ để tiêu cue. | Chưa ghi engine receipt ở launch. Real reflected voice tiến ít nhất0.35s trong audience hợp lệ mới ghi. Ngắt trước đó giữ cơ hội sống/re-eligible; không ghi receipt giả từ elapsed timestamp. |
| False Radar | Sau SEND L2, quan sát một scan thật và lưu `radar.opportunity` cùng distance scan gốc ngay trong cửa sổ scan. Việc arm độc lập quiet gate. | Marker opportunity không phải delivered. Sau quiet và đúng Radar mới phát; đúng procedural preset hiện liên tục tối thiểu0.25s và real echo voice tiến mới ghi `radar`. Đổi panel/pause trước checkpoint thì dừng handle, chờ audience/quiet để thử lại; không Scan lần nữa, không tiêu charge nữa. Opportunity theo save/Continue/Journal/Rest bình thường. |
| Phản chiếu | Vẫn cần radar delivered và100distance thật. **Đạt depth>=600m để ghi nhớ cơ hội deep-passage**, rồi phát trên Helm khi không đang chỉnh lái/chỉnh sâu; không khóa điều khiển khi đã phát. | Bóng trên bề mặt Reflection thật và accompanying voice cùng đạt checkpoint, exposure liên tục tối thiểu0.25s mới ghi receipt. Panel/modal/pause trước đó không tiêu cue. Sau khi cơ hội đã kiếm được ở600m, việc lên590m để phá đá không xóa retry. Di chuyển thẳng vẫn được, không ép đứng yên chờ scare; không phát sau rock destroyed. |
| Mẫu, assembly, demolition | Giữ sequence/âm/visual hiện có, craft/install/rock authority giữ nguyên. | Sửa câu mô tả tiêu hao theo đúng action craft hoặc install. Giữ10s lock/1.5s reveal, sound tail, calm exit. |

Checkpoint là quan sát kỹ thuật về voice/preset thực, không chứng minh nhận thức của người. Không dùng `IsActive==false` để suy ra completed, vì đó cũng có thể là cancel. Không credit cả Tick delta trước lần đầu quan sát preset. Chỉ một primary delivery pending tại một thời điểm; busy/quiet ngăn burst.

## Save và lifecycle

Ưu tiên controller Zone03 hiện có, EffectDirector/EffectSequence và pooled voices. Không xây global horror framework, không đổi Director nếu API hiện có đủ. Existing played-list chứa thêm `radar.opportunity` và `reflection.opportunity` (cơ hội đạt độ sâu, chưa phải bóng delivered), không cần schema/version mới. API commit có seam optional observed distance để lưu scan anchor chính xác, bao gồm0; delivered radar không được dời anchor theo lúc retry.

Trước checkpoint: mất audience/safe state dừng owned sequence, reset exposure/quiet, chưa delivered receipt. Sau checkpoint mà save fail: giữ `deliveredAwaitingCommit` trong đúng live timeline và retry publication có khoảng cách; không phát lại nội dung đã giao. Khi publication thành công, counter tăng một lần. Continue sau save thành công không replay. Nếu lưu delivered thất bại rồi process/timeline reload trước lần lưu thành công, chỉ opportunity còn bền; không thể hứa exactly-once trong trường hợp đó. Giữ LastError và ghi giới hạn này.

Legacy receipt của bản cũ vẫn được tôn trọng, không đổi anchor hoặc replay, nhưng không gán ngược ý nghĩa checkpoint mới. Reset/disable/terminal zone chỉ cleanup owned transients. Không tạo objective, contact, tài nguyên hoặc tác động chi phí từ presentation.

## Audio/visual và nguồn tài nguyên

Đã xem frame thật của Radar, kính Helm, frame mẫu07 có telemetry pulse và cấu trúc listener capture. Bộ hiện có18WAV,30definitions/6sequences và shader native đã đủ bề mặt chuyển động, âm phản hồi và cao trào. Chọn **tái sử dụng**, không tạo thêm tiếng/quái vật/overlay: vấn đề chính được chứng minh là điều phối. Độ nhận biết pulse mẫu07 cần người chơi đánh giá trước thay art. Giữ inventory/provenance tại `Assets/_Project/Audio/Horror/Zone03/` và `Docs/Zone03CinematicProduction.md`: audio Free ElevenLabs có giới hạn phi thương mại/attribution như nguồn đã ghi; không thay license, mua credit hay phát sinh generation. Không thêm package/library mới; không có nghiên cứu dependency cần thiết.

## Kế hoạch kiểm chứng

1. Native compile; chọn tests mission/manualSEND/capture/material/craft/install, rock reveal/lock/physical exit.
2. Adapt natural engine/radar/reflection tests sang checkpoint thật, không assert receipt ngay sau Tick. Kiểm tra depth gate và không ăn cơ hội khi rời Helm.
3. Một focused test scan→đóng audience trước peak→save/Continue→mở lại→delivered không charge mới; một failed-publication test phân biệt opportunity write và delivered write, không replay trong phiên khi delivered save fail.
4. Pause/Rest/Journal/legacy state và visual/audio cleanup theo vùng thay đổi; roundtrip opportunity/zero anchor. Các placement/refill trong functional fixture chỉ để cô lập điều kiện, không dùng làm bằng chứng trải nghiệm.
5. Final từ legitimate Debug Zone03 tương đương, normal inputs/time/resources tới physical Zone04, raw frames/PCM thật. Tuyến đã học và thời gian công cụ thay đổi phải ghi là confound. So vị trí/ngữ cảnh peak và delivery, không hứa cải thiện tốc độ hay nỗi sợ.

Không đổi maps, missions, exits, capture requirements, craft/install cost, return gate, fonts hoặc unrelated work. Baseline/user save đã có byte backups. Tiếp tục outer isolation cho native tests, frozen Assets trong final voyage, kiểm toàn bộ manifest sau mỗi run.
