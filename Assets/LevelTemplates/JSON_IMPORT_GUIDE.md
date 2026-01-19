# JSON Level Import - 1-Click Setup Guide

## Automatic Features

Khi import level từ JSON, system sẽ **TỰ ĐỘNG** setup:

### ✅ 1. Student Visual Markers
- **Tự động** add `StudentVisualMarker` cho tất cả students
- Color-coded capsules:
  - 🔴 Student_A = Red
  - 🔵 Student_B = Blue
  - 🟢 Student_C = Green
  - 🟡 Student_D = Yellow
  - 🟠 Student_E = Orange
  - 🟣 Student_F = Purple
  - 🔵 Student_G = Cyan
  - 🩷 Student_H = Pink
- Floating name labels above students
- Labels always face camera

### ✅ 2. Student Interaction Processor
- **Tự động** create `StudentInteractionProcessor` nếu JSON có `studentInteractions`
- **Tự động** load interactions từ JSON
- Ready to trigger student-to-student interactions

## How to Import

### Step 1: Prepare JSON File
Create your level JSON with:
```json
{
  "levelName": "MyLevel",
  "students": [...],
  "studentInteractions": [
    {
      "sourceStudent": "Student_B",
      "targetStudent": "Student_C",
      "eventType": "ThrowingObject",
      "triggerCondition": "Always",
      "probability": 1.0,
      "description": "B hits C"
    }
  ]
}
```

### Step 2: Import via Unity Menu
1. **FunClass → Import Level from JSON**
2. Select your JSON file
3. Click **Open**

### Step 3: Done! 🎉
System automatically:
- ✅ Creates all students with configs
- ✅ Adds StudentVisualMarker to each student (color-coded)
- ✅ Creates StudentInteractionProcessor (if interactions exist)
- ✅ Loads interactions into processor
- ✅ Creates routes (EscapeRoute, ReturnRoute)
- ✅ Sets up classroom door
- ✅ Bakes NavMesh

## What You'll See

### Console Logs
```
[SceneHierarchyBuilder] ✓ Added StudentVisualMarker to Student_A
[SceneHierarchyBuilder] ✓ Added StudentVisualMarker to Student_B
[SceneHierarchyBuilder] ✓ Added StudentVisualMarker to Student_C

[JSONLevelImporter] Setting up StudentInteractionProcessor with 1 interactions
[JSONLevelImporter] ✓ Created StudentInteractionProcessor
[JSONLevelImporter]   - Student_B → Student_C (ThrowingObject, Always, prob: 1)
[JSONLevelImporter] ✓ Loaded 1 interactions into StudentInteractionProcessor
```

### In Scene Hierarchy
```
=== MANAGERS ===
  └─ StudentInteractionProcessor  ← Auto-created!

=== STUDENTS ===
  ├─ Student_Student_A  ← Red capsule + label
  │   └─ StudentVisualMarker  ← Auto-added!
  ├─ Student_Student_B  ← Blue capsule + label
  │   └─ StudentVisualMarker  ← Auto-added!
  └─ Student_Student_C  ← Green capsule + label
      └─ StudentVisualMarker  ← Auto-added!
```

### In Game View
- Students have colored capsules (easy to identify)
- Name labels floating above students
- Labels always face camera

## Example JSON Files

### Simple Level (No Interactions)
```json
{
  "levelName": "Simple_Level",
  "students": [
    {
      "studentName": "Student_A",
      "position": { "x": -1, "y": 0.1, "z": 0 },
      "personality": { ... },
      "behaviors": { ... }
    }
  ]
}
```
**Result:** Students created with visual markers only.

### Complex Level (With Interactions)
```json
{
  "levelName": "Complex_Level",
  "students": [...],
  "studentInteractions": [
    {
      "sourceStudent": "Student_B",
      "targetStudent": "Student_C",
      "eventType": "ThrowingObject",
      "triggerCondition": "Always",
      "probability": 1.0
    }
  ]
}
```
**Result:** Students + visual markers + StudentInteractionProcessor + loaded interactions.

## Testing After Import

### 1. Check Visual Markers
- Press **Play**
- Look at students - should see colored capsules
- Look above students - should see name labels

### 2. Check Interactions (if configured)
- Press **Play**
- Check console for:
```
[StudentInteractionProcessor] Awake - Instance created
[StudentInteractionProcessor] Start - Interactions loaded: 1
[StudentInteractionProcessor] Activated
```

### 3. Trigger Interactions
- Wait for interactions to trigger (every 2 seconds)
- Check console for:
```
[StudentInteractionProcessor] >>> Checking 1 interactions
[StudentInteractionProcessor] Checking: Student_B → Student_C (Always)
[StudentInteractionProcessor]   ✓ All checks passed!
[StudentInteractionProcessor] >>> Triggering: Student_B → Student_C
```

## Troubleshooting

### Issue: Students have no colors
**Solution:** Visual markers are added automatically. If not visible:
1. Check console for: `✓ Added StudentVisualMarker to Student_X`
2. Check student GameObject has `StudentVisualMarker` component
3. Re-import JSON if needed

### Issue: No StudentInteractionProcessor
**Cause:** JSON has no `studentInteractions` section
**Solution:** Add `studentInteractions` to JSON and re-import

### Issue: Interactions not triggering
**Check:**
1. StudentInteractionProcessor exists in scene?
2. Console shows: `Loaded X interactions`?
3. Trigger condition appropriate? (use `"Always"` for testing)
4. Probability high enough? (use `1.0` for testing)

## Advanced: Manual Setup (Not Needed!)

If you want to manually add systems (not recommended):
- **FunClass → Quick Setup → Add Student Systems**

But with JSON import, **everything is automatic!** 🎉

## Benefits of 1-Click Setup

✅ **No manual work** - Import JSON and everything is ready
✅ **Visual differentiation** - Color-coded students, easy to identify
✅ **Interaction system** - Auto-configured from JSON
✅ **Consistent setup** - Same setup every time
✅ **Fast iteration** - Change JSON, re-import, done!

## Summary

**Before:** Manual setup required
- Add StudentVisualMarker to each student manually
- Create StudentInteractionProcessor manually
- Load interactions manually

**Now:** 1-click import
- Import JSON
- Everything auto-configured
- Ready to play!

**Time saved:** ~5-10 minutes per level! 🚀
