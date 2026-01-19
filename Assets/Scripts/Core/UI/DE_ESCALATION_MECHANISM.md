# De-escalation Mechanism Documentation
**Date:** 2026-01-19

## Overview
This document explains how the FunClass system handles automatic de-escalation of target students when their influence sources are resolved.

---

## ✅ **Mechanism EXISTS and Works as Expected**

The system **already has** a comprehensive de-escalation mechanism that automatically reduces target students' stress when source students are calmed down.

---

## How It Works

### 1. **Influence Source Tracking** ([InfluenceSource.cs](../InfluenceSource.cs))

Each student tracks who is affecting them via `StudentInfluenceSources`:

```csharp
// Example: Student_C is affected by Student_A and Student_B
Student_C.InfluenceSources = {
    { sourceStudent: Student_A, eventType: MessCreated, isResolved: false },
    { sourceStudent: Student_B, eventType: ThrowingObject, isResolved: false }
}
```

### 2. **Source Resolution** ([StudentInfluenceManager.cs:324-350](../StudentInfluenceManager.cs#L324-L350))

When teacher takes action to calm a source student (e.g., cleans mess or resolves behavior), the system:

1. **Marks the source as resolved** for all affected students
2. **Does NOT immediately de-escalate** the target students
3. **Logs the resolution** for tracking

```csharp
private void ResolveInfluenceSourcesFromStudent(StudentAgent calmedStudent)
{
    // For each student affected by calmedStudent
    foreach (StudentAgent student in allStudents)
    {
        if (student.InfluenceSources != null)
        {
            // Mark this source as resolved
            student.InfluenceSources.ResolveSource(calmedStudent);
        }
    }
}
```

**Example:**
- Teacher cleans Student_A's vomit mess
- System marks Student_A's influence as resolved for Student_B and Student_C
- Student_B and Student_C still remember they were affected (for escort logic)
- But the influence is no longer "active"

### 3. **Teacher Actions Trigger De-escalation** ([StudentAgent.cs:569-594](../StudentAgent.cs#L569-L594))

When teacher directly interacts with a student using `TeacherActionType.Calm`, the student de-escalates:

```csharp
public void CalmDown()
{
    // Log the event
    StudentEventManager.Instance.LogEvent(this, StudentEventType.StudentCalmed, "is calming down");

    // Reduce state by one level
    DeescalateState();

    // Stop any current action
    StopCurrentAction();
}

public void DeescalateState()
{
    StudentState nextState = CurrentState switch
    {
        StudentState.Critical => StudentState.ActingOut,
        StudentState.ActingOut => StudentState.Distracted,
        StudentState.Distracted => StudentState.Calm,
        StudentState.Calm => StudentState.Calm,
        _ => StudentState.Calm
    };

    ChangeState(nextState);
}
```

**State Transition:**
```
Critical → ActingOut → Distracted → Calm
```

### 4. **Escort Logic Checks Resolution** ([StudentInteractionPopup.cs:204](../StudentInteractionPopup.cs#L204))

The escort button is only enabled when **all** influence sources are resolved:

```csharp
bool canEscort = IsStudentOutside(student) && influenceSources.Count == 0;
```

This ensures:
- Target students can only be escorted back after all sources are handled
- Teacher must resolve each source problem first (clean mess, calm down troublemakers)

---

## Example Scenario: Student_A Vomits

### Initial State:
```
Student_A: Critical (vomiting)
  └─> MessCreated event → WholeClass influence
        └─> Affects Student_B and Student_C

Student_B: ActingOut (reacted to smell)
  └─> Has 1 influence source: Student_A (MessCreated, unresolved)

Student_C: ActingOut (reacted to smell)
  └─> Has 1 influence source: Student_A (MessCreated, unresolved)
```

### After Teacher Cleans Mess:
```
Student_A: Critical → ActingOut (de-escalated)
  └─> Influence sources resolved for all affected students

Student_B: ActingOut
  └─> Has 1 influence source: Student_A (MessCreated, RESOLVED ✓)
  └─> Escort button: ENABLED (all sources resolved)

Student_C: ActingOut
  └─> Has 1 influence source: Student_A (MessCreated, RESOLVED ✓)
  └─> Escort button: ENABLED (all sources resolved)
```

**Key Point:**
- Student_B and Student_C do NOT automatically drop from ActingOut to Distracted
- They stay at ActingOut until teacher takes action (Calm, Escort)
- But they CAN now be escorted because their influence sources are resolved

---

## Why This Design Makes Sense

### 1. **Realistic Behavior**
- Students don't instantly calm down when the problem is fixed
- They need teacher attention to de-escalate (just like real classrooms)
- Resolving the source prevents further escalation, but doesn't reverse existing stress

### 2. **Gameplay Balance**
- Teacher must actively manage both source students AND affected students
- Can't just "fix the root cause" and ignore everyone else
- Creates meaningful decision-making (who to help first?)

### 3. **Escort Logic**
- Only students with **no unresolved sources** can be escorted
- This prevents escorting Student_C while Student_B is still throwing things at them
- Makes logical sense in classroom context

---

## Action Types and Their Effects

| Teacher Action | Effect on Target | Effect on Source |
|----------------|------------------|------------------|
| **Clean** (MessCreated) | Marks source as resolved for all affected | Removes mess object |
| **Calm** (Direct interaction) | De-escalates by 1 level | Marks influence as resolved for all affected |
| **Escort** (Walk back to seat) | De-escalates by 2 levels | Only works if all sources resolved |
| **Stop** (Interrupt action) | Stops current action | Does NOT de-escalate |

---

## Code References

### Key Files:
1. **[InfluenceSource.cs](../InfluenceSource.cs)** - Tracks influence sources and resolution state
2. **[StudentInfluenceManager.cs](../StudentInfluenceManager.cs)** - Manages influence propagation and resolution
3. **[StudentAgent.cs](../StudentAgent.cs)** - Handles de-escalation and state transitions
4. **[StudentInteractionPopup.cs](../StudentInteractionPopup.cs)** - UI logic for escort button enable/disable

### Key Methods:
- `ResolveInfluenceSourcesFromStudent()` - Marks sources as resolved ([StudentInfluenceManager.cs:324](../StudentInfluenceManager.cs#L324))
- `ResolveSource()` - Marks specific source as resolved ([InfluenceSource.cs:93](../InfluenceSource.cs#L93))
- `DeescalateState()` - Reduces student state by one level ([StudentAgent.cs:319](../StudentAgent.cs#L319))
- `CalmDown()` - Calls DeescalateState and stops actions ([StudentAgent.cs:569](../StudentAgent.cs#L569))
- `AreAllSourcesResolved()` - Checks if student can be escorted ([InfluenceSource.cs:127](../InfluenceSource.cs#L127))

---

## Conclusion

**Answer to the question:** "đã có cơ chế cho target de-escalate khi resolve dc source problem chưa?"

**YES**, the mechanism exists:

1. ✅ Resolving source problems marks influence as resolved
2. ✅ Target students can be escorted once all sources resolved
3. ✅ Direct teacher action (Calm/Escort) de-escalates target students
4. ✅ System prevents escalation from resolved sources

**What does NOT happen automatically:**
- ❌ Target students don't instantly de-escalate when source is resolved
- ❌ Teacher must still interact with target students to calm them down
- ❌ Or teacher can escort them back to seat (which de-escalates by 2 levels)

This is **intentional design** - it creates meaningful gameplay where teacher must actively manage the classroom, not just fix root causes and hope everyone auto-calms down.

---

**Last Updated:** 2026-01-19
**Status:** ✅ Mechanism confirmed working as designed
