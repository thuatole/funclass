# Mess & Cleanup System

## Overview

The Mess & Cleanup System provides a complete framework for handling mess events (like vomit) in the FunClass Unity project. Students can create mess objects that affect classroom disruption, and teachers can clean them up for score rewards and disruption reduction.

**Complete Gameplay Loop:**
```
Student Action (Vomit) 
        ↓
Mess Object Spawned (VomitMess)
        ↓
Disruption Increases
        ↓
Teacher Detects Mess
        ↓
Contextual Prompt Appears
        ↓
Teacher Cleans Mess (Press E)
        ↓
Mess Removed
        ↓
Disruption Reduced + Score Awarded
```

## Architecture

### Core Components

1. **MessObject** - Base class for all mess types
2. **VomitMess** - Vomit implementation
3. **TeacherController** - Mess detection and cleanup
4. **StudentEventManager** - Event logging (MessCreated, MessCleaned)
5. **ClassroomManager** - Disruption tracking
6. **TeacherScoreManager** - Score rewards

### Class Hierarchy

```
MonoBehaviour
    ↓
MessObject (abstract base class)
    ↓
VomitMess (concrete implementation)
```

## How It Works

### 1. Mess Object System

**MessObject Base Class:**
- Configurable severity, disruption, and cleanup rewards
- Automatic disruption increase on creation
- Cleanup time support (instant or timed)
- Event logging integration
- Interaction prompt generation

**Key Properties:**
```csharp
public string messName = "Mess";
public int severityLevel = 5;           // 1-10 scale
public float disruptionAmount = 10f;    // Added when created
public float cleanupDisruptionReduction = 15f;  // Removed when cleaned
public int cleanupScore = 10;           // Points awarded
public float cleanupTime = 0f;          // Cleanup duration (0 = instant)
```

### 2. VomitMess Implementation

**Default Values:**
- Severity: 7/10
- Disruption Added: 12
- Disruption Reduced: 15
- Score Reward: 15 points
- Cleanup Time: Instant (0s)

**Creation:**
```csharp
VomitMess.Create(position, studentCreator, puddlePrefab);
```

### 3. Teacher Interaction Flow

**Detection:**
1. TeacherController raycasts forward
2. Checks for MessObject component
3. Displays contextual prompt
4. Prioritizes mess over other interactables

**Cleanup:**
1. Player presses E while looking at mess
2. `CleanMess()` called on TeacherController
3. `StartCleanup()` called on MessObject
4. Cleanup completes (instant or after delay)
5. Disruption reduced, score awarded
6. Mess object destroyed

### 4. Event Logging

**Events Generated:**

**MessCreated:**
```
[Mess] Nam created vomit
[StudentEvent] Nam: created vomit
[ClassroomManager] Disruption increased by 12.0 to 42.0/100 (vomit created)
```

**MessCleaned:**
```
[Teacher] Cleaning vomit
[Teacher] Cleaned vomit successfully
[StudentEvent] Nam: vomit was cleaned by teacher
[ClassroomManager] Disruption decreased by 15.0 to 27.0/100 (vomit cleaned)
[TeacherScoreManager] earned 15 points: Cleaned vomit (Total: 85)
```

## Configuration

### Creating a New Mess Type

**Step 1: Create Mess Class**
```csharp
using UnityEngine;

namespace FunClass.Core
{
    public class SpillMess : MessObject
    {
        protected override void Awake()
        {
            base.Awake();
            
            messName = "spill";
            severityLevel = 4;
            disruptionAmount = 8f;
            cleanupDisruptionReduction = 10f;
            cleanupScore = 10;
            cleanupTime = 2f; // 2 second cleanup
        }
        
        public static SpillMess Create(Vector3 position, StudentAgent creator)
        {
            GameObject messObject = new GameObject("SpillMess");
            messObject.transform.position = position;
            
            SpillMess spill = messObject.AddComponent<SpillMess>();
            
            // Add collider
            SphereCollider collider = messObject.AddComponent<SphereCollider>();
            collider.radius = 0.3f;
            
            spill.Initialize(creator);
            return spill;
        }
    }
}
```

**Step 2: Create Visual Prefab**
1. Create GameObject in scene
2. Add visual mesh/sprite
3. Save as prefab
4. Assign to mess creation code

**Step 3: Integrate with Student Actions**
```csharp
// In StudentAgent or custom component
public void SpillDrink()
{
    Vector3 spillPosition = transform.position + transform.forward * 0.5f;
    SpillMess.Create(spillPosition, this);
}
```

### VomitMess Setup

**In Unity Inspector:**

