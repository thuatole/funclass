# Disruption System Review
**Date:** 2026-01-19

## 📊 Current System Overview

### Disruption là gì?

**Disruption Level** là chỉ số chính để đo mức độ rối loạn trong lớp học:
- **Range:** 0-100
- **Tăng:** Khi học sinh gây rối (vomit, throw object, wandering, etc.)
- **Giảm:** Khi teacher xử lý vấn đề (calm student, clean mess, escort back)
- **Mục tiêu:** Giữ disruption dưới ngưỡng để win level

---

## 🎯 Level Goals - Dựa trên Disruption

**File:** [LevelGoalConfig.cs](LevelGoalConfig.cs)

### Win Conditions (Line 74-78):
```csharp
public bool MeetsWinConditions(float finalDisruption, int resolvedProblems, int calmDowns)
{
    return finalDisruption <= winDisruptionThreshold &&     // Disruption ≤ 50
           resolvedProblems >= requiredResolvedProblems &&  // Resolved ≥ 5 problems
           calmDowns >= requiredCalmDowns;                  // Calmed ≥ 3 students
}
```

### Lose Conditions:

**1. Catastrophic Disruption (Line 13):**
```csharp
public float catastrophicDisruptionLevel = 95f;
// If disruption ≥ 95 → Instant lose!
```

**2. Disruption Timeout (Lines 48-56):**
```csharp
public bool enableDisruptionTimeout = false;  // Can be enabled
public float disruptionTimeoutThreshold = 80f;  // Disruption must stay < 80
public float disruptionTimeoutSeconds = 60f;    // For 60 seconds
// If disruption ≥ 80 for 60s → Lose!
```

**3. Outside Student Limits (Lines 20-28):**
```csharp
public int maxAllowedOutsideStudents = 2;        // Warning at 2 students
public int catastrophicOutsideStudents = 5;       // Lose at 5 students
public float maxOutsideTimePerStudent = 60f;      // Max 60s per student
public float maxAllowedOutsideGracePeriod = 10f;  // 10s grace period
```

**4. Critical Students (Lines 16-18):**
```csharp
public int maxAllowedCriticalStudents = 2;      // Warning at 2 students
public int catastrophicCriticalStudents = 4;     // Lose at 4 students
```

---

## 🔥 Disruption Sources (ClassroomManager.cs)

