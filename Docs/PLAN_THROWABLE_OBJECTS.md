# Throwable & Knockable Objects System

## Vấn đề hiện tại

- ThrowingObject event chỉ log + cộng disruption, không có visual của vật thật sự
- ExecuteThrowAt teleport object lên đầu target nhưng không có pickup phase, không reset, không variation theo size vật
- Không có pipeline spawn objects lên bàn student lúc level load → bàn trống, không có gì để ném
- Mess prefab có sẵn (TornPaper, BrokenGlass, Spill...) nhưng không spawn khi event xảy ra
- 78 prop prefabs có sẵn (book × 16, sheet × 5, laptop, computer × 4, speaker, projector...) nhưng chỉ làm decor, không tương tác

## Mục tiêu

Tạo hệ thống "lớp học bẩn bựa" — học sinh thực sự cầm và ném object, vật to nằm bàn vật nhỏ dính đầu, mess tích tụ → áp lực trực quan cho player can thiệp sớm.

## Yêu cầu chức năng

### 1. Phân loại size 2 nhóm

SizeCategory enum trên StudentInteractableObject:

- **Small** — đính vào đầu target (parent target.transform)
  - books (book1-16), sheets (sheet, sheet1-4), tray
- **Large** — đặt trên bàn target (world space)
  - laptop, computer (computer1-3), speaker, projector, chair (chair1)

### 2. Attach trực tiếp lên target (không hiển thị trên source)

ExecuteThrowAt(source, target) — synchronous, không coroutine:

- Log ThrowingObject event với target → influence apply
- **Small**: parent → target.transform, localPos = HEAD_OFFSET (0, 1.7, 0). Object follow target khi di chuyển
- **Large**: parent → null (world), position = derive từ target.OriginalSeatPosition + forward * 0.5 + up * 0.75, rotation = random tilt ±15°. KHÔNG follow target khi LeftSeat
- Object xuất hiện ngay trên đầu/bàn target — không có phase cầm tay source

### 3. Stack tự do trên bàn

- Nhiều object trên cùng bàn → mỗi object stack offset thêm Vector3.up * 0.1f * stackIndex
- KHÔNG giới hạn số lượng — chồng cao bao nhiêu cũng được
- Static dict `Dictionary<StudentAgent, int> deskStackCount` để track

### 3.5. Single interaction model: "source ném đồ vô target"

Toàn bộ object interaction đi theo 1 model duy nhất: source ném/làm rơi object → object xuất hiện ở target. KHÔNG có:
- Autonomous knock-over của bàn ghế trong scene (LevelManager.SetupDesksForInteraction đã bỏ)
- Autonomous re-throw của object đã spawn (canBeThrown/canBeKnockedOver = false trên spawned visuals)
- Tương tác "source phá object của target" — chỉ có "source bắn object SANG target"

Hệ quả:
- Desks trong scene KHÔNG có StudentInteractableObject component
- Spawned throwables là decorative artifacts, không tương tác lại
- Dialogue cho KnockedOverObject hiểu là "source làm rớt object cạnh target" (không phải "target's object bị rơi")

### 4. Spawn on-demand (KHÔNG pre-spawn lúc level load)

Object KHÔNG xuất hiện sẵn trên bàn lúc vào game. Chỉ spawn khi có interaction giữa students:

- StudentInteractionProcessor.TriggerInteraction kích hoạt event có target → call `ThrowableSpawner.SpawnAtTarget(target, size)`
- Object instantiate trực tiếp tại target (head cho Small, desk cho Large)
- Pool prefab pick từ `LevelConfig.deskLoadout.smallObjectPool` / `largeObjectPool` (fallback "Book" / "Laptop")
- Size determined bởi event type:
  - ThrowingObject → Small
  - KnockedOverObject → Large
- targetObject của StudentEvent = spawned GameObject → displayName flow vào influence + popup `{object}`

LevelManager.SpawnThrowables chỉ tạo singleton `ThrowableSpawner` + `MessSpawner`, KHÔNG gọi pre-spawn.

`SpawnDeskLoadouts` / `SpawnClassroomProps` còn lại trong code nhưng không được call — kept for potential future feature (decorative pre-placed items).

### 5. Mess spawn theo action

Sau ThrowingObject / KnockedOverObject hoàn tất → spawn mess prefab tại điểm va chạm:

