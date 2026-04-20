# Dynamic NPC Dialogue System

## Vấn đề hiện tại

- Opening phrase (chữ vàng) chỉ phụ thuộc state, không quan tâm có ai đang ảnh hưởng student hay không
- Khi student bị ảnh hưởng bởi bạn khác, câu thoại không phản ánh được mối quan hệ đó
- Indirect complaint hiện chỉ dạng "Bạn X làm gì đó" — quá generic, không gắn với hành động cụ thể của source
- Direct complaint cũng ít biến thể, mỗi eventType chỉ 1-2 câu

## Mục tiêu

Refactor opening phrase và complaint text trong StudentInteractionPopup để câu thoại của NPC sinh động, đa dạng và context-aware.

## Yêu cầu chức năng

### 1. Opening phrase đa chiều (state × context)

Pool được phân loại theo cặp (state, ngữ cảnh):

- Calm_Default: student bình tĩnh, không có sự kiện gì — "Dạ cô?", "Em đây cô!"
- Calm_AfterCalmed: vừa được giáo viên calm down — "Em cảm ơn cô...", "Em sẽ chú ý hơn ạ"
- Distracted_SelfCaused: tự gây ra sự kiện — "Em không cố ý...", "Em lỡ tay thôi cô"
- Distracted_Influenced: bị bạn khác ảnh hưởng — "Tại bạn ấy làm em phân tâm", "Em không làm gì cả!"
- ActingOut_SelfCaused: tự gây rối, tỉnh bơ/bướng — "Có gì đâu cô!", "Em đùa thôi mà"
- ActingOut_Influenced: bị lôi kéo, đổ lỗi — "Bạn ấy rủ em đấy chứ!", "Em chỉ làm theo thôi"
- Critical: mất kiểm soát, hoảng/khóc — "...", "Hức hức...", "Em không biết nữa..."

Mỗi pool 4-6 câu. Dùng PickNoRepeat đã có sẵn.

### 2. Indirect complaint gắn với hành động cụ thể của source

Câu phải mô tả hành động của source theo eventType, kèm tên source qua placeholder:

- KnockedOverObject: "Bạn {sourceName} vừa làm đổ đồ kìa cô!", "Tại bạn {sourceName} làm rơi đồ nên em giật mình"
- ThrowingObject: "Bạn {sourceName} đang ném đồ lung tung!", "Bạn {sourceName} ném trúng em!"
- MakingNoise: "Bạn {sourceName} ồn quá em không nghe được", "Bạn {sourceName} cứ nói chuyện hoài"
- WanderingAround: "Bạn {sourceName} đi lung tung làm em mất tập trung"
- MessCreated: "Bạn {sourceName} bày bừa kìa cô", "Chỗ bạn {sourceName} dơ quá"
- Distraction: "Bạn {sourceName} cứ nghịch làm em không học được"
- PhysicalInteraction: "Bạn {sourceName} chọc em!", "Bạn {sourceName} đụng em hoài"
- Poop: nhắc gián tiếp, tế nhị

Mỗi eventType 3-5 biến thể. WholeClass scope thay {sourceName} bằng "các bạn".

### 3. Direct complaint đa dạng theo state

Khi chính target gây ra sự kiện, tone theo state:

- Distracted: biện hộ, ngại ngùng — "em không cố ý...", "em lỡ thôi..."
- ActingOut: bướng bỉnh, không nhận sai — "có gì đâu cô!", "em đùa thôi mà"
- Critical: hoảng loạn — không trả lời mạch lạc

Mỗi eventType × state 3-4 biến thể.

## Yêu cầu phi chức năng

- Toàn bộ content Vietnamese, tone học sinh tiểu học (lớp 1-3)
- Không lặp câu trong cùng popup session (PickNoRepeat per pool key)
- Content load từ JSON, không hardcode trong cs file (trừ default fallback)
- Backwards compatible: nếu pool mới chưa có data, fallback về sourceStatements cũ
- Performance: pool lookup O(1) theo dictionary key

## Cấu trúc data

ComplaintTemplate mở rộng thêm 3 trường:

- openingByContext: dictionary key dạng "State_Context" → list câu
- directComplaintsByEvent: dictionary eventType → dictionary state → list câu
- indirectComplaintsByEvent: dictionary eventType → list câu (có placeholder {sourceName})

Giữ nguyên các trường cũ để backwards compatible.

Note: Unity JsonUtility không serialize nested dict trực tiếp. Cần wrapper class (list of key-value pair) hoặc switch sang Newtonsoft.Json nếu đã có trong project.

## Logic flow

Bước 1: Xác định state hiện tại của target student.

Bước 2: Xác định context:
- Không có source → Default
- Source là chính target → SelfCaused
- Source là student khác → Influenced
- State = Calm và WasRecentlyCalmed = true → AfterCalmed

Bước 3: Lookup opening pool theo "{state}_{context}". Fallback: "{state}_Default" → "{state}" → hard default.

Bước 4: Pick câu opening bằng PickNoRepeat.

Bước 5: Build complaint:
- SelfCaused hoặc no source → direct complaint theo eventType + state
- Influenced bởi student khác → indirect complaint theo eventType, format {sourceName}
- WholeClass scope → indirect với "các bạn"

Bước 6: Render popup.

