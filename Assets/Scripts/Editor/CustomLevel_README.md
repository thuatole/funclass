# Custom Level Designer - Hướng Dẫn Đầy Đủ

## 🎨 2 Phương Pháp Tạo Custom Level

### **Phương Pháp 1: UI Editor Window** (Dễ dùng, trực quan)
### **Phương Pháp 2: JSON Import** (Mạnh mẽ, có thể version control)

---

## 🖥️ PHƯƠNG PHÁP 1: UI EDITOR WINDOW

### **Mở Editor:**
```
Tools > FunClass > Custom Level Designer
```

### **Giao Diện:**

**5 Tabs:**
1. **General** - Cài đặt level và goals
2. **Students** - Thêm/sửa students
3. **Routes** - Tạo waypoints và routes
4. **Prefabs** - Thêm prefabs vào scene
5. **Import/Export** - Import/Export JSON

### **Tab 1: General**

**Level Settings:**
- Level Name: Tên màn chơi
- Difficulty: Easy/Normal/Hard

**Goal Settings:**
- Max Disruption: Ngưỡng disruption tối đa
- Catastrophic Disruption: Disruption instant lose
- Max Critical Students: Số học sinh critical tối đa
- Time Limit: Thời gian giới hạn (giây)
- Required Problems: Số vấn đề cần giải quyết
- Star Thresholds: Điểm cho 1/2/3 sao

**Ví dụ:**
```
Level Name: "MyCustomLevel"
Difficulty: "Normal"
Max Disruption: 80
Time Limit: 300 (5 phút)
Required Problems: 5
1 Star: 100 points
2 Stars: 250 points
3 Stars: 500 points
```

### **Tab 2: Students**

**Thêm Student:**
1. Nhập tên student
2. Chọn vị trí (Vector3)
3. Click "Add Student"

**Quick Add:**
- Click "Quick Add 5 Students (Grid)"
- Tự động tạo 5 students với tên mặc định
- Positioned in grid layout

**Edit Student:**
- Click "Edit" để chỉnh sửa chi tiết
- Personality: patience, attention span, impulsiveness
- Behaviors: can stand up, can throw objects, etc.

**Ví dụ:**
```
Name: "Nam"
Position: (-2, 0, 0)
→ Click "Add Student"

Name: "Lan"
Position: (0, 0, 0)
→ Click "Add Student"
```

### **Tab 3: Routes**

**Tạo Custom Route:**
1. Nhập route name
2. Add waypoints (Vector3 positions)
3. Click "Create Route"

**Quick Create:**
- **Escape Route** - Click button → tạo escape route mặc định
- **Return Route** - Click button → tạo return route mặc định

**Waypoints:**
- Mỗi route cần ít nhất 2 waypoints
- Có thể add/remove waypoints
- Position cho mỗi waypoint

**Ví dụ:**
```
Route Name: "CustomWander"
Waypoints:
  WP 0: (0, 0, 0)
  WP 1: (3, 0, 2)
  WP 2: (6, 0, 0)
  WP 3: (3, 0, -2)
→ Click "Create Route"
```

### **Tab 4: Prefabs**

**Thêm Prefab:**
1. Nhập prefab name
2. Chọn type (Student/Furniture/Interactable/Decoration)
3. Chọn position
4. Click "Add Prefab"

**Prefab Types:**
- **Student** - Capsule với StudentAgent
- **Furniture** - Cube (bàn, ghế, tủ)
- **Interactable** - Sphere với StudentInteractableObject
- **Decoration** - Cube (trang trí)

**Ví dụ:**
```
Name: "Desk_01"
Type: "Furniture"
Position: (-2, 0, -1)
→ Click "Add Prefab"
```

### **Tab 5: Import/Export**

**Import:**
1. Click "Select JSON File to Import"
2. Chọn file .json
3. Data được load vào editor

**Export:**
1. Click "Export Current Level Data"
2. Chọn nơi save
3. File .json được tạo

**Sample Template:**
- Click "Create Sample JSON Template"
- Tạo file mẫu để tham khảo
- Có thể edit và import lại

### **Tạo Level:**

**Sau khi setup xong:**
1. Click "CREATE LEVEL" (nút xanh lớn)
2. Chờ progress bar
3. Level được tạo hoàn chỉnh!

