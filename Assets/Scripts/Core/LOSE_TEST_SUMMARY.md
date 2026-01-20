# Lose Condition Test Summary
**Date:** 2026-01-19

## ✅ Changes Applied

### 1. Updated scenario_complex_example.json

**File:** [scenario_complex_example.json](../LevelTemplates/scenario_complex_example.json)

**Changes (Lines 43-46):**
```json
// BEFORE:
"disruptionTimeoutThreshold": 70,
"disruptionTimeoutSeconds": 45,
"disruptionTimeoutWarningSeconds": 15

// AFTER:
"disruptionTimeoutThreshold": 60,
"disruptionTimeoutSeconds": 30,
"disruptionTimeoutWarningSeconds": 10
```

**Impact:**
- Threshold lowered: 70 → 60 (easier to trigger)
- Timeout duration shortened: 45s → 30s (less time to recover)
- Warning earlier: 15s → 10s before timeout (at 20s remaining instead of 30s)

---

### 2. Enhanced ClassroomManager Logging

**File:** [ClassroomManager.cs](ClassroomManager.cs)

**Enhanced Timeout Start Log (Lines 380-388):**
```csharp
Debug.LogWarning($"[ClassroomManager] ⚠️ ⚠️ ⚠️ DISRUPTION TIMEOUT STARTED! ⚠️ ⚠️ ⚠️");
Debug.LogWarning($"[ClassroomManager] Disruption: {DisruptionLevel:F1}% ≥ Threshold: {threshold}%");
Debug.LogWarning($"[ClassroomManager] You have {timeoutDuration:F0} seconds to reduce disruption below {threshold}% or YOU WILL LOSE!");
Debug.LogWarning($"[ClassroomManager] Students outside: {OutsideStudentCount}");
```

**Enhanced Final Warning Log (Lines 395-399):**
```csharp
Debug.LogWarning($"[ClassroomManager] ⏰ ⏰ ⏰ FINAL WARNING! ⏰ ⏰ ⏰");
Debug.LogWarning($"[ClassroomManager] Only {remainingTime:F0} seconds left before LOSE!");
Debug.LogWarning($"[ClassroomManager] Current disruption: {DisruptionLevel:F1}% (must reduce below {threshold}%)");
```

**Enhanced Lose Log (Lines 430-442):**
```csharp
Debug.LogError($"[ClassroomManager] ═══════════════════════════════════════════════════");
Debug.LogError($"[ClassroomManager] ❌ ❌ ❌ GAME OVER - YOU LOSE! ❌ ❌ ❌");
Debug.LogError($"[ClassroomManager] ═══════════════════════════════════════════════════");
Debug.LogError($"[ClassroomManager] Reason: Disruption Timeout");
Debug.LogError($"[ClassroomManager] Disruption stayed above {threshold}% for {timeoutDuration} seconds!");
Debug.LogError($"[ClassroomManager] Final disruption: {DisruptionLevel:F1}%");
Debug.LogError($"[ClassroomManager] Students outside: {OutsideStudentCount}");
Debug.LogError($"[ClassroomManager] ═══════════════════════════════════════════════════");
```

---

## 📊 Expected Timeline

### Scenario: B & C Outside Forever (No Intervention)

**Assumptions:**
- Initial disruption from events (vomit, escalation, throw): ~48%
- 2 students outside
- Outside disruption rate: 0.5 per student per 5s = +1.0% per 5s

**Timeline:**

```
T=0s:    Game starts
         Student_A vomits → Disruption = 15%

T=10s:   B and C escalate (various events)
         Disruption = ~48%

T=20s:   B and C go outside
         OutsideStudentCount = 2

T=25s:   First outside check
         Disruption: 48 + 1.0 = 49%

T=30s:   Second check
         Disruption: 49 + 1.0 = 50%

T=35s:   Third check
         Disruption: 50 + 1.0 = 51%

... (continues +1.0% every 5 seconds) ...

T=80s:   Disruption reaches 60%

         🚨 TIMEOUT STARTS! 🚨

         Console logs:
         ⚠️ ⚠️ ⚠️ DISRUPTION TIMEOUT STARTED! ⚠️ ⚠️ ⚠️
         Disruption: 60.0% ≥ Threshold: 60.0%
         You have 30 seconds to reduce disruption below 60% or YOU WILL LOSE!
         Students outside: 2

T=100s:  Final warning (10s remaining)

         Console logs:
         ⏰ ⏰ ⏰ FINAL WARNING! ⏰ ⏰ ⏰
         Only 10 seconds left before LOSE!
         Current disruption: 64.0% (must reduce below 60%)

T=110s:  TIMEOUT EXPIRES

         💀 GAME OVER! 💀

         Console logs:
         ═══════════════════════════════════════════════════
         ❌ ❌ ❌ GAME OVER - YOU LOSE! ❌ ❌ ❌
         ═══════════════════════════════════════════════════
         Reason: Disruption Timeout
         Disruption stayed above 60% for 30 seconds!
         Final disruption: 68.0%
         Students outside: 2
         ═══════════════════════════════════════════════════
```

**Total Time to Lose:** ~110 seconds = **1 minute 50 seconds** ⏱️

---

## 🧪 Testing Instructions

### Test Case 1: AFK Test (Do Nothing)

**Steps:**
1. Start `scenario_complex_example` level
2. Do NOTHING - just watch
3. Observe console logs

