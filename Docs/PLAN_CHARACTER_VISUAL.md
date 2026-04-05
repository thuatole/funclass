# Plan: Character Models + Visual Feedback System

## Mục tiêu
Thay thế Capsule bằng character FBX models. Thêm visual feedback cho trạng thái học sinh (Calm/Distracted/ActingOut/Critical) bằng indicators + transform animations. Player phải nhìn vào là hiểu ngay tình trạng của từng học sinh.

## Hiện trạng

Đã có:
- 12 character FBX models (6 female, 6 male) trong Assets/Characters/
- 1 texture colormap.png dùng chung
- StudentVisualMarker: gán màu tĩnh theo tên (chưa phản ánh state)
- InfluenceStatusIcon: hiện ! và ? cho influence (độc lập với state)
- OnStateChanged event trên StudentAgent (chưa có visual subscriber)
- NavMeshAgent movement (transform animations tương thích)
- PrefabGenerator: có sẵn logic tạo prefab từ GameObject

Chưa có:
- Character FBX chưa được dùng trong game
- Không có visual feedback khi state thay đổi
- Không có animation (FBX tĩnh, không rig)
- Player không phân biệt được student Calm vs Critical bằng mắt

---

## Kiến trúc tổng thể

3 layer visual feedback chồng lên nhau, mỗi layer bổ sung thông tin:

Layer 1 - Character Model: hình dáng nhân vật (thay Capsule)
Layer 2 - State Color + Emission: đổi màu material theo state (nhìn xa biết ngay)
Layer 3 - State Indicators + Transform Animations: icon trên đầu + chuyển động cơ thể (nhìn gần hiểu chi tiết)

---

## Phase 1 - Character Model Integration

### Bước 1.1 - Tạo Character Prefabs từ FBX

Mỗi FBX cần trở thành prefab có đầy đủ components để dùng làm student.

Quy trình cho mỗi FBX:
- Load FBX từ Assets/Characters/
- Tạo prefab variant trong Assets/Prefabs/Characters/
- Prefab cấu trúc: Root GameObject (vị trí + logic) > Model child (visual mesh)
- Tách root và model để transform animations trên model không ảnh hưởng NavMeshAgent trên root

Components cần có trên root:
- StudentAgent
- NavMeshAgent
- CapsuleCollider (invisible, dùng cho collision/interaction)
- Rigidbody (kinematic)
- StudentVisualMarker (sẽ refactor ở Phase 2)
- InfluenceStatusIcon
- StudentMessCreator

Components trên model child:
- MeshRenderer (từ FBX)
- MeshFilter (từ FBX)
- Không có logic components

### Bước 1.2 - Thêm field characterModel vào level config JSON

Trong studentConfigs, thêm field mới:
- "characterModel": tên FBX (ví dụ "character-female-a")
- Nếu rỗng hoặc không có: fallback về Capsule (backward compatible)

Ví dụ:
- studentId: "student_binh", characterModel: "character-male-a"
- studentId: "student_lan", characterModel: "character-female-b"
- studentId: "student_nam", characterModel: "character-male-c"
- studentId: "student_mai", characterModel: "character-female-a"

### Bước 1.3 - Sửa StudentPlacementManager

Thay đổi logic tạo student:
- Đọc characterModel từ studentConfig
- Nếu có: load FBX từ Assets/Characters/{characterModel}.fbx, instantiate làm child của root
- Nếu không: tạo Capsule như hiện tại (fallback)
- Đảm bảo collider bounds khớp với model size
- Điều chỉnh NavMeshAgent height/radius theo model

### Bước 1.4 - Sửa EnsureStudentComponents

Hiện tại tạo Capsule child cho visual. Cần:
- Kiểm tra nếu đã có MeshRenderer trong children (từ FBX) thì KHÔNG tạo Capsule
- Chỉ tạo Capsule khi không có model nào

### Bước 1.5 - Material setup cho FBX models

- FBX models dùng chung colormap.png texture
- Tạo material instance per student (giống StudentVisualMarker hiện tại) để đổi màu per-student
- Material cần hỗ trợ: base color, emission color, tint color (cho Phase 2)
- Dùng URP Lit shader (dự án dùng URP)

### Bước 1.6 - Verify

- Import lại FirstDaySummerBreak với characterModel được set
- Verify: 4 students hiện FBX models thay vì Capsule
- Verify: collision, NavMesh navigation, interaction vẫn hoạt động
- Verify: InfluenceStatusIcon vẫn floating đúng vị trí trên đầu model
- Chạy Series 4 scenario test để verify logic không bị ảnh hưởng

---

## Phase 2 - State Color System

### Bước 2.1 - Tạo StudentStateVisual component

Component mới subscribe vào StudentAgent.OnStateChanged. Khi state thay đổi, đổi visual tương ứng.

