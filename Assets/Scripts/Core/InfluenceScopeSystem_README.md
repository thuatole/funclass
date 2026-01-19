# Influence Scope System

## Overview

System quản lý ảnh hưởng giữa các học sinh với 2 loại scope: **WholeClass** và **SingleStudent**. Không xét khoảng cách (distance) cho cả 2 loại.

## Key Features

### 1. Influence Scopes

- **WholeClass**: Ảnh hưởng tất cả học sinh **trong cùng vị trí** (inside/outside classroom)
- **SingleStudent**: Ảnh hưởng 1 học sinh cụ thể **nếu cùng vị trí**
- **None**: Không có ảnh hưởng

### 2. Location-Based Filtering

**IMPORTANT:** Influence chỉ xảy ra khi source và target ở **cùng vị trí**:
- **Inside classroom** - Students inside only affect other students inside
- **Outside classroom** - Students outside only affect other students outside
- **Different locations** - NO influence between inside/outside students

**Examples:**
- Student_A vomits **inside** → Only affects students **inside**
- Student_B hits Student_C **outside** → Only works if both are **outside**
- Student_A **inside** cannot influence Student_B **outside**

### 2. Multiple Influence Sources

- 1 học sinh có thể bị ảnh hưởng bởi nhiều sources
- Mỗi source được track riêng biệt với: sourceStudent, eventType, strength, timestamp, isResolved
- Ví dụ: Student_D bị ảnh hưởng bởi Student_A (vomit) và Student_C (hit)

### 3. Source Resolution

- Khi source student được calmed down → sources từ student đó được mark là resolved
- Teacher phải calm down tất cả source students trước khi escort target student về chỗ

### 4. Escort Validation

**Trước khi escort:**
1. Check xem tất cả influence sources đã resolved chưa
2. Nếu YES → Escort thành công, clear sources, set immunity
3. Nếu NO → Student quay lại outdoor, tăng disruption

**Disruption penalty:** 10 điểm cho mỗi unresolved source

## Flow Example

```
1. Student_A vomits (WholeClass scope)
   ↓
2. Student_B, C, D all affected (no distance check)
   - B.InfluenceSources.AddSource(A, MessCreated, 0.85)
   - C.InfluenceSources.AddSource(A, MessCreated, 0.75)
   - D.InfluenceSources.AddSource(A, MessCreated, 0.80)
   ↓
3. Student_C hits Student_D (SingleStudent scope)
   ↓
4. D.InfluenceSources.AddSource(C, ThrowingObject, 0.70)
   → D now has 2 sources: A (vomit) and C (hit)
   ↓
5. Teacher calms A
   → A's sources resolved for B, C, D
   → D still has 1 unresolved source: C
   ↓
6. Teacher tries to escort D
   → FAILED (C not resolved yet)
   → D returns to outdoor
   → +10 disruption penalty
   ↓
7. Teacher calms C
   → C's sources resolved for D
   → D has 0 unresolved sources
   ↓
8. Teacher escorts D
   → SUCCESS
   → D returns to seat with 15s immunity
```

## Code Structure

### InfluenceSource.cs

```csharp
// Track single influence source
public class InfluenceSource
{
    public StudentAgent sourceStudent;
    public StudentEventType eventType;
    public float influenceStrength;
    public float timestamp;
    public bool isResolved;
}

// Manage all sources affecting a student
public class StudentInfluenceSources
{
    public void AddSource(StudentAgent source, StudentEventType type, float strength);
    public void ResolveSource(StudentAgent source);
    public bool AreAllSourcesResolved();
    public int GetUnresolvedSourceCount();
    public List<StudentAgent> GetUnresolvedSourceStudents();
    public void ClearAllSources();
}
```

### StudentEvent.cs

```csharp
public enum InfluenceScope
{
    None,
    WholeClass,
    SingleStudent
}

public class StudentEvent
{
    public StudentAgent targetStudent;  // For SingleStudent scope
    public InfluenceScope influenceScope;
    
    // Auto-determine scope based on event type or explicit target
    private InfluenceScope DetermineInfluenceScope(StudentEventType type, StudentAgent target);
}
```

### StudentAgent.cs

```csharp
public class StudentAgent
{
    private StudentInfluenceSources influenceSources;
    public StudentInfluenceSources InfluenceSources => influenceSources;
}
```

### StudentInfluenceManager.cs

```csharp
private void ProcessInfluence(StudentEvent evt)
{
    if (scope == InfluenceScope.SingleStudent)
    {
        // Affect only targetStudent, NO distance check
        evt.targetStudent.InfluenceSources.AddSource(sourceStudent, evt.eventType, strength);
        ApplyInfluence(evt.targetStudent, sourceStudent, strength, evt.eventType);
    }
    else if (scope == InfluenceScope.WholeClass)
    {
        // Affect ALL students, NO distance check
        foreach (StudentAgent student in allStudents)
        {
            student.InfluenceSources.AddSource(sourceStudent, evt.eventType, strength);
            ApplyInfluence(student, sourceStudent, strength, evt.eventType);
        }
    }
}

private void ResolveInfluenceSourcesFromStudent(StudentAgent calmedStudent)
{
    // Mark all sources from calmedStudent as resolved
    foreach (StudentAgent student in allStudents)
    {
        student.InfluenceSources.ResolveSource(calmedStudent);
    }
}
```

### TeacherController.cs

