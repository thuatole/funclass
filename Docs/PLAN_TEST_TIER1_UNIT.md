# Plan: Tier 1 - Unit Testing

## Mục tiêu
Test từng function độc lập, không phụ thuộc Unity scene hay MonoBehaviour lifecycle. Chạy nhanh, không cần Play mode.

## Unity Test Framework - EditMode Tests

### Cách thiết lập
Mở Unity Editor, vào Window > General > Test Runner. Tab "EditMode" chứa các test không cần scene.

Để tạo test assembly, vào thư mục Assets, tạo folder mới tên "Tests" hoặc "Tests/EditMode". Trong folder đó, tạo Assembly Definition File (chuột phải > Create > Testing > Tests Assembly Folder). Unity tự tạo file asmdef cho EditMode tests.

QUAN TRỌNG: Sau khi tạo asmdef, phải thêm reference đến assembly chính của game (Assembly-CSharp hoặc FunClass.Core nếu dùng custom asmdef). Không có reference thì test không compile được. Thao tác: chọn file asmdef trong Inspector, mục "Assembly Definition References", bấm + và chọn assembly của game.

Mỗi test file là một C# class thông thường với attribute TestFixture. Mỗi test method có attribute Test. Không kế thừa MonoBehaviour.

### Cách chạy
Trong Test Runner window, bấm "Run All" để chạy toàn bộ. Kết quả hiện màu xanh (pass) hoặc đỏ (fail) ngay trong Editor, không cần Play mode.

---

## Step 0 - BẮT BUỘC: Tạo GameMath Utility Class

EditMode tests không thể dùng MonoBehaviour. Hiện tại hầu hết logic nằm trong MonoBehaviour (StudentInfluenceManager, StudentAgent, TeacherController). Phải tách logic thuần túy ra trước khi viết test.

Tạo file Assets/Scripts/Core/GameMath.cs - static class, không kế thừa MonoBehaviour. Chứa các method sau:

CalculateInfluenceStrength: nhận baseSeverity, susceptibility, resistance, trả về float. Công thức: baseSeverity × susceptibility × (1 - resistance). Clamp kết quả trong 0-1.

IsWithinInfluenceRange: nhận distance, maxDistance, trả về bool.

ApplyDeadZone: nhận inputValue, deadZone, trả về float (0 nếu abs < deadZone, nguyên giá trị nếu không).

IsTimeWithinTolerance: nhận elapsedTime, targetTime, tolerance, trả về bool. Công thức: abs(elapsed - target) <= tolerance.

GetNextState (escalate): nhận StudentState hiện tại, trả về StudentState tiếp theo. Calm→Distracted→ActingOut→Critical→Critical.

GetPreviousState (deescalate): nhận StudentState hiện tại, trả về StudentState trước đó. Critical→ActingOut→Distracted→Calm→Calm.

Sau khi tạo GameMath, refactor các component để gọi GameMath thay vì tính inline:
- StudentInfluenceManager.ProcessInfluence gọi GameMath.CalculateInfluenceStrength
- TeacherController.HandleMovement và HandleCamera gọi GameMath.ApplyDeadZone
- StudentInteractionProcessor.CheckTimeElapsedCondition gọi GameMath.IsTimeWithinTolerance
- StudentAgent.EscalateState và DeescalateState gọi GameMath.GetNextState/GetPreviousState

Khi đã refactor xong, tất cả Tier 1 tests đều gọi GameMath trực tiếp, không cần MonoBehaviour.

---

## Các test cần viết

### 1. Influence Strength Calculation
Mục tiêu: Verify GameMath.CalculateInfluenceStrength cho ra kết quả đúng.

Test cases:
- susceptibility=1.0, resistance=0.0, base=0.7 → phải ra 0.7
- susceptibility=0.5, resistance=0.5, base=0.7 → phải ra 0.175
- susceptibility=0.0 → phải ra 0.0 (không ảnh hưởng)
- resistance=1.0 → phải ra 0.0 (miễn dịch hoàn toàn)
- Kết quả luôn trong khoảng 0-1, không bao giờ âm
- Edge case: base=0.0 → phải ra 0.0
- Edge case: tất cả bằng 1.0 → phải ra 0.0 (vì 1-resistance=0)

File test: Assets/Tests/EditMode/InfluenceCalculationTests.cs

Đây cũng là regression test cho Bug 2 (distance) và các bug liên quan susceptibility.

### 2. State Transition Logic
Mục tiêu: Verify GameMath.GetNextState và GetPreviousState chuyển đúng thứ tự.

