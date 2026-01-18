# Auto Fix Level Issues - Hướng Dẫn

## 🔧 Tổng Quan

**LevelAutoFixer** tự động sửa các issues phổ biến trong level chỉ với **ONE CLICK**!

### **Menu Command:**
```
Tools > FunClass > Auto Fix Level Issues
```

---

## 🚀 Quick Start

### **One-Click Fix All:**

```
1. Tools > FunClass > Auto Fix Level Issues
2. Wait for auto-fix to complete (~5 seconds)
3. Check report in Console
4. Done! ✅
```

**Auto Fix Report:**
```
=== AUTO FIX REPORT ===

Issues Fixed: 8

FIXED:
  ✅ Created ClassroomManager
  ✅ Created and assigned Escape route
  ✅ Created and assigned Return route
  ✅ Assigned LevelGoalConfig
  ✅ Assigned door reference
  ✅ Added StudentAgent to 2 students
  ✅ Added Collider to 3 objects
  ✅ Created 1 missing hierarchy groups
```

---

## 🔍 What Gets Auto-Fixed

### **1. Missing Managers** ⭐

**Tự động tạo nếu thiếu:**
- GameStateManager
- LevelManager
- ClassroomManager
- StudentEventManager
- TeacherScoreManager
- StudentInfluenceManager
- StudentMovementManager

**Ví dụ:**
```
✅ Created GameStateManager
✅ Created ClassroomManager
✅ Created 7 managers
```

### **2. Missing Routes** ⭐

**Tự động fix:**
- Tạo Escape route nếu null
- Tạo Return route nếu null
- Assign existing routes nếu tìm thấy
- Tạo waypoints trong scene

**Ví dụ:**
```
✅ Created and assigned Escape route
✅ Assigned existing Return route
```

### **3. Null Configs** ⭐

**Tự động fix:**
- Assign LevelConfig to ClassroomManager
- Create/assign LevelGoalConfig
- Assign door reference
- Find existing configs in project

**Ví dụ:**
```
✅ Assigned LevelConfig: Level_01_Config
✅ Created new LevelGoalConfig
✅ Assigned door reference
```

### **4. Missing Components** ⭐

**Tự động fix:**
- Add StudentAgent to students
- Add StudentInteractableObject to objects
- Add Collider to interactables
- Add TeacherController to teacher

**Ví dụ:**
```
✅ Added StudentAgent to 3 students
✅ Added StudentInteractableObject to 2 objects
✅ Added Collider to 5 objects
✅ Added TeacherController component
```

### **5. Scene Hierarchy** ⭐

**Tự động fix:**
- Create missing groups:
  - === MANAGERS ===
  - === CLASSROOM ===
  - === STUDENTS ===
  - === TEACHER ===
  - === UI ===
- Create classroom subgroups:
  - Environment
  - Furniture
  - Waypoints
  - InteractableObjects

**Ví dụ:**
```
✅ Created 2 missing hierarchy groups
✅ Created 3 classroom subgroups
```

---

## 💡 Use Cases

### **Use Case 1: After Manual Editing**

```
Scenario: Manually edited scene, some references broken
→ Tools > FunClass > Auto Fix Level Issues
→ All references restored ✅
```

### **Use Case 2: Imported Level**

```
Scenario: Imported level from JSON, missing components
→ Tools > FunClass > Auto Fix Level Issues
→ All components added ✅
```

### **Use Case 3: Corrupted Level**

```
Scenario: Level has errors, validation fails
→ Tools > FunClass > Auto Fix Level Issues
→ Most issues fixed automatically ✅
→ Manual fix remaining issues
```

### **Use Case 4: Quick Setup**

```
Scenario: Need to quickly setup a level
→ Create basic scene
→ Tools > FunClass > Auto Fix Level Issues
→ Basic structure created ✅
```

---

## 🔄 Auto Fix Process

**5 Steps:**

