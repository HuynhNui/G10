# Zone03 — implementation và native validation

Thực hiện theo [03_Horror_Redesign](03_Horror_Redesign.md) sau baseline/audit. Không bắt đầu vòng redesign thứ hai. Maps/missions/exits/resources và authored return gate giữ nguyên.

## Thay đổi

- `ZoneThreePresentationController`: một primary delivery pending; checkpoint dựa real pooled voice tiến triển và đúng procedural surface đang Visible. Engine có0.35s exposure; Radar/Reflection có0.25s liên tục, không lấy Tick delta trước lần quan sát đầu để suy ra đã giao. Audience/focus/modal/pause cancel trước checkpoint không tiêu delivered receipt. Save fail sau checkpoint chỉ retry publication cách1s trong cùng live timeline.
- `ZoneThreePresentationState` và `ExpeditionLoop.CommitZoneThreeCue`: lưu `radar.opportunity` từ scan thật và `reflection.opportunity` từ passage đạt600m trong existing played-list. Chúng là cơ hội, không phải scare delivered. Optional observationDistance giữ anchor scan gốc kể cả0; legacy delivered receipt vẫn được tôn trọng. Không đổi schema/version. Radar retry không gọi Scan hoặc tiêu charge; reflection retry sau lần đạt600m có thể giao trên chặng590m tới rock.
- Reflection chỉ bắt đầu khi đúng Helm và không đang chỉnh lái/chỉnh sâu; điều khiển vẫn hoạt động khi sequence đã bắt đầu. Quiet gate tính từ sau sequence/cancel. Không phát thêm scare khi đã phá đá.
- `ZoneOneStory.ProgressionActionDescription`: nói đúng tiêu hao tại CRAFT; INSTALL không có materials nói rõ không cần thêm. Không thay action/cost.
- Editor-only recorder quan sát Zone03 surfaces/rock/voice/counters và PendingCue/ExperienceStage; input driver vẫn đưa phím/chuột qua InputSystem. Script tổng hợp có `--zone Zone03`, defaultZone02 giữ nguyên. Baseline fields chưa quan sát stage ghi `unobserved`, không điền ngược bằng state final.

Không đổi EffectDirector, sequence assets, shader, audio, art hoặc package. Tái sử dụng18WAV/30definitions/6sequences native hiện có; inventory/license giữ như03. Không generation, mua credit, commit, merge hoặc push.

## Kiểm chứng native

Unity6000.4.2f1/AnkleBreaker port7890. Native refresh/compile thành công. [Request15case](native-validation-request.json), [kết quả đầy đủ](native-validation-results.json), [NUnit XML thực](native-validation/TestResults.xml). Job `178323075784`: **15passed,0failed,0skipped**, 2026-10-26T07:07:36.7078993Z→07:10:47.2774276Z; testduration187.45s **không phải thời gian chơi**.

Bổ sung đúng một case lifecycle sau khi final đã đóng, không đổi production: `RestAndJournalRestoreKeepUndeliveredRadarOpportunityAndOriginalAnchor`, job `ddffe685d349`. [NUnit XML](native-validation/RestJournal-TestResults.xml) ghi **1passed,0failed**,08:01:38Z→08:02:09Z,30.8312746s; [summary khôi phục](native-validation/rest-journal-results.json). Khi tiếp tục công việc, cache job MCP đã hết hạn; kết quả lấy từ XML native còn nguyên, không chạy lại để thay lịch sử. **Tổng16case khác nhau passed**, không dùng số test cũ của dự án để cộng vào.

| Nhóm | Coverage thực |
|---|---|
|10 ZoneThreeCinematicTests|Natural capture/propulsion stop; real scan/distance; deferred checkpoint; panel brake/focus; interruption→Continue→retry không charge mới; opportunity và delivered publication failures; zero anchor/legacy roundtrip; deep opportunity→ascent590→pause/retry; Pause/Continue; native Radar/Helm/PhotoLab pixels/occlusion; demolition audio seek/reveal/cleanup.|
|2 ZoneThreeMissionTests|PHOTO cần SEND, L2 không CAPTURE, L1/L3 cấp materials, chỉ craft tiêu một lần, install không tiêu thêm; failed craft publication rollback.|
|2 RockBreakerPlayModeTests|Marker/radius/depth,10s lock/1.5s terrain reveal, commit một lần, NoShake/pause/interruptedContinue giữ rock state không replay.|
|1 ZoneExitFlowPlayModeTests|Không bỏ qua rock; chỉ physical entry qua lối thật sau destruction mới chuyển Zone04.|

Fixture dùng temp saves, placement/refill, direct action/preview để cô lập điều kiện. Đó là functional tests, không phải input voyage, pacing hay bằng chứng nỗi sợ. Output fixture đổi sang `native-validation` để không ghi đè QA cinematic cũ. Native screenshots/JSON/actual listener WAV ở thư mục đó. No-audio-listener log lúc chuyển/teardown scene không được biến thành kết luận mix ở gameplay; final recorder sẽ kiểm listener/gaps riêng.

## Preservation và giới hạn

Outer isolation được bật trước native runner; đã dừng về Bootstrap clean, overrides save/photo trở lại null. Kiểm toàn bộ152persistent-entry files: game saves/photos nguyên vẹn; runner thay TestResults.xml và Unity thay một analytics event. Đã lưu XML mới vào report, rồi phục hồi2file từ byte backups: [152/152 khớp SHA256](native-validation/preservation-restored.json).13protected mission/map/scene/font/settings files khớp entry trước native; sẽ kiểm lại sau final. Bốn performance resource deletions có từ entry vẫn giữ nguyên.

Đã chọn coverage theo vùng thay đổi, không chạy lại toàn bộ suite. Phát hiện thiếu coverage Rest/Journal khi đọc lại fixture sau run15pass; đã chuẩn bị patch ngoài Assets trong lúc final chạy, chỉ áp dụng và chạy riêng sau final. Case bổ sung qua: opportunity/anchor giữ qua Rest trong current/dayStart/Journal với charge đúng trước/sau refill; delivery không Scan mới; rollback bỏ future radar/reflection marker, dừng owned tail; retry một lần theo timeline khôi phục rồi Continue không replay. Không đổi production hoặc mở redesign thứ hai. Chưa đo target-hardware GPU/CPU hoặc standalone build. Không thêm render layer/material/loop mới; pixel/cleanup và NoShake regressions qua không thay phép đo performance. Human fear, mức che tiếng, chất âm và độ nhận biết pulse cần người chơi kiểm chứng.

Kiểm cuối sau cả hai lượt và native runs: [152/152 persistent-entry files và13/13 protected-entry files khớp SHA256](preservation-final.json). Unity tự dọn68 analytics files và cập nhật một session event; native runner ghi TestResults.xml. Đã phục hồi70file đó từ byte backups đã xác minh, sau khi giữ XML mới trong report. Gameplay save/photo không khác entry. Fonts với thay đổi có từ trước, missions, maps, scenes và TimeManager giữ nguyên byte; bốn resource deletions có từ entry vẫn giữ. [162production files frozen xuyên final](final/frozen-source-check.json). Editor kết thúc Bootstrap clean, stopped, save/photo overrides null.

Nếu delivered publication thất bại rồi process/timeline reload trước khi save thành công, opportunity còn lại có thể cho retry; không hứa exactly-once qua failed durable write+reload. Final voyage tiếp theo sẽ dùng source frozen và normal input tới Zone04, không sửa giữa lượt.
