# FunClass Configuration Guide

Tài liệu hướng dẫn cấu hình game FunClass. Bao gồm level config, GUI text configs, và các enums chính.

---

## Phần 1 - Level Configuration

Level được cấu hình qua JSON file trong `Assets/LevelTemplates/`.

### Unified JSON Schema với 3 Modes

| Mode | Mô tả |
|------|-------|
| **Auto** | Hệ thống tự tính toán vị trí door, desk grid, routes |
| **Manual** | Dev chỉ định tất cả vị trí cố định |
| **Hybrid** | Kết hợp - một số tự động, một số thủ công |

### Các trường chính

#### classroom
```json
"classroom": {
  "width": 10,        // Chiều rộng lớp học (units)
  "depth": 8,         // Chiều sâu lớp học (units)
  "height": 3.0,      // Chiều cao trần (units)
  "doorPosition": {   // Vị trí cửa (null = auto, 60% từ trái)
    "x": 1,
    "y": 0,
    "z": 4
  }
}
```

#### deskLayout
```json
"deskLayout": {
  "rows": 2,          // Số hàng desk (luôn = 2)
  "spacingX": 2.5,    // Khoảng cách giữa các desk theo X
  "spacingZ": 2.5,    // Khoảng cách giữa các desk theo Z
  "aisleWidth": 1.5   // Độ rộng lối đi giữa 2 hàng
}
```

#### goalSettings
```json
"goalSettings": {
  "maxDisruptionThreshold": 80,     // Ngưỡng disruption tối đa
  "catastrophicDisruptionLevel": 95,// Ngưỡng thảm họa
  "timeLimitSeconds": 60,           // Thời gian limit (giây)
  "requiredCalmDowns": 0,           // Số lần cần calm down
  "requiredResolvedProblems": 0,    // Số problems cần resolve

  "oneStarScore": 50,
  "twoStarScore": 100,
  "threeStarScore": 150
}
```

#### environment
```json
"environment": {
  "boardMaterial": "White",
  "floorMaterial": "Floor",
  "wallMaterial": "Wall",
  "autoSetupLighting": true
}
```

#### routeGeneration
```json
"routeGeneration": {
  "autoGenerateRoutes": true,
  "escapeRouteSpeed": 5.0,   // Tốc độ khi escape
  "returnRouteSpeed": 3.0    // Tốc độ khi quay lại
}
```

---

## Phần 2 - GUI Text Configs

Thư mục: `Assets/Configs/GUI/`

### PopupText.json
Text hiển thị cho student popup.

```json
{
  "targetStudent": {
    "openingPhrase": "Cô ơi!",
    "noComplaints": "Em ổn rồi cô!",
    "escortButtonEnabled": "🏠 Escort Back",
    "escortButtonDisabled": "🏠 Escort Back",
    "closeButton": "❌ Close"
  },
  "sourceStudent": {
    "impactWholeClass": "⚠️ Đang ảnh hưởng cả lớp ({count} học sinh)",
    "impactIndividual": "⚠️ Đang ảnh hưởng:",
    "resolveWholeClassButton": "✅ Giải quyết cho cả lớp",
    "resolveIndividualButton": "✅ Giải quyết cho {studentName}",
    "closeButton": "❌ Close"
  },
  "stateEmojis": {
    "Calm": "😌",
    "Distracted": "😕",
    "ActingOut": "😠",
    "Critical": "😰"
  }
}
```

### ComplaintTemplates.json
Template khiếu nại theo event type. Dùng `{source}` để thay tên student.

```json
{
  "complaints": {
    "MessCreated": {
      "template": "Bạn {source} ói, thúi quá!",
      "icon": "😷"
    },
    "PhysicalInteraction": {
      "template": "Bạn {source} đánh con, đau lắm!",
      "icon": "😢"
    },
    "ThrowingObject": {
      "template": "Bạn {source} ném đồ vào con!",
      "icon": "🎯"
    },
    "MakingNoise": {
      "template": "Bạn {source} làm ồn, con không học được!",
      "icon": "🔊"
    },
    "Distraction": {
      "template": "Bạn {source} làm con mất tập trung!",
      "icon": "😵"
    },
    "Poop": {
      "template": "Bạn {source} ỉa, thúi lắm cô!",
      "icon": "💩"
    }
  }
}
```

### SourceStatements.json
Lời nói của source student. Dùng `{targets}` để thay tên student bị ảnh hưởng.

