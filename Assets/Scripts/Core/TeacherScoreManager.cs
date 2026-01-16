using UnityEngine;
using System;

namespace FunClass.Core
{
    public class TeacherScoreManager : MonoBehaviour
    {
        public static TeacherScoreManager Instance { get; private set; }

        public int CurrentScore { get; private set; }
        public int ResolvedProblems { get; private set; }
        public int CalmDownCount { get; private set; }
        public int WrongActionCount { get; private set; }

        public event Action<int, string> OnScoreChanged;
        public event Action<int> OnProblemResolved;

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

            if (StudentEventManager.Instance != null)
            {
                StudentEventManager.Instance.OnEventLogged += HandleStudentEvent;
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
        }

        private void HandleGameStateChanged(GameState oldState, GameState newState)
        {
            if (newState == GameState.InLevel)
            {
                ActivateScoring();
            }
            else
            {
                DeactivateScoring();
            }
        }

        private void ActivateScoring()
        {
            isActive = true;
            ResetScore();
            Debug.Log("[TeacherScoreManager] Scoring activated");
        }

        private void DeactivateScoring()
        {
            isActive = false;
            Debug.Log("[TeacherScoreManager] Scoring deactivated");
        }

        private void ResetScore()
        {
            CurrentScore = 0;
            ResolvedProblems = 0;
            CalmDownCount = 0;
            WrongActionCount = 0;
            OnScoreChanged?.Invoke(CurrentScore, "Score reset");
            Debug.Log("[TeacherScoreManager] Score reset to 0");
        }

        private void HandleStudentEvent(StudentEvent evt)
        {
            if (!isActive) return;

            int scoreChange = GetScoreChangeForEvent(evt.eventType);
            string reason = GetScoreReasonForEvent(evt.eventType, evt.student?.Config?.studentName);

            if (scoreChange != 0)
            {
                AddScore(scoreChange, reason);
            }

            TrackObjectiveProgress(evt.eventType);
        }

        private int GetScoreChangeForEvent(StudentEventType eventType)
        {
            return eventType switch
            {
                StudentEventType.StudentCalmed => 20,
                StudentEventType.StudentReturnedToSeat => 25,
                StudentEventType.StudentStoppedAction => 15,
                StudentEventType.StudentReacted when IsPositiveReaction(eventType) => 5,
                StudentEventType.TeacherInteracted => 5,
                StudentEventType.ObjectTakenAway => 10,
                StudentEventType.MakingNoise => -5,
                StudentEventType.ThrowingObject => -10,
                StudentEventType.KnockedOverObject => -8,
                _ => 0
            };
        }

        private bool IsPositiveReaction(StudentEventType eventType)
        {
            return eventType == StudentEventType.StudentReacted;
        }

        private string GetScoreReasonForEvent(StudentEventType eventType, string studentName)
        {
            string name = studentName ?? "Student";
            return eventType switch
            {
                StudentEventType.StudentCalmed => $"Calmed {name}",
                StudentEventType.StudentReturnedToSeat => $"Sent {name} back to seat",
                StudentEventType.StudentStoppedAction => $"Stopped {name}'s action",
                StudentEventType.TeacherInteracted => $"Interacted with {name}",
                StudentEventType.ObjectTakenAway => $"Confiscated object from {name}",
                StudentEventType.MakingNoise => $"{name} making noise",
                StudentEventType.ThrowingObject => $"{name} throwing objects",
                StudentEventType.KnockedOverObject => $"{name} knocked over object",
                _ => "Unknown action"
            };
        }

        private void TrackObjectiveProgress(StudentEventType eventType)
        {
            switch (eventType)
            {
                case StudentEventType.StudentCalmed:
                    CalmDownCount++;
                    ResolvedProblems++;
                    OnProblemResolved?.Invoke(ResolvedProblems);
                    Debug.Log($"[TeacherScoreManager] Problems resolved: {ResolvedProblems}, Calm downs: {CalmDownCount}");
                    break;

                case StudentEventType.StudentReturnedToSeat:
                    ResolvedProblems++;
                    OnProblemResolved?.Invoke(ResolvedProblems);
                    Debug.Log($"[TeacherScoreManager] Problems resolved: {ResolvedProblems}");
                    break;
            }
        }

        public void AddScore(int amount, string reason)
        {
            if (!isActive) return;

            CurrentScore = Mathf.Max(0, CurrentScore + amount);
            
            string changeType = amount > 0 ? "earned" : "lost";
            Debug.Log($"[TeacherScoreManager] {changeType} {Mathf.Abs(amount)} points: {reason} (Total: {CurrentScore})");
            
            OnScoreChanged?.Invoke(CurrentScore, reason);
        }

        public void AddBonusScore(int amount, string reason)
        {
            if (!isActive) return;

            CurrentScore += amount;
            Debug.Log($"[TeacherScoreManager] BONUS: +{amount} points for {reason} (Total: {CurrentScore})");
            OnScoreChanged?.Invoke(CurrentScore, $"BONUS: {reason}");
        }

        public void RecordWrongAction()
        {
            WrongActionCount++;
            AddScore(-10, "Wrong action penalty");
        }

        public void AwardSequenceBonus(string sequencePath)
        {
            int bonus = sequencePath.ToLower().Contains("success") ? 50 : 10;
            AddBonusScore(bonus, $"Completed sequence via {sequencePath} path");
        }

        public void AwardSpeedBonus(float timeRemaining)
        {
            if (timeRemaining > 0)
            {
                int bonus = Mathf.RoundToInt(timeRemaining * 0.5f);
                AddBonusScore(bonus, $"Fast resolution ({timeRemaining:F0}s remaining)");
            }
        }

        public ScoreSummary GetScoreSummary()
        {
            return new ScoreSummary
            {
                totalScore = CurrentScore,
                resolvedProblems = ResolvedProblems,
                calmDowns = CalmDownCount,
                wrongActions = WrongActionCount
            };
        }
    }

    [System.Serializable]
    public struct ScoreSummary
    {
        public int totalScore;
        public int resolvedProblems;
        public int calmDowns;
        public int wrongActions;
    }
}
