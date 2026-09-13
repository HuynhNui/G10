# plan_codex_photo_logic.md

## Mục tiêu
Làm hệ thống **chụp ảnh dưới nước** cho game 2D sao cho ảnh trả về có cảm giác là kết quả của **thế giới thật** (tọa độ X/Y/Z + hướng nhìn), không phải random ảnh có sẵn.

---

## 1) Phạm vi MVP
Cho Zone 1 trước:
- Player có `position = (x, y, z)` và `headingDeg`.
- Bấm nút chụp => game dựng ra 1 ảnh từ các layer asset.
- Ảnh có thể ra các kết quả: `NoSubject`, `LifeDetected`, `GoodPhoto`, `TooFar`, `Obstructed`, `LowVisibility`.
- Chỉ cần support 1 loài chính (`Creature01`) và landmark/prop của Zone 1.

---

## 2) Cấu trúc code đề xuất

```text
Assets/_Project/
├── Art/
│   └── PhotoKit/Zone1_PhotoKit/... (gói asset này)
├── Scripts/
│   ├── Photo/
│   │   ├── Data/
│   │   │   ├── PhotoZoneDefinition.cs
│   │   │   ├── PhotoLandmarkDefinition.cs
│   │   │   ├── PhotoCreatureDefinition.cs
│   │   │   ├── PhotoOverlayProfile.cs
│   │   │   └── PhotoCaptureConfig.cs
│   │   ├── Runtime/
│   │   │   ├── PhotoCaptureService.cs
│   │   │   ├── PhotoWorldQuery.cs
│   │   │   ├── PhotoProjection.cs
│   │   │   ├── PhotoLayerComposer.cs
│   │   │   ├── PhotoEvaluationService.cs
│   │   │   └── PhotoSaveService.cs
│   │   ├── UI/
│   │   │   ├── PhotoCameraController.cs
│   │   │   └── PhotoResultPanel.cs
│   │   └── Tests/
│   │       ├── PhotoProjectionTests.cs
│   │       └── PhotoEvaluationTests.cs
```

---

## 3) Data model cần có

### 3.1 PhotoZoneDefinition (ScriptableObject)
Lưu cấu hình chung cho Zone 1.

```csharp
public class PhotoZoneDefinition : ScriptableObject
{
    public string zoneId; // "Z1"
    public float minDepth; // 0
    public float maxDepth; // -80
    public Sprite[] bgShallow;
    public Sprite[] bgMid;
    public Sprite[] bgDeep;
    public Sprite[] bgVoid;
    public Sprite[] seabedFlat;
    public Sprite[] seabedSlope;
    public Sprite[] seabedDistant;
    public Sprite[] overlaysFogShallow;
    public Sprite[] overlaysFogMid;
    public Sprite[] overlaysFogDeep;
    public Sprite[] overlayParticles;
    public Sprite[] overlayGlow;
}
```

### 3.2 PhotoLandmarkDefinition
Mỗi landmark lớn/prop trong zone có một record.

```csharp
public class PhotoLandmarkDefinition : MonoBehaviour
{
    public string landmarkId;
    public Sprite sprite;
    public LandmarkType type; // Landmark, Prop, Foreground
    public Vector3 worldPosition; // x, y, z
    public float visualRadius;
    public float importance;
    public bool canOcclude;
    public int renderOrderBase;
}
```

### 3.3 PhotoCreatureDefinition

```csharp
public class PhotoCreatureDefinition : MonoBehaviour
{
    public string creatureId;
    public Vector3 worldPosition;
    public float bodyRadius;
    public Sprite mainSprite;
    public Sprite threeQuarterSprite;
    public Sprite closeSprite;
    public Sprite silhouetteSprite;
    public Sprite altPoseSprite;
}
```

### 3.4 PhotoCaptureConfig

```csharp
public class PhotoCaptureConfig : ScriptableObject
{
    public float captureFov = 60f;
    public float maxVisibleDistance = 40f;
    public float goodPhotoMinScreenCoverage = 0.08f;
    public float tooFarScreenCoverage = 0.02f;
    public float maxOcclusionRatioForGood = 0.35f;
    public float nearDistance = 6f;
    public float midDistance = 16f;
}
```

---

## 4) Logic chụp hình tổng quát

### Input
- player world pos `(px, py, pz)`
- player heading `h`
- zone hiện tại `Z1`

### Output
- `PhotoCaptureResult`
  - ảnh đã render (RenderTexture hoặc Texture2D)
  - danh sách object được nhìn thấy
  - trạng thái ảnh (`NoSubject`, `GoodPhoto`, ...)

### Pipeline
1. Query zone + object gần player.
2. Chọn background theo depth.
3. Chọn seabed theo depth + local preset.
4. Query landmark/prop/creature nằm trong bán kính xét.
5. Project world -> photo frame.
6. Sắp layer theo thứ tự vẽ.
7. Render ra texture.
8. Đánh giá ảnh.
9. Lưu ảnh / show UI result.

---

## 5) Công thức project vật thể vào khung hình

