# Zone 01 — Miền Tảo Hát: gói SFX và ambient

Tạo ngày 2026-10-21 theo kịch bản Story & Event Design V1. **Không tạo thoại Lily**, theo lựa chọn của người dùng. Không tạo nhạc nền, giọng hồn ma, tiếng cào hoặc bất thường radar cho Zone 1.

## Nội dung đã tạo

- **25 nguồn âm mới** qua ElevenLabs `eleven_text_to_sound_v2`, `prompt_influence=0.6`, `mp3_44100_128`.
- **40 WAV thực tế**: 32 clip dùng trong game, 6 reference mix, 2 nguồn để chỉnh sửa. Các bản ghép/tái sử dụng không phát sinh API request.
- WAV stereo 44.1 kHz, PCM 16-bit. Có AudioImporter metadata PCM/DecompressOnLoad, preload, không ép mono/normalize lại.
- 32 `EffectDefinition` chỉ có audio, tại `Assets/_Project/FeedbackEffects/Zone01`. Không có shake, flicker, overlay, screen interference hay silhouette trong các definition này.
- `generation-requests.json`: 25 prompt và thông số chính xác. Mỗi WAV có `.provenance.json`, hash nguồn/thành phẩm, recipe xử lý, cấp phép và đo peak/RMS.

Lần tạo gói âm thanh ban đầu chỉ chuẩn bị asset và cue sheet. **Đợt nối gameplay ngày 2026-10-21 đã cài trigger vào scene**, cập nhật tiến trình hai địa điểm/Bio Lamp và lưu cue theo timeline/Journal. Chi tiết triển khai, điều kiện thực tế và kiểm tra Editor: `Docs/Zone01AudioGameplay.md`. Catalog JSON vẫn giữ bản thiết kế âm thanh để tham khảo.

## Chi phí và giấy phép

25 request SFX thành công, **1.642 credit**; không retry, không TTS. Quota từ **7.310 xuống 5.668**, đối chiếu cả header `character-cost` lẫn subscription API. Không bật overage hoặc mua gói. Các asset tái sử dụng từ lần tạo ElevenLabs trước được ghi riêng và không tính lại vào chi phí này.

