# Level Validation System - Hướng Dẫn

## 🎯 Tổng Quan

**LevelValidator** tự động kiểm tra level sau khi generate để đảm bảo:
- ✅ Routes hợp lệ
- ✅ Students có seat
- ✅ Goals hợp lý
- ✅ References không null

---

## 🔍 Validation Checks

### **1. Goals Validation**

**Kiểm tra:**
- Disruption thresholds (0-100%)
- Critical students limits
- Outside students limits
- Time limit (>= 0)
- Star score progression (1★ < 2★ < 3★)

### **2. Routes Validation**

**Kiểm tra:**
- Escape route exists
- Return route exists
- Available routes list
- Movement speed > 0
- Rotation speed > 0
- Minimum 2 waypoints per route
- Door reference assigned

### **3. Sequences Validation** ⭐ EXPANDED

**Kiểm tra:**
- Sequences assigned to LevelConfig
- Each sequence has valid ID
- Entry state not None
- Steps exist (minimum 1)
- Each step validated:
  - Required student state valid
  - Resulting reaction valid
  - State change logic correct
  - Step description present
- Total steps count

**Ví dụ validation:**
```
ℹ️ Checking sequences...
ℹ️   ✓ Sequence 'simple_warning': 1 steps
ℹ️   ✓ Sequence 'escalating_behavior': 3 steps
ℹ️   ✓ Sequence 'object_confiscation': 2 steps
ℹ️ ✓ 5 sequences validated (15 total steps)
```

### **4. Interactables Validation** ⭐ NEW

**Kiểm tra:**
- InteractableObjects group exists
- Each object has StudentInteractableObject component
- Each object has Collider
- Each object has Renderer (visibility)
- Objects positioned correctly (not all at origin)

**Ví dụ validation:**
```
ℹ️ Checking interactable objects...
ℹ️ ✓ 5/5 interactables validated
⚠️ 1 objects missing Collider
⚠️ Interactable 'Phone_01' at origin position
```

**Ví dụ lỗi:**
```
❌ No valid interactable objects found
⚠️ Interactable 'Ball' missing StudentInteractableObject component
⚠️ Interactable 'Book' missing Renderer
```

### **5. Mess System Validation** ⭐ NEW

**Kiểm tra:**
- Mess prefabs exist (6 types):
  - VomitMess
  - SpillMess
  - TrashMess
  - StainMess
  - BrokenGlassMess
  - TornPaperMess
- Each prefab has MessObject/VomitMess component
- Each prefab has Collider
- VomitMess instances in scene (if any)
- MessObject instances in scene (if any)
- ClassroomManager available for mess handling

**Ví dụ validation:**
```
ℹ️ Checking mess system...
ℹ️ ✓ 6/6 mess prefabs found
ℹ️ ✓ ClassroomManager available for mess handling
```

**Ví dụ warning:**
```
⚠️ No mess prefabs found (may not be generated yet)
⚠️ 2 mess prefab types missing
⚠️ VomitMess prefab missing Collider
```

### **6. Students Validation**

**Ví dụ lỗi:**
```
❌ Invalid maxDisruptionThreshold: 150 (should be 0-100)
❌ twoStarScore (100) must be higher than oneStarScore (150)
```

**Ví dụ warning:**
```
⚠️ catastrophicDisruptionLevel (85) should be higher than maxDisruptionThreshold (90)
```

### **2. Routes Validation**

**Kiểm tra:**
- Escape route exists
- Return route exists
- Movement speed > 0
- Rotation speed > 0
- Minimum 2 waypoints per route
- Door reference assigned

**Ví dụ lỗi:**
```
❌ Escape route is null
❌ EscapeRoute has only 1 waypoints (minimum 2 required)
❌ Return route has invalid movement speed: 0
```

**Ví dụ info:**
```
ℹ️ ✓ Escape route: 3 waypoints, speed 4
ℹ️ ✓ Return route: 3 waypoints, speed 2
ℹ️ ✓ Door reference: Door
```

### **3. Students Validation**

**Kiểm tra:**
- Students group exists
- Each student has StudentAgent component
- Each student has config assigned
- Students positioned (not all at origin)

**Ví dụ lỗi:**
```
❌ Students group not found in scene
❌ 3 students missing StudentAgent component
```

**Ví dụ warning:**
```
⚠️ 2 students missing config
⚠️ 1 students at origin position
```