### 5.1 Tính khoảng cách ngang
```csharp
Vector2 delta2 = new Vector2(obj.x - px, obj.y - py);
float planarDistance = delta2.magnitude;
```

### 5.2 Tính góc lệch so với hướng nhìn
```csharp
float targetAngle = Mathf.Atan2(delta2.y, delta2.x) * Mathf.Rad2Deg;
float angleDiff = Mathf.DeltaAngle(h, targetAngle);
```

### 5.3 Kiểm tra trong FOV
```csharp
bool inFov = Mathf.Abs(angleDiff) <= captureFov * 0.5f;
```

### 5.4 Đổi sang screenX
`screenX` nằm từ 0..1
```csharp
float screenX = 0.5f + (angleDiff / (captureFov * 0.5f)) * 0.5f;
```
- lệch trái => nhỏ hơn 0.5
- lệch phải => lớn hơn 0.5

### 5.5 Tính screenY từ chênh lệch độ sâu
```csharp
float dz = obj.z - pz;
float verticalNormalized = Mathf.Clamp(dz / 20f, -1f, 1f);
float screenY = 0.55f - verticalNormalized * 0.25f;
```
- vật thể cao hơn player => lên trên khung hình
- sâu hơn => xuống thấp hơn

### 5.6 Tính scale từ khoảng cách
```csharp
float scale = Mathf.Clamp01(1f - planarDistance / maxVisibleDistance);
scale = Mathf.Lerp(0.15f, 1.0f, scale);
```

### 5.7 Tính alpha theo khoảng cách + visibility
```csharp
float depthFog = GetFogFactorByDepth(pz);
float alpha = Mathf.Clamp01((1f - planarDistance / maxVisibleDistance) * (1f - depthFog * 0.5f));
```

---

## 6) Chọn background / seabed / overlay

### 6.1 Depth band
```csharp
if (pz >= -20) => Shallow
else if (pz >= -50) => Mid
else => Deep
```

### 6.2 Background rules
- **Shallow**: dùng `Z1_BG_OpenWater_Shallow_*`
- **Mid**: dùng `Z1_BG_OpenWater_Mid_*`
- **Deep**: dùng `Z1_BG_OpenWater_Deep_*`
- Nếu vùng thật sự trống: ưu tiên `Z1_Void_OpenWater_*`

### 6.3 Seabed rules
- Nếu local area là open-water: có thể không vẽ seabed hoặc vẽ `DistantFloor`
- Nếu area gần đáy/cồn cát: vẽ `Sand_Flat` hoặc `Sand_Slope`

### 6.4 Overlay rules
- luôn có 1 overlay particles nhẹ
- fog theo depth band
- soft glow chỉ thêm khi gần landmark/sinh vật phát sáng

---

## 7) Chọn sprite cho Creature01

```csharp
if (distance <= nearDistance) use closeSprite;
else if (distance <= midDistance) use Random(mainSprite, threeQuarterSprite, altPoseSprite);
else use silhouetteSprite;
```

Optional:
- nếu `Mathf.Abs(angleDiff) < 12f` => ưu tiên `mainSprite`
- nếu lệch góc lớn => ưu tiên `threeQuarterSprite`

---

## 8) Layer order đề xuất
Vẽ từ xa tới gần:
1. Background
2. Distant floor / distant silhouette
3. Main seabed
4. Landmark xa
5. Creature / prop midground
6. Foreground rock / kelp
7. Fog overlay
8. Particles overlay
9. Soft glow overlay

Codex nên làm `PhotoRenderable` để gom chung sprite + position + scale + alpha + order.

---

## 9) Đánh giá kết quả ảnh

### 9.1 Struct
```csharp
public enum PhotoResultType
{
    NoSubject,
    LifeDetected,
    GoodPhoto,
    TooFar,
    Obstructed,
    LowVisibility
}
```

### 9.2 Rule gợi ý
- Nếu không có creature nào visible => `NoSubject`
- Nếu có creature visible nhưng screen coverage < `tooFarScreenCoverage` => `TooFar`
- Nếu creature visible nhưng occlusion > `maxOcclusionRatioForGood` => `Obstructed`
- Nếu fog quá cao hoặc alpha quá thấp => `LowVisibility`
- Nếu creature visible + coverage đủ lớn + occlusion thấp => `GoodPhoto`
- Nếu visible nhưng chưa đủ đẹp => `LifeDetected`

### 9.3 Screen coverage đơn giản
```csharp
coverage = renderedWidth * renderedHeight / (photoWidth * photoHeight)
```
Hoặc ước lượng từ `spriteBounds * scale` cũng đủ cho MVP.

---

## 10) Mô hình dữ liệu local area
Để ảnh không quá random, chia map ra các ô hoặc area nhỏ.

### Cách đơn giản nhất cho Codex
- Tạo `PhotoAreaTrigger` hoặc grid data.
- Mỗi area có preset:
  - `areaId`
  - `preferVoidBackground`
  - `allowSeabed`
  - `landmarkIdsNearby`
  - `spawnCreatureIds`

