# One-Click Level Generation - Hướng Dẫn Hoàn Chỉnh

## 🎯 Tổng Quan

Hệ thống **Full Auto Level Generator** cho phép tạo **complete playable level** chỉ với **ONE CLICK**!

### **Menu Command:**
```
Tools > FunClass > Generate Complete Level
```

---

## 🚀 Quick Start (30 Giây)

### **Cách 1: Quick Generate Buttons**

```
1. Tools > FunClass > Generate Complete Level
2. Click "Quick Normal" button
3. Done! ✅
```

→ Level hoàn chỉnh được tạo trong ~20 giây!

### **Cách 2: Custom Settings**

```
1. Tools > FunClass > Generate Complete Level
2. Nhập Level Name: "MyLevel"
3. Chọn Difficulty: Normal
4. Click "GENERATE COMPLETE LEVEL"
5. Done! ✅
```

---

## 📦 Modules Đã Tích Hợp

Hệ thống sử dụng **7 modules** để tạo level:

### **1. SceneHierarchyBuilder**
- Tạo scene hierarchy đầy đủ
- Managers, Classroom, UI groups

### **2. LevelGoalGenerator** ⭐ NEW
- Tạo LevelGoalConfig theo difficulty
- Tutorial/Boss presets
- Custom goal parameters

### **3. StudentGenerator** ⭐ NEW
- Tạo StudentConfig với 5 archetypes
- Auto-distribution theo difficulty
- Personality & behaviors configured

### **4. WaypointRouteBuilder**
- Tạo Escape & Return routes
- Waypoints positioned
- Route configs

### **5. InteractableObjectGenerator**
- 10 loại objects (Book, Ball, Phone, etc.)
- Auto-placement trong classroom
- StudentInteractableObject component

### **6. MessPrefabGenerator**
- 6 loại mess (Vomit, Spill, Trash, etc.)
- Visual representations
- MessObject components

### **7. SequenceGenerator**
- 7 sequence templates
- Pre-configured interactions
- Difficulty-based selection

---

## 🎨 Giao Diện Editor Window

### **Basic Settings:**
- **Level Name** - Tên level
- **Difficulty** - Easy/Normal/Hard

### **Quick Presets:**
- **Tutorial Level** - 3 students, easy goals, no mess
- **Boss Level** - 12 students, strict goals, all features

### **Generation Options:**
- ✅ Generate Routes
- ✅ Generate Interactables
- ✅ Generate Mess Prefabs
- ✅ Generate Sequences

### **Advanced Options:**
- Custom Student Count (0 = auto)
- Custom Interactable Count (0 = auto)

### **Preview:**
Hiển thị tóm tắt level sẽ được tạo

### **Action Buttons:**
- **GENERATE COMPLETE LEVEL** - Main button
- **Quick Easy/Normal/Hard** - One-click presets

---

## 📊 Difficulty Levels

### **Easy:**
- **Students:** 5 (mostly well-behaved)
- **Interactables:** 3
- **Sequences:** 3
- **Goals:**
  - Max Disruption: 90
  - Time Limit: 600s (10 min)
  - Required Problems: 3
  - Stars: 50/150/300

### **Normal:**
- **Students:** 8 (mixed behavior)
- **Interactables:** 5
- **Sequences:** 5
- **Goals:**
  - Max Disruption: 80
  - Time Limit: 300s (5 min)
  - Required Problems: 5
  - Stars: 100/250/500

### **Hard:**
- **Students:** 10 (mostly troublemakers)
- **Interactables:** 8
- **Sequences:** 8
- **Goals:**
  - Max Disruption: 60
  - Time Limit: 180s (3 min)
  - Required Problems: 8
  - Stars: 150/400/800

### **Tutorial:**
- **Students:** 3 (all well-behaved)
- **Interactables:** 2
- **Sequences:** 3
- **Goals:**
  - Max Disruption: 100
  - Time Limit: None
  - Required Problems: 1
  - Stars: 10/50/100

### **Boss:**
- **Students:** 12 (hyperactive + troublemakers)
- **Interactables:** 10
- **Sequences:** 8
- **Goals:**
  - Max Disruption: 40
  - Time Limit: 120s (2 min)
  - Required Problems: 12
  - Stars: 200/600/1200

---

## 🔄 Generation Process

### **Step-by-Step:**

1. **Scene Hierarchy** (10%)
   - Create manager groups
   - Create classroom structure
   - Create UI groups

2. **Level Goals** (20%)
   - Generate LevelGoalConfig
   - Configure thresholds
   - Set star requirements

3. **Students** (30%)
   - Generate StudentConfigs
   - Assign archetypes
   - Configure personalities

4. **Routes** (40%)
   - Create waypoints
   - Generate Escape route
   - Generate Return route