**Ví dụ info:**
```
ℹ️ ✓ 8 students validated
```

### **4. References Validation**

**Kiểm tra:**
- ClassroomManager exists
- LevelConfig assigned to ClassroomManager
- TeacherController exists
- All required managers exist:
  - GameStateManager
  - LevelManager
  - StudentEventManager
  - TeacherScoreManager

**Ví dụ lỗi:**
```
❌ ClassroomManager not found in scene
❌ TeacherController not found in scene
```

**Ví dụ warning:**
```
⚠️ LevelConfig not assigned to ClassroomManager
⚠️ StudentEventManager not found in scene
```

### **5. Scene Hierarchy Validation**

**Kiểm tra required groups:**
- `=== MANAGERS ===`
- `=== CLASSROOM ===`
- `=== STUDENTS ===`
- `=== TEACHER ===`
- `=== UI ===`

**Ví dụ lỗi:**
```
❌ Required group not found: === STUDENTS ===
```

---

## 🚀 Cách Sử Dụng

### **Automatic Validation (Sau khi Generate)**

Khi dùng `FullAutoLevelGenerator`, validation tự động chạy:

```
Tools > FunClass > Generate Complete Level
→ Generate level
→ Auto-validate
→ Show results
```

**Dialog hiển thị:**
```
Level Generated - ✅ VALIDATED

Level 'MyLevel' generated successfully!

Difficulty: Normal
Students: 8
Routes: 2
Interactables: Yes
Mess Prefabs: Yes
Sequences: Yes

Validation: Errors: 0, Warnings: 2

Check Console for detailed validation report.
```

### **Manual Validation**

Validate level hiện tại bất kỳ lúc nào:

```
Tools > FunClass > Validate Current Level
```

**Kết quả trong Console:**
```
=== LEVEL VALIDATION REPORT ===

✅ VALIDATION PASSED

WARNINGS (2):
  ⚠️ catastrophicDisruptionLevel (85) should be higher than maxDisruptionThreshold (80)
  ⚠️ 1 students at origin position

INFO (15):
  ℹ️ Validating level: MyLevel_Config
  ℹ️ Checking goals...
  ℹ️ ✓ Goals validated: 80% disruption, 300s time limit
  ℹ️ Checking routes...
  ℹ️ ✓ Escape route: 3 waypoints, speed 4
  ℹ️ ✓ Return route: 3 waypoints, speed 2
  ℹ️ ✓ Door reference: Door
  ℹ️ Checking students...
  ℹ️ ✓ 8 students validated
  ℹ️ Checking references...
  ℹ️ ✓ ClassroomManager has LevelConfig
  ℹ️ ✓ TeacherController found
  ℹ️ ✓ GameStateManager found
  ℹ️ ✓ LevelManager found
  ℹ️ Checking scene hierarchy...
```

### **Code Usage**

```csharp
using FunClass.Editor.Modules;

// Validate level
var result = LevelValidator.ValidateLevel(levelConfig);

// Check result
if (result.isValid)
{
    Debug.Log("Level is valid!");
}
else
{
    Debug.LogError($"Level has {result.errors.Count} errors");
}

// Get detailed report
string report = result.GetReport();
Debug.Log(report);

// Access specific issues
foreach (var error in result.errors)
{
    Debug.LogError(error);
}

foreach (var warning in result.warnings)
{
    Debug.LogWarning(warning);
}
```

---

## 📊 Validation Result Structure

```csharp
public class ValidationResult
{
    public bool isValid;                    // Overall pass/fail
    public List<string> errors;             // Critical issues
    public List<string> warnings;           // Non-critical issues
    public List<string> info;               // Informational messages
    
    public string GetReport();              // Full formatted report
}
```

**Status:**
- `isValid = true` → No errors (warnings OK)
- `isValid = false` → Has errors (must fix)

---

## 🔧 Common Issues & Fixes

### **Issue 1: Routes null**

**Error:**
```
❌ Escape route is null
```

**Fix:**
```csharp
// Regenerate routes
var routes = WaypointRouteBuilder.CreateDefaultRoutes(levelName);
levelConfig.escapeRoute = routes[0];
levelConfig.returnRoute = routes[1];
```

### **Issue 2: Students missing config**

**Error:**
```
⚠️ 3 students missing config
```

