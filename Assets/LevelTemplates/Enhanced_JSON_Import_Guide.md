# Enhanced JSON Level Import System

## Overview
The enhanced JSON import system automatically creates complete classroom levels from a simplified JSON schema. It handles desk placement, student binding, route generation, environment setup, and material fixing.

## Key Features
- **Auto desk grid**: Creates optimal 2-row desk layout based on student count
- **Student binding**: Automatically binds one student per desk
- **Route generation**: Creates EscapeRoute and ReturnRoute for each student
- **Environment setup**: Places board, door, walls, floor, teacher area
- **Material fixing**: Automatically detects and fixes pink/missing materials
- **Asset mapping**: Uses `AssetMapConfig` to map asset keys to prefabs

## Quick Start

### 1. Create Default Asset Map
Before importing levels, create the default asset map:

1. Go to **Tools > FunClass > Create Default Asset Map**
2. Confirm the creation
3. The system will auto-assign prefabs from `Assets/school/` directory

### 2. Create Sample JSON
1. Open **Tools > FunClass > Import Level From JSON**
2. Click **"Create Sample JSON"**
3. A sample level_01.json will be created at `Assets/Levels/Json/level_01.json`

### 3. Import Level
1. In the import window, select the JSON file
2. Click **"Import Level"**
3. Watch the progress bar as the level is generated
4. The scene will be saved to `Assets/Levels/Generated/`

## JSON Schema

### Minimal Example
```json
{
    "levelId": "my_level",
    "difficulty": "medium",
    "students": 6,
    "deskLayout": {
        "rows": 2,
        "spacingX": 2.0,
        "spacingZ": 2.5,
        "aisleWidth": 1.5
    },
    "classroom": {
        "width": 20.0,
        "depth": 15.0,
        "height": 5.0,
        "doorPosition": {
            "x": 0.0,
            "y": 0.0,
            "z": 7.5
        },
        "boardPosition": {
            "x": 0.0,
            "y": 1.5,
            "z": -7.0
        }
    }
}
```

### Full Schema Reference
| Field | Type | Description | Required |
|-------|------|-------------|----------|
| `levelId` | string | Unique identifier for the level | Yes |
| `difficulty` | string | "easy", "medium", or "hard" | Yes |
| `students` | integer | Number of students (4-10, even) | Yes |
| `deskLayout` | object | Desk layout configuration | No (defaults) |
| `deskLayout.rows` | integer | Always 2 (enforced) | No |
| `deskLayout.spacingX` | float | Spacing between desks in X direction (1.0-5.0m) | No |
| `deskLayout.spacingZ` | float | Spacing between desks in Z direction (1.0-5.0m) | No |
| `deskLayout.aisleWidth` | float | Gap between rows (1.0-3.0m) | No |
| `classroom` | object | Classroom dimensions | No (defaults) |
| `classroom.width` | float | Classroom width in meters (5.0-30.0) | No |
| `classroom.depth` | float | Classroom depth in meters (5.0-30.0) | No |
| `classroom.height` | float | Classroom height in meters (2.0-10.0) | No |
| `classroom.doorPosition` | Vector3 | Door position (optional, auto-calculated) | No |
| `classroom.boardPosition` | Vector3 | Board position (optional, auto-calculated) | No |
| `environment` | object | Environment settings (optional) | No |
| `environment.boardMaterial` | string | Material key for board (from AssetMapConfig) | No |
| `environment.floorMaterial` | string | Material key for floor | No |
| `environment.wallMaterial` | string | Material key for walls | No |
| `environment.autoSetupLighting` | bool | Whether to auto-setup lighting | No |
| `environment.ambientIntensity` | float | Ambient lighting intensity | No |

## Asset Mapping System

### Default Mappings
The system uses `AssetMapConfig` to map asset keys to prefabs. Default mappings:

| Asset Key | Default Prefab Path | Description |
|-----------|---------------------|-------------|
| DESK | `Assets/Prefabs/Chair.prefab` | Student desk (chair as placeholder) |
| CHAIR | `Assets/Prefabs/Chair.prefab` | Chair for desk |
| STUDENT | `Assets/Prefabs/Student.prefab` | Student character |
| BOARD | `Assets/school/Prefabs/props/board.prefab` | Classroom board |
| TEACHER | `Assets/Prefabs/TeacherPlayer.prefab` | Teacher player |
| DOOR | `Assets/school/Prefabs/props/a door.prefab` | Classroom door |
| FLOOR | `Assets/school/Prefabs/road/floor.prefab` | Floor prefab |
| WALL | `Assets/school/props/wall001.fbx` | Wall model |
| CEILING | `Assets/school/props/wall001.fbx` | Ceiling placeholder |

