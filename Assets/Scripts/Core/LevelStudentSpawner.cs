using UnityEngine;
using System.Collections.Generic;

namespace FunClass.Core
{
    public class LevelStudentSpawner : MonoBehaviour
    {
        [Header("Spawn Configuration")]
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private bool spawnOnLevelLoad = true;

        void OnEnable()
        {
            if (LevelLoader.Instance != null)
            {
                LevelLoader.Instance.OnLevelLoaded += HandleLevelLoaded;
            }
        }

        void OnDisable()
        {
            if (LevelLoader.Instance != null)
            {
                LevelLoader.Instance.OnLevelLoaded -= HandleLevelLoaded;
            }
        }

        void Start()
        {
            if (spawnOnLevelLoad && LevelLoader.Instance != null && LevelLoader.Instance.CurrentLevel != null)
            {
                SpawnStudentsForLevel(LevelLoader.Instance.CurrentLevel);
            }
        }

        private void HandleLevelLoaded(LevelConfig level)
        {
            if (spawnOnLevelLoad)
            {
                SpawnStudentsForLevel(level);
            }
        }

        private void SpawnStudentsForLevel(LevelConfig level)
        {
            if (level == null || level.students == null || level.students.Count == 0)
            {
                Debug.Log("[LevelStudentSpawner] No students to spawn for this level");
                return;
            }

            if (StudentManager.Instance == null)
            {
                Debug.LogError("[LevelStudentSpawner] StudentManager not found");
                return;
            }

            List<Vector3> positions = GetSpawnPositions(level.students.Count);
            StudentManager.Instance.SpawnStudentsFromConfigs(level.students, positions);
        }

        private List<Vector3> GetSpawnPositions(int count)
        {
            List<Vector3> positions = new List<Vector3>();

            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    int spawnIndex = i % spawnPoints.Length;
                    positions.Add(spawnPoints[spawnIndex].position);
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    Vector3 position = new Vector3(i * 2f, 0f, 0f);
                    positions.Add(position);
                }
                Debug.LogWarning("[LevelStudentSpawner] No spawn points assigned, using default positions");
            }

            return positions;
        }

        public void ManualSpawn()
        {
            if (LevelLoader.Instance != null && LevelLoader.Instance.CurrentLevel != null)
            {
                SpawnStudentsForLevel(LevelLoader.Instance.CurrentLevel);
            }
        }
    }
}
