# Teacher Recall System

## Overview

The Teacher Recall System enables teachers to bring escaped students back to class, completing the gameplay loop: **Influence → Escape → Recall → Return**. This system integrates seamlessly with the existing movement, influence, and interaction systems without breaking any current functionality.

## Architecture

### Core Components

1. **TeacherActionType** - Extended with recall actions
2. **TeacherController** - Context-sensitive detection and recall methods
3. **StudentAgent** - Handlers for recall actions
4. **StudentMovementManager** - Return route execution
5. **LevelConfig** - Return route configuration

### Integration Points

```
Student Escapes (Influence System)
            ↓
Teacher Detects Student Outside
            ↓
TeacherController.GetContextualPrompt()
            ↓
Player Presses E
            ↓
HandleContextualInteraction()
            ↓
CallStudentBack() or EscortStudentBack()
            ↓
StudentAgent.StartRoute(returnRoute)
            ↓
StudentMovementManager executes return
            ↓
Student reaches seat → DeescalateState()
```

## How It Works

### 1. Student Location Detection

**IsStudentOutsideClassroom():**
- Checks student distance from classroom door (LevelConfig.classroomDoor)
- Checks student distance from original seat
- Returns true if student is far from seat (>5m) and near door (<10m)

**Example Logic:**
```csharp
bool isOutside = distanceFromSeat > 5f && distanceFromDoor < 10f;
```

### 2. Context-Sensitive Prompts

**GetContextualPrompt():**
- Detects if student is outside or on escape route
- Shows different prompts based on student state and location

**Prompt Examples:**
- Outside + Critical: "Press E to escort Nam back to class"
- Outside + Other: "Press E to call Nam back to class"
- Inside + Calm: "Talk to Nam"
- Inside + Critical: "Send Nam back to seat"

### 3. Recall Actions

#### CallStudentBack
**When:** Student is outside but not Critical
**Effect:**
- Stops current escape route
- Starts return route from LevelConfig
- Reduces disruption by 10
- Student shows Embarrassed reaction
- Student de-escalates one state level

#### EscortStudentBack
**When:** Student is outside and Critical
**Effect:**
- Stops current behavior immediately
- Forces instant return to seat
- Reduces disruption by 15
- Student shows Apologize reaction
- Student de-escalates two state levels

#### ForceReturnToSeat
**When:** Manual teacher command
**Effect:**
- Stops any route
- Forces return to seat
- Reduces disruption by 5
- 50% chance of Angry or Embarrassed reaction

## Configuration

### LevelConfig Setup

**Return Route:**
1. Create waypoints: Outside → Door → Seat
2. Create StudentRoute asset
3. Configure route:
   ```
   Route Name: "Return Route"
   Waypoints: [Outside, Door, Seat_Nam]
   Movement Speed: 2.0 (walking)
   Is Running: false
   Completion Behavior: ReturnToSeat
   ```
4. Assign to LevelConfig.returnRoute

**Key Locations:**
```
Classroom Door: [Transform at door position]
Outside Area: [Transform outside classroom]
```

### TeacherActionType Enum

New actions added:
```csharp
public enum TeacherActionType
{
    // ... existing actions ...
    CallStudentBack,      // Call student back via return route
    EscortStudentBack,    // Escort critical student immediately
    ForceReturnToSeat     // Force immediate return
}
```

## Usage Examples

### Example 1: Student Escapes, Teacher Calls Back

**Scenario:** Nam panics from peer influence and runs out of classroom

**Flow:**
```
1. [Influence] Nam → reaction Scared (strength 0.85)
2. [Influence] Nam started escape route due to panic
3. [Movement] Nam started route: Escape Route
4. [Movement] Nam reached waypoint: Outside
5. Teacher looks at Nam
6. [TeacherController] Looking at: Press E to call Nam back to class
7. Teacher presses E
8. [Teacher] Called Nam back to class
9. [Teacher] Nam starting return route
10. [Movement] Nam started route: Return Route
11. [Movement] Nam reached waypoint: Door
12. [Movement] Nam reached waypoint: Seat_Nam
13. [Movement] Nam completed route: Return Route
14. [StudentEvent] Nam: returned to their seat
15. [ClassroomManager] Disruption decreased by 10.0
```

### Example 2: Critical Student Requires Escort

**Scenario:** Lan is in Critical state and outside classroom

