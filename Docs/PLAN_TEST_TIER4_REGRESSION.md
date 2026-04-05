# Plan: Tier 4 - Bug Registry & Regression Tracking

## Mục tiêu
Danh mục tracking tất cả bug đã fix, liên kết đến test tương ứng trong Tier 1/2. Đảm bảo mỗi bug có test bảo vệ, không cần folder test riêng.

## Nguyên tắc

Regression tests KHÔNG nằm trong folder riêng. Chúng chính là các test trong Tier 1 (EditMode) và Tier 2 (PlayMode), được đánh dấu rõ bằng naming convention và comment.

Naming convention: tên test method bắt đầu bằng Regression_ nếu test đó bảo vệ một bug cụ thể. Ví dụ: Regression_TriggerValueMapping_CustomSeverityNotDefault.

File này là danh mục tra cứu: bug nào đã fix, test nào bảo vệ nó, nằm ở file nào.

---

## Bug Registry

### Bug 1 - triggerValue không map vào customSeverity
Mô tả: LoadRuntimeInteractions thiếu customSeverity = runtimeConfig.triggerValue, khiến tất cả time-based interactions dùng default 30s thay vì 20/40/60/80s.
Ngày fix: trước 2026-04-05
Component: StudentInteractionProcessor

Test bảo vệ:
- Tier 2: Assets/Tests/PlayMode/TriggerValueMappingTest.cs - verify LoadRuntimeInteractions map triggerValue đúng
- Tier 1: Assets/Tests/EditMode/TimeTriggerTests.cs - verify GameMath.IsTimeWithinTolerance logic

### Bug 2 - singleStudentMaxDistance quá nhỏ
Mô tả: Distance mặc định 2f, sau đó 5f, cuối cùng 6f. Diagonal desk spacing = 5.41m (sqrt(3^2 + 4.5^2)).
Ngày fix: trước 2026-04-05
Component: StudentInfluenceManager

Test bảo vệ:
- Tier 1: Assets/Tests/EditMode/DistanceCheckTests.cs - verify GameMath.IsWithinInfluenceRange với 5.41m < 6.0m

### Bug 3 - SingleStudent scope không có target
Mô tả: Autonomous events với SingleStudent scope mà targetStudent = null bị bỏ qua. Fix: fallback tìm nearest student, nếu không có thì fallback WholeClass.
Ngày fix: trước 2026-04-05
Component: StudentInfluenceManager

Test bảo vệ:
- Tier 2: Assets/Tests/PlayMode/FallbackNearestStudentTest.cs - verify cả hai kịch bản (có/không có student gần)

### Bug 4 - Late subscription pattern
Mô tả: StudentInteractionProcessor và StudentInfluenceManager gọi OnEnable trước GameStateManager tồn tại, không subscribe được. Fix: fallback subscription trong Start().
Ngày fix: trước 2026-04-05
Component: StudentInteractionProcessor, StudentInfluenceManager, TeacherController

Test bảo vệ:
- Tier 2: Assets/Tests/PlayMode/LateSubscriptionTest.cs - verify processor activates dù OnEnable miss subscription

### Bug 5 - Phantom input từ Unity 6 HID devices
Mô tả: Unity 6 detect 10 unsupported HID devices, gửi phantom joystick data (vertical: -0.079, mouseY: 0.200). Fix: movementDeadZone=0.15, mouseDeadZone=0.05.
Ngày fix: trước 2026-04-05
Component: TeacherController

Test bảo vệ:
- Tier 1: Assets/Tests/EditMode/InputDeadZoneTests.cs - verify GameMath.ApplyDeadZone lọc phantom values

Lưu ý: Unity Input.GetAxis không mock được trong test. Chỉ test được dead zone math logic, không test full input pipeline. Nếu cần test pipeline đầy đủ, phải wrap Input.GetAxis trong interface/abstraction layer - overhead không đáng cho bug này.

### Bug 6 - JSON syntax error trong level config
Mô tả: level_first_day_summer_break.json thiếu ký tự đóng cho eventScopes array và influenceScopeSettings object.
Ngày fix: trước 2026-04-05
Component: UnifiedLevelImporter

Test bảo vệ: Không có test tự động. Đây là lỗi cú pháp JSON do viết tay, không phải lỗi logic. Ngăn ngừa bằng cách validate JSON schema trước khi commit (có thể thêm pre-commit hook nếu muốn).

---

## Template cho bug mới

Khi phát hiện và fix bug mới:

Bước 1 - Ghi bug vào registry này (mô tả, component, ngày)
Bước 2 - Xác định test thuộc Tier nào (logic thuần túy → Tier 1, cần runtime → Tier 2)
Bước 3 - Viết test trong folder Tier tương ứng, đặt tên bắt đầu bằng Regression_
Bước 4 - Verify test FAIL khi revert fix, PASS khi apply fix
Bước 5 - Cập nhật entry trong registry với đường dẫn test file

Nếu bug quá phức tạp hoặc liên quan timing/scenario, có thể thêm assertion vào Tier 3 expected file thay vì viết test riêng.

---

## Bảo trì

Khi refactor code, nếu regression test fail:
- Kiểm tra: behavior vẫn đúng nhưng implementation thay đổi → update test
- Kiểm tra: behavior sai → bug quay lại, fix lại
- Không bao giờ xóa regression test chỉ vì nó fail sau refactor

Khi feature bị xóa hoàn toàn khỏi codebase: xóa regression test tương ứng và ghi note trong registry.
