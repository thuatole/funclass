# Popup System Bug Fixes V2 - January 19, 2026

## 🐛 Additional Bugs Found After Testing

### Bug #1: Student Highlight Turns Bright Green/Neon (CRITICAL)
**Status:** 🟢 FIXED

**Symptoms:**
- When clicking Student_B, the capsule mesh turns bright neon green instead of yellow highlight
- Original red color completely lost

**Root Cause:**
In [StudentHighlight.cs:56-58](../StudentHighlight.cs#L56-L58):
```csharp
// BEFORE (WRONG):
Color brightColor = originalMaterials[i].color * 1.5f;
brightColor.a = 1f;
renderers[i].material.color = brightColor;
```

**Problem:**
- Multiplying color by 1.5f doesn't create proper highlight
- If material is red (1, 0, 0) × 1.5 = (1.5, 0, 0) → clamped to (1, 0, 0) → still red
- If material already has some green component, × 1.5 amplifies it → bright green
- This approach changes the BASE color instead of adding highlight glow

**Fix Applied:**
```csharp
// AFTER (CORRECT):
if (highlight)
{
    Material mat = renderers[i].material;

    // Add emission glow for highlight
    mat.SetColor("_EmissionColor", highlightColor * 2f);
    mat.EnableKeyword("_EMISSION");

    // Tint base color slightly toward yellow
    Color baseColor = originalMaterials[i].color;
    Color highlightedColor = Color.Lerp(baseColor, Color.yellow, 0.3f);
    mat.color = highlightedColor;
}
else
{
    // Restore original
    Material mat = renderers[i].material;
    mat.SetColor("_EmissionColor", Color.black);
    mat.DisableKeyword("_EMISSION");
    mat.color = originalMaterials[i].color;
}
```

**Benefits:**
- Preserves original color (red capsule stays reddish)
- Adds yellow emission glow for visibility
- Uses proper shader properties (_EmissionColor)
- Smooth color blend (30% toward yellow) instead of multiplication

---

### Bug #2: Popup Content Shows Wrong Data (CRITICAL)
**Status:** 🔄 INVESTIGATING

**Symptoms from Screenshots:**

**Student_B (Critical state):**
- Shows: "Em xin lỗi cô..."
- Shows: "Đang ảnh hưởng:" (warning message)
- Shows targets: "• Student_C", "• Student_A", "• Student_C" (DUPLICATE!)
- Shows green action buttons for each

**Expected for Student_B:**
According to scenario, Student_B is:
- AFFECTING Student_C (via ThrowingObject)
- AFFECTED BY Student_A (via MessCreated/vomit)

**So popup should show:**
- PopupType: SourceIndividualActions (because B affects C)
- Opening: "Em tức quá cô ơi, nên em đánh bạn Student_C..."
- Impact: "⚠️ Đang ảnh hưởng:"
- Targets: "• Student_C" with button "✅ Giải quyết cho Student_C"

**Student_C (ActingOut state):**
- Shows: "Em xin lỗi cô..."
- Shows: "Đang ảnh hưởng:"
- Shows targets: "• Student_B", "• Student_A"
- Shows green action buttons

**Expected for Student_C:**
According to scenario, Student_C is:
- AFFECTED BY Student_A (via MessCreated/vomit)
- AFFECTED BY Student_B (via ThrowingObject)
- NOT affecting anyone else

**So popup should show:**
- PopupType: TargetStudent (because C doesn't affect anyone)
- Opening: "Cô ơi!"
- Complaints:
  - "😷 Bạn Student_A ói, thúi quá!"
  - "🎯 Bạn Student_B ném đồ vào con!"
- Escort button (disabled)

---

### Possible Causes:

#### Theory 1: Logic is Inverted
The `DeterminePopupType()` logic might be checking wrong direction:
```csharp
// Current logic:
var affectedStudents = GetAffectedStudents(student);  // Who THIS student affects

if (affectedStudents.Count == 0) {
    return PopupType.TargetStudent;  // Should show who affects THIS student
}
```

This logic seems CORRECT, but something is displaying wrong content.

#### Theory 2: GetAffectedStudents() Returns Wrong Data
The function loops through all students and checks:
```csharp
foreach (var influenceSource in activeSources)
{
    if (!influenceSource.isResolved && influenceSource.sourceStudent == source)
    {
        affectedStudents.Add(student);  // Add THIS student if SOURCE affects them
    }
}
```

This logic is CORRECT:
- For Student_B: should return [Student_C] ✅
- For Student_C: should return [] ✅

#### Theory 3: Duplicate Target Bug
Screenshot shows "Student_C" appearing TWICE in Student_B's popup.

Possible cause in `GroupTargetsByActionType()`:
```csharp
foreach (var influenceSource in activeSources)
{
    if (!influenceSource.isResolved && influenceSource.sourceStudent == source)
    {
        string actionType = influenceSource.eventType.ToString();

        if (!grouped[actionType].Contains(target))  // Check prevents duplicates
        {
            grouped[actionType].Add(target);
        }
    }
}
```

**BUT:** If Student_C has 2 influence sources from Student_B (different event types), they would be in different groups!

Example:
- Student_B → Student_C (ThrowingObject)
- Student_B → Student_C (MakingNoise)

This would create 2 action groups, each showing Student_C once.

---

### Debug Steps Added:

**Added comprehensive logging to track data flow:**

1. In `GenerateTargetStudentPopup()`:
```csharp
Debug.Log($"[Popup] GenerateTargetStudentPopup for {student.Config?.studentName}");
Debug.Log($"[Popup] This student is affected by {influenceSources.Count} sources");
foreach (var src in influenceSources)
{
    Debug.Log($"[Popup]   - Affected by: {src.sourceStudent?.Config?.studentName} ({src.eventType})");
}
```

2. In `GenerateSourceIndividualActionsPopup()`:
```csharp
Debug.Log($"[Popup] GenerateSourceIndividualActionsPopup for {student.Config?.studentName}");
Debug.Log($"[Popup] This student is affecting {affectedStudents.Count} students");

foreach (var actionGroup in groupedByAction)
{
    Debug.Log($"[Popup] Action group: {actionType} → {targets.Count} targets");
    foreach (var t in targets)
    {
        Debug.Log($"[Popup]   - Target: {t.Config?.studentName}");
    }
}

foreach (var target in targets)
{
    Debug.Log($"[Popup] Creating action button for target: {targetName}");
    CreateTargetActionItemWithButton(...);
}
```

---

### Next Steps:

**CRITICAL: Need console logs from user to diagnose!**

When testing, please:
1. Start the game with scenario_complex_example.json
2. Open Unity Console (Ctrl+Shift+C)
3. Click Student_B → Copy all console logs
4. Click Student_C → Copy all console logs
5. Send logs to verify:
   - What `DeterminePopupType()` returns
   - What `GetAffectedStudents()` returns
   - What `GetInfluenceSources()` returns
   - Which popup generation method is called

**Expected Logs for Student_B:**
```
[Popup] DeterminePopupType for Student_B: affectedStudents.Count = 1
[Popup] Source event type: ThrowingObject
[Popup] → PopupType.SourceIndividualActions (individual actions for 1 students)
[Popup] GenerateSourceIndividualActionsPopup for Student_B
[Popup] This student is affecting 1 students
[Popup] Action group: ThrowingObject → 1 targets
[Popup]   - Target: Student_C
[Popup] Creating action button for target: Student_C
```

**Expected Logs for Student_C:**
```
[Popup] DeterminePopupType for Student_C: affectedStudents.Count = 0
[Popup] → PopupType.TargetStudent (no one affected by this student)
[Popup] GenerateTargetStudentPopup for Student_C
[Popup] This student is affected by 2 sources
[Popup]   - Affected by: Student_A (MessCreated)
[Popup]   - Affected by: Student_B (ThrowingObject)
```

---

### Hypothesis:

Based on screenshots showing:
- "Em xin lỗi cô..." (default opening phrase)
- Target lists instead of complaint lists

**I suspect the popup is showing the WRONG popup type!**

Possible causes:
1. `DeterminePopupType()` returns wrong type
2. Switch statement calls wrong generation method
3. Content from previous popup is cached

**Test to verify:**
Look for this in console logs:
```
[Popup] Student_B → PopupType.SourceIndividualActions
```
vs
```
[Popup] Student_B → PopupType.TargetStudent  // WRONG!
```

---

## 📝 Files Modified (V2):

1. **[StudentHighlight.cs](../StudentHighlight.cs)** - Fixed highlight color calculation (lines 42-67)
2. **[StudentInteractionPopup.cs](../StudentInteractionPopup.cs)** - Added debug logging (lines 161-203, 238-275)

---

## ✅ Testing Instructions:

1. **Test Student Highlight:**
   - Hover over Student_B → Should show yellow glow + slight color tint
   - Capsule should NOT turn bright green
   - Original red color should be preserved

2. **Test Popup Content:**
   - Click Student_B → Check console logs
   - Click Student_C → Check console logs
   - Compare logs with expected values above
   - Take screenshot of console + popup

3. **Send Results:**
   - Console logs (text)
   - Popup screenshots
   - Verify which popup type is actually generated

---

## 🔄 Updates After Further Investigation:

### Bug #2 Root Cause Identified and Fixed:
**Status:** 🟢 FIXED

**Root Cause:** WanderingAround and LeftSeat were set as influence triggers in `StudentInfluenceManager.cs`, creating resolvable influence sources that shouldn't exist.

**Fix Applied:** Modified [StudentInfluenceManager.cs:182-195](../StudentInfluenceManager.cs#L182-L195)
```csharp
private bool IsInfluenceTrigger(StudentEventType eventType)
{
    return eventType switch
    {
        StudentEventType.WanderingAround => false,  // Changed from true
        StudentEventType.LeftSeat => false,         // Changed from true
        // ... other event types
    };
}
```

**Result:**
- Student_B now correctly shows only Student_C as affected (via ThrowingObject)
- Student_C now correctly shows as TargetStudent with complaints about Student_A and Student_B
- No more duplicate targets in action lists

---

### Calm Button: REVERTED

After discussion, the Calm button added to TargetStudent popup was **removed**.

**Reasoning:**
- Resolving source problems (clean mess, calm troublemakers) should be sufficient
- The system already has a comprehensive de-escalation mechanism (see [DE_ESCALATION_MECHANISM.md](DE_ESCALATION_MECHANISM.md))
- Target students can be escorted once all their influence sources are resolved
- Teacher actions (Calm, Escort) will de-escalate the target students when needed

**Changes Reverted:**
- Removed Calm button from `GenerateTargetStudentPopup()` (line ~202-205)
- Removed `CalmStudent()` method (line ~646-659)

---

**Last Updated:** 2026-01-19 (V2 - Final)
**Status:** 🟢 All bugs fixed, system working as designed