**Flow:**
```
1. Teacher looks at Lan (outside, Critical state)
2. [TeacherController] Looking at: Press E to escort Lan back to class
3. Teacher presses E
4. [Teacher] Escorting Lan back to seat
5. [StudentAgent] Lan returned to seat
6. [StudentEvent] Lan: was escorted back to seat by teacher
7. [Reaction] Lan apologized
8. [StudentAgent] Lan de-escalated from Critical to ActingOut
9. [StudentAgent] Lan de-escalated from ActingOut to Distracted
10. [ClassroomManager] Disruption decreased by 15.0
```

### Example 3: Multiple Students Escape

**Scenario:** Chain reaction causes multiple students to escape

**Flow:**
```
1. Nam throws object → triggers influence
2. Lan panics → starts escape route
3. Minh panics → starts escape route
4. Teacher calls Lan back
5. [Movement] Lan starting return route
6. Teacher calls Minh back
7. [Movement] Minh starting return route
8. Both students return to seats
9. Classroom disruption significantly reduced
10. Teacher regains control
```

## TeacherController API

### Public Methods

**ForceStudentReturnToSeat(StudentAgent student)**
```csharp
// Can be called from external scripts or input
TeacherController.Instance.ForceStudentReturnToSeat(student);
```

### Private Methods (Internal Logic)

**IsStudentOutsideClassroom(StudentAgent student)**
- Returns true if student is outside classroom boundaries

**GetContextualPrompt(StudentAgent student)**
- Returns appropriate interaction prompt based on context

**HandleContextualInteraction(StudentAgent student)**
- Routes to appropriate action based on student location/state

**CallStudentBack(StudentAgent student)**
- Initiates return route for non-critical students

**EscortStudentBack(StudentAgent student)**
- Immediately returns critical students to seats

## StudentAgent Handlers

### CallStudentBack Handler
```csharp
case TeacherActionType.CallStudentBack:
    if (CurrentState == StudentState.Critical)
        TriggerReaction(StudentReactionType.Scared, 4f);
    else
        TriggerReaction(StudentReactionType.Embarrassed, 3f);
    DeescalateState();
```

### EscortStudentBack Handler
```csharp
case TeacherActionType.EscortStudentBack:
    TriggerReaction(StudentReactionType.Apologize, 5f);
    DeescalateState();
    DeescalateState(); // Double de-escalate
```

### ForceReturnToSeat Handler
```csharp
case TeacherActionType.ForceReturnToSeat:
    if (Random.value < 0.5f)
        TriggerReaction(StudentReactionType.Angry, 2f);
    else
        TriggerReaction(StudentReactionType.Embarrassed, 3f);
```

## Disruption Reduction

| Action | Disruption Reduction | Notes |
|--------|---------------------|-------|
| **CallStudentBack** | -10 | Standard recall |
| **EscortStudentBack** | -15 | Critical student escort |
| **ForceReturnToSeat** | -5 | Manual force return |

## Event Logging

All recall actions generate detailed logs:

```
[Teacher] Called Nam back to class
[Teacher] Nam starting return route
[Movement] Nam started route: Return Route
[StudentEvent] Nam: was called back to class by teacher
[ClassroomManager] Disruption decreased by 10.0 to 35.0/100 (Nam called back)
```

## Integration with Existing Systems

### ✅ Compatible With

- **StudentMovementManager**: Uses return routes for smooth navigation
- **StudentInfluenceManager**: Recall reduces disruption, preventing further panic
- **ClassroomManager**: Disruption reduction integrated
- **StudentEventManager**: All actions logged as events
- **Sequence System**: Recall actions don't interfere with sequences
- **Level Objectives**: Recall counts toward classroom control objectives

### ✅ Gameplay Loop Completion

```
1. Student Autonomous Behavior
2. Peer Influence → Panic
3. Student Escapes (Escape Route)
4. Teacher Detects & Recalls
5. Student Returns (Return Route)
6. Disruption Reduced
7. Classroom Order Restored
```

## Best Practices

### 1. Return Route Design

**Good Return Route:**
```
Waypoints: [Outside, Hallway, Door, Aisle, Seat]
Movement Speed: 2.0 (walking pace)
Completion Behavior: ReturnToSeat
Wait Durations: 0.5s at Door (natural pause)
```

**Why:** Smooth, natural-looking return path

