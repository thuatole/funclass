# Plan: Tier 2 - Integration Testing

## Mục tiêu
Test interaction giữa các component khi chạy cùng nhau trong Unity runtime. Cần Play mode nhưng không cần full level.

## Unity Test Framework - PlayMode Tests

### Cách thiết lập
Trong Test Runner window, chọn tab "PlayMode". Tạo folder Assets/Tests/PlayMode với Assembly Definition riêng. Khi tạo asmdef, check vào "Test Assemblies" và đặt Platform Settings sang "Any".

QUAN TRỌNG: Thêm reference đến assembly chính của game (Assembly-CSharp hoặc FunClass.Core). Không có reference thì test không compile được. Thao tác: chọn file asmdef trong Inspector, mục "Assembly Definition References", bấm + và chọn assembly của game.

PlayMode tests dùng attribute UnityTest thay vì Test cho các test cần đợi frame. UnityTest là coroutine, trả về IEnumerator, có thể yield WaitForSeconds để chờ xử lý.

### Tạo objects programmatic (KHÔNG dùng scene thủ công)
Tạo scene thủ công fragile vì dễ bị ai đó sửa nhầm. Thay vào đó, mỗi test tự tạo objects trong SetUp method và xóa trong TearDown.

SetUp flow cho mỗi test:
- Tạo GameObject mới, add GameStateManager component
- Tạo GameObject mới, add StudentEventManager component
- Tạo GameObject mới, add StudentInfluenceManager component (nếu test cần)
- Tạo GameObject mới, add StudentInteractionProcessor component (nếu test cần)
- Tạo 2-3 GameObject cho StudentAgent, gán StudentConfig programmatically
- yield return null để Awake/Start chạy

TearDown flow:
- Destroy tất cả GameObjects đã tạo
- yield return null để OnDisable/OnDestroy chạy sạch

Lợi ích: mỗi test tự kiểm soát chính xác objects nó cần, không ảnh hưởng test khác.

### Xử lý LevelManager dependency
Nhiều component gọi LevelManager.Instance.GetCurrentLevelConfig() để lấy InfluenceScopeConfig. Trong integration test có 2 cách:
- Tạo LevelManager mock với LevelConfig đơn giản chứa influenceScopeConfig cần thiết
- Chấp nhận fallback: khi LevelManager null, StudentInfluenceManager dùng hardcoded severity values (đã có trong code). Đa số integration test chấp nhận fallback là đủ.

Ghi rõ trong mỗi test: test này dùng mock LevelConfig hay fallback values.

---

## Các test cần viết

### 1. Scripted Event Pipeline Test
Mục tiêu: Verify luồng từ StudentInteractionProcessor → StudentEventManager → StudentInfluenceManager hoạt động end-to-end.

Kịch bản test:
- Tạo GameStateManager, StudentEventManager, StudentInfluenceManager, StudentInteractionProcessor
- Tạo student Bình và Lan, đặt cách nhau 4m (trong singleStudentMaxDistance)
- Load interaction: Bình → Lan, KnockedOverObject, timeElapsed, triggerValue=5s (dùng 5s thay 20s để test nhanh)
- Gọi GameStateManager.ChangeState(InLevel)
- Chờ 6 giây game time (5s trigger + 1s buffer)
- Assert: Lan đã nhận influence (InfluenceSources.GetUnresolvedSourceCount() > 0)
- Assert: Lan không còn ở Calm state

Dependency: cần LevelManager mock cho LevelTimeElapsed. Hoặc dùng Time.timeScale cao để rút ngắn chờ.

File test: Assets/Tests/PlayMode/ScriptedEventPipelineTest.cs

### 2. SingleStudent vs WholeClass Scope Test
Mục tiêu: Verify SingleStudent scope chỉ ảnh hưởng target, WholeClass ảnh hưởng tất cả.

Kịch bản SingleStudent:
- 3 students: A (source), B (target, cách 3m), C (không liên quan, cách 3m hướng khác)
- Inject event trực tiếp qua StudentEventManager.LogEvent với influenceScope = SingleStudent, targetStudent = B
- Assert: B bị ảnh hưởng (InfluenceSources count > 0)
- Assert: C KHÔNG bị ảnh hưởng (InfluenceSources count == 0)

Kịch bản WholeClass:
- 3 students: A (source), B, C (cùng location)
- Inject event qua StudentEventManager.LogEvent với influenceScope = WholeClass
- Assert: cả B và C bị ảnh hưởng

File test: Assets/Tests/PlayMode/ScopeTest.cs

Đây cũng là regression test cho bug scope WholeClass→SingleStudent.

### 3. Teacher Calm → Resolve Influence Test
Mục tiêu: Verify khi teacher calm student nguồn, influence được resolve trên student đích.

Kịch bản:
- Tạo Bình và Lan
- Inject influence event: Bình → Lan (Lan có InfluenceSources từ Bình)
- Gọi ResolveInfluenceSourcesFromStudent(Bình) trực tiếp (không cần TeacherController)
- Assert: Lan.InfluenceSources không còn Bình (GetUnresolvedSourceCount == 0)

File test: Assets/Tests/PlayMode/TeacherCalmResolveTest.cs