Tài khoản tạo âm là **Free**: audio chỉ dùng phi thương mại, cần attribution khi công bố. Không dùng các bản này cho phát hành thương mại trước khi có quyền phù hợp. [Chính sách ElevenLabs](https://help.elevenlabs.io/hc/en-us/articles/13313564601361-Can-I-publish-the-content-I-generate-on-the-platform).

## File dùng trong game

| Asset | Thời lượng | Kiểu | Đường dẫn |
|---|---:|---|---|
| `ambient_submarine_engine` | 19.96 s | Loop | `Assets/_Project/Audio/Zone01/Machinery/ambient_submarine_engine.wav` |
| `ambient_algae_harmonics` | 19.96 s | Loop | `Assets/_Project/Audio/Zone01/Ambience/ambient_algae_harmonics.wav` |
| `intro_descent_engine` | 12.00 s | One-shot | `Assets/_Project/Audio/Zone01/Intro/intro_descent_engine.wav` |
| `intro_water_impact` | 8.00 s | One-shot | `Assets/_Project/Audio/Zone01/Intro/intro_water_impact.wav` |
| `intro_descent_instability` | 7.00 s | One-shot | `Assets/_Project/Audio/Zone01/Intro/intro_descent_instability.wav` |
| `intro_landing_accident` | 3.00 s | One-shot | `Assets/_Project/Audio/Zone01/Intro/intro_landing_accident.wav` |
| `sfx_system_boot` | 1.00 s | One-shot | `Assets/_Project/Audio/Zone01/Interface/sfx_system_boot.wav` |
| `sfx_intro_warning` | 1.00 s | One-shot | `Assets/_Project/Audio/Zone01/Interface/sfx_intro_warning.wav` |
| `ambient_cabin_ventilation` | 15.96 s | Loop | `Assets/_Project/Audio/Zone01/Machinery/ambient_cabin_ventilation.wav` |
| `ambient_propulsion_thrust` | 11.96 s | Loop | `Assets/_Project/Audio/Zone01/Machinery/ambient_propulsion_thrust.wav` |
| `ambient_water_current` | 15.96 s | Loop | `Assets/_Project/Audio/Zone01/Ambience/ambient_water_current.wav` |
| `ambient_algae_glass_tremors` | 11.96 s | Loop | `Assets/_Project/Audio/Zone01/Ambience/ambient_algae_glass_tremors.wav` |
| `sfx_steering_servo` | 3.00 s | One-shot | `Assets/_Project/Audio/Zone01/Machinery/sfx_steering_servo.wav` |
| `sfx_depth_pressure_adjustment` | 3.00 s | One-shot | `Assets/_Project/Audio/Zone01/Machinery/sfx_depth_pressure_adjustment.wav` |
| `sfx_capture_mechanism` | 7.96 s | Loop | `Assets/_Project/Audio/Zone01/Machinery/sfx_capture_mechanism.wav` |
| `sfx_radar_scan_ping` | 1.76 s | One-shot | `Assets/_Project/Audio/Zone01/Interface/sfx_radar_scan_ping.wav` |
| `sfx_camera_shutter` | 0.60 s | One-shot | `Assets/_Project/Audio/Zone01/Interface/sfx_camera_shutter.wav` |
| `sfx_data_transmit_success` | 1.00 s | One-shot | `Assets/_Project/Audio/Zone01/Interface/sfx_data_transmit_success.wav` |
| `sfx_hull_pressure_creak` | 2.48 s | One-shot | `Assets/_Project/Audio/Zone01/SFX/sfx_hull_pressure_creak.wav` |
| `sfx_deep_specimen_low_tone` | 2.76 s | One-shot | `Assets/_Project/Audio/Zone01/SFX/sfx_deep_specimen_low_tone.wav` |
| `sfx_algae_capture_resonance` | 3.00 s | One-shot | `Assets/_Project/Audio/Zone01/SFX/sfx_algae_capture_resonance.wav` |
| `sfx_biolamp_latch` | 0.68 s | One-shot | `Assets/_Project/Audio/Zone01/SFX/sfx_biolamp_latch.wav` |
| `sfx_biolamp_power_rise` | 2.00 s | One-shot | `Assets/_Project/Audio/Zone01/SFX/sfx_biolamp_power_rise.wav` |
| `ambient_singing_algae` | 19.96 s | Loop | `Assets/_Project/Audio/Zone01/Ambience/ambient_singing_algae.wav` |
| `ambient_deep_water` | 19.96 s | Loop | `Assets/_Project/Audio/Zone01/Ambience/ambient_deep_water.wav` |
| `sfx_hull_knocks_three` | 3.20 s | One-shot | `Assets/_Project/Audio/Zone01/SFX/sfx_hull_knocks_three.wav` |
| `sfx_algae_final_echo` | 8.50 s | One-shot | `Assets/_Project/Audio/Zone01/SFX/sfx_algae_final_echo.wav` |
| `ambient_algae_response` | 19.43 s | Loop | `Assets/_Project/Audio/Zone01/Ambience/ambient_algae_response.wav` |
| `sfx_biolamp_startup` | 4.00 s | One-shot | `Assets/_Project/Audio/Zone01/SFX/sfx_biolamp_startup.wav` |
| `intro_system_reboot` | 7.00 s | One-shot | `Assets/_Project/Audio/Zone01/Intro/intro_system_reboot.wav` |
| `intro_descent` | 12.00 s | One-shot | `Assets/_Project/Audio/Zone01/Intro/intro_descent.wav` |
| `zone02_low_rumble` | 8.00 s | One-shot | `Assets/_Project/Audio/Zone01/Transitions/zone02_low_rumble.wav` |

## Các bản nghe thử đã ghép

Trong `Previews/`:

- `preview_opening_35s.wav` — 35 giây.
- `preview_algae_approach_stop_leave.wav` — 35 giây.
- `preview_three_knocks_right.wav` — 12 giây.
- `preview_capture_forest_continues.wav` — 14 giây.
- `preview_biolamp_near_algae.wav` — 10 giây.
- `preview_departure_final_three_notes.wav` — 14 giây.

Các bản này dùng âm thật đã tạo, không có placeholder hoặc thoại giả. Chúng minh họa phối âm; không phải event chạy tự động theo tiến trình game.

## Điểm bám kịch bản

- **Intro:** click/boot, descent, tiếp nước, instability, một cảnh báo ngắn, va đập, **im lặng tuyệt đối giây 20–21**, reboot rồi máy/ventilation trở lại từ giây 27. Reference mix dài 35 giây, chừa chỗ cho thoại tương lai.
- **Vận hành thường:** idle engine, thrust, ventilation, steering servo, ballast/depth adjustment và radar ping là các clip riêng, để người chơi quen âm bình thường trước hiện tượng kinh dị đầu tiên.
- **Z1-E01/E02:** water / biological harmonics / glass tremors / engine là các lớp tách biệt. Khi dừng tàu, giảm thrust để tiếng tảo hiện ra; không dùng glitch hay blackout.
- **Z1-E03:** collector loop ngừng khi capture thành công; chỉ tiếng ngân của mẫu sinh vật tắt dần. Forest bed vẫn tiếp tục trong bản nghe thử.
- **Z1-E04:** fade-out các lớp tảo theo khoảng cách; water/machinery vẫn chạy. Không tạo một “file im lặng” để giả việc rời khu vực.
- **Z1-E05:** ba bản sao của **một tiếng gõ kim loại thật** tại 0.12 / 0.72 / 1.32 s, đúng khoảng cách 0.6 s. Stereo lệch phải đã được bake; không pan phải lần nữa. Ánh sáng, màn hình và radar không thay đổi. Chỉ gọi sau CAPTURE L1, đã rời L1, đang điều khiển, không modal, và chưa nghe trong timeline hiện tại.
- **Z1-E06/E07:** creak nhỏ khác với hull groan cảnh báo; âm trầm L2 dài 2.76 s, cần hủy khi dừng hoặc mở camera. Không tạo damage hoặc báo động giả.
- **Z1-E08:** `sfx_biolamp_startup.wav` có latch ở 0, power rise ở 0.6, tick điện ở 1.5 và completion chirp ở 3.0; dài 4 s. Chọn composite **hoặc** stems, không phát chồng cả hai. Bản gần tảo có khoảng ngắt biological layers từ khoảng 4.2–4.65 s, tức 2 s sau khi ánh sáng ổn định ở 2.2 s. Chỉ áp dụng nếu tảo đang nghe được; không ép phát tảo khi đã ở xa.
- **Z1-E09:** ba nốt sinh học xa, nốt cuối kéo dài hơn (1.4 / 1.65 / 4.7 s). Không có tiếng gõ/cào/giọng người trong cue này. Rumble Zone02 nằm riêng trong `Transitions/`.

## Kiểm tra đã làm và phần còn lại

Đã decode/kiểm tra cả 40 WAV; tất cả có tín hiệu, đúng định dạng, không có sample PCM clipping. Kiểm tra hash nguồn/thành phẩm, GUID metadata, clip references và các field audio-only. Waveform ba tiếng gõ khớp ba thành phần đã ghép ở sai số lượng tử PCM16; phải/trái RMS khoảng 2.71. Khoảng lặng trong opening/reboot được kiểm tra bằng mẫu số, không chỉ dựa vào tên file.

`Docs/QA/Zone01Audio/verification.json` lưu kết quả. Những file đã thay đổi từ công việc trước được kiểm tra hash và giữ nguyên.

**Cập nhật:** đã import và nối gameplay trong Unity G10 6000.4.2f1; kiểm tra native clip reference và Play Mode được ghi riêng ở `Docs/QA/Zone01AudioGameplay/`. Vẫn chưa có human listening review cho timbre, cảm giác khoảng cách, giọng hát sinh học không giống người, seam loop và nhịp căng thẳng. Không khẳng định đã nghe duyệt các phần đó.

## Cách gắn

1. Mở project G10; cho Unity import thư mục này và `FeedbackEffects/Zone01`.
2. Dùng AudioClip trực tiếp hoặc các EffectDefinition đã trỏ sẵn clip. Thêm các definition cần dùng vào catalog của EffectDirector tương ứng; không tự bật bất thường trước tutorial.
3. Caller vẫn sở hữu tiến trình/điều kiện event. Giữ handle riêng cho capture, tone L2 và mỗi loop; dừng đúng handle, không StopAll âm thanh của hệ thống khác.
4. Thực thi once-only và Journal restoration ở timeline owner hiện tại. Preset audio không lưu hoặc thay đổi tiến trình.
5. Không đưa preset Horror Combined/silhouette/scrape cũ vào Zone 1 theo kịch bản mới.
