using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using FunClass.Editor.Data;
using FunClass.Editor.Modules;

namespace FunClass.Editor
{
    /// <summary>
    /// Editor Window để tạo custom level với UI trực quan
    /// Menu: Tools > FunClass > Custom Level Designer
    /// </summary>
    public class CustomLevelDesigner : EditorWindow
    {
        private LevelDataSchema levelData = new LevelDataSchema();
        private Vector2 scrollPosition;
        private int selectedTab = 0;
        private string[] tabs = { "General", "Students", "Routes", "Prefabs", "Import/Export" };

        // Student editor
        private string newStudentName = "";
        private Vector3 newStudentPosition = Vector3.zero;

        // Route editor
        private string newRouteName = "";
        private List<Vector3> routeWaypoints = new List<Vector3>();

        // Prefab editor
        private string newPrefabName = "";
        private string newPrefabType = "Decoration";
        private Vector3 newPrefabPosition = Vector3.zero;

        [MenuItem("Tools/FunClass/Custom Level Designer")]
        public static void ShowWindow()
        {
            CustomLevelDesigner window = GetWindow<CustomLevelDesigner>("Custom Level Designer");
            window.minSize = new Vector2(500, 600);
            window.Show();
        }

        void OnEnable()
        {
            InitializeDefaultData();
        }

