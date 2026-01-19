# Future Ideas & Design Concepts

## Auto Re-escalate System (Future Implementation)

### Problem Statement
Currently, when a teacher calms a student who has unresolved influence sources (from mess objects or other students), the student stays calm permanently. This creates unrealistic gameplay where calming is a permanent solution even when the root cause (mess, disruptive student) still exists.

### Proposed Solution
Implement an auto re-escalate system where students who are calmed but still have unresolved influence sources will gradually escalate again over time.

---

## Design Concept

### Core Logic

**Students should re-escalate if:**
1. Student is currently in Calm state
2. Student has unresolved influence sources (mess objects or other students)
3. Enough time has passed since last calm

**Students should NOT re-escalate if:**
1. All influence sources are resolved (mess cleaned, source students calmed)
2. Student is already escalated (not Calm)
3. Student is being escorted or in a sequence

### Gameplay Loop

```
Student affected by mess/student
    ↓
Student escalates (Acting Out/Critical)
    ↓
Teacher calms student (-5 disruption)
    ↓
Student becomes Calm (temporary)
    ↓
[If sources unresolved]
    ↓
Wait X seconds
    ↓
Student re-escalates (Distracted → Acting Out)
    ↓
Loop continues until sources resolved
```

---

## Timing Options

### Option 1: Immediate After Calm (Recommended)

**Implementation:**
```csharp
public void DeescalateState()
{
    // ... de-escalate logic ...
    
    if (CurrentState == StudentState.Calm && 
        InfluenceSources.GetUnresolvedSourceCount() > 0)
    {
        float delay = 8f; // 8 seconds
        Invoke(nameof(CheckAndReEscalate), delay);
        Debug.Log($"Calmed but has unresolved sources - will re-escalate in {delay}s");
    }
}

private void CheckAndReEscalate()
{
    if (CurrentState == StudentState.Calm && 
        InfluenceSources.GetUnresolvedSourceCount() > 0)
    {
        EscalateState();
        // Schedule next check (loop)
        Invoke(nameof(CheckAndReEscalate), 8f);
    }
}
```

**Pros:**
- Immediate feedback to player
- Clear cause-and-effect
- Player learns quickly that calming is temporary

**Cons:**
- May feel too fast/punishing
- Requires careful delay tuning

**Recommended Delay:** 8-10 seconds

---

### Option 2: Periodic Check

**Implementation:**
```csharp
private float reEscalateCheckInterval = 15f;
private float lastReEscalateCheck = 0f;

void Update()
{
    if (Time.time - lastReEscalateCheck >= reEscalateCheckInterval)
    {
        lastReEscalateCheck = Time.time;
        
        if (CurrentState == StudentState.Calm && 
            InfluenceSources.GetUnresolvedSourceCount() > 0)
        {
            EscalateState();
        }
    }
}
```

**Pros:**
- Simple implementation
- Predictable timing
- Less aggressive

**Cons:**
- Less responsive
- May miss immediate feedback

**Recommended Interval:** 10-15 seconds

---

### Option 3: Random Chance

**Implementation:**
```csharp
void Update()
{
    if (CurrentState == StudentState.Calm && 
        InfluenceSources.GetUnresolvedSourceCount() > 0)
    {
        // Small chance per frame (~1% per second at 60fps)
        if (Random.value < 0.0001f)
        {
            EscalateState();
        }
    }
}
```

**Pros:**
- Unpredictable, realistic
- Varied gameplay

**Cons:**
- Inconsistent player experience
- Hard to balance
- May feel unfair

**Not Recommended** - Too unpredictable for teaching gameplay

---

## Example Scenarios

### Scenario 1: Mess Object Influence

```
T+0s:  Student A vomits → Mess created
T+0s:  Student B affected by mess → B escalates to Acting Out
T+10s: Teacher calms B → B becomes Calm (-5 disruption)
T+10s: System checks: Mess still exists (unresolved source)
T+10s: Schedule re-escalate in 8s
T+18s: B re-escalates to Distracted
T+18s: Schedule re-escalate in 8s
T+26s: B re-escalates to Acting Out
T+30s: Teacher cleans mess → Source resolved
T+30s: B stays Calm (no more re-escalate)
```

**Player learns:** Must clean mess, not just calm students

---

### Scenario 2: Student-to-Student Influence

```
T+0s:  Student A hits Student C → C has influence source from A
T+0s:  C escalates to Critical
T+10s: Teacher calms C → C becomes Calm
T+10s: System checks: A still Acting Out (unresolved source)
T+10s: Schedule re-escalate in 8s
T+15s: Teacher calms A → A's influence on C resolved
T+18s: Re-escalate check: C has 0 unresolved sources
T+18s: C stays Calm (no re-escalate)
```

