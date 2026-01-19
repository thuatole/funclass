# StudentInteractionProcessor Setup Guide

## Quick Setup (Recommended)

### Step 1: Add Systems to Scene
1. In Unity Editor, go to: **FunClass → Quick Setup → Add Student Systems**
2. Click **"Setup All (Processor + Visual Markers)"**

This will:
- Add `StudentInteractionProcessor` to scene (under Managers group)
- Add `StudentVisualMarker` to all students (color-coded capsules + name labels)

### Step 2: Load Interactions from JSON
The `StudentInteractionProcessor` needs to load interactions from your level JSON.

**Option A: Automatic (via LevelLoader)**
- Interactions will be loaded automatically when level is imported from JSON
- Make sure your JSON has `studentInteractions` section

**Option B: Manual (for testing)**
```csharp
// In your level setup code
if (StudentInteractionProcessor.Instance != null)
{
    var interactions = new List<StudentInteractionConfig>
    {
        new StudentInteractionConfig
        {
            sourceStudent = "Student_B",
            targetStudent = "Student_C",
            eventType = "ThrowingObject",
            triggerCondition = "Always",
            probability = 1.0f,
            customSeverity = 0.8f,
            description = "B hits C"
        }
    };
    
    StudentInteractionProcessor.Instance.LoadInteractions(interactions);
}
```

## Student Visual Markers

### Color Coding
- **Student_A** = Red
- **Student_B** = Blue
- **Student_C** = Green
- **Student_D** = Yellow
- **Student_E** = Orange
- **Student_F** = Purple
- **Student_G** = Cyan
- **Student_H** = Pink

### Features
- Colored capsule mesh
- Floating name label above student
- Label always faces camera
- Easy to identify students in scene

## Debug Logs

### StudentInteractionProcessor Logs
```
[StudentInteractionProcessor] Awake - Instance created
[StudentInteractionProcessor] OnEnable called
[StudentInteractionProcessor] Subscribed to GameStateManager
[StudentInteractionProcessor] Loaded 1 student interactions
[StudentInteractionProcessor]   - Student_B → Student_C (ThrowingObject, Always, prob: 1)
[StudentInteractionProcessor] Activated

[StudentInteractionProcessor] >>> Checking 1 interactions
[StudentInteractionProcessor] Checking: Student_B → Student_C (Always)
[StudentInteractionProcessor]   ✓ All checks passed! (state: Critical, roll: 0.45 <= 1.00)
[StudentInteractionProcessor] >>> Triggering: Student_B → Student_C (ThrowingObject)
[StudentInteractionProcessor] ✓ Triggered interaction event
```

### Common Issues

#### Issue 1: No interactions triggering
**Symptoms:**
- No `[StudentInteractionProcessor]` logs
- Students don't interact

**Solutions:**
1. Check if `StudentInteractionProcessor` exists in scene
   - Look for it under `=== MANAGERS ===` group
   - Or use Quick Setup tool

2. Check if interactions are loaded
   - Look for log: `Loaded X student interactions`
   - If 0, interactions not loaded from JSON

3. Check trigger conditions
   - `Always` - Always triggers (filtered by probability)
   - `OnActingOut` - Only when source is ActingOut
   - `OnCritical` - Only when source is Critical
   - `Random` - Random (filtered by probability)

4. Check probability
   - Set to `1.0` for testing (100% chance)
   - Lower values may not trigger every check

#### Issue 2: Interactions loaded but not triggering
**Check logs for:**
```
✗ Source student not found: Student_B
```
→ Student name mismatch in JSON

```
✗ Student_B is following route
```
→ Student is moving, can't trigger interaction
→ Wait until student stops moving

```
✗ Condition not met: OnCritical (current state: Calm)
```
→ Source student not in required state
→ Change trigger condition to `Always` for testing

```
✗ Probability check failed: 0.95 > 0.90
```
→ Random roll failed
→ Increase probability to `1.0` for testing

#### Issue 3: Can't differentiate students
**Solution:**
- Use Quick Setup tool: **FunClass → Quick Setup → Add Student Systems**
- Click **"Add StudentVisualMarker to All Students"**
- Students will be color-coded and labeled

## Testing Scenario

### Test B→C Interaction

**JSON Configuration:**
```json
{
  "studentInteractions": [
    {
      "sourceStudent": "Student_B",
      "targetStudent": "Student_C",
      "eventType": "ThrowingObject",
      "triggerCondition": "Always",
      "probability": 1.0,
      "customSeverity": 0.8,
      "description": "B always hits C (testing)"
    }
  ]
}
```

**Expected Behavior:**
1. Every 2 seconds, processor checks interactions
2. If B is not immune and not following route → Trigger
3. Create StudentEvent: B → C (ThrowingObject, SingleStudent)
4. C.InfluenceSources.AddSource(B, ThrowingObject, 0.8)
5. C affected by B

**Expected Logs:**
```
[StudentInteractionProcessor] >>> Checking 1 interactions
[StudentInteractionProcessor] Checking: Student_B → Student_C (Always)
[StudentInteractionProcessor]   ✓ All checks passed!
[StudentInteractionProcessor] >>> Triggering: Student_B → Student_C (ThrowingObject)
[InfluenceSources] >>> AddSource called: Student_B → Student_C (ThrowingObject, strength: 0.80)
[InfluenceSources] ✓ Added NEW source to Student_C
```

## Advanced Configuration

### Multiple Interactions
```json
{
  "studentInteractions": [
    {
      "sourceStudent": "Student_A",
      "targetStudent": "Student_B",
      "eventType": "ThrowingObject",
      "triggerCondition": "OnActingOut",
      "probability": 0.8
    },
    {
      "sourceStudent": "Student_C",
      "targetStudent": "Student_D",
      "eventType": "ThrowingObject",
      "triggerCondition": "OnCritical",
      "probability": 0.9
    }
  ]
}
```

### Custom Severity
```json
{
  "sourceStudent": "Student_B",
  "targetStudent": "Student_C",
  "eventType": "ThrowingObject",
  "triggerCondition": "Always",
  "probability": 1.0,
  "customSeverity": 0.95,  // Very high severity
  "description": "B hits C very hard"
}
```

Use `-1` for default severity from `influenceScopeSettings`.

## Troubleshooting Checklist

- [ ] StudentInteractionProcessor exists in scene?
- [ ] StudentInteractionProcessor loaded interactions? (check logs)
- [ ] Student names match exactly between JSON and scene?
- [ ] Trigger condition appropriate for student state?
- [ ] Probability set high enough for testing? (use 1.0)
- [ ] Students have StudentVisualMarker for easy identification?
- [ ] Check interval appropriate? (default 2s)
- [ ] Debug logs enabled? (check enableDebugLogs in Inspector)

## Performance Notes

- Check interval: 2 seconds (configurable)
- Only checks when game is InLevel state
- Skips students that are immune or following routes
- Minimal performance impact with <10 interactions
