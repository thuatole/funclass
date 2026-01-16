using UnityEngine;
using System.Collections.Generic;

namespace FunClass.Core
{
    public class StudentManager : MonoBehaviour
    {
        public static StudentManager Instance { get; private set; }

        [Header("Spawning")]
        [SerializeField] private GameObject studentPrefab;
        [SerializeField] private Transform studentSpawnParent;

        private List<StudentAgent> activeStudents = new List<StudentAgent>();
        private bool isActive = false;

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

        void OnEnable()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged += HandleGameStateChanged;
            }
        }

        void OnDisable()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged -= HandleGameStateChanged;
            }
        }

        private void HandleGameStateChanged(GameState oldState, GameState newState)
        {
            if (newState == GameState.InLevel)
            {
                ActivateStudents();
            }
            else if (oldState == GameState.InLevel)
            {
                DeactivateStudents();
            }
        }

        private void ActivateStudents()
        {
            isActive = true;
            Debug.Log($"[StudentManager] Managing {activeStudents.Count} students");
        }

        private void DeactivateStudents()
        {
            isActive = false;
            ClearAllStudents();
        }

        public void SpawnStudent(StudentConfig config, Vector3 position, Quaternion rotation)
        {
            if (studentPrefab == null)
            {
                Debug.LogError("[StudentManager] No student prefab assigned");
                return;
            }

            GameObject studentObj = Instantiate(studentPrefab, position, rotation);
            
            if (studentSpawnParent != null)
            {
                studentObj.transform.SetParent(studentSpawnParent);
            }

            StudentAgent agent = studentObj.GetComponent<StudentAgent>();
            if (agent == null)
            {
                agent = studentObj.AddComponent<StudentAgent>();
            }

            agent.Initialize(config);
            activeStudents.Add(agent);

            Debug.Log($"[StudentManager] Spawned student: {config.studentName} at {position}");
        }

        public void SpawnStudentsFromConfigs(List<StudentConfig> configs, List<Vector3> positions)
        {
            if (configs == null || positions == null)
            {
                Debug.LogWarning("[StudentManager] Cannot spawn students - configs or positions are null");
                return;
            }

            int count = Mathf.Min(configs.Count, positions.Count);
            
            for (int i = 0; i < count; i++)
            {
                SpawnStudent(configs[i], positions[i], Quaternion.identity);
            }

            Debug.Log($"[StudentManager] Spawned {count} students");
        }

        public void ClearAllStudents()
        {
            foreach (StudentAgent student in activeStudents)
            {
                if (student != null)
                {
                    Destroy(student.gameObject);
                }
            }

            activeStudents.Clear();
            Debug.Log("[StudentManager] Cleared all students");
        }

        public List<StudentAgent> GetActiveStudents()
        {
            return new List<StudentAgent>(activeStudents);
        }

        public int GetStudentCount()
        {
            return activeStudents.Count;
        }

        public StudentAgent GetStudentByName(string studentName)
        {
            return activeStudents.Find(s => s.Config != null && s.Config.studentName == studentName);
        }
    }
}
