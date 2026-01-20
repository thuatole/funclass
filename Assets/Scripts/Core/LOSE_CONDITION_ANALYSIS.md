# Lose Condition Analysis - scenario_complex_example.json
**Date:** 2026-01-19

## 📊 Current Config Analysis

### Scenario: B và C Outside Mãi Mãi

**File:** [scenario_complex_example.json](../LevelTemplates/scenario_complex_example.json)

---

## 🎯 Goal Settings (Lines 36-51)

```json
"goalSettings": {
    "maxDisruptionThreshold": 80,               // Warning threshold
    "catastrophicDisruptionLevel": 95,          // Instant lose
    "timeLimitSeconds": 300,                    // 5 minutes total

    "enableDisruptionTimeout": true,            // Timeout enabled ✓
    "disruptionTimeoutThreshold": 70,           // If disruption ≥ 70
    "disruptionTimeoutSeconds": 45,             // For 45 seconds
    "disruptionTimeoutWarningSeconds": 15       // Warning at 30s remaining
}
```

---

## ⏱️ Time to Lose Calculation

### Current ClassroomManager Settings:

```csharp
outsideDisruptionRate = 0.5f;           // Per student per check
outsideCheckInterval = 5f;               // Every 5 seconds
maxOutsideDisruptionPenalty = 30f;       // Cap at 30
```

### Scenario: 2 Students (B + C) Outside

**Disruption Rate:**
```
Per check: 2 students × 0.5 = 1.0 disruption
Interval: Every 5 seconds
Rate: +1.0 disruption per 5 seconds = +12 disruption per minute
```

---

## 📈 Disruption Timeline

### Path 1: Reach Catastrophic Level (95)

```
T=0s:     Disruption = 0
          B and C go outside

T=5s:     +1.0 → Disruption = 1.0
T=10s:    +1.0 → Disruption = 2.0
T=15s:    +1.0 → Disruption = 3.0
...

At rate of +12 per minute:
Time to reach 95 = 95 / 12 = 7.92 minutes = 475 seconds

BUT: maxOutsideDisruptionPenalty = 30
So outside students can only add 30 total!

After 30 disruption from outside:
30 / 1.0 per check = 30 checks
30 checks × 5s = 150 seconds = 2.5 minutes

Then stops adding disruption from outside!
```

**Result:** Will NEVER reach 95 from outside students alone! ❌

---

### Path 2: Disruption Timeout (WILL TRIGGER!)

```json
"enableDisruptionTimeout": true,
"disruptionTimeoutThreshold": 70,
"disruptionTimeoutSeconds": 45
```

**Condition:** If disruption ≥ 70 for 45 consecutive seconds → Lose!

**But:** Outside students can only add max 30 disruption!

**Problem:** Starting disruption is 0, so we need OTHER events to reach 70!

---

## 🧮 Realistic Scenario Analysis

### Initial Events (Before B & C Go Outside):

1. **Student_A Vomits (MessCreated)**
   ```
   Disruption: 0 → +15 → 15
   ```

2. **Student_B and C Escalate (Various Events)**
   - WanderingAround: +3 each = +6
   - MakingNoise: +5 each = +10
   - Total from escalation: ~20-30

3. **Student_B Throws at Student_C (ThrowingObject)**
   ```
   Disruption: +8
   ```

**Total disruption before outside:**
```
Vomit (15) + Escalation (~25) + Throw (8) = 48 disruption
```

### After B & C Go Outside:

```
T=0s:     Initial disruption = 48
          B and C outside

T=5s:     +1.0 → 49
T=10s:    +1.0 → 50
...
T=110s:   +1.0 → 70 (THRESHOLD REACHED!)
          Timeout timer starts!

T=155s:   Timeout timer = 45s → LOSE!
```

**Time to reach threshold:** (70 - 48) / 1.0 per check = 22 checks × 5s = **110 seconds = 1m 50s**

**Time for timeout:** **45 seconds**

**Total time to lose:** **110 + 45 = 155 seconds = 2m 35s** ⏰

---

## ❌ Problem: TOO LONG!

**Current:** ~2.5 minutes để thua nếu B & C outside mãi

**Issues:**
1. Too forgiving - teacher has too much time
2. Not urgent enough
3. Players won't feel pressure

---

## ✅ Recommended Changes

### Option 1: Increase Outside Disruption Rate (Easiest)

**Change in ClassroomManager.cs:**

```csharp
// BEFORE:
private float outsideDisruptionRate = 0.5f;  // +1.0 per 5s with 2 students

// AFTER:
private float outsideDisruptionRate = 1.5f;  // +3.0 per 5s with 2 students
```

