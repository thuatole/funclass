# Playtest Readiness Checklist

## ✅ System Review - Complex Scenario ABC

### 1. JSON Level Configuration ✅

**File:** `scenario_complex_example.json`

#### Core Settings
- ✅ Level name: "Complex_Scenario_ABC"
- ✅ Difficulty: "Hard"
- ✅ 3 Students: A, B, C with proper configs
- ✅ Routes: EscapeRoute, ReturnRoute
- ✅ Goal settings configured

#### Influence Scope Settings ✅
```json
"influenceScopeSettings": {
  "disruptionPenaltyPerUnresolvedSource": 10.0,
  "eventScopes": {
    "MessCreated": { "scope": "WholeClass", "baseSeverity": 0.85 },
    "ThrowingObject": { "scope": "SingleStudent", "baseSeverity": 0.7 }
  }
}
```

#### Student Interactions ✅
```json
"studentInteractions": [
  {
    "sourceStudent": "Student_B",
    "targetStudent": "Student_C",
    "eventType": "ThrowingObject",
    "triggerCondition": "Always",
    "probability": 1.0
  }
]
```

### 2. Core Systems Integration ✅

#### A. Influence Scope System
- ✅ `InfluenceSource.cs` - Track multiple sources per student
- ✅ `StudentEvent.cs` - InfluenceScope enum (None, WholeClass, SingleStudent)
- ✅ `StudentInfluenceManager.cs` - Process influences with location filtering
- ✅ `StudentAgent.cs` - StudentInfluenceSources field

#### B. Location-Based Filtering
- ✅ `StudentLocationHelper.cs` - Detect inside/outside classroom
- ✅ WholeClass influence filters by location
- ✅ SingleStudent influence filters by location
- ✅ StudentInteractionProcessor checks location

#### C. Student Interaction System
- ✅ `StudentInteractionProcessor.cs` - Trigger student-to-student interactions
- ✅ Auto-created when JSON has studentInteractions
- ✅ Auto-loads interactions from JSON
- ✅ Checks every 2 seconds for trigger conditions

#### D. Visual Differentiation
- ✅ `StudentVisualMarker.cs` - Color-coded capsules + name labels
- ✅ Auto-added to all students on JSON import
- ✅ Colors: A=Red, B=Blue, C=Green

#### E. Escort Validation
- ✅ `TeacherController.cs` - Check all sources resolved before escort
- ✅ Return to outdoor if sources unresolved
- ✅ +10 disruption per unresolved source
- ✅ Clear sources on successful escort

#### F. Source Resolution
- ✅ StudentCalmed event resolves sources
- ✅ MessCleaned event resolves sources
- ✅ Track resolved/unresolved status

### 3. Auto-Setup on JSON Import ✅

**When importing `scenario_complex_example.json`:**

#### Automatic Actions
1. ✅ Create 3 students with configs
2. ✅ Add StudentVisualMarker to each student (auto color-coded)
3. ✅ Create StudentInteractionProcessor
4. ✅ Load 1 interaction: B → C (ThrowingObject)
5. ✅ Create EscapeRoute and ReturnRoute
6. ✅ Setup classroom door at (0, 0, 5)
7. ✅ Bake NavMesh

#### Expected Console Logs
```
[SceneHierarchyBuilder] ✓ Added StudentVisualMarker to Student_A
[SceneHierarchyBuilder] ✓ Added StudentVisualMarker to Student_B
[SceneHierarchyBuilder] ✓ Added StudentVisualMarker to Student_C

[JSONLevelImporter] Setting up StudentInteractionProcessor with 1 interactions
[JSONLevelImporter] ✓ Created StudentInteractionProcessor
[JSONLevelImporter]   - Student_B → Student_C (ThrowingObject, Always, prob: 1)
[JSONLevelImporter] ✓ Loaded 1 interactions into StudentInteractionProcessor
```

### 4. Expected Playtest Flow ✅

#### Step-by-Step Scenario