**Player learns:** Must calm source students, not just victims

---

### Scenario 3: Multiple Sources

```
T+0s:  Student C affected by:
       - Student A (vomit mess)
       - Student B (hit C)
T+10s: Teacher calms C → C becomes Calm
T+10s: System checks: 2 unresolved sources
T+18s: C re-escalates to Distracted
T+20s: Teacher calms A → 1 source resolved (A's influence)
T+26s: C re-escalates to Acting Out (still has B source)
T+30s: Teacher calms B → 0 sources unresolved
T+38s: Re-escalate check: No unresolved sources
T+38s: C stays Calm permanently
```

**Player learns:** Must resolve ALL sources

---

## Integration with Existing Systems

### StudentInfluenceManager
- Already tracks unresolved sources
- Use `InfluenceSources.GetUnresolvedSourceCount()`
- Use `InfluenceSources.AreAllSourcesResolved()`

### ClassroomManager
- Re-escalation should NOT add disruption
- Only initial escalation from influence adds disruption
- Prevents disruption spam

### GUI System (Future)
- Show warning: "⚠️ Calm chỉ tạm thời" when student has unresolved sources
- Visual indicator on student: pulsing outline if will re-escalate
- Tooltip: "X unresolved sources - will re-escalate"

---

## Balancing Considerations

### Delay Timing
- **Too Short (< 5s):** Feels punishing, player can't react
- **Too Long (> 15s):** Player doesn't learn, feels disconnected
- **Recommended:** 8-10 seconds

### Escalation Rate
- **One step at a time:** Calm → Distracted → Acting Out → Critical
- Gives player time to respond
- More forgiving than instant Critical

### Disruption Impact
- **Initial influence:** +disruption
- **Re-escalation:** No additional disruption (already counted)
- **Calm action:** -5 disruption (immediate benefit)
- Net effect: Calming still reduces disruption temporarily

---

## Implementation Checklist

When implementing this system:

- [ ] Add re-escalate timer to StudentAgent
- [ ] Implement CheckAndReEscalate() method
- [ ] Hook into DeescalateState() to schedule checks
- [ ] Add debug logging for re-escalate events
- [ ] Test with mess objects
- [ ] Test with student-to-student influence
- [ ] Test with multiple sources
- [ ] Balance delay timing
- [ ] Add visual indicators (optional)
- [ ] Update GUI to show warnings
- [ ] Add to StudentEventManager for tracking
- [ ] Update documentation

---

## Alternative Approaches (Considered but Not Recommended)

### Approach 1: Proximity-Based
Check if student is near mess object (distance-based).

**Rejected because:**
- More complex (distance calculations)
- Doesn't work for student-to-student influence
- Unrealistic (smell/noise travels)

### Approach 2: Instant Re-escalate
Student re-escalates immediately if sources unresolved.

**Rejected because:**
- No benefit to calming
- Too punishing
- Removes strategic choice

### Approach 3: Permanent Calm
Current system - student stays calm forever.

**Rejected because:**
- Unrealistic
- No incentive to resolve root causes
- Simplifies gameplay too much

---

## Future Enhancements

### Phase 1: Basic Implementation
- Simple timer-based re-escalate
- Works with existing influence system
- Debug logging

### Phase 2: Visual Feedback
- Student visual indicator (pulsing, color)
- GUI warnings
- Tooltip showing unresolved sources

### Phase 3: Advanced Mechanics
- Different re-escalate rates based on source type
- Personality traits affect re-escalate speed
- Teacher presence reduces re-escalate chance
- Classroom state affects re-escalate timing

### Phase 4: Player Feedback
- Tutorial explaining system
- Achievement: "Resolved all sources"
- Score bonus for preventing re-escalation

---

## Testing Scenarios

### Test Case 1: Single Mess Source
1. Create vomit mess
2. Student affected → escalates
3. Calm student
4. Wait 8s → student should re-escalate
5. Clean mess
6. Student should stay calm

### Test Case 2: Single Student Source
1. Student A hits Student B
2. B escalates
3. Calm B
4. Wait 8s → B should re-escalate
5. Calm A
6. B should stay calm

### Test Case 3: Multiple Sources
1. Create mess + Student A hits Student B
2. B escalates
3. Calm B
4. Clean mess only
5. B should still re-escalate (A unresolved)
6. Calm A
7. B should stay calm