- Small thrown → TornPaperMess cạnh chân target
- Large knocked → BrokenGlassMess (laptop/computer) hoặc SpillMess (speaker)
- Mess KHÔNG tự reset — tích tụ đến cuối level

### 6. Reset cuối level

LevelManager track `spawnedInteractables` + `spawnedMess` list → destroy hết khi EndLevel.

### 7. Dialogue dynamic theo object name

Popup phải nói tên vật cụ thể, không generic "đồ":

- "Bạn Nam ném **sách** vô con!" thay vì "Bạn Nam ném đồ vô con!"
- "Bạn Bình làm đổ **laptop** của con!" thay vì "Bạn Bình làm đổ đồ của con!"

Yêu cầu:

- Thêm placeholder `{object}` cho indirect + direct complaints
- StudentInteractableObject thêm field `displayName` (Vietnamese tên hiển thị: "sách", "laptop", "loa")
- InfluenceSources tracker lưu `sourceObjectName` khi record influence
- InfluenceSourceData DTO thêm `sourceObjectName`
- PopupTextLoader replace `{object}` → sourceObjectName, fallback "đồ" nếu null
- Content rewrite: tất cả pool ThrowingObject / KnockedOverObject / DroppedItem có `{object}` variant

## Yêu cầu phi chức năng

- Pure code change ([StudentInteractableObject.cs](Assets/Scripts/Core/StudentInteractableObject.cs), [LevelManager.cs](Assets/Scripts/Core/LevelManager.cs)) + tạo prefabs Editor
- Coroutine dùng `WaitForSeconds` (scaled), KHÔNG dùng Realtime → đồng bộ với scenario test timeScale
- Structured Milestone log cho Tier 3 scenario test (xem [PLAN_TEST_TIER3_SCENARIO.md](Docs/PLAN_TEST_TIER3_SCENARIO.md))
- Object pool cho mess prefab (tránh GC spike khi spawn nhiều)
- Không thêm Rigidbody / physics — chỉ transform + parent

## Cấu trúc data

### Schema mở rộng [LevelDataSchema.cs](Assets/Scripts/Editor/Data/LevelDataSchema.cs)

Thêm 2 field vào LevelData:
- `DeskLoadoutData deskLoadout`
- `ClassroomPropsData classroomProps`

DeskLoadoutData fields:
- perStudentMin (default 2), perStudentMax (default 3)
- smallObjectPool (List<string>) — ["Book", "Sheet", "Tray"]
- largeObjectChancePerDesk (float, default 0.2)
- largeObjectPool (List<string>) — ["Laptop"]
- randomizeVariants (bool, default true) — book1 vs book5 vs book9 random

ClassroomPropsData fields:
- sharedProps (List<string>) — ["Speaker", "Projector", "Computer"]
- extraDecoCount (int, default 4)
- autoPlace (bool, default true)

### StudentInteractableObject mở rộng

Thêm field:
- `SizeCategory sizeCategory = SizeCategory.Small`
- `string displayName` — tên Việt hiển thị popup ("sách", "laptop", "loa", "tờ giấy")
- Static `Dictionary<StudentAgent, int> deskStackCount`
- Field `originalParent` (cache để reset)

### InfluenceSources thread objectName qua 4 layer

