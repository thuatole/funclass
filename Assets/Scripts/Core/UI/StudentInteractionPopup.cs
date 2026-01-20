using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using FunClass.Core;

namespace FunClass.Core.UI
{
    public enum PopupType
    {
        TargetStudent,              // Student is only affected by others
        SourceInfoOnly,             // Student affects others but no actions available
        SourceWholeClassAction,     // Student affects whole class
        SourceIndividualActions,    // Student affects specific students
        SourceAndTarget            // Student both affects others AND is affected by others
    }

    public class StudentInteractionPopup : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Text headerText;
        [SerializeField] private Text openingPhraseText;
        [SerializeField] private Transform complaintListContainer;
        [SerializeField] private Transform targetListContainer;
        [SerializeField] private Transform buttonContainer;

        [Header("Prefabs")]
        [SerializeField] private GameObject complaintItemPrefab;
        [SerializeField] private GameObject targetActionItemPrefab;
        [SerializeField] private GameObject buttonPrefab;

        private StudentAgent student;
        private PopupManager popupManager;
        private PopupType currentPopupType;

        public void Initialize(StudentAgent student, PopupManager manager)
        {
            this.student = student;
            this.popupManager = manager;

            if (!PopupTextLoader.Instance.IsLoaded)
            {
                PopupTextLoader.Instance.LoadAllConfigs();
            }

            GenerateContent();
        }

        public void RefreshContent()
        {
            ClearContent();
            GenerateContent();
        }

        private void ClearContent()
        {
            if (headerText != null)
            {
                headerText.text = "";
            }
            
            if (openingPhraseText != null)
            {
                openingPhraseText.text = "";
            }
            
            if (complaintListContainer != null)
            {
                int childCount = complaintListContainer.childCount;
                for (int i = childCount - 1; i >= 0; i--)
                {
                    DestroyImmediate(complaintListContainer.GetChild(i).gameObject);
                }
            }

            if (targetListContainer != null)
            {
                int childCount = targetListContainer.childCount;
                for (int i = childCount - 1; i >= 0; i--)
                {
                    DestroyImmediate(targetListContainer.GetChild(i).gameObject);
                }
            }

            if (buttonContainer != null)
            {
                int childCount = buttonContainer.childCount;
                for (int i = childCount - 1; i >= 0; i--)
                {
                    DestroyImmediate(buttonContainer.GetChild(i).gameObject);
                }
            }
        }

        private void GenerateContent()
        {
            if (student == null)
            {
                Debug.LogError("[Popup] GenerateContent: student is null!");
                return;
            }

            currentPopupType = DeterminePopupType(student);
            Debug.Log($"[Popup] {student.Config?.studentName} -> {currentPopupType}");

            UpdateHeader();

            switch (currentPopupType)
            {
                case PopupType.TargetStudent:
                    GenerateTargetStudentPopup();
                    break;
                case PopupType.SourceInfoOnly:
                    GenerateSourceInfoOnlyPopup();
                    break;
                case PopupType.SourceWholeClassAction:
                    GenerateSourceWholeClassPopup();
                    break;
                case PopupType.SourceIndividualActions:
                    GenerateSourceIndividualActionsPopup();
                    break;
                case PopupType.SourceAndTarget:
                    GenerateSourceAndTargetPopup();
                    break;
            }
        }

