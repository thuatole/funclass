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
                string stateVN = PopupTextLoader.Instance.GetStateNameVietnamese(state);
                string emoji = PopupTextLoader.Instance.GetStateEmoji(state);

                Color stateColor = GetStateColor(student.CurrentState);
                string hexColor = ColorUtility.ToHtmlStringRGB(stateColor);
                headerText.text = $"{studentName}  <color=#{hexColor}>[{emoji} {stateVN}]</color>";
                headerText.supportRichText = true;
            }

            // Auto-configure containers if not already set up
            EnsureVerticalLayout(complaintListContainer, 8, new RectOffset(0, 0, 4, 4));
            EnsureVerticalLayout(targetListContainer, 6, new RectOffset(0, 0, 4, 4));
            EnsureHorizontalLayout(buttonContainer, 10, new RectOffset(0, 0, 6, 6));
        }

        // ──────────────────────────────────────────────────────────────────
        // Design system helpers
        // ──────────────────────────────────────────────────────────────────

        private static Color GetStateColor(StudentState state) => state switch
        {
            StudentState.Calm       => new Color(0.20f, 0.78f, 0.60f),
            StudentState.Distracted => new Color(0.95f, 0.75f, 0.20f),
            StudentState.ActingOut  => new Color(0.95f, 0.50f, 0.15f),
            StudentState.Critical   => new Color(0.90f, 0.25f, 0.25f),
            _                       => new Color(0.60f, 0.65f, 0.75f)
        };

        // Name-based consistent avatar color (same student always same color)
        private static Color GetAvatarColor(string name)
        {
            int hash = 0;
            foreach (char c in (name ?? "")) hash = hash * 31 + c;
            float h = Mathf.Abs(hash % 360) / 360f;
            return Color.HSVToRGB(h, 0.60f, 0.75f);
        }

        private static Color CardBg    => new Color(0.12f, 0.14f, 0.24f, 0.92f);
        private static Color DividerColor => new Color(0.30f, 0.33f, 0.48f, 0.50f);
        private static Color TextPrimary  => new Color(0.92f, 0.93f, 0.97f);
        private static Color TextSecondary => new Color(0.60f, 0.65f, 0.78f);

        private enum ButtonStyle { Resolve, ResolveWhole, Close, Disabled }

        private static Color GetButtonColor(ButtonStyle style, bool enabled) => style switch
        {
            ButtonStyle.Resolve      => enabled ? new Color(0.13f, 0.65f, 0.38f) : new Color(0.25f, 0.35f, 0.30f),
            ButtonStyle.ResolveWhole => enabled ? new Color(0.10f, 0.52f, 0.78f) : new Color(0.20f, 0.30f, 0.40f),
            ButtonStyle.Close        => new Color(0.20f, 0.22f, 0.34f),
            _                        => new Color(0.25f, 0.25f, 0.30f)
        };

        private void EnsureVerticalLayout(Transform t, int spacing, RectOffset padding)
        {
            if (t == null) return;
            var vlg = t.GetComponent<VerticalLayoutGroup>() ?? t.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = spacing;
            vlg.padding = padding;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            var csf = t.GetComponent<ContentSizeFitter>() ?? t.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private void EnsureHorizontalLayout(Transform t, int spacing, RectOffset padding)
        {
            if (t == null) return;
            var hlg = t.GetComponent<HorizontalLayoutGroup>() ?? t.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = spacing;
            hlg.padding = padding;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            // Must be true so HLG honors LayoutElement.preferredWidth/Height of buttons.
            // With false, HLG ignores LayoutElement and uses RectTransform.sizeDelta (which is 0×0
            // since BuildButton doesn't set sizeDelta) → buttons render with 0 width and disappear.
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
        }

        // Add card background image to a container
        private Image AddCardBackground(GameObject go, Color color, float cornerPad = 0)
        {
            Image img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        // Create a left-side accent bar on a container.
        // Uses LayoutElement.ignoreLayout = true so the parent's HorizontalLayoutGroup
        // doesn't reposition this overlay — bar stays anchored full-height on the left edge.
        private void AddAccentBar(GameObject parent, Color color, float width = 3f)
        {
            GameObject bar = new GameObject("AccentBar");
            bar.transform.SetParent(parent.transform, false);
            RectTransform rt = bar.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(width, 0);
            Image img = bar.AddComponent<Image>();
            img.color = color;

            // Keep bar out of the parent layout flow
            LayoutElement le = bar.AddComponent<LayoutElement>();
            le.ignoreLayout = true;
        }

        private void GenerateTargetStudentPopup()
        {
            Debug.Log($"[Popup] GenerateTargetStudentPopup for {student.Config?.studentName}");

            var influenceSources = GetInfluenceSources(student);

            string stateKey = student.CurrentState.ToString();
            if (openingPhraseText != null)
            {
                string context = DetermineContext(student, influenceSources.Count > 0);
                string phrase = PopupTextLoader.Instance.GetOpeningPhrase(stateKey, context);
                openingPhraseText.text = $"💬 \"{phrase}\"";
            }

            Debug.Log($"[Popup] This student is affected by {influenceSources.Count} sources");
            foreach (var src in influenceSources)
            {
                string resolvedStatus = src.isResolved ? "✓ resolved" : "✗ unresolved";
                Debug.Log($"[Popup]   - Affected by: {src.sourceStudent?.Config?.studentName} ({src.eventType}) [{resolvedStatus}]");
            }

            if (influenceSources.Count == 0)
            {
                string noComplaintCtx = DetermineContext(student, false);
                CreateComplaintText(PopupTextLoader.Instance.GetTargetNoComplaints(noComplaintCtx), "😌");
            }
            else
            {
                foreach (var source in influenceSources)
                {
                    string sourceName = ExtractLetter(source.sourceStudent?.Config?.studentName);
                    string eventTypeStr = source.eventType.ToString();
                    string icon = PopupTextLoader.Instance.GetComplaintTemplate(eventTypeStr).icon;

                    // Choose direct vs indirect complaint based on influence scope
                    string complaint = GetComplaintByScope(source, sourceName, eventTypeStr);

                    if (source.isResolved)
                        complaint = $"✓ {complaint}";

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

            string impactMessage = IsWholeClassAction(eventType)
                ? PopupTextLoader.Instance.GetSourceImpactWholeClass(unresolvedCount)
                : PopupTextLoader.Instance.GetSourceImpactIndividual(unresolvedCount);
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

                CreateSectionLabel(PopupTextLoader.Instance.GetSourceImpactIndividual(targets.Count));

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
                    string stateKey = student.CurrentState.ToString();
                    string context = DetermineContext(student, true);
                    string phrase = PopupTextLoader.Instance.GetOpeningPhrase(stateKey, context);
                    openingPhraseText.text = $"💬 \"{phrase}\"";
                }

                CreateSectionLabel("📋 Đang bị ảnh hưởng bởi:");
                foreach (var source in influenceSources)
                {
                    string sourceName = ExtractLetter(source.sourceStudent?.Config?.studentName);
                    string eventTypeStr = source.eventType.ToString();
                    string icon = PopupTextLoader.Instance.GetComplaintTemplate(eventTypeStr).icon;

                    string complaint = GetComplaintByScope(source, sourceName, eventTypeStr);
                    if (source.isResolved)
                        complaint = $"✓ {complaint}";

                    CreateComplaintText(complaint, icon);
                }
            }

            // PART 2: Show who THIS student affects (Source role)
            var affectedStudents = GetAffectedStudents(student);
            if (affectedStudents.Count > 0 && influenceSources.Count > 0) CreateDivider();

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

                    CreateSectionLabel(PopupTextLoader.Instance.GetSourceImpactIndividual(targets.Count));

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

        /// Returns context string for opening phrase: Default, AfterCalmed, SelfCaused, Influenced.
        private string DetermineContext(StudentAgent s, bool hasExternalSources, bool isSelfCause = false)
        {
            if (s.CurrentState == StudentState.Calm)
            {
                if (TeacherController.Instance != null &&
                    TeacherController.Instance.WasRecentlyCalmed(s.Config?.studentId))
                    return "AfterCalmed";
                return "Default";
            }
            if (hasExternalSources) return "Influenced";
            if (isSelfCause)        return "SelfCaused";
            return "Default";
        }

        /// Determine direct vs indirect complaint based on influence scope of the source event.
        private string GetComplaintByScope(InfluenceSourceData source, string sourceName, string eventTypeStr)
        {
            InfluenceScope scope = DeriveScope(source);
            string objectName = string.IsNullOrEmpty(source.sourceObjectName) ? null : source.sourceObjectName;
            if (scope == InfluenceScope.SingleStudent)
                return PopupTextLoader.Instance.GetDirectComplaint(eventTypeStr, sourceName, objectName);
            return PopupTextLoader.Instance.GetIndirectComplaint(eventTypeStr, sourceName, objectName);
        }

        private InfluenceScope DeriveScope(InfluenceSourceData source)
        {
            // Check LevelConfig influenceScopeConfig first (authoritative)
            if (LevelManager.Instance != null)
            {
                var levelConfig = LevelManager.Instance.GetCurrentLevelConfig();
                if (levelConfig?.influenceScopeConfig != null)
                {
                    // GetScope returns string "SingleStudent"/"WholeClass"/"None"
                    string scopeStr = levelConfig.influenceScopeConfig.GetScope(source.eventType.ToString());
                    if (scopeStr == "SingleStudent") return InfluenceScope.SingleStudent;
                    if (scopeStr == "WholeClass")    return InfluenceScope.WholeClass;
                    if (scopeStr == "None")          return InfluenceScope.None;
                    // scopeStr == "None" or unrecognized → fall through to event-based default
                }
            }
            // Fallback: derive from event type the same way StudentEvent does
            // Note: KnockedOverObject defaults to SingleStudent here because summer break
            // config sets it to SingleStudent. WholeClass is the StudentEvent.cs default
            // but that fires before per-level config is applied.
            return source.eventType switch
            {
                StudentEventType.ThrowingObject    => InfluenceScope.SingleStudent,
                StudentEventType.KnockedOverObject => InfluenceScope.SingleStudent,
                StudentEventType.MessCreated       => InfluenceScope.WholeClass,
                StudentEventType.MakingNoise       => InfluenceScope.WholeClass,
                StudentEventType.WanderingAround   => InfluenceScope.WholeClass,
                _                                  => InfluenceScope.WholeClass
            };
        }

        // ──────────────────────────────────────────────────────────────────
        // UI factory methods (new design)
        // ──────────────────────────────────────────────────────────────────

        private void CreateComplaintText(string text, string icon)
        {
            if (complaintListContainer == null) return;

            // Strip duplicate icon — text from pools already contains emoji prefix
            string display = text;

            GameObject item = new GameObject("ComplaintItem");
            item.transform.SetParent(complaintListContainer, false);

            // Subtle card background
            Image bg = item.AddComponent<Image>();
            bg.color = CardBg;

            HorizontalLayoutGroup row = item.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 8;
            row.padding = new RectOffset(10, 10, 8, 8);
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childForceExpandWidth = false;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandHeight = false;

            // Text
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(item.transform, false);
            Text t = textGO.AddComponent<Text>();
            t.text = display;
            t.font = GetDefaultFont();
            t.fontSize = 15;
            t.color = TextPrimary;
            t.alignment = TextAnchor.MiddleLeft;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;

            LayoutElement le = textGO.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;

            LayoutElement itemLE = item.AddComponent<LayoutElement>();
            itemLE.minHeight = 36;
            itemLE.preferredHeight = -1;
            itemLE.flexibleHeight = 0;
        }

        private void CreateSectionLabel(string text)
        {
            if (complaintListContainer == null) return;

            GameObject go = new GameObject("SectionLabel");
            go.transform.SetParent(complaintListContainer, false);

            Text t = go.AddComponent<Text>();
            t.text = text.ToUpper();
            t.font = GetDefaultFont();
            t.fontSize = 11;
            t.color = TextSecondary;
            t.alignment = TextAnchor.MiddleLeft;

            LayoutElement le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 20;
            le.minHeight = 20;
        }

        private void CreateDivider()
        {
            if (complaintListContainer == null) return;

            GameObject go = new GameObject("Divider");
            go.transform.SetParent(complaintListContainer, false);

            Image img = go.AddComponent<Image>();
            img.color = DividerColor;

            LayoutElement le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 1;
            le.minHeight = 1;
            le.flexibleWidth = 1;
        }

        private void CreateTargetActionItemWithButton(StudentAgent target, string targetName, System.Action onResolve)
        {
            if (targetListContainer == null) return;

            Color avatarColor = GetAvatarColor(targetName);

            // Card container
            GameObject card = new GameObject($"TargetCard_{targetName}");
            card.transform.SetParent(targetListContainer, false);

            Image cardBg = card.AddComponent<Image>();
            cardBg.color = CardBg;

            // Left accent bar
            AddAccentBar(card, avatarColor, 3f);

            HorizontalLayoutGroup row = card.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 10;
            row.padding = new RectOffset(14, 10, 8, 8);
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childForceExpandWidth = false;
            row.childControlWidth = true;
            row.childControlHeight = false;
            row.childForceExpandHeight = false;

            LayoutElement cardLE = card.AddComponent<LayoutElement>();
            cardLE.minHeight = 48;
            cardLE.preferredHeight = 48;

            // Avatar circle
            GameObject avatarGO = new GameObject("Avatar");
            avatarGO.transform.SetParent(card.transform, false);

            Image avatarImg = avatarGO.AddComponent<Image>();
            avatarImg.color = avatarColor;

            LayoutElement avatarLE = avatarGO.AddComponent<LayoutElement>();
            avatarLE.preferredWidth = 32;
            avatarLE.preferredHeight = 32;
            avatarLE.minWidth = 32;
            avatarLE.flexibleWidth = 0;

            // Avatar initial letter
            GameObject initGO = new GameObject("Initial");
            initGO.transform.SetParent(avatarGO.transform, false);
            RectTransform initRT = initGO.AddComponent<RectTransform>();
            initRT.anchorMin = Vector2.zero;
            initRT.anchorMax = Vector2.one;
            initRT.sizeDelta = Vector2.zero;
            Text initText = initGO.AddComponent<Text>();
            initText.text = targetName.Length > 0 ? targetName[0].ToString().ToUpper() : "?";
            initText.font = GetDefaultFont();
            initText.fontSize = 14;
            initText.fontStyle = FontStyle.Bold;
            initText.color = Color.white;
            initText.alignment = TextAnchor.MiddleCenter;

            // Name
            GameObject nameGO = new GameObject("Name");
            nameGO.transform.SetParent(card.transform, false);
            Text nameText = nameGO.AddComponent<Text>();
            nameText.text = targetName;
            nameText.font = GetDefaultFont();
            nameText.fontSize = 15;
            nameText.fontStyle = FontStyle.Bold;
            nameText.color = TextPrimary;
            nameText.alignment = TextAnchor.MiddleLeft;
            LayoutElement nameLE = nameGO.AddComponent<LayoutElement>();
            nameLE.flexibleWidth = 1;

            // Resolve button compact — load label from PopupTextLoader (data-driven, supports localization).
            // Caller targetName context already shown via avatar+name, so button only needs short verb.
            string resolveLabel = PopupTextLoader.Instance.GetSourceResolveIndividualButton(targetName);
            BuildButton(card.transform, resolveLabel, GetButtonColor(ButtonStyle.Resolve, true), 120, 34, onResolve, true);
        }

        private void CreateButton(string label, System.Action onClick, bool enabled = true)
        {
            if (buttonContainer == null) return;

            bool isClose = label.Contains("Đóng") || label.Contains("Close") || label.Contains("❌");
            bool isResolveWhole = label.Contains("cả lớp") || label.Contains("Giải quyết cho cả");

            ButtonStyle style = isClose ? ButtonStyle.Close
                              : isResolveWhole ? ButtonStyle.ResolveWhole
                              : ButtonStyle.Resolve;

            int btnWidth = isClose ? 110 : isResolveWhole ? 180 : 150;

            BuildButton(buttonContainer, label, GetButtonColor(style, enabled), btnWidth, 38, onClick, enabled);
        }

        // Shared button builder used by both inline (target card) and footer buttons
        private void BuildButton(Transform parent, string label, Color bgColor, int width, int height, System.Action onClick, bool enabled)
        {
            GameObject go = new GameObject($"Btn_{label}");
            go.transform.SetParent(parent, false);

            Image bg = go.AddComponent<Image>();
            bg.color = enabled ? bgColor : new Color(bgColor.r, bgColor.g, bgColor.b, 0.45f);

            Button btn = go.AddComponent<Button>();
            btn.interactable = enabled;
            btn.onClick.AddListener(() => onClick?.Invoke());

            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            colors.fadeDuration = 0.1f;
            btn.colors = colors;
            btn.targetGraphic = bg;

            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(go.transform, false);
            RectTransform textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.sizeDelta = Vector2.zero;
            textRT.offsetMin = new Vector2(6, 0);
            textRT.offsetMax = new Vector2(-6, 0);

            Text t = textGO.AddComponent<Text>();
            t.text = label;
            t.font = GetDefaultFont();
            t.fontSize = 13;
            t.fontStyle = FontStyle.Bold;
            t.color = enabled ? Color.white : new Color(1f, 1f, 1f, 0.5f);
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;

            LayoutElement le = go.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.minWidth = width;
            le.preferredHeight = height;
            le.minHeight = height;
            le.flexibleWidth = 0;
        }

        private Font GetDefaultFont()
        {
            Font font = Resources.Load<Font>("Fonts/DefaultFont");
            if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 14);
            return font;
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
                        isResolved = source.isResolved,
                        sourceObjectName = source.sourceObjectName
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
            // Authoritative: level config decides scope per event type
            if (LevelManager.Instance != null)
            {
                var levelConfig = LevelManager.Instance.GetCurrentLevelConfig();
                if (levelConfig?.influenceScopeConfig != null)
                {
                    string scopeStr = levelConfig.influenceScopeConfig.GetScope(eventType.ToString());
                    if (scopeStr == "WholeClass")    return true;
                    if (scopeStr == "SingleStudent") return false;
                    // "None" or unrecognized → fall through to default
                }
            }

            // Fallback: hardcoded defaults if no level config
            return eventType switch
            {
                StudentEventType.MakingNoise => true,
                StudentEventType.KnockedOverObject => true,
                StudentEventType.WanderingAround => false,
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
            StudentMovementManager.Instance.StopMovement(student);

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
        public string sourceObjectName;  // Vietnamese name of thrown/knocked object — may be null
    }
}
