# Structured Log Strategy for FunClass Testing

## Overview

Implemented a unified, tiered logging system that provides clear visibility into game flow at different detail levels for testing and debugging.

---

## 3-Tier Log System

### Tier 1 - MILESTONE (Always On)
Critical game events that define the scenario flow. Always visible.

**Logged Events:**
- Game state changes (Boot → StudentIntro → InLevel → LevelComplete)
- Level loaded (level ID, goal settings, time limit)
- Scripted interaction triggered (source → target, event type, timestamp)
- Student state transitions (Calm → Distracted → ActingOut → Critical)
- Influence successfully applied (source → target, scope, strength)
- Influence blocked (distance exceeded, immunity, resistance too high)
- Teacher action performed (calm, escort, clean mess)
- Goal progress (disruption %, calm downs count, time remaining)
- Game over (win/lose reason, final score, stars)
- Periodic summary (30s intervals)

### Tier 2 - DETAIL (Per-Component Toggle)
Useful for debugging specific systems. Each component has its own toggle.

**StudentInteractionProcessor:**
- Interactions loaded (count, each interaction details)
- Time check evaluations, condition results, skip reasons
- One-time trigger already fired

**StudentInfluenceManager:**
- Distance calculations between source and target
- Susceptibility and resistance checks
- Fallback from SingleStudent to WholeClass scope
- Nearest student search results
- Influence strength calculation breakdown

**StudentAgent:**
- Behavior check results (state, chance, roll, decision)
- Idle timer scheduling (next behavior time)
- Object interaction selection
- Reaction triggered (type, duration)
- Config loading and initialization

**TeacherController:**
- Input values after dead zone filtering
- Movement and camera state
- Interaction target detection and selection
- Cursor visibility and lock state

**LevelManager:**
- Config loading steps
- Student placement
- Desk setup for interaction
- Progress updates (5s intervals)

**GameStateManager:**
- State transition notifications
- Listener count confirmation
- Intro screen creation

### Tier 3 - TRACE (Master Toggle)
Verbose per-frame data. Off by default, only enable when deep-diving.

- Every Update tick in StudentInteractionProcessor
- Every influence calculation detail
- Raw input values before dead zone
- Student state evaluation per frame
- Influence source tracking changes

---

## Unified Log Format

```
[ComponentName] ★ (elapsed) message   // MILESTONE - always visible
[ComponentName] ● (elapsed) message   // DETAIL - per-component toggle
[ComponentName] ~ (elapsed) message   // TRACE - master toggle
[ComponentName] ⚠ (elapsed) message  // WARNING
[ComponentName] ✗ (elapsed) message   // ERROR
```

**Example Output:**
```
[GameStateManager] ★ (0.0) STATE TRANSITION: Boot → StudentIntro
[GameStateManager] ★ (0.0) Notifying 5 listeners of state change
[LevelManager] ★ (0.1) Level started - level_first_day_summer_break
[LevelManager] ★ (0.1) Goal: 300s, disruption ≤ 50%, calmDowns: 3
[LevelManager] ★ (0.1) Loaded 4 student interactions
[StudentInteractionProcessor] ★ (0.1) Activated - Found 4 students, 4 interactions loaded
[StudentAgent] ★ (1.2) Bình initialized - state: Calm
[StudentAgent] ★ (1.3) Lan initialized - state: Calm
[StudentAgent] ★ (1.4) Nam initialized - state: Calm
[StudentAgent] ★ (1.5) Mai initialized - state: Calm
[StudentInteractionProcessor] ★ (20.0) Time-based trigger: student_binh → student_lan (WanderingAround)
[StudentInfluenceManager] ★ (20.1) WholeClass: Bình affected 3 students (skipped 0 in different location)
[StudentAgent] ★ (20.1) Bình: Calm → Distracted
[StudentAgent] ★ (25.3) Lan: Calm → Distracted
[StudentInteractionProcessor] ★ (40.0) Time-based trigger: student_binh → student_lan (KnockedOverObject)
[StudentInfluenceManager] ★ (40.1) Bình → Lan: influence applied (strength=0.56)
[StudentAgent] ★ (40.1) Lan: Distracted → ActingOut
[GameLogger] ★ (30.0) === SCENARIO SUMMARY ===
[GameLogger] ★ (30.0) Time: 270s remaining / 300s total
[GameLogger] ★ (30.0) Disruption: 35.1% (max: 50%)
[GameLogger] ★ (30.0) CalmDowns: 0 / 3
[GameLogger] ★ (30.0) Resolved: 0 / 0
[GameLogger] ★ (30.0) Bình: Distracted
[GameLogger] ★ (30.0) Lan: ActingOut [influenced by 1]
[GameLogger] ★ (30.0) Nam: Calm
[GameLogger] ★ (30.0) Mai: Calm
[GameLogger] ★ (30.0) === END SUMMARY (4 students, 1 influenced) ===
```

---

## Implementation Files

### Core Logging Infrastructure

**GameLogger.cs** - Static utility class
- Methods: `Milestone()`, `Detail()`, `Trace()`, `Warning()`, `Error()`
- `Initialize(GameLoggerConfig)` - Set config reference
- `SetDetailEnabled(string, bool)` - Toggle per-component detail
- `IsDetailEnabled(string)` - Check if detail logging is enabled
- `OutputPeriodicSummary()` - Trigger summary manually

