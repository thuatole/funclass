# Plan: Tier 3 - Scenario Testing (Automated)

## Mục tiêu
Test toàn bộ kịch bản game từ đầu đến cuối một cách tự động, không cần quan sát thủ công. Hỗ trợ nhiều scenario mà không tốn công.

## Nguyên tắc cốt lõi

Ba vấn đề của manual testing:
- Game chạy real-time: 100s scenario = 100s chờ
- Đọc log thủ công: dễ bỏ sót, không scale với nhiều scenario
- Không có pass/fail rõ ràng: phải tự phán đoán

Ba giải pháp tương ứng:
- Time.timeScale = 10 → scenario 100s chạy xong trong 10s thực tế
- GameLogger event capture → tự động ghi lại tất cả MILESTONE events
- Expected sequence file → tự động so sánh, output PASS/FAIL có lý do

---

## Kiến trúc Automated Scenario Runner

### Thành phần 1 - GameLogger Event Capture (Structured Data)
Thêm vào GameLogger:
- Một struct CapturedEvent chứa: component (string), eventType (enum hoặc string), source (string), target (string), elapsed (float), rawMessage (string)
- Một static event OnMilestoneLogged(CapturedEvent) kích hoạt mỗi khi Milestone được gọi
- Một static List CapturedEvents để lưu trữ tất cả events

ScenarioRunner subscribe vào event này để thu thập milestones.

QUAN TRỌNG: Dùng structured data (component + eventType + source + target) thay vì chỉ string matching trên rawMessage. Lý do: nếu format log message thay đổi (thêm dấu ngoặc, đổi ký tự mũi tên, v.v.) thì string matching sẽ fail mặc dù logic đúng. Structured matching ổn định hơn.

Để hỗ trợ structured data, thêm overload cho GameLogger.Milestone:
- Milestone(component, message) - giữ nguyên như hiện tại
- Milestone(component, message, eventType, source, target) - thêm structured fields

Không thay đổi behavior hiện tại của Debug.Log, chỉ thêm event và structured fields.

### Thành phần 2 - Expected Sequence File (JSON)
Mỗi scenario có một file JSON nhỏ trong Assets/Tests/Scenarios/.

Cấu trúc mỗi assertion trong file:
- id: tên định danh
- component: component nào log (StudentInteractionProcessor, StudentInfluenceManager, v.v.)
- eventType: loại event (KnockedOverObject, WanderingAround, ThrowingObject, v.v.) - match structured field, không phải string trong message
- source: tên student nguồn (optional)
- target: tên student đích (optional)
- minTime: thời gian sớm nhất event được phép xảy ra
- maxTime: thời gian muộn nhất
- mustOccur: true nếu bắt buộc phải xảy ra, false nếu optional
- afterEventId: nếu có, event này phải xảy ra SAU event kia (kiểm tra thứ tự)

Matching ưu tiên structured fields (eventType, source, target). Chỉ fallback sang messageContains khi structured fields không đủ.

Khi thêm scenario mới: chỉ cần thêm một file JSON, không cần viết thêm code.

### Thành phần 3 - ScenarioRunner MonoBehaviour
Đặt trong scene hoặc gọi từ PlayMode test.

Flow hoạt động:
- Nhận tên scenario và đường dẫn expected file
- Set Time.timeScale = testTimeScale (mặc định 10, configurable)
- Clear captured events list
- Subscribe vào GameLogger.OnMilestoneLogged
- Load level: gọi LevelManager.Instance.LoadLevel(scenarioLevelName) hoặc SceneManager.LoadScene nếu level là scene riêng. Cần ghi rõ trong expected file tên level/scene cần load.
- Subscribe vào GameStateManager.OnStateChanged để detect game kết thúc
- Chờ game kết thúc (GameOver, Win) hoặc timeout (maxRealTimeSeconds, mặc định 30s)
- Set Time.timeScale = 1
- Chạy ScenarioAsserter.Validate(capturedEvents, expectedAssertions)
- Output kết quả và ghi file

### Thành phần 4 - Scripted Teacher Actions
Để test scenario có teacher intervention, ScenarioRunner cần tự động hóa hành động teacher.

Thêm field trong expected file: teacherActions - list các hành động teacher tại thời gian cụ thể:
- time: giây game time thực hiện
- action: loại hành động (CalmStudent, EscortBack, UseItem, v.v.)
- targetStudent: tên student mục tiêu

ScenarioRunner trong Update() kiểm tra: nếu elapsed time đạt mốc, gọi TeacherController API trực tiếp. Ví dụ: tại giây 42, gọi TeacherController.Instance.CalmStudent(binh) - bypass input hoàn toàn.