**Kết quả:**
- Scene mới với hierarchy đầy đủ
- Configs được tạo trong Assets/Configs/
- Students positioned theo data
- Routes và waypoints ready
- Prefabs placed in scene

---

## 📄 PHƯƠNG PHÁP 2: JSON IMPORT

### **Tại sao dùng JSON?**

✅ **Version Control** - Commit vào Git
✅ **Collaboration** - Share với team
✅ **Batch Creation** - Tạo nhiều levels nhanh
✅ **Procedural Generation** - Generate từ code
✅ **Easy Editing** - Edit trong text editor
✅ **Backup** - Dễ backup và restore

### **JSON Schema:**

```json
{
  "levelName": "MyLevel",
  "difficulty": "Normal",
  "goalSettings": { ... },
  "students": [ ... ],
  "routes": [ ... ],
  "prefabs": [ ... ],
  "environment": { ... }
}
```

### **Sample JSON File:**

`@c:\Users\thuat\funclass\Assets\LevelTemplates\SampleLevel.json`

**Cấu trúc đầy đủ với:**
- 3 students (Nam, Lan, Minh)
- 2 routes (Escape, Return)
- 1 prefab (Desk)
- Goal settings
- Environment data

### **Tạo Level từ JSON:**

**Cách 1: Qua UI Editor**
```
1. Tools > FunClass > Custom Level Designer
2. Tab "Import/Export"
3. Click "Select JSON File to Import"
4. Chọn file .json
5. Click "CREATE LEVEL"
```

**Cách 2: Qua Code**
```csharp
using FunClass.Editor.Modules;

var data = JSONLevelImporter.ImportFromJSON("Assets/LevelTemplates/MyLevel.json");
JSONLevelImporter.CreateLevelFromData(data);
```

**Cách 3: Menu Command**
```
Tools > FunClass > Import Level from JSON
(Có thể thêm menu này nếu cần)
```

### **Edit JSON File:**

**Dùng bất kỳ text editor:**
- Visual Studio Code
- Notepad++
- Unity's built-in editor

**Tips:**
- Dùng JSON formatter để format đẹp
- Validate JSON trước khi import
- Copy từ SampleLevel.json làm template

### **JSON Fields Chi Tiết:**

**Level Settings:**
```json
{
  "levelName": "Level_01",
  "difficulty": "Normal"
}
```

**Goal Settings:**
```json
{
  "goalSettings": {
    "maxDisruptionThreshold": 80.0,
    "catastrophicDisruptionLevel": 95.0,
    "maxAllowedCriticalStudents": 2,
    "timeLimitSeconds": 300.0,
    "requiredResolvedProblems": 5,
    "oneStarScore": 100,
    "twoStarScore": 250,
    "threeStarScore": 500
  }
}
```

**Student:**
```json
{
  "studentName": "Nam",
  "position": { "x": -2.0, "y": 0.0, "z": 0.0 },
  "personality": {
    "patience": 0.5,
    "attentionSpan": 0.6,
    "impulsiveness": 0.4,
    "influenceSusceptibility": 0.7,
    "influenceResistance": 0.2,
    "panicThreshold": 0.7
  },
  "behaviors": {
    "canFidget": true,
    "canStandUp": true,
    "canThrowObjects": false,
    "minIdleTime": 2.0,
    "maxIdleTime": 8.0
  }
}
```

**Route:**
```json
{
  "routeName": "EscapeRoute",
  "routeType": "Escape",
  "waypoints": [
    {
      "waypointName": "Escape_0",
      "position": { "x": 0.0, "y": 0.0, "z": 0.0 },
      "waitDuration": 0.0
    },
    {
      "waypointName": "Escape_1",
      "position": { "x": 10.0, "y": 0.0, "z": 0.0 },
      "waitDuration": 0.0
    }
  ],
  "movementSpeed": 4.0,
  "isRunning": true
}
```

**Prefab:**
```json
{
  "prefabName": "Desk_01",
  "prefabType": "Furniture",
  "position": { "x": -2.0, "y": 0.0, "z": -1.0 },
  "rotation": { "x": 0.0, "y": 0.0, "z": 0.0 },
  "scale": { "x": 1.0, "y": 1.0, "z": 1.0 },
  "prefabPath": "Assets/Prefabs/Desk.prefab"
}
```