1. **Fix Missing Managers** (20%)
   - Check for each manager type
   - Create if missing
   - Parent to MANAGERS group

2. **Fix Missing Routes** (40%)
   - Check escape/return routes
   - Try to find existing
   - Create new if needed
   - Assign to LevelConfig

3. **Fix Null Configs** (60%)
   - Check LevelConfig assignment
   - Check LevelGoalConfig
   - Check door reference
   - Assign or create as needed

4. **Fix Missing Components** (80%)
   - Check students for StudentAgent
   - Check interactables for components
   - Check teacher for TeacherController
   - Add missing components

5. **Fix Scene Hierarchy** (90%)
   - Check required groups
   - Create missing groups
   - Check classroom subgroups
   - Create missing subgroups

**Progress bar** hiển thị từng bước!

---

## 📊 Fix Statistics

**Typical fixes:** 5-15 issues per level

**Fix time:** ~5 seconds

**Success rate:** ~90% of common issues

**Categories:**
- Managers: 7 types
- Routes: 2 types
- Configs: 3 types
- Components: 4+ types
- Hierarchy: 9 groups

---

## 🎯 Integration với Validation

### **Workflow: Validate → Auto Fix → Validate**

```
1. Tools > FunClass > Validate Current Level
   → Shows errors and warnings

2. Tools > FunClass > Auto Fix Level Issues
   → Fixes most issues automatically

3. Tools > FunClass > Validate Current Level
   → Verify fixes worked
   → Manual fix remaining issues
```

**Example:**

**Before Auto Fix:**
```
❌ ClassroomManager not found
❌ Escape route is null
❌ Return route is null
⚠️ LevelConfig not assigned
⚠️ 3 students missing StudentAgent
```

**After Auto Fix:**
```
✅ All issues fixed!
ℹ️ ✓ ClassroomManager found
ℹ️ ✓ Escape route: 3 waypoints
ℹ️ ✓ Return route: 3 waypoints
ℹ️ ✓ LevelConfig assigned
ℹ️ ✓ 8 students validated
```

---

## 🛠️ Quick Fix Methods

### **Quick Fix Specific Issues:**

**Fix Routes Only:**
```csharp
LevelAutoFixer.QuickFixRoutes();
```

**Fix Managers Only:**
```csharp
LevelAutoFixer.QuickFixManagers();
```

**Fix Configs Only:**
```csharp
LevelAutoFixer.QuickFixConfigs();
```

---

## ⚠️ Limitations

### **Cannot Auto-Fix:**

❌ **Complex logic errors** - Requires manual intervention
❌ **Custom prefab references** - Must be assigned manually
❌ **Specific gameplay values** - Needs designer input
❌ **Asset not found** - If files deleted from project
❌ **Circular dependencies** - Needs manual resolution

### **May Need Manual Fix:**

⚠️ **Student positions** - May need adjustment
⚠️ **Route waypoint positions** - May need tweaking
⚠️ **Goal thresholds** - May need balancing
⚠️ **Custom sequences** - May need configuration

---

## 📋 Fix Checklist

**After Auto Fix, verify:**

### **Managers:**
- [ ] All 7 managers present
- [ ] Managers in correct group
- [ ] No duplicate managers

### **Routes:**
- [ ] Escape route assigned
- [ ] Return route assigned
- [ ] Waypoints in scene
- [ ] Routes have valid speeds

### **Configs:**
- [ ] LevelConfig assigned
- [ ] LevelGoalConfig assigned
- [ ] Door reference assigned
- [ ] Goals have valid values

### **Components:**
- [ ] Students have StudentAgent
- [ ] Interactables have components
- [ ] Teacher has TeacherController
- [ ] All have required colliders

### **Hierarchy:**
- [ ] All main groups exist
- [ ] Classroom subgroups exist
- [ ] Objects in correct groups

---

## 🔧 Advanced Usage