Test cases:
- GetNextState(Calm) → Distracted
- GetNextState(Distracted) → ActingOut
- GetNextState(ActingOut) → Critical
- GetNextState(Critical) → Critical (không vượt quá)
- GetPreviousState(Critical) → ActingOut
- GetPreviousState(ActingOut) → Distracted
- GetPreviousState(Distracted) → Calm
- GetPreviousState(Calm) → Calm (không đi xuống)

File test: Assets/Tests/EditMode/StudentStateTests.cs

### 3. Time Tolerance Check
Mục tiêu: Verify GameMath.IsTimeWithinTolerance hoạt động đúng.

Test cases:
- elapsed=20.0, target=20.0, tolerance=0.5 → true (chính xác)
- elapsed=20.4, target=20.0, tolerance=0.5 → true (trong tolerance)
- elapsed=19.6, target=20.0, tolerance=0.5 → true (trong tolerance, phía trước)
- elapsed=19.3, target=20.0, tolerance=0.5 → false (quá sớm)
- elapsed=25.0, target=20.0, tolerance=0.5 → false (đã qua lâu)
- elapsed=20.5, target=20.0, tolerance=0.5 → true (boundary chính xác)
- elapsed=20.51, target=20.0, tolerance=0.5 → false (vượt boundary)

File test: Assets/Tests/EditMode/TimeTriggerTests.cs

Đây cũng là regression test cho Bug 1 (triggerValue mapping) - verify logic trigger time hoạt động đúng.

### 4. Distance Threshold Logic
Mục tiêu: Verify GameMath.IsWithinInfluenceRange hoạt động đúng.

Test cases:
- distance=4.0, maxDistance=6.0 → true (trong range)
- distance=5.41, maxDistance=6.0 → true (diagonal desk spacing)
- distance=6.0, maxDistance=6.0 → true (boundary chính xác)
- distance=6.01, maxDistance=6.0 → false (vượt boundary)
- distance=0.0, maxDistance=6.0 → true (cùng vị trí)

File test: Assets/Tests/EditMode/DistanceCheckTests.cs

Đây cũng là regression test cho Bug 2 (singleStudentMaxDistance quá nhỏ).

### 5. Dead Zone Filter Logic
Mục tiêu: Verify GameMath.ApplyDeadZone lọc phantom input đúng.

Test cases:
- input=0.079, deadZone=0.15 → ra 0.0 (phantom input bị lọc)
- input=0.2, deadZone=0.15 → ra 0.2 (input thật qua)
- input=-0.079, deadZone=0.15 → ra 0.0 (phantom âm cũng bị lọc)
- input=-0.2, deadZone=0.15 → ra -0.2 (input thật âm qua)
- input=0.0, deadZone=0.15 → ra 0.0
- input=0.15, deadZone=0.15 → ra 0.0 (boundary, dưới deadzone)
- input=0.151, deadZone=0.15 → ra 0.151 (vừa qua boundary)

File test: Assets/Tests/EditMode/InputDeadZoneTests.cs

Đây cũng là regression test cho Bug 5 (phantom input Unity 6).

### 6. ScenarioAsserter Logic (cho Tier 3)
Mục tiêu: Verify ScenarioAsserter.Validate cho ra kết quả đúng khi nhận captured events và expected sequence.

Test cases:
- Captured events chứa đúng tất cả expected events → PASS
- Thiếu một mustOccur event → FAIL với lý do "event not found"
- Event đúng nhưng sai thời gian → FAIL với lý do "wrong timing"
- Event đúng nhưng sai thứ tự (afterEventId) → FAIL với lý do "wrong order"
- mustOccur=false và thiếu → vẫn PASS
- Captured events có thêm events không nằm trong expected → vẫn PASS (không strict)

File test: Assets/Tests/EditMode/ScenarioAsserterTests.cs

---

## Cấu trúc thư mục

Assets/Tests/EditMode/ chứa tất cả unit tests và EditMode regression tests.

Mỗi file test tương ứng một nhóm logic trong GameMath hoặc utility class.

---

## Thứ tự ưu tiên

0. Tạo GameMath utility class và refactor các component (BẮT BUỘC trước tất cả)
1. InfluenceCalculationTests - hay bị sai nhất, đã từng có bug susceptibility/resistance
2. TimeTriggerTests - thời gian trigger là điểm cốt lõi của kịch bản
3. StateTransitionTests - nền tảng toàn bộ game loop
4. DistanceCheckTests - từng bug singleStudentMaxDistance quá nhỏ
5. InputDeadZoneTests - ít thay đổi nhất
6. ScenarioAsserterTests - khi bắt đầu Tier 3