```json
{
  "statements": {
    "Vomit": [
      "Em ói rồi cô ơi...",
      "Em không kìm được cô...",
      "Em bị ốm cô ơi..."
    ],
    "Poop": [
      "Em không kìm được cô ơi...",
      "Em đau bụng quá cô...",
      "Em xin lỗi cô..."
    ],
    "Hit": [
      "Em tức quá cô ơi, nên em đánh bạn {targets}...",
      "Bạn ấy chọc em trước cô, nên em đánh bạn {targets}!"
    ],
    "ThrowObject": [
      "Em không cố ý cô ơi, em ném đồ vào bạn {targets}..."
    ],
    "MakeNoise": [
      "Em đang nói chuyện với bạn {targets} cô ơi..."
    ],
    "Push": [
      "Em vô tình đụng phải bạn {targets} cô ơi...",
      "Bạn ấy đứng chắn đường em, nên em đẩy..."
    ],
    "TakeItem": [
      "Em chỉ mượn bút của bạn {targets} cô ơi...",
      "Bạn ấy không dùng, nên em lấy tạm..."
    ],
    "Tease": [
      "Em chỉ đùa với bạn {targets} thôi cô...",
      "Bạn ấy không hiểu hài hước của em..."
    ],
    "Distract": [
      "Em chỉ đi một chút cô...",
      "Em cần đi vệ sinh cô ơi..."
    ]
  }
}
```

### ButtonLabels.json
Nhãn nút và tooltips.

```json
{
  "actions": {
    "resolveWholeClass": "✅ Giải quyết cho cả lớp",
    "resolveIndividual": "✅ Giải quyết cho {name}",
    "escortBack": "🏠 Escort Back",
    "close": "❌ Close"
  },
  "tooltips": {
    "escortDisabled": "Cần giải quyết các nguồn gốc trước",
    "escortEnabled": "Đưa học sinh về chỗ ngồi"
  }
}
```

### EventTypeMapping.json
Mapping giữa StudentEventType enum và keys trong config files.

```json
{
  "sourceStatementMapping": {
    "MessCreated": "Vomit",
    "ThrowingObject": "ThrowObject",
    "MakingNoise": "MakeNoise",
    "KnockedOverObject": "Push",
    "WanderingAround": "Distract",
    "DroppedItem": "Push",
    "LeftSeat": "Distract",
    "StudentActedOut": "Hit"
  },
  "complaintMapping": {
    "Vomit": "MessCreated",
    "ThrowObject": "ThrowingObject",
    "MakeNoise": "MakingNoise",
    "Push": "KnockedOverObject",
    "Distract": "WanderingAround",
    "Hit": "StudentActedOut"
  }
}
```

---

## Phần 3 - Event Types (StudentEventType Enum)

Các event types thực tế trong code (StudentEvent.cs).

| Event Type | Mô tả | Source Key | Complaint Key |
|------------|-------|------------|---------------|
| `MessCreated` | Student ói | Vomit | MessCreated |
| `ThrowingObject` | Ném đồ | ThrowObject | ThrowingObject |
| `MakingNoise` | Làm ồn | MakeNoise | MakingNoise |
| `KnockedOverObject` | Đẩy đổ vật | Push | KnockedOverObject |
| `WanderingAround` | Đi lại trong lớp | Distract | WanderingAround |
| `DroppedItem` | Làm rơi đồ | Push | KnockedOverObject |
| `LeftSeat` | Rời khỏi chỗ ngồi | Distract | WanderingAround |
| `StudentActedOut` | Hành động bạo lực | Hit | StudentActedOut |
| `TouchedObject` | Chạm vào vật | Distract | WanderingAround |

**Ghi chú:** Các event types sau KHÔNG tồn tại trong enum:
- ~~PhysicalInteraction~~ → Dùng `StudentActedOut`
- ~~StudentHit~~ → Dùng `StudentActedOut`
- ~~Teasing/Tease~~ → Dùng `KnockedOverObject` hoặc `StudentActedOut`
- ~~Poop/StudentPooped~~ → Chưa implemented
- ~~StudentVomited~~ → Dùng `MessCreated`
| `Distraction` | Làm mất tập trung | Distract | Distraction |
| `WanderingAround` | Đi lại | Distract | Distraction |

---

## Phần 4 - Influence Scopes

Xác định mức độ ảnh hưởng của event đến các students khác.

| Scope | Mô tả |
|-------|-------|
| `SingleStudent` | Chỉ ảnh hưởng một student cụ thể |
| `WholeClass` | Ảnh hưởng đến tất cả students trong lớp |
| `None` | Không ảnh hưởng ai |

### Cấu hình trong level JSON

```json
"influenceScopeSettings": {
  "eventScopes": [
    {
      "eventTypeName": "MessCreated",
      "scope": "WholeClass",
      "baseSeverity": 1.0,
      "description": "Vomit affects all students"
    },
    {
      "eventTypeName": "PhysicalInteraction",
      "scope": "SingleStudent",
      "baseSeverity": 0.8,
      "description": "Hit only affects target student"
    }
  ]
}
```

