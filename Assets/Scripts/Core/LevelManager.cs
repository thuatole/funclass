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
        private float lastProgressLogTime = 0f;

        void Awake()
        {
            GameLogger.Milestone("LevelManager", "Awake called");

            if (Instance != null && Instance != this)
            {
                GameLogger.Warning("LevelManager", "Duplicate instance, destroying...");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            GameLogger.Milestone("LevelManager", "Instance created");
        }

        void OnEnable()
        {
            GameLogger.Detail("LevelManager", "OnEnable called");

            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged += HandleGameStateChanged;
                hasSubscribedToGameState = true;
                GameLogger.Detail("LevelManager", $"Subscribed to GameStateManager - state: {GameStateManager.Instance.CurrentState}");
            }
            else
            {
                GameLogger.Warning("LevelManager", "GameStateManager.Instance is NULL in OnEnable");
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

        void Start()
        {
            GameLogger.Detail("LevelManager", "Start called");

            // Retry subscription if failed in OnEnable
            if (!hasSubscribedToGameState && GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged += HandleGameStateChanged;
                hasSubscribedToGameState = true;
                GameLogger.Detail("LevelManager", $"Late subscription - state: {GameStateManager.Instance.CurrentState}");

                if (GameStateManager.Instance.CurrentState == GameState.InLevel)
                {
                    GameLogger.Detail("LevelManager", "Already in InLevel, starting level");
                    StartLevel();
                }
            }
        }

        void Update()
        {
            if (!IsLevelActive || levelEnded) return;

            LevelTimeElapsed += Time.deltaTime;

            if (currentGoal != null && currentGoal.hasTimeLimit)
            {
                LevelTimeRemaining = currentGoal.timeLimitSeconds - LevelTimeElapsed;

                // Progress log every 5 seconds
                if (Time.time - lastProgressLogTime >= 5f)
                {
                    lastProgressLogTime = Time.time;
                    LogProgress();
                }

                if (LevelTimeRemaining <= 0)
                {
                    CheckWinConditions();
                }
            }
            else if (Time.time - lastProgressLogTime >= 5f)
            {
                lastProgressLogTime = Time.time;
                GameLogger.Warning("LevelManager", "currentGoal is NULL or hasTimeLimit=false");
            }

            CheckLoseConditions();
        }

        private void LogProgress()
        {
            ScoreSummary summary = TeacherScoreManager.Instance?.GetScoreSummary() ?? new ScoreSummary();
            float disruption = ClassroomManager.Instance?.DisruptionLevel ?? 0f;

            GameLogger.Milestone("LevelManager", $"=== PROGRESS ===");
            GameLogger.Milestone("LevelManager", 
                $"Time: {LevelTimeRemaining:F0}s remaining / {currentGoal?.timeLimitSeconds ?? 0}s total");
            GameLogger.Milestone("LevelManager", 
                $"Disruption: {disruption:F1}% (max: {currentGoal?.maxDisruptionThreshold ?? 0}%)");
            GameLogger.Milestone("LevelManager", 
                $"CalmDowns: {summary.calmDowns} / {currentGoal?.requiredCalmDowns ?? 0}");
            GameLogger.Milestone("LevelManager", $"Resolved: {summary.resolvedProblems} / {currentGoal?.requiredResolvedProblems ?? 0}");
        }

        private void HandleGameStateChanged(GameState oldState, GameState newState)
        {
            GameLogger.Detail("LevelManager", $"{oldState} → {newState}");

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
            lastProgressLogTime = 0f;

            if (LevelLoader.Instance != null && LevelLoader.Instance.CurrentLevel != null)
            {
                currentGoal = LevelLoader.Instance.CurrentLevel.levelGoal;
                if (currentGoal != null && currentGoal.hasTimeLimit)
                {
                    LevelTimeRemaining = currentGoal.timeLimitSeconds;
                }

                LoadStudentInteractions();
            }

            GameLogger.Milestone("LevelManager", $"Level started - {LevelLoader.Instance?.CurrentLevel?.levelId ?? "Unknown"}");
            if (currentGoal != null)
            {
                GameLogger.Milestone("LevelManager", 
                    $"Goal: {currentGoal.timeLimitSeconds}s, disruption ≤ {currentGoal.maxDisruptionThreshold}%, calmDowns: {currentGoal.requiredCalmDowns}");
            }
        }

        private void HandleOutsideStudentCountChanged(int count)
        {
            // Event used for tracking, actual checking happens in CheckLoseConditions()
        }

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
            GameLogger.Detail("LevelManager", "Level ended");
        }

        private void LoadStudentInteractions()
        {
            LevelConfig levelConfig = GetCurrentLevelConfig();
            if (levelConfig == null || levelConfig.studentInteractions == null)
            {
                GameLogger.Detail("LevelManager", "No student interactions in level config");
                return;
            }

            if (StudentInteractionProcessor.Instance == null)
            {
                GameLogger.Detail("LevelManager", "StudentInteractionProcessor not found - creating at runtime");
                GameObject processorObj = new GameObject("StudentInteractionProcessor");
                processorObj.AddComponent<StudentInteractionProcessor>();
                DontDestroyOnLoad(processorObj);
            }

            if (StudentInteractionProcessor.Instance != null)
            {
                StudentInteractionProcessor.Instance.LoadRuntimeInteractions(levelConfig.studentInteractions);
                GameLogger.Milestone("LevelManager", $"Loaded {levelConfig.studentInteractions.Count} student interactions");
            }

            SetupDesksForInteraction();
        }
        
        private void SetupDesksForInteraction()
        {
            GameLogger.Detail("LevelManager", "Setting up desks with StudentInteractableObject at runtime");
            
            Transform desksGroup = GameObject.Find("Desks")?.transform;
            
            if (desksGroup != null)
            {
                GameLogger.Detail("LevelManager", $"Found Desks group with {desksGroup.childCount} children");
                
                foreach (Transform desk in desksGroup)
                {
                    SetupDeskComponent(desk.gameObject);
                }
            }
            else
            {
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
                
                GameLogger.Detail("LevelManager", $"Found {deskCount} desks by tag/name search");
            }
        }
        
        private void SetupDeskComponent(GameObject deskObj)
        {
            if (deskObj == null) return;
            
            if (deskObj.GetComponent<StudentInteractableObject>() != null)
            {
                GameLogger.Detail("LevelManager", $"Desk {deskObj.name} already has StudentInteractableObject");
                return;
            }
            
            StudentInteractableObject interactable = deskObj.AddComponent<StudentInteractableObject>();
            
            interactable.objectName = deskObj.name;
            interactable.canBeKnockedOver = true;
            interactable.canBeThrown = false;
            interactable.canMakeNoise = false;
            interactable.canBeDropped = false;
            
            Collider collider = deskObj.GetComponent<Collider>();
            if (collider != null)
            {
                if (collider.isTrigger)
                {
                    collider.isTrigger = false;
                    GameLogger.Detail("LevelManager", $"Disabled isTrigger on desk {deskObj.name}");
                }
            }
            else
            {
                BoxCollider boxCollider = deskObj.AddComponent<BoxCollider>();
                boxCollider.isTrigger = false;
                GameLogger.Detail("LevelManager", $"Added BoxCollider to desk {deskObj.name}");
            }
            
            GameLogger.Detail("LevelManager", $"Added StudentInteractableObject to desk {deskObj.name}");
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

                int outsideCount = ClassroomManager.Instance.OutsideStudentCount;

                if (outsideCount >= currentGoal.catastrophicOutsideStudents)
                {
                    LoseLevel($"Too many students outside the classroom! ({outsideCount} students escaped)");
                    return;
                }

                if (currentGoal.maxOutsideTimePerStudent > 0)
                {
                    StudentAgent[] allStudents = FindObjectsOfType<StudentAgent>();
                    foreach (StudentAgent student in allStudents)
                    {
                        float timeOutside = ClassroomManager.Instance.GetStudentOutsideDuration(student);
                        if (timeOutside > currentGoal.maxOutsideTimePerStudent)
                        {
                            LoseLevel($"{student.Config?.studentName} was outside for too long ({timeOutside:F0}s)");
                            return;
                        }
                    }
                }

                if (outsideCount > currentGoal.maxAllowedOutsideStudents)
                {
                    if (!isTrackingOutsideExcess)
                    {
                        isTrackingOutsideExcess = true;
                        outsideStudentsExceededTime = 0f;
                        GameLogger.Detail("LevelManager", 
                            $"Too many students outside ({outsideCount}/{currentGoal.maxAllowedOutsideStudents}), grace period started");
                    }
                    else
                    {
                        outsideStudentsExceededTime += Time.deltaTime;
                        if (outsideStudentsExceededTime >= currentGoal.maxAllowedOutsideGracePeriod)
                        {
                            LoseLevel($"Too many students remained outside for too long ({outsideCount} students)");
                            return;
                        }
                    }
                }
                else
                {
                    if (isTrackingOutsideExcess)
                    {
                        isTrackingOutsideExcess = false;
                        outsideStudentsExceededTime = 0f;
                        GameLogger.Detail("LevelManager", "Outside student count back to acceptable levels");
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
                GameLogger.Warning("LevelManager", "No level goal configured");
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
                LoseLevel($"Failed to meet objectives (Disruption: {finalDisruption:F0}%, CalmDowns: {summary.calmDowns}/{currentGoal.requiredCalmDowns})");
            }
        }

        private void WinLevel(float score)
        {
            levelEnded = true;
            GameLogger.Milestone("LevelManager", $"=== LEVEL COMPLETE ===");
            GameLogger.Milestone("LevelManager", $"Final Score: {score:F0}");
            
            // Calculate stars
            int stars = 0;
            if (score >= currentGoal.threeStarScore) stars = 3;
            else if (score >= currentGoal.twoStarScore) stars = 2;
            else if (score >= currentGoal.oneStarScore) stars = 1;
            
            GameLogger.Milestone("LevelManager", $"Stars: {stars}");
            OnLevelWon?.Invoke();
        }

        private void LoseLevel(string reason)
        {
            levelEnded = true;
            GameLogger.Milestone("LevelManager", $"=== LEVEL FAILED ===");
            GameLogger.Milestone("LevelManager", $"Reason: {reason}");
            OnLevelLost?.Invoke(reason);
        }
    }
}