### 2. Location Detection Tuning

Adjust distance thresholds in `IsStudentOutsideClassroom()`:
```csharp
// Conservative (fewer false positives)
return distanceFromSeat > 7f && distanceFromDoor < 8f;

// Aggressive (catch students earlier)
return distanceFromSeat > 3f && distanceFromDoor < 15f;
```

### 3. Disruption Balance

Tune disruption reduction values for game balance:
- Too high → Recall becomes overpowered
- Too low → Players ignore escaped students
- Recommended: -10 to -15 range

### 4. Prompt Clarity

Ensure prompts are clear and actionable:
- ✅ "Press E to call Nam back to class"
- ❌ "Interact with Nam"

## Troubleshooting

### Student not detected as outside

**Check:**
- LevelConfig.classroomDoor is assigned
- Door position is correct in scene
- Distance thresholds in IsStudentOutsideClassroom()
- Student has actually moved far enough

**Solution:**
```csharp
// Add debug logging
Debug.Log($"Distance from seat: {distanceFromSeat}, from door: {distanceFromDoor}");
```

### Wrong prompt showing

**Check:**
- Student.IsFollowingRoute property
- Student.CurrentState value
- IsStudentOutsideClassroom() logic

**Solution:**
- Enable debug logs in GetContextualPrompt()
- Verify student location in scene view

### Return route not starting

**Check:**
- LevelConfig.returnRoute is assigned
- Return route has valid waypoints
- StudentMovementManager is active in scene

**Solution:**
- Verify route assignment in Inspector
- Check console for movement system logs

### Disruption not reducing

**Check:**
- ClassroomManager exists in scene
- ClassroomManager is active during gameplay
- AddDisruption() is being called

**Solution:**
- Enable ClassroomManager debug logs
- Verify disruption value in Inspector

## Advanced Features

### Custom Location Detection

Extend `IsStudentOutsideClassroom()` for complex layouts:
```csharp
private bool IsStudentOutsideClassroom(StudentAgent student)
{
    // Use trigger volumes
    if (outsideTrigger.bounds.Contains(student.transform.position))
        return true;
    
    // Use multiple door positions
    foreach (Transform door in levelConfig.doorPositions)
    {
        if (Vector3.Distance(student.transform.position, door.position) < 3f)
            return true;
    }
    
    return false;
}
```

### Escort Animation

Add visual feedback for escort:
```csharp
private void EscortStudentBack(StudentAgent student)
{
    // ... existing code ...
    
    // Optional: Move teacher toward student
    StartCoroutine(MoveToStudent(student));
}
```

### Recall Resistance

Make some students resist recall based on state:
```csharp
private void CallStudentBack(StudentAgent student)
{
    if (student.CurrentState == StudentState.Critical)
    {
        // Critical students may resist
        if (Random.value < 0.3f)
        {
            Debug.Log($"[Teacher] {student.Config.studentName} is resisting recall");
            // Require escort instead
            EscortStudentBack(student);
            return;
        }
    }
    
    // ... normal recall logic ...
}
```

## Future Extensions

Potential areas for expansion:

- **Recall Cooldown**: Prevent spam-recalling students
- **Resistance Mechanics**: Students may refuse based on state
- **Group Recall**: Call multiple students back at once
- **Reputation System**: Frequent recalls affect student behavior
- **Parent Notification**: Severe cases trigger additional consequences
- **Escort Path**: Teacher physically walks student back
- **Recall Scoring**: Bonus points for quick recalls

## Performance Considerations

### Efficient Design

- **Location Checks**: Only performed when looking at student
- **No Update Loop**: Detection happens on raycast hit
- **Minimal Calculations**: Simple distance checks
- **Event-Driven**: Recall triggered by player input

### Performance Tips

1. **Cache Transforms**: Store door/seat positions
2. **Distance Squared**: Use sqrMagnitude for distance checks
3. **Early Exit**: Return early if conditions not met
4. **Batch Recalls**: Handle multiple students efficiently

## Summary

The Teacher Recall System completes the gameplay loop by enabling teachers to:

✅ **Detect** students who have escaped
✅ **Interact** with context-sensitive prompts
✅ **Recall** students using return routes
✅ **Restore** classroom order and reduce disruption

All while maintaining compatibility with existing systems and providing a smooth, integrated gameplay experience.