### Test Case 4: Edge Cases
- Student escorted while re-escalate scheduled → cancel timer
- Student in sequence → don't re-escalate
- Source resolved during delay → cancel re-escalate
- Multiple calms in quick succession → reset timer

---

## Notes

- This system creates a **gameplay loop** where calming is a temporary solution
- Forces player to **address root causes** (clean mess, calm source students)
- Adds **strategic depth** - prioritize which students to calm first
- Maintains **game balance** - calming still reduces disruption immediately
- Provides **clear feedback** - player learns through experience

**Status:** Design complete, ready for implementation when needed

**Priority:** Medium - Enhances gameplay but not critical for MVP

**Estimated Effort:** 2-3 hours implementation + 1 hour testing

---

## Punish Action - High Risk/High Reward (Future Implementation)

### Problem Statement
Some students with high impulsiveness (ngổ ngáo) frequently harass other students. Sometimes, normal resolve actions (Calm) don't work on these students due to their personality traits. Teachers need a more aggressive intervention option, but it comes with serious risks.

### Proposed Solution
Implement a "Punish" action that is a last-resort, high-risk/high-reward option for dealing with problematic students.

---

## Design Concept

### When to Use Punish

**Trigger Conditions:**
1. Student has high impulsiveness trait (ngổ ngáo cao)
2. Student is harassing other students (hitting, throwing objects)
3. Normal Calm action has failed (random chance based on impulsiveness)
4. Teacher is desperate to restore order

**Warning to Player:**
- "⚠️ Punish is a risky action - 70% chance of being caught for school violence!"
- "Use only as last resort when Calm doesn't work"
- Confirmation dialog before executing

---

## Mechanics

### Action Execution

```csharp
public void PunishStudent(StudentAgent student)
{
    Debug.Log($"[Teacher] PUNISHING {student.Config?.studentName} - HIGH RISK ACTION");
    
    // Roll for detection
    float detectionRoll = Random.value;
    float detectionChance = 0.7f; // 70% chance of being caught
    
    if (detectionRoll < detectionChance)
    {
        // FAILURE - Caught for school violence
        OnPunishmentDetected(student);
    }
    else
    {
        // SUCCESS - Immediate classroom order restored
        OnPunishmentSuccess(student);
    }
}
```

### Failure Outcome (70% Chance)

**Detected for School Violence:**
```csharp
private void OnPunishmentDetected(StudentAgent student)
{
    Debug.LogError($"[Teacher] ✗ PUNISH DETECTED - School violence reported!");
    
    // Show dramatic failure screen
    ShowPunishmentDetectedScreen();
    
    // Instant level loss
    GameManager.Instance.LoseLevel("School Violence Detected");
    
    // Log event
    StudentEventManager.Instance.LogEvent(
        student,
        StudentEventType.TeacherViolence,
        $"Teacher punished {student.Config?.studentName} - caught by authorities",
        null
    );
    
    // Game over
    SceneManager.LoadScene("LevelFailedScene");
}
```

**Effects:**
- ❌ Instant level loss
- ❌ Game over screen: "Bạo lực học đường bị phát hiện"
- ❌ No chance to continue
- ❌ Reputation penalty (if meta-progression exists)

---

### Success Outcome (30% Chance)

**Punishment Successful:**
```csharp
private void OnPunishmentSuccess(StudentAgent student)
{
    Debug.Log($"[Teacher] ✓ PUNISH SUCCESS - Classroom order restored!");
    
    // Show dramatic success effect
    ShowPunishmentSuccessEffect();
    
    // 1. Reset ALL disruption to 0
    ClassroomManager.Instance.SetDisruption(0f);
    
    // 2. Calm ALL students
    var allStudents = FindObjectsOfType<StudentAgent>();
    foreach (var s in allStudents)
    {
        while (s.CurrentState != StudentState.Calm)
        {
            s.DeescalateState();
        }
    }
    
    // 3. Return ALL outside students to seats (no escort needed)
    foreach (var s in allStudents)
    {
        if (IsStudentOutside(s))
        {
            s.ForceReturnToSeat(); // Instant teleport to seat
        }
    }
    
    // 4. Resolve ALL influence sources
    StudentInfluenceManager.Instance.ResolveAllSources();
    
    // Log event
    StudentEventManager.Instance.LogEvent(
        student,
        StudentEventType.TeacherPunishment,
        $"Teacher punished {student.Config?.studentName} - order restored",
        null
    );
    
    Debug.Log("[Teacher] ✓ All students calmed, all disruption cleared!");
}
```