Với scenario "no teacher": teacherActions rỗng, TeacherController bị disable hoặc không nhận input.

### Thành phần 5 - ScenarioAsserter (logic thuần túy)
Không có MonoBehaviour. Nhận danh sách captured events và expected assertions, trả về kết quả validation.

Matching logic cho mỗi assertion:
- Lọc captured events theo component (bắt buộc match)
- Lọc tiếp theo eventType nếu có (structured match)
- Lọc tiếp theo source/target nếu có
- Nếu không dùng structured fields, fallback sang messageContains
- Check time range: event.elapsed trong minTime - maxTime
- Check ordering: nếu afterEventId set, event phải có index lớn hơn afterEvent

Kết quả cho mỗi assertion:
- PASS: event tìm thấy, đúng time, đúng order
- FAIL - NOT_FOUND: mustOccur=true nhưng không tìm thấy event khớp
- FAIL - WRONG_TIMING: event tìm thấy nhưng ngoài minTime/maxTime
- FAIL - WRONG_ORDER: event tìm thấy nhưng xảy ra trước afterEventId

ScenarioAsserter là class thuần túy, unit test riêng ở Tier 1 (xem Tier 1 test 6).

---

## Test Series - Từ đơn giản đến phức tạp

Logic: mỗi series chỉ thêm 1 biến số so với series trước. Nếu series N fail, biết chắc bug nằm ở biến số mới thêm.

Mỗi series cần 2 files nằm chung trong 1 folder:
- level_config.json → import vào Unity tạo scene
- assertions.json → ScenarioRunner đọc để verify

Tất cả nằm trong Assets/Tests/Scenarios/SeriesN/

Series 1-3 có level_config.json riêng (ít students, timing ngắn, autonomous behavior tắt).
Series 4-6 dùng level_first_day_summer_break.json đã có (full level), chỉ có assertions.json.

### Series 1 - Isolated Single Student (0 biến số)
Folder: Assets/Tests/Scenarios/Series1/
- level_config.json (1 student, 0 interaction)
- assertions.json

Biến số: 1 student, 0 interaction, 0 teacher
Verify: systems khởi động đúng (StudentAgent activated, Processor activated, InfluenceManager activated). Student giữ Calm trong 15s đầu.
Nếu fail → bug ở system initialization hoặc autonomous behavior quá aggressive.

### Series 2 - Single Scripted Event (thêm 1 scripted event)
Folder: Assets/Tests/Scenarios/Series2/
- level_config.json (2 students, 1 event tại 10s)
- assertions.json

Biến số: 2 students, 1 scripted event (KnockedOverObject tại 10s), influence apply
Verify: event trigger đúng thời gian, influence apply đúng target, target state thay đổi.
Nếu fail → bug ở pipeline (Processor → EventManager → InfluenceManager → StateChange).

### Series 3 - Event Chain (thêm nhiều events sequential)
Folder: Assets/Tests/Scenarios/Series3/
- level_config.json (3 students, 4 events tại 10/20/30/40s)
- assertions.json

Biến số: 3 students, 4 scripted events (10s/20s/30s/40s), influence chain
Verify: tất cả events trigger đúng thứ tự, influence chain Bình→Lan + Nam→Lan hoạt động.
Nếu fail → bug ở timing, ordering, hoặc multi-source influence.

### Series 3b - Throw Lifecycle (isolated throw pipeline)
Folder: Assets/Tests/Scenarios/Series3b/
- level_config.json (2 students khoảng cách >2m, 1 ThrowingObject scripted tại 10s)
- assertions.json

Biến số mới: ThrowAt pipeline (walk closer → attach trực tiếp lên target, không qua tay source)
Verify:
- `throwables_spawned` — ThrowablesSpawned tại 0-2s (level start)
- `target_attach` — ObjectAttachedToTarget target=Lan tại 10-10.5s
- `influence_applied` — InfluenceApplied source=Bình target=Lan tại 10-10.5s, afterEventId=target_attach
Nếu fail → bug ở AttachToHead/AttachToDesk hoặc event log ordering

### Series 4 - Teacher Intervention (thêm teacher action)
Folder: Assets/Tests/Scenarios/Series4/
- assertions.json (level dùng level_first_day_summer_break.json)

Biến số: thêm 1 teacher action (CalmStudent tại 42s)
Verify: teacher calm → influence resolved → chain bị cắt → Lan không lên Critical.
Nếu fail → bug ở resolve influence logic hoặc teacher action pipeline.

### Series 5 - Full Level (full flow, 2 kịch bản)
Folder: Assets/Tests/Scenarios/Series5/
- assertions_lose.json (no teacher → GameOver)
- assertions_win.json (teacher intervenes → Win)