**New Timeline:**
```
T=0s:   Disruption = 48
T=5s:   +3.0 → 51
T=10s:  +3.0 → 54
T=15s:  +3.0 → 57
T=20s:  +3.0 → 60
T=25s:  +3.0 → 63
T=30s:  +3.0 → 66
T=35s:  +3.0 → 69
T=40s:  +3.0 → 72 (THRESHOLD!)
Timeout starts...
T=85s:  Timeout = 45s → LOSE!
```

**Total time to lose:** **85 seconds = 1m 25s** ✅

---

### Option 2: Reduce Timeout Duration (More Aggressive)

**Change in scenario_complex_example.json:**

```json
// BEFORE:
"disruptionTimeoutThreshold": 70,
"disruptionTimeoutSeconds": 45,

// AFTER:
"disruptionTimeoutThreshold": 60,     // Lower threshold
"disruptionTimeoutSeconds": 30,        // Shorter timeout
```

**New Timeline (with original rate 0.5):**
```
T=0s:    Disruption = 48
T=60s:   +12.0 → 60 (THRESHOLD!)
         Timeout starts...
T=90s:   Timeout = 30s → LOSE!
```

**Total time to lose:** **90 seconds = 1m 30s** ✅

---

### Option 3: Both Changes (Most Aggressive)

**Combine:**
- `outsideDisruptionRate = 1.5f` (in code)
- `disruptionTimeoutThreshold = 60` (in JSON)
- `disruptionTimeoutSeconds = 30` (in JSON)

**New Timeline:**
```
T=0s:   Disruption = 48
T=5s:   +3.0 → 51
T=10s:  +3.0 → 54
T=15s:  +3.0 → 57
T=20s:  +3.0 → 60 (THRESHOLD!)
        Timeout starts...
T=50s:  Timeout = 30s → LOSE!
```

**Total time to lose:** **50 seconds** ⚡ (Very aggressive!)

---

## 🎮 Recommended Settings for Good Gameplay

### Balanced Approach (Option 2):

**scenario_complex_example.json:**
```json
"goalSettings": {
    "disruptionTimeoutThreshold": 60,     // Easier to reach
    "disruptionTimeoutSeconds": 30,        // 30s to fix (not 45s)
    "disruptionTimeoutWarningSeconds": 10  // Warning at 20s remaining
}
```

**Result:** **~90 seconds** to lose if students outside

**Why this is good:**
- ✅ Not too punishing (gives ~1.5 minutes)
- ✅ Creates urgency (must act within 30s after threshold)
- ✅ Clear warning system (10s before lose)
- ✅ No code changes needed (just config)

---

## 📝 Implementation: Add Lose Warning Log

**File:** [ClassroomManager.cs](ClassroomManager.cs)

**Current Implementation (Lines 367-410):**

```csharp
private void CheckDisruptionTimeout()
{
    if (levelConfig == null || levelConfig.levelGoal == null) return;
    if (!levelConfig.levelGoal.enableDisruptionTimeout) return;

    float threshold = levelConfig.levelGoal.disruptionTimeoutThreshold;
    float timeoutDuration = levelConfig.levelGoal.disruptionTimeoutSeconds;
    float warningTime = levelConfig.levelGoal.disruptionTimeoutWarningSeconds;

    // Check if disruption is above threshold
    if (DisruptionLevel >= threshold)
    {
        // Start timeout if not already active
        if (!isDisruptionTimeoutActive)
        {
            isDisruptionTimeoutActive = true;
            disruptionTimeoutStartTime = Time.time;
            hasShownTimeoutWarning = false;

            Debug.LogWarning($"[ClassroomManager] ⚠️ DISRUPTION TIMEOUT STARTED! Disruption: {DisruptionLevel:F1} ≥ {threshold:F1}");
            Debug.LogWarning($"[ClassroomManager] You have {timeoutDuration:F0} seconds to reduce disruption below {threshold:F1} or you will LOSE!");
        }

        // Calculate remaining time
        float elapsedTime = Time.time - disruptionTimeoutStartTime;
        float remainingTime = timeoutDuration - elapsedTime;

        // Show warning
        if (!hasShownTimeoutWarning && remainingTime <= warningTime)
        {
            hasShownTimeoutWarning = true;
            OnDisruptionTimeoutWarning?.Invoke(remainingTime);

            Debug.LogWarning($"[ClassroomManager] ⏰ WARNING! Only {remainingTime:F0} seconds left before timeout!");
        }

        // Check if timeout expired
        if (elapsedTime >= timeoutDuration)
        {
            Debug.LogError($"[ClassroomManager] ❌ DISRUPTION TIMEOUT! Disruption stayed above {threshold:F1} for {timeoutDuration:F0}s!");
            Debug.LogError($"[ClassroomManager] 💀 YOU LOSE!");
            OnDisruptionTimeoutLose?.Invoke();
        }
    }
    else
    {
        // Reset timeout if disruption drops below threshold
        if (isDisruptionTimeoutActive)
        {
            Debug.Log($"[ClassroomManager] ✓ Disruption dropped below {threshold:F1}, timeout cancelled");
            isDisruptionTimeoutActive = false;
            hasShownTimeoutWarning = false;
        }
    }
}
```

