# FunClass Quick Scene Setup - Hướng Dẫn

## 🚀 Cách Sử Dụng Nhanh

### **Phương Pháp 1: Menu Tools (NHANH NHẤT)**

1. Mở Unity Editor
2. Click menu: **Tools > FunClass > Setup Scene**
3. Click "Yes" để confirm
4. ✅ **XONG!** - Toàn bộ scene hierarchy đã được tạo tự động

### **Phương Pháp 2: ScriptableObject Config**

Tạo config files một lần, sau đó reuse cho nhiều level:

1. Right-click trong Project window
2. **Create > FunClass > Level Config**
3. Assign vào LevelManager
4. Done!

## 📋 Menu Commands Có Sẵn

### **Tools > FunClass > Setup Scene**
Tạo toàn bộ scene hierarchy tự động:
- ✅ Managers (7 managers)
- ✅ Classroom (Environment, Furniture, Waypoints)
- ✅ Students (5 students mặc định)
- ✅ Teacher (với camera)
- ✅ UI (Canvas với các elements)

### **Tools > FunClass > Clear Scene**
Xóa toàn bộ FunClass objects để reset scene

### **Tools > FunClass > Setup Prefab Variants**
Tạo folder Prefabs để lưu prefab variants

## 🎯 Scene Hierarchy Được Tạo

```
=== MANAGERS ===
├── GameStateManager
├── LevelManager
├── ClassroomManager
├── StudentEventManager
├── TeacherScoreManager
├── StudentInfluenceManager
└── StudentMovementManager

=== CLASSROOM ===
├── Environment
│   ├── Floor
│   ├── Walls
│   ├── Ceiling
│   ├── Door
│   └── Windows
├── Furniture
│   ├── TeacherDesk
│   ├── Whiteboard
│   └── StudentDesks
└── Waypoints
    ├── EscapeRoute
    ├── ReturnRoute
    └── WanderRoutes

=== STUDENTS ===
├── Student_1 (StudentAgent + Capsule)
├── Student_2 (StudentAgent + Capsule)
├── Student_3 (StudentAgent + Capsule)
├── Student_4 (StudentAgent + Capsule)
└── Student_5 (StudentAgent + Capsule)

=== TEACHER ===
└── Teacher (TeacherController)
    ├── TeacherCamera (Camera)
    └── Visual (Capsule)

=== UI ===
└── Canvas
    ├── InteractionPrompt
    ├── DisruptionMeter
    ├── ScoreDisplay
    └── TimerDisplay
```

## ⚙️ Workflow Được Đề Xuất

### **Lần Đầu Setup Project:**

1. **Tạo Scene Hierarchy:**
   ```
   Tools > FunClass > Setup Scene
   ```

2. **Tạo ScriptableObject Configs:**
   ```
   Right-click > Create > FunClass > Student Config
   Right-click > Create > FunClass > Level Config
   Right-click > Create > FunClass > Level Goal Config
   ```

3. **Assign Configs:**
   - Kéo StudentConfig vào từng Student trong hierarchy
   - Kéo LevelConfig vào LevelManager
   - Kéo LevelGoalConfig vào LevelConfig

4. **Tạo Prefabs:**
   ```
   Tools > FunClass > Setup Prefab Variants
   ```
   - Kéo Student_1 vào Assets/Prefabs → Tạo Student prefab
   - Kéo các objects khác vào để tạo prefabs

### **Tạo Level Mới:**

1. **Duplicate Scene:**
   - Duplicate scene hiện tại
   - Hoặc chạy `Setup Scene` lại

2. **Tạo Level Config Mới:**
   ```
   Right-click > Create > FunClass > Level Config
   ```

3. **Customize:**
   - Thay đổi số lượng students
   - Adjust waypoints
   - Configure goals

## 🔧 Customization Sau Khi Setup

### **Thêm Students:**
```csharp
// Trong Unity Editor:
1. Duplicate Student_1
2. Rename thành Student_6
3. Assign StudentConfig khác
```

### **Thay Đổi Visual:**
```csharp
// Replace Capsule placeholder:
1. Delete "Visual" child object
2. Kéo 3D model vào làm child
3. Adjust position/scale
```

### **Setup Waypoints:**
```csharp
1. Click vào EscapeRoute
2. Add component: StudentRoute (ScriptableObject)
3. Add StudentWaypoint components vào các empty objects
4. Assign waypoints vào route
```

## 📦 Alternative: JSON Import/Export