Biến số: full level + game end condition
5a Lose: game chạy đến GameOver đúng lý do (disruption vượt threshold).
5b Win: teacher can thiệp đúng lúc → disruption giảm → Win.
Nếu fail → bug ở game end detection, disruption calculation, hoặc win condition.

### Series 6 - Edge Cases (biến số sai)
Folder: Assets/Tests/Scenarios/Series6/
- assertions.json (level dùng level_first_day_summer_break.json)

Biến số: teacher calm SAI target (Mai thay vì Bình)
Verify: calm wrong target KHÔNG resolve influence → chain vẫn tiếp tục → GameOver.
Nếu fail → bug ở resolve logic (resolve nhầm student).

---

## Logging trong quá trình chạy

### Progress logs (dùng GameLogger tiers hiện có)
ScenarioRunner log bằng Milestone:
- Khi bắt đầu: tên scenario, timeScale, số assertions cần kiểm tra
- Khi kết thúc: tóm tắt kết quả (X passed, Y failed)

ScenarioAsserter log từng assertion sau khi validate:
- Pass → Milestone với ký hiệu ✓ và thời gian thực tế event xảy ra
- Fail → Error với lý do cụ thể (not found / wrong timing / wrong order)

Trace tier dùng để log từng captured event khi đến (tắt mặc định, bật khi cần debug).

### Result block cuối mỗi scenario
ScenarioRunner in một block tóm tắt sau khi chạy xong:

Ví dụ output (Series 3 - Event Chain):
- SCENARIO RESULT: Series 3 - Event Chain
- End: Timeout | GameTime: 55.0s | RealTime: 10.5s | timeScale=10
- Assertions: 6 total, 5 passed, 1 failed
- FAIL [lan_influenced_by_nam] FailNotFound — No matching event found
- PASS [binh_wandering_10s] at 10.1s, PASS [binh_knocked_20s] at 20.0s, ...

---

## Ghi kết quả ra file (tự động)

ScenarioRunner tự gọi System.IO.File.WriteAllText sau mỗi lần chạy, không cần thao tác thủ công.

File kết quả lưu tại Assets/Tests/Results/ với tên gồm scenario name và timestamp, ví dụ Series_3_-_Event_Chain_2026-04-05_14-30-00.txt.

Nội dung file giống result block trong Console, thêm danh sách đầy đủ tất cả captured events để tiện debug.

Lợi ích: khi chạy batch nhiều scenario, kết quả không trôi mất trong Console. Có thể so sánh kết quả giữa các lần chạy để phát hiện regression mới.

Lưu ý: Assets/Tests/Results/ nên thêm vào .gitignore vì đây là output tạm thời, không phải source code.

---

## Cách chạy nhiều scenario

### Từ PlayMode Test
Tạo một test method cho mỗi scenario file. Mỗi test: khởi tạo ScenarioRunner với file tương ứng, yield return WaitUntil(runner.IsComplete), Assert.IsTrue(runner.Result.AllPassed).

Với timeScale=20, chạy 10 scenario mỗi scenario 100s game time = tổng 50s thực tế.

### Batch Runner trong Editor
Tạo custom Editor window "Scenario Batch Runner" cho phép chọn nhiều scenario file, bấm "Run All", xem kết quả dạng table: tên scenario, PASS/FAIL, danh sách failed assertions. Mỗi lần Run All tự động ghi toàn bộ kết quả ra một file batch report.

Không cần vào Play mode thủ công cho từng scenario.

---

## Thêm scenario mới

Quy trình chỉ cần 3 bước:
- Chạy game một lần và quan sát MILESTONE logs để biết expected timeline
- Tạo file JSON expected sequence dựa trên log đó
- Thêm vào batch runner

Không cần viết thêm code C#.

---

## Thứ tự triển khai chi tiết

### Phase 1 - Setup hệ thống (làm 1 lần)

Bước 1.1 [CODE] Thêm CapturedEvent struct vào GameLogger.cs
- Thêm struct CapturedEvent chứa: component, eventType, source, target, elapsed, rawMessage
- Thêm static List capturedEvents để lưu trữ
- Thêm static event OnMilestoneLogged(CapturedEvent)
- Thêm method ClearCapturedEvents() để reset list

Bước 1.2 [CODE] Thêm Milestone overload có structured fields vào GameLogger.cs
- Thêm overload: Milestone(component, message, eventType, source, target)
- Overload này tạo CapturedEvent với đủ structured fields, add vào list, kích hoạt event
- Sửa overload cũ Milestone(component, message): vẫn tạo CapturedEvent nhưng eventType/source/target để rỗng, vẫn add vào list và kích hoạt event
- Đảm bảo Debug.Log vẫn hoạt động như trước