- StudentEvent đã có `targetObject` (GameObject) — không đổi
- InfluenceSourceTracker (StudentAgent side): khi `RecordSource(evt)`, lưu thêm `sourceObjectName` từ `evt.targetObject?.GetComponent<StudentInteractableObject>()?.displayName`
- InfluenceSourceData DTO ([StudentInteractionPopup.cs:912](Assets/Scripts/Core/UI/StudentInteractionPopup.cs#L912)): thêm `string sourceObjectName`
- GetInfluenceSources(): copy sourceObjectName từ tracker → DTO
- PopupTextLoader: thêm replace `{object}` → sourceObjectName, fallback "đồ" nếu rỗng

### PrefabRegistry (tạo mới)

Map objectType string → prefab path:
- "Book" → random từ Books folder
- "Sheet" → random từ Sheets folder
- "Laptop" → a laptop.prefab
- ...

Editor utility tạo Throwable prefab variants từ school props.

## Logic flow

### Throw flow

1. StudentAgent quyết định throw → `ThrowAt(source, target)`
2. Distance check — nếu >2m, walk closer
3. ExecuteThrowAt Phase 1 — attach hand, wait 0.3s
4. ExecuteThrowAt Phase 2 — branch theo sizeCategory:
   - Small: parent target, HEAD_OFFSET
   - Large: world space, desk position derive từ target seat
5. Log structured Milestone (ObjectAttachedToSource → ObjectAttachedToTarget)
6. Spawn mess prefab cạnh target chân
7. KHÔNG auto-reset — object ở nguyên vị trí

### Spawn flow lúc level load

1. LevelManager.StartLevel đọc deskLoadout config
2. Foreach student: random N từ smallPool, spawn trên bàn (seat + forward * 0.5 + up * 0.6 + stackOffset)
3. Roll largeObjectChance cho mỗi desk
4. Spawn classroomProps tại vị trí auto
5. Cache spawned list cho reset

### LeftSeat behavior

- Small object đính đầu → follow target (parent transform handle tự động)
- Large object trên bàn → vẫn nằm bàn (parent=null, world space) — đã handle tự nhiên do không parent

## Edge cases

- **Source / target null giữa Phase 1-2**: cancel coroutine, object rơi sàn tại vị trí hiện tại
- **Object bị throw lần 2 khi đang đính đầu**: cancel pending, restart Phase 1 (object di chuyển từ đầu cũ → tay source mới → đầu target mới)
- **Stack offset z-fighting**: y += 0.1f * stackIndex, không clamp max
- **Target không có seat (NPC visitor)**: fallback Large → drop sàn cạnh target
- **Throw không target (Throw(student) gốc)**: chỉ Phase 1 → drop sàn forward * 1m, không Phase 2
- **Multiple students throw cùng target**: mỗi object stack độc lập, không conflict
- **Reset không tự động**: chấp nhận, mess + object tích tụ là feature
- **Object null trong event** (event không carry object): popup `{object}` → fallback "đồ"
- **displayName chưa set** (prefab cũ chưa wrap): fallback `objectName` → "đồ"
- **Multiple sources khác object** (Bình ném sách + Nam ném laptop): mỗi InfluenceSourceData có sourceObjectName riêng, popup hiển thị từng source với object riêng

## Acceptance criteria

- Mỗi student có 2-3 object trên bàn lúc level start
- Khi student throw small object → object đính đầu target, follow khi target di chuyển
- Khi student throw large object → object nằm bàn target, KHÔNG follow khi target rời ghế
- Throw 5 lần liên tiếp → 5 object stack chồng lên nhau, không z-fight
- Mess prefab xuất hiện cạnh chân target sau mỗi throw
- Tier 3 scenario test capture được ObjectAttachedToSource + ObjectAttachedToTarget events đúng timing
- Mở popup target bị Nam ném sách → text chứa "sách" (KHÔNG phải "đồ" generic)
- Mở popup target bị Bình knock laptop → text chứa "laptop"
- Multiple sources khác object → popup hiển thị từng source kèm tên object riêng

## Out of scope

- Audio (tray clatter, computer crash) — phase sau
- Physics thực (Rigidbody + AddForce) — chỉ teleport + parent
- Throw arc animation (lerp position theo bezier) — không cần per user
- Clean action cho teacher để reset mess — phase sau
- Object damage/break (laptop vỡ, sheet rách) — phase sau
- Personalize object theo student personality — phase sau

---

## Dev Plan

### Phase 0: Tạo prefab variants (Editor work, no code)

Deliverable: folder Assets/Prefabs/Throwables/ chứa 22+ prefab có StudentInteractableObject component.

Task 0.1: Folder structure
- `Assets/Prefabs/Throwables/Books/` — copy book1-16 prefab
- `Assets/Prefabs/Throwables/Sheets/` — copy sheet, sheet1-4
- `Assets/Prefabs/Throwables/Misc/` — tray
- `Assets/Prefabs/Throwables/Large/` — laptop, computer, computer1-3, speaker, projector, chair, chair1

Task 0.2: Wrap mỗi prefab
- Add component StudentInteractableObject
- Reset transform position về (0,0,0)
- Set canBeThrown=true cho Small + Large
- Set canBeKnockedOver=true cho Large
- Set sizeCategory phù hợp
- Add small BoxCollider (isTrigger=true) để raycast pickup nếu cần

Task 0.3: Editor utility tự động hóa (optional)
- Script ThrowablePrefabGenerator: scan school/Prefabs/props/, duplicate, wrap component, save vào Throwables folder
- Tránh làm manual cho 22+ prefabs

### Phase 1: Mở rộng schema + parser

Deliverable: LevelDataSchema có DeskLoadoutData + ClassroomPropsData, parser load được từ JSON.

Task 1.1: Thêm class DeskLoadoutData và ClassroomPropsData vào [LevelDataSchema.cs](Assets/Scripts/Editor/Data/LevelDataSchema.cs)

Task 1.2: Thêm 2 field vào LevelData class

Task 1.3: Test load JSON → check field populated đúng (không cần spawn yet)

Task 1.4: Tạo PrefabRegistry singleton — map objectType string → prefab path. Load prefabs lazy.

### Phase 2: ExecuteThrowAt refactor (Small vs Large)

Deliverable: Phase 1 attach hand + Phase 2 branch theo size, structured Milestone log.

Task 2.1: Thêm SizeCategory enum + sizeCategory field vào StudentInteractableObject

Task 2.2: Thêm const HEAD_OFFSET, DESK_FORWARD_OFFSET, DESK_RANDOM_TILT_DEG (HAND_OFFSET đã bỏ)

Task 2.3: Refactor ExecuteThrowAt:
- Synchronous (không coroutine) — log event → branch small/large → attach ngay
- Log Milestone "ObjectAttachedToTarget" sau attach

Task 2.4: Sửa Throw(student) — log event, drop sàn forward * 1m (ThrowNoTarget)

Task 2.5: Stack offset cho Large
- Static `Dictionary<StudentAgent, int> deskStackCount`
- Phase 2 Large: deskPos.y += 0.1f * stackCount, increment

### Phase 3: On-demand spawn pipeline

Deliverable: object chỉ xuất hiện khi interaction fire, KHÔNG pre-spawn trên bàn.

Task 3.1: ThrowableSpawner.SpawnAtTarget(target, size) — instantiate prefab tại target (head/desk via AttachToTarget)

Task 3.2: ThrowableSpawner.PickObjectTypeFromConfig(size) — đọc pool từ LevelConfig.deskLoadout, fallback "Book"/"Laptop"

Task 3.3: StudentInteractableObject.AttachToTarget(target) — public wrapper gọi AttachToHead/AttachToDesk theo sizeCategory

Task 3.4: Hook vào StudentInteractionProcessor.TriggerInteraction — spawn visual TRƯỚC khi LogEvent, pass GameObject làm targetObject để displayName flow vào influence

Task 3.5: LevelManager.SpawnThrowables — chỉ tạo singleton + SetCurrentLevelConfig, KHÔNG call pre-spawn methods

Task 3.6: EndLevel destroy spawnedObjects (đã có trong DespawnAll)

### Phase 4: Mess spawn integration

Deliverable: sau mỗi throw / knock, mess prefab xuất hiện cạnh target.

Task 4.1: MessSpawner singleton — pool VomitMess/SpillMess/TornPaperMess/BrokenGlassMess/StainMess/TrashMess

Task 4.2: ExecuteThrowAt Phase 2 cuối → MessSpawner.SpawnAt(target.position, MessType.TornPaper) cho Small

Task 4.3: KnockOver coroutine → SpawnAt cho BrokenGlass (laptop/computer) hoặc Spill (speaker)

Task 4.4: Cache spawnedMess, EndLevel destroy

### Phase 4.5: Dialogue object integration

Deliverable: popup nói tên vật cụ thể thay vì "đồ" generic.

Task 4.5.1: Thêm field `displayName` vào StudentInteractableObject. Set Vietnamese name cho 22+ throwable prefab (Phase 0 wrap luôn): book → "sách", sheet → "tờ giấy", tray → "khay", laptop → "laptop", computer → "máy tính", speaker → "loa", projector → "máy chiếu", chair → "ghế"

Task 4.5.2: InfluenceSourceTracker (StudentAgent side) — lưu thêm `sourceObjectName` khi RecordSource. Lấy từ `evt.targetObject.GetComponent<StudentInteractableObject>().displayName`

Task 4.5.3: InfluenceSourceData DTO — thêm field `sourceObjectName`. GetInfluenceSources() copy field này từ tracker

Task 4.5.4: PopupTextLoader — thêm replace `{object}` → sourceObjectName, fallback "đồ" nếu null/rỗng. Áp cho cả indirect + direct complaints

Task 4.5.5: Content rewrite trong CreateDefaultComplaintTemplates() — pool ThrowingObject / KnockedOverObject / DroppedItem viết lại có {object}:
- "Bạn {source} ném {object} vô con!"
- "Bạn {source} làm đổ {object} của con!"
- "Đau lắm cô, {object} của bạn {source} bay trúng con!"
- Mỗi pool 3-5 variant có {object}, giữ vài variant không có {object} cho fallback

Task 4.5.6: Test 4 case: book throw / laptop knock / object null event / multiple sources khác object

### Phase 5: Update scenario test (Tier 3)

Deliverable: assertions verify được throw lifecycle, không break series cũ.

Task 5.1: Update [PLAN_TEST_TIER3_SCENARIO.md](Docs/PLAN_TEST_TIER3_SCENARIO.md) — Bước 1.3 thêm StudentInteractableObject structured Milestone calls

Task 5.2: Update Series 4/5/6 assertions.json — thêm ObjectAttachedToSource + ObjectAttachedToTarget assertions cho ThrowingObject events

Task 5.3: assertions_win.json (Series 5b) — thêm `no_object_attach` mustOccur=false để verify throw bị cắt khi calm sớm

Task 5.4: (Optional) Tạo Series 7 — isolated throw lifecycle test (2 students, 1 ThrowingObject scripted, verify đầy đủ 4 milestones)

### Phase 6: Polish (optional)

Task 6.1: Editor preview gizmo cho desk position (giúp debug spawn lệch)
Task 6.2: Inspector custom cho StudentInteractableObject hiển thị stack count runtime
Task 6.3: Mess fade-in animation (tránh pop)
Task 6.4: Random rotation cho object Phase 1 attach (tự nhiên hơn)

### Dependency

Phase 0 → Phase 1 + 2 song song được. Phase 3 sau Phase 0+1+2. Phase 4 độc lập, có thể song song Phase 3. Phase 4.5 sau Phase 0 (cần displayName trên prefab) + Phase 2 (cần ExecuteThrowAt thread targetObject vào influence). Phase 5 sau Phase 2 + 4.5 (assertion cần verify object name trong text). Phase 6 cuối.

### Risk

- **Bone offset cứng** — HAND_OFFSET / HEAD_OFFSET có thể lệch nếu character scale khác. Mitigation: test 4 character model có sẵn (male-a, female-a, male-b, female-b), tweak offset nếu cần. Nâng cấp lên bone lookup ở Phase 6.
- **Desk position derive sai** — `seat + forward * 0.5` giả định forward = hướng nhìn lên bảng. Nếu student xoay → sai. Mitigation: dùng `OriginalSeatRotation.forward` thay vì `transform.forward` runtime.
- **Editor wrap 22+ prefab manual lâu** — Phase 0 task 0.3 viết script tự động, save 30+ phút.
- **Mess accumulate gây lag** — chấp nhận tới 50 mess; nếu vượt → dùng object pool + LRU recycle.
- **Schema break backward compat** — giữ field optional, default value, parser không throw khi field thiếu.
- **timeScale × WaitForSeconds** — coroutine throw phải dùng scaled (đã note trong yêu cầu phi chức năng).
- **displayName chưa cover hết object type** — fallback `objectName` rồi fallback "đồ"; viết unit test PopupTextLoader cho fallback chain.
- **{object} trong câu sai văn phong** ("ném tờ giấy vô con" → có thể awkward) — review content writer ở Task 4.5.5, ưu tiên câu nature trên syntactic correctness.

### Estimate

- Phase 0: 1h (utility script) hoặc 30 min × 22 = 11h (manual). Recommend script.
- Phase 1: 30 min
- Phase 2: 1.5h
- Phase 3: 1.5h
- Phase 4: 1h
- Phase 4.5: 2h (schema thread 1h + content rewrite 30 min + test 30 min)
- Phase 5: 1h (update plan + assertions)
- Phase 6: skip release đầu

**Tổng: ~8.5h dev (với Phase 0 script) — gọn 1 ngày + buổi sáng hôm sau**
