# Student Peer Influence System

## Overview

The Peer Influence System allows student actions to affect nearby students, creating realistic group reactions and chain behaviors. When a student performs a significant action (throwing objects, making noise, etc.), nearby students may react based on distance, severity, and their individual susceptibility.

This is a **non-invasive layer** that works alongside existing systems without modifying core gameplay logic.

## Architecture

### Core Components

1. **StudentInfluenceManager.cs** - Central manager that processes peer influence
2. **StudentConfig** - Stores per-student influence parameters
3. **StudentEventManager** - Event source for influence triggers
4. **StudentAgent** - Receives influence effects (state changes, reactions)

### Integration Points

```
StudentEventManager.OnEventLogged
            ↓
StudentInfluenceManager.HandleStudentEvent()
            ↓
IsInfluenceTrigger() → ProcessInfluence()
            ↓
FindNearbyStudents() → CalculateInfluenceStrength()
            ↓
ApplyInfluence() → StudentAgent.EscalateState() / TriggerReaction()
```

## How It Works

### 1. Event Detection

The system listens to `StudentEventManager` for significant events:

**Influence Triggers:**
- `ThrowingObject` - High severity (0.9)
- `KnockedOverObject` - High severity (0.7)
- `MakingNoise` - Medium severity (0.6)
- `LeftSeat` - Medium severity (0.5)
- `WanderingAround` - Low severity (0.4)

### 2. Distance-Based Calculation

Influence strength depends on distance from the source:

| Distance | Category | Multiplier |
|----------|----------|------------|
| 0-2m | Strong | 1.0x |
| 2-4m | Medium | 0.6x |
| 4-6m | Weak | 0.3x |
| >6m | None | 0x |

### 3. Influence Strength Formula

```
Strength = BaseSeverity × DistanceMultiplier × Falloff × Susceptibility × (1 - Resistance)
```

Where:
- **BaseSeverity**: Event type severity (0.4 - 0.9)
- **DistanceMultiplier**: Based on distance category
- **Falloff**: Linear falloff from 1.0 at source to 0.0 at max radius
- **Susceptibility**: Target student's `influenceSusceptibility` (0-1)
- **Resistance**: Target student's `influenceResistance` (0-1)

### 4. Effect Application

Based on calculated strength:

**Strong Influence (≥ Panic Threshold):**
- Escalates student state
- Triggers `Scared` reaction (strength ≥ 0.8) or `Confused` reaction
- Always affects the student

**Medium Influence (0.3 - Panic Threshold):**
- May escalate state (probabilistic based on current state)
- 60% chance to trigger `Confused` reaction
- More likely to affect Calm or Distracted students

**Weak Influence (< 0.3):**
- Only affects Calm students occasionally
- Low probability of state escalation
- No guaranteed reactions

## Configuration

### StudentInfluenceManager Settings (Inspector)

Add `StudentInfluenceManager` component to a GameObject in your scene.

**Influence Settings:**
- **Max Influence Radius**: Maximum distance for influence (default: 6m)
- **Strong Influence Distance**: Threshold for strong influence (default: 2m)
- **Medium Influence Distance**: Threshold for medium influence (default: 4m)

**Influence Strength Multipliers:**
- **Strong Influence Multiplier**: Multiplier for close students (default: 1.0)
- **Medium Influence Multiplier**: Multiplier for medium distance (default: 0.6)
- **Weak Influence Multiplier**: Multiplier for far students (default: 0.3)

**Debug:**
- **Enable Debug Logs**: Show detailed influence logs in console

### StudentConfig Settings (Per Student)

Configure in each student's StudentConfig asset:

**Peer Influence Settings:**

- **Influence Susceptibility** (0-1)
  - How easily this student is influenced by peers
  - 0 = Immune to influence
  - 0.5 = Normal susceptibility (default)
  - 1 = Highly susceptible
  
- **Influence Resistance** (0-1)
  - How much this student resists peer influence
  - 0 = No resistance (default: 0.2)
  - 0.5 = Moderate resistance
  - 1 = Complete resistance (immune)
  