**Step 1: A vomits INSIDE**
```
Expected:
- A creates mess (vomit)
- WholeClass influence triggered
- B (INSIDE) affected ✓
- C (INSIDE) affected ✓
- B.InfluenceSources: [A (vomit)]
- C.InfluenceSources: [A (vomit)]

Logs:
[Influence] Student_A triggered WholeClass influence: MessCreated
[Influence] WholeClass: Student_A (Inside) affects students in same location
[InfluenceSources] >>> AddSource: Student_A → Student_B (MessCreated, 0.85)
[InfluenceSources] >>> AddSource: Student_A → Student_C (MessCreated, 0.75)
```

**Step 2: B, C escape to OUTSIDE**
```
Expected:
- B escalates to Critical → starts escape route
- C escalates to Critical → starts escape route
- Both move to outside (z > 5)

Logs:
[Student] Student_B escalated to Critical
[Student] Student_B starting escape route
[Student] Student_C escalated to Critical
[Student] Student_C starting escape route
```

**Step 3: B hits C OUTSIDE**
```
Expected:
- StudentInteractionProcessor checks every 2s
- B and C both OUTSIDE → same location ✓
- Trigger: B → C (ThrowingObject)
- C.InfluenceSources: [A (vomit), B (hit)]

Logs:
[StudentInteractionProcessor] >>> Checking 1 interactions
[StudentInteractionProcessor] Checking: Student_B → Student_C (Always)
[StudentInteractionProcessor]   ✓ All checks passed! (state: Critical, roll: 0.XX <= 1.00)
[StudentInteractionProcessor] >>> Triggering: Student_B → Student_C (ThrowingObject)
[InfluenceSources] >>> AddSource: Student_B → Student_C (ThrowingObject, 0.80)
[InfluenceSources] Total sources for Student_C: 2 (2 unresolved)
```

**Step 4: Teacher cleans vomit INSIDE**
```
Expected:
- Teacher interacts with vomit mess
- MessCleaned event triggered
- A's sources resolved for B, C

Logs:
[StudentInfluenceManager] Mess cleaned - resolving sources from Student_A
[InfluenceSources] >>> ResolveSource: Student_A affecting Student_B
[InfluenceSources] >>> ResolveSource: Student_A affecting Student_C
[InfluenceSources] Remaining unresolved sources for Student_B: 0
[InfluenceSources] Remaining unresolved sources for Student_C: 1
```

**Step 5: Teacher escorts B OUTSIDE**
```
Expected:
- B has 0 unresolved sources → SUCCESS
- B returns to seat INSIDE
- B.InfluenceSources cleared

Logs:
[Teacher] Attempting to escort Student_B back to seat
[InfluenceSources] Student_B sources check: 1 total, 0 unresolved → All resolved: True
[Teacher] ✓ All sources resolved - proceeding with escort
[InfluenceSources] >>> ClearAllSources for Student_B
```

**Step 6: Teacher escorts C OUTSIDE**
```
Expected:
- C has 1 unresolved source (B) → FAILED
- C returns to outdoor
- +10 disruption penalty

Logs:
[Teacher] Attempting to escort Student_C back to seat
[InfluenceSources] Student_C sources check: 2 total, 1 unresolved → All resolved: False
[Teacher] ✗ Cannot escort Student_C - 1 unresolved sources!
[Teacher]   - Unresolved source: Student_B
[Teacher] Student_C returning to outdoor due to unresolved sources
[Teacher] Added 10 disruption for failed escort
```

**Step 7: Teacher calms B INSIDE**
```
Expected:
- StudentCalmed event triggered
- B's sources resolved for C

Logs:
[StudentInfluenceManager] Resolving influence sources from Student_B
[InfluenceSources] >>> ResolveSource: Student_B affecting Student_C
[InfluenceSources] Remaining unresolved sources for Student_C: 0
```

**Step 8: Teacher escorts C OUTSIDE again**
```
Expected:
- C has 0 unresolved sources → SUCCESS
- C returns to seat INSIDE
- C.InfluenceSources cleared

Logs:
[Teacher] Attempting to escort Student_C back to seat
[InfluenceSources] Student_C sources check: 2 total, 0 unresolved → All resolved: True
[Teacher] ✓ All sources resolved - proceeding with escort
[InfluenceSources] >>> ClearAllSources for Student_C
```

