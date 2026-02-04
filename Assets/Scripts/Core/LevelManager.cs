using UnityEngine;
using System;

namespace FunClass.Core
{
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        public bool IsLevelActive { get; private set; }
        public float LevelTimeElapsed { get; private set; }
        public float LevelTimeRemaining { get; private set; }

        public event Action OnLevelWon;
        public event Action<string> OnLevelLost;
        public event Action<int> OnStarRatingAchieved;

        private LevelGoalConfig currentGoal;
        private bool levelEnded = false;
        private int criticalStudentCount = 0;
        private float outsideStudentsExceededTime = 0f;
        private bool isTrackingOutsideExcess = false;

        private bool hasSubscribedToGameState = false;

        void Awake()
        {
            Debug.Log($"[LevelManager] ★ Awake called!");

            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[LevelManager] Duplicate instance, destroying...");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[LevelManager] ★ Instance set successfully");
        }

        void OnEnable()
        {
            Debug.Log($"[LevelManager] ★ OnEnable called!");

            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged += HandleGameStateChanged;
                hasSubscribedToGameState = true;
                Debug.Log($"[LevelManager] Subscribed to GameStateManager. Current state: {GameStateManager.Instance.CurrentState}");
            }
            else
            {
                Debug.LogWarning("[LevelManager] GameStateManager.Instance is NULL in OnEnable - will retry in Start()");
            }

            if (StudentEventManager.Instance != null)
            {
                StudentEventManager.Instance.OnEventLogged += HandleStudentEvent;
            }

            if (ClassroomManager.Instance != null)
            {
                ClassroomManager.Instance.OnOutsideStudentCountChanged += HandleOutsideStudentCountChanged;
            }
        }

        void Start()
        {
            Debug.Log("[LevelManager] Start called");

            // Retry subscription if failed in OnEnable
            if (!hasSubscribedToGameState && GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged += HandleGameStateChanged;
                hasSubscribedToGameState = true;
                Debug.Log($"[LevelManager] ★ Late subscription to GameStateManager. Current state: {GameStateManager.Instance.CurrentState}");

                // Check if already in InLevel
                if (GameStateManager.Instance.CurrentState == GameState.InLevel)
                {
                    Debug.Log("[LevelManager] Already in InLevel, starting level");
                    StartLevel();
                }
            }
        }

        void OnDisable()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged -= HandleGameStateChanged;
            }

            if (StudentEventManager.Instance != null)
            {
                StudentEventManager.Instance.OnEventLogged -= HandleStudentEvent;
            }

            if (ClassroomManager.Instance != null)
            {
                ClassroomManager.Instance.OnOutsideStudentCountChanged -= HandleOutsideStudentCountChanged;
            }
        }

        private float lastLevelManagerDebugTime = 0f;

        void Update()
        {
            if (!IsLevelActive || levelEnded) return;

            LevelTimeElapsed += Time.deltaTime;

            if (currentGoal != null && currentGoal.hasTimeLimit)
            {
                LevelTimeRemaining = currentGoal.timeLimitSeconds - LevelTimeElapsed;

                // Debug log every 5 seconds
                if (Time.time - lastLevelManagerDebugTime >= 5f)
                {
                    lastLevelManagerDebugTime = Time.time;
                    ScoreSummary summary = TeacherScoreManager.Instance?.GetScoreSummary() ?? new ScoreSummary();
                    float disruption = ClassroomManager.Instance?.DisruptionLevel ?? 0f;

                    Debug.Log($"[LevelManager] ═══ PROGRESS ═══");
                    Debug.Log($"[LevelManager] Time remaining: {LevelTimeRemaining:F0}s / {currentGoal.timeLimitSeconds}s");
                    Debug.Log($"[LevelManager] Disruption: {disruption:F1}% (need ≤ {currentGoal.winDisruptionThreshold}%)");
                    Debug.Log($"[LevelManager] Problems: {summary.resolvedProblems} / {currentGoal.requiredResolvedProblems}");
                    Debug.Log($"[LevelManager] CalmDowns: {summary.calmDowns} / {currentGoal.requiredCalmDowns}");
                }

                if (LevelTimeRemaining <= 0)
                {
                    CheckWinConditions();
                }
            }
            else if (Time.time - lastLevelManagerDebugTime >= 5f)
            {
                lastLevelManagerDebugTime = Time.time;
                Debug.LogWarning($"[LevelManager] currentGoal is NULL or hasTimeLimit=false!");
            }

            CheckLoseConditions();
        }

        private void HandleGameStateChanged(GameState oldState, GameState newState)
        {
            if (newState == GameState.InLevel)
            {
                StartLevel();
            }
            else if (oldState == GameState.InLevel)
            {
                EndLevel();
            }
        }

        private void StartLevel()
        {
            IsLevelActive = true;
            levelEnded = false;
            LevelTimeElapsed = 0f;
            criticalStudentCount = 0;
            outsideStudentsExceededTime = 0f;
            isTrackingOutsideExcess = false;

            if (LevelLoader.Instance != null && LevelLoader.Instance.CurrentLevel != null)
            {
                currentGoal = LevelLoader.Instance.CurrentLevel.levelGoal;
                if (currentGoal != null && currentGoal.hasTimeLimit)
                {
                    LevelTimeRemaining = currentGoal.timeLimitSeconds;
                }

                // Load student interactions for scripted events
                LoadStudentInteractions();
            }

            Debug.Log("[LevelManager] Level started");
        }

        /// <summary>
        /// Handles changes in the number of students outside the classroom
        /// </summary>
        private void HandleOutsideStudentCountChanged(int count)
        {
            // This event is used to trigger immediate checks if needed
            // The actual lose condition checking happens in CheckLoseConditions()
        }

        /// <summary>
        /// Gets the current level configuration
        /// </summary>
        public LevelConfig GetCurrentLevelConfig()
        {
            if (LevelLoader.Instance != null)
            {
                return LevelLoader.Instance.CurrentLevel;
            }
            return null;
        }

        private void EndLevel()
        {
            IsLevelActive = false;
            Debug.Log("[LevelManager] Level ended");
        }

        /// <summary>
        /// Load student interactions from level config and pass to processor
        /// </summary>
        private void LoadStudentInteractions()
        {
            LevelConfig levelConfig = GetCurrentLevelConfig();
            if (levelConfig == null || levelConfig.studentInteractions == null)
            {
                Debug.Log("[LevelManager] No student interactions in level config");
                return;
            }

            // Ensure StudentInteractionProcessor exists at runtime
            if (StudentInteractionProcessor.Instance == null)
            {
                Debug.Log("[LevelManager] StudentInteractionProcessor not found - creating at runtime");
                GameObject processorObj = new GameObject("StudentInteractionProcessor");
                processorObj.AddComponent<StudentInteractionProcessor>();
                DontDestroyOnLoad(processorObj);
            }

            if (StudentInteractionProcessor.Instance != null)
            {
                StudentInteractionProcessor.Instance.LoadRuntimeInteractions(levelConfig.studentInteractions);
                Debug.Log($"[LevelManager] Loaded {levelConfig.studentInteractions.Count} student interactions");
            }
            
            // Setup desks with StudentInteractableObject at runtime (avoids import-time serialization issues)
            SetupDesksForInteraction();
        }
        
        /// <summary>
        /// Setup desks with StudentInteractableObject at runtime to avoid import-time serialization issues
        /// </summary>
        private void SetupDesksForInteraction()
        {
            Debug.Log("[LevelManager] Setting up desks with StudentInteractableObject at runtime");
            
            // Find desks - check "Desks" group first, then fallback to tag
            Transform desksGroup = GameObject.Find("Desks")?.transform;
            
            if (desksGroup != null)
            {
                Debug.Log($"[LevelManager] Found Desks group with {desksGroup.childCount} children");
                
                foreach (Transform desk in desksGroup)
                {
                    SetupDeskComponent(desk.gameObject);
                }
            }
            else
            {
                // Fallback: find all objects with "Desk" in name or with specific tag
                GameObject[] allObjects = FindObjectsOfType<GameObject>();
                int deskCount = 0;
                
                foreach (GameObject obj in allObjects)
                {
                    if (obj.name.Contains("Desk") || obj.CompareTag("Desk"))
                    {
                        SetupDeskComponent(obj);
                        deskCount++;
                    }
                }
                
                Debug.Log($"[LevelManager] Found {deskCount} desks by tag/name search");
            }
        }
        
        /// <summary>
        /// Add StudentInteractableObject component to a desk if not already present
        /// </summary>
        private void SetupDeskComponent(GameObject deskObj)
        {
            if (deskObj == null) return;
            
            // Check if component already exists
            if (deskObj.GetComponent<StudentInteractableObject>() != null)
            {
                Debug.Log($"[LevelManager] Desk {deskObj.name} already has StudentInteractableObject");
                return;
            }
            
            // Add the component
            StudentInteractableObject interactable = deskObj.AddComponent<StudentInteractableObject>();
            
            // Configure the desk as an interactable object
            interactable.objectName = deskObj.name;
            interactable.canBeKnockedOver = true;
            interactable.canBeThrown = false;
            interactable.canMakeNoise = false;
            interactable.canBeDropped = false;
            
            // Ensure desk has a non-trigger collider for OverlapSphere detection
            Collider collider = deskObj.GetComponent<Collider>();
            if (collider != null)
            {
                if (collider.isTrigger)
                {
                    collider.isTrigger = false;
                    Debug.Log($"[LevelManager] Disabled isTrigger on desk {deskObj.name} collider for OverlapSphere detection");
                }
            }
            else
            {
                // Add BoxCollider if no collider exists
                BoxCollider boxCollider = deskObj.AddComponent<BoxCollider>();
                boxCollider.isTrigger = false;
                Debug.Log($"[LevelManager] Added BoxCollider to desk {deskObj.name} for OverlapSphere detection");
            }
            
            Debug.Log($"[LevelManager] Added StudentInteractableObject to desk {deskObj.name}");
        }

        private void HandleStudentEvent(StudentEvent evt)
        {
            if (!IsLevelActive || levelEnded) return;

            if (evt.student != null)
            {
                TrackCriticalStudents(evt);
            }
        }

        private void TrackCriticalStudents(StudentEvent evt)
        {
            if (evt.eventType == StudentEventType.StudentCalmed || 
                evt.eventType == StudentEventType.StudentReturnedToSeat)
            {
                if (evt.student.CurrentState != StudentState.Critical)
                {
                    criticalStudentCount = Mathf.Max(0, criticalStudentCount - 1);
                }
            }
        }

        private void CheckLoseConditions()
        {
            if (currentGoal == null) return;

            if (ClassroomManager.Instance != null)
            {
                float disruption = ClassroomManager.Instance.DisruptionLevel;

                if (disruption >= currentGoal.catastrophicDisruptionLevel)
                {
                    LoseLevel("Classroom became too chaotic!");
                    return;
                }

                if (disruption >= currentGoal.maxDisruptionThreshold)
                {
                    LoseLevel("Disruption exceeded maximum threshold!");
                    return;
                }

                // Check outside student conditions
                int outsideCount = ClassroomManager.Instance.OutsideStudentCount;

                // Catastrophic: Too many students outside at once
                if (outsideCount >= currentGoal.catastrophicOutsideStudents)
                {
                    LoseLevel($"LOSS: Too many students outside the classroom! ({outsideCount} students escaped)");
                    return;
                }

                // Check if any individual student has been outside too long
                if (currentGoal.maxOutsideTimePerStudent > 0)
                {
                    StudentAgent[] allStudents = FindObjectsOfType<StudentAgent>();
                    foreach (StudentAgent student in allStudents)
                    {
                        float timeOutside = ClassroomManager.Instance.GetStudentOutsideDuration(student);
                        if (timeOutside > currentGoal.maxOutsideTimePerStudent)
                        {
                            LoseLevel($"LOSS: {student.Config?.studentName} was outside for too long ({timeOutside:F0}s)");
                            return;
                        }
                    }
                }

                // Track grace period for exceeding max allowed outside students
                if (outsideCount > currentGoal.maxAllowedOutsideStudents)
                {
                    if (!isTrackingOutsideExcess)
                    {
                        isTrackingOutsideExcess = true;
                        outsideStudentsExceededTime = 0f;
                        Debug.LogWarning($"[LevelManager] Too many students outside ({outsideCount}/{currentGoal.maxAllowedOutsideStudents}), grace period started");
                    }
                    else
                    {
                        outsideStudentsExceededTime += Time.deltaTime;
                        if (outsideStudentsExceededTime >= currentGoal.maxAllowedOutsideGracePeriod)
                        {
                            LoseLevel($"LOSS: Too many students remained outside for too long ({outsideCount} students)");
                            return;
                        }
                    }
                }
                else
                {
                    // Reset grace period if count drops back down
                    if (isTrackingOutsideExcess)
                    {
                        isTrackingOutsideExcess = false;
                        outsideStudentsExceededTime = 0f;
                        Debug.Log($"[LevelManager] Outside student count back to acceptable levels");
                    }
                }
            }

            if (criticalStudentCount >= currentGoal.catastrophicCriticalStudents)
            {
                LoseLevel("Too many students in critical state!");
                return;
            }
        }

        private void CheckWinConditions()
        {
            if (levelEnded) return;

            if (currentGoal == null)
            {
                Debug.LogWarning("[LevelManager] No level goal configured");
                return;
            }

            float finalDisruption = ClassroomManager.Instance?.DisruptionLevel ?? 0f;
            ScoreSummary summary = TeacherScoreManager.Instance?.GetScoreSummary() ?? new ScoreSummary();

            if (currentGoal.MeetsWinConditions(finalDisruption, summary.resolvedProblems, summary.calmDowns))
            {
                WinLevel(summary.totalScore);
            }
            else
            {
                LoseLevel($"Failed to meet objectives (Disruption: {finalDisruption:F0}, Problems: {summary.resolvedProblems}/{currentGoal.requiredResolvedProblems})");
            }
        }

        private void WinLevel(int finalScore)
        {
            if (levelEnded) return;
            levelEnded = true;

            int starRating = currentGoal?.GetStarRating(finalScore) ?? 0;

            Debug.Log($"[LevelManager] LEVEL WON! Score: {finalScore}, Stars: {starRating}");
            
            OnStarRatingAchieved?.Invoke(starRating);
            OnLevelWon?.Invoke();

            if (TeacherScoreManager.Instance != null && currentGoal != null && currentGoal.hasTimeLimit)
            {
                TeacherScoreManager.Instance.AwardSpeedBonus(LevelTimeRemaining);
            }

            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.CompleteLevel();
            }
        }

        private void LoseLevel(string reason)
        {
            if (levelEnded) return;
            levelEnded = true;

            Debug.Log($"[LevelManager] LEVEL LOST: {reason}");
            OnLevelLost?.Invoke(reason);

            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.CompleteLevel();
            }
        }

        public void ForceCheckWinConditions()
        {
            CheckWinConditions();
        }

        public LevelProgress GetLevelProgress()
        {
            ScoreSummary summary = TeacherScoreManager.Instance?.GetScoreSummary() ?? new ScoreSummary();
            
            return new LevelProgress
            {
                timeElapsed = LevelTimeElapsed,
                timeRemaining = LevelTimeRemaining,
                currentScore = summary.totalScore,
                resolvedProblems = summary.resolvedProblems,
                requiredProblems = currentGoal?.requiredResolvedProblems ?? 0,
                calmDowns = summary.calmDowns,
                requiredCalmDowns = currentGoal?.requiredCalmDowns ?? 0,
                disruption = ClassroomManager.Instance?.DisruptionLevel ?? 0f,
                maxDisruption = currentGoal?.maxDisruptionThreshold ?? 100f
            };
        }
    }

    [System.Serializable]
    public struct LevelProgress
    {
        public float timeElapsed;
        public float timeRemaining;
        public int currentScore;
        public int resolvedProblems;
        public int requiredProblems;
        public int calmDowns;
        public int requiredCalmDowns;
        public float disruption;
        public float maxDisruption;
    }
}