1. **Create Vomit Puddle Prefab:**
   - Create plane or sprite
   - Scale to puddle size (e.g., 0.5 x 0.01 x 0.5)
   - Add vomit texture/material
   - Save as prefab

2. **Add to StudentAgent (Optional):**
   - Add `StudentMessCreator` component
   - Assign vomit puddle prefab
   - Configure vomit chance and cooldown

3. **Manual Integration:**
   ```csharp
   // In StudentAgent.cs
   [Header("Mess Settings")]
   [SerializeField] private GameObject vomitPuddlePrefab;
   
   public void PerformVomitAction()
   {
       StudentAgentMessIntegration.CreateVomitMess(this, vomitPuddlePrefab);
       TriggerReaction(StudentReactionType.Embarrassed, 5f);
   }
   ```

## Usage Examples

### Example 1: Student Vomits, Teacher Cleans

**Scenario:** Nam is in Critical state and vomits

**Console Output:**
```
[StudentMessCreator] Nam is vomiting!
[Mess] Nam created vomit
[StudentEvent] Nam: created vomit
[ClassroomManager] Disruption increased by 12.0 to 42.0/100 (vomit created)
[Reaction] Nam looks embarrassed

[Teacher looks at vomit]
[TeacherController] Looking at: Press E to clean vomit

[Teacher presses E]
[Teacher] Cleaning vomit
[Teacher] Cleaned vomit successfully
[StudentEvent] Nam: vomit was cleaned by teacher
[ClassroomManager] Disruption decreased by 15.0 to 27.0/100 (vomit cleaned)
[TeacherScoreManager] earned 15 points: Cleaned vomit (Total: 85)
```

### Example 2: Multiple Messes

**Scenario:** Two students create messes, teacher cleans both

**Flow:**
```
1. Nam vomits → Disruption +12 (Total: 42)
2. Lan vomits → Disruption +12 (Total: 54)
3. Teacher cleans Nam's vomit → Disruption -15 (Total: 39), Score +15
4. Teacher cleans Lan's vomit → Disruption -15 (Total: 24), Score +15
5. Classroom order restored
```

### Example 3: Timed Cleanup

**Scenario:** Mess requires 3 seconds to clean

```csharp
public class BigVomitMess : MessObject
{
    protected override void Awake()
    {
        base.Awake();
        messName = "big vomit";
        cleanupTime = 3f; // 3 second cleanup
        cleanupScore = 25; // Higher reward
    }
}
```

**Gameplay:**
```
1. Teacher presses E
2. [Teacher] Started cleaning big vomit
3. [Wait 3 seconds...]
4. [Teacher] Cleaned big vomit successfully
5. Score +25
```

## Integration with Existing Systems

### ✅ StudentEventManager

**New Event Types:**
- `StudentEventType.MessCreated` - When mess is spawned
- `StudentEventType.MessCleaned` - When mess is cleaned

**Usage:**
```csharp
StudentEventManager.Instance.LogEvent(
    student,
    StudentEventType.MessCreated,
    "created vomit",
    messGameObject
);
```

### ✅ ClassroomManager

**Disruption Integration:**
```csharp
// Mess created
ClassroomManager.Instance.AddDisruption(12f, "vomit created");

// Mess cleaned
ClassroomManager.Instance.AddDisruption(-15f, "vomit cleaned");
```

### ✅ TeacherScoreManager

**Score Rewards:**
```csharp
TeacherScoreManager.Instance.AddScore(15, "Cleaned vomit");
```

### ✅ TeacherController

**Interaction Priority:**
1. MessObject (highest priority)
2. StudentAgent
3. InteractableObject

**Why:** Ensures teacher can clean messes even if students are nearby

### ✅ Level Objectives

Mess cleanup can count toward objectives:
- "Clean up all messes"
- "Maintain disruption below 50"
- "Earn 100 points"

## API Reference

### MessObject

**Public Methods:**

**Initialize(StudentAgent student)**
```csharp
// Called when mess is created
mess.Initialize(studentCreator);
```

**GetInteractionPrompt()**
```csharp
// Returns prompt string
string prompt = mess.GetInteractionPrompt();
// Returns: "Press E to clean vomit"
```

**StartCleanup(TeacherController teacher)**
```csharp
// Begins cleanup process
mess.StartCleanup(teacher);
```

**IsCleaned()**
```csharp
// Check if already cleaned
if (mess.IsCleaned()) return;
```

**GetCreator()**
```csharp
// Get student who created mess
StudentAgent creator = mess.GetCreator();
```

**GetAge()**
```csharp
// Get how long mess has existed
float age = mess.GetAge(); // in seconds
```

