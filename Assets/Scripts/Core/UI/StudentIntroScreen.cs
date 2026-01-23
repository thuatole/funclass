using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace FunClass.Core.UI
{
    /// <summary>
    /// Displays student introduction screen before level starts.
    /// Shows student faces in a grid layout matching their seating positions.
    /// Player must memorize students before proceeding.
    /// </summary>
    public class StudentIntroScreen : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject screenPanel;
        [SerializeField] private GameObject studentGridContainer; // Changed to GameObject to avoid Unity serialization issues with Transform
        [SerializeField] private GameObject studentCardPrefab;
        [SerializeField] private Button readyButton;
        [SerializeField] private Text helpText;
        [SerializeField] private Text titleText;

        [Header("Grid Settings")]
        [SerializeField] private int gridColumns = 4;
        [SerializeField] private float cardWidth = 150f;
        [SerializeField] private float cardHeight = 200f;
        [SerializeField] private float cardSpacing = 20f;

        [Header("Default Student Avatar")]
        [SerializeField] private Sprite defaultStudentAvatar;

        [Header("Custom Cursor")]
        [SerializeField] private Texture2D customCursor; // Assign a chalk/pencil cursor texture in Inspector
        [SerializeField] private Vector2 cursorHotspot = new Vector2(0, 0); // Click point offset

        private List<GameObject> studentCards = new List<GameObject>();
        private bool uiCreated = false;
        private Dictionary<string, Color> studentColors = new Dictionary<string, Color>();

        // Singleton to ensure only one instance
        public static StudentIntroScreen Instance { get; private set; }

        void Awake()
        {
            // Singleton pattern - destroy duplicates
            if (Instance != null && Instance != this)
            {
                Debug.Log($"[StudentIntroScreen] Duplicate instance found, destroying this one. Existing: {Instance.gameObject.name}, This: {gameObject.name}");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // IMPORTANT: Clear any serialized references - we create UI dynamically
            // This fixes issues with stale references from previous play sessions
            screenPanel = null;
            studentGridContainer = null;
            titleText = null;
            helpText = null;
            readyButton = null;

            Debug.Log($"[StudentIntroScreen] Instance set to {gameObject.name}, cleared all references");
        }

        void Start()
        {
            // Skip if we're being destroyed (duplicate)
            if (Instance != this)
            {
                Debug.Log($"[StudentIntroScreen] Start: This is not the Instance, skipping. Instance={Instance?.gameObject.name}, This={gameObject.name}");
                return;
            }

            Debug.Log($"[StudentIntroScreen] Start called on Instance. uiCreated={uiCreated}, screenPanel={(screenPanel != null ? "SET" : "NULL")}, studentGridContainer={(studentGridContainer != null ? "SET" : "NULL")}, gameObject={gameObject.name}");

            // Ensure UI is COMPLETE - check uiCreated flag and both screenPanel AND studentGridContainer
            bool needsUI = !uiCreated || (screenPanel == null || !screenPanel) || (studentGridContainer == null || !studentGridContainer);

            if (needsUI)
            {
                Debug.Log($"[StudentIntroScreen] Start: UI incomplete (uiCreated={uiCreated}), creating/recreating...");
                CreateUI();
                Debug.Log($"[StudentIntroScreen] After CreateUI: uiCreated={uiCreated}, screenPanel={(screenPanel != null ? "SET" : "NULL")}, studentGridContainer={(studentGridContainer != null ? "SET" : "NULL")}");
            }

            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged += HandleGameStateChanged;

                // Check if we're already in StudentIntro state
                if (GameStateManager.Instance.CurrentState == GameState.StudentIntro)
                {
                    Debug.Log("[StudentIntroScreen] Already in StudentIntro state, showing screen");
                    ShowIntroScreen();
                }
                else
                {
                    // Only hide if we're NOT in StudentIntro state
                    if (screenPanel != null)
                    {
                        screenPanel.SetActive(false);
                    }
                }
            }
            else
            {
                // No GameStateManager, hide by default
                if (screenPanel != null)
                {
                    screenPanel.SetActive(false);
                }
            }
        }

        void Update()
        {
            // Skip if not the singleton instance or screen not visible
            if (Instance != this) return;
            if (screenPanel == null || !screenPanel.activeSelf) return;

            // Handle keyboard input for "Đã nhớ" button (Enter or Space)
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log("[StudentIntroScreen] Keyboard input detected (Enter/Space), triggering ready button");
                OnReadyButtonClicked();
            }
        }

        void OnDestroy()
        {
            // Clear singleton reference if this is the instance
            if (Instance == this)
            {
                Instance = null;
            }

            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnStateChanged -= HandleGameStateChanged;
            }

            // Clean up button listener
            if (readyButton != null)
            {
                readyButton.onClick.RemoveAllListeners();
            }
        }

        private void HandleGameStateChanged(GameState oldState, GameState newState)
        {
            if (newState == GameState.StudentIntro)
            {
                ShowIntroScreen();
            }
            else if (oldState == GameState.StudentIntro)
            {
                HideIntroScreen();
            }
        }

        private void ShowIntroScreen()
        {
            Debug.Log($"[StudentIntroScreen] Showing intro screen. uiCreated={uiCreated}");

            // Ensure UI exists before showing
            if (!uiCreated || screenPanel == null || !screenPanel || studentGridContainer == null || !studentGridContainer)
            {
                Debug.Log($"[StudentIntroScreen] ShowIntroScreen: UI missing (uiCreated={uiCreated}), creating...");
                CreateUI();
            }

            if (screenPanel != null)
            {
                screenPanel.SetActive(true);
            }

            // Set texts
            if (titleText != null)
            {
                titleText.text = "GHI NHỚ HỌC SINH";
            }

            if (helpText != null)
            {
                helpText.text = "Bạn cần ghi nhớ học sinh trước khi vào game";
            }

            // Populate student grid
            PopulateStudentGrid();

            // Show cursor using CursorManager (for global custom cursor)
            if (CursorManager.Instance != null)
            {
                CursorManager.Instance.ShowCursor();
                Debug.Log("[StudentIntroScreen] Cursor shown via CursorManager");
            }
            else
            {
                // Fallback: direct cursor control
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                // Use custom cursor if assigned locally
                if (customCursor != null)
                {
                    Cursor.SetCursor(customCursor, cursorHotspot, CursorMode.Auto);
                    Debug.Log("[StudentIntroScreen] Custom cursor set (fallback)");
                }
            }

            Debug.Log($"[StudentIntroScreen] ShowIntroScreen complete. Time.timeScale={Time.timeScale}, Cursor visible");
        }

        private void HideIntroScreen()
        {
            Debug.Log($"[StudentIntroScreen] Hiding intro screen. Time.timeScale before={Time.timeScale}");

            if (screenPanel != null)
            {
                screenPanel.SetActive(false);
            }

            // Clear student cards
            ClearStudentCards();

            // Hide cursor for gameplay using CursorManager
            if (CursorManager.Instance != null)
            {
                CursorManager.Instance.HideCursor();
                Debug.Log("[StudentIntroScreen] Cursor hidden via CursorManager");
            }
            else
            {
                // Fallback: direct cursor control
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            // Ensure game time is running
            Time.timeScale = 1f;
            Debug.Log($"[StudentIntroScreen] HideIntroScreen complete. Time.timeScale={Time.timeScale}, Cursor hidden");
        }

        private void PopulateStudentGrid()
        {
            Debug.Log("[StudentIntroScreen] PopulateStudentGrid called");
            ClearStudentCards();

            if (studentGridContainer == null)
            {
                Debug.LogError("[StudentIntroScreen] studentGridContainer is NULL!");
                return;
            }

            if (LevelLoader.Instance == null)
            {
                Debug.LogWarning("[StudentIntroScreen] LevelLoader.Instance is NULL");
                return;
            }

            if (LevelLoader.Instance.CurrentLevel == null)
            {
                Debug.LogWarning("[StudentIntroScreen] CurrentLevel is NULL");
                return;
            }

            LevelConfig level = LevelLoader.Instance.CurrentLevel;
            List<StudentConfig> students = level.students;

            if (students == null || students.Count == 0)
            {
                Debug.LogWarning("[StudentIntroScreen] No students in level");
                return;
            }

            Debug.Log($"[StudentIntroScreen] Displaying {students.Count} students");

            // Get student positions and colors from scene (StudentAgent instances)
            Dictionary<string, Vector3> studentPositions = GetStudentPositions();
            studentColors = GetStudentColors();

            // Sort students by position (back to front, left to right) to simulate classroom layout
            List<StudentConfig> sortedStudents = new List<StudentConfig>(students);
            sortedStudents.Sort((a, b) =>
            {
                Vector3 posA = studentPositions.ContainsKey(a.studentId) ? studentPositions[a.studentId] : Vector3.zero;
                Vector3 posB = studentPositions.ContainsKey(b.studentId) ? studentPositions[b.studentId] : Vector3.zero;

                // Sort by Z (depth) first (higher Z = back of class), then by X (left to right)
                if (Mathf.Abs(posA.z - posB.z) > 0.5f)
                {
                    return posB.z.CompareTo(posA.z); // Higher Z first (back row first)
                }
                return posA.x.CompareTo(posB.x); // Lower X first (left to right)
            });

            // Create cards
            for (int i = 0; i < sortedStudents.Count; i++)
            {
                CreateStudentCard(sortedStudents[i], i);
            }

            // Update grid layout
            if (studentGridContainer != null)
            {
                GridLayoutGroup grid = studentGridContainer.GetComponent<GridLayoutGroup>();
                if (grid != null)
                {
                    grid.cellSize = new Vector2(cardWidth, cardHeight);
                    grid.spacing = new Vector2(cardSpacing, cardSpacing);
                    grid.constraintCount = gridColumns;
                }
            }
        }

        private Dictionary<string, Vector3> GetStudentPositions()
        {
            Dictionary<string, Vector3> positions = new Dictionary<string, Vector3>();

            StudentAgent[] agents = FindObjectsOfType<StudentAgent>();
            foreach (StudentAgent agent in agents)
            {
                if (agent.Config != null)
                {
                    positions[agent.Config.studentId] = agent.transform.position;
                }
            }

            return positions;
        }

        private Dictionary<string, Color> GetStudentColors()
        {
            Dictionary<string, Color> colors = new Dictionary<string, Color>();

            StudentAgent[] agents = FindObjectsOfType<StudentAgent>();
            foreach (StudentAgent agent in agents)
            {
                if (agent.Config != null)
                {
                    // Try to get color from the agent's renderer
                    Renderer renderer = agent.GetComponentInChildren<Renderer>();
                    if (renderer != null && renderer.sharedMaterial != null)
                    {
                        colors[agent.Config.studentId] = renderer.sharedMaterial.color;
                    }
                }
            }

            return colors;
        }

        private void CreateStudentCard(StudentConfig student, int index)
        {
            if (studentGridContainer == null) return;

            GameObject card;

            if (studentCardPrefab != null)
            {
                card = Instantiate(studentCardPrefab, studentGridContainer.transform);
            }
            else
            {
                // Create card dynamically if no prefab assigned
                card = CreateDefaultStudentCard();
                card.transform.SetParent(studentGridContainer.transform, false);
            }

            studentCards.Add(card);

            // Setup card content
            SetupStudentCard(card, student, index);
        }

        private GameObject CreateDefaultStudentCard()
        {
            // Create card container
            GameObject card = new GameObject("StudentCard");

            RectTransform cardRect = card.AddComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(cardWidth, cardHeight);

            // Add background image
            Image cardBg = card.AddComponent<Image>();
            cardBg.color = new Color(0.9f, 0.9f, 0.95f, 1f);

            // Add vertical layout
            VerticalLayoutGroup layout = card.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 10f;
            layout.padding = new RectOffset(10, 10, 15, 15);

            // Create avatar container
            GameObject avatarObj = new GameObject("Avatar");
            avatarObj.transform.SetParent(card.transform, false);

            RectTransform avatarRect = avatarObj.AddComponent<RectTransform>();
            avatarRect.sizeDelta = new Vector2(100f, 100f);

            Image avatarImage = avatarObj.AddComponent<Image>();
            avatarImage.preserveAspect = true;

            // Add LayoutElement to control size
            LayoutElement avatarLayout = avatarObj.AddComponent<LayoutElement>();
            avatarLayout.preferredWidth = 100f;
            avatarLayout.preferredHeight = 100f;

            // Create name text
            GameObject nameObj = new GameObject("NameText");
            nameObj.transform.SetParent(card.transform, false);

            Text nameText = nameObj.AddComponent<Text>();
            nameText.alignment = TextAnchor.MiddleCenter;
            nameText.fontSize = 18;
            nameText.fontStyle = FontStyle.Bold;
            nameText.color = Color.black;
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            LayoutElement nameLayout = nameObj.AddComponent<LayoutElement>();
            nameLayout.preferredHeight = 30f;

            return card;
        }

        private void SetupStudentCard(GameObject card, StudentConfig student, int index)
        {
            // Find avatar image
            Image avatarImage = card.transform.Find("Avatar")?.GetComponent<Image>();
            if (avatarImage != null)
            {
                // Try to load student-specific avatar, or use default
                Sprite avatar = LoadStudentAvatar(student);
                avatarImage.sprite = avatar ?? defaultStudentAvatar;

                if (avatarImage.sprite == null)
                {
                    // Use actual color from the student in the scene, or fallback to index-based color
                    if (studentColors.ContainsKey(student.studentId))
                    {
                        avatarImage.color = studentColors[student.studentId];
                    }
                    else
                    {
                        avatarImage.color = GetStudentColor(index);
                    }
                }
            }

            // Find name text
            Text nameText = card.transform.Find("NameText")?.GetComponent<Text>();
            if (nameText != null)
            {
                nameText.text = student.studentName;
            }

            // Add seat position indicator (optional)
            AddSeatIndicator(card, student, index);
        }

        private Sprite LoadStudentAvatar(StudentConfig student)
        {
            // Try to load avatar from Resources/StudentAvatars/{studentId}
            string path = $"StudentAvatars/{student.studentId}";
            Sprite sprite = Resources.Load<Sprite>(path);

            if (sprite == null)
            {
                // Try by name
                path = $"StudentAvatars/{student.studentName}";
                sprite = Resources.Load<Sprite>(path);
            }

            return sprite;
        }

        private Color GetStudentColor(int index)
        {
            // Generate distinct colors for different students
            Color[] colors = new Color[]
            {
                new Color(0.4f, 0.6f, 0.9f, 1f),  // Blue
                new Color(0.9f, 0.5f, 0.5f, 1f),  // Red
                new Color(0.5f, 0.8f, 0.5f, 1f),  // Green
                new Color(0.9f, 0.7f, 0.4f, 1f),  // Orange
                new Color(0.7f, 0.5f, 0.8f, 1f),  // Purple
                new Color(0.4f, 0.8f, 0.8f, 1f),  // Cyan
                new Color(0.9f, 0.6f, 0.7f, 1f),  // Pink
                new Color(0.7f, 0.7f, 0.5f, 1f),  // Olive
            };

            return colors[index % colors.Length];
        }

        private void AddSeatIndicator(GameObject card, StudentConfig student, int index)
        {
            // Add a small indicator showing row/column position
            // This helps players associate students with their seat positions

            GameObject indicatorObj = new GameObject("SeatIndicator");
            indicatorObj.transform.SetParent(card.transform, false);

            Text indicatorText = indicatorObj.AddComponent<Text>();
            indicatorText.text = $"Ghế {index + 1}";
            indicatorText.alignment = TextAnchor.MiddleCenter;
            indicatorText.fontSize = 12;
            indicatorText.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            indicatorText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            LayoutElement indicatorLayout = indicatorObj.AddComponent<LayoutElement>();
            indicatorLayout.preferredHeight = 20f;
        }

        private void ClearStudentCards()
        {
            foreach (GameObject card in studentCards)
            {
                if (card != null)
                {
                    Destroy(card);
                }
            }
            studentCards.Clear();
        }

        private void OnReadyButtonClicked()
        {
            Debug.Log("[StudentIntroScreen] Player clicked Ready button");

            // Resume game time FIRST before transitioning
            Time.timeScale = 1f;
            Debug.Log("[StudentIntroScreen] Time.timeScale reset to 1");

            // Hide the screen
            HideIntroScreen();

            // Transition to InLevel state
            if (GameStateManager.Instance != null)
            {
                Debug.Log($"[StudentIntroScreen] Before StartLevel: GameState={GameStateManager.Instance.CurrentState}");
                GameStateManager.Instance.StartLevel();
                Debug.Log($"[StudentIntroScreen] After StartLevel: GameState={GameStateManager.Instance.CurrentState}, Time.timeScale={Time.timeScale}");
            }
            else
            {
                Debug.LogError("[StudentIntroScreen] GameStateManager.Instance is NULL!");
            }
        }

        /// <summary>
        /// Create the intro screen UI dynamically if not set up in scene
        /// </summary>
        public void CreateUI()
        {
            Debug.Log($"[StudentIntroScreen] CreateUI called on {gameObject.name}. uiCreated={uiCreated}, screenPanel={(screenPanel != null && screenPanel ? screenPanel.name : "NULL")}, studentGridContainer={(studentGridContainer != null && studentGridContainer ? "EXISTS" : "NULL")}");

            // Use Unity's proper null check (handles destroyed objects)
            // Only skip if UI was already created AND both panel AND grid container are valid
            if (uiCreated && screenPanel != null && screenPanel && studentGridContainer != null && studentGridContainer)
            {
                Debug.Log("[StudentIntroScreen] UI already complete, skipping creation");
                return;
            }

            // Destroy old panel if it exists but is incomplete
            if (screenPanel != null && screenPanel)
            {
                Debug.Log("[StudentIntroScreen] Destroying incomplete UI panel");
                Destroy(screenPanel);
            }

            // Clear any stale references
            screenPanel = null;
            studentGridContainer = null;
            titleText = null;
            helpText = null;
            readyButton = null;

            Debug.Log("[StudentIntroScreen] Creating UI from scratch...");
            uiCreated = false;

            // Create Canvas if needed
            Canvas canvas = GetComponentInParent<Canvas>();
            Debug.Log($"[StudentIntroScreen] GetComponentInParent<Canvas>: {(canvas != null ? canvas.name : "NULL")}");

            if (canvas == null)
            {
                canvas = FindObjectOfType<Canvas>();
                Debug.Log($"[StudentIntroScreen] FindObjectOfType<Canvas>: {(canvas != null ? canvas.name : "NULL")}");
            }

            if (canvas == null)
            {
                // Create our own canvas
                Debug.Log("[StudentIntroScreen] No Canvas found, creating one...");
                GameObject canvasObj = new GameObject("StudentIntroCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }

            // Ensure canvas has GraphicRaycaster for button clicks
            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
                Debug.Log("[StudentIntroScreen] Added GraphicRaycaster to Canvas");
            }

            // Ensure EventSystem exists for UI interaction
            if (FindObjectOfType<EventSystem>() == null)
            {
                Debug.Log("[StudentIntroScreen] No EventSystem found, creating one...");
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
            }

            // Create main panel
            screenPanel = new GameObject("StudentIntroPanel");
            screenPanel.transform.SetParent(canvas.transform, false);
            Debug.Log($"[StudentIntroScreen] Created screenPanel, parent={canvas.name}");

            RectTransform panelRect = screenPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // Semi-transparent background
            Image panelBg = screenPanel.AddComponent<Image>();
            panelBg.color = new Color(0.1f, 0.1f, 0.2f, 0.95f);

            // Add vertical layout for content
            VerticalLayoutGroup panelLayout = screenPanel.AddComponent<VerticalLayoutGroup>();
            panelLayout.childAlignment = TextAnchor.MiddleCenter;
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = true;
            panelLayout.childForceExpandWidth = false;
            panelLayout.childForceExpandHeight = false;
            panelLayout.spacing = 20f;
            panelLayout.padding = new RectOffset(50, 50, 30, 30);

            // Title
            GameObject titleObj = CreateTextObject("TitleText", "GHI NHỚ HỌC SINH", 36, FontStyle.Bold, Color.white);
            titleObj.transform.SetParent(screenPanel.transform, false);
            titleText = titleObj.GetComponent<Text>();
            LayoutElement titleLayout = titleObj.AddComponent<LayoutElement>();
            titleLayout.preferredHeight = 60f;
            titleLayout.preferredWidth = 600f;

            // Student grid container
            GameObject gridObj = new GameObject("StudentGrid");
            gridObj.transform.SetParent(screenPanel.transform, false);
            studentGridContainer = gridObj;
            Debug.Log($"[StudentIntroScreen] Created studentGridContainer: {studentGridContainer != null}, name={studentGridContainer?.name}");

            RectTransform gridRect = gridObj.AddComponent<RectTransform>();
            gridRect.sizeDelta = new Vector2(800f, 400f);

            LayoutElement gridLayout = gridObj.AddComponent<LayoutElement>();
            gridLayout.preferredWidth = 800f;
            gridLayout.preferredHeight = 400f;
            gridLayout.flexibleHeight = 1f;

            GridLayoutGroup grid = gridObj.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(cardWidth, cardHeight);
            grid.spacing = new Vector2(cardSpacing, cardSpacing);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = gridColumns;
            grid.childAlignment = TextAnchor.MiddleCenter;

            // Help text
            GameObject helpObj = CreateTextObject("HelpText", "Bạn cần ghi nhớ học sinh trước khi vào game", 18, FontStyle.Italic, new Color(0.8f, 0.8f, 0.8f, 1f));
            helpObj.transform.SetParent(screenPanel.transform, false);
            helpText = helpObj.GetComponent<Text>();
            LayoutElement helpLayout = helpObj.AddComponent<LayoutElement>();
            helpLayout.preferredHeight = 40f;
            helpLayout.preferredWidth = 600f;

            // Ready button
            GameObject buttonObj = new GameObject("ReadyButton");
            buttonObj.transform.SetParent(screenPanel.transform, false);

            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(200f, 60f);

            LayoutElement buttonLayout = buttonObj.AddComponent<LayoutElement>();
            buttonLayout.preferredWidth = 200f;
            buttonLayout.preferredHeight = 60f;

            Image buttonBg = buttonObj.AddComponent<Image>();
            buttonBg.color = new Color(0.2f, 0.6f, 0.3f, 1f);

            readyButton = buttonObj.AddComponent<Button>();
            readyButton.targetGraphic = buttonBg;
            readyButton.onClick.AddListener(OnReadyButtonClicked);

            // Button text
            GameObject buttonTextObj = CreateTextObject("ButtonText", "ĐÃ NHỚ", 24, FontStyle.Bold, Color.white);
            buttonTextObj.transform.SetParent(buttonObj.transform, false);
            RectTransform buttonTextRect = buttonTextObj.GetComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.offsetMin = Vector2.zero;
            buttonTextRect.offsetMax = Vector2.zero;

            // Keep panel active - ShowIntroScreen will be called to populate it
            // screenPanel visibility is managed by ShowIntroScreen/HideIntroScreen
            uiCreated = true;
            Debug.Log($"[StudentIntroScreen] UI created, panel ready. studentGridContainer={(studentGridContainer != null ? "SET" : "NULL")}");
        }

        private GameObject CreateTextObject(string name, string text, int fontSize, FontStyle style, Color color)
        {
            GameObject obj = new GameObject(name);

            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(600f, 50f);

            Text textComp = obj.AddComponent<Text>();
            textComp.text = text;
            textComp.fontSize = fontSize;
            textComp.fontStyle = style;
            textComp.color = color;
            textComp.alignment = TextAnchor.MiddleCenter;
            textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            return obj;
        }
    }
}
