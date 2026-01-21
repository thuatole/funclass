# Hướng dẫn thêm hành động mới cho học sinh

## Tổng quan

Hệ thống hiện tại được thiết kế theo mô hình **Event-Driven**, việc thêm hành động mới chỉ cần **3-4 bước đơn giản**.

---

## Ví dụ: Thêm hành động "Ị ra sàn" (Poop)

### Bước 1: Thêm Event Type

File: `Assets/Scripts/Core/StudentEvent.cs`

```csharp
public enum StudentEventType
{
    // ... existing types ...
    MessCreated,
    MessCleaned,
    StudentActedOut,

    // === THÊM MỚI ===
    PoopedOnFloor,      // Ị ra sàn
    PeedOnFloor,        // Tè ra sàn
    PokedWithPen,       // Chọt viết vào bạn
    EatingInClass,      // Ăn vụn trong lớp
    PassingNotes,       // Chuyền giấy
    ChewingGum,         // Nhai kẹo cao su
    SleepingInClass,    // Ngủ gật
    TalkingBack,        // Cãi lại giáo viên
}
```

### Bước 2: Cấu hình Influence

File: `Assets/Scripts/Core/StudentInfluenceManager.cs`

**2a. Thêm vào IsInfluenceTrigger():**
```csharp
private bool IsInfluenceTrigger(StudentEventType eventType)
{
    return eventType switch
    {
        StudentEventType.ThrowingObject => true,
        StudentEventType.MakingNoise => true,
        StudentEventType.KnockedOverObject => true,
        StudentEventType.MessCreated => true,

        // === THÊM MỚI ===
        StudentEventType.PoopedOnFloor => true,    // Gây influence mạnh
        StudentEventType.PeedOnFloor => true,      // Gây influence mạnh
        StudentEventType.PokedWithPen => true,     // SingleStudent influence
        StudentEventType.EatingInClass => true,    // Influence nhẹ
        StudentEventType.PassingNotes => true,     // SingleStudent influence
        StudentEventType.ChewingGum => false,      // Không influence
        StudentEventType.SleepingInClass => false, // Không influence
        StudentEventType.TalkingBack => true,      // Influence cả lớp

        _ => false
    };
}
```

**2b. Thêm vào GetInfluenceSeverity():**
```csharp
private float GetInfluenceSeverity(StudentEventType eventType)
{
    return eventType switch
    {
        StudentEventType.ThrowingObject => 0.9f,
        StudentEventType.MessCreated => 0.85f,

        // === THÊM MỚI ===
        StudentEventType.PoopedOnFloor => 0.95f,   // Rất mạnh - cả lớp panic
        StudentEventType.PeedOnFloor => 0.9f,      // Mạnh
        StudentEventType.PokedWithPen => 0.7f,     // Trung bình - chỉ 1 người
        StudentEventType.EatingInClass => 0.3f,   // Nhẹ
        StudentEventType.PassingNotes => 0.4f,    // Nhẹ
        StudentEventType.TalkingBack => 0.6f,     // Trung bình

        _ => 0.3f
    };
}
```

### Bước 3: Tạo hành động trigger

**Option A: Thêm vào StudentAgent.cs**

```csharp
// Trong StudentAgent.cs
public void PoopOnFloor()
{
    if (StudentEventManager.Instance != null)
    {
        // Tạo mess object (giống như vomit)
        if (StudentMessCreator.Instance != null)
        {
            StudentMessCreator.Instance.CreateMessAtStudent(this, "Poop");
        }

        // Log event với WholeClass influence
        StudentEventManager.Instance.LogEvent(
            this,
            StudentEventType.PoopedOnFloor,
            "had an accident on the floor",
            null,
            null,
            InfluenceScope.WholeClass  // Ảnh hưởng cả lớp
        );
    }
}

public void PokeStudentWithPen(StudentAgent target)
{
    if (StudentEventManager.Instance != null)
    {
        StudentEventManager.Instance.LogEvent(
            this,
            StudentEventType.PokedWithPen,
            $"poked {target.Config?.studentName} with a pen",
            null,
            target,                       // Target cụ thể
            InfluenceScope.SingleStudent  // Chỉ ảnh hưởng 1 người
        );
    }
}
```

**Option B: Tạo component riêng**

```csharp
// Assets/Scripts/Core/Behaviors/StudentAccidentBehavior.cs
public class StudentAccidentBehavior : MonoBehaviour
{
    private StudentAgent studentAgent;

    [Header("Accident Chances")]
    [Range(0f, 1f)]
    public float poopChance = 0.01f;  // 1% mỗi phút khi Critical
    [Range(0f, 1f)]
    public float peeChance = 0.02f;   // 2% mỗi phút khi Critical

    void Start()
    {
        studentAgent = GetComponent<StudentAgent>();
    }

    void Update()
    {
        // Chỉ xảy ra khi ở trạng thái Critical
        if (studentAgent.CurrentState != StudentState.Critical) return;

        // Random check mỗi frame
        if (Random.value < poopChance * Time.deltaTime / 60f)
        {
            TriggerAccident(StudentEventType.PoopedOnFloor);
        }
    }

    private void TriggerAccident(StudentEventType type)
    {
        // Log event
        StudentEventManager.Instance?.LogEvent(
            studentAgent,
            type,
            "had an accident",
            null,
            null,
            InfluenceScope.WholeClass
        );

        // Create mess
        // ...
    }
}
```

