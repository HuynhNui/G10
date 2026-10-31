# Zone04 — triển khai và kiểm chứng native

**Native33/33 đã đạt; lượt chơi cuối đã chốt riêng ở[05](05_Final_Playtest.md) với actual hidden Ending và đủ16s presentation.** Thiết kế theo[03](03_Horror_Redesign.md), sau[baseline incomplete](01_Baseline_Playtest.md). Đánh giá trước/sau và giới hạn ở[06](06_Comparison.md); không có vòng redesign thứ hai. Các mục bên dưới giữ bằng chứng/giới hạn của giai đoạn native.

## Thay đổi thực tế

- Chart Zone04 có lớp địa hình tĩnh mặc định bật và nút `ĐỊA HÌNH: BẬT/TẮT`. Vàng nhạt biểu diễn đúng ô vật cản; nước vẫn trong suốt. Không thêm vị trí tàu, tuyến đường hoặc marker bí mật. Màu Radar, geometry, tọa độ và chi phí giữ nguyên. Cache một texture mỗi config, giải phóng khi owner bị hủy. `ZoneMapConfig` chỉ thêm API đọc bản sao độc lập; schema/asset không đổi. [Chi tiết](chart-implementation-notes.md).
- Fragment được ghi receipt sau khi card thực hiện đủ hai tọa độ/progress và đọc được qua các frame render ít nhất0,25s. Hidden A/B dùng đúng ảnh đang chọn cùng voice thực tiến ít nhất0,65s. Tín hiệu cuối lưu cơ hội từ chuyển động thật ở approach đã mở, cho phép dừng tàu rồi thử lại. Receipt chính chỉ ghi sau checkpoint tiếp nhận; played receipt cũ vẫn được tôn trọng.
- Đóng audience, pause hoặc mất focus trước checkpoint giữ khả năng thử lại. Lỗi lưu sau khi đã quan sát giữ pending trong cùng phiên và retry publication có backoff, không phát lại attack. Journal/timeline restore loại state tương lai theo snapshot. Đây là checkpoint kỹ thuật, không chứng minh người thật nghe/hiểu trọn cue. [Chi tiết và giới hạn](implementation-notes.md).
- Chỉ environmental sparse-water bed rút nhẹ trong hidden/final primary và recovery4s; engine, controls, warnings và minigame giữ bus hiện tại. Distant structure không chồng vào primary/recovery.
- Wreck L1 dùng cùng texture procedural đã cache, đổi kích thước/vị trí và RGB để tách khỏi foreground. Subject alpha, camera geometry, validation và archive authority không đổi. `WreckReadabilityEnabled=false` là diagnostic không serialize để chụp đối chiếu đúng kiểu cũ.
- Recorder Editor chỉ đọc thêm `PendingCue`/`ExperienceStage`; dữ liệu baseline cũ vẫn được ghi là unknown khi trước đây chưa quan sát được.

## Phạm vi và nguồn tài nguyên

Đổi7 source files trong390 production/config/scene files đã hash trước baseline: photo composition, recorder Editor, controller/state/fragment view Zone04, map config reader và map presentation. Các thay đổi Zone03 tồn tại từ entry được giữ nguyên. Tests có thay đổi riêng, ghi [scope và controlled setup](native-fixture-source-notes.md).

Tái sử dụng8 WAV, catalog/sequences, artwork, EffectDirector và uGUI hiện có. Không mua, tạo audio mới, cài package, thay sinh vật hay sửa scene/map/mission assets. Bốn creature definition vẫn dùng ảnh fallback chung; không tuyên bố đã có creature art riêng. Audio cũ giữ nguyên giới hạn nguồn/license đã ghi trong tài liệu production, gồm yêu cầu noncommercial/attribution của nguồn Free lịch sử.

## Native validation

Unity6000.4.2f1, G10:7890, outer isolation `native-isolation/` cộng mỗi fixture có save/photo riêng. Compiler sau sửa fixture compatibility: **0 lỗi C#**. Kiểm tra source độc lập không thấy lỗi correctness/preservation cần sửa; đây không thay thế native test.

