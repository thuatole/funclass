using UnityEngine;
using UnityEngine.UI;

namespace FunClass.Core.UI
{
    /// <summary>
    /// Auto-creates StudentIntroScreen UI if not present in scene.
    /// Attach this to any GameObject (e.g., GameManager) to ensure the intro screen exists.
    /// </summary>
    public class StudentIntroScreenSetup : MonoBehaviour
    {
        [Header("Optional: Assign existing UI elements")]
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private StudentIntroScreen existingIntroScreen;

        [Header("Settings")]
        [SerializeField] private bool autoCreateIfMissing = true;

        void Awake()
        {
            // Check if StudentIntroScreen singleton already exists
            if (StudentIntroScreen.Instance != null)
            {
                Debug.Log("[StudentIntroScreenSetup] StudentIntroScreen.Instance already exists, skipping");
                return;
            }

            if (existingIntroScreen != null)
            {
                Debug.Log("[StudentIntroScreenSetup] Using existing StudentIntroScreen");
                return;
            }

            // Try to find existing
            existingIntroScreen = FindObjectOfType<StudentIntroScreen>();
            if (existingIntroScreen != null)
            {
                Debug.Log("[StudentIntroScreenSetup] Found existing StudentIntroScreen in scene");
                return;
            }

            if (!autoCreateIfMissing)
            {
                Debug.LogWarning("[StudentIntroScreenSetup] No StudentIntroScreen found and autoCreate is disabled");
                return;
            }

            // Create the intro screen
            CreateIntroScreen();
        }

        private void CreateIntroScreen()
        {
            Debug.Log("[StudentIntroScreenSetup] Creating StudentIntroScreen...");

            // Find or create canvas
            if (targetCanvas == null)
            {
                targetCanvas = FindObjectOfType<Canvas>();
            }

            if (targetCanvas == null)
            {
                // Create new canvas
                GameObject canvasObj = new GameObject("StudentIntroCanvas");
                targetCanvas = canvasObj.AddComponent<Canvas>();
                targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                targetCanvas.sortingOrder = 100; // On top of other UI

                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();

                Debug.Log("[StudentIntroScreenSetup] Created new Canvas");
            }

            // Create intro screen object
            GameObject introObj = new GameObject("StudentIntroScreen");
            introObj.transform.SetParent(targetCanvas.transform, false);

            StudentIntroScreen introScreen = introObj.AddComponent<StudentIntroScreen>();

            // Call CreateUI to build the UI elements
            introScreen.CreateUI();

            existingIntroScreen = introScreen;
            Debug.Log("[StudentIntroScreenSetup] StudentIntroScreen created successfully");
        }
    }
}
