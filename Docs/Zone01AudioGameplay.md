# Zone 01 — nối gameplay với gói âm thanh

Cập nhật 2026-10-21, theo kịch bản Miền Tảo Hát V1. Người dùng đã chọn cập nhật tiến trình Zone 1 theo kịch bản mới; chỉ SFX/ambient, không tạo hay phát thoại Lily mới.

## Tiến trình đã cài vào scene

- Rạn Tảo Đỏ: `(440, 115.71)`, 230 m. Chụp ảnh chưa hoàn thành nhiệm vụ; phải SEND qua Photo Lab. Sau SEND mới được CAPTURE vật liệu tảo phát sáng. Quần thể tảo vẫn hiện diện sau khi thu một mẫu.
- Thềm Biển Sâu: `(1000, 732.86)`, 460 m. Chụp và SEND sinh vật thứ hai.
- Sau hai ảnh SEND và CAPTURE, Computer → Upgrade → Modules cho phép INSTALL BIO LAMP. Giao dịch tiêu thụ đúng một mẫu, ghi objective/world flag/Zone02 unlock cùng bản lưu trước khi thông báo hoàn tất và phát âm thanh.
- Lối ra giữ nguyên `(1875, 680)`. Cổng chuyển vùng hiện có vẫn yêu cầu thực sự di chuyển vào vùng Exit.
- Địa điểm thu bản vẽ Emma và gate Pressure Hull không còn là nhiệm vụ của Zone 1 mới. Định nghĩa nội dung cũ vẫn được giữ để đọc Cargo/save cũ. Không chuyển vật phẩm cũ thành mẫu tảo và không tự lắp Bio Lamp.
- Hai objective ảnh giữ ID cũ `Z1_L1_PHOTO` / `Z1_L3_PHOTO` để tiếp tục ghi nhận các ảnh SEND đã có. Site thứ hai mới có location ID `Z1_L2`, POI `zone01-north`.

## Trigger và vòng đời

| Cue | Gameplay thực tế |
| --- | --- |
| S01/S02 | NEW GAME đặt cờ mở đầu. SceneFlow giữ input lock trong cảnh 35 giây; màn hình khởi động, tiếp nước, mất ổn định, va chạm, khoảng lặng 20–21 s, reboot rồi cabin. Sau khi xong, cờ được ghi vào timeline và checkpoint đầu ngày để chết trong ngày 1 không phát lại cảnh hạ cánh. Continue của save cũ không tự phát prologue. |
| Tutorial | Máy phụ, thông gió, nước hoạt động bình thường. Servo/đổi độ sâu dùng loop theo trạng thái chuyển động thực của tick điều khiển hiện tại; giữ nút thì phát qua biên clip, thả nút hoặc chạm giới hạn thì fade 0.08 s. Adapter đọc ở LateUpdate sau Helm xử lý input/phanh; không dùng one-shot/cooldown 3 s. Lực đẩy theo tốc độ. Chưa học quét radar thì chưa có âm sinh học/anomaly. Giới thiệu hiện bằng thông báo hệ thống, không thêm modal thoại. |
| E01/E02 | Bốn lớp tảo tăng theo khoảng cách tới L1 (bắt đầu 240 đơn vị, rõ nhất trong 85), nhân suy giảm theo chênh độ sâu 120 m. Khi tàu dừng, lớp lực đẩy fade xuống. Radar chỉ thêm một biến thể sinh học nhỏ; không sửa dữ liệu radar. |
| SEND | Chỉ sự kiện SEND đã commit thành công phát tín hiệu truyền dữ liệu. SEND lỗi, chụp lỗi hoặc click lại không phát báo thành công. Radar/camera dùng clip ElevenLabs riêng của Zone 1 qua AudioManager. |
| E03 | CAPTURE đang hoạt động tại L1 mới phát loop thiết bị. Thành công, thất bại, hủy và đổi timeline đều dừng loop. Thành công nhận mẫu mới phát cộng hưởng; các lớp rừng tảo tiếp tục. Tiếng cào ngẫu nhiên sau CAPTURE bị tắt riêng cho Zone 1. |
| E04 | Rời bán kính nghe/depth band thì các lớp sinh học nhỏ dần. Nước/máy vẫn chạy. |
| E05 | Đã CAPTURE L1, chưa lắp Bio Lamp, đã ra ngoài vùng hát, cách L1 hơn 310 đơn vị, đang điều khiển và di chuyển ở Helm, không modal/direct interaction, chưa phát trong timeline. Cần ít nhất 8 giây di chuyển trong nước yên; thêm điều kiện gần `(745,295)` trong 110 đơn vị hoặc đi thêm 330 đơn vị từ lúc thu mẫu. Clip đã bake ba tiếng gõ cách 0.6 s, lệch phải. Theo yêu cầu bổ sung, `HullKnocks` chạy ba nhịp rung ngắn và tối màn hình nhẹ tại 0.12 / 0.72 / 1.32 s, theo chính vị trí phát audio. Không pan phải lần hai hoặc thêm bất thường radar. Không bù sự kiện gõ đã bỏ lỡ vào đoạn cuối sau khi lắp đèn. |
| E06 | Creak đầu tiên chờ chuyển động lặn thật vào vùng sâu từ 380 m; nếu vừa có ba tiếng gõ thì giữ thêm khoảng yên 8 s sau đó, không chồng âm sinh vật sâu. Cue đầu có receipt timeline; các lần tiếp theo cách 28 s khi đang di chuyển tại Helm. Không gây damage hay giả cảnh báo Hull. |
| E07 | Đến gần L2 trong 150 đơn vị, lệch độ sâu không quá 70 m và đang di chuyển: âm trầm ngắn phát một lần. Dừng, rời Helm hoặc dùng camera thì dừng ngay, không khởi động lại cùng timeline. |
| E08 | INSTALL Bio Lamp đã lưu thành công mới phát composite 4 s. Ánh sáng camera tăng từ 1.5–2.2 s; chỉ lớp sinh học hiện có bị ngắt ở 4.2–4.65 s rồi trở lại. Xa tảo không cưỡng ép tiếng hát. Nước/máy không ngắt theo. |
| E09 | Có Bio Lamp, đang di chuyển ở Helm và còn cách Exit không quá 260 đơn vị: ba nốt xa phát một lần, nốt cuối dài; không thêm gõ/cào/bóng người. |
| Zone02 | SceneFlow fade các lớp tàu trong khoảng 1 s khi chuyển vùng, dùng transition hiện có và tiêu đề Zone02, sau đó mở tiếng rền thấp 8 s. Không phát khi chỉ xem bản đồ hay khôi phục Journal. |

