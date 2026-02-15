# FunClass Session Summary

## Project Goals
FunClass is a Unity educational game about classroom management. Current focus: Fixing scripted student interaction system where 4 students (Bình, Lan, Nam, Mai) trigger escalating behaviors at 20s/40s/60/80s intervals.

## Architecture Decisions

### 3-Tier Logging System
- **GameLogger.cs** - Static utility with tiers: Milestone (★), Detail (●), Trace (~)
- **GameLoggerConfig.cs** - Inspector-based toggles per component
- Log format: `[ComponentName] tier-symbol (elapsed-time) message`

### Key Files Created
- `Assets/Scripts/Core/GameLogger.cs`
- `Assets/Scripts/Core/GameLoggerConfig.cs`

### Key Files Refactored
| File | Purpose |
|------|---------|
| `StudentInteractionProcessor.cs` | Scripted interactions, time-based triggers |
| `StudentInfluenceManager.cs` | Peer influence (SingleStudent/WholeClass) |
| `StudentAgent.cs` | Student behavior, state transitions |
| `TeacherController.cs` | Dead zones: movement=0.15, mouse=0.05 |
| `LevelManager.cs` | Level flow, runtime desk setup |
| `GameStateManager.cs` | State transitions |

## Important Constraints

1. **Route Management**: Use `StudentMovementManager.Instance.StartRoute()/StopMovement()`, NOT `StudentAgent` methods
2. **Disruption Property**: Use `ClassroomManager.Instance.DisruptionLevel`
3. **Late Subscription**: Always retry subscription in `Start()` when `GameStateManager.Instance` is null in `OnEnable()`
4. **Runtime Components**: Add `StudentInteractableObject` at runtime only (not import-time)

## Unfinished Tasks

1. **Compile Errors**: None currently - all fixed
2. **Testing Needed**:
   - Verify scripted interactions trigger at 20s/40s/60s/80s
   - Test periodic summary (30s intervals)
   - Confirm influence chains: Bình→Lan (SingleStudent), wandering→WholeClass
3. **Unity Editor**:
   - Add GameLoggerConfig to scene
   - Configure toggles for testing phases

## Key Code Patterns

### Late Subscription
```csharp
void Start() {
    if (!hasSubscribed && GameStateManager.Instance != null) {
        GameStateManager.Instance.OnStateChanged += HandleGameStateChanged;
        hasSubscribed = true;
    }
}
```

### Dead Zone Filtering
```csharp
if (Mathf.Abs(horizontal) < movementDeadZone) horizontal = 0f;
if (Mathf.Abs(mouseX) < mouseDeadZone) mouseX = 0f;
```

### Influence Scope Fallback
```csharp
if (evt.targetStudent == null) {
    StudentAgent nearest = FindNearestStudentInRange(source, maxDistance);
    if (nearest != null) evt.targetStudent = nearest;
    else ProcessWholeClassInfluence(...);
}
```

### GameLogger Usage
```csharp
GameLogger.Milestone("Component", "Critical event message");
GameLogger.Detail("Component", "Debug info message");
GameLogger.Trace("Component", "Per-frame detail");
GameLogger.Warning("Component", "Issue message");
GameLogger.Error("Component", "Error message");
```

## Test Scenario
**File**: `level_first_day_summer_break.json`
- 4 students: Bình, Lan, Nam, Mai
- Scripted interactions at 20s, 40s, 60s, 80s
- Expected flow: Bình wandering→Lan distracted→Nam throwing→Lan acting out

## Next Steps
1. Add GameLoggerConfig to scene in Unity Editor
2. Play test with MILESTONE logs only
3. Verify scripted events trigger at correct times
4. Enable DETAIL/TRACE as needed for debugging
5. Check periodic summary every 30s