**File:** [ClassroomManager.cs:219-237](ClassroomManager.cs#L219-L237)

### Event-Based Disruption Changes:

```csharp
private float GetDisruptionChange(StudentEventType eventType)
{
    return eventType switch
    {
        // INCREASES (positive values)
        StudentEventType.MessCreated => 15f,          // Vomit/mess
        StudentEventType.ThrowingObject => 8f,        // Throwing
        StudentEventType.MakingNoise => 5f,           // Loud noise
        StudentEventType.WanderingAround => 3f,       // Walking around
        StudentEventType.KnockingOverObject => 7f,    // Knocking things
        StudentEventType.DroppingItem => 2f,          // Drop item
        StudentEventType.FidgetingWithItem => 1f,     // Fidgeting

        // DECREASES (negative values)
        StudentEventType.StudentCalmed => -5f,        // Teacher calms
        StudentEventType.MessCleaned => -15f,         // Teacher cleans
        StudentEventType.StudentReturnedToSeat => -10f,  // Student returns
        StudentEventType.StudentStoppedAction => -3f,    // Stops action

        _ => 0f
    };
}
```

---

## ✅ Students Outside → Disruption Increase (ALREADY EXISTS!)

**File:** [ClassroomManager.cs:331-351](ClassroomManager.cs#L331-L351)

### Current Implementation:

```csharp
[Header("Outside Student Tracking")]
[Tooltip("Disruption added per student outside per check interval")]
[SerializeField] private float outsideDisruptionRate = 0.5f;  // 0.5 per student

[Tooltip("How often to check and apply outside student penalty (seconds)")]
[SerializeField] private float outsideCheckInterval = 5f;  // Every 5 seconds

[Tooltip("Maximum disruption that can be added from students outside")]
[SerializeField] private float maxOutsideDisruptionPenalty = 30f;  // Cap at 30
```

### Processing Logic:

```csharp
private void ProcessOutsideStudentDisruption()
{
    if (OutsideStudentCount == 0) return;

    // Calculate disruption to add
    float disruptionToAdd = OutsideStudentCount * outsideDisruptionRate;
    // Example: 2 students × 0.5 = +1.0 disruption per check

    // Check if we've exceeded max penalty
    if (totalOutsideDisruptionApplied + disruptionToAdd > maxOutsideDisruptionPenalty)
    {
        disruptionToAdd = Mathf.Max(0, maxOutsideDisruptionPenalty - totalOutsideDisruptionApplied);
    }

    if (disruptionToAdd > 0.01f)
    {
        totalOutsideDisruptionApplied += disruptionToAdd;
        AddDisruption(disruptionToAdd, $"{OutsideStudentCount} student(s) outside");
    }
}
```

**Called from Update() every frame:**
```csharp
void Update()
{
    if (!isActive) return;

    // Check for outside student disruption penalty
    if (Time.time >= nextOutsideCheckTime)
    {
        ProcessOutsideStudentDisruption();
        nextOutsideCheckTime = Time.time + outsideCheckInterval;  // Next check in 5s
    }
}
```

---

## 📈 Example: Disruption Over Time (Students Outside)

### Scenario: 2 Students Outside

```
T=0s:   2 students leave classroom
        OutsideStudentCount = 2

T=5s:   First check
        disruptionToAdd = 2 × 0.5 = 1.0
        Disruption: 0 → 1.0

T=10s:  Second check
        disruptionToAdd = 2 × 0.5 = 1.0
        Disruption: 1.0 → 2.0

T=15s:  Third check
        disruptionToAdd = 2 × 0.5 = 1.0
        Disruption: 2.0 → 3.0

...continues every 5 seconds...

T=150s: 30th check (2.5 minutes)
        totalOutsideDisruptionApplied = 30.0
        Reached maxOutsideDisruptionPenalty!
        No more disruption added from students outside
```

**Rate:** +1.0 disruption per 5 seconds (with 2 students outside)

**Time to Cap:** 30 / (2 × 0.5) = 30 checks = 150 seconds = 2.5 minutes

---

## 🎮 Tracking Students Outside

**File:** [ClassroomManager.cs:285-326](ClassroomManager.cs#L285-L326)

### Registration System:

```csharp
private Dictionary<StudentAgent, float> studentsOutside = new Dictionary<StudentAgent, float>();
// Key: StudentAgent
// Value: Time.time when they went outside
```

### API Methods:

**1. RegisterStudentOutside() - Lines 285-294:**
```csharp
public void RegisterStudentOutside(StudentAgent student)
{
    if (student == null || studentsOutside.ContainsKey(student)) return;

    studentsOutside[student] = Time.time;  // Store timestamp
    OutsideStudentCount = studentsOutside.Count;
    OnOutsideStudentCountChanged?.Invoke(OutsideStudentCount);

    Debug.Log($"[ClassroomManager] {student.Config?.studentName} is now outside classroom");
}
```

**2. UnregisterStudentOutside() - Lines 299-309:**
```csharp
public void UnregisterStudentOutside(StudentAgent student)
{
    if (student == null || !studentsOutside.ContainsKey(student)) return;

    float timeOutside = Time.time - studentsOutside[student];
    studentsOutside.Remove(student);
    OutsideStudentCount = studentsOutside.Count;
    OnOutsideStudentCountChanged?.Invoke(OutsideStudentCount);

    Debug.Log($"[ClassroomManager] {student.Config?.studentName} returned after {timeOutside:F1}s");
}
```

**3. GetStudentOutsideDuration() - Lines 314-318:**
```csharp
public float GetStudentOutsideDuration(StudentAgent student)
{
    if (student == null || !studentsOutside.ContainsKey(student)) return 0f;
    return Time.time - studentsOutside[student];
}
```

**4. IsStudentOutside() - Lines 323-326:**
```csharp
public bool IsStudentOutside(StudentAgent student)
{
    return student != null && studentsOutside.ContainsKey(student);
}
```

---

## 🆕 Potential Improvements

### 1. ✅ **ALREADY IMPLEMENTED: Basic Outside Disruption**

Current system:
- ✅ Tracks students outside with timestamps
- ✅ Adds disruption every 5 seconds based on count
- ✅ Has configurable rate and cap
- ✅ Logs duration when students return

---

### 2. 🔄 **POTENTIAL ENHANCEMENT: Progressive Disruption Rate**

**Idea:** Disruption rate increases the longer a student is outside

**Current:** Constant rate (0.5 per student per 5s check)

**Proposed:** Escalating rate based on duration

```csharp
// Example implementation:
private float CalculateOutsideDisruptionRate(StudentAgent student)
{
    float timeOutside = GetStudentOutsideDuration(student);

    // Progressive multipliers based on time outside
    if (timeOutside < 30f)
    {
        return 0.5f;  // 0-30s: Normal rate
    }
    else if (timeOutside < 60f)
    {
        return 1.0f;  // 30-60s: Double rate
    }
    else if (timeOutside < 120f)
    {
        return 2.0f;  // 60-120s: 4x rate
    }
    else
    {
        return 3.0f;  // 120s+: 6x rate (urgent!)
    }
}

private void ProcessOutsideStudentDisruption()
{
    if (OutsideStudentCount == 0) return;

    float totalDisruptionToAdd = 0f;

    // Calculate disruption for each student individually
    foreach (var kvp in studentsOutside)
    {
        StudentAgent student = kvp.Key;
        float studentRate = CalculateOutsideDisruptionRate(student);
        totalDisruptionToAdd += studentRate;

        float timeOutside = GetStudentOutsideDuration(student);
        Debug.Log($"[ClassroomManager] {student.Config?.studentName} outside for {timeOutside:F1}s, rate: {studentRate:F2}");
    }

    // Apply cap
    if (totalOutsideDisruptionApplied + totalDisruptionToAdd > maxOutsideDisruptionPenalty)
    {
        totalDisruptionToAdd = Mathf.Max(0, maxOutsideDisruptionPenalty - totalOutsideDisruptionApplied);
    }

    if (totalDisruptionToAdd > 0.01f)
    {
        totalOutsideDisruptionApplied += totalDisruptionToAdd;
        AddDisruption(totalDisruptionToAdd, $"{OutsideStudentCount} student(s) outside");
    }
}
```

**Benefits:**
- Creates urgency to deal with students outside quickly
- More realistic (longer absence = more problematic)
- Forces teacher to prioritize students who've been outside longest

**Example Timeline (1 student):**
```
T=0-30s:   +0.5 per 5s = +3.0 disruption total
T=30-60s:  +1.0 per 5s = +6.0 disruption total
T=60-120s: +2.0 per 5s = +24.0 disruption total
Total at 2 minutes: 33.0 disruption (exceeds cap)
```

---

### 3. 🆕 **NEW FEATURE: Individual Student Outside Timeout**

**Idea:** Lose condition if ANY student is outside too long

**Already exists in config (Line 26):**
```csharp
public float maxOutsideTimePerStudent = 60f;  // Max 60s per student
```

**But NOT enforced in LevelManager!**

**Proposed Implementation in LevelManager.cs:**

```csharp
private void CheckLoseConditions()
{
    // ... existing checks ...

    // Check individual student outside timeout
    if (currentGoal.maxOutsideTimePerStudent > 0)
    {
        foreach (StudentAgent student in FindObjectsOfType<StudentAgent>())
        {
            if (ClassroomManager.Instance.IsStudentOutside(student))
            {
                float timeOutside = ClassroomManager.Instance.GetStudentOutsideDuration(student);

                if (timeOutside >= currentGoal.maxOutsideTimePerStudent)
                {
                    LoseLevel($"Student {student.Config?.studentName} was outside for too long ({timeOutside:F0}s)");
                    return;
                }
            }
        }
    }
}
```

---

### 4. 🆕 **NEW FEATURE: Visual Warning System**

**Idea:** Show warning UI when students have been outside for a while

```csharp
// In ClassroomManager.cs
public event Action<StudentAgent, float> OnStudentOutsideWarning;

private void ProcessOutsideStudentDisruption()
{
    // ... existing code ...

    // Check for warnings
    foreach (var kvp in studentsOutside)
    {
        StudentAgent student = kvp.Key;
        float timeOutside = GetStudentOutsideDuration(student);

        // Warning at 30s, 45s (if max is 60s)
        if (levelConfig?.levelGoal != null)
        {
            float maxTime = levelConfig.levelGoal.maxOutsideTimePerStudent;
            float warningTime = maxTime * 0.5f;  // 50% of max

            if (timeOutside >= warningTime && timeOutside < warningTime + outsideCheckInterval)
            {
                OnStudentOutsideWarning?.Invoke(student, maxTime - timeOutside);
            }
        }
    }
}
```

---

## 📋 Summary

### ✅ Current System (Already Working):

1. **Disruption tracks classroom chaos** (0-100 scale)
2. **Level goals based on disruption** (win if < 50, lose if ≥ 95)
3. **Students outside ADD disruption** (0.5 per student per 5s)
4. **Tracking system in place** (timestamps, durations, counts)
5. **Configurable cap** (max 30 disruption from outside students)

### 🔄 Recommended Enhancements:

**Priority 1 - Increase Urgency:**
- [ ] **Progressive disruption rate** based on time outside
  - Example: 0.5x (0-30s) → 1.0x (30-60s) → 2.0x (60-120s) → 3.0x (120s+)
  - Makes it critical to handle students outside quickly

**Priority 2 - Enforce Timeout:**
- [ ] **Individual student timeout** lose condition
  - Config already has `maxOutsideTimePerStudent = 60f`
  - Need to check in `LevelManager.CheckLoseConditions()`
  - If any student outside > 60s → instant lose

**Priority 3 - Better Feedback:**
- [ ] **Visual warning system** when students approaching timeout
  - Show UI warning at 50% of max time (e.g., at 30s if max is 60s)
  - Highlight student in UI with timer countdown

---

## 🎯 Recommendation

**Keep current system as-is** (it's solid!) **BUT** consider adding:

1. **Progressive rate** for gameplay depth
2. **Timeout enforcement** for challenge
3. **Warning UI** for better UX

All the infrastructure is already there - just need to enhance the calculations!

---

**Last Updated:** 2026-01-19
**Status:** ✅ Current system working, enhancements proposed