        void OnGUI()
        {
            GUILayout.Label("CUSTOM LEVEL DESIGNER", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // Tabs
            selectedTab = GUILayout.Toolbar(selectedTab, tabs);
            GUILayout.Space(10);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            switch (selectedTab)
            {
                case 0: DrawGeneralTab(); break;
                case 1: DrawStudentsTab(); break;
                case 2: DrawRoutesTab(); break;
                case 3: DrawPrefabsTab(); break;
                case 4: DrawImportExportTab(); break;
            }

            EditorGUILayout.EndScrollView();

            GUILayout.Space(10);

            // Action buttons
            DrawActionButtons();
        }

        private void DrawGeneralTab()
        {
            GUILayout.Label("Level Settings", EditorStyles.boldLabel);
            
            levelData.levelName = EditorGUILayout.TextField("Level Name:", levelData.levelName);
            levelData.difficulty = EditorGUILayout.TextField("Difficulty:", levelData.difficulty);

            GUILayout.Space(10);
            GUILayout.Label("Goal Settings", EditorStyles.boldLabel);

            if (levelData.goalSettings == null)
                levelData.goalSettings = new LevelGoalData();

            levelData.goalSettings.maxDisruptionThreshold = EditorGUILayout.Slider(
                "Max Disruption:", levelData.goalSettings.maxDisruptionThreshold, 0f, 100f);
            
            levelData.goalSettings.catastrophicDisruptionLevel = EditorGUILayout.Slider(
                "Catastrophic Disruption:", levelData.goalSettings.catastrophicDisruptionLevel, 0f, 100f);
            
            levelData.goalSettings.maxAllowedCriticalStudents = EditorGUILayout.IntField(
                "Max Critical Students:", levelData.goalSettings.maxAllowedCriticalStudents);
            
            levelData.goalSettings.timeLimitSeconds = EditorGUILayout.FloatField(
                "Time Limit (seconds):", levelData.goalSettings.timeLimitSeconds);
            
            levelData.goalSettings.requiredResolvedProblems = EditorGUILayout.IntField(
                "Required Problems:", levelData.goalSettings.requiredResolvedProblems);

            GUILayout.Space(5);
            GUILayout.Label("Star Thresholds:", EditorStyles.boldLabel);
            
            levelData.goalSettings.oneStarScore = EditorGUILayout.IntField(
                "1 Star:", levelData.goalSettings.oneStarScore);
            levelData.goalSettings.twoStarScore = EditorGUILayout.IntField(
                "2 Stars:", levelData.goalSettings.twoStarScore);
            levelData.goalSettings.threeStarScore = EditorGUILayout.IntField(
                "3 Stars:", levelData.goalSettings.threeStarScore);
        }

        private void DrawStudentsTab()
        {
            GUILayout.Label("Students", EditorStyles.boldLabel);

            if (levelData.students == null)
                levelData.students = new List<StudentData>();

            // List existing students
            for (int i = 0; i < levelData.students.Count; i++)
            {
                EditorGUILayout.BeginHorizontal("box");
                
                GUILayout.Label($"{i + 1}. {levelData.students[i].studentName}", GUILayout.Width(150));
                
                if (GUILayout.Button("Edit", GUILayout.Width(50)))
                {
                    EditStudent(i);
                }
                
                if (GUILayout.Button("Delete", GUILayout.Width(60)))
                {
                    levelData.students.RemoveAt(i);
                }
                
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(10);
            GUILayout.Label("Add New Student", EditorStyles.boldLabel);

            newStudentName = EditorGUILayout.TextField("Name:", newStudentName);
            newStudentPosition = EditorGUILayout.Vector3Field("Position:", newStudentPosition);

            if (GUILayout.Button("Add Student", GUILayout.Height(30)))
            {
                AddNewStudent();
            }

            GUILayout.Space(10);
            
            if (GUILayout.Button("Quick Add 5 Students (Grid)", GUILayout.Height(25)))
            {
                QuickAddStudents(5);
            }
        }

        private void DrawRoutesTab()
        {
            GUILayout.Label("Routes", EditorStyles.boldLabel);

            if (levelData.routes == null)
                levelData.routes = new List<RouteData>();

            // List existing routes
            for (int i = 0; i < levelData.routes.Count; i++)
            {
                EditorGUILayout.BeginHorizontal("box");
                
                GUILayout.Label($"{levelData.routes[i].routeName} ({levelData.routes[i].waypoints?.Count ?? 0} waypoints)", 
                    GUILayout.Width(250));
                
                if (GUILayout.Button("Delete", GUILayout.Width(60)))
                {
                    levelData.routes.RemoveAt(i);
                }
                
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(10);
            GUILayout.Label("Create New Route", EditorStyles.boldLabel);

            newRouteName = EditorGUILayout.TextField("Route Name:", newRouteName);

            GUILayout.Label("Waypoints:", EditorStyles.boldLabel);
            
            for (int i = 0; i < routeWaypoints.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                routeWaypoints[i] = EditorGUILayout.Vector3Field($"WP {i}:", routeWaypoints[i]);
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    routeWaypoints.RemoveAt(i);
                }
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Add Waypoint"))
            {
                routeWaypoints.Add(Vector3.zero);
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Create Route", GUILayout.Height(30)))
            {
                CreateRoute();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("Quick Create Escape Route", GUILayout.Height(25)))
            {
                QuickCreateEscapeRoute();
            }

            if (GUILayout.Button("Quick Create Return Route", GUILayout.Height(25)))
            {
                QuickCreateReturnRoute();
            }
        }

        private void DrawPrefabsTab()
        {
            GUILayout.Label("Prefabs", EditorStyles.boldLabel);

            if (levelData.prefabs == null)
                levelData.prefabs = new List<PrefabData>();

            // List existing prefabs
            for (int i = 0; i < levelData.prefabs.Count; i++)
            {
                EditorGUILayout.BeginHorizontal("box");
                
                GUILayout.Label($"{levelData.prefabs[i].prefabName} ({levelData.prefabs[i].prefabType})", 
                    GUILayout.Width(250));
                
                if (GUILayout.Button("Delete", GUILayout.Width(60)))
                {
                    levelData.prefabs.RemoveAt(i);
                }
                
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(10);
            GUILayout.Label("Add New Prefab", EditorStyles.boldLabel);

            newPrefabName = EditorGUILayout.TextField("Name:", newPrefabName);
            newPrefabType = EditorGUILayout.TextField("Type:", newPrefabType);
            newPrefabPosition = EditorGUILayout.Vector3Field("Position:", newPrefabPosition);

            if (GUILayout.Button("Add Prefab", GUILayout.Height(30)))
            {
                AddNewPrefab();
            }
        }

        private void DrawImportExportTab()
        {
            GUILayout.Label("Import / Export", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Import: Load level data từ JSON file\n" +
                "Export: Save level data hiện tại ra JSON file",
                MessageType.Info
            );

            GUILayout.Space(10);

            // Import
            GUILayout.Label("Import from JSON", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Select JSON File to Import", GUILayout.Height(35)))
            {
                ImportFromJSON();
            }

            GUILayout.Space(20);

            // Export
            GUILayout.Label("Export to JSON", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Export Current Level Data", GUILayout.Height(35)))
            {
                ExportToJSON();
            }

            GUILayout.Space(20);

            // Sample templates
            GUILayout.Label("Sample Templates", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Create Sample JSON Template", GUILayout.Height(30)))
            {
                CreateSampleTemplate();
            }
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("CREATE LEVEL", GUILayout.Height(40)))
            {
                CreateLevel();
            }
            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("Clear All", GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog("Clear All", "Are you sure?", "Yes", "No"))
                {
                    InitializeDefaultData();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void InitializeDefaultData()
        {
            levelData = new LevelDataSchema
            {
                levelName = "CustomLevel_01",
                difficulty = "Normal",
                goalSettings = new LevelGoalData(),
                students = new List<StudentData>(),
                routes = new List<RouteData>(),
                prefabs = new List<PrefabData>()
            };
            
            routeWaypoints = new List<Vector3>();
        }

        private void AddNewStudent()
        {
            if (string.IsNullOrEmpty(newStudentName))
            {
                EditorUtility.DisplayDialog("Error", "Please enter student name", "OK");
                return;
            }

            levelData.students.Add(new StudentData
            {
                studentName = newStudentName,
                position = newStudentPosition,
                personality = new PersonalityData(),
                behaviors = new BehaviorData()
            });

            newStudentName = "";
            newStudentPosition = Vector3.zero;
        }

        private void QuickAddStudents(int count)
        {
            string[] names = { "Nam", "Lan", "Minh", "Hoa", "Tuan", "Mai", "Khoa", "Linh", "Duc", "Nga" };
            
            for (int i = 0; i < count; i++)
            {
                int row = i / 3;
                int col = i % 3;
                
                levelData.students.Add(new StudentData
                {
                    studentName = names[i % names.Length],
                    position = new Vector3(col * 2f - 2f, 0, -row * 2f),
                    personality = new PersonalityData(),
                    behaviors = new BehaviorData()
                });
            }
        }

        private void EditStudent(int index)
        {
            // TODO: Open detailed editor window
            Debug.Log($"Edit student {index}");
        }

        private void CreateRoute()
        {
            if (string.IsNullOrEmpty(newRouteName))
            {
                EditorUtility.DisplayDialog("Error", "Please enter route name", "OK");
                return;
            }

            if (routeWaypoints.Count < 2)
            {
                EditorUtility.DisplayDialog("Error", "Route needs at least 2 waypoints", "OK");
                return;
            }

            var route = new RouteData
            {
                routeName = newRouteName,
                routeType = "Custom",
                waypoints = new List<WaypointData>(),
                movementSpeed = 2f,
                rotationSpeed = 180f
            };

            for (int i = 0; i < routeWaypoints.Count; i++)
            {
                route.waypoints.Add(new WaypointData
                {
                    waypointName = $"{newRouteName}_WP{i}",
                    position = routeWaypoints[i],
                    waitDuration = 0f
                });
            }

            levelData.routes.Add(route);
            
            newRouteName = "";
            routeWaypoints.Clear();
        }

        private void QuickCreateEscapeRoute()
        {
            levelData.routes.Add(new RouteData
            {
                routeName = "EscapeRoute",
                routeType = "Escape",
                isRunning = true,
                movementSpeed = 4f,
                waypoints = new List<WaypointData>
                {
                    new WaypointData { waypointName = "Escape_0", position = new Vector3(0, 0, 0) },
                    new WaypointData { waypointName = "Escape_1", position = new Vector3(5, 0, 0) },
                    new WaypointData { waypointName = "Escape_2", position = new Vector3(10, 0, 0) }
                }
            });
        }

        private void QuickCreateReturnRoute()
        {
            levelData.routes.Add(new RouteData
            {
                routeName = "ReturnRoute",
                routeType = "Return",
                isRunning = false,
                movementSpeed = 2f,
                waypoints = new List<WaypointData>
                {
                    new WaypointData { waypointName = "Return_0", position = new Vector3(10, 0, 0) },
                    new WaypointData { waypointName = "Return_1", position = new Vector3(5, 0, 0) },
                    new WaypointData { waypointName = "Return_2", position = new Vector3(0, 0, 0) }
                }
            });
        }

        private void AddNewPrefab()
        {
            if (string.IsNullOrEmpty(newPrefabName))
            {
                EditorUtility.DisplayDialog("Error", "Please enter prefab name", "OK");
                return;
            }

            levelData.prefabs.Add(new PrefabData
            {
                prefabName = newPrefabName,
                prefabType = newPrefabType,
                position = newPrefabPosition,
                rotation = Vector3.zero,
                scale = Vector3.one
            });

            newPrefabName = "";
            newPrefabPosition = Vector3.zero;
        }

        private void ImportFromJSON()
        {
            string path = EditorUtility.OpenFilePanel("Select JSON File", Application.dataPath, "json");
            
            if (string.IsNullOrEmpty(path))
                return;

            var imported = JSONLevelImporter.ImportFromJSON(path);
            
            if (imported != null)
            {
                levelData = imported;
                EditorUtility.DisplayDialog("Success", $"Imported level: {levelData.levelName}", "OK");
            }
        }

        private void ExportToJSON()
        {
            string path = EditorUtility.SaveFilePanel(
                "Save Level JSON", 
                Application.dataPath, 
                $"{levelData.levelName}.json", 
                "json"
            );
            
            if (string.IsNullOrEmpty(path))
                return;

            JSONLevelImporter.ExportToJSON(levelData, path);
            EditorUtility.DisplayDialog("Success", $"Exported to: {path}", "OK");
        }

        private void CreateSampleTemplate()
        {
            string path = EditorUtility.SaveFilePanel(
                "Save Sample Template", 
                Application.dataPath, 
                "SampleLevel.json", 
                "json"
            );
            
            if (string.IsNullOrEmpty(path))
                return;

            var sample = CreateSampleLevelData();
            JSONLevelImporter.ExportToJSON(sample, path);
            
            EditorUtility.DisplayDialog("Success", 
                $"Sample template created!\n\nYou can edit this JSON file and import it later.", 
                "OK");
        }

        private void CreateLevel()
        {
            if (string.IsNullOrEmpty(levelData.levelName))
            {
                EditorUtility.DisplayDialog("Error", "Please enter level name", "OK");
                return;
            }

            JSONLevelImporter.CreateLevelFromData(levelData);
        }

        private LevelDataSchema CreateSampleLevelData()
        {
            return new LevelDataSchema
            {
                levelName = "SampleLevel",
                difficulty = "Normal",
                goalSettings = new LevelGoalData
                {
                    maxDisruptionThreshold = 80f,
                    timeLimitSeconds = 300f,
                    requiredResolvedProblems = 5,
                    oneStarScore = 100,
                    twoStarScore = 250,
                    threeStarScore = 500
                },
                students = new List<StudentData>
                {
                    new StudentData
                    {
                        studentName = "Nam",
                        position = new Vector3(-2, 0, 0),
                        personality = new PersonalityData
                        {
                            patience = 0.5f,
                            attentionSpan = 0.6f,
                            impulsiveness = 0.4f
                        },
                        behaviors = new BehaviorData
                        {
                            canFidget = true,
                            canStandUp = true,
                            minIdleTime = 2f,
                            maxIdleTime = 8f
                        }
                    }
                },
                routes = new List<RouteData>
                {
                    new RouteData
                    {
                        routeName = "EscapeRoute",
                        routeType = "Escape",
                        isRunning = true,
                        movementSpeed = 4f,
                        waypoints = new List<WaypointData>
                        {
                            new WaypointData { waypointName = "Escape_0", position = new Vector3(0, 0, 0) },
                            new WaypointData { waypointName = "Escape_1", position = new Vector3(10, 0, 0) }
                        }
                    }
                },
                prefabs = new List<PrefabData>()
            };
        }
    }
}
