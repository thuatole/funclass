using UnityEngine;
#if TEXTMESHPRO_PRESENT
using TMPro;
#endif

namespace FunClass.Core
{
    /// <summary>
    /// Visual marker for students to differentiate them easily
    /// Shows student name and color-codes the capsule
    /// Works with or without TextMeshPro
    /// </summary>
    public class StudentVisualMarker : MonoBehaviour
    {
        [Header("Visual Settings")]
        [SerializeField] private Color[] studentColors = new Color[]
        {
            new Color(1f, 0.2f, 0.2f),      // Bright Red - Student A
            new Color(0.2f, 0.5f, 1f),      // Bright Blue - Student B
            new Color(0.2f, 1f, 0.2f),      // Bright Green - Student C
            new Color(1f, 1f, 0.2f),        // Bright Yellow - Student D
            new Color(1f, 0.5f, 0f),        // Orange - Student E
            new Color(0.8f, 0.2f, 1f),      // Purple - Student F
            new Color(0.2f, 1f, 1f),        // Cyan - Student G
            new Color(1f, 0.2f, 0.8f)       // Magenta - Student H
        };

        [Header("Label Settings")]
        [SerializeField] private GameObject labelPrefab;
        [SerializeField] private Vector3 labelOffset = new Vector3(0, 2f, 0);
        [SerializeField] private float labelSize = 0.5f;

        private StudentAgent studentAgent;
        private GameObject labelObject;
#if TEXTMESHPRO_PRESENT
        private TextMeshPro labelText;
#else
        private TextMesh labelText;
#endif
        private Renderer studentRenderer;

        void Start()
        {
            studentAgent = GetComponent<StudentAgent>();
            if (studentAgent == null)
            {
                Debug.LogWarning($"[StudentVisualMarker] No StudentAgent found on {gameObject.name}");
                return;
            }

            ApplyVisualMarkers();
        }

        private void ApplyVisualMarkers()
        {
            if (studentAgent.Config == null)
            {
                Debug.LogWarning($"[StudentVisualMarker] StudentAgent has no config on {gameObject.name}");
                return;
            }

            string studentName = studentAgent.Config.studentName;

            // Find and destroy any existing labels (from Editor import or previous runs)
            Transform existingLabel = transform.Find($"{studentName}_Label");
            if (existingLabel != null)
            {
                Destroy(existingLabel.gameObject);
                Debug.Log($"[StudentVisualMarker] Destroyed existing label for {studentName}");
            }

            // Also destroy if labelObject reference exists
            if (labelObject != null)
            {
                Destroy(labelObject);
                labelObject = null;
                labelText = null;
            }

            // Apply color to capsule
            ApplyStudentColor(studentName);

            // NOTE: Floating label disabled - students are now introduced via StudentIntroScreen
            // CreateFloatingLabel(studentName);

            Debug.Log($"[StudentVisualMarker] Applied visual markers to {studentName} (label disabled)");
        }

        private void ApplyStudentColor(string studentName)
        {
            // Get renderer (capsule mesh)
            studentRenderer = GetComponentInChildren<Renderer>();
            if (studentRenderer == null)
            {
                Debug.LogWarning($"[StudentVisualMarker] No Renderer found for {studentName}");
                return;
            }

            // Determine color index from student name
            int colorIndex = GetColorIndexFromName(studentName);
            Color studentColor = studentColors[colorIndex % studentColors.Length];

            // Create new material instance to avoid shared material issues
            Material mat = new Material(studentRenderer.material);
            mat.color = studentColor;
            studentRenderer.material = mat;

            Debug.Log($"[StudentVisualMarker] ✓ {studentName} assigned color index {colorIndex}: RGB({studentColor.r:F2}, {studentColor.g:F2}, {studentColor.b:F2})");
        }

        private void CreateFloatingLabel(string studentName)
        {
            // Extract letter from "Student_A" -> "A"
            string displayName = ExtractLetterFromName(studentName);

            // Create label GameObject
            labelObject = new GameObject($"{studentName}_Label");
            labelObject.transform.SetParent(transform);
            labelObject.transform.localPosition = labelOffset;
            labelObject.transform.localRotation = Quaternion.identity;

#if TEXTMESHPRO_PRESENT
            // Add TextMeshPro
            labelText = labelObject.AddComponent<TextMeshPro>();
            labelText.text = displayName;
            labelText.fontSize = 4;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = Color.white;

            // Add outline for better visibility
            labelText.outlineWidth = 0.2f;
            labelText.outlineColor = Color.black;
#else
            // Fallback: use legacy TextMesh
            labelText = labelObject.AddComponent<TextMesh>();
            labelText.text = displayName;
            labelText.fontSize = 48;
            labelText.characterSize = 0.1f;
            labelText.anchor = TextAnchor.MiddleCenter;
            labelText.alignment = TextAlignment.Center;
            labelText.color = Color.white;
#endif

            // Make label always face camera
            Billboard billboard = labelObject.AddComponent<Billboard>();

            Debug.Log($"[StudentVisualMarker] Created label '{displayName}' for {studentName}");
        }
        
        private string ExtractLetterFromName(string studentName)
        {
            // Extract letter from "Student_A", "Student_B", etc.
            if (studentName.Contains("_"))
            {
                string[] parts = studentName.Split('_');
                if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1]))
                {
                    return parts[1]; // Return "A", "B", "C", etc.
                }
            }
            
            // Fallback: return first character
            return studentName.Length > 0 ? studentName.Substring(0, 1) : "?";
        }

        private int GetColorIndexFromName(string studentName)
        {
            // Extract letter from "Student_A", "Student_B", etc.
            if (studentName.Contains("_"))
            {
                string[] parts = studentName.Split('_');
                if (parts.Length > 1)
                {
                    string letter = parts[1];
                    if (letter.Length > 0)
                    {
                        char c = letter[0];
                        // A=0, B=1, C=2, etc.
                        return c - 'A';
                    }
                }
            }

            // Fallback: use hash
            return Mathf.Abs(studentName.GetHashCode()) % studentColors.Length;
        }

        // NOTE: Update disabled - floating label is no longer used
        // void Update()
        // {
        //     // Update label to always face camera
        //     if (labelObject != null && Camera.main != null)
        //     {
        //         labelObject.transform.LookAt(Camera.main.transform);
        //         labelObject.transform.Rotate(0, 180, 0); // Flip to face camera
        //     }
        // }
    }

    /// <summary>
    /// Simple billboard component to make label face camera
    /// </summary>
    public class Billboard : MonoBehaviour
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
