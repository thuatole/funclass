# FunClass - Game Completion Checklist

## Tổng quan dự án
Game quản lý lớp học, người chơi đóng vai giáo viên phải kiểm soát hành vi học sinh, giữ disruption level thấp để win level.

---

## 1. CORE GAMEPLAY (Đã hoàn thành phần lớn)

### 1.1 Student System
- [x] StudentAgent - Quản lý state học sinh (Calm, Distracted, ActingOut, Critical)
- [x] StudentConfig - Cấu hình personality, behaviors
- [x] StudentState machine - Chuyển đổi trạng thái
- [x] Autonomous behaviors (fidget, look around, stand up, move)
- [x] Object interactions (knock over, make noise, throw, drop, touch)
- [x] Student reactions (angry, scared, cry, laugh, apologize, embarrassed)
- [ ] **Animation system** - Cần thêm Animator và animations cho học sinh

### 1.2 Influence System
- [x] StudentInfluenceManager - Xử lý peer influence
- [x] InfluenceScope (None, SingleStudent, WholeClass)
- [x] SingleStudent distance check (<=2m)
- [x] WholeClass location check (inside/outside)
- [x] Source tracking và resolve khi calm down
- [x] Influence status icons (! và ?)
- [ ] **Visual feedback** - Thêm particle effects khi influence xảy ra

### 1.3 Teacher System
- [x] TeacherController - Di chuyển, click để interact
- [x] Click học sinh để calm down
- [x] Item confiscation system
- [ ] **Teacher actions đa dạng** - Thêm: Warning, Praise, Send outside, Call parents
- [ ] **Teacher animations** - Walking, talking, pointing

### 1.4 Level System
- [x] LevelManager - Win/Lose conditions
- [x] LevelConfig, LevelGoalConfig
- [x] Disruption tracking
- [x] Time limit
- [x] Star rating
- [ ] **Level progression** - Save/load progress, unlock levels
- [ ] **Level selection UI**

---

## 2. MOVEMENT & NAVIGATION

### 2.1 Student Movement
- [x] StudentMovementManager
- [x] NavMeshAgent integration
- [x] Route following (waypoints)
- [x] Escape route (chạy ra ngoài khi panic)
- [x] Return route (quay lại lớp)
- [x] MoveToStudent (cho SingleStudent influence)
- [ ] **Collision avoidance** - Students không đi xuyên qua nhau

### 2.2 Teacher Movement
- [x] Click to move
- [x] NavMeshAgent
- [ ] **Path preview** - Hiển thị đường đi trước khi click

---

## 3. UI/UX (Cần bổ sung nhiều)

### 3.1 In-Game UI
- [x] Basic HUD (disruption bar, time)
- [x] Student visual markers (A, B, C labels)
- [x] Influence status icons
- [ ] **Student info popup** - Click học sinh hiện thông tin chi tiết
- [ ] **Action buttons** - Các nút hành động cho teacher
- [ ] **Notification system** - Thông báo events quan trọng
- [ ] **Tutorial tooltips**

### 3.2 Menus
- [ ] **Main Menu** - Start, Options, Quit
- [ ] **Pause Menu** - Resume, Restart, Settings, Main Menu
- [ ] **Level Complete Screen** - Stars, score, next level
- [ ] **Game Over Screen** - Retry, Main Menu
- [ ] **Settings Menu** - Volume, graphics, controls

### 3.3 Visual Feedback
- [ ] **Particle effects** - Influence, calm down, state change
- [ ] **Screen shake** - Khi disruption cao
- [ ] **Color tinting** - Màn hình đỏ dần khi gần thua
- [ ] **Sound effects indicators**

---

## 4. AUDIO (Chưa có)

### 4.1 Sound Effects
- [ ] **Student sounds** - Talking, laughing, crying, noise making
- [ ] **Teacher sounds** - Footsteps, voice commands
- [ ] **Object sounds** - Knock, throw, drop
- [ ] **UI sounds** - Button click, notification

### 4.2 Music
- [ ] **Background music** - Calm classroom ambience
- [ ] **Tension music** - Khi disruption cao
- [ ] **Victory/Defeat music**

