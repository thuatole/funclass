# Concurrent Influences Mechanism - How Multiple Events Interact
**Date:** 2026-01-19

## ❓ Question

**Vietnamese:** "nếu hành động clean mess xảy ra sau khi hành động throw object thì sao? nó có tác động đè kết quả hành động sau? cơ chế hiện tại phân biệt nhiều tác động xảy ra song song ra sao?"

**English:** "If the clean mess action happens after the throw object action, what happens? Does it overwrite the later action's result? How does the current mechanism distinguish multiple concurrent influences?"

---

## ✅ Answer: Multiple Influences Are Tracked Independently

**The system CORRECTLY handles concurrent influences!** Each influence source is tracked separately and resolved independently. Clean mess does NOT overwrite or cancel throw object.

---

## 🔍 How It Works

### Core Data Structure: `StudentInfluenceSources`

**File:** [InfluenceSource.cs:24-46](../InfluenceSource.cs#L24-L46)

```csharp
public class StudentInfluenceSources
{
    private StudentAgent targetStudent;
    private List<InfluenceSource> activeSources = new List<InfluenceSource>();

    // Each InfluenceSource contains:
    // - sourceStudent: Who is causing the influence
    // - eventType: What type of event (MessCreated, ThrowingObject, etc.)
    // - influenceStrength: How strong the influence is
    // - isResolved: Whether the source has been resolved
    // - timestamp: When it was created
}
```

**Key Point:** Each student maintains a **list** of influence sources, not just one. Multiple sources can coexist!

---

## 📊 Example Scenario: Vomit + Throwing

### Timeline:

```
T=0s:  Student_A vomits
       └─> MessCreated event
           └─> WholeClass influence
               └─> Student_C.InfluenceSources.AddSource(Student_A, MessCreated, strength=0.8)

T=5s:  Student_B throws object at Student_C
       └─> ThrowingObject event
           └─> SingleStudent influence
               └─> Student_C.InfluenceSources.AddSource(Student_B, ThrowingObject, strength=0.7)

T=10s: Teacher cleans mess
       └─> MessCleaned event
           └─> ResolveInfluenceSourcesFromStudent(Student_A)
               └─> Student_C.InfluenceSources.ResolveSource(Student_A)
                   └─> Marks Student_A's MessCreated as resolved

T=15s: Teacher calms Student_B
       └─> StudentCalmed event
           └─> ResolveInfluenceSourcesFromStudent(Student_B)
               └─> Student_C.InfluenceSources.ResolveSource(Student_B)
                   └─> Marks Student_B's ThrowingObject as resolved
```

---

## 🧬 Student_C's Influence Sources Over Time

### At T=0s (After Vomit):
```csharp
Student_C.InfluenceSources.activeSources = [
    {
        sourceStudent: Student_A,
        eventType: MessCreated,
        influenceStrength: 0.8,
        isResolved: false,
        timestamp: 0s
    }
]
```

**State:** Student_C has 1 unresolved source

---

### At T=5s (After Throw):
```csharp
Student_C.InfluenceSources.activeSources = [
    {
        sourceStudent: Student_A,
        eventType: MessCreated,
        influenceStrength: 0.8,
        isResolved: false,
        timestamp: 0s
    },
    {
        sourceStudent: Student_B,
        eventType: ThrowingObject,
        influenceStrength: 0.7,
        isResolved: false,
        timestamp: 5s
    }
]
```

**State:** Student_C has 2 unresolved sources (both active!)

---

### At T=10s (After Clean Mess):
```csharp
Student_C.InfluenceSources.activeSources = [
    {
        sourceStudent: Student_A,
        eventType: MessCreated,
        influenceStrength: 0.8,
        isResolved: true,  // ✓ RESOLVED
        timestamp: 0s
    },
    {
        sourceStudent: Student_B,
        eventType: ThrowingObject,
        influenceStrength: 0.7,
        isResolved: false,  // ✗ STILL UNRESOLVED
        timestamp: 5s
    }
]
```

**State:** Student_C has 1 unresolved source (Student_B still affecting)

**IMPORTANT:** Clean mess does NOT remove or overwrite Student_B's influence!

---

### At T=15s (After Calm Student_B):
```csharp
Student_C.InfluenceSources.activeSources = [
    {
        sourceStudent: Student_A,
        eventType: MessCreated,
        influenceStrength: 0.8,
        isResolved: true,  // ✓ RESOLVED
        timestamp: 0s
    },
    {
        sourceStudent: Student_B,
        eventType: ThrowingObject,
        influenceStrength: 0.7,
        isResolved: true,  // ✓ RESOLVED
        timestamp: 5s
    }
]
```

**State:** Student_C has 0 unresolved sources (all resolved!)

---

## 🔧 How Resolution Works

### ResolveSource() Method

**File:** [InfluenceSource.cs:93-122](../InfluenceSource.cs#L93-L122)

```csharp
public void ResolveSource(StudentAgent sourceStudent)
{
    // Loop through ALL sources
    foreach (var source in activeSources)
    {
        // Only resolve sources from THIS specific student
        if (source.sourceStudent == sourceStudent && !source.isResolved)
        {
            source.isResolved = true;  // Mark as resolved
            resolvedCount++;
        }
    }
}
```

**Key Points:**

1. **Only resolves sources from the specified student**
   - Clean mess resolves Student_A's sources only
   - Does NOT touch Student_B's sources

2. **Marks as resolved, does NOT delete**
   - Sources remain in the list with `isResolved = true`
   - This preserves history for popup display

3. **Multiple sources from same student**
   - If Student_A has both MessCreated and MakingNoise
   - Both get resolved when Student_A is calmed

---

## 🚫 What Does NOT Happen

### ❌ Clean Mess Does NOT:

1. **Remove Student_B's influence**
   ```csharp
   // WRONG (doesn't happen):
   Student_C.InfluenceSources.ClearAllSources();

   // CORRECT (what actually happens):
   Student_C.InfluenceSources.ResolveSource(Student_A);  // Only Student_A
   ```

2. **Overwrite later events**
   ```csharp
   // Clean mess at T=10s does NOT cancel throw object from T=5s
   // Both influences coexist independently
   ```

3. **Auto-resolve other students' influences**
   ```csharp
   // Clean mess resolves Student_A only
   // Student_B's influence remains unresolved until Student_B is calmed
   ```

---

## 🎯 Popup Display Logic

### With Multiple Influences

**After cleaning mess (T=10s), click Student_C:**

```
Popup shows:
━━━━━━━━━━━━━━━━━━━━━━━
Student_C - Distracted 😟
━━━━━━━━━━━━━━━━━━━━━━━
💬 "Cô ơi!"

Complaints:
✓ 😷 Bạn Student_A ói, thúi quá!  (resolved, with checkmark)
  🎯 Bạn Student_B ném đồ vào con!  (unresolved, no checkmark)

Buttons:
[Escort Back (Disabled)]  ← Cannot escort (Student_B unresolved)
[Close]
━━━━━━━━━━━━━━━━━━━━━━━
```

**After calming Student_B (T=15s), click Student_C:**

```
Popup shows:
━━━━━━━━━━━━━━━━━━━━━━━
Student_C - Distracted 😟
━━━━━━━━━━━━━━━━━━━━━━━
💬 "Cô ơi!"

Complaints:
✓ 😷 Bạn Student_A ói, thúi quá!  (resolved)
✓ 🎯 Bạn Student_B ném đồ vào con!  (resolved)

Buttons:
[Escort Back (Enabled)]  ← Can escort now! ✅
[Close]
━━━━━━━━━━━━━━━━━━━━━━━
```

---

## 🔄 What About Same Source, Different Events?

### Scenario: Student_B Creates Multiple Problems

```
T=0s:  Student_B makes loud noise
       └─> Student_C.InfluenceSources.AddSource(Student_B, MakingNoise, 0.5)

T=5s:  Student_B throws object at Student_C
       └─> Student_C.InfluenceSources.AddSource(Student_B, ThrowingObject, 0.7)
```

**Result:**
```csharp
Student_C.InfluenceSources.activeSources = [
    {
        sourceStudent: Student_B,
        eventType: MakingNoise,
        influenceStrength: 0.5,
        isResolved: false
    },
    {
        sourceStudent: Student_B,
        eventType: ThrowingObject,
        influenceStrength: 0.7,
        isResolved: false
    }
]
```

**When teacher calms Student_B:**
```csharp
ResolveSource(Student_B);
// BOTH sources from Student_B are resolved!
```

```csharp
Student_C.InfluenceSources.activeSources = [
    {
        sourceStudent: Student_B,
        eventType: MakingNoise,
        isResolved: true  // ✓ Resolved
    },
    {
        sourceStudent: Student_B,
        eventType: ThrowingObject,
        isResolved: true  // ✓ Resolved
    }
]
```

**Popup shows:**
```
✓ 📢 Bạn Student_B làm ồn quá!
✓ 🎯 Bạn Student_B ném đồ vào con!
```

Both complaints appear, both with checkmarks!

---

## 🛡️ Duplicate Prevention

### AddSource() Logic

**File:** [InfluenceSource.cs:58-78](../InfluenceSource.cs#L58-L78)

```csharp
public void AddSource(StudentAgent sourceStudent, StudentEventType eventType, float strength)
{
    // Check if source already exists (same student + same event type + unresolved)
    var existing = activeSources.Find(s =>
        s.sourceStudent == sourceStudent &&
        s.eventType == eventType &&
        !s.isResolved
    );

    if (existing != null)
    {
        // Update strength if stronger
        if (strength > existing.influenceStrength)
        {
            existing.influenceStrength = strength;
            existing.timestamp = Time.time;
        }
    }
    else
    {
        // Add new source
        activeSources.Add(new InfluenceSource(...));
    }
}
```

**Key Points:**

1. **Prevents duplicate unresolved sources**
   - If Student_B already has ThrowingObject (unresolved) affecting Student_C
   - Another ThrowingObject event updates strength instead of adding duplicate

2. **Allows same source type after resolution**
   - If Student_B's ThrowingObject is resolved
   - New ThrowingObject event creates a NEW source (not duplicate)

3. **Tracks strongest influence**
   - If new event has stronger influence, updates existing source
   - If weaker, keeps existing stronger influence

---

## 📋 Summary Table

| Action | Student_A Sources | Student_B Sources | Result |
|--------|------------------|-------------------|---------|
| **Initial** | None | None | Clean state |
| **Vomit (A)** | A: MessCreated (unresolved) | A: MessCreated (unresolved) | WholeClass affected |
| **Throw (B→C)** | A: MessCreated (unresolved) | A: MessCreated (unresolved)<br>B: ThrowingObject (unresolved) | C has 2 sources |
| **Clean Mess** | A: MessCreated (✓ resolved) | A: MessCreated (✓ resolved)<br>B: ThrowingObject (unresolved) | A's influence resolved |
| **Calm B** | A: MessCreated (✓ resolved) | A: MessCreated (✓ resolved)<br>B: ThrowingObject (✓ resolved) | All resolved |

---

## ✅ Conclusion

### Answer to Original Question:

**Q:** "nếu hành động clean mess xảy ra sau khi hành động throw object thì sao?"

**A:** Clean mess does NOT affect throw object influence! They are tracked independently.

**Q:** "nó có tác động đè kết quả hành động sau?"

**A:** NO! Clean mess only resolves Student_A's influences, does NOT overwrite or cancel Student_B's influences.

**Q:** "cơ chế hiện tại phân biệt nhiều tác động xảy ra song song ra sao?"

**A:** Each student maintains a **list** of influence sources. Each source has:
- `sourceStudent` - who is causing it
- `eventType` - what type of influence
- `isResolved` - whether it's been resolved
- `timestamp` - when it happened

Multiple sources coexist independently and are resolved separately.

---

## 🎮 Game Design Benefits

### 1. **Realistic Complexity**
- Multiple problems can happen simultaneously
- Teacher must handle each problem individually
- Can't just "fix one thing" and expect everything to be fine

### 2. **Clear Cause-and-Effect**
- Users see exactly which problems are fixed (✓)
- Users see which problems remain (no checkmark)
- Transparent relationship between actions and results

### 3. **Strategic Gameplay**
- Must prioritize which problems to solve first
- Some students have multiple influences (harder to help)
- Complete history preserved for teaching feedback

---

**Last Updated:** 2026-01-19
**Status:** ✅ Mechanism confirmed working correctly - multiple influences tracked independently
