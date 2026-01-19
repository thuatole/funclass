# Popup Influence Bug Diagnosis
**Date:** 2026-01-19

## 🐛 Bug Report

**Symptoms:**
```
start game
→ clean mess
→ A ổn (correct ✓)
→ B, C cũng ổn (WRONG ✗)
→ B, C đều có action escort back (WRONG for C ✗)
→ không thấy action "giải quyết cho C" của B (WRONG ✗)
→ C không hiện trạng thái đang bị B ảnh hưởng (WRONG ✗)
```

**Expected Behavior:**
```
After cleaning mess:
- Student_A: Should be calm (vomit source resolved)
- Student_B: Should still be ActingOut (throwing at C)
  → Popup should show: "⚠️ Đang ảnh hưởng: • Student_C" with button "Giải quyết cho Student_C"
- Student_C: Should still be Distracted (being hit by B)
  → Popup should show complaints:
    - "😷 Bạn Student_A ói, thúi quá!" (resolved ✓)
    - "🎯 Bạn Student_B ném đồ vào con!" (unresolved ✗)
  → Escort button should be DISABLED (has unresolved source from B)
```

---

## 🔍 Root Causes Identified

### Issue #1: GetInfluenceSources() Filters Out Resolved Sources

**File:** [StudentInteractionPopup.cs:509](../StudentInteractionPopup.cs#L509)

```csharp
private List<InfluenceSourceData> GetInfluenceSources(StudentAgent target)
{
    // ...
    foreach (var source in activeSources)
    {
        if (!source.isResolved && source.sourceStudent != null)  // ❌ ONLY returns unresolved
        {
            sources.Add(new InfluenceSourceData { ... });
        }
    }
    return sources;
}
```

**Problem:**
- This method ONLY returns unresolved sources
- After cleaning mess, Student_A's influence is marked as resolved
- When opening Student_C's popup, resolved sources are filtered out
- Popup shows NO complaints about Student_A (but should show with ✓ indicator)

**Impact:**
- Target students' popups don't show resolved sources
- User can't see what was already fixed
- No feedback that cleaning the mess actually worked

---

### Issue #2: Escort Button Logic is Wrong

**File:** [StudentInteractionPopup.cs:202](../StudentInteractionPopup.cs#L202)

```csharp
private void GenerateTargetStudentPopup()
{
    // ...
    var influenceSources = GetInfluenceSources(student);  // Returns ONLY unresolved

    // WRONG: Checks if influenceSources.Count == 0
    // This is true if GetInfluenceSources() filtered out all resolved sources!
    bool canEscort = IsStudentOutside(student) && influenceSources.Count == 0;

    if (IsStudentOutside(student))
    {
        CreateButton(PopupTextLoader.Instance.GetTargetEscortButton(canEscort),
                     () => EscortStudent(student),
                     canEscort);
    }
}
```

**Problem:**
- `GetInfluenceSources()` returns filtered list (unresolved only)
- After cleaning mess, Student_C has 1 resolved source (Student_A) + 0 unresolved sources
- `influenceSources.Count == 0` evaluates to TRUE
- Escort button appears as ENABLED (wrong!)

**Correct Logic:**
```csharp
// Should check the ACTUAL influence sources, not the filtered list
bool canEscort = IsStudentOutside(student) &&
                 student.InfluenceSources.AreAllSourcesResolved();
```

---

### Issue #3: Student_B ThrowingObject Interaction Not Triggering

**File:** [scenario_complex_example.json:23-33](../../Configs/scenario_complex_example.json#L23-L33)

```json
{
  "sourceStudent": "Student_B",
  "targetStudent": "Student_C",
  "eventType": "ThrowingObject",
  "triggerCondition": "Always",
  "probability": 1.0,
  "description": "Student_B always hits Student_C (100% chance, for testing)"
}
```

**File:** [StudentInteractionProcessor.cs:245](../StudentInteractionProcessor.cs#L245)

```csharp
private void TriggerInteraction(StudentInteractionConfig interaction)
{
    string interactionKey = $"{interaction.sourceStudent}_{interaction.targetStudent}_{interaction.eventType}";

    // ONE-TIME ONLY: Interaction only triggers once per level!
    if (oneTimeOnly && triggeredInteractions.Contains(interactionKey))
    {
        Log($"[StudentInteractionProcessor]   Already triggered (one-time only): {interactionKey}");
        return;
    }

    triggeredInteractions.Add(interactionKey);
    // ... trigger event ...
}
```

**Possible Causes:**

**A) Interaction hasn't triggered yet:**
- StudentInteractionProcessor checks every 2 seconds (line 14: `checkInterval = 2f`)
- Condition: "Always" with probability 1.0
- But checks multiple conditions:
  - Source must NOT be immune ([line 171](../StudentInteractionProcessor.cs#L171))
  - Source must NOT be following route ([line 178](../StudentInteractionProcessor.cs#L178))
  - Source and target must be in same location ([line 185](../StudentInteractionProcessor.cs#L185))

**B) Interaction triggered but was one-time:**
- If Student_B already threw at Student_C once, it won't trigger again (line 245: `oneTimeOnly = true`)
- The influence might have been resolved already
- No new ThrowingObject event will be created

**C) Student_B doesn't meet trigger conditions:**
- Student_B might be immune after being calmed
- Student_B might not be in same location as Student_C
- Student_B might be following a route

---

## 🩺 Diagnostic Steps Needed

### Need Console Logs:

**When the user tests, they should provide these logs:**

```
1. Start game → Wait 10 seconds → Check logs:
   - Look for: "[StudentInteractionProcessor] >>> Triggering: Student_B → Student_C (ThrowingObject)"
   - If NOT found: Check why (immunity? location? route? already triggered?)

2. Clean mess → Check logs:
   - Look for: "[StudentInfluenceManager] Mess cleaned - resolving sources from Student_A"
   - Look for: "[Influence] ✓ Resolved Student_A's influence on Student_B"
   - Look for: "[Influence] ✓ Resolved Student_A's influence on Student_C"

3. Click Student_B → Check popup logs:
   - Look for: "[Popup] DeterminePopupType for Student_B: affectedStudents.Count = ?"
   - Look for: "[Popup] → PopupType.SourceIndividualActions" (expected)
   - Look for: "[Popup] This student is affecting N students"

4. Click Student_C → Check popup logs:
   - Look for: "[Popup] GenerateTargetStudentPopup for Student_C"
   - Look for: "[Popup] This student is affected by N sources"
   - Look for: "[Popup]   - Affected by: Student_A (MessCreated)"
   - Look for: "[Popup]   - Affected by: Student_B (ThrowingObject)" (expected but might be missing)
   - Look for: "[Popup] IsStudentOutside(Student_C): distance=X.XXm → true/false"
```

---

## ✅ Fixes Required

### Fix #1: Show Resolved Sources in Complaints

**File:** [StudentInteractionPopup.cs:509](../StudentInteractionPopup.cs#L509)

**Current (WRONG):**
```csharp
if (!source.isResolved && source.sourceStudent != null)  // Only unresolved
```

**Option A - Show ALL sources with indicators:**
```csharp
// Remove the isResolved filter - show ALL sources
if (source.sourceStudent != null)
{
    sources.Add(new InfluenceSourceData
    {
        sourceStudent = source.sourceStudent,
        eventType = source.eventType,
        isResolved = source.isResolved  // Pass resolution state
    });
}
```

Then in `GenerateTargetStudentPopup()`:
```csharp
foreach (var source in influenceSources)
{
    string sourceName = ExtractLetter(source.sourceStudent?.Config?.studentName);
    string eventTypeStr = source.eventType.ToString();
    string complaint = PopupTextLoader.Instance.GetComplaint(eventTypeStr, sourceName);
    string icon = PopupTextLoader.Instance.GetComplaintTemplate(eventTypeStr).icon;

    // Add checkmark if resolved
    if (source.isResolved)
    {
        complaint = $"✓ {complaint}";  // Add checkmark prefix
    }

    CreateComplaintText(complaint, icon);
}
```

**Option B - Keep current behavior, just fix escort logic:**
- Keep filtering out resolved sources
- But fix escort button to check actual source count, not filtered count

---

### Fix #2: Correct Escort Button Logic

**File:** [StudentInteractionPopup.cs:202](../StudentInteractionPopup.cs#L202)

**Current (WRONG):**
```csharp
var influenceSources = GetInfluenceSources(student);  // Filtered list!
bool canEscort = IsStudentOutside(student) && influenceSources.Count == 0;
```

**Fixed (CORRECT):**
```csharp
var influenceSources = GetInfluenceSources(student);
bool allSourcesResolved = (student.InfluenceSources == null ||
                           student.InfluenceSources.AreAllSourcesResolved());
bool canEscort = IsStudentOutside(student) && allSourcesResolved;
```

---

### Fix #3: Debug Student_B Throwing Interaction

**Need to check:**

1. Is StudentInteractionProcessor active?
   ```csharp
   Log($"[StudentInteractionProcessor] isActive={isActive}, interactions.Count={interactions.Count}");
   ```

2. Are interactions loaded correctly?
   ```csharp
   Log($"[StudentInteractionProcessor] Loaded interactions:");
   foreach (var config in interactions) {
       Log($"  - {config.sourceStudent} → {config.targetStudent} ({config.eventType}, {config.triggerCondition}, prob: {config.probability})");
   }
   ```

3. Why isn't Student_B → Student_C triggering?
   ```csharp
   // In ShouldTriggerInteraction(), add more detailed logs:
   Log($"[StudentInteractionProcessor] Checking B→C:");
   Log($"  - Source immune? {source.IsImmuneToInfluence()}");
   Log($"  - Source on route? {source.IsFollowingRoute}");
   Log($"  - Same location? {StudentLocationHelper.AreInSameLocation(source, target)}");
   Log($"  - Source state: {source.CurrentState}");
   Log($"  - Already triggered? {triggeredInteractions.Contains(interactionKey)}");
   ```

---

## 📋 Recommended Fix Order

**Priority 1: Fix escort button logic** (critical bug)
- Student_C should NOT have escort button enabled if Student_B is still affecting them
- This prevents incorrect game state

**Priority 2: Show resolved sources in complaints** (UX improvement)
- Users need feedback that cleaning the mess worked
- Add checkmark indicator for resolved sources

**Priority 3: Debug Student_B throwing interaction** (may not be a bug)
- Need console logs to confirm if interaction is triggering
- Might be working as designed (one-time only)
- May need to adjust interaction trigger conditions or make it repeatable

---

## 🧪 Test Plan

**Test Case 1: After Cleaning Mess**
1. Start game
2. Wait for Student_A to vomit
3. Clean the mess
4. Click Student_A → Should show Calm state, no targets
5. Click Student_B → Should show ActingOut state, affecting Student_C (if throwing triggered)
6. Click Student_C → Should show complaints about A (resolved ✓) and B (unresolved ✗)
7. Escort button for Student_C should be DISABLED

**Test Case 2: After Resolving Student_B**
1. Continue from above
2. Click Student_B → Click "Giải quyết cho Student_C"
3. Student_B de-escalates
4. Click Student_C → Should show complaints about A (resolved ✓) and B (resolved ✓)
5. If Student_C is outside, escort button should now be ENABLED
6. Click escort → Student_C returns to seat

---

**Last Updated:** 2026-01-19
**Status:** 🔍 DIAGNOSIS COMPLETE - Fixes ready to implement