### 4.3 Audio Manager
- [ ] **AudioManager script** - Quản lý tất cả audio
- [ ] **Volume controls** - Master, SFX, Music
- [ ] **3D spatial audio** - Sounds từ vị trí học sinh

---

## 5. ENTITY BEHAVIORS & ANIMATIONS (Chi tiết)

### 5.1 Student Behaviors (Logic - Cần kết nối với Animation)

#### 5.1.1 Seating & Position
- [x] Original seat position tracking
- [x] Return to seat logic
- [ ] **Sit down animation trigger** - Khi về ghế
- [ ] **Stand up animation trigger** - Khi rời ghế
- [ ] **Seat assignment system** - Mỗi student có ghế riêng
- [ ] **Desk interaction zone** - Vùng tương tác với bàn

#### 5.1.2 Idle Behaviors (Ngồi tại chỗ)
- [x] Fidget behavior (logic)
- [x] Look around behavior (logic)
- [ ] **Fidget animations** - Ngọ nguậy, gãi đầu, vặn người
- [ ] **Look around animations** - Quay đầu nhìn xung quanh
- [ ] **Yawn animation** - Ngáp
- [ ] **Stretch animation** - Vươn vai
- [ ] **Doodle animation** - Vẽ nguệch ngoạc
- [ ] **Sleep animation** - Gục đầu ngủ

#### 5.1.3 Movement Behaviors
- [x] Stand up logic
- [x] Walk/Move around logic
- [x] Run (escape) logic
- [ ] **Stand up animation** - Đứng dậy từ ghế
- [ ] **Walk animation** - Đi bộ
- [ ] **Run animation** - Chạy
- [ ] **Sneak animation** - Đi lén lút
- [ ] **Turn animation** - Xoay người

#### 5.1.4 Object Interaction Behaviors
- [x] Touch object (logic)
- [x] Knock over object (logic)
- [x] Throw object (logic)
- [x] Drop object (logic)
- [x] Make noise (logic)
- [ ] **Pick up animation** - Nhặt đồ
- [ ] **Throw animation** - Ném đồ
- [ ] **Drop animation** - Thả đồ
- [ ] **Knock over animation** - Đẩy đổ
- [ ] **Drum/tap animation** - Gõ tạo tiếng ồn

#### 5.1.5 Expressions & Reactions (Biểu cảm)
- [x] Reaction types defined (Angry, Scared, Cry, Laugh, Apologize, Embarrassed)
- [ ] **Facial expressions** - Blend shapes hoặc texture swap
  - [ ] Happy/Smile
  - [ ] Sad/Crying
  - [ ] Angry/Frown
  - [ ] Scared/Shocked
  - [ ] Embarrassed/Blush
  - [ ] Bored/Sleepy
  - [ ] Confused
- [ ] **Body language animations**
  - [ ] Shrug shoulders
  - [ ] Cross arms (defiant)
  - [ ] Slouch (bored)
  - [ ] Sit up straight (attentive)
  - [ ] Cover face (embarrassed)
  - [ ] Wipe tears (crying)
  - [ ] Shake fist (angry)

#### 5.1.6 Social Interactions
- [x] Influence system (logic)
- [ ] **Whisper animation** - Thì thầm với bạn
- [ ] **Pass note animation** - Chuyền giấy
- [ ] **Point at someone** - Chỉ tay
- [ ] **Laugh at someone** - Cười nhạo
- [ ] **High five** - Đập tay

#### 5.1.7 State-Based Poses
- [ ] **Calm state pose** - Ngồi ngay ngắn, chú ý
- [ ] **Distracted state pose** - Ngồi nghiêng, nhìn chỗ khác
- [ ] **Acting out state pose** - Vặn vẹo, không yên
- [ ] **Critical state pose** - Đứng lên, hung hăng

### 5.2 Teacher Behaviors & Animations

#### 5.2.1 Movement
- [x] Click to move (logic)
- [ ] **Walk animation**
- [ ] **Idle animation** - Đứng quan sát lớp
- [ ] **Turn animation**

#### 5.2.2 Teaching Actions
- [ ] **Write on board animation**
- [ ] **Point at board animation**
- [ ] **Hold book animation**
- [ ] **Explain/gesture animation**

