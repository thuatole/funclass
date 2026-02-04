using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FunClass.Core.UI
{
    [Serializable]
    public class PopupTextData
    {
        public TargetStudentText targetStudent;
        public SourceStudentText sourceStudent;
        public Dictionary<string, string> stateEmojis;
    }

    [Serializable]
    public class TargetStudentText
    {
        public string openingPhrase;
        public string noComplaints;
        public string escortButtonEnabled;
        public string escortButtonDisabled;
        public string closeButton;
    }

    [Serializable]
    public class SourceStudentText
    {
        public string impactWholeClass;
        public string impactIndividual;
        public string resolveWholeClassButton;
        public string resolveIndividualButton;
        public string closeButton;
    }

    [Serializable]
    public class ComplaintTemplate
    {
        public string template;
        public string icon;
    }

    [Serializable]
    public class ComplaintTemplatesData
    {
        public Dictionary<string, ComplaintTemplate> complaints;
    }

    [Serializable]
    public class SourceStatementsData
    {
        public Dictionary<string, List<string>> statements;
    }

    [Serializable]
    public class ButtonLabelsData
    {
        public Dictionary<string, string> actions;
        public Dictionary<string, string> tooltips;
    }

    [Serializable]
    public class EventTypeMappingData
    {
        public Dictionary<string, string> sourceStatementMapping;
        public Dictionary<string, string> complaintMapping;
    }

    public class PopupTextLoader : MonoBehaviour
    {
        private static PopupTextLoader _instance;
        public static PopupTextLoader Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("PopupTextLoader");
                    _instance = go.AddComponent<PopupTextLoader>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private PopupTextData popupText;
        private ComplaintTemplatesData complaintTemplates;
        private SourceStatementsData sourceStatements;
        private ButtonLabelsData buttonLabels;
        private EventTypeMappingData eventTypeMapping;

        private bool isLoaded = false;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            LoadAllConfigs();
        }

        public void LoadAllConfigs()
        {
            try
            {
                string configPath = Path.Combine(Application.dataPath, "Configs", "GUI");
                
                string popupTextPath = Path.Combine(configPath, "PopupText.json");
                string complaintTemplatesPath = Path.Combine(configPath, "ComplaintTemplates.json");
                string sourceStatementsPath = Path.Combine(configPath, "SourceStatements.json");
                string buttonLabelsPath = Path.Combine(configPath, "ButtonLabels.json");
                string eventTypeMappingPath = Path.Combine(configPath, "EventTypeMapping.json");

                if (File.Exists(popupTextPath))
                {
                    string json = File.ReadAllText(popupTextPath);
                    popupText = JsonUtility.FromJson<PopupTextData>(json);
                }
                else
                {
                    Debug.LogError($"[PopupTextLoader] PopupText.json not found at {popupTextPath}");
                    CreateDefaultPopupText();
                }

                if (File.Exists(complaintTemplatesPath))
                {
                    string json = File.ReadAllText(complaintTemplatesPath);
                    complaintTemplates = JsonUtility.FromJson<ComplaintTemplatesData>(json);
                }
                else
                {
                    Debug.LogError($"[PopupTextLoader] ComplaintTemplates.json not found");
                    CreateDefaultComplaintTemplates();
                }

                if (File.Exists(sourceStatementsPath))
                {
                    string json = File.ReadAllText(sourceStatementsPath);
                    sourceStatements = JsonUtility.FromJson<SourceStatementsData>(json);
                }
                else
                {
                    Debug.LogError($"[PopupTextLoader] SourceStatements.json not found");
                    CreateDefaultSourceStatements();
                }

                if (File.Exists(buttonLabelsPath))
                {
                    string json = File.ReadAllText(buttonLabelsPath);
                    buttonLabels = JsonUtility.FromJson<ButtonLabelsData>(json);
                }
                else
                {
                    Debug.LogError($"[PopupTextLoader] ButtonLabels.json not found");
                    CreateDefaultButtonLabels();
                }

                if (File.Exists(eventTypeMappingPath))
                {
                    string json = File.ReadAllText(eventTypeMappingPath);
                    eventTypeMapping = JsonUtility.FromJson<EventTypeMappingData>(json);
                }
                else
                {
                    Debug.LogWarning($"[PopupTextLoader] EventTypeMapping.json not found, using default mapping");
                    CreateDefaultEventTypeMapping();
                }

                isLoaded = true;
                Debug.Log("[PopupTextLoader] All configs loaded successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"[PopupTextLoader] Error loading configs: {e.Message}");
                CreateAllDefaults();
            }
        }

        private void CreateDefaultPopupText()
        {
            popupText = new PopupTextData
            {
                targetStudent = new TargetStudentText
                {
                    openingPhrase = "Cô ơi!",
                    noComplaints = "Em ổn rồi cô!",
                    escortButtonEnabled = "🏠 Escort Back",
                    escortButtonDisabled = "🏠 Escort Back",
                    closeButton = "❌ Close"
                },
                sourceStudent = new SourceStudentText
                {
                    impactWholeClass = "⚠️ Đang ảnh hưởng cả lớp ({count} học sinh)",
                    impactIndividual = "⚠️ Đang ảnh hưởng:",
                    resolveWholeClassButton = "✅ Giải quyết cho cả lớp",
                    resolveIndividualButton = "✅ Giải quyết cho {studentName}",
                    closeButton = "❌ Close"
                },
                stateEmojis = new Dictionary<string, string>
                {
                    { "Calm", "😌" },
                    { "Distracted", "😕" },
                    { "ActingOut", "😠" },
                    { "Critical", "😰" }
                }
            };
        }

        private void CreateDefaultComplaintTemplates()
        {
            complaintTemplates = new ComplaintTemplatesData
            {
                complaints = new Dictionary<string, ComplaintTemplate>
                {
                    { "MessCreated", new ComplaintTemplate { template = "Bạn {source} ói, thúi quá!", icon = "😷" } },
                    { "PhysicalInteraction", new ComplaintTemplate { template = "Bạn {source} đánh con, đau lắm!", icon = "😢" } },
                    { "ThrowingObject", new ComplaintTemplate { template = "Bạn {source} ném đồ vào con!", icon = "🎯" } },
                    { "MakingNoise", new ComplaintTemplate { template = "Bạn {source} làm ồn, con không học được!", icon = "🔊" } },
                    { "Distraction", new ComplaintTemplate { template = "Bạn {source} làm con mất tập trung!", icon = "😵" } },
                    { "Poop", new ComplaintTemplate { template = "Bạn {source} ỉa, thúi lắm cô!", icon = "💩" } }
                }
            };
        }

        private void CreateDefaultSourceStatements()
        {
            sourceStatements = new SourceStatementsData
            {
                statements = new Dictionary<string, List<string>>
                {
                    { "Vomit", new List<string> { "Em ói rồi cô ơi...", "Em không kìm được cô...", "Em bị ốm cô ơi..." } },
                    { "Poop", new List<string> { "Em không kìm được cô ơi...", "Em đau bụng quá cô...", "Em xin lỗi cô..." } },
                    { "Hit", new List<string> { "Em tức quá cô ơi, nên em đánh bạn {targets}...", "Bạn ấy chọc em trước cô, nên em đánh bạn {targets}!" } },
                    { "ThrowObject", new List<string> { "Em không cố ý cô ơi, em ném đồ vào bạn {targets}..." } },
                    { "MakeNoise", new List<string> { "Em đang nói chuyện với bạn {targets} cô ơi..." } }
                }
            };
        }

        private void CreateDefaultButtonLabels()
        {
            buttonLabels = new ButtonLabelsData
            {
                actions = new Dictionary<string, string>
                {
                    { "resolveWholeClass", "✅ Giải quyết cho cả lớp" },
                    { "resolveIndividual", "✅ Giải quyết cho {name}" },
                    { "escortBack", "🏠 Escort Back" },
                    { "close", "❌ Close" }
                },
                tooltips = new Dictionary<string, string>
                {
                    { "escortDisabled", "Cần giải quyết các nguồn gốc trước" },
                    { "escortEnabled", "Đưa học sinh về chỗ ngồi" }
                }
            };
        }

        private void CreateDefaultEventTypeMapping()
        {
            eventTypeMapping = new EventTypeMappingData
            {
                sourceStatementMapping = new Dictionary<string, string>
                {
                    { "MessCreated", "Vomit" },
                    { "StudentVomited", "Vomit" },
                    { "Poop", "Poop" },
                    { "StudentPooped", "Poop" },
                    { "PhysicalInteraction", "Hit" },
                    { "StudentHit", "Hit" },
                    { "ThrowingObject", "ThrowObject" },
                    { "StudentThrewObject", "ThrowObject" },
                    { "MakingNoise", "MakeNoise" },
                    { "StudentMadeNoise", "MakeNoise" },
                    { "KnockedOverObject", "Push" },
                    { "Distraction", "Distract" },
                    { "WanderingAround", "Distract" }
                },
                complaintMapping = new Dictionary<string, string>
                {
                    { "Vomit", "MessCreated" },
                    { "StudentVomited", "MessCreated" },
                    { "Hit", "PhysicalInteraction" },
                    { "ThrowObject", "ThrowingObject" },
                    { "MakeNoise", "MakingNoise" },
                    { "Push", "PhysicalInteraction" },
                    { "Distract", "Distraction" }
                }
            };
        }

        private void CreateAllDefaults()
        {
            CreateDefaultPopupText();
            CreateDefaultComplaintTemplates();
            CreateDefaultSourceStatements();
            CreateDefaultButtonLabels();
            CreateDefaultEventTypeMapping();
            isLoaded = true;
        }

        public string GetTargetOpeningPhrase() => popupText?.targetStudent?.openingPhrase ?? "Cô ơi!";
        public string GetTargetNoComplaints() => popupText?.targetStudent?.noComplaints ?? "Em ổn rồi cô!";
        public string GetTargetEscortButton(bool enabled) => enabled 
            ? (popupText?.targetStudent?.escortButtonEnabled ?? "🏠 Escort Back")
            : (popupText?.targetStudent?.escortButtonDisabled ?? "🏠 Escort Back");
        public string GetTargetCloseButton() => popupText?.targetStudent?.closeButton ?? "❌ Close";

        public string GetSourceImpactWholeClass(int count)
        {
            string template = popupText?.sourceStudent?.impactWholeClass ?? "⚠️ Đang ảnh hưởng cả lớp ({count} học sinh)";
            return template.Replace("{count}", count.ToString());
        }

        public string GetSourceImpactIndividual() => popupText?.sourceStudent?.impactIndividual ?? "⚠️ Đang ảnh hưởng:";
        
        public string GetSourceResolveWholeClassButton() => popupText?.sourceStudent?.resolveWholeClassButton ?? "✅ Giải quyết cho cả lớp";
        
        public string GetSourceResolveIndividualButton(string studentName)
        {
            string template = popupText?.sourceStudent?.resolveIndividualButton ?? "✅ Giải quyết cho {studentName}";
            return template.Replace("{studentName}", studentName);
        }

        public string GetSourceCloseButton() => popupText?.sourceStudent?.closeButton ?? "❌ Close";

        public string GetStateEmoji(string state)
        {
            if (popupText?.stateEmojis != null && popupText.stateEmojis.ContainsKey(state))
                return popupText.stateEmojis[state];
            return "😐";
        }

        public ComplaintTemplate GetComplaintTemplate(string eventType)
        {
            string mappedEventType = MapToComplaintKey(eventType);
            if (complaintTemplates?.complaints != null && complaintTemplates.complaints.ContainsKey(mappedEventType))
                return complaintTemplates.complaints[mappedEventType];
            return new ComplaintTemplate { template = "Bạn {source} làm gì đó!", icon = "❓" };
        }

        public string GetComplaint(string eventType, string sourceName)
        {
            string mappedEventType = MapToComplaintKey(eventType);
            ComplaintTemplate template = GetComplaintTemplate(mappedEventType);
            return template.template.Replace("{source}", sourceName);
        }

        public string MapToSourceStatementKey(string eventType)
        {
            if (eventTypeMapping?.sourceStatementMapping != null 
                && eventTypeMapping.sourceStatementMapping.ContainsKey(eventType))
            {
                return eventTypeMapping.sourceStatementMapping[eventType];
            }
            return eventType; // fallback to original key
        }

        public string MapToComplaintKey(string eventType)
        {
            if (eventTypeMapping?.complaintMapping != null 
                && eventTypeMapping.complaintMapping.ContainsKey(eventType))
            {
                return eventTypeMapping.complaintMapping[eventType];
            }
            return eventType; // fallback to original key
        }

        public string GetSourceStatement(string eventType, string targets = "")
        {
            string mappedKey = MapToSourceStatementKey(eventType);
            
            if (sourceStatements?.statements != null && sourceStatements.statements.ContainsKey(mappedKey))
            {
                List<string> templates = sourceStatements.statements[mappedKey];
                if (templates.Count > 0)
                {
                    string template = templates[UnityEngine.Random.Range(0, templates.Count)];
                    return template.Replace("{targets}", targets);
                }
            }
            return "Em xin lỗi cô...";
        }

        public string GetButtonLabel(string actionKey)
        {
            if (buttonLabels?.actions != null && buttonLabels.actions.ContainsKey(actionKey))
                return buttonLabels.actions[actionKey];
            return actionKey;
        }

        public string GetTooltip(string tooltipKey)
        {
            if (buttonLabels?.tooltips != null && buttonLabels.tooltips.ContainsKey(tooltipKey))
                return buttonLabels.tooltips[tooltipKey];
            return "";
        }

        public bool IsLoaded => isLoaded;
    }
}
