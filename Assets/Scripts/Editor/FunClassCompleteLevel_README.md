# Tạo Màn Chơi Hoàn Chỉnh - Hướng Dẫn

## 🎮 Tạo Màn Chơi Chỉ Với 1 Click!

### **Cách Sử Dụng Nhanh Nhất:**

1. **Mở Unity Editor**
2. **Click menu: `Tools > FunClass > Create Complete Level`**
3. **Nhập thông tin màn chơi**
4. **Click "TẠO MÀN CHƠI HOÀN CHỈNH"**
5. ✅ **XONG!** - Màn chơi đã sẵn sàng để chơi!

## 📋 Màn Chơi Bao Gồm Gì?

### **Tự động tạo:**
- ✅ **Scene mới** - Đầy đủ hierarchy
- ✅ **Tất cả Managers** - 7 managers cần thiết
- ✅ **Students** - Với configs đã setup sẵn
- ✅ **Classroom** - Environment + Furniture
- ✅ **Waypoints & Routes** - Escape và Return routes
- ✅ **Level Config** - Với độ khó đã chọn
- ✅ **Student Configs** - Personality ngẫu nhiên
- ✅ **UI Canvas** - Sẵn sàng cho UI elements

### **Cấu trúc files được tạo:**
```
Assets/
├── Scenes/
│   └── Level_01.unity ✅ Scene hoàn chỉnh
├── Configs/
│   └── Level_01/
│       ├── Level_01_Config.asset ✅ Level config
│       ├── Level_01_Goal.asset ✅ Goal config
│       ├── Students/
│       │   ├── Student_Nam.asset ✅ 
│       │   ├── Student_Lan.asset ✅
│       │   ├── Student_Minh.asset ✅
│       │   └── ... (tùy số lượng)
│       └── Routes/
│           ├── EscapeRoute.asset ✅
│           └── ReturnRoute.asset ✅
```

## 🎯 Giao Diện Setup

### **Window Settings:**

**Tên Màn:**
- Nhập tên màn chơi (VD: "Level_01", "Level_Boss")

**Số Học Sinh:**
- Slider từ 3 đến 10 học sinh
- Mặc định: 5 học sinh

**Độ Khó:**
- **Easy** - Dễ (3 học sinh, thời gian nhiều)
- **Normal** - Thường (5 học sinh, cân bằng)
- **Hard** - Khó (8 học sinh, thời gian ít)

**Tùy Chọn:**
- ☑ Tạo Waypoints & Routes
- ☑ Tạo Sample Data

### **Templates Nhanh:**

**Màn Dễ (3 học sinh)**
- 3 students
- Thời gian: 600s (10 phút)
- Disruption threshold: 90
- Easy win conditions

**Màn Thường (5 học sinh)**
- 5 students
- Thời gian: 300s (5 phút)
- Disruption threshold: 80
- Normal win conditions

**Màn Khó (8 học sinh)**
- 8 students
- Thời gian: 180s (3 phút)
- Disruption threshold: 70
- Hard win conditions

## 📊 Độ Khó Chi Tiết

### **Easy Mode:**
```
Max Disruption: 90/100
Catastrophic Disruption: 100
Max Critical Students: 3
Catastrophic Critical: 5
Max Outside Students: 3
Catastrophic Outside: 6
Time Limit: 600s (10 phút)
Required Problems: 3
Stars: 50/150/300 points
```

### **Normal Mode:**
```
Max Disruption: 80/100
Catastrophic Disruption: 95
Max Critical Students: 2
Catastrophic Critical: 4
Max Outside Students: 2
Catastrophic Outside: 5
Time Limit: 300s (5 phút)
Required Problems: 5
Stars: 100/250/500 points
```

### **Hard Mode:**
```
Max Disruption: 70/100
Catastrophic Disruption: 90
Max Critical Students: 1
Catastrophic Critical: 3
Max Outside Students: 1
Catastrophic Outside: 3
Time Limit: 180s (3 phút)
Required Problems: 8
Stars: 150/400/800 points
```

## 🎨 Student Configs Tự Động

### **Tên học sinh có sẵn:**
1. Nam
2. Lan
3. Minh
4. Hoa
5. Tuan
6. Mai
7. Khoa
8. Linh
9. Duc
10. Nga

### **Personality ngẫu nhiên:**
- **Base Distraction** - Mức độ dễ bị phân tâm
- **Escalation Speed** - Tốc độ leo thang
- **Calm Down Speed** - Tốc độ bình tĩnh lại
- **Influence Susceptibility** - Dễ bị ảnh hưởng
- **Influence Resistance** - Kháng ảnh hưởng
- **Panic Threshold** - Ngưỡng hoảng loạn

→ Mỗi học sinh có tính cách khác nhau!

### **Màu sắc visual:**
- Mỗi student có màu ngẫu nhiên
- Dễ phân biệt trong scene
- Có thể thay bằng 3D model sau

## 🗺️ Waypoints & Routes

### **Escape Route:**
```
Waypoint_0 (Classroom) → Waypoint_1 (Middle) → Waypoint_2 (Door/Outside)
```
- Running speed: 4 m/s
- Được gọi khi học sinh panic

### **Return Route:**
```
Waypoint_0 (Outside) → Waypoint_1 (Middle) → Waypoint_2 (Classroom)
```
- Walking speed: 2 m/s
- Được gọi khi teacher recall

