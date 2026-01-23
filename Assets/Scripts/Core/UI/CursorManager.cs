using UnityEngine;

namespace FunClass.Core.UI
{
    /// <summary>
    /// Manages custom cursor for the entire game.
    /// Attach to a persistent GameObject (e.g., GameManager).
    /// </summary>
    public class CursorManager : MonoBehaviour
    {
        public static CursorManager Instance { get; private set; }

        [Header("Cursor Textures")]
        [SerializeField] private Texture2D defaultCursor; // Main cursor (e.g., chalk)
        [SerializeField] private Texture2D clickCursor;   // Optional: cursor when clicking
        [SerializeField] private Texture2D hoverCursor;   // Optional: cursor when hovering UI

        [Header("Hotspot Settings")]
        [SerializeField] private Vector2 defaultHotspot = new Vector2(0, 0);
        [SerializeField] private Vector2 clickHotspot = new Vector2(0, 0);
        [SerializeField] private Vector2 hoverHotspot = new Vector2(0, 0);

        [Header("Cursor Mode")]
        [SerializeField] private CursorMode cursorMode = CursorMode.Auto;

        private bool isCustomCursorActive = false;

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

        void Start()
        {
            // Apply custom cursor at game start
            ApplyDefaultCursor();
        }

        /// <summary>
        /// Apply the default custom cursor
        /// </summary>
        public void ApplyDefaultCursor()
        {
            if (defaultCursor != null)
            {
                Cursor.SetCursor(defaultCursor, defaultHotspot, cursorMode);
                isCustomCursorActive = true;
                Debug.Log("[CursorManager] Custom cursor applied");
            }
        }

        /// <summary>
        /// Apply click cursor (call on mouse down)
        /// </summary>
        public void ApplyClickCursor()
        {
            if (clickCursor != null)
            {
                Cursor.SetCursor(clickCursor, clickHotspot, cursorMode);
            }
        }

        /// <summary>
        /// Apply hover cursor (call when hovering interactive elements)
        /// </summary>
        public void ApplyHoverCursor()
        {
            if (hoverCursor != null)
            {
                Cursor.SetCursor(hoverCursor, hoverHotspot, cursorMode);
            }
        }

        /// <summary>
        /// Reset to default custom cursor
        /// </summary>
        public void ResetToDefault()
        {
            ApplyDefaultCursor();
        }

        /// <summary>
        /// Reset to system cursor
        /// </summary>
        public void UseSystemCursor()
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            isCustomCursorActive = false;
        }

        /// <summary>
        /// Show cursor (for UI screens)
        /// </summary>
        public void ShowCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            ApplyDefaultCursor();
        }

        /// <summary>
        /// Hide cursor (for gameplay with locked camera)
        /// </summary>
        public void HideCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        /// <summary>
        /// Confine cursor to game window
        /// </summary>
        public void ConfineCursor()
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
            ApplyDefaultCursor();
        }

        void Update()
        {
            // Optional: Apply click cursor on mouse down
            if (clickCursor != null && isCustomCursorActive)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    ApplyClickCursor();
                }
                else if (Input.GetMouseButtonUp(0))
                {
                    ApplyDefaultCursor();
                }
            }
        }
    }
}