---

## Phần 5 - Student States

Trạng thái của student trong game.

| State | Icon | Mô tả | Hành vi |
|-------|------|-------|---------|
| `Calm` | 😌 | Bình tĩnh | Ngồi học bình thường |
| `Distracted` | 😕 | Mất tập trung | Nhìn xung quanh, không tập trung |
| `ActingOut` | 😠 | Hành động bạo lực | Có thể gây disruption |
| `Critical` | 😰 | Nguy hiểm | Có thể escape hoặc gây hại |

### State Transition

```
Calm → Distracted (bị ảnh hưởng nhẹ)
Distracted → ActingOut (bị ảnh hưởng nặng)
ActingOut → Critical (vượt ngưỡng)
Critical → Calm (được resolve)
```

### Personality factors ảnh hưởng:

- `patience`: Thời gian trước khi chuyển sang distracted
- `influenceSusceptibility`: Dễ bị ảnh hưởng bởi events khác
- `panicThreshold`: Ngưỡng chuyển sang critical

---

## Phần 6 - Teacher Actions

Các hành động teacher có thể thực hiện.

| Action | Mô tả | Effect |
|--------|-------|--------|
| `Calm` | Làm dịu student | Giảm disruption của student |
| `EscortBack` | Đưa về chỗ | Di chuyển student về desk |
| `CallBack` | Gọi lại | Student tự động quay về |
| `CleanMess` | Dọn dẹp | Xóa mess trên sàn |
| `ResolveInfluence` | Giải quyết ảnh hưởng | Loại bỏ influence lên students khác |

### Workflow điển hình

```
1. Student A ói (MessCreated)
   → Disruption tăng
   → Students khác bị ảnh hưởng (nếu WholeClass)

2. Teacher click vào Student A
   → Popup hiện ra với SourceStatement: "Em ói rồi cô ơi..."
   → Nút "Giải quyết cho cả lớp" nếu ảnh hưởng WholeClass

3. Teacher chọn action phù hợp
   → CleanMess: Xóa vết ói
   → Calm: Giảm disruption của Student A

4. Students bị ảnh hưởng quay về Calm
   → Hoặc tự động nếu source được resolve
```

---

## Phần 7 - StudentConfig

Cấu hình personality và behaviors cho từng student.

### Personality Traits

| Trait | Range | Mô tả |
|-------|-------|-------|
| `patience` | 0.0 - 1.0 | Thời gian trước khi chuyển sang Distracted |
| `attentionSpan` | 0.0 - 1.0 | Khả năng tập trung lâu |
| `impulsiveness` | 0.0 - 1.0 | Xác suất gây disruption khi Critical |
| `influenceSusceptibility` | 0.0 - 1.0 | Dễ bị ảnh hưởng bởi events khác |
| `influenceResistance` | 0.0 - 1.0 | Khả năng kháng lại influence |
| `panicThreshold` | 0.0 - 1.0 | Ngưỡng chuyển sang Critical |

### Behaviors

| Behavior | Mô tả |
|----------|-------|
| `canFidget` | Có thể nghịch ngợm (nhún chân, gõ bút) |
| `canLookAround` | Có thể nhìn xung quanh |
| `canStandUp` | Có thể đứng dậy (trigger LeftSeat event) |
| `canMoveAround` | Có thể đi lại trong lớp (trigger WanderingAround event) |
| `canDropItems` | Có thể làm rơi đồ (trigger DroppedItem event) |
| `canKnockOverObjects` | Có thể đẩy đổ vật (trigger KnockedOverObject event) |
| `canMakeNoiseWithObjects` | Có thể tạo tiếng ồn (trigger MakingNoise event) |
| `canThrowObjects` | Có thể ném đồ (trigger ThrowingObject event) |
| `canTouchObjects` | Có thể chạm vào vật xung quanh |

**Ghi chú:** Các behaviors sau KHÔNG tồn tại:
- ~~canTease~~ → Dùng `canKnockOverObjects` hoặc `canThrowObjects`
- ~~canWanderAround~~ → Dùng `canMoveAround`

### Cấu hình trong level JSON

```json
"studentConfigs": [
  {
    "studentId": "student_a",
    "studentName": "Student_A",
    "personality": {
      "patience": 0.1,
      "attentionSpan": 0.2,
      "impulsiveness": 1.0,
      "influenceSusceptibility": 0.3,
      "influenceResistance": 0.5,
      "panicThreshold": 0.2
    },
    "behaviors": {
      "canFidget": true,
      "canLookAround": true,
      "canStandUp": true,
      "canMoveAround": true,
      "canDropItems": false,
      "canKnockOverObjects": false,
      "canMakeNoiseWithObjects": false,
      "canThrowObjects": false,
      "minIdleTime": 1,
      "maxIdleTime": 2
    }
  }
]
```

