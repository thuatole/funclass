# Hệ Thống Modular Editor Scripts - Hướng Dẫn

## 📦 Cấu Trúc Modules

Editor scripts đã được tách thành các modules nhỏ, dễ quản lý:

```
Assets/Scripts/Editor/
├── Modules/
│   ├── EditorUtils.cs              ✅ Utilities dùng chung
│   ├── LevelConfigGenerator.cs     ✅ Tạo Level & Goal configs
│   ├── StudentConfigGenerator.cs   ✅ Tạo Student configs
│   ├── SceneHierarchyBuilder.cs    ✅ Tạo scene hierarchy
│   └── WaypointRouteBuilder.cs     ✅ Tạo waypoints & routes
│
├── FunClassSceneSetup.cs                    // Setup scene hierarchy only
├── FunClassCompleteLevelSetup.cs            // Monolithic version (cũ)
└── FunClassCompleteLevelSetup_Modular.cs    // Modular version (mới) ⭐
```

## 🎯 Lợi Ích Của Modular Design

### **1. Dễ Maintain**
- Mỗi module chỉ làm 1 việc
- Code ngắn gọn, dễ đọc
- Dễ tìm bug

### **2. Dễ Extend**
- Thêm tính năng mới không ảnh hưởng code cũ
- Có thể tạo module mới độc lập

### **3. Reusable**
- Modules có thể dùng lại ở nhiều nơi
- Không cần copy-paste code

### **4. Testable**
- Test từng module riêng biệt
- Dễ debug

## 📚 Chi Tiết Từng Module

### **1. EditorUtils.cs**
**Chức năng:** Utility functions dùng chung

**Methods:**
```csharp
// Tạo hoặc tìm GameObject
GameObject CreateOrFind(string name)

// Tạo child GameObject
GameObject CreateChild(GameObject parent, string name)

// Tạo ScriptableObject
T CreateScriptableObject<T>(string path)

// Tạo folder
void CreateFolderIfNotExists(string path)

// Tạo cấu trúc folder cho level
void CreateLevelFolderStructure(string levelName)

// Xóa group
void DeleteGroup(string name)
```

**Sử dụng:**
```csharp
using FunClass.Editor.Modules;

// Tạo GameObject
GameObject managers = EditorUtils.CreateOrFind("Managers");

// Tạo ScriptableObject
var config = EditorUtils.CreateScriptableObject<StudentConfig>("Assets/Configs/Student.asset");

// Tạo folders
EditorUtils.CreateLevelFolderStructure("Level_01");
```

### **2. LevelConfigGenerator.cs**
**Chức năng:** Tạo LevelConfig và LevelGoalConfig

**Methods:**
```csharp
// Tạo level configs
(LevelGoalConfig goalConfig, LevelConfig levelConfig) 
    CreateLevelConfigs(string levelName, Difficulty difficulty)
```

**Difficulty enum:**
- `Difficulty.Easy`
- `Difficulty.Normal`
- `Difficulty.Hard`

**Sử dụng:**
```csharp
using FunClass.Editor.Modules;

var (goalConfig, levelConfig) = LevelConfigGenerator.CreateLevelConfigs(
    "Level_01", 
    LevelConfigGenerator.Difficulty.Normal
);

// goalConfig và levelConfig đã được tạo và saved
```

### **3. StudentConfigGenerator.cs**
**Chức năng:** Tạo StudentConfig cho học sinh

**Methods:**
```csharp
// Tạo student configs
StudentConfig[] CreateStudentConfigs(
    string levelName, 
    int studentCount, 
    LevelConfigGenerator.Difficulty difficulty
)
```

**Features:**
- Random personality cho mỗi student
- Behaviors dựa trên difficulty
- Tên mặc định: Nam, Lan, Minh, Hoa, Tuan, Mai, Khoa, Linh, Duc, Nga

**Sử dụng:**
```csharp
using FunClass.Editor.Modules;

var configs = StudentConfigGenerator.CreateStudentConfigs(
    "Level_01", 
    5, 
    LevelConfigGenerator.Difficulty.Normal
);

// 5 student configs đã được tạo
```

### **4. SceneHierarchyBuilder.cs**
**Chức năng:** Tạo scene hierarchy

**Methods:**
```csharp
// Tạo managers
GameObject CreateManagersGroup()

// Tạo classroom
GameObject CreateClassroomGroup()

// Tạo students
GameObject CreateStudentsGroup(int studentCount, StudentConfig[] configs, string levelName)

// Tạo teacher
GameObject CreateTeacherGroup()

// Tạo UI
GameObject CreateUIGroup()
```

**Sử dụng:**
```csharp
using FunClass.Editor.Modules;

// Tạo từng group
SceneHierarchyBuilder.CreateManagersGroup();
SceneHierarchyBuilder.CreateClassroomGroup();
SceneHierarchyBuilder.CreateTeacherGroup();
SceneHierarchyBuilder.CreateUIGroup();

// Tạo students với configs
var configs = StudentConfigGenerator.CreateStudentConfigs("Level_01", 5, Difficulty.Normal);
SceneHierarchyBuilder.CreateStudentsGroup(5, configs, "Level_01");
```

### **5. WaypointRouteBuilder.cs**
**Chức năng:** Tạo waypoints và routes

**Methods:**
```csharp
// Tạo escape và return routes
(StudentRoute escapeRoute, StudentRoute returnRoute) 
    CreateRoutes(string levelName)

// Assign routes vào level config
void AssignRoutesToLevelConfig(
    LevelConfig levelConfig, 
    StudentRoute escapeRoute, 
    StudentRoute returnRoute
)
```