5. **Level Config** (50%)
   - Create LevelConfig
   - Link all configs
   - Set references

6. **Interactables** (60%)
   - Create objects
   - Position in classroom
   - Add components

7. **Mess Prefabs** (70%)
   - Generate prefabs
   - Save to Assets
   - Configure visuals

8. **Sequences** (80%)
   - Create interaction sequences
   - Configure steps
   - Save as assets

9. **Student GameObjects** (90%)
   - Instantiate in scene
   - Position in grid
   - Assign configs

10. **Assign to Managers** (95%)
    - Link LevelConfig to ClassroomManager
    - Save scene
    - Refresh assets

11. **Complete!** (100%)

---

## 💡 Use Cases

### **Use Case 1: Rapid Prototyping**

```
Cần test gameplay nhanh?
→ Click "Quick Normal"
→ Test ngay trong 30 giây!
```

### **Use Case 2: Level Series**

```csharp
// Tạo 10 levels với difficulty tăng dần
for (int i = 1; i <= 10; i++)
{
    string levelName = $"Level_{i:D2}";
    var difficulty = i <= 3 ? Difficulty.Easy : 
                     i <= 7 ? Difficulty.Normal : 
                     Difficulty.Hard;
    
    // Generate via code
    GenerateLevel(levelName, difficulty);
}
```

### **Use Case 3: Tutorial Sequence**

```
1. Click "Tutorial Level" preset
2. Level Name: "Tutorial_01"
3. GENERATE
→ Perfect tutorial level!
```

### **Use Case 4: Boss Challenge**

```
1. Click "Boss Level" preset
2. Level Name: "Boss_Final"
3. GENERATE
→ Extreme challenge level!
```

### **Use Case 5: Custom Mix**

```
1. Level Name: "Custom_Mix"
2. Difficulty: Normal
3. Advanced Options:
   - Custom Student Count: 6
   - Custom Interactable Count: 10
4. Uncheck "Generate Mess Prefabs"
5. GENERATE
→ Custom configured level!
```

---

## 📁 Generated Assets Structure

```
Assets/
├── Configs/
│   └── [LevelName]/
│       ├── [LevelName]_GoalConfig.asset
│       ├── [LevelName]_Config.asset
│       ├── Students/
│       │   ├── Nam_WellBehaved.asset
│       │   ├── Lan_Average.asset
│       │   ├── Minh_Troublemaker.asset
│       │   └── ...
│       ├── Routes/
│       │   ├── EscapeRoute.asset
│       │   └── ReturnRoute.asset
│       └── Sequences/
│           ├── SimpleWarning.asset
│           ├── ObjectConfiscation.asset
│           └── ...
├── Prefabs/
│   └── [LevelName]/
│       └── Mess/
│           ├── VomitMess.prefab
│           ├── SpillMess.prefab
│           └── ...
└── Scenes/
    └── [LevelName].unity
```

---

## 🎯 Student Archetypes

### **1. WellBehaved (Học sinh ngoan)**
- **Patience:** 0.7-0.9
- **Attention Span:** 0.7-0.9
- **Impulsiveness:** 0.1-0.3
- **Behaviors:** Minimal (fidget, look around only)
- **Idle Time:** 5-15s

### **2. Average (Trung bình)**
- **Patience:** 0.4-0.6
- **Attention Span:** 0.5-0.7
- **Impulsiveness:** 0.3-0.5
- **Behaviors:** Moderate (can stand, drop items)
- **Idle Time:** 3-10s

### **3. Mischievous (Nghịch ngợm)**
- **Patience:** 0.3-0.5
- **Attention Span:** 0.3-0.5
- **Impulsiveness:** 0.5-0.7
- **Behaviors:** Many (can move, knock objects)
- **Idle Time:** 2-7s

### **4. Troublemaker (Gây rối)**
- **Patience:** 0.1-0.3
- **Attention Span:** 0.2-0.4
- **Impulsiveness:** 0.7-0.9
- **Behaviors:** Almost all (can throw objects)
- **Idle Time:** 1-5s

### **5. Hyperactive (Hiếu động)**
- **Patience:** 0.05-0.2
- **Attention Span:** 0.1-0.3
- **Impulsiveness:** 0.8-0.95
- **Behaviors:** All enabled
- **Idle Time:** 0.5-3s

---

## 🔧 Advanced Features

### **Custom Student Distribution:**

```csharp
// Trong StudentGenerator.cs
// Easy: 60% WellBehaved, 30% Average, 10% Mischievous
// Normal: 30% WellBehaved, 30% Average, 25% Mischievous, 15% Troublemaker
// Hard: 20% Average, 30% Mischievous, 30% Troublemaker, 20% Hyperactive
```

### **Goal Scaling:**