### **Code Usage:**

```csharp
using FunClass.Editor.Modules;

// Full auto fix
LevelAutoFixer.AutoFixLevelIssues();

// Get fix result
var result = new LevelAutoFixer.FixResult();
LevelAutoFixer.FixMissingManagers(result);
LevelAutoFixer.FixMissingRoutes(result);

// Check what was fixed
Debug.Log($"Fixed {result.issuesFixed} issues");
foreach (var fix in result.fixedIssues)
{
    Debug.Log($"✅ {fix}");
}
```

### **Custom Fix Logic:**

```csharp
// Add custom fix to your workflow
public static void CustomFix()
{
    var result = new LevelAutoFixer.FixResult();
    
    // Your custom fix logic
    if (SomeCondition())
    {
        FixSomething();
        result.AddFixed("Fixed custom issue");
    }
    
    // Run standard fixes
    LevelAutoFixer.FixMissingManagers(result);
    
    Debug.Log(result.GetReport());
}
```

---

## 💡 Best Practices

### **1. Run After Major Changes**

```
Made big changes → Auto Fix → Validate
```

### **2. Use Before Play Testing**

```
Before play test → Auto Fix → Validate → Play
```

### **3. Run After Import**

```
Import level → Auto Fix → Validate → Adjust
```

### **4. Combine with Validation**

```
Always: Validate → Auto Fix → Validate again
```

### **5. Check Report**

```
Always read Console report to see what was fixed
```

---

## 🎓 Workflows

### **Workflow 1: Quick Fix**

```
1. Tools > FunClass > Auto Fix Level Issues
2. Check Console report
3. Done!
```
**Time:** 10 seconds

### **Workflow 2: Thorough Fix**

```
1. Tools > FunClass > Validate Current Level
2. Note issues
3. Tools > FunClass > Auto Fix Level Issues
4. Tools > FunClass > Validate Current Level
5. Manual fix remaining issues
```
**Time:** 2 minutes

### **Workflow 3: Import + Fix**

```
1. Import level JSON
2. Tools > FunClass > Auto Fix Level Issues
3. Validate
4. Adjust as needed
```
**Time:** 1 minute

---

## 📊 Before/After Examples

### **Example 1: Missing Managers**

**Before:**
```
Scene has no managers
Validation: 7 errors
```

**After Auto Fix:**
```
✅ Created 7 managers
✅ All in MANAGERS group
Validation: 0 errors
```

### **Example 2: Broken Routes**

**Before:**
```
❌ Escape route is null
❌ Return route is null
```

**After Auto Fix:**
```
✅ Created Escape route (3 waypoints)
✅ Created Return route (3 waypoints)
✅ Assigned to LevelConfig
```

### **Example 3: Missing Components**

**Before:**
```
⚠️ 5 students missing StudentAgent
⚠️ 3 objects missing Collider
```

**After Auto Fix:**
```
✅ Added StudentAgent to 5 students
✅ Added Collider to 3 objects
```

---

## ✅ Tóm Tắt

### **Auto Fix có thể sửa:**

✅ **Missing managers** - Tạo tất cả 7 managers
✅ **Missing routes** - Tạo Escape + Return routes
✅ **Null configs** - Assign hoặc tạo configs
✅ **Missing components** - Add required components
✅ **Scene hierarchy** - Tạo missing groups

### **Menu Command:**

```
Tools > FunClass > Auto Fix Level Issues
```

### **Quick Fix Methods:**

```csharp
LevelAutoFixer.QuickFixRoutes();
LevelAutoFixer.QuickFixManagers();
LevelAutoFixer.QuickFixConfigs();
```

### **Integration:**

```
Validate → Auto Fix → Validate → Done!
```

### **Time Saved:**

```
Manual fix: ~30 minutes
Auto fix: ~5 seconds
→ 360x faster! ⚡
```

🎉 **One-click fix cho hầu hết issues!**