### VomitMess

**Static Factory Method:**

**Create(Vector3 position, StudentAgent creator, GameObject puddlePrefab = null)**
```csharp
VomitMess vomit = VomitMess.Create(
    position,
    studentCreator,
    vomitPuddlePrefab
);
```

### TeacherController

**New Methods:**

**GetCurrentMessTarget()**
```csharp
MessObject mess = TeacherController.Instance.GetCurrentMessTarget();
```

### StudentAgentMessIntegration

**Static Helper Methods:**

**CreateVomitMess(StudentAgent student, GameObject puddlePrefab = null)**
```csharp
VomitMess vomit = StudentAgentMessIntegration.CreateVomitMess(
    student,
    vomitPuddlePrefab
);
```

## Scoring & Disruption

### Default Values

| Mess Type | Disruption Added | Disruption Removed | Score Reward |
|-----------|------------------|-------------------|--------------|
| **Vomit** | +12 | -15 | +15 |

### Customization

**Per-Mess Configuration:**
```csharp
protected override void Awake()
{
    base.Awake();
    
    // Customize values
    disruptionAmount = 20f;        // High disruption
    cleanupDisruptionReduction = 25f;  // Big reward for cleaning
    cleanupScore = 30;             // High score
}
```

**Balance Considerations:**
- **Disruption Removed > Disruption Added**: Incentivizes cleanup
- **Higher Score = Higher Disruption**: Harder messes worth more
- **Cleanup Time**: Longer cleanup = higher rewards

## Advanced Features

### 1. Timed Cleanup

**Implementation:**
```csharp
public class DifficultMess : MessObject
{
    protected override void Awake()
    {
        base.Awake();
        cleanupTime = 5f; // 5 seconds to clean
    }
    
    protected override System.Collections.IEnumerator CleanupCoroutine(TeacherController teacher)
    {
        Debug.Log("[Mess] Cleanup started...");
        
        // Optional: Show progress
        float elapsed = 0f;
        while (elapsed < cleanupTime)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / cleanupTime;
            Debug.Log($"[Mess] Cleanup progress: {progress * 100:F0}%");
            yield return null;
        }
        
        CompleteCleanup(teacher);
    }
}
```

### 2. Mess Aging

**Track how long mess exists:**
```csharp
void Update()
{
    float age = GetAge();
    
    // Increase disruption over time
    if (age > 60f && !hasAgedPenalty)
    {
        hasAgedPenalty = true;
        ClassroomManager.Instance.AddDisruption(5f, "old mess");
    }
}
```

### 3. Multiple Cleanup Steps

**Require multiple interactions:**
```csharp
public class BigMess : MessObject
{
    private int cleanupSteps = 3;
    private int currentStep = 0;
    
    public override void StartCleanup(TeacherController teacher)
    {
        currentStep++;
        Debug.Log($"[Mess] Cleanup step {currentStep}/{cleanupSteps}");
        
        if (currentStep >= cleanupSteps)
        {
            CompleteCleanup(teacher);
        }
    }
    
    public override string GetInteractionPrompt()
    {
        return $"Press E to clean {messName} ({currentStep}/{cleanupSteps})";
    }
}
```

### 4. Cleanup Tools

**Require specific items:**
```csharp
public override void StartCleanup(TeacherController teacher)
{
    HeldItem item = teacher.GetHeldItem();
    
    if (item != null && item.itemName.Contains("mop"))
    {
        // Faster cleanup with mop
        cleanupTime = 1f;
        CompleteCleanup(teacher);
    }
    else
    {
        Debug.Log("[Mess] Need a mop to clean this!");
    }
}
```

### 5. Mess Spreading

**Mess grows over time:**
```csharp
public class SpreadingMess : MessObject
{
    [SerializeField] private float spreadInterval = 10f;
    private float nextSpreadTime;
    
    void Update()
    {
        if (Time.time > nextSpreadTime)
        {
            nextSpreadTime = Time.time + spreadInterval;
            SpreadMess();
        }
    }
    
    private void SpreadMess()
    {
        Vector3 spreadPos = transform.position + Random.insideUnitSphere * 2f;
        spreadPos.y = 0.01f;
        
        SpreadingMess newMess = Create(spreadPos, creator);
        Debug.Log("[Mess] Mess is spreading!");
    }
}
```

## Troubleshooting

### Mess not detected by teacher

**Check:**
- MessObject has a Collider component
- Collider is not set to trigger
- GameObject layer is included in TeacherController.interactionLayer
- Mess is within interaction range (default 3m)