        private PopupType DeterminePopupType(StudentAgent student)
        {
            var affectedStudents = GetAffectedStudents(student);
            var influenceSources = GetInfluenceSources(student);

            Debug.Log($"[Popup] DeterminePopupType for {student.Config?.studentName}:");
            Debug.Log($"[Popup]   - Affects {affectedStudents.Count} student(s)");
            Debug.Log($"[Popup]   - Affected by {influenceSources.Count} source(s)");

            // Case 1: Pure target (only affected, doesn't affect anyone)
            if (affectedStudents.Count == 0)
            {
                Debug.Log($"[Popup] → PopupType.TargetStudent (no one affected by this student)");
                return PopupType.TargetStudent;
            }

            // Case 2-5: Student affects others
            var eventType = GetSourceEventType(student);
            Debug.Log($"[Popup] Source event type: {eventType}");

            // Check if this student is ALSO affected by others
            bool isAlsoTarget = influenceSources.Count > 0;

            if (!HasStudentResolveAction(eventType))
            {
                Debug.Log($"[Popup] → PopupType.SourceInfoOnly (no student resolve action for {eventType})");
                return PopupType.SourceInfoOnly;
            }
            else if (IsWholeClassAction(eventType))
            {
                Debug.Log($"[Popup] → PopupType.SourceWholeClassAction (whole class action)");
                return PopupType.SourceWholeClassAction;
            }
            else
            {
                // Check if student is BOTH source AND target
                if (isAlsoTarget)
                {
                    Debug.Log($"[Popup] → PopupType.SourceAndTarget (affects {affectedStudents.Count} students AND affected by {influenceSources.Count} sources)");
                    return PopupType.SourceAndTarget;
                }
                else
                {
                    Debug.Log($"[Popup] → PopupType.SourceIndividualActions (individual actions for {affectedStudents.Count} students)");
                    return PopupType.SourceIndividualActions;
                }
            }
        }

        private void UpdateHeader()
        {
            if (headerText != null && student != null)
            {
                string studentName = student.Config?.studentName ?? "Student";
                string state = student.CurrentState.ToString();
                string emoji = PopupTextLoader.Instance.GetStateEmoji(state);
                
                headerText.text = $"{studentName} - {state} {emoji}";
            }
        }

        private void GenerateTargetStudentPopup()
        {
            Debug.Log($"[Popup] GenerateTargetStudentPopup for {student.Config?.studentName}");

            if (openingPhraseText != null)
            {
                openingPhraseText.text = $"💬 \"{PopupTextLoader.Instance.GetTargetOpeningPhrase()}\"";
            }

            var influenceSources = GetInfluenceSources(student);

            Debug.Log($"[Popup] This student is affected by {influenceSources.Count} sources");
            foreach (var src in influenceSources)
            {
                string resolvedStatus = src.isResolved ? "✓ resolved" : "✗ unresolved";
                Debug.Log($"[Popup]   - Affected by: {src.sourceStudent?.Config?.studentName} ({src.eventType}) [{resolvedStatus}]");
            }

            if (influenceSources.Count == 0)
            {
                CreateComplaintText(PopupTextLoader.Instance.GetTargetNoComplaints(), "😌");
            }
            else
            {
                foreach (var source in influenceSources)
                {
                    string sourceName = ExtractLetter(source.sourceStudent?.Config?.studentName);
                    string eventTypeStr = source.eventType.ToString();
                    string complaint = PopupTextLoader.Instance.GetComplaint(eventTypeStr, sourceName);
                    string icon = PopupTextLoader.Instance.GetComplaintTemplate(eventTypeStr).icon;

                    // Add checkmark prefix if source is resolved
                    if (source.isResolved)
                    {
                        complaint = $"✓ {complaint}";
                    }

                    CreateComplaintText(complaint, icon);
                }
            }

            // Add Escort button if student is outside
            // Check if ALL sources are resolved (not just the filtered unresolved list)
            bool allSourcesResolved = (student.InfluenceSources == null ||
                                       student.InfluenceSources.AreAllSourcesResolved());
            bool canEscort = IsStudentOutside(student) && allSourcesResolved;

            Debug.Log($"[Popup] Escort check: outside={IsStudentOutside(student)}, allSourcesResolved={allSourcesResolved}, canEscort={canEscort}");

            if (IsStudentOutside(student))
            {
                CreateButton(PopupTextLoader.Instance.GetTargetEscortButton(canEscort), () => EscortStudent(student), canEscort);
            }

            CreateButton(PopupTextLoader.Instance.GetTargetCloseButton(), () => ClosePopup());
        }

        private void GenerateSourceInfoOnlyPopup()
        {
            var affectedStudents = GetAffectedStudents(student);
            int unresolvedCount = affectedStudents.Count;

            var eventType = GetSourceEventType(student);
            string statement = PopupTextLoader.Instance.GetSourceStatement(eventType.ToString());

            if (openingPhraseText != null)
            {
                openingPhraseText.text = $"💬 \"{statement}\"";
            }

            string impactMessage = PopupTextLoader.Instance.GetSourceImpactWholeClass(unresolvedCount);
            CreateComplaintText(impactMessage, "⚠️");

            CreateButton(PopupTextLoader.Instance.GetSourceCloseButton(), () => ClosePopup());
        }