## Kiến trúc và dữ liệu

`ZoneOneAudioController` chỉ nối trạng thái gameplay với EffectDirector. Scene đã bind 32 EffectDefinition audio của gói Zone01, visual cho hạ cánh/reboot và sequence `HullKnocks`; không cần kéo asset thủ công. Voice pool, mixer categories, mute và thiết lập screen shake tiếp tục dùng hệ thống cũ. Clip sinh học và máy tách lớp, không phát các reference mix 35 giây thay cho điều phối gameplay.

Va chạm địa hình thật đi qua `ZoneNavigation.TerrainImpact` → `CabinFeedbackController` → `hull.impact`. Definition dùng clip ElevenLabs mới `sfx_terrain_impact.wav` ở nhóm Impacts, gain/pitch/rung theo vận tốc va chạm. Một lần tiếp xúc chỉ phát một lần; phải lùi khỏi vách mới phát lại. Hiệu ứng ngắn không cắt đuôi âm thanh. Đây là tiếng va chạm vật lý, không phải sự kiện kinh dị ngẫu nhiên.

Ba tiếng gõ dùng một sequence chứa audio và ba visual cue, bám theo `AudioSource.time` để không chạy trước âm thanh khi frame bị khựng. Rung tác động lên CabinFrame cùng các hitbox, mỗi nhịp 0.24 s, biên độ tối đa 7 pixel; lớp tint đen tối đa 10%, tự trả về. Tắt Screen Shake chỉ bỏ chuyển động. Pause, modal, đổi timeline, tắt component và chuyển cảnh hủy cả sequence/voice; resume không phát lại receipt đã lưu. Ba tiếng gõ không trừ Hull, không đổi đèn cabin hoặc radar.

`ZoneOneAudioSetup.ConfigureHullFeedback` là installer hẹp cho phần này, được gọi từ installer Zone01 đầy đủ. Kết quả kiểm thử và ảnh hiện tại: `Docs/QA/Zone01HullFeedback/`.

