using UnityEngine;

namespace FunClass.Core
{
    /// <summary>
    /// Phase 5.1: Particle effects for state transitions and teacher calm.
    /// All particle systems are created at runtime; max 20 particles per system.
    /// </summary>
    public class StudentStateParticles : MonoBehaviour
    {
        StudentAgent agent;

        // Burst on every state transition (one-shot)
        ParticleSystem transitionBurst;

        // Continuous smoke-like particles only while Critical
        ParticleSystem criticalLoop;

        // One-shot sparkle played externally by TeacherController
        ParticleSystem calmSparkle;

        void Start()
        {
            agent = GetComponent<StudentAgent>();
            if (agent == null) return;

            transitionBurst = CreateBurst();
            criticalLoop    = CreateCriticalLoop();
            calmSparkle     = CreateSparkle();

            agent.OnStateChanged += HandleStateChanged;
            UpdateCriticalLoop(agent.CurrentState);
        }

        void OnDestroy()
        {
            if (agent != null) agent.OnStateChanged -= HandleStateChanged;
        }

        // ------------------------------------------------------------------ //
        //  Public API                                                          //
        // ------------------------------------------------------------------ //

        /// Called by TeacherController after calming a student.
        public void PlayCalmSparkle()
        {
            if (calmSparkle == null) return;
            calmSparkle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            calmSparkle.Play();
        }

        // ------------------------------------------------------------------ //
        //  Internal                                                            //
        // ------------------------------------------------------------------ //

        void HandleStateChanged(StudentState old, StudentState newState)
        {
            PlayTransitionBurst(newState);
            UpdateCriticalLoop(newState);
        }

        void PlayTransitionBurst(StudentState newState)
        {
            if (transitionBurst == null) return;
            var main = transitionBurst.main;
            main.startColor = StateToColor(newState);
            transitionBurst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            transitionBurst.Play();
        }

        void UpdateCriticalLoop(StudentState state)
        {
            if (criticalLoop == null) return;
            if (state == StudentState.Critical)
            {
                if (!criticalLoop.isPlaying) criticalLoop.Play();
            }
            else
            {
                criticalLoop.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        // ------------------------------------------------------------------ //
        //  Factory helpers                                                     //
        // ------------------------------------------------------------------ //

        ParticleSystem CreateBurst()
        {
            var go = new GameObject("FX_Burst");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0, 1f, 0); // chest height

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop              = false;
            main.duration          = 0.3f;
            main.startLifetime     = 0.6f;
            main.startSpeed        = 1.5f;
            main.startSize         = 0.08f;
            main.maxParticles      = 20;
            main.simulationSpace   = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime  = 0f;
            var burst = new ParticleSystem.Burst(0f, 15);
            emission.SetBursts(new[] { burst });

            var shape = ps.shape;
            shape.shapeType        = ParticleSystemShapeType.Sphere;
            shape.radius           = 0.15f;

            return ps;
        }

        ParticleSystem CreateCriticalLoop()
        {
            var go = new GameObject("FX_Critical");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0, 0.2f, 0); // near feet

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop              = true;
            main.duration          = 1f;
            main.startLifetime     = 1.2f;
            main.startSpeed        = 0.6f;
            main.startSize         = new ParticleSystem.MinMaxCurve(0.04f, 0.10f);
            main.startColor        = new Color(0.9f, 0.2f, 0.1f, 0.8f);
            main.maxParticles      = 20;
            main.simulationSpace   = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime  = 8f;

            var shape = ps.shape;
            shape.shapeType        = ParticleSystemShapeType.Circle;
            shape.radius           = 0.25f;

            return ps;
        }

        ParticleSystem CreateSparkle()
        {
            var go = new GameObject("FX_Sparkle");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0, 1.5f, 0);

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop              = false;
            main.duration          = 0.5f;
            main.startLifetime     = 0.8f;
            main.startSpeed        = 2f;
            main.startSize         = 0.06f;
            main.startColor        = new Color(1f, 0.95f, 0.4f, 1f); // golden
            main.maxParticles      = 20;
            main.simulationSpace   = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime  = 0f;
            var burst = new ParticleSystem.Burst(0f, 18);
            emission.SetBursts(new[] { burst });

            var shape = ps.shape;
            shape.shapeType        = ParticleSystemShapeType.Sphere;
            shape.radius           = 0.1f;

            return ps;
        }

        static Color StateToColor(StudentState state)
        {
            switch (state)
            {
                case StudentState.Distracted: return new Color(1f, 0.92f, 0.4f);
                case StudentState.ActingOut:  return new Color(1f, 0.5f, 0.1f);
                case StudentState.Critical:   return new Color(1f, 0.15f, 0.1f);
                default:                      return new Color(0.6f, 0.9f, 0.6f); // calm = green
            }
        }
    }
}