        private void GenerateSourceWholeClassPopup()
        {
            var affectedStudents = GetAffectedStudents(student);
            int unresolvedCount = affectedStudents.Count;

            var eventType = GetSourceEventType(student);
            string targets = GetTargetsString(affectedStudents);
            string statement = PopupTextLoader.Instance.GetSourceStatement(eventType.ToString(), targets);

            if (openingPhraseText != null)
            {
                openingPhraseText.text = $"💬 \"{statement}\"";
            }

            string impactMessage = PopupTextLoader.Instance.GetSourceImpactWholeClass(unresolvedCount);
            CreateComplaintText(impactMessage, "⚠️");

            CreateButton(PopupTextLoader.Instance.GetSourceResolveWholeClassButton(), () => ResolveForWholeClass(student));
            CreateButton(PopupTextLoader.Instance.GetSourceCloseButton(), () => ClosePopup());
        }

        private void GenerateSourceIndividualActionsPopup()
        {
            var affectedStudents = GetAffectedStudents(student);

            Debug.Log($"[Popup] GenerateSourceIndividualActionsPopup for {student.Config?.studentName}");
            Debug.Log($"[Popup] This student is affecting {affectedStudents.Count} students");

            var groupedByAction = GroupTargetsByActionType(student, affectedStudents);

            foreach (var actionGroup in groupedByAction)
            {
                string actionType = actionGroup.Key;
                List<StudentAgent> targets = actionGroup.Value;

                Debug.Log($"[Popup] Action group: {actionType} → {targets.Count} targets");
                foreach (var t in targets)
                {
                    Debug.Log($"[Popup]   - Target: {t.Config?.studentName}");
                }

                string targetsString = GetTargetsString(targets);
                string statement = PopupTextLoader.Instance.GetSourceStatement(actionType, targetsString);

                if (openingPhraseText != null)
                {
                    openingPhraseText.text = $"💬 \"{statement}\"";
                }

                CreateComplaintText(PopupTextLoader.Instance.GetSourceImpactIndividual(), "⚠️");

                // Create target list with individual resolve buttons
                foreach (var target in targets)
                {
                    string targetName = ExtractLetter(target.Config?.studentName);
                    Debug.Log($"[Popup] Creating action button for target: {targetName}");
                    CreateTargetActionItemWithButton(target, targetName, () => ResolveForTarget(student, target));
                }
            }

            CreateButton(PopupTextLoader.Instance.GetSourceCloseButton(), () => ClosePopup());
        }

        private void GenerateSourceAndTargetPopup()
        {
            Debug.Log($"[Popup] GenerateSourceAndTargetPopup for {student.Config?.studentName}");

            // PART 1: Show who affects THIS student (Target role)
            var influenceSources = GetInfluenceSources(student);

            Debug.Log($"[Popup] PART 1 - This student is affected by {influenceSources.Count} sources");
            foreach (var src in influenceSources)
            {
                string resolvedStatus = src.isResolved ? "✓ resolved" : "✗ unresolved";
                Debug.Log($"[Popup]   - Affected by: {src.sourceStudent?.Config?.studentName} ({src.eventType}) [{resolvedStatus}]");
            }

            if (influenceSources.Count > 0)
            {
                if (openingPhraseText != null)
                {
                    openingPhraseText.text = $"💬 \"Cô ơi! Em bị ảnh hưởng...\"";
                }

                // Show complaints about sources
                CreateComplaintText("📋 Em đang bị ảnh hưởng bởi:", "😟");
                foreach (var source in influenceSources)
                {
                    string sourceName = ExtractLetter(source.sourceStudent?.Config?.studentName);
                    string eventTypeStr = source.eventType.ToString();
                    string complaint = PopupTextLoader.Instance.GetComplaint(eventTypeStr, sourceName);
                    string icon = PopupTextLoader.Instance.GetComplaintTemplate(eventTypeStr).icon;

                    // Add checkmark if resolved
                    if (source.isResolved)
                    {
                        complaint = $"✓ {complaint}";
                    }

                    CreateComplaintText(complaint, icon);
                }
            }

            // PART 2: Show who THIS student affects (Source role)
            var affectedStudents = GetAffectedStudents(student);

            Debug.Log($"[Popup] PART 2 - This student is affecting {affectedStudents.Count} students");
            foreach (var t in affectedStudents)
            {
                Debug.Log($"[Popup]   - Affecting: {t.Config?.studentName}");
            }

            if (affectedStudents.Count > 0)
            {
                var groupedByAction = GroupTargetsByActionType(student, affectedStudents);

                foreach (var actionGroup in groupedByAction)
                {
                    string actionType = actionGroup.Key;
                    List<StudentAgent> targets = actionGroup.Value;

                    Debug.Log($"[Popup] Action group: {actionType} → {targets.Count} targets");

                    CreateComplaintText(PopupTextLoader.Instance.GetSourceImpactIndividual(), "⚠️");

                    // Create target list with individual resolve buttons
                    foreach (var target in targets)
                    {
                        string targetName = ExtractLetter(target.Config?.studentName);
                        Debug.Log($"[Popup] Creating action button for target: {targetName}");
                        CreateTargetActionItemWithButton(target, targetName, () => ResolveForTarget(student, target));
                    }
                }
            }

            CreateButton(PopupTextLoader.Instance.GetSourceCloseButton(), () => ClosePopup());
        }

