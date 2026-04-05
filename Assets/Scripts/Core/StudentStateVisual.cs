using UnityEngine;
using System.Collections;

namespace FunClass.Core
{
    /// <summary>
    /// Phase 2: Smooth color/emission tint on model renderer that tracks StudentAgent state.
    /// Uses MaterialPropertyBlock for performance (no material clones per frame).
    /// </summary>
    public class StudentStateVisual : MonoBehaviour
    {
        // ----- State colors -----
        static readonly Color ColorCalm        = new Color(1.00f, 1.00f, 1.00f, 1f); // white (neutral)
        static readonly Color ColorDistracted  = new Color(1.00f, 0.92f, 0.40f, 1f); // soft yellow
        static readonly Color ColorActingOut   = new Color(1.00f, 0.50f, 0.10f, 1f); // orange
        static readonly Color ColorCritical    = new Color(1.00f, 0.15f, 0.10f, 1f); // red

        static readonly Color EmissionOff      = Color.black;
        static readonly Color EmissionCritical = new Color(0.6f, 0.0f, 0.0f);        // faint red glow

        const float TransitionDuration = 0.5f;

        // ----- Runtime -----
        StudentAgent agent;
        Renderer[] renderers;
        MaterialPropertyBlock mpb;

        Color currentTint;
        Color currentEmission;
        Coroutine lerpRoutine;

        void Start()
        {
            agent = GetComponent<StudentAgent>();
            if (agent == null) return;

            renderers = GetComponentsInChildren<Renderer>(true);
            mpb = new MaterialPropertyBlock();

            currentTint     = ColorCalm;
            currentEmission = EmissionOff;
            ApplyToAll(currentTint, currentEmission);

            agent.OnStateChanged += HandleStateChanged;
        }

        void OnDestroy()
        {
            if (agent != null) agent.OnStateChanged -= HandleStateChanged;
        }

        void HandleStateChanged(StudentState oldState, StudentState newState)
        {
            Color targetTint;
            Color targetEmission;
            StateToColors(newState, out targetTint, out targetEmission);

            if (lerpRoutine != null) StopCoroutine(lerpRoutine);
            lerpRoutine = StartCoroutine(LerpColors(currentTint, currentEmission, targetTint, targetEmission));
        }

        IEnumerator LerpColors(Color fromTint, Color fromEmission, Color toTint, Color toEmission)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / TransitionDuration;
                currentTint     = Color.Lerp(fromTint,     toTint,     t);
                currentEmission = Color.Lerp(fromEmission, toEmission, t);
                ApplyToAll(currentTint, currentEmission);
                yield return null;
            }
            currentTint     = toTint;
            currentEmission = toEmission;
            ApplyToAll(currentTint, currentEmission);
            lerpRoutine = null;
        }

        void ApplyToAll(Color tint, Color emission)
        {
            if (renderers == null || mpb == null) return;
            foreach (var r in renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(mpb);
                mpb.SetColor("_BaseColor", tint);
                mpb.SetColor("_EmissionColor", emission);
                r.SetPropertyBlock(mpb);
            }
        }

        static void StateToColors(StudentState state, out Color tint, out Color emission)
        {
            switch (state)
            {
                case StudentState.Distracted:
                    tint = ColorDistracted;  emission = EmissionOff;      break;
                case StudentState.ActingOut:
                    tint = ColorActingOut;   emission = EmissionOff;      break;
                case StudentState.Critical:
                    tint = ColorCritical;    emission = EmissionCritical; break;
                default: // Calm
                    tint = ColorCalm;        emission = EmissionOff;      break;
            }
        }
    }
}