- **Panic Threshold** (0-1)
  - Influence strength that triggers strong reactions
  - 0.5 = Easily panicked
  - 0.7 = Normal threshold (default)
  - 0.9 = Hard to panic
  
- **Custom Influence Radius** (meters)
  - Override global radius for this student
  - 0 = Use global radius (default)
  - >0 = Custom radius

## Example Configurations

### Example 1: Nervous Student (Easily Influenced)

```
Influence Susceptibility: 0.8
Influence Resistance: 0.1
Panic Threshold: 0.5
Custom Influence Radius: 0 (use global)
```

**Result**: Highly susceptible to peer influence, panics easily, affected by distant events.

### Example 2: Calm Student (Resistant)

```
Influence Susceptibility: 0.3
Influence Resistance: 0.6
Panic Threshold: 0.85
Custom Influence Radius: 0
```

**Result**: Resistant to influence, rarely panics, only affected by very close/severe events.

### Example 3: Leader Student (Immune)

```
Influence Susceptibility: 0.1
Influence Resistance: 0.9
Panic Threshold: 0.95
Custom Influence Radius: 0
```

**Result**: Nearly immune to peer influence, almost never affected by others.

### Example 4: Sensitive Student (Wide Awareness)

```
Influence Susceptibility: 0.7
Influence Resistance: 0.2
Panic Threshold: 0.6
Custom Influence Radius: 8.0
```

**Result**: Affected by events farther away than other students, moderately susceptible.

## Usage in Game

### Setup

1. **Add StudentInfluenceManager to Scene**
   - Create empty GameObject named "StudentInfluenceManager"
   - Add `StudentInfluenceManager` component
   - Configure global settings in Inspector

2. **Configure Student Personalities**
   - Open each StudentConfig asset
   - Adjust "Peer Influence Settings" section
   - Set susceptibility, resistance, and panic threshold

3. **Play and Observe**
   - Students will automatically influence each other
   - Check console for detailed influence logs

### Expected Console Output

```
[StudentInfluenceManager] Influence system activated
[StudentInfluenceManager] Found 5 students in scene
[Influence] Nam triggered influence event: ThrowingObject
[Influence] 3 nearby students detected within 6m
[Influence] Lan affected by Nam at 1.5m with strength 0.72
[Influence] Lan escalated from Calm to Distracted (strong influence from Nam)
[Influence] Lan → reaction Scared (strength 0.72)
[Influence] Minh affected by Nam at 3.2m with strength 0.41
[Influence] Minh → reaction Confused (strength 0.41)
[Influence] Thuật affected by Nam at 5.8m with strength 0.15
[Influence] Thuật escalated from Calm to Distracted (weak influence from Nam)
```

## Gameplay Scenarios

### Scenario 1: Chain Reaction

1. **Nam throws object** (ThrowingObject event)
2. **Lan** (1.5m away, high susceptibility) → Scared, escalates to Distracted
3. **Lan's state change** triggers new behaviors
4. **Minh** (3m away) → Confused, may escalate
5. **Classroom disruption increases** from multiple affected students

### Scenario 2: Isolated Incident

1. **Thuật makes noise** (MakingNoise event, medium severity)
2. **No students within 2m** → No strong influence
3. **Minh** (4m away, high resistance) → Not affected
4. **Minimal classroom impact**

### Scenario 3: Panic Spread

1. **Critical student leaves seat** (LeftSeat event)
2. **Multiple nearby students** within strong influence range
3. **Several students panic** → Multiple state escalations
4. **Classroom state rapidly deteriorates**

## Integration with Existing Systems

### ✅ Compatible With

- **StudentAgent**: Uses existing `EscalateState()` and `TriggerReaction()` methods
- **StudentEventManager**: Listens to existing events, no modifications needed
- **ClassroomManager**: Works alongside disruption tracking
- **Sequence System**: Does not interfere with ongoing sequences
- **Item Confiscation**: Confiscation events can trigger influence
- **Teacher Actions**: Teacher interventions work normally

