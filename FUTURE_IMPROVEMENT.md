# FunClass - Future Improvements

**Date:** January 27, 2026
**Version:** 0.9.2
**Status:** Active Development - Core Systems 100%, UI 95%, Polish In Progress

---

## Table of Contents

1. [Influence System Safeguards](#1-influence-system-safeguards)
2. [Door and Environment Fixes](#2-door-and-environment-fixes)
3. [Student Positioning](#3-student-positioning)
4. [Event Type System](#4-event-type-system)
5. [Level Configuration](#5-level-configuration)
6. [Student Personality Tuning](#6-student-personality-tuning)
7. [UI/UX Improvements](#7-uiux-improvements)
8. [Performance Optimization](#8-performance-optimization)
9. [Testing Infrastructure](#9-testing-infrastructure)
10. [Documentation](#10-documentation)

---

## 1. Influence System Safeguards

**Priority:** High | **Status:** Identified | **Effort:** Medium

### Problem

Current influence system lacks safeguards against chain reactions:

```
Student A Vomit → WholeClass Influence → Student B Vomit → Student C Vomit
                                        → Disruption +30 → Game Over
```

### Issues Identified

#### 1.1 No Cooldown Between Influence Events

**Current Behavior:**
```csharp
ApplyInfluence(targetStudent, sourceStudent, strength, triggerEvent);
targetStudent.EscalateState();  // Immediate escalation
```

**Impact:**
- Events cascade too fast for player response
- "Sudden death" scenarios
- Frustrating gameplay

#### 1.2 No Same-Event-Type Prevention

**Current Behavior:**
```csharp
// No check for "if target already affected by this event type"
targetStudent.EscalateState();  // Can trigger SAME event type
```

**Impact:**
- Unnatural chain reactions (vomit → vomit → vomit)
- Impossible to recover from single event

#### 1.3 No Influence Chain Limit

**Current Behavior:**
```
Event A → Influence → Event B → Influence → Event C → ... (infinite)
```

**Impact:**
- Exponential disruption growth
- Unpredictable classroom state

### Proposed Solutions

#### Solution A: Cooldown System
```csharp
public class InfluenceCooldownManager
{
    private float globalCooldown = 2.0f;      // 2 seconds minimum
    private float eventTypeCooldown = 5.0f;   // 5 seconds for same type

    public bool CanReceiveInfluence(StudentAgent student, StudentEventType eventType)
    {
        // Check cooldown before applying influence
    }
}
```
**Dependencies:** StudentInfluenceManager.cs
**Quick Win:** Yes - configuration-based solution exists

#### Solution B: Same-Event-Type Prevention
```csharp
public class InfluencePreventer
{
    // Students cannot trigger same event type they were influenced by
    // Vomit influenced → escalate to WanderingAround instead
}

public AlternativeEventMapping {
    "MessCreated" → "WanderingAround",
    "ThrowingObject" → "WanderingAround",
    "KnockedOverObject" → "WanderingAround"
}
```
**Dependencies:** StudentAgent.cs (EscalateState method)
**Quick Win:** Yes - can be done via config mapping

#### Solution C: Chain Limit
```csharp
public class ChainLimitManager
{
    private int maxChainLength = 3;
    
    public bool CanPropagateInfluence(StudentEvent originalEvent, int currentDepth)
    {
        return currentDepth < maxChainLength;
    }
}
```
**Dependencies:** StudentInfluenceManager.cs
**Quick Win:** Medium - requires new class

#### Solution D: Severity Decay
```csharp
public float GetDecayedSeverity(float baseSeverity, int chainDepth)
{
    return baseSeverity * Mathf.Pow(0.5f, chainDepth);  // 50% decay per hop
}
```
**Dependencies:** StudentInfluenceManager.cs (ApplyInfluence method)
**Quick Win:** Yes - simple formula change

### Recommended Configuration

```json
"influenceSafeguards": {
  "enableCooldown": true,
  "globalCooldownSeconds": 2.0,
  "eventTypeCooldownSeconds": 5.0,
  "preventSameEventType": true,
  "maxChainDepth": 3,
  "severityDecayPerHop": 0.5,
  "influenceImmunityDuration": 5.0
}
```

**Dependencies:** None - can implement standalone
**Quick Win:** Yes - just configuration change in level JSON

### Testing Scenarios

| Scenario | Setup | Expected | Current | After Fix |
|----------|-------|----------|---------|-----------|
| Single Vomit | Student A high impulsiveness | Only A affected | B, C also vomit | Only A affected |
| Multiple Throwing | 3 students with throwing | Max 3 affected | Entire class | Max 3 affected |
| Recovery Time | Teacher intervenes | Chain stops | Chain continues | Cooldown allows intervention |

---

## 2. Door and Environment Fixes

**Priority:** High | **Status:** ✅ Completed | **Effort:** Completed
**Reference:** See Appendix A.1 for details

### Summary
- Door positioning aligned with wall gap
- Door touches floor (Y=0)
- Door opens 90° outward with left hinge
- Wall generated with 3-part door opening
- Floor extended for escape routes

---

## 3. Student Positioning

**Priority:** High | **Status:** ✅ Completed | **Effort:** Completed
**Reference:** See Appendix A.2 for details

### Summary
- Student slot offset changed from -0.3f to +0.5f
- Students now sit behind desk, facing board
- Fixed students standing on desks

---

## 4. Event Type System

**Priority:** High | **Status:** Implemented | **Effort:** Completed

### EventTypeMapping System

**Files:**
- `Assets/Configs/GUI/EventTypeMapping.json` - Mapping configuration
- `Assets/Scripts/Core/UI/PopupTextLoader.cs` - Mapping logic

### Valid Event Types (StudentEventType Enum)

| Event Type | Description | Source Key | Complaint Key |
|------------|-------------|------------|---------------|
| `MessCreated` | Student vomits | Vomit | MessCreated |
| `ThrowingObject` | Throws objects | ThrowObject | ThrowingObject |
| `MakingNoise` | Makes noise | MakeNoise | MakingNoise |
| `KnockedOverObject` | Knocks over objects | Push | KnockedOverObject |
| `WanderingAround` | Wanders in class | Distract | WanderingAround |
| `DroppedItem` | Drops item | Push | KnockedOverObject |
| `LeftSeat` | Leaves seat | Distract | WanderingAround |
| `StudentActedOut` | Violent action | Hit | StudentActedOut |

### Invalid Event Types (Do NOT Use)

| Invalid | Reason | Use Instead |
|---------|--------|-------------|
| `PhysicalInteraction` | Not in enum | `StudentActedOut` |
| `StudentHit` | Not in enum | `StudentActedOut` |
| `Teasing` / `Tease` | Not in enum | `KnockedOverObject` |
| `Poop` | Not implemented | Remove |
| `StudentVomited` | Not in enum | `MessCreated` |
| `Distraction` | Not in enum | `WanderingAround` |

### Mapping Configuration

```json
{
  "sourceStatementMapping": {
    "MessCreated": "Vomit",
    "ThrowingObject": "ThrowObject",
    "MakingNoise": "MakeNoise",
    "KnockedOverObject": "Push",
    "WanderingAround": "Distract"
  },
   "complaintMapping": {
     "Vomit": "MessCreated",
     "ThrowObject": "ThrowingObject",
     "MakeNoise": "MakingNoise",
     "Push": "KnockedOverObject",
     "Distract": "WanderingAround",
     "Hit": "StudentActedOut"
   }
}
```

---

## 5. Level Configuration

**Priority:** Medium | **Status:** Documented | **Effort:** Ongoing

### Unified JSON Schema

**Modes:**
- **Auto:** System generates desk grid, door position, routes
- **Manual:** Dev specifies all positions
- **Hybrid:** Mix of auto and manual

### Key Fields

#### classroom
```json
"classroom": {
  "width": 10,
  "depth": 8,
  "height": 3.0,
  "doorPosition": null  // null = auto (60% from left)
}
```

#### deskLayout
```json
"deskLayout": {
  "rows": 2,
  "spacingX": 2.5,
  "spacingZ": 2.5,
  "aisleWidth": 1.5
}
```

#### influenceScopeSettings
```json
"influenceScopeSettings": {
  "eventScopes": [
    {
      "eventTypeName": "MessCreated",
      "scope": "None",  // None prevents chain reaction
      "baseSeverity": 1.0
    }
  ]
}
```

#### studentConfigs.behaviors
```json
"behaviors": {
  "canFidget": true,
  "canLookAround": true,
  "canStandUp": true,
  "canMoveAround": true,
  "canDropItems": true,
  "canKnockOverObjects": true,
  "canMakeNoiseWithObjects": true,
  "canThrowObjects": true,
  "canTouchObjects": true
}
```

**Invalid Behaviors (Remove):**
- `canTease` - Does not exist
- `canWanderAround` - Use `canMoveAround` instead

---

## 6. Student Personality Tuning

**Priority:** Medium | **Status:** Documented | **Effort:** Ongoing

### Personality Traits

| Trait | Range | Description |
|-------|-------|-------------|
| `patience` | 0.0 - 1.0 | Time before becoming Distracted |
| `attentionSpan` | 0.0 - 1.0 | Ability to focus |
| `impulsiveness` | 0.0 - 1.0 | Probability of causing disruption when Critical |
| `influenceSusceptibility` | 0.0 - 1.0 | Vulnerability to peer influence |
| `influenceResistance` | 0.0 - 1.0 | Ability to resist influence |
| `panicThreshold` | 0.0 - 1.0 | Threshold for Critical state |

### Example Configurations

#### Troublemaker (Bình)
```json
{
  "patience": 0.2,
  "impulsiveness": 0.9,
  "influenceSusceptibility": 0.5,
  "influenceResistance": 0.3,
  "panicThreshold": 0.3
}
```

#### Sensitive (Lan)
```json
{
  "patience": 0.4,
  "impulsiveness": 0.2,  // Low to prevent auto-vomit
  "influenceSusceptibility": 0.9,
  "influenceResistance": 0.5,  // Increased to prevent chain
  "panicThreshold": 0.5
}
```

#### Follower (Nam)
```json
{
  "patience": 0.5,
  "impulsiveness": 0.7,
  "influenceSusceptibility": 0.5,  // Reduced from 0.7
  "influenceResistance": 0.5,
  "panicThreshold": 0.5
}
```

#### Good Student (Mai)
```json
{
  "patience": 0.8,
  "impulsiveness": 0.2,
  "influenceSusceptibility": 0.3,
  "influenceResistance": 0.7,
  "panicThreshold": 0.6
}
```

---

## 7. UI/UX Improvements

**Priority:** Medium | **Status:** In Progress | **Effort:** Medium

### Completed

| Feature | Status |
|---------|--------|
| Popup Text Externalization | ✅ Done |
| EventTypeMapping System | ✅ Done |
| CursorManager Integration | ✅ Done |
| Door Positioning | ✅ Done |
| Student Position Fix | ✅ Done |

### In Progress

| Feature | Status | Notes |
|---------|--------|-------|
| Cursor State Management | 🔄 Working | Lock during gameplay, unlock on popup |
| Tutorial System | ❌ Not Started | Needed for new player onboarding |

### Planned

#### 7.1 Tutorial System

**Purpose:** Guide new players through gameplay mechanics.

**Content:**
1. Student states and escalation
2. Teacher actions (Calm, Escort, Clean)
3. Influence system explanation
4. Disruption management

**Implementation:**
```csharp
public class TutorialManager
{
    public TutorialStep[] steps;
    public void StartTutorial();
    public void SkipTutorial();
    public void CompleteStep(int stepIndex);
}
```
**Dependencies:** PopupManager.cs, UI Canvas
**Quick Win:** No - requires new system

#### 7.2 Visual Feedback

**Improvements:**
- Cooldown indicators on students
- Influence chain visualization
- Disruption meter animations
- Student highlight effects

**Configuration:**
```json
"visualFeedback": {
  "showCooldownIndicator": true,
  "showImmunityGlow": true,
  "showChainLimitWarning": true,
  "highlightIntensity": 1.5f
}
```
**Dependencies:** StudentAgent.cs, UI scripts
**Quick Win:** Yes - configuration-based

#### 7.3 Influence Chain Visualization

**Purpose:** Show players which students are influencing whom in real-time.

**Features:**
- Arrows/lines connecting source to affected students
- Color-coded by event type (red=violent, yellow=noise, etc.)
- Animated flow direction
- Toggle on/off with UI button

**Implementation:**
```csharp
public class InfluenceChainVisualizer : MonoBehaviour
{
    public LineRenderer[] influenceLines;
    public GameObject arrowPrefab;
    
    public void UpdateVisualization()
    {
        // Clear existing lines
        // Draw new lines based on StudentInfluenceManager data
        foreach (var influence in activeInfluences)
        {
            DrawLine(influence.source, influence.target, influence.eventType);
        }
    }
}
```

**Visual Design:**
| Event Type | Color | Line Style |
|------------|-------|------------|
| MessCreated | 🔴 Red | Solid |
| ThrowingObject | 🟠 Orange | Dashed |
| MakingNoise | 🟡 Yellow | Dotted |
| KnockedOverObject | 🟣 Purple | Solid |
| WanderingAround | 🔵 Blue | Dashed |

**Dependencies:** StudentInfluenceManager, LineRenderer component, UI Canvas
**Quick Win:** Medium - requires new UI element

#### 7.4 Sound Effects

**Missing Features:**
- Vomit sound effect
- Throwing object sound
- Student vocalizations
- Teacher action sounds
- Ambient classroom sounds

---

## 8. Performance Optimization

**Priority:** Low | **Status:** Not Started | **Effort:** Medium

### Identified Issues

#### 8.1 Influence Range Checks

**Current:** O(n²) for n students
**Issue:** Frame rate drops with 20+ students

**Solution:** Spatial partitioning
```csharp
public class SpatialGrid
{
    private Dictionary<GridCell, List<StudentAgent>> cells;
    public List<StudentAgent> GetNearbyStudents(Vector3 position, float radius);
}
```
**Dependencies:** StudentInfluenceManager.cs
**Quick Win:** No - requires significant refactor

#### 8.2 Object Pooling

**Issue:** Mess objects created/destroyed frequently

**Solution:** Object pool for mess objects
```csharp
public class MessObjectPool
{
    private Queue<MessObject> pool;
    public MessObject GetMessObject();
    public void ReturnMessObject(MessObject mess);
}
```
**Dependencies:** StudentMessCreator.cs, Mess cleanup system
**Quick Win:** Yes - can implement with moderate effort

#### 8.3 Update Frequency

**Issue:** Non-critical systems running every frame

**Solution:** Reduce Update() frequency
```csharp
// Run influence calculations every 0.5 seconds instead of every frame
private float influenceUpdateInterval = 0.5f;
```
**Dependencies:** StudentInfluenceManager.cs, StudentAgent.cs
**Quick Win:** Yes - simple code change

---

## 9. Testing Infrastructure

**Priority:** Low | **Status:** Not Started | **Effort:** Medium

### Test Levels

| Level | Purpose | Status |
|-------|---------|--------|
| `test_vomit_escape.json` | Vomit and escape mechanics | ✅ Created |
| `level_first_day_summer_break.json` | Full gameplay scenario | ✅ Created |

### Automated Tests Needed

| Test | Description |
|------|-------------|
| Influence Chain Test | Verify chain limit works |
| Cooldown Test | Verify cooldown prevents rapid escalation |
| Event Mapping Test | Verify EventTypeMapping works correctly |
| Student Config Test | Verify configs load correctly |
| Level Import Test | Verify JSON import works |

### Testing Scenarios

| Scenario | Expected Result | Current Status |
|----------|-----------------|----------------|
| Single student vomits | Only that student affected | ✅ Fixed via config (scope=None) |
| Student A hits Student B | Only B affected | ✅ Works |
| Student throws object | Nearby students affected | ✅ Works |
| Teacher calms student | Disruption decreases | ✅ Works |

---

## 10. Documentation

**Priority:** Medium | **Status:** Ongoing | **Effort:** Low

### Created Documentation

| File | Description | Status |
|------|-------------|--------|
| `CONFIG_GUIDE.md` | Complete configuration reference | ✅ Done |
| `PROJECT_OVERVIEW.md` | Project overview and architecture | ✅ Updated |
| `FUTURE_IMPROVEMENT_InfluenceSafeguards.md` | Influence system improvements | ✅ Created |

### Pending Documentation

| Document | Description | Priority |
|----------|-------------|----------|
| API_REFERENCE.md | Complete API documentation | Low |
| BEHAVIOR_TREE.md | Student behavior system | Medium |
| TESTING_GUIDE.md | Testing procedures | Low |
| DEPLOYMENT.md | Build and deployment guide | Low |

---

## 0. Known Issues

**Priority:** Varies | **Status:** Documented

### Critical (Blocking Gameplay)

**None currently** - Game is playable end-to-end

### High Priority

**None currently** - All high priority issues resolved

### Medium Priority

#### Influence Chain Not Visible
**Status:** 🟡 Known
**Severity:** Medium
**Issue:** Players can't see who is influencing whom
**Fix:** Implement InfluenceChainVisualization (see Section 7.3)

### Low Priority

#### Student Highlight Turns White on Click
**Status:** 🟡 Known
**Severity:** Low
**Issue:** Model turns white instead of highlighted color
**Fix Required:** Adjust StudentHighlight.cs brightness calculation

#### Performance with 20+ Students
**Status:** 🟡 Known
**Severity:** Low
**Issue:** Frame rate drops with large number of students
**Fix:** See Section 8.1 - Spatial partitioning

### Resolved Issues (Archived)

| Issue | Status | Fixed Date | Reference |
|-------|--------|------------|-----------|
| EventTypeMapping Vietnamese Keys | ✅ Fixed | 2026-01-27 | CONFIG_GUIDE.md |
| StudentConfig Assignment | ✅ Fixed | 2026-01-26 | PROJECT_OVERVIEW.md |
| Door Positioning | ✅ Fixed | 2026-01-27 | Appendix A.1 |
| Student Position Behind Desk | ✅ Fixed | 2026-01-27 | Appendix A.2 |
| Vomit Chain Reaction | ✅ Fixed | 2026-01-27 | Appendix A.3 |

---

## Quick Wins

**Easy improvements that can be implemented in 1-2 days**

### Configuration Changes (No Code Required)

| Quick Win | Effort | Impact | Description |
|-----------|--------|--------|-------------|
| Adjust influenceScope for events | 5 min | High | Change scope from WholeClass to None/SingleStudent |
| Tune student personality values | 10 min | Medium | Adjust patience, impulsiveness, susceptibility |
| Add new event type mapping | 5 min | Low | Add mapping to EventTypeMapping.json |
| Adjust disruption thresholds | 5 min | Medium | Change maxDisruptionThreshold in goalSettings |

### Simple Code Changes

| Quick Win | Effort | Impact | Dependencies |
|-----------|--------|--------|--------------|
| Add debug log for influence events | 30 min | Low | None |
| Increase influence cooldown | 1 hour | Medium | StudentInfluenceManager.cs |
| Add student name to popup | 1 hour | Low | StudentInteractionPopup.cs |
| Change disruption colors | 2 hours | Low | UI scripts |

### Testing & Documentation

| Quick Win | Effort | Impact |
|-----------|--------|--------|
| Create test level template | 2 hours | High |
| Document student behaviors | 1 hour | Medium |
| Add inline code comments | 3 hours | Low |
| Create bug report template | 1 hour | Low |

---

## Implementation Roadmap

### Phase 1: Critical Fixes (Week 1)

| Task | Priority | Status | Dependencies |
|------|----------|--------|--------------|
| Influence Safeguards (Cooldown) | High | Not Started | StudentInfluenceManager.cs |
| Influence Safeguards (Chain Limit) | High | Not Started | StudentInfluenceManager.cs |
| Tutorial System | Medium | Not Started | UI Canvas, PopupManager.cs |

### Phase 2: Polish (Week 2)

| Task | Priority | Status | Dependencies |
|------|----------|--------|--------------|
| Visual Feedback | Medium | Not Started | StudentAgent.cs, UI scripts |
| Sound Effects | Low | Not Started | Audio system |
| Performance Optimization | Low | Not Started | See Section 8 |

### Phase 3: Testing (Week 3)

| Task | Priority | Status | Dependencies |
|------|----------|--------|--------------|
| Automated Tests | Medium | Not Started | Unity Test Framework |
| Test Level Creation | Medium | In Progress | None |
| Balance Tuning | Medium | Ongoing | Playtesting |

---

## Related Files

### Configuration Files
- `Assets/Configs/GUI/PopupText.json`
- `Assets/Configs/GUI/ComplaintTemplates.json`
- `Assets/Configs/GUI/SourceStatements.json`
- `Assets/Configs/GUI/ButtonLabels.json`
- `Assets/Configs/GUI/EventTypeMapping.json`

### Level Templates
- `Assets/LevelTemplates/test_vomit_escape.json`
- `Assets/LevelTemplates/level_first_day_summer_break.json`

### Source Code
- `Assets/Scripts/Core/UI/PopupTextLoader.cs`
- `Assets/Scripts/Core/StudentInfluenceManager.cs`
- `Assets/Scripts/Core/StudentAgent.cs`
- `Assets/Scripts/Editor/Modules/EnvironmentSetup.cs`
- `Assets/Scripts/Editor/Modules/DeskGridGenerator.cs`
- `Assets/Scripts/Editor/Modules/WallGenerator.cs`

### Documentation
- `CONFIG_GUIDE.md`
- `PROJECT_OVERVIEW.md`
- `FUTURE_IMPROVEMENT_InfluenceSafeguards.md`

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 0.9.2 | 2026-01-27 | Door positioning, student positioning, EventTypeMapping |
| 0.9.1 | 2026-01-26 | StudentConfig fixes, Unified Import System |
| 0.9.0 | 2026-01-25 | Core systems complete |

---

**Last Updated:** January 27, 2026
**Next Review:** February 3, 2026

---

## Appendix A: Completed Fixes Archive

**These issues have been resolved and are kept for reference.**

### A.1 Door and Environment Fixes (Fixed 2026-01-27)

**Problem:** Door was misaligned with wall gap, not touching floor.

**Fix Applied (EnvironmentSetup.cs:269-296):**

| Property | Before | After |
|----------|--------|-------|
| Door X | `doorPosition.x - (doorWidth / 2f)` | `doorPosition.x - (gapWidth / 2f)` |
| Door Z | `doorPosition.z` | `backWallZ + 0.1f` |
| Door Rotation | `90f` | `-90f` |
| Door Y | `doorHeight / 2f` | `0f` |

**Formula for Door Position:**
```
Door X = gap center X - (gap width / 2) = Hinge at gap left edge
Door Z = backWallZ + 0.1f = In front of wall
Door Y = 0 = Touching floor
Door Rotation = -90° = Open 90 degrees outward
```

**Wall Generation (WallGenerator.cs):**
- Wall with door opening created from 3 cube primitives
- Left part (X: wall left to door left)
- Right part (X: door right to wall right)
- Top part (Y: door top to wall top)

**Floor Extension:**
```csharp
float outsideDepth = 3f;  // Extend 3 units outside
float totalDepth = schema.classroom.depth + outsideDepth;
```

### A.2 Student Positioning (Fixed 2026-01-27)

**Problem:** Students were standing on top of desks due to incorrect offset.

**Fix (DeskGridGenerator.cs:73-75):**

| Before | After |
|--------|-------|
| `position + Vector3(0, 0, -0.3f)` | `position + Vector3(0, 0, 0.5f)` |

**Explanation:**
- `-0.3f` placed student in FRONT of desk (toward board)
- `+0.5f` places student BEHIND desk (toward back wall)
- Student now sits facing board (-Z direction)

### A.3 Vomit Chain Reaction (Fixed 2026-01-27)

**Problem:** Student A vomit → WholeClass influence → Student B vomit → Student C vomit → Game Over

**Root Cause:**
- MessCreated had WholeClass scope
- Lan and Nam had high susceptibility but low resistance
- No cooldown between influence events

**Fix Applied (level_first_day_summer_break.json):**

| Change | Before | After |
|--------|--------|-------|
| MessCreated scope | `WholeClass` | `None` |
| Lan influenceResistance | `0.1` | `0.5` |
| Lan panicThreshold | `0.2` | `0.5` |
| Lan impulsiveness | `0.3` | `0.2` |
| Nam influenceSusceptibility | `0.7` | `0.5` |
| Nam influenceResistance | `0.2` | `0.5` |
| Nam panicThreshold | `0.4` | `0.5` |

**Result:** Vomit no longer triggers chain reaction.

**Dependencies:** None - configuration-based fix

---

## Appendix B: File Reference

### Configuration Files
| File | Purpose |
|------|---------|
| `Assets/Configs/GUI/PopupText.json` | Popup text templates |
| `Assets/Configs/GUI/ComplaintTemplates.json` | Complaint templates |
| `Assets/Configs/GUI/SourceStatements.json` | Source student statements |
| `Assets/Configs/GUI/ButtonLabels.json` | Button labels and tooltips |
| `Assets/Configs/GUI/EventTypeMapping.json` | Event type mappings |

### Level Templates
| File | Purpose |
|------|---------|
| `Assets/LevelTemplates/test_vomit_escape.json` | Vomit and escape test |
| `Assets/LevelTemplates/level_first_day_summer_break.json` | Full gameplay scenario |

### Source Code
| File | Purpose |
|------|---------|
| `Assets/Scripts/Core/UI/PopupTextLoader.cs` | Text loading and mapping |
| `Assets/Scripts/Core/StudentInfluenceManager.cs` | Influence system |
| `Assets/Scripts/Core/StudentAgent.cs` | Student behavior |
| `Assets/Scripts/Editor/Modules/EnvironmentSetup.cs` | Environment setup |
| `Assets/Scripts/Editor/Modules/DeskGridGenerator.cs` | Desk grid generation |
| `Assets/Scripts/Editor/Modules/WallGenerator.cs` | Wall generation |

### Documentation
| File | Purpose |
|------|---------|
| `CONFIG_GUIDE.md` | Configuration reference |
| `PROJECT_OVERVIEW.md` | Project overview |
| `FUTURE_IMPROVEMENT_InfluenceSafeguards.md` | Influence safeguards |