```csharp
private void EscortStudentBack(StudentAgent student)
{
    // Check if all sources resolved
    if (!student.InfluenceSources.AreAllSourcesResolved())
    {
        // Get unresolved sources
        int unresolvedCount = student.InfluenceSources.GetUnresolvedSourceCount();
        
        // Student returns to outdoor
        student.StartRoute(escapeRoute);
        
        // Add disruption penalty
        float penalty = 10f * unresolvedCount;
        ClassroomManager.Instance.AddDisruption(penalty, "Unresolved sources");
        
        return;
    }
    
    // All sources resolved - proceed with escort
    student.InfluenceSources.ClearAllSources();
    student.SetInfluenceImmunity(15f);
    StudentMovementManager.Instance.ReturnToSeat(student);
}
```

## JSON Configuration

### Influence Scope Settings

```json
{
  "influenceScopeSettings": {
    "description": "Defines how events influence students",
    "disruptionPenaltyPerUnresolvedSource": 10.0,
    "eventScopes": {
      "MessCreated": {
        "scope": "WholeClass",
        "baseSeverity": 0.85,
        "description": "Vomit affects all students"
      },
      "ThrowingObject": {
        "scope": "SingleStudent",
        "baseSeverity": 0.7,
        "description": "Hitting affects only target"
      }
    }
  }
}
```

### Student Interactions (SingleStudent Scope)

Định nghĩa chính xác student nào sẽ affect student nào:

```json
{
  "studentInteractions": [
    {
      "sourceStudent": "Student_C",
      "targetStudent": "Student_D",
      "eventType": "ThrowingObject",
      "triggerCondition": "OnActingOut",
      "probability": 0.8,
      "customSeverity": -1,
      "description": "Student_C hits Student_D when acting out (80% chance)"
    },
    {
      "sourceStudent": "Student_C",
      "targetStudent": "Student_B",
      "eventType": "ThrowingObject",
      "triggerCondition": "OnCritical",
      "probability": 0.5,
      "customSeverity": 0.9,
      "description": "Student_C hits Student_B when critical (50% chance, high severity)"
    },
    {
      "sourceStudent": "Student_A",
      "targetStudent": "Student_B",
      "eventType": "ThrowingObject",
      "triggerCondition": "Random",
      "probability": 0.2,
      "customSeverity": -1,
      "description": "Student_A occasionally hits Student_B (20% chance)"
    }
  ]
}
```

**Trigger Conditions:**
- `Always` - Luôn trigger khi check
- `OnActingOut` - Trigger khi source student ở state ActingOut
- `OnCritical` - Trigger khi source student ở state Critical
- `Random` - Random trigger (dựa vào probability)

**Custom Severity:**
- `-1` - Use default severity from influenceScopeSettings
- `0.0 - 1.0` - Override với custom severity

## Debug Logs

System có detailed logs để debug:

### InfluenceSource Logs
```
[InfluenceSources] >>> AddSource called: Student_A → Student_D (MessCreated, strength: 0.85)
[InfluenceSources] ✓ Added NEW source to Student_D: Student_A (MessCreated, strength: 0.85, resolved: False)
[InfluenceSources] Total sources for Student_D: 1 (1 unresolved)
```

### Resolve Source Logs
```
[InfluenceSources] >>> ResolveSource called: Student_A affecting Student_D
[InfluenceSources] Resolving source: Student_A (MessCreated, strength: 0.85, resolved: False)
[InfluenceSources] ✓ Resolved 1 sources from Student_A affecting Student_D
[InfluenceSources] Remaining unresolved sources for Student_D: 0
```

### Escort Validation Logs
```
[Teacher] Attempting to escort Student_D back to seat
[InfluenceSources] Student_D sources check: 2 total, 1 unresolved → All resolved: False
[Teacher] ✗ Cannot escort Student_D - 1 unresolved influence sources!
[Teacher]   - Unresolved source: Student_C
[Teacher] Student_D returning to outdoor due to unresolved sources
[Teacher] Added 10 disruption for failed escort
```

## Testing Scenarios

### Scenario 1: Single Source
1. Student_A vomits
2. Student_B affected
3. Teacher calms A
4. Teacher escorts B → SUCCESS

### Scenario 2: Multiple Sources
1. Student_A vomits → affects B, C, D
2. Student_C hits D → D has 2 sources (A and C)
3. Teacher calms A → D still has 1 source (C)
4. Teacher escorts D → FAILED, returns to outdoor, +10 disruption
5. Teacher calms C → D has 0 sources
6. Teacher escorts D → SUCCESS

### Scenario 3: Parallel Influences
1. Student_A vomits → affects all
2. Student_B hits Student_C
3. Student_D hits Student_C
4. C has 3 sources: A (vomit), B (hit), D (hit)
5. Teacher must calm A, B, D before escorting C

## Important Notes

1. **No distance checks** - Both WholeClass and SingleStudent ignore distance
2. **Source tracking** - Each influence is tracked separately
3. **Resolution required** - All sources must be resolved before escort
4. **Disruption penalty** - 10 points per unresolved source on failed escort
5. **Immunity after escort** - 15 seconds immunity after successful escort
6. **Clear sources** - Sources cleared after successful escort

## Future Enhancements

- Configurable disruption penalty per source
- Time-based source decay
- Source priority/severity levels
- Visual indicators for unresolved sources
- UI to show which students need to be calmed first
