using UnityEngine;
using System.Collections;

namespace FunClass.Core
{
    /// <summary>
    /// Phase 3: Floating TextMesh icon above student head that shows current state.
    /// Positioned above InfluenceStatusIcon. Billboard so it always faces camera.
    /// </summary>
    public class StudentStateIndicator : MonoBehaviour
    {
        // Vertical offset above root (InfluenceStatusIcon usually sits ~1.8-2.2 above root)
        const float HeightOffset = 2.8f;

        StudentAgent agent;
        GameObject iconObj;
        TextMesh label;
        Coroutine animRoutine;
        StudentState currentState = StudentState.Calm;

        void Start()
        {
            agent = GetComponent<StudentAgent>();
            if (agent == null) return;

            CreateIcon();
            agent.OnStateChanged += HandleStateChanged;
            UpdateIcon(agent.CurrentState);
        }

        void OnDestroy()
        {
            if (agent != null) agent.OnStateChanged -= HandleStateChanged;
        }

        void LateUpdate()
        {
            // Billboard: always face camera
            if (iconObj != null && Camera.main != null)
            {
                iconObj.transform.LookAt(Camera.main.transform);
                iconObj.transform.Rotate(0, 180, 0);
            }
        }

        void CreateIcon()
        {
            iconObj = new GameObject("StateIndicator");
            iconObj.transform.SetParent(transform, false);
            iconObj.transform.localPosition = new Vector3(0, HeightOffset, 0);

            label = iconObj.AddComponent<TextMesh>();
            label.fontSize = 60;
            label.characterSize = 0.05f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = Color.white;
            label.text = "";
        }

        void HandleStateChanged(StudentState oldState, StudentState newState)
        {
            currentState = newState;
            UpdateIcon(newState);
        }

        void UpdateIcon(StudentState state)
        {
            if (animRoutine != null) { StopCoroutine(animRoutine); animRoutine = null; }
            if (iconObj == null) return;

            // Reset transform
            iconObj.transform.localPosition = new Vector3(0, HeightOffset, 0);
            iconObj.transform.localScale = Vector3.one;

            switch (state)
            {
                case StudentState.Calm:
                    label.text = "";
                    iconObj.SetActive(false);
                    break;

                case StudentState.Distracted:
                    iconObj.SetActive(true);
                    label.text = "...";
                    label.color = new Color(1f, 0.92f, 0.3f);
                    animRoutine = StartCoroutine(FadeBreath());
                    break;

                case StudentState.ActingOut:
                    iconObj.SetActive(true);
                    label.text = "!";
                    label.color = new Color(1f, 0.5f, 0.1f);
                    animRoutine = StartCoroutine(Bounce());
                    break;

                case StudentState.Critical:
                    iconObj.SetActive(true);
                    label.text = "!!!";
                    label.color = new Color(1f, 0.1f, 0.1f);
                    animRoutine = StartCoroutine(FlashAndPulse());
                    break;
            }
        }

        // Distracted: soft fade in/out (breathing)
        IEnumerator FadeBreath()
        {
            while (true)
            {
                float a = 0.5f + 0.5f * Mathf.Sin(Time.time * 1.5f);
                if (label != null)
                {
                    Color c = label.color;
                    c.a = a;
                    label.color = c;
                }
                yield return null;
            }
        }

        // ActingOut: small bounce up and down
        IEnumerator Bounce()
        {
            while (true)
            {
                float y = HeightOffset + Mathf.Abs(Mathf.Sin(Time.time * 4f)) * 0.12f;
                if (iconObj != null)
                    iconObj.transform.localPosition = new Vector3(0, y, 0);
                yield return null;
            }
        }

        // Critical: fast flash + scale pulse
        IEnumerator FlashAndPulse()
        {
            while (true)
            {
                float t = Time.time;
                // Flash: visible at high frequency
                bool visible = (Mathf.Sin(t * 10f) > 0f);
                if (label != null)
                {
                    Color c = label.color;
                    c.a = visible ? 1f : 0.2f;
                    label.color = c;
                }
                // Scale pulse
                float scale = 1f + Mathf.Sin(t * 4f) * 0.1f;
                if (iconObj != null)
                    iconObj.transform.localScale = Vector3.one * scale;
                yield return null;
            }
        }
    }
}
