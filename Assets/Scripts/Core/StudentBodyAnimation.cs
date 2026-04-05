using UnityEngine;
using System.Collections;

namespace FunClass.Core
{
    /// <summary>
    /// Phase 4: Procedural transform animations on the Model/Visual child.
    /// Root stays still for NavMeshAgent; only the model child moves.
    /// Continuous state animations + one-shot interaction animations.
    /// </summary>
    public class StudentBodyAnimation : MonoBehaviour
    {
        StudentAgent agent;
        Transform modelChild;

        // Lerp between animation intensities for smooth transitions
        float currentIntensity = 0f;
        float targetIntensity  = 0f;
        const float IntensityLerpSpeed = 2f; // units/sec

        StudentState currentState = StudentState.Calm;
        bool oneShotActive = false;

        // Baseline local transform of model child
        Vector3    baseLocalPos;
        Quaternion baseLocalRot;
        Vector3    baseLocalScale;

        void Start()
        {
            agent = GetComponent<StudentAgent>();
            if (agent == null) return;

            // Find the model/visual child
            modelChild = transform.Find("Model") ?? transform.Find("Visual");
            if (modelChild == null && transform.childCount > 0)
                modelChild = transform.GetChild(0); // first child as fallback

            if (modelChild == null)
            {
                Debug.LogWarning($"[StudentBodyAnimation] No model child found on {gameObject.name}");
                return;
            }

            baseLocalPos   = modelChild.localPosition;
            baseLocalRot   = modelChild.localRotation;
            baseLocalScale = modelChild.localScale;

            agent.OnStateChanged += HandleStateChanged;
            SetIntensityForState(agent.CurrentState);
            currentState = agent.CurrentState;
        }

        void OnDestroy()
        {
            if (agent != null) agent.OnStateChanged -= HandleStateChanged;
        }

        void HandleStateChanged(StudentState old, StudentState newState)
        {
            currentState = newState;
            SetIntensityForState(newState);
        }

        void SetIntensityForState(StudentState state)
        {
            switch (state)
            {
                case StudentState.Calm:       targetIntensity = 0.02f; break;
                case StudentState.Distracted: targetIntensity = 1.00f; break;
                case StudentState.ActingOut:  targetIntensity = 2.00f; break;
                case StudentState.Critical:   targetIntensity = 3.00f; break;
            }
        }

        void Update()
        {
            if (modelChild == null || oneShotActive) return;

            // Lerp intensity
            currentIntensity = Mathf.MoveTowards(currentIntensity, targetIntensity, IntensityLerpSpeed * Time.deltaTime);

            float t = Time.time;
            Vector3    pos   = baseLocalPos;
            Quaternion rot   = baseLocalRot;
            Vector3    scale = baseLocalScale;

            switch (currentState)
            {
                case StudentState.Calm:
                    // Very slow breathing (barely visible)
                    pos.y += Mathf.Sin(t * 0.5f) * 0.02f * currentIntensity;
                    break;

                case StudentState.Distracted:
                    // Fidgety rotation + lean
                    float dRotY = Mathf.Sin(t * 2f) * 5f * currentIntensity;
                    float dRotZ = Mathf.Sin(t * 1.5f) * 3f * currentIntensity;
                    rot = baseLocalRot * Quaternion.Euler(0, dRotY, dRotZ);
                    break;

                case StudentState.ActingOut:
                    // Faster shake + small hop
                    float aRotY = Mathf.Sin(t * 4f) * 10f * (currentIntensity * 0.5f);
                    float aHop  = Mathf.Abs(Mathf.Sin(t * 3f)) * 0.05f * currentIntensity;
                    pos.y += aHop;
                    rot = baseLocalRot * Quaternion.Euler(0, aRotY, 0);
                    break;

                case StudentState.Critical:
                    // Chaotic shake
                    float cRotY  = Mathf.Sin(t * 6f) * 15f * (currentIntensity * 0.33f);
                    float cShake = Mathf.PerlinNoise(t * 8f, 0f) * 0.06f * currentIntensity - 0.03f * currentIntensity;
                    float cPulse = 1f + Mathf.Sin(t * 4f) * 0.05f;
                    pos.x += cShake;
                    rot    = baseLocalRot * Quaternion.Euler(0, cRotY, 0);
                    scale  = baseLocalScale * cPulse;
                    break;
            }

            modelChild.localPosition = pos;
            modelChild.localRotation = rot;
            modelChild.localScale    = scale;
        }

