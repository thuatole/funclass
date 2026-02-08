using UnityEngine;
using UnityEngine.UI;
using System;
using FunClass.Core.UI;

namespace FunClass.Core
{
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        public GameState CurrentState { get; private set; }
        public event Action<GameState, GameState> OnStateChanged;

        [Header("Student Intro Settings")]
        [SerializeField] private bool showStudentIntro = true;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                GameLogger.Warning("GameStateManager", "Duplicate instance destroyed");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            CurrentState = GameState.Boot;
            GameLogger.Milestone("GameStateManager", "Initialized in Boot state");
        }

        void Start()
        {
            StartCoroutine(DelayedLevelStart());
        }

        private System.Collections.IEnumerator DelayedLevelStart()
        {
            GameLogger.Detail("GameStateManager", "Waiting for managers to initialize...");

            yield return null;

            if (showStudentIntro)
            {
                GameLogger.Detail("GameStateManager", "Starting level transition to StudentIntro");
                TransitionTo(GameState.StudentIntro);

                EnsureStudentIntroScreenExists();
            }
            else
            {
                GameLogger.Detail("GameStateManager", "Skipping StudentIntro, going to InLevel");
                TransitionTo(GameState.InLevel);
            }

            GameLogger.Detail("GameStateManager", $"Current state after transition: {CurrentState}");
        }

        private void EnsureStudentIntroScreenExists()
        {
            if (StudentIntroScreen.Instance != null)
            {
                GameLogger.Detail("GameStateManager", "StudentIntroScreen already exists");
                return;
            }

            StudentIntroScreen introScreen = FindObjectOfType<StudentIntroScreen>();
            if (introScreen != null)
            {
                GameLogger.Detail("GameStateManager", "StudentIntroScreen already exists (via FindObjectOfType)");
                return;
            }

            GameLogger.Detail("GameStateManager", "Creating StudentIntroScreen...");

            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("StudentIntroCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            GameObject introObj = new GameObject("StudentIntroScreen");
            introObj.transform.SetParent(canvas.transform, false);
            introScreen = introObj.AddComponent<StudentIntroScreen>();
            introScreen.CreateUI();

            GameLogger.Detail("GameStateManager", "StudentIntroScreen created");
        }

        public void TransitionTo(GameState newState)
        {
            if (CurrentState == newState)
            {
                GameLogger.Detail("GameStateManager", $"Already in {newState} state, skipping transition");
                return;
            }

            GameState oldState = CurrentState;
            CurrentState = newState;

            GameLogger.Milestone("GameStateManager", $"STATE TRANSITION: {oldState} → {newState}");
            
            int listenerCount = OnStateChanged?.GetInvocationList().Length ?? 0;
            GameLogger.Detail("GameStateManager", $"Notifying {listenerCount} listeners of state change");
            
            OnStateChanged?.Invoke(oldState, newState);
        }

        public void CompleteLevel()
        {
            if (CurrentState == GameState.InLevel)
            {
                TransitionTo(GameState.LevelComplete);
            }
        }

        public void StartLevel()
        {
            TransitionTo(GameState.InLevel);
        }
    }
}