`ExpeditionZoneState.zoneOneAudio` lưu receipt của cue quan trọng và quãng đường tại thời điểm thu mẫu. Snapshot/checkpoint sao chép dữ liệu này; handles, AudioSource, timer creak và các giá trị trình bày không được ghi vào save. Restore dừng các handle thuộc adapter này rồi tính lại từ gameplay; không xóa hiệu ứng của hệ thống khác. Thêm trường tùy chọn trong định dạng save v5; save cũ được normalize với danh sách cue rỗng.

Bio Lamp có giao dịch mission/inventory/save có rollback. Notification đến tutorial chỉ được công bố sau commit, tránh dạy xong nâng cấp hoặc phát tiếng thành công trước khi bản lưu bền vững. Bio Lamp không tăng Hull, Energy hoặc độ sâu tối đa của tàu.

**Phạm vi Zone01:** giữ trần lặn cơ bản 500 m; Bio Lamp không nâng cấp thân tàu. Công việc Zone02 ngày 2026-10-22 theo kịch bản mới đã đưa mục tiêu giáp cứng về 460 m và chuyển Pressure Hull sang Zone02 (500 → 750 m). Điều này giải quyết điểm nghẽn 510 m được ghi nhận ở lần hoàn thiện Zone01 trước đó. Xem `Docs/Zone02EnvironmentalHorror.md`.

Hình ảnh đèn ngoài trong phạm vi lần nối này là lớp tối cục bộ của camera dịu đi khi lắp đèn, dùng UI sẵn có; chưa thêm artwork tia đèn hay môi trường mới. Âm sắc và cân bằng cảm nhận vẫn cần nghe thử trên thiết bị thực tế.

Đợt hoàn thiện màn hình bổ sung terminal mở đầu có chữ hiện dần, telemetry, nhiễu lúc hạ cánh, rung nhìn thấy được, blackout/reboot và fade cabin; Bio Lamp có readout tiến trình trong Upgrade. Xem `Docs/Zone01Cutscene.md` và ảnh/test tại `Docs/QA/Zone01Cutscene/`.

## Authoring

Mở scene Zone01, chọn menu **G10 → Zone 1 → Install Singing Forest Gameplay and Audio** để cài lại các reference. Công cụ dùng các GUID đang có, giữ những catalog khác, thêm vật liệu mới vào GameplayCore, lưu scene bằng Unity Editor. Menu story cũ chuyển tiếp sang công cụ mới khi asset Zone01 dùng gate Bio Lamp. Mission config builder cũng tạo nhiệm vụ Zone1 mới.

Gói WAV, provenance, điều kiện giấy phép và báo cáo kiểm tra audio nằm ở `Docs/Zone01AudioAssets.md`. Quyền sử dụng ElevenLabs Free vẫn là phi thương mại; việc nối gameplay không thay đổi giấy phép của file đã tạo.

## Xác minh

Kết quả test và kiểm tra Editor của lần tích hợp được ghi tại `Docs/QA/Zone01AudioGameplay/`. Test dùng các đường dẫn save/photo tạm riêng, không reset timeline thật của người dùng. Không commit/merge/push.

Sửa vòng đời âm điều khiển ngày 2026-10-21: kết quả kiểm thử nguồn AudioSource thực (giữ xoay/lặn/nổi qua hai vòng clip, nhả/bấm lại, giới hạn độ sâu, chuyển bảng, pause, focus, khóa chuyển cảnh và hết năng lượng) cùng regression nằm ở `Docs/QA/Zone01ControlAudio/`. Đánh giá nhịp ban đầu và trạng thái triển khai ở `Docs/Zone01PacingReview.md`.

Sáu nút giữ điều khiển tàu (tiến, lùi, xoay trái/phải, nổi/lặn) không còn nhận tiếng click mặc định từ AudioManager.AutoBindAllButtonsInActiveScene. Chúng được nhận diện bằng CabinPointerTarget.IsHelmControl; âm vận hành tiếp tục theo chuyển động thực qua EffectDirector. Cơ chế nhận diện theo component cũng áp dụng cho các nút được editor installer tạo lại.

Phần hoàn thiện ảnh chụp/ánh sáng được mô tả tại `Docs/Zone01Observation.md`. Theo xác nhận của người dùng, màn hình quan sát trực tiếp đã gỡ; luồng hiện tại là chụp nhanh → Photo Lab → SEND. Kết quả hiện tại nằm trong `Docs/QA/Zone01QuickPhoto/`; thư mục `Docs/QA/Zone01Observation/` là lịch sử của phương án đã gỡ.