**Solution:**
```csharp
// In VomitMess.Create()
SphereCollider collider = messObject.AddComponent<SphereCollider>();
collider.radius = 0.5f;
collider.isTrigger = false; // Important!
```

### Prompt not showing

**Check:**
- TeacherController is active
- Camera transform is assigned
- Raycast is hitting the mess collider
- GetInteractionPrompt() returns valid string

**Solution:**
Enable debug logs in TeacherController:
```csharp
Debug.Log($"Hit: {hit.collider.name}, MessObject: {mess != null}");
```

### Mess not spawning

**Check:**
- StudentAgent reference is valid
- Position is valid (not underground)
- MessObject.Initialize() is called
- No exceptions in console

**Solution:**
```csharp
// Add validation
if (student == null)
{
    Debug.LogError("[Mess] Cannot create mess - student is null");
    return null;
}
```

### Disruption not changing

**Check:**
- ClassroomManager exists in scene
- ClassroomManager is active
- AddDisruption() is being called

**Solution:**
```csharp
if (ClassroomManager.Instance == null)
{
    Debug.LogWarning("[Mess] ClassroomManager not found");
}
```

### Score not awarded

**Check:**
- TeacherScoreManager exists in scene
- TeacherScoreManager is active during gameplay
- AddScore() is being called

**Solution:**
Verify in TeacherScoreManager:
```csharp
Debug.Log($"[Score] Active: {isActive}, Current: {CurrentScore}");
```

## Best Practices

### 1. Mess Placement

**Good:**
```csharp
// Place in front of student
Vector3 position = student.transform.position + student.transform.forward * 0.5f;
position.y = 0.01f; // Slightly above ground
```

**Bad:**
```csharp
// Don't place at exact student position
Vector3 position = student.transform.position; // Student stands in mess!
```

### 2. Collider Sizing

**Good:**
```csharp
collider.radius = 0.5f; // Easy to target
```

**Bad:**
```csharp
collider.radius = 0.05f; // Too small, hard to click
collider.radius = 5.0f;  // Too large, blocks other interactions
```

### 3. Cleanup Timing

**Good:**
```csharp
cleanupTime = 0f;   // Instant - good for simple messes
cleanupTime = 2f;   // Short - good for medium messes
cleanupTime = 5f;   // Long - good for severe messes
```

**Bad:**
```csharp
cleanupTime = 30f;  // Too long, frustrating gameplay
```

### 4. Disruption Balance

**Good:**
```csharp
disruptionAmount = 12f;
cleanupDisruptionReduction = 15f; // Reward > penalty
```

**Bad:**
```csharp
disruptionAmount = 50f;
cleanupDisruptionReduction = 5f;  // Penalty > reward (no incentive)
```

### 5. Event Logging

**Always log both creation and cleanup:**
```csharp
// In Initialize()
StudentEventManager.Instance.LogEvent(creator, StudentEventType.MessCreated, ...);

// In CompleteCleanup()
StudentEventManager.Instance.LogEvent(creator, StudentEventType.MessCleaned, ...);
```

## Future Extensions

Potential areas for expansion:

- **Mess Variants**: Different vomit colors/sizes
- **Environmental Messes**: Broken windows, graffiti
- **Cleanup Tools**: Mop, bucket, cleaning spray
- **Partial Cleanup**: Multi-step cleaning process
- **Mess Combos**: Multiple messes increase disruption exponentially
- **Student Reactions**: Students avoid/react to nearby messes
- **Health System**: Messes can make other students sick
- **Janitor NPC**: AI character that cleans messes automatically

## Performance Considerations

### Efficient Design

- **No Update Loop**: Cleanup triggered by player input only
- **Automatic Cleanup**: Destroyed after completion
- **Minimal Components**: Only necessary collider and script
- **Event-Driven**: Uses existing event system

### Optimization Tips

1. **Limit Active Messes**: Destroy old messes after timeout
2. **Simple Colliders**: Use sphere/box, not mesh colliders
3. **Object Pooling**: Reuse mess objects instead of destroying
4. **LOD for Visuals**: Simplify distant mess visuals

## Summary

The Mess & Cleanup System provides:

✅ **Complete Framework** for mess objects
✅ **VomitMess Implementation** ready to use
✅ **Teacher Interaction** with contextual prompts
✅ **Disruption Integration** automatic tracking
✅ **Score Rewards** for cleanup actions
✅ **Event Logging** comprehensive tracking
✅ **Extensible Design** easy to add new mess types
✅ **Non-Breaking** all existing systems intact

The system is production-ready and fully integrated with your existing FunClass architecture!
