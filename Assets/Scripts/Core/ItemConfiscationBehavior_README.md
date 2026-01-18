# Item Confiscation Reaction System

## Overview

This system allows students to react dynamically when the teacher confiscates items from them. Different items can trigger different reactions, state changes, and disruption reductions.

## Architecture

### Core Components

1. **ItemConfiscationBehavior.cs** - Defines behavior rules for specific items
2. **StudentAgent.OnItemConfiscated()** - Hook method that processes confiscation
3. **StudentConfig** - Stores confiscation behavior configurations
4. **ClassroomManager.ReduceDisruption()** - Reduces classroom disruption

### Integration Points

- **TeacherController.UseItemOnStudent()** → calls `student.TakeObjectAway(item)`
- **StudentAgent.TakeObjectAway()** → calls `OnItemConfiscated(itemName)`
- **StudentAgent.OnItemConfiscated()** → applies reactions and state changes
- **ClassroomManager** → receives disruption reduction notifications

## Configuration

### In Unity Inspector (StudentConfig asset)

#### Item Confiscation Reactions Section

Add entries to the `Confiscation Behaviors` array. Each entry has:

- **Item Keywords**: Array of strings to match against item names (case-insensitive)
  - Example: `["toy", "ball"]` matches "Toy Car", "Basketball", etc.
  
- **Reaction**: The emotional reaction type
  - Options: None, Cry, Apologize, Angry, Scared, Embarrassed, Confused
  
- **Reaction Duration**: How long the reaction lasts (seconds)

- **Change State**: Whether to change the student's state
  
- **New State**: The target state if changeState is true
  - Options: Calm, Distracted, ActingOut, Critical
  
- **Disruption Reduction**: How much classroom disruption decreases (0-20)

#### Default Confiscation Behavior Section

Fallback behavior when no specific rule matches:

- **Default Confiscation Reaction**: Default reaction type
- **Default Confiscation Reaction Duration**: Default duration
- **Default Confiscation Changes State**: Whether to change state by default
- **Default Confiscation New State**: Default target state

## Example Configurations

### Example 1: Taking a Toy (Makes student angry)

```
Item Keywords: ["toy", "game", "ball"]
Reaction: Angry
Reaction Duration: 5.0
Change State: true
New State: Distracted
Disruption Reduction: 10.0
```

**Result**: Student becomes angry for 5 seconds, transitions to Distracted state, classroom disruption reduces by 10.

### Example 2: Taking a Book (Calms student)

```
Item Keywords: ["book", "textbook", "notebook"]
Reaction: Embarrassed
Reaction Duration: 3.0
Change State: true
New State: Calm
Disruption Reduction: 15.0
```

**Result**: Student feels embarrassed for 3 seconds, becomes Calm, disruption reduces by 15.

### Example 3: Taking a Phone (Student apologizes)

```
Item Keywords: ["phone", "mobile", "cellphone"]
Reaction: Apologize
Reaction Duration: 4.0
Change State: true
New State: Calm
Disruption Reduction: 12.0
```

**Result**: Student apologizes for 4 seconds, becomes Calm, disruption reduces by 12.

### Example 4: Taking Dangerous Item (Student scared)

```
Item Keywords: ["scissors", "knife", "sharp"]
Reaction: Scared
Reaction Duration: 6.0
Change State: true
New State: Calm
Disruption Reduction: 20.0
```

**Result**: Student becomes scared for 6 seconds, immediately calms down, major disruption reduction.

## Usage in Game

### For Teachers (Player)

1. Look at a student with an item
2. Press `4` to use held item on student (or trigger confiscation)
3. The system automatically:
   - Removes the item from the scene
   - Triggers the appropriate student reaction
   - Changes student state if configured
   - Reduces classroom disruption
   - Logs all events to console

### Expected Console Output