**Fix:**
```csharp
// Regenerate students with configs
var studentConfigs = StudentGenerator.GenerateStudents(levelName, difficulty);
SceneHierarchyBuilder.CreateStudents(studentConfigs);
```

### **Issue 3: Invalid goal thresholds**

**Error:**
```
❌ twoStarScore (100) must be higher than oneStarScore (150)
```

**Fix:**
```csharp
// Correct star thresholds
goalConfig.oneStarScore = 100;
goalConfig.twoStarScore = 250;
goalConfig.threeStarScore = 500;
EditorUtility.SetDirty(goalConfig);
```

### **Issue 4: Missing managers**

**Error:**
```
❌ ClassroomManager not found in scene
```

**Fix:**
```csharp
// Recreate hierarchy
SceneHierarchyBuilder.CreateCompleteHierarchy();
```

### **Issue 5: LevelConfig not assigned**

**Warning:**
```
⚠️ LevelConfig not assigned to ClassroomManager
```

**Fix:**
```csharp
var classroomManager = FindObjectOfType<ClassroomManager>();
var so = new SerializedObject(classroomManager);
so.FindProperty("levelConfig").objectReferenceValue = levelConfig;
so.ApplyModifiedProperties();
```

---

## 📋 Validation Checklist

Sau khi generate level, đảm bảo:

### **Goals:**
- [ ] Max disruption: 0-100%
- [ ] Catastrophic > Max
- [ ] Time limit >= 0
- [ ] Star scores: 1★ < 2★ < 3★

### **Routes:**
- [ ] Escape route exists
- [ ] Return route exists
- [ ] Each route has >= 2 waypoints
- [ ] Movement speed > 0
- [ ] Door reference assigned

### **Students:**
- [ ] Students group exists
- [ ] All students have StudentAgent
- [ ] All students have config
- [ ] Students positioned correctly

### **References:**
- [ ] ClassroomManager exists
- [ ] LevelConfig assigned
- [ ] TeacherController exists
- [ ] All managers present

### **Hierarchy:**
- [ ] MANAGERS group
- [ ] CLASSROOM group
- [ ] STUDENTS group
- [ ] TEACHER group
- [ ] UI group

---

## 💡 Best Practices

### **1. Always Validate After Changes**

```
Make changes → Save → Validate
```

### **2. Fix Errors Before Warnings**

Errors = Must fix
Warnings = Should fix

### **3. Check Console for Details**

Dialog shows summary, Console shows full report

### **4. Use Auto-Validation**

Let `FullAutoLevelGenerator` validate automatically

### **5. Validate Before Play**

```
Validate → Fix issues → Play test
```

---

## 🎯 Integration với Workflow

### **Workflow 1: Generate + Validate**

```
1. Tools > FunClass > Generate Complete Level
2. Auto-validation runs
3. Check results in dialog
4. Read detailed report in Console
5. Fix any issues
6. Re-validate if needed
```

### **Workflow 2: Manual Edit + Validate**

```
1. Edit level configs manually
2. Tools > FunClass > Validate Current Level
3. Check validation report
4. Fix issues
5. Validate again
6. Play test
```

### **Workflow 3: Continuous Validation**

```
While editing:
  → Make change
  → Validate
  → Fix if needed
  → Repeat
```

---

## 📊 Validation Statistics

**Typical validation time:** ~0.5 seconds

**Checks performed:** 20+ validation rules

**Categories:**
- Goals: 10 checks
- Routes: 8 checks
- Students: 4 checks
- References: 6 checks
- Hierarchy: 5 checks

---

## ✅ Tóm Tắt

### **Validation đảm bảo:**

✅ **Routes hợp lệ**
- Escape + Return routes exist
- Minimum 2 waypoints each
- Valid speeds
- Door reference

✅ **Students có seat**
- All have StudentAgent
- All have configs
- Positioned correctly

✅ **Goals hợp lý**
- Valid thresholds
- Proper progression
- Realistic time limits

✅ **References không null**
- All managers present
- LevelConfig assigned
- Scene hierarchy complete

### **Menu Commands:**

```
Tools > FunClass > Generate Complete Level (auto-validates)
Tools > FunClass > Validate Current Level (manual)
```

### **Result:**

```
✅ VALIDATED → Ready to play!
⚠️ HAS ISSUES → Check Console, fix issues
```

🎉 **Level validation hoàn toàn tự động!**
