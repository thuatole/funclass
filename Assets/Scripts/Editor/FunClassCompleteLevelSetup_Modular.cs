using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using FunClass.Editor.Modules;

namespace FunClass.Editor
{
    /// <summary>
    /// Version modular của Complete Level Setup - sử dụng các modules riêng biệt
    /// Menu: Tools > FunClass > Create Complete Level (Modular)
    /// </summary>
    public class FunClassCompleteLevelSetup_Modular : EditorWindow
    {
        private string levelName = "Level_01";
        private int studentCount = 5;
        private LevelConfigGenerator.Difficulty difficulty = LevelConfigGenerator.Difficulty.Normal;
        private bool createWaypoints = true;

        [MenuItem("Tools/FunClass/Create Complete Level (Modular)")]
        public static void ShowWindow()
        {
            FunClassCompleteLevelSetup_Modular window = GetWindow<FunClassCompleteLevelSetup_Modular>("Tạo Màn Chơi");
            window.minSize = new Vector2(400, 450);
            window.Show();
        }

        void OnGUI()
        {
            GUILayout.Label("TẠO MÀN CHƠI HOÀN CHỈNH (MODULAR)", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // Level Settings
            GUILayout.Label("Cài Đặt Màn Chơi:", EditorStyles.boldLabel);
            levelName = EditorGUILayout.TextField("Tên Màn:", levelName);
            studentCount = EditorGUILayout.IntSlider("Số Học Sinh:", studentCount, 3, 10);
            difficulty = (LevelConfigGenerator.Difficulty)EditorGUILayout.EnumPopup("Độ Khó:", difficulty);
            
            GUILayout.Space(10);
            
            // Options
            GUILayout.Label("Tùy Chọn:", EditorStyles.boldLabel);
            createWaypoints = EditorGUILayout.Toggle("Tạo Waypoints & Routes", createWaypoints);

            GUILayout.Space(20);

            // Preview
            EditorGUILayout.HelpBox(
                $"Sẽ tạo:\n" +
                $"• Scene: {levelName}.unity\n" +
                $"• {studentCount} học sinh với configs\n" +
                $"• Level Config với độ khó {difficulty}\n" +
                $"• Waypoints & Routes (nếu chọn)",
                MessageType.Info
            );

            GUILayout.Space(10);

            // Create Button
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("TẠO MÀN CHƠI HOÀN CHỈNH", GUILayout.Height(40)))
            {
                CreateCompleteLevel();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(10);

            // Quick Templates
            GUILayout.Label("Templates Nhanh:", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Màn Dễ (3 học sinh)"))
            {
                levelName = "Level_Easy";
                studentCount = 3;
                difficulty = LevelConfigGenerator.Difficulty.Easy;
            }
            
            if (GUILayout.Button("Màn Thường (5 học sinh)"))
            {
                levelName = "Level_Normal";
                studentCount = 5;
                difficulty = LevelConfigGenerator.Difficulty.Normal;
            }
            
            if (GUILayout.Button("Màn Khó (8 học sinh)"))
            {
                levelName = "Level_Hard";
                studentCount = 8;
                difficulty = LevelConfigGenerator.Difficulty.Hard;
            }
        }

        private void CreateCompleteLevel()
        {
            if (string.IsNullOrEmpty(levelName))
            {
                EditorUtility.DisplayDialog("Lỗi", "Vui lòng nhập tên màn chơi!", "OK");
                return;
            }

            EditorUtility.DisplayProgressBar("Tạo Màn Chơi", "Đang khởi tạo...", 0f);

            try
            {
                // 1. Create folders
                EditorUtils.CreateLevelFolderStructure(levelName);
                EditorUtility.DisplayProgressBar("Tạo Màn Chơi", "Tạo folders...", 0.1f);

                // 2. Create new scene
                CreateNewScene();
                EditorUtility.DisplayProgressBar("Tạo Màn Chơi", "Tạo scene...", 0.2f);

                // 3. Create scene hierarchy using modules
                SceneHierarchyBuilder.CreateManagersGroup();
                SceneHierarchyBuilder.CreateClassroomGroup();
                SceneHierarchyBuilder.CreateTeacherGroup();
                SceneHierarchyBuilder.CreateUIGroup();
                EditorUtility.DisplayProgressBar("Tạo Màn Chơi", "Tạo hierarchy...", 0.3f);

                // 4. Create configs using modules
                var (goalConfig, levelConfig) = LevelConfigGenerator.CreateLevelConfigs(levelName, difficulty);
                EditorUtility.DisplayProgressBar("Tạo Màn Chơi", "Tạo level configs...", 0.5f);

                // 5. Create student configs using module
                var studentConfigs = StudentConfigGenerator.CreateStudentConfigs(levelName, studentCount, difficulty);
                EditorUtility.DisplayProgressBar("Tạo Màn Chơi", "Tạo student configs...", 0.6f);

                // 6. Setup students in scene
                SceneHierarchyBuilder.CreateStudentsGroup(studentCount, studentConfigs, levelName);
                EditorUtility.DisplayProgressBar("Tạo Màn Chơi", "Setup students...", 0.7f);

                // 7. Create waypoints & routes using module
                if (createWaypoints)
                {
                    var (escapeRoute, returnRoute) = WaypointRouteBuilder.CreateRoutes(levelName);
                    WaypointRouteBuilder.AssignRoutesToLevelConfig(levelConfig, escapeRoute, returnRoute);
                    EditorUtility.DisplayProgressBar("Tạo Màn Chơi", "Tạo waypoints...", 0.8f);
                }

                // 8. Save scene
                SaveScene();
                EditorUtility.DisplayProgressBar("Tạo Màn Chơi", "Lưu scene...", 1f);

                EditorUtility.ClearProgressBar();
                
                EditorUtility.DisplayDialog(
                    "Thành Công!", 
                    $"Đã tạo màn chơi '{levelName}' hoàn chỉnh!\n\n" +
                    $"Scene: Assets/Scenes/{levelName}.unity\n" +
                    $"Configs: Assets/Configs/{levelName}/\n\n" +
                    $"Bạn có thể chơi thử ngay bây giờ!",
                    "OK"
                );

                Debug.Log($"[LevelSetup] ✅ Tạo màn chơi '{levelName}' thành công!");
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Lỗi", $"Có lỗi xảy ra:\n{e.Message}", "OK");
                Debug.LogError($"[LevelSetup] Error: {e.Message}\n{e.StackTrace}");
            }
        }

        private void CreateNewScene()
        {
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            // Remove default objects
            GameObject mainCamera = GameObject.Find("Main Camera");
            if (mainCamera != null) DestroyImmediate(mainCamera);
            
            GameObject directionalLight = GameObject.Find("Directional Light");
            if (directionalLight != null) DestroyImmediate(directionalLight);
        }

        private void SaveScene()
        {
            string scenePath = $"Assets/Scenes/{levelName}.unity";
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), scenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