**Effects:**
- ✅ Disruption → 0 (instant)
- ✅ All students → Calm state
- ✅ All outside students → Return to seats automatically
- ✅ All influence sources → Resolved
- ✅ Classroom completely reset to order

---

## Calm Action Failure Mechanic

### When Calm Doesn't Work

**Implementation:**
```csharp
private void CalmStudent(StudentAgent student)
{
    // Check if student is highly impulsive
    float impulsiveness = student.Config.impulsiveness;
    
    if (impulsiveness >= 0.8f) // High impulsiveness threshold
    {
        // Random chance of Calm failing
        float failureChance = (impulsiveness - 0.5f) * 0.4f; // 0.8 → 12%, 1.0 → 20%
        
        if (Random.value < failureChance)
        {
            // Calm FAILED
            Debug.LogWarning($"[Teacher] ✗ Calm failed on {student.Config?.studentName} - too impulsive!");
            
            // Show failure feedback
            ShowCalmFailedFeedback(student);
            
            // Student may even escalate further
            if (Random.value < 0.5f)
            {
                student.EscalateState();
                Debug.Log($"[Teacher] {student.Config?.studentName} became MORE agitated!");
            }
            
            // Suggest Punish as option (risky)
            ShowPunishSuggestion(student);
            
            return; // Exit without calming
        }
    }
    
    // Normal calm logic if not failed
    // ... existing calm code ...
}
```

**Failure Indicators:**
- Student shakes head
- Red X animation
- Sound effect: "Không nghe lời!"
- GUI shows: "⚠️ Calm failed - student too impulsive"
- Hint: "Consider Punish (risky) or resolve influence sources"

---

## GUI Integration

### Popup with Punish Option

**When Calm fails on impulsive student:**

```
┌─────────────────────────────────────────┐
│  [Student A] - Critical 😡               │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│                                          │
│  💬 "Em không nghe cô đâu!"              │
│                                          │
│  ⚠️ Học sinh rất ngổ ngáo                │
│  ⚠️ Calm đã thất bại                     │
│                                          │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│  [💙 Calm] (may fail again)              │
│  [⚡ Punish] ⚠️ RISKY                    │
│  [❌ Close]                              │
│                                          │
│  ⚠️ Punish: 70% bị phát hiện = thua      │
│  ✅ Punish: 30% thành công = reset all   │
└─────────────────────────────────────────┘
```

### Confirmation Dialog

**When player clicks Punish:**

```
┌─────────────────────────────────────────┐
│  ⚠️ XÁC NHẬN HÀNH ĐỘNG NGUY HIỂM ⚠️     │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│                                          │
│  Bạn chắc chắn muốn PUNISH học sinh?     │
│                                          │
│  📊 Tỷ lệ:                               │
│  ❌ 70% - Bị phát hiện bạo lực           │
│           → THUA LEVEL NGAY              │
│                                          │
│  ✅ 30% - Thành công                     │
│           → Disruption về 0              │
│           → Tất cả học sinh về chỗ       │
│           → Lớp học trật tự hoàn toàn    │
│                                          │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│  [⚡ Chấp nhận rủi ro] [❌ Hủy bỏ]       │
└─────────────────────────────────────────┘
```

---

## Balancing Considerations

### Risk/Reward Balance

**Current Design:**
- 70% failure (instant loss) vs 30% success (complete reset)
- High risk, high reward

**Alternative Balances:**
- **More Forgiving:** 50/50 split
- **More Punishing:** 80/20 split
- **Dynamic:** Based on student impulsiveness (higher impulsiveness = higher success chance)

### When Players Use Punish

**Desperate Situations:**
- Multiple students outside
- High disruption (>80)
- Time running out
- Calm keeps failing
- Last resort before losing anyway

**Strategic Use:**
- Early game gamble (high risk for easy win)
- Speedrun strategy (30% chance of instant win)
- Challenge mode (self-imposed handicap)

---

## Example Scenarios

### Scenario 1: Punish Success (30%)

```
T+0s:  Student A (impulsiveness: 0.9) hits Student B
T+0s:  B escalates to Critical, disruption +15
T+10s: Teacher calms A → FAILED (too impulsive)
T+10s: A escalates further to Critical
T+15s: Teacher calms B → B calm temporarily
T+20s: B re-escalates (A still unresolved)
T+25s: Disruption at 85/100
T+30s: Teacher decides to PUNISH A (desperate)
T+30s: Roll: 0.25 (< 0.30) → SUCCESS! ✓
T+30s: Disruption → 0
T+30s: All students → Calm
T+30s: All outside students → Return to seats
T+30s: Level continues with clean slate
```

**Player reaction:** "Phew! Lucky!"