---

## 🔄 Workflow Đề Xuất

### **Workflow 1: UI Editor (Prototyping)**

```
1. Mở Custom Level Designer
2. Setup general settings
3. Quick add students
4. Quick create routes
5. Add prefabs nếu cần
6. Click CREATE LEVEL
7. Test trong Unity
8. Export to JSON để save
```

**Ưu điểm:**
- Nhanh cho prototyping
- Trực quan
- Không cần biết JSON

### **Workflow 2: JSON First (Production)**

```
1. Copy SampleLevel.json
2. Rename thành MyLevel.json
3. Edit trong text editor
4. Adjust students, routes, goals
5. Import vào Unity
6. Test
7. Tweak JSON
8. Re-import
```

**Ưu điểm:**
- Version control friendly
- Dễ duplicate levels
- Có thể procedural generate

### **Workflow 3: Hybrid (Best)**

```
1. Tạo base level bằng UI Editor
2. Export to JSON
3. Commit JSON vào Git
4. Team members edit JSON
5. Import JSON updates
6. Fine-tune trong UI Editor
7. Export lại
```

**Ưu điểm:**
- Kết hợp cả 2 phương pháp
- Flexible
- Team collaboration

---

## 📦 Modules Mới

### **1. LevelDataSchema.cs**
`@c:\Users\thuat\funclass\Assets\Scripts\Editor\Data\LevelDataSchema.cs`

**Classes:**
- `LevelDataSchema` - Root schema
- `LevelGoalData` - Goal settings
- `StudentData` - Student info
- `PersonalityData` - Personality traits
- `BehaviorData` - Behavior flags
- `RouteData` - Route definition
- `WaypointData` - Waypoint info
- `PrefabData` - Prefab placement
- `EnvironmentData` - Environment settings
- `Vector3Data` - Serializable Vector3

**Serializable cho JSON:**
```csharp
[Serializable]
public class LevelDataSchema
{
    public string levelName;
    public List<StudentData> students;
    // ...
}
```

### **2. JSONLevelImporter.cs**
`@c:\Users\thuat\funclass\Assets\Scripts\Editor\Modules\JSONLevelImporter.cs`

**Methods:**
- `ImportFromJSON(string path)` - Load JSON
- `ExportToJSON(LevelDataSchema data, string path)` - Save JSON
- `CreateLevelFromData(LevelDataSchema data)` - Tạo level từ data

**Usage:**
```csharp
// Import
var data = JSONLevelImporter.ImportFromJSON("path/to/level.json");

// Create level
JSONLevelImporter.CreateLevelFromData(data);

// Export
JSONLevelImporter.ExportToJSON(data, "path/to/save.json");
```

### **3. PrefabGenerator.cs**
`@c:\Users\thuat\funclass\Assets\Scripts\Editor\Modules\PrefabGenerator.cs`

**Methods:**
- `CreatePrefabsFromData(List<PrefabData>)` - Tạo từ data
- `CreatePrefabInstance(PrefabData)` - Tạo instance
- `CreatePrefabFromGameObject(GameObject, string)` - Save as prefab
- `CreatePrefabVariant(GameObject, string)` - Tạo variant
- `ExportGameObjectToPrefabData(GameObject)` - Export to data

**Menu Commands:**
```
Tools > FunClass > Prefabs > Create Prefabs from Selection
```

**Usage:**
```csharp
// Tạo prefab từ selection
PrefabGenerator.CreatePrefabsFromSelection();

// Tạo từ data
var prefabData = new PrefabData { ... };
PrefabGenerator.CreatePrefabInstance(prefabData);
```

### **4. CustomLevelDesigner.cs**
`@c:\Users\thuat\funclass\Assets\Scripts\Editor\CustomLevelDesigner.cs`

**Editor Window với 5 tabs:**
- General settings
- Students management
- Routes creation
- Prefabs placement
- Import/Export

**Menu:**
```
Tools > FunClass > Custom Level Designer
```

---

## 💡 Use Cases

### **Use Case 1: Tạo Tutorial Level**

**UI Editor:**
```
1. Level Name: "Tutorial"
2. Difficulty: "Easy"
3. Quick Add 2 Students
4. Time Limit: 600s (10 phút)
5. No escape routes
6. CREATE LEVEL
```

