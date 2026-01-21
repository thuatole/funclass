using UnityEngine;

namespace FunClass.Core.UI
{
    /// <summary>
    /// Displays influence status icons above students
    /// - Influencer icon: shown when this student is influencing others
    /// - Influenced icon: shown when this student is being influenced by others
    /// Icons disappear when the influence problem is resolved
    /// </summary>
    public class InfluenceStatusIcon : MonoBehaviour
    {
        [Header("Icon Settings")]
        [SerializeField] private Vector3 iconOffset = new Vector3(0, 2.5f, 0);
        [SerializeField] private float iconSize = 0.5f;
        [SerializeField] private float iconSpacing = 0.6f;

        [Header("Icon Colors")]
        [SerializeField] private Color influencerColor = new Color(1f, 0.5f, 0f, 1f);  // Orange - causing trouble
        [SerializeField] private Color influencedColor = new Color(0.5f, 0.5f, 1f, 1f); // Blue - being affected

        [Header("Icon Symbols")]
        [SerializeField] private string influencerSymbol = "!";   // Exclamation - causing influence
        [SerializeField] private string influencedSymbol = "?";   // Question mark - being influenced

        private StudentAgent studentAgent;
        private GameObject influencerIconObject;
        private GameObject influencedIconObject;
        private TextMesh influencerText;
        private TextMesh influencedText;

        private bool isInfluencer = false;
        private bool isInfluenced = false;
        private int influencingCount = 0;   // How many students this student is influencing
        private int influencedByCount = 0;  // How many students are influencing this student

        void Start()
        {
            studentAgent = GetComponent<StudentAgent>();
            if (studentAgent == null)
            {
                Debug.LogWarning($"[InfluenceStatusIcon] No StudentAgent found on {gameObject.name}");
                return;
            }

            CreateIcons();
            SubscribeToEvents();
        }

        void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void CreateIcons()
        {
            // Create Influencer Icon (left side)
            influencerIconObject = CreateIconObject("InfluencerIcon", influencerSymbol, influencerColor, -iconSpacing / 2f);
            influencerText = influencerIconObject.GetComponent<TextMesh>();
            influencerIconObject.SetActive(false);

            // Create Influenced Icon (right side)
            influencedIconObject = CreateIconObject("InfluencedIcon", influencedSymbol, influencedColor, iconSpacing / 2f);
            influencedText = influencedIconObject.GetComponent<TextMesh>();
            influencedIconObject.SetActive(false);
        }

        private GameObject CreateIconObject(string name, string symbol, Color color, float xOffset)
        {
            GameObject iconObj = new GameObject(name);
            iconObj.transform.SetParent(transform);
            iconObj.transform.localPosition = iconOffset + new Vector3(xOffset, 0, 0);
            iconObj.transform.localRotation = Quaternion.identity;

            // Add TextMesh for the icon
            TextMesh textMesh = iconObj.AddComponent<TextMesh>();
            textMesh.text = symbol;
            textMesh.fontSize = 64;
            textMesh.characterSize = iconSize * 0.1f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = color;
            textMesh.fontStyle = FontStyle.Bold;

            // Add billboard component
            iconObj.AddComponent<IconBillboard>();

            return iconObj;
        }

        private void SubscribeToEvents()
        {
            if (StudentInfluenceManager.Instance != null)
            {
                // Subscribe to influence events would be ideal, but we'll use polling for now
            }
        }

        private void UnsubscribeFromEvents()
        {
            // Cleanup if needed
        }

        void Update()
        {
            // Update icons to face camera
            UpdateIconVisibility();
        }

        private void UpdateIconVisibility()
        {
            if (studentAgent == null || studentAgent.InfluenceSources == null) return;

            // Check if this student is being influenced (has unresolved sources)
            int unresolvedSources = studentAgent.InfluenceSources.GetUnresolvedSourceCount();
            bool shouldShowInfluenced = unresolvedSources > 0;

            if (shouldShowInfluenced != isInfluenced)
            {
                isInfluenced = shouldShowInfluenced;
                influencedByCount = unresolvedSources;
                influencedIconObject.SetActive(isInfluenced);

                if (isInfluenced)
                {
                    // Update text to show count if > 1
                    influencedText.text = unresolvedSources > 1 ? $"{influencedSymbol}{unresolvedSources}" : influencedSymbol;
                    Debug.Log($"[InfluenceStatusIcon] {studentAgent.Config?.studentName} showing INFLUENCED icon (by {unresolvedSources} source(s))");
                }
                else
                {
                    Debug.Log($"[InfluenceStatusIcon] {studentAgent.Config?.studentName} hiding INFLUENCED icon");
                }
            }
            else if (isInfluenced && influencedByCount != unresolvedSources)
            {
                // Update count display
                influencedByCount = unresolvedSources;
                influencedText.text = unresolvedSources > 1 ? $"{influencedSymbol}{unresolvedSources}" : influencedSymbol;
            }
        }

        /// <summary>
        /// Called when this student starts influencing another student
        /// </summary>
        public void ShowInfluencerIcon()
        {
            influencingCount++;
            if (!isInfluencer)
            {
                isInfluencer = true;
                influencerIconObject.SetActive(true);
                Debug.Log($"[InfluenceStatusIcon] {studentAgent.Config?.studentName} showing INFLUENCER icon");
            }
            UpdateInfluencerText();
        }

        /// <summary>
        /// Called when this student's influence on another is resolved
        /// </summary>
        public void OnInfluenceResolved()
        {
            influencingCount = Mathf.Max(0, influencingCount - 1);
            UpdateInfluencerText();

            if (influencingCount <= 0)
            {
                isInfluencer = false;
                influencerIconObject.SetActive(false);
                Debug.Log($"[InfluenceStatusIcon] {studentAgent.Config?.studentName} hiding INFLUENCER icon");
            }
        }

        /// <summary>
        /// Force hide all icons (e.g., when student is calmed down completely)
        /// </summary>
        public void HideAllIcons()
        {
            isInfluencer = false;
            isInfluenced = false;
            influencingCount = 0;
            influencedByCount = 0;
            influencerIconObject.SetActive(false);
            influencedIconObject.SetActive(false);
        }

        private void UpdateInfluencerText()
        {
            if (influencingCount > 1)
            {
                influencerText.text = $"{influencerSymbol}{influencingCount}";
            }
            else
            {
                influencerText.text = influencerSymbol;
            }
        }

        /// <summary>
        /// Get current influence status for debugging
        /// </summary>
        public string GetStatusString()
        {
            string status = "";
            if (isInfluencer) status += $"Influencing {influencingCount} ";
            if (isInfluenced) status += $"Influenced by {influencedByCount}";
            return string.IsNullOrEmpty(status) ? "No influence" : status;
        }
    }

    /// <summary>
    /// Billboard component for influence icons
    /// </summary>
    public class IconBillboard : MonoBehaviour
    {
        void LateUpdate()
        {
            if (Camera.main != null)
            {
                transform.LookAt(Camera.main.transform);
                transform.Rotate(0, 180, 0);
            }
        }
    }
}
