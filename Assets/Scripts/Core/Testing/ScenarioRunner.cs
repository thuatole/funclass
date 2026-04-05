using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FunClass.Core.Testing
{
    // ---------------------------------------------------------------------------
    // JSON data classes — must match the scenario JSON structure exactly.
    // ---------------------------------------------------------------------------

    [Serializable]
    public class TeacherActionData
    {
        public float time;
        public string action;
        public string targetStudent;
        public string description;
    }

    [Serializable]
    public class ScenarioData
    {
        public string scenarioName;
        public string description;
        public int series;
        public string level;
        public string levelConfig;
        public int testTimeScale;
        public float maxGameTimeSeconds;
        public List<TeacherActionData> teacherActions;
        public List<ExpectedAssertion> assertions;
        public string expectedEndState;
        public string notes;
    }

    // ---------------------------------------------------------------------------
    // ScenarioRunner
    // ---------------------------------------------------------------------------

    /// <summary>
    /// MonoBehaviour that drives automated scenario tests.
    /// Add to a GameObject in the target scene, set scenarioFilePath, press Play.
    /// </summary>
    public class ScenarioRunner : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private string scenarioFilePath;
        [SerializeField] private int testTimeScale = 10;
        [SerializeField] private float maxRealTimeSeconds = 60f;

        // Public results for external access (e.g. PlayMode test assertions).
        public bool IsComplete { get; private set; }
        public ValidationResult Result { get; private set; }

        private ScenarioData scenario;
        private List<CapturedEvent> capturedEvents = new List<CapturedEvent>();
        private HashSet<int> executedActions = new HashSet<int>();
        private float realTimeElapsed;
        private bool gameEnded;
        private GameState endState;

        // -----------------------------------------------------------------------
        // Unity lifecycle
        // -----------------------------------------------------------------------

        void OnEnable()
        {
            GameLogger.OnMilestoneLogged += OnMilestoneLogged;
        }

        void OnDisable()
        {
            GameLogger.OnMilestoneLogged -= OnMilestoneLogged;
        }

        void Start()
        {
            if (string.IsNullOrEmpty(scenarioFilePath))
            {
                GameLogger.Error("ScenarioRunner", "scenarioFilePath is empty — cannot run scenario");
                enabled = false;
                return;
            }

            scenario = LoadScenarioFile(scenarioFilePath);
            if (scenario == null)
            {
                enabled = false;
                return;
            }

            // Override timeScale from file if it was set there; inspector value is the fallback.
            if (scenario.testTimeScale > 0)
                testTimeScale = scenario.testTimeScale;

            if (scenario.maxGameTimeSeconds > 0)
                maxRealTimeSeconds = scenario.maxGameTimeSeconds / testTimeScale + 5f; // +5s safety margin

            GameLogger.Milestone("ScenarioRunner",
                $"START '{scenario.scenarioName}' | timeScale={testTimeScale} | assertions={scenario.assertions?.Count ?? 0}");

            GameLogger.ClearCapturedEvents();
            capturedEvents.Clear();
            executedActions.Clear();
            gameEnded = false;
            IsComplete = false;

            Time.timeScale = testTimeScale;

            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OnStateChanged += OnGameStateChanged;

            StartCoroutine(WatchdogCoroutine());
        }

        void Update()
        {
            if (IsComplete) return;

            realTimeElapsed += Time.unscaledDeltaTime;

            ExecutePendingTeacherActions();
        }

        // -----------------------------------------------------------------------
        // Event handlers
        // -----------------------------------------------------------------------

        private void OnMilestoneLogged(CapturedEvent ev)
        {
            capturedEvents.Add(ev);
        }

        private void OnGameStateChanged(GameState oldState, GameState newState)
        {
            if (newState == GameState.StudentIntro)
            {
                // Skip intro screen automatically — same call the Ready button makes.
                // Delay 1 frame so StudentIntro listeners finish processing before transitioning.
                GameLogger.Milestone("ScenarioRunner", "Auto-skipping StudentIntro");
                StartCoroutine(SkipIntroNextFrame());
                return;
            }

            if (newState == GameState.LevelComplete || newState == GameState.LevelFailed)
            {
                endState = newState;
                gameEnded = true;
            }
        }

        // -----------------------------------------------------------------------
        // Teacher action execution
        // -----------------------------------------------------------------------

        private void ExecutePendingTeacherActions()
        {
            if (scenario.teacherActions == null || scenario.teacherActions.Count == 0) return;
            if (LevelManager.Instance == null || !LevelManager.Instance.IsLevelActive) return;

            float gameTime = LevelManager.Instance.LevelTimeElapsed;

            for (int i = 0; i < scenario.teacherActions.Count; i++)
            {
                if (executedActions.Contains(i)) continue;

                var action = scenario.teacherActions[i];
                if (gameTime >= action.time)
                {
                    ExecuteTeacherAction(action);
                    executedActions.Add(i);
                }
            }
        }

        private void ExecuteTeacherAction(TeacherActionData action)
        {
            StudentAgent target = FindStudentByName(action.targetStudent);

            switch (action.action)
            {
                case "CalmStudent":
                    if (target != null)
                    {
                        // Log so assertion "TeacherController / CalmStudent" can match.
                        GameLogger.Milestone("TeacherController",
                            $"Calming {target.Config?.studentName}",
                            "CalmStudent", "", target.Config?.studentName);

                        int loops = 0;
                        while (target.CurrentState != StudentState.Calm && loops < 10)
                        {
                            target.DeescalateState();
                            loops++;
                        }
                        target.HandleTeacherAction(TeacherActionType.Calm);

                        // Trigger influence resolution — StudentInfluenceManager listens for StudentCalmed.
                        if (StudentEventManager.Instance != null)
                            StudentEventManager.Instance.LogEvent(
                                target,
                                StudentEventType.StudentCalmed,
                                $"Teacher calmed {target.Config?.studentName}",
                                null);

                        if (ClassroomManager.Instance != null)
                            ClassroomManager.Instance.AddDisruption(-5f, $"Calmed {target.Config?.studentName}");
                    }
                    else
                    {
                        GameLogger.Warning("ScenarioRunner", $"CalmStudent: student '{action.targetStudent}' not found");
                    }
                    break;

                case "EscortStudentBack":
                    if (target != null)
                    {
                        GameLogger.Milestone("TeacherController",
                            $"Escorting {target.Config?.studentName} back to seat",
                            "EscortStudentBack", "", target.Config?.studentName);

                        int loops = 0;
                        while (target.CurrentState != StudentState.Calm && loops < 10)
                        {
                            target.DeescalateState();
                            loops++;
                        }
                        target.HandleTeacherAction(TeacherActionType.EscortStudentBack);

                        // Trigger influence resolution.
                        if (StudentEventManager.Instance != null)
                            StudentEventManager.Instance.LogEvent(
                                target,
                                StudentEventType.StudentCalmed,
                                $"Teacher escorted {target.Config?.studentName}",
                                null);

                        if (StudentMovementManager.Instance != null)
                            StudentMovementManager.Instance.ReturnToSeat(target);

                        if (ClassroomManager.Instance != null)
                            ClassroomManager.Instance.AddDisruption(-15f, $"{target.Config?.studentName} escorted back");
                    }
                    else
                    {
                        GameLogger.Warning("ScenarioRunner", $"EscortStudentBack: student '{action.targetStudent}' not found");
                    }
                    break;

                case "CallStudentBack":
                    if (target != null)
                    {
                        GameLogger.Milestone("TeacherController",
                            $"Calling {target.Config?.studentName} back to class",
                            "CallStudentBack", "", target.Config?.studentName);

                        target.HandleTeacherAction(TeacherActionType.CallStudentBack);

                        if (StudentMovementManager.Instance != null)
                            StudentMovementManager.Instance.ReturnToSeat(target);

                        if (ClassroomManager.Instance != null)
                            ClassroomManager.Instance.AddDisruption(-10f, $"{target.Config?.studentName} called back");
                    }
                    else
                    {
                        GameLogger.Warning("ScenarioRunner", $"CallStudentBack: student '{action.targetStudent}' not found");
                    }
                    break;

                default:
                    GameLogger.Warning("ScenarioRunner", $"Unknown teacher action: '{action.action}'");
                    break;
            }
        }

        private IEnumerator SkipIntroNextFrame()
        {
            yield return null; // wait 1 frame for StudentIntro listeners to process
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.StartLevel();

            // StudentIntroScreen.HideIntroScreen() resets Time.timeScale = 1 — re-apply after it runs.
            yield return null;
            Time.timeScale = testTimeScale;
            GameLogger.Milestone("ScenarioRunner", $"timeScale re-applied after intro: {testTimeScale}");
        }

        private StudentAgent FindStudentByName(string studentName)
        {
            if (string.IsNullOrEmpty(studentName)) return null;
            foreach (var agent in FindObjectsOfType<StudentAgent>())
            {
                if (agent.Config?.studentName == studentName)
                    return agent;
            }
            return null;
        }

        // -----------------------------------------------------------------------
        // Watchdog coroutine — waits for game end or timeout, then finalizes.
        // -----------------------------------------------------------------------

        private IEnumerator WatchdogCoroutine()
        {
            float started = Time.unscaledTime;

            while (!gameEnded)
            {
                if (Time.unscaledTime - started >= maxRealTimeSeconds)
                {
                    GameLogger.Warning("ScenarioRunner",
                        $"TIMEOUT after {maxRealTimeSeconds:F0}s real time — forcing finalization");
                    break;
                }
                yield return null;
            }

            Finalize();
        }

        // -----------------------------------------------------------------------
        // Finalize — validate + log + write file
        // -----------------------------------------------------------------------

        private void Finalize()
        {
            Time.timeScale = 1f;

            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OnStateChanged -= OnGameStateChanged;

            float gameTimePlayed = LevelManager.Instance != null ? LevelManager.Instance.LevelTimeElapsed : -1f;
            string endReason = gameEnded ? endState.ToString() : "Timeout";

            // Validate
            Result = ScenarioAsserter.Validate(capturedEvents, scenario.assertions ?? new List<ExpectedAssertion>());

            // Build result block
            string block = BuildResultBlock(gameTimePlayed, endReason);

            // Log to console
            if (Result.allPassed)
                GameLogger.Milestone("ScenarioRunner", block);
            else
                GameLogger.Error("ScenarioRunner", block);

            // Write to file
            WriteResultFile(block);

            IsComplete = true;
        }

        // -----------------------------------------------------------------------
        // Result block formatting
        // -----------------------------------------------------------------------

        private string BuildResultBlock(float gameTimePlayed, string endReason)
        {
            int total = Result.results.Count;
            int passed = 0;
            int failed = 0;
            foreach (var r in Result.results)
            {
                if (r.status == AssertionStatus.Pass) passed++;
                else failed++;
            }

            var sb = new StringBuilder();
            sb.AppendLine("==================================================");
            sb.AppendLine($"SCENARIO RESULT: {scenario.scenarioName}");
            sb.AppendLine($"End: {endReason} | GameTime: {gameTimePlayed:F1}s | RealTime: {realTimeElapsed:F1}s | timeScale={testTimeScale}");
            sb.AppendLine($"Assertions: {total} total, {passed} passed, {failed} failed");
            sb.AppendLine();

            foreach (var r in Result.results)
            {
                if (r.status == AssertionStatus.Pass)
                {
                    string timeStr = r.actualTime >= 0 ? $" at {r.actualTime:F1}s" : "";
                    sb.AppendLine($"  PASS [{r.id}]{timeStr} — {r.reason}");
                }
                else
                {
                    sb.AppendLine($"  FAIL [{r.id}] {r.status} — {r.reason}");
                }
            }

            sb.AppendLine();
            sb.AppendLine($"--- All captured events ({capturedEvents.Count}) ---");
            foreach (var ev in capturedEvents)
            {
                sb.AppendLine($"  [{ev.component}] {ev.eventType} src={ev.source} tgt={ev.target} t={ev.elapsed:F1}s | {ev.rawMessage}");
            }

            sb.AppendLine("==================================================");
            return sb.ToString();
        }

        // -----------------------------------------------------------------------
        // File I/O
        // -----------------------------------------------------------------------

        private void WriteResultFile(string content)
        {
            try
            {
                string resultsDir = Path.Combine(Application.dataPath, "Tests", "Results");
                Directory.CreateDirectory(resultsDir);

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string safeName = (scenario.scenarioName ?? "unknown")
                    .Replace(" ", "_")
                    .Replace("/", "-")
                    .Replace("\\", "-");

                string filePath = Path.Combine(resultsDir, $"{safeName}_{timestamp}.txt");
                File.WriteAllText(filePath, content, Encoding.UTF8);

                GameLogger.Milestone("ScenarioRunner", $"Result written to: {filePath}");
            }
            catch (Exception ex)
            {
                GameLogger.Error("ScenarioRunner", $"Failed to write result file: {ex.Message}");
            }
        }

        // -----------------------------------------------------------------------
        // JSON loading
        // -----------------------------------------------------------------------

        private ScenarioData LoadScenarioFile(string path)
        {
            try
            {
                // Fallback: if relative path, try resolving from Application.dataPath.
                if (!Path.IsPathRooted(path))
                {
                    string fromData = Path.Combine(Application.dataPath, path);
                    if (File.Exists(fromData))
                        path = fromData;
                }

                if (!File.Exists(path))
                {
                    GameLogger.Error("ScenarioRunner", $"Scenario file not found: {path}");
                    return null;
                }

                string json = File.ReadAllText(path, Encoding.UTF8);
                var data = JsonUtility.FromJson<ScenarioData>(json);

                if (data == null)
                {
                    GameLogger.Error("ScenarioRunner", $"Failed to parse scenario JSON: {path}");
                    return null;
                }

                return data;
            }
            catch (Exception ex)
            {
                GameLogger.Error("ScenarioRunner", $"Exception loading scenario file: {ex.Message}");
                return null;
            }
        }
    }
}