## Edge cases

- Source = target (self-caused): dùng direct, không indirect
- Multiple sources: pick source severity cao nhất làm primary, list còn lại hiện bên dưới như cũ
- Pool trống: fallback về Default pool của state đó
- WholeClass scope (no specific source): indirect format với "các bạn"
- Calm + active influence từ trước: ưu tiên AfterCalmed nếu có flag, không thì Default

## Acceptance criteria

- Mở popup 5 lần liên tiếp cho cùng 1 student không thấy lặp opening phrase
- Student bị bạn ném đồ → opening khác student bị bạn ồn ào
- Student tự gây rối ActingOut → tone khác student bị ảnh hưởng Distracted
- Student vừa được calm → opening có cảm giác "vừa được nhắc nhở"
- Critical state → opening tỏ rõ hoảng loạn, không trả lời rõ ràng

## Out of scope

- Không thêm voice acting / TTS
- Không thay đổi UI layout popup
- Không build dialogue editor in-game
- Không personalize theo personality student (phase 2 sau)

---

## Dev Plan

### Phase 0: Chuẩn bị content (no code)

Deliverable: file Docs/CONTENT_DIALOGUE_VN.md chứa toàn bộ content draft.

Task 0.1: Viết 7 opening pools (4-6 câu mỗi pool), tone học sinh tiểu học.

Task 0.2: Viết direct complaints — 8 eventType × 3 state (Distracted, ActingOut, Critical) = 24 sub-pool, mỗi pool 3-4 câu.

Task 0.3: Viết indirect complaints — 8 eventType, mỗi pool 3-5 câu, bắt buộc có {sourceName}, thêm variant "các bạn" cho WholeClass scope.

Task 0.4: Review toàn bộ — tránh trùng câu giữa pool, check tone consistency, approve trước khi sang Phase 1.

### Phase 1: Mở rộng data schema

Deliverable: PopupText.json updated, PopupTextLoader.cs compile được, no behavior change.

Task 1.1: Mở rộng ComplaintTemplate class — thêm 3 trường mới, giữ nguyên trường cũ.

Task 1.2: Convert content Phase 0 sang JSON. Giải quyết nested dict bằng wrapper class nếu cần.

Task 1.3: Fallback loader — JSON thiếu trường → load defaults từ code; pool trống → fallback chain.

### Phase 2: API mới trong PopupTextLoader

Deliverable: 3 method mới, test thủ công qua Editor menu.

Task 2.1: GetOpeningPhrase(state, context) — fallback chain exact key → State_Default → legacy → hard default.

Task 2.2: GetDirectComplaintByState(eventType, state) — fallback state-specific → eventType default → generic.

Task 2.3: GetIndirectComplaintByEvent(eventType, sourceName, isWholeClass) — format placeholder, fallback về generic.

### Phase 3: Refactor StudentInteractionPopup

Deliverable: popup dùng pool mới, behavior thay đổi rõ rệt.

Task 3.1: Detect context khi build popup (hasSource, sourceIsSelf, afterCalmed), map về context name.

Task 3.2: Thay GetTargetOpeningPhrase — truyền thêm context.

Task 3.3: Thay GetComplaintByScope — routing sang direct / indirect / WholeClass.

Task 3.4: Multiple sources — pick primary theo severity, list còn lại giữ nguyên.

### Phase 4: TeacherController integration

Deliverable: mở popup sau khi calm → opening dùng AfterCalmed pool.

Task 4.1: Thêm lastCalmedStudentId + lastCalmedTimestamp vào TeacherController, cập nhật trong CalmStudent(), expose WasRecentlyCalmed(studentId, windowSeconds=3f).

Task 4.2: StudentInteractionPopup — khi state=Calm và WasRecentlyCalmed=true → context=AfterCalmed.

### Phase 5: Testing

Task 5.1: Manual test matrix — 4 state × 3 context = 12 combo, mở popup 5 lần liên tiếp mỗi combo.

Task 5.2: Edge case — multiple sources, source rời zone, pool trống, ký tự đặc biệt.

Task 5.3: Regression — popup cũ không source, StudentIntroScreen không bị ảnh hưởng.

### Phase 6: Polish (optional, skip release đầu)

Task 6.1: Context mới — Calm_FirstTime, ActingOut_Repeat.

Task 6.2: Personality variation — shy → câu rụt rè, energetic → câu năng động.

### Dependency

Phase 0 → Phase 1 → Phase 2 → Phase 3. Phase 4 độc lập, xong trước Phase 3 task 3.1 là được. Phase 5 sau Phase 3 + 4. Phase 1 và 2 có thể song song sau khi schema chốt.

### Risk

- Unity JsonUtility không serialize nested dict → dùng wrapper list-of-pair hoặc Newtonsoft.Json
- Content writing lâu → Phase 0 tách riêng, có thể outsource
- Performance lookup mỗi popup → cache dict reference khi load, không lookup mỗi frame
- Backwards compat break → giữ method cũ, thêm method mới, fallback chain rõ

### Estimate

Phase 0: 2-3 giờ. Phase 1: 1-2 giờ. Phase 2: 1 giờ. Phase 3: 2 giờ. Phase 4: 30 phút. Phase 5: 1-2 giờ. Tổng: ~1 ngày dev.