        private void CreateComplaintText(string text, string icon)
        {
            if (complaintListContainer == null) return;

            GameObject item = new GameObject("ComplaintItem");
            item.transform.SetParent(complaintListContainer, false);

            RectTransform itemRect = item.AddComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(0, 50);

            Text textComponent = item.AddComponent<Text>();
            textComponent.text = $"{icon} {text}";
            textComponent.font = GetDefaultFont();
            textComponent.fontSize = 16;
            textComponent.color = new Color(1f, 1f, 1f, 1f);  // Pure white for better contrast
            textComponent.alignment = TextAnchor.UpperLeft;
            textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            textComponent.verticalOverflow = VerticalWrapMode.Overflow;

            UnityEngine.UI.LayoutElement layoutElement = item.AddComponent<UnityEngine.UI.LayoutElement>();
            layoutElement.minHeight = 50;
            layoutElement.preferredHeight = -1;  // Auto height based on content
            layoutElement.flexibleHeight = 1;
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

        private void CreateTargetActionItem(StudentAgent target, string targetName, System.Action onResolve)
        {
            if (targetListContainer == null) return;

            GameObject item = new GameObject($"TargetItem_{targetName}");
            item.transform.SetParent(targetListContainer, false);

            RectTransform itemRect = item.AddComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(0, 25);

            Text nameText = item.AddComponent<Text>();
            nameText.text = $"• {targetName}";
            nameText.font = GetDefaultFont();
            nameText.fontSize = 14;
            nameText.color = Color.white;
            nameText.alignment = TextAnchor.MiddleLeft;

            string buttonLabel = PopupTextLoader.Instance.GetSourceResolveIndividualButton(targetName);
            CreateButton(buttonLabel, onResolve);
        }

        private void CreateTargetActionItemWithButton(StudentAgent target, string targetName, System.Action onResolve)
        {
            if (targetListContainer == null) return;

            // Create horizontal container for target name + button
            GameObject item = new GameObject($"TargetItem_{targetName}");
            item.transform.SetParent(targetListContainer, false);

            RectTransform itemRect = item.AddComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(0, 60);

            // Add horizontal layout
            UnityEngine.UI.HorizontalLayoutGroup layout = item.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandWidth = false;
            layout.childControlWidth = true;
            layout.padding = new RectOffset(10, 10, 5, 5);

            // Create name text
            GameObject nameGO = new GameObject("NameText");
            nameGO.transform.SetParent(item.transform, false);

            Text nameText = nameGO.AddComponent<Text>();
            nameText.text = $"• {targetName}";
            nameText.font = GetDefaultFont();
            nameText.fontSize = 16;
            nameText.color = Color.white;
            nameText.alignment = TextAnchor.MiddleLeft;

            UnityEngine.UI.LayoutElement nameLayout = nameGO.AddComponent<UnityEngine.UI.LayoutElement>();
            nameLayout.preferredWidth = 150;
            nameLayout.flexibleWidth = 0;

            // Create resolve button inline
            string buttonLabel = PopupTextLoader.Instance.GetSourceResolveIndividualButton(targetName);

            GameObject buttonGO = new GameObject($"ResolveButton_{targetName}");
            buttonGO.transform.SetParent(item.transform, false);

            RectTransform buttonRect = buttonGO.AddComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(200, 50);

            UnityEngine.UI.Image buttonBg = buttonGO.AddComponent<UnityEngine.UI.Image>();
            buttonBg.color = new Color(0.2f, 0.7f, 0.3f, 1f); // Green for action

            Button button = buttonGO.AddComponent<Button>();
            button.interactable = true;
            button.onClick.AddListener(() => onResolve?.Invoke());

            GameObject btnTextGO = new GameObject("Text");
            btnTextGO.transform.SetParent(buttonGO.transform, false);
            RectTransform btnTextRect = btnTextGO.AddComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.sizeDelta = Vector2.zero;

            Text buttonText = btnTextGO.AddComponent<Text>();
            buttonText.text = buttonLabel;
            buttonText.font = GetDefaultFont();
            buttonText.fontSize = 14;
            buttonText.color = Color.white;
            buttonText.alignment = TextAnchor.MiddleCenter;

            UnityEngine.UI.LayoutElement buttonLayoutElem = buttonGO.AddComponent<UnityEngine.UI.LayoutElement>();
            buttonLayoutElem.preferredWidth = 200;
            buttonLayoutElem.preferredHeight = 50;
            buttonLayoutElem.flexibleWidth = 0;
        }

        private void CreateButton(string label, System.Action onClick, bool enabled = true)
        {
            if (buttonContainer == null) return;

            GameObject buttonGO = new GameObject($"Button_{label}");
            buttonGO.transform.SetParent(buttonContainer, false);

            RectTransform buttonRect = buttonGO.AddComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(200, 60);  // Increased width and height

            UnityEngine.UI.Image buttonBg = buttonGO.AddComponent<UnityEngine.UI.Image>();
            buttonBg.color = enabled ? new Color(0.2f, 0.5f, 0.8f, 1f) : new Color(0.3f, 0.3f, 0.3f, 1f);

            Button button = buttonGO.AddComponent<Button>();
            button.interactable = enabled;
            button.onClick.AddListener(() => onClick?.Invoke());

            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);
            RectTransform textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            Text buttonText = textGO.AddComponent<Text>();
            buttonText.text = label;
            buttonText.font = GetDefaultFont();
            buttonText.fontSize = 16;
            buttonText.color = Color.white;
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.horizontalOverflow = HorizontalWrapMode.Overflow;
            buttonText.verticalOverflow = VerticalWrapMode.Truncate;
            buttonText.resizeTextForBestFit = false;

            // Add LayoutElement to control button size in horizontal layout
            UnityEngine.UI.LayoutElement buttonLayout = buttonGO.AddComponent<UnityEngine.UI.LayoutElement>();
            buttonLayout.preferredWidth = 200;
            buttonLayout.preferredHeight = 60;
            buttonLayout.flexibleWidth = 0;
        }