### **Use Case 2: Tạo Boss Level**

**JSON:**
```json
{
  "levelName": "Boss_Final",
  "difficulty": "Hard",
  "goalSettings": {
    "maxDisruptionThreshold": 60.0,
    "timeLimitSeconds": 120.0,
    "catastrophicCriticalStudents": 2
  },
  "students": [
    // 10 students với high impulsiveness
  ]
}
```

### **Use Case 3: Procedural Generation**

**Code:**
```csharp
LevelDataSchema GenerateRandomLevel(int difficulty)
{
    var data = new LevelDataSchema();
    data.levelName = $"Random_{Random.Range(1000, 9999)}";
    
    // Generate random students
    for (int i = 0; i < difficulty * 2; i++)
    {
        data.students.Add(GenerateRandomStudent());
    }
    
    return data;
}

// Use
var randomLevel = GenerateRandomLevel(3);
JSONLevelImporter.CreateLevelFromData(randomLevel);
```

### **Use Case 4: Level Variations**

**JSON Template:**
```
SampleLevel.json (base)
├── SampleLevel_Easy.json (less students)
├── SampleLevel_Hard.json (more students)
└── SampleLevel_Timed.json (shorter time)
```

**Batch import:**
```csharp
string[] levels = {
    "SampleLevel_Easy.json",
    "SampleLevel_Hard.json",
    "SampleLevel_Timed.json"
};

foreach (var level in levels)
{
    var data = JSONLevelImporter.ImportFromJSON($"Assets/LevelTemplates/{level}");
    JSONLevelImporter.CreateLevelFromData(data);
}
```

---

## 🎯 So Sánh 2 Phương Pháp

| Feature | UI Editor | JSON Import |
|---------|-----------|-------------|
| **Ease of Use** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ |
| **Speed (single level)** | ⭐⭐⭐⭐ | ⭐⭐⭐ |
| **Speed (batch)** | ⭐⭐ | ⭐⭐⭐⭐⭐ |
| **Version Control** | ⭐⭐ | ⭐⭐⭐⭐⭐ |
| **Collaboration** | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| **Procedural Gen** | ⭐ | ⭐⭐⭐⭐⭐ |
| **Flexibility** | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| **Learning Curve** | Dễ | Trung bình |

---

## 🔧 Tips & Tricks

### **Tip 1: Combine Both Methods**
```
1. Prototype trong UI Editor
2. Export to JSON
3. Edit JSON cho variations
4. Import variations
```

### **Tip 2: JSON Templates Library**
```
Assets/LevelTemplates/
├── Easy_Template.json
├── Normal_Template.json
├── Hard_Template.json
├── Boss_Template.json
└── Tutorial_Template.json
```

### **Tip 3: Validation**
```csharp
// Validate trước khi import
bool ValidateJSON(string json)
{
    try {
        var data = JsonUtility.FromJson<LevelDataSchema>(json);
        return data != null && !string.IsNullOrEmpty(data.levelName);
    } catch {
        return false;
    }
}
```

### **Tip 4: Auto-backup**
```csharp
// Export backup mỗi khi create level
void CreateLevelWithBackup(LevelDataSchema data)
{
    // Create level
    JSONLevelImporter.CreateLevelFromData(data);
    
    // Auto backup
    string backupPath = $"Assets/Backups/{data.levelName}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
    JSONLevelImporter.ExportToJSON(data, backupPath);
}
```

---

## ✅ Tóm Tắt

### **Bạn có thể:**

✅ **Tạo custom levels** qua UI hoặc JSON
✅ **Import/Export** level data
✅ **Version control** levels với Git
✅ **Collaborate** với team qua JSON
✅ **Generate** levels procedurally
✅ **Create prefabs** từ selection
✅ **Customize** mọi aspect của level

### **Bắt đầu:**

**UI Editor:**
```
Tools > FunClass > Custom Level Designer
```

**JSON Import:**
```
1. Edit Assets/LevelTemplates/SampleLevel.json
2. Tools > FunClass > Custom Level Designer
3. Tab "Import/Export" > Import
4. CREATE LEVEL
```

🎉 **Bây giờ bạn có full control để tạo bất kỳ level nào!**