**Sử dụng:**
```csharp
using FunClass.Editor.Modules;

// Tạo routes
var (escapeRoute, returnRoute) = WaypointRouteBuilder.CreateRoutes("Level_01");

// Assign vào level config
WaypointRouteBuilder.AssignRoutesToLevelConfig(levelConfig, escapeRoute, returnRoute);
```

## 🚀 Sử Dụng Version Modular

### **Menu Command:**
```
Tools > FunClass > Create Complete Level (Modular)
```

### **Code Flow:**
```csharp
1. EditorUtils.CreateLevelFolderStructure(levelName)
2. SceneHierarchyBuilder.CreateManagersGroup()
3. SceneHierarchyBuilder.CreateClassroomGroup()
4. LevelConfigGenerator.CreateLevelConfigs(levelName, difficulty)
5. StudentConfigGenerator.CreateStudentConfigs(levelName, count, difficulty)
6. SceneHierarchyBuilder.CreateStudentsGroup(count, configs, levelName)
7. WaypointRouteBuilder.CreateRoutes(levelName)
8. WaypointRouteBuilder.AssignRoutesToLevelConfig(...)
9. Save scene
```

## 🔧 Tạo Module Mới

### **Template:**
```csharp
using UnityEngine;
using UnityEditor;

namespace FunClass.Editor.Modules
{
    /// <summary>
    /// Module mô tả chức năng
    /// </summary>
    public static class MyNewModule
    {
        /// <summary>
        /// Method chính
        /// </summary>
        public static void DoSomething(string param)
        {
            // Implementation
        }
    }
}
```

### **Ví dụ: Tạo InteractableObjectGenerator**
```csharp
using UnityEngine;
using UnityEditor;

namespace FunClass.Editor.Modules
{
    public static class InteractableObjectGenerator
    {
        public static void CreateInteractableObjects(GameObject classroom, int count)
        {
            GameObject objectsGroup = EditorUtils.CreateChild(classroom, "InteractableObjects");
            
            for (int i = 0; i < count; i++)
            {
                GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obj.name = $"Object_{i}";
                obj.transform.SetParent(objectsGroup.transform);
                obj.AddComponent<FunClass.Core.StudentInteractableObject>();
            }
        }
    }
}
```

**Sử dụng:**
```csharp
// Trong FunClassCompleteLevelSetup_Modular.cs
InteractableObjectGenerator.CreateInteractableObjects(classroom, 10);
```

## 📊 So Sánh Versions

| Feature | Monolithic | Modular |
|---------|-----------|---------|
| **File size** | ~600 lines | ~150 lines (main) + modules |
| **Maintainability** | Khó | Dễ |
| **Reusability** | Không | Cao |
| **Testability** | Khó | Dễ |
| **Extensibility** | Khó | Dễ |

## 💡 Best Practices

### **1. Một module, một trách nhiệm**
```csharp
// ✅ Good
public static class StudentConfigGenerator
{
    public static StudentConfig[] CreateStudentConfigs(...) { }
}

// ❌ Bad - làm quá nhiều việc
public static class EverythingGenerator
{
    public static void CreateEverything(...) { }
}
```

### **2. Static classes cho utility modules**
```csharp
// ✅ Good - không cần instance
public static class EditorUtils
{
    public static GameObject CreateOrFind(string name) { }
}

// ❌ Bad - không cần instance nhưng vẫn tạo class
public class EditorUtils
{
    public GameObject CreateOrFind(string name) { }
}
```

### **3. Return values cho reusability**
```csharp
// ✅ Good - return config để có thể dùng tiếp
public static StudentConfig CreateStudentConfig(...)
{
    var config = CreateScriptableObject<StudentConfig>(...);
    return config;
}

// ❌ Bad - không return, khó dùng lại
public static void CreateStudentConfig(...)
{
    var config = CreateScriptableObject<StudentConfig>(...);
    // Không return
}
```

### **4. Namespace organization**
```csharp
// ✅ Good
namespace FunClass.Editor.Modules
{
    public static class MyModule { }
}

// ❌ Bad - không có namespace riêng
namespace FunClass.Editor
{
    public static class MyModule { }
}
```

## 🔍 Debugging Modules

### **Enable debug logs:**
```csharp
public static class LevelConfigGenerator
{
    private static bool debugMode = true;
    
    public static void CreateLevelConfigs(...)
    {
        if (debugMode) Debug.Log("[LevelConfigGenerator] Creating configs...");
        // ...
    }
}
```

### **Test individual modules:**
```csharp
[MenuItem("Tools/FunClass/Test/Test Student Config Generator")]
public static void TestStudentConfigGenerator()
{
    var configs = StudentConfigGenerator.CreateStudentConfigs(
        "Test_Level", 
        3, 
        LevelConfigGenerator.Difficulty.Easy
    );
    
    Debug.Log($"Created {configs.Length} configs");
}
```

## 📝 Tóm Tắt

### **Khi nào dùng Modular version:**
✅ Khi cần customize từng bước
✅ Khi muốn tạo variations của level setup
✅ Khi cần debug từng phần riêng
✅ Khi muốn extend thêm features

### **Khi nào dùng Monolithic version:**
✅ Khi chỉ cần tạo level nhanh
✅ Khi không cần customize
✅ Khi không cần hiểu chi tiết

### **Modules có sẵn:**
- ✅ EditorUtils - Utilities
- ✅ LevelConfigGenerator - Level configs
- ✅ StudentConfigGenerator - Student configs
- ✅ SceneHierarchyBuilder - Scene hierarchy
- ✅ WaypointRouteBuilder - Waypoints & routes

**Bắt đầu:** `Tools > FunClass > Create Complete Level (Modular)` 🚀
