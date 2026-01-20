# Source And Target Popup Fix
**Date:** 2026-01-19

## 🐛 Bug Report

**User Issue:** "chưa clean mess nhưng ko thấy B ảnh hưởng bởi A trong popup (chỉ có đang ảnh hưởng lên C)"

**Screenshot Analysis:**
```
Student_B Popup shows:
- "Em xin lỗi cô..."
- "Đang ảnh hưởng:" (Warning section)
- "• Student_C" with button "✅ Giải quyết cho Student_C"
- Close button

MISSING:
- No mention that Student_B is affected by Student_A's vomit!
- Student should show complaints about being affected
```

---

## 🔍 Root Cause

### Old Logic (WRONG):

**File:** [StudentInteractionPopup.cs:124-154](StudentInteractionPopup.cs#L124-L154)

```csharp
private PopupType DeterminePopupType(StudentAgent student)
{
    var affectedStudents = GetAffectedStudents(student);

    if (affectedStudents.Count == 0)
    {
        return PopupType.TargetStudent;  // Show who affects THIS student
    }

    // If student affects anyone...
    return PopupType.SourceIndividualActions;  // Show who THIS student affects
}
```

**Problem:**
- If `affectedStudents.Count > 0`, always treats as pure source
- Never checks if student is ALSO affected by others
- Student_B affects Student_C → PopupType.SourceIndividualActions
- But Student_B is ALSO affected by Student_A → IGNORED!

---

## ✅ Solution: New Popup Type `SourceAndTarget`

### New Enum Value:

**File:** [StudentInteractionPopup.cs:9-16](StudentInteractionPopup.cs#L9-L16)

```csharp
public enum PopupType
{
    TargetStudent,              // Student is only affected by others
    SourceInfoOnly,             // Student affects others but no actions available
    SourceWholeClassAction,     // Student affects whole class
    SourceIndividualActions,    // Student affects specific students
    SourceAndTarget            // ✨ NEW: Both affects others AND is affected by others
}
```

---

## 🔧 Fixed Logic

### Updated DeterminePopupType:

**File:** [StudentInteractionPopup.cs:129-171](StudentInteractionPopup.cs#L129-L171)

```csharp
private PopupType DeterminePopupType(StudentAgent student)
{
    var affectedStudents = GetAffectedStudents(student);
    var influenceSources = GetInfluenceSources(student);  // ✨ NEW: Check sources too!

    Debug.Log($"[Popup] DeterminePopupType for {student.Config?.studentName}:");
    Debug.Log($"[Popup]   - Affects {affectedStudents.Count} student(s)");
    Debug.Log($"[Popup]   - Affected by {influenceSources.Count} source(s)");

    // Case 1: Pure target (only affected, doesn't affect anyone)
    if (affectedStudents.Count == 0)
    {
        return PopupType.TargetStudent;
    }

    // Case 2-5: Student affects others
    var eventType = GetSourceEventType(student);

    // ✨ NEW: Check if this student is ALSO affected by others
    bool isAlsoTarget = influenceSources.Count > 0;

    if (!HasStudentResolveAction(eventType))
    {
        return PopupType.SourceInfoOnly;
    }
    else if (IsWholeClassAction(eventType))
    {
        return PopupType.SourceWholeClassAction;
    }
    else
    {
        // ✨ NEW: Check if student is BOTH source AND target
        if (isAlsoTarget)
        {
            Debug.Log($"[Popup] → PopupType.SourceAndTarget (affects {affectedStudents.Count} AND affected by {influenceSources.Count})");
            return PopupType.SourceAndTarget;
        }
        else
        {
            return PopupType.SourceIndividualActions;
        }
    }
}
```

---

## 🎨 New Popup Layout: SourceAndTarget

### GenerateSourceAndTargetPopup Method:

**File:** [StudentInteractionPopup.cs:329-400](StudentInteractionPopup.cs#L329-L400)

```csharp
private void GenerateSourceAndTargetPopup()
{
    Debug.Log($"[Popup] GenerateSourceAndTargetPopup for {student.Config?.studentName}");

    // ═══════════════════════════════════════════════════════════
    // PART 1: Show who affects THIS student (Target role)
    // ═══════════════════════════════════════════════════════════

    var influenceSources = GetInfluenceSources(student);

    if (influenceSources.Count > 0)
    {
        openingPhraseText.text = "💬 \"Cô ơi! Em bị ảnh hưởng...\"";

        // Section header
        CreateComplaintText("📋 Em đang bị ảnh hưởng bởi:", "😟");

        // List all sources affecting this student
        foreach (var source in influenceSources)
        {
            string sourceName = ExtractLetter(source.sourceStudent?.Config?.studentName);
            string eventTypeStr = source.eventType.ToString();
            string complaint = PopupTextLoader.Instance.GetComplaint(eventTypeStr, sourceName);
            string icon = PopupTextLoader.Instance.GetComplaintTemplate(eventTypeStr).icon;

            // Add checkmark if resolved
            if (source.isResolved)
            {
                complaint = $"✓ {complaint}";
            }

            CreateComplaintText(complaint, icon);
        }
    }

    // ═══════════════════════════════════════════════════════════
    // PART 2: Show who THIS student affects (Source role)
    // ═══════════════════════════════════════════════════════════

    var affectedStudents = GetAffectedStudents(student);

    if (affectedStudents.Count > 0)
    {
        var groupedByAction = GroupTargetsByActionType(student, affectedStudents);

        foreach (var actionGroup in groupedByAction)
        {
            string actionType = actionGroup.Key;
            List<StudentAgent> targets = actionGroup.Value;

            // Impact warning
            CreateComplaintText(PopupTextLoader.Instance.GetSourceImpactIndividual(), "⚠️");

            // Create target list with individual resolve buttons
            foreach (var target in targets)
            {
                string targetName = ExtractLetter(target.Config?.studentName);
                CreateTargetActionItemWithButton(target, targetName, () => ResolveForTarget(student, target));
            }
        }
    }

    CreateButton(PopupTextLoader.Instance.GetSourceCloseButton(), () => ClosePopup());
}
```

---

## 📊 Example: Student_B Popup (After Fix)

### Scenario:
- Student_A vomits (affects B and C)
- Student_B throws at Student_C

### Old Popup (WRONG):
```
━━━━━━━━━━━━━━━━━━━━━━━
Student_B - Critical 🤯
━━━━━━━━━━━━━━━━━━━━━━━
💬 "Em xin lỗi cô..."

⚠️ Đang ảnh hưởng:
• Student_C [✅ Giải quyết cho Student_C]

[Close]
━━━━━━━━━━━━━━━━━━━━━━━
```
**Missing:** No mention of Student_A's vomit!

---

### New Popup (CORRECT):
```
━━━━━━━━━━━━━━━━━━━━━━━
Student_B - Critical 🤯
━━━━━━━━━━━━━━━━━━━━━━━
💬 "Cô ơi! Em bị ảnh hưởng..."

📋 Em đang bị ảnh hưởng bởi:
  😷 Bạn Student_A ói, thúi quá!

⚠️ Đang ảnh hưởng:
• Student_C [✅ Giải quyết cho Student_C]

[Close]
━━━━━━━━━━━━━━━━━━━━━━━
```
**Shows BOTH:**
1. ✅ Who affects B (Student_A's vomit)
2. ✅ Who B affects (Student_C)

---

### After Cleaning Mess:
```
━━━━━━━━━━━━━━━━━━━━━━━
Student_B - Critical 🤯
━━━━━━━━━━━━━━━━━━━━━━━
💬 "Cô ơi! Em bị ảnh hưởng..."

📋 Em đang bị ảnh hưởng bởi:
✓ 😷 Bạn Student_A ói, thúi quá!  (✓ RESOLVED!)

⚠️ Đang ảnh hưởng:
• Student_C [✅ Giải quyết cho Student_C]

[Close]
━━━━━━━━━━━━━━━━━━━━━━━
```
**Now shows:**
1. ✅ Student_A's influence is resolved (checkmark)
2. ✅ Student_B still affecting Student_C (unresolved)

---

## 🎯 All Popup Types Comparison

### Type 1: TargetStudent (Pure Victim)
**When:** Student affects NO ONE but is affected by others

**Example:** Student_C (affected by A and B, doesn't affect anyone)

```
Student_C - Distracted 😟
━━━━━━━━━━━━━━━━━━━━━━━
💬 "Cô ơi!"

✓ 😷 Bạn Student_A ói, thúi quá!
  🎯 Bạn Student_B ném đồ vào con!

[Escort Back (Disabled)]
[Close]
```

---

### Type 2: SourceIndividualActions (Pure Aggressor)
**When:** Student affects others but is NOT affected by anyone

**Example:** Student_B (after A's vomit is cleaned and forgotten)

```
Student_B - ActingOut 😤
━━━━━━━━━━━━━━━━━━━━━━━
💬 "Em tức quá cô ơi, nên em đánh bạn Student_C..."

⚠️ Đang ảnh hưởng:
• Student_C [✅ Giải quyết cho Student_C]

[Close]
```

---

### Type 3: SourceAndTarget (Mixed Role) ✨ NEW!
**When:** Student affects others AND is affected by others

**Example:** Student_B (affects C, affected by A's vomit)

```
Student_B - Critical 🤯
━━━━━━━━━━━━━━━━━━━━━━━
💬 "Cô ơi! Em bị ảnh hưởng..."

📋 Em đang bị ảnh hưởng bởi:
  😷 Bạn Student_A ói, thúi quá!

⚠️ Đang ảnh hưởng:
• Student_C [✅ Giải quyết cho Student_C]

[Close]
```

---

### Type 4: SourceWholeClassAction (Whole Class Impact)
**When:** Student affects whole class (e.g., vomit)

**Example:** Student_A (vomits, affects everyone)

```
Student_A - Critical 🤯
━━━━━━━━━━━━━━━━━━━━━━━
💬 "Em xin lỗi cô..."

⚠️ Đang ảnh hưởng cả lớp!

[Giải quyết cho cả lớp]
[Close]
```

---

### Type 5: SourceInfoOnly (No Actions Available)
**When:** Student affects others but event type has no resolve action

**Example:** Student wandering around (informational only)

```
Student_X - Distracted 😟
━━━━━━━━━━━━━━━━━━━━━━━
💬 "Em chỉ đi dạo thôi cô..."

(No action buttons)

[Close]
```

---

## 🧪 Testing Instructions

### Test Case 1: Student_B (Mixed Role)

**Setup:**
1. Start scenario_complex_example
2. Let Student_A vomit
3. Let Student_B throw at Student_C
4. Click Student_B

**Expected Popup:**
```
Student_B - Critical 🤯
━━━━━━━━━━━━━━━━━━━━━━━
💬 "Cô ơi! Em bị ảnh hưởng..."

📋 Em đang bị ảnh hưởng bởi:
  😷 Bạn Student_A ói, thúi quá!

⚠️ Đang ảnh hưởng:
• Student_C [✅ Giải quyết cho Student_C]

[Close]
```

**Verify:**
- ✅ Shows Student_A affecting Student_B
- ✅ Shows Student_B affecting Student_C
- ✅ Has resolve button for Student_C

---

### Test Case 2: After Cleaning Mess

**Continue from Test Case 1:**
1. Clean the vomit mess
2. Click Student_B again

**Expected Popup:**
```
Student_B - Critical 🤯
━━━━━━━━━━━━━━━━━━━━━━━
💬 "Cô ơi! Em bị ảnh hưởng..."

📋 Em đang bị ảnh hưởng bởi:
✓ 😷 Bạn Student_A ói, thúi quá!  (with checkmark!)

⚠️ Đang ảnh hưởng:
• Student_C [✅ Giải quyết cho Student_C]

[Close]
```

**Verify:**
- ✅ Student_A's influence shows with ✓ checkmark
- ✅ Student_B still affecting Student_C (no checkmark)

---

### Test Case 3: Pure Target (Student_C)

**Setup:**
1. Student_A vomits
2. Student_B throws at Student_C
3. Click Student_C

**Expected Popup Type:** TargetStudent (not SourceAndTarget)

```
Student_C - Distracted 😟
━━━━━━━━━━━━━━━━━━━━━━━
💬 "Cô ơi!"

  😷 Bạn Student_A ói, thúi quá!
  🎯 Bạn Student_B ném đồ vào con!

[Escort Back (Disabled)]
[Close]
```

**Verify:**
- ✅ Shows both influences on Student_C
- ✅ No "Đang ảnh hưởng" section (C doesn't affect anyone)
- ✅ Escort button present

---

## 📋 Console Logs to Verify

**When clicking Student_B:**

```
[Popup] DeterminePopupType for Student_B:
[Popup]   - Affects 1 student(s)
[Popup]   - Affected by 1 source(s)
[Popup] Source event type: ThrowingObject
[Popup] → PopupType.SourceAndTarget (affects 1 students AND affected by 1 sources)

[Popup] GenerateSourceAndTargetPopup for Student_B
[Popup] PART 1 - This student is affected by 1 sources
[Popup]   - Affected by: Student_A (MessCreated) [✗ unresolved]
[Popup] PART 2 - This student is affecting 1 students
[Popup]   - Affecting: Student_C
[Popup] Action group: ThrowingObject → 1 targets
[Popup] Creating action button for target: C
```

---

## 🎓 Design Benefits

### 1. Complete Information
- Users see FULL picture of student's situation
- Both incoming influences AND outgoing influences
- No hidden information

### 2. Clear Cause and Effect
- Shows chain of influences: A → B → C
- Users understand WHY B is acting out (because A affected B)
- Makes it clear what needs to be resolved first

### 3. Realistic Classroom Dynamics
- Students can be both victim AND aggressor
- B is upset (victim of A) so B takes it out on C (aggressor)
- Mirrors real classroom behavior

### 4. Better Teacher Decision-Making
- Teacher sees: "Oh, B is acting out because A affected them"
- Strategy: Fix A first → B calms down → Then deal with C
- Clear prioritization of actions

---

## 📁 Files Modified

1. **[StudentInteractionPopup.cs](StudentInteractionPopup.cs)**
   - Lines 9-16: Added `SourceAndTarget` enum value
   - Lines 129-171: Enhanced `DeterminePopupType()` to check both roles
   - Lines 329-400: New `GenerateSourceAndTargetPopup()` method
   - Lines 119-123: Added case handler in switch statement

---

## 🔗 Related Documentation

- [CONCURRENT_INFLUENCES_MECHANISM.md](../CONCURRENT_INFLUENCES_MECHANISM.md) - How multiple influences work
- [POPUP_ESCORT_BUTTON_FIX.md](POPUP_ESCORT_BUTTON_FIX.md) - Escort button logic
- [DE_ESCALATION_MECHANISM.md](DE_ESCALATION_MECHANISM.md) - Resolution system

---

**Last Updated:** 2026-01-19
**Status:** ✅ FIXED - Students with mixed roles now show complete information
