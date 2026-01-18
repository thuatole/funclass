using UnityEngine;
using UnityEditor;

namespace FunClass.Editor.Modules
{
    /// <summary>
    /// Module tạo scene hierarchy
    /// </summary>
    public static class SceneHierarchyBuilder
    {
        /// <summary>
        /// Tạo toàn bộ hierarchy cho level
        /// </summary>
        public static void CreateCompleteHierarchy()
        {
            CreateManagersGroup();
            CreateClassroomGroup();
            CreateTeacherGroup();
            CreateUIGroup();
            
            // Create empty students group
            EditorUtils.CreateOrFind("=== STUDENTS ===");
            
            Debug.Log("[SceneHierarchyBuilder] Created complete hierarchy");
        }

        /// <summary>
        /// Tạo students từ list configs
        /// </summary>
        public static void CreateStudents(System.Collections.Generic.List<FunClass.Core.StudentConfig> configs)
        {
            if (configs == null || configs.Count == 0)
            {
                Debug.LogWarning("[SceneHierarchyBuilder] No student configs provided");
                return;
            }

            GameObject studentsGroup = EditorUtils.CreateOrFind("=== STUDENTS ===");
            
            // Delete existing students
            for (int i = studentsGroup.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(studentsGroup.transform.GetChild(i).gameObject);
            }
            
            // Create new students
            for (int i = 0; i < configs.Count; i++)
            {
                CreateStudent(studentsGroup, configs[i], i);
            }
            
            Debug.Log($"[SceneHierarchyBuilder] Created {configs.Count} students");
        }

        /// <summary>
        /// Tạo toàn bộ managers
        /// </summary>
        public static GameObject CreateManagersGroup()
        {
            GameObject managers = EditorUtils.CreateOrFind("=== MANAGERS ===");

            CreateManagerObject(managers, "GameStateManager", typeof(FunClass.Core.GameStateManager));
            CreateManagerObject(managers, "LevelManager", typeof(FunClass.Core.LevelManager));
            CreateManagerObject(managers, "ClassroomManager", typeof(FunClass.Core.ClassroomManager));
            CreateManagerObject(managers, "StudentEventManager", typeof(FunClass.Core.StudentEventManager));
            CreateManagerObject(managers, "TeacherScoreManager", typeof(FunClass.Core.TeacherScoreManager));
            CreateManagerObject(managers, "StudentInfluenceManager", typeof(FunClass.Core.StudentInfluenceManager));
            CreateManagerObject(managers, "StudentMovementManager", typeof(FunClass.Core.StudentMovementManager));

            return managers;
        }

        /// <summary>
        /// Tạo classroom environment
        /// </summary>
        public static GameObject CreateClassroomGroup()
        {
            GameObject classroom = EditorUtils.CreateOrFind("=== CLASSROOM ===");

            // Environment
            GameObject environment = EditorUtils.CreateChild(classroom, "Environment");
            
            // Create Floor with actual geometry and collider
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.SetParent(environment.transform);
            floor.transform.localPosition = Vector3.zero;
            floor.transform.localScale = new Vector3(10, 1, 10); // 100x100 units floor
            
            // Ensure floor has collider
            var floorCollider = floor.GetComponent<Collider>();
            if (floorCollider != null)
            {
                floorCollider.enabled = true;
            }
            
            EditorUtils.CreateChild(environment, "Walls");
            EditorUtils.CreateChild(environment, "Ceiling");
            EditorUtils.CreateChild(environment, "Door");
            EditorUtils.CreateChild(environment, "Windows");

            // Furniture
            GameObject furniture = EditorUtils.CreateChild(classroom, "Furniture");
            EditorUtils.CreateChild(furniture, "TeacherDesk");
            EditorUtils.CreateChild(furniture, "Whiteboard");
            EditorUtils.CreateChild(furniture, "StudentDesks");

            // Waypoints
            GameObject waypoints = EditorUtils.CreateChild(classroom, "Waypoints");
            EditorUtils.CreateChild(waypoints, "EscapeRoute");
            EditorUtils.CreateChild(waypoints, "ReturnRoute");
            EditorUtils.CreateChild(waypoints, "WanderRoutes");

            return classroom;
        }

