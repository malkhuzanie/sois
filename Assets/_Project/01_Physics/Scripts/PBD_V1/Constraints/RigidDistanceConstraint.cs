// Assets/_Project/01_Physics/Scripts/PBD/RigidDistanceConstraint.cs

using System.Collections.Generic;
using UnityEngine;

namespace _Project._01_Physics.Scripts.PBD_V1.Constraints
{
    /// <summary>
    /// Ultra-strong distance constraint for elastic objects that NEVER breaks
    /// and aggressively maintains original distances
    /// </summary>
    public class RigidDistanceConstraint : PBDConstraint
    {
        public int ParticleA;
        public int ParticleB;
        public float RestLength;
        public float RigidStiffness = 1.0f;

        public RigidDistanceConstraint(int particleA, int particleB, float restLength, float stiffness = 1.0f)
        {
            ParticleA = particleA;
            ParticleB = particleB;
            RestLength = restLength;
            RigidStiffness = stiffness;
            Stiffness = stiffness;
            
            // NEVER breaks, no stress accumulation
            CanBreak = false;
            BreakThreshold = float.MaxValue;
        }

        public override void SolveConstraint(List<PBDParticle> particles, float globalStiffness)
        {
            if (!IsActive || ParticleA >= particles.Count || ParticleB >= particles.Count)
                return;

            var pA = particles[ParticleA];
            var pB = particles[ParticleB];
            
            if (!pA.IsActive || !pB.IsActive) return;

            // Calculate current distance
            Vector3 delta = pB.PredictedPosition - pA.PredictedPosition;
            float currentLength = delta.magnitude;

            if (currentLength < 0.0001f) return; // Avoid division by zero

            // Calculate constraint violation
            float constraint = currentLength - RestLength;

            if (Mathf.Abs(constraint) < 0.0001f) return; // Already satisfied

            // Calculate correction direction
            Vector3 direction = delta / currentLength;

            // Calculate correction amount based on inverse masses
            float totalInverseMass = pA.InverseMass + pB.InverseMass;
            if (totalInverseMass < 0.0001f) return; // Both particles are fixed

            // VERY AGGRESSIVE correction for rigid constraints
            float effectiveStiffness = RigidStiffness * globalStiffness;
            Vector3 correction = direction * (constraint * effectiveStiffness);

            // Apply corrections proportional to inverse masses
            if (pA.InverseMass > 0)
            {
                pA.PredictedPosition += correction * (pA.InverseMass / totalInverseMass);
            }

            if (pB.InverseMass > 0)
            {
                pB.PredictedPosition -= correction * (pB.InverseMass / totalInverseMass);
            }
        }

        public override bool IsSatisfied(List<PBDParticle> particles, float tolerance = 0.001f)
        {
            if (!IsActive || ParticleA >= particles.Count || ParticleB >= particles.Count)
                return true;

            var pA = particles[ParticleA];
            var pB = particles[ParticleB];

            float currentLength = Vector3.Distance(pA.PredictedPosition, pB.PredictedPosition);
            return Mathf.Abs(currentLength - RestLength) < tolerance;
        }
    }
}