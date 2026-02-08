# Plan: Structured Log Strategy for FunClass Testing

## Problem
Current logging is ad-hoc across components - each script logs differently, making it hard to trace game flow, identify bugs, and verify scenario timelines during testing.

## Goal
Implement a unified, tiered logging system that makes testing efficient by providing clear visibility into game flow at different detail levels.

---

## Design: 3-Tier Log System

### Tier 1 - MILESTONE (Always On)
Critical game events that define the scenario flow. These should always be visible.

- Game state changes (MainMenu, InLevel, Paused, GameOver)
- Level loaded with student count and interaction count
- Scripted interaction triggered (source, target, event type, timestamp)
- Student state transitions (Calm to Distracted, Distracted to ActingOut, etc.)
- Influence applied successfully (source, target, scope, severity)
- Teacher intervention performed (type, target student)
- Goal progress updates (disruption percentage, calm downs achieved)
- Game over reason (timeout, disruption threshold, catastrophic, win)

### Tier 2 - DETAIL (Toggle Per Component)
Useful for debugging specific systems. Each component has its own toggle.

- StudentInteractionProcessor: time check results, condition evaluations, why an interaction was skipped
- StudentInfluenceManager: distance calculations, susceptibility checks, resistance rolls, fallback decisions
- StudentAgent: autonomous behavior rolls, idle timer resets, behavior selection logic
- TeacherController: input values after dead zone, movement speed, interaction target selection
- LevelManager: config loading steps, route generation, student placement

### Tier 3 - TRACE (Off by Default)
Verbose per-frame data. Only enable when deep-diving a specific issue.

- Every Update tick check in StudentInteractionProcessor
- Every influence distance calculation
- Raw input values in TeacherController
- Student animation state changes
- Navigation agent path updates

---

## Unified Log Format

All logs follow the same structure in plain language:

- Tag: component name in brackets
- Tier indicator: MILESTONE uses a star symbol, DETAIL uses a dot, TRACE uses a tilde
- Timestamp: level elapsed time in seconds with one decimal
- Message: what happened

Example format description: [ComponentName] tier-symbol (elapsed-time) message-content

---

## Step 0 - Clean Up Existing Ad-Hoc Logs

Before implementing the new system, remove all current ad-hoc Debug.Log calls and helper methods from each component.

### TeacherController
- Remove Debug.Log for "FIRST ACTIVATION" position logging
- Remove Debug.Log for "INPUT DETECTED" phantom input detection in HandleMovement
- Remove Debug.Log for "MOUSE INPUT" phantom mouse detection in HandleCamera
- Remove Debug.Log for "Teacher deactivated"
- Remove the hasLoggedFirstActivation field (no longer needed)

### StudentInteractionProcessor
- Remove the private Log() helper method at the bottom of the class
- Remove the enableDebugLogs SerializeField
- Remove all Log() calls throughout (Awake, Start, OnEnable, HandleGameStateChanged, RefreshStudentList, LoadInteractions, LoadRuntimeInteractions, CheckAndTriggerInteractions, CheckTriggerCondition, CheckTimeElapsedCondition, TriggerInteraction)

### StudentInfluenceManager
- Remove the private Log() helper method
- Remove the enableDebugLogs SerializeField
- Remove all Log() calls throughout (ProcessInfluence, ProcessWholeClassInfluence, FindNearbyStudents, FindNearestStudentInRange, ResolveInfluenceSourcesFromStudent, UpdateInfluenceIcons)
- Remove the Debug.LogError for NULL Config (will be replaced by GameLogger)

### StudentAgent
- Remove any existing ad-hoc Debug.Log calls related to behavior and state changes

### LevelManager / GameStateManager
- Remove any existing ad-hoc Debug.Log calls related to level loading and state transitions

---

## Implementation Steps

### Step 1 - Create GameLogger Utility
- Create a static utility class in Assets/Scripts/Core called GameLogger
- Provide three static methods: Milestone, Detail, Trace
- Each method takes a component name string and a message string
- Automatically prepends the tier symbol, component tag, and elapsed time from LevelManager
- Detail and Trace methods check a per-component flag before logging
- All flags are static booleans, settable from Inspector via a GameLoggerConfig MonoBehaviour