### ❌ Does Not Break

- Student autonomous behaviors
- Teacher-student interactions
- Object interaction system
- Level objectives and scoring
- Existing state transitions
- Reaction system

## Performance Considerations

### Efficient Design

- **Event-Driven**: Only processes when relevant events occur
- **No Update() Loop**: Avoids per-frame calculations
- **Spatial Queries**: Uses simple distance checks, not physics queries
- **Early Exit**: Skips processing if no nearby students

### Performance Tips

1. **Limit Max Radius**: Keep `maxInfluenceRadius` reasonable (4-8m)
2. **Reduce Student Count**: Fewer students = less processing
3. **Disable Debug Logs**: Turn off `enableDebugLogs` in production
4. **Adjust Thresholds**: Higher panic thresholds = fewer reactions

## Customization Points

### For Designers

- Adjust global influence distances and multipliers
- Configure per-student susceptibility and resistance
- Balance panic thresholds for different student types
- Create student archetypes (nervous, calm, leader, etc.)

### For Programmers

#### Add New Influence Triggers

Modify `IsInfluenceTrigger()`:

```csharp
private bool IsInfluenceTrigger(StudentEventType eventType)
{
    return eventType switch
    {
        StudentEventType.ThrowingObject => true,
        StudentEventType.YourNewEvent => true, // Add here
        _ => false
    };
}
```

#### Adjust Severity Values

Modify `GetInfluenceSeverity()`:

```csharp
private float GetInfluenceSeverity(StudentEventType eventType)
{
    return eventType switch
    {
        StudentEventType.YourNewEvent => 0.8f, // Add here
        _ => 0.3f
    };
}
```

#### Custom Influence Effects

Extend `ApplyInfluence()` or create new effect methods:

```csharp
private void ApplyCustomInfluence(StudentAgent target, float strength)
{
    // Your custom logic here
    if (strength > 0.9f)
    {
        target.TriggerReaction(StudentReactionType.Cry, 5f);
    }
}
```

## Troubleshooting

### Students not being influenced

**Check:**
- StudentInfluenceManager exists in scene and is active
- Students are within `maxInfluenceRadius`
- Student `influenceSusceptibility` > 0
- Student `influenceResistance` < 1
- Debug logs are enabled to see calculations

### Too much influence / Chain reactions

**Solutions:**
- Increase `influenceResistance` for most students
- Increase `panicThreshold` values
- Reduce `maxInfluenceRadius`
- Lower `influenceSusceptibility` globally

### Not enough influence / No reactions

**Solutions:**
- Decrease `influenceResistance`
- Decrease `panicThreshold`
- Increase `influenceSusceptibility`
- Increase influence multipliers in manager

### Performance issues

**Solutions:**
- Disable debug logs
- Reduce `maxInfluenceRadius`
- Limit number of students in scene
- Increase distance thresholds

## Advanced Features

### Manual Student List Refresh

If you spawn students dynamically:

```csharp
StudentInfluenceManager.Instance.ForceRefreshStudentList();
```

### Custom Influence Radius Per Student

Set `customInfluenceRadius` > 0 in StudentConfig to override global radius for specific students.

### Influence Strength Inspection

Enable debug logs to see exact influence calculations in console.

## Best Practices

1. **Start Conservative**: Begin with low susceptibility (0.3-0.5) and adjust up
2. **Vary Student Types**: Mix susceptible and resistant students for interesting dynamics
3. **Test Scenarios**: Verify influence works at different distances
4. **Balance Gameplay**: Ensure influence enhances, not dominates, gameplay
5. **Monitor Performance**: Watch for excessive influence processing

## Future Extensions

Potential areas for expansion:

- Positive influence (calming effects from teacher presence)
- Influence decay over time
- Student relationships affecting influence strength
- Group formation and leader dynamics
- Influence visualization (debug gizmos)
- Influence history tracking for analytics