**GameLoggerConfig.cs** - MonoBehaviour for Inspector control
- `Milestone Enabled` - Master toggle for milestones (always on)
- `Trace Enabled` - Master toggle for trace (off by default)
- Per-component Detail toggles:
  - StudentInteractionProcessor Detail
  - StudentInfluenceManager Detail
  - StudentAgent Detail
  - TeacherController Detail
  - LevelManager Detail
  - GameStateManager Detail
- `Summary Enabled` - Periodic summary feature
- `Summary Interval` - Summary frequency (default: 30s)

### Components Using GameLogger

| Component | File |
|-----------|------|
| StudentInteractionProcessor | Assets/Scripts/Core/ |
| StudentInfluenceManager | Assets/Scripts/Core/ |
| StudentAgent | Assets/Scripts/Core/ |
| TeacherController | Assets/Scripts/Core/ |
| LevelManager | Assets/Scripts/Core/ |
| GameStateManager | Assets/Scripts/Core/ |

---

## Scenario Timeline Verification

### First Day Summer Break Scenario

**Expected MILESTONE Flow:**
```
0.0s: [GameStateManager] STATE TRANSITION: Boot → StudentIntro
0.1s: [LevelManager] Level started - level_first_day_summer_break
0.1s: [LevelManager] Goal: 300s, disruption ≤ 50%, calmDowns: 3
0.1s: [LevelManager] Loaded 4 student interactions
0.1s: [StudentInteractionProcessor] Activated - Found 4 students, 4 interactions loaded

20.0s: [StudentInteractionProcessor] Time-based trigger: student_binh → student_lan (WanderingAround)
20.1s: [StudentInfluenceManager] WholeClass: Bình affected 3 students
20.1s: [StudentAgent] Bình: Calm → Distracted

40.0s: [StudentInteractionProcessor] Time-based trigger: student_binh → student_lan (KnockedOverObject)
40.1s: [StudentInfluenceManager] Bình → Lan: influence applied (strength=0.56)
40.1s: [StudentAgent] Lan: Calm → Distracted

60.0s: [StudentInteractionProcessor] Time-based trigger: student_lan → (WanderingAround)
60.1s: [StudentInfluenceManager] WholeClass: Lan affected 3 students
60.1s: [StudentAgent] Nam: Calm → Distracted

80.0s: [StudentInteractionProcessor] Time-based trigger: student_nam → student_lan (ThrowingObject)
80.1s: [StudentInfluenceManager] Nam → Lan: influence applied (strength=0.49)
80.1s: [StudentAgent] Lan: Distracted → ActingOut

Every 30s: [GameLogger] === SCENARIO SUMMARY ===
```

---

## Testing Workflow

### Phase 1: MILESTONE Only (Default)
1. Open scene, play game
2. Observe MILESTONE logs only
3. Verify scripted interactions trigger at correct times (20s, 40s, 60s, 80s)
4. Verify student state transitions match scenario design
5. Check periodic summary every 30s

### Phase 2: Enable DETAIL as Needed
**If scripted interactions don't trigger:**
- Enable StudentInteractionProcessor Detail
- Check why conditions fail

**If influence doesn't apply:**
- Enable StudentInfluenceManager Detail
- Check distance, susceptibility, resistance calculations

**If student behavior seems wrong:**
- Enable StudentAgent Detail
- Check roll results, idle timers, state evaluation

### Phase 3: Enable TRACE for Deep Dive
- Enable Trace Enabled
- Watch per-frame updates
- Trace raw input values
- Track influence source changes

---

## Configuration in Inspector

**GameLoggerConfig Component:**
```
┌─────────────────────────────────────────────────┐
│ GameLoggerConfig                                  │
├─────────────────────────────────────────────────┤
│ MILESTONE (Always On)                            │
│ [✓] Milestone Enabled                            │
├─────────────────────────────────────────────────┤
│ TRACE (Master Toggle)                            │
│ [ ] Trace Enabled                                │
├─────────────────────────────────────────────────┤
│ DETAIL Toggles                                   │
│ [ ] StudentInteractionProcessor Detail           │
│ [ ] StudentInfluenceManager Detail               │
│ [ ] StudentAgent Detail                          │
│ [ ] TeacherController Detail                     │
│ [ ] LevelManager Detail                          │
│ [ ] GameStateManager Detail                      │
├─────────────────────────────────────────────────┤
│ Periodic Summary                                 │
│ [✓] Summary Enabled                              │
│ Summary Interval: 30                             │
└─────────────────────────────────────────────────┘
```

---

## Key Design Decisions

1. **No StartRoute/StopRoute on StudentAgent** - Route management delegated to StudentMovementManager.Instance

2. **CurrentDisruption renamed to DisruptionLevel** - Consistent naming with ClassroomManager

3. **Periodic summary uses MILESTONE level** - Important for testing, should always be visible

4. **TeacherController input logging at TRACE level** - Raw values only useful for deep debugging

5. **Elapsed time from LevelManager.LevelTimeElapsed** - Consistent timestamp across all components

---

## Notes

- MILESTONE logs should fit on one screen for a full 5-minute scenario run
- DETAIL logs answer "why did this happen or not happen"
- TRACE logs are for per-frame deep dives, never leave on in normal testing
- Periodic summary is the most valuable testing tool - provides game health snapshot without scrolling