Nếu bạn muốn save/load scene config từ file:

### **Export Scene Config:**
```csharp
[MenuItem("Tools/FunClass/Export Scene Config")]
public static void ExportSceneConfig()
{
    SceneConfig config = new SceneConfig();
    // Collect all objects...
    string json = JsonUtility.ToJson(config, true);
    File.WriteAllText("Assets/scene_config.json", json);
}
```

### **Import Scene Config:**
```csharp
[MenuItem("Tools/FunClass/Import Scene Config")]
public static void ImportSceneConfig()
{
    string json = File.ReadAllText("Assets/scene_config.json");
    SceneConfig config = JsonUtility.FromJson<SceneConfig>(json);
    // Create objects from config...
}
```

## 🎨 Prefab Workflow

### **Tạo Prefab Variants:**

1. **Base Student Prefab:**
   ```
   - Kéo Student_1 vào Assets/Prefabs/
   - Rename: StudentBase.prefab
   ```

2. **Tạo Variants:**
   ```
   - Right-click StudentBase.prefab
   - Create > Prefab Variant
   - Rename: StudentCalm.prefab, StudentDistracted.prefab, etc.
   ```

3. **Customize Variants:**
   - Mỗi variant có StudentConfig khác nhau
   - Khác nhau về visual, animations, etc.

### **Sử Dụng Prefabs:**
```csharp
// Trong scene:
1. Delete Student_1 đến Student_5
2. Kéo prefab variants vào scene
3. Position theo ý muốn
```

## 🚀 Advanced: Editor Window

Nếu muốn UI window thay vì menu:

```csharp
[MenuItem("Tools/FunClass/Scene Setup Window")]
public static void ShowWindow()
{
    GetWindow<FunClassSceneSetupWindow>("FunClass Setup");
}

public class FunClassSceneSetupWindow : EditorWindow
{
    int studentCount = 5;
    
    void OnGUI()
    {
        GUILayout.Label("Scene Setup", EditorStyles.boldLabel);
        
        studentCount = EditorGUILayout.IntField("Student Count", studentCount);
        
        if (GUILayout.Button("Create Scene"))
        {
            CreateSceneWithStudents(studentCount);
        }
    }
}
```

## 📝 Tips & Tricks

### **Nhanh Hơn Nữa:**

1. **Keyboard Shortcuts:**
   ```
   Ctrl+Shift+S - Setup Scene (custom shortcut)
   ```

2. **Template Scenes:**
   - Save scene đã setup làm template
   - Duplicate template khi cần level mới

3. **Prefab Nesting:**
   - Classroom environment → 1 prefab
   - Student group → 1 prefab
   - Kéo vào scene là xong

### **Batch Operations:**

```csharp
// Select multiple students:
1. Shift+Click để select nhiều
2. Inspector > Add Component (apply to all)
3. Hoặc dùng script để batch assign configs
```

## 🎯 So Sánh Tốc Độ

| Phương Pháp | Thời Gian | Độ Chính Xác |
|-------------|-----------|--------------|
| **Manual (tay)** | ~30 phút | 70% (dễ sai) |
| **Menu Setup** | ~10 giây | 100% |
| **Prefab Template** | ~5 giây | 100% |
| **JSON Import** | ~3 giây | 100% |

## ✅ Checklist Sau Khi Setup

- [ ] Tất cả Managers có trong scene
- [ ] StudentAgent có StudentConfig assigned
- [ ] TeacherController có Camera reference
- [ ] LevelManager có LevelConfig assigned
- [ ] Waypoints đã được tạo và assigned vào routes
- [ ] UI Canvas có EventSystem
- [ ] Main Camera tagged đúng

## 🔍 Troubleshooting

**Lỗi: "Type not found"**
- Đảm bảo tất cả scripts đã compile
- Restart Unity Editor

**Lỗi: "Prefab connection lost"**
- Revert prefab về base
- Apply overrides lại

**Scene quá lag:**
- Giảm số lượng students
- Optimize visual models
- Disable debug gizmos

## 📚 Tài Liệu Liên Quan

- Unity Editor Scripting: https://docs.unity3d.com/Manual/editor-EditorWindows.html
- Prefab Variants: https://docs.unity3d.com/Manual/PrefabVariants.html
- ScriptableObjects: https://docs.unity3d.com/Manual/class-ScriptableObject.html

---

**Tóm lại:** Chỉ cần chạy `Tools > FunClass > Setup Scene` là có ngay toàn bộ hierarchy! 🎉