Trách nhiệm:
- Cache tất cả Renderer trên student (root + children)
- Khi state thay đổi: lerp màu material sang màu mới (smooth transition, không nhảy đột ngột)
- Quản lý material instances (không ảnh hưởng shared materials)

### Bước 2.2 - Bảng màu theo state

4 state, 4 màu rõ ràng, tương phản cao:
- Calm: trắng/xám nhạt (neutral, không nổi bật) - đây là trạng thái bình thường
- Distracted: vàng nhạt (cảnh báo nhẹ, bắt đầu có vấn đề)
- ActingOut: cam (cảnh báo mạnh, cần can thiệp)
- Critical: đỏ (nguy hiểm, khẩn cấp)

Cách apply:
- Dùng tint color overlay lên base texture (không thay thế texture)
- Calm: tint = white (base color giữ nguyên)
- Distracted: tint = soft yellow
- ActingOut: tint = orange
- Critical: tint = red + emission glow (phát sáng nhẹ để nổi bật)

### Bước 2.3 - Smooth color transition

Khi state thay đổi, không đổi màu ngay lập tức. Dùng coroutine lerp:
- Duration: 0.5s
- Lerp cả tint color và emission intensity
- Nếu state thay đổi giữa chừng lerp: cancel lerp cũ, bắt đầu lerp mới từ màu hiện tại

### Bước 2.4 - Refactor StudentVisualMarker

Hiện tại StudentVisualMarker gán màu tĩnh theo tên. Cần thay đổi:
- Giữ logic identification color (dùng cho StudentIntroScreen)
- Nhưng runtime color bị override bởi StudentStateVisual
- Hoặc gộp chức năng vào StudentStateVisual luôn (base color từ tên + tint từ state)

### Bước 2.5 - Verify

- Chạy game, quan sát: students bắt đầu trắng/xám, dần chuyển vàng khi Distracted
- Calm student thấy ngay vì nổi bật khác màu với Distracted
- Critical student đỏ + phát sáng, không thể bỏ sót
- Transition mượt, không giật

---

## Phase 3 - State Indicators (Icons trên đầu)

### Bước 3.1 - Tạo StudentStateIndicator component

Hiển thị icon/symbol floating trên đầu student, bổ sung cho color system.

Vị trí: phía trên InfluenceStatusIcon (hoặc gộp chung vào 1 panel)

### Bước 3.2 - Icon design cho mỗi state

- Calm: không hiện gì (clean, không visual noise)
- Distracted: icon "..." (ba chấm, đang suy nghĩ lung tung)
- ActingOut: icon "!" (cảnh báo, đang gây rối)
- Critical: icon "!!" hoặc "!!!" nhấp nháy (nguy hiểm, cần hành động ngay)

Sử dụng TextMesh hoặc SpriteRenderer:
- TextMesh đơn giản hơn, consistent với InfluenceStatusIcon đang dùng TextMesh
- Sprite cho phép icon phong phú hơn nhưng cần asset
- Khuyến nghị: dùng TextMesh trước cho MVP, upgrade sprite sau

### Bước 3.3 - Icon animation

- Distracted "...": fade in/out nhẹ (breathing effect)
- ActingOut "!": bounce nhẹ lên xuống
- Critical "!!!": nhấp nháy nhanh (flash) + scale pulse (phóng to thu nhỏ)
- Animation bằng code (coroutine hoặc Update), không cần Animator component

### Bước 3.4 - Kết hợp với InfluenceStatusIcon hiện có

Hiện tại InfluenceStatusIcon hiện "!" (influencer) và "?" (influenced). Cần tránh xung đột:
- InfluenceStatusIcon giữ nguyên vị trí và logic (track influence)
- StudentStateIndicator đặt ở vị trí khác (cao hơn hoặc bên cạnh)
- Hoặc gộp cả hai vào 1 component StudentStatusPanel: hàng trên = state icon, hàng dưới = influence icons

### Bước 3.5 - Verify

- Nhìn từ xa: màu sắc cho biết state tổng quan
- Nhìn gần: icon cho biết chi tiết state + influence
- Không bị rối mắt, không quá nhiều visual elements chồng chéo

---

## Phase 4 - Transform Animations (chuyển động cơ thể)

### Bước 4.1 - Tạo StudentBodyAnimation component

Điều khiển transform của model child (KHÔNG phải root). Root giữ yên cho NavMeshAgent, model child xoay/rung/lắc.

### Bước 4.2 - Animation cho mỗi state

Tất cả dùng Mathf.Sin, Mathf.Cos trong Update hoặc coroutine. Không cần Animator.

Calm (idle nhẹ):
- Nhẹ nhàng lên xuống rất chậm (breathing): Y += Sin(time * 0.5) * 0.02
- Gần như không thấy, tạo cảm giác sống

