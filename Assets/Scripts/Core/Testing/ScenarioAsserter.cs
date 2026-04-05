using System.Collections.Generic;

namespace FunClass.Core.Testing
{
    /// <summary>
    /// One expected assertion from the scenario JSON file.
    /// </summary>
    [System.Serializable]
    public class ExpectedAssertion
    {
        public string id;
        public string component;
        public string eventType;
        public string source;
        public string target;
        public float minTime;
        public float maxTime;
        public bool mustOccur;
        public string afterEventId;
        public string messageContains;
        public string description;
    }

    public enum AssertionStatus
    {
        Pass,
        FailNotFound,
        FailWrongTiming,
        FailWrongOrder
    }

    public class AssertionResult
    {
        public string id;
        public AssertionStatus status;
        public string reason;
        public float actualTime = -1f;
        public bool skipped; // mustOccur=false and not found → not a failure
    }

    public class ValidationResult
    {
        public bool allPassed;
        public List<AssertionResult> results = new List<AssertionResult>();
    }

    /// <summary>
    /// Pure logic class. Validates captured events against expected assertions.
    /// No MonoBehaviour, no Unity dependencies beyond CapturedEvent struct.
    /// </summary>
    public static class ScenarioAsserter
    {
        public static ValidationResult Validate(List<CapturedEvent> capturedEvents, List<ExpectedAssertion> assertions)
        {
            var result = new ValidationResult();
            bool allPassed = true;

            // Track the captured-event index of each validated assertion for ordering checks.
            var matchedEventIndices = new Dictionary<string, int>();

            foreach (var assertion in assertions)
            {
                var ar = new AssertionResult { id = assertion.id };

                // --- Step 1: Collect candidate indices ---
                var candidates = new List<int>();
                for (int i = 0; i < capturedEvents.Count; i++)
                {
                    var ev = capturedEvents[i];

                    if (!string.IsNullOrEmpty(assertion.component) && ev.component != assertion.component)
                        continue;

                    bool usedStructured = false;

                    if (!string.IsNullOrEmpty(assertion.eventType))
                    {
                        if (ev.eventType != assertion.eventType) continue;
                        usedStructured = true;
                    }

                    if (!string.IsNullOrEmpty(assertion.source))
                    {
                        if (ev.source != assertion.source) continue;
                        usedStructured = true;
                    }

                    if (!string.IsNullOrEmpty(assertion.target))
                    {
                        if (ev.target != assertion.target) continue;
                        usedStructured = true;
                    }

                    // Fallback to message substring when no structured fields were specified.
                    if (!usedStructured && !string.IsNullOrEmpty(assertion.messageContains))
                    {
                        if (!ev.rawMessage.Contains(assertion.messageContains)) continue;
                    }

                    candidates.Add(i);
                }

                // --- Step 2: Not found ---
                if (candidates.Count == 0)
                {
                    if (assertion.mustOccur)
                    {
                        ar.status = AssertionStatus.FailNotFound;
                        ar.reason = "No matching event found";
                        allPassed = false;
                    }
                    else
                    {
                        ar.status = AssertionStatus.Pass;
                        ar.skipped = true;
                        ar.reason = "Not found (mustOccur=false, OK)";
                    }
                    result.results.Add(ar);
                    continue;
                }

                // --- Step 3: Time range check — pick first candidate in range ---
                int matchedIdx = -1;
                float matchedTime = -1f;
                foreach (int idx in candidates)
                {
                    float t = capturedEvents[idx].elapsed;
                    if (t >= assertion.minTime && t <= assertion.maxTime)
                    {
                        matchedIdx = idx;
                        matchedTime = t;
                        break;
                    }
                }

                if (matchedIdx == -1)
                {
                    float closest = capturedEvents[candidates[0]].elapsed;
                    if (assertion.mustOccur)
                    {
                        ar.status = AssertionStatus.FailWrongTiming;
                        ar.reason = $"Event found at {closest:F1}s, expected {assertion.minTime}-{assertion.maxTime}s";
                        ar.actualTime = closest;
                        allPassed = false;
                    }
                    else
                    {
                        ar.status = AssertionStatus.Pass;
                        ar.skipped = true;
                        ar.reason = $"Event at {closest:F1}s outside range (mustOccur=false, OK)";
                    }
                    result.results.Add(ar);
                    continue;
                }

                // --- Step 4: Ordering check ---
                if (!string.IsNullOrEmpty(assertion.afterEventId))
                {
                    if (!matchedEventIndices.TryGetValue(assertion.afterEventId, out int afterIdx))
                    {
                        if (assertion.mustOccur)
                        {
                            ar.status = AssertionStatus.FailWrongOrder;
                            ar.reason = $"afterEventId '{assertion.afterEventId}' was not matched — cannot verify order";
                            allPassed = false;
                        }
                        else
                        {
                            ar.status = AssertionStatus.Pass;
                            ar.skipped = true;
                            ar.reason = "Order check skipped (mustOccur=false)";
                        }
                        result.results.Add(ar);
                        continue;
                    }

                    if (matchedIdx <= afterIdx)
                    {
                        if (assertion.mustOccur)
                        {
                            ar.status = AssertionStatus.FailWrongOrder;
                            ar.reason = $"Event at index {matchedIdx} occurred before '{assertion.afterEventId}' at index {afterIdx}";
                            ar.actualTime = matchedTime;
                            allPassed = false;
                        }
                        else
                        {
                            ar.status = AssertionStatus.Pass;
                            ar.skipped = true;
                            ar.reason = "Wrong order (mustOccur=false, OK)";
                        }
                        result.results.Add(ar);
                        continue;
                    }
                }

                // --- All checks passed ---
                ar.status = AssertionStatus.Pass;
                ar.actualTime = matchedTime;
                ar.reason = $"Event found at {matchedTime:F1}s";
                matchedEventIndices[assertion.id] = matchedIdx;
                result.results.Add(ar);
            }

            result.allPassed = allPassed;
            return result;
        }
    }
}
