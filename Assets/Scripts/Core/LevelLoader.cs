using UnityEngine;
using System;

namespace FunClass.Core
{
    public class LevelLoader : MonoBehaviour
    {
        public static LevelLoader Instance { get; private set; }

        [SerializeField] private LevelConfig currentLevel;

        public LevelConfig CurrentLevel => currentLevel;
        public event Action<LevelConfig> OnLevelLoaded;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            if (currentLevel != null)
            {
                LoadLevel(currentLevel);
            }
            else
            {
                Debug.LogWarning("[LevelLoader] No level assigned. Assign a LevelConfig in the inspector.");
            }
        }

        public void LoadLevel(LevelConfig level)
        {
            currentLevel = level;
            Debug.Log($"[LevelLoader] Loading level: {level.levelId} (Grade {level.grade})");
            Debug.Log($"[LevelLoader] Description: {level.description}");
            
            // ALWAYS load routes from Assets at runtime (bypass serialization issues)
            Debug.Log($"[LevelLoader] Loading routes from Assets...");
            TryLoadRouteFromAssets(level, "EscapeRoute", true);
            TryLoadRouteFromAssets(level, "ReturnRoute", false);
            
            // Verify escape route
            if (level.escapeRoute != null)
            {
                Debug.Log($"[LevelLoader] ✓ Escape route loaded: {level.escapeRoute.routeName} with {level.escapeRoute.waypoints?.Count ?? 0} waypoints");
                
                // Force refresh waypoints from scene
                level.escapeRoute.RefreshWaypointsFromScene();
                Debug.Log($"[LevelLoader] Refreshed escape route waypoints: {level.escapeRoute.waypoints?.Count ?? 0}");
            }
            else
            {
                Debug.LogError($"[LevelLoader] ✗ CRITICAL: No escape route available!");
            }
            
            if (level.returnRoute != null)
            {
                Debug.Log($"[LevelLoader] ✓ Return route loaded: {level.returnRoute.routeName}");
                level.returnRoute.RefreshWaypointsFromScene();
            }
            
            OnLevelLoaded?.Invoke(level);
        }
        
        private void TryLoadRouteFromAssets(LevelConfig level, string routeName, bool isEscapeRoute)
        {
            // Extract level folder name from LevelConfig asset name
            // LevelConfig name format: "LevelName_Config" or just "LevelName"
            string levelFolderName = level.name.Replace("_Config", "");
            string assetPath = $"Assets/Configs/{levelFolderName}/Routes/{routeName}.asset";
            
            Debug.Log($"[LevelLoader] Attempting to load route from: {assetPath}");
            
#if UNITY_EDITOR
            var route = UnityEditor.AssetDatabase.LoadAssetAtPath<StudentRoute>(assetPath);
            if (route != null)
            {
                if (isEscapeRoute)
                {
                    level.escapeRoute = route;
                }
                else
                {
                    level.returnRoute = route;
                }
                
                Debug.Log($"[LevelLoader] ✓ Loaded {routeName} from Assets at runtime");
            }
            else
            {
                Debug.LogError($"[LevelLoader] ✗ Failed to load {routeName} from {assetPath}");
            }
#else
            Debug.LogError($"[LevelLoader] Cannot load routes at runtime in build - routes must be assigned in editor");
#endif
        }
    }
}
