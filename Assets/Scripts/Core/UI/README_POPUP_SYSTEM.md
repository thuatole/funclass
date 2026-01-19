# Student Interaction Popup System - Setup & Usage Guide

## Overview
A comprehensive GUI popup system that displays contextual information and actions when clicking on students. The system automatically determines popup type based on whether the student is a source of influence or a target.

---

## Files Created

### JSON Config Files (Assets/Configs/GUI/)
- `PopupText.json` - Main popup text and labels
- `ComplaintTemplates.json` - Target student complaint templates
- `SourceStatements.json` - Source student statement templates
- `ButtonLabels.json` - Button labels and tooltips

### C# Scripts (Assets/Scripts/Core/UI/)
- `PopupTextLoader.cs` - Loads and manages JSON configs
- `PopupManager.cs` - Singleton manager for popup lifecycle
- `StudentInteractionPopup.cs` - Main popup controller
- `PopupAnimator.cs` - Handles popup animations

---

## Setup Instructions

### 1. Create Popup Prefab in Unity

1. **Create Canvas:**
   - Right-click in Hierarchy → UI → Canvas
   - Name it "StudentPopupCanvas"
   - Set Render Mode to "World Space"
   - Set Sorting Order to 100

2. **Create Popup Panel:**
   - Right-click on Canvas → UI → Panel
   - Name it "StudentInteractionPopup"
   - Add `StudentInteractionPopup` component
   - Add `PopupAnimator` component

3. **Add UI Elements:**
   ```
   StudentInteractionPopup (Panel)
   ├── HeaderText (Text)
   ├── OpeningPhraseText (Text)
   ├── ComplaintListContainer (Vertical Layout Group)
   ├── TargetListContainer (Vertical Layout Group)
   └── ButtonContainer (Horizontal Layout Group)
   ```

4. **Assign References:**
   - Drag UI elements to `StudentInteractionPopup` component fields
   - Save as Prefab in `Assets/Prefabs/UI/`

### 2. Setup PopupManager

1. **Create PopupManager GameObject:**
   - Create empty GameObject in scene
   - Name it "PopupManager"
   - Add `PopupManager` component

2. **Assign Popup Prefab:**
   - Drag popup prefab to `Popup Prefab` field
   - PopupManager will auto-create canvas if needed

### 3. Verify Integration

- `TeacherController.cs` already integrated
- Left-click on students to show popup
- ESC key to close popup

---

## Usage

### For Players

**Show Popup:**
- Left-click on any student to show their popup

**Close Popup:**
- Press ESC key
- Click "Close" button
- Click on another student (auto-closes previous)

**Popup Types:**

1. **Target Student (Victim):**
   - Shows complaints from influence sources
   - Escort Back button (if applicable)

2. **Source Student (Info Only):**
   - Shows impact count
   - No action buttons (must resolve via other means)

3. **Source Student (WholeClass Action):**
   - Shows class-wide impact
   - "Resolve for whole class" button

4. **Source Student (Individual Actions):**
   - Shows each affected student
   - Individual resolve buttons

---

## Customization

### Editing Text

All text is stored in JSON files at `Assets/Configs/GUI/`:

**PopupText.json:**
```json
{
  "targetStudent": {
    "openingPhrase": "Cô ơi!",
    "noComplaints": "Em ổn rồi cô!"
  }
}
```

**ComplaintTemplates.json:**
```json
{
  "complaints": {
    "MessCreated": {
      "template": "Bạn {source} ói, thúi quá!",
      "icon": "😷"
    }
  }
}
```

**SourceStatements.json:**
```json
{
  "statements": {
    "Hit": [
      "Em tức quá cô ơi, nên em đánh bạn {targets}...",
      "Bạn ấy chọc em trước cô!"
    ]
  }
}
```

### Adding New Action Types

1. Add to `SourceStatements.json`:
```json
"NewAction": [
  "Em làm gì đó với bạn {targets}..."
]
```

2. Add to `ComplaintTemplates.json`:
```json
"NewAction": {
  "template": "Bạn {source} làm gì đó!",
  "icon": "❓"
}
```

3. Update `HasStudentResolveAction()` in `StudentInteractionPopup.cs` if needed

---

## Implementation Status