```csharp
// Tutorial: No time limit, minimal requirements
// Easy: Lenient thresholds, 10 min
// Normal: Moderate thresholds, 5 min
// Hard: Strict thresholds, 3 min
// Boss: Extreme thresholds, 2 min
```

### **Sequence Selection:**

```csharp
// Easy: SimpleWarning, ObjectConfiscation, MessCleanup
// Normal: + EscalatingBehavior, OutsideRecall
// Hard: + PeerInfluence, ComplexIntervention
```

---

## 📊 Performance

| Task | Manual | One-Click |
|------|--------|-----------|
| **Scene setup** | ~10 min | ~2 sec |
| **Goal config** | ~5 min | ~1 sec |
| **5-10 students** | ~30 min | ~3 sec |
| **Routes** | ~10 min | ~2 sec |
| **Interactables** | ~15 min | ~2 sec |
| **Mess prefabs** | ~30 min | ~3 sec |
| **Sequences** | ~45 min | ~2 sec |
| **Total** | **~145 min** | **~15 sec** ⚡ |

→ **Nhanh hơn 580 lần!**

---

## 🎓 Workflows

### **Workflow 1: Quick Test**
```
Tools > FunClass > Generate Complete Level
→ Click "Quick Normal"
→ Play test ngay!
```
**Time:** 30 giây

### **Workflow 2: Custom Level**
```
Tools > FunClass > Generate Complete Level
→ Setup custom settings
→ GENERATE COMPLETE LEVEL
→ Tweak configs if needed
→ Play test
```
**Time:** 2 phút

### **Workflow 3: Level Series**
```
Loop:
  → Generate level with incremental difficulty
  → Export to JSON
  → Commit to Git
→ 10 levels in 5 minutes!
```

### **Workflow 4: Tutorial + Levels + Boss**
```
1. Generate Tutorial (preset)
2. Generate 5 Normal levels (quick)
3. Generate 3 Hard levels (quick)
4. Generate Boss (preset)
→ Complete game progression in 10 minutes!
```

---

## ✅ Checklist

### **Sau khi generate:**

- [ ] Check scene hierarchy
- [ ] Verify student positions
- [ ] Test routes (escape/return)
- [ ] Check interactable objects
- [ ] Verify goal thresholds
- [ ] Test sequences
- [ ] Play test level
- [ ] Adjust configs if needed
- [ ] Save scene
- [ ] Export to JSON (optional)

---

## 🎉 Tóm Tắt

### **One-Click Generation bao gồm:**

✅ **Scene hierarchy** - Managers, Classroom, UI
✅ **Level goals** - Difficulty-based thresholds
✅ **Students** - 5 archetypes, auto-distribution
✅ **Routes** - Escape + Return with waypoints
✅ **Interactables** - 10 object types
✅ **Mess prefabs** - 6 mess types
✅ **Sequences** - 7 interaction templates
✅ **Complete integration** - All configs linked

### **Menu Command:**
```
Tools > FunClass > Generate Complete Level
```

### **Quick Buttons:**
- **Quick Easy** - Tutorial-like level
- **Quick Normal** - Balanced level
- **Quick Hard** - Challenge level

### **Presets:**
- **Tutorial Level** - Learning experience
- **Boss Level** - Ultimate challenge

### **Time Saved:**
```
Manual: ~145 minutes
One-Click: ~15 seconds
→ 580x faster! ⚡
```

🚀 **Tạo complete playable level chỉ với ONE CLICK!**

---

## 📚 Related Documentation

- **AutoGeneration_README.md** - Chi tiết các modules
- **CustomLevel_README.md** - UI editor và JSON import
- **ModularSetup_README.md** - Module architecture

---

## 🔗 Module References

### **Core Modules:**
- `@c:\Users\thuat\funclass\Assets\Scripts\Editor\Modules\LevelGoalGenerator.cs`
- `@c:\Users\thuat\funclass\Assets\Scripts\Editor\Modules\StudentGenerator.cs`
- `@c:\Users\thuat\funclass\Assets\Scripts\Editor\Modules\InteractableObjectGenerator.cs`
- `@c:\Users\thuat\funclass\Assets\Scripts\Editor\Modules\MessPrefabGenerator.cs`
- `@c:\Users\thuat\funclass\Assets\Scripts\Editor\Modules\SequenceGenerator.cs`
- `@c:\Users\thuat\funclass\Assets\Scripts\Editor\Modules\WaypointRouteBuilder.cs`
- `@c:\Users\thuat\funclass\Assets\Scripts\Editor\Modules\SceneHierarchyBuilder.cs`

### **Wrapper:**
- `@c:\Users\thuat\funclass\Assets\Scripts\Editor\FullAutoLevelGenerator.cs`

🎯 **Everything you need for instant level creation!**
