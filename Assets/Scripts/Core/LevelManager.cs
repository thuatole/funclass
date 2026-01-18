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

        void Update()
        {
            if (!IsLevelActive || levelEnded) return;

            LevelTimeElapsed += Time.deltaTime;

            if (currentGoal != null && currentGoal.hasTimeLimit)
            {
                LevelTimeRemaining = currentGoal.timeLimitSeconds - LevelTimeElapsed;

                if (LevelTimeRemaining <= 0)
                {
                    CheckWinConditions();
                }
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