### 4. Late Subscription Pattern Test
Mục tiêu: Verify các singleton subscribe đúng dù GameStateManager chưa tồn tại lúc OnEnable.

Kịch bản:
- Tạo StudentInteractionProcessor TRƯỚC (OnEnable gọi, GameStateManager null)
- yield return null (Start chạy, GameStateManager vẫn null)
- Tạo GameStateManager
- yield return null (frame mới)
- Lúc này StudentInteractionProcessor đã miss subscription
- Gọi GameStateManager.ChangeState(InLevel)
- Assert: cần verify processor có active không

Lưu ý thực tế: test này mô phỏng race condition. Kết quả phụ thuộc vào thứ tự Awake/Start. Nếu Start() fallback subscription hoạt động đúng thì pass, nhưng nếu GameStateManager được tạo SAU Start() đã chạy thì fallback cũng fail. Test nên cover cả hai case.

File test: Assets/Tests/PlayMode/LateSubscriptionTest.cs

Đây cũng là regression test cho Bug 4.

### 5. Fallback Nearest Student Test
Mục tiêu: Verify khi SingleStudent scope không có target, fallback tìm student gần nhất.

Kịch bản 1 - có student gần:
- Student A và Student B đứng cách nhau 3m
- Inject event qua StudentEventManager.LogEvent: influenceScope = SingleStudent, targetStudent = null
- Assert: B nhận influence (nearest student fallback hoạt động)

Kịch bản 2 - không có student gần:
- Student A là source
- Student B đứng cách 10m (ngoài singleStudentMaxDistance)
- Inject event SingleStudent không có target
- Assert: không crash, ProcessWholeClassInfluence xử lý

File test: Assets/Tests/PlayMode/FallbackNearestStudentTest.cs

Đây cũng là regression test cho Bug 3.

### 6. TriggerValue Mapping Test
Mục tiêu: Verify LoadRuntimeInteractions map triggerValue vào customSeverity đúng.

Kịch bản:
- Tạo StudentInteractionProcessor
- Tạo list RuntimeStudentInteraction với triggerValue = 20f, 40f, 60f, 80f
- Gọi LoadRuntimeInteractions
- Verify: interactions đã load có customSeverity tương ứng 20, 40, 60, 80

Lưu ý: interactions list là private. Cách access:
- Cách 1: Thêm InternalsVisibleTo attribute trên main assembly, đổi private thành internal
- Cách 2: Test gián tiếp - load interactions rồi chờ đúng thời gian, verify event trigger
- Khuyến nghị Cách 2 vì không cần sửa production code

File test: Assets/Tests/PlayMode/TriggerValueMappingTest.cs

Đây cũng là regression test cho Bug 1.

---

## Cách viết UnityTest với yield

Vì PlayMode test là coroutine, khi cần đợi thời gian game trôi qua:
- yield return null → đợi 1 frame
- yield return new WaitForSeconds(N) → đợi N giây
- yield return new WaitUntil(() => condition) → đợi condition đúng (có timeout tự động sau 30s)

Ví dụ flow: SetUp tạo objects → yield return null để Awake/Start chạy → inject event → yield return WaitForSeconds(1f) → Assert kết quả → TearDown xóa objects.

Dùng Time.timeScale = 10 trong test để rút ngắn thời gian chờ. Nhớ reset về 1 trong TearDown.

---

## Xử lý private members trong test

Nhiều field/method cần verify là private (isActive, interactions list, ProcessInfluence). Có 3 cách:

Cách 1 - InternalsVisibleTo: Thêm attribute trên main assembly cho phép test assembly truy cập internal members. Đổi private thành internal cho các field cần test. Sạch nhất nhưng phải sửa production code.

Cách 2 - Test qua public API: Không access private trực tiếp. Thay vào đó verify hành vi bên ngoài (student state thay đổi, influence sources count, events logged). Không cần sửa production code.

Cách 3 - Reflection: Dùng System.Reflection để get/set private fields. Fragile nhưng không sửa production code.

Khuyến nghị: dùng Cách 2 làm mặc định. Chỉ dùng Cách 1 khi Cách 2 không khả thi.

---

## Thứ tự ưu tiên

1. ScriptedEventPipelineTest - end-to-end quan trọng nhất
2. ScopeTest - từng là nguồn gốc bug lớn
3. TeacherCalmResolveTest - cơ chế giải quyết game
4. TriggerValueMappingTest - regression cho bug cốt lõi
5. LateSubscriptionTest - từng là bug cốt lõi
6. FallbackNearestStudentTest - edge case

---

## Lưu ý

PlayMode tests chạy chậm hơn EditMode do phải khởi động Play mode. Giữ số lượng test vừa phải, mỗi test tập trung một luồng cụ thể.

Dùng Time.timeScale cao (10-20) trong test để rút ngắn thời gian chờ cho time-based triggers. Nhớ reset trong TearDown.

Inject events trực tiếp qua StudentEventManager.LogEvent thay vì chờ autonomous behavior (xác suất, không đảm bảo).

Sau mỗi test cần TearDown: Destroy tất cả GameObjects, reset Time.timeScale = 1, clear static Instance references nếu cần.