### Bước 4 (Optional): Thêm vào StudentConfig

Nếu muốn cấu hình per-student:

```csharp
// Trong StudentConfig.cs
[Header("Special Behaviors")]
public bool canHaveAccidents = false;
public bool canPokeOthers = false;
public bool canEatInClass = false;
public bool canPassNotes = false;
```

---

## Các loại Influence Scope

| Scope | Mô tả | Điều kiện | Ví dụ |
|-------|-------|-----------|-------|
| `None` | Không influence | - | Ngủ gật, nhai kẹo |
| `WholeClass` | Ảnh hưởng cả lớp | Cùng vị trí (inside/outside) | Ị/tè ra sàn, nôn |
| `SingleStudent` | Ảnh hưởng 1 người | Distance <= 2m | Chọt viết, chuyền giấy |

---

## Template nhanh cho hành động mới

### 1. Hành động không cần target (WholeClass)

```csharp
// VD: Hét to, đập bàn
public void MakeLoudNoise()
{
    StudentEventManager.Instance?.LogEvent(
        this,
        StudentEventType.YourNewEventType,
        "description",
        null,   // targetObject
        null,   // targetStudent
        InfluenceScope.WholeClass
    );
}
```

### 2. Hành động cần target cụ thể (SingleStudent)

```csharp
// VD: Chọt viết, chuyền giấy, cười nhạo
public void InteractWithStudent(StudentAgent target)
{
    // Check distance trước
    float dist = Vector3.Distance(transform.position, target.transform.position);
    if (dist > 2f)
    {
        // Move closer first
        StudentMovementManager.Instance?.MoveToStudent(
            this, target, 2f,
            () => ExecuteInteraction(target)
        );
    }
    else
    {
        ExecuteInteraction(target);
    }
}

private void ExecuteInteraction(StudentAgent target)
{
    StudentEventManager.Instance?.LogEvent(
        this,
        StudentEventType.YourNewEventType,
        $"did something to {target.Config?.studentName}",
        null,
        target,
        InfluenceScope.SingleStudent
    );
}
```

### 3. Hành động tạo mess (vật thể trên sàn)

```csharp
public void CreateMess()
{
    // Tạo mess object
    StudentMessCreator.Instance?.CreateMessAtStudent(this, "MessType");

    // Log event
    StudentEventManager.Instance?.LogEvent(
        this,
        StudentEventType.MessCreated,
        "created a mess",
        null, null,
        InfluenceScope.WholeClass
    );
}
```

---

## Checklist thêm hành động mới

- [ ] Thêm enum vào `StudentEventType`
- [ ] Thêm vào `IsInfluenceTrigger()` (true/false)
- [ ] Thêm vào `GetInfluenceSeverity()` (0.0 - 1.0)
- [ ] Tạo method trigger trong `StudentAgent` hoặc component riêng
- [ ] (Optional) Thêm config trong `StudentConfig`
- [ ] (Optional) Thêm animation trigger
- [ ] (Optional) Thêm sound effect
- [ ] (Optional) Thêm visual effect

---

## Ví dụ đầy đủ: Thêm "Chọt viết vào bạn"

### StudentEvent.cs
```csharp
PokedWithPen,  // Thêm vào enum
```

### StudentInfluenceManager.cs
```csharp
// IsInfluenceTrigger
StudentEventType.PokedWithPen => true,

// GetInfluenceSeverity
StudentEventType.PokedWithPen => 0.7f,
```

### StudentAgent.cs
```csharp
public void PokeNearbyStudent()
{
    // Tìm student gần nhất
    var target = FindNearestStudent(3f);
    if (target == null) return;

    // Di chuyển lại gần nếu cần
    float dist = Vector3.Distance(transform.position, target.transform.position);
    if (dist > 2f)
    {
        StudentMovementManager.Instance?.MoveToStudent(this, target, 1.5f, () => {
            ExecutePoke(target);
        });
    }
    else
    {
        ExecutePoke(target);
    }
}

private void ExecutePoke(StudentAgent target)
{
    // Animation (future)
    // TriggerAnimation("Poke");

    // Log event
    StudentEventManager.Instance?.LogEvent(
        this,
        StudentEventType.PokedWithPen,
        $"poked {target.Config?.studentName} with a pen",
        null,
        target,
        InfluenceScope.SingleStudent
    );

    // Target reaction
    target.TriggerReaction(StudentReactionType.Angry, 3f);
}

private StudentAgent FindNearestStudent(float maxDist)
{
    StudentAgent nearest = null;
    float nearestDist = maxDist;

    foreach (var student in FindObjectsOfType<StudentAgent>())
    {
        if (student == this) continue;
        float dist = Vector3.Distance(transform.position, student.transform.position);
        if (dist < nearestDist)
        {
            nearestDist = dist;
            nearest = student;
        }
    }
    return nearest;
}
```

### StudentConfig.cs
```csharp
[Header("Social Behaviors")]
public bool canPokeOthers = false;
```

---

## Kết luận

Hệ thống **Event-Driven** hiện tại cho phép:
- ✅ Thêm hành động mới trong **5-10 phút**
- ✅ Không cần sửa code core
- ✅ Dễ dàng điều chỉnh influence severity
- ✅ Hỗ trợ cả WholeClass và SingleStudent
- ✅ Tự động integrate với icon system
- ✅ Tự động resolve khi calm down

**Complexity: LOW** - Chỉ cần thêm enum + 2-3 dòng switch case + method trigger.
