using UnityEngine;
using FunClass.Core;

namespace FunClass.Core.UI
{
    public class PopupManager : MonoBehaviour
    {
        private static PopupManager _instance;
        public static PopupManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("PopupManager");
                    _instance = go.AddComponent<PopupManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        [Header("Popup Prefab")]
        [SerializeField] private GameObject popupPrefab;

        [Header("Settings")]
        [SerializeField] private float popupOffsetY = 2.5f;
        [SerializeField] private Canvas popupCanvas;

        private StudentInteractionPopup currentPopup;
        private StudentAgent currentStudent;
        private GameObject currentOverlay;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            if (popupCanvas == null)
            {
                CreatePopupCanvas();
            }
        }

        private void CreatePopupCanvas()
        {
            // Ensure EventSystem exists
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                GameObject eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
            
            GameObject canvasGO = new GameObject("PopupCanvas");
            canvasGO.transform.SetParent(transform);
            
            popupCanvas = canvasGO.AddComponent<Canvas>();
            popupCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            popupCanvas.sortingOrder = 100;

            UnityEngine.UI.CanvasScaler scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        public void ShowPopup(StudentAgent student)
        {
            if (student == null)
            {
                Debug.LogError("[PopupManager] ShowPopup called with null student!");
                return;
            }
            
            // Set currentStudent BEFORE CloseCurrentPopup to prevent Update() from destroying new popup
            currentStudent = student;
            
            CloseCurrentPopup();
            
            if (popupCanvas == null)
            {
                CreatePopupCanvas();
            }
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (popupPrefab != null)
            {
                GameObject popupGO = Instantiate(popupPrefab, popupCanvas.transform);
                
                currentPopup = popupGO.GetComponent<StudentInteractionPopup>();
                if (currentPopup != null)
                {
                    currentPopup.Initialize(student, this);
                }
                else
                {
                    Debug.LogError("[PopupManager] Popup prefab missing StudentInteractionPopup component");
                    Destroy(popupGO);
                }
            }
            else
            {
                CreateTemporaryPopup(student);
            }
        }

        private void CreateTemporaryPopup(StudentAgent student)
        {
            currentOverlay = new GameObject("ModalOverlay");
            currentOverlay.transform.SetParent(popupCanvas.transform, false);
            RectTransform overlayRect = currentOverlay.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;
            UnityEngine.UI.Image overlayBg = currentOverlay.AddComponent<UnityEngine.UI.Image>();
            overlayBg.color = new Color(0f, 0f, 0f, 0.7f);
            
            GameObject popupGO = new GameObject("TempPopup");
            popupGO.transform.SetParent(popupCanvas.transform, false);
            
            RectTransform rectTransform = popupGO.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(600, 700);
            
            UnityEngine.UI.Image background = popupGO.AddComponent<UnityEngine.UI.Image>();
            background.color = new Color(0.15f, 0.15f, 0.2f, 0.98f);
            
            UnityEngine.UI.Outline outline = popupGO.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = new Color(0.3f, 0.5f, 1f, 1f);
            outline.effectDistance = new Vector2(4, -4);
            
            GameObject headerGO = new GameObject("HeaderText");
            headerGO.transform.SetParent(popupGO.transform, false);
            RectTransform headerRect = headerGO.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.anchoredPosition = new Vector2(0, -20);
            headerRect.sizeDelta = new Vector2(-40, 50);
            UnityEngine.UI.Text headerText = headerGO.AddComponent<UnityEngine.UI.Text>();
            headerText.font = GetDefaultFont();
            headerText.fontSize = 24;
            headerText.fontStyle = FontStyle.Bold;
            headerText.color = new Color(1f, 1f, 1f, 1f);
            headerText.alignment = TextAnchor.MiddleCenter;
            
            GameObject openingGO = new GameObject("OpeningPhraseText");
            openingGO.transform.SetParent(popupGO.transform, false);
            RectTransform openingRect = openingGO.AddComponent<RectTransform>();
            openingRect.anchorMin = new Vector2(0, 1);
            openingRect.anchorMax = new Vector2(1, 1);
            openingRect.pivot = new Vector2(0.5f, 1);
            openingRect.anchoredPosition = new Vector2(0, -80);
            openingRect.sizeDelta = new Vector2(-40, 40);
            UnityEngine.UI.Text openingText = openingGO.AddComponent<UnityEngine.UI.Text>();
            openingText.font = GetDefaultFont();
            openingText.fontSize = 18;
            openingText.color = new Color(1f, 0.9f, 0.3f, 1f);
            openingText.alignment = TextAnchor.MiddleLeft;
            
            GameObject complaintContainer = new GameObject("ComplaintListContainer");
            complaintContainer.transform.SetParent(popupGO.transform, false);
            RectTransform complaintRect = complaintContainer.AddComponent<RectTransform>();
            complaintRect.anchorMin = new Vector2(0, 0.5f);  // Start from middle
            complaintRect.anchorMax = new Vector2(1, 1);     // To top
            complaintRect.pivot = new Vector2(0.5f, 1);
            complaintRect.anchoredPosition = new Vector2(0, -130);
            complaintRect.sizeDelta = new Vector2(-40, -140);
            UnityEngine.UI.VerticalLayoutGroup complaintLayout = complaintContainer.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            complaintLayout.spacing = 10;
            complaintLayout.childForceExpandHeight = false;
            complaintLayout.childControlHeight = true;
            complaintLayout.childAlignment = TextAnchor.UpperLeft;
            complaintLayout.padding = new RectOffset(20, 20, 10, 10);

            GameObject targetContainer = new GameObject("TargetListContainer");
            targetContainer.transform.SetParent(popupGO.transform, false);
            RectTransform targetRect = targetContainer.AddComponent<RectTransform>();
            targetRect.anchorMin = new Vector2(0, 0.15f);    // Start above buttons
            targetRect.anchorMax = new Vector2(1, 0.5f);     // To middle
            targetRect.pivot = new Vector2(0.5f, 1);
            targetRect.anchoredPosition = new Vector2(0, -10);
            targetRect.sizeDelta = new Vector2(-40, 0);
            UnityEngine.UI.VerticalLayoutGroup targetLayout = targetContainer.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            targetLayout.spacing = 8;
            targetLayout.childForceExpandHeight = false;
            targetLayout.childControlHeight = true;
            targetLayout.childAlignment = TextAnchor.UpperLeft;
            targetLayout.padding = new RectOffset(20, 20, 10, 10);
            
            GameObject buttonContainer = new GameObject("ButtonContainer");
            buttonContainer.transform.SetParent(popupGO.transform, false);
            RectTransform buttonRect = buttonContainer.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0, 0);
            buttonRect.anchorMax = new Vector2(1, 0.15f);   // Give more vertical space
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = Vector2.zero;
            buttonRect.sizeDelta = new Vector2(-40, 0);
            UnityEngine.UI.HorizontalLayoutGroup buttonLayout = buttonContainer.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            buttonLayout.spacing = 15;
            buttonLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonLayout.childForceExpandWidth = false;
            buttonLayout.childControlWidth = true;
            buttonLayout.padding = new RectOffset(20, 20, 10, 10);
            
            currentPopup = popupGO.AddComponent<StudentInteractionPopup>();
            
            var popupScript = currentPopup;
            var headerField = typeof(StudentInteractionPopup).GetField("headerText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var openingField = typeof(StudentInteractionPopup).GetField("openingPhraseText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var complaintField = typeof(StudentInteractionPopup).GetField("complaintListContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var targetField = typeof(StudentInteractionPopup).GetField("targetListContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var buttonField = typeof(StudentInteractionPopup).GetField("buttonContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (headerField != null) headerField.SetValue(popupScript, headerText);
            if (openingField != null) openingField.SetValue(popupScript, openingText);
            if (complaintField != null) complaintField.SetValue(popupScript, complaintContainer.transform);
            if (targetField != null) targetField.SetValue(popupScript, targetContainer.transform);
            if (buttonField != null) buttonField.SetValue(popupScript, buttonContainer.transform);
            
            // PopupAnimator will create CanvasGroup in Awake()
            PopupAnimator animator = popupGO.AddComponent<PopupAnimator>();
            
            currentPopup.Initialize(student, this);
        }

        private Font GetDefaultFont()
        {
            Font font = Resources.Load<Font>("Fonts/DefaultFont");
            if (font == null)
            {
                font = Font.CreateDynamicFontFromOSFont("Arial", 14);
            }
            return font;
        }

        public void CloseCurrentPopup()
        {
            if (currentPopup != null)
            {
                Destroy(currentPopup.gameObject);
                currentPopup = null;
                currentStudent = null;
            }
            
            if (currentOverlay != null)
            {
                Destroy(currentOverlay);
                currentOverlay = null;
            }
            
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void OnInfluenceResolved(StudentAgent source, StudentAgent target)
        {
            if (currentPopup != null && currentStudent != null)
            {
                if (currentStudent == source || currentStudent == target)
                {
                    currentPopup.RefreshContent();
                }
            }
        }

        public void OnWholeClassResolved(StudentAgent source)
        {
            if (currentPopup != null && currentStudent == source)
            {
                currentPopup.RefreshContent();
            }
        }

        private void Update()
        {
            if (currentPopup != null && currentStudent == null)
            {
                CloseCurrentPopup();
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseCurrentPopup();
            }
        }

        public void SetPopupPrefab(GameObject prefab)
        {
            popupPrefab = prefab;
        }

        public bool IsPopupOpen => currentPopup != null;
        public StudentAgent CurrentStudent => currentStudent;
    }
}