### ✅ Completed
- JSON config system with default values
- PopupTextLoader with fallback defaults
- PopupManager singleton
- StudentInteractionPopup with all 4 popup types
- PopupAnimator with fade/scale animations
- TeacherController integration (left-click)
- Dynamic text with {source}, {targets}, {count} variables

### ⚠️ Needs Implementation
- **GetAffectedStudents()** - Query StudentInfluenceManager for affected students
- **GetInfluenceSources()** - Query student's influence sources
- **GroupTargetsByActionType()** - Group targets by action type
- **GetSourceEventType()** - Determine source student's event type
- **IsStudentOutside()** - Check if student is outside classroom
- **ResolveForTarget()** - Actual resolve logic integration
- **ResolveForWholeClass()** - Actual whole class resolve logic
- **EscortStudent()** - Actual escort logic integration

### 🎨 Optional Enhancements
- Custom prefabs for ComplaintItem and TargetActionItem
- Better UI styling and layout
- Sound effects
- More animations
- Tooltips
- Student portraits

---

## Integration Points

### StudentInfluenceManager Integration

The popup system needs to query `StudentInfluenceManager` for:
- Which students are affected by a source student
- Which sources are affecting a target student
- Resolve influence methods

**Example Integration:**
```csharp
private List<StudentAgent> GetAffectedStudents(StudentAgent source)
{
    if (StudentInfluenceManager.Instance != null)
    {
        return StudentInfluenceManager.Instance.GetStudentsAffectedBy(source);
    }
    return new List<StudentAgent>();
}
```

### TeacherController Integration

Already integrated:
- Left-click shows popup
- ESC closes popup

Can add more integrations:
- Keyboard shortcuts for actions
- Context menu
- Quick actions

---

## Troubleshooting

### Popup Not Showing
1. Check PopupManager has popup prefab assigned
2. Verify StudentInteractionPopup component on prefab
3. Check console for errors
4. Ensure JSON files loaded successfully

### Text Not Loading
1. Verify JSON files exist in `Assets/Configs/GUI/`
2. Check JSON syntax is valid
3. PopupTextLoader will use defaults if files missing

### Animations Not Working
1. Ensure PopupAnimator component attached
2. Check CanvasGroup component exists
3. Verify LeanTween is available (or remove animation code)

### Click Not Working
1. Check TeacherController is active
2. Verify student has collider
3. Check interaction layer mask
4. Ensure PopupManager instance exists

---

## Performance Considerations

- Popups are created/destroyed on demand (not pooled)
- JSON files loaded once on startup
- Text templates cached in memory
- Only one popup can be open at a time

**Optimization Ideas:**
- Implement object pooling for popups
- Cache frequently used text strings
- Lazy load JSON files
- Reduce UI element count

---

## Future Enhancements

### Phase 2
- Student portraits in popup
- Animated student expressions
- Voice lines for complaints
- More detailed influence information
- History log of past influences

### Phase 3
- Multi-student comparison view
- Batch actions (resolve multiple at once)
- Drag-and-drop to resolve
- Keyboard shortcuts
- Tutorial tooltips

---

## API Reference

### PopupManager

```csharp
// Show popup for student
PopupManager.Instance.ShowPopup(StudentAgent student);

// Close current popup
PopupManager.Instance.CloseCurrentPopup();

// Check if popup is open
bool isOpen = PopupManager.Instance.IsPopupOpen;

// Get current student
StudentAgent current = PopupManager.Instance.CurrentStudent;
```

### PopupTextLoader

```csharp
// Get text
string phrase = PopupTextLoader.Instance.GetTargetOpeningPhrase();
string complaint = PopupTextLoader.Instance.GetComplaint("MessCreated", "A");
string statement = PopupTextLoader.Instance.GetSourceStatement("Hit", "C và D");

// Get emoji
string emoji = PopupTextLoader.Instance.GetStateEmoji("Critical");

// Check if loaded
bool loaded = PopupTextLoader.Instance.IsLoaded;
```

---

## Credits

**Design:** Based on GUI_POPUP_SYSTEM_PLAN.md
**Implementation:** Cascade AI Assistant
**JSON Configs:** Default Vietnamese text templates

---

## Support

For issues or questions:
1. Check console logs for errors
2. Verify JSON file syntax
3. Review this README
4. Check GUI_POPUP_SYSTEM_PLAN.md for design details

**Status:** Core system implemented, needs integration with StudentInfluenceManager