### Personality Examples

| Student Type | Patience | Impulsiveness | Susceptibility | Behavior |
|--------------|----------|---------------|----------------|----------|
| "Good Student" | 0.9 | 0.1 | 0.2 | Calm, rarely causes issues |
| "Troublemaker" | 0.2 | 0.9 | 0.5 | Frequently acting out |
| "Sensitive" | 0.3 | 0.3 | 0.9 | Easily influenced by others |
| "Leader" | 0.5 | 0.7 | 0.1 | Influences others, resists influence |

---

## Phần 8 - Route Configuration

Cấu hình đường đi cho students khi escape hoặc quay lại.

### Waypoint Types

| Type | Mô tả |
|------|-------|
| `Desk` | Vị trí ngồi của student |
| `Aisle` | Lối đi giữa các desk |
| `Door` | Cửa ra vào |
| `Outside` | Khu vực bên ngoài lớp |
| `Board` | Bảng (tránh xa) |

### Route Generation

Routes được tự động sinh dựa trên:
- Vị trí desk của student
- Vị trí door (tính toán tự động hoặc chỉ định thủ công)
- NavMesh surface

### Cấu hình tùy chỉnh

```json
"routeGeneration": {
  "autoGenerateRoutes": true,    // Tự sinh routes
  "escapeRouteSpeed": 5.0,       // Tốc độ khi escape
  "returnRouteSpeed": 3.0,       // Tốc độ khi quay lại
  "waypointThreshold": 1.5,      // Khoảng cách giữa các waypoints
  "avoidCollision": true         // Tránh va chạm với students khác
}
```

### Route Types

| Route Type | Mô tả | Trigger |
|------------|-------|---------|
| `EscapeRoute` | Đường từ desk ra outside | Student đạt Critical state |
| `ReturnRoute` | Đường từ outside về desk | Teacher gọi về |
| `EmergencyRoute` | Đường khẩn cấp | Student cần toilet (Poop) |

---

## Phần 9 - Mess Types

Các loại mess (bẩn) có thể xuất hiện trong lớp học.

### Mess Type Enum

| Type | Icon | Description | Severity | Cleanup |
|------|------|-------------|----------|---------|
| `VomitMess` | 🤢 | Chất nôn | Cao (ảnh hưởng WholeClass) | CleanMess |
| `PoopMess` | 💩 | Phân | Rất cao | CleanMess |
| `KnockedOverObject` | 🔥 | Vật bị đẩy đổ | Trung bình | ResolveInfluence |
| `SpilledItem` | 💧 | Đồ bị đổ | Thấp | CleanMess |

### Mess Properties

```json
{
  "messId": "mess_001",
  "type": "VomitMess",
  "position": { "x": 2.5, "y": 0, "z": 3.0 },
  "sourceStudentId": "student_a",
  "createdAt": 1234567890,
  "radius": 1.5,           // Bán kính ảnh hưởng
  "severity": 1.0,         // Mức độ nghiêm trọng
  "cleanupRequired": true
}
```

### Cleanup Actions

| Mess Type | Teacher Action | Effect |
|-----------|----------------|--------|
| VomitMess | CleanMess | Xóa mess, giảm disruption |
| PoopMess | CleanMess + Calm | Xóa mess, dịu student |
| KnockedOverObject | ResolveInfluence | Dọn dẹp, giải quyết influence |
| SpilledItem | CleanMess | Xóa mess đơn giản |

### Mess Spawn Rules

| Event | Mess Type | Spawn Probability |
|-------|-----------|-------------------|
| MessCreated | VomitMess | 100% |
| Poop | PoopMess | 100% |
| KnockedOverObject | KnockedOverObject | 80% |
| ThrowingObject | SpilledItem | 50% |

---

## Quick Reference

### Thêm Event Type mới

1. Thêm vào `StudentEventType` enum
2. Thêm mapping trong `EventTypeMapping.json`
3. Thêm template vào `ComplaintTemplates.json`
4. Thêm statements vào `SourceStatements.json`
5. Cấu hình scope trong `influenceScopeSettings`

### Sửa Text hiển thị

- Popup text: Sửa `PopupText.json`
- Complaint template: Sửa `ComplaintTemplates.json`
- Student statements: Sửa `SourceStatements.json`
- Button labels: Sửa `ButtonLabels.json`

### Điều chỉnh Gameplay

- Thay đổi `goalSettings` để điều chỉnh độ khó
- Thay đổi `influenceScopeSettings` để điều chỉnh mức độ ảnh hưởng
- Thay đổi student personality để tạo các behaviors khác nhau