### **Vị trí waypoints:**
- Escape: (0,0,0) → (5,0,0) → (10,0,0)
- Return: (10,0,0) → (5,0,0) → (0,0,0)
- Có thể di chuyển trong scene sau khi tạo

## 🎮 Workflow Hoàn Chỉnh

### **Tạo màn mới:**

1. **Chạy Create Complete Level**
   ```
   Tools > FunClass > Create Complete Level
   ```

2. **Nhập settings:**
   - Tên: "Level_Tutorial"
   - Students: 3
   - Difficulty: Easy

3. **Click "TẠO MÀN CHƠI"**
   - Chờ 5-10 giây
   - Xem progress bar

4. **Hoàn thành!**
   - Scene đã mở
   - Configs đã assign
   - Sẵn sàng chơi

### **Customize sau khi tạo:**

**Thay đổi visual:**
```
1. Select Student_Nam
2. Delete "Visual" child
3. Kéo 3D model vào
4. Adjust position
```

**Thêm interactable objects:**
```
1. Create GameObject
2. Add StudentInteractableObject component
3. Configure interaction type
```

**Adjust waypoints:**
```
1. Select waypoint trong scene
2. Di chuyển đến vị trí mong muốn
3. Routes tự động update
```

**Thay đổi difficulty:**
```
1. Open Level_01_Goal.asset
2. Adjust thresholds
3. Save
```

## 🚀 So Sánh Tốc Độ

| Phương Pháp | Thời Gian | Độ Hoàn Chỉnh |
|-------------|-----------|---------------|
| **Làm tay hoàn toàn** | ~2 giờ | 80% (dễ thiếu) |
| **Setup Scene + Manual Config** | ~45 phút | 85% |
| **Create Complete Level** | **~10 giây** | **100%** ⚡ |

→ **Nhanh hơn 720 lần!**

## 💡 Tips & Tricks

### **Tạo nhiều màn nhanh:**
```
1. Level_01 (Easy, 3 students)
2. Level_02 (Normal, 5 students)
3. Level_03 (Hard, 8 students)
4. Level_Boss (Hard, 10 students)
```
→ Mỗi màn chỉ mất 10 giây!

### **Template workflow:**
```
1. Tạo Level_Template với settings ưa thích
2. Duplicate scene
3. Rename và adjust nhỏ
```

### **Reuse configs:**
```
1. Copy student configs từ màn cũ
2. Paste vào màn mới
3. Chỉ cần adjust một vài giá trị
```

### **Batch testing:**
```
1. Tạo 5 màn cùng lúc
2. Test từng màn
3. Giữ lại màn hay nhất
```

## 🔧 Customization Examples

### **Màn Tutorial:**
```csharp
Level Name: "Tutorial"
Students: 2
Difficulty: Easy
Time: 900s (15 phút)
No lose conditions (set catastrophic very high)
```

### **Màn Boss:**
```csharp
Level Name: "Boss_Final"
Students: 10
Difficulty: Hard
Time: 120s (2 phút)
Very strict conditions
```

### **Màn Endless:**
```csharp
Level Name: "Endless"
Students: 5
Difficulty: Normal
Time: Unlimited (hasTimeLimit = false)
Goal: Survive as long as possible
```

## 📝 Checklist Sau Khi Tạo

- [ ] Scene đã được tạo trong Assets/Scenes/
- [ ] Configs đã được tạo trong Assets/Configs/
- [ ] Students có configs assigned
- [ ] Waypoints đã được tạo
- [ ] Routes đã được assigned vào LevelConfig
- [ ] LevelManager có LevelConfig
- [ ] Có thể play scene ngay

## 🎯 Next Steps

### **Sau khi tạo màn:**

1. **Test chơi:**
   - Click Play button
   - Kiểm tra gameplay
   - Adjust difficulty nếu cần

2. **Thêm visual:**
   - Import 3D models
   - Replace capsule placeholders
   - Add textures/materials

3. **Thêm interactions:**
   - Create interactable objects
   - Setup interaction sequences
   - Add reactions

4. **Polish UI:**
   - Design UI elements
   - Add animations
   - Implement feedback

5. **Build level progression:**
   - Link levels together
   - Add level select menu
   - Save progress system

## 🐛 Troubleshooting

**Lỗi: "Cannot create asset"**
- Kiểm tra folder permissions
- Đảm bảo không có file trùng tên

**Lỗi: "Config not assigned"**
- Reopen scene
- Manually assign configs
- Check console for errors

**Students không có config:**
- Check Assets/Configs/{LevelName}/Students/
- Manually assign từ Inspector

**Waypoints không hoạt động:**
- Kiểm tra StudentRoute asset
- Assign waypoints vào route
- Check route assigned to LevelConfig

## 🎉 Kết Luận

Với **Create Complete Level**, bạn có thể:

✅ Tạo màn chơi hoàn chỉnh trong **10 giây**
✅ Không cần setup thủ công
✅ Tất cả configs đã được tạo sẵn
✅ Sẵn sàng chơi ngay lập tức
✅ Dễ dàng customize sau
✅ Tạo nhiều màn nhanh chóng

**Bắt đầu ngay:** `Tools > FunClass > Create Complete Level` 🚀
