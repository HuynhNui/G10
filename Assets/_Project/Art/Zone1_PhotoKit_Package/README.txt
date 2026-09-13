# Zone1 Photo Kit Package

Gói này gom các asset đã tạo cho **Zone 1 / Miền Tảo Hát** và đặt lại tên theo naming convention thống nhất để mày có thể quăng thẳng vào project Unity.

## Cấu trúc
- `Zone1_PhotoKit/Backgrounds`: nền open water / void water
- `Zone1_PhotoKit/Seabed`: các lớp đáy biển / cát
- `Zone1_PhotoKit/Landmarks`: landmark lớn để người chơi nhận ra vị trí
- `Zone1_PhotoKit/Props`: prop nhỏ / patch / silhouette
- `Zone1_PhotoKit/Foreground`: lớp gần camera để che/gia tăng chiều sâu
- `Zone1_PhotoKit/Overlays`: fog / particles / glow
- `Zone1_PhotoKit/Creatures/Creature01`: sprite cho sinh vật 1
- `Docs`: plan logic chụp hình + manifest

## Gợi ý import Unity
- Sprite Mode: Single
- Mesh Type: Full Rect
- Alpha Is Transparency: ON
- Filter Mode: Bilinear hoặc Point tùy pipeline art của mày
- Compression: None hoặc Low nếu cần giữ alpha đẹp

## Lưu ý
- Một vài overlay/fog hiện tại đang là placeholder tái dùng từ bộ đã tạo trước. Khi gameplay ổn rồi thì artist có thể vẽ thêm bản chuyên dụng.
- Bộ này là **MVP production kit**, đủ để Codex triển khai logic chụp hình và ghép ảnh trong game.