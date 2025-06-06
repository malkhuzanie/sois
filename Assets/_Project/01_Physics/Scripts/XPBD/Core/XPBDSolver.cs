// Assets/_Project/01_Physics/Scripts/XPBD/Core/XPBDSolver.cs

using System.Collections.Generic;
using System.Linq;
using _Project._01_Physics.Scripts.XPBD.Constraints;
using UnityEngine;

namespace _Project._01_Physics.Scripts.XPBD.Core
{
    /// <summary>
    /// Core XPBD solver with time-step independent stiffness
    /// Implements Jakobsen's approach with compliance-based parameters
    /// </summary>
    public class XPBDSolver
    {
        [Header("Solver Settings")]
        public int SolverIterations = 8;
        public int SubSteps = 4;
        public Vector3 Gravity = new(0, -9.81f, 0);
        public float GlobalDamping = 0.98f;
        
        // Collections
        public List<XPBDParticle> Particles { get; private set; }
        public List<XPBDConstraint> Constraints { get; private set; }
        
        // Performance tracking
        public float LastSolveTime { get; private set; }
        public int LastIterationsUsed { get; private set; }
        
        public XPBDSolver()
        {
            Particles = new List<XPBDParticle>();
            Constraints = new List<XPBDConstraint>();
        }
        
        /// <summary>
        /// Main XPBD simulation step
        /// </summary>
        public void Update(float deltaTime)
        {
            if (Particles.Count == 0) return;
            
            float startTime = Time.realtimeSinceStartup;
            
            // Use sub-stepping for stability
            float subDeltaTime = deltaTime / SubSteps;
            
            for (int substep = 0; substep < SubSteps; substep++)
            {
                SimulationStep(subDeltaTime);
            }
            
            LastSolveTime = Time.realtimeSinceStartup - startTime;
        }
        
        void SimulationStep(float deltaTime)
        {
            // Phase 1: Predict positions using Verlet integration
            foreach (var particle in Particles)
            {
                particle.PredictPosition(Gravity, deltaTime);
            }
            
            // Phase 2: Solve constraints iteratively
            LastIterationsUsed = 0;
            for (int iteration = 0; iteration < SolverIterations; iteration++)
            {
                LastIterationsUsed++;

                foreach (var constraint in Constraints.Where(constraint => constraint.IsActive))
                {
                    constraint.SolveConstraint(Particles, deltaTime);
                }
            }
            
            // Phase 3: Update positions and apply damping
            foreach (var particle in Particles)
            {
                particle.UpdatePosition();
                particle.ApplyDamping(GlobalDamping);
            }
        }
        
        /// <summary>
        /// Add particle to simulation
        /// </summary>
        public int AddParticle(Vector3 position, float mass = 1.0f, int vertexIndex = -1)
        {
            var particle = new XPBDParticle(position, mass, vertexIndex);
            Particles.Add(particle);
            return Particles.Count - 1;
        }
        
        /// <summary>
        /// Add constraint to simulation
        /// </summary>
        public void AddConstraint(XPBDConstraint constraint)
        {
            Constraints.Add(constraint);
        }
        
        /// <summary>
        /// Clear all particles and constraints
        /// </summary>
        public void Clear()
        {
            Particles.Clear();
            Constraints.Clear();
        }
        
        /// <summary>
        /// Get solver statistics
        /// </summary>
        public (int particles, int constraints, float solveTime, int iterations) GetStatistics()
        {
            return (Particles.Count, Constraints.Count, LastSolveTime, LastIterationsUsed);
        }
        
        /// <summary>
        /// Apply impulse to particles within radius
        /// </summary>
        public void ApplyImpulse(Vector3 center, Vector3 impulse, float radius, float deltaTime)
        {
            foreach (var particle in Particles)
            {
                float distance = Vector3.Distance(particle.Position, center);
                if (distance < radius)
                {
                    float falloff = 1.0f - (distance / radius);
                    particle.ApplyImpulse(impulse * falloff, deltaTime);
                }
            }
        }
        
        /// <summary>
        /// Reset simulation to initial state
        /// </summary>
        public void Reset()
        {
            foreach (var particle in Particles)
            {
                particle.PreviousPosition = particle.Position;
                particle.PredictedPosition = particle.Position;
            }
        }
    }
}