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
                Debug.LogWarning("[GameStateManager] Duplicate instance destroyed");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            CurrentState = GameState.Boot;
            Debug.Log("[GameStateManager] Initialized in Boot state");
        }

        void Start()
        {
            // Delay transition to ensure all managers are initialized
            StartCoroutine(DelayedLevelStart());
        }

        private System.Collections.IEnumerator DelayedLevelStart()
        {
            Debug.Log("[GameStateManager] Waiting for managers to initialize...");

            // Wait one frame for all managers to initialize
            yield return null;

            if (showStudentIntro)
            {
                // Transition FIRST so listeners know the state
                Debug.Log("[GameStateManager] Starting level transition to StudentIntro");
                TransitionTo(GameState.StudentIntro);

                // Then ensure StudentIntroScreen exists (it will show itself because state is StudentIntro)
                EnsureStudentIntroScreenExists();
            }
            else
            {
                // Skip intro and go directly to InLevel
                Debug.Log("[GameStateManager] Skipping StudentIntro, going to InLevel");
                TransitionTo(GameState.InLevel);
            }

            Debug.Log($"[GameStateManager] Current state after transition: {CurrentState}");
        }

        private void EnsureStudentIntroScreenExists()
        {
            // Check if StudentIntroScreen singleton already exists
            if (StudentIntroScreen.Instance != null)
            {
                Debug.Log("[GameStateManager] StudentIntroScreen.Instance already exists");
                return;
            }

            // Also check via FindObjectOfType as fallback
            StudentIntroScreen introScreen = FindObjectOfType<StudentIntroScreen>();
            if (introScreen != null)
            {
                Debug.Log("[GameStateManager] StudentIntroScreen already exists (via FindObjectOfType)");
                return;
            }

            Debug.Log("[GameStateManager] Creating StudentIntroScreen...");

            // Find or create canvas
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

            // Create intro screen
            GameObject introObj = new GameObject("StudentIntroScreen");
            introObj.transform.SetParent(canvas.transform, false);
            introScreen = introObj.AddComponent<StudentIntroScreen>();
            introScreen.CreateUI();

            Debug.Log("[GameStateManager] StudentIntroScreen created");
        }

        public void TransitionTo(GameState newState)
        {
            if (CurrentState == newState)
            {
                Debug.Log($"[GameStateManager] Already in {newState} state, skipping transition");
                return;
            }

            GameState oldState = CurrentState;
            CurrentState = newState;

            Debug.Log($"[GameStateManager] STATE TRANSITION: {oldState} -> {newState}");
            
            if (OnStateChanged != null)
            {
                int listenerCount = OnStateChanged.GetInvocationList().Length;
                Debug.Log($"[GameStateManager] Notifying {listenerCount} listeners of state change");
                OnStateChanged.Invoke(oldState, newState);
            }
            else
            {
                Debug.LogWarning("[GameStateManager] No listeners subscribed to OnStateChanged!");
            }
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