Job PlayMode `6f6f0f1c0a33`: **33/33 passed,0 failed,0 skipped**, chạy271,7s;17 Zone04/ending hiện có +7 delivery mới +9 map. [Request](native-validation/request.json), [results đầy đủ](native-validation/results.json), [NUnit XML](native-validation/TestResults.xml). Đây là selected PlayMode, không phải toàn bộ suite hoặc standalone build. Compile sau test vẫn0 lỗi; Editor tự trở về Bootstrap sạch, stopped/unpaused.

Native lần đầu gặp lỗi compatibility trong fixture (`Canvas.WillRenderCanvases` delegate và `CanvasRenderer.GetMesh()`), đã sửa trước khi chạy. Không có ca runtime thất bại bị ẩn hoặc cộng rerun trùng: job terminal trên có33 tên test duy nhất.

Đối chiếu wreck thật:2064 pixels thay đổi,0 pixel ngoài hợp của hai background rect; GoodPhoto, subject coverage0,155662984 và occlusion0 giống nhau. Cùng texture cache, loose PNG và durable timeline PNG giữ đúng dữ liệu. [Legacy Lab](native-validation/ZoneFourVerticalSliceTests/A-00-legacy-wreck-in-real-lab.png), [candidate Lab](native-validation/ZoneFourVerticalSliceTests/A-01-wreck-photo-in-real-lab.png), [measurement](native-validation/ZoneFourVerticalSliceTests/A-wreck-matched-measurement.txt). Dáng thân/mast mờ đã tách sang khoảng nước bên phải; vẫn tinh tế và chưa đo khả năng nhận ra của người thật.

Actual listener mix có dữ liệu và không clipping ở các take native; ví dụ final-signal5,952s,48kHz stereo, RMS−32,03dBFS/peak−14,78dBFS. Engine0,5 và vent0,39 giữ nguyên qua environmental withdrawal; category Anomalies mute không tắt engine. [Metrics](native-validation/ZoneFourAudioProductionTests/M3-final-signal-listener.json), [mix/gain evidence](native-validation/ZoneFourAudioProductionTests/audio-m3-runtime-evidence.json). Các take dùng gains tạm của fixture rồi restore; không phải so sánh âm lượng perceptual baseline/final ở thiết bị người chơi.

Ảnh native đã có: [địa hình bật](native-validation/ZoneMapPlayModeTests/chart-terrain-on.png) / [tắt](native-validation/ZoneMapPlayModeTests/chart-terrain-off.png). Chú giải/nút cùng header, không đè coordinate readout; gold contour tách các đường tối khỏi cyan decoration rõ hơn. Tooltip POI hiện có che một phần chart khi chọn location, cần bỏ hover/selection khi quan sát tuyến. Không suy từ ảnh này ra cảm giác sợ hoặc tốc độ hoàn tất hành trình.

## Bảo toàn và giới hạn

Trước native,196 persistent files được đối chiếu với resume backup: gameplay/save/photo bytes không đổi; chỉ Unity Insights session metadata khác. Sau native, gameplay/save/photo vẫn đúng; TestResults.xml đã được archive ở trên. Unity tự thay session metadata và dọn archived analytics cũ; mọi bản gốc có verified backup. Font audit trước native cho thấy chỉ thêm968 feature records trong `m_FontFeatureTable`, không xóa record cũ và không đổi top-level setting khác; cache sẽ được đối chiếu/bảo toàn lúc cleanup.13/14 protected assets giống entry, ngoại lệ là font cache nói trên. [Audit](preparation/font-cache-audit.json), [pre-native check](preparation/pre-native-persistent-check.json), [post-native check](native-validation/preservation-check.json).

Không đảm bảo exactly-once xuyên crash/reload khi storage không ghi được: delivery cục bộ chưa durable có thể phát lại từ saved opportunity. Rare Radar seed issue trong diagnosis chưa quan sát ở baseline và không sửa trong scope này. Human headset review, recognition, reading speed và fear còn chưa được đo. Native setup có controlled state/position và không được dùng làm thời gian hoặc bằng chứng hoàn thành actual-input voyage.