        // ------------------------------------------------------------------
        // One-shot interaction animations — call from interaction processors
        // ------------------------------------------------------------------

        public void PlayKnockedOver(Transform target)   => PlayOneShot(KnockedOverAnim(target));
        public void PlayThrowObject(Transform target)   => PlayOneShot(ThrowAnim(target));
        public void PlayWandering()                     => PlayOneShot(WanderingAnim());
        public void PlayCalmReaction()                  => PlayOneShot(CalmReactionAnim());

        void PlayOneShot(IEnumerator anim)
        {
            if (modelChild == null) return;
            StopAllCoroutines();
            StartCoroutine(OneShotWrapper(anim));
        }

        IEnumerator OneShotWrapper(IEnumerator anim)
        {
            oneShotActive = true;
            yield return StartCoroutine(anim);
            oneShotActive = false;
            // Snap back to baseline; Update() will resume from there
            modelChild.localPosition = baseLocalPos;
            modelChild.localRotation = baseLocalRot;
            modelChild.localScale    = baseLocalScale;
        }

        // Lean toward target then return (0.5s)
        IEnumerator KnockedOverAnim(Transform target)
        {
            Vector3 dir = target != null
                ? (target.position - transform.position).normalized
                : Vector3.forward;
            dir.y = 0;
            Quaternion tiltTo = baseLocalRot * Quaternion.AngleAxis(30f, Vector3.Cross(Vector3.up, dir));
            yield return LerpRot(baseLocalRot, tiltTo, 0.2f);
            yield return LerpRot(tiltTo, baseLocalRot, 0.3f);
        }

        // Quick jerk toward target (0.3s)
        IEnumerator ThrowAnim(Transform target)
        {
            Vector3 dir = target != null
                ? (target.position - transform.position).normalized
                : Vector3.forward;
            dir.y = 0;
            Vector3 jerk   = baseLocalPos + dir * 0.1f;
            yield return LerpPos(baseLocalPos, jerk,          0.1f);
            yield return LerpPos(jerk,         baseLocalPos,  0.2f);
        }

        // Slow 360 rotation (2s)
        IEnumerator WanderingAnim()
        {
            float elapsed = 0f;
            float duration = 2f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float angle = (elapsed / duration) * 360f;
                modelChild.localRotation = baseLocalRot * Quaternion.Euler(0, angle, 0);
                yield return null;
            }
        }

        // Shrink + bounce (relief, used when teacher calms)
        IEnumerator CalmReactionAnim()
        {
            yield return LerpScale(baseLocalScale, baseLocalScale * 0.92f, 0.15f);
            yield return LerpScale(baseLocalScale * 0.92f, baseLocalScale * 1.05f, 0.1f);
            yield return LerpScale(baseLocalScale * 1.05f, baseLocalScale, 0.1f);
        }

        // ----- Lerp helpers -----

        IEnumerator LerpRot(Quaternion from, Quaternion to, float duration)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                if (modelChild != null) modelChild.localRotation = Quaternion.Slerp(from, to, t);
                yield return null;
            }
        }

        IEnumerator LerpPos(Vector3 from, Vector3 to, float duration)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                if (modelChild != null) modelChild.localPosition = Vector3.Lerp(from, to, t);
                yield return null;
            }
        }

        IEnumerator LerpScale(Vector3 from, Vector3 to, float duration)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                if (modelChild != null) modelChild.localScale = Vector3.Lerp(from, to, t);
                yield return null;
            }
        }
    }
}