**Already has good logging! ✓**

---

## 🎯 Additional Enhancement: Count-Based Lose

**Currently missing:** Direct lose condition for students outside too long

**Add to LevelManager.cs:**

```csharp
private void CheckLoseConditions()
{
    // ... existing checks ...

    // Check if too many students outside
    if (currentGoal.catastrophicOutsideStudents > 0)
    {
        int outsideCount = ClassroomManager.Instance.OutsideStudentCount;

        if (outsideCount >= currentGoal.catastrophicOutsideStudents)
        {
            Debug.LogError($"[LevelManager] ❌ TOO MANY STUDENTS OUTSIDE! {outsideCount} ≥ {currentGoal.catastrophicOutsideStudents}");
            Debug.LogError($"[LevelManager] 💀 YOU LOSE!");
            LoseLevel($"{outsideCount} students outside (max: {currentGoal.catastrophicOutsideStudents - 1})");
            return;
        }
    }

    // Check disruption timeout (already exists in ClassroomManager)
    // Just listen to event
}

void OnEnable()
{
    // ... existing subscriptions ...

    if (ClassroomManager.Instance != null)
    {
        ClassroomManager.Instance.OnDisruptionTimeoutLose += HandleDisruptionTimeoutLose;
    }
}

private void HandleDisruptionTimeoutLose()
{
    Debug.LogError($"[LevelManager] ❌ DISRUPTION TIMEOUT LOSE!");
    Debug.LogError($"[LevelManager] Disruption: {ClassroomManager.Instance.DisruptionLevel:F1}");
    LoseLevel("Disruption stayed too high for too long");
}
```

---

## 📋 Final Recommendations

### Immediate Changes (JSON Only):

**File:** `scenario_complex_example.json`

```json
"goalSettings": {
    "maxDisruptionThreshold": 80,
    "catastrophicDisruptionLevel": 95,
    "timeLimitSeconds": 300,

    "enableDisruptionTimeout": true,
    "disruptionTimeoutThreshold": 60,      // Changed from 70
    "disruptionTimeoutSeconds": 30,         // Changed from 45
    "disruptionTimeoutWarningSeconds": 10   // Changed from 15
}
```

**Result:**
- Time to lose: **~90 seconds** if B & C outside
- Clear warnings at 30s, 20s, 10s remaining
- No code changes needed

---

### Optional Code Enhancement:

**File:** `ClassroomManager.cs` (Line 21)

```csharp
// Change rate for more urgency:
[SerializeField] private float outsideDisruptionRate = 1.0f;  // Was 0.5f
```

**Result:** Time to lose: **~60 seconds** (even more urgent)

---

## 🧪 Testing Instructions

1. **Start scenario_complex_example**
2. **Let A vomit**
3. **Let B & C go outside**
4. **DON'T intervene** - just watch console

**Expected Logs:**
```
[ClassroomManager] Student_B is now outside classroom (Total outside: 1)
[ClassroomManager] Student_C is now outside classroom (Total outside: 2)

[ClassroomManager] 2 students are currently outside classroom
[ClassroomManager] Disruption increased by 1.0 to 49.0/100 (2 student(s) outside)

... (continues every 5 seconds) ...

[ClassroomManager] Disruption increased by 1.0 to 60.0/100 (2 student(s) outside)
[ClassroomManager] ⚠️ DISRUPTION TIMEOUT STARTED! Disruption: 60.0 ≥ 60.0
[ClassroomManager] You have 30 seconds to reduce disruption below 60.0 or you will LOSE!

... (20 seconds later) ...

[ClassroomManager] ⏰ WARNING! Only 10 seconds left before timeout!

... (10 seconds later) ...

[ClassroomManager] ❌ DISRUPTION TIMEOUT! Disruption stayed above 60.0 for 30s!
[ClassroomManager] 💀 YOU LOSE!
```

---

**Last Updated:** 2026-01-19
**Status:** 🎯 Ready to implement - just change JSON config!