#### 5.2.3 Discipline Actions
- [x] Calm down student (logic)
- [x] Confiscate item (logic)
- [ ] **Approach student animation**
- [ ] **Talk to student animation** - Nhắc nhở
- [ ] **Point at student animation** - Chỉ tay cảnh cáo
- [ ] **Take item animation** - Tịch thu đồ
- [ ] **Escort student animation** - Đưa học sinh ra ngoài
- [ ] **Praise animation** - Khen ngợi, vỗ tay

#### 5.2.4 Teacher Expressions
- [ ] **Neutral face**
- [ ] **Smile** - Khi khen
- [ ] **Stern face** - Khi phạt
- [ ] **Surprised** - Khi có sự cố
- [ ] **Frustrated** - Khi disruption cao

### 5.3 Animation System Architecture

#### 5.3.1 Animator Controllers
- [ ] **StudentAnimatorController** - State machine cho student
  - [ ] Idle Layer (base)
  - [ ] Movement Layer (additive/override)
  - [ ] Action Layer (one-shot actions)
  - [ ] Expression Layer (face)
- [ ] **TeacherAnimatorController** - State machine cho teacher

#### 5.3.2 Animation Parameters
- [ ] **isWalking** (bool)
- [ ] **isRunning** (bool)
- [ ] **isSitting** (bool)
- [ ] **currentState** (int) - 0=Calm, 1=Distracted, 2=ActingOut, 3=Critical
- [ ] **expressionType** (int)
- [ ] **actionTrigger** (trigger) - For one-shot animations

#### 5.3.3 Animation Events
- [ ] **OnActionStart** - Khi bắt đầu action
- [ ] **OnActionComplete** - Khi kết thúc action
- [ ] **OnFootstep** - Cho sound effects
- [ ] **OnImpact** - Khi ném/đánh

### 5.4 3D Models Requirements

#### 5.4.1 Student Model
- [ ] **Rigged humanoid** - Với standard Unity rig
- [ ] **Blend shapes** - Cho facial expressions (optional)
- [ ] **Multiple outfits** - Để phân biệt học sinh
- [ ] **Hair variations**
- [ ] **Accessory slots** - Kính, mũ, etc.

#### 5.4.2 Teacher Model
- [ ] **Rigged humanoid**
- [ ] **Professional attire**
- [ ] **Interchangeable items** - Sách, bút, thước

#### 5.4.3 Furniture Models
- [ ] **Student desk** - Với collision
- [ ] **Student chair** - Với sit point
- [ ] **Teacher desk**
- [ ] **Whiteboard/Blackboard**
- [ ] **Cabinet/Shelf**
- [ ] **Door** - Có thể mở/đóng

#### 5.4.4 Interactable Objects
- [ ] **Book** - canThrow, canDrop
- [ ] **Pencil/Pen**
- [ ] **Eraser**
- [ ] **Paper ball** - canThrow
- [ ] **Ruler**
- [ ] **Backpack**

### 5.5 Visual Effects

#### 5.5.1 Particle Systems
- [ ] **Influence spread** - Waves khi influence
- [ ] **Calm down sparkle** - Khi được an ủi
- [ ] **State change burst** - Khi đổi state
- [ ] **Throw trail** - Khi ném đồ

#### 5.5.2 Shader Effects
- [ ] **Student highlight** - Outline khi hover
- [ ] **State color tint** - Màu theo state
- [ ] **Emotion bubble** - Icon cảm xúc floating

#### 5.5.3 Post-processing
- [ ] **Bloom** - Nhẹ
- [ ] **Color grading** - Theo mood
- [ ] **Vignette** - Khi disruption cao

---

## 6. CONTENT & LEVELS

### 6.1 Level Design
- [ ] **Level 1** - Tutorial, 3-4 học sinh dễ
- [ ] **Level 2** - 5-6 học sinh, thêm mechanics
- [ ] **Level 3** - 7-8 học sinh, harder
- [ ] **Level 4+** - Progressive difficulty
- [ ] **Classroom layouts** - Nhiều layout khác nhau

### 6.2 Student Variety
- [x] Different personalities (patience, impulsiveness, etc.)
- [ ] **Student archetypes** - The Troublemaker, The Follower, The Loner, etc.
- [ ] **Special students** - Có abilities đặc biệt

