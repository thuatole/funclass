using UnityEngine;
using UnityEditor;
using FunClass.Core;

namespace FunClass.Editor
{
    /// <summary>
    /// Utility to fix LevelConfig route references after git revert
    /// </summary>
    public class FixLevelConfigRoutes : EditorWindow
    {
        [MenuItem("FunClass/Fix Level Config Routes")]
        public static void ShowWindow()
        {
            GetWindow<FixLevelConfigRoutes>("Fix Routes");
        }

        private void OnGUI()
        {
            GUILayout.Label("Fix LevelConfig Route References", EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("Fix All LevelConfigs"))
            {
                FixAllLevelConfigs();
            }

            GUILayout.Space(10);
            
            if (GUILayout.Button("Fix Vomit_Panic_Scenario"))
            {
                FixSpecificLevel("Vomit_Panic_Scenario");
            }
        }

        private static void FixAllLevelConfigs()
        {
            string[] guids = AssetDatabase.FindAssets("t:LevelConfig");
            int fixedCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                LevelConfig config = AssetDatabase.LoadAssetAtPath<LevelConfig>(path);

                if (config != null && FixLevelConfig(config))
                {
                    fixedCount++;
                }
            }

            Debug.Log($"[FixRoutes] Fixed {fixedCount} LevelConfigs");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void FixSpecificLevel(string levelName)
        {
            string configPath = $"Assets/Configs/{levelName}/{levelName}.asset";
            LevelConfig config = AssetDatabase.LoadAssetAtPath<LevelConfig>(configPath);

            if (config == null)
            {
                Debug.LogError($"[FixRoutes] LevelConfig not found at {configPath}");
                return;
            }

            if (FixLevelConfig(config))
            {
                Debug.Log($"[FixRoutes] ✓ Fixed {levelName}");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            else
            {
                Debug.LogWarning($"[FixRoutes] No fixes needed for {levelName}");
            }
        }

        private static bool FixLevelConfig(LevelConfig config)
        {
            bool changed = false;
            string levelName = config.name;

            // Fix escape route
            if (config.escapeRoute == null)
            {
                string escapePath = $"Assets/Configs/{levelName}/Routes/EscapeRoute.asset";
                StudentRoute escapeRoute = AssetDatabase.LoadAssetAtPath<StudentRoute>(escapePath);

                if (escapeRoute != null)
                {
                    config.escapeRoute = escapeRoute;
                    EditorUtility.SetDirty(config);
                    changed = true;
                    Debug.Log($"[FixRoutes] ✓ Assigned EscapeRoute to {levelName}");
                }
                else
                {
                    Debug.LogWarning($"[FixRoutes] EscapeRoute not found at {escapePath}");
                }
            }

            // Fix return route
            if (config.returnRoute == null)
            {
                string returnPath = $"Assets/Configs/{levelName}/Routes/ReturnRoute.asset";
                StudentRoute returnRoute = AssetDatabase.LoadAssetAtPath<StudentRoute>(returnPath);

                if (returnRoute != null)
                {
                    config.returnRoute = returnRoute;
                    EditorUtility.SetDirty(config);
                    changed = true;
                    Debug.Log($"[FixRoutes] ✓ Assigned ReturnRoute to {levelName}");
                }
                else
                {
                    Debug.LogWarning($"[FixRoutes] ReturnRoute not found at {returnPath}");
                }
            }

            // Fix classroom door
            if (config.classroomDoor == null)
            {
                GameObject door = GameObject.Find("ClassroomDoor");
                if (door != null)
                {
                    config.classroomDoor = door.transform;
                    EditorUtility.SetDirty(config);
                    changed = true;
                    Debug.Log($"[FixRoutes] ✓ Assigned ClassroomDoor to {levelName}");
                }
            }

            return changed;
        }
    }
}
