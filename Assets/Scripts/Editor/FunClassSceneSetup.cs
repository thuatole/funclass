using UnityEngine;
using UnityEditor;

namespace FunClass.Editor
{
    /// <summary>
    /// Editor utility to quickly setup FunClass scene hierarchy
    /// Menu: Tools > FunClass > Setup Scene
    /// </summary>
    public class FunClassSceneSetup : EditorWindow
    {
        [MenuItem("Tools/FunClass/Setup Scene")]
        public static void SetupScene()
        {
            if (EditorUtility.DisplayDialog(
                "Setup FunClass Scene",
                "This will create the complete scene hierarchy for FunClass. Continue?",
                "Yes", "Cancel"))
            {
                CreateSceneHierarchy();
                Debug.Log("[FunClassSetup] Scene setup complete!");
            }
        }

        private static void CreateSceneHierarchy()
        {
            // 1. Create Managers
            CreateManagersGroup();

            // 2. Create Classroom Environment
            CreateClassroomGroup();

            // 3. Create Students
            CreateStudentsGroup();

            // 4. Create Teacher
            CreateTeacherGroup();

            // 5. Create UI
            CreateUIGroup();

            Debug.Log("[FunClassSetup] All groups created successfully!");
        }

        private static void CreateManagersGroup()
        {
            GameObject managers = CreateOrFind("=== MANAGERS ===");

            // Core Managers
            CreateManagerObject(managers, "GameStateManager", typeof(FunClass.Core.GameStateManager));
            CreateManagerObject(managers, "LevelManager", typeof(FunClass.Core.LevelManager));
            CreateManagerObject(managers, "ClassroomManager", typeof(FunClass.Core.ClassroomManager));
            CreateManagerObject(managers, "StudentEventManager", typeof(FunClass.Core.StudentEventManager));
            CreateManagerObject(managers, "TeacherScoreManager", typeof(FunClass.Core.TeacherScoreManager));
            CreateManagerObject(managers, "StudentInfluenceManager", typeof(FunClass.Core.StudentInfluenceManager));
            CreateManagerObject(managers, "StudentMovementManager", typeof(FunClass.Core.StudentMovementManager));

            Debug.Log("[FunClassSetup] Managers created");
        }

        private static void CreateClassroomGroup()
        {
            GameObject classroom = CreateOrFind("=== CLASSROOM ===");

            // Environment
            GameObject environment = CreateChild(classroom, "Environment");
            CreateChild(environment, "Floor");
            CreateChild(environment, "Walls");
            CreateChild(environment, "Ceiling");
            CreateChild(environment, "Door");
            CreateChild(environment, "Windows");

            // Furniture
            GameObject furniture = CreateChild(classroom, "Furniture");
            CreateChild(furniture, "TeacherDesk");
            CreateChild(furniture, "Whiteboard");
            CreateChild(furniture, "StudentDesks");

            // Waypoints & Routes
            GameObject waypoints = CreateChild(classroom, "Waypoints");
            CreateChild(waypoints, "EscapeRoute");
            CreateChild(waypoints, "ReturnRoute");
            CreateChild(waypoints, "WanderRoutes");

            Debug.Log("[FunClassSetup] Classroom created");
        }

        private static void CreateStudentsGroup()
        {
            GameObject students = CreateOrFind("=== STUDENTS ===");

            // Create example students
            for (int i = 1; i <= 5; i++)
            {
                GameObject student = CreateChild(students, $"Student_{i}");
                student.AddComponent<FunClass.Core.StudentAgent>();
                
                // Add visual placeholder
                GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                visual.name = "Visual";
                visual.transform.SetParent(student.transform);
                visual.transform.localPosition = Vector3.zero;
            }

            Debug.Log("[FunClassSetup] Students created");
        }

        private static void CreateTeacherGroup()
        {
            GameObject teacher = CreateOrFind("=== TEACHER ===");
            
            GameObject teacherObj = CreateChild(teacher, "Teacher");
            teacherObj.AddComponent<FunClass.Core.TeacherController>();

            // Add camera
            GameObject camera = new GameObject("TeacherCamera");
            camera.transform.SetParent(teacherObj.transform);
            camera.transform.localPosition = new Vector3(0, 1.6f, 0);
            camera.AddComponent<Camera>();
            camera.tag = "MainCamera";

            // Add visual placeholder
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(teacherObj.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(1, 1.2f, 1);

            Debug.Log("[FunClassSetup] Teacher created");
        }

        private static void CreateUIGroup()
        {
            GameObject ui = CreateOrFind("=== UI ===");

            // Canvas
            GameObject canvas = CreateChild(ui, "Canvas");
            Canvas canvasComponent = canvas.AddComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // UI Elements
            CreateChild(canvas, "InteractionPrompt");
            CreateChild(canvas, "DisruptionMeter");
            CreateChild(canvas, "ScoreDisplay");
            CreateChild(canvas, "TimerDisplay");

            Debug.Log("[FunClassSetup] UI created");
        }

        private static GameObject CreateOrFind(string name)
        {
            GameObject obj = GameObject.Find(name);
            if (obj == null)
            {
                obj = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
            }
            return obj;
        }

        private static GameObject CreateChild(GameObject parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent.transform);
            Undo.RegisterCreatedObjectUndo(child, "Create " + name);
            return child;
        }

        private static void CreateManagerObject(GameObject parent, string name, System.Type componentType)
        {
            GameObject obj = CreateChild(parent, name);
            obj.AddComponent(componentType);
        }

        [MenuItem("Tools/FunClass/Clear Scene")]
        public static void ClearScene()
        {
            if (EditorUtility.DisplayDialog(
                "Clear Scene",
                "This will delete all FunClass objects. Continue?",
                "Yes", "Cancel"))
            {
                DeleteGroup("=== MANAGERS ===");
                DeleteGroup("=== CLASSROOM ===");
                DeleteGroup("=== STUDENTS ===");
                DeleteGroup("=== TEACHER ===");
                DeleteGroup("=== UI ===");
                
                Debug.Log("[FunClassSetup] Scene cleared!");
            }
        }

        private static void DeleteGroup(string name)
        {
            GameObject obj = GameObject.Find(name);
            if (obj != null)
            {
                Undo.DestroyObjectImmediate(obj);
            }
        }

        [MenuItem("Tools/FunClass/Setup Prefab Variants")]
        public static void SetupPrefabVariants()
        {
            Debug.Log("[FunClassSetup] Creating prefab variants...");
            
            // Create Prefabs folder if doesn't exist
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            Debug.Log("[FunClassSetup] Prefab folder ready at Assets/Prefabs");
            EditorUtility.DisplayDialog("Prefab Setup", 
                "Prefabs folder created at Assets/Prefabs\n\nDrag objects from hierarchy to this folder to create prefabs.", 
                "OK");
        }
    }
}
