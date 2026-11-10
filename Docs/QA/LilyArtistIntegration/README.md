# Lily 25 Days — tích hợp artist assets, 10/11/2026

Đã tích hợp vào scene và luồng đang chạy; không chỉ thêm file vào Assets. Nguồn là [gameplay/tài liệu](https://drive.google.com/drive/folders/1XDTIPCow4-FbWLAJpWXYLS9W8zuX9t53), [UI/branding](https://drive.google.com/drive/folders/15lgLFaHG8aEeGWmJLyxNYCuQ-QVobQV-) và [thư viện font liên quan](https://drive.google.com/drive/folders/1HcqeJM0AkVas6wem7RmFlpwQWur3ALGH). Làm trên workspace G10 hiện tại với các thay đổi Photo Lab, tranh sinh vật và nhát ma trước đó còn nguyên. Không checkout, merge, commit hoặc push.

## Artwork đã dùng

| Artwork gốc | Vị trí và thời điểm sử dụng | ID gameplay giữ nguyên |
|---|---|---|
| `vật phẩm ải 2 và 3.psd` / túi nhựa | Mẫu kết dính nhận khi CAPTURE tại Zone02 L1; icon/detail Cargo và nguyên liệu Pressure Hull | `Z2_Creature_01`, `Z2_L1_CAPTURE`, `zone02-l1` |
| Cùng PSD / Letonium | Mẫu cứng tại Zone02 L2; Cargo và nguyên liệu Pressure Hull | `Z2_Creature_02`, `Z2_L2_CAPTURE`, `zone02-l3` |
| Cùng PSD / thuốc nổ | Vật liệu nổ tại Zone03 L1; Cargo và nguyên liệu CRAFT bộ phá đá | `Z3_Creature_01`, `Z3_L1_CAPTURE`, `zone03-l1` |
| Cùng PSD / đá đánh lửa | Vật liệu kích nổ tại Zone03 L3; Cargo và nguyên liệu CRAFT bộ phá đá | `Z3_Creature_03`, `Z3_L3_CAPTURE`, `zone03-l4` |
| `2 cccd va 1 mat thu.psd` / cccd lily | Mission Log → HỒ SƠ THÁM HIỂM, trang 1; không cần thêm vật phẩm hoặc nhiệm vụ | Không thêm ID/progress |
| Cùng PSD / cccd emma | Trang 2, mở sau receipt CAPTURE dữ liệu xác tàu Zone04 L1; giữ tên, mã nhân viên và tranh thẻ nguyên bản, có bản đọc rõ bên cạnh | Chỉ đọc `Z4_L1_CAPTURE` |
| Cùng PSD / mật thư | Giấy của cue phục hồi tọa độ thật trong Mission Log; trang 3 cho đọc lại sau receipt trình bày `fragment` đã được phát thành công | Không tự reveal `Z4_L3` / `Z4_L4` |
| `MANG HINH DANG NHAP.psd` | Nền menu chính gồm nhân vật/không gian/bóng ma/texture; wordmark gốc thành Image riêng trên menu và Ending | Không đổi flow New Game / Continue / ending |
| `kenney_fantasy-ui-borders.zip` | Panel/nút menu dùng 9-slice; giữ giấy màu nước hiện có cho hồ sơ trong desktop | CC0, file license đi kèm |
| `UTM AmericanaBEx.ttf` | TMP font tĩnh cho tiêu đề hồ sơ và giấy tọa độ; fallback bộ chữ đọc được đang có | Không thay toàn bộ font cabin/Photo Lab |

Mapping bốn nguyên liệu được suy luận từ tên artboard, mô tả mẫu hiện tại và vai trò chi phí nhiệm vụ. PSD không nhúng creature ID. Đây là quyết định tích hợp, không phải tên ID do artist ghi sẵn.

## Quyết định thiết kế và kỹ thuật

- Giữ tranh sinh vật trong camera/capture, giữ toàn bộ tranh PHOTO đã bake trong Photo Lab. Thêm presentation riêng `collectedMaterial` vào catalog để nguyên liệu thu hồi có đúng tranh vật phẩm; stable cargo ID, số lượng và giao dịch không đổi. Save cũ tự lấy icon mới theo ID khi Continue, không migrate format.
- Zone02 dùng 1 túi sinh học + 1 Letonium khi INSTALL; Zone03 dùng 1 thuốc nổ + 1 đá đánh lửa khi CRAFT, INSTALL không trừ lại. Không thêm một lần thu thập hoặc một receipt nhiệm vụ cho thẻ/tài liệu.
- Hồ sơ là view đọc, dùng cơ chế cửa sổ và back handler hiện có. Escape từ hồ sơ trở lại nhiệm vụ trước khi đóng app. Mở hồ sơ che scroll nhiệm vụ và tạm mất audience của cue phục hồi để không ghi nhận đã đọc một cue đang bị che. View theo sự kiện đổi timeline: Journal quay về trước khi thu hồi sẽ khóa lại cả hồ sơ đang mở, không để sót tọa độ của tương lai. Continue/Journal vẫn là nguồn trạng thái; view không có state save riêng.
- `111111` trong mật thư là chữ mẫu: loại đúng các type layer này khỏi bản giấy xuất, rồi gắn TMP riêng đọc X/Y từ POI map thật. Không dùng số mẫu, không nhập lại tọa độ vào artwork. Các tọa độ không tự mở radar contact hoặc ending. Giấy được giữ trong suốt, giữ texture và mép rách gốc.
- Lorem ipsum trong PSD menu không đưa vào game. Nền, logo, nút và chữ là các thành phần độc lập. Không đặt nguyên mockup có nút giả lên menu. Logo là raster wordmark gốc.
- Thẻ Emma giữ ngày sinh không xác định như tài liệu gốc; bản đọc rõ ghi “không xác định”, không bịa ngày sinh. Hồ sơ Lily lấy thông tin nhân vật từ PSD artist; tài liệu concept cũ còn để trống tiểu sử không được sửa lại tùy tiện.
- Font PSD tham chiếu SVN-BradleyHand, SVN-GothamRounded và SVN-Beast nhưng hai thư mục mới không cung cấp file font đó. Giữ nét chữ trong artwork đã raster; dùng UTM Americana EB từ thư viện Drive liên quan cho heading, body đọc rõ dùng font đang có. ZIP Kenney chỉ nhắc Aoboshi One trong ảnh mẫu, không chứa file font đó.
- Texture Single Sprite, sRGB, NPOT nguyên kích thước, Bilinear/Clamp, không mipmap, không nén mất dữ liệu. Giữ layer ánh sáng trong tranh vật phẩm. Vùng crop được ghi rõ, chỉ bỏ swatch/paint mark rời khỏi vật phẩm; không repaint hoặc key nền theo màu. Các tranh PHOTO đã tích hợp trước đó không xuất lại.
- Scene được author bằng Unity Editor API/Undo/SerializedObject, đã SaveScene. `LilyArtistIntegrationSetup.Install()` là entry cài đặt lặp lại; không rebuild cabin hoặc prefab nền. `ComputerDesktopSkin.cs` và `PhotoLabView.cs` giữ đúng byte tại đầu task.
- Menu và Ending mang nhận diện **Lily 25 Days**. Định danh kỹ thuật G10/DefaultCompany và PlayerSettings được giữ để không đổi đường dẫn save thật. Namespace/mission/creature IDs vẫn là G10.

## Bằng chứng Unity

Screenshots được lấy từ Game View thật và đã tự xem; không phải ảnh dựng ngoài engine:

- [Menu 1920×1080](screens/1920-native-menu.png), [1366×768](screens/1366-native-menu.png).
- [Hồ sơ Lily](screens/1366-lily-dossier.png), [thẻ Emma](screens/1366-emma-card.png), [giấy tọa độ đọc lại](screens/1366-coordinate-paper.png).
- [Cue phục hồi tọa độ thật](screens/1920-native-coordinate-recovery.png), [đọc lại sau Continue](screens/1366-continued-coordinate-paper.png).
- [Journal khóa lại hồ sơ đang mở sau rollback](screens/1366-journal-rollback-locks-open-document.png).
- [Túi sinh học](screens/Z2_Creature_01-native-cargo.png), [Letonium](screens/Z2_Creature_02-native-cargo.png), [thuốc nổ](screens/Z3_Creature_01-native-cargo.png), [đá đánh lửa](screens/Z3_Creature_03-native-cargo.png).
- [Nguyên liệu vỏ tàu](screens/Zone02-materials.png), [nguyên liệu bộ phá đá](screens/Zone03-materials.png).
- [Logo ở ending thường](screens/1920-normal-branding-fixture.png), [ending ẩn](screens/1366-hidden-branding-fixture.png) — hai ảnh này là fixture trình bày, không chứng nhận đạt ending bằng gameplay.

`screens/*-full-painting-before-send.png` và `*-capture-creature.png` chứng minh tranh nguyên vẹn và tranh sinh vật vẫn ở luồng chụp/bắt khi vật phẩm có presentation riêng. Fixture Ending cố ý có survey 00/09 hoặc 00/11; nó không grant ending hoặc ảnh.

## Phạm vi kiểm tra

Gameplay kiểm tra dùng scene/canvas gốc, click qua EventSystem có kiểm tra raycast, nút chụp và capture của cabin, minigame thật được mở, SEND thủ công trong Photo Lab, Cargo, nút INSTALL/CRAFT và Continue. Năm capture ở Zone02/03/04 giữ đúng creature artwork trong encounter, sau thắng mới thêm đúng material art vào kho. Các PNG PHOTO được so exact pixel với tranh hoàn chỉnh, GoodPhoto và target ID phải đúng.

**Có fixture hỗ trợ:** Debug Start ở Zone02–04, đặt vị trí/hướng/độ sâu/refill tại contact và helper thắng minigame. Lượt hồ sơ Zone01 dùng Continue của returning player. Đây không phải chuyến chơi tự nhiên từ đầu qua bốn zone. Cue tọa độ dùng clock thật sau PHOTO → SEND → CAPTURE; không gọi Preview hoặc Tick giả để xuất hiện giấy. Hai ending được gọi view trực tiếp chỉ để kiểm tra logo/layout.

Lượt kiểm tra rollback tạo checkpoint bằng API REST thật trước khi thu hồi, rồi dùng RestoreDay cùng zone trong lúc hồ sơ còn mở. Test mouse riêng ngăn chuột vật lý của Editor tác động vào desktop trong lúc chờ screenshot; các pointer event được gửi rõ tới control đã kiểm tra raycast. Mọi device/settings test được phục hồi khi kết thúc.

Rerun hồi quy có Cargo cũ, empty/portrait/landscape/legacy gallery, xem lớn/Escape, xóa/hủy xóa, SEND chống lặp, scroll nhiệm vụ, rollback craft và receipt tọa độ. Những lỗi ban đầu về alpha giấy, vị trí chữ, UV sprite nền và mô tả nâng cấp chật đã sửa sau khi xem screenshot. Kết quả job chính xác và preservation audit được lưu cùng thư mục này.

**Kết quả cuối:** 15 ca PlayMode khác nhau đều qua (12 ca từ job `3e88acf6c4b7`, hai ca PhotoLab chạy lại và một ca receipt tọa độ; ca hồ sơ/Escape được lặp lại ở job `184666800244`, 4/4). Toàn bộ EditMode: **115 qua, 0 lỗi, 1 skip** trong tổng 116 (`4d9175fbd062`). Đây là tập PlayMode liên quan, không phải toàn bộ PlayMode repository. Rerun input dùng keyboard giả và settings riêng được phục hồi sau mỗi Escape, theo mẫu ComputerShell có sẵn; không đổi UIManager/PhotoLab runtime hoặc ProjectSettings để làm test qua. Native font/texture/raycast và screenshot được kiểm tra ở 1920×1080 và 1366×768; upgrade/capture chính kiểm tra ở 1920×1080.

Sau khi thêm refresh hồ sơ theo timeline, cả năm ca tích hợp mới được chạy lại: **5/5**, job `d7fa442b05ef`, bao gồm thu hồi thật → đọc → Continue → rollback khóa hồ sơ đang mở. `final-integration-results.json` giữ chi tiết từng ca. Cache hai font cũ đã được khôi phục đúng byte đầu task; 54 file persistent thật không đổi, không thêm hoặc mất file. Các trường gốc của cả 17 catalog assets không đổi (`catalog-preservation.json`).

Toàn bộ EditMode đã chạy lại trên code cuối: `54d45710b4db`, **115 qua / 0 lỗi / 1 skip**; đầy đủ từng ca trong `editmode-final-results.json`. Ca skip là đánh giá KinoGlitch tùy chọn chưa cài, không phải dependency của game. Audit cuối: 0 lỗi C#, 0 missing script ở ba scene sửa, Editor dừng trên Bootstrap sạch, save/photo overrides đều null.

Một job trộn fixture cũ đã có 2 lỗi SetUp của PhotoLab vì fixture trước trả PathOverride về null; chúng không chạy tới UI. PhotoLab được chạy lại trong job riêng với outer save isolation mới. Có một lần filter kết hợp group/test name cho ra 0 test, không tính vào kết quả nghiệm thu.

## Tái tạo và file chính

1. File nguồn được giữ ngoài Assets ở `.tools/lily-artist-sources`; xem `source-provenance.json` để lấy Drive ID/hash.
2. Chạy `Tools/Artwork/export_lily_assets.py` bằng `.tools/creature-artwork-venv/Scripts/python.exe`.
3. Trong Edit Mode sạch, gọi `G10.Prototype.Editor.LilyArtistIntegrationSetup.Install()` qua Unity MCP hoặc menu `G10/Artwork/Integrate Lily 25 Days Artist Assets`.
4. Dùng `ZoneTwoExperiencePlaytestTools.SetupIsolation(...)` trước từng native PlayMode job. Test ảnh Photo Lab cũ cần chạy riêng để tránh override bị fixture khác trả về null.

Runtime assets: `Assets/_Project/Art/Lily25Days/`. Controllers: `MissionArchiveView`, `LilyMenuPresentation`; adapter vật phẩm dùng `SurveyContentDefinition.InventoryImage/InventoryName`. Scene sở hữu là MainMenu, cabin dùng chung `Gameplay/Zone01` và Ending. Source inspection, typeface names, export operations và checksum ở các JSON trong thư mục này.

## Tài nguyên chưa dùng và giới hạn

- Không đưa mọi biến thể border Kenney vào màn hình; chỉ chọn panel/nút menu phù hợp, giữ giấy và vỏ desktop màu nước trong cabin.
- Không có file SVN-BradleyHand/GothamRounded/Beast hoặc Aoboshi One trong các nguồn mới, nên chưa thay chúng thành font chữ động cho toàn game. Chữ nằm trong tranh gốc vẫn nguyên bản.
- Chưa chạy chuyến thường xuyên suốt 25 ngày/bốn zone hoặc build standalone. Việc kiểm tra tự động trực tiếp trong Editor không thay cho đánh giá nhịp truyện bởi người chơi thật.