### 6.3 Events & Scenarios
- [x] ScenarioController cho one-time events
- [ ] **Random events** - Fire drill, principal visit, etc.
- [ ] **Scripted sequences** - Story moments

---

## 7. DATA & PERSISTENCE

### 7.1 Save System
- [ ] **PlayerPrefs** cho settings
- [ ] **JSON save files** - Progress, unlocks, high scores
- [ ] **Auto-save**

### 7.2 JSON Import (Partially done)
- [x] Students với positions
- [x] Routes/Waypoints
- [x] Level goals
- [ ] **Classroom layout** - Tường, sàn, furniture positions
- [ ] **Bàn ghế học sinh** - Desk/chair positions
- [ ] **Bàn giáo viên** - Teacher desk, whiteboard

---

## 8. POLISH & OPTIMIZATION

### 8.1 Performance
- [ ] **Object pooling** - Cho particles, UI elements
- [ ] **LOD system** - Cho models
- [ ] **Occlusion culling**
- [ ] **Profiling & optimization**

### 8.2 Quality of Life
- [ ] **Hotkeys** - Keyboard shortcuts
- [ ] **Undo system** - Undo last action
- [ ] **Fast forward** - Speed up time
- [ ] **Pause anywhere**

### 8.3 Bug Fixes & Edge Cases
- [ ] Test tất cả win/lose conditions
- [ ] Test với nhiều học sinh (10+)
- [ ] Test edge cases (all students outside, etc.)
- [ ] Memory leak check

---

## 9. BUILD & DEPLOYMENT

### 9.1 Build Settings
- [ ] **PC build** - Windows standalone
- [ ] **Resolution options**
- [ ] **Quality settings**

### 9.2 Testing
- [ ] **Playtesting** - Balance, difficulty curve
- [ ] **Bug testing** - QA pass
- [ ] **Performance testing** - FPS, memory

---

## PRIORITY ORDER (Đề xuất)

### Phase 1: Playable Prototype (1-2 tuần)
1. [ ] Main Menu + Pause Menu
2. [ ] Level Complete/Game Over screens
3. [ ] Basic sound effects
4. [ ] 3 levels hoàn chỉnh với difficulty curve

### Phase 2: Core Polish (2-3 tuần)
1. [ ] Student 3D models (hoặc 2D sprites)
2. [ ] Basic animations
3. [ ] More teacher actions
4. [ ] Tutorial level
5. [ ] Save/load progress

### Phase 3: Content & Features (2-4 tuần)
1. [ ] 5+ levels
2. [ ] Audio complete (music + SFX)
3. [ ] Visual effects
4. [ ] Student archetypes
5. [ ] Random events

### Phase 4: Final Polish (1-2 tuần)
1. [ ] Performance optimization
2. [ ] Bug fixes
3. [ ] Playtesting & balance
4. [ ] Final build

---

## QUICK WINS (Có thể làm ngay)

1. **Add AudioSource** - Placeholder sounds cho feedback
2. **Simple Main Menu** - Start button, quit button
3. **Pause with ESC** - Basic pause menu
4. **Level restart button** - Khi thua có thể retry
5. **Win celebration** - Confetti particles khi win

---

## FILES CẦN TẠO

```
Assets/
├── Audio/
│   ├── Music/
│   └── SFX/
├── Prefabs/
│   ├── Characters/
│   │   ├── Student.prefab
│   │   └── Teacher.prefab
│   ├── Furniture/
│   │   ├── Desk.prefab
│   │   ├── Chair.prefab
│   │   └── Whiteboard.prefab
│   └── UI/
│       ├── MainMenu.prefab
│       └── PauseMenu.prefab
├── Scenes/
│   ├── MainMenu.unity
│   ├── Level_01.unity
│   ├── Level_02.unity
│   └── Level_03.unity
└── Scripts/
    ├── Audio/
    │   └── AudioManager.cs
    ├── UI/
    │   ├── MainMenuController.cs
    │   ├── PauseMenuController.cs
    │   ├── LevelCompleteUI.cs
    │   └── GameOverUI.cs
    └── Save/
        └── SaveManager.cs
```

---

## GHI CHÚ

- Đánh dấu [x] khi hoàn thành
- Update file này thường xuyên
- Ưu tiên theo Phase order
- Test sau mỗi feature lớn

**Last Updated:** 2026-01-21