```
[TeacherController] Using ToyBall on Nam
[StudentAgent] Teacher took ToyBall away from Nam
[StudentEvent] Nam: had ToyBall taken away
[StudentAgent] Nam processing confiscation of: ToyBall
[StudentAgent] Nam matched confiscation behavior for: ToyBall
[StudentAgent] Nam applying custom confiscation behavior for: ToyBall
[Reaction] Nam looks angry
[StudentEvent] Nam: looks angry
[StudentAgent] Nam state changed from ActingOut to Distracted due to confiscation of ToyBall
[ClassroomManager] Disruption decreased by 10.0 to 45.0/100 (item confiscated)
[StudentAgent] Nam confiscation reduced disruption by 10.0
```

## Adding New Behavior Rules

### Option 1: Via Unity Inspector (Recommended)

1. Select the StudentConfig asset
2. Expand "Item Confiscation Reactions"
3. Increase the "Confiscation Behaviors" array size
4. Fill in the new entry with keywords and behavior

### Option 2: Via Code Extension

To add programmatic rules, extend `StudentAgent.OnItemConfiscated()`:

```csharp
public void OnItemConfiscated(string itemName)
{
    // ... existing code ...
    
    // Custom logic example:
    if (itemName.Contains("special"))
    {
        TriggerReaction(StudentReactionType.Confused, 5f);
        SetState(StudentState.Calm);
        return;
    }
    
    // ... rest of existing code ...
}
```

## Event Flow Diagram

```
Teacher presses key → TeacherController.UseItemOnStudent()
                              ↓
                    StudentAgent.TakeObjectAway()
                              ↓
                    StudentEventManager.LogEvent(ObjectTakenAway)
                              ↓
                    StudentAgent.OnItemConfiscated()
                              ↓
                    FindMatchingConfiscationBehavior()
                              ↓
                    ┌─────────┴─────────┐
                    ↓                   ↓
        ApplyConfiscationBehavior   ApplyDefaultConfiscationBehavior
                    ↓                   ↓
            TriggerReaction()      TriggerReaction()
            SetState()             SetState() or DeescalateState()
            ReduceDisruption()
                    ↓
        StudentEventManager.LogEvent(StudentReacted)
        ClassroomManager.ReduceDisruption()
```

## Integration with Existing Systems

### ✅ Compatible With

- **StudentReaction System**: Uses existing `TriggerReaction()` method
- **StudentState System**: Uses existing `SetState()` and `DeescalateState()`
- **StudentEvent System**: Logs events through `StudentEventManager`
- **Sequence System**: Does not interfere with ongoing sequences
- **ClassroomManager**: Integrates with disruption tracking
- **TeacherScoreManager**: Events are automatically tracked for scoring

### ❌ Does Not Break

- Existing teacher actions (Calm, Stop, Scold, etc.)
- Student autonomous behaviors
- Object interaction system
- Level objectives and win/lose conditions

## Customization Points

### For Designers

- Configure behaviors per student in StudentConfig assets
- Adjust reaction types and durations
- Control state transitions
- Balance disruption reduction values

### For Programmers

- Extend `OnItemConfiscated()` for complex logic
- Add new `StudentReactionType` values in enum
- Modify `ItemConfiscationBehavior` class for additional properties
- Hook into the confiscation event for custom systems

## Best Practices

1. **Use Keywords Wisely**: Make keywords specific enough to avoid false matches
2. **Balance Disruption**: Keep reduction values between 5-20 for game balance
3. **Reaction Duration**: 3-5 seconds is ideal for most reactions
4. **State Changes**: Only change state when it makes narrative sense
5. **Test Combinations**: Verify behaviors work with different student states

## Troubleshooting

### Student doesn't react when item is taken

- Check that StudentConfig has confiscation behaviors configured
- Verify item name matches one of the keywords
- Check console for `[StudentAgent] processing confiscation` logs

### Wrong reaction triggered

- Check keyword matching - more specific keywords should come first in array
- Verify item name in scene matches expected keywords

### Disruption not reducing

- Ensure ClassroomManager exists in scene
- Check that `disruptionReduction` value is > 0
- Verify ClassroomManager is active during gameplay

## Future Extensions

Potential areas for expansion:

- Item rarity affecting reaction intensity
- Student personality traits influencing reactions
- Multiple items confiscated triggering combo reactions
- Teacher reputation affecting student reactions
- Time-of-day or classroom state affecting reactions