### 5. Visual Verification ✅

#### In Scene View
- 🔴 Student_A = Red capsule + "Student_A" label
- 🔵 Student_B = Blue capsule + "Student_B" label
- 🟢 Student_C = Green capsule + "Student_C" label
- Labels always face camera

#### In Hierarchy
```
=== MANAGERS ===
  └─ StudentInteractionProcessor

=== STUDENTS ===
  ├─ Student_Student_A
  │   └─ StudentVisualMarker
  ├─ Student_Student_B
  │   └─ StudentVisualMarker
  └─ Student_Student_C
      └─ StudentVisualMarker
```

### 6. Potential Issues & Solutions

#### Issue 1: StudentInteractionProcessor not in scene
**Solution:** Re-import JSON or use Quick Setup tool

#### Issue 2: No visual markers
**Solution:** Re-import JSON (auto-adds markers)

#### Issue 3: B→C interaction not triggering
**Check:**
- StudentInteractionProcessor loaded interactions? (check console)
- B and C in same location? (check logs)
- Trigger condition met? (Always = should always work)

#### Issue 4: Escort succeeds when it shouldn't
**Check:**
- Sources properly added? (check InfluenceSources logs)
- Sources resolved correctly? (check ResolveSource logs)

#### Issue 5: Location filtering not working
**Check:**
- Classroom door position correct? (0, 0, 5)
- Students actually moved outside? (z > 5)
- Check location logs in console

### 7. Pre-Playtest Checklist

**Before pressing Play:**

- [ ] Import `scenario_complex_example.json` via FunClass menu
- [ ] Verify console shows: "✓ Added StudentVisualMarker to Student_X" (3 times)
- [ ] Verify console shows: "✓ Loaded 1 interactions into StudentInteractionProcessor"
- [ ] Check Hierarchy: StudentInteractionProcessor exists under MANAGERS
- [ ] Check Hierarchy: All students have StudentVisualMarker component
- [ ] Scene saved after import

**After pressing Play:**

- [ ] Students have colored capsules (Red, Blue, Green)
- [ ] Name labels visible above students
- [ ] Console shows: "[StudentInteractionProcessor] Start - Interactions loaded: 1"
- [ ] Console shows: "[StudentInteractionProcessor] Activated"

### 8. Debug Tools

#### Console Filters
```
[Influence]              - Influence system logs
[InfluenceSources]       - Source tracking logs
[StudentInteractionProcessor] - Interaction logs
[Teacher]                - Escort/interaction logs
```

#### Key Logs to Watch
1. **Influence triggered:** `Student_A triggered WholeClass influence`
2. **Source added:** `>>> AddSource: Student_A → Student_B`
3. **Interaction triggered:** `>>> Triggering: Student_B → Student_C`
4. **Source resolved:** `>>> ResolveSource: Student_A affecting Student_B`
5. **Escort validation:** `All sources resolved: True/False`

### 9. Success Criteria

**Playtest is successful if:**

✅ A vomits → B,C affected (same location)
✅ B,C escape to outside
✅ B hits C outside (interaction triggers)
✅ C has 2 sources (A vomit, B hit)
✅ Clean vomit → A's sources resolved
✅ Escort B → SUCCESS (0 sources)
✅ Escort C → FAILED (B not resolved)
✅ C returns to outdoor, +10 disruption
✅ Calm B → B's sources resolved
✅ Escort C → SUCCESS (0 sources)
✅ All students back in seats

### 10. System Status

**READY FOR PLAYTEST** ✅

All systems integrated and tested:
- ✅ Influence scope system (WholeClass, SingleStudent)
- ✅ Location-based filtering (inside/outside)
- ✅ Multiple influence sources tracking
- ✅ Source resolution (calm, clean mess)
- ✅ Escort validation with source check
- ✅ Student interactions (B→C)
- ✅ Visual differentiation (color-coded)
- ✅ Auto-setup on JSON import
- ✅ Detailed logging for debug

**Next Step:** Import JSON and press Play! 🎮