---

### Scenario 2: Punish Failure (70%)

```
T+0s:  Student A (impulsiveness: 0.9) hits Student B
T+0s:  B escalates to Critical, disruption +15
T+10s: Teacher calms A → FAILED (too impulsive)
T+15s: Teacher decides to PUNISH A (risky)
T+15s: Confirmation dialog → Player accepts risk
T+15s: Roll: 0.45 (< 0.70) → DETECTED! ✗
T+15s: Screen flash red
T+15s: "BẠO LỰC HỌC ĐƯỜNG BỊ PHÁT HIỆN"
T+15s: Game over screen
T+15s: Level failed
```

**Player reaction:** "I shouldn't have risked it..."

---

### Scenario 3: Multiple Calm Failures

```
T+0s:  Student A (impulsiveness: 0.95) acting out
T+10s: Teacher calms A → FAILED (15% chance, rolled 0.12)
T+15s: Teacher calms A again → FAILED (rolled 0.08)
T+20s: Teacher calms A again → SUCCESS! (rolled 0.20)
T+20s: A calmed without using Punish
T+20s: Player avoids risk, plays safe
```

**Player learns:** Persistence can work, Punish is last resort

---

## Implementation Checklist

When implementing Punish action:

- [ ] Add Punish button to GUI (with warning icon)
- [ ] Implement confirmation dialog with risk display
- [ ] Add PunishStudent() method to TeacherController
- [ ] Implement detection roll (70/30 split)
- [ ] Create OnPunishmentDetected() - instant loss
- [ ] Create OnPunishmentSuccess() - reset all
- [ ] Add Calm failure mechanic based on impulsiveness
- [ ] Add visual/audio feedback for both outcomes
- [ ] Create game over screen for detection
- [ ] Create success effect animation
- [ ] Add StudentEventType.TeacherViolence
- [ ] Add StudentEventType.TeacherPunishment
- [ ] Test with high impulsiveness students
- [ ] Balance detection chance
- [ ] Add tutorial/warning for new players
- [ ] Add achievement: "Desperate Measures" (use Punish)
- [ ] Add achievement: "Lucky Teacher" (Punish success)

---

## Alternative Approaches

### Approach 1: Graduated Punishment
Multiple punishment levels with different risks.

**Rejected because:**
- Too complex for players to understand
- Dilutes the high-stakes decision

### Approach 2: Guaranteed Success with Penalty
Punish always works but adds permanent penalty.

**Rejected because:**
- Removes risk/reward tension
- Less dramatic

### Approach 3: Cooldown-Based
Punish has cooldown, can be used multiple times.

**Rejected because:**
- Becomes standard strategy
- Loses "desperate last resort" feel

---

## Future Enhancements

### Phase 1: Basic Implementation
- Simple 70/30 roll
- Instant loss or instant win
- Confirmation dialog

### Phase 2: Visual Polish
- Dramatic camera shake
- Slow-motion effect during roll
- Particle effects for success/failure
- Sound design (tense music)

### Phase 3: Dynamic Difficulty
- Detection chance varies by level
- Student personality affects success rate
- Classroom state affects detection
- Witness system (more students = higher detection)

### Phase 4: Meta-Progression
- Reputation system affected by Punish use
- Unlock "Gentle Teacher" achievement (never use Punish)
- Unlock "Risk Taker" achievement (use Punish 10 times)
- Statistics tracking (Punish success rate)

---

## Design Philosophy

**Why This Mechanic Exists:**

1. **Tension:** Creates high-stakes moments
2. **Desperation:** Gives players an out when losing
3. **Risk/Reward:** Tests player decision-making
4. **Realism:** Reflects real-world consequences of inappropriate discipline
5. **Gameplay Variety:** Adds strategic depth
6. **Memorable Moments:** Win-or-lose moments players remember

**Why 70/30 Split:**

- 70% failure ensures it's truly risky
- 30% success makes it tempting when desperate
- Not 50/50 to avoid "fair gamble" feeling
- Should feel like "last resort," not "viable strategy"

---

## Notes

- This is a **controversial mechanic** - handle with care
- Consider cultural sensitivity (school violence is serious topic)
- May need content warning or option to disable
- Alternative: Rename to "Strict Discipline" or "Harsh Warning"
- Consider adding "Calm+ " action as safer alternative (costs resource)

**Status:** Design complete, needs careful implementation and testing

**Priority:** Low - Controversial, not essential for core gameplay

**Estimated Effort:** 4-5 hours implementation + 2 hours testing + ethical review

**Recommendation:** Implement only after core systems stable and with player feedback