        private List<StudentAgent> GetAffectedStudents(StudentAgent source)
        {
            List<StudentAgent> affectedStudents = new List<StudentAgent>();
            
            if (source == null)
            {
                Debug.Log($"[Popup] GetAffectedStudents: source is null");
                return affectedStudents;
            }
            
            StudentAgent[] allStudents = FindObjectsOfType<StudentAgent>();
            Debug.Log($"[Popup] GetAffectedStudents for {source.Config?.studentName}: checking {allStudents.Length} students");
            
            foreach (StudentAgent student in allStudents)
            {
                if (student == source) continue;
                if (student.InfluenceSources == null) continue;
                
                var activeSources = student.InfluenceSources.GetActiveSources();
                foreach (var influenceSource in activeSources)
                {
                    if (!influenceSource.isResolved && influenceSource.sourceStudent == source)
                    {
                        affectedStudents.Add(student);
                        Debug.Log($"[Popup] - {student.Config?.studentName} is affected by {source.Config?.studentName} ({influenceSource.eventType})");
                        break;
                    }
                }
            }
            
            Debug.Log($"[Popup] Total affected students: {affectedStudents.Count}");
            return affectedStudents;
        }

        private List<InfluenceSourceData> GetInfluenceSources(StudentAgent target)
        {
            List<InfluenceSourceData> sources = new List<InfluenceSourceData>();
            
            if (target == null || target.InfluenceSources == null)
            {
                Debug.Log($"[Popup] GetInfluenceSources: target or InfluenceSources is null");
                return sources;
            }
            
            var activeSources = target.InfluenceSources.GetActiveSources();
            Debug.Log($"[Popup] GetInfluenceSources for {target.Config?.studentName}: {activeSources.Count} active sources");

            // Show ALL sources (both resolved and unresolved) for complete history
            foreach (var source in activeSources)
            {
                if (source.sourceStudent != null)
                {
                    sources.Add(new InfluenceSourceData
                    {
                        sourceStudent = source.sourceStudent,
                        eventType = source.eventType,
                        isResolved = source.isResolved
                    });
                    string resolvedStatus = source.isResolved ? "✓ resolved" : "✗ unresolved";
                    Debug.Log($"[Popup] - Source: {source.sourceStudent.Config?.studentName} ({source.eventType}) [{resolvedStatus}]");
                }
            }
            
            return sources;
        }