Bước 1.3 [CODE] Refactor các component để gọi structured Milestone
- StudentInteractionProcessor: khi trigger interaction, gọi Milestone với eventType, source, target
- StudentInfluenceManager: khi influence applied, gọi Milestone với eventType, source, target
- StudentAgent: khi state changed, gọi Milestone với eventType="StateChanged", source=studentName
- TeacherController: khi calm/escort, gọi Milestone với eventType, target
- **StudentInteractableObject**: khi attach/detach object, gọi Milestone với:
  - eventType="ObjectAttachedToTarget", target=targetStudentName (attach head hoặc desk — không có source phase)
  - eventType="ObjectKnockedOver", source=studentName
  - eventType="ThrowablesSpawned" (ThrowableSpawner, khi spawn xong lúc level start)
- Chỉ refactor các MILESTONE calls quan trọng, không cần refactor Detail/Trace

Bước 1.4 [UNITY EDITOR] Verify logs vẫn hoạt động
- Chạy game Play mode bình thường
- Mở Console window, kiểm tra MILESTONE logs vẫn hiện đúng format
- Không có lỗi compile, không có regression

Bước 1.5 [CODE] Viết ScenarioAsserter class
- Tạo file mới Assets/Scripts/Core/Testing/ScenarioAsserter.cs
- Class thuần túy (không MonoBehaviour)
- Method Validate: nhận List CapturedEvent + List ExpectedAssertion, trả về ValidationResult
- ValidationResult chứa: allPassed (bool), list kết quả từng assertion (PASS/FAIL + lý do)
- Matching logic: filter by component → match eventType/source/target → check time range → check order

Bước 1.6 [CODE] Viết ScenarioRunner MonoBehaviour
- Tạo file mới Assets/Scripts/Core/Testing/ScenarioRunner.cs
- SerializeField: scenarioFilePath, testTimeScale (default 10), maxRealTimeSeconds (default 30)
- Flow: OnEnable subscribe OnMilestoneLogged, Start set timeScale và clear events
- Update: check game end (GameStateManager state) hoặc timeout
- Khi kết thúc: reset timeScale=1, load expected file, gọi ScenarioAsserter.Validate, log result block, ghi file kết quả

Bước 1.7 [CODE] Viết ScenarioRunner ghi file kết quả tự động
- Ghi vào Assets/Tests/Results/ với tên scenario_timestamp.txt
- Nội dung: result summary + danh sách đầy đủ captured events

Bước 1.8 [UNITY EDITOR] Tạo folder structure
- Tạo folder Assets/Tests/Scenarios/ (chứa expected JSON files)
- Tạo folder Assets/Tests/Results/ (chứa output, đã có trong .gitignore)
- Tạo folder Assets/Scripts/Core/Testing/ (chứa ScenarioRunner, ScenarioAsserter)

### Phase 2 - Chạy scenario tests theo thứ tự series

Tất cả scenario JSON files đã tạo sẵn trong Assets/Tests/Scenarios/SeriesN/. Chạy tuần tự từ Series 1 đến 6, chỉ qua series tiếp khi series trước PASS.

Bước 2.1 [UNITY EDITOR] Import level config cho Series 1-3
- Series 1-3 có level_config.json riêng, cần import qua UnifiedLevelImporter để tạo scene
- Series 1: Assets/Tests/Scenarios/Series1/level_config.json (1 student, 0 interaction)
- Series 2: Assets/Tests/Scenarios/Series2/level_config.json (2 students, 1 event tại 10s)
- Series 3: Assets/Tests/Scenarios/Series3/level_config.json (3 students, 4 events)
- Series 4-6 dùng scene FirstDaySummerBreak đã có, không cần import thêm

Bước 2.2 [UNITY EDITOR] Thêm ScenarioRunner vào scene và chạy Series 1
- Mở scene vừa import từ Series 1
- Tạo GameObject mới tên "ScenarioRunner"
- Add component ScenarioRunner
- Gán scenarioFilePath = Assets/Tests/Scenarios/Series1/assertions.json
- Đặt testTimeScale = 10
- Bấm Play, kiểm tra Console: 4 assertions phải PASS (system init)
- Kiểm tra Assets/Tests/Results/ có file kết quả mới

