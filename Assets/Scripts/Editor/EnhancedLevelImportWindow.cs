using UnityEngine;
using UnityEditor;
using System.IO;
using FunClass.Editor.Modules;
using FunClass.Editor.Data;

namespace FunClass.Editor
{
    /// <summary>
    /// Editor window for importing unified JSON levels (supports Auto, Manual, and Hybrid modes)
    /// Menu: Tools > FunClass > Import Level From JSON
    /// </summary>
    public class EnhancedLevelImportWindow : EditorWindow
    {
        private string selectedJsonPath = "";
        private Vector2 scrollPosition;
        
        [MenuItem("Tools/FunClass/Import Level From JSON")]
        public static void ShowWindow()
        {
            GetWindow<EnhancedLevelImportWindow>("Import JSON Level");
        }
        
        void OnGUI()
        {
            EditorGUILayout.LabelField("Unified JSON Level Import", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.HelpBox(
                "Import a classroom level from a JSON file using unified schema.\n" +
                "Supports Auto (auto-generation), Manual (legacy), and Hybrid modes.\n" +
                "Auto: Generate desks, environment, routes automatically\n" +
                "Manual: Use exact positions and prefabs from JSON\n" +
                "Hybrid: Mix of auto-generation and manual overrides",
                MessageType.Info);
            EditorGUILayout.Space();
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            // File selection section
            EditorGUILayout.LabelField("JSON File Selection", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.TextField("JSON Path", selectedJsonPath);
            
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFilePanel(
                    "Select JSON Level File",
                    "Assets/Levels/Json",
                    "json");
                    
                if (!string.IsNullOrEmpty(path))
                {
                    // Convert to relative path if within project
                    if (path.StartsWith(Application.dataPath))
                    {
                        selectedJsonPath = "Assets" + path.Substring(Application.dataPath.Length);
                    }
                    else
                    {
                        selectedJsonPath = path;
                    }
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Quick path buttons
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Use Sample Level 1"))
            {
                selectedJsonPath = "Assets/Levels/Json/level_01.json";
                if (!File.Exists(selectedJsonPath))
                {
                    EditorUtility.DisplayDialog("File Not Found",
                        "Sample level_01.json not found. Create it first using the button below.",
                        "OK");
                    selectedJsonPath = "";
                }
            }
            
            if (GUILayout.Button("Create Sample JSON"))
            {
                CreateSampleJSON();
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            
            // JSON preview
            if (!string.IsNullOrEmpty(selectedJsonPath) && File.Exists(selectedJsonPath))
            {
                EditorGUILayout.LabelField("JSON Preview", EditorStyles.boldLabel);
                
                try
                {
                    string jsonContent = File.ReadAllText(selectedJsonPath);
                    if (jsonContent.Length > 500)
                    {
                        jsonContent = jsonContent.Substring(0, 500) + "...\n[truncated]";
                    }
                    
                    EditorGUILayout.TextArea(jsonContent, GUILayout.Height(150));
                }
                catch (System.Exception e)
                {
                    EditorGUILayout.HelpBox($"Error reading JSON: {e.Message}", MessageType.Error);
                }
                
                EditorGUILayout.Space();
                
                // Import button
                if (GUILayout.Button("Import Level", GUILayout.Height(40)))
                {
                    ImportSelectedJSON();
                }
            }
            else if (!string.IsNullOrEmpty(selectedJsonPath))
            {
                EditorGUILayout.HelpBox($"File not found: {selectedJsonPath}", MessageType.Warning);
            }
            
            EditorGUILayout.Space();
            
            // Instructions section
            EditorGUILayout.LabelField("Instructions", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "1. Select a JSON file using the Browse button\n" +
                "2. Or use the sample level (create it first)\n" +
                "3. Click Import Level to generate the classroom\n" +
                "\n" +
                "Auto Mode (enhanced schema):\n" +
                "• Create 2-row desk grid with proper spacing\n" +
                "• Bind one student per desk\n" +
                "• Auto-generate escape and return routes\n" +
                "• Setup board, walls, door, and floor\n" +
                "\n" +
                "Manual Mode (legacy schema):\n" +
                "• Use exact student positions from JSON\n" +
                "• Use manual route definitions\n" +
                "• Place prefabs at specified positions\n" +
                "\n" +
                "All modes:\n" +
                "• Fix pink/missing materials\n" +
                "• Save scene to Assets/Levels/Generated/",
                MessageType.None);
            
            EditorGUILayout.EndScrollView();
        }
        
        private void ImportSelectedJSON()
        {
            if (string.IsNullOrEmpty(selectedJsonPath))
            {
                EditorUtility.DisplayDialog("Error", "Please select a JSON file first.", "OK");
                return;
            }
            
            if (!File.Exists(selectedJsonPath))
            {
                EditorUtility.DisplayDialog("Error", $"File not found: {selectedJsonPath}", "OK");
                return;
            }
            
            // Validate JSON extension
            if (!selectedJsonPath.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("Error", "Selected file is not a JSON file.", "OK");
                return;
            }
            
            UnifiedLevelImporter.ImportLevelFromJSON(selectedJsonPath);
        }
        
        private void CreateSampleJSON()
        {
            string sampleJsonPath = "Assets/Levels/Json/level_01.json";
            
            // Ensure directory exists
            string directory = Path.GetDirectoryName(sampleJsonPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // Create sample unified JSON schema (Auto mode)
            var sampleSchema = new UnifiedLevelSchema
            {
                levelId = "level_01",
                difficulty = "medium",
                students = 6,
                deskLayout = new DeskLayoutData
                {
                    rows = 2,
                    spacingX = 2.0f,
                    spacingZ = 2.5f,
                    aisleWidth = 1.5f
                },
                classroom = new ClassroomData
                {
                    width = 20f,
                    depth = 15f,
                    height = 5f,
                    doorPosition = new Vector3Data { x = 0, y = 0, z = 7.5f },
                    boardPosition = new Vector3Data { x = 0, y = 1.5f, z = -7f }
                },
                environment = new EnvironmentSettingsData
                {
                    boardSize = new Vector3Data(4f, 2f, 0.1f),
                    boardMaterial = "White",
                    floorMaterial = "Floor",
                    wallMaterial = "Wall",
                    autoSetupLighting = true,
                    ambientIntensity = 1.0f
                },
                goalSettings = new LevelGoalData
                {
                    maxDisruptionThreshold = 80f,
                    catastrophicDisruptionLevel = 95f,
                    maxAllowedCriticalStudents = 2,
                    catastrophicCriticalStudents = 4,
                    maxAllowedOutsideStudents = 2,
                    catastrophicOutsideStudents = 5,
                    maxOutsideTimePerStudent = 60f,
                    maxAllowedOutsideGracePeriod = 10f,
                    timeLimitSeconds = 300f,
                    requiredResolvedProblems = 5,
                    requiredCalmDowns = 3,
                    enableDisruptionTimeout = false,
                    disruptionTimeoutThreshold = 80f,
                    disruptionTimeoutSeconds = 60f,
                    disruptionTimeoutWarningSeconds = 15f,
                    oneStarScore = 100,
                    twoStarScore = 250,
                    threeStarScore = 500
                },
                influenceScopeSettings = null,
                studentInteractions = new System.Collections.Generic.List<StudentInteractionData>
                {
                    new StudentInteractionData
                    {
                        id = "sample_interaction_1",
                        sourceStudentId = "Student_0",
                        targetStudentId = "Student_1",
                        eventType = "ThrowingObject",
                        triggerCondition = "timeElapsed",
                        triggerValue = 30f,
                        probability = 0.5f,
                        oneTimeOnly = true,
                        description = "Sample interaction: Student 0 throws object at Student 1"
                    }
                },
                routes = null,
                expectedFlow = new ExpectedFlowData
                {
                    description = "Sample scenario: Students gradually become disruptive.",
                    steps = new System.Collections.Generic.List<FlowStep>
                    {
                        new FlowStep { stepId = "step1", description = "Students start calm" },
                        new FlowStep { stepId = "step2", description = "First student becomes disruptive" },
                        new FlowStep { stepId = "step3", description = "Influence spreads to neighbors" }
                    }
                }
            };
            
            string json = JsonUtility.ToJson(sampleSchema, true);
            File.WriteAllText(sampleJsonPath, json);
            
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("Sample JSON Created",
                $"Sample JSON created at:\n{sampleJsonPath}\n\nYou can now import this level.",
                "OK");
                
            selectedJsonPath = sampleJsonPath;
            Repaint();
        }
    }
}