        /// <summary>
        /// Tạo students trong scene với configs
        /// </summary>
        public static GameObject CreateStudentsGroup(int studentCount, FunClass.Core.StudentConfig[] configs, string levelName)
        {
            GameObject studentsGroup = EditorUtils.CreateOrFind("=== STUDENTS ===");
            
            // Delete default students
            for (int i = studentsGroup.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(studentsGroup.transform.GetChild(i).gameObject);
            }
            
            // Create new students
            for (int i = 0; i < studentCount && i < configs.Length; i++)
            {
                CreateStudent(studentsGroup, configs[i], i);
            }

            return studentsGroup;
        }

        /// <summary>
        /// Tạo teacher với camera
        /// </summary>
        public static GameObject CreateTeacherGroup()
        {
            GameObject teacher = EditorUtils.CreateOrFind("=== TEACHER ===");
            
            GameObject teacherObj = EditorUtils.CreateChild(teacher, "Teacher");
            teacherObj.SetActive(true); // Ensure active
            
            // Add CharacterController (required by TeacherController)
            var charController = teacherObj.AddComponent<CharacterController>();
            charController.center = new Vector3(0, 1, 0);
            charController.height = 2f;
            charController.radius = 0.5f;
            
            // Add TeacherController
            var teacherController = teacherObj.AddComponent<FunClass.Core.TeacherController>();
            teacherController.enabled = true; // Ensure enabled

            // Add camera
            GameObject camera = new GameObject("TeacherCamera");
            camera.transform.SetParent(teacherObj.transform);
            camera.transform.localPosition = new Vector3(0, 1.6f, 0);
            var cam = camera.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.SetActive(true); // Ensure active
            
            // Assign camera to TeacherController via SerializedObject
            var so = new UnityEditor.SerializedObject(teacherController);
            so.FindProperty("playerCamera").objectReferenceValue = cam;
            so.FindProperty("cameraTransform").objectReferenceValue = camera.transform;
            so.ApplyModifiedProperties();

            // Add visual placeholder
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(teacherObj.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(1, 1.2f, 1);
            
            Debug.Log($"[SceneHierarchyBuilder] Created Teacher: active={teacherObj.activeSelf}, controller enabled={teacherController.enabled}");

            return teacher;
        }

        /// <summary>
        /// Tạo UI canvas
        /// </summary>
        public static GameObject CreateUIGroup()
        {
            GameObject ui = EditorUtils.CreateOrFind("=== UI ===");

            // Canvas
            GameObject canvas = EditorUtils.CreateChild(ui, "Canvas");
            Canvas canvasComponent = canvas.AddComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // UI Elements
            EditorUtils.CreateChild(canvas, "InteractionPrompt");
            EditorUtils.CreateChild(canvas, "DisruptionMeter");
            EditorUtils.CreateChild(canvas, "ScoreDisplay");
            EditorUtils.CreateChild(canvas, "TimerDisplay");

            // EventSystem
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            return ui;
        }

        private static void CreateStudent(GameObject parent, FunClass.Core.StudentConfig config, int index)
        {
            GameObject student = new GameObject($"Student_{config.studentName}");
            student.transform.SetParent(parent.transform);
            
            // Position in grid
            int row = index / 3;
            int col = index % 3;
            student.transform.position = new Vector3(col * 2f - 2f, 0, -row * 2f);
            
            // Add StudentAgent
            var agent = student.AddComponent<FunClass.Core.StudentAgent>();
            
            // Assign config using reflection
            var field = typeof(FunClass.Core.StudentAgent).GetField("config", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(agent, config);
            }
            
            // Add visual with random color
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(student.transform);
            visual.transform.localPosition = Vector3.zero;
            
            var renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f);
                renderer.material = mat;
            }
        }

        private static void CreateManagerObject(GameObject parent, string name, System.Type componentType)
        {
            GameObject obj = EditorUtils.CreateChild(parent, name);
            obj.AddComponent(componentType);
        }
    }
}
