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
            
            // Verify escape route
            if (level.escapeRoute != null)
            {
                Debug.Log($"[LevelLoader] ✓ Escape route loaded: {level.escapeRoute.routeName} with {level.escapeRoute.waypoints?.Count ?? 0} waypoints");
            }
            else
            {
                Debug.LogWarning($"[LevelLoader] ⚠ No escape route in LevelConfig!");
            }
            
            OnLevelLoaded?.Invoke(level);
        }
    }
}