### Material Mappings
| Material Key | Default Material Path | Description |
|--------------|-----------------------|-------------|
| Default | `Assets/school/material/Materials/1.mat` | Fallback for pink materials |
| White | `Assets/school/material/Materials/1.mat` | White material for board |
| Floor | `Assets/school/material/Materials/floor_color.mat` | Floor material |
| Wall | `Assets/school/material/Materials/1.mat` | Wall material |

### Customizing Mappings
1. Select `Assets/Configs/DefaultAssetMap.asset`
2. In the Inspector, modify prefab and material references
3. Or use **Tools > FunClass > Update Asset Map References** to auto-assign

## Import Process Steps

1. **Schema Validation** - Validate JSON against schema rules
2. **Asset Map Loading** - Load or create AssetMapConfig
3. **Scene Preparation** - Create new scene or clear existing
4. **Desk Grid Generation** - Calculate and create 2-row desk grid
5. **Student Placement** - Instantiate desks and bind students
6. **Route Generation** - Create EscapeRoute and ReturnRoute per student
7. **Environment Setup** - Place board, door, walls, floor, teacher area
8. **Material Fixing** - Scan and fix pink/missing materials
9. **Lighting Setup** - Configure directional and classroom lights
10. **Scene Saving** - Save scene to `Assets/Levels/Generated/`

## Troubleshooting

### Common Issues

#### "No prefab found for asset key: DESK"
- Ensure `Assets/Configs/DefaultAssetMap.asset` exists
- Run **Tools > FunClass > Update Asset Map References**
- Check that `Assets/Prefabs/Chair.prefab` exists

#### "Students must be even"
- The system requires even number of students for 2-row grid
- Change student count to 4, 6, 8, or 10

#### "Door position outside classroom bounds"
- Ensure `doorPosition` values are within classroom dimensions
- Or omit `doorPosition` to use auto-calculation

#### Pink/Missing Materials
- The system automatically fixes pink materials
- If materials remain pink, check material mappings in AssetMapConfig

### Console Logs
Look for these success messages:
```
[EnhancedLevelImporter] Level 'level_01' imported successfully!
[EnhancedLevelImporter] Scene saved to: Assets/Levels/Generated/Level_level_01.unity
[EnhancedLevelImporter] Desks: 6, Students: 6
```

## Advanced Usage

### Manual Student Configuration
Add `studentConfigs` array to JSON for custom student personalities:
```json
"studentConfigs": [
    {
        "studentId": "student_0",
        "studentName": "Troublemaker",
        "deskId": "Desk_0_0",
        "personality": {
            "patience": 0.2,
            "attentionSpan": 0.3,
            "impulsiveness": 0.9
        }
    }
]
```

### Custom Asset Mapping
Add `assetMapping` to JSON for level-specific prefabs:
```json
"assetMapping": {
    "prefabMapping": {
        "DESK": "Assets/MyPrefabs/CustomDesk.prefab",
        "BOARD": "Assets/MyPrefabs/CustomBoard.prefab"
    }
}
```

### Environment Overrides
Customize lighting and materials:
```json
"environment": {
    "boardMaterial": "Chalkboard",
    "floorMaterial": "Carpet",
    "wallMaterial": "Brick",
    "autoSetupLighting": false
}
```

## Integration with Existing Systems

The enhanced import system is compatible with existing FunClass systems:

- Uses existing `StudentAgent` component on student prefabs
- Creates `StudentRoute` ScriptableObjects for routes
- Sets up scene hierarchy for existing gameplay systems
- Compatible with navigation and influence systems

## File Locations
- **JSON Files**: `Assets/Levels/Json/`
- **Generated Scenes**: `Assets/Levels/Generated/`
- **Asset Map Config**: `Assets/Configs/DefaultAssetMap.asset`
- **Editor Scripts**: `Assets/Scripts/Editor/`

## Menu Items
- **Tools > FunClass > Import Level From JSON** - Main import window
- **Tools > FunClass > Create Default Asset Map** - Create asset map
- **Tools > FunClass > Update Asset Map References** - Auto-assign prefabs

## Support
For issues, check console logs and ensure all required prefabs exist in the project.