        private Dictionary<string, List<StudentAgent>> GroupTargetsByActionType(StudentAgent source, List<StudentAgent> targets)
        {
            Dictionary<string, List<StudentAgent>> grouped = new Dictionary<string, List<StudentAgent>>();
            
            foreach (StudentAgent target in targets)
            {
                if (target.InfluenceSources == null) continue;
                
                var activeSources = target.InfluenceSources.GetActiveSources();
                foreach (var influenceSource in activeSources)
                {
                    if (!influenceSource.isResolved && influenceSource.sourceStudent == source)
                    {
                        string actionType = influenceSource.eventType.ToString();
                        
                        if (!grouped.ContainsKey(actionType))
                        {
                            grouped[actionType] = new List<StudentAgent>();
                        }
                        
                        if (!grouped[actionType].Contains(target))
                        {
                            grouped[actionType].Add(target);
                        }
                    }
                }
            }
            
            Debug.Log($"[Popup] Grouped {targets.Count} targets into {grouped.Count} action types");
            return grouped;
        }

        private StudentEventType GetSourceEventType(StudentAgent source)
        {
            if (source == null || source.InfluenceSources == null)
            {
                return StudentEventType.MessCreated;
            }
            
            StudentAgent[] allStudents = FindObjectsOfType<StudentAgent>();
            foreach (StudentAgent student in allStudents)
            {
                if (student == source) continue;
                if (student.InfluenceSources == null) continue;
                
                var activeSources = student.InfluenceSources.GetActiveSources();
                foreach (var influenceSource in activeSources)
                {
                    if (!influenceSource.isResolved && influenceSource.sourceStudent == source)
                    {
                        Debug.Log($"[Popup] GetSourceEventType: {source.Config?.studentName} has event type {influenceSource.eventType}");
                        return influenceSource.eventType;
                    }
                }
            }
            
            return StudentEventType.MessCreated;
        }

        private bool HasStudentResolveAction(StudentEventType eventType)
        {
            return eventType switch
            {
                StudentEventType.MessCreated => false,
                StudentEventType.MakingNoise => true,
                StudentEventType.ThrowingObject => true,
                StudentEventType.KnockedOverObject => true,
                StudentEventType.WanderingAround => true,
                _ => false
            };
        }

        private bool IsWholeClassAction(StudentEventType eventType)
        {
            return eventType switch
            {
                StudentEventType.MakingNoise => true,
                StudentEventType.KnockedOverObject => true,
                StudentEventType.WanderingAround => false, // Individual action - affects nearby students
                _ => false
            };
        }

        private bool IsStudentOutside(StudentAgent student)
        {
            if (student == null) return false;

            // Check if student has moved away from original seat position
            float distanceFromSeat = Vector3.Distance(student.transform.position, student.OriginalSeatPosition);
            float thresholdDistance = 2.0f; // Consider "outside" if more than 2 units from seat

            bool isOutside = distanceFromSeat > thresholdDistance;

            Debug.Log($"[Popup] IsStudentOutside({student.Config?.studentName}): distance={distanceFromSeat:F2}m, threshold={thresholdDistance}m → {isOutside}");

            return isOutside;
        }