Ví dụ:
```csharp
public class PhotoAreaPreset : ScriptableObject
{
    public string areaId;
    public Rect worldRectXY;
    public bool preferVoidBackground;
    public bool allowSeabed;
    public string[] prioritizedLandmarks;
}
```

Khi player đứng trong area nào thì chọn asset/preset của area đó trước rồi mới random nhẹ.

---

## 11) Random hợp lý
Cho phép random **nhẹ**, không random mạnh.

### Được random
- variant background A/B
- overlay particles A/B
- pose sinh vật (nếu đang ở cùng nhóm)
- vài prop nhỏ bật/tắt

### Không được random mạnh
- landmark lớn không được tự dưng biến mất nếu player vẫn đang nhìn cùng khu
- cùng vị trí + cùng heading phải cho ra bố cục gần giống nhau

=> Cần seed theo local area + rounded position + heading bucket.

```csharp
int seed = Hash(areaId, Mathf.RoundToInt(px), Mathf.RoundToInt(py), Mathf.RoundToInt(h / 10f));
Random.InitState(seed);
```

---

## 12) Cách render kỹ thuật trong Unity

### Option dễ nhất cho Codex (khuyên dùng)
- Tạo `PhotoComposerCanvas` trong scene ẩn.
- Bên trong có nhiều `SpriteRenderer` hoặc `Image` layer.
- Khi chụp:
  1. clear layer cũ
  2. gán sprite + pos/scale/alpha
  3. render camera phụ vào `RenderTexture`
  4. convert sang `Texture2D`
  5. show kết quả

### File gợi ý
- `PhotoLayerComposer.cs`: nhận list `PhotoRenderable`
- `PhotoCaptureCamera`: camera orthographic riêng cho photo
- `PhotoSaveService.cs`: encode PNG nếu muốn lưu file

---

## 13) UI result tối thiểu
Sau khi chụp:
- preview ảnh
- label kết quả
- tên sinh vật nếu detect được
- nút `Save`, `Close`, `Inspect`

---

## 14) Checklist triển khai theo thứ tự

### Phase 1 — Framework
- [ ] Tạo folder Scripts/Photo
- [ ] Tạo ScriptableObject data classes
- [ ] Import bộ `Zone1_PhotoKit`
- [ ] Tạo prefab `PhotoComposerCanvas`

### Phase 2 — Capture pipeline
- [ ] Đọc player pos + heading
- [ ] Query zone hiện tại
- [ ] Chọn background / seabed / overlay theo depth
- [ ] Project landmark + creature vào khung
- [ ] Render ra `RenderTexture`

### Phase 3 — Result evaluation
- [ ] Detect `NoSubject / TooFar / GoodPhoto / Obstructed / LowVisibility`
- [ ] Show panel kết quả
- [ ] Log species nếu `GoodPhoto`

### Phase 4 — Stability
- [ ] Seed random theo area + pos + heading
- [ ] Không cho landmark lớn biến mất vô lý
- [ ] Viết 2–3 test cho projection/evaluation

---

## 15) Pseudocode đầy đủ

```csharp
PhotoCaptureResult CapturePhoto(PlayerState player)
{
    var zone = zoneResolver.GetCurrentZone(player.position);
    var area = areaResolver.GetArea(zone, player.position);

    var ctx = new PhotoContext(player, zone, area, captureConfig);

    var renderables = new List<PhotoRenderable>();

    // 1. base
    renderables.Add(backgroundSelector.Select(ctx));
    renderables.AddRange(seabedSelector.Select(ctx));

    // 2. world objects
    var landmarks = worldQuery.GetVisibleLandmarks(ctx);
    var creatures = worldQuery.GetVisibleCreatures(ctx);

    renderables.AddRange(projector.ProjectLandmarks(ctx, landmarks));
    renderables.AddRange(projector.ProjectCreatures(ctx, creatures));

    // 3. foreground + overlays
    renderables.AddRange(foregroundSelector.Select(ctx));
    renderables.AddRange(overlaySelector.Select(ctx));

    // 4. compose
    Texture2D photo = composer.Render(renderables);

    // 5. evaluate
    var resultType = evaluator.Evaluate(ctx, renderables, creatures);

    return new PhotoCaptureResult
    {
        texture = photo,
        resultType = resultType,
        visibleCreatureIds = creatures.Select(c => c.creatureId).ToList()
    };
}
```

---

## 16) Nên giao Codex làm gì trước
Prompt ngắn cho Codex:

> Implement a Unity photo capture system for Zone 1. Use player X/Y/Z and heading to project landmarks and creatures into a generated photo frame. Build the system around ScriptableObjects for zone data, a hidden composition canvas rendered to a RenderTexture, and result evaluation with states NoSubject / LifeDetected / GoodPhoto / TooFar / Obstructed / LowVisibility. Import assets from the provided `Zone1_PhotoKit` package and wire a first-pass MVP for Creature01.

---

## 17) Chốt
Nếu làm theo plan này thì:
- mày không cần vẽ từng ảnh chụp
- asset được reuse đúng kiểu
- ảnh chụp vẫn có cảm giác phụ thuộc vào thế giới thật
- dễ mở rộng sang Zone 2, Zone 3 sau này