Distracted (bồn chồn):
- Xoay nhẹ qua lại quanh trục Y: rotY += Sin(time * 2) * 5 độ
- Nghiêng nhẹ: rotZ += Sin(time * 1.5) * 3 độ
- Tạo cảm giác không yên, đang nhìn xung quanh

ActingOut (kích động):
- Rung lắc nhanh hơn: rotY += Sin(time * 4) * 10 độ
- Nhảy nhẹ: Y += Abs(Sin(time * 3)) * 0.05
- Tạo cảm giác đang hoạt động mạnh

Critical (hoảng loạn):
- Rung mạnh random: position += Random.insideUnitSphere * 0.03
- Xoay loạn: rotY += Sin(time * 6) * 15 độ
- Scale pulse: scale = 1 + Sin(time * 4) * 0.05
- Tạo cảm giác mất kiểm soát

### Bước 4.3 - Smooth transition giữa animations

Khi state thay đổi, không nhảy ngay sang animation mới:
- Lerp intensity từ animation cũ sang mới trong 0.3-0.5s
- Ví dụ: Calm (amplitude 0.02) → Distracted (amplitude 5 độ): lerp amplitude qua 0.5s

### Bước 4.4 - Interaction animations (one-shot)

Khi student thực hiện hành động cụ thể, chơi animation ngắn:
- KnockedOverObject: nghiêng về phía target 30 độ rồi về (0.5s)
- ThrowingObject: giật về phía target rồi về (0.3s)
- WanderingAround: xoay tròn chậm 360 độ (2s)
- Interacted with desk: nghiêng xuống phía trước 20 độ rồi về (0.4s)
- One-shot animations override state animation tạm thời, xong quay lại state animation

### Bước 4.5 - Teacher calm animation

Khi teacher calm student thành công:
- Student: scale shrink nhẹ (0.95) rồi bounce lại 1.0 (relief effect)
- Particle effect nhỏ: sparkle hoặc checkmark (optional)

### Bước 4.6 - Verify

- Mỗi state có chuyển động khác biệt rõ ràng
- Transition mượt, không giật
- Animations không ảnh hưởng NavMesh movement
- One-shot animations chơi đúng timing

---

## Phase 5 - Polish

### Bước 5.1 - Particle effects (optional)

- State transition: burst nhỏ particles khi chuyển state
- Critical: continuous smoke/fire particles quanh student
- Teacher calm: sparkle effect
- Dùng Unity Particle System, lightweight

### Bước 5.2 - Sound effects (optional)

- State escalation: âm thanh cảnh báo tăng dần
- Critical: alarm sound
- Teacher calm: chime sound
- Không nằm trong scope plan này, chỉ note để tương lai

### Bước 5.3 - Performance

- Material instances: pool và reuse, không tạo mới mỗi frame
- Animations: dùng Update, không Coroutine cho continuous animations (tránh GC)
- Particle systems: limit particle count (max 20 per student)
- Test với 10+ students để verify không drop FPS

---

## Thứ tự triển khai

Phase 1 (Character Models): thay Capsule bằng FBX - thay đổi visual lớn nhất
Phase 2 (State Colors): feedback rõ ràng nhất, nhìn xa biết ngay - quan trọng nhất cho gameplay
Phase 3 (State Indicators): bổ sung chi tiết, giúp player ra quyết định
Phase 4 (Transform Animations): tạo cảm giác sống động, immersive
Phase 5 (Polish): nice-to-have, làm sau MVP nếu cần

Có thể làm Phase 2 trước Phase 1 nếu muốn feedback nhanh trên Capsule trước khi đổi sang FBX.

---

## Lưu ý kỹ thuật

### Tách root và model child
Rất quan trọng: NavMeshAgent điều khiển root transform. Transform animations điều khiển model child. Nếu gộp chung, animations sẽ conflict với navigation.

Cấu trúc hierarchy:
- Student_Binh (root: NavMeshAgent, StudentAgent, Collider)
  - Model (child: MeshRenderer, StudentBodyAnimation)
  - StatusPanel (child: InfluenceStatusIcon, StudentStateIndicator)

### Material strategy
- Dùng MaterialPropertyBlock thay vì material instances khi có thể (performance tốt hơn)
- MaterialPropertyBlock cho phép thay đổi color/emission per-renderer mà không tạo material clone
- Chỉ tạo material instance khi cần thay đổi shader properties không hỗ trợ bởi MaterialPropertyBlock

### URP compatibility
- Shader: Universal Render Pipeline/Lit
- Emission: cần enable emission keyword trên material
- Color property: "_BaseColor" (URP) thay vì "_Color" (Built-in)
- Emission property: "_EmissionColor"