        private string ExtractLetter(string studentName)
        {
            if (string.IsNullOrEmpty(studentName)) return "?";
            
            // Return full student name
            return studentName;
        }

        private string GetTargetsString(List<StudentAgent> targets)
        {
            if (targets.Count == 0) return "";
            if (targets.Count == 1) return ExtractLetter(targets[0].Config?.studentName);
            if (targets.Count == 2) return $"{ExtractLetter(targets[0].Config?.studentName)} và {ExtractLetter(targets[1].Config?.studentName)}";
            
            string result = "";
            for (int i = 0; i < targets.Count - 1; i++)
            {
                result += ExtractLetter(targets[i].Config?.studentName) + ", ";
            }
            result += "và " + ExtractLetter(targets[targets.Count - 1].Config?.studentName);
            return result;
        }

        private void ResolveForTarget(StudentAgent source, StudentAgent target)
        {
            Debug.Log($"[Popup] Resolving influence from {source.Config?.studentName} on {target.Config?.studentName}");

            // Calm down the source student to resolve their influence on target
            if (source != null)
            {
                source.HandleTeacherAction(TeacherActionType.Calm);
                Debug.Log($"[Popup] Calmed source {source.Config?.studentName} - this resolves influence on {target.Config?.studentName}");
            }

            RefreshContent();
        }

        private void ResolveForWholeClass(StudentAgent source)
        {
            Debug.Log($"[Popup] Resolving whole class influence from {source.Config?.studentName}");

            // Calm down the source student to resolve their whole class influence
            if (source != null)
            {
                source.HandleTeacherAction(TeacherActionType.Calm);
                Debug.Log($"[Popup] Calmed source {source.Config?.studentName} - this resolves whole class influence");
            }

            RefreshContent();
        }

        private void EscortStudent(StudentAgent student)
        {
            if (student == null)
            {
                Debug.LogError("[Popup] EscortStudent called with null student!");
                return;
            }

            Debug.Log($"[Popup] Escorting {student.Config?.studentName} back to seat");

            // Check if all influence sources are resolved (should be, but double-check)
            if (student.InfluenceSources != null && !student.InfluenceSources.AreAllSourcesResolved())
            {
                int unresolvedCount = student.InfluenceSources.GetUnresolvedSourceCount();
                Debug.LogWarning($"[Popup] Cannot escort - {unresolvedCount} unresolved sources remain!");
                ClosePopup();
                return;
            }

            // Calm down student completely
            Debug.Log($"[Popup] Calming down {student.Config?.studentName} from {student.CurrentState}...");
            int deescalateCount = 0;
            while (student.CurrentState != StudentState.Calm && deescalateCount < 10)
            {
                student.DeescalateState();
                deescalateCount++;
            }
            Debug.Log($"[Popup] Calmed to {student.CurrentState}");

            // Clear all influence sources
            if (student.InfluenceSources != null)
            {
                student.InfluenceSources.ClearAllSources();
                Debug.Log($"[Popup] Cleared all influence sources for {student.Config?.studentName}");
            }

            // Set immunity to prevent immediate re-escalation
            student.SetInfluenceImmunity(15f);

            // Stop any routes
            student.StopRoute();

            // Return to seat with visual movement
            if (StudentMovementManager.Instance != null)
            {
                StudentMovementManager.Instance.ReturnToSeat(student);
                Debug.Log($"[Popup] Using StudentMovementManager to return {student.Config?.studentName} to seat");
            }
            else
            {
                student.ReturnToSeat(); // Teleport fallback
                Debug.Log($"[Popup] Teleporting {student.Config?.studentName} back to seat");
            }

            // Trigger teacher action for reactions
            student.HandleTeacherAction(TeacherActionType.EscortStudentBack);

            Debug.Log($"[Popup] ✓ Successfully escorted {student.Config?.studentName} back to seat");

            ClosePopup();
        }

        private void ClosePopup()
        {
            popupManager?.CloseCurrentPopup();
        }
    }

    public class InfluenceSourceData
    {
        public StudentAgent sourceStudent;
        public StudentEventType eventType;
        public bool isResolved;
    }
}