Bước 2.3 [UNITY EDITOR] Chạy Series 2
- Mở scene Series 2, thêm ScenarioRunner
- Gán scenarioFilePath = Assets/Tests/Scenarios/Series2/assertions.json
- Bấm Play, verify: InteractionsLoaded, KnockedOverObject tại 10s, InfluenceApplied, StateChanged
- Nếu FAIL: kiểm tra pipeline Processor -> EventManager -> InfluenceManager -> StateChange

Bước 2.4 [UNITY EDITOR] Chạy Series 3
- Mở scene Series 3, thêm ScenarioRunner
- Gán scenarioFilePath = Assets/Tests/Scenarios/Series3/assertions.json
- Bấm Play, verify: 4 events trigger đúng thứ tự 10/20/30/40s, influence chains hoạt động

Bước 2.5 [UNITY EDITOR] Chạy Series 4
- Mở scene FirstDaySummerBreak, thêm ScenarioRunner
- Gán scenarioFilePath = Assets/Tests/Scenarios/Series4/assertions.json
- Bấm Play, verify: teacher calm tại 42s, influence resolved, chain bị cắt

Bước 2.6 [UNITY EDITOR] Chạy Series 5 (2 kịch bản)
- Cùng scene FirstDaySummerBreak
- Chạy 1: scenarioFilePath = Assets/Tests/Scenarios/Series5/assertions_lose.json (no teacher -> GameOver)
- Chạy 2: scenarioFilePath = Assets/Tests/Scenarios/Series5/assertions_win.json (teacher -> Win)

Bước 2.7 [UNITY EDITOR] Chạy Series 6
- Cùng scene FirstDaySummerBreak
- Gán scenarioFilePath = Assets/Tests/Scenarios/Series6/assertions.json
- Verify: calm wrong target -> chain tiếp tục -> GameOver

Bước 2.8 [CODE hoặc JSON] Fix nếu FAIL
- Nếu assertion fail do tolerance quá chặt → sửa minTime/maxTime trong assertions.json
- Nếu assertion fail do bug game → fix code → chạy lại series bị fail
- Nếu assertion fail do structured fields không match → kiểm tra refactor ở bước 1.3

### Phase 3 - Thêm scenario mới (khi cần mở rộng)

Bước 3.1 Xác định biến số mới cần test
- Mỗi scenario mới chỉ thêm 1 biến số so với series trước
- Ví dụ: Series 7 có thể test autonomous behavior ON (calmInteractionChance > 0)

Bước 3.2 Tạo folder và files
- Tạo folder Assets/Tests/Scenarios/Series7/
- Nếu cần level riêng: tạo level_config.json, import qua UnifiedLevelImporter
- Tạo assertions.json với assertions phù hợp

Bước 3.3 Chạy và verify
- Thêm ScenarioRunner vào scene, gán scenarioFilePath
- Bấm Play, kiểm tra kết quả

### Phase 4 - (Optional) Batch Runner

Bước 4.1 [CODE] Tạo Batch Runner Editor window
- File Assets/Scripts/Editor/ScenarioBatchRunner.cs
- Custom EditorWindow cho phép chọn nhiều JSON files, bấm Run All

Bước 4.2 [UNITY EDITOR] Chạy batch
- Mở window từ menu, chọn files, bấm Run All
- Xem kết quả tổng hợp

---

## Lưu ý

### Throw và timeScale
ExecuteThrowAt là synchronous (không coroutine, không WaitForSeconds). Attach object xảy ra ngay trong cùng frame với event log → assertions có thể dùng window nhỏ (±0.1s). Parent re-attach frame-based, an toàn với mọi timeScale.

### timeScale và NavMesh
Mặc định dùng timeScale = 10 (không phải 20). Lý do: NavMeshAgent ở timeScale quá cao có thể teleport qua walls hoặc miss waypoints.

Trước khi dùng, chạy thủ công FirstDaySummerBreak ở timeScale 5, 10, 15, 20 để tìm ngưỡng an toàn. Nếu student navigation bị lỗi (teleport, đi xuyên tường, kẹt), giảm timeScale. Ghi ngưỡng an toàn vào config.

FixedUpdate và physics vẫn chạy theo fixedDeltaTime, nhưng timeScale ảnh hưởng cả hai.

### Timeout safety
Nếu game không kết thúc sau maxRealTimeSeconds (mặc định 30s), ScenarioRunner tự dừng và mark FAIL với lý do "timeout - game did not end within expected time". Ở timeScale=10, 30s real = 300s game time, đủ cho hầu hết scenario.

### Structured events backward compatibility
Khi thêm structured overload cho GameLogger.Milestone, giữ overload cũ (component, message) hoạt động bình thường. Các component chưa refactor sang structured vẫn dùng overload cũ, ScenarioAsserter fallback sang messageContains cho những events đó.