**Expected Result:**
- Student_A vomits automatically
- B and C escalate and go outside
- Disruption increases every 5 seconds
- At ~80s: Timeout warning appears
- At ~100s: Final warning appears
- At ~110s: GAME OVER screen

**Console Output to Watch For:**
```
[ClassroomManager] 2 students are currently outside classroom
[ClassroomManager] Disruption increased by 1.0 to 60.0/100 (2 student(s) outside)
[ClassroomManager] ⚠️ ⚠️ ⚠️ DISRUPTION TIMEOUT STARTED! ⚠️ ⚠️ ⚠️
[ClassroomManager] ⏰ ⏰ ⏰ FINAL WARNING! ⏰ ⏰ ⏰
[ClassroomManager] ❌ ❌ ❌ GAME OVER - YOU LOSE! ❌ ❌ ❌
```

---

### Test Case 2: Too Slow Intervention

**Steps:**
1. Start level
2. Wait for B and C to go outside
3. Wait ~70 seconds (let timeout start)
4. Try to clean mess
5. Try to escort students

**Expected Result:**
- Timeout starts at ~80s
- Even if you start intervening, you only have 30s
- If disruption doesn't drop below 60% in time → LOSE
- Console shows warnings throughout

---

### Test Case 3: Successful Recovery

**Steps:**
1. Start level
2. When B and C go outside (~20s)
3. **Immediately clean mess** (-15 disruption)
4. **Calm Student_B** (-5 disruption)
5. **Escort B and C back** (-10 each = -20 disruption)

**Expected Result:**
- Disruption drops quickly
- Timeout may start but will cancel when disruption < 60%
- Console shows: "✅ Disruption Timeout Reset"
- Level continues (WIN possible)

---

## 📈 Disruption Breakdown

### Events and Their Impact:

| Event | Disruption Change |
|-------|------------------|
| **INCREASES** |
| MessCreated (vomit) | +15 |
| ThrowingObject | +8 |
| WanderingAround | +3 |
| MakingNoise | +5 |
| KnockingOverObject | +7 |
| Students outside (per 5s) | +0.5 per student |
| **DECREASES** |
| MessCleaned | -15 |
| StudentCalmed | -5 |
| StudentReturnedToSeat | -10 |
| StudentStoppedAction | -3 |

### Example Calculation:

**Starting disruption:**
```
Vomit:        +15
B escalates:  +3 (wander) +5 (noise) = +8
C escalates:  +3 (wander) +5 (noise) = +8
B throws:     +8
Total:        39
```

**If clean mess immediately:**
```
39 - 15 (clean) = 24
Still have buffer of 36 before timeout (60 - 24)
```

**If wait too long:**
```
39 + (12 per minute from outside × 4 minutes) = 39 + 48 = 87
Way above 60 threshold → Timeout triggers → LOSE
```

---

## 🎯 Gameplay Analysis

### Current Settings (After Changes):

**Pros:**
- ✅ Creates urgency (only 1m 50s buffer)
- ✅ Forces quick decision-making
- ✅ Clear feedback (multiple warnings)
- ✅ Recoverable if act fast
- ✅ Punishing if ignore

**Cons:**
- ⚠️ May be too harsh for beginners
- ⚠️ Little room for experimentation
- ⚠️ Requires understanding mechanics quickly

---

## 🔧 Optional Adjustments

### If Too Easy:
```json
"disruptionTimeoutThreshold": 50,    // Even lower
"disruptionTimeoutSeconds": 20        // Shorter timeout
```
**Result:** ~70 seconds to lose (very hard!)

### If Too Hard:
```json
"disruptionTimeoutThreshold": 70,    // Back to original
"disruptionTimeoutSeconds": 45       // Original duration
```
**Result:** ~155 seconds to lose (more forgiving)

### Alternative: Increase Outside Rate

**In ClassroomManager.cs (Line 21):**
```csharp
[SerializeField] private float outsideDisruptionRate = 1.0f;  // Was 0.5f
```
**Result:** Disruption increases faster (+2% per 5s with 2 students)
**Time to lose:** ~65 seconds (much more aggressive!)

---

## 📝 Related Systems

### Disruption Timeout Components:

1. **ClassroomManager.cs**
   - Tracks disruption level
   - Processes outside student penalties
   - Checks timeout conditions
   - Triggers warnings and lose

2. **LevelGoalConfig.cs**
   - Defines thresholds
   - Configures timeout duration
   - Sets warning timing

3. **scenario_complex_example.json**
   - Level-specific goal settings
   - Can override defaults

4. **GameStateManager.cs**
   - Handles state transition to LevelFailed
   - Shows lose screen

---

## ✅ Verification Checklist

After testing, verify:

- [ ] Timeout starts when disruption ≥ 60%
- [ ] First warning appears (at ~80s)
- [ ] Final warning appears (at ~100s, 10s before lose)
- [ ] Lose screen appears (at ~110s)
- [ ] Console shows clear logs with emojis
- [ ] Students outside count is logged
- [ ] Final disruption value is shown
- [ ] Game transitions to LevelFailed state
- [ ] Can restart level after lose

---

**Last Updated:** 2026-01-19
**Status:** ✅ Changes applied, ready for testing
**Expected Time to Lose:** ~110 seconds (1m 50s) if students stay outside
