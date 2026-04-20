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
        public string openingPhrase;     // fallback (backward compat)
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

    /// <summary>
    /// Expanded complaint template with per-role dialogue pools.
    /// Keeps 'template' field for backward compatibility.
    /// {source} = tên người gây, {targets} = tên người bị
    /// </summary>
    [Serializable]
    public class ComplaintTemplate
    {
        public string template;                    // backward compat (single string)
        public string icon;
        public List<string> sourceStatements;      // câu nói của người gây
        public List<string> directComplaints;      // than phiền của người bị trực tiếp (SingleStudent scope)
        public List<string> indirectComplaints;    // than phiền của người bị gián tiếp (WholeClass scope)
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

        // No-repeat random: last index used per (key + pool type)
        private readonly Dictionary<string, int> lastUsedIndex = new Dictionary<string, int>();

        // ----- State name mappings -----
        private static readonly Dictionary<string, string> stateNamesVN = new Dictionary<string, string>
        {
            { "Calm",       "Bình tĩnh" },
            { "Distracted", "Mất tập trung" },
            { "ActingOut",  "Đang gây rối" },
            { "Critical",   "Mất kiểm soát" }
        };

        // Opening phrases per state (legacy fallback)
        private static readonly Dictionary<string, List<string>> openingPhrasesByState =
            new Dictionary<string, List<string>>
        {
            { "Calm",       new List<string> { "Em ổn rồi cô!", "Dạ cô?", "Em đây cô!" } },
            { "Distracted", new List<string> { "Dạ cô?", "Cô gọi em ạ?", "Em đang nghe cô!" } },
            { "ActingOut",  new List<string> { "Cô ơi!", "Em... em xin lỗi cô!", "Không phải lỗi em cô ơi!" } },
            { "Critical",   new List<string> { "Cô ơi con sợ!", "Huhu cô ơi!", "Em không chịu nổi nữa cô!" } }
        };

        // Opening phrases by (State_Context) — context: Default, AfterCalmed, SelfCaused, Influenced
        private static readonly Dictionary<string, List<string>> openingPhrasesByContext =
            new Dictionary<string, List<string>>
        {
            { "Calm_Default",          new List<string> { "Dạ cô?", "Em đây cô!", "Cô cần gì ạ?", "Dạ, em nghe cô!", "Có gì cô ơi?" } },
            { "Calm_AfterCalmed",      new List<string> { "Em cảm ơn cô...", "Em sẽ chú ý hơn ạ", "Vâng ạ, em hiểu rồi", "Em không làm vậy nữa đâu cô", "Em ngoan hơn rồi ạ" } },
            { "Distracted_Default",    new List<string> { "Dạ cô?", "Em đây cô!", "Cô gọi em ạ?" } },
            { "Distracted_SelfCaused", new List<string> { "Em không cố ý đâu cô!", "Em lỡ tay thôi cô ơi...", "Em xin lỗi cô, tại em mất tập trung", "Em không để ý cô ơi...", "Thôi chết, em làm sao vậy cô?" } },
            { "Distracted_Influenced", new List<string> { "Tại bạn ấy làm em phân tâm cô!", "Em đang cố học mà cô ơi!", "Em không làm gì cả cô!", "Không phải tại em đâu cô ạ!", "Em cố lắm mà không được cô ơi..." } },
            { "ActingOut_Default",     new List<string> { "Cô ơi!", "Dạ cô?", "Cô gọi em ạ?" } },
            { "ActingOut_SelfCaused",  new List<string> { "Có gì đâu cô!", "Em đùa thôi mà cô!", "Ủa, sao cô gọi em?", "Em làm gì sai vậy cô?", "Hì hì... em chỉ vui thôi mà!" } },
            { "ActingOut_Influenced",  new List<string> { "Bạn ấy rủ em đấy chứ!", "Em chỉ làm theo thôi mà!", "Tại bạn đó khiêu khích em cô!", "Bạn ấy bắt đầu trước cô ơi!", "Em chả muốn làm vậy đâu, bạn ấy trước!" } },
            { "Critical_Default",      new List<string> { "...", "Hức hức...", "Em không biết nữa...", "Huhuhu cô ơi...", "Em sợ lắm cô ơi..." } },
            { "Critical_SelfCaused",   new List<string> { "Em không kiểm soát được cô ơi...", "Em không biết sao em làm vậy...", "Huhu, em xin lỗi cô...", "Em sai rồi cô ơi..." } },
            { "Critical_Influenced",   new List<string> { "Em sợ lắm cô!", "Huhu, em không chịu được nữa cô...", "Cô giúp em với!", "Em không biết phải làm sao nữa cô ơi..." } },
        };

        // Body messages when target student has NO complaints (Calm + no influence sources)
        // Used as the white-text body line below the yellow opening phrase.
        private static readonly Dictionary<string, List<string>> noComplaintsByContext =
            new Dictionary<string, List<string>>
        {
            { "Default",     new List<string> {
                "Em đang chú ý nghe cô giảng ạ.",
                "Em không có vấn đề gì cô ơi.",
                "Em đang làm bài tập cô giao.",
                "Em đang ngoan, không quậy gì cả.",
                "Em ổn cả cô ạ, cảm ơn cô.",
                "Em đang ngồi yên học bài.",
            } },
            { "AfterCalmed", new List<string> {
                "Em đã hết phá rồi cô ạ.",
                "Em hứa sẽ ngoan hơn cô ơi.",
                "Em rút kinh nghiệm rồi cô.",
                "Em sẽ tập trung học hơn ạ.",
                "Em xin lỗi cô lần nữa, em sẽ chú ý.",
            } },
        };

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

                string popupTextPath          = Path.Combine(configPath, "PopupText.json");
                string complaintTemplatesPath = Path.Combine(configPath, "ComplaintTemplates.json");
                string sourceStatementsPath   = Path.Combine(configPath, "SourceStatements.json");
                string buttonLabelsPath       = Path.Combine(configPath, "ButtonLabels.json");
                string eventTypeMappingPath   = Path.Combine(configPath, "EventTypeMapping.json");

                // NOTE: Unity JsonUtility cannot deserialize Dictionary<,> fields — they remain null.
                // So we check the inner dict, not just the wrapper instance, before falling back to defaults.

                popupText = File.Exists(popupTextPath)
                    ? JsonUtility.FromJson<PopupTextData>(File.ReadAllText(popupTextPath))
                    : null;
                if (popupText == null) CreateDefaultPopupText();

                complaintTemplates = File.Exists(complaintTemplatesPath)
                    ? JsonUtility.FromJson<ComplaintTemplatesData>(File.ReadAllText(complaintTemplatesPath))
                    : null;
                if (complaintTemplates?.complaints == null) CreateDefaultComplaintTemplates();

                sourceStatements = File.Exists(sourceStatementsPath)
                    ? JsonUtility.FromJson<SourceStatementsData>(File.ReadAllText(sourceStatementsPath))
                    : null;
                if (sourceStatements?.statements == null) CreateDefaultSourceStatements();

                buttonLabels = File.Exists(buttonLabelsPath)
                    ? JsonUtility.FromJson<ButtonLabelsData>(File.ReadAllText(buttonLabelsPath))
                    : null;
                if (buttonLabels?.actions == null) CreateDefaultButtonLabels();

                eventTypeMapping = File.Exists(eventTypeMappingPath)
                    ? JsonUtility.FromJson<EventTypeMappingData>(File.ReadAllText(eventTypeMappingPath))
                    : null;
                if (eventTypeMapping?.sourceStatementMapping == null) CreateDefaultEventTypeMapping();

                isLoaded = true;
                Debug.Log("[PopupTextLoader] All configs loaded");
            }
            catch (Exception e)
            {
                Debug.LogError($"[PopupTextLoader] Error loading configs: {e.Message}");
                CreateAllDefaults();
            }
        }

        // ------------------------------------------------------------------ //
        //  Default content builders                                           //
        // ------------------------------------------------------------------ //

        private void CreateDefaultPopupText()
        {
            popupText = new PopupTextData
            {
                targetStudent = new TargetStudentText
                {
                    openingPhrase          = "Cô ơi!",
                    noComplaints           = "Em ổn rồi cô!",
                    escortButtonEnabled    = "🏠 Đưa về chỗ",
                    escortButtonDisabled   = "🔒 Đưa về chỗ (cần giải quyết trước)",
                    closeButton            = "❌ Đóng"
                },
                sourceStudent = new SourceStudentText
                {
                    impactWholeClass          = "⚠️ Đang ảnh hưởng cả lớp",
                    impactIndividual          = "⚠️ Đang ảnh hưởng {count} học sinh:",
                    resolveWholeClassButton   = "✅ Giải quyết cho cả lớp",
                    resolveIndividualButton   = "✅ Giải quyết cho {studentName}",
                    closeButton               = "❌ Đóng"
                },
                stateEmojis = new Dictionary<string, string>
                {
                    { "Calm",       "😌" },
                    { "Distracted", "😕" },
                    { "ActingOut",  "😠" },
                    { "Critical",   "😰" }
                }
            };
        }

        private void CreateDefaultComplaintTemplates()
        {
            complaintTemplates = new ComplaintTemplatesData
            {
                complaints = new Dictionary<string, ComplaintTemplate>
                {
                    {
                        "KnockedOverObject", new ComplaintTemplate
                        {
                            icon = "💥",
                            template = "Bạn {source} làm rơi {object} qua chỗ con!",
                            sourceStatements = new List<string>
                            {
                                "Em lỡ tay cô ơi!",
                                "Tại nó nặng quá em không giữ được!",
                                "Em đang cầm thì nó rớt cô!",
                                "Em không cố ý đâu cô!"
                            },
                            directComplaints = new List<string>
                            {
                                "Bạn {source} làm rơi {object} ngay cạnh con cô ơi!",
                                "{object} của bạn {source} rớt cái ầm sát chỗ con!",
                                "Bạn {source} đẩy {object} bay qua chỗ con!",
                                "Cô ơi, {object} từ chỗ bạn {source} rơi xuống bàn con luôn!",
                                "Bạn {source} làm rớt {object}, đập trúng bàn con!"
                            },
                            indirectComplaints = new List<string>
                            {
                                "Bạn {source} làm rớt {object} cái ầm, em giật mình quá cô!",
                                "Tiếng {object} của bạn {source} rớt làm em không tập trung được!",
                                "Bạn {source} làm rơi {object} ồn quá, em sợ luôn!",
                                "Em đang học thì bạn {source} làm rớt {object} cô ơi!"
                            }
                        }
                    },
                    {
                        "ThrowingObject", new ComplaintTemplate
                        {
                            icon = "🎯",
                            template = "Bạn {source} ném {object} vào con!",
                            sourceStatements = new List<string>
                            {
                                "Em chuyền {object} cho bạn thôi mà!",
                                "Em chỉ ném chơi thôi cô!",
                                "Tại bạn {targets} xin {object} em!",
                                "Em thử xem ném có trúng không!"
                            },
                            directComplaints = new List<string>
                            {
                                "Bạn {source} ném {object} trúng con cô ơi!",
                                "Đau lắm cô, {object} của bạn {source} bay trúng đầu con!",
                                "Con bị bạn {source} ném {object} vô đầu!",
                                "Bạn {source} cứ ném {object} vô con hoài!",
                                "{object} bay qua đầu con rồi, bạn {source} ném đó cô!"
                            },
                            indirectComplaints = new List<string>
                            {
                                "Bạn {source} ném {object} lung tung, em sợ trúng lắm cô!",
                                "Cô ơi bạn {source} ném {object} bay qua đầu em!",
                                "Em ngồi gần bạn {source}, bạn ấy ném {object} hoài em sợ lắm!",
                                "Bạn {source} nguy hiểm quá, ném {object} lung tung cô ơi!",
                                "{object} của bạn {source} gần bay vô mặt em rồi cô!"
                            }
                        }
                    },
                    {
                        "MakingNoise", new ComplaintTemplate
                        {
                            icon = "🔊",
                            template = "Bạn {source} làm ồn, con không học được!",
                            sourceStatements = new List<string>
                            {
                                "Em đang hát cô ơi!",
                                "Em kể chuyện hè cho bạn nghe!",
                                "Tụi con đang bàn bài cô!",
                                "Em vui quá nên la lớn chút thôi!"
                            },
                            directComplaints = new List<string>
                            {
                                "Bạn {source} ồn quá con không nghe được!",
                                "Con không tập trung được vì bạn {source}!",
                                "Bạn {source} nói hoài không ngừng!"
                            },
                            indirectComplaints = new List<string>
                            {
                                "Bạn {source} ồn quá em không nghe cô giảng được!",
                                "Bạn {source} cứ nói chuyện, em không học được cô ơi!",
                                "Em muốn tập trung lắm nhưng bạn {source} cứ ồn hoài!",
                                "Tai em ù hết rồi cô, tại bạn {source} to tiếng quá!"
                            }
                        }
                    },
                    {
                        "WanderingAround", new ComplaintTemplate
                        {
                            icon = "🚶",
                            template = "Bạn {source} đi qua đi lại làm con mất tập trung!",
                            sourceStatements = new List<string>
                            {
                                "Em đi lấy bút cô!",
                                "Em muốn ngồi chỗ khác!",
                                "Em ngồi lâu mỏi quá cô!",
                                "Em đi thăm bạn chút thôi!"
                            },
                            directComplaints = new List<string>
                            {
                                "Bạn {source} đi qua chỗ con hoài!",
                                "Bạn {source} đụng bàn con!"
                            },
                            indirectComplaints = new List<string>
                            {
                                "Bạn {source} đi qua đi lại làm em mất tập trung cô ơi!",
                                "Cô ơi bạn {source} đi lung tung không chịu ngồi yên!",
                                "Bạn {source} đụng bàn em rồi đi luôn không xin lỗi!",
                                "Em không học được vì bạn {source} cứ đi lòng vòng!"
                            }
                        }
                    },
                    {
                        "MessCreated", new ComplaintTemplate
                        {
                            icon = "😷",
                            template = "Bạn {source} ói, thúi quá cô!",
                            sourceStatements = new List<string>
                            {
                                "Em ói rồi cô ơi...",
                                "Em không kìm được cô...",
                                "Em bị ốm cô ơi..."
                            },
                            directComplaints = new List<string>
                            {
                                "Bạn {source} ói, thúi quá cô!",
                                "Con ngồi gần bạn {source}, hôi lắm!"
                            },
                            indirectComplaints = new List<string>
                            {
                                "Cô ơi bạn {source} có mùi gì hôi lắm!",
                                "Em ngồi gần bạn {source}, hôi quá em chịu không được!",
                                "Bạn {source} bẩn quá cô, em buồn nôn luôn!",
                                "Cô giải quyết bạn {source} đi cô, em ngồi không được rồi!"
                            }
                        }
                    },
                    {
                        "PhysicalInteraction", new ComplaintTemplate
                        {
                            icon = "😢",
                            template = "Bạn {source} đánh con, đau lắm!",
                            sourceStatements = new List<string>
                            {
                                "Em tức quá cô ơi!",
                                "Tại bạn {targets} chọc em trước!"
                            },
                            directComplaints = new List<string>
                            {
                                "Bạn {source} đánh con, đau lắm!",
                                "Con bị bạn {source} đánh cô ơi!"
                            },
                            indirectComplaints = new List<string>
                            {
                                "Bạn {source} chọc em hoài cô ơi!",
                                "Bạn {source} đụng em mấy lần rồi mà không xin lỗi!",
                                "Em đang ngồi yên thì bạn {source} đánh em cô!",
                                "Bạn {source} hung quá cô, em sợ lắm!"
                            }
                        }
                    },
                    {
                        "Distraction", new ComplaintTemplate
                        {
                            icon = "😵",
                            template = "Bạn {source} làm con mất tập trung!",
                            sourceStatements = new List<string>
                            {
                                "Em không có làm gì cô ơi!",
                                "Tại bạn {targets} nhìn em hoài!"
                            },
                            directComplaints = new List<string>
                            {
                                "Bạn {source} cứ nhìn con hoài!",
                                "Bạn {source} làm con không tập trung được!"
                            },
                            indirectComplaints = new List<string>
                            {
                                "Bạn {source} cứ nghịch làm em không học được cô!",
                                "Em nhìn bạn {source} hoài không nhìn bảng được!",
                                "Bạn {source} làm trò kỳ lắm, em không bỏ qua được!",
                                "Tại bạn {source} cứ phá, em không tập trung được cô ơi!"
                            }
                        }
                    },
                    {
                        "Poop", new ComplaintTemplate
                        {
                            icon = "💩",
                            template = "Bạn {source} ỉa, thúi lắm cô!",
                            sourceStatements = new List<string>
                            {
                                "Em không kìm được cô ơi...",
                                "Em đau bụng quá cô...",
                                "Em xin lỗi cô..."
                            },
                            directComplaints = new List<string>
                            {
                                "Bạn {source} ỉa, thúi lắm cô!",
                                "Con không chịu được mùi này cô ơi!"
                            },
                            indirectComplaints = new List<string>
                            {
                                "Cô ơi bạn {source} có mùi gì kỳ lắm!",
                                "Em ngồi gần bạn {source} không chịu được cô ơi!",
                                "Bạn {source} làm gì mà mùi ghê quá cô!",
                                "Em muốn ra ngoài cô ơi, hôi quá vì bạn {source}!"
                            }
                        }
                    }
                }
            };
        }

        private void CreateDefaultSourceStatements()
        {
            sourceStatements = new SourceStatementsData
            {
                statements = new Dictionary<string, List<string>>
                {
                    { "Vomit",       new List<string> { "Em ói rồi cô ơi...", "Em không kìm được cô...", "Em bị ốm cô ơi..." } },
                    { "Poop",        new List<string> { "Em không kìm được cô ơi...", "Em đau bụng quá cô...", "Em xin lỗi cô..." } },
                    { "Hit",         new List<string> { "Em tức quá cô ơi, nên em đánh bạn {targets}...", "Bạn ấy chọc em trước cô, nên em đánh bạn {targets}!" } },
                    { "ThrowObject", new List<string> { "Em chuyền đồ cho bạn thôi mà!", "Em chỉ ném chơi thôi cô!", "Tại bạn {targets} xin đồ em!" } },
                    { "MakeNoise",   new List<string> { "Em đang hát cô ơi!", "Em kể chuyện hè cho bạn nghe!", "Tụi con đang bàn bài cô!" } },
                    { "Push",        new List<string> { "Em lỡ tay cô ơi!", "Em không cố ý đâu cô!", "Em đang dọn bàn mà nó rớt cô!" } },
                    { "Distract",    new List<string> { "Em đi lấy bút cô!", "Em ngồi lâu mỏi quá cô!", "Em đi thăm bạn chút thôi!" } }
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
                    { "escortBack",        "🏠 Đưa về chỗ" },
                    { "close",             "❌ Đóng" }
                },
                tooltips = new Dictionary<string, string>
                {
                    { "escortDisabled", "Cần giải quyết các nguồn gốc trước" },
                    { "escortEnabled",  "Đưa học sinh về chỗ ngồi" }
                }
            };
        }

        private void CreateDefaultEventTypeMapping()
        {
            eventTypeMapping = new EventTypeMappingData
            {
                sourceStatementMapping = new Dictionary<string, string>
                {
                    { "MessCreated",          "Vomit" },
                    { "StudentVomited",       "Vomit" },
                    { "Poop",                 "Poop" },
                    { "StudentPooped",        "Poop" },
                    { "PhysicalInteraction",  "Hit" },
                    { "StudentHit",           "Hit" },
                    { "ThrowingObject",       "ThrowObject" },
                    { "StudentThrewObject",   "ThrowObject" },
                    { "MakingNoise",          "MakeNoise" },
                    { "StudentMadeNoise",     "MakeNoise" },
                    { "KnockedOverObject",    "Push" },
                    { "Distraction",          "Distract" },
                    { "WanderingAround",      "Distract" }
                },
                complaintMapping = new Dictionary<string, string>
                {
                    { "Vomit",       "MessCreated" },
                    { "StudentVomited", "MessCreated" },
                    { "Hit",         "PhysicalInteraction" },
                    { "ThrowObject", "ThrowingObject" },
                    { "MakeNoise",   "MakingNoise" },
                    { "Push",        "KnockedOverObject" },
                    { "Distract",    "Distraction" }
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

        // ------------------------------------------------------------------ //
        //  Public API                                                          //
        // ------------------------------------------------------------------ //

        // ----- State names -----

        public string GetStateNameVietnamese(string state)
        {
            return stateNamesVN.TryGetValue(state, out string vn) ? vn : state;
        }

        // ----- Opening phrases -----

        /// Returns a context-aware opening phrase: key = "{state}_{context}" e.g. "Distracted_Influenced".
        /// Context values: Default, AfterCalmed, SelfCaused, Influenced.
        public string GetOpeningPhrase(string state, string context = "Default")
        {
            string key = state + "_" + context;
            if (openingPhrasesByContext.TryGetValue(key, out var pool) && pool.Count > 0)
                return PickNoRepeat(pool, "opening_ctx_" + key);

            string fallbackKey = state + "_Default";
            if (openingPhrasesByContext.TryGetValue(fallbackKey, out var fallback) && fallback.Count > 0)
                return PickNoRepeat(fallback, "opening_ctx_" + fallbackKey);

            return GetTargetOpeningPhrase(state);
        }

        /// Returns a random (non-repeating) opening phrase for the given student state.
        public string GetTargetOpeningPhrase(string state = null)
        {
            if (!string.IsNullOrEmpty(state) && openingPhrasesByState.TryGetValue(state, out var pool) && pool.Count > 0)
                return PickNoRepeat(pool, "opening_" + state);

            return popupText?.targetStudent?.openingPhrase ?? "Cô ơi!";
        }

        // ----- Complaint pools -----

        /// Direct complaint (SingleStudent scope) - person hit directly by the action.
        public string GetDirectComplaint(string eventType, string sourceName, string objectName = null)
        {
            var template = GetComplaintTemplateByEventType(eventType);
            if (template?.directComplaints != null && template.directComplaints.Count > 0)
            {
                string raw = PickNoRepeat(template.directComplaints, "direct_" + eventType);
                return ApplyPlaceholders(raw, sourceName, objectName);
            }
            return GetComplaint(eventType, sourceName);
        }

        /// Indirect complaint (WholeClass scope) - bystanders disturbed by the action.
        public string GetIndirectComplaint(string eventType, string sourceName, string objectName = null)
        {
            var template = GetComplaintTemplateByEventType(eventType);
            if (template?.indirectComplaints != null && template.indirectComplaints.Count > 0)
            {
                string raw = PickNoRepeat(template.indirectComplaints, "indirect_" + eventType);
                return ApplyPlaceholders(raw, sourceName, objectName);
            }
            return GetComplaint(eventType, sourceName);
        }

        private string ApplyPlaceholders(string raw, string sourceName, string objectName)
        {
            string result = raw.Replace("{source}", sourceName ?? "bạn ấy");
            result = result.Replace("{object}", string.IsNullOrEmpty(objectName) ? "đồ" : objectName);
            return result;
        }

        // ----- Source statements -----

        public string GetSourceStatement(string eventType, string targets = "")
        {
            // Try new pool in complaintTemplates first
            var template = GetComplaintTemplateByEventType(eventType);
            if (template?.sourceStatements != null && template.sourceStatements.Count > 0)
            {
                string raw = PickNoRepeat(template.sourceStatements, "src_" + eventType);
                return raw.Replace("{targets}", targets);
            }
            // Fallback to legacy sourceStatements dict
            string mappedKey = MapToSourceStatementKey(eventType);
            if (sourceStatements?.statements != null && sourceStatements.statements.TryGetValue(mappedKey, out var list) && list.Count > 0)
            {
                string raw = PickNoRepeat(list, "src_legacy_" + mappedKey);
                return raw.Replace("{targets}", targets);
            }
            return "Em xin lỗi cô...";
        }

        // ----- Existing API (kept for backward compat) -----

        /// Body message when target has no complaints. context: "Default" or "AfterCalmed".
        public string GetTargetNoComplaints(string context = "Default")
        {
            if (noComplaintsByContext.TryGetValue(context, out var pool) && pool.Count > 0)
                return PickNoRepeat(pool, "noComplaints_" + context);
            if (noComplaintsByContext.TryGetValue("Default", out var def) && def.Count > 0)
                return PickNoRepeat(def, "noComplaints_Default");
            return popupText?.targetStudent?.noComplaints ?? "Em ổn rồi cô!";
        }

        public string GetTargetCloseButton()   => popupText?.targetStudent?.closeButton   ?? "❌ Đóng";

        public string GetTargetEscortButton(bool enabled) => enabled
            ? (popupText?.targetStudent?.escortButtonEnabled  ?? "🏠 Đưa về chỗ")
            : (popupText?.targetStudent?.escortButtonDisabled ?? "🔒 Đưa về chỗ (cần giải quyết trước)");

        public string GetSourceImpactWholeClass(int count)
        {
            // Whole class: text only, no count (count would contradict "cả lớp")
            string t = popupText?.sourceStudent?.impactWholeClass ?? "⚠️ Đang ảnh hưởng cả lớp";
            return t.Replace("{count}", count.ToString());  // safe no-op if template has no {count}
        }

        public string GetSourceImpactIndividual(int count = 0)
        {
            string t = popupText?.sourceStudent?.impactIndividual ?? "⚠️ Đang ảnh hưởng {count} học sinh:";
            return t.Replace("{count}", count.ToString());
        }

        public string GetSourceResolveWholeClassButton() =>
            popupText?.sourceStudent?.resolveWholeClassButton ?? "✅ Giải quyết cho cả lớp";

        public string GetSourceResolveIndividualButton(string studentName)
        {
            string t = popupText?.sourceStudent?.resolveIndividualButton ?? "✅ Giải quyết cho {studentName}";
            return t.Replace("{studentName}", studentName);
        }

        public string GetSourceCloseButton() => popupText?.sourceStudent?.closeButton ?? "❌ Đóng";

        public string GetStateEmoji(string state)
        {
            if (popupText?.stateEmojis != null && popupText.stateEmojis.TryGetValue(state, out string emoji))
                return emoji;
            return "😐";
        }

        public ComplaintTemplate GetComplaintTemplate(string eventType) =>
            GetComplaintTemplateByEventType(eventType)
            ?? new ComplaintTemplate { template = "Bạn {source} làm gì đó!", icon = "❓" };

        public string GetComplaint(string eventType, string sourceName)
        {
            var template = GetComplaintTemplate(eventType);
            string t = template.template ?? "Bạn {source} làm gì đó!";
            return t.Replace("{source}", sourceName);
        }

        public string MapToSourceStatementKey(string eventType)
        {
            if (eventTypeMapping?.sourceStatementMapping != null
                && eventTypeMapping.sourceStatementMapping.TryGetValue(eventType, out string mapped))
                return mapped;
            return eventType;
        }

        public string MapToComplaintKey(string eventType)
        {
            if (eventTypeMapping?.complaintMapping != null
                && eventTypeMapping.complaintMapping.TryGetValue(eventType, out string mapped))
                return mapped;
            return eventType;
        }

        public string GetButtonLabel(string actionKey)
        {
            if (buttonLabels?.actions != null && buttonLabels.actions.TryGetValue(actionKey, out string label))
                return label;
            return actionKey;
        }

        public string GetTooltip(string tooltipKey)
        {
            if (buttonLabels?.tooltips != null && buttonLabels.tooltips.TryGetValue(tooltipKey, out string tip))
                return tip;
            return "";
        }

        public bool IsLoaded => isLoaded;

        // ------------------------------------------------------------------ //
        //  Private helpers                                                     //
        // ------------------------------------------------------------------ //

        private ComplaintTemplate GetComplaintTemplateByEventType(string eventType)
        {
            if (complaintTemplates?.complaints == null) return null;

            // Direct key match first
            if (complaintTemplates.complaints.TryGetValue(eventType, out var t)) return t;

            // Try complaint mapping
            string mapped = MapToComplaintKey(eventType);
            if (complaintTemplates.complaints.TryGetValue(mapped, out var t2)) return t2;

            return null;
        }

        /// Pick a random item from pool, avoiding the last used index (no-repeat).
        private string PickNoRepeat(List<string> pool, string key)
        {
            if (pool == null || pool.Count == 0) return "";
            if (pool.Count == 1) return pool[0];

            lastUsedIndex.TryGetValue(key, out int last);
            int idx = UnityEngine.Random.Range(0, pool.Count - 1);
            if (idx >= last) idx++; // shift to avoid repeating
            lastUsedIndex[key] = idx;
            return pool[idx];
        }
    }
}