### Step 2 - Create GameLoggerConfig MonoBehaviour
- Attach to a persistent game object (same as GameStateManager)
- Expose SerializeField toggles for each component at Detail level
- Expose a single master toggle for Trace level
- Expose a toggle for the periodic scenario summary feature
- Summary interval as a float field (default 30 seconds)

### Step 3 - Refactor StudentInteractionProcessor Logging
- Replace all Debug.Log calls with GameLogger calls
- MILESTONE: when a scripted interaction triggers (source, target, event, time)
- MILESTONE: when interactions are loaded (count)
- DETAIL: time check evaluations, condition results, skip reasons
- TRACE: every CheckAndTriggerInteractions tick
- Remove the existing enableDebugLogs field, use GameLogger flags instead

### Step 4 - Refactor StudentInfluenceManager Logging
- Replace all Debug.Log calls with GameLogger calls
- MILESTONE: influence successfully applied (source to target, scope, severity result)
- MILESTONE: influence blocked by immunity or resistance
- DETAIL: distance calculations, susceptibility multipliers, nearest student search
- DETAIL: fallback from SingleStudent to WholeClass scope
- TRACE: every ProcessInfluence call details

### Step 5 - Refactor StudentAgent Logging
- MILESTONE: state transitions (old state to new state with student name)
- MILESTONE: autonomous behavior triggered (behavior type, student name)
- DETAIL: behavior roll results (chance vs roll value)
- DETAIL: idle timer events
- TRACE: per-frame state evaluation

### Step 6 - Refactor TeacherController Logging
- MILESTONE: teacher intervention performed (calm, warn, redirect with target)
- DETAIL: input values after dead zone filtering
- DETAIL: interaction target detection
- TRACE: raw input values before dead zone
- Remove existing debug log fields, migrate to GameLogger

### Step 7 - Refactor LevelManager and GameStateManager Logging
- MILESTONE: level loaded (level name, student count, interaction count, goal settings)
- MILESTONE: game state transitions
- MILESTONE: goal progress changes (disruption level, calm downs)
- DETAIL: config parsing steps, student placement, route generation
- TRACE: per-frame goal evaluation

### Step 8 - Implement Periodic Scenario Summary
- In GameLoggerConfig, run a coroutine every N seconds (default 30)
- Output a MILESTONE summary block containing:
  - Current elapsed time
  - Each student name, current state, and whether they are influenced
  - Current disruption percentage
  - Number of scripted interactions triggered vs total
  - Number of teacher interventions performed
- This gives a snapshot of game health without scrolling through logs

### Step 9 - Testing and Validation
- Run the FirstDaySummerBreak scenario with only MILESTONE logs
- Verify the timeline is clearly visible: 20s Binh wanders, 40s Binh knocks object near Lan, 60s Lan wanders, 80s Nam throws at Lan
- Enable DETAIL for StudentInfluenceManager to verify distance and susceptibility calculations
- Enable TRACE for TeacherController to verify dead zone filtering
- Confirm periodic summary shows correct state every 30 seconds

---

## Component Flag Matrix

StudentInteractionProcessor - detail toggle, trace toggle
StudentInfluenceManager - detail toggle, trace toggle
StudentAgent - detail toggle, trace toggle
TeacherController - detail toggle, trace toggle
LevelManager - detail toggle, trace toggle
GameStateManager - detail toggle, trace toggle
Periodic Summary - on/off toggle, interval setting

---

## Priority Order
1. GameLogger utility and GameLoggerConfig (foundation)
2. StudentInteractionProcessor (scripted events are most critical to verify)
3. StudentInfluenceManager (influence chain is second priority)
4. StudentAgent (autonomous behavior verification)
5. Periodic Scenario Summary (testing quality of life)
6. TeacherController (input debugging, lower priority)
7. LevelManager and GameStateManager (loading/state, lowest priority)

---

## Notes
- Keep MILESTONE logs minimal - they should fit on one screen for a full scenario run
- DETAIL logs should answer "why did this happen or not happen"
- TRACE logs are only for per-frame deep dives, never leave on in normal testing
- The periodic summary is the most valuable testing tool - it removes the need to manually track game state while